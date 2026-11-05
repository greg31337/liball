# Universal Cache Library Development Plan

## Goal

Build a dependency-free .NET cache library for in-process caching, expiration policy evaluation, eviction, cache loading, and observability. The implementation must use only built-in .NET libraries and must conform to the simplified C# subset in `syntax.md`.

## Implementation Constraints From `syntax.md`

- Source must use file-scoped namespaces, classes, enums, fields, constructors, and block-bodied methods.
- Interfaces may contain instance methods only and cannot inherit from other interfaces. Classes may implement interfaces. Do not use structs, records, declared delegates, properties, indexers, events, attributes, other inheritance, overrides, extension methods, LINQ, `try`/`catch`, `await`, `params`, `out`, `ref`, named arguments, default parameter values, local functions, or generic type/method declarations.
- Only `static`, `public`, `internal`, and `const` modifiers are permitted. Public types and members define the supported API layer; all implementation-only types and members must be `internal`.
- Generic .NET library types are allowed, so internal fields can use types such as `Dictionary<object, CacheEntry>`, `List<CacheEntry>`, `Func<object, object>`, and `Action<CacheNotification>`.
- Public APIs should use ordinary classes, object keys, object values, public fields for option/result data, and result objects instead of `out` parameters. Nullable reference annotations should describe optional inputs, callbacks, and results; nullable value-type forms remain prohibited.
- Every public type, constructor, method, field, enum, and enum member requires plain-English XML documentation. Documentation may use only `summary`, `remarks`, `param`, `returns`, `exception`, `example`, `see`, `seealso`, `para`, and `c` elements.
- Internal implementation symbols should not have XML documentation.
- Callback behavior must be simple and documented because callback exceptions cannot be caught inside the library under the allowed syntax.

## Research Notes

- Caffeine: builder-driven cache with optional loading, async loading, maximum size/weight, frequency-and-recency eviction, expire-after-write, expire-after-access, custom per-entry expiry, refresh-after-write, weak/soft references, removal notifications, writer integration, statistics, policy inspection, and explicit cleanup/scheduler support. Sources: https://github.com/ben-manes/caffeine and https://javadoc.io/static/com.github.ben-manes.caffeine/caffeine/3.1.8/com.github.benmanes.caffeine/com/github/benmanes/caffeine/cache/Caffeine.html
- Guava Cache: optional loading cache, LRU size eviction, weight eviction, expire-after-write, expire-after-access, refresh-after-write, weak/soft references, removal listener, stats, explicit cleanup, configurable ticker/time source, and string specs. Source: https://guava.dev/releases/17.0/api/docs/com/google/common/cache/CacheBuilder.html
- Microsoft.Extensions.Caching.Memory: in-memory cache with absolute expiration, sliding expiration, expiration tokens, priority, size, eviction callbacks, and statistics in newer versions. Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/caching
- System.Runtime.Caching.MemoryCache: absolute expiration, sliding expiration, update/removal callbacks, change monitors, and set/add/remove semantics. Source: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache.set
- Python cachetools: fixed-size mutable mapping caches, FIFO/LFU/LRU/RR algorithms, TTLCache combining TTL with LRU fallback, custom timer, get-size callback, memoization helpers, cache info, and clear operations. Source: https://cachetools.readthedocs.io/en/stable/
- Python functools.lru_cache: bounded LRU memoization, optional unbounded mode, typed keys, cache info, cache clear, and explicit parameter inspection. Source: https://docs.python.org/3.9/library/functools.html
- Ehcache: cache-level TTL, time-to-idle, no-expiry, custom per-entry expiry for create/read/update, event listeners, resource pools, writers, and JSR-107 compatibility. Sources: https://www.ehcache.org/documentation/3.0/ and https://www.ehcache.org/documentation/3.1/expiry.html
- Moka: thread-safe cache with size and weight capacity, TTL, TTI, per-entry expiry, eviction listener, explicit pending maintenance, concurrent operations, sync and async variants, and policy maintenance queues. Source: https://docs.rs/moka/latest/moka/sync/struct.Cache.html
- .NET interface guidance: abstractions should be implemented and consumed, should remain as small as their role permits, and can isolate dependencies for testing. `IMemoryCache` demonstrates a public cache interface backed by a concrete implementation. Sources: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/abstractions-abstract-types-and-interfaces and https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache

