using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheLruEvictionTests
{
    [Test]
    public void Put_WhenCapacityExceeded_EvictsLeastRecentlyUsedEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WhenCapacityExceeded_DoesNotTreatPeekAsRecentAccess()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Peek("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WhenLastAccessTimesTie_EvictsOlderVersion()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WhenEvictingLruEntry_RecordsCapacityReasonOnEvictedEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        object firstEntry = Entry(cache, "a");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Get("b");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");

        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(EntryField(firstEntry, "RemovalReason"), Is.EqualTo(CacheRemovalReason.Capacity));
    }

    [Test]
    public void Put_WhenCapacityIsThree_EvictsLeastRecentlyUsedEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("c", "three");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        cache.Get("c");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("d", "four");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(Entries(cache).Contains("d"), Is.True);
    }

    [Test]
    public void Put_ReplacementRefreshesRecencyForMaximumEntryEviction()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Put("a", "updated");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(cache.Get("a").Value, Is.EqualTo("updated"));
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
