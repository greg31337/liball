using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePolicySnapshotTests
{
    [Test]
    public void Policy_ReturnsNormalizedConfigurationAndRuntimeState()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.Name = "primary";
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = 1000L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 50L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 25L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 10L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 20L;
        options.EvictionPolicy = CacheEvictionPolicy.Lru;
        options.DefaultPriority = CacheEntryPriority.High;
        options.RecordStats = true;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 500L;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 3L;
        cache.PutEntry("a", "one", entryOptions);

        CachePolicySnapshot snapshot = cache.Policy();

        Assert.That(snapshot.Name, Is.EqualTo("primary"));
        Assert.That(snapshot.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(snapshot.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(1000L));
        Assert.That(snapshot.HasDefaultTimeToLive, Is.True);
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(50L));
        Assert.That(snapshot.HasDefaultSlidingExpiration, Is.True);
        Assert.That(snapshot.DefaultSlidingExpirationTicks, Is.EqualTo(25L));
        Assert.That(snapshot.HasMaximumEntries, Is.True);
        Assert.That(snapshot.MaximumEntries, Is.EqualTo(10L));
        Assert.That(snapshot.HasMaximumWeight, Is.True);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(20L));
        Assert.That(snapshot.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(snapshot.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(snapshot.RecordStats, Is.True);
        Assert.That(snapshot.HasCleanupInterval, Is.True);
        Assert.That(snapshot.CleanupIntervalTicks, Is.EqualTo(500L));
        Assert.That(snapshot.CurrentCount, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(3L));
        Assert.That(snapshot.SnapshotUtcTicks, Is.EqualTo(100L));
    }

    [Test]
    public void Policy_ReturnsNormalizedValuesForInvalidConfiguration()
    {
        CacheOptions options = CacheOptions.Default();
        options.Name = null;
        options.HasMaximumEntries = true;
        options.MaximumEntries = -1L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 0L;
        options.EvictionPolicy = (CacheEvictionPolicy)999;
        options.DefaultPriority = (CacheEntryPriority)999;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = -1L;
        Cache cache = new Cache(options);

        CachePolicySnapshot snapshot = cache.Policy();

        Assert.That(snapshot.Name, Is.EqualTo(""));
        Assert.That(snapshot.HasMaximumEntries, Is.False);
        Assert.That(snapshot.MaximumEntries, Is.EqualTo(0L));
        Assert.That(snapshot.HasMaximumWeight, Is.False);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(0L));
        Assert.That(snapshot.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(snapshot.DefaultPriority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(snapshot.HasCleanupInterval, Is.False);
        Assert.That(snapshot.CleanupIntervalTicks, Is.EqualTo(0L));
    }

    [Test]
    public void Policy_ReturnsPointInTimeSnapshot()
    {
        Cache cache = new Cache(CacheOptions.Default());

        cache.Put("a", "one");
        CachePolicySnapshot before = cache.Policy();
        cache.Put("b", "two");
        CachePolicySnapshot after = cache.Policy();

        Assert.That(before.CurrentCount, Is.EqualTo(1L));
        Assert.That(before.CurrentWeight, Is.EqualTo(1L));
        Assert.That(after.CurrentCount, Is.EqualTo(2L));
        Assert.That(after.CurrentWeight, Is.EqualTo(2L));
    }

    [Test]
    public void Policy_ReturnsOldestHottestAndExpiringSoonKeys()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.Put("oldest", "one");
        clock.AdvanceTicks(1L);
        cache.PutEntry("expires-later", "two", CacheEntryOptions.TimeToLive(50L));
        clock.AdvanceTicks(1L);
        cache.PutEntry("hottest", "three", CacheEntryOptions.SlidingExpiration(30L));
        clock.AdvanceTicks(1L);
        cache.PutEntry("expires-first", "four", CacheEntryOptions.AbsoluteExpiration(120L));
        clock.AdvanceTicks(1L);
        cache.Get("hottest");
        clock.AdvanceTicks(1L);
        cache.Get("expires-later");
        clock.AdvanceTicks(1L);
        cache.Get("hottest");

        CachePolicySnapshot snapshot = cache.Policy();

        Assert.That(snapshot.OldestKeys, Is.EqualTo(new object[] { "oldest", "expires-later", "hottest", "expires-first" }));
        Assert.That(snapshot.HottestKeys, Is.EqualTo(new object[] { "hottest", "expires-later", "expires-first", "oldest" }));
        Assert.That(snapshot.ExpiringSoonKeys, Is.EqualTo(new object[] { "expires-first", "hottest", "expires-later" }));
    }

    [Test]
    public void Policy_KeyListsArePointInTimeArrays()
    {
        Cache cache = new Cache(CacheOptions.Default());

        cache.Put("a", "one");
        CachePolicySnapshot before = cache.Policy();
        cache.Get("a");
        cache.Put("b", "two");
        CachePolicySnapshot after = cache.Policy();

        Assert.That(before.OldestKeys, Is.EqualTo(new object[] { "a" }));
        Assert.That(before.HottestKeys, Is.EqualTo(new object[] { "a" }));
        Assert.That(before.ExpiringSoonKeys, Is.Empty);
        Assert.That(after.OldestKeys, Is.EqualTo(new object[] { "a", "b" }));
        Assert.That(after.HottestKeys, Is.EqualTo(new object[] { "a", "b" }));
    }
}
