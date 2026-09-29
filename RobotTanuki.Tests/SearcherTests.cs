namespace RobotTanuki.Tests;

public class SearcherTests
{
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
}
