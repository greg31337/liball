using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheBuilderTests
{
    [Test]
    public void Build_WithFluentOptions_ConfiguresPolicy()
    {
        CacheClock clock = CacheClock.Manual(100L);
        Cache cache = CacheBuilder.Create()
            .WithName("built")
            .WithClock(clock)
            .WithMaximumEntries(2L)
            .WithMaximumWeight(5L)
            .WithTimeToLive(10L)
            .WithSlidingExpiration(20L)
            .WithDefaultPriority(CacheEntryPriority.High)
            .WithStats()
            .Build();

        CachePolicySnapshot policy = cache.Policy();

        Assert.That(policy.Name, Is.EqualTo("built"));
        Assert.That(policy.HasMaximumEntries, Is.True);
        Assert.That(policy.MaximumEntries, Is.EqualTo(2L));
        Assert.That(policy.HasMaximumWeight, Is.True);
        Assert.That(policy.MaximumWeight, Is.EqualTo(5L));
        Assert.That(policy.HasDefaultTimeToLive, Is.True);
        Assert.That(policy.DefaultTimeToLiveTicks, Is.EqualTo(10L));
        Assert.That(policy.HasDefaultSlidingExpiration, Is.True);
        Assert.That(policy.DefaultSlidingExpirationTicks, Is.EqualTo(20L));
        Assert.That(policy.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(policy.RecordStats, Is.True);
        Assert.That(policy.SnapshotUtcTicks, Is.EqualTo(100L));

        cache.Stop();
    }

    [Test]
    public void Build_WithMaximumEntries_EnforcesCapacity()
    {
        Cache cache = new CacheBuilder()
            .WithMaximumEntries(1L)
            .Build();

        cache.Put("a", "one");
        CacheWriteResult second = cache.Put("b", "two");

        Assert.That(second.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Get("a").Found, Is.False);
        Assert.That(cache.Get("b").Found, Is.True);
    }

    [Test]
    public void Build_WithEvictionPolicy_ReportsAndUsesSelectedPolicy()
    {
        CacheClock clock = CacheClock.Manual(100L);
        ICacheBuilder builder = new CacheBuilder();
        Cache cache = builder
            .WithClock(clock)
            .WithMaximumEntries(2L)
            .WithEvictionPolicy(CacheEvictionPolicy.Fifo)
            .Build();

        cache.Put("first", "one");
        clock.AdvanceTicks(1L);
        cache.Put("second", "two");
        clock.AdvanceTicks(1L);
        cache.Get("first");
        cache.Put("third", "three");
        CachePolicySnapshot policy = cache.Policy();

        Assert.That(policy.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Fifo));
        Assert.That(cache.Contains("first"), Is.False);
        Assert.That(cache.Contains("second"), Is.True);
        Assert.That(cache.Contains("third"), Is.True);
    }

    [Test]
    public void Build_WithUnknownEvictionPolicy_NormalizesToLru()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        Cache cache = new CacheBuilder()
            .WithOptions(options)
            .WithEvictionPolicy((CacheEvictionPolicy)999)
            .Build();

        CachePolicySnapshot policy = cache.Policy();

        Assert.That(policy.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
    }

    [Test]
    public void Build_WithLoaderStatsAndCacheNullValues_WiresReadThroughBehavior()
    {
        int loadCount = 0;
        Cache cache = new CacheBuilder()
            .WithStats()
            .WithCacheNullLoaderValues(true)
            .WithLoader(key =>
            {
                loadCount = loadCount + 1;
                return null;
            })
            .Build();

        CacheGetResult first = cache.GetOrLoad("a");
        CacheGetResult second = cache.GetOrLoad("a");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(first.Found, Is.True);
        Assert.That(first.Loaded, Is.True);
        Assert.That(second.Found, Is.True);
        Assert.That(second.Loaded, Is.False);
        Assert.That(loadCount, Is.EqualTo(1));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
    }

    [Test]
    public void Build_CopiesOptionsSoLaterBuilderChangesDoNotMutateExistingCache()
    {
        CacheBuilder builder = new CacheBuilder();
        Cache first = builder.WithMaximumEntries(1L).Build();
        Cache second = builder.WithMaximumEntries(3L).Build();

        CachePolicySnapshot firstPolicy = first.Policy();
        CachePolicySnapshot secondPolicy = second.Policy();

        Assert.That(firstPolicy.MaximumEntries, Is.EqualTo(1L));
        Assert.That(secondPolicy.MaximumEntries, Is.EqualTo(3L));
    }

    [Test]
    public void Build_WithAbsoluteExpirationWeigherRemovalListenerAndCleanupInterval_WiresOptions()
    {
        int notificationCount = 0;
        CacheClock clock = CacheClock.Manual(500L);
        Cache cache = new CacheBuilder()
            .WithClock(clock)
            .WithAbsoluteExpiration(505L)
            .WithMaximumWeight(3L)
            .WithWeigher((key, value) => 2L)
            .WithRemovalListener(notification =>
            {
                if (notification.Reason == CacheRemovalReason.Capacity)
                {
                    notificationCount = notificationCount + 1;
                }
            })
            .WithCleanupInterval(36000000000L)
            .Build();

        CachePolicySnapshot policy = cache.Policy();
        cache.Put("a", "one");
        CacheWriteResult second = cache.Put("b", "two");

        Assert.That(policy.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(policy.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(505L));
        Assert.That(policy.HasMaximumWeight, Is.True);
        Assert.That(policy.MaximumWeight, Is.EqualTo(3L));
        Assert.That(policy.HasCleanupInterval, Is.True);
        Assert.That(policy.CleanupIntervalTicks, Is.EqualTo(36000000000L));
        Assert.That(second.EvictedCount, Is.EqualTo(1L));
        Assert.That(notificationCount, Is.EqualTo(1));

        cache.Stop();
    }
}
