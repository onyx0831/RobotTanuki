namespace RobotTanuki.Tests;

public class MoveTests
{
    public static TheoryData<string> Positions => new()
    {
        Position.StartposSfen,
        // 合法手が最も多い局面。打つ手・成る手を多く含む
        "8R/kSS1S1K2/4B4/9/9/9/9/9/3L1L1L1 b RBGSNLP3g3n17p 1",
        // 後手番で、盤上の駒の取り合いと打つ手がある終盤局面
        "l4S2l/4g1gs1/5p1p1/pr2N1pkp/4Gn3/PP3PPPP/2GPP4/1K7/L3r+s2L w BS2N5Pb 1",
    };

    private static readonly Move[] SpecialMoves = { Move.Resign, Move.Win, Move.None };

    [Theory]
    [MemberData(nameof(Positions))]
    public void 全ての合法手が16ビットに詰めて戻すと元の指し手になる(string sfen)
    {
        var position = CreatePosition(sfen);
        foreach (var move in MoveGenerator.GenerateLegal(position))
        {
            Assert.Equal(move, Move.FromUshort(position, move.ToUshort()));
        }
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void 全ての合法手がUSIの文字列にして戻すと元の指し手になる(string sfen)
    {
        var position = CreatePosition(sfen);
        foreach (var move in MoveGenerator.GenerateLegal(position))
        {
            Assert.Equal(move, Move.FromUsiString(position, move.ToUsiString()));
        }
    }

    [Fact]
    public void 特別な手は戻すと同じインスタンスになる()
    {
        // 特別な手は参照で比べられているため、値が同じ別のインスタンスでは困る。
        var position = CreatePosition(Position.StartposSfen);
        foreach (var special in SpecialMoves)
        {
            Assert.Same(special, Move.FromUshort(position, special.ToUshort()));
            Assert.Same(special, Move.FromUsiString(position, special.ToUsiString()));
        }
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void 特別な手を詰めた値は合法手と重ならない(string sfen)
    {
        var specialValues = SpecialMoves.Select(special => special.ToUshort()).ToArray();
        var position = CreatePosition(sfen);
        foreach (var move in MoveGenerator.GenerateLegal(position))
        {
            Assert.DoesNotContain(move.ToUshort(), specialValues);
        }
    }

    private static Position CreatePosition(string sfen)
    {
        var position = new Position();
        position.Set(sfen);
        return position;
    }
}
