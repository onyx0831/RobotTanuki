using System.Runtime.CompilerServices;

namespace RobotTanuki.Tests;

internal static class TestInitializer
{
    // テストではProgram.Mainを通らないため、本体と同じ初期化をアセンブリの読み込み時に1回だけ行う。
    [ModuleInitializer]
    internal static void Initialize()
    {
        Program.InitializeTables();
    }
}
