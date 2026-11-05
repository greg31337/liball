using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheLoaderStatisticsCompletionTests
{
    [Test]
    public void GetOrLoad_NonCachedNullsCountAsSuccessfulLoadsWithDurations()
    {
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            clock.AdvanceTicks(3L);
            return null;
        };
        Cache cache = new Cache(options);

        CacheGetResult first = cache.GetOrLoad("key");
        CacheGetResult second = cache.GetOrLoad("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(first.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(first.Loaded, Is.True);
        Assert.That(second.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(second.Loaded, Is.True);
        Assert.That(stats.LoadSuccesses, Is.EqualTo(2L));
        Assert.That(stats.LoadFailures, Is.EqualTo(0L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(6L));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(1.0));
        Assert.That(stats.Puts, Is.EqualTo(0L));
        Assert.That(stats.Misses, Is.EqualTo(2L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    [Test]
    public void GetOrLoad_CapacityRejectedWriteStillCountsAsSuccessfulLoad()
    {
        CacheClock clock = CacheClock.Manual(200L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 1L;
        options.Weigher = (key, value) => 2L;
        options.Loader = key =>
        {
            clock.AdvanceTicks(4L);
            return "loaded";
        };
        Cache cache = new Cache(options);

        CacheGetResult result = cache.GetOrLoad("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(result.Found, Is.False);
        Assert.That(result.Loaded, Is.True);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Rejected));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(stats.LoadFailures, Is.EqualTo(0L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(4L));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(1.0));
        Assert.That(stats.Puts, Is.EqualTo(1L));
        Assert.That(stats.Evictions, Is.EqualTo(1L));
        Assert.That(stats.CurrentSize, Is.EqualTo(0L));
    }

    [Test]
    public void GetOrLoad_ConcurrentFailedSingleFlightRecordsOneFailureAndRetrySuccess()
    {
        int loadCount = 0;
        ManualResetEventSlim loaderEntered = new ManualResetEventSlim(false);
        ManualResetEventSlim releaseLoader = new ManualResetEventSlim(false);
        CacheClock clock = CacheClock.Manual(300L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            int invocation = Interlocked.Increment(ref loadCount);
            if (invocation == 1)
            {
                loaderEntered.Set();
                releaseLoader.Wait(1000);
                clock.AdvanceTicks(9L);
                throw new InvalidOperationException("shared failure");
            }

            clock.AdvanceTicks(5L);
            return "loaded";
        };
        Cache cache = new Cache(options);
        int callerCount = 4;
        Task<CacheGetResult>[] callers = new Task<CacheGetResult>[callerCount];
        callers[0] = Task.Run(() => cache.GetOrLoad("key"));
        Assert.That(loaderEntered.Wait(1000), Is.True);
        for (int index = 1; index < callerCount; index++)
        {
            callers[index] = Task.Run(() => cache.GetOrLoad("key"));
        }

        Assert.That(SpinWait.SpinUntil(() => cache.Stats().Misses == callerCount, 1000), Is.True);
        Thread.Sleep(50);
        Assert.That(loadCount, Is.EqualTo(1));
        releaseLoader.Set();
        Assert.That(SpinWait.SpinUntil(() => AllCompleted(callers), 1000), Is.True);

        for (int index = 0; index < callers.Length; index++)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                delegate
                {
                    callers[index].GetAwaiter().GetResult();
                })!;
            Assert.That(exception.Message, Is.EqualTo("shared failure"));
        }

        CacheStatsSnapshot afterFailure = cache.Stats();
        CacheGetResult retry = cache.GetOrLoad("key");
        CacheStatsSnapshot afterRetry = cache.Stats();

        Assert.That(afterFailure.LoadFailures, Is.EqualTo(1L));
        Assert.That(afterFailure.LoadSuccesses, Is.EqualTo(0L));
        Assert.That(afterFailure.TotalLoadDurationTicks, Is.EqualTo(9L));
        Assert.That(afterFailure.LoadSuccessRate, Is.EqualTo(0.0));
        Assert.That(retry.Found, Is.True);
        Assert.That(retry.Loaded, Is.True);
        Assert.That(loadCount, Is.EqualTo(2));
        Assert.That(afterRetry.LoadFailures, Is.EqualTo(1L));
        Assert.That(afterRetry.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(afterRetry.TotalLoadDurationTicks, Is.EqualTo(14L));
        Assert.That(afterRetry.LoadSuccessRate, Is.EqualTo(0.5));
    }

    [Test]
    public void Refresh_MixedSuccessfulNullAndThrowingLoadsAccumulateStatistics()
    {
        int loadCount = 0;
        CacheClock clock = CacheClock.Manual(400L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            loadCount++;
            if (loadCount == 1)
            {
                clock.AdvanceTicks(2L);
                return "refreshed";
            }

            if (loadCount == 2)
            {
                clock.AdvanceTicks(3L);
                return null;
            }

            clock.AdvanceTicks(5L);
            throw new InvalidOperationException("refresh failed");
        };
        Cache cache = new Cache(options);
        cache.Put("key", "original");

        CacheGetResult successful = cache.Refresh("key");
        CacheGetResult nullResult = cache.Refresh("key");
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.Refresh("key");
            })!;
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(successful.Status, Is.EqualTo(CacheResultStatus.Loaded));
        Assert.That(successful.Value, Is.EqualTo("refreshed"));
        Assert.That(nullResult.Status, Is.EqualTo(CacheResultStatus.Miss));
        Assert.That(nullResult.Loaded, Is.True);
        Assert.That(exception.Message, Is.EqualTo("refresh failed"));
        Assert.That(cache.Peek("key").Value, Is.EqualTo("refreshed"));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(2L));
        Assert.That(stats.LoadFailures, Is.EqualTo(1L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(10L));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(2.0 / 3.0).Within(0.000001));
        Assert.That(stats.Updates, Is.EqualTo(1L));
    }

    [Test]
    public void LoadStatistics_SaturateSuccessFailureAndDurationCountersAndKeepRateStable()
    {
        CacheOptions options = CacheOptions.Default();
        options.RecordStats = true;
        Cache cache = new Cache(options);
        cache._stats.LoadSuccesses = long.MaxValue;
        cache._stats.LoadFailures = long.MaxValue;
        cache._stats.TotalLoadDurationTicks = long.MaxValue;

        cache.RecordLoadSuccess(1L);
        cache.RecordLoadFailure(1L);
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(stats.LoadSuccesses, Is.EqualTo(long.MaxValue));
        Assert.That(stats.LoadFailures, Is.EqualTo(long.MaxValue));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(long.MaxValue));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(0.5));
    }

    static bool AllCompleted(Task<CacheGetResult>[] tasks)
    {
        for (int index = 0; index < tasks.Length; index++)
        {
            if (!tasks[index].IsCompleted)
            {
                return false;
            }
        }

        return true;
    }
}
