using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePeekTests
{
    [Test]
    public void Peek_MissingKeyReturnsMiss()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheGetResult result = cache.Peek("missing");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.EqualTo("missing"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void Peek_NullKeyReturnsMissWithoutMutatingCache()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheGetResult result = cache.Peek(null!);

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Peek_LiveEntryReturnsHitWithoutUpdatingAccessMetadata()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(25L);
        CacheGetResult result = cache.Peek("a");
        object entry = Entry(cache, "a");

        Assert.That(result.Found, Is.True);
        Assert.That(result.Key, Is.EqualTo("a"));
        Assert.That(result.Value, Is.EqualTo("one"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Loaded, Is.False);
        Assert.That(result.CreatedUtcTicks, Is.EqualTo(100L));
        Assert.That(result.LastAccessUtcTicks, Is.EqualTo(100L));
        Assert.That(result.LastWriteUtcTicks, Is.EqualTo(100L));
        Assert.That(EntryField(entry, "LastAccessUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "AccessCount"), Is.EqualTo(0L));
    }

    [Test]
    public void Peek_SlidingEntryDoesNotRefreshExpiration()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(5L);
        CacheGetResult first = cache.Peek("a");
        object entry = Entry(cache, "a");
        clock.AdvanceTicks(5L);
        CacheGetResult second = cache.Peek("a");

        Assert.That(first.Found, Is.True);
        Assert.That(first.LastAccessUtcTicks, Is.EqualTo(100L));
        Assert.That(first.ExpirationUtcTicks, Is.EqualTo(110L));
        Assert.That(EntryField(entry, "LastAccessUtcTicks"), Is.EqualTo(100L));
        Assert.That(second.Found, Is.False);
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(second.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
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
