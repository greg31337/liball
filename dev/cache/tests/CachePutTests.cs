using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePutTests
{
    [Test]
    public void Put_AddsEntryAndReturnsWrittenResult()
    {
        CacheOptions options = CacheOptions.Default();
        options.Clock = CacheClock.Manual(100L);
        Cache cache = new Cache(options);

        CacheWriteResult result = cache.Put("a", "one");
        object entry = Entry(cache, "a");

        Assert.That(result.Written, Is.True);
        Assert.That(result.Replaced, Is.False);
        Assert.That(result.Key, Is.EqualTo("a"));
        Assert.That(result.Value, Is.EqualTo("one"));
        Assert.That(result.PreviousValue, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Written));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Unknown));
        Assert.That(result.Weight, Is.EqualTo(1L));
        Assert.That(Entries(cache).Count, Is.EqualTo(1));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(1L));
        Assert.That(InternalField(cache, "_version"), Is.EqualTo(1L));
        Assert.That(EntryField(entry, "Key"), Is.EqualTo("a"));
        Assert.That(EntryField(entry, "Value"), Is.EqualTo("one"));
        Assert.That(EntryField(entry, "CreatedUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "LastAccessUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "LastWriteUtcTicks"), Is.EqualTo(100L));
        Assert.That(EntryField(entry, "Weight"), Is.EqualTo(1L));
        Assert.That(EntryField(entry, "Priority"), Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(EntryField(entry, "Version"), Is.EqualTo(1L));
    }

    [Test]
    public void Put_ReplacesExistingEntryAndUpdatesWeight()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(10L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        CacheEntryOptions firstOptions = CacheEntryOptions.NoExpiration();
        CacheEntryOptions secondOptions = CacheEntryOptions.NoExpiration();
        firstOptions.Weight = 2L;
        secondOptions.Weight = 5L;

        CacheWriteResult first = cache.PutEntry("a", "one", firstOptions);
        clock.AdvanceTicks(10L);
        CacheWriteResult second = cache.PutEntry("a", "two", secondOptions);
        object entry = Entry(cache, "a");

        Assert.That(first.Written, Is.True);
        Assert.That(second.Written, Is.True);
        Assert.That(second.Replaced, Is.True);
        Assert.That(second.PreviousValue, Is.EqualTo("one"));
        Assert.That(second.PreviousWeight, Is.EqualTo(2L));
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Replaced));
        Assert.That(second.Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(second.Weight, Is.EqualTo(5L));
        Assert.That(Entries(cache).Count, Is.EqualTo(1));
        Assert.That(InternalField(cache, "_currentWeight"), Is.EqualTo(5L));
        Assert.That(InternalField(cache, "_version"), Is.EqualTo(2L));
        Assert.That(EntryField(entry, "Value"), Is.EqualTo("two"));
        Assert.That(EntryField(entry, "CreatedUtcTicks"), Is.EqualTo(20L));
        Assert.That(EntryField(entry, "Version"), Is.EqualTo(2L));
    }

    [Test]
    public void Put_UsesCacheDefaultEntryOptions()
    {
        CacheOptions options = CacheOptions.Default();
        options.Clock = CacheClock.Manual(1000L);
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 100L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 30L;
        options.DefaultPriority = CacheEntryPriority.High;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        object entry = Entry(cache, "a");

        Assert.That(EntryField(entry, "HasAbsoluteExpiration"), Is.EqualTo(true));
        Assert.That(EntryField(entry, "AbsoluteExpirationUtcTicks"), Is.EqualTo(1100L));
        Assert.That(EntryField(entry, "HasSlidingExpiration"), Is.EqualTo(true));
        Assert.That(EntryField(entry, "SlidingExpirationTicks"), Is.EqualTo(30L));
        Assert.That(EntryField(entry, "Priority"), Is.EqualTo(CacheEntryPriority.High));
    }

    [Test]
    public void PutEntry_UsesExplicitEntryOptionsInsteadOfCacheDefaults()
    {
        CacheOptions options = CacheOptions.Default();
        options.Clock = CacheClock.Manual(1000L);
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 100L;
        options.DefaultPriority = CacheEntryPriority.Low;
        CacheEntryOptions entryOptions = CacheEntryOptions.SlidingExpiration(50L);
        entryOptions.Weight = 3L;
        entryOptions.Priority = CacheEntryPriority.High;
        Cache cache = new Cache(options);

        CacheWriteResult result = cache.PutEntry("a", "one", entryOptions);
        object entry = Entry(cache, "a");

        Assert.That(result.Written, Is.True);
        Assert.That(result.Weight, Is.EqualTo(3L));
        Assert.That(EntryField(entry, "HasAbsoluteExpiration"), Is.EqualTo(false));
        Assert.That(EntryField(entry, "AbsoluteExpirationUtcTicks"), Is.EqualTo(0L));
        Assert.That(EntryField(entry, "HasSlidingExpiration"), Is.EqualTo(true));
        Assert.That(EntryField(entry, "SlidingExpirationTicks"), Is.EqualTo(50L));
        Assert.That(EntryField(entry, "Weight"), Is.EqualTo(3L));
        Assert.That(EntryField(entry, "Priority"), Is.EqualTo(CacheEntryPriority.High));
    }

    [Test]
    public void PutEntry_RejectsNullKey()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheWriteResult result = cache.PutEntry(null!, "value", CacheEntryOptions.NoExpiration());

        Assert.That(result.Written, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void PutEntry_RejectsNullValueWhenEntryOptionsDisallowIt()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.CacheNullValue = false;

        CacheWriteResult result = cache.PutEntry("a", null, entryOptions);

        Assert.That(result.Written, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Rejected));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void PutEntry_NormalizesInvalidEntryWeightAndPriority()
    {
        Cache cache = new Cache(CacheOptions.Default());
        CacheEntryOptions entryOptions = CacheEntryOptions.NoExpiration();
        entryOptions.Weight = -10L;
        entryOptions.Priority = (CacheEntryPriority)999;

        CacheWriteResult result = cache.PutEntry("a", "one", entryOptions);
        object entry = Entry(cache, "a");

        Assert.That(result.Written, Is.True);
        Assert.That(result.Weight, Is.EqualTo(1L));
        Assert.That(EntryField(entry, "Weight"), Is.EqualTo(1L));
        Assert.That(EntryField(entry, "Priority"), Is.EqualTo(CacheEntryPriority.Normal));
    }

    [Test]
    public void Put_WhenVersionReachesLongLimit_SaturatesVersion()
    {
        Cache cache = new Cache(CacheOptions.Default());
        cache._version = long.MaxValue;

        cache.Put("a", "one");

        Assert.That(cache._version, Is.EqualTo(long.MaxValue));
        Assert.That(cache._entries["a"].Version, Is.EqualTo(long.MaxValue));
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
