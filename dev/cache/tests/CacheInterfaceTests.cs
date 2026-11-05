namespace cache.Tests;

public class CacheInterfaceTests
{
    [Test]
    public void Cache_ThroughInterface_ProvidesThePublicContract()
    {
        ICache cache = new Cache(CacheOptions.Default());

        CacheWriteResult write = cache.Put("key", "value");
        CacheGetResult read = cache.Get("key");

        Assert.That(write.Written, Is.True);
        Assert.That(read.Found, Is.True);
        Assert.That(read.Value, Is.EqualTo("value"));
    }

    [Test]
    public void Cache_WithCustomClockInterface_UsesApplicationOwnedTimeSource()
    {
        StubCacheClock clock = new StubCacheClock(100L);
        CacheOptions options = CacheOptions.Default();
        options.Clock = clock;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 10L;
        ICache cache = new Cache(options);

        cache.Put("key", "value");
        clock.UtcTicks = 110L;

        CacheGetResult result = cache.Get("key");
        Assert.That(result.Found, Is.False);
        Assert.That(result.Status, Is.EqualTo(CacheResultStatus.Expired));
    }

    [Test]
    public void Memoizer_WithCacheStoreInterface_CanBeTestedWithoutConcreteCacheBehavior()
    {
        StubCache cache = new StubCache();
        int functionCalls = 0;
        Memoizer memoizer = new Memoizer(
            cache,
            argument =>
            {
                functionCalls++;
                return "computed-" + argument.ToString();
            });

        object? first = memoizer.Invoke("a");
        object? second = memoizer.Invoke("a");

        Assert.That(first, Is.EqualTo("computed-a"));
        Assert.That(second, Is.EqualTo("computed-a"));
        Assert.That(functionCalls, Is.EqualTo(1));
        Assert.That(cache.GetCalls, Is.EqualTo(2));
        Assert.That(cache.PutCalls, Is.EqualTo(1));
    }

    internal class StubCacheClock : ICacheClock
    {
        internal long UtcTicks;

        internal StubCacheClock(long utcTicks)
        {
            UtcTicks = utcTicks;
        }

        public long GetUtcTicks()
        {
            return UtcTicks;
        }
    }

    internal class StubCache : ICacheStore
    {
        internal bool HasValue;
        internal object? Key;
        internal object? Value;
        internal int GetCalls;
        internal int PutCalls;

        internal StubCache()
        {
            HasValue = false;
            Key = null;
            Value = null;
            GetCalls = 0;
            PutCalls = 0;
        }

        public CacheWriteResult Write(object? key, object? value)
        {
            PutCalls++;
            HasValue = true;
            Key = key;
            Value = value;

            CacheWriteResult result = new CacheWriteResult();
            result.Written = true;
            result.Key = key;
            result.Value = value;
            result.Status = CacheResultStatus.Written;
            return result;
        }

        public CacheGetResult Read(object? key)
        {
            GetCalls++;
            CacheGetResult result = new CacheGetResult();
            result.Key = key;

            if (HasValue && object.Equals(Key, key))
            {
                result.Found = true;
                result.Value = Value;
                result.Status = CacheResultStatus.Hit;
            }
            else
            {
                result.Status = CacheResultStatus.Miss;
            }

            return result;
        }
    }
}
