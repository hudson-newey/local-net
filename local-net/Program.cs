using static Cli;

namespace LocalNetNamespace
{
    public class LocalNet
    {
        static void Main(string[] args)
        {
            bool debugMode = args.Contains("--debug");

            Console.WriteLine($"Running in debug mode: {debugMode}");

            Server interceptorServer = new Server(debugMode);

            Cli.PrintHelpMessage(interceptorServer);

            interceptorServer.Start();
        }
    }
}
