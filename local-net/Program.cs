using static Cli;

namespace LocalNetNamespace
{
    public class LocalNet
    {
        static void Main(string[] args)
        {
            Server interceptorServer = new Server();

            Cli.PrintHelpMessage();

            interceptorServer.Start();
        }
    }
}
