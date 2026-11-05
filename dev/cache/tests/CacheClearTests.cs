using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheClearTests
{
    [Test]
    public void Clear_EmptyCacheReturnsClearResultWithZeroCounts()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheMaintenanceResult result = cache.Clear();

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Cleared));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Clear));
        Assert.That(result.RemovedCount, Is.EqualTo(0L));
        Assert.That(result.ClearedCount, Is.EqualTo(0L));
        Assert.That(result.ExpiredCount, Is.EqualTo(0L));
        Assert.That(result.EvictedCount, Is.EqualTo(0L));
        Assert.That(result.CurrentCount, Is.EqualTo(0L));
        Assert.That(result.CurrentWeight, Is.EqualTo(0L));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Clear_RemovesAllEntriesAndResetsWeight()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 5L;
        cache.PutEntry("a", "one", firstOptions);
        cache.PutEntry("b", "two", secondOptions);

        CacheMaintenanceResult result = cache.Clear();

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Cleared));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Clear));
        Assert.That(result.RemovedCount, Is.EqualTo(2L));
        Assert.That(result.ClearedCount, Is.EqualTo(2L));
        Assert.That(result.CurrentCount, Is.EqualTo(0L));
        Assert.That(result.CurrentWeight, Is.EqualTo(0L));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(0L));
    }

    [Test]
    public void Clear_AllowsCacheToBeUsedAgain()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("a", "one");
        cache.Clear();

        CacheWriteResult write = cache.Put("b", "two");
        CacheGetResult read = cache.Get("b");

        Assert.That(write.Written, Is.True);
        Assert.That(read.Found, Is.True);
        Assert.That(read.Value, Is.EqualTo("two"));
        Assert.That(Entries(cache).Count, Is.EqualTo(1));
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
