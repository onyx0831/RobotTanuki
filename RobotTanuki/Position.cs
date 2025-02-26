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
        public const string StartposSfen = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1";
        
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
        /// sfen文字列をセットする
        /// </summary>

        public void Set(string sfen)
        {
            SideToMove = Color.Black;
            Array.Clear(Board, 0, Board.Length);
            Array.Clear(HandPieces, 0, HandPieces.Length);
            Play = 1;

            // 盤面パース
            int file = BoardSize - 1;
            int rank = 0;
            int index = 0;
            bool promotion = false;
            while (true)
            {
                var ch = sfen[index++];
                if (ch == ' ') break;
                if (ch == '/')
                {
                    rank++;
                    file = BoardSize - 1;
                }
                else if (ch == '+')
                {
                    promotion = true;
                }
                else if (Char.IsDigit(ch))
                {
                    int emptySquares = ch -'0';
                    while (emptySquares-- > 0)
                        Board[file--, rank] = Piece.NoPiece;
                }
                else
                {
                    var piece = Types.CharToPiece[ch];
                    Debug.Assert(piece != Piece.NoPiece);
                    Board[file--, rank] = promotion ? piece.AsPromoted() : piece;
                    promotion = false;
                }
            }

            // 手番パース
            SideToMove = sfen[index++] == 'b' ? Color.Black : Color.White;
            index++;

            // 持ち駒パース
            int count = 0;
            while (true)
            {
                var ch = sfen[index++];
                if (ch == ' ') break;
                if (ch == '-') continue;

                if (Char.IsDigit(ch))
                {
                    count = count * 10 + (ch - '0');
                    continue;
                }
                
                var piece = Types.CharToPiece[ch];
                Debug.Assert(piece != Piece.NoPiece);
                HandPieces[(int)piece] += Math.Max(1, count);
                count = 0;
            }

            // 手数パース
            Play = int.Parse(sfen.Substring(index));

        }

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