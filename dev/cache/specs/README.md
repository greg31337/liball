# Cache library reference set

Retrieved on 2026-08-24. These documents are design and behavior references for an in-process object cache; they are not runtime dependencies.

No HTTP caching RFC is included because HTTP freshness, validation, and intermediary semantics are outside this library's stated in-process object-cache scope.

## Sources

- `caffeine/`: Caffeine's official wiki pages for eviction, design, population/loading, refresh, statistics, cleanup, removal notifications, and runtime policy inspection. Source: <https://github.com/ben-manes/caffeine/wiki>.
- `guava/CachesExplained.md`: Guava's official cache guide. Source: <https://github.com/google/guava/wiki/CachesExplained>.
- `dotnet/caching.md`: Microsoft's .NET caching overview. Source: <https://learn.microsoft.com/dotnet/core/extensions/caching>.
- `dotnet/*.xml`: Microsoft's API descriptions for `System.Runtime.Caching.MemoryCache`, `CacheItemPolicy`, `Microsoft.Extensions.Caching.Memory.MemoryCache`, and `MemoryCacheOptions`. Source: <https://github.com/dotnet/dotnet-api-docs>.
- `cachetools/index.rst`: cachetools' official cache and memoization documentation. Source: <https://cachetools.readthedocs.io/>.
- `python/functools.rst`: CPython's official `functools` documentation source, including `cache` and `lru_cache`. Source: <https://docs.python.org/3/library/functools.html>.
- `ehcache/expiry.adoc`: Ehcache's official expiry documentation source. Source: <https://www.ehcache.org/documentation/3.10/expiry.html>.
- `moka/README.md`: Moka's official feature and expiration overview. Source: <https://github.com/moka-rs/moka>.
- `jsr107/README.md`: The JCache/JSR 107 specification project's overview and links to its API specification. Source: <https://github.com/jsr107/jsr107spec> and <https://www.jcp.org/en/jsr/detail?id=107>.
- `papers/tinylfu.pdf`: Einziger, Friedman, and Manes, “TinyLFU: A Highly Efficient Cache Admission Policy,” including W-TinyLFU. Source: <https://arxiv.org/abs/1512.00727>.
