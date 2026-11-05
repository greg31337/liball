Caffeine provides four types of population strategies: manual, loading synchronously, and asynchronous variants.

### Manual

```java
Cache<Key, Graph> cache = Caffeine.newBuilder()
    .expireAfterWrite(10, TimeUnit.MINUTES)
    .maximumSize(10_000)
    .build();

// Lookup an entry, or null if not found
Graph graph = cache.getIfPresent(key);
// Lookup and compute an entry if absent, or null if not computable
graph = cache.get(key, k -> createExpensiveGraph(key));
// Insert or update an entry
cache.put(key, graph);
// Remove an entry
cache.invalidate(key);
```

The `Cache` interface allows for explicit control of retrieving, updating, and invalidating entries.

Prefer using `cache.get(key, k -> value)` to atomically compute and insert the value into the cache if absent, or else return the existing value. A `cache.put(key, value)` may be used to directly insert or update an entry, overwriting any previous value in the cache for the specified key. Note that a `cache.get` may return `null` if the entry was not computable or throw an exception if the computation failed.

Changes can also be made to the cache using any of the [ConcurrentMap][concurrent-map] methods exposed by the `Cache.asMap()` view.

### Loading

```java
LoadingCache<Key, Graph> cache = Caffeine.newBuilder()
    .maximumSize(10_000)
    .expireAfterWrite(10, TimeUnit.MINUTES)
    .build(key -> createExpensiveGraph(key));

// Lookup and compute an entry if absent, or null if not computable
Graph graph = cache.get(key);
// Lookup and compute entries that are absent
Map<Key, Graph> graphs = cache.getAll(keys);
```

A `LoadingCache` is a `Cache` built with an attached `CacheLoader`. 

Bulk lookups can be performed with the method `getAll`. By default, `getAll` will issue a separate call to `CacheLoader.load` for each key which is absent from the cache. When bulk retrieval is more efficient than many individual lookups, you can override `CacheLoader.loadAll` to exploit this.

Note that you can write a `CacheLoader.loadAll` implementation that loads values for keys that were not specifically requested. For example, if computing the value of any key from some group gives you the value for all keys in the group, `loadAll` might load the rest of the group at the same time.

### Asynchronous (Manual)
```java
AsyncCache<Key, Graph> cache = Caffeine.newBuilder()
    .expireAfterWrite(10, TimeUnit.MINUTES)
    .maximumSize(10_000)
    .buildAsync();

// Lookup an entry, or null if not found
CompletableFuture<Graph> graph = cache.getIfPresent(key);
// Lookup and asynchronously compute an entry if absent
graph = cache.get(key, k -> createExpensiveGraph(key));
// Insert or update an entry
cache.put(key, graph);
// Remove an entry
cache.synchronous().invalidate(key);
```

A `AsyncCache` is a `Cache` variant that computes entries on an [Executor][executor] and returns a [CompletableFuture][future]. This allows utilizing caches with the popular reactive programming model.

The `synchronous()` view provides a `Cache` that blocks until the asynchronous computation completes.

Changes can also be made to the cache using any of the [ConcurrentMap][concurrent-map] methods exposed by the `AsyncCache.asMap()` view.

The default executor is [ForkJoinPool.commonPool()][fork-join] and can be overridden via `Caffeine.executor(Executor)`.

### Asynchronously Loading

```java
AsyncLoadingCache<Key, Graph> cache = Caffeine.newBuilder()
    .maximumSize(10_000)
    .expireAfterWrite(10, TimeUnit.MINUTES)
    // Either: Build with a synchronous computation that is wrapped as asynchronous 
    .buildAsync(key -> createExpensiveGraph(key));
    // Or: Build with a asynchronous computation that returns a future
    .buildAsync((key, executor) -> createExpensiveGraphAsync(key, executor));

// Lookup and asynchronously compute an entry if absent
CompletableFuture<Graph> graph = cache.get(key);
// Lookup and asynchronously compute entries that are absent
CompletableFuture<Map<Key, Graph>> graphs = cache.getAll(keys);
```

A `AsyncLoadingCache` is a `AsyncCache` built with an attached `AsyncCacheLoader`. 

A `CacheLoader` should be supplied when the computation is best expressed in a synchronous fashion. Alternatively, a `AsyncCacheLoader` should be supplied when the computation is expressed asynchronously and returns a [CompletableFuture][future].

Bulk lookups can be performed with the method `getAll`. By default, `getAll` will issue a separate call to `AsyncCacheLoader.asyncLoad` for each key which is absent from the cache. When bulk retrieval is more efficient than many individual lookups, you can override `AsyncCacheLoader.asyncLoadAll` to exploit this.

Note that you can write a `AsyncCacheLoader.asyncLoadAll` implementation that loads values for keys that were not specifically requested. For example, if computing the value of any key from some group gives you the value for all keys in the group, `asyncLoadAll` might load the rest of the group at the same time.

[concurrent-map]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/ConcurrentMap.html
[executor]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/Executor.html
[fork-join]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/ForkJoinPool.html
[future]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/CompletableFuture.html