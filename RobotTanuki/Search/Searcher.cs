using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public static BestMove Search(Position position, int depth, ref int nodes)
        {
            return Search(position, depth, -Infinity, Infinity, ref nodes);
        }

        /// <summary>
        /// ネガマックス形式のアルファベータ法で探索する。
        /// </summary>
        private static BestMove Search(Position position, int depth, int alpha, int beta, ref int nodes)
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

                ++nodes;
                position.DoMove(move);
                BestMove childBestMove = Search(position, depth - 1, -beta, -alpha, ref nodes);
                position.UndoMove(move);

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

        /// <summary>
        /// 指し手オーダリング用のスコア（MVV-LVA）。置換表に記録されている手を最優先にし、
        /// それ以外は駒を取らない手を0（元の生成順のまま）、取る手は取られる駒の価値が高いほど、
        /// 攻撃する駒の価値が安いほど優先する。
        /// </summary>
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