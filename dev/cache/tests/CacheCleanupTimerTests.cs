using System.Collections;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheCleanupTimerTests
{
    [Test]
    public void Constructor_WhenCleanupIntervalIsNotConfigured_DoesNotStartTimer()
    {
        Cache cache = new Cache(CacheOptions.Default());

        Assert.That(InternalField(cache, "_cleanupTimer"), Is.Null);
    }

    [Test]
    public void Constructor_WhenCleanupIntervalIsConfigured_StartsTimer()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 10000L;
        Cache cache = new Cache(options);

        Assert.That(InternalField(cache, "_cleanupTimer"), Is.Not.Null);

        cache.Stop();
    }

    [Test]
    public void CleanupTimer_RemovesExpiredEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 10000L;
        Cache cache = new Cache(options);

        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);
        bool removed = WaitForEntryCount(cache, 0, 1000);

        cache.Stop();

        Assert.That(removed, Is.True);
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Stop_DisposesTimerAndIsIdempotent()
    {
        CacheOptions options = CacheOptions.Default();
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 36000000000L;
        Cache cache = new Cache(options);

        cache.Stop();
        cache.Stop();

        Assert.That(InternalField(cache, "_cleanupTimer"), Is.Null);
        Assert.That(InternalField(cache, "_stopped"), Is.EqualTo(true));
    }

    [Test]
    public void CleanupTimerTick_AfterStop_DoesNotCleanupExpiredEntries()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        options.Clock = clock;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 36000000000L;
        Cache cache = new Cache(options);
        cache.PutEntry("expired", "one", CacheEntryOptions.TimeToLive(5L));
        clock.AdvanceTicks(5L);

        cache.Stop();
        InvokeCleanupTimerTick(cache);

        Assert.That(Entries(cache).Count, Is.EqualTo(1));
        Assert.That(Entries(cache).Contains("expired"), Is.True);
    }

    static bool WaitForEntryCount(Cache cache, int expectedCount, int timeoutMilliseconds)
    {
        int waitedMilliseconds = 0;

        while (waitedMilliseconds <= timeoutMilliseconds)
        {
            if (Entries(cache).Count == expectedCount)
            {
                return true;
            }

            Thread.Sleep(10);
            waitedMilliseconds = waitedMilliseconds + 10;
        }

        return false;
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

    static void InvokeCleanupTimerTick(Cache cache)
    {
        MethodInfo? method = typeof(Cache).GetMethod("CleanupTimerTick", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(cache, new object?[] { null });
    }
}
