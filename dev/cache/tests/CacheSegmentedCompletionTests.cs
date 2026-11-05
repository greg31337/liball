using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheSegmentedCompletionTests
{
    [Test]
    public void SegmentedPolicy_AgesHistoricFrequenciesAtSampleBoundary()
    {
        CacheOptions options = SegmentedOptions(2L);
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache.Put("frequent", "one");
        cache.Put("cold", "two");

        for (int index = 0; index < 18; index++)
        {
            cache.Get("frequent");
        }

        Assert.That(cache._frequencyEvents, Is.EqualTo(0L));
        Assert.That(cache.SegmentedFrequency("frequent"), Is.EqualTo(9L));
        Assert.That(cache.SegmentedFrequency("cold"), Is.EqualTo(0L));
        Assert.That(cache.Stats().Hits, Is.EqualTo(18L));
    }

    [Test]
    public void SegmentedPolicy_AdaptsWindowTowardRecentAndMainHitRates()
    {
        CacheOptions options = SegmentedOptions(10L);
        Cache cache = new Cache(options);
        cache.Put("main", "one");
        cache.Put("window-a", "two");
        cache.Put("window-b", "three");

        for (int index = 0; index < 40; index++)
        {
            cache.Get("window-b");
        }

        Assert.That(cache._segmentedWindowPercent, Is.EqualTo(25));

        for (int index = 0; index < 40; index++)
        {
            cache.Get("main");
        }

        Assert.That(cache._segmentedWindowPercent, Is.EqualTo(20));
        Assert.That(cache._segmentedAdaptationEvents, Is.EqualTo(0L));
    }

    [Test]
    public void SegmentedPolicy_DemotesLeastRecentProtectedEntryWhenProtectedSegmentIsFull()
    {
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = SegmentedOptions(5L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        cache.Put("b", "two");
        cache.Put("c", "three");
        cache.Put("d", "four");
        cache.Put("window", "five");

        cache.Get("a");
        clock.AdvanceTicks(1L);
        cache.Get("b");
        clock.AdvanceTicks(1L);
        cache.Get("c");
        clock.AdvanceTicks(1L);
        cache.Get("d");

        Assert.That(cache._entries["a"].Segment, Is.EqualTo(CacheEntrySegment.Probation));
        Assert.That(cache._entries["b"].Segment, Is.EqualTo(CacheEntrySegment.Protected));
        Assert.That(cache._entries["c"].Segment, Is.EqualTo(CacheEntrySegment.Protected));
        Assert.That(cache._entries["d"].Segment, Is.EqualTo(CacheEntrySegment.Protected));
        Assert.That(cache._entries["window"].Segment, Is.EqualTo(CacheEntrySegment.Window));
    }

    [Test]
    public void SegmentedPolicy_EvictsLowerPriorityEntryBeforeAdmissionCandidate()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = SegmentedOptions(3L);
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        low.Priority = CacheEntryPriority.Low;
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        high.Priority = CacheEntryPriority.High;

        cache.PutEntry("low", "one", low);
        cache.Put("normal", "two");
        cache.PutEntry("high", "three", high);
        CacheWriteResult write = cache.Put("new", "four");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("low"), Is.False);
        Assert.That(cache.Contains("normal"), Is.True);
        Assert.That(cache.Contains("high"), Is.True);
        Assert.That(cache.Contains("new"), Is.True);
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("low"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    [Test]
    public void SegmentedPolicy_EnforcesWeightLimitWithFrequencyAdmission()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = 5L;
        options.EvictionPolicy = CacheEvictionPolicy.Segmented;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions one = CacheEntryOptions.NoExpiration();
        one.Weight = 1L;
        CacheEntryOptions two = CacheEntryOptions.NoExpiration();
        two.Weight = 2L;
        CacheEntryOptions three = CacheEntryOptions.NoExpiration();
        three.Weight = 3L;

        cache.PutEntry("one", "a", one);
        cache.PutEntry("two", "b", two);
        CacheWriteResult write = cache.PutEntry("three", "c", three);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("one"), Is.True);
        Assert.That(cache.Contains("two"), Is.True);
        Assert.That(cache.Contains("three"), Is.False);
        Assert.That(stats.CurrentSize, Is.EqualTo(2L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(3L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("three"));
    }

    [Test]
    public void SegmentedPolicy_ReplacementReentersWindowAndRetainsHistoricFrequency()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = SegmentedOptions(3L);
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("a", "old");
        cache.Put("b", "two");
        cache.Put("c", "three");
        cache.Get("a");

        CacheWriteResult replacement = cache.Put("a", "new");
        CacheWriteResult next = cache.Put("d", "four");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(replacement.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(next.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("a"), Is.True);
        Assert.That(cache.Contains("b"), Is.False);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(cache.Peek("a").Value, Is.EqualTo("new"));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(HasNotification(notifications, "a", CacheRemovalReason.Replace), Is.True);
        Assert.That(HasNotification(notifications, "b", CacheRemovalReason.Capacity), Is.True);
    }

    [Test]
    public void SegmentedPolicy_RemovesExpiredEntryBeforeCapacityAdmission()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(500L);
        CacheOptions options = SegmentedOptions(3L);
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.PutEntry("expired", "old", CacheEntryOptions.TimeToLive(5L));
        cache.Put("b", "two");
        cache.Put("c", "three");
        clock.AdvanceTicks(5L);

        CacheWriteResult write = cache.Put("d", "four");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(write.EvictedCount, Is.EqualTo(0L));
        Assert.That(cache.Contains("expired"), Is.False);
        Assert.That(cache.Contains("b"), Is.True);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(0L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void SegmentedPolicy_UsesOldestProbationEntryToBreakVictimFrequencyTie()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(700L);
        CacheOptions options = SegmentedOptions(4L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");
        clock.AdvanceTicks(1L);
        cache.Put("d", "four");
        clock.AdvanceTicks(1L);
        cache.Get("d");
        clock.AdvanceTicks(1L);

        CacheWriteResult write = cache.Put("e", "five");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("a"), Is.False);
        Assert.That(cache.Contains("b"), Is.True);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(cache.Contains("e"), Is.True);
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("a"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    static CacheOptions SegmentedOptions(long maximumEntries)
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = maximumEntries;
        options.EvictionPolicy = CacheEvictionPolicy.Segmented;
        return options;
    }

    static bool HasNotification(List<CacheNotification> notifications, object key, CacheRemovalReason reason)
    {
        foreach (CacheNotification notification in notifications)
        {
            if (object.Equals(notification.Key, key) && notification.Reason == reason)
            {
                return true;
            }
        }

        return false;
    }
}
