Terminology:
* **eviction** means removal due to the policy
* **invalidation** means manual removal by the caller
* **removal** occurs as a consequence of invalidation or eviction

### Explicit Removals
At any time, you may explicitly invalidate cache entries rather than waiting for entries to be evicted.

```java
// individual key
cache.invalidate(key)
// bulk keys
cache.invalidateAll(keys)
// all keys
cache.invalidateAll()
```

### Removal Listeners
```java
Cache<Key, Graph> graphs = Caffeine.newBuilder()
    .evictionListener((Key key, Graph graph, RemovalCause cause) ->
        System.out.printf("Key %s was evicted (%s)%n", key, cause))
    .removalListener((Key key, Graph graph, RemovalCause cause) ->
        System.out.printf("Key %s was removed (%s)%n", key, cause))
    .build();
```

You may specify a removal listener for your cache to perform some operation when an entry is removed, via `Caffeine.removalListener(RemovalListener)`. These operations are executed asynchronously using an [Executor][2], where the default executor is [ForkJoinPool.commonPool()][3] and can be overridden via `Caffeine.executor(Executor)`.

When the operation must be performed synchronously with eviction, use `Caffeine.evictionListener(RemovalListener)` instead. This listener will only be notified when `RemovalCause.wasEvicted()` is true. For an explicit removal, `Cache.asMap()` offers compute methods that are performed atomically.   

Note that any exceptions thrown by the `RemovalListener` are logged (using [System.Logger][1]) and swallowed.

[1]: https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/lang/System.Logger.html
[2]: http://docs.oracle.com/javase/8/docs/api/java/util/concurrent/Executor.html
[3]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/ForkJoinPool.html