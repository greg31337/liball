using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheEntryOptionsTests
{
    [Test]
    public void Constructor_DefaultsToNoExpirationWithNormalMetadata()
    {
        CacheEntryOptions options = new CacheEntryOptions();

        Assert.That(options.HasAbsoluteExpiration, Is.False);
        Assert.That(options.AbsoluteExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(options.HasTimeToLive, Is.False);
        Assert.That(options.TimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(options.HasSlidingExpiration, Is.False);
        Assert.That(options.SlidingExpirationTicks, Is.EqualTo(0L));
        Assert.That(options.Weight, Is.EqualTo(1L));
        Assert.That(options.Priority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(options.HasRefreshAfterWrite, Is.False);
        Assert.That(options.RefreshAfterWriteTicks, Is.EqualTo(0L));
        Assert.That(options.CacheNullValue, Is.True);
    }

    [Test]
    public void NoExpiration_ReturnsDefaultNoExpirationOptions()
    {
        CacheEntryOptions options = CacheEntryOptions.NoExpiration();

        Assert.That(options.HasAbsoluteExpiration, Is.False);
        Assert.That(options.HasTimeToLive, Is.False);
        Assert.That(options.HasSlidingExpiration, Is.False);
        Assert.That(options.Weight, Is.EqualTo(1L));
        Assert.That(options.Priority, Is.EqualTo(CacheEntryPriority.Normal));
    }

    [Test]
    public void TimeToLive_SetsTtlOnly()
    {
        CacheEntryOptions options = CacheEntryOptions.TimeToLive(123L);

        Assert.That(options.HasTimeToLive, Is.True);
        Assert.That(options.TimeToLiveTicks, Is.EqualTo(123L));
        Assert.That(options.HasAbsoluteExpiration, Is.False);
        Assert.That(options.HasSlidingExpiration, Is.False);
    }

    [Test]
    public void TimeToLive_AllowsZeroTicksForImmediateExpiration()
    {
        CacheEntryOptions options = CacheEntryOptions.TimeToLive(0L);

        Assert.That(options.HasTimeToLive, Is.True);
        Assert.That(options.TimeToLiveTicks, Is.EqualTo(0L));
    }

    [Test]
    public void TimeToLive_RejectsNegativeTicks()
    {
        Assert.That(
            delegate
            {
                CacheEntryOptions.TimeToLive(-1L);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void SlidingExpiration_SetsSlidingExpirationOnly()
    {
        CacheEntryOptions options = CacheEntryOptions.SlidingExpiration(456L);

        Assert.That(options.HasSlidingExpiration, Is.True);
        Assert.That(options.SlidingExpirationTicks, Is.EqualTo(456L));
        Assert.That(options.HasAbsoluteExpiration, Is.False);
        Assert.That(options.HasTimeToLive, Is.False);
    }

    [Test]
    public void SlidingExpiration_AllowsZeroTicksForImmediateExpiration()
    {
        CacheEntryOptions options = CacheEntryOptions.SlidingExpiration(0L);

        Assert.That(options.HasSlidingExpiration, Is.True);
        Assert.That(options.SlidingExpirationTicks, Is.EqualTo(0L));
    }

    [Test]
    public void SlidingExpiration_RejectsNegativeTicks()
    {
        Assert.That(
            delegate
            {
                CacheEntryOptions.SlidingExpiration(-1L);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void AbsoluteExpiration_SetsAbsoluteExpirationOnly()
    {
        CacheEntryOptions options = CacheEntryOptions.AbsoluteExpiration(789L);

        Assert.That(options.HasAbsoluteExpiration, Is.True);
        Assert.That(options.AbsoluteExpirationUtcTicks, Is.EqualTo(789L));
        Assert.That(options.HasTimeToLive, Is.False);
        Assert.That(options.HasSlidingExpiration, Is.False);
    }

    [Test]
    public void AbsoluteExpiration_AllowsZeroTicksForImmediateExpiration()
    {
        CacheEntryOptions options = CacheEntryOptions.AbsoluteExpiration(0L);

        Assert.That(options.HasAbsoluteExpiration, Is.True);
        Assert.That(options.AbsoluteExpirationUtcTicks, Is.EqualTo(0L));
    }

    [Test]
    public void AbsoluteExpiration_RejectsNegativeTicks()
    {
        Assert.That(
            delegate
            {
                CacheEntryOptions.AbsoluteExpiration(-1L);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
