using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static RobotTanuki.Types;
using static System.Math;

namespace RobotTanuki
{
    /// <summary>局面を表すデータ構造</summary>
    public class Position
    {
        public const int BoardSize = 9;  // 盤面の一辺のマス数

        /// <summary>手番</summary>
        public Color SideToMove { get; set; }

        /// <summary>盤面の状態</summary>
        public Piece[,] Board { get; } = new Piece[BoardSize, BoardSize];

        /// <summary>持ち駒（各駒の枚数）</summary>
        public int[] HandPieces { get; } = new int[(int)Piece.NumPieces];

        /// <summary>手数</summary>
        public int Play { get; set; }
        
        
        /// <summary>
        /// 局面を文字列化する。sfenではなく独自形式とする。
        /// </summary>
        public override String ToString()
        {
            var writer = new StringWriter();
            writer.WriteLine("+----+----+----+----+----+----+----+----+----+");
            for (int rank = 0; rank < BoardSize; ++rank)
            {
                writer.Write("|");
                for (int file = BoardSize - 1; file >= 0; --file)
                {
                    writer.Write(Board[file, rank].ToHumanReadableString());
                    writer.Write("|");
                }
                writer.WriteLine();

                writer.WriteLine("+----+----+----+----+----+----+----+----+----+");
            }

            writer.Write("先手 手駒 : ");
            for (var piece = Piece.BlackPawn; piece < Piece.WhitePawn; ++piece)
            {
                for (int i = 0; i < HandPieces[(int)piece]; ++i)
                {
                    writer.Write(piece.ToHumanReadableString().Trim()[0]);
                }
            }

            writer.Write(" , 後手 手駒 : ");
            for (var piece = Piece.WhitePawn; piece < Piece.NumPieces; ++piece)
            {
                for (int i = 0; i < HandPieces[(int)piece]; ++i)
                {
                    writer.Write(piece.ToHumanReadableString().Trim()[0]);
                }
            }
            writer.WriteLine();

            writer.Write("手番 = ");
            writer.Write(SideToMove == Color.Black ? "先手" : "後手");
            writer.WriteLine();

            return writer.ToString();
        }
    }
}