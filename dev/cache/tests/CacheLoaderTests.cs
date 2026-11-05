using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheLoaderTests
{
    [Test]
    public void GetOrLoad_WhenEntryExists_ReturnsHitWithoutCallingLoader()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded";
        };
        Cache cache = new Cache(options);
        cache.Put("a", "cached");

        CacheGetResult result = cache.GetOrLoad("a");

        Assert.That(result.Found, Is.True);
        Assert.That(result.Value, Is.EqualTo("cached"));
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(result.Loaded, Is.False);
        Assert.That(loadCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOrLoad_WhenMiss_LoadsStoresAndReturnsLoadedValue()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded-" + key.ToString();
        };
        Cache cache = new Cache(options);

        CacheGetResult loaded = cache.GetOrLoad("a");
        CacheGetResult cached = cache.Get("a");

        Assert.That(loaded.Found, Is.True);
        Assert.That(loaded.Value, Is.EqualTo("loaded-a"));
        Assert.That(loaded.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(loaded.Loaded, Is.True);
        Assert.That(cached.Found, Is.True);
        Assert.That(cached.Value, Is.EqualTo("loaded-a"));
        Assert.That(loadCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrLoad_WhenNoLoader_ReturnsMiss()
    {
        Cache cache = new Cache(CacheOptions.Default());

        CacheGetResult result = cache.GetOrLoad("a");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Loaded, Is.False);
    }

    [Test]
    public void GetOrLoad_WhenLoaderReturnsNull_DoesNotCacheByDefault()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return null;
        };
        Cache cache = new Cache(options);

        CacheGetResult first = cache.GetOrLoad("a");
        CacheGetResult second = cache.GetOrLoad("a");

        Assert.That(first.Found, Is.False);
        Assert.That(first.Value, Is.Null);
        Assert.That(first.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(first.Loaded, Is.True);
        Assert.That(second.Found, Is.False);
        Assert.That(second.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(2));
    }

    [Test]
    public void GetOrLoad_RecordsLoadSuccessStats()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            clock.AdvanceTicks(7L);
            return "loaded";
        };
        Cache cache = new Cache(options);

        CacheGetResult result = cache.GetOrLoad("a");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Found, Is.True);
        Assert.That(result.Loaded, Is.True);
        Assert.That(stats.Misses, Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(7L));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(1.0));
    }

    [Test]
    public void GetOrLoad_WhenLoaderThrows_ExceptionPropagatesAndEntryIsNotCached()
    {
        CacheOptions options = CacheOptions.Default();
        options.Loader = key => throw new InvalidOperationException("load failed");
        Cache cache = new Cache(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.GetOrLoad("a");
            })!;

        Assert.That(exception.Message, Is.EqualTo("load failed"));
        Assert.That(cache.Get("a").Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void GetOrLoad_ConcurrentMissesForSameKey_ShareSingleLoaderExecution()
    {
        int loadCount = 0;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.Loader = key =>
        {
            Interlocked.Increment(ref loadCount);
            loaderEntered.Set();
            releaseLoader.Wait(1000);
            return "loaded";
        };
        Cache cache = new Cache(options);

        Task<CacheGetResult> first = Task.Run(() => cache.GetOrLoad("a"));
        Assert.That(loaderEntered.Wait(1000), Is.True);
        Task<CacheGetResult> second = Task.Run(() => cache.GetOrLoad("a"));
        Thread.Sleep(50);

        Assert.That(loadCount, Is.EqualTo(1));
        releaseLoader.Set();
        Assert.That(Task.WaitAll(new Task[] { first, second }, 1000), Is.True);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(first.Result.Found, Is.True);
        Assert.That(first.Result.Value, Is.EqualTo("loaded"));
        Assert.That(first.Result.Loaded, Is.True);
        Assert.That(second.Result.Found, Is.True);
        Assert.That(second.Result.Value, Is.EqualTo("loaded"));
        Assert.That(second.Result.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(1));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
    }

    [Test]
    public void GetOrLoad_AfterLoaderThrows_AllowsLaterRetry()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            if (loadCount == 1)
            {
                throw new InvalidOperationException("load failed");
            }

            return "loaded";
        };
        Cache cache = new Cache(options);

        Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.GetOrLoad("a");
            });
        CacheGetResult retry = cache.GetOrLoad("a");

        Assert.That(retry.Found, Is.True);
        Assert.That(retry.Value, Is.EqualTo("loaded"));
        Assert.That(retry.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(2));
    }

    [Test]
    public void GetOrLoad_WhenExistingEntryExpired_LoadsReplacementValue()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(200L);
        options.Clock = clock;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return "loaded";
        };
        Cache cache = new Cache(options);

        cache.PutEntry("a", "expired", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        CacheGetResult result = cache.GetOrLoad("a");
        CacheGetResult cached = cache.Get("a");

        Assert.That(result.Found, Is.True);
        Assert.That(result.Loaded, Is.True);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(result.Value, Is.EqualTo("loaded"));
        Assert.That(cached.Found, Is.True);
        Assert.That(cached.Value, Is.EqualTo("loaded"));
        Assert.That(loadCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrLoad_LoaderMissReturnsLoadedMissAndLeavesCacheEmpty()
    {
        CacheOptions options = CacheOptions.Default();
        options.Loader = key => null;
        Cache cache = new Cache(options);

        CacheGetResult result = cache.GetOrLoad("a");
        CacheGetResult cached = cache.Get("a");

        Assert.That(result.Found, Is.False);
        Assert.That(result.Loaded, Is.True);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(result.Value, Is.Null);
        Assert.That(cached.Found, Is.False);
        Assert.That(cached.Status, Is.EqualTo(CacheResultStatus.Miss));
    }

    [Test]
    public void GetOrLoad_WhenCacheNullLoaderValuesIsEnabled_CachesNullAsLoadedHit()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.CacheNullLoaderValues = true;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return null;
        };
        Cache cache = new Cache(options);

        CacheGetResult first = cache.GetOrLoad("a");
        CacheGetResult second = cache.GetOrLoad("a");

        Assert.That(first.Found, Is.True);
        Assert.That(first.Value, Is.Null);
        Assert.That(first.Loaded, Is.True);
        Assert.That(first.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(second.Found, Is.True);
        Assert.That(second.Value, Is.Null);
        Assert.That(second.Loaded, Is.False);
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Hit));
        Assert.That(loadCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrLoad_WhenCacheNullLoaderValuesIsEnabled_RecordsCachedNullStats()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        options.CacheNullLoaderValues = true;
        options.Loader = key => null;
        Cache cache = new Cache(options);

        cache.GetOrLoad("a");
        cache.GetOrLoad("a");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Hits, Is.EqualTo(1L));
        Assert.That(stats.Misses, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
        Assert.That(stats.CurrentWeight, Is.EqualTo(1L));
    }

    [Test]
    public void NegativeCaching_CachedNullExpiresAndCanBeLoadedAgain()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(300L);
        options.Clock = clock;
        options.CacheNullLoaderValues = true;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 5L;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return null;
        };
        Cache cache = new Cache(options);

        CacheGetResult first = cache.GetOrLoad("missing");
        clock.AdvanceTicks(4L);
        CacheGetResult cached = cache.GetOrLoad("missing");
        clock.AdvanceTicks(1L);
        CacheGetResult reloaded = cache.GetOrLoad("missing");

        Assert.That(first.Found, Is.True);
        Assert.That(first.Loaded, Is.True);
        Assert.That(cached.Found, Is.True);
        Assert.That(cached.Loaded, Is.False);
        Assert.That(reloaded.Found, Is.True);
        Assert.That(reloaded.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(2));
    }

    [Test]
    public void NegativeCaching_ClearRemovesCachedNullAndAllowsReload()
    {
        int loadCount = 0;
        CacheOptions options = CacheOptions.Default();
        options.CacheNullLoaderValues = true;
        options.Loader = key =>
        {
            loadCount = loadCount + 1;
            return null;
        };
        Cache cache = new Cache(options);

        CacheGetResult first = cache.GetOrLoad("missing");
        cache.Clear();
        CacheGetResult second = cache.GetOrLoad("missing");

        Assert.That(first.Found, Is.True);
        Assert.That(first.Loaded, Is.True);
        Assert.That(second.Found, Is.True);
        Assert.That(second.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(2));
    }
}
