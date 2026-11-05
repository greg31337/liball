using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheSlidingExpirationTests
{
    [Test]
    public void Get_WithDefaultSlidingExpiration_RefreshesManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 10L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(9L);
        CacheGetResult first = cache.Get("a");
        clock.AdvanceTicks(9L);
        CacheGetResult second = cache.Get("a");
        clock.AdvanceTicks(10L);
        CacheGetResult third = cache.Get("a");

        Assert.That(first.Found, Is.True);
        Assert.That(first.LastAccessUtcTicks, Is.EqualTo(109L));
        Assert.That(first.ExpirationUtcTicks, Is.EqualTo(119L));
        Assert.That(second.Found, Is.True);
        Assert.That(second.LastAccessUtcTicks, Is.EqualTo(118L));
        Assert.That(second.ExpirationUtcTicks, Is.EqualTo(128L));
        Assert.That(third.Found, Is.False);
        Assert.That(third.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Peek_WithPerEntrySlidingExpiration_DoesNotRefreshManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.SlidingExpiration(10L));
        clock.AdvanceTicks(9L);
        CacheGetResult beforeBoundary = cache.Peek("a");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Peek("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.LastAccessUtcTicks, Is.EqualTo(200L));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(210L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void PutEntry_ReplacementRestartsSlidingExpirationFromReplacementWriteTime()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(5L);
        cache.PutEntry("a", "two", entryOptions);
        clock.AdvanceTicks(9L);
        CacheGetResult beforeBoundary = cache.Peek("a");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Peek("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.Value, Is.EqualTo("two"));
        Assert.That(beforeBoundary.CreatedUtcTicks, Is.EqualTo(305L));
        Assert.That(beforeBoundary.LastAccessUtcTicks, Is.EqualTo(305L));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(315L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Cleanup_WithSlidingExpiration_UsesLastAccessFromManualClock()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        cache.PutEntry("a", "one", CacheEntryOptions.SlidingExpiration(10L));

        clock.AdvanceTicks(5L);
        cache.Get("a");
        clock.AdvanceTicks(9L);
        CacheMaintenanceResult beforeBoundary = cache.Cleanup();
        clock.AdvanceTicks(1L);
        CacheMaintenanceResult atBoundary = cache.Cleanup();

        Assert.That(beforeBoundary.ExpiredCount, Is.EqualTo(0L));
        Assert.That(beforeBoundary.CurrentCount, Is.EqualTo(1L));
        Assert.That(atBoundary.ExpiredCount, Is.EqualTo(1L));
        Assert.That(atBoundary.CurrentCount, Is.EqualTo(0L));
    }
}
