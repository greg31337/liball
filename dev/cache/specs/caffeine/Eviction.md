Caffeine provides three types of eviction: size-based eviction, time-based eviction, and reference-based eviction.

### Size-based
```java
// Evict based on the number of entries in the cache
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .maximumSize(10_000)
    .build(key -> createExpensiveGraph(key));

// Evict based on the number of vertices in the cache
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .maximumWeight(10_000)
    .weigher((Key key, Graph graph) -> graph.vertices().size())
    .build(key -> createExpensiveGraph(key));
```

If your cache should not grow beyond a certain size, use `Caffeine.maximumSize(long)`. The cache will try to evict entries that have not been used [recently or very often](Efficiency).

Alternately, if different cache entries have different "weights" -- for example, if your cache values have radically different memory footprints -- you may specify a weight function with `Caffeine.weigher(Weigher)` and a maximum cache weight with `Caffeine.maximumWeight(long)`. In addition to the same caveats as `maximumSize` requires, be aware that weights are computed at entry creation and update time, are static thereafter, and that relative weights are not used when making eviction selections.

### Time-based
```java
// Evict based on a fixed expiration policy
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .expireAfterAccess(5, TimeUnit.MINUTES)
    .build(key -> createExpensiveGraph(key));
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .expireAfterWrite(10, TimeUnit.MINUTES)
    .build(key -> createExpensiveGraph(key));

// Evict based on a varying expiration policy
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .expireAfter(Expiry.creating((Key key, Graph graph) -> 
        Duration.between(Instant.now(), graph.creationDate().plusHours(5))))
    .build(key -> createExpensiveGraph(key));
```

Caffeine provides three approaches to timed eviction:
 * `expireAfterAccess(long, TimeUnit):` Expire entries after the specified duration has passed since the entry was last accessed by a read or a write. This could be desirable if the cached data is bound to a session and expires due to inactivity.
 * `expireAfterWrite(long, TimeUnit):` Expire entries after the specified duration has passed since the entry was created, or the most recent replacement of the value. This could be desirable if cached data grows stale after a certain amount of time.
 * `expireAfter(Expiry):` Expires entries after the variable duration has passed. This could be desirable if the expiration time of an entry is determined by an external resource.

Expiration is performed with periodic maintenance during writes and occasionally during reads. Scheduling and firing of an expiration event is executed in amortized O(1) time.

For prompt expiration, rather than relying on other cache activity to trigger routine maintenance, use the `Scheduler` interface and the `Caffeine.scheduler(Scheduler)` method to specify a scheduling thread in your cache builder. Java 9+ users may prefer using `Scheduler.systemScheduler()` to leverage the dedicated, system-wide scheduling thread.

Testing timed eviction does not require that tests wait until the wall clock time has elapsed. Use the `Ticker` interface and the `Caffeine.ticker(Ticker)` method to specify a time source in your cache builder, rather than having to wait for the system clock. Guava's testlib provides a convenient `FakeTicker` for this purpose.

### Reference-based
```java
// Evict when neither the key nor value are strongly reachable
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .weakKeys()
    .weakValues()
    .build(key -> createExpensiveGraph(key));

// Evict when the garbage collector needs to free memory
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .softValues()
    .build(key -> createExpensiveGraph(key));
```

Caffeine allows you to set up your cache to allow the garbage collection of entries, by using weak references for keys or values, and by using soft references for values. Note that weak and soft value references are not supported by `AsyncCache`.

`Caffeine.weakKeys()` stores keys using weak references. This allows entries to be garbage-collected if there are no other strong references to the keys. Since garbage collection depends only on identity equality, this causes the whole cache to use identity (==) equality to compare keys, instead of `equals()`.

`Caffeine.weakValues()` stores values using weak references. This allows entries to be garbage-collected if there are no other strong references to the values. Since garbage collection depends only on identity equality, this causes the whole cache to use identity (==) equality to compare values, instead of `equals()`.

`Caffeine.softValues()` stores values using soft references. Softly referenced objects are garbage-collected in a globally least-recently-used manner, in response to memory demand. Because of the performance implications of using soft references, we generally recommend using the more predictable maximum cache size instead. Use of `softValues()` will cause values to be compared using identity (==) equality instead of `equals()`.