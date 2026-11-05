using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheStaleWhileRefreshTests
{
    [Test]
    public void Get_WhenRefreshIsDue_ReturnsStaleAndPublishesOneBackgroundReplacement()
    {
        int loadCount = 0;
        List<CacheNotification> notifications = new List<CacheNotification>();
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(5L));
        clock.AdvanceTicks(5L);

        CacheGetResult first = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        CacheGetResult second = cache.Get("key");

        Assert.That(first.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(first.Value, Is.EqualTo("old"));
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(second.Value, Is.EqualTo("old"));
        Assert.That(loadCount, Is.EqualTo(1));

        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(current.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(current.Value, Is.EqualTo("new"));
        Assert.That(cache.NeedsRefresh("key"), Is.False);
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Replace));
        Assert.That(notifications[0].Value, Is.EqualTo("old"));
    }

    [Test]
    public void Get_WhenBackgroundLoaderFails_PreservesOldValue()
    {
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.Loader = key =>
        {
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            throw new InvalidOperationException("refresh failed");
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);

        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(current.Value, Is.EqualTo("old"));
        Assert.That(stats.LoadFailures, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
    }

    [Test]
    public void Get_WhenBackgroundLoaderReturnsNonCacheableNull_PreservesOldValue()
    {
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.Loader = key =>
        {
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return null;
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);

        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(current.Value, Is.EqualTo("old"));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
    }

    [Test]
    public void BackgroundRefresh_DoesNotOverwriteAConcurrentReplacement()
    {
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.Loader = key =>
        {
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "background";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        cache.Put("key", "manual");
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);

        CacheGetResult current = cache.Peek("key");

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(current.Value, Is.EqualTo("manual"));
    }

    [Test]
    public void Stop_PreventsNewBackgroundRefreshWork()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));
        cache.Stop();

        CacheGetResult result = cache.Get("key");

        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Value, Is.EqualTo("old"));
        Assert.That(loadCount, Is.EqualTo(0));
        Assert.That(BackgroundRefreshCount(cache), Is.EqualTo(0));
    }

    [Test]
    public void Get_UsesBeforeAndExactRefreshBoundariesAfterEverySuccessfulWrite()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.StaleWhileRefresh = true;
        options.Loader = key => "value-" + Interlocked.Increment(ref loadCount).ToString();
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(10L));

        clock.AdvanceTicks(9L);
        CacheGetResult beforeFirstBoundary = cache.Get("key");
        clock.AdvanceTicks(1L);
        CacheGetResult atFirstBoundary = cache.Get("key");
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);

        clock.AdvanceTicks(9L);
        CacheGetResult beforeSecondBoundary = cache.Get("key");
        clock.AdvanceTicks(1L);
        CacheGetResult atSecondBoundary = cache.Get("key");
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult current = cache.Peek("key");

        Assert.That(beforeFirstBoundary.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(beforeFirstBoundary.Value, Is.EqualTo("old"));
        Assert.That(atFirstBoundary.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(atFirstBoundary.Value, Is.EqualTo("old"));
        Assert.That(beforeSecondBoundary.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(beforeSecondBoundary.Value, Is.EqualTo("value-1"));
        Assert.That(atSecondBoundary.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(atSecondBoundary.Value, Is.EqualTo("value-1"));
        Assert.That(current.Value, Is.EqualTo("value-2"));
        Assert.That(loadCount, Is.EqualTo(2));
    }

    [Test]
    public void GetMany_WithDuplicateDueKeys_ReturnsStaleHitsAndStartsOneRefresh()
    {
        int loadCount = 0;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheBulkGetResult result = cache.GetMany(new object?[] { "key", "key", "key" });
        Assert.That(loaderEntered.Wait(2000), Is.True);

        Assert.That(result.Results, Has.Length.EqualTo(3));
        Assert.That(result.Results[0].Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(result.Results[1].Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(result.Results[2].Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(result.Results[0].Value, Is.EqualTo("old"));
        Assert.That(result.Results[1].Value, Is.EqualTo("old"));
        Assert.That(result.Results[2].Value, Is.EqualTo("old"));
        Assert.That(loadCount, Is.EqualTo(1));

        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
    }

    [Test]
    public void ConcurrentGets_ForOneDueKey_ShareOneBackgroundRefresh()
    {
        int loadCount = 0;
        ManualResetEventSlim startReads = new ManualResetEventSlim(false);
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));
        Task<CacheGetResult>[] reads = new Task<CacheGetResult>[8];
        int index = 0;
        while (index < reads.Length)
        {
            reads[index] = Task.Run(() =>
            {
                startReads.Wait(2000);
                return cache.Get("key");
            });
            index = index + 1;
        }

        startReads.Set();
        Assert.That(Task.WaitAll(reads, 2000), Is.True);
        Assert.That(loaderEntered.Wait(2000), Is.True);

        index = 0;
        while (index < reads.Length)
        {
            Assert.That(reads[index].Result.Status, Is.EqualTo(CacheResultStatus.Stale));
            Assert.That(reads[index].Result.Value, Is.EqualTo("old"));
            index = index + 1;
        }
        Assert.That(loadCount, Is.EqualTo(1));

        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        Assert.That(cache.Peek("key").Value, Is.EqualTo("new"));
    }

    [Test]
    public void Get_WhenNullCachingIsEnabled_PublishesNullAsAFoundReplacement()
    {
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.CacheNullLoaderValues = true;
        options.RecordStats = true;
        options.Loader = key => null;
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(stale.Value, Is.EqualTo("old"));
        Assert.That(current.Found, Is.True);
        Assert.That(current.Value, Is.Null);
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
    }

    [Test]
    public void BackgroundRefresh_WhenEntryExpiresDuringLoad_RemovesWithoutPublishing()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        options.Loader = key =>
        {
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        CacheEntryOptions entryOptions = CacheEntryOptions.TimeToLive(10L);
        entryOptions.HasRefreshAfterWrite = true;
        entryOptions.RefreshAfterWriteTicks = 5L;
        cache.PutEntry("key", "old", entryOptions);
        clock.AdvanceTicks(5L);

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        clock.AdvanceTicks(5L);
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(current.Found, Is.False);
        Assert.That(current.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
        Assert.That(stats.Expirations, Is.EqualTo(1L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.Expired));
        Assert.That(notifications[0].Value, Is.EqualTo("old"));
    }

    [Test]
    public void BackgroundRefresh_DoesNotResurrectAnExplicitlyRemovedEntry()
    {
        List<CacheNotification> notifications = new List<CacheNotification>();
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.RemovalListener = notification => notifications.Add(notification);
        options.Loader = key =>
        {
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        CacheRemoveResult removed = cache.Remove("key");
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(removed.Removed, Is.True);
        Assert.That(current.Found, Is.False);
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Removes, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(0L));
        Assert.That(notifications, Has.Count.EqualTo(1));
        Assert.That(notifications[0].Reason, Is.EqualTo(CacheRemovalReason.ExplicitRemove));
    }

    [Test]
    public void Statistics_CountEveryStaleHitAndBackgroundLoadDurationOnce()
    {
        int loadCount = 0;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.StaleWhileRefresh = true;
        options.RecordStats = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            clock.AdvanceTicks(7L);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult first = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        CacheGetResult second = cache.Get("key");
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(first.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(loadCount, Is.EqualTo(1));
        Assert.That(stats.Hits, Is.EqualTo(2L));
        Assert.That(stats.Misses, Is.EqualTo(0L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Updates, Is.EqualTo(1L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.LoadFailures, Is.EqualTo(0L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(7L));
    }

    [Test]
    public void Stop_DuringBackgroundRefresh_AllowsItToFinishButPreventsTheNextOne()
    {
        int loadCount = 0;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.StaleWhileRefresh = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(2000);
            return "new";
        };
        Cache cache = new Cache(options);
        cache.PutEntry("key", "old", CacheEntryOptions.RefreshAfterWrite(0L));

        CacheGetResult stale = cache.Get("key");
        Assert.That(loaderEntered.Wait(2000), Is.True);
        cache.Stop();
        releaseLoader.Set();
        Assert.That(WaitForBackgroundRefreshes(cache), Is.True);
        CacheGetResult afterStop = cache.Get("key");

        Assert.That(stale.Status, Is.EqualTo(CacheResultStatus.Stale));
        Assert.That(afterStop.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(afterStop.Value, Is.EqualTo("new"));
        Assert.That(loadCount, Is.EqualTo(1));
        Assert.That(BackgroundRefreshCount(cache), Is.EqualTo(0));
    }

    static bool WaitForBackgroundRefreshes(Cache cache)
    {
        return SpinWait.SpinUntil(() => BackgroundRefreshCount(cache) == 0, 2000);
    }

    static int BackgroundRefreshCount(Cache cache)
    {
        FieldInfo? syncField = typeof(Cache).GetField("_sync", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo? field = typeof(Cache).GetField("_refreshes", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(syncField, Is.Not.Null);
        Assert.That(field, Is.Not.Null);
        object? sync = syncField!.GetValue(cache);
        object? value = field!.GetValue(cache);
        Assert.That(sync, Is.Not.Null);
        Assert.That(value, Is.Not.Null);
        lock (sync!)
        {
            return ((IDictionary)value!).Count;
        }
    }
}
