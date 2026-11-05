By default, Caffeine does not perform cleanup and evict values "automatically" or instantly after a value expires. Instead, it performs small amounts of maintenance work after write operations or occasionally after read operations if writes are rare. If your cache is high-throughput then you don't have to worry about performing cache maintenance to clean up expired entries and the like. If your cache is read and written to rarely, you may wish to leverage an external thread, as described below, that calls `Cache.cleanUp()` when appropriate.

```java
LoadingCache<Key, Graph> graphs = Caffeine.newBuilder()
    .scheduler(Scheduler.systemScheduler())
    .expireAfterWrite(10, TimeUnit.MINUTES)
    .build(key -> createExpensiveGraph(key));
```

A `Scheduler` may be provided to enable prompt removal of expired entries. The scheduling between expiration events is paced to exploit batching and to minimize executions in short succession. The scheduling is best-effort and does not make any hard guarantees of when an expired entry will be removed. Java 9+ users may prefer using `Scheduler.systemScheduler()` to leverage the dedicated, system-wide scheduling thread.

```java
Cache<Key, Graph> graphs = Caffeine.newBuilder().weakValues().build();
Cleaner cleaner = Cleaner.create();

cleaner.register(graph, graphs::cleanUp);
graphs.put(key, graph);
```

A [Cleaner][cleaner] may be used in Java 9+ to enable prompt removal of reference-based entries (if using `weakKeys`, `weakValues`, or `softValues`). Simply register the key or value with the `Cleaner`, where the runnable action invokes `Cache.cleanUp()` to trigger the maintenance routine.

[cleaner]: https://docs.oracle.com/javase/9/docs/api/java/lang/ref/Cleaner.html