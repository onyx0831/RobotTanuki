using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RobotTanuki
{
    public class Program
    {
        public const string USI_Ponder = "USI_Ponder";
        public const string USI_Hash = "USI_Hash";

        public static void Initialize()
        {
            // Position.Initialize();
            Types.Initialize();
        }

        void Run()
        {

            Initialize();
            var position = new Position();
            
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                var split = line.Split();
                if (split.Length == 0) continue;

                var command = split[0];
                switch (command)
                {
                    case "usi":
                        Console.WriteLine("id name Robot Tanuki");
                        Console.WriteLine("id author onyx31");
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
                    case "setoption":
                    case "position":
                        Debug.Assert(split.Length >= 2);
                        int nextIndex;
                        if (split[1] == "sfen")
                        {
                            var sfen = string.Join(" ", split.Skip(2).Take(4));
                            position.Set(sfen);
                            nextIndex = 6;
                        }
                        else if (split[1] == "startpos")
                        {
                            position.Set(Position.StartposSfen);
                            nextIndex = 2;
                        }
                        else
                        {
                            throw new Exception($"不正なpositionコマンドです: {line}");
                        }
                        // 指し手を適用
                        foreach (var moveString in split.Skip(nextIndex))
                        {
                            if (moveString == "moves") continue;
                            var move = Move.FromUsiString(position, moveString);
                            position.DoMove(move);
                        }
                        break;

                    case "stop":
                    case "ponderhit":
                    case "gameover":
                        break;

                    case "go":
                        Console.WriteLine("bestmove resign");
                        Console.Out.Flush();
                        break;

                    case "quit":
                        return;

                    // 以下デバッグ用コマンド
                    case "d":
                        Console.WriteLine(position);
                        break;

                    case "generatemove":
                        foreach (var move in MoveGenerator.Generate(position, Move.None))
                        {
                            Console.Write(move);
                            Console.Write(" ");
                        }
                        Console.WriteLine();
                        break;

                    default:
                        Console.WriteLine($"info string Unsupported command: {command}");
                        Console.Out.Flush();
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }
    }
}