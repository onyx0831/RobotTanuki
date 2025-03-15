using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RobotTanuki
{
    public static class Evaluator
    {
        public static int Evaluate(Position position)
        {
            int value = 0;

            // 盤上の駒の評価値を合算
            foreach (var piece in position.Board)
            {
                value += PieceValues[(int)piece];
            }

            // 持ち駒の評価値を合算
            for (int i = 0; i < position.HandPieces.Length; i++)
            {
                value += PieceValues[i] * position.HandPieces[i];
            }

            // 後手の場合は評価値を反転
            if (position.SideToMove == Color.White)
            {
                value = -value;
            }

            return value;
        }

        private static readonly int[] PieceValues = {
            0,    // NoPiece
            90,   // 歩
            315,  // 香
            405,  // 桂
            495,  // 銀
            540,  // 金
            855,  // 角
            945,  // 飛
            15000, // 王
            540,  // と
            540,  // 成香
            540,  // 成桂
            540,  // 成銀
            945,  // 馬
            1395, // 龍
            -90,  // 後手 歩
            -315, // 後手 香
            -405, // 後手 桂
            -495, // 後手 銀
            -540, // 後手 金
            -855, // 後手 角
            -945, // 後手 飛
            -15000, // 後手 王
            -540, // 後手 と
            -540, // 後手 成香
            -540, // 後手 成桂
            -540, // 後手 成銀
            -945, // 後手 馬
            -1395 // 後手 龍
        };
    }
}