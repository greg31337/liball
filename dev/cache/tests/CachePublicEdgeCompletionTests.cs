using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CachePublicEdgeCompletionTests
{
    [Test]
    public void NullKey_LoadAndRefreshOperationsReturnMissWithoutCallingLoader()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded";
        };
        Cache cache = new Cache(options);

        CacheGetResult loaded = cache.GetOrLoad(null);
        CacheGetResult refreshed = cache.Refresh(null);
        bool needsRefresh = cache.NeedsRefresh(null);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(loaded.Found, Is.False);
        Assert.That(loaded.Key, Is.Null);
        Assert.That(loaded.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(loaded.Loaded, Is.False);
        Assert.That(refreshed.Found, Is.False);
        Assert.That(refreshed.Key, Is.Null);
        Assert.That(refreshed.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(refreshed.Loaded, Is.False);
        Assert.That(needsRefresh, Is.False);
        Assert.That(loadCount, Is.EqualTo(0));
        Assert.That(stats.Misses, Is.EqualTo(1L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(0L));
        Assert.That(stats.LoadFailures, Is.EqualTo(0L));
    }

    [Test]
    public void RefreshChecks_ReturnFalseForMissingAndExpiredEntriesWithoutLoading()
    {
        int loadCount = 0;
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RemovalListener = notification => notifications.Add(notification);
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded";
        };
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.TimeToLive(5L);
        entryOptions.HasRefreshAfterWrite = true;
        entryOptions.RefreshAfterWriteTicks = 1L;
        cache.PutEntry("expired", "old", entryOptions);

        bool missingNeedsRefresh = cache.NeedsRefresh("missing");
        CacheGetResult missingRefresh = cache.Refresh("missing");
        clock.AdvanceTicks(5L);
        bool expiredNeedsRefresh = cache.NeedsRefresh("expired");
        CachePolicySnapshot beforeRefresh = cache.Policy();
        CacheGetResult expiredRefresh = cache.Refresh("expired");

        Assert.That(missingNeedsRefresh, Is.False);
        Assert.That(missingRefresh.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(expiredNeedsRefresh, Is.False);
        Assert.That(beforeRefresh.CurrentCount, Is.EqualTo(1L));
        Assert.That(expiredRefresh.Status, Is.EqualTo(CacheResultStatus.Expired));
        Assert.That(expiredRefresh.Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(loadCount, Is.EqualTo(0));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
    }

    [Test]
    public void Refresh_WhenLoadedReplacementIsEvicted_ReturnsRejectedResult()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 1L;
        options.Weigher = (key, value) => object.Equals(value, "old") ? 1L : 2L;
        options.Loader = key => "too-heavy";
        options.RemovalListener = notification => notifications.Add(notification);
        Cache cache = new Cache(options);
        cache.Put("key", "old");

        CacheGetResult result = cache.Refresh("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Found, Is.False);
        Assert.That(result.Key, Is.EqualTo("key"));
        Assert.That(result.Loaded, Is.True);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(cache.Contains("key"), Is.False);
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(2));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(notifications[0].Value, Is.EqualTo("old"));
        Assert.That(notifications[1].Reason, Is.EqualTo(CacheRemovalReason.Capacity));
        Assert.That(notifications[1].Value, Is.EqualTo("too-heavy"));
    }

    [Test]
    public void Builder_WithNullOptionsRestoresFreshDefaults()
    {
        CacheClock clock = CacheClock.Manual(100L);
        CacheBuilder builder = new CacheBuilder();
        Cache configured = builder
            .WithName("configured")
            .WithClock(clock)
            .WithMaximumEntries(1L)
            .WithStats()
            .Build();
        Cache reset = builder.WithOptions(null).Build();

        CachePolicySnapshot configuredPolicy = configured.Policy();
        CachePolicySnapshot resetPolicy = reset.Policy();

        Assert.That(configuredPolicy.Name, Is.EqualTo("configured"));
        Assert.That(configuredPolicy.HasMaximumEntries, Is.True);
        Assert.That(configuredPolicy.RecordStats, Is.True);
        Assert.That(configuredPolicy.SnapshotUtcTicks, Is.EqualTo(100L));
        Assert.That(resetPolicy.Name, Is.EqualTo(""));
        Assert.That(resetPolicy.HasMaximumEntries, Is.False);
        Assert.That(resetPolicy.MaximumEntries, Is.EqualTo(0L));
        Assert.That(resetPolicy.RecordStats, Is.False);
        Assert.That(resetPolicy.SnapshotUtcTicks, Is.GreaterThan(100L));
    }

    [Test]
    public void Builder_WithCustomComparerUsesEquivalentKeysAcrossOperations()
    {
        ICacheBuilder builder = new CacheBuilder();
        Cache cache = builder
            .WithKeyComparer(new EdgeCaseInsensitiveComparer())
            .Build();

        CacheWriteResult written = cache.Put("Key", "value");
        CacheGetResult equivalent = cache.Get("key");
        CacheWriteResult duplicate = cache.Add("KEY", "other");
        CacheRemoveResult removed = cache.Remove("kEy");

        Assert.That(written.Written, Is.True);
        Assert.That(equivalent.Found, Is.True);
        Assert.That(equivalent.Value, Is.EqualTo("value"));
        Assert.That(duplicate.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(removed.Removed, Is.True);
        Assert.That(removed.Value, Is.EqualTo("value"));
        Assert.That(cache.Count(), Is.EqualTo(0L));
    }

    [Test]
    public void AdvanceTicks_FromSystemModeCapturesTimeAndSwitchesToManualMode()
    {
        CacheClock clock = CacheClock.SystemUtc();
        long before = DateTime.UtcNow.Ticks;

        clock.AdvanceTicks(100000L);

        long firstManual = clock.GetUtcTicks();
        long after = DateTime.UtcNow.Ticks;
        Assert.That(clock.UsesManualTicks, Is.True);
        Assert.That(firstManual, Is.GreaterThanOrEqualTo(before + 100000L));
        Assert.That(firstManual, Is.LessThanOrEqualTo(after + 100000L));

        clock.AdvanceTicks(25L);
        Assert.That(clock.GetUtcTicks(), Is.EqualTo(firstManual + 25L));
    }

    [Test]
    public void Memoizer_InvokeRejectsNullBeforeReadingCacheOrCallingFunction()
    {
        int functionCalls = 0;
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        Memoizer memoizer = new Memoizer(
            cache,
            argument =>
            {
                functionCalls = functionCalls + 1;
                return argument;
            });

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            delegate
            {
                memoizer.Invoke(null!);
            })!;
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(exception.ParamName, Is.EqualTo("argument"));
        Assert.That(functionCalls, Is.EqualTo(0));
        Assert.That(stats.Hits, Is.EqualTo(0L));
        Assert.That(stats.Misses, Is.EqualTo(0L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    public class EdgeCaseInsensitiveComparer : EqualityComparer<object>
    {
        public override bool Equals(object? x, object? y)
        {
            return string.Equals(x?.ToString(), y?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(object obj)
        {
            string value = obj.ToString() ?? "";
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
        }
    }
}
