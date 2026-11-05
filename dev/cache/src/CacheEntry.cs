namespace cache;

internal class CacheEntry
{
    internal object? Key;
    internal object? Value;
    internal long CreatedUtcTicks;
    internal long LastAccessUtcTicks;
    internal long LastWriteUtcTicks;
    internal bool HasConfiguredAbsoluteExpiration;
    internal long ConfiguredAbsoluteExpirationUtcTicks;
    internal bool HasTimeToLive;
    internal long TimeToLiveTicks;
    internal bool HasAbsoluteExpiration;
    internal long AbsoluteExpirationUtcTicks;
    internal bool HasSlidingExpiration;
    internal long SlidingExpirationTicks;
    internal long Weight;
    internal CacheEntryPriority Priority;
    internal bool HasRefreshAfterWrite;
    internal long RefreshAfterWriteTicks;
    internal long Version;
    internal CacheRemovalReason RemovalReason;
    internal long AccessCount;
    internal CacheEntrySegment Segment;

    internal CacheEntry()
    {
        Key = null;
        Value = null;
        CreatedUtcTicks = 0L;
        LastAccessUtcTicks = 0L;
        LastWriteUtcTicks = 0L;
        HasConfiguredAbsoluteExpiration = false;
        ConfiguredAbsoluteExpirationUtcTicks = 0L;
        HasTimeToLive = false;
        TimeToLiveTicks = 0L;
        HasAbsoluteExpiration = false;
        AbsoluteExpirationUtcTicks = 0L;
        HasSlidingExpiration = false;
        SlidingExpirationTicks = 0L;
        Weight = 1L;
        Priority = CacheEntryPriority.Normal;
        HasRefreshAfterWrite = false;
        RefreshAfterWriteTicks = 0L;
        Version = 0L;
        RemovalReason = CacheRemovalReason.Unknown;
        AccessCount = 0L;
        Segment = CacheEntrySegment.Window;
    }
}
