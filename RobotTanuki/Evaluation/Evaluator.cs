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

            // 盤上の駒の評価値を合算。持ち駒よりどこにでも打てる分だけ柔軟性が低いので、価値を1割引く。
            foreach (var piece in position.Board)
            {
                value += PieceValues[(int)piece] * 9 / 10;
            }

            // 持ち駒の評価値を合算
            for (int i = 0; i < position.HandPieces.Length; i++)
            {
                value += PieceValues[i] * position.HandPieces[i];
            }

            var blackControl = MoveGenerator.ComputeControlCounts(position, Color.Black);
            var whiteControl = MoveGenerator.ComputeControlCounts(position, Color.White);

            value += EvaluateKingSafety(position, blackControl, whiteControl);
            value += EvaluatePieceSafety(position, blackControl, whiteControl);
            value += EvaluateKingPosition(position);

            // 後手の場合は評価値を反転
            if (position.SideToMove == Color.White)
            {
                value = -value;
            }

            return value;
        }

        /// <summary>
        /// 駒の価値を手番に依存しない絶対値で返す。指し手オーダリング（MVV-LVA）で使用する。
        /// </summary>
        public static int GetPieceValue(Piece piece)
        {
            return Math.Abs(PieceValues[(int)piece]);
        }

        // 自玉を守る利きの価値（距離1マスあたりの基準値）
        private const int DefenseBaseValue = 60;

        // 敵玉を攻める利きの価値。守りより攻めを高く評価する。
        private const int ThreatBaseValue = 90;

        /// <summary>
        /// 玉の周囲の利きを評価する。先手から見た値を返す。
        /// 玉に近いマスほど価値が高く、距離に反比例して減衰する。
        /// </summary>
        private static int EvaluateKingSafety(Position position, int[,] blackControl, int[,] whiteControl)
        {
            return EvaluateKingSafetyFor(position, Color.Black, blackControl, whiteControl)
                - EvaluateKingSafetyFor(position, Color.White, whiteControl, blackControl);
        }

        /// <summary>
        /// colorの玉について、その周囲の利きの価値をcolorから見た値で返す。
        /// </summary>
        private static int EvaluateKingSafetyFor(Position position, Color color, int[,] ownControl, int[,] enemyControl)
        {
            if (!position.TryFindKingSquare(color, out var kingSquare))
            {
                // 玉が盤面にない局面（デバッグ用のSFENなど）では、その玉の安全度評価は0点として扱う。
                return 0;
            }
            var (kingFile, kingRank) = kingSquare;
            int value = 0;

            for (int file = 0; file < Position.BoardSize; ++file)
            {
                for (int rank = 0; rank < Position.BoardSize; ++rank)
                {
                    int distance = Math.Max(Math.Abs(file - kingFile), Math.Abs(rank - kingRank));
                    value += ownControl[file, rank] * DefenseBaseValue / (distance + 1);
                    value -= enemyControl[file, rank] * ThreatBaseValue / (distance + 1);
                }
            }

            return value;
        }

        // 味方に守られている駒へのボーナス（駒価値に対する割合、%）
        private const int DefendedBonusPercent = 3;

        // 敵に狙われている駒へのペナルティ（駒価値に対する割合、%）。守りより狙われている方を重く見る。
        private const int AttackedPenaltyPercent = 8;

        /// <summary>
        /// 盤上の各駒について、味方の利きで守られているか・敵の利きに狙われているかを判定し、
        /// 駒価値に対する小さい割合で加減点する。先手から見た値を返す。
        /// </summary>
        private static int EvaluatePieceSafety(Position position, int[,] blackControl, int[,] whiteControl)
        {
            var board = position.Board;
            int value = 0;

            for (int file = 0; file < Position.BoardSize; ++file)
            {
                for (int rank = 0; rank < Position.BoardSize; ++rank)
                {
                    var piece = board[file, rank];
                    if (piece == Piece.NoPiece)
                    {
                        continue;
                    }

                    int pieceValue = GetPieceValue(piece);
                    bool isBlack = piece.ToColor() == Color.Black;
                    var ownControl = isBlack ? blackControl : whiteControl;
                    var enemyControl = isBlack ? whiteControl : blackControl;
                    int sign = isBlack ? 1 : -1;

                    if (ownControl[file, rank] > 0)
                    {
                        value += sign * pieceValue * DefendedBonusPercent / 100;
                    }
                    if (enemyControl[file, rank] > 0)
                    {
                        value -= sign * pieceValue * AttackedPenaltyPercent / 100;
                    }
                }
            }

            return value;
        }

        // 段によるボーナス（rank0=盤の最上段〜rank8=先手の最下段）。先手は自陣（rank8側）にいるほど高い。
        // 段だけで評価すると「単に後方に下がるだけの不自然な手」を誘発するため、筋のボーナスと組み合わせて使う。
        private static readonly int[] KingRankBonus = { 0, 0, 0, 0, 0, 0, 10, 20, 25 };

        // 筋によるボーナス。中央より端に寄っているほど高い（囲いが端に寄る傾向を軽く後押しする）。
        private static readonly int[] KingFileBonus = { 15, 10, 5, 0, 0, 0, 5, 10, 15 };

        /// <summary>
        /// 玉の位置による小さいボーナスを先手から見た値で返す。
        /// </summary>
        private static int EvaluateKingPosition(Position position)
        {
            int value = 0;

            if (position.TryFindKingSquare(Color.Black, out var blackKing))
            {
                value += KingFileBonus[blackKing.File] + KingRankBonus[blackKing.Rank];
            }
            if (position.TryFindKingSquare(Color.White, out var whiteKing))
            {
                value -= KingFileBonus[whiteKing.File] + KingRankBonus[Position.BoardSize - 1 - whiteKing.Rank];
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