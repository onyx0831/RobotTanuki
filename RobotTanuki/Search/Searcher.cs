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

        // 静止探索の延長上限。取り合い・王手が続く限り延長するが、際限なく続かないための安全弁。
        private const int QuiescenceMaxPly = 32;

        // Null Move Pruningを試す最小の残り深さ。浅すぎる場所で使うと縮小後の深さが0未満になり得るため。
        private const int NullMoveMinDepth = 3;

        // パスした後に探索する深さの減らし幅。
        private const int NullMoveReduction = 2;

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
        private static BestMove Search(Position position, int depth, int alpha, int beta, ref int nodes, CancellationToken cancellationToken, bool allowNullMove = true)
        {
            if (depth == 0)
            {
                return QuiescenceSearch(position, alpha, beta, QuiescenceMaxPly, ref nodes, cancellationToken);
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

            // Null Move Pruning: 一手パスしても（＝相手に手番をそのまま渡しても）なおbeta以上なら、
            // 自分が指せば当然beta以上のはずなので、全ての指し手を調べずに打ち切る。
            // 王手中にパスするのは不自然（王手放置になる）なので対象外。詰みが絡む窓では
            // 誤ったmateスコアの打ち切りを避けるため対象外にする。連続パスは同じ局面を深さだけ
            // 減らして読み直すだけの無駄な探索になるため、パスの直後は再度パスできないようにする。
            if (allowNullMove
                && depth >= NullMoveMinDepth
                && !IsMateScore(beta)
                && !MoveGenerator.IsInCheck(position, position.SideToMove))
            {
                position.DoNullMove();
                int nullMoveValue;
                try
                {
                    nullMoveValue = -Search(position, depth - 1 - NullMoveReduction, -beta, -beta + 1, ref nodes, cancellationToken, allowNullMove: false).Value;
                }
                finally
                {
                    position.UndoNullMove();
                }

                if (nullMoveValue >= beta)
                {
                    return new BestMove { Move = Move.None, Value = beta };
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

        /// <summary>
        /// 静止探索。駒を取り合っている最中に探索を打ち切ると、駒を取られる直前で評価してしまう
        /// 「地平線効果」が起きるため、取り合いが落ち着くまで（駒を取る手が尽きるまで）延長して読む。
        /// 王手されている場合はstand pat（今の評価値をそのまま採用する）をせず、合法な応手を全て読む。
        /// </summary>
        private static BestMove QuiescenceSearch(Position position, int alpha, int beta, int ply, ref int nodes, CancellationToken cancellationToken)
        {
            bool inCheck = MoveGenerator.IsInCheck(position, position.SideToMove);
            // 王手されている間はstand patを使わないので、Evaluateの呼び出し（利きの計算を含み重い）は
            // 実際に値が必要になる場合（王手されていない場合、または下のply<=0の安全弁）まで遅らせる。
            int standPat = inCheck ? 0 : Evaluator.Evaluate(position);

            if (!inCheck)
            {
                if (standPat >= beta)
                {
                    return new BestMove { Move = Move.None, Value = standPat };
                }
                if (alpha < standPat)
                {
                    alpha = standPat;
                }
            }

            if (ply <= 0)
            {
                // 延長の安全弁。ここまで来たら取り合い・王手の連続が続いていても評価値で打ち切る。
                return new BestMove { Move = Move.None, Value = inCheck ? Evaluator.Evaluate(position) : standPat };
            }

            int bestValue = inCheck ? -Infinity : standPat;
            Move bestMove = Move.Resign;
            BestMove? bestChildMove = null;

            // 王手されていなければ「駒を取る手」「成る手」だけ、王手されていれば全ての合法手（回避手）を読む。
            // 成る手も対象にすることで、成り捨てや、成った直後に取り返される手を静止探索で検知できるようにする。
            var moves = MoveGenerator.Generate(position)
                .Where(move => inCheck || move.PieceTo != Piece.NoPiece || move.Promotion)
                .OrderByDescending(move => ScoreForOrdering(move, null));
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
                    childBestMove = QuiescenceSearch(position, -beta, -alpha, ply - 1, ref nodes, cancellationToken);
                }
                finally
                {
                    position.UndoMove(move);
                }

                int value = -childBestMove.Value;
                if (IsMateScore(value))
                {
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

            if (inCheck && bestMove == Move.Resign)
            {
                // 王手を回避する合法手が1つもない＝詰み。
                return new BestMove { Move = Move.Resign, Value = -Infinity };
            }

            return new BestMove
            {
                Move = bestMove == Move.Resign ? Move.None : bestMove,
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