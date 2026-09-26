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
        /// </summary>
        private const int Infinity = 1_000_000_000;

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
                if (bestValue < value)
                {
                    bestValue = value;
                    bestMove = move;
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