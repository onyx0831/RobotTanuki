using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobotTanuki
{
    /// <summary>
    /// 盤面と対局オプションというエンジンの状態を保持し、USIプロトコルの文字列を一切知らない
    /// C#として素直なAPIを公開する層。
    /// </summary>
    public class Engine
    {
        public string Name => "Robot Tanuki";
        public string Author => "onyx31";

        // 反復深化がここまで到達することは通常なく、stop/時間切れが来る前の安全網として置いている。
        private const int MaxSearchDepth = 64;

        private readonly Position position = new Position();
        private readonly Dictionary<string, string> options = new Dictionary<string, string>();
        private CancellationTokenSource? searchCancellation;
        private Task? searchTask;
        private GoOptions? currentGoOptions;
        private Color searchSideToMove;
        private int hashMegabytes = Searcher.DefaultHashMegabytes;

        public void SetOption(string name, string value)
        {
            options[name] = value;
        }

        /// <summary>
        /// 置換表の大きさ（メガバイト）を設定する。実際に作り直すのはPrepareのとき。
        /// </summary>
        public void SetHashSize(int megabytes)
        {
            hashMegabytes = Math.Clamp(megabytes, Searcher.MinHashMegabytes, Searcher.MaxHashMegabytes);
        }

        /// <summary>
        /// 探索の前の重い準備（置換表の確保）をする。大きさが変わっていなければ何もしない。
        /// </summary>
        public void Prepare()
        {
            if (hashMegabytes == Searcher.HashMegabytes)
            {
                return;
            }

            // バックグラウンドの探索が置換表を使っている間に作り直さないよう、先に探索の終了を待つ。
            Stop();
            WaitForSearchToStop();
            Searcher.ResizeTable(hashMegabytes);
        }

        public void NewGame()
        {
            Stop();
            WaitForSearchToStop();
        }

        /// <summary>
        /// 局面をセットする。sfenがnullの場合は平手初期局面から開始する。
        /// バックグラウンドの探索がPositionを触っている間に書き換えないよう、先に探索の終了を待つ。
        /// </summary>
        public void SetPosition(string? sfen, IEnumerable<string> moveStrings)
        {
            Stop();
            WaitForSearchToStop();

            position.Set(sfen ?? Position.StartposSfen);
            // 連続王手の千日手の判定で、対局中の各局面が王手だったかを使うため、ここで記録しておく。
            position.IsInCheck();

            foreach (var moveString in moveStrings)
            {
                var move = Move.FromUsiString(position, moveString);
                position.DoMove(move);
                position.IsInCheck();
            }
        }

        /// <summary>
        /// 反復深化で探索をバックグラウンド実行する。深さが1つ完了するたびにonDepthCompletedが呼ばれ、
        /// 探索が終了（時間切れ/stop/最大深さ到達）するとonSearchCompletedが1回呼ばれる。
        /// </summary>
        public void Go(GoOptions options, Action<SearchProgress> onDepthCompleted, Action<SearchProgress> onSearchCompleted, Action<string>? onError = null)
        {
            Stop();
            WaitForSearchToStop();
            searchCancellation?.Dispose();

            currentGoOptions = options;
            searchSideToMove = position.SideToMove;

            if (!options.Infinite && !options.Ponder)
            {
                var firstTwoLegalMoves = MoveGenerator.GenerateLegal(position).Take(2).ToList();
                if (firstTwoLegalMoves.Count == 1)
                {
                    // 合法手が1つしかない場合は、探索しても選択肢が変わらないので即座に指す。
                    searchCancellation = null;
                    searchTask = Task.CompletedTask;
                    onSearchCompleted(new SearchProgress
                    {
                        Result = new BestMove { Move = firstTwoLegalMoves[0], Value = 0 },
                        Depth = 0,
                        Nodes = 0,
                        TimeMs = 0,
                    });
                    return;
                }
            }

            var cts = new CancellationTokenSource();
            searchCancellation = cts;

            var thinkingTimeMs = TimeManager.CalculateThinkingTimeMs(options, searchSideToMove);
            if (thinkingTimeMs.HasValue)
            {
                cts.CancelAfter(thinkingTimeMs.Value);
            }

            var beginTime = DateTime.Now;
            searchTask = Task.Run(() =>
            {
                try
                {
                    var bestMove = Searcher.SearchIterative(position, MaxSearchDepth, cts.Token, out int nodes, (result, depth, depthNodes) =>
                    {
                        onDepthCompleted(new SearchProgress
                        {
                            Result = result,
                            Depth = depth,
                            Nodes = depthNodes,
                            TimeMs = (int)(DateTime.Now - beginTime).TotalMilliseconds,
                        });
                    });

                    // USIの規約上、infinite/ponder中はstop（またはponderhit後の時間切れ）が来るまでbestmoveを送ってはいけない。
                    // 詰み等で反復深化が最大深さまで瞬時に終わっても、ここで実際のキャンセルを待つ。
                    if ((options.Infinite || options.Ponder) && !cts.Token.IsCancellationRequested)
                    {
                        cts.Token.WaitHandle.WaitOne();
                    }

                    onSearchCompleted(new SearchProgress
                    {
                        Result = bestMove,
                        Depth = bestMove.Depth,
                        Nodes = nodes,
                        TimeMs = (int)(DateTime.Now - beginTime).TotalMilliseconds,
                    });
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex.Message);
                    onSearchCompleted(new SearchProgress
                    {
                        Result = new BestMove { Move = Move.Resign, Value = -1_000_000_000 },
                        Depth = 0,
                        Nodes = 0,
                        TimeMs = (int)(DateTime.Now - beginTime).TotalMilliseconds,
                    });
                }
            });
        }

        /// <summary>
        /// 進行中の探索があれば打ち切りを要求する。探索がなければ何もしない。
        /// </summary>
        public void Stop()
        {
            searchCancellation?.Cancel();
        }

        /// <summary>
        /// 進行中の探索が完全に終わるまで待つ。quit時などに使う。
        /// </summary>
        public void WaitForSearchToStop()
        {
            searchTask?.Wait();
        }

        /// <summary>
        /// ponder中に、読みが当たって実際に相手がその手を指した時に呼ぶ。
        /// それまで無制限に考えていた探索に、今から数える本来の持ち時間の期限を設定する。
        /// ponder中でなければ何もしない。
        /// </summary>
        public void PonderHit()
        {
            if (searchCancellation == null || currentGoOptions == null || !currentGoOptions.Ponder)
            {
                return;
            }

            var realOptions = new GoOptions
            {
                Ponder = false,
                BlackTimeMs = currentGoOptions.BlackTimeMs,
                WhiteTimeMs = currentGoOptions.WhiteTimeMs,
                ByoyomiMs = currentGoOptions.ByoyomiMs,
                BlackIncMs = currentGoOptions.BlackIncMs,
                WhiteIncMs = currentGoOptions.WhiteIncMs,
                Infinite = currentGoOptions.Infinite,
            };
            currentGoOptions = realOptions;

            // position.SideToMoveはバックグラウンドの探索がDoMove/UndoMoveで随時書き換えているため使わず、
            // Go()開始時点の手番をsearchSideToMoveに保存しておいたものを使う。
            var thinkingTimeMs = TimeManager.CalculateThinkingTimeMs(realOptions, searchSideToMove);
            if (thinkingTimeMs.HasValue)
            {
                searchCancellation.CancelAfter(thinkingTimeMs.Value);
            }
        }

        public string DebugPositionString()
        {
            return position.ToString();
        }

        public IEnumerable<Move> DebugGenerateLegalMoves()
        {
            return MoveGenerator.GenerateLegal(position);
        }

        public int DebugEvaluate()
        {
            return Evaluator.Evaluate(position);
        }

        public ulong DebugHash()
        {
            return position.Hash;
        }
    }
}
