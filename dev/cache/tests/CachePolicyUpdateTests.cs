using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePolicyUpdateTests
{
    [Test]
    public void UpdatePolicy_ReducedCountAndWeightLimitsEvictImmediately()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 2L;
        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(1L);
        cache.PutEntry("b", "two", entryOptions);
        clock.AdvanceTicks(1L);
        cache.PutEntry("c", "three", entryOptions);
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 2L;
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = 2L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(snapshot.HasMaximumEntries, Is.True);
        Assert.That(snapshot.MaximumEntries, Is.EqualTo(2L));
        Assert.That(snapshot.HasMaximumWeight, Is.True);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(2L));
        Assert.That(snapshot.CurrentCount, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(2L));
        Assert.That(cache.Contains("a"), Is.False);
        Assert.That(cache.Contains("b"), Is.False);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(stats.Evictions, Is.EqualTo(2L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[0].Key, Is.EqualTo("a"));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[1].Key, Is.EqualTo("b"));
    }

    [Test]
    public void UpdatePolicy_NewExpirationDefaultsAffectLaterWritesButNotExistingEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 50L;
        Cache cache = new Cache(options);
        cache.Put("existing", "old-default");
        CacheGetResult existingBefore = cache.Peek("existing");
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateDefaultAbsoluteExpiration = true;
        update.DefaultAbsoluteExpirationUtcTicks = 1000L;
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = 10L;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = 20L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        CacheGetResult existingAfterUpdate = cache.Peek("existing");
        cache.Put("later", "new-defaults");
        clock.AdvanceTicks(10L);
        CacheGetResult later = cache.Peek("later");
        CacheGetResult existingLater = cache.Peek("existing");

        Assert.That(snapshot.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(snapshot.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(1000L));
        Assert.That(snapshot.HasDefaultTimeToLive, Is.True);
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(10L));
        Assert.That(snapshot.HasDefaultSlidingExpiration, Is.True);
        Assert.That(snapshot.DefaultSlidingExpirationTicks, Is.EqualTo(20L));
        Assert.That(existingBefore.ExpirationUtcTicks, Is.EqualTo(150L));
        Assert.That(existingAfterUpdate.ExpirationUtcTicks, Is.EqualTo(150L));
        Assert.That(later.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(existingLater.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(existingLater.ExpirationUtcTicks, Is.EqualTo(150L));
    }

    [Test]
    public void UpdatePolicy_NonPositiveLimitsAndNegativeExpirationsDisableSettings()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 4L;
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = 1000L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 100L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 100L;
        Cache cache = new Cache(options);
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 0L;
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = -1L;
        update.UpdateDefaultAbsoluteExpiration = true;
        update.DefaultAbsoluteExpirationUtcTicks = -1L;
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = -1L;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = -1L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        cache.Put("a", "one");
        cache.Put("b", "two");
        cache.Put("c", "three");
        cache.Put("d", "four");

        Assert.That(snapshot.HasMaximumEntries, Is.False);
        Assert.That(snapshot.MaximumEntries, Is.EqualTo(0L));
        Assert.That(snapshot.HasMaximumWeight, Is.False);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(0L));
        Assert.That(snapshot.HasDefaultAbsoluteExpiration, Is.False);
        Assert.That(snapshot.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(snapshot.HasDefaultTimeToLive, Is.False);
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(snapshot.HasDefaultSlidingExpiration, Is.False);
        Assert.That(snapshot.DefaultSlidingExpirationTicks, Is.EqualTo(0L));
        Assert.That(cache.Count(), Is.EqualTo(4L));
    }

    [Test]
    public void UpdatePolicy_LeavesSettingsWithoutEnabledFlagsUnchanged()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 5L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 10L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 20L;
        Cache cache = new Cache(options);
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 2L;
        update.MaximumWeight = 1L;
        update.DefaultTimeToLiveTicks = 1L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);

        Assert.That(snapshot.MaximumEntries, Is.EqualTo(2L));
        Assert.That(snapshot.HasMaximumWeight, Is.True);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(10L));
        Assert.That(snapshot.HasDefaultTimeToLive, Is.True);
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(20L));
    }
}
