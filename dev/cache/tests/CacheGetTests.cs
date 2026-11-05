using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheGetTests
{
    [Test]
    public void Get_MissingKeyReturnsMiss()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheGetResult result = cache.Get("missing");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.EqualTo("missing"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
    }

    [Test]
    public void Get_NullKeyReturnsMissWithoutMutatingCache()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheGetResult result = cache.Get(null!);

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Get_LiveEntryReturnsHitAndUpdatesLastAccess()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        clock.AdvanceTicks(25L);
        CacheGetResult result = cache.Get("a");
        object entry = Entry(cache, "a");

        Assert.That(result.Found, Is.True);
        Assert.That(result.Key, Is.EqualTo("a"));
        Assert.That(result.Value, Is.EqualTo("one"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Loaded, Is.False);
        Assert.That(result.HasExpiration, Is.False);
        Assert.That(result.CreatedUtcTicks, Is.EqualTo(100L));
        Assert.That(result.LastAccessUtcTicks, Is.EqualTo(125L));
        Assert.That(result.LastWriteUtcTicks, Is.EqualTo(100L));
        Assert.That(result.ExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(EntryField(entry, "LastAccessUtcTicks"), Is.EqualTo(125L));
        Assert.That(EntryField(entry, "LastWriteUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "AccessCount"), Is.EqualTo(1L));
    }

    [Test]
    public void Get_LiveSlidingEntryReturnsRefreshedExpirationTime()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(5L);
        CacheGetResult result = cache.Get("a");

        Assert.That(result.Found, Is.True);
        Assert.That(result.HasExpiration, Is.True);
        Assert.That(result.LastAccessUtcTicks, Is.EqualTo(105L));
        Assert.That(result.ExpirationUtcTicks, Is.EqualTo(115L));
    }

    [Test]
    public void Get_ExpiredAbsoluteEntryRemovesEntryAndDoesNotReturnValue()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.TimeToLive(10L);
        entryOptions.Weight = 3L;

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(10L);
        CacheGetResult result = cache.Get("a");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.EqualTo("a"));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(0L));
    }

    [Test]
    public void Get_SlidingExpirationUsesPreviousLastAccessBeforeRefresh()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(10L);

        cache.PutEntry("a", "one", entryOptions);
        clock.AdvanceTicks(9L);
        CacheGetResult first = cache.Get("a");
        clock.AdvanceTicks(9L);
        CacheGetResult second = cache.Get("a");
        clock.AdvanceTicks(10L);
        CacheGetResult third = cache.Get("a");

        Assert.That(first.Found, Is.True);
        Assert.That(first.LastAccessUtcTicks, Is.EqualTo(109L));
        Assert.That(second.Found, Is.True);
        Assert.That(second.LastAccessUtcTicks, Is.EqualTo(118L));
        Assert.That(third.Found, Is.False);
        Assert.That(third.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Get_WhenAccessCountReachesLongLimit_SaturatesCount()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("a", "one");
        cache._entries["a"].AccessCount = long.MaxValue;

        CacheGetResult result = cache.Get("a");

        Assert.That(result.Found, Is.True);
        Assert.That(cache._entries["a"].AccessCount, Is.EqualTo(long.MaxValue));
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
