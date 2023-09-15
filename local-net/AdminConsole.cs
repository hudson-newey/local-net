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
        adminConsoleHtml = adminConsoleHtml.Replace("{{ cachedRequestsPercentage }}", Statistics.cachedRequestsPercentage().ToString());
        adminConsoleHtml = adminConsoleHtml.Replace("{{ cachedUrlsCount }}", Statistics.cachedUrlsCount(serverInstance).ToString());

        return adminConsoleHtml;
    }
}
