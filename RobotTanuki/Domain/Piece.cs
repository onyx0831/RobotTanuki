using System;
using System.Diagnostics;

namespace RobotTanuki
{
    public enum Piece
    {
        NoPiece, // 駒なし
        BlackPawn,
        BlackLance,
        BlackKnight,
        BlackSilver,
        BlackGold,
        BlackBishop,
        BlackRook,
        BlackKing,
        BlackPromotedPawn,
        BlackPromotedLance,
        BlackPromotedKnight,
        BlackPromotedSilver,
        BlackHorse,
        BlackDragon,
        WhitePawn,
        WhiteLance,
        WhiteKnight,
        WhiteSilver,
        WhiteGold,
        WhiteBishop,
        WhiteRook,
        WhiteKing,
        WhitePromotedPawn,
        WhitePromotedLance,
        WhitePromotedKnight,
        WhitePromotedSilver,
        WhiteHorse,
        WhiteDragon,
        NumPieces, // 駒の種類数
    }

    /// <summary>
    /// 駒の種類に関する変換などのユーティリティクラス
    /// </summary>
    public static class PieceExtensions
    {
        public static Piece[] CharToPiece { get; } = new Piece[128];
        private static char[] PieceToChar { get; } = new char[(int)Piece.NumPieces];
        private static Piece[] NonPromotedToPromoted { get; } = new Piece[(int)Piece.NumPieces];

        public static void Initialize()
        {
            // 成り駒の対応付け
            NonPromotedToPromoted[(int)Piece.BlackPawn] = Piece.BlackPromotedPawn;
            NonPromotedToPromoted[(int)Piece.BlackLance] = Piece.BlackPromotedLance;
            NonPromotedToPromoted[(int)Piece.BlackKnight] = Piece.BlackPromotedKnight;
            NonPromotedToPromoted[(int)Piece.BlackSilver] = Piece.BlackPromotedSilver;
            NonPromotedToPromoted[(int)Piece.BlackBishop] = Piece.BlackHorse;
            NonPromotedToPromoted[(int)Piece.BlackRook] = Piece.BlackDragon;
            NonPromotedToPromoted[(int)Piece.WhitePawn] = Piece.WhitePromotedPawn;
            NonPromotedToPromoted[(int)Piece.WhiteLance] = Piece.WhitePromotedLance;
            NonPromotedToPromoted[(int)Piece.WhiteKnight] = Piece.WhitePromotedKnight;
            NonPromotedToPromoted[(int)Piece.WhiteSilver] = Piece.WhitePromotedSilver;
            NonPromotedToPromoted[(int)Piece.WhiteBishop] = Piece.WhiteHorse;
            NonPromotedToPromoted[(int)Piece.WhiteRook] = Piece.WhiteDragon;

            // 文字と駒の対応付け
            CharToPiece['K'] = Piece.BlackKing;
            CharToPiece['k'] = Piece.WhiteKing;
            CharToPiece['R'] = Piece.BlackRook;
            CharToPiece['r'] = Piece.WhiteRook;
            CharToPiece['B'] = Piece.BlackBishop;
            CharToPiece['b'] = Piece.WhiteBishop;
            CharToPiece['G'] = Piece.BlackGold;
            CharToPiece['g'] = Piece.WhiteGold;
            CharToPiece['S'] = Piece.BlackSilver;
            CharToPiece['s'] = Piece.WhiteSilver;
            CharToPiece['N'] = Piece.BlackKnight;
            CharToPiece['n'] = Piece.WhiteKnight;
            CharToPiece['L'] = Piece.BlackLance;
            CharToPiece['l'] = Piece.WhiteLance;
            CharToPiece['P'] = Piece.BlackPawn;
            CharToPiece['p'] = Piece.WhitePawn;

            PieceToChar[(int)Piece.BlackKing] = 'K';
            PieceToChar[(int)Piece.WhiteKing] = 'k';
            PieceToChar[(int)Piece.BlackRook] = 'R';
            PieceToChar[(int)Piece.WhiteRook] = 'r';
            PieceToChar[(int)Piece.BlackBishop] = 'B';
            PieceToChar[(int)Piece.WhiteBishop] = 'b';
            PieceToChar[(int)Piece.BlackGold] = 'G';
            PieceToChar[(int)Piece.WhiteGold] = 'g';
            PieceToChar[(int)Piece.BlackSilver] = 'S';
            PieceToChar[(int)Piece.WhiteSilver] = 's';
            PieceToChar[(int)Piece.BlackKnight] = 'N';
            PieceToChar[(int)Piece.WhiteKnight] = 'n';
            PieceToChar[(int)Piece.BlackLance] = 'L';
            PieceToChar[(int)Piece.WhiteLance] = 'l';
            PieceToChar[(int)Piece.BlackPawn] = 'P';
            PieceToChar[(int)Piece.WhitePawn] = 'p';
        }

