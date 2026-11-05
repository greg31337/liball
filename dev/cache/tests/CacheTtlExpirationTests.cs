using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheTtlExpirationTests
{
    [Test]
    public void Get_WithDefaultTtl_UsesManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 10L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(9L);
        CacheGetResult beforeBoundary = cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Get("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.Value, Is.EqualTo("one"));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(110L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(atBoundary.Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void Peek_WithPerEntryTtl_UsesManualClockWithoutRefreshingExpiration()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(4L);
        CacheGetResult beforeBoundary = cache.Peek("a");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Peek("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(205L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void PutEntry_ReplacementRestartsTtlFromReplacementWriteTime()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.TimeToLive(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(5L);
        cache.PutEntry("a", "two", entryOptions);
        clock.AdvanceTicks(9L);
        CacheGetResult beforeBoundary = cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Get("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.Value, Is.EqualTo("two"));
        Assert.That(beforeBoundary.CreatedUtcTicks, Is.EqualTo(305L));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(315L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Cleanup_WithTtl_RemovesEntriesAtManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(4L);
        CacheMaintenanceResult beforeBoundary = cache.Cleanup();
        clock.AdvanceTicks(1L);
        CacheMaintenanceResult atBoundary = cache.Cleanup();

        Assert.That(beforeBoundary.ExpiredCount, Is.EqualTo(0L));
        Assert.That(beforeBoundary.CurrentCount, Is.EqualTo(1L));
        Assert.That(atBoundary.ExpiredCount, Is.EqualTo(1L));
        Assert.That(atBoundary.CurrentCount, Is.EqualTo(0L));
    }
}
