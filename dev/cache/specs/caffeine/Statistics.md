```java
Cache<Key, Graph> graphs = Caffeine.newBuilder()
    .maximumSize(10_000)
    .recordStats()
    .build();
```

By using `Caffeine.recordStats()`, you can turn on statistics collection. The `Cache.stats()` method returns a `CacheStats` which provides statistics such as
 * `hitRate():` returns the ratio of hits to requests
 * `evictionCount():` the number of cache evictions
 * `averageLoadPenalty():` the average time spent loading new values

These statistics are critical in cache tuning and we advise keeping an eye on these statistics in performance-critical applications.

The cache statistics can be integrated with a reporting system using either a pull or push based approach. A pull-based approach periodically calls `Cache.stats()` and records the latest snapshot. A push-based approach supplies a custom `StatsCounter` so that the metrics are updated directly during the cache operations.

See [metrics-caffeine][metrics-caffeine] if using [Dropwizard Metrics][metrics].

Try [prometheus-java][prometheus-java] if using [Prometheus][prometheus].

Can't choose? Try the [Micrometer][micrometer] integration.

[metrics]: http://metrics.dropwizard.io
[metrics-caffeine]: https://metrics.dropwizard.io/4.2.0/manual/caffeine.html
[prometheus-java]: https://prometheus.github.io/client_java/api/io/prometheus/metrics/instrumentation/caffeine/CacheMetricsCollector.html
[prometheus]: https://prometheus.github.io/client_java
[micrometer]: https://docs.micrometer.io/micrometer/reference/reference/cache.html#cache-caffeine
