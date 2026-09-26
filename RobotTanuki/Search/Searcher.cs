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
        /// <summary>
        /// 評価値の「無限大」を表す値。intの範囲内で安全に符号反転できる大きさにしている。
        /// 詰みの基準値（詰みが今起きた瞬間の値）としても使う。
        /// </summary>
        private const int Infinity = 1_000_000_000;

        /// <summary>
        /// 評価値の絶対値がこれを超えていたら「詰み絡みの値」とみなす閾値。
        /// 詰みまでの手数ぶんInfinityから減衰させても、通常の評価値と混同しない余裕を持たせている。
        /// </summary>
        private const int MateThreshold = Infinity - 1000;

        /// <summary>
        /// 評価値が詰み絡みの値かどうかを判定する。
        /// </summary>
        public static bool IsMateScore(int value)
        {
            return Math.Abs(value) > MateThreshold;
        }

        /// <summary>
        /// 詰み絡みの評価値から、詰みまでの手数を符号付きで返す。
        /// 正なら自分が詰ますまでの手数、負なら自分が詰まされるまでの手数。
        /// </summary>
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

            int bestValue = -Infinity;
            Move bestMove = Move.Resign;
            BestMove? bestChildMove = null;
            // 擬似合法手（生成コストが軽い）の段階で並べ替え、合法性チェックは1手ずつ遅延評価する。
            // 先にGenerateLegal()で合法手化してから並べ替えると、枝刈りで不要になったはずの
            // 合法性チェック（DoMove/UndoMoveを伴う重い処理）まで全手分先に済ませてしまい、かえって遅くなる。
            var moves = MoveGenerator.Generate(position).OrderByDescending(ScoreForOrdering);
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
                    // 1手分伝播するごとに、詰みまでの距離を1手分伸ばす（＝評価値の絶対値を1減らす）。
                    // こうすることで、同じ詰みでも近い（速い）詰みほど評価値の絶対値が大きくなり、優先される。
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
                    // betaカット: これ以上調べても親のalphaを超えられないため打ち切る
                    break;
                }
            }

            return new BestMove
            {
                Move = bestMove,
                Value = bestValue,
                Next = bestChildMove,
            };
        }

        /// <summary>
        /// 指し手オーダリング用のスコア（MVV-LVA）。駒を取らない手は0（元の生成順のまま）。
        /// 取る手は、取られる駒の価値が高いほど、攻撃する駒の価値が安いほど優先される。
        /// </summary>
        private static int ScoreForOrdering(Move move)
        {
            if (move.PieceTo == Piece.NoPiece)
            {
                return 0;
            }

            return Evaluator.GetPieceValue(move.PieceTo) * 10 - Evaluator.GetPieceValue(move.PieceFrom);
        }
    }
}