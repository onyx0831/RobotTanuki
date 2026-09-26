using System;

namespace RobotTanuki
{
    /// <summary>
    /// 局面のZobristハッシュ計算に使う乱数テーブル。
    /// </summary>
    public static class Zobrist
    {
        private const int MaxHandCount = 18;

        public static ulong[,,] PieceSquare { get; } = new ulong[(int)Piece.NumPieces, Position.BoardSize, Position.BoardSize];

        // 持ち駒は枚数ごとに別の乱数を持つ（枚数の増減をXORで差分更新するため）。
        public static ulong[,] HandPiece { get; } = new ulong[(int)Piece.NumPieces, MaxHandCount + 1];

        public static ulong BlackToMove { get; private set; }

        public static void Initialize()
        {
            var random = new Random(0);

            for (int piece = 0; piece < (int)Piece.NumPieces; ++piece)
            {
                for (int file = 0; file < Position.BoardSize; ++file)
                {
                    for (int rank = 0; rank < Position.BoardSize; ++rank)
                    {
                        PieceSquare[piece, file, rank] = NextUInt64(random);
                    }
                }

                for (int count = 0; count <= MaxHandCount; ++count)
                {
                    HandPiece[piece, count] = NextUInt64(random);
                }
            }

            BlackToMove = NextUInt64(random);
        }

        private static ulong NextUInt64(Random random)
        {
            var bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes);
        }
    }
}
