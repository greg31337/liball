using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace cache;

/// <summary>
/// Provides a thread-safe in-process cache with expiration, capacity eviction, loading, notifications, and optional statistics.
/// </summary>
/// <remarks>
/// Keys must be non-null. Values may be null only when the applicable entry or loader option permits them. Expired entries are never returned as hits, and removal callbacks run after the cache has updated its internal state.
/// </remarks>
public class Cache : ICache, ICacheStore
{
    internal object _sync;
    internal Dictionary<object, CacheEntry> _entries;
    internal Dictionary<object, Task<CacheGetResult>> _loads;
    internal Dictionary<object, Task> _refreshes;
    internal Dictionary<object, CacheEntry> _refreshEntries;
    internal CacheOptions _options;
    internal ICacheClock _clock;
    internal IEqualityComparer<object> _keyComparer;
    internal Func<object, object?>? _loader;
    internal Func<object, object?, long>? _weigher;
    internal Action<CacheNotification>? _removalListener;
    internal long _currentWeight;
    internal long _version;
    internal CacheStats _stats;
    internal Timer? _cleanupTimer;
    internal bool _stopped;
    internal Random _random;
    internal Dictionary<object, long> _frequencies;
    internal long _frequencyEvents;
    internal long _segmentedWindowHits;
    internal long _segmentedMainHits;
    internal long _segmentedAdaptationEvents;
    internal int _segmentedWindowPercent;

    /// <summary>Creates a cache from a normalized copy of the supplied options.</summary>
    /// <remarks>A <c>null</c> options value is treated the same as a default-constructed <see cref="CacheOptions"/>. If scheduled cleanup is enabled, construction also starts its timer.</remarks>
    /// <param name="options">The cache configuration, or <c>null</c> to use defaults.</param>
    public Cache(CacheOptions? options)
    {
        _sync = new object();
        _options = NormalizeOptions(options);
        _clock = NormalizeClock(_options.Clock);
        _keyComparer = NormalizeKeyComparer(_options.KeyComparer);
        _options.KeyComparer = _keyComparer;
        _entries = new Dictionary<object, CacheEntry>(_keyComparer);
        _loads = new Dictionary<object, Task<CacheGetResult>>(_keyComparer);
        _refreshes = new Dictionary<object, Task>(_keyComparer);
        _refreshEntries = new Dictionary<object, CacheEntry>(_keyComparer);
        _loader = _options.Loader;
        _weigher = _options.Weigher;
        _removalListener = _options.RemovalListener;
        _currentWeight = 0L;
        _version = 0L;
        _stats = new CacheStats();
        _cleanupTimer = null;
        _stopped = false;
        _random = new Random();
        _frequencies = new Dictionary<object, long>(_keyComparer);
        _frequencyEvents = 0L;
        _segmentedWindowHits = 0L;
        _segmentedMainHits = 0L;
        _segmentedAdaptationEvents = 0L;
        _segmentedWindowPercent = 20;
        StartCleanupTimer();
    }

    /// <summary>Creates a cache using the library defaults.</summary>
    /// <returns>A new unbounded, non-expiring cache with no callbacks, scheduled cleanup, or recorded statistics.</returns>
    internal static Cache Create()
    {
        return new Cache(null);
    }

