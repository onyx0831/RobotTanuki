using System.Runtime.CompilerServices;

namespace RobotTanuki.Tests;

public class TranspositionTableTests
{
    [Fact]
    public void 要素は参照を含まない16バイトの構造体()
    {
        // 参照を含むとGCのたびに置換表全体が走査され、置換表を大きくするほど遅くなる。
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<TranspositionTableEntry>());
        Assert.Equal(16, Unsafe.SizeOf<TranspositionTableEntry>());
    }
}
