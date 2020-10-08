﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using static RocketTanuki.Types;

namespace RocketTanuki
{
    /// <summary>
    /// 局面を表すデータ構造
    /// </summary>
    public class Position
    {
        public const int BoardSize = 9;
        public Color SideToMove { get; set; }
        public Piece[,] Board { get; } = new Piece[BoardSize, BoardSize];
        public int[] HandPieces { get; } = new int[(int)Piece.NumPieces];
        public int Ply { get; set; }
        public const string StartposSfen = "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b - 1";

        public static void Initialize()
        {
        }

        /// <summary>
        /// 与えられた指し手に従い、局面を更新する。
        /// </summary>
        /// <param name="move"></param>
        public void DoMove(Move move)
        {
            Debug.Assert(move.Drop || Board[move.FileFrom, move.RankFrom] == move.PieceFrom);
            Debug.Assert(move.Drop || Board[move.FileTo, move.RankTo] == move.PieceTo);

            // 相手の駒を取る
            if (move.PieceTo != Piece.NoPiece)
            {
                ++HandPieces[(int)move.PieceTo.ToOpponentsHandPiece()];
            }

            Board[move.FileTo, move.RankTo] = move.Promotion
                ? Types.ToPromoted(move.PieceFrom)
                : move.PieceFrom;

            if (move.Drop)
            {
                // 駒を打つ指し手
                Debug.Assert(HandPieces[(int)move.PieceFrom] > 0);
                --HandPieces[(int)move.PieceFrom];
            }
            else
            {
                // 駒を移動する指し手
                Board[move.FileFrom, move.RankFrom] = Piece.NoPiece;
            }
        }

        /// <summary>
        /// 与えられた指し手に従い、局面を1手戻す。
        /// </summary>
        /// <param name="move"></param>
        public void UndoMove(Move move)
        {
            if (move.Drop)
            {
                // 駒を打つ指し手
                ++HandPieces[(int)move.PieceFrom];
            }
            else
            {
                // 駒を移動する指し手
                Board[move.FileFrom, move.RankFrom] = move.PieceFrom;
            }

            Board[move.FileTo, move.RankTo] = move.PieceTo;

            // 相手の駒を取る
            if (move.PieceTo != Piece.NoPiece)
            {
                Debug.Assert(HandPieces[(int)move.PieceTo] > 0);
                --HandPieces[(int)move.PieceTo.ToOpponentsHandPiece()];
            }
        }

        /// <summary>
        /// sfen文字列をセットする
        /// </summary>
        /// <param name="sfen"></param>
        public void Set(string sfen)
        {
            int file = BoardSize - 1;
            int rank = 0;
            int index = 0;
            bool promotion = false;
            while (true)
            {
                var ch = sfen[index++];
                if (ch == ' ')
                {
                    break;
                }
                else if (ch == '/')
                {
                    Debug.Assert(file == -1);
                    ++rank;
                    file = BoardSize - 1;
                }
                else if (ch == '+')
                {
                    promotion = true;
                }
                else if (Char.IsDigit(ch))
                {
                    int numNoPieces = ch - '0';
                    do
                    {
                        Board[file--, rank] = Piece.NoPiece;
                    } while (--numNoPieces > 0);
                }
                else
                {
                    var piece = CharToPiece[ch];
                    Debug.Assert(piece != null);
                    if (promotion)
                    {
                        piece = piece.ToPromoted();
                        promotion = false;
                    }
                    Board[file--, rank] = piece;
                }
            }

            // 手番
            var sideToMove = sfen[index++];
            Debug.Assert(sideToMove == 'b' || sideToMove == 'w');
            if (sideToMove == 'b')
            {
                SideToMove = Color.Black;
            }
            else
            {
                SideToMove = Color.White;
            }
            ++index;

            while (true)
            {
                var ch = sfen[index++];
                if (ch == ' ')
                {
                    break;
                }

                var piece = CharToPiece[ch];
                Debug.Assert(piece != null);
                ++HandPieces[(int)piece];
            }

            Ply = int.Parse(sfen.Substring(index));
        }

        public override String ToString()
        {
            var writer = new StringWriter();
            writer.WriteLine("+----+----+----+----+----+----+----+----+----+");
            for (int rank = 0; rank < BoardSize; ++rank)
            {
                writer.Write("|");
                for (int file = BoardSize - 1; file >= 0; --file)
                {
                    writer.Write(PieceToString[(int)Board[file, rank]]);
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
                    writer.Write(PieceToString[(int)piece][0]);
                }
            }

            writer.Write(" , 後手 手駒 : ");
            for (var piece = Piece.WhitePawn; piece < Piece.NumPieces; ++piece)
            {
                for (int i = 0; i < HandPieces[(int)piece]; ++i)
                {
                    writer.Write(PieceToString[(int)piece][0]);
                }
            }
            writer.WriteLine();

            writer.Write("手番 = ");
            writer.Write(SideToMove == Color.Black ? "先手" : "後手");
            writer.WriteLine();

            return writer.ToString();
        }

        private static String[] PieceToString = new[]
        {
            "　　",
            " 歩 ",
            " 香 ",
            " 桂 ",
            " 銀 ",
            " 金 ",
            " 角 ",
            " 飛 ",
            " 王 ",
            " と ",
            " 杏 ",
            " 圭 ",
            " 全 ",
            " 馬 ",
            " 龍 ",
            "歩↓",
            "香↓",
            "桂↓",
            "銀↓",
            "金↓",
            "角↓",
            "飛↓",
            "王↓",
            "と↓",
            "杏↓",
            "圭↓",
            "全↓",
            "馬↓",
            "龍↓",
            null,
        };
    }
}
