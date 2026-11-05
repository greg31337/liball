using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheSnapshotTests
{
    [Test]
    public void Snapshot_ReturnsVisibleKeysValuesCountWeightAndTime()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 3L;
        cache.PutEntry("a", "one", firstOptions);
        cache.PutEntry("b", "two", secondOptions);

        CacheSnapshot snapshot = cache.Snapshot();

        Assert.That(snapshot.Count, Is.EqualTo(2L));
        Assert.That(snapshot.Weight, Is.EqualTo(5L));
        Assert.That(snapshot.SnapshotUtcTicks, Is.EqualTo(100L));
        Assert.That(snapshot.Keys, Has.Length.EqualTo(2));
        Assert.That(snapshot.Values, Has.Length.EqualTo(2));
        Assert.That(ArrayContains(snapshot.Keys, "a"), Is.True);
        Assert.That(ArrayContains(snapshot.Keys, "b"), Is.True);
        Assert.That(ArrayContains(snapshot.Values, "one"), Is.True);
        Assert.That(ArrayContains(snapshot.Values, "two"), Is.True);
    }

    [Test]
    public void Snapshot_ExcludesExpiredEntriesAndCleansThem()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        cache.Put("live", "two");

        clock.AdvanceTicks(5L);
        CacheSnapshot snapshot = cache.Snapshot();
        CacheGetResult expired = cache.Get("expired");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(snapshot.Count, Is.EqualTo(1L));
        Assert.That(snapshot.Weight, Is.EqualTo(1L));
        Assert.That(ArrayContains(snapshot.Keys, "expired"), Is.False);
        Assert.That(ArrayContains(snapshot.Keys, "live"), Is.True);
        Assert.That(expired.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Key, Is.EqualTo("expired"));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void Snapshot_ReturnsPointInTimeArrays()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("a", "one");

        CacheSnapshot before = cache.Snapshot();
        cache.Put("b", "two");
        cache.Remove("a");
        CacheSnapshot after = cache.Snapshot();

        Assert.That(before.Count, Is.EqualTo(1L));
        Assert.That(before.Keys, Has.Length.EqualTo(1));
        Assert.That(before.Values, Has.Length.EqualTo(1));
        Assert.That(before.Keys[0], Is.EqualTo("a"));
        Assert.That(before.Values[0], Is.EqualTo("one"));
        Assert.That(after.Count, Is.EqualTo(1L));
        Assert.That(after.Keys[0], Is.EqualTo("b"));
        Assert.That(after.Values[0], Is.EqualTo("two"));
    }

    static bool ArrayContains(object?[] values, object? expected)
    {
        foreach (object? value in values)
        {
            if (object.Equals(value, expected))
            {
                return true;
            }
        }

        return false;
    }
}
