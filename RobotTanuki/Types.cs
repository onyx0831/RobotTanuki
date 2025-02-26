using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RobotTanuki
{
    public enum Color
    {
        /// <summary>先手</summary>
        Black,

        /// <summary>後手</summary>
        White,

        /// <summary>手番の種類数</summary>
        NumColors,
    }

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
    /// 型変換などのユーティリティクラス
    /// </summary>
    public static class Types
    {
        public static Piece[] CharToPiece { get; } = new Piece[128];
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

        /// <summary>駒を人間が読める文字列に変換</summary>
        public static string ToHumanReadableString(this Piece piece)
        {
            Debug.Assert(PieceToString[(int)piece] != null);
            return PieceToString[(int)piece];
        }
    }
}