## Necessary Features, Low Level To High Level

- Time source abstraction with system time and manually advanced fake time for deterministic tests.
- Entry metadata: key, value, created time, last access time, last write time, absolute expiration time, idle expiration duration, weight, priority, version, and removal reason.
- Stable cache result objects for hits, misses, expired entries, loaded values, rejected writes, and removed entries.
- Key comparison support through built-in `IEqualityComparer<object>`.
- Basic store operations: add, put, replace, get, peek without access tracking, contains, remove, clear, count, and snapshot keys.
- Expiration primitives: no expiration, absolute expiration at a point in time, relative TTL after write, sliding expiration after access, and immediate expiration.
- Per-entry options overriding cache defaults.
- Expiration evaluation that never returns expired values from normal reads.
- Lazy maintenance on reads/writes plus explicit cleanup for low-traffic caches.
- Optional scheduled cleanup using built-in timers for users who need prompt expiration.
- Removal reason enum: explicit remove, replace, expired, capacity, clear, rejected, and unknown.
- Removal notification callback with key, value, reason, weight, and timestamps.
- Capacity by entry count.
- Capacity by total weight.
- Weight calculation callback, with default weight of 1.
- Eviction policy enum: FIFO, LRU, LFU, random replacement, and segmented recency/frequency.
- Critical first eviction policy: LRU for predictable behavior and simple implementation.
- Priority support so low-priority entries are evicted before normal/high entries.
- Stats counters: hits, misses, puts, updates, removes, expirations, evictions, clears, load successes, load failures, total load duration ticks, and current size/weight.
- Stats snapshot object rather than live mutable counters.
- Loader callback for read-through cache misses.
- Single-flight loading option so one key has at most one in-progress load.
- Negative caching option for known missing values.
- Refresh-after-write metadata and APIs, with synchronous refresh first.
- Stale-while-refresh behavior as a later feature.
- Bulk operations: get many, put many, remove many, clear expired.
- Policy inspection: maximum entries, maximum weight, active expiration policy, stats enabled, cleanup mode, current count, current weight, oldest entries, hottest entries, and entries expiring soon.
- Dynamic policy tuning for capacity and default expiration durations.
- Memoization wrapper around `Func<object, object>` using cache keys built from argument objects.
- Names for cache instances to make diagnostics and notifications easier.
- Disposal/stop support for background timers.
- Thread safety for all public operations using built-in locks and collections.
- Deterministic tests for expiration, eviction order, stats, callbacks, and syntax conformance.
- Documentation that explains expiration timing, lazy cleanup, callback exception behavior, and object-key/object-value API tradeoffs.

## Critical Must-Have Feature Set

- Thread-safe in-memory cache.
- Object-key/object-value public API compatible with simplified C#.
- Default no-expiration cache behavior.
- Per-entry and default TTL expiration.
- Per-entry and default sliding idle expiration.
- Absolute expiration.
- Explicit remove and clear.
- Manual cleanup of expired entries.
- Maximum entry count with LRU eviction.
- Removal reasons and removal notifications.
- Optional stats recording and stats snapshot.
- Injectable clock for deterministic expiration tests.
- Loader callback for cache-aside/read-through miss handling.
- No external runtime dependencies.

## Current Public API Coverage Review

The public layer already covers the critical core: thread-safe object-key/object-value storage; default, per-entry, absolute, write-relative, and sliding expiration; lazy, explicit, and scheduled cleanup; count- and weight-based capacity; priority-aware LRU, FIFO, LFU, and random eviction; removal notifications; optional statistics; injectable and adjustable clocks; synchronous loading with per-key single-flight coordination and negative caching; explicit refresh-after-write; policy and content snapshots; a builder; and memoization.

The planned surface is not complete in these areas:

