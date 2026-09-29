using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static RobotTanuki.Evaluator;

namespace RobotTanuki
{
    public class Searcher
    {
        // USI_Hashの既定値・範囲（メガバイト）。上限は、要素数がintの範囲に十分収まり、一般的なPCのメモリで使える大きさにしている。
        public const int DefaultHashMegabytes = 32;
        public const int MinHashMegabytes = 1;
        public const int MaxHashMegabytes = 4096;

        private static TranspositionTable table = TranspositionTable.FromMegabytes(DefaultHashMegabytes);

        /// <summary>今の置換表を作ったときに指定された大きさ（メガバイト）。</summary>
        public static int HashMegabytes { get; private set; } = DefaultHashMegabytes;

        // intの範囲で安全に符号反転できる大きさ。詰みの値は、根から詰むまでの手数をこれから引いて表す。
        internal const int Infinity = 1_000_000_000;

        // 根からの手数を引いても、通常の評価値と混同しない余裕を持たせた閾値。
        private const int MateThreshold = Infinity - 1000;

        // 静止探索の延長上限。取り合い・王手が続く限り延長するが、際限なく続かないための安全弁。
        private const int QuiescenceMaxPly = 32;

        // 静止探索で王手を回避する手のうち、駒を取らない手（玉の移動・合駒）を読む上限。
        private const int QuiescenceMaxQuietEvasions = 2;

        // Null Move Pruningを試す最小の残り深さ。浅すぎる場所で使うと縮小後の深さが0未満になり得るため。
        private const int NullMoveMinDepth = 3;

        // パスした後に探索する深さの減らし幅。
        private const int NullMoveReduction = 2;

        // Late Move Reductionsを試す最小の残り深さ。縮小後も静止探索に直行しない深さを残すため。
        private const int LateMoveReductionMinDepth = 3;

        // 最初の数手（先頭の置換表の手など）は縮小しない。
        private const int LateMoveReductionMinMoveCount = 4;

        // Futility Pruningを試す最大の残り深さ。深いほど静かな手でも評価値が大きく動き得るため、浅い局面に限る。
        private const int FutilityMaxDepth = 2;

        // 静かな手1手で評価値がこれ以上は動かないとみなす余裕分（残り深さ1あたり）。
        // 玉が味方に守られるだけで駒の安全度の評価が+450動くため、それを上回る値にする。
        private const int FutilityMarginPerDepth = 500;

        // 千日手を探す範囲。対局の手順での4回目は最初の出現から3周分遡る必要があるが、遡るほど判定が重くなるため、
        // 8手周期の千日手まで判定できる手数に限る。
        private const int MaxRepetitionPly = 24;

        private const int DrawValue = 0;

        /// <summary>置換表をmegabytesに収まる大きさで作り直す。</summary>
        public static void ResizeTable(int megabytes)
        {
            // 古い置換表を参照したまま新しいものを確保すると、その間は両方がメモリに載るため、先に手放して回収しておく。
            table = new TranspositionTable(1);
            GC.Collect();
            table = TranspositionTable.FromMegabytes(megabytes);
            HashMegabytes = megabytes;
        }

        /// <summary>
        /// 詰みの値を、根からの手数から今の局面からの手数に直して置換表に保存する。
        /// 同じ局面に別の手数で来たときにも、正しい手数に戻せるようにするため。
        /// </summary>
        internal static int ToTranspositionTableValue(int value, int ply)
        {
            return value > MateThreshold ? value + ply : value < -MateThreshold ? value - ply : value;
        }

        /// <summary>
        /// 置換表に保存した詰みの値（今の局面からの手数）を、根からの手数に戻す。
        /// </summary>
        internal static int FromTranspositionTableValue(int value, int ply)
        {
            return value > MateThreshold ? value - ply : value < -MateThreshold ? value + ply : value;
        }

        public static bool IsMateScore(int value)
        {
            return Math.Abs(value) > MateThreshold;
        }

        /// <summary>正なら詰ますまでの手数、負なら詰まされるまでの手数。</summary>
        public static int PliesUntilMate(int value)
        {
            return value > 0 ? Infinity - value : -(Infinity + value);
        }

