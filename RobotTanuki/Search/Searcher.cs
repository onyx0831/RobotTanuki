using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static RobotTanuki.Evaluator;

namespace RobotTanuki
{
    public class Searcher
    {
        private static readonly TranspositionTable table = new TranspositionTable(1 << 20);

        // intの範囲で安全に符号反転できる大きさ。詰みが今起きた瞬間の値としても使う。
        private const int Infinity = 1_000_000_000;

        // 減衰させても通常の評価値と混同しない余裕を持たせた閾値。
        private const int MateThreshold = Infinity - 1000;

        public static bool IsMateScore(int value)
        {
            return Math.Abs(value) > MateThreshold;
        }

        /// <summary>正なら詰ますまでの手数、負なら詰まされるまでの手数。</summary>
        public static int PliesUntilMate(int value)
        {
            return value > 0 ? Infinity - value : -(Infinity + value);
        }

        /// <summary>
        /// 深さ1から順に探索し、完了した深さごとにonDepthCompletedを呼ぶ。
        /// cancellationTokenが要求された時点では直前に完了した深さの結果を返す（深さ1は必ず完了させる）。
        /// </summary>
        public static BestMove SearchIterative(Position position, int maxDepth, CancellationToken cancellationToken, out int totalNodes, Action<BestMove, int, int>? onDepthCompleted = null)
        {
            int nodes = 0;
            var bestMove = new BestMove { Move = Move.Resign, Value = -Infinity };

            for (int depth = 1; depth <= maxDepth; ++depth)
            {
                // 深さ1は思考時間がどれだけ短くても必ず完了させ、指す手が必ずある状態を保証する。
                var tokenForThisDepth = depth == 1 ? CancellationToken.None : cancellationToken;

                BestMove result;
                try
                {
                    result = Search(position, depth, -Infinity, Infinity, ref nodes, tokenForThisDepth);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                bestMove = result;
                bestMove.Depth = depth;
                onDepthCompleted?.Invoke(bestMove, depth, nodes);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            totalNodes = nodes;
            return bestMove;
        }

        /// <summary>
        /// ネガマックス形式のアルファベータ法で探索する。
        /// </summary>
        private static BestMove Search(Position position, int depth, int alpha, int beta, ref int nodes, CancellationToken cancellationToken)
        {
            if (depth == 0)
            {
                return new BestMove
                {
                    Move = Move.None,
                    Value = Evaluator.Evaluate(position),
                };
            }

            ulong hash = position.Hash;
            int originalAlpha = alpha;
            Move? ttMove = null;
            if (table.TryGet(hash, out var ttEntry))
            {
                ttMove = ttEntry.BestMove;
                if (ttEntry.Depth >= depth)
                {
                    if (ttEntry.Bound == TranspositionTableBound.Exact
                        || (ttEntry.Bound == TranspositionTableBound.LowerBound && ttEntry.Value >= beta)
                        || (ttEntry.Bound == TranspositionTableBound.UpperBound && ttEntry.Value <= alpha))
                    {
                        return new BestMove { Move = ttEntry.BestMove, Value = ttEntry.Value };
                    }
                }
            }

            int bestValue = -Infinity;
            Move bestMove = Move.Resign;
            BestMove? bestChildMove = null;
            // GenerateLegal()後に並べ替えると遅延評価が効かず、枝刈りで省けるはずの
            // 合法性チェックまで全手分先に実行してしまうため、擬似合法手の段階で並べ替える。
            var moves = MoveGenerator.Generate(position).OrderByDescending(move => ScoreForOrdering(move, ttMove));
            foreach (var move in moves)
            {
                if (!MoveGenerator.IsLegal(position, move))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                ++nodes;
                BestMove childBestMove;
                position.DoMove(move);
                try
                {
                    childBestMove = Search(position, depth - 1, -beta, -alpha, ref nodes, cancellationToken);
                }
                finally
                {
                    // キャンセルで例外が飛んでもUndoMoveを必ず実行し、局面を壊さない。
                    position.UndoMove(move);
                }

                int value = -childBestMove.Value;
                if (IsMateScore(value))
                {
                    // 速い詰みほど絶対値が大きくなるよう、1手伝播するごとに1減らす。
                    value += value > 0 ? -1 : 1;
                }

                if (bestValue < value)
                {
                    bestValue = value;
                    bestMove = move;
                    bestChildMove = childBestMove;
                }

                if (alpha < bestValue)
                {
                    alpha = bestValue;
                }

                if (beta <= alpha)
                {
                    break;
                }
            }

            var bound = bestValue <= originalAlpha ? TranspositionTableBound.UpperBound
                : bestValue >= beta ? TranspositionTableBound.LowerBound
                : TranspositionTableBound.Exact;
            table.Store(hash, depth, bestValue, bestMove, bound);

            return new BestMove
            {
                Move = bestMove,
                Value = bestValue,
                Next = bestChildMove,
            };
        }

        /// <summary>指し手オーダリング用のスコア（置換表の手を最優先、次にMVV-LVA）。</summary>
        private static int ScoreForOrdering(Move move, Move? ttMove)
        {
            if (ttMove != null && move.Equals(ttMove))
            {
                return int.MaxValue;
            }

            if (move.PieceTo == Piece.NoPiece)
            {
                return 0;
            }

            return Evaluator.GetPieceValue(move.PieceTo) * 10 - Evaluator.GetPieceValue(move.PieceFrom);
        }
    }
}