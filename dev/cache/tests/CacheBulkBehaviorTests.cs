using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheBulkBehaviorTests
{
    [Test]
    public void PutMany_ProcessesDuplicatesAndFailuresAndEnforcesCapacityPerWrite()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions rejectNull = CacheEntryOptions.NoExpiration();
        rejectNull.CacheNullValue = false;
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("a", "one", null),
            null,
            Request(null, "no-key", null),
            Request("no-null", null, rejectNull),
            Request("a", "two", null),
            Request("b", "three", null),
            Request("c", "four", null)
        };

        CacheBulkWriteResult result = cache.PutMany(requests);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Results[0].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(result.Results[1].Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Results[2].Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Results[3].Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Results[4].Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(result.Results[4].PreviousValue, Is.EqualTo("one"));
        Assert.That(result.Results[5].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(result.Results[6].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(result.Results[6].EvictedCount, Is.EqualTo(1L));
        Assert.That(cache.Contains("a"), Is.False);
        Assert.That(cache.Contains("b"), Is.True);
        Assert.That(cache.Contains("c"), Is.True);
        Assert.That(stats.Puts, Is.EqualTo(3L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(notifications[0].Value, Is.EqualTo("one"));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void GetMany_TracksDuplicateAccessesAndExpiresAnEntryOnlyOnce()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("live", "one");
        cache.PutEntry("expired", "two", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        object?[] keys = new object?[] { "live", "live", "expired", "expired", null, "missing" };

        CacheBulkGetResult result = cache.GetMany(keys);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Results[0].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Results[1].Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Results[2].Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(result.Results[3].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[4].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[5].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[0].LastAccessUtcTicks, Is.EqualTo(105L));
        Assert.That(result.Results[1].LastAccessUtcTicks, Is.EqualTo(105L));
        Assert.That(cache._entries["live"].AccessCount, Is.EqualTo(3L));
        Assert.That(stats.Hits, Is.EqualTo(2L));
        Assert.That(stats.Misses, Is.EqualTo(4L));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void RemoveMany_RemovesFirstDuplicateAndReportsLaterKeysAsMisses()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        cache.Put("b", "two");
        object?[] keys = new object?[] { "a", "a", null, "missing", "b" };

        CacheBulkRemoveResult result = cache.RemoveMany(keys);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Results[0].Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(result.Results[0].Value, Is.EqualTo("one"));
        Assert.That(result.Results[1].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[2].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[3].Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Results[4].Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(result.Results[4].Value, Is.EqualTo("two"));
        Assert.That(stats.Removes, Is.EqualTo(2L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
    }

    [Test]
    public void PutMany_DeliversNotificationsAfterEveryRequestHasMutatedState()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        Cache? observedCache = null;
        bool listenerSawLaterWrite = false;
        CacheOptions options = CacheOptions.Default();
        options.RemovalListener = notification =>
        {
            notifications.Add(notification);
            if (observedCache != null)
            {
                listenerSawLaterWrite = observedCache.Contains("later");
            }
        };
        Cache cache = new Cache(options);
        observedCache = cache;
        cache.Put("existing", "old");
        CachePutRequest?[] requests = new CachePutRequest?[]
        {
            Request("existing", "new", null),
            Request("later", "value", null)
        };

        CacheBulkWriteResult result = cache.PutMany(requests);

        Assert.That(result.Results[0].Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(result.Results[1].Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(listenerSawLaterWrite, Is.True);
    }

    static CachePutRequest Request(object? key, object? value, CacheEntryOptions? options)
    {
        CachePutRequest request = new CachePutRequest();
        request.Key = key;
        request.Value = value;
        request.Options = options;
        return request;
    }
}