    /// <summary>Stores a key and value only when no live entry has an equal key.</summary>
    /// <remarks>Expired entries are removed before the condition is evaluated. A rejected add does not replace, access, or notify for the existing entry.</remarks>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store. Null is accepted and is distinguished from a miss by <see cref="CacheGetResult.Found"/>.</param>
    /// <returns>A written result when the key was absent, or a rejected result when the key was null or already live.</returns>
    public CacheWriteResult Add(object? key, object? value)
    {
        CacheWriteResult result = new CacheWriteResult();
        result.Key = key;
        result.Value = value;
        List<CacheNotification> notifications = new List<CacheNotification>();

        if (key == null)
        {
            result.Status = CacheResultStatus.Rejected;
            result.Reason = CacheRemovalReason.Rejected;
            return result;
        }

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
            RecordExpirations(expiredCount);

            if (_entries.ContainsKey(key))
            {
                result.Status = CacheResultStatus.Rejected;
                result.Reason = CacheRemovalReason.Rejected;
            }
            else
            {
                CacheEntryOptions entryOptions = NormalizeEntryOptions(null);
                long version = NextVersion();
                CacheEntry entry = CreateEntry(key, value, entryOptions, nowUtcTicks, version);
                _entries[key] = entry;
                AddWeight(entry.Weight);
                result.Written = true;
                result.Status = CacheResultStatus.Written;
                result.Weight = entry.Weight;
                RecordPut();
                result.EvictedCount = EnforceMaximumEntries(notifications, nowUtcTicks);
                result.EvictedCount = result.EvictedCount + EnforceMaximumWeight(notifications, nowUtcTicks);
            }
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Stores a key and value using the cache-level defaults.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store. Null is accepted and is distinguished from a miss by <see cref="CacheGetResult.Found"/>.</param>
    /// <returns>A result describing whether the value was written, replaced an entry, or was rejected, including any capacity evictions.</returns>
    public CacheWriteResult Put(object? key, object? value)
    {
        return PutEntry(key, value, null);
    }

    /// <summary>Stores a key and value through the minimal cache-store contract.</summary>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A result describing the completed or rejected write.</returns>
    public CacheWriteResult Write(object? key, object? value)
    {
        return Put(key, value);
    }

    /// <summary>Stores a key and value using explicit per-entry settings.</summary>
    /// <remarks>Before writing, the cache removes all expired entries. A cache-level weigher overrides <see cref="CacheEntryOptions.Weight"/>, and capacity rules may immediately evict this or other entries.</remarks>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The value to store. Null is accepted unless explicit entry options disable <see cref="CacheEntryOptions.CacheNullValue"/>.</param>
    /// <param name="options">Per-entry settings, or <c>null</c> to use cache-level expiration and priority defaults with null-value storage enabled.</param>
    /// <returns>A result describing the write, replacement metadata, final weight, and number of capacity evictions.</returns>
    public CacheWriteResult PutEntry(object? key, object? value, CacheEntryOptions? options)
    {
        CacheWriteResult result = new CacheWriteResult();
        result.Key = key;
        result.Value = value;
        List<CacheNotification> notifications = new List<CacheNotification>();

        if (key == null)
        {
            result.Status = CacheResultStatus.Rejected;
            result.Reason = CacheRemovalReason.Rejected;
            return result;
        }

        CacheEntryOptions entryOptions = NormalizeEntryOptions(options);
        if (value == null && !entryOptions.CacheNullValue)
        {
            result.Status = CacheResultStatus.Rejected;
            result.Reason = CacheRemovalReason.Rejected;
            return result;
        }

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            long version = NextVersion();
            CacheEntry entry = CreateEntry(key, value, entryOptions, nowUtcTicks, version);
            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
            RecordExpirations(expiredCount);

            if (_entries.ContainsKey(key))
            {
                CacheEntry previous = _entries[key];
                result.Replaced = true;
                result.PreviousValue = previous.Value;
                result.PreviousWeight = previous.Weight;
                result.Status = CacheResultStatus.Replaced;
                result.Reason = CacheRemovalReason.Replace;
                _entries.Remove(key);
                RemoveWeight(previous.Weight);
                notifications.Add(CreateNotification(previous, CacheRemovalReason.Replace, nowUtcTicks));
                RecordUpdate();
            }
            else
            {
                result.Status = CacheResultStatus.Written;
                RecordPut();
            }

            _entries[key] = entry;
            AddWeight(entry.Weight);

            result.Written = true;
            result.Weight = entry.Weight;
            result.EvictedCount = EnforceMaximumEntries(notifications, nowUtcTicks);
            result.EvictedCount = result.EvictedCount + EnforceMaximumWeight(notifications, nowUtcTicks);
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Stores a sequence of requested entries in input order.</summary>
    /// <remarks>The batch holds the cache lock while all requests are processed. Duplicate keys observe earlier writes in the batch. Null requests, null keys, and disallowed null values produce individual rejected results without stopping later requests. Capacity is enforced after each successful write, and removal notifications run after the full batch leaves the lock. Callback exceptions propagate; a weigher exception stops later requests after preserving earlier state changes.</remarks>
    /// <param name="requests">The entry requests to process.</param>
    /// <returns>A result containing one write outcome for each input request at the matching index.</returns>
    public CacheBulkWriteResult PutMany(ReadOnlySpan<CachePutRequest?> requests)
    {
        CacheBulkWriteResult result = new CacheBulkWriteResult();
        result.Results = new CacheWriteResult[requests.Length];
        result.Count = requests.Length;
        List<CacheNotification> notifications = new List<CacheNotification>();
        int index = 0;

        lock (_sync)
        {
            while (index < requests.Length)
            {
                CachePutRequest? request = requests[index];
                CacheWriteResult writeResult = new CacheWriteResult();
                result.Results[index] = writeResult;

                if (request == null)
                {
                    writeResult.Status = CacheResultStatus.Rejected;
                    writeResult.Reason = CacheRemovalReason.Rejected;
                }
                else
                {
                    writeResult.Key = request.Key;
                    writeResult.Value = request.Value;

                    if (request.Key == null)
                    {
                        writeResult.Status = CacheResultStatus.Rejected;
                        writeResult.Reason = CacheRemovalReason.Rejected;
                    }
                    else
                    {
                        CacheEntryOptions entryOptions = NormalizeEntryOptions(request.Options);
                        if (request.Value == null && !entryOptions.CacheNullValue)
                        {
                            writeResult.Status = CacheResultStatus.Rejected;
                            writeResult.Reason = CacheRemovalReason.Rejected;
                        }
                        else
                        {
                            long nowUtcTicks = _clock.GetUtcTicks();
                            long version = NextVersion();
                            CacheEntry entry = CreateEntry(request.Key, request.Value, entryOptions, nowUtcTicks, version);
                            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
                            RecordExpirations(expiredCount);

                            if (_entries.ContainsKey(request.Key))
                            {
                                CacheEntry previous = _entries[request.Key];
                                writeResult.Replaced = true;
                                writeResult.PreviousValue = previous.Value;
                                writeResult.PreviousWeight = previous.Weight;
                                writeResult.Status = CacheResultStatus.Replaced;
                                writeResult.Reason = CacheRemovalReason.Replace;
                                _entries.Remove(request.Key);
                                RemoveWeight(previous.Weight);
                                notifications.Add(CreateNotification(previous, CacheRemovalReason.Replace, nowUtcTicks));
                                RecordUpdate();
                            }
                            else
                            {
                                writeResult.Status = CacheResultStatus.Written;
                                RecordPut();
                            }

                            _entries[request.Key] = entry;
                            AddWeight(entry.Weight);
                            writeResult.Written = true;
                            writeResult.Weight = entry.Weight;
                            writeResult.EvictedCount = EnforceMaximumEntries(notifications, nowUtcTicks);
                            writeResult.EvictedCount = writeResult.EvictedCount + EnforceMaximumWeight(notifications, nowUtcTicks);
                        }
                    }
                }

                index = index + 1;
            }
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Stores a key and value only when a live entry has an equal key.</summary>
    /// <remarks>Expired entries are removed before the condition is evaluated. A successful replacement resets expiration and access metadata and may enforce weight capacity.</remarks>
    /// <param name="key">The non-null key used for lookup.</param>
    /// <param name="value">The replacement value. Null is accepted and is distinguished from a miss by <see cref="CacheGetResult.Found"/>.</param>
    /// <returns>A replaced result on success, a miss or expired result when no live target exists, or a rejected result for a null key.</returns>
    public CacheWriteResult Replace(object? key, object? value)
    {
        CacheWriteResult result = new CacheWriteResult();
        result.Key = key;
        result.Value = value;
        List<CacheNotification> notifications = new List<CacheNotification>();

        if (key == null)
        {
            result.Status = CacheResultStatus.Rejected;
            result.Reason = CacheRemovalReason.Rejected;
            return result;
        }

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            bool requestedEntryExpired = false;
            if (_entries.ContainsKey(key))
            {
                requestedEntryExpired = IsEntryExpired(_entries[key], nowUtcTicks);
            }

            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
            RecordExpirations(expiredCount);

            if (!_entries.ContainsKey(key))
            {
                if (requestedEntryExpired)
                {
                    result.Status = CacheResultStatus.Expired;
                    result.Reason = CacheRemovalReason.Expired;
                }
                else
                {
                    result.Status = CacheResultStatus.Miss;
                }
            }
            else
            {
                CacheEntry previous = _entries[key];
                CacheEntryOptions entryOptions = NormalizeEntryOptions(null);
                long version = NextVersion();
                CacheEntry entry = CreateEntry(key, value, entryOptions, nowUtcTicks, version);
                _entries.Remove(key);
                RemoveWeight(previous.Weight);
                notifications.Add(CreateNotification(previous, CacheRemovalReason.Replace, nowUtcTicks));
                _entries[key] = entry;
                AddWeight(entry.Weight);
                result.Written = true;
                result.Replaced = true;
                result.PreviousValue = previous.Value;
                result.Status = CacheResultStatus.Replaced;
                result.Reason = CacheRemovalReason.Replace;
                result.Weight = entry.Weight;
                result.PreviousWeight = previous.Weight;
                RecordUpdate();
                result.EvictedCount = EnforceMaximumEntries(notifications, nowUtcTicks);
                result.EvictedCount = result.EvictedCount + EnforceMaximumWeight(notifications, nowUtcTicks);
            }
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Gets a live entry and records an access for sliding expiration and eviction policies.</summary>
    /// <remarks>An entry at or beyond its expiration boundary is removed and reported as expired. A null or unknown key is reported as a miss. When stale-while-refresh is enabled, a refresh-eligible live entry is returned with <see cref="CacheResultStatus.Stale"/> while one background refresh for its key runs.</remarks>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    public CacheGetResult Get(object? key)
    {
        CacheGetResult result = new CacheGetResult();
        result.Key = key;
        CacheNotification? notification = null;

        if (key == null)
        {
            result.Status = CacheResultStatus.Miss;
            RecordMiss();
            return result;
        }

        lock (_sync)
        {
            RecordSegmentedFrequency(key);
            if (!_entries.ContainsKey(key))
            {
                result.Status = CacheResultStatus.Miss;
                RecordMiss();
                return result;
            }

            CacheEntry entry = _entries[key];
            long nowUtcTicks = _clock.GetUtcTicks();
            if (IsEntryExpired(entry, nowUtcTicks))
            {
                _entries.Remove(key);
                RemoveWeight(entry.Weight);
                entry.RemovalReason = CacheRemovalReason.Expired;
                notification = CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks);
                result.Status = CacheResultStatus.Expired;
                result.Reason = CacheRemovalReason.Expired;
                RecordMiss();
                RecordExpirations(1L);
            }
            else
            {
                entry.LastAccessUtcTicks = nowUtcTicks;
                entry.AccessCount = CacheMath.Add(entry.AccessCount, 1L);
                RecordSegmentedHit(entry);
                FillGetResultFromEntry(result, entry);
                if (StartBackgroundRefresh(key, entry, nowUtcTicks))
                {
                    result.Status = CacheResultStatus.Stale;
                }
                RecordHit();
            }
        }

        DeliverNotification(notification);
        return result;
    }

    /// <summary>Gets a sequence of keys in input order and records each successful access.</summary>
    /// <remarks>The batch holds the cache lock while all keys are processed. Duplicate live keys record separate accesses and hits and share one background refresh when stale-while-refresh is enabled. When the first duplicate removes an expired entry, later duplicates observe a miss. Expiration notifications run after the full batch leaves the lock.</remarks>
    /// <param name="keys">The keys to look up.</param>
    /// <returns>A result containing one lookup outcome for each input key at the matching index.</returns>
    public CacheBulkGetResult GetMany(ReadOnlySpan<object?> keys)
    {
        CacheBulkGetResult result = new CacheBulkGetResult();
        result.Results = new CacheGetResult[keys.Length];
        result.Count = keys.Length;
        List<CacheNotification> notifications = new List<CacheNotification>();
        int index = 0;

        lock (_sync)
        {
            while (index < keys.Length)
            {
                object? key = keys[index];
                CacheGetResult getResult = new CacheGetResult();
                getResult.Key = key;
                result.Results[index] = getResult;

                if (key == null)
                {
                    getResult.Status = CacheResultStatus.Miss;
                    RecordMiss();
                }
                else if (!_entries.ContainsKey(key))
                {
                    RecordSegmentedFrequency(key);
                    getResult.Status = CacheResultStatus.Miss;
                    RecordMiss();
                }
                else
                {
                    RecordSegmentedFrequency(key);
                    CacheEntry entry = _entries[key];
                    long nowUtcTicks = _clock.GetUtcTicks();
                    if (IsEntryExpired(entry, nowUtcTicks))
                    {
                        _entries.Remove(key);
                        RemoveWeight(entry.Weight);
                        entry.RemovalReason = CacheRemovalReason.Expired;
                        notifications.Add(CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks));
                        getResult.Status = CacheResultStatus.Expired;
                        getResult.Reason = CacheRemovalReason.Expired;
                        RecordMiss();
                        RecordExpirations(1L);
                    }
                    else
                    {
                        entry.LastAccessUtcTicks = nowUtcTicks;
                        entry.AccessCount = CacheMath.Add(entry.AccessCount, 1L);
                        RecordSegmentedHit(entry);
                        FillGetResultFromEntry(getResult, entry);
                        if (StartBackgroundRefresh(key, entry, nowUtcTicks))
                        {
                            getResult.Status = CacheResultStatus.Stale;
                        }
                        RecordHit();
                    }
                }

                index = index + 1;
            }
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Gets a live entry through the minimal cache-store contract.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    public CacheGetResult Read(object? key)
    {
        return Get(key);
    }

    /// <summary>Gets a live entry without updating its last-access time or access count.</summary>
    /// <remarks>Peek still checks expiration, removes an expired entry, records hit or miss statistics, and may deliver a removal notification.</remarks>
    /// <param name="key">The key to look up.</param>
    /// <returns>A snapshot describing the hit, miss, or expired entry.</returns>
    public CacheGetResult Peek(object? key)
    {
        CacheGetResult result = new CacheGetResult();
        result.Key = key;
        CacheNotification? notification = null;

        if (key == null)
        {
            result.Status = CacheResultStatus.Miss;
            RecordMiss();
            return result;
        }

        lock (_sync)
        {
            if (!_entries.ContainsKey(key))
            {
                result.Status = CacheResultStatus.Miss;
                RecordMiss();
                return result;
            }

            CacheEntry entry = _entries[key];
            long nowUtcTicks = _clock.GetUtcTicks();
            if (IsEntryExpired(entry, nowUtcTicks))
            {
                _entries.Remove(key);
                RemoveWeight(entry.Weight);
                entry.RemovalReason = CacheRemovalReason.Expired;
                notification = CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks);
                result.Status = CacheResultStatus.Expired;
                result.Reason = CacheRemovalReason.Expired;
                RecordMiss();
                RecordExpirations(1L);
            }
            else
            {
                FillGetResultFromEntry(result, entry);
                RecordHit();
            }
        }

        DeliverNotification(notification);
        return result;
    }

    /// <summary>Reports whether a live entry exists without updating its last-access time, access count, or hit and miss statistics.</summary>
    /// <remarks>An equal expired entry is removed and may produce an expiration notification.</remarks>
    /// <param name="key">The key to look up.</param>
    /// <returns><c>true</c> when a live entry has an equal key; otherwise <c>false</c>.</returns>
    public bool Contains(object? key)
    {
        bool found = false;
        CacheNotification? notification = null;

        if (key == null)
        {
            return false;
        }

        lock (_sync)
        {
            if (_entries.ContainsKey(key))
            {
                CacheEntry entry = _entries[key];
                long nowUtcTicks = _clock.GetUtcTicks();
                if (IsEntryExpired(entry, nowUtcTicks))
                {
                    _entries.Remove(key);
                    RemoveWeight(entry.Weight);
                    entry.RemovalReason = CacheRemovalReason.Expired;
                    notification = CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks);
                    RecordExpirations(1L);
                }
                else
                {
                    found = true;
                }
            }
        }

        DeliverNotification(notification);
        return found;
    }

    /// <summary>Counts the entries that are live at the time of the operation.</summary>
    /// <remarks>The operation removes all expired entries without recording cache hits or misses.</remarks>
    /// <returns>The number of live entries after expiration cleanup.</returns>
    public long Count()
    {
        long count = 0L;
        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
            RecordExpirations(expiredCount);
            count = _entries.Count;
        }

        DeliverNotifications(notifications);
        return count;
    }

    /// <summary>Gets a live entry or synchronously loads a missing value with the configured loader.</summary>
    /// <remarks>Concurrent misses for an equal key share one in-progress load. A null loader result is stored only when <see cref="CacheOptions.CacheNullLoaderValues"/> is enabled.</remarks>
    /// <param name="key">The non-null key to get or load.</param>
    /// <returns>The existing hit, the loaded value, or a miss when no loader or cacheable value is available.</returns>
    public CacheGetResult GetOrLoad(object? key)
    {
        CacheGetResult existing = Get(key);

        if (existing.Found)
        {
            return existing;
        }

        if (key == null)
        {
            return existing;
        }

        if (_loader == null)
        {
            return existing;
        }

        Task<CacheGetResult> loadTask = GetOrCreateLoadTask(key);
        return loadTask.GetAwaiter().GetResult();
    }

    internal Task<CacheGetResult> GetOrCreateLoadTask(object key)
    {
        lock (_sync)
        {
            if (_loads.ContainsKey(key))
            {
                Task<CacheGetResult> existingTask = _loads[key];
                if (!existingTask.IsCompleted)
                {
                    return existingTask;
                }

                _loads.Remove(key);
            }

            Task<CacheGetResult> task = Task.Run(() => LoadEntryForKey(key));
            _loads[key] = task;
            task.ContinueWith(completedTask => RemoveLoadTask(key, completedTask), TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }
    }

    internal void RemoveLoadTask(object key, Task<CacheGetResult> task)
    {
        lock (_sync)
        {
            if (_loads.ContainsKey(key) && object.ReferenceEquals(_loads[key], task))
            {
                _loads.Remove(key);
            }
        }
    }

    internal bool StartBackgroundRefresh(object key, CacheEntry entry, long nowUtcTicks)
    {
        if (!_options.StaleWhileRefresh || _loader == null || _stopped)
        {
            return false;
        }

        if (!EntryNeedsRefresh(entry, nowUtcTicks))
        {
            return false;
        }

        if (_refreshes.ContainsKey(key))
        {
            Task existingTask = _refreshes[key];
            if (!existingTask.IsCompleted)
            {
                return object.ReferenceEquals(_refreshEntries[key], entry);
            }

            _refreshes.Remove(key);
            _refreshEntries.Remove(key);
        }

        Task task = Task.Run(() => RefreshEntryInBackground(key, entry));
        _refreshes[key] = task;
        _refreshEntries[key] = entry;
        task.ContinueWith(completedTask => RemoveRefreshTask(key, completedTask), TaskContinuationOptions.ExecuteSynchronously);
        return true;
    }

    internal void RemoveRefreshTask(object key, Task task)
    {
        if (task.IsFaulted)
        {
            task.Exception?.Flatten();
        }

        lock (_sync)
        {
            if (_refreshes.ContainsKey(key) && object.ReferenceEquals(_refreshes[key], task))
            {
                _refreshes.Remove(key);
                _refreshEntries.Remove(key);
            }
        }
    }

    internal void RefreshEntryInBackground(object key, CacheEntry sourceEntry)
    {
        object? loadedValue = InvokeLoader(key);

        if (loadedValue == null && !_options.CacheNullLoaderValues)
        {
            return;
        }

        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            if (!_entries.ContainsKey(key))
            {
                return;
            }

            CacheEntry currentEntry = _entries[key];
            if (!object.ReferenceEquals(currentEntry, sourceEntry))
            {
                return;
            }

            long nowUtcTicks = _clock.GetUtcTicks();
            if (IsEntryExpired(currentEntry, nowUtcTicks))
            {
                _entries.Remove(key);
                RemoveWeight(currentEntry.Weight);
                currentEntry.RemovalReason = CacheRemovalReason.Expired;
                notifications.Add(CreateNotification(currentEntry, CacheRemovalReason.Expired, nowUtcTicks));
                RecordExpirations(1L);
            }
            else
            {
                CacheEntryOptions entryOptions = RefreshEntryOptions(currentEntry);
                long version = NextVersion();
                CacheEntry replacement = CreateEntry(key, loadedValue, entryOptions, nowUtcTicks, version);
                _entries.Remove(key);
                RemoveWeight(currentEntry.Weight);
                currentEntry.RemovalReason = CacheRemovalReason.Replace;
                notifications.Add(CreateNotification(currentEntry, CacheRemovalReason.Replace, nowUtcTicks));
                _entries[key] = replacement;
                AddWeight(replacement.Weight);
                RecordUpdate();
                EnforceMaximumEntries(notifications, nowUtcTicks);
                EnforceMaximumWeight(notifications, nowUtcTicks);
            }
        }

        DeliverNotifications(notifications);
    }

    internal CacheEntryOptions RefreshEntryOptions(CacheEntry entry)
    {
        CacheEntryOptions options = new CacheEntryOptions();
        options.HasAbsoluteExpiration = entry.HasConfiguredAbsoluteExpiration;
        options.AbsoluteExpirationUtcTicks = entry.ConfiguredAbsoluteExpirationUtcTicks;
        options.HasTimeToLive = entry.HasTimeToLive;
        options.TimeToLiveTicks = entry.TimeToLiveTicks;
        options.HasSlidingExpiration = entry.HasSlidingExpiration;
        options.SlidingExpirationTicks = entry.SlidingExpirationTicks;
        options.Weight = entry.Weight;
        options.Priority = entry.Priority;
        options.HasRefreshAfterWrite = entry.HasRefreshAfterWrite;
        options.RefreshAfterWriteTicks = entry.RefreshAfterWriteTicks;
        options.CacheNullValue = true;
        return options;
    }

    internal bool EntryNeedsRefresh(CacheEntry entry, long nowUtcTicks)
    {
        if (!entry.HasRefreshAfterWrite)
        {
            return false;
        }

        long refreshUtcTicks = CacheExpiration.AddTicks(entry.LastWriteUtcTicks, entry.RefreshAfterWriteTicks);
        return nowUtcTicks >= refreshUtcTicks;
    }

    internal CacheGetResult LoadEntryForKey(object key)
    {
        CacheGetResult result = new CacheGetResult();
        result.Key = key;

        if (_loader == null)
        {
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        object? loadedValue = InvokeLoader(key);

        if (loadedValue == null && !_options.CacheNullLoaderValues)
        {
            result.Loaded = true;
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        Put(key, loadedValue);
        CacheGetResult loaded = SnapshotLoadedEntry(key);
        loaded.Loaded = true;
        if (loaded.Found)
        {
            loaded.Status = CacheResultStatus.Loaded;
        }
        else
        {
            loaded.Status = CacheResultStatus.Rejected;
        }

        return loaded;
    }

    /// <summary>Reports whether a live entry has reached its configured refresh-after-write boundary.</summary>
    /// <remarks>This method does not load, replace, access, or remove an entry.</remarks>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> only when a live entry exists, refresh metadata is enabled, and its boundary has been reached.</returns>
    public bool NeedsRefresh(object? key)
    {
        if (key == null)
        {
            return false;
        }

        lock (_sync)
        {
            if (!_entries.ContainsKey(key))
            {
                return false;
            }

            CacheEntry entry = _entries[key];
            if (!entry.HasRefreshAfterWrite)
            {
                return false;
            }

            if (IsEntryExpired(entry, _clock.GetUtcTicks()))
            {
                return false;
            }

            return EntryNeedsRefresh(entry, _clock.GetUtcTicks());
        }
    }

    /// <summary>Reloads and replaces an existing live entry using the configured loader.</summary>
    /// <remarks>Refresh may be called regardless of <see cref="NeedsRefresh"/>. It returns a miss when the key is absent or no loader exists, and removes rather than reloads an entry that is already expired.</remarks>
    /// <param name="key">The key of the existing entry to refresh.</param>
    /// <returns>A snapshot of the loaded replacement, or a miss, expired, or rejected result.</returns>
    public CacheGetResult Refresh(object? key)
    {
        CacheGetResult result = new CacheGetResult();
        result.Key = key;
        CacheNotification? notification = null;

        if (key == null)
        {
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        lock (_sync)
        {
            if (!_entries.ContainsKey(key))
            {
                result.Status = CacheResultStatus.Miss;
                return result;
            }

            CacheEntry entry = _entries[key];
            long nowUtcTicks = _clock.GetUtcTicks();
            if (IsEntryExpired(entry, nowUtcTicks))
            {
                _entries.Remove(key);
                RemoveWeight(entry.Weight);
                entry.RemovalReason = CacheRemovalReason.Expired;
                notification = CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks);
                result.Status = CacheResultStatus.Expired;
                result.Reason = CacheRemovalReason.Expired;
                RecordMiss();
                RecordExpirations(1L);
            }
        }

        DeliverNotification(notification);

        if (result.Status == CacheResultStatus.Expired)
        {
            return result;
        }

        if (_loader == null)
        {
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        object? loadedValue = InvokeLoader(key);

        if (loadedValue == null && !_options.CacheNullLoaderValues)
        {
            result.Loaded = true;
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        Put(key, loadedValue);
        result = SnapshotLoadedEntry(key);
        result.Loaded = true;
        if (result.Found)
        {
            result.Status = CacheResultStatus.Loaded;
        }
        else
        {
            result.Status = CacheResultStatus.Rejected;
        }

        return result;
    }

    /// <summary>Explicitly removes an entry by key.</summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>A result containing the removed value and weight, or a miss when no entry exists.</returns>
    public CacheRemoveResult Remove(object? key)
    {
        CacheRemoveResult result = new CacheRemoveResult();
        result.Key = key;
        CacheNotification? notification = null;

        if (key == null)
        {
            result.Status = CacheResultStatus.Miss;
            return result;
        }

        lock (_sync)
        {
            if (!_entries.ContainsKey(key))
            {
                result.Status = CacheResultStatus.Miss;
                return result;
            }

            CacheEntry entry = _entries[key];
            _entries.Remove(key);
            RemoveWeight(entry.Weight);
            entry.RemovalReason = CacheRemovalReason.ExplicitRemove;
            notification = CreateNotification(entry, CacheRemovalReason.ExplicitRemove, _clock.GetUtcTicks());

            result.Removed = true;
            result.Key = entry.Key;
            result.Value = entry.Value;
            result.Status = CacheResultStatus.Removed;
            result.Reason = CacheRemovalReason.ExplicitRemove;
            result.Weight = entry.Weight;
            RecordRemove();
        }

        DeliverNotification(notification);
        return result;
    }

    /// <summary>Explicitly removes a sequence of keys in input order.</summary>
    /// <remarks>The batch holds the cache lock while all keys are processed. The first occurrence of a stored key removes it explicitly, and later equal keys report misses. Null keys also report misses. Removal notifications run after the full batch leaves the lock.</remarks>
    /// <param name="keys">The keys to remove.</param>
    /// <returns>A result containing one removal outcome for each input key at the matching index.</returns>
    public CacheBulkRemoveResult RemoveMany(ReadOnlySpan<object?> keys)
    {
        CacheBulkRemoveResult result = new CacheBulkRemoveResult();
        result.Results = new CacheRemoveResult[keys.Length];
        result.Count = keys.Length;
        List<CacheNotification> notifications = new List<CacheNotification>();
        int index = 0;

        lock (_sync)
        {
            while (index < keys.Length)
            {
                object? key = keys[index];
                CacheRemoveResult removeResult = new CacheRemoveResult();
                removeResult.Key = key;
                result.Results[index] = removeResult;

                if (key == null)
                {
                    removeResult.Status = CacheResultStatus.Miss;
                }
                else if (!_entries.ContainsKey(key))
                {
                    removeResult.Status = CacheResultStatus.Miss;
                }
                else
                {
                    CacheEntry entry = _entries[key];
                    _entries.Remove(key);
                    RemoveWeight(entry.Weight);
                    entry.RemovalReason = CacheRemovalReason.ExplicitRemove;
                    notifications.Add(CreateNotification(entry, CacheRemovalReason.ExplicitRemove, _clock.GetUtcTicks()));
                    removeResult.Removed = true;
                    removeResult.Key = entry.Key;
                    removeResult.Value = entry.Value;
                    removeResult.Status = CacheResultStatus.Removed;
                    removeResult.Reason = CacheRemovalReason.ExplicitRemove;
                    removeResult.Weight = entry.Weight;
                    RecordRemove();
                }

                index = index + 1;
            }
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Removes every currently stored entry.</summary>
    /// <remarks>Each entry generates a clear notification after the internal dictionary has been emptied.</remarks>
    /// <returns>A maintenance result containing the number of cleared entries and zero current size and weight.</returns>
    public CacheMaintenanceResult Clear()
    {
        CacheMaintenanceResult result = new CacheMaintenanceResult();
        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            long count = _entries.Count;
            long nowUtcTicks = _clock.GetUtcTicks();
            foreach (CacheEntry entry in _entries.Values)
            {
                entry.RemovalReason = CacheRemovalReason.Clear;
                notifications.Add(CreateNotification(entry, CacheRemovalReason.Clear, nowUtcTicks));
            }

            _entries.Clear();
            _currentWeight = 0L;
            _frequencies.Clear();
            _frequencyEvents = 0L;
            _segmentedWindowHits = 0L;
            _segmentedMainHits = 0L;
            _segmentedAdaptationEvents = 0L;
            _segmentedWindowPercent = 20;

            result.Status = CacheResultStatus.Cleared;
            result.Reason = CacheRemovalReason.Clear;
            result.RemovedCount = count;
            result.ClearedCount = count;
            result.CurrentCount = 0L;
            result.CurrentWeight = 0L;
            RecordClear();
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Finds and removes every entry whose expiration boundary has been reached.</summary>
    /// <remarks>This is useful for low-traffic caches; normal reads already prevent expired values from being returned.</remarks>
    /// <returns>A maintenance result containing the expired count and the remaining size and weight.</returns>
    public CacheMaintenanceResult Cleanup()
    {
        CacheMaintenanceResult result = new CacheMaintenanceResult();
        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);

            result.Status = CacheResultStatus.Removed;
            result.Reason = CacheRemovalReason.Expired;
            result.RemovedCount = expiredCount;
            result.ExpiredCount = expiredCount;
            result.CurrentCount = _entries.Count;
            result.CurrentWeight = _currentWeight;
            RecordExpirations(expiredCount);
        }

        DeliverNotifications(notifications);
        return result;
    }

    /// <summary>Captures operation statistics and current capacity measurements.</summary>
    /// <remarks>Counters and rates remain zero when statistics recording is disabled, while current size and weight are always populated.</remarks>
    /// <returns>A point-in-time statistics snapshot.</returns>
    public CacheStatsSnapshot Stats()
    {
        CacheStatsSnapshot snapshot = new CacheStatsSnapshot();

        lock (_sync)
        {
            if (_options.RecordStats)
            {
                snapshot.Hits = _stats.Hits;
                snapshot.Misses = _stats.Misses;
                snapshot.Puts = _stats.Puts;
                snapshot.Updates = _stats.Updates;
                snapshot.Removes = _stats.Removes;
                snapshot.Expirations = _stats.Expirations;
                snapshot.Evictions = _stats.Evictions;
                snapshot.Clears = _stats.Clears;
                snapshot.LoadSuccesses = _stats.LoadSuccesses;
                snapshot.LoadFailures = _stats.LoadFailures;
                snapshot.TotalLoadDurationTicks = _stats.TotalLoadDurationTicks;
                snapshot.HitRate = CalculateRateFromCounts(_stats.Hits, _stats.Misses);
                snapshot.LoadSuccessRate = CalculateRateFromCounts(_stats.LoadSuccesses, _stats.LoadFailures);
            }

            snapshot.CurrentSize = _entries.Count;
            snapshot.CurrentWeight = _currentWeight;
        }

        return snapshot;
    }

    /// <summary>Captures normalized policy settings and ordered diagnostic key lists.</summary>
    /// <remarks>Oldest and hottest lists include all stored entries. The expiring-soon list contains only entries with an expiration boundary.</remarks>
    /// <returns>A point-in-time policy and capacity snapshot.</returns>
    public CachePolicySnapshot Policy()
    {
        CachePolicySnapshot snapshot = new CachePolicySnapshot();

        lock (_sync)
        {
            snapshot.Name = NormalizeName(_options.Name);
            snapshot.HasDefaultAbsoluteExpiration = _options.HasDefaultAbsoluteExpiration;
            snapshot.DefaultAbsoluteExpirationUtcTicks = _options.DefaultAbsoluteExpirationUtcTicks;
            snapshot.HasDefaultTimeToLive = _options.HasDefaultTimeToLive;
            snapshot.DefaultTimeToLiveTicks = _options.DefaultTimeToLiveTicks;
            snapshot.HasDefaultSlidingExpiration = _options.HasDefaultSlidingExpiration;
            snapshot.DefaultSlidingExpirationTicks = _options.DefaultSlidingExpirationTicks;
            snapshot.HasMaximumEntries = _options.HasMaximumEntries;
            snapshot.MaximumEntries = _options.MaximumEntries;
            snapshot.HasMaximumWeight = _options.HasMaximumWeight;
            snapshot.MaximumWeight = _options.MaximumWeight;
            snapshot.EvictionPolicy = _options.EvictionPolicy;
            snapshot.DefaultPriority = _options.DefaultPriority;
            snapshot.RecordStats = _options.RecordStats;
            snapshot.StaleWhileRefresh = _options.StaleWhileRefresh;
            snapshot.HasCleanupInterval = _options.HasCleanupInterval;
            snapshot.CleanupIntervalTicks = _options.CleanupIntervalTicks;
            snapshot.CurrentCount = _entries.Count;
            snapshot.CurrentWeight = _currentWeight;
            CacheEntry[] entries = EntryArray();
            snapshot.OldestKeys = OldestKeys(entries);
            snapshot.HottestKeys = HottestKeys(entries);
            snapshot.ExpiringSoonKeys = ExpiringSoonKeys(entries);
            snapshot.SnapshotUtcTicks = _clock.GetUtcTicks();
        }

        return snapshot;
    }

    /// <summary>Changes selected capacity and default expiration policy settings.</summary>
    /// <remarks>All enabled changes are applied atomically. Non-positive capacities and negative expiration values disable their settings. Capacity reductions remove expired entries first and then evict immediately as needed. Notifications are delivered after the policy and cache contents have changed.</remarks>
    /// <param name="update">The requested changes, or <c>null</c> to leave every setting unchanged.</param>
    /// <returns>A point-in-time policy snapshot after the operation.</returns>
    public CachePolicySnapshot UpdatePolicy(CachePolicyUpdate? update)
    {
        if (update == null)
        {
            return Policy();
        }

        CachePolicySnapshot snapshot;
        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            bool updateCapacity = update.UpdateMaximumEntries || update.UpdateMaximumWeight;
            ApplyPolicyUpdate(update);

            bool capacityExceeded = false;
            if (updateCapacity && _options.HasMaximumEntries && _entries.Count > _options.MaximumEntries)
            {
                capacityExceeded = true;
            }

            if (updateCapacity && _options.HasMaximumWeight && CurrentWeightExceedsMaximum())
            {
                capacityExceeded = true;
            }

            if (capacityExceeded)
            {
                long nowUtcTicks = _clock.GetUtcTicks();
                long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
                RecordExpirations(expiredCount);
                EnforceMaximumEntries(notifications, nowUtcTicks);
                EnforceMaximumWeight(notifications, nowUtcTicks);
            }

            snapshot = Policy();
        }

        DeliverNotifications(notifications);
        return snapshot;
    }

    internal void ApplyPolicyUpdate(CachePolicyUpdate update)
    {
        if (update.UpdateMaximumEntries)
        {
            _options.HasMaximumEntries = update.MaximumEntries > 0L;
            _options.MaximumEntries = NormalizeEnabledTicks(_options.HasMaximumEntries, update.MaximumEntries);
        }

        if (update.UpdateMaximumWeight)
        {
            _options.HasMaximumWeight = update.MaximumWeight > 0L;
            _options.MaximumWeight = NormalizeEnabledTicks(_options.HasMaximumWeight, update.MaximumWeight);
        }

        if (update.UpdateDefaultAbsoluteExpiration)
        {
            _options.HasDefaultAbsoluteExpiration = update.DefaultAbsoluteExpirationUtcTicks >= 0L;
            _options.DefaultAbsoluteExpirationUtcTicks = NormalizeEnabledTicks(_options.HasDefaultAbsoluteExpiration, update.DefaultAbsoluteExpirationUtcTicks);
        }

        if (update.UpdateDefaultTimeToLive)
        {
            _options.HasDefaultTimeToLive = update.DefaultTimeToLiveTicks >= 0L;
            _options.DefaultTimeToLiveTicks = NormalizeEnabledTicks(_options.HasDefaultTimeToLive, update.DefaultTimeToLiveTicks);
        }

        if (update.UpdateDefaultSlidingExpiration)
        {
            _options.HasDefaultSlidingExpiration = update.DefaultSlidingExpirationTicks >= 0L;
            _options.DefaultSlidingExpirationTicks = NormalizeEnabledTicks(_options.HasDefaultSlidingExpiration, update.DefaultSlidingExpirationTicks);
        }
    }

    /// <summary>Copies the keys and values of all live entries.</summary>
    /// <remarks>The operation first removes every expired entry. Key and value arrays use matching indices but do not promise a stable ordering between calls.</remarks>
    /// <returns>A point-in-time content snapshot with count, weight, and clock ticks.</returns>
    public CacheSnapshot Snapshot()
    {
        CacheSnapshot snapshot = new CacheSnapshot();
        List<CacheNotification> notifications = new List<CacheNotification>();

        lock (_sync)
        {
            long nowUtcTicks = _clock.GetUtcTicks();
            long expiredCount = RemoveExpiredEntries(notifications, nowUtcTicks);
            RecordExpirations(expiredCount);

            snapshot.Count = _entries.Count;
            snapshot.Weight = _currentWeight;
            snapshot.SnapshotUtcTicks = nowUtcTicks;
            snapshot.Keys = new object[_entries.Count];
            snapshot.Values = new object?[_entries.Count];

            int index = 0;
            foreach (KeyValuePair<object, CacheEntry> pair in _entries)
            {
                snapshot.Keys[index] = pair.Key;
                snapshot.Values[index] = pair.Value.Value;
                index = index + 1;
            }
        }

        DeliverNotifications(notifications);
        return snapshot;
    }

    /// <summary>Stops and disposes scheduled cleanup for this cache.</summary>
    /// <remarks>The operation is idempotent, prevents new scheduled cleanup and background refresh work, and does not clear entries or prevent ordinary cache operations. Background refreshes already in progress are allowed to finish.</remarks>
    public void Stop()
    {
        Timer? timer = null;

        lock (_sync)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            timer = _cleanupTimer;
            _cleanupTimer = null;
        }

        if (timer != null)
        {
            timer.Dispose();
        }
    }

    internal CacheEntry[] EntryArray()
    {
        CacheEntry[] entries = new CacheEntry[_entries.Count];
        int index = 0;

        foreach (KeyValuePair<object, CacheEntry> pair in _entries)
        {
            entries[index] = pair.Value;
            index = index + 1;
        }

        return entries;
    }

    internal object[] OldestKeys(CacheEntry[] entries)
    {
        CacheEntry[] sorted = CopyEntries(entries);
        SortByOldest(sorted);
        return KeysFromEntries(sorted);
    }

    internal object[] HottestKeys(CacheEntry[] entries)
    {
        CacheEntry[] sorted = CopyEntries(entries);
        SortByHottest(sorted);
        return KeysFromEntries(sorted);
    }

    internal object[] ExpiringSoonKeys(CacheEntry[] entries)
    {
        CacheEntry[] expiringEntries = ExpiringEntries(entries);
        SortByExpiringSoon(expiringEntries);
        return KeysFromEntries(expiringEntries);
    }

    internal CacheEntry[] CopyEntries(CacheEntry[] entries)
    {
        CacheEntry[] copy = new CacheEntry[entries.Length];
        int index = 0;

        while (index < entries.Length)
        {
            copy[index] = entries[index];
            index = index + 1;
        }

        return copy;
    }

    internal CacheEntry[] ExpiringEntries(CacheEntry[] entries)
    {
        int count = 0;
        int index = 0;

        while (index < entries.Length)
        {
            if (EntryHasExpiration(entries[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        CacheEntry[] expiringEntries = new CacheEntry[count];
        int expiringIndex = 0;
        index = 0;

        while (index < entries.Length)
        {
            if (EntryHasExpiration(entries[index]))
            {
                expiringEntries[expiringIndex] = entries[index];
                expiringIndex = expiringIndex + 1;
            }

            index = index + 1;
        }

        return expiringEntries;
    }

    internal bool EntryHasExpiration(CacheEntry entry)
    {
        return entry.HasAbsoluteExpiration || entry.HasSlidingExpiration;
    }

    internal object[] KeysFromEntries(CacheEntry[] entries)
    {
        object[] keys = new object[entries.Length];
        int index = 0;

        while (index < entries.Length)
        {
            object? key = entries[index].Key;
            if (key == null)
            {
                throw new InvalidOperationException("A stored cache entry has a null key.");
            }

            keys[index] = key;
            index = index + 1;
        }

        return keys;
    }

    internal void SortByOldest(CacheEntry[] entries)
    {
        int index = 0;

        while (index < entries.Length)
        {
            int selected = index;
            int candidate = index + 1;

            while (candidate < entries.Length)
            {
                if (IsOlder(entries[candidate], entries[selected]))
                {
                    selected = candidate;
                }

                candidate = candidate + 1;
            }

            SwapEntries(entries, index, selected);
            index = index + 1;
        }
    }

    internal void SortByHottest(CacheEntry[] entries)
    {
        int index = 0;

        while (index < entries.Length)
        {
            int selected = index;
            int candidate = index + 1;

            while (candidate < entries.Length)
            {
                if (IsHotter(entries[candidate], entries[selected]))
                {
                    selected = candidate;
                }

                candidate = candidate + 1;
            }

            SwapEntries(entries, index, selected);
            index = index + 1;
        }
    }

    internal void SortByExpiringSoon(CacheEntry[] entries)
    {
        int index = 0;

        while (index < entries.Length)
        {
            int selected = index;
            int candidate = index + 1;

            while (candidate < entries.Length)
            {
                if (ExpiresSooner(entries[candidate], entries[selected]))
                {
                    selected = candidate;
                }

                candidate = candidate + 1;
            }

            SwapEntries(entries, index, selected);
            index = index + 1;
        }
    }

    internal bool IsOlder(CacheEntry left, CacheEntry right)
    {
        if (left.CreatedUtcTicks < right.CreatedUtcTicks)
        {
            return true;
        }

        if (left.CreatedUtcTicks > right.CreatedUtcTicks)
        {
            return false;
        }

        return left.Version < right.Version;
    }

    internal bool IsHotter(CacheEntry left, CacheEntry right)
    {
        if (left.AccessCount > right.AccessCount)
        {
            return true;
        }

        if (left.AccessCount < right.AccessCount)
        {
            return false;
        }

        if (left.LastAccessUtcTicks > right.LastAccessUtcTicks)
        {
            return true;
        }

        if (left.LastAccessUtcTicks < right.LastAccessUtcTicks)
        {
            return false;
        }

        return left.Version < right.Version;
    }

    internal bool ExpiresSooner(CacheEntry left, CacheEntry right)
    {
        long leftExpiration = CalculateEntryExpirationUtcTicks(left);
        long rightExpiration = CalculateEntryExpirationUtcTicks(right);

        if (leftExpiration < rightExpiration)
        {
            return true;
        }

        if (leftExpiration > rightExpiration)
        {
            return false;
        }

        return left.Version < right.Version;
    }

    internal void SwapEntries(CacheEntry[] entries, int leftIndex, int rightIndex)
    {
        if (leftIndex == rightIndex)
        {
            return;
        }

        CacheEntry entry = entries[leftIndex];
        entries[leftIndex] = entries[rightIndex];
        entries[rightIndex] = entry;
    }

    internal CacheGetResult SnapshotLoadedEntry(object key)
    {
        CacheGetResult result = new CacheGetResult();
        result.Key = key;

        lock (_sync)
        {
            if (_entries.ContainsKey(key))
            {
                CacheEntry entry = _entries[key];
                FillGetResultFromEntry(result, entry);
            }
        }

        return result;
    }

    internal CacheOptions NormalizeOptions(CacheOptions? options)
    {
        CacheOptions normalized = new CacheOptions();

        if (options == null)
        {
            normalized.KeyComparer = NormalizeKeyComparer(normalized.KeyComparer);
            normalized.Clock = NormalizeClock(normalized.Clock);
            return normalized;
        }

        normalized.Name = NormalizeName(options.Name);
        normalized.KeyComparer = NormalizeKeyComparer(options.KeyComparer);
        normalized.HasDefaultAbsoluteExpiration = options.HasDefaultAbsoluteExpiration && options.DefaultAbsoluteExpirationUtcTicks >= 0L;
        normalized.DefaultAbsoluteExpirationUtcTicks = NormalizeEnabledTicks(normalized.HasDefaultAbsoluteExpiration, options.DefaultAbsoluteExpirationUtcTicks);
        normalized.HasDefaultTimeToLive = options.HasDefaultTimeToLive && options.DefaultTimeToLiveTicks >= 0L;
        normalized.DefaultTimeToLiveTicks = NormalizeEnabledTicks(normalized.HasDefaultTimeToLive, options.DefaultTimeToLiveTicks);
        normalized.HasDefaultSlidingExpiration = options.HasDefaultSlidingExpiration && options.DefaultSlidingExpirationTicks >= 0L;
        normalized.DefaultSlidingExpirationTicks = NormalizeEnabledTicks(normalized.HasDefaultSlidingExpiration, options.DefaultSlidingExpirationTicks);
        normalized.HasMaximumEntries = options.HasMaximumEntries && options.MaximumEntries > 0L;
        normalized.MaximumEntries = NormalizeEnabledTicks(normalized.HasMaximumEntries, options.MaximumEntries);
        normalized.HasMaximumWeight = options.HasMaximumWeight && options.MaximumWeight > 0L;
        normalized.MaximumWeight = NormalizeEnabledTicks(normalized.HasMaximumWeight, options.MaximumWeight);
        normalized.EvictionPolicy = NormalizeEvictionPolicy(options.EvictionPolicy);
        normalized.DefaultPriority = NormalizePriority(options.DefaultPriority);
        normalized.Clock = NormalizeClock(options.Clock);
        normalized.Loader = options.Loader;
        normalized.Weigher = options.Weigher;
        normalized.RemovalListener = options.RemovalListener;
        normalized.RecordStats = options.RecordStats;
        normalized.StaleWhileRefresh = options.StaleWhileRefresh;
        normalized.HasCleanupInterval = options.HasCleanupInterval && options.CleanupIntervalTicks > 0L;
        normalized.CleanupIntervalTicks = NormalizeEnabledTicks(normalized.HasCleanupInterval, options.CleanupIntervalTicks);
        normalized.CacheNullLoaderValues = options.CacheNullLoaderValues;

        return normalized;
    }

    internal string NormalizeName(string? name)
    {
        if (name == null)
        {
            return "";
        }

        return name;
    }

    internal IEqualityComparer<object> NormalizeKeyComparer(IEqualityComparer<object>? keyComparer)
    {
        if (keyComparer == null)
        {
            return EqualityComparer<object>.Default;
        }

        return keyComparer;
    }

    internal ICacheClock NormalizeClock(ICacheClock? clock)
    {
        if (clock == null)
        {
            return new CacheClock();
        }

        return clock;
    }

    internal long NormalizeEnabledTicks(bool enabled, long ticks)
    {
        if (enabled)
        {
            return ticks;
        }

        return 0L;
    }

    internal CacheEvictionPolicy NormalizeEvictionPolicy(CacheEvictionPolicy policy)
    {
        switch (policy)
        {
            case CacheEvictionPolicy.Lru:
                return CacheEvictionPolicy.Lru;
            case CacheEvictionPolicy.Fifo:
                return CacheEvictionPolicy.Fifo;
            case CacheEvictionPolicy.Lfu:
                return CacheEvictionPolicy.Lfu;
            case CacheEvictionPolicy.Random:
                return CacheEvictionPolicy.Random;
            case CacheEvictionPolicy.Segmented:
                return CacheEvictionPolicy.Segmented;
            default:
                return CacheEvictionPolicy.Lru;
        }
    }

    internal CacheEntryPriority NormalizePriority(CacheEntryPriority priority)
    {
        switch (priority)
        {
            case CacheEntryPriority.Low:
                return CacheEntryPriority.Low;
            case CacheEntryPriority.Normal:
                return CacheEntryPriority.Normal;
            case CacheEntryPriority.High:
                return CacheEntryPriority.High;
            default:
                return CacheEntryPriority.Normal;
        }
    }

    internal CacheEntryOptions NormalizeEntryOptions(CacheEntryOptions? options)
    {
        if (options == null)
        {
            return DefaultEntryOptions();
        }

        return CopyEntryOptions(options);
    }

    internal CacheEntryOptions DefaultEntryOptions()
    {
        CacheEntryOptions options = new CacheEntryOptions();
        options.HasAbsoluteExpiration = _options.HasDefaultAbsoluteExpiration;
        options.AbsoluteExpirationUtcTicks = _options.DefaultAbsoluteExpirationUtcTicks;
        options.HasTimeToLive = _options.HasDefaultTimeToLive;
        options.TimeToLiveTicks = _options.DefaultTimeToLiveTicks;
        options.HasSlidingExpiration = _options.HasDefaultSlidingExpiration;
        options.SlidingExpirationTicks = _options.DefaultSlidingExpirationTicks;
        options.Priority = _options.DefaultPriority;
        return options;
    }

    internal CacheEntryOptions CopyEntryOptions(CacheEntryOptions options)
    {
        CacheEntryOptions copy = new CacheEntryOptions();
        copy.HasAbsoluteExpiration = options.HasAbsoluteExpiration && options.AbsoluteExpirationUtcTicks >= 0L;
        copy.AbsoluteExpirationUtcTicks = NormalizeEnabledTicks(copy.HasAbsoluteExpiration, options.AbsoluteExpirationUtcTicks);
        copy.HasTimeToLive = options.HasTimeToLive && options.TimeToLiveTicks >= 0L;
        copy.TimeToLiveTicks = NormalizeEnabledTicks(copy.HasTimeToLive, options.TimeToLiveTicks);
        copy.HasSlidingExpiration = options.HasSlidingExpiration && options.SlidingExpirationTicks >= 0L;
        copy.SlidingExpirationTicks = NormalizeEnabledTicks(copy.HasSlidingExpiration, options.SlidingExpirationTicks);
        copy.Weight = NormalizeWeight(options.Weight);
        copy.Priority = NormalizePriority(options.Priority);
        copy.HasRefreshAfterWrite = options.HasRefreshAfterWrite && options.RefreshAfterWriteTicks >= 0L;
        copy.RefreshAfterWriteTicks = NormalizeEnabledTicks(copy.HasRefreshAfterWrite, options.RefreshAfterWriteTicks);
        copy.CacheNullValue = options.CacheNullValue;
        return copy;
    }

    internal long NormalizeWeight(long weight)
    {
        if (weight <= 0L)
        {
            return 1L;
        }

        return weight;
    }

    internal long NextVersion()
    {
        _version = CacheMath.Add(_version, 1L);
        return _version;
    }

    internal void AddWeight(long weight)
    {
        _currentWeight = CacheMath.Add(_currentWeight, weight);
    }

    internal void RemoveWeight(long weight)
    {
        if (_currentWeight == long.MaxValue)
        {
            RecalculateCurrentWeight();
            return;
        }

        _currentWeight = CacheMath.Subtract(_currentWeight, weight);
    }

    internal void RecalculateCurrentWeight()
    {
        long weight = 0L;

        foreach (CacheEntry entry in _entries.Values)
        {
            weight = CacheMath.Add(weight, entry.Weight);
        }

        _currentWeight = weight;
    }

    internal CacheEntry CreateEntry(object key, object? value, CacheEntryOptions options, long nowUtcTicks, long version)
    {
        CacheEntry entry = new CacheEntry();
        bool hasFixedExpiration = options.HasAbsoluteExpiration || options.HasTimeToLive;

        entry.Key = key;
        entry.Value = value;
        entry.CreatedUtcTicks = nowUtcTicks;
        entry.LastAccessUtcTicks = nowUtcTicks;
        entry.LastWriteUtcTicks = nowUtcTicks;
        entry.HasConfiguredAbsoluteExpiration = options.HasAbsoluteExpiration;
        entry.ConfiguredAbsoluteExpirationUtcTicks = options.AbsoluteExpirationUtcTicks;
        entry.HasTimeToLive = options.HasTimeToLive;
        entry.TimeToLiveTicks = options.TimeToLiveTicks;
        entry.HasAbsoluteExpiration = hasFixedExpiration;
        entry.AbsoluteExpirationUtcTicks = CacheExpiration.CalculateFixedExpirationUtcTicks(options, nowUtcTicks);
        entry.HasSlidingExpiration = options.HasSlidingExpiration;
        entry.SlidingExpirationTicks = options.SlidingExpirationTicks;
        entry.Weight = ResolveWeight(key, value, options);
        entry.Priority = NormalizePriority(options.Priority);
        entry.HasRefreshAfterWrite = options.HasRefreshAfterWrite;
        entry.RefreshAfterWriteTicks = options.RefreshAfterWriteTicks;
        entry.Version = version;
        entry.RemovalReason = CacheRemovalReason.Unknown;
        entry.AccessCount = InitialAccessCount();
        entry.Segment = CacheEntrySegment.Window;
        RecordSegmentedFrequency(key);

        return entry;
    }

    internal long ResolveWeight(object key, object? value, CacheEntryOptions options)
    {
        if (_weigher != null)
        {
            return NormalizeWeight(_weigher(key, value));
        }

        return NormalizeWeight(options.Weight);
    }

    internal long EnforceMaximumEntries(List<CacheNotification> notifications, long nowUtcTicks)
    {
        long evictedCount = 0L;

        if (!_options.HasMaximumEntries)
        {
            return evictedCount;
        }

        object? admissionCandidate = null;
        if (!_options.HasMaximumWeight || _entries.Count > _options.MaximumEntries)
        {
            admissionCandidate = MaintainSegmentedSegments(false);
        }

        while (_entries.Count > _options.MaximumEntries)
        {
            object keyToRemove = SelectEvictionKey(false, admissionCandidate);
            CacheEntry entry = _entries[keyToRemove];
            _entries.Remove(keyToRemove);
            RemoveWeight(entry.Weight);
            entry.RemovalReason = CacheRemovalReason.Capacity;
            notifications.Add(CreateNotification(entry, CacheRemovalReason.Capacity, nowUtcTicks));
            RecordEviction();
            evictedCount = evictedCount + 1L;
            admissionCandidate = MaintainSegmentedSegments(false);
        }

        return evictedCount;
    }

    internal long EnforceMaximumWeight(List<CacheNotification> notifications, long nowUtcTicks)
    {
        long evictedCount = 0L;

        if (!_options.HasMaximumWeight)
        {
            return evictedCount;
        }

        object? admissionCandidate = MaintainSegmentedSegments(true);

        while (CurrentWeightExceedsMaximum() && _entries.Count > 0)
        {
            object keyToRemove = SelectEvictionKey(true, admissionCandidate);
            CacheEntry entry = _entries[keyToRemove];
            _entries.Remove(keyToRemove);
            RemoveWeight(entry.Weight);
            entry.RemovalReason = CacheRemovalReason.Capacity;
            notifications.Add(CreateNotification(entry, CacheRemovalReason.Capacity, nowUtcTicks));
            RecordEviction();
            evictedCount = evictedCount + 1L;
            admissionCandidate = MaintainSegmentedSegments(true);
        }

        return evictedCount;
    }

    internal bool CurrentWeightExceedsMaximum()
    {
        if (_currentWeight > _options.MaximumWeight)
        {
            return true;
        }

        if (_currentWeight < _options.MaximumWeight)
        {
            return false;
        }

        long weight = 0L;

        foreach (CacheEntry entry in _entries.Values)
        {
            if (weight > _options.MaximumWeight - entry.Weight)
            {
                return true;
            }

            weight = weight + entry.Weight;
        }

        return false;
    }

    internal long RemoveExpiredEntries(List<CacheNotification> notifications, long nowUtcTicks)
    {
        List<object> expiredKeys = new List<object>();

        foreach (KeyValuePair<object, CacheEntry> pair in _entries)
        {
            if (IsEntryExpired(pair.Value, nowUtcTicks))
            {
                expiredKeys.Add(pair.Key);
            }
        }

        foreach (object key in expiredKeys)
        {
            CacheEntry entry = _entries[key];
            _entries.Remove(key);
            RemoveWeight(entry.Weight);
            entry.RemovalReason = CacheRemovalReason.Expired;
            notifications.Add(CreateNotification(entry, CacheRemovalReason.Expired, nowUtcTicks));
        }

        return expiredKeys.Count;
    }

    internal CacheNotification CreateNotification(CacheEntry entry, CacheRemovalReason reason, long removedUtcTicks)
    {
        CacheNotification notification = new CacheNotification();
        notification.Name = NormalizeName(_options.Name);
        notification.Key = entry.Key;
        notification.Value = entry.Value;
        notification.Reason = reason;
        notification.Weight = entry.Weight;
        notification.CreatedUtcTicks = entry.CreatedUtcTicks;
        notification.LastAccessUtcTicks = entry.LastAccessUtcTicks;
        notification.LastWriteUtcTicks = entry.LastWriteUtcTicks;
        notification.RemovedUtcTicks = removedUtcTicks;
        return notification;
    }

    internal void DeliverNotification(CacheNotification? notification)
    {
        if (notification == null)
        {
            return;
        }

        if (_removalListener == null)
        {
            return;
        }

        _removalListener(notification);
    }

    internal void DeliverNotifications(List<CacheNotification> notifications)
    {
        foreach (CacheNotification notification in notifications)
        {
            DeliverNotification(notification);
        }
    }

    internal void RecordHit()
    {
        if (_options.RecordStats)
        {
            _stats.Hits = CacheMath.Add(_stats.Hits, 1L);
        }
    }

    internal void RecordMiss()
    {
        if (_options.RecordStats)
        {
            _stats.Misses = CacheMath.Add(_stats.Misses, 1L);
        }
    }

    internal void RecordPut()
    {
        if (_options.RecordStats)
        {
            _stats.Puts = CacheMath.Add(_stats.Puts, 1L);
        }
    }

    internal void RecordUpdate()
    {
        if (_options.RecordStats)
        {
            _stats.Updates = CacheMath.Add(_stats.Updates, 1L);
        }
    }

    internal void RecordRemove()
    {
        if (_options.RecordStats)
        {
            _stats.Removes = CacheMath.Add(_stats.Removes, 1L);
        }
    }

    internal void RecordExpirations(long count)
    {
        if (_options.RecordStats)
        {
            _stats.Expirations = CacheMath.Add(_stats.Expirations, count);
        }
    }

    internal void RecordEviction()
    {
        if (_options.RecordStats)
        {
            _stats.Evictions = CacheMath.Add(_stats.Evictions, 1L);
        }
    }

    internal void RecordClear()
    {
        if (_options.RecordStats)
        {
            _stats.Clears = CacheMath.Add(_stats.Clears, 1L);
        }
    }

    internal void RecordLoadSuccess(long durationTicks)
    {
        lock (_sync)
        {
            if (_options.RecordStats)
            {
                _stats.LoadSuccesses = CacheMath.Add(_stats.LoadSuccesses, 1L);
                _stats.TotalLoadDurationTicks = CacheMath.Add(_stats.TotalLoadDurationTicks, durationTicks);
            }
        }
    }

    internal void RecordLoadFailure(long durationTicks)
    {
        lock (_sync)
        {
            if (_options.RecordStats)
            {
                _stats.LoadFailures = CacheMath.Add(_stats.LoadFailures, 1L);
                _stats.TotalLoadDurationTicks = CacheMath.Add(_stats.TotalLoadDurationTicks, durationTicks);
            }
        }
    }

    internal object? InvokeLoader(object key)
    {
        Func<object, object?>? loader = _loader;
        if (loader == null)
        {
            throw new InvalidOperationException("No cache loader is configured.");
        }

        long startedUtcTicks = _clock.GetUtcTicks();
        Task<object?> loaderTask = Task.Run(() => loader(key));
        Task recordingTask = loaderTask.ContinueWith(
            completedTask => RecordLoadCompletion(completedTask, startedUtcTicks),
            TaskContinuationOptions.ExecuteSynchronously);
        recordingTask.GetAwaiter().GetResult();
        return loaderTask.GetAwaiter().GetResult();
    }

    internal void RecordLoadCompletion(Task<object?> loaderTask, long startedUtcTicks)
    {
        long finishedUtcTicks = _clock.GetUtcTicks();
        long durationTicks = CacheMath.Subtract(finishedUtcTicks, startedUtcTicks);

        if (loaderTask.IsFaulted || loaderTask.IsCanceled)
        {
            RecordLoadFailure(durationTicks);
        }
        else
        {
            RecordLoadSuccess(durationTicks);
        }
    }

    internal double CalculateRate(long numerator, long denominator)
    {
        if (denominator == 0L)
        {
            return 0.0;
        }

        return (double)numerator / (double)denominator;
    }

    internal double CalculateRateFromCounts(long numerator, long otherCount)
    {
        double denominator = (double)numerator + (double)otherCount;

        if (denominator == 0.0)
        {
            return 0.0;
        }

        return (double)numerator / denominator;
    }

    internal void StartCleanupTimer()
    {
        if (!_options.HasCleanupInterval)
        {
            return;
        }

        int intervalMilliseconds = CleanupIntervalMilliseconds(_options.CleanupIntervalTicks);
        _cleanupTimer = new Timer(CleanupTimerTick, null, intervalMilliseconds, intervalMilliseconds);
    }

    internal int CleanupIntervalMilliseconds(long ticks)
    {
        long milliseconds = ticks / 10000L;

        if (milliseconds <= 0L)
        {
            return 1;
        }

        if (milliseconds > 2147483647L)
        {
            return 2147483647;
        }

        return (int)milliseconds;
    }

    internal void CleanupTimerTick(object? state)
    {
        lock (_sync)
        {
            if (_stopped)
            {
                return;
            }
        }

        Cleanup();
    }

    internal void RecordSegmentedFrequency(object key)
    {
        if (_options.EvictionPolicy != CacheEvictionPolicy.Segmented)
        {
            return;
        }

        if (_frequencies.ContainsKey(key))
        {
            _frequencies[key] = CacheMath.Add(_frequencies[key], 1L);
        }
        else
        {
            _frequencies[key] = 1L;
        }

        _frequencyEvents = CacheMath.Add(_frequencyEvents, 1L);
        if (_frequencyEvents >= SegmentedFrequencySampleSize())
        {
            AgeSegmentedFrequencies();
        }
    }

    internal long SegmentedFrequencySampleSize()
    {
        long capacity = 0L;
        if (_options.HasMaximumEntries)
        {
            capacity = _options.MaximumEntries;
        }
        else
        {
            capacity = _entries.Count;
        }

        if (capacity < 1L)
        {
            capacity = 1L;
        }

        if (capacity > long.MaxValue / 10L)
        {
            return long.MaxValue;
        }

        long sampleSize = capacity * 10L;
        if (sampleSize < 10L)
        {
            return 10L;
        }

        return sampleSize;
    }

    internal void AgeSegmentedFrequencies()
    {
        List<object> keys = new List<object>();
        foreach (KeyValuePair<object, long> pair in _frequencies)
        {
            keys.Add(pair.Key);
        }

        foreach (object key in keys)
        {
            long agedFrequency = _frequencies[key] / 2L;
            if (agedFrequency == 0L)
            {
                _frequencies.Remove(key);
            }
            else
            {
                _frequencies[key] = agedFrequency;
            }
        }

        _frequencyEvents = 0L;
    }

    internal long SegmentedFrequency(object key)
    {
        if (_frequencies.ContainsKey(key))
        {
            return _frequencies[key];
        }

        return 0L;
    }

    internal void RecordSegmentedHit(CacheEntry entry)
    {
        if (_options.EvictionPolicy != CacheEvictionPolicy.Segmented)
        {
            return;
        }

        if (entry.Segment == CacheEntrySegment.Window)
        {
            _segmentedWindowHits = CacheMath.Add(_segmentedWindowHits, 1L);
        }
        else
        {
            _segmentedMainHits = CacheMath.Add(_segmentedMainHits, 1L);
        }

        if (entry.Segment == CacheEntrySegment.Probation)
        {
            entry.Segment = CacheEntrySegment.Protected;
        }

        _segmentedAdaptationEvents = CacheMath.Add(_segmentedAdaptationEvents, 1L);
        bool weightBased = _options.HasMaximumWeight;
        MaintainSegmentedProtectedBalance(weightBased);

        if (_segmentedAdaptationEvents >= SegmentedAdaptationSampleSize())
        {
            AdaptSegmentedWindow(weightBased);
        }
    }

    internal long SegmentedAdaptationSampleSize()
    {
        long capacity = _options.HasMaximumEntries ? _options.MaximumEntries : _entries.Count;
        if (capacity < 1L)
        {
            capacity = 1L;
        }

        if (capacity > long.MaxValue / 4L)
        {
            return long.MaxValue;
        }

        long sampleSize = capacity * 4L;
        if (sampleSize < 4L)
        {
            return 4L;
        }

        return sampleSize;
    }

    internal void AdaptSegmentedWindow(bool weightBased)
    {
        long capacity = SegmentedCapacity(weightBased);
        long windowMaximum = SegmentedWindowMaximum(weightBased);
        long mainMaximum = CacheMath.Subtract(capacity, windowMaximum);
        double windowRate = 0.0;
        double mainRate = 0.0;

        if (windowMaximum > 0L)
        {
            windowRate = (double)_segmentedWindowHits / (double)windowMaximum;
        }

        if (mainMaximum > 0L)
        {
            mainRate = (double)_segmentedMainHits / (double)mainMaximum;
        }

        if (windowRate > mainRate && _segmentedWindowPercent < 50)
        {
            _segmentedWindowPercent = _segmentedWindowPercent + 5;
        }
        else if (mainRate > windowRate && _segmentedWindowPercent > 5)
        {
            _segmentedWindowPercent = _segmentedWindowPercent - 5;
        }

        _segmentedWindowHits = 0L;
        _segmentedMainHits = 0L;
        _segmentedAdaptationEvents = 0L;
        MaintainSegmentedSegments(weightBased);
    }

    internal long SegmentedCapacity(bool weightBased)
    {
        if (weightBased && _options.HasMaximumWeight)
        {
            return _options.MaximumWeight;
        }

        if (_options.HasMaximumEntries)
        {
            return _options.MaximumEntries;
        }

        if (_options.HasMaximumWeight)
        {
            return _options.MaximumWeight;
        }

        return _entries.Count;
    }

    internal long SegmentedWindowMaximum(bool weightBased)
    {
        long capacity = SegmentedCapacity(weightBased);
        if (capacity <= 1L)
        {
            return capacity;
        }

        long maximum = SegmentedScaledPercent(capacity, _segmentedWindowPercent);
        if (maximum < 1L)
        {
            maximum = 1L;
        }

        if (maximum >= capacity)
        {
            maximum = capacity - 1L;
        }

        return maximum;
    }

    internal long SegmentedProtectedMaximum(bool weightBased)
    {
        long capacity = SegmentedCapacity(weightBased);
        long mainMaximum = CacheMath.Subtract(capacity, SegmentedWindowMaximum(weightBased));
        if (mainMaximum <= 0L)
        {
            return 0L;
        }

        long maximum = SegmentedScaledPercent(mainMaximum, 80);
        if (maximum < 1L)
        {
            maximum = 1L;
        }

        if (maximum > mainMaximum)
        {
            maximum = mainMaximum;
        }

        return maximum;
    }

    internal long SegmentedScaledPercent(long value, int percent)
    {
        long quotient = value / 100L;
        long remainder = value % 100L;
        long scaledQuotient = quotient * percent;
        long scaledRemainder = remainder * percent / 100L;
        return CacheMath.Add(scaledQuotient, scaledRemainder);
    }

    internal object? MaintainSegmentedSegments(bool weightBased)
    {
        if (_options.EvictionPolicy != CacheEvictionPolicy.Segmented)
        {
            return null;
        }

        MaintainSegmentedProtectedBalance(weightBased);
        object? admissionCandidate = null;
        long windowMaximum = SegmentedWindowMaximum(weightBased);

        while (SegmentedUsage(CacheEntrySegment.Window, weightBased) > windowMaximum)
        {
            object? key = SelectSegmentedLru(CacheEntrySegment.Window, -1, null);
            if (key == null)
            {
                return admissionCandidate;
            }

            _entries[key].Segment = CacheEntrySegment.Probation;
            admissionCandidate = key;
        }

        return admissionCandidate;
    }

    internal void MaintainSegmentedProtectedBalance(bool weightBased)
    {
        if (_options.EvictionPolicy != CacheEvictionPolicy.Segmented)
        {
            return;
        }

        long protectedMaximum = SegmentedProtectedMaximum(weightBased);
        while (SegmentedUsage(CacheEntrySegment.Protected, weightBased) > protectedMaximum)
        {
            object? key = SelectSegmentedLru(CacheEntrySegment.Protected, -1, null);
            if (key == null)
            {
                return;
            }

            _entries[key].Segment = CacheEntrySegment.Probation;
        }
    }

    internal long SegmentedUsage(CacheEntrySegment segment, bool weightBased)
    {
        long usage = 0L;
        foreach (CacheEntry entry in _entries.Values)
        {
            if (entry.Segment == segment)
            {
                if (weightBased)
                {
                    usage = CacheMath.Add(usage, entry.Weight);
                }
                else
                {
                    usage = CacheMath.Add(usage, 1L);
                }
            }
        }

        return usage;
    }

    internal object? SelectSegmentedLru(CacheEntrySegment segment, int priorityRank, object? excludedKey)
    {
        object? selectedKey = null;
        CacheEntry? selectedEntry = null;

        foreach (KeyValuePair<object, CacheEntry> pair in _entries)
        {
            if (pair.Value.Segment != segment)
            {
                continue;
            }

            if (priorityRank >= 0 && PriorityRank(pair.Value.Priority) != priorityRank)
            {
                continue;
            }

            if (excludedKey != null && _keyComparer.Equals(pair.Key, excludedKey))
            {
                continue;
            }

            if (selectedEntry == null || IsSegmentedOlder(pair.Value, selectedEntry))
            {
                selectedKey = pair.Key;
                selectedEntry = pair.Value;
            }
        }

        return selectedKey;
    }

    internal bool IsSegmentedOlder(CacheEntry left, CacheEntry right)
    {
        if (left.LastAccessUtcTicks < right.LastAccessUtcTicks)
        {
            return true;
        }

        if (left.LastAccessUtcTicks > right.LastAccessUtcTicks)
        {
            return false;
        }

        return left.Version < right.Version;
    }

    internal int LowestSegmentedPriorityRank()
    {
        int lowestRank = 2;
        bool found = false;

        foreach (CacheEntry entry in _entries.Values)
        {
            int rank = PriorityRank(entry.Priority);
            if (!found || rank < lowestRank)
            {
                lowestRank = rank;
                found = true;
            }
        }

        return lowestRank;
    }

    internal object SelectSegmentedEvictionKey(bool weightBased, object? admissionCandidate)
    {
        int lowestPriorityRank = LowestSegmentedPriorityRank();
        if (admissionCandidate != null && _entries.ContainsKey(admissionCandidate))
        {
            CacheEntry candidateEntry = _entries[admissionCandidate];
            int candidatePriorityRank = PriorityRank(candidateEntry.Priority);
            if (candidatePriorityRank == lowestPriorityRank)
            {
                object? probationVictim = SelectSegmentedLru(CacheEntrySegment.Probation, lowestPriorityRank, admissionCandidate);
                if (probationVictim == null)
                {
                    return admissionCandidate;
                }

                long candidateFrequency = SegmentedFrequency(admissionCandidate);
                long victimFrequency = SegmentedFrequency(probationVictim);
                if (candidateFrequency > victimFrequency)
                {
                    return probationVictim;
                }

                return admissionCandidate;
            }
        }

        object? selectedKey = SelectSegmentedLru(CacheEntrySegment.Probation, lowestPriorityRank, null);
        if (selectedKey == null)
        {
            selectedKey = SelectSegmentedLru(CacheEntrySegment.Window, lowestPriorityRank, null);
        }

        if (selectedKey == null)
        {
            selectedKey = SelectSegmentedLru(CacheEntrySegment.Protected, lowestPriorityRank, null);
        }

        if (selectedKey == null)
        {
            throw new InvalidOperationException("No segmented eviction candidate was found.");
        }

        return selectedKey;
    }

    internal object SelectEvictionKey(bool weightBased, object? admissionCandidate)
    {
        if (_options.EvictionPolicy == CacheEvictionPolicy.Segmented)
        {
            return SelectSegmentedEvictionKey(weightBased, admissionCandidate);
        }

        if (_options.EvictionPolicy == CacheEvictionPolicy.Random)
        {
            return SelectRandomEvictionKey();
        }

        object? selectedKey = null;
        int selectedPriorityRank = 0;
        long selectedPolicyScore = 0L;
        long selectedTieTicks = 0L;
        long selectedVersion = 0L;
        bool foundCandidate = false;

        foreach (KeyValuePair<object, CacheEntry> pair in _entries)
        {
            int priorityRank = PriorityRank(pair.Value.Priority);
            long policyScore = EvictionPolicyScore(pair.Value);
            long tieTicks = EvictionPolicyTieTicks(pair.Value);

            if (!foundCandidate)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                selectedPolicyScore = policyScore;
                selectedTieTicks = tieTicks;
                selectedVersion = pair.Value.Version;
                foundCandidate = true;
                continue;
            }

            if (priorityRank < selectedPriorityRank)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                selectedPolicyScore = policyScore;
                selectedTieTicks = tieTicks;
                selectedVersion = pair.Value.Version;
                continue;
            }

            if (priorityRank == selectedPriorityRank && policyScore < selectedPolicyScore)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                selectedPolicyScore = policyScore;
                selectedTieTicks = tieTicks;
                selectedVersion = pair.Value.Version;
                continue;
            }

            if (priorityRank == selectedPriorityRank && policyScore == selectedPolicyScore && tieTicks < selectedTieTicks)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                selectedPolicyScore = policyScore;
                selectedTieTicks = tieTicks;
                selectedVersion = pair.Value.Version;
                continue;
            }

            if (priorityRank == selectedPriorityRank && policyScore == selectedPolicyScore && tieTicks == selectedTieTicks && pair.Value.Version < selectedVersion)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                selectedPolicyScore = policyScore;
                selectedTieTicks = tieTicks;
                selectedVersion = pair.Value.Version;
            }
        }

        if (selectedKey == null)
        {
            throw new InvalidOperationException("No eviction candidate was found.");
        }

        return selectedKey;
    }

    internal object SelectRandomEvictionKey()
    {
        object? selectedKey = null;
        int selectedPriorityRank = 0;
        int candidateCount = 0;
        bool foundCandidate = false;

        foreach (KeyValuePair<object, CacheEntry> pair in _entries)
        {
            int priorityRank = PriorityRank(pair.Value.Priority);

            if (!foundCandidate)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                candidateCount = 1;
                foundCandidate = true;
                continue;
            }

            if (priorityRank < selectedPriorityRank)
            {
                selectedKey = pair.Key;
                selectedPriorityRank = priorityRank;
                candidateCount = 1;
                continue;
            }

            if (priorityRank == selectedPriorityRank)
            {
                candidateCount = candidateCount + 1;
                if (_random.Next(candidateCount) == 0)
                {
                    selectedKey = pair.Key;
                }
            }
        }

        if (selectedKey == null)
        {
            throw new InvalidOperationException("No eviction candidate was found.");
        }

        return selectedKey;
    }

    internal long EvictionPolicyScore(CacheEntry entry)
    {
        switch (_options.EvictionPolicy)
        {
            case CacheEvictionPolicy.Lfu:
                return entry.AccessCount;
            case CacheEvictionPolicy.Fifo:
                return entry.CreatedUtcTicks;
            default:
                return entry.LastAccessUtcTicks;
        }
    }

    internal long EvictionPolicyTieTicks(CacheEntry entry)
    {
        switch (_options.EvictionPolicy)
        {
            case CacheEvictionPolicy.Lfu:
                return entry.LastAccessUtcTicks;
            default:
                return 0L;
        }
    }

    internal long InitialAccessCount()
    {
        if (_options.EvictionPolicy == CacheEvictionPolicy.Lfu)
        {
            return 1L;
        }

        return 0L;
    }

    internal int PriorityRank(CacheEntryPriority priority)
    {
        switch (priority)
        {
            case CacheEntryPriority.Low:
                return 0;
            case CacheEntryPriority.Normal:
                return 1;
            case CacheEntryPriority.High:
                return 2;
            default:
                return 1;
        }
    }

    internal bool IsEntryExpired(CacheEntry entry, long nowUtcTicks)
    {
        if (entry.HasAbsoluteExpiration)
        {
            if (nowUtcTicks >= entry.AbsoluteExpirationUtcTicks)
            {
                return true;
            }
        }

        if (entry.HasSlidingExpiration)
        {
            long slidingExpirationUtcTicks = CacheExpiration.AddTicks(entry.LastAccessUtcTicks, entry.SlidingExpirationTicks);
            if (nowUtcTicks >= slidingExpirationUtcTicks)
            {
                return true;
            }
        }

        return false;
    }

    internal void FillGetResultFromEntry(CacheGetResult result, CacheEntry entry)
    {
        result.Found = true;
        result.Key = entry.Key;
        result.Value = entry.Value;
        result.Status = CacheResultStatus.Hit;
        result.Reason = CacheRemovalReason.Unknown;
        result.Loaded = false;
        result.HasExpiration = entry.HasAbsoluteExpiration || entry.HasSlidingExpiration;
        result.CreatedUtcTicks = entry.CreatedUtcTicks;
        result.LastAccessUtcTicks = entry.LastAccessUtcTicks;
        result.LastWriteUtcTicks = entry.LastWriteUtcTicks;
        result.ExpirationUtcTicks = CalculateEntryExpirationUtcTicks(entry);
    }

    internal long CalculateEntryExpirationUtcTicks(CacheEntry entry)
    {
        bool foundExpiration = false;
        long expirationUtcTicks = 0L;

        if (entry.HasAbsoluteExpiration)
        {
            foundExpiration = true;
            expirationUtcTicks = entry.AbsoluteExpirationUtcTicks;
        }

        if (entry.HasSlidingExpiration)
        {
            long slidingExpirationUtcTicks = CacheExpiration.AddTicks(entry.LastAccessUtcTicks, entry.SlidingExpirationTicks);
            if (!foundExpiration || slidingExpirationUtcTicks < expirationUtcTicks)
            {
                foundExpiration = true;
                expirationUtcTicks = slidingExpirationUtcTicks;
            }
        }

        if (foundExpiration)
        {
            return expirationUtcTicks;
        }

        return 0L;
    }
}
