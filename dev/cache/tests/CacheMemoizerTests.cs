using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheMemoizerTests
{
    [Test]
    public void Constructor_RejectsNullCacheAndFunction()
    {
        Cache cache = new Cache(CacheOptions.Default());

        Assert.Throws<ArgumentNullException>(
            delegate
            {
                new Memoizer(null!, key => key);
            });
        Assert.Throws<ArgumentNullException>(
            delegate
            {
                new Memoizer(cache, null!);
            });
    }

    [Test]
    public void Invoke_WhenCacheAlreadyContainsArgument_ReturnsCachedValueWithoutCallingFunction()
    {
        int callCount = 0;
        Cache cache = new Cache(CacheOptions.Default());
        cache.Put("a", "cached");
        Memoizer memoizer = new Memoizer(
            cache,
            key =>
            {
                callCount = callCount + 1;
                return "computed";
            });

        object? value = memoizer.Invoke("a");

        Assert.That(value, Is.EqualTo("cached"));
        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public void Invoke_WhenCacheMisses_ComputesStoresAndReusesValue()
    {
        int callCount = 0;
        Cache cache = new Cache(CacheOptions.Default());
        Memoizer memoizer = new Memoizer(
            cache,
            key =>
            {
                callCount = callCount + 1;
                return "computed-" + key.ToString();
            });

        object? first = memoizer.Invoke("a");
        object? second = memoizer.Invoke("a");
        CacheGetResult cached = cache.Get("a");

        Assert.That(first, Is.EqualTo("computed-a"));
        Assert.That(second, Is.EqualTo("computed-a"));
        Assert.That(cached.Found, Is.True);
        Assert.That(cached.Value, Is.EqualTo("computed-a"));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Invoke_AfterCacheClear_ComputesAgainAndStoresReplacement()
    {
        int callCount = 0;
        Cache cache = new Cache(CacheOptions.Default());
        Memoizer memoizer = new Memoizer(
            cache,
            key =>
            {
                callCount = callCount + 1;
                return "computed-" + callCount.ToString();
            });

        object? first = memoizer.Invoke("a");
        cache.Clear();
        object? second = memoizer.Invoke("a");
        object? third = memoizer.Invoke("a");

        Assert.That(first, Is.EqualTo("computed-1"));
        Assert.That(second, Is.EqualTo("computed-2"));
        Assert.That(third, Is.EqualTo("computed-2"));
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void Invoke_WhenFunctionReturnsNull_CachesNullByDefault()
    {
        int callCount = 0;
        Cache cache = new Cache(CacheOptions.Default());
        Memoizer memoizer = new Memoizer(
            cache,
            key =>
            {
                callCount = callCount + 1;
                return null;
            });

        object? first = memoizer.Invoke("a");
        object? second = memoizer.Invoke("a");
        CacheGetResult cached = cache.Get("a");

        Assert.That(first, Is.Null);
        Assert.That(second, Is.Null);
        Assert.That(cached.Found, Is.True);
        Assert.That(cached.Value, Is.Null);
        Assert.That(callCount, Is.EqualTo(1));
    }
}
