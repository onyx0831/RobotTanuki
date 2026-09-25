using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            foreach (var move in MoveGenerator.GenerateLegal(position))
            {
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
    }
}