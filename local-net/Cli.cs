using LocalNetNamespace;

static class Cli
{
    public static void PrintHelpMessage(Server serverInstance)
    {
        Console.WriteLine($"Interceptor server started at {serverInstance.serverUrl}:{serverInstance.serverPort}");
        Console.WriteLine($"To use, please set your default search engine to {serverInstance.interceptorPath()}%s");
    }
}
