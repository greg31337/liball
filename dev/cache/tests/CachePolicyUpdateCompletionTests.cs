using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePolicyUpdateCompletionTests
{
    [Test]
    public void UpdatePolicy_IncreasingCountAndWeightLimitsAllowsMoreEntries()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 1L;
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        cache.Put("b", "two");
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 3L;
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = 3L;

        CachePolicySnapshot increased = cache.UpdatePolicy(update);
        cache.Put("c", "three");
        cache.Put("d", "four");
        CachePolicySnapshot afterWrites = cache.Policy();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(increased.MaximumEntries, Is.EqualTo(3L));
        Assert.That(increased.MaximumWeight, Is.EqualTo(3L));
        Assert.That(increased.CurrentCount, Is.EqualTo(1L));
        Assert.That(afterWrites.CurrentCount, Is.EqualTo(3L));
        Assert.That(afterWrites.CurrentWeight, Is.EqualTo(3L));
        Assert.That(cache.Contains("b"), Is.True);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(stats.Evictions, Is.EqualTo(1L));
    }

    [Test]
    public void UpdatePolicy_DecreasingCountUsesCurrentLruOrder()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 2L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);

        Assert.That(snapshot.CurrentCount, Is.EqualTo(2L));
        Assert.That(cache.Contains("a"), Is.True);
        Assert.That(cache.Contains("b"), Is.False);
        Assert.That(cache.Contains("c"), Is.True);
    }

    [Test]
    public void UpdatePolicy_MaximumWeightDetectsSaturatedAggregateOverflow()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RecordStats = true;
        Cache cache = new Cache(options);
        CacheEntryOptions huge = CacheEntryOptions.NoExpiration();
        huge.Weight = long.MaxValue;
        cache.PutEntry("huge", "one", huge);
        clock.AdvanceTicks(1L);
        cache.Put("small", "two");
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumWeight = true;
        update.MaximumWeight = long.MaxValue;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(snapshot.HasMaximumWeight, Is.True);
        Assert.That(snapshot.MaximumWeight, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.CurrentCount, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeight, Is.EqualTo(1L));
        Assert.That(cache.Contains("huge"), Is.False);
        Assert.That(cache.Contains("small"), Is.True);
        Assert.That(stats.Evictions, Is.EqualTo(1L));
    }

    [Test]
    public void UpdatePolicy_MaximumExpirationValuesSaturateLaterEntryBoundaries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(long.MaxValue - 5L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateDefaultAbsoluteExpiration = true;
        update.DefaultAbsoluteExpirationUtcTicks = long.MaxValue;
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = long.MaxValue;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = long.MaxValue;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        cache.Put("key", "value");
        CacheGetResult beforeBoundary = cache.Peek("key");
        clock.AdvanceTicks(5L);
        CacheGetResult atBoundary = cache.Peek("key");

        Assert.That(snapshot.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(long.MaxValue));
        Assert.That(snapshot.DefaultSlidingExpirationTicks, Is.EqualTo(long.MaxValue));
        Assert.That(beforeBoundary.Found, Is.True);
        Assert.That(beforeBoundary.ExpirationUtcTicks, Is.EqualTo(long.MaxValue));
        Assert.That(atBoundary.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void UpdatePolicy_ZeroExpirationDurationsRemainEnabledAndExpireImmediately()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateDefaultTimeToLive = true;
        update.DefaultTimeToLiveTicks = 0L;
        update.UpdateDefaultSlidingExpiration = true;
        update.DefaultSlidingExpirationTicks = 0L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        cache.Put("key", "value");
        CacheGetResult result = cache.Get("key");

        Assert.That(snapshot.HasDefaultTimeToLive, Is.True);
        Assert.That(snapshot.DefaultTimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(snapshot.HasDefaultSlidingExpiration, Is.True);
        Assert.That(snapshot.DefaultSlidingExpirationTicks, Is.EqualTo(0L));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void UpdatePolicy_ReductionExpiresOldEntriesBeforeCapacityEviction()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.PutEntry("expired", "gone", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(1L);
        cache.Put("older-live", "one");
        clock.AdvanceTicks(1L);
        cache.Put("newer-live", "two");
        clock.AdvanceTicks(3L);
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 1L;

        CachePolicySnapshot snapshot = cache.UpdatePolicy(update);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(snapshot.CurrentCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("expired"), Is.False);
        Assert.That(cache.Contains("older-live"), Is.False);
        Assert.That(cache.Contains("newer-live"), Is.True);
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[1].Key, Is.EqualTo("older-live"));
    }

    [Test]
    public void UpdatePolicy_ReturnedSnapshotAndAppliedValuesDoNotRetainMutableInputs()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("a", "one");
        CachePolicyUpdate update = new CachePolicyUpdate();
        update.UpdateMaximumEntries = true;
        update.MaximumEntries = 3L;

        CachePolicySnapshot applied = cache.UpdatePolicy(update);
        update.MaximumEntries = 1L;
        applied.OldestKeys[0] = "changed";
        cache.Put("b", "two");
        CachePolicySnapshot current = cache.Policy();

        Assert.That(applied.MaximumEntries, Is.EqualTo(3L));
        Assert.That(applied.CurrentCount, Is.EqualTo(1L));
        Assert.That(current.MaximumEntries, Is.EqualTo(3L));
        Assert.That(current.CurrentCount, Is.EqualTo(2L));
        Assert.That(current.OldestKeys, Is.EqualTo(new object[] { "a", "b" }));
    }

    [Test]
    public void UpdatePolicy_ConcurrentReadersWritersAndUpdatesPreserveFinalLimit()
    {
        Cache cache = new Cache(CacheOptions.Default());
        ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task writer = Task.Run(() =>
        {
            start.Wait(5000);
            int index = 0;
            while (index < 200)
            {
                cache.Put("key-" + index.ToString(), index);
                index = index + 1;
            }
        });
        Task reader = Task.Run(() =>
        {
            start.Wait(5000);
            int index = 0;
            while (index < 400)
            {
                cache.Get("key-" + (index % 200).ToString());
                index = index + 1;
            }
        });
        Task updater = Task.Run(() =>
        {
            start.Wait(5000);
            int index = 0;
            while (index < 100)
            {
                CachePolicyUpdate update = new CachePolicyUpdate();
                update.UpdateMaximumEntries = true;
                if (index % 2 == 0)
                {
                    update.MaximumEntries = 7L;
                }
                else
                {
                    update.MaximumEntries = 0L;
                }
                cache.UpdatePolicy(update);
                index = index + 1;
            }
        });
        Task observer = Task.Run(() =>
        {
            start.Wait(5000);
            int index = 0;
            while (index < 100)
            {
                cache.Policy();
                cache.Count();
                index = index + 1;
            }
        });

        start.Set();
        Assert.That(Task.WaitAll(new Task[] { writer, reader, updater, observer }, 5000), Is.True);
        CachePolicyUpdate finalUpdate = new CachePolicyUpdate();
        finalUpdate.UpdateMaximumEntries = true;
        finalUpdate.MaximumEntries = 5L;
        CachePolicySnapshot finalSnapshot = cache.UpdatePolicy(finalUpdate);

        Assert.That(finalSnapshot.HasMaximumEntries, Is.True);
        Assert.That(finalSnapshot.MaximumEntries, Is.EqualTo(5L));
        Assert.That(finalSnapshot.CurrentCount, Is.LessThanOrEqualTo(5L));
        Assert.That(finalSnapshot.CurrentWeight, Is.EqualTo(finalSnapshot.CurrentCount));
        Assert.That(cache.Count(), Is.EqualTo(finalSnapshot.CurrentCount));
    }
}
