using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RobotTanuki
{
    /// <summary>
    /// USIプロトコルの入出力を担当する。標準入力の行をパースしてEngineのメソッドを呼び出し、
    /// 結果をUSI形式の文字列に整形して標準出力に書き出す。
    /// </summary>
    public class Usi
    {
        private const string USI_Ponder = "USI_Ponder";
        private const string USI_Hash = "USI_Hash";

        private readonly Engine engine;

        public Usi(Engine engine)
        {
            this.engine = engine;
        }

        public void Run()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                var split = line.Split();
                if (split.Length == 0)
                {
                    continue;
                }

                var command = split[0];
                switch (command)
                {
                    case "usi":
                        Console.WriteLine($"id name {engine.Name}");
                        Console.WriteLine($"id author {engine.Author}");
                        Console.WriteLine($"option name {USI_Ponder} type check default true");
                        Console.WriteLine($"option name {USI_Hash} type spin default 256");
                        Console.WriteLine("usiok");
                        Console.Out.Flush();
                        break;

                    case "isready":
                        Console.WriteLine("readyok");
                        Console.Out.Flush();
                        break;

                    case "usinewgame":
                        engine.NewGame();
                        break;

                    case "setoption":
                        Debug.Assert(split.Length == 5);
                        Debug.Assert(split[1] == "name");
                        Debug.Assert(split[3] == "value");
                        engine.SetOption(split[2], split[4]);
                        break;

                    case "position":
                        Debug.Assert(split.Length >= 2);
                        Debug.Assert(split[1] == "sfen" || split[1] == "startpos");
                        int nextIndex;
                        string? sfen;
                        if (split[1] == "sfen")
                        {
                            sfen = string.Join(" ", split.Skip(2).Take(4));
                            nextIndex = 6;
                        }
                        else if (split[1] == "startpos")
                        {
                            sfen = null;
                            nextIndex = 2;
                        }
                        else
                        {
                            throw new Exception($"不正なpositionコマンドです: {line}");
                        }
                        // 指し手を適用
                        engine.SetPosition(sfen, split.Skip(nextIndex).Where(moveString => moveString != "moves"));
                        break;

                    case "stop":
                    case "ponderhit":
                    case "gameover":
                        break;

                    case "go":
                        var (bestMove, depth, nodes, timeMs) = engine.Go();
                        string bestMoveString = bestMove.Move.ToUsiString();
                        int nps = timeMs > 0 ? (int)(nodes / (timeMs / 1000.0)) : 0;
                        string pv = BuildPvString(bestMove);
                        Console.WriteLine($"info depth {depth} seldepth {depth} time {timeMs} nodes {nodes} score cp {bestMove.Value} nps {nps} pv {pv}");

                        if (bestMove.Value < -30000)
                        {
                            Console.WriteLine("bestmove resign");
                        }
                        else
                        {
                            Console.WriteLine("bestmove " + bestMoveString);
                        }
                        break;

                    case "quit":
                        return;

                    // 以下デバッグ用コマンド
                    case "d":
                        Console.WriteLine(engine.DebugPositionString());
                        break;

                    case "generatemove":
                        foreach (var move in engine.DebugGenerateLegalMoves())
                        {
                            Console.Write(move);
                            Console.Write(" ");
                        }
                        Console.WriteLine();
                        break;

                    case "eval":
                        Console.WriteLine(engine.DebugEvaluate());
                        break;

                    default:
                        Console.WriteLine($"info string Unsupported command: {command}");
                        Console.Out.Flush();
                        break;
                }
            }
        }

        /// <summary>
        /// BestMove.Nextを辿って読み筋（Principal Variation）の文字列を組み立てる。
        /// </summary>
        private static string BuildPvString(BestMove bestMove)
        {
            var moveStrings = new List<string>();
            BestMove? current = bestMove;
            while (current != null && current.Move != Move.Resign && current.Move != Move.None)
            {
                moveStrings.Add(current.Move.ToUsiString());
                current = current.Next;
            }
            return string.Join(" ", moveStrings);
        }
    }
}
