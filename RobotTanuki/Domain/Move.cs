using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RobotTanuki.PieceExtensions;

namespace RobotTanuki
{
    /// <summary>
    /// 指し手を表すデータ構造。指し手を生成するたびに確保してGCの負担にならないよう、構造体にしている。
    /// </summary>
    public readonly record struct Move
    {
        public int FileFrom { get; init; }
        public int RankFrom { get; init; }
        public Piece PieceFrom { get; init; }
        public int FileTo { get; init; }
        public int RankTo { get; init; }
        public Piece PieceTo { get; init; }
        public bool Drop { get; init; }
        public bool Promotion { get; init; }
        public Color SideToMove { get; init; }


        /// 差し手の文字列変換
        public override string ToString()
        {
            return $"{SideToMove.ToHumanReadableString()}{(char)('１' + FileTo)}{RankToKanjiLetters[RankTo]}{PieceFrom.ToHumanReadableString().Trim()[0]}{(Promotion ? "成" : "")}";
        }

        /// <summary>
        /// 投了などの特別な手か。特別な手は「同じマスからそのマスへ動く」ありえない指し手にしている。
        /// </summary>
        public bool IsSpecial => !Drop && FileFrom == FileTo && RankFrom == RankTo;

        public string ToUsiString()
        {
            foreach (var special in SpecialMoves)
            {
                if (this == special.Move)
                {
                    return special.UsiString;
                }
            }

            string usiString = "";
            if (Drop)
            {
                usiString += char.ToUpper(PieceFrom.ToUsiChar());
                usiString += "*";
            }
            else
            {
                usiString += (char)(FileFrom + '1');
                usiString += (char)(RankFrom + 'a');
            }

            usiString += (char)(FileTo + '1');
            usiString += (char)(RankTo + 'a');

            if (Promotion)
            {
                usiString += "+";
            }

            return usiString;
        }

        public static Move FromUsiString(Position position, string moveString)
        {
            foreach (var special in SpecialMoves)
            {
                if (moveString == special.UsiString)
                {
                    return special.Move;
                }
            }

            int fileTo = moveString[2] - '1';
            int rankTo = moveString[3] - 'a';
            if (moveString[1] == '*')
            {
                var piece = CharToPiece[moveString[0]];
                return CreateDrop(position, position.SideToMove == Color.White ? piece.AsOpponentHandPiece() : piece, fileTo, rankTo);
            }

            return CreateBoardMove(position, moveString[0] - '1', moveString[1] - 'a', fileTo, rankTo, moveString.Length == 5);
        }

        /// <summary>
        /// 16ビット整数に詰める。置換表に参照を持たせないため（本家RocketTanukiと同じ形式）。
        /// </summary>
        public ushort ToUshort()
        {
            // 0〜6ビット目: 移動先のマス、7〜13ビット目: 移動元のマス（打つ手なら駒の種類）、
            // 14ビット目: 打つ手なら1、15ビット目: 成る手なら1
            int to = FileTo + RankTo * Position.BoardSize;
            int from = Drop ? (int)PieceFrom : FileFrom + RankFrom * Position.BoardSize;
            int drop = Drop ? 1 : 0;
            int promotion = Promotion ? 1 : 0;
            return (ushort)(to | (from << 7) | (drop << 14) | (promotion << 15));
        }

        /// <summary>
        /// ToUshortで詰めた指し手を戻す。駒と手番は詰めていないので、その指し手を指す局面から復元する。
        /// </summary>
        public static Move FromUshort(Position position, ushort move16)
        {
            foreach (var special in SpecialMoves)
            {
                if (move16 == special.Move.ToUshort())
                {
                    return special.Move;
                }
            }

            int to = move16 & ((1 << 7) - 1);
            int from = (move16 >> 7) & ((1 << 7) - 1);
            bool drop = ((move16 >> 14) & 1) == 1;
            bool promotion = ((move16 >> 15) & 1) == 1;
            int fileTo = to % Position.BoardSize;
            int rankTo = to / Position.BoardSize;
            if (drop)
            {
                return CreateDrop(position, (Piece)from, fileTo, rankTo);
            }

            return CreateBoardMove(position, from % Position.BoardSize, from / Position.BoardSize, fileTo, rankTo, promotion);
        }

        /// <summary>
        /// 盤上の駒を動かす指し手を作る。動かす駒・取る駒・手番は、その指し手を指す局面から埋める。
        /// </summary>
        private static Move CreateBoardMove(Position position, int fileFrom, int rankFrom, int fileTo, int rankTo, bool promotion)
        {
            return new Move
            {
                FileFrom = fileFrom,
                RankFrom = rankFrom,
                PieceFrom = position.Board[fileFrom, rankFrom],
                FileTo = fileTo,
                RankTo = rankTo,
                PieceTo = position.Board[fileTo, rankTo],
                Drop = false,
                Promotion = promotion,
                SideToMove = position.SideToMove,
            };
        }

        /// <summary>
        /// 持ち駒を打つ指し手を作る。取る駒・手番は、その指し手を指す局面から埋める。
        /// </summary>
        private static Move CreateDrop(Position position, Piece piece, int fileTo, int rankTo)
        {
            return new Move
            {
                FileFrom = -1,
                RankFrom = -1,
                PieceFrom = piece,
                FileTo = fileTo,
                RankTo = rankTo,
                PieceTo = position.Board[fileTo, rankTo],
                Drop = true,
                Promotion = false,
                SideToMove = position.SideToMove,
            };
        }

        public static readonly Move Resign = new Move
        {
            FileFrom = 2,
            FileTo = 2,
        };

        public static readonly Move Win = new Move
        {
            FileFrom = 3,
            FileTo = 3,
        };

        public static readonly Move None = new Move
        {
            FileFrom = 4,
            FileTo = 4,
        };

        // 特別な手とUSIでの表記。文字列や整数から戻すときは、局面の駒で埋めずにここにある特別な手を返す。
        // 特別な手は値で比べるので、「同じマスからそのマスへ動く」ありえない指し手にしてマスを手ごとに変え、実在する指し手や互いと重ならないようにする。
        // 静的フィールドは書かれた順に初期化されるので、Resign・Win・Noneより後に宣言する必要がある。
        private static readonly (Move Move, string UsiString)[] SpecialMoves =
        {
            (Resign, "resign"),
            (Win, "win"),
            (None, "none"),
        };

        private static string[] RankToKanjiLetters = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
    }
}