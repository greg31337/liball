using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheExpirationTests
{
    [Test]
    public void NoExpiration_HasNoEffectiveExpirationAndDoesNotExpire()
    {
        CacheEntryOptions options = CacheEntryOptions.NoExpiration();

        Assert.That(CacheExpiration.HasExpiration(options), Is.False);
        Assert.That(CacheExpiration.CalculateFixedExpirationUtcTicks(options, 10L), Is.EqualTo(0L));
        Assert.That(CacheExpiration.CalculateSlidingExpirationUtcTicks(options, 20L), Is.EqualTo(0L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 10L, 20L), Is.EqualTo(0L));
        Assert.That(CacheExpiration.IsExpired(options, 10L, 20L, long.MaxValue), Is.False);
    }

    [Test]
    public void AbsoluteExpiration_ExpiresAtConfiguredUtcTicks()
    {
        CacheEntryOptions options = CacheEntryOptions.AbsoluteExpiration(100L);

        Assert.That(CacheExpiration.HasExpiration(options), Is.True);
        Assert.That(CacheExpiration.CalculateFixedExpirationUtcTicks(options, 10L), Is.EqualTo(100L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 10L, 20L), Is.EqualTo(100L));
        Assert.That(CacheExpiration.IsExpired(options, 10L, 20L, 99L), Is.False);
        Assert.That(CacheExpiration.IsExpired(options, 10L, 20L, 100L), Is.True);
    }

    [Test]
    public void TimeToLive_ExpiresAtCreatedTimePlusDuration()
    {
        CacheEntryOptions options = CacheEntryOptions.TimeToLive(25L);

        Assert.That(CacheExpiration.HasExpiration(options), Is.True);
        Assert.That(CacheExpiration.CalculateFixedExpirationUtcTicks(options, 100L), Is.EqualTo(125L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 100L, 110L), Is.EqualTo(125L));
        Assert.That(CacheExpiration.IsExpired(options, 100L, 110L, 124L), Is.False);
        Assert.That(CacheExpiration.IsExpired(options, 100L, 110L, 125L), Is.True);
    }

    [Test]
    public void AbsoluteAndTimeToLive_UseEarliestFixedExpiration()
    {
        CacheEntryOptions options = CacheEntryOptions.AbsoluteExpiration(200L);
        options.HasTimeToLive = true;
        options.TimeToLiveTicks = 25L;

        Assert.That(CacheExpiration.CalculateFixedExpirationUtcTicks(options, 100L), Is.EqualTo(125L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 100L, 100L), Is.EqualTo(125L));
    }

    [Test]
    public void SlidingExpiration_ExpiresAtLastAccessPlusDuration()
    {
        CacheEntryOptions options = CacheEntryOptions.SlidingExpiration(30L);

        Assert.That(CacheExpiration.HasExpiration(options), Is.True);
        Assert.That(CacheExpiration.CalculateSlidingExpirationUtcTicks(options, 50L), Is.EqualTo(80L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 10L, 50L), Is.EqualTo(80L));
        Assert.That(CacheExpiration.IsExpired(options, 10L, 50L, 79L), Is.False);
        Assert.That(CacheExpiration.IsExpired(options, 10L, 50L, 80L), Is.True);
    }

    [Test]
    public void FixedAndSlidingExpiration_UseEarliestEffectiveExpiration()
    {
        CacheEntryOptions options = CacheEntryOptions.TimeToLive(100L);
        options.HasSlidingExpiration = true;
        options.SlidingExpirationTicks = 20L;

        Assert.That(CacheExpiration.CalculateFixedExpirationUtcTicks(options, 10L), Is.EqualTo(110L));
        Assert.That(CacheExpiration.CalculateSlidingExpirationUtcTicks(options, 40L), Is.EqualTo(60L));
        Assert.That(CacheExpiration.CalculateEffectiveExpirationUtcTicks(options, 10L, 40L), Is.EqualTo(60L));
    }

    [Test]
    public void ZeroDurationExpiration_ExpiresImmediatelyAtBoundary()
    {
        CacheEntryOptions ttl = CacheEntryOptions.TimeToLive(0L);
        CacheEntryOptions sliding = CacheEntryOptions.SlidingExpiration(0L);

        Assert.That(CacheExpiration.IsExpired(ttl, 100L, 100L, 99L), Is.False);
        Assert.That(CacheExpiration.IsExpired(ttl, 100L, 100L, 100L), Is.True);
        Assert.That(CacheExpiration.IsExpired(sliding, 100L, 200L, 199L), Is.False);
        Assert.That(CacheExpiration.IsExpired(sliding, 100L, 200L, 200L), Is.True);
    }

    [Test]
    public void AddTicks_SaturatesOnOverflow()
    {
        Assert.That(CacheExpiration.AddTicks(long.MaxValue - 1L, 5L), Is.EqualTo(long.MaxValue));
        Assert.That(CacheExpiration.AddTicks(long.MinValue + 1L, -5L), Is.EqualTo(long.MinValue));
    }

    [Test]
    public void CacheMath_SaturatesArithmeticOutsideLongRange()
    {
        Assert.That(CacheMath.Add(long.MaxValue, 1L), Is.EqualTo(long.MaxValue));
        Assert.That(CacheMath.Add(long.MinValue, -1L), Is.EqualTo(long.MinValue));
        Assert.That(CacheMath.Subtract(long.MinValue, 1L), Is.EqualTo(long.MinValue));
        Assert.That(CacheMath.Subtract(long.MaxValue, -1L), Is.EqualTo(long.MaxValue));
        Assert.That(CacheMath.AddScaled(long.MinValue, long.MaxValue, 10000L), Is.EqualTo(long.MaxValue));
    }
}
