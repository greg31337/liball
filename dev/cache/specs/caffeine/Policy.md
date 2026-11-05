The policies that the cache supports are fixed at construction. At runtime this configuration can be inspected and adjusted. The policies are obtained through an [Optional][1] to indicate whether the feature is supported by the cache.

### Size-based
```java
cache.policy().eviction().ifPresent(eviction -> {
  eviction.setMaximum(2 * eviction.getMaximum());
});
```

If the cache is bounded by a maximum weight, the current weight can be obtained using `weightedSize()`. This differs from `Cache.estimatedSize()` which reports the number of entries present.

The maximum size or weight can be read from `getMaximum()` and adjusted using `setMaximum(long)`. The cache will evict until it is within the new threshold.

If the subset of entries most likely to be kept or evicted is needed, the `hottest(int)` and `coldest(int)` methods provide an ordered snapshot of the entries.

### Time-based
```java
cache.policy().expireAfterAccess().ifPresent(expiration -> ...);
cache.policy().expireAfterWrite().ifPresent(expiration -> ...);
cache.policy().expireVariably().ifPresent(expiration -> ...);
cache.policy().refreshAfterWrite().ifPresent(refresh -> ...);
```

The `ageOf(key, TimeUnit)` provides how long an entry has been idle from the perspective of the `expireAfterAccess`, `expireAfterWrite`, or `refreshAfterWrite` policy. The maximum duration can be read from `getExpiresAfter(TimeUnit)` and adjusted using `setExpiresAfter(long, TimeUnit)`.

If the subset of entries most likely to be retained or expired is needed, the `youngest(int)` and `oldest(int)` methods provide an ordered snapshot of the entries.

[1]: https://docs.oracle.com/javase/8/docs/api/java/util/Optional.html