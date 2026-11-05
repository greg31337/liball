using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheCleanupBehaviorTests
{
    [Test]
    public void Cleanup_ExplicitlyRemovesExpiredEntriesBeforeTheyAreRead()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        cache.PutEntry("live", "two", CacheEntryOptions.TimeToLive(20L));
        clock.AdvanceTicks(5L);
        CacheMaintenanceResult cleanup = cache.Cleanup();
        CacheGetResult expired = cache.Get("expired");
        CacheGetResult live = cache.Get("live");

        Assert.That(cleanup.ExpiredCount, Is.EqualTo(1L));
        Assert.That(cleanup.CurrentCount, Is.EqualTo(1L));
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(expired.Value, Is.Null);
        Assert.That(live.Found, Is.True);
        Assert.That(live.Value, Is.EqualTo("two"));
    }

    [Test]
    public void Get_LazilyCleansExpiredEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.RecordStats = true;
        Cache cache = new Cache(options);

        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        CacheGetResult expired = cache.Get("expired");
        CacheGetResult afterLazyCleanup = cache.Get("expired");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(expired.Value, Is.Null);
        Assert.That(afterLazyCleanup.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.Misses, Is.EqualTo(2L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    [Test]
    public void Put_LazilyCleansExpiredEntriesBeforeCapacityEviction()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.RecordStats = true;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        CacheWriteResult write = cache.Put("fresh", "two");
        CacheGetResult expired = cache.Get("expired");
        CacheGetResult fresh = cache.Get("fresh");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(write.Written, Is.True);
        Assert.That(write.EvictedCount, Is.EqualTo(0L));
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(fresh.Found, Is.True);
        Assert.That(fresh.Value, Is.EqualTo("two"));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(0L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }
}
