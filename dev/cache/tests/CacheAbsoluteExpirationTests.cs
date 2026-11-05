using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheAbsoluteExpirationTests
{
    [Test]
    public void Get_WithDefaultAbsoluteExpiration_UsesManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = 110L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.SetUtcTicks(109L);
        CacheGetResult beforeBoundary = cache.Get("a");
        clock.SetUtcTicks(110L);
        CacheGetResult atBoundary = cache.Get("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(110L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(atBoundary.Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void Peek_WithPerEntryAbsoluteExpiration_DoesNotDependOnWriteTimeOffset()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.AbsoluteExpiration(205L));
        clock.SetUtcTicks(204L);
        CacheGetResult beforeBoundary = cache.Peek("a");
        clock.SetUtcTicks(205L);
        CacheGetResult atBoundary = cache.Peek("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.CreatedUtcTicks, Is.EqualTo(200L));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(205L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Get_WhenAbsoluteExpirationIsEarlierThanSliding_ExpiresAtAbsoluteBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.AbsoluteExpiration(310L);
        entryOptions.HasSlidingExpiration = true;
        entryOptions.SlidingExpirationTicks = 100L;

        cache.PutEntry("a", "one", entryOptions);
        clock.SetUtcTicks(309L);
        CacheGetResult beforeBoundary = cache.Get("a");
        clock.SetUtcTicks(310L);
        CacheGetResult atBoundary = cache.Get("a");

        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.LastAccessUtcTicks, Is.EqualTo(309L));
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(310L));
        Assert.That(atBoundary.Found, Is.False);
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Cleanup_WithAbsoluteExpiration_RemovesEntriesAtManualClockBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.AbsoluteExpiration(405L));
        clock.SetUtcTicks(404L);
        CacheMaintenanceResult beforeBoundary = cache.Cleanup();
        clock.SetUtcTicks(405L);
        CacheMaintenanceResult atBoundary = cache.Cleanup();

        Assert.That(beforeBoundary.ExpiredCount, Is.EqualTo(0L));
        Assert.That(beforeBoundary.CurrentCount, Is.EqualTo(1L));
        Assert.That(atBoundary.ExpiredCount, Is.EqualTo(1L));
        Assert.That(atBoundary.CurrentCount, Is.EqualTo(0L));
    }
}
