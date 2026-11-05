using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheFifoEvictionTests
{
    [Test]
    public void Put_WithFifoPolicy_EvictsOldestCreatedEntryEvenWhenRecentlyAccessed()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
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
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void Put_WithFifoPolicy_StillEvictsLowerPriorityBeforeOlderHighPriority()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        high.Priority = CacheEntryPriority.High;
        low.Priority = CacheEntryPriority.Low;

        cache.PutEntry("high", "one", high);
        clock.AdvanceTicks(1L);
        cache.PutEntry("low", "two", low);
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("normal", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("high"), Is.True);
        Assert.That(Entries(cache).Contains("low"), Is.False);
        Assert.That(Entries(cache).Contains("normal"), Is.True);
    }

    [Test]
    public void PutEntry_WithFifoPolicy_UsesCreatedTimeForMaximumWeightEviction()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("c", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void Put_WithFifoPolicy_WhenCreatedTimesTie_EvictsOlderVersion()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
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
    public void Put_WithFifoPolicy_ReplacementResetsCreationOrder()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(500L);
        options.Clock = clock;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
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

    static object? InternalField(Cache cache, string name)
    {
        FieldInfo? field = typeof(Cache).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field!.GetValue(cache);
    }
}
