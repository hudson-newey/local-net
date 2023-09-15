using LocalNetNamespace;
using static Util;

static class Statistics
{
    public static int cachedRequestsCount = 0;
    public static int totalRequestsCount = 0;

    public static float cachedRequestsPercentage()
    {
        return (float) (100 * cachedRequestsCount / totalRequestsCount);
    }

    public static int cachedUrlsCount(Server serverInstance)
    {
        return DirectoryFilesCount(serverInstance.localCachePath);
    }
}