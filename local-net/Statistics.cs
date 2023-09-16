using LocalNetNamespace;
using static Util;

static class Statistics
{
    public static int cachedRequestsCount = 1;
    public static int totalRequestsCount = 1;

    public static float CachedRequestsPercentage()
    {
        return (float)(100 * cachedRequestsCount / totalRequestsCount);
    }

    public static int CachedUrlsCount(Server serverInstance)
    {
        return DirectoryFilesCount(serverInstance.localCachePath);
    }
}