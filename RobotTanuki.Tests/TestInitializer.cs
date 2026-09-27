using System.Runtime.CompilerServices;

namespace RobotTanuki.Tests;

internal static class TestInitializer
{
    // 本体ではProgram.Mainで行っている初期化。テストではMainを通らないため、アセンブリの読み込み時に1回だけ行う。
    [ModuleInitializer]
    internal static void Initialize()
    {
        PieceExtensions.Initialize();
        Zobrist.Initialize();
    }
}
