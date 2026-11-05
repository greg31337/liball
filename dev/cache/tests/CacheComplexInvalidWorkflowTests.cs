using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheComplexInvalidWorkflowTests
{
    [Test]
    public void InvalidWriteAttempts_DoNotReplaceExistingEntryOrProduceSideEffects()
    {
        int weighCount = 0;
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.Weigher = (key, value) =>
        {
            weighCount = weighCount + 1;
            if (value == null)
            {
                return 0L;
            }

            return ((string)value).Length;
        };
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions existingOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions rejectNullValue = CacheEntryOptions.NoExpiration();
        rejectNullValue.CacheNullValue = false;

        cache.PutEntry("stable", "kept", existingOptions);
        CacheWriteResult rejectedReplacement = cache.PutEntry("stable", null, rejectNullValue);
        CacheWriteResult rejectedNullKey = cache.PutEntry(null!, "bad", CacheEntryOptions.NoExpiration());
        CacheGetResult stable = cache.Get("stable");
        CacheSnapshot snapshot = cache.Snapshot();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(rejectedReplacement.Written, Is.False);
        Assert.That(rejectedReplacement.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(rejectedReplacement.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(rejectedReplacement.Key, Is.EqualTo("stable"));
        Assert.That(rejectedReplacement.Value, Is.Null);
        Assert.That(rejectedReplacement.Weight, Is.EqualTo(0L));
        Assert.That(rejectedReplacement.EvictedCount, Is.EqualTo(0L));
        Assert.That(rejectedNullKey.Written, Is.False);
        Assert.That(rejectedNullKey.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(rejectedNullKey.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(rejectedNullKey.Key, Is.Null);
        Assert.That(rejectedNullKey.Value, Is.EqualTo("bad"));
        Assert.That(stable.Found, Is.True);
        Assert.That(stable.Value, Is.EqualTo("kept"));
        Assert.That(snapshot.Count, Is.EqualTo(1L));
        Assert.That(snapshot.Weight, Is.EqualTo(4L));
        Assert.That(ArrayContains(snapshot.Keys, "stable"), Is.True);
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
        Assert.That(stats.Evictions, Is.EqualTo(0L));
        Assert.That(stats.Hits, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(4L));
        Assert.That(weighCount, Is.EqualTo(1));
        Assert.That(notifications, Is.Empty);
    }

    [Test]
    public void InvalidEntryOptions_AreNormalizedBeforeExpirationRefreshWeightAndPriorityBehavior()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(500L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions highPriority = CacheEntryOptions.NoExpiration();
        CacheEntryOptions invalidOptions = CacheEntryOptions.NoExpiration();
        highPriority.Priority = CacheEntryPriority.High;
        invalidOptions.HasAbsoluteExpiration = true;
        invalidOptions.AbsoluteExpirationUtcTicks = -10L;
        invalidOptions.HasTimeToLive = true;
        invalidOptions.TimeToLiveTicks = -20L;
        invalidOptions.HasSlidingExpiration = true;
        invalidOptions.SlidingExpirationTicks = -30L;
        invalidOptions.Weight = 0L;
        invalidOptions.Priority = (CacheEntryPriority)999;
        invalidOptions.HasRefreshAfterWrite = true;
        invalidOptions.RefreshAfterWriteTicks = -40L;

        cache.PutEntry("high-a", "one", highPriority);
        clock.AdvanceTicks(1L);
        CacheWriteResult invalidWrite = cache.PutEntry("invalid-options", "two", invalidOptions);
        CacheGetResult invalidRead = cache.Get("invalid-options");
        bool needsRefresh = cache.NeedsRefresh("invalid-options");
        clock.AdvanceTicks(1L);
        CacheWriteResult highWrite = cache.PutEntry("high-b", "three", highPriority);
        CacheSnapshot snapshot = cache.Snapshot();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(invalidWrite.Written, Is.True);
        Assert.That(invalidWrite.Weight, Is.EqualTo(1L));
        Assert.That(invalidRead.Found, Is.True);
        Assert.That(invalidRead.HasExpiration, Is.False);
        Assert.That(invalidRead.ExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(needsRefresh, Is.False);
        Assert.That(highWrite.EvictedCount, Is.EqualTo(1L));
        Assert.That(snapshot.Count, Is.EqualTo(2L));
        Assert.That(snapshot.Weight, Is.EqualTo(2L));
        Assert.That(ArrayContains(snapshot.Keys, "high-a"), Is.True);
        Assert.That(ArrayContains(snapshot.Keys, "high-b"), Is.True);
        Assert.That(ArrayContains(snapshot.Keys, "invalid-options"), Is.False);
        Assert.That(stats.Puts, Is.EqualTo(3L));
        Assert.That(stats.Hits, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(2L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(2L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("invalid-options"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[0].Weight, Is.EqualTo(1L));
    }

    static bool ArrayContains(object[] values, object expected)
    {
        foreach (object value in values)
        {
            if (object.Equals(value, expected))
            {
                return true;
            }
        }

        return false;
    }
}
