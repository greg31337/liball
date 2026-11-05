using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheOptionsTests
{
    [Test]
    public void Constructor_DefaultsToNoExpirationNoCapacityAndLru()
    {
        CacheOptions options = new CacheOptions();

        Assert.That(options.Name, Is.EqualTo(""));
        Assert.That(options.KeyComparer, Is.Null);
        Assert.That(options.HasDefaultAbsoluteExpiration, Is.False);
        Assert.That(options.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(options.HasDefaultTimeToLive, Is.False);
        Assert.That(options.DefaultTimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(options.HasDefaultSlidingExpiration, Is.False);
        Assert.That(options.DefaultSlidingExpirationTicks, Is.EqualTo(0L));
        Assert.That(options.HasMaximumEntries, Is.False);
        Assert.That(options.MaximumEntries, Is.EqualTo(0L));
        Assert.That(options.HasMaximumWeight, Is.False);
        Assert.That(options.MaximumWeight, Is.EqualTo(0L));
        Assert.That(options.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(options.DefaultPriority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(options.Clock, Is.Not.Null);
        Assert.That(options.Loader, Is.Null);
        Assert.That(options.Weigher, Is.Null);
        Assert.That(options.RemovalListener, Is.Null);
        Assert.That(options.RecordStats, Is.False);
        Assert.That(options.HasCleanupInterval, Is.False);
        Assert.That(options.CleanupIntervalTicks, Is.EqualTo(0L));
        Assert.That(options.CacheNullLoaderValues, Is.False);
    }

    [Test]
    public void Default_ReturnsFreshDefaultOptions()
    {
        CacheOptions first = CacheOptions.Default();
        CacheOptions second = CacheOptions.Default();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.Clock, Is.Not.Null);
        Assert.That(second.Clock, Is.Not.Null);
        Assert.That(first.Clock, Is.Not.SameAs(second.Clock));
        Assert.That(first.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(second.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
    }

    [Test]
    public void PublicFields_CanConfigureCachePolicyInputs()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(10L);

        options.Name = "primary";
        options.KeyComparer = EqualityComparer<object>.Default;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 20L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 30L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 40L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 50L;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
        options.DefaultPriority = CacheEntryPriority.High;
        options.Clock = clock;
        options.RecordStats = true;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 60L;
        options.CacheNullLoaderValues = true;

        Assert.That(options.Name, Is.EqualTo("primary"));
        Assert.That(options.KeyComparer, Is.SameAs(EqualityComparer<object>.Default));
        Assert.That(options.HasDefaultTimeToLive, Is.True);
        Assert.That(options.DefaultTimeToLiveTicks, Is.EqualTo(20L));
        Assert.That(options.HasDefaultSlidingExpiration, Is.True);
        Assert.That(options.DefaultSlidingExpirationTicks, Is.EqualTo(30L));
        Assert.That(options.HasMaximumEntries, Is.True);
        Assert.That(options.MaximumEntries, Is.EqualTo(40L));
        Assert.That(options.HasMaximumWeight, Is.True);
        Assert.That(options.MaximumWeight, Is.EqualTo(50L));
        Assert.That(options.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Fifo));
        Assert.That(options.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(options.Clock, Is.SameAs(clock));
        Assert.That(options.RecordStats, Is.True);
        Assert.That(options.HasCleanupInterval, Is.True);
        Assert.That(options.CleanupIntervalTicks, Is.EqualTo(60L));
        Assert.That(options.CacheNullLoaderValues, Is.True);
    }

    [Test]
    public void CallbackFields_AcceptLoaderWeigherAndRemovalListener()
    {
        CacheOptions options = CacheOptions.Default();
        CacheNotification observed = new CacheNotification();

        options.Loader = key => "loaded-" + key.ToString();
        options.Weigher = (key, value) => 7L;
        options.RemovalListener = notification => observed = notification;

        object? loaded = options.Loader!("a");
        long weight = options.Weigher!("a", loaded);
        CacheNotification notification = new CacheNotification();
        notification.Key = "a";
        notification.Value = loaded;
        notification.Reason = CacheRemovalReason.Replace;
        options.RemovalListener!(notification);

        Assert.That(loaded, Is.EqualTo("loaded-a"));
        Assert.That(weight, Is.EqualTo(7L));
        Assert.That(observed, Is.SameAs(notification));
    }
}
