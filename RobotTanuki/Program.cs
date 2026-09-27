namespace RobotTanuki
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Debugger.Launch();

            InitializeTables();
            new Usi(new Engine()).Run();
        }

        /// <summary>
        /// 駒の対応表とZobristハッシュの乱数表を作る。テストも同じ初期化を使えるよう、Mainから分けている。
        /// </summary>
        public static void InitializeTables()
        {
            PieceExtensions.Initialize();
            Zobrist.Initialize();
        }
    }
}
