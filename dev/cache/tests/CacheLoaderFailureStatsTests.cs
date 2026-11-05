using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheLoaderFailureStatsTests
{
    [Test]
    public void GetOrLoad_WhenLoaderFails_RecordsDurationPropagatesAndAllowsRetry()
    {
        int loadCount = 0;
        CacheClock clock = CacheClock.Manual(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            loadCount++;
            clock.AdvanceTicks(7L);
            if (loadCount == 1)
            {
                throw new InvalidOperationException("load failed");
            }

            return "loaded";
        };
        Cache cache = new Cache(options);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.GetOrLoad("key");
            })!;
        CacheStatsSnapshot afterFailure = cache.Stats();
        CacheGetResult retry = cache.GetOrLoad("key");
        CacheStatsSnapshot afterRetry = cache.Stats();

        Assert.That(exception.Message, Is.EqualTo("load failed"));
        Assert.That(afterFailure.LoadFailures, Is.EqualTo(1L));
        Assert.That(afterFailure.LoadSuccesses, Is.EqualTo(0L));
        Assert.That(afterFailure.TotalLoadDurationTicks, Is.EqualTo(7L));
        Assert.That(afterFailure.LoadSuccessRate, Is.EqualTo(0.0));
        Assert.That(retry.Found, Is.True);
        Assert.That(retry.Loaded, Is.True);
        Assert.That(retry.Value, Is.EqualTo("loaded"));
        Assert.That(loadCount, Is.EqualTo(2));
        Assert.That(afterRetry.LoadFailures, Is.EqualTo(1L));
        Assert.That(afterRetry.LoadSuccesses, Is.EqualTo(1L));
        Assert.That(afterRetry.TotalLoadDurationTicks, Is.EqualTo(14L));
        Assert.That(afterRetry.LoadSuccessRate, Is.EqualTo(0.5));
    }

    [Test]
    public void Refresh_WhenLoaderFails_RecordsDurationAndPreservesExistingEntry()
    {
        CacheClock clock = CacheClock.Manual(200L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.RecordStats = true;
        options.Loader = key =>
        {
            clock.AdvanceTicks(11L);
            throw new InvalidOperationException("refresh failed");
        };
        Cache cache = new Cache(options);
        cache.Put("key", "original");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            delegate
            {
                cache.Refresh("key");
            })!;
        CacheGetResult current = cache.Peek("key");
        CacheStatsSnapshot stats = cache.Stats();

        Assert.That(exception.Message, Is.EqualTo("refresh failed"));
        Assert.That(current.Found, Is.True);
        Assert.That(current.Value, Is.EqualTo("original"));
        Assert.That(stats.LoadFailures, Is.EqualTo(1L));
        Assert.That(stats.LoadSuccesses, Is.EqualTo(0L));
        Assert.That(stats.TotalLoadDurationTicks, Is.EqualTo(11L));
        Assert.That(stats.LoadSuccessRate, Is.EqualTo(0.0));
        Assert.That(stats.Updates, Is.EqualTo(0L));
        Assert.That(stats.CurrentSize, Is.EqualTo(1L));
    }
}
