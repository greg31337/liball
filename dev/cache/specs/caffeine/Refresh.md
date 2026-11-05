```java
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .maximumSize(10_000)
    .expireAfterWrite(Duration.ofMinutes(5))
    .refreshAfterWrite(Duration.ofMinutes(1))
    .build(key -> createExpensiveGraph(key));
```

Refreshing is not quite the same as eviction. As specified in `LoadingCache.refresh(K)`, refreshing a key loads a new value for the key asynchronously. The old value (if any) is still returned while the key is being refreshed, in contrast to eviction, which forces retrievals to wait until the value is loaded anew.

In contrast to `expireAfterWrite`, `refreshAfterWrite` will make a key eligible for refresh after the specified duration, but a refresh will only be actually initiated when the entry is queried. So, for example, you can specify both `refreshAfterWrite` and `expireAfterWrite` on the same cache so that the expiration timer on an entry isn't blindly reset whenever an entry becomes eligible for a refresh. If an entry isn't queried after it comes eligible for refreshing, it is allowed to expire.

A `CacheLoader` may specify smart behavior to use on a refresh by overriding `CacheLoader.reload(K, V)` which allows you to use the old value in computing the new value. The [coalescing-bulkloader-reactor](https://github.com/ben-manes/caffeine/tree/master/examples/coalescing-bulkloader-reactor) example demonstrates how to accumulated multiple reloads into a batch request by using [Reactor](https://projectreactor.io).

`LoadingCache.refresh(K)` can be used to explicitly refresh an entry and will deduplicate requests while they are in-flight. The returned future can be used to implement a fallback cache where the entry is reloaded to obtain the latest value from the source, but if that fails, the cached value is looked up and returned instead.

Refresh operations are executed asynchronously using an [Executor][2]. The default executor is [ForkJoinPool.commonPool()][3] and can be overridden via `Caffeine.executor(Executor)`.

If an exception is thrown while refreshing then the old value is kept and the exception is logged (using [System.Logger][1]) and swallowed.

[1]: https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/lang/System.Logger.html
[2]: http://docs.oracle.com/javase/8/docs/api/java/util/concurrent/Executor.html
[3]: https://docs.oracle.com/javase/8/docs/api/java/util/concurrent/ForkJoinPool.html