using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotTanuki
{
    public class BestMove
    {
        /// <summary>
        /// 探索込みの評価値
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 指し手。構造体なので設定し忘れても既定値（1a→1a）の手として通ってしまうため、必ず設定させる。
        /// </summary>
        public required Move Move { get; set; }

        /// <summary>
        /// 次の手（読み筋の末端ではnull）
        /// </summary>
        public BestMove? Next { get; set; }

        /// <summary>
        /// 探索深さ
        /// </summary>
        public int Depth { get; set; }
    }
}
