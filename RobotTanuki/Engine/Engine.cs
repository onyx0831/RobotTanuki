using System;
using System.Collections.Generic;

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

        private readonly Position position = new Position();
        private readonly Dictionary<string, string> options = new Dictionary<string, string>();

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

        public (BestMove result, int depth, int nodes, int timeMs) Go(int depth = 3)
        {
            var beginTime = DateTime.Now;
            int nodes = 0;
            var result = Searcher.Search(position, depth, ref nodes);
            var endTime = DateTime.Now;
            int timeMs = (int)(endTime - beginTime).TotalMilliseconds;
            return (result, depth, nodes, timeMs);
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
