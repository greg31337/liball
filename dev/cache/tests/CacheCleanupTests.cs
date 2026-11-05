using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheCleanupTests
{
    [Test]
    public void Cleanup_EmptyCacheReturnsZeroExpiredCounts()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheMaintenanceResult result = cache.Cleanup();

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(result.RemovedCount, Is.EqualTo(0L));
        Assert.That(result.ExpiredCount, Is.EqualTo(0L));
        Assert.That(result.EvictedCount, Is.EqualTo(0L));
        Assert.That(result.ClearedCount, Is.EqualTo(0L));
        Assert.That(result.CurrentCount, Is.EqualTo(0L));
        Assert.That(result.CurrentWeight, Is.EqualTo(0L));
    }

    [Test]
    public void Cleanup_RemovesExpiredEntriesAndLeavesLiveEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions expiredOptions = CacheEntryOptions.TimeToLive(10L);
        CacheEntryOptions liveOptions = CacheEntryOptions.TimeToLive(30L);
        CacheEntryOptions noExpirationOptions = CacheEntryOptions.NoExpiration();
        expiredOptions.Weight = 2L;
        liveOptions.Weight = 3L;
        noExpirationOptions.Weight = 5L;
        cache.PutEntry("expired", "one", expiredOptions);
        cache.PutEntry("live", "two", liveOptions);
        cache.PutEntry("forever", "three", noExpirationOptions);

        clock.AdvanceTicks(10L);
        CacheMaintenanceResult result = cache.Cleanup();

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(result.RemovedCount, Is.EqualTo(1L));
        Assert.That(result.ExpiredCount, Is.EqualTo(1L));
        Assert.That(result.CurrentCount, Is.EqualTo(2L));
        Assert.That(result.CurrentWeight, Is.EqualTo(8L));
        Assert.That(Entries(cache).Contains("expired"), Is.False);
        Assert.That(Entries(cache).Contains("live"), Is.True);
        Assert.That(Entries(cache).Contains("forever"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(8L));
    }

    [Test]
    public void Cleanup_RemovesSlidingEntriesAtPreviousAccessBoundary()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(9L);
        CacheMaintenanceResult beforeBoundary = cache.Cleanup();
        clock.AdvanceTicks(1L);
        CacheMaintenanceResult atBoundary = cache.Cleanup();

        Assert.That(beforeBoundary.RemovedCount, Is.EqualTo(0L));
        Assert.That(beforeBoundary.CurrentCount, Is.EqualTo(1L));
        Assert.That(atBoundary.RemovedCount, Is.EqualTo(1L));
        Assert.That(atBoundary.ExpiredCount, Is.EqualTo(1L));
        Assert.That(atBoundary.CurrentCount, Is.EqualTo(0L));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Cleanup_DoesNotUpdateLiveEntryAccessMetadata()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(100L);
        cache.PutEntry("a", "one", entryOptions);

        clock.AdvanceTicks(10L);
        CacheMaintenanceResult result = cache.Cleanup();
        object entry = Entry(cache, "a");

        Assert.That(result.RemovedCount, Is.EqualTo(0L));
        Assert.That(EntryField(entry, "LastAccessUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "AccessCount"), Is.EqualTo(0L));
    }

    static IDictionary Entries(Cache cache)
    {
        object? value = InternalField(cache, "_entries");
        Assert.That(value, Is.Not.Null);
        return (IDictionary)value!;
    }

    static object Entry(Cache cache, object key)
    {
        object? value = Entries(cache)[key];
        Assert.That(value, Is.Not.Null);
        return value!;
    }

    static object? InternalField(Cache cache, string name)
    {
        FieldInfo? field = typeof(Cache).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field!.GetValue(cache);
    }

    static object? EntryField(object entry, string name)
    {
        FieldInfo? field = entry.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field!.GetValue(entry);
    }
}
