namespace svg;

/// <summary>Caches bounded success and failure snapshots by normalized fragment-free URI.</summary>
public class SvgResourceCache : ISvgResourceCache
{
    System.Collections.Generic.List<SvgResourceCacheEntry> Entries;
    int MaximumEntries;
    long MaximumBytes;
    long StoredBytes;
    long HitCount;
    long MissCount;
    long AdditionCount;
    long EvictionCount;
    long RemovalCount;
    long AccessClock;

    /// <summary>Creates an empty cache with explicit entry and byte capacities.</summary>
    /// <param name="maxEntries">The positive maximum entry count.</param>
    /// <param name="maxBytes">The positive maximum successful-entry byte count.</param>
    public SvgResourceCache(int maxEntries, long maxBytes)
    {
        if (maxEntries < 1) throw new System.ArgumentOutOfRangeException("maxEntries");
        if (maxBytes < 1) throw new System.ArgumentOutOfRangeException("maxBytes");
        Entries = new System.Collections.Generic.List<SvgResourceCacheEntry>();
        MaximumEntries = maxEntries;
        MaximumBytes = maxBytes;
        StoredBytes = 0;
        HitCount = 0;
        MissCount = 0;
        AdditionCount = 0;
        EvictionCount = 0;
        RemovalCount = 0;
        AccessClock = 0;
    }

    /// <summary>Gets an independent snapshot by normalized initial or final URI.</summary>
    /// <param name="normalizedUri">The normalized URI, with an optional fragment.</param>
    /// <returns>A hit containing an independent entry snapshot, or a miss.</returns>
    public SvgResourceCacheLookup Get(string normalizedUri)
    {
        string key = CacheKey(normalizedUri);
        lock (Entries)
        {
            int index = key.Length == 0 ? -1 : Find(key);
            if (index < 0)
            {
                MissCount = MissCount + 1;
                return new SvgResourceCacheLookup(false, null);
            }
            HitCount = HitCount + 1;
            Entries[index].AccessOrder = NextAccess();
            return new SvgResourceCacheLookup(true, SvgResourceCacheEntry.Copy(Entries[index]));
        }
    }

    /// <summary>Adds or replaces a successful or failed acquisition snapshot.</summary>
    /// <param name="normalizedUri">The normalized initial URI used as the cache identity.</param>
    /// <param name="result">The completed acquisition result to snapshot.</param>
    /// <returns><c>true</c> when the entry fit within the configured capacities.</returns>
    public bool Add(string normalizedUri, SvgResourceAcquisitionResult result)
    {
        if (result == null) throw new System.ArgumentNullException("result");
        if (result.NormalizedUri == null || result.FinalUri == null || result.Bytes == null || result.DeclaredMediaType == null || result.ActualMediaType == null || result.Diagnostics == null || result.RedirectCount < 0) return false;
        int diagnosticIndex = 0;
        while (diagnosticIndex < result.Diagnostics.Count)
        {
            if (result.Diagnostics[diagnosticIndex] == null) return false;
            diagnosticIndex = diagnosticIndex + 1;
        }
        string key = CacheKey(normalizedUri);
        string finalKey = CacheKey(result.FinalUri);
        if (key.Length == 0 || finalKey.Length == 0) return false;
        long bytes = result.Success ? result.Bytes.LongLength : 0;
        if (bytes > MaximumBytes) return false;
        lock (Entries)
        {
            RemoveAliases(key, finalKey);
            while (Entries.Count >= MaximumEntries || StoredBytes > MaximumBytes - bytes) EvictOldest();
            Entries.Add(SvgResourceCacheEntry.Create(key, finalKey, result, NextAccess()));
            StoredBytes = StoredBytes + bytes;
            AdditionCount = AdditionCount + 1;
            return true;
        }
    }

    /// <summary>Removes an entry addressed by its normalized initial or final URI.</summary>
    /// <param name="normalizedUri">The normalized URI, with an optional fragment.</param>
    /// <returns><c>true</c> when an entry was removed.</returns>
    public bool Remove(string normalizedUri)
    {
        string key = CacheKey(normalizedUri);
        lock (Entries)
        {
            int index = key.Length == 0 ? -1 : Find(key);
            if (index < 0) return false;
            RemoveAt(index);
            RemovalCount = RemovalCount + 1;
            return true;
        }
    }

    /// <summary>Clears every entry while retaining cumulative counters.</summary>
    /// <returns>The removed entry count.</returns>
    public int Clear()
    {
        lock (Entries)
        {
            int removed = Entries.Count;
            Entries.Clear();
            StoredBytes = 0;
            RemovalCount = RemovalCount + removed;
            return removed;
        }
    }

    /// <summary>Returns an independent capacity and activity snapshot.</summary>
    /// <returns>The current cache statistics.</returns>
    public SvgResourceCacheStatistics GetStatistics()
    {
        lock (Entries)
        {
            int failures = 0;
            int index = 0;
            while (index < Entries.Count)
            {
                if (!Entries[index].Success) failures = failures + 1;
                index = index + 1;
            }
            return new SvgResourceCacheStatistics(MaximumEntries, MaximumBytes, Entries.Count, StoredBytes, failures, HitCount, MissCount, AdditionCount, EvictionCount, RemovalCount);
        }
    }

    static string CacheKey(string? value)
    {
        if (value == null || value.Length == 0) return "";
        SvgUriResult normalized = new SvgUri().NormalizeWithValueAndMaxCharacters(value, 268435455);
        return normalized.Success ? normalized.CacheKey : "";
    }

    int Find(string key)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            SvgResourceCacheEntry entry = Entries[index];
            if (entry.Key == key || entry.FinalKey == key) return index;
            index = index + 1;
        }
        return -1;
    }

    void RemoveAliases(string key, string finalKey)
    {
        int index = Entries.Count - 1;
        while (index >= 0)
        {
            SvgResourceCacheEntry entry = Entries[index];
            if (entry.Key == key || entry.FinalKey == key || entry.Key == finalKey || entry.FinalKey == finalKey) RemoveAt(index);
            index = index - 1;
        }
    }

    void EvictOldest()
    {
        int oldest = 0;
        int index = 1;
        while (index < Entries.Count)
        {
            if (Entries[index].AccessOrder < Entries[oldest].AccessOrder) oldest = index;
            index = index + 1;
        }
        RemoveAt(oldest);
        EvictionCount = EvictionCount + 1;
    }

    void RemoveAt(int index)
    {
        if (Entries[index].Success) StoredBytes = StoredBytes - Entries[index].Bytes.LongLength;
        Entries.RemoveAt(index);
    }

    long NextAccess()
    {
        if (AccessClock == long.MaxValue)
        {
            int index = 0;
            while (index < Entries.Count)
            {
                Entries[index].AccessOrder = index + 1;
                index = index + 1;
            }
            AccessClock = Entries.Count;
        }
        AccessClock = AccessClock + 1;
        return AccessClock;
    }
}
