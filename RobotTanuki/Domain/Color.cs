namespace RobotTanuki
{
    public enum Color
    {
        /// <summary>先手</summary>
        Black,

        /// <summary>後手</summary>
        White,

        /// <summary>手番の種類数</summary>
        NumColors,
    }

    public static class ColorExtensions
    {
        /// <summary>
        /// 相手側の手番へと変換する。
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ToOpponent(this Color color)
        {
            return color == Color.Black ? Color.White : Color.Black;
        }

        /// <summary>
        /// 人間にとって読みやすい文字列へ変換する。
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ToHumanReadableString(this Color color)
        {
            // return color == Color.Black ? "☗" : "☖";
            return color == Color.Black ? "▲" : "△";
        }
    }
}
