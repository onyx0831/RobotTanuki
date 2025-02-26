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
    public static class Types
    {
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

        /// <summary>駒を人間が読める文字列に変換</summary>
        public static string ToHumanReadableString(this Piece piece)
        {
            Debug.Assert(PieceToString[(int)piece] != null);
            return PieceToString[(int)piece];
        }
    }
}
