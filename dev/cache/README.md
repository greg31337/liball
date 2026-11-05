# cache

`cache` is a dependency-free in-process .NET cache library. The public API uses
object keys, object values, public fields, and result objects so callers can use
the same simplified C# subset as the library source.

The symbols declared `public` under `src/Api/` are the supported API surface.
Library implementation types and members outside that directory are `internal`.
Every public API symbol has XML documentation; internal implementation symbols
intentionally do not.

## Interfaces And Substitution

`Cache` implements the complete `ICache` contract and the smaller `ICacheStore`
read/write contract. Application services can accept `ICache` when they use the
full cache API, or `ICacheStore` when they only need `Read` and `Write`.
`CacheBuilder` and `Memoizer` implement `ICacheBuilder` and `IMemoizer`, while
`Memoizer` accepts `ICacheStore` to remain independent of cache maintenance,
policy, loading, and lifecycle behavior.

`CacheClock` implements `ICacheClock` and `IAdjustableCacheClock`. Assign any
application-owned `ICacheClock` implementation to `CacheOptions.Clock`, or pass
it through `CacheBuilder.WithClock`, to provide time without depending on the
mutable clock supplied by this library.

The loader, weigher, and removal listener remain delegates. They are single-call
callbacks, so delegates preserve direct lambda configuration without adding
one-method interface types.

## Basic Operations

```csharp
namespace cache;

internal class BasicCacheExample
{
    internal void Run()
    {
        ICache cache = new Cache(null);

        cache.Put("user:1", "Ada");
        CacheGetResult hit = cache.Get("user:1");
        if (hit.Found)
        {
            object value = hit.Value;
        }

        CacheGetResult peek = cache.Peek("user:1");
        CacheRemoveResult removed = cache.Remove("user:1");
        CacheMaintenanceResult cleared = cache.Clear();
    }
}
```

## Cache Options

```csharp
namespace cache;

internal class CacheOptionsExample
{
    internal Cache Build()
    {
        CacheOptions options = new CacheOptions();
        options.Name = "users";
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 600000000L;
        options.HasMaximumEntries = true;
        options.MaximumEntries = 1000L;
        options.RecordStats = true;

        return new Cache(options);
    }
}
```

## Per-Entry Options

```csharp
namespace cache;

internal class CacheEntryOptionsExample
{
    internal void Run()
    {
        Cache cache = new Cache(new CacheOptions());

        CacheEntryOptions options = new CacheEntryOptions();
        options.HasTimeToLive = true;
        options.TimeToLiveTicks = 300000000L;
        options.Priority = CacheEntryPriority.High;
        options.Weight = 2L;

        cache.PutEntry("page:/home", "<html></html>", options);
        CacheGetResult result = cache.Get("page:/home");
    }
}
```

## Expiration Timing

Expiration uses ticks from the cache clock. System caches use UTC ticks from the
system clock. Tests and deterministic callers can pass a manual `CacheClock`.

An entry is expired when the current clock value is greater than or equal to its
expiration boundary.

- Time-to-live expires at write time plus `TimeToLiveTicks`.
- Sliding expiration expires at last access time plus `SlidingExpirationTicks`.
- Absolute expiration expires at `AbsoluteExpirationUtcTicks`.
- If fixed and sliding expiration both apply, the earliest boundary wins.
- A zero tick duration is valid and expires immediately at the boundary.

```csharp
namespace cache;

internal class ExpirationTimingExample
{
    internal void Run()
    {
        CacheClock clock = new CacheClock();
        clock.SetUtcTicks(100L);
        CacheOptions options = new CacheOptions();
        options.Clock = clock;
        Cache cache = new Cache(options);

        CacheEntryOptions ttl = new CacheEntryOptions();
        ttl.HasTimeToLive = true;
        ttl.TimeToLiveTicks = 10L;
        cache.PutEntry("ttl", "one", ttl);

        CacheEntryOptions idle = new CacheEntryOptions();
        idle.HasSlidingExpiration = true;
        idle.SlidingExpirationTicks = 20L;
        cache.PutEntry("idle", "two", idle);

        CacheEntryOptions fixedExpiration = new CacheEntryOptions();
        fixedExpiration.HasAbsoluteExpiration = true;
        fixedExpiration.AbsoluteExpirationUtcTicks = 150L;
        cache.PutEntry("fixed", "three", fixedExpiration);

        clock.AdvanceTicks(9L);
        CacheGetResult beforeBoundary = cache.Get("ttl");
        clock.AdvanceTicks(1L);
        CacheGetResult atBoundary = cache.Get("ttl");
    }
}
```

## Lazy Cleanup