- Basic operations do not expose distinct add-if-absent, replace-if-present, contains, or direct count operations.
- Bulk get, put, and remove operations are absent; `Cleanup` already provides the planned clear-expired operation.
- `CacheEvictionPolicy.Segmented` currently falls back to LRU instead of implementing segmented recency/frequency behavior.
- `CacheStatsSnapshot.LoadFailures` is exposed but loader failures are never recorded.
- Refresh is explicit and synchronous; stale-while-refresh behavior is absent.
- Capacity and default expiration settings cannot be tuned after construction.
- Cache names appear in policy diagnostics but not in removal notifications.
- The builder has no direct eviction-policy selector.
- The existing 222 tests pass, but targeted public edge paths remain uncovered, including null and expired refresh checks, builder option/comparer paths, automatic-to-manual clock advancement, and null memoizer arguments.

## Public API Completion Tasks

- [DONE] T001: Add thread-safe add-if-absent, replace-if-present, contains-without-access, and direct live-count operations to the public cache contract and built-in cache implementation.
- [DONE] T002: Add tests for the new basic operations covering hits, misses, expired entries, null inputs, custom key comparers, result states, statistics, notifications, capacity enforcement, and atomic behavior under contention.
- [DONE] T003: Add public bulk request and result data classes plus bulk get, put, and remove methods, using `ReadOnlySpan<T>` inputs where this keeps the non-generic object API simple and returning per-item outcomes in input order.
- [DONE] T004: Implement bulk operations with built-in .NET collections and synchronization, defining duplicate-key, null-item, partial-failure, expiration, access-tracking, capacity, statistics, and notification behavior.
- [DONE] T005: Add complete bulk-operation tests for empty and mixed inputs, duplicate and equivalent keys, per-entry options, expired and null values, ordering, partial failures, callbacks, counters, and concurrent overlap with scalar operations.
- [DONE] T006: Add the normalized cache name to removal notifications and populate it consistently for replace, explicit remove, expiration, capacity eviction, and clear operations.
- [DONE] T007: Add tests proving names are copied into policy snapshots and every notification reason without retaining later mutations to the original options object.
- [DONE] T008: Record loader failures and failed-load durations for both miss loading and explicit refresh while preserving exception propagation and per-key single-flight cleanup.
- [DONE] T009: Add statistics tests covering successful, null, rejected, throwing, concurrent single-flight, and refresh loads, including saturated counters, total durations, and load-success-rate calculations.
- [DONE] T010: Add a direct eviction-policy selector to the public builder contract and implementation, including normalization consistent with `CacheOptions` and `CachePolicySnapshot`.
- [DONE] T011: Replace the segmented-policy LRU fallback with a deterministic segmented recency/frequency policy based on the bundled W-TinyLFU references, using only built-in .NET collections and primitives.
- [DONE] T012: Add segmented-policy tests for admission, probation and protected promotion, frequency aging, recency adaptation, priority tiers, count and weight limits, replacement, expiration, deterministic tie-breaking, statistics, and notifications.
- [DONE] T013: Add public stale-while-refresh configuration and result state so a refresh-eligible live entry can be returned while one background refresh for its key is in progress.
- [DONE] T014: Implement stale-while-refresh with built-in task primitives, preserving the old value on loader failure or non-cacheable null, atomically publishing successful replacements, and making `Stop` prevent new background refresh work.
- [DONE] T015: Add deterministic stale-while-refresh tests for boundaries, stale hits, successful and failed refreshes, null caching, single-flight concurrency, expiration during refresh, removal or replacement races, callbacks, statistics, and stop behavior.
- [DONE] T016: Add a public policy-update data class and cache operation for changing maximum entries, maximum weight, and default absolute, write-relative, and sliding expiration settings after construction.
- [DONE] T017: Implement policy updates atomically so reduced capacities are enforced immediately, updated defaults affect later writes, existing entry expiration metadata remains stable, and policy snapshots reflect normalized settings.
- [DONE] T018: Add policy-update tests for enabling, disabling, increasing, decreasing, and combining limits and expiration defaults, including eviction order, overflow boundaries, callbacks, statistics, snapshots, and concurrent reads and writes.
- [DONE] T019: Add targeted tests for all remaining public edge behavior, including null-key load and refresh calls, missing and expired refresh checks, rejected loader writes, builder option reset and custom comparer paths, advancing a system-mode clock into manual mode, and null memoizer arguments.
- [DONE] T020: Add a public-surface coverage matrix that exercises every interface method, concrete public method, option field, result state, enum value, callback path, and interaction introduced by these tasks, without syntax-policy, source-checking, benchmark, documentation, or release tests.
