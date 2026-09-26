namespace RobotTanuki
{
    /// <summary>
    /// 探索の途中経過、または最終結果を表す。USIプロトコルの文字列を含まない。
    /// </summary>
    public class SearchProgress
    {
        public BestMove Result { get; set; } = null!;
        public int Depth { get; set; }
        public int Nodes { get; set; }
        public int TimeMs { get; set; }
    }
}
