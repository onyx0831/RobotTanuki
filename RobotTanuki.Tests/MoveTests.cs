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
        // 後手番で持ち駒を全種類持つ局面。後手の打つ手は、USIの文字列から戻すときに駒の対応表を使う
        "4k4/9/9/9/9/9/9/9/4K4 w rbgsnlp 1",
    };

    private static readonly Move[] SpecialMoves = { Move.Resign, Move.Win, Move.None };

    [Theory]
    [MemberData(nameof(Positions))]
    public void 全ての合法手が16ビットに詰めて戻すと元の指し手になる(string sfen)
    {
        var position = CreatePosition(sfen);
        foreach (var move in LegalMoves(position))
        {
            AssertSameMove(move, Move.FromUshort(position, move.ToUshort()));
        }
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void 全ての合法手がUSIの文字列にして戻すと元の指し手になる(string sfen)
    {
        var position = CreatePosition(sfen);
        foreach (var move in LegalMoves(position))
        {
            AssertSameMove(move, Move.FromUsiString(position, move.ToUsiString()));
        }
    }

    [Fact]
    public void 特別な手は戻すと同じ特別な手になる()
    {
        // 局面の駒で埋めて戻すと、同じマスに駒がある局面では特別な手と値が変わってしまう。
        var position = CreatePosition(Position.StartposSfen);
        foreach (var special in SpecialMoves)
        {
            var fromUshort = Move.FromUshort(position, special.ToUshort());
            AssertSameMove(special, fromUshort);
            AssertSameMove(special, Move.FromUsiString(position, special.ToUsiString()));
            // 本体は == や != で特別な手と比べているので、演算子でも確かめる。
            Assert.True(fromUshort == special);
            Assert.False(fromUshort != special);
        }
    }

    [Fact]
    public void 特別な手は実在しない指し手で互いに重ならない()
    {
        // 同じマスからそのマスへ動く手は実在しないので、詰めた値がどの局面の指し手とも重ならない。
        foreach (var special in SpecialMoves)
        {
            Assert.False(special.Drop);
            Assert.Equal(special.FileFrom, special.FileTo);
            Assert.Equal(special.RankFrom, special.RankTo);
            Assert.True(special.IsSpecial);
        }
        Assert.Equal(SpecialMoves.Length, SpecialMoves.Select(special => special.ToUshort()).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void 合法手は特別な手と判定されない(string sfen)
    {
        var position = CreatePosition(sfen);
        foreach (var move in LegalMoves(position))
        {
            Assert.False(move.IsSpecial);
        }
    }

    private static Position CreatePosition(string sfen)
    {
        var position = new Position();
        position.Set(sfen);
        return position;
    }

    // 生成の途中で同じ局面を読むと、指し手生成が局面を一時的に動かす実装の細部に左右されるため、先に確定させる。
    private static List<Move> LegalMoves(Position position)
    {
        return MoveGenerator.GenerateLegal(position).ToList();
    }

    private static void AssertSameMove(Move expected, Move actual)
    {
        // ToStringは駒のない手で例外になり違いが読めないため、先にUSIの文字列で比べる。
        Assert.Equal(expected.ToUsiString(), actual.ToUsiString());
        Assert.Equal(expected, actual);
    }
}
