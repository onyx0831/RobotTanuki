using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static RobotTanuki.Evaluator;

namespace RobotTanuki
{
    public class Searcher
    {
        public static BestMove Search(Position position, int depth, ref int nodes)
        {
            if (depth == 0)
            {
                return new BestMove
                {
                    Move = Move.None,
                    Value = Evaluator.Evaluate(position),
                };
            }

            int bestValue = int.MinValue;
            Move bestMove = Move.Resign;
            foreach (var move in MoveGenerator.Generate(position))
            {
                // HACK: 相手の玉を取る手なら、early returnする。
                if (move.PieceTo == Piece.BlackKing || move.PieceTo == Piece.WhiteKing)
                {
                    return new BestMove
                    {
                        Move = move,
                        Value = int.MaxValue,
                    };
                }

                ++nodes;
                position.DoMove(move);
                BestMove childBestMove = Search(position, depth - 1, ref nodes);
                position.UndoMove(move);

                if (bestValue < -childBestMove.Value)
                {
                    bestValue = -childBestMove.Value;
                    bestMove = move;
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