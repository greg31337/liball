using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheRandomEvictionTests
{
    [Test]
    public void Put_WithRandomPolicy_EnforcesMaximumEntryCount()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        CacheWriteResult third = cache.Put("c", "three");

        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(2));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void Put_WithRandomPolicy_EnforcesMaximumWeight()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        CacheWriteResult third = cache.Put("c", "three");

        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(2));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void Put_WithRandomPolicy_EvictsLowerPriorityBeforeRandomHighPriorityCandidate()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
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
    public void Policy_ReturnsRandomEvictionPolicy()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
        Cache cache = new Cache(options);

        CachePolicySnapshot policy = cache.Policy();

        Assert.That(policy.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Random));
    }

    [Test]
    public void Put_WithRandomPolicy_AfterManyWrites_NeverExceedsMaximumEntries()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        Cache cache = new Cache(options);

        int index = 0;
        while (index < 20)
        {
            cache.Put("key-" + index.ToString(), "value");
            Assert.That(Entries(cache).Count, Is.LessThanOrEqualTo(3));
            index = index + 1;
        }

        Assert.That(cache.Snapshot().Count, Is.EqualTo(3L));
    }

    [Test]
    public void Put_WithRandomPolicy_AfterManyWrites_RecordsCapacityEvictionInvariants()
    {
        CacheOptions options = CacheOptions.Default();
        options.EvictionPolicy = CacheEvictionPolicy.Random;
        options.RecordStats = true;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 3L;
        Cache cache = new Cache(options);

        int index = 0;
        while (index < 10)
        {
            cache.Put("key-" + index.ToString(), "value");
            index = index + 1;
        }

        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stats.Puts, Is.EqualTo(10L));
        Assert.That(stats.Evictions, Is.EqualTo(7L));
        Assert.That(stats.CurrentSize, Is.EqualTo(3L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(3L));
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
