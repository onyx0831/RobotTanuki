namespace RobotTanuki
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Debugger.Launch();

            PieceExtensions.Initialize();
            Zobrist.Initialize();
            new Usi(new Engine()).Run();
        }
    }
}
