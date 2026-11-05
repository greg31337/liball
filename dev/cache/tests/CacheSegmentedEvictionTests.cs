using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheSegmentedEvictionTests
{
    [Test]
    public void SegmentedPolicy_UsesWindowAdmissionInsteadOfLruFallback()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        options.EvictionPolicy = CacheEvictionPolicy.Segmented;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("d", "four");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("a"), Is.True);
        Assert.That(cache.Contains("b"), Is.True);
        Assert.That(cache.Contains("c"), Is.False);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(cache._entries["a"].Segment, Is.EqualTo(CacheEntrySegment.Protected));
        Assert.That(cache._entries["b"].Segment, Is.EqualTo(CacheEntrySegment.Probation));
        Assert.That(cache._entries["d"].Segment, Is.EqualTo(CacheEntrySegment.Window));
        Assert.That(cache.Stats().Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("c"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    [Test]
    public void SegmentedPolicy_AdmitsFrequentWindowCandidateOverProbationVictim()
    {
        CacheClock clock = CacheClock.Manual(200L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        options.EvictionPolicy = CacheEvictionPolicy.Segmented;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        cache.Put("d", "four");
        clock.AdvanceTicks(1L);
        cache.Get("d");
        clock.AdvanceTicks(1L);
        cache.Get("d");
        clock.AdvanceTicks(1L);

        CacheWriteResult write = cache.Put("e", "five");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("a"), Is.True);
        Assert.That(cache.Contains("b"), Is.False);
        Assert.That(cache.Contains("d"), Is.True);
        Assert.That(cache.Contains("e"), Is.True);
        Assert.That(cache._entries["a"].Segment, Is.EqualTo(CacheEntrySegment.Protected));
        Assert.That(cache._entries["d"].Segment, Is.EqualTo(CacheEntrySegment.Probation));
        Assert.That(cache._entries["e"].Segment, Is.EqualTo(CacheEntrySegment.Window));
        Assert.That(cache.SegmentedFrequency("d"), Is.GreaterThan(cache.SegmentedFrequency("b")));
    }
}
