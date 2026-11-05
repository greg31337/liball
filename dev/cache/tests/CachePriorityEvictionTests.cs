using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePriorityEvictionTests
{
    [Test]
    public void Put_WhenMaximumEntriesExceeded_EvictsLowPriorityBeforeOlderHighPriority()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
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
        cache.Get("low");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.PutEntry("normal", "three", CacheEntryOptions.NoExpiration());

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("high"), Is.True);
        Assert.That(Entries(cache).Contains("low"), Is.False);
        Assert.That(Entries(cache).Contains("normal"), Is.True);
    }

    [Test]
    public void Put_WhenPrioritiesMatch_EvictsLeastRecentlyUsedEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
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
        cache.Put("c", "three");

        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
    }

    [Test]
    public void PutEntry_WhenMaximumWeightExceeded_EvictsLowPriorityBeforeLruNormalPriority()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions normal = CacheEntryOptions.NoExpiration();
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        normal.Priority = CacheEntryPriority.Normal;
        low.Priority = CacheEntryPriority.Low;

        cache.PutEntry("normal", "one", normal);
        clock.AdvanceTicks(1L);
        cache.PutEntry("low", "two", low);
        clock.AdvanceTicks(1L);
        cache.Get("low");
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.PutEntry("new", "three", normal);

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("normal"), Is.True);
        Assert.That(Entries(cache).Contains("low"), Is.False);
        Assert.That(Entries(cache).Contains("new"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void PutEntry_LowPriorityWrittenEntryCanBeEvictedBeforeHighPriorityEntries()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        CacheEntryOptions low = CacheEntryOptions.NoExpiration();
        high.Priority = CacheEntryPriority.High;
        low.Priority = CacheEntryPriority.Low;

        cache.PutEntry("high-a", "one", high);
        cache.PutEntry("high-b", "two", high);
        CacheWriteResult write = cache.PutEntry("low", "three", low);

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("high-a"), Is.True);
        Assert.That(Entries(cache).Contains("high-b"), Is.True);
        Assert.That(Entries(cache).Contains("low"), Is.False);
    }

    [Test]
    public void Put_CacheDefaultPriorityAndEntryPriorityBothInfluenceEviction()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(400L);
        options.Clock = clock;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        options.DefaultPriority = CacheEntryPriority.Low;
        Cache cache = new Cache(options);
        CacheEntryOptions high = CacheEntryOptions.NoExpiration();
        high.Priority = CacheEntryPriority.High;

        cache.Put("default-low-a", "one");
        clock.AdvanceTicks(1L);
        cache.PutEntry("explicit-high", "two", high);
        clock.AdvanceTicks(1L);
        CacheWriteResult write = cache.Put("default-low-b", "three");

        Assert.That(write.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("default-low-a"), Is.False);
        Assert.That(Entries(cache).Contains("explicit-high"), Is.True);
        Assert.That(Entries(cache).Contains("default-low-b"), Is.True);
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
