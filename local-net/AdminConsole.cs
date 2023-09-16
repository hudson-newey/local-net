using LocalNetNamespace;

using static Util;
using static Statistics;

static class AdminConsole
{
    public static string adminConsoleSubpath = "/admin";

    public static string render(Server serverInstance)
    {
        string adminConsoleHtml = ReadFile(@$"local-net/static/admin-console.html");

        adminConsoleHtml = adminConsoleHtml.Replace("{{ totalRequestsCount }}", Statistics.totalRequestsCount.ToString());
        adminConsoleHtml = adminConsoleHtml.Replace("{{ cachedRequestsCount }}", Statistics.cachedRequestsCount.ToString());
        adminConsoleHtml = adminConsoleHtml.Replace("{{ CachedRequestsPercentage }}", Statistics.CachedRequestsPercentage().ToString());
        adminConsoleHtml = adminConsoleHtml.Replace("{{ CachedUrlsCount }}", Statistics.CachedUrlsCount(serverInstance).ToString());

        return adminConsoleHtml;
    }
}
