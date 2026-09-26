using System;
using System.Collections.Generic;
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

        public void SetOption(string name, string value)
        {
            options[name] = value;
        }

        public void NewGame()
        {
        }

        /// <summary>
        /// 局面をセットする。sfenがnullの場合は平手初期局面から開始する。
        /// </summary>
        public void SetPosition(string? sfen, IEnumerable<string> moveStrings)
        {
            position.Set(sfen ?? Position.StartposSfen);

            foreach (var moveString in moveStrings)
            {
                var move = Move.FromUsiString(position, moveString);
                position.DoMove(move);
            }
        }

        /// <summary>
        /// 反復深化で探索をバックグラウンド実行する。深さが1つ完了するたびにonDepthCompletedが呼ばれ、
        /// 探索が終了（時間切れ/stop/最大深さ到達）するとonSearchCompletedが1回呼ばれる。
        /// </summary>
        public void Go(GoOptions options, Action<SearchProgress> onDepthCompleted, Action<SearchProgress> onSearchCompleted)
        {
            Stop();
            WaitForSearchToStop();

            currentGoOptions = options;
            var cts = new CancellationTokenSource();
            searchCancellation = cts;

            var thinkingTimeMs = TimeManager.CalculateThinkingTimeMs(options, position.SideToMove);
            if (thinkingTimeMs.HasValue)
            {
                cts.CancelAfter(thinkingTimeMs.Value);
            }

            var beginTime = DateTime.Now;
            searchTask = Task.Run(() =>
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

                onSearchCompleted(new SearchProgress
                {
                    Result = bestMove,
                    Depth = bestMove.Depth,
                    Nodes = nodes,
                    TimeMs = (int)(DateTime.Now - beginTime).TotalMilliseconds,
                });
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

            var thinkingTimeMs = TimeManager.CalculateThinkingTimeMs(realOptions, position.SideToMove);
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
