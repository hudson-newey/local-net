using static Util;

static class AdminConsole
{
    public static string adminConsoleSubpath = "/admin";

    public static string render()
    {
        string adminConsoleHtml = ReadFile(@$"local-net/static/admin-console.html");
        return adminConsoleHtml;
    }
}
