using System;

namespace RobotTanuki
{
    /// <summary>
    /// goコマンドの持ち時間から、今回の思考に使う時間（ミリ秒）を計算する。
    /// </summary>
    public static class TimeManager
    {
        // どれだけ持ち時間が短くても、最低限これだけは考える。
        private const int MinimumThinkingTimeMs = 1000;

        // bestmoveの送信・USI経由の通信に食われる分の余裕。
        private const int NetworkDelayMs = 100;

        // 秒読み・フィッシャークロックがない場合、残り時間のこの分の1を使う。
        private const int SuddenDeathDivisor = 8;

        /// <summary>
        /// 無制限に思考する場合（infinite/ponder）はnullを返す。
        /// </summary>
        public static int? CalculateThinkingTimeMs(GoOptions options, Color sideToMove)
        {
            if (options.Infinite || options.Ponder)
            {
                return null;
            }

            int time = sideToMove == Color.Black ? options.BlackTimeMs : options.WhiteTimeMs;
            int inc = sideToMove == Color.Black ? options.BlackIncMs : options.WhiteIncMs;

            int thinkingTimeMs;
            if (options.ByoyomiMs > 0)
            {
                // 秒読み: 持ち時間+秒読みの1/8を目安にしつつ、最低でも秒読み分は使う。
                thinkingTimeMs = Math.Max((time + options.ByoyomiMs) / SuddenDeathDivisor, options.ByoyomiMs);
            }
            else if (inc > 0)
            {
                // フィッシャークロック
                thinkingTimeMs = (time + inc) / SuddenDeathDivisor;
            }
            else
            {
                // 切れ負け
                thinkingTimeMs = time / SuddenDeathDivisor;
            }

            thinkingTimeMs -= NetworkDelayMs;
            return Math.Max(thinkingTimeMs, MinimumThinkingTimeMs);
        }
    }
}
