using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheMaximumEntriesTests
{
    [Test]
    public void Put_WhenMaximumEntriesIsNotConfigured_DoesNotEvict()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheWriteResult first = cache.Put("a", "one");
        CacheWriteResult second = cache.Put("b", "two");
        CacheWriteResult third = cache.Put("c", "three");

        Assert.That(first.EvictedCount, Is.EqualTo(0L));
        Assert.That(second.EvictedCount, Is.EqualTo(0L));
        Assert.That(third.EvictedCount, Is.EqualTo(0L));
        Assert.That(Entries(cache).Count, Is.EqualTo(3));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(3L));
    }

    [Test]
    public void Put_EnforcesMaximumEntryCountAndKeepsWrittenEntry()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Put("b", "two");
        CacheWriteResult third = cache.Put("c", "three");

        Assert.That(third.Written, Is.True);
        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(2));
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(2L));
    }

    [Test]
    public void PutEntry_UpdatesCurrentWeightAfterCapacityEviction()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 2L;
        Cache cache = new Cache(options);
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions thirdOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 3L;
        thirdOptions.Weight = 5L;

        cache.PutEntry("a", "one", firstOptions);
        cache.PutEntry("b", "two", secondOptions);
        CacheWriteResult third = cache.PutEntry("c", "three", thirdOptions);

        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(2));
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(8L));
    }

    [Test]
    public void Put_ReplacementDoesNotEvictWhenCountIsWithinLimit()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        CacheWriteResult replacement = cache.Put("a", "two");
        CacheGetResult read = cache.Get("a");

        Assert.That(replacement.Written, Is.True);
        Assert.That(replacement.Replaced, Is.True);
        Assert.That(replacement.EvictedCount, Is.EqualTo(0L));
        Assert.That(Entries(cache).Count, Is.EqualTo(1));
        Assert.That(read.Found, Is.True);
        Assert.That(read.Value, Is.EqualTo("two"));
    }

    [Test]
    public void Put_EvictedEntryRecordsCapacityRemovalReason()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1L;
        Cache cache = new Cache(options);
        cache.Put("a", "one");
        object firstEntry = Entry(cache, "a");

        CacheWriteResult second = cache.Put("b", "two");

        Assert.That(second.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(EntryField(firstEntry, "RemovalReason"), Is.EqualTo(CacheRemovalReason.Capacity));
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
