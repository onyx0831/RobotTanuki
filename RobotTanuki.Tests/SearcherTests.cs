namespace RobotTanuki.Tests;

// 置換表は静的な変数で共有されているため、探索を呼ぶテストはこのクラスにまとめ、並列に実行されないようにする。
public class SearcherTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void 一手詰めの局面では根から1手で詰ませる値と手を返す(int depth)
    {
        // 深さ1では静止探索が、深さ3では通常探索が詰みを見つけるので、両方の経路を確かめる。
        var bestMove = Search("4k4/9/4P4/9/9/9/9/9/4K4 b G 1", depth);
        Assert.Equal(Searcher.Infinity - 1, bestMove.Value);
        Assert.Equal("G*5b", bestMove.Move.ToUsiString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void 詰まされている局面では根で詰まされた値を返す(int depth)
    {
        var bestMove = Search("4k4/4G4/4P4/9/9/9/9/9/4K4 w - 2", depth);
        Assert.Equal(-Searcher.Infinity, bestMove.Value);
    }

    [Fact]
    public void 詰ませる値は置換表に今の局面からの手数で保存され根からの手数に戻る()
    {
        // 根から3手目の局面で「根から7手目で詰ませる」値は、その局面からは4手で詰む。
        int value = Searcher.Infinity - 7;
        int stored = Searcher.ToTranspositionTableValue(value, 3);
        Assert.Equal(Searcher.Infinity - 4, stored);
        Assert.Equal(value, Searcher.FromTranspositionTableValue(stored, 3));
        // 同じ局面に根から5手目で来たなら、根から9手目で詰ませることになる。
        Assert.Equal(Searcher.Infinity - 9, Searcher.FromTranspositionTableValue(stored, 5));
    }

    [Fact]
    public void 詰まされる値も今の局面からの手数で保存され根からの手数に戻る()
    {
        int value = -Searcher.Infinity + 7;
        int stored = Searcher.ToTranspositionTableValue(value, 3);
        Assert.Equal(-Searcher.Infinity + 4, stored);
        Assert.Equal(value, Searcher.FromTranspositionTableValue(stored, 3));
        Assert.Equal(-Searcher.Infinity + 9, Searcher.FromTranspositionTableValue(stored, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1234)]
    [InlineData(-1234)]
    public void 詰みでない値は置換表の保存で変わらない(int value)
    {
        Assert.Equal(value, Searcher.ToTranspositionTableValue(value, 5));
        Assert.Equal(value, Searcher.FromTranspositionTableValue(value, 5));
    }

    [Fact]
    public void 根から数えた詰みの値から詰みまでの手数を求める()
    {
        Assert.Equal(7, Searcher.PliesUntilMate(Searcher.Infinity - 7));
        Assert.Equal(-7, Searcher.PliesUntilMate(-Searcher.Infinity + 7));
    }

    private static BestMove Search(string sfen, int depth)
    {
        var position = new Position();
        position.Set(sfen);
        return Searcher.SearchIterative(position, depth, CancellationToken.None, out _);
    }
}
