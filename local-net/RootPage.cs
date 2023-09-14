using static Util;

static class RootPage
{
    public static string rootPageSubpath = "/";

    public static string render()
    {
        string rootPageHtml = ReadFile(@$"local-net/static/index.html");
        return rootPageHtml;
    }
}
