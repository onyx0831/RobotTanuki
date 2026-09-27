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
            void PrintSearchInfo(SearchProgress progress)
            {
                int nps = progress.TimeMs > 0 ? (int)(progress.Nodes / (progress.TimeMs / 1000.0)) : 0;
                string pv = BuildPvString(progress.Result);
                string score = Searcher.IsMateScore(progress.Result.Value)
                    ? $"mate {Searcher.PliesUntilMate(progress.Result.Value)}"
                    : $"cp {progress.Result.Value}";
                Console.WriteLine($"info depth {progress.Depth} seldepth {progress.Depth} time {progress.TimeMs} nodes {progress.Nodes} score {score} nps {nps} pv {pv}");
                Console.Out.Flush();
            }

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
                        Console.WriteLine($"option name {USI_Hash} type spin default {Searcher.DefaultHashMegabytes} min {Searcher.MinHashMegabytes} max {Searcher.MaxHashMegabytes}");
                        Console.WriteLine("usiok");
                        Console.Out.Flush();
                        break;

                    case "isready":
                        engine.Prepare();
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
                        if (split[2] == USI_Hash)
                        {
                            // 読めない値で落ちないよう、無視して前の値のままにする。
                            if (int.TryParse(split[4], out int megabytes))
                            {
                                engine.SetHashSize(megabytes);
                            }
                        }
                        else
                        {
                            engine.SetOption(split[2], split[4]);
                        }
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
                        engine.Stop();
                        break;

                    case "ponderhit":
                        engine.PonderHit();
                        break;

                    case "gameover":
                        break;

                    case "go":
                        var goOptions = new GoOptions();
                        for (int index = 1; index < split.Length; ++index)
                        {
                            switch (split[index])
                            {
                                case "ponder":
                                    goOptions.Ponder = true;
                                    break;
                                case "btime":
                                    goOptions.BlackTimeMs = int.Parse(split[++index]);
                                    break;
                                case "wtime":
                                    goOptions.WhiteTimeMs = int.Parse(split[++index]);
                                    break;
                                case "byoyomi":
                                    goOptions.ByoyomiMs = int.Parse(split[++index]);
                                    break;
                                case "binc":
                                    goOptions.BlackIncMs = int.Parse(split[++index]);
                                    break;
                                case "winc":
                                    goOptions.WhiteIncMs = int.Parse(split[++index]);
                                    break;
                                case "infinite":
                                    goOptions.Infinite = true;
                                    break;
                            }
                        }

                        engine.Go(goOptions, PrintSearchInfo, progress =>
                        {
                            if (progress.Result.Value < -30000)
                            {
                                Console.WriteLine("bestmove resign");
                            }
                            else
                            {
                                var ponderMove = progress.Result.Next;
                                if (ponderMove != null && ponderMove.Move != Move.Resign && ponderMove.Move != Move.None)
                                {
                                    Console.WriteLine($"bestmove {progress.Result.Move.ToUsiString()} ponder {ponderMove.Move.ToUsiString()}");
                                }
                                else
                                {
                                    Console.WriteLine("bestmove " + progress.Result.Move.ToUsiString());
                                }
                            }
                            Console.Out.Flush();
                        }, message =>
                        {
                            Console.WriteLine("info string " + message);
                            Console.Out.Flush();
                        });
                        break;

                    case "quit":
                        engine.Stop();
                        engine.WaitForSearchToStop();
                        return;

                    // 以下デバッグ用コマンド
                    case "d":
                        Console.WriteLine(engine.DebugPositionString());
                        break;

                    case "hash":
                        Console.WriteLine(engine.DebugHash());
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