Normal reads never return expired values. `Get`, `Peek`, `Snapshot`, `Put`, and
`PutEntry` perform lazy maintenance as part of their normal work when they find
expired entries. Low-traffic caches can call `Cleanup()` directly to remove
expired entries without waiting for another read or write.

```csharp
namespace cache;

internal class LazyCleanupExample
{
    internal CacheMaintenanceResult Run()
    {
        CacheClock clock = new CacheClock();
        clock.SetUtcTicks(200L);
        CacheOptions options = new CacheOptions();
        options.Clock = clock;
        Cache cache = new Cache(options);

        CacheEntryOptions entryOptions = new CacheEntryOptions();
        entryOptions.HasTimeToLive = true;
        entryOptions.TimeToLiveTicks = 5L;
        cache.PutEntry("expired", "one", entryOptions);
        clock.AdvanceTicks(5L);

        return cache.Cleanup();
    }
}
```

## Optional Scheduled Cleanup

Scheduled cleanup is disabled by default. Enable it with `HasCleanupInterval`
and `CleanupIntervalTicks`, or use `CacheBuilder.WithCleanupInterval`. The timer
calls `Cleanup()` periodically. Call `Stop()` when the cache no longer needs
scheduled cleanup.

```csharp
namespace cache;

internal class ScheduledCleanupExample
{
    internal Cache BuildWithOptions()
    {
        CacheOptions options = new CacheOptions();
        options.HasDefaultTimeToLive = true;
        options.DefaultTimeToLiveTicks = 600000000L;
        options.HasCleanupInterval = true;
        options.CleanupIntervalTicks = 10000000L;

        return new Cache(options);
    }

    internal Cache BuildWithBuilder()
    {
        CacheBuilder builder = new CacheBuilder();
        return builder
            .WithTimeToLive(600000000L)
            .WithCleanupInterval(10000000L)
            .Build();
    }

    internal void Stop(Cache cache)
    {
        cache.Stop();
    }
}
```

## Callback Exception Behavior

The cache calls user callbacks directly. Loader, weigher, and removal listener
exceptions are not caught by the library. For direct operations, a callback
exception propagates to the caller. During scheduled cleanup, a removal-listener
exception escapes the timer callback.

Removal notifications are delivered after the cache has already updated its
internal state for the removal. Each removal listener receives a
`CacheNotification`. Keep callbacks short, deterministic, and exception-free
when callers should not observe callback failures.

```csharp
namespace cache;

internal class RemovalCallbackExample
{
    internal Cache Build()
    {
        CacheOptions options = new CacheOptions();
        options.RemovalListener = notification =>
        {
            object key = notification.Key;
            CacheRemovalReason reason = notification.Reason;
            long removedAt = notification.RemovedUtcTicks;
        };

        return new Cache(options);
    }
}
```

## Loading

```csharp
namespace cache;

internal class LoadingCacheExample
{
    internal Cache Build()
    {
        CacheOptions options = new CacheOptions();
        options.CacheNullLoaderValues = true;
        options.Loader = key =>
        {
            return "loaded-" + key.ToString();
        };

        return new Cache(options);
    }

    internal object Read(Cache cache)
    {
        CacheGetResult result = cache.GetOrLoad("product:1");
        return result.Value;
    }
}
```

## Builder

```csharp
namespace cache;

internal class CacheBuilderExample
{
    internal Cache Build()
    {
        CacheBuilder builder = new CacheBuilder();
        return builder
            .WithName("products")
            .WithMaximumEntries(500L)
            .WithTimeToLive(600000000L)
            .WithStats()
            .WithLoader(key =>
            {
                return "loaded-" + key.ToString();
            })
            .Build();
    }
}
```

## Stats, Policy, And Snapshot

```csharp
namespace cache;

internal class CacheInspectionExample
{
    internal void Run()
    {
        CacheOptions options = new CacheOptions();
        options.RecordStats = true;
        Cache cache = new Cache(options);

        cache.Put("a", "one");
        cache.Get("a");

        CacheStatsSnapshot stats = cache.Stats();
        CachePolicySnapshot policy = cache.Policy();
        CacheSnapshot snapshot = cache.Snapshot();

        long hits = stats.Hits;
        long maximumEntries = policy.MaximumEntries;
        long count = snapshot.Count;
    }
}
```

## Memoizer

```csharp
namespace cache;

internal class MemoizerExample
{
    internal object Run()
    {
        Cache cache = new Cache(new CacheOptions());
        Memoizer memoizer = new Memoizer(
            cache,
            argument =>
            {
                return "computed-" + argument.ToString();
            });

        object first = memoizer.Invoke("a");
        object second = memoizer.Invoke("a");
        return second;
    }
}
```
