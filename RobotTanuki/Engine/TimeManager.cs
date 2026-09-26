using System;

namespace RobotTanuki
{
    /// <summary>
    /// goコマンドの持ち時間から、今回の思考に使う時間（ミリ秒）を計算する。
    /// </summary>
    public static class TimeManager
    {
        // どれだけ持ち時間が短くても、最低限これだけは考える（使える時間がこれより短い場合は使える時間の方を優先する）。
        private const int MinimumThinkingTimeMs = 1000;

        // 使える時間がほぼゼロでも、CancelAfterに渡す値が0以下にならないようにするための絶対的な下限。
        private const int MinimumSafetyFloorMs = 50;

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
            int availableMs;
            if (options.ByoyomiMs > 0)
            {
                // 秒読み: 持ち時間+秒読みの1/8を目安にしつつ、最低でも秒読み分は使う。
                availableMs = time + options.ByoyomiMs;
                thinkingTimeMs = Math.Max(availableMs / SuddenDeathDivisor, options.ByoyomiMs);
            }
            else if (inc > 0)
            {
                // フィッシャークロック
                availableMs = time + inc;
                thinkingTimeMs = availableMs / SuddenDeathDivisor;
            }
            else
            {
                // 切れ負け
                availableMs = time;
                thinkingTimeMs = availableMs / SuddenDeathDivisor;
            }

            thinkingTimeMs -= NetworkDelayMs;

            // MinimumThinkingTimeMsはあくまで目安の下限。実際に使える時間そのものを超えて時間切れ負けに
            // ならないよう、使える時間（通信マージン差し引き後）を上限として必ず切り詰める。
            int upperBoundMs = Math.Max(availableMs - NetworkDelayMs, MinimumSafetyFloorMs);
            int lowerBoundMs = Math.Min(MinimumThinkingTimeMs, upperBoundMs);
            return Math.Clamp(thinkingTimeMs, lowerBoundMs, upperBoundMs);
        }
    }
}