        /// <summary>
        /// 与えられた駒の種類を、成り駒の種類に変換する。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static Piece AsPromoted(this Piece piece)
        {
            Debug.Assert(NonPromotedToPromoted[(int)piece] != Piece.NoPiece);
            return NonPromotedToPromoted[(int)piece];
        }

        /// <summary>
        /// 成ることができる駒かどうかを判定する。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static bool CanPromote(this Piece piece)
        {
            return NonPromotedToPromoted[(int)piece] != Piece.NoPiece;
        }

        /// <summary>
        /// 与えられた駒の種類を、先手・後手に変換する。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static Color ToColor(this Piece piece)
        {
            Debug.Assert(Piece.BlackPawn <= piece && piece < Piece.NumPieces);
            return piece < Piece.WhitePawn ? Color.Black : Color.White;
        }

        /// <summary>
        /// 相手の持ち駒に加わったときの駒の種類を返す。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static Piece AsOpponentHandPiece(this Piece piece)
        {
            Debug.Assert(PieceToOpponentHandPieces[(int)piece] != Piece.NoPiece);
            return PieceToOpponentHandPieces[(int)piece];
        }

        /// <summary>
        /// USIで使用される文字へ変換する。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static char ToUsiChar(this Piece piece)
        {
            Debug.Assert(PieceToChar[(int)piece] != '\0');
            return PieceToChar[(int)piece];
        }

        /// <summary>
        /// 人間が読める文字列に変換する。USIとの互換性はない。
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public static string ToHumanReadableString(this Piece piece)
        {
            Debug.Assert(PieceToString[(int)piece] != null);
            return PieceToString[(int)piece];
        }

        private static Piece[] PieceToOpponentHandPieces = {
            Piece.NoPiece,
            Piece.WhitePawn,
            Piece.WhiteLance,
            Piece.WhiteKnight,
            Piece.WhiteSilver,
            Piece.WhiteGold,
            Piece.WhiteBishop,
            Piece.WhiteRook,
            Piece.NoPiece,
            Piece.WhitePawn,
            Piece.WhiteLance,
            Piece.WhiteKnight,
            Piece.WhiteSilver,
            Piece.WhiteBishop,
            Piece.WhiteRook,
            Piece.BlackPawn,
            Piece.BlackLance,
            Piece.BlackKnight,
            Piece.BlackSilver,
            Piece.BlackGold,
            Piece.BlackBishop,
            Piece.BlackRook,
            Piece.NoPiece,
            Piece.BlackPawn,
            Piece.BlackLance,
            Piece.BlackKnight,
            Piece.BlackSilver,
            Piece.BlackBishop,
            Piece.BlackRook,
            Piece.NumPieces,
        };

        private static String[] PieceToString { get; } = {
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
            " 歩↓",
            " 香↓",
            " 桂↓",
            " 銀↓",
            " 金↓",
            " 角↓",
            " 飛↓",
            " 王↓",
            " と↓",
            " 杏↓",
            " 圭↓",
            " 全↓",
            " 馬↓",
            " 龍↓",
            null,
        };
    }
}
