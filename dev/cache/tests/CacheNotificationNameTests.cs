using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheNotificationNameTests
{
    [Test]
    public void RemovalNotification_ContainsConfiguredCacheName()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.Name = "primary-cache";
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("key", "value");

        cache.Remove("key");

        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Name, Is.EqualTo("primary-cache"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
    }

    [Test]
    public void RemovalNotification_NormalizesNullCacheNameToEmptyString()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.Name = null;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("key", "old");

        cache.Put("key", "new");

        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Name, Is.EqualTo(""));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
    }

    [Test]
    public void CacheName_IsolatedFromOriginalOptionsAndCopiedToPolicyAndEveryNotificationReason()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Name = "original-name";
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);

        options.Name = "mutated-options-name";
        CachePolicySnapshot firstPolicy = cache.Policy();
        firstPolicy.Name = "mutated-snapshot-name";
        CachePolicySnapshot secondPolicy = cache.Policy();

        cache.Put("replace", "old");
        cache.Put("replace", "new");
        cache.Clear();
        cache.Put("remove", "value");
        cache.Remove("remove");
        cache.PutEntry("expired", "value", CacheEntryOptions.TimeToLive(1L));
        clock.AdvanceTicks(1L);
        cache.Get("expired");
        cache.Put("capacity-a", "one");
        cache.Put("capacity-b", "two");
        cache.Put("capacity-c", "three");

        Assert.That(firstPolicy.Name, Is.EqualTo("mutated-snapshot-name"));
        Assert.That(secondPolicy.Name, Is.EqualTo("original-name"));
        Assert.That(cache.Policy().Name, Is.EqualTo("original-name"));
        Assert.That(notifications, Has.Count.EqualTo(5));
        foreach (CacheNotification notification in notifications)
        {
            Assert.That(notification.Name, Is.EqualTo("original-name"));
        }

        Assert.That(HasReason(notifications, CacheRemovalReason.Replace), Is.True);
        Assert.That(HasReason(notifications, CacheRemovalReason.ExplicitRemove), Is.True);
        Assert.That(HasReason(notifications, CacheRemovalReason.Expired), Is.True);
        Assert.That(HasReason(notifications, CacheRemovalReason.Capacity), Is.True);
        Assert.That(HasReason(notifications, CacheRemovalReason.Clear), Is.True);
    }

    static bool HasReason(List<CacheNotification> notifications, CacheRemovalReason reason)
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
}
