using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheStatsTests
{
    [Test]
    public void Stats_WhenRecordingDisabled_ReturnsZeroCountersButCurrentSizeAndWeight()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 3L;

        cache.PutEntry("a", "one", firstOptions);
        cache.PutEntry("b", "two", secondOptions);
        cache.Get("a");
        cache.Get("missing");
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Hits, Is.EqualTo(0L));
        Assert.That(snapshot.Misses, Is.EqualTo(0L));
        Assert.That(snapshot.Puts, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(2L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(5L));
        Assert.That(snapshot.HitRate, Is.EqualTo(0.0));
    }

    [Test]
    public void Stats_WhenRecordingEnabled_RecordsWritesReadsRemovesAndClears()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("a", "two");
        cache.Get("a");
        cache.Peek("a");
        cache.Get("missing");
        cache.Remove("a");
        cache.Put("b", "three");
        cache.Clear();
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Puts, Is.EqualTo(2L));
        Assert.That(snapshot.Updates, Is.EqualTo(1L));
        Assert.That(snapshot.Hits, Is.EqualTo(2L));
        Assert.That(snapshot.Misses, Is.EqualTo(1L));
        Assert.That(snapshot.Removes, Is.EqualTo(1L));
        Assert.That(snapshot.Clears, Is.EqualTo(1L));
        Assert.That(snapshot.Expirations, Is.EqualTo(0L));
        Assert.That(snapshot.Evictions, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(0L));
        Assert.That(snapshot.HitRate, Is.EqualTo(2.0 / 3.0).Within(0.000001));
        Assert.That(snapshot.LoadSuccessRate, Is.EqualTo(0.0));
    }

    [Test]
    public void Stats_RecordsExpirationsFromReadsAndCleanup()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RecordStats = true;
        Cache cache = new Cache(options);

        cache.PutEntry("read-expired", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        cache.Get("read-expired");
        cache.PutEntry("cleanup-expired", "two", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        cache.Cleanup();
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Puts, Is.EqualTo(2L));
        Assert.That(snapshot.Misses, Is.EqualTo(1L));
        Assert.That(snapshot.Expirations, Is.EqualTo(2L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(0L));
    }

    [Test]
    public void Stats_RecordsCapacityEvictions()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Puts, Is.EqualTo(2L));
        Assert.That(snapshot.Evictions, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(1L));
    }

    [Test]
    public void Stats_ReturnsSnapshotNotLiveCounters()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);

        CacheStatsSnapshot before = cache.Stats();
        cache.Put("a", "one");
        cache.Get("a");
        CacheStatsSnapshot after = cache.Stats();

        Assert.That(before.Puts, Is.EqualTo(0L));
        Assert.That(before.Hits, Is.EqualTo(0L));
        Assert.That(before.CurrentSize, Is.EqualTo(0L));
        Assert.That(after.Puts, Is.EqualTo(1L));
        Assert.That(after.Hits, Is.EqualTo(1L));
        Assert.That(after.CurrentSize, Is.EqualTo(1L));
    }

    [Test]
    public void Stats_RecordsHitsMissesWritesRemovalsExpirationsAndEvictionsTogether()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(1000L);
        options.Clock = clock;
        options.RecordStats = true;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("hit", "one");
        cache.Get("hit");
        cache.Get("missing");
        cache.Put("removed", "two");
        cache.Remove("removed");
        cache.PutEntry("expired", "three", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        cache.Get("expired");
        cache.Put("evict-a", "four");
        cache.Put("evict-b", "five");
        cache.Put("evict-c", "six");
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Puts, Is.EqualTo(6L));
        Assert.That(snapshot.Updates, Is.EqualTo(0L));
        Assert.That(snapshot.Hits, Is.EqualTo(1L));
        Assert.That(snapshot.Misses, Is.EqualTo(2L));
        Assert.That(snapshot.Removes, Is.EqualTo(1L));
        Assert.That(snapshot.Expirations, Is.EqualTo(1L));
        Assert.That(snapshot.Evictions, Is.EqualTo(2L));
        Assert.That(snapshot.Clears, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentSize, Is.EqualTo(2L));
        Assert.That(snapshot.HitRate, Is.EqualTo(1.0 / 3.0).Within(0.000001));
    }

    [Test]
    public void Stats_WhenCountersReachLongLimit_SaturatesAndCalculatesRates()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache._stats.Hits = long.MaxValue;
        cache._stats.Misses = long.MaxValue;
        cache._stats.LoadSuccesses = long.MaxValue;
        cache._stats.LoadFailures = long.MaxValue;

        cache.RecordHit();
        cache.RecordLoadSuccess(long.MaxValue);
        CacheStatsSnapshot snapshot = cache.Stats();

        Assert.That(snapshot.Hits, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.LoadSuccesses, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.TotalLoadDurationTicks, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.HitRate, Is.EqualTo(0.5));
        Assert.That(snapshot.LoadSuccessRate, Is.EqualTo(0.5));
    }
}