        /// <summary>
        /// 深さ1から順に探索し、完了した深さごとにonDepthCompletedを呼ぶ。
        /// cancellationTokenが要求された時点では直前に完了した深さの結果を返す（深さ1は必ず完了させる）。
        /// </summary>
        public static BestMove SearchIterative(Position position, int maxDepth, CancellationToken cancellationToken, out int totalNodes, Action<BestMove, int, int>? onDepthCompleted = null)
        {
            int nodes = 0;
            var bestMove = new BestMove { Move = Move.Resign, Value = -Infinity };

            for (int depth = 1; depth <= maxDepth; ++depth)
            {
                // 深さ1は思考時間がどれだけ短くても必ず完了させ、指す手が必ずある状態を保証する。
                var tokenForThisDepth = depth == 1 ? CancellationToken.None : cancellationToken;

                BestMove result;
                try
                {
                    result = Search(position, depth, 0, -Infinity, Infinity, ref nodes, tokenForThisDepth);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                bestMove = result;
                bestMove.Depth = depth;
                onDepthCompleted?.Invoke(bestMove, depth, nodes);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            totalNodes = nodes;
            return bestMove;
        }

        /// <summary>
        /// ネガマックス形式のアルファベータ法で探索する。
        /// </summary>
        /// <param name="ply">根からの手数</param>
        private static BestMove Search(Position position, int depth, int ply, int alpha, int beta, ref int nodes, CancellationToken cancellationToken, bool allowNullMove = true)
        {
            // 千日手になった局面は読まずに規則どおりの結果にする。根の局面は指す手を決める必要があるので対象外。
            if (ply > 0)
            {
                var repetition = position.GetRepetition(ply, MaxRepetitionPly);
                if (repetition != Repetition.None)
                {
                    int repetitionValue = repetition == Repetition.Win ? Infinity - ply : repetition == Repetition.Lose ? -Infinity + ply : DrawValue;
                    return new BestMove { Move = Move.None, Value = repetitionValue };
                }
            }

            // 縮小で深さが0を飛び越えて負になっても、再帰が止まるようにする。
            if (depth <= 0)
            {
                return QuiescenceSearch(position, alpha, beta, ply, QuiescenceMaxPly, ref nodes, cancellationToken);
            }

            ulong hash = position.Hash;
            int originalAlpha = alpha;
            ushort? ttMove16 = null;
            if (table.TryGet(hash, out var ttEntry))
            {
                ttMove16 = ttEntry.BestMove16;
                // 根で打ち切ると、読み筋もponderの手も出ず、手順に依存する千日手の値で指す手が決まることもあるため、根では打ち切らない。
                if (ply > 0 && ttEntry.Depth >= depth)
                {
                    int ttValue = FromTranspositionTableValue(ttEntry.Value, ply);
                    if (ttEntry.Bound == TranspositionTableBound.Exact
                        || (ttEntry.Bound == TranspositionTableBound.LowerBound && ttValue >= beta)
                        || (ttEntry.Bound == TranspositionTableBound.UpperBound && ttValue <= alpha))
                    {
                        return new BestMove { Move = Move.FromUshort(position, ttMove16.Value), Value = ttValue };
                    }
                }
            }

            bool inCheck = position.IsInCheck();

            // Null Move Pruning: 一手パスしても（＝相手に手番をそのまま渡しても）なおbeta以上なら、
            // 自分が指せば当然beta以上のはずなので、全ての指し手を調べずに打ち切る。
            // 王手中にパスするのは不自然（王手放置になる）なので対象外。詰みが絡む窓では
            // 誤ったmateスコアの打ち切りを避けるため対象外にする。連続パスは同じ局面を深さだけ
            // 減らして読み直すだけの無駄な探索になるため、パスの直後は再度パスできないようにする。
            if (allowNullMove
                && depth >= NullMoveMinDepth
                && !IsMateScore(beta)
                && !inCheck)
            {
                position.DoNullMove();
                int nullMoveValue;
                try
                {
                    nullMoveValue = -Search(position, depth - 1 - NullMoveReduction, ply + 1, -beta, -beta + 1, ref nodes, cancellationToken, allowNullMove: false).Value;
                }
                finally
                {
                    position.UndoNullMove();
                }

                if (nullMoveValue >= beta)
                {
                    return new BestMove { Move = Move.None, Value = beta };
                }
            }

            // Futility Pruning: 評価値に余裕分を足してもalphaに届かないなら、静かな手ではalphaを超えないとみなして読まない。
            bool canFutilityPrune = depth <= FutilityMaxDepth && !inCheck;
            int? futilityValue = null;

            int bestValue = -Infinity;
            Move bestMove = Move.Resign;
            BestMove? bestChildMove = null;
            int moveCount = 0;
            // GenerateLegal()後に並べ替えると遅延評価が効かず、枝刈りで省けるはずの
            // 合法性チェックまで全手分先に実行してしまうため、擬似合法手の段階で並べ替える。
            var moves = OrderByScore(MoveGenerator.Generate(position), ttMove16);
            foreach (var move in moves)
            {
                // 重い合法性チェックより前に判定する。最初の1手は必ず読み、詰みと誤判定しないようにする。
                if (canFutilityPrune
                    && moveCount > 0
                    && move.PieceTo == Piece.NoPiece
                    && !move.Promotion
                    && !IsMateScore(alpha))
                {
                    // 1手目でbeta cutになるノードが多いので、評価値は枝刈りの候補が来てから計算する。
                    futilityValue ??= Evaluator.Evaluate(position) + FutilityMarginPerDepth * depth;
                    if (futilityValue <= alpha && !MoveGenerator.GivesCheck(position, move))
                    {
                        // 読まなかった手の分として、返す上限値を低く見積もりすぎないようにする。
                        bestValue = Math.Max(bestValue, futilityValue.Value);
                        continue;
                    }
                }

                if (!MoveGenerator.IsLegal(position, move))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                ++moveCount;
                BestMove? childBestMove = null;
                position.DoMove(move);
                try
                {
                    // Principal Variation Search: オーダリングが良ければ最初の手が最善のはずなので、
                    // 2手目以降は「alphaを超えないこと」だけをnull windowで安く確かめ、
                    // 超えてしまった場合だけ正しい値を得るために通常の窓で読み直す。
                    if (moveCount > 1)
                    {
                        // Late Move Reductions: 後ろに並んだ静かな手は最善手の可能性が低いので浅く読み、alphaを超えたら読み直す。
                        // 王手をかける手・回避する手は、浅く読むと詰みを見落としやすいため縮小しない。
                        int reduction = depth >= LateMoveReductionMinDepth
                            && moveCount >= LateMoveReductionMinMoveCount
                            && !inCheck
                            && move.PieceTo == Piece.NoPiece
                            && !move.Promotion
                            && !position.IsInCheck()
                            ? 1 : 0;
                        ++nodes;
                        childBestMove = Search(position, depth - 1 - reduction, ply + 1, -alpha - 1, -alpha, ref nodes, cancellationToken);
                        if (reduction > 0 && -childBestMove.Value > alpha)
                        {
                            ++nodes;
                            childBestMove = Search(position, depth - 1, ply + 1, -alpha - 1, -alpha, ref nodes, cancellationToken);
                        }
                    }

                    if (childBestMove == null || (alpha < -childBestMove.Value && -childBestMove.Value < beta))
                    {
                        ++nodes;
                        childBestMove = Search(position, depth - 1, ply + 1, -beta, -alpha, ref nodes, cancellationToken);
                    }
                }
                finally
                {
                    // キャンセルで例外が飛んでもUndoMoveを必ず実行し、局面を壊さない。
                    position.UndoMove(move);
                }

                int value = -childBestMove.Value;
                if (bestValue < value)
                {
                    bestValue = value;
                    bestMove = move;
                    bestChildMove = childBestMove;
                }

                if (alpha < bestValue)
                {
                    alpha = bestValue;
                }

                if (beta <= alpha)
                {
                    break;
                }
            }

            if (bestMove == Move.Resign)
            {
                // 合法手が1つもない＝この局面で詰んでいる。
                bestValue = -Infinity + ply;
            }

            var bound = bestValue <= originalAlpha ? TranspositionTableBound.UpperBound
                : bestValue >= beta ? TranspositionTableBound.LowerBound
                : TranspositionTableBound.Exact;
            table.Store(hash, depth, ToTranspositionTableValue(bestValue, ply), bestMove, bound);

            return new BestMove
            {
                Move = bestMove,
                Value = bestValue,
                Next = bestChildMove,
            };
        }

        /// <summary>
        /// 静止探索。駒を取り合っている最中に探索を打ち切ると、駒を取られる直前で評価してしまう
        /// 「地平線効果」が起きるため、取り合いが落ち着くまで（駒を取る手が尽きるまで）延長して読む。
        /// 王手されている場合はstand pat（今の評価値をそのまま採用する）をせず、回避手を読む。
        /// </summary>
        /// <param name="ply">根からの手数</param>
        /// <param name="remainingPly">あと何手まで延長できるか</param>
        private static BestMove QuiescenceSearch(Position position, int alpha, int beta, int ply, int remainingPly, ref int nodes, CancellationToken cancellationToken)
        {
            bool inCheck = position.IsInCheck();
            // 王手されている間はstand patを使わないので、Evaluateの呼び出し（利きの計算を含み重い）は
            // 実際に値が必要になる場合（王手されていない場合、または下のremainingPly<=0の安全弁）まで遅らせる。
            int standPat = inCheck ? 0 : Evaluator.Evaluate(position);

            if (!inCheck)
            {
                if (standPat >= beta)
                {
                    return new BestMove { Move = Move.None, Value = standPat };
                }
                if (alpha < standPat)
                {
                    alpha = standPat;
                }
            }

            if (remainingPly <= 0)
            {
                // 延長の安全弁。ここまで来たら取り合い・王手の連続が続いていても評価値で打ち切る。
                return new BestMove { Move = Move.None, Value = inCheck ? Evaluator.Evaluate(position) : standPat };
            }

            int bestValue = inCheck ? -Infinity : standPat;
            Move bestMove = Move.Resign;
            BestMove? bestChildMove = null;

            // 通常探索が置換表に残した最善手があれば先に読み、回避手の枠に良い手が入りやすくする。
            ushort? ttMove16 = table.TryGet(position.Hash, out var ttEntry) ? ttEntry.BestMove16 : null;
            var moves = GenerateQuiescenceMoves(position, inCheck, ttMove16);
            int quietEvasionCount = 0;
            foreach (var move in moves)
            {
                // 「王手→逃げ・合駒→取りながら王手」の連鎖で爆発するため、駒を取らない回避手は上限までにする。
                // 詰まされない手が見つかるまで制限しないのは、詰みと誤判定しないため。
                // 入口の王手を除くのは、受けを削ると無理な王手を過大評価するため。
                bool isQuietEvasion = inCheck && move.PieceTo == Piece.NoPiece && remainingPly < QuiescenceMaxPly;
                if (isQuietEvasion && quietEvasionCount >= QuiescenceMaxQuietEvasions && bestValue > -MateThreshold)
                {
                    break;
                }

                if (!MoveGenerator.IsLegal(position, move))
                {
                    continue;
                }

                if (isQuietEvasion)
                {
                    ++quietEvasionCount;
                }

                cancellationToken.ThrowIfCancellationRequested();

                ++nodes;
                BestMove childBestMove;
                position.DoMove(move);
                try
                {
                    childBestMove = QuiescenceSearch(position, -beta, -alpha, ply + 1, remainingPly - 1, ref nodes, cancellationToken);
                }
                finally
                {
                    position.UndoMove(move);
                }

                int value = -childBestMove.Value;
                if (bestValue < value)
                {
                    bestValue = value;
                    bestMove = move;
                    bestChildMove = childBestMove;
                }

                if (alpha < bestValue)
                {
                    alpha = bestValue;
                }

                if (beta <= alpha)
                {
                    break;
                }
            }

            if (inCheck && bestMove == Move.Resign)
            {
                // 王手を回避する合法手が1つもない＝この局面で詰んでいる。
                return new BestMove { Move = Move.Resign, Value = -Infinity + ply };
            }

            return new BestMove
            {
                Move = bestMove == Move.Resign ? Move.None : bestMove,
                Value = bestValue,
                Next = bestChildMove,
            };
        }

        /// <summary>
        /// 静止探索で読む手を、置換表の手を最優先、次にMVV-LVAの順に並べて返す。
        /// </summary>
        private static IEnumerable<Move> GenerateQuiescenceMoves(Position position, bool inCheck, ushort? ttMove16)
        {
            // 王手されていなければ「駒を取る手」「成る手」だけ、王手されていれば回避手を読む。
            // 成る手も対象にすることで、成り捨てや、成った直後に取り返される手を静止探索で検知できるようにする。
            return OrderByScore(MoveGenerator.Generate(position).Where(move => inCheck || move.PieceTo != Piece.NoPiece || move.Promotion), ttMove16);
        }

        /// <summary>
        /// 置換表の手を最優先、次にMVV-LVAの順に並べる。並べ替えのラムダが探索のメソッドの変数を捕まえると、
        /// 並べ替えずに早く抜けるノードでもクロージャが確保されるため、引数で受け取るメソッドに分けている。
        /// </summary>
        private static IEnumerable<Move> OrderByScore(IEnumerable<Move> moves, ushort? ttMove16)
        {
            return moves.OrderByDescending(move => ScoreForOrdering(move, ttMove16));
        }

        /// <summary>指し手オーダリング用のスコア（置換表の手を最優先、次にMVV-LVA）。</summary>
        /// <param name="ttMove16">置換表の手。当たるたびにMoveを確保しないよう詰めたまま比べるので、同じ局面の手どうしでしか比べられない。</param>
        private static int ScoreForOrdering(Move move, ushort? ttMove16)
        {
            if (ttMove16.HasValue && move.ToUshort() == ttMove16.Value)
            {
                return int.MaxValue;
            }

            if (move.PieceTo == Piece.NoPiece)
            {
                return 0;
            }

            // 玉や大駒で安い駒を取ると値がマイナスになり、駒を取らない手より後ろに並んでしまうため玉の価値を足す。
            // 静止探索の回避手のbreakは、駒を取る手が全て先に並ぶことを前提にしている。
            return Evaluator.GetPieceValue(Piece.BlackKing) + Evaluator.GetPieceValue(move.PieceTo) * 10 - Evaluator.GetPieceValue(move.PieceFrom);
        }
    }
}