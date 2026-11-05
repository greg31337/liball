using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheExpiredReadTests
{
    [Test]
    public void Get_WhenTtlExpired_DoesNotReturnStaleValue()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "stale", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        CacheGetResult expired = cache.Get("a");
        CacheGetResult afterRemoval = cache.Get("a");

        Assert.That(expired.Found, Is.False);
        Assert.That(expired.Value, Is.Null);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(expired.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(afterRemoval.Found, Is.False);
        Assert.That(afterRemoval.Value, Is.Null);
        Assert.That(afterRemoval.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void Peek_WhenSlidingExpired_DoesNotReturnStaleValue()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "stale", CacheEntryOptions.SlidingExpiration(5L));
        clock.AdvanceTicks(5L);
        CacheGetResult expired = cache.Peek("a");
        CacheGetResult afterRemoval = cache.Peek("a");

        Assert.That(expired.Found, Is.False);
        Assert.That(expired.Value, Is.Null);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(expired.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(afterRemoval.Found, Is.False);
        Assert.That(afterRemoval.Value, Is.Null);
        Assert.That(afterRemoval.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void Get_WhenAbsoluteExpired_DoesNotOverwriteFreshReplacementValue()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.PutEntry("a", "stale", CacheEntryOptions.AbsoluteExpiration(305L));
        clock.SetUtcTicks(305L);
        CacheGetResult expired = cache.Get("a");
        cache.Put("a", "fresh");
        CacheGetResult fresh = cache.Get("a");

        Assert.That(expired.Found, Is.False);
        Assert.That(expired.Value, Is.Null);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(fresh.Found, Is.True);
        Assert.That(fresh.Value, Is.EqualTo("fresh"));
    }
}
