using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheRemoveTests
{
    [Test]
    public void Remove_MissingKeyReturnsMiss()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheRemoveResult result = cache.Remove("missing");

        Assert.That(result.Removed, Is.False);
        Assert.That(result.Key, Is.EqualTo("missing"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Weight, Is.EqualTo(0L));
    }

    [Test]
    public void Remove_NullKeyReturnsMissWithoutMutatingCache()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheRemoveResult result = cache.Remove(null!);

        Assert.That(result.Removed, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Remove_ExistingEntryRemovesAndReturnsExplicitReason()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 4L;
        cache.PutEntry("a", "one", entryOptions);

        CacheRemoveResult result = cache.Remove("a");

        Assert.That(result.Removed, Is.True);
        Assert.That(result.Key, Is.EqualTo("a"));
        Assert.That(result.Value, Is.EqualTo("one"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Removed));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
        Assert.That(result.Weight, Is.EqualTo(4L));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(0L));
    }

    [Test]
    public void Remove_OnlyRemovesRequestedEntry()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 5L;
        cache.PutEntry("a", "one", firstOptions);
        cache.PutEntry("b", "two", secondOptions);

        CacheRemoveResult result = cache.Remove("a");

        Assert.That(result.Removed, Is.True);
        Assert.That(Entries(cache).Count, Is.EqualTo(1));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(5L));
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
