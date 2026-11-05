namespace cache;

internal class CacheStats
{
    internal long Hits;
    internal long Misses;
    internal long Puts;
    internal long Updates;
    internal long Removes;
    internal long Expirations;
    internal long Evictions;
    internal long Clears;
    internal long LoadSuccesses;
    internal long LoadFailures;
    internal long TotalLoadDurationTicks;

    internal CacheStats()
    {
        Hits = 0L;
        Misses = 0L;
        Puts = 0L;
        Updates = 0L;
        Removes = 0L;
        Expirations = 0L;
        Evictions = 0L;
        Clears = 0L;
        LoadSuccesses = 0L;
        LoadFailures = 0L;
        TotalLoadDurationTicks = 0L;
    }
}
