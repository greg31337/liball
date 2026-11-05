using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheCustomWeigherTests
{
    [Test]
    public void Put_UsesCustomWeigherForEntryWeightAndMaximumWeight()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasMaximumWeight = true;
        options.MaximumWeight = 5L;
        options.Weigher = (key, value) => value!.ToString()!.Length;
        Cache cache = new Cache(options);

        CacheWriteResult first = cache.Put("a", "one");
        CacheWriteResult second = cache.Put("b", "xx");
        CacheWriteResult third = cache.Put("c", "z");

        Assert.That(first.Weight, Is.EqualTo(3L));
        Assert.That(second.Weight, Is.EqualTo(2L));
        Assert.That(third.Weight, Is.EqualTo(1L));
        Assert.That(third.EvictedCount, Is.EqualTo(1L));
        Assert.That(Entries(cache).Contains("a"), Is.False);
        Assert.That(Entries(cache).Contains("b"), Is.True);
        Assert.That(Entries(cache).Contains("c"), Is.True);
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(3L));
    }

    [Test]
    public void Put_CustomWeigherNormalizesNonPositiveWeightToOne()
    {
        CacheOptions options = CacheOptions.Default();
        options.Weigher = (key, value) => 0L;
        Cache cache = new Cache(options);

        CacheWriteResult result = cache.Put("a", "one");

        Assert.That(result.Weight, Is.EqualTo(1L));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(1L));
    }

    [Test]
    public void PutEntry_CustomWeigherOverridesEntryOptionWeight()
    {
        CacheOptions options = CacheOptions.Default();
        options.Weigher = (key, value) => 4L;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = 10L;

        CacheWriteResult result = cache.PutEntry("a", "one", entryOptions);

        Assert.That(result.Weight, Is.EqualTo(4L));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(4L));
    }

    [Test]
    public void Put_WhenCustomWeigherThrows_ExceptionPropagatesAndEntryIsNotAdded()
    {
        CacheOptions options = CacheOptions.Default();
        options.Weigher = (key, value) => throw new InvalidOperationException("weigher failed");
        Cache cache = new Cache(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.Put("a", "one");
            })!;

        Assert.That(exception.Message, Is.EqualTo("weigher failed"));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(0L));
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
