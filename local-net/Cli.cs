using LocalNetNamespace;

static class Cli
{
    public static void PrintHelpMessage()
    {
        Console.WriteLine($"Interceptor server started at http://localhost:8080");
        Console.WriteLine($"To use, please set your default search engine to http://localhost:8008/?q=%s");
    }
}