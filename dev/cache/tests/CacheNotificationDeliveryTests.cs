using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheNotificationDeliveryTests
{
    [Test]
    public void Put_WhenReplacingEntry_DeliversReplaceNotification()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 4L;

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(5L);
        cache.Put("a", "two");

        Assert.That(notifications, Has.Count.EqualTo(1));
        AssertNotification(notifications[0], "a", "one", CacheRemovalReason.Replace, 4L, 100L, 100L, 100L, 105L);
    }

    [Test]
    public void Remove_WhenEntryExists_DeliversExplicitRemoveNotification()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(10L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(2L);
        cache.Get("a");
        clock.AdvanceTicks(3L);
        CacheRemoveResult result = cache.Remove("a");

        Assert.That(result.Removed, Is.True);
        Assert.That(notifications, Has.Count.EqualTo(1));
        AssertNotification(notifications[0], "a", "one", CacheRemovalReason.ExplicitRemove, 1L, 10L, 12L, 10L, 15L);
    }

    [Test]
    public void Clear_DeliversClearNotificationForEachEntry()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(20L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        clock.AdvanceTicks(5L);
        CacheMaintenanceResult result = cache.Clear();

        Assert.That(result.RemovedCount, Is.EqualTo(2L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(NotificationFor(notifications, "a").Reason, Is.EqualTo(CacheRemovalReason.Clear));
        Assert.That(NotificationFor(notifications, "a").RemovedUtcTicks, Is.EqualTo(25L));
        Assert.That(NotificationFor(notifications, "b").Reason, Is.EqualTo(CacheRemovalReason.Clear));
        Assert.That(NotificationFor(notifications, "b").RemovedUtcTicks, Is.EqualTo(25L));
    }

    [Test]
    public void Get_WhenEntryExpired_DeliversExpiredNotification()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(30L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        CacheGetResult result = cache.Get("a");

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(notifications, Has.Count.EqualTo(1));
        AssertNotification(notifications[0], "a", "one", CacheRemovalReason.Expired, 1L, 30L, 30L, 30L, 35L);
    }

    [Test]
    public void Cleanup_WhenEntriesExpired_DeliversExpiredNotifications()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(40L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.PutEntry("a", "one", CacheEntryOptions.TimeToLive(5L));
        cache.PutEntry("b", "two", CacheEntryOptions.NoExpiration());
        clock.AdvanceTicks(5L);
        CacheMaintenanceResult result = cache.Cleanup();

        Assert.That(result.ExpiredCount, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("a"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(notifications[0].RemovedUtcTicks, Is.EqualTo(45L));
    }

    [Test]
    public void Put_WhenCapacityEvictsEntry_DeliversCapacityNotification()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(50L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(3L);
        CacheWriteResult result = cache.Put("b", "two");

        Assert.That(result.EvictedCount, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        AssertNotification(notifications[0], "a", "one", CacheRemovalReason.Capacity, 1L, 50L, 50L, 50L, 53L);
    }

    [Test]
    public void RemovalListenerExceptions_PropagateAfterStateMutation()
    {
        CacheOptions options = CacheOptions.Default();
        options.RemovalListener = notification => throw new InvalidOperationException("listener failed");
        Cache cache = new Cache(options);
        cache.Put("a", "one");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.Remove("a");
            })!;

        Assert.That(exception.Message, Is.EqualTo("listener failed"));
        Assert.That(cache.Get("a").Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void RemovalNotifications_CoverReplaceRemoveClearExpiredAndCapacityReasons()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(1000L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        cache.Put("replace", "old");
        cache.Put("replace", "new");
        cache.Put("remove", "value");
        cache.Remove("remove");
        cache.Put("clear", "value");
        cache.Clear();
        cache.PutEntry("expired", "value", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        cache.Get("expired");
        cache.Put("capacity-a", "value");
        cache.Put("capacity-b", "value");
        cache.Put("capacity-c", "value");

        Assert.That(ContainsReason(notifications, CacheRemovalReason.Replace), Is.True);
        Assert.That(ContainsReason(notifications, CacheRemovalReason.ExplicitRemove), Is.True);
        Assert.That(ContainsReason(notifications, CacheRemovalReason.Clear), Is.True);
        Assert.That(ContainsReason(notifications, CacheRemovalReason.Expired), Is.True);
        Assert.That(ContainsReason(notifications, CacheRemovalReason.Capacity), Is.True);
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

    static bool ContainsReason(List<CacheNotification> notifications, CacheRemovalReason reason)
    {
        foreach (CacheNotification notification in notifications)
        {
            if (notification.Reason == reason)
            {
                return true;
            }
        }

        return false;
    }

    static void AssertNotification(
        CacheNotification notification,
        object key,
        object value,
        CacheRemovalReason reason,
        long weight,
        long createdUtcTicks,
        long lastAccessUtcTicks,
        long lastWriteUtcTicks,
        long removedUtcTicks)
    {
        Assert.That(notification.Key, Is.EqualTo(key));
        Assert.That(notification.Value, Is.EqualTo(value));
        Assert.That(notification.Reason, Is.EqualTo(reason));
        Assert.That(notification.Weight, Is.EqualTo(weight));
        Assert.That(notification.CreatedUtcTicks, Is.EqualTo(createdUtcTicks));
        Assert.That(notification.LastAccessUtcTicks, Is.EqualTo(lastAccessUtcTicks));
        Assert.That(notification.LastWriteUtcTicks, Is.EqualTo(lastWriteUtcTicks));
        Assert.That(notification.RemovedUtcTicks, Is.EqualTo(removedUtcTicks));
    }
}
