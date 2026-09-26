using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        /// <summary>局面を一意に識別するZobristハッシュ</summary>
        public ulong Hash { get; private set; }

        /// <summary>
        /// 与えられた指し手に従い、局面を更新する。
        /// </summary>
        /// <param name="move"></param>
        public void DoMove(Move move)
        {
            Debug.Assert(SideToMove == move.SideToMove);
            Debug.Assert(move.Drop || Board[move.FileFrom, move.RankFrom] == move.PieceFrom);
            Debug.Assert(move.Drop || Board[move.FileTo, move.RankTo] == move.PieceTo);

            // 相手の駒を取る
            if (move.PieceTo != Piece.NoPiece)
            {
                RemovePiece(move.FileTo, move.RankTo);

                // Debug.Assert(move.PieceTo.ToColor() != SideToMove);
                // Debug.Assert(move.PieceTo.AsOpponentHandPiece().ToColor() == SideToMove);
                PutHandPiece(move.PieceTo.AsOpponentHandPiece());
            }

            if (move.Drop)
            {
                // 駒を打つ指し手
                // Debug.Assert(move.PieceFrom.ToColor() == SideToMove);
                RemoveHandPiece(move.PieceFrom);
            }
            else
            {
                // 駒を移動する指し手
                RemovePiece(move.FileFrom, move.RankFrom);
            }

            PutPiece(move.FileTo, move.RankTo,
                move.Promotion
                ? move.PieceFrom.AsPromoted()
                : move.PieceFrom);
            // Debug.Assert(Board[move.FileTo, move.RankTo].ToColor() == SideToMove);

            SideToMove = SideToMove.ToOpponent();
            Hash ^= Zobrist.BlackToMove;

            ++Play;
        }

        /// <summary>
        /// 与えられた指し手に従い、局面を1手戻す。
        /// </summary>
        /// <param name="move"></param>
        public void UndoMove(Move move)
        {
            Debug.Assert(SideToMove != move.SideToMove);

            --Play;
            SideToMove = SideToMove.ToOpponent();
            Hash ^= Zobrist.BlackToMove;
            RemovePiece(move.FileTo, move.RankTo);

            if (move.Drop)
            {
                // 駒を打つ指し手
                // Debug.Assert(move.PieceFrom.ToColor() == SideToMove);
                PutHandPiece(move.PieceFrom);
            }
            else
            {
                // 駒を移動する指し手
                // Debug.Assert(move.PieceFrom.ToColor() == SideToMove);
                PutPiece(move.FileFrom, move.RankFrom, move.PieceFrom);
            }

            // 相手の駒を取る
            if (move.PieceTo != Piece.NoPiece)
            {
                // Debug.Assert(move.PieceTo.ToColor() != SideToMove);
                RemoveHandPiece(move.PieceTo.AsOpponentHandPiece());
                PutPiece(move.FileTo, move.RankTo, move.PieceTo);
            }
        }

        /// <summary>
        /// 盤面に駒を配置する
        /// </summary>
        /// <param name="file"></param>
        /// <param name="rank"></param>
        /// <param name="piece"></param>
        private void PutPiece(int file, int rank, Piece piece)
        {
            Debug.Assert(Board[file, rank] == Piece.NoPiece);
            Hash ^= Zobrist.PieceSquare[(int)piece, file, rank];
            Board[file, rank] = piece;
        }

        /// <summary>
        /// 盤面から駒を取り除く
        /// </summary>
        /// <param name="file"></param>
        /// <param name="rank"></param>
        private void RemovePiece(int file, int rank)
        {
            Debug.Assert(Board[file, rank] != Piece.NoPiece);
            Hash ^= Zobrist.PieceSquare[(int)Board[file, rank], file, rank];
            Board[file, rank] = Piece.NoPiece;
        }

        /// <summary>
        /// 持ち駒に駒を加える
        /// </summary>
        /// <param name="piece"></param>
        private void PutHandPiece(Piece piece)
        {
            Hash ^= Zobrist.HandPiece[(int)piece, HandPieces[(int)piece]];
            ++HandPieces[(int)piece];
            Hash ^= Zobrist.HandPiece[(int)piece, HandPieces[(int)piece]];
        }

        /// <summary>
        /// 持ちがお魔から駒を取り除く
        /// </summary>
        /// <param name="piece"></param>
        private void RemoveHandPiece(Piece piece)
        {
            Debug.Assert(HandPieces[(int)piece] > 0);
            Hash ^= Zobrist.HandPiece[(int)piece, HandPieces[(int)piece]];
            --HandPieces[(int)piece];
            Hash ^= Zobrist.HandPiece[(int)piece, HandPieces[(int)piece]];
        }

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
                    int emptySquares = ch - '0';
                    while (emptySquares-- > 0)
                        Board[file--, rank] = Piece.NoPiece;
                }
                else
                {
                    var piece = PieceExtensions.CharToPiece[ch];
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

                var piece = PieceExtensions.CharToPiece[ch];
                Debug.Assert(piece != Piece.NoPiece);
                HandPieces[(int)piece] += Math.Max(1, count);
                count = 0;
            }

            // 手数パース
            Play = int.Parse(sfen.Substring(index));

            RecomputeHash();
        }

        /// <summary>
        /// Board/HandPieces/SideToMoveからHashを1から計算し直す。
        /// Set()はPutPiece等を経由せず盤面を直接書き換えるため、都度ここで再計算する。
        /// </summary>
        private void RecomputeHash()
        {
            Hash = 0;
            for (int file = 0; file < BoardSize; ++file)
            {
                for (int rank = 0; rank < BoardSize; ++rank)
                {
                    // PutPiece/RemovePieceは駒があるマスしかXORしないため、ここでもNoPieceのマスは対象外にする。
                    if (Board[file, rank] == Piece.NoPiece)
                    {
                        continue;
                    }
                    Hash ^= Zobrist.PieceSquare[(int)Board[file, rank], file, rank];
                }
            }

            for (int piece = 0; piece < (int)Piece.NumPieces; ++piece)
            {
                Hash ^= Zobrist.HandPiece[piece, HandPieces[piece]];
            }

            if (SideToMove == Color.Black)
            {
                Hash ^= Zobrist.BlackToMove;
            }
        }

        /// <summary>
        /// 指定した色の玉の位置を探す。
        /// </summary>
        public (int File, int Rank) FindKingSquare(Color color)
        {
            if (TryFindKingSquare(color, out var square))
            {
                return square;
            }

            throw new InvalidOperationException($"{color}の玉が盤面に見つかりません");
        }

        /// <summary>
        /// 指定した色の玉の位置を探す。見つからない場合はfalseを返す。
        /// </summary>
        public bool TryFindKingSquare(Color color, out (int File, int Rank) square)
        {
            var king = color == Color.Black ? Piece.BlackKing : Piece.WhiteKing;
            for (int file = 0; file < BoardSize; ++file)
            {
                for (int rank = 0; rank < BoardSize; ++rank)
                {
                    if (Board[file, rank] == king)
                    {
                        square = (file, rank);
                        return true;
                    }
                }
            }

            square = default;
            return false;
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