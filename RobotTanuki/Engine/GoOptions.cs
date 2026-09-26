namespace RobotTanuki
{
    /// <summary>
    /// goコマンドの持ち時間関連オプション。USIプロトコルの文字列を含まない、プレーンな値の集まり。
    /// </summary>
    public class GoOptions
    {
        public bool Ponder { get; set; }
        public int BlackTimeMs { get; set; }
        public int WhiteTimeMs { get; set; }
        public int ByoyomiMs { get; set; }
        public int BlackIncMs { get; set; }
        public int WhiteIncMs { get; set; }
        public bool Infinite { get; set; }
    }
}
