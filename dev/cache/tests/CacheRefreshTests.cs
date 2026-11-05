using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheRefreshTests
{
    [Test]
    public void RefreshAfterWriteFactory_ConfiguresRefreshMetadata()
    {
        CacheEntryOptions options = CacheEntryOptions.RefreshAfterWrite(25L);

        Assert.That(options.HasRefreshAfterWrite, Is.True);
        Assert.That(options.RefreshAfterWriteTicks, Is.EqualTo(25L));
        Assert.That(options.HasTimeToLive, Is.False);
        Assert.That(options.HasSlidingExpiration, Is.False);
        Assert.That(options.HasAbsoluteExpiration, Is.False);
    }

    [Test]
    public void RefreshAfterWriteFactory_RejectsNegativeTicks()
    {
        Assert.That(
            delegate
            {
                CacheEntryOptions.RefreshAfterWrite(-1L);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void NeedsRefresh_UsesLastWriteTimeBoundaryAndIgnoresAccessRefresh()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        Cache cache = new Cache(options);
        cache.PutEntry("a", "one", CacheEntryOptions.RefreshAfterWrite(10L));

        clock.AdvanceTicks(9L);
        bool beforeBoundary = cache.NeedsRefresh("a");
        cache.Get("a");
        clock.AdvanceTicks(1L);
        bool atBoundary = cache.NeedsRefresh("a");

        Assert.That(beforeBoundary, Is.False);
        Assert.That(atBoundary, Is.True);
    }

    [Test]
    public void PutEntry_StoresRefreshAfterWriteMetadata()
    {
        Cache cache = new Cache(CacheOptions.Default());

        cache.PutEntry("a", "one", CacheEntryOptions.RefreshAfterWrite(10L));
        object entry = Entry(cache, "a");

        Assert.That(EntryField(entry, "HasRefreshAfterWrite"), Is.EqualTo(true));
        Assert.That(EntryField(entry, "RefreshAfterWriteTicks"), Is.EqualTo(10L));
    }

    [Test]
    public void Refresh_WithLoader_ReplacesLiveEntryAndResetsRefreshBoundary()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        options.Loader = key => "loaded-" + key.ToString();
        Cache cache = new Cache(options);
        cache.PutEntry("a", "old", CacheEntryOptions.RefreshAfterWrite(10L));
        clock.AdvanceTicks(10L);

        CacheGetResult refreshed = cache.Refresh("a");
        CacheGetResult cached = cache.Get("a");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(refreshed.Found, Is.True);
        Assert.That(refreshed.Loaded, Is.True);
        Assert.That(refreshed.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(refreshed.Value, Is.EqualTo("loaded-a"));
        Assert.That(refreshed.LastWriteUtcTicks, Is.EqualTo(210L));
        Assert.That(cache.NeedsRefresh("a"), Is.False);
        Assert.That(cached.Value, Is.EqualTo("loaded-a"));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(notifications[0].Value, Is.EqualTo("old"));
    }

    [Test]
    public void Refresh_WithoutLiveEntryOrLoader_ReturnsMiss()
    {
        Cache noLoader = new Cache(CacheOptions.Default());
        CacheOptions options = CacheOptions.Default();
        options.Loader = key => "loaded";
        Cache missing = new Cache(options);

        noLoader.Put("a", "one");
        CacheGetResult withoutLoader = noLoader.Refresh("a");
        CacheGetResult missingResult = missing.Refresh("a");

        Assert.That(withoutLoader.Found, Is.False);
        Assert.That(withoutLoader.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(missingResult.Found, Is.False);
        Assert.That(missingResult.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void Refresh_WhenEntryExpired_RemovesExpiredEntryAndDoesNotLoad()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("a", "old", CacheEntryOptions.TimeToLive(5L));

        clock.AdvanceTicks(5L);
        CacheGetResult result = cache.Refresh("a");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(result.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(loadCount, Is.EqualTo(0));
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void NeedsRefresh_WithZeroRefreshInterval_IsImmediatelyDue()
    {
        Cache cache = new Cache(CacheOptions.Default());

        cache.PutEntry("a", "one", CacheEntryOptions.RefreshAfterWrite(0L));

        Assert.That(cache.NeedsRefresh("a"), Is.True);
    }

    [Test]
    public void Refresh_WhenLoaderReturnsNullAndNullCachingDisabled_LeavesOldValue()
    {
        CacheOptions options = CacheOptions.Default();
        options.Loader = key => null;
        Cache cache = new Cache(options);
        cache.PutEntry("a", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult refresh = cache.Refresh("a");
        CacheGetResult current = cache.Get("a");

        Assert.That(refresh.Found, Is.False);
        Assert.That(refresh.Loaded, Is.True);
        Assert.That(refresh.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(current.Found, Is.True);
        Assert.That(current.Value, Is.EqualTo("old"));
    }

    [Test]
    public void Refresh_WhenLoaderReturnsNullAndNullCachingEnabled_ReplacesWithCachedNull()
    {
        CacheOptions options = CacheOptions.Default();
        options.CacheNullLoaderValues = true;
        options.Loader = key => null;
        Cache cache = new Cache(options);
        cache.PutEntry("a", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult refresh = cache.Refresh("a");
        CacheGetResult current = cache.Get("a");

        Assert.That(refresh.Found, Is.True);
        Assert.That(refresh.Loaded, Is.True);
        Assert.That(refresh.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(refresh.Value, Is.Null);
        Assert.That(current.Found, Is.True);
        Assert.That(current.Value, Is.Null);
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
