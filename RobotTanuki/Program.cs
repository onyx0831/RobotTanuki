namespace RobotTanuki
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Debugger.Launch();

            PieceExtensions.Initialize();
            new Usi(new Engine()).Run();
        }
    }
}
