using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheLfuEvictionTests
{
    [Test]
    public void Put_WithLfuPolicy_EvictsLowerFrequencyEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WithLfuPolicy_CanEvictNewEntryWhenExistingEntriesAreHotter()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        cache.Get("a");
        cache.Get("b");
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.False);
    }

    [Test]
    public void Peek_WithLfuPolicy_DoesNotIncreaseFrequency()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        cache.Peek("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WithLfuPolicy_InitializesNewEntriesWithOneFrequencyCount()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        object entry = Entry(cache, "a");

        Assert.That(EntryField(entry, "AccessCount"), Is.EqualTo(1L));
    }

    [Test]
    public void Put_WithLfuPolicy_UsesFrequencyForMaximumWeightEviction()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        cache.Get("a");
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WithLfuPolicy_EvictsLowerPriorityBeforeLowerFrequencyHighPriority()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Lfu;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        high.Priority = CacheEntryPriority.High;
        low.Priority = CacheEntryPriority.Low;

        cache.PutEntry("high", "one", high);
        cache.PutEntry("low", "two", low);
        cache.Get("low");
        cache.Get("low");
        CacheWriteResult write = cache.PutEntry("normal", "three", CacheEntryOptions.NoExpiration());

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("high"), Is.True);
        Assert.That(Entries(cache).Contains("low"), Is.False);
        Assert.That(Entries(cache).Contains("normal"), Is.True);
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
