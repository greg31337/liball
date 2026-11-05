using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheComplexValidWorkflowTests
{
    [Test]
    public void ValidWorkflow_CombinesRefreshExpirationPriorityWeightSnapshotsAndStats()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(1000L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 10L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 7L;
        options.Weigher = (key, value) =>
        {
            if (value == null)
            {
                return 0L;
            }

            return ((string)value).Length;
        };
        options.Loader = key => "rr";
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions lowPriority = CacheEntryOptions.NoExpiration();
        CacheEntryOptions refreshAndSlide = CacheEntryOptions.SlidingExpiration(12L);
        lowPriority.Priority = CacheEntryPriority.Low;
        refreshAndSlide.Priority = CacheEntryPriority.High;
        refreshAndSlide.HasRefreshAfterWrite = true;
        refreshAndSlide.RefreshAfterWriteTicks = 4L;

        cache.PutEntry("low", "llll", lowPriority);
        clock.AdvanceTicks(1L);
        cache.PutEntry("combo", "cc", refreshAndSlide);
        clock.AdvanceTicks(1L);
        cache.Put("default", "d");
        clock.AdvanceTicks(3L);
        CacheGetResult accessed = cache.Get("combo");
        bool needsRefresh = cache.NeedsRefresh("combo");
        CacheGetResult refreshed = cache.Refresh("combo");
        clock.AdvanceTicks(7L);
        CacheMaintenanceResult cleanup = cache.Cleanup();
        CacheWriteResult capacityWrite = cache.Put("new", "nnn");
        CacheSnapshot snapshot = cache.Snapshot();
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(accessed.Found, Is.True);
        Assert.That(accessed.Value, Is.EqualTo("cc"));
        Assert.That(accessed.HasExpiration, Is.True);
        Assert.That(accessed.ExpirationUtcTicks, Is.EqualTo(1017L));
        Assert.That(needsRefresh, Is.True);
        Assert.That(refreshed.Found, Is.True);
        Assert.That(refreshed.Loaded, Is.True);
        Assert.That(refreshed.Value, Is.EqualTo("rr"));
        Assert.That(refreshed.LastWriteUtcTicks, Is.EqualTo(1005L));
        Assert.That(refreshed.ExpirationUtcTicks, Is.EqualTo(1015L));
        Assert.That(cache.NeedsRefresh("combo"), Is.False);
        Assert.That(cleanup.ExpiredCount, Is.EqualTo(1L));
        Assert.That(capacityWrite.EvictedCount, Is.EqualTo(1L));
        Assert.That(snapshot.Count, Is.EqualTo(2L));
        Assert.That(snapshot.Weight, Is.EqualTo(5L));
        Assert.That(ArrayContains(snapshot.Keys, "combo"), Is.True);
        Assert.That(ArrayContains(snapshot.Keys, "new"), Is.True);
        Assert.That(ArrayContains(snapshot.Keys, "default"), Is.False);
        Assert.That(ArrayContains(snapshot.Keys, "low"), Is.False);
        Assert.That(stats.Hits, Is.EqualTo(1L));
        Assert.That(stats.Misses, Is.EqualTo(0L));
        Assert.That(stats.Puts, Is.EqualTo(4L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(2L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(5L));
        Assert.That(notifications, Has.Count.EqualTo(3));
        Assert.That(NotificationFor(notifications, "combo").Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(NotificationFor(notifications, "default").Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(NotificationFor(notifications, "low").Reason, Is.EqualTo(CacheRemovalReason.Capacity));
    }

    static CacheNotification NotificationFor(List<CacheNotification> notifications, object key)
    {
        foreach (CacheNotification notification in notifications)
        {
            if (object.Equals(notification.Key, key))
            {
                return notification;
            }
        }

        Assert.Fail("Notification was not found for key " + key.ToString());
        return new CacheNotification();
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
