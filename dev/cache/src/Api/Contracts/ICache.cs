using System;

namespace cache;

/// <summary>
/// Defines the operations provided by an in-process cache.
/// </summary>
/// <remarks>
/// Use this contract when an application component should accept a cache without depending on the built-in <see cref="Cache"/> implementation.
/// </remarks>
public interface ICache
{
    /// <summary>Stores a key and value only when no live entry has an equal key.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A result describing the completed or rejected conditional write.</returns>
    CacheWriteResult Add(object? key, object? value);

    /// <summary>Stores a key and value using cache-level defaults.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A result describing the completed or rejected write.</returns>
    CacheWriteResult Put(object? key, object? value);

    /// <summary>Stores a key and value using explicit per-entry settings.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="options">Per-entry settings, or <c>null</c> to use cache-level defaults.</param>
    /// <returns>A result describing the completed or rejected write.</returns>
    CacheWriteResult PutEntry(object? key, object? value, CacheEntryOptions? options);

    /// <summary>Stores a sequence of requested entries in input order.</summary>
    /// <param name="requests">The entry requests to process.</param>
    /// <returns>Ordered per-request write outcomes.</returns>
    CacheBulkWriteResult PutMany(ReadOnlySpan<CachePutRequest?> requests);

    /// <summary>Stores a key and value only when a live entry has an equal key.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The replacement value.</param>
    /// <returns>A result describing the replacement, miss, expiration, or rejection.</returns>
    CacheWriteResult Replace(object? key, object? value);

    /// <summary>Gets a live entry and records an access.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    CacheGetResult Get(object? key);

    /// <summary>Gets a sequence of keys in input order and records each successful access.</summary>
    /// <param name="keys">The keys to look up.</param>
    /// <returns>Ordered per-key lookup outcomes.</returns>
    CacheBulkGetResult GetMany(ReadOnlySpan<object?> keys);

    /// <summary>Gets a live entry without recording an access.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    CacheGetResult Peek(object? key);

    /// <summary>Reports whether a live entry exists without recording an access.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns><c>true</c> when a live entry has an equal key; otherwise <c>false</c>.</returns>
    bool Contains(object? key);

    /// <summary>Counts the entries that are live at the time of the operation.</summary>
    /// <returns>The number of live entries.</returns>
    long Count();

    /// <summary>Gets a live entry or loads a missing value.</summary>
    /// <param name="key">The key to get or load.</param>
    /// <returns>The existing hit, loaded value, or miss.</returns>
    CacheGetResult GetOrLoad(object? key);

    /// <summary>Reports whether a live entry has reached its refresh boundary.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> when the entry is due for refresh; otherwise <c>false</c>.</returns>
    bool NeedsRefresh(object? key);

    /// <summary>Reloads and replaces an existing live entry.</summary>
    /// <param name="key">The key of the entry to refresh.</param>
    /// <returns>A snapshot describing the refresh outcome.</returns>
    CacheGetResult Refresh(object? key);

    /// <summary>Explicitly removes an entry by key.</summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A result describing the removal or miss.</returns>
    CacheRemoveResult Remove(object? key);

    /// <summary>Explicitly removes a sequence of keys in input order.</summary>
    /// <param name="keys">The keys to remove.</param>
    /// <returns>Ordered per-key removal outcomes.</returns>
    CacheBulkRemoveResult RemoveMany(ReadOnlySpan<object?> keys);

    /// <summary>Removes every currently stored entry.</summary>
    /// <returns>A result describing the completed maintenance operation.</returns>
    CacheMaintenanceResult Clear();

    /// <summary>Removes every entry whose expiration boundary has been reached.</summary>
    /// <returns>A result describing the completed maintenance operation.</returns>
    CacheMaintenanceResult Cleanup();

    /// <summary>Captures operation statistics and current capacity measurements.</summary>
    /// <returns>A point-in-time statistics snapshot.</returns>
    CacheStatsSnapshot Stats();

    /// <summary>Captures normalized policy settings and diagnostic key lists.</summary>
    /// <returns>A point-in-time policy snapshot.</returns>
    CachePolicySnapshot Policy();

    /// <summary>Changes selected capacity and default expiration policy settings.</summary>
    /// <param name="update">The requested changes, or <c>null</c> to leave every setting unchanged.</param>
    /// <returns>A policy snapshot after the operation.</returns>
    CachePolicySnapshot UpdatePolicy(CachePolicyUpdate? update);

    /// <summary>Copies the keys and values of all live entries.</summary>
    /// <returns>A point-in-time content snapshot.</returns>
    CacheSnapshot Snapshot();

    /// <summary>Stops scheduled cleanup without preventing ordinary cache operations.</summary>
    void Stop();
}
