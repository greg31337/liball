using System.Reflection;
using System.Collections;
using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheConstructorTests
{
    [Test]
    public void Create_UsesNormalizedDefaults()
    {
        Cache cache = Cache.Create();
        CacheOptions normalized = NormalizedOptions(cache);

        Assert.That(normalized.Name, Is.EqualTo(""));
        Assert.That(normalized.HasDefaultTimeToLive, Is.False);
        Assert.That(normalized.HasMaximumEntries, Is.False);
        Assert.That(normalized.RecordStats, Is.False);
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithNullOptions_UsesNormalizedDefaults()
    {
        Cache cache = new Cache(null);
        CacheOptions normalized = NormalizedOptions(cache);

        Assert.That(normalized.Name, Is.EqualTo(""));
        Assert.That(normalized.KeyComparer, Is.SameAs(EqualityComparer<object>.Default));
        Assert.That(normalized.HasDefaultAbsoluteExpiration, Is.False);
        Assert.That(normalized.HasDefaultTimeToLive, Is.False);
        Assert.That(normalized.HasDefaultSlidingExpiration, Is.False);
        Assert.That(normalized.HasMaximumEntries, Is.False);
        Assert.That(normalized.HasMaximumWeight, Is.False);
        Assert.That(normalized.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(normalized.DefaultPriority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(normalized.Clock, Is.Not.Null);
        Assert.That(normalized.Loader, Is.Null);
        Assert.That(normalized.Weigher, Is.Null);
        Assert.That(normalized.RemovalListener, Is.Null);
        Assert.That(normalized.RecordStats, Is.False);
        Assert.That(normalized.HasCleanupInterval, Is.False);
        Assert.That(normalized.CacheNullLoaderValues, Is.False);
        Assert.That(Entries(cache).Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_CopiesProvidedOptionsIntoNormalizedOptions()
    {
        CacheOptions options = CacheOptions.Default();
        CacheClock clock = CacheClock.Manual(100L);
        ObjectComparer comparer = new ObjectComparer();
        Func<object, object> loader = key => "value";
        Func<object, object?, long> weigher = (key, value) => 2L;
        Action<CacheNotification> listener = notification => notification.Reason = CacheRemovalReason.Clear;

        options.Name = "named";
        options.KeyComparer = comparer;
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = 10L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 20L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = 30L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 40L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = 50L;
        options.EvictionPolicy = CacheEvictionPolicy.Fifo;
        options.DefaultPriority = CacheEntryPriority.High;
        options.Clock = clock;
        options.Loader = loader;
        options.Weigher = weigher;
        options.RemovalListener = listener;
        options.RecordStats = true;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 60L;
        options.CacheNullLoaderValues = true;

        Cache cache = new Cache(options);
        CacheOptions normalized = NormalizedOptions(cache);

        Assert.That(normalized.Name, Is.EqualTo("named"));
        Assert.That(normalized.KeyComparer, Is.SameAs(comparer));
        Assert.That(normalized.HasDefaultAbsoluteExpiration, Is.True);
        Assert.That(normalized.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(10L));
        Assert.That(normalized.HasDefaultTimeToLive, Is.True);
        Assert.That(normalized.DefaultTimeToLiveTicks, Is.EqualTo(20L));
        Assert.That(normalized.HasDefaultSlidingExpiration, Is.True);
        Assert.That(normalized.DefaultSlidingExpirationTicks, Is.EqualTo(30L));
        Assert.That(normalized.HasMaximumEntries, Is.True);
        Assert.That(normalized.MaximumEntries, Is.EqualTo(40L));
        Assert.That(normalized.HasMaximumWeight, Is.True);
        Assert.That(normalized.MaximumWeight, Is.EqualTo(50L));
        Assert.That(normalized.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Fifo));
        Assert.That(normalized.DefaultPriority, Is.EqualTo(CacheEntryPriority.High));
        Assert.That(normalized.Clock, Is.SameAs(clock));
        Assert.That(normalized.Loader, Is.SameAs(loader));
        Assert.That(normalized.Weigher, Is.SameAs(weigher));
        Assert.That(normalized.RemovalListener, Is.SameAs(listener));
        Assert.That(normalized.RecordStats, Is.True);
        Assert.That(normalized.HasCleanupInterval, Is.True);
        Assert.That(normalized.CleanupIntervalTicks, Is.EqualTo(60L));
        Assert.That(normalized.CacheNullLoaderValues, Is.True);
        Assert.That(InternalField(cache, "_clock"), Is.SameAs(clock));
        Assert.That(InternalField(cache, "_keyComparer"), Is.SameAs(comparer));
        Assert.That(InternalField(cache, "_loader"), Is.SameAs(loader));
        Assert.That(InternalField(cache, "_weigher"), Is.SameAs(weigher));
        Assert.That(InternalField(cache, "_removalListener"), Is.SameAs(listener));
    }

    [Test]
    public void Constructor_NormalizesNullAndInvalidOptionValues()
    {
        CacheOptions options = CacheOptions.Default();
        options.Name = null;
        options.KeyComparer = null;
        options.Clock = null;
        options.HasDefaultAbsoluteExpiration = true;
        options.DefaultAbsoluteExpirationUtcTicks = -1L;
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = -2L;
        options.HasDefaultSlidingExpiration = true;
        options.DefaultSlidingExpirationTicks = -3L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 0L;
        options.HasMaximumWeight = true;
        options.MaximumWeight = -1L;
        options.EvictionPolicy = (CacheEvictionPolicy)999;
        options.DefaultPriority = (CacheEntryPriority)999;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 0L;

        Cache cache = new Cache(options);
        CacheOptions normalized = NormalizedOptions(cache);

        Assert.That(normalized.Name, Is.EqualTo(""));
        Assert.That(normalized.KeyComparer, Is.SameAs(EqualityComparer<object>.Default));
        Assert.That(normalized.Clock, Is.Not.Null);
        Assert.That(normalized.HasDefaultAbsoluteExpiration, Is.False);
        Assert.That(normalized.DefaultAbsoluteExpirationUtcTicks, Is.EqualTo(0L));
        Assert.That(normalized.HasDefaultTimeToLive, Is.False);
        Assert.That(normalized.DefaultTimeToLiveTicks, Is.EqualTo(0L));
        Assert.That(normalized.HasDefaultSlidingExpiration, Is.False);
        Assert.That(normalized.DefaultSlidingExpirationTicks, Is.EqualTo(0L));
        Assert.That(normalized.HasMaximumEntries, Is.False);
        Assert.That(normalized.MaximumEntries, Is.EqualTo(0L));
        Assert.That(normalized.HasMaximumWeight, Is.False);
        Assert.That(normalized.MaximumWeight, Is.EqualTo(0L));
        Assert.That(normalized.EvictionPolicy, Is.EqualTo(CacheEvictionPolicy.Lru));
        Assert.That(normalized.DefaultPriority, Is.EqualTo(CacheEntryPriority.Normal));
        Assert.That(normalized.HasCleanupInterval, Is.False);
        Assert.That(normalized.CleanupIntervalTicks, Is.EqualTo(0L));
    }

    [Test]
    public void Constructor_DoesNotRetainMutableOptionsObject()
    {
        CacheOptions options = CacheOptions.Default();
        options.Name = "before";

        Cache cache = new Cache(options);
        options.Name = "after";
        options.HasMaximumEntries = true;
        options.MaximumEntries = 99L;

        CacheOptions normalized = NormalizedOptions(cache);

        Assert.That(normalized, Is.Not.SameAs(options));
        Assert.That(normalized.Name, Is.EqualTo("before"));
        Assert.That(normalized.HasMaximumEntries, Is.False);
        Assert.That(normalized.MaximumEntries, Is.EqualTo(0L));
    }

    static CacheOptions NormalizedOptions(Cache cache)
    {
        object? value = InternalField(cache, "_options");
        Assert.That(value, Is.Not.Null);
        return (CacheOptions)value!;
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

    public class ObjectComparer : EqualityComparer<object>
    {
        public override bool Equals(object? x, object? y)
        {
            return object.Equals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
    }
}
