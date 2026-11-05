using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheMaximumWeightTests
{
    [Test]
    public void Put_DefaultEntryWeightIsOne()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheWriteResult first = cache.Put("a", "one");
        CacheWriteResult second = cache.Put("b", "two");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(first.Weight, Is.EqualTo(1L));
        Assert.That(second.Weight, Is.EqualTo(1L));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
        Assert.That(stats.CurrentSize, Is.EqualTo(2L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(2L));
    }

    [Test]
    public void Put_WhenMaximumWeightUsesDefaultWeights_EvictsLeastRecentlyUsedEntry()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(1L);
        cache.Put("b", "two");
        clock.AdvanceTicks(1L);
        cache.Get("a");
        clock.AdvanceTicks(1L);
        CacheWriteResult third = cache.Put("c", "three");

        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(2));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void PutEntry_WhenEntryWeightExceedsMaximumWeight_RemovesWrittenEntry()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 3L;

        CacheWriteResult result = cache.PutEntry("a", "one", entryOptions);

        Assert.That(result.Written, Is.True);
        Assert.That(result.Weight, Is.EqualTo(3L));
        Assert.That(result.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(0L));
    }

    [Test]
    public void PutEntry_WhenReplacingWithLargerWeight_EnforcesMaximumWeight()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = 3L;
        Cache cache = new Cache(options);
        CacheEntryOptions light = CacheEntryOptions.NoExpiration();
        CacheEntryOptions heavy = CacheEntryOptions.NoExpiration();
        light.Weight = 1L;
        heavy.Weight = 3L;

        cache.PutEntry("a", "one", light);
        cache.PutEntry("b", "two", light);
        CacheWriteResult replacement = cache.PutEntry("a", "updated", heavy);

        Assert.That(replacement.Written, Is.True);
        Assert.That(replacement.Replaced, Is.True);
        Assert.That(replacement.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.True);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(3L));
    }

    [Test]
    public void PutEntry_WhenMaximumWeightRequiresMultipleEvictions_RemovesLruEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 5L;
        Cache cache = new Cache(options);
        CacheEntryOptions medium = CacheEntryOptions.NoExpiration();
        CacheEntryOptions heavy = CacheEntryOptions.NoExpiration();
        medium.Weight = 2L;
        heavy.Weight = 4L;

        cache.PutEntry("a", "one", medium);
        clock.AdvanceTicks(1L);
        cache.PutEntry("b", "two", medium);
        clock.AdvanceTicks(1L);
        CacheWriteResult third = cache.PutEntry("c", "three", heavy);

        Assert.That(third.EvictedCount, Is.EqualTo(2L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.False);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(4L));
    }

    [Test]
    public void PutEntry_MaximumWeightEvictionUpdatesStatsAndNotifications()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 3L;
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 2L;

        cache.PutEntry("a", "one", firstOptions);
        CacheWriteResult second = cache.PutEntry("b", "two", secondOptions);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(second.EvictedCount, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(2L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[0].Weight, Is.EqualTo(2L));
    }

    [Test]
    public void PutEntry_WhenTotalWeightExceedsLongRange_StillEnforcesMaximumWeight()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = long.MaxValue;
        Cache cache = new Cache(options);
        CacheEntryOptions maximum = CacheEntryOptions.NoExpiration();
        maximum.Weight = long.MaxValue;

        cache.PutEntry("maximum", "one", maximum);
        CacheWriteResult result = cache.Put("extra", "two");

        Assert.That(result.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("maximum"), Is.False);
        Assert.That(Entries(cache).Contains("extra"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(1L));
    }

    [Test]
    public void Remove_WhenTotalWeightIsSaturated_RecalculatesRemainingWeight()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions maximum = CacheEntryOptions.NoExpiration();
        maximum.Weight = long.MaxValue;

        cache.PutEntry("maximum", "one", maximum);
        cache.Put("extra", "two");
        cache.Remove("maximum");

        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(1L));
        Assert.That(cache.Stats().CurrentWeight, Is.EqualTo(1L));
    }

    [Test]
    public void PutEntry_WhenReplacingAtSaturatedWeight_RecalculatesTotal()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions maximum = CacheEntryOptions.NoExpiration();
        CacheEntryOptions replacement = CacheEntryOptions.NoExpiration();
        maximum.Weight = long.MaxValue;
        replacement.Weight = 2L;

        cache.PutEntry("a", "one", maximum);
        cache.PutEntry("a", "two", replacement);

        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
        Assert.That(cache.Stats().CurrentWeight, Is.EqualTo(2L));
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
