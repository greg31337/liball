using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace template;

/// <summary>
/// Caches reusable parsed templates using source identity and parse-affecting options in a thread-safe, capacity-bounded store.
/// </summary>
public class TemplateCompilationCache : ITemplateCompilationCache
{
    internal Dictionary<string, TemplateCompiledTemplate> Entries;
    internal Dictionary<string, TemplateSource> EntrySources;
    internal List<string> UsageOrder;
    internal object SyncRoot;
    internal int Capacity;
    internal int HitCount;
    internal int MissCount;
    internal int EvictionCount;
    internal int InvalidationCount;
    internal TemplateProfileRegistry ProfileRegistry;

    /// <summary>
    /// Initializes an empty cache with a capacity of 128 compiled templates.
    /// </summary>
    public TemplateCompilationCache()
    {
        Entries = new Dictionary<string, TemplateCompiledTemplate>();
        EntrySources = new Dictionary<string, TemplateSource>();
        UsageOrder = new List<string>();
        SyncRoot = new object();
        Capacity = 128;
        HitCount = 0;
        MissCount = 0;
        EvictionCount = 0;
        InvalidationCount = 0;
        ProfileRegistry = new TemplateProfileRegistry();
    }

    /// <summary>
    /// Sets the profile registry used to select delimiters and compatibility conversion during compilation.
    /// </summary>
    /// <param name="profileRegistry">The profile registry to use.</param>
    public void SetProfileRegistry(TemplateProfileRegistry profileRegistry)
    {
        if (profileRegistry == null)
        {
            throw new ArgumentNullException("profileRegistry");
        }

        lock (SyncRoot)
        {
            ProfileRegistry = profileRegistry;
            ClearCore();
        }
    }

    /// <summary>
    /// Returns a cached compiled template or compiles and stores it atomically when absent.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="options">The options that control compilation.</param>
    /// <returns>The reusable compiled template.</returns>
    public TemplateCompiledTemplate GetOrCompile(TemplateSource source, TemplateOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        source.Validate();
        lock (SyncRoot)
        {
            string key = CreateKeyCore(source, options);
            if (Entries.ContainsKey(key))
            {
                IncrementHitCount();
                TouchEntry(key);
                return Entries[key];
            }

            TemplateSource storedSource = CopySource(source);
            TemplateParseResult parseResult = ProfileRegistry.ParseSource(storedSource, options.SyntaxProfile);
            TemplateCompiledTemplate compiled = new TemplateCompiledTemplate(storedSource, parseResult);
            while (Entries.Count >= Capacity)
            {
                EvictOldestEntry();
            }

            Entries[key] = compiled;
            EntrySources[key] = storedSource;
            UsageOrder.Add(key);
            IncrementMissCount();
            return compiled;
        }
    }

    /// <summary>
    /// Sets the maximum number of compiled templates retained by the cache and evicts least-recently-used entries when necessary.
    /// </summary>
    /// <param name="capacity">The positive maximum entry count.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the capacity is less than one.</exception>
    public void SetCapacity(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException("capacity");
        }

        lock (SyncRoot)
        {
            Capacity = capacity;
            while (Entries.Count > Capacity)
            {
                EvictOldestEntry();
            }
        }
    }

    /// <summary>
    /// Returns the maximum number of compiled templates retained by the cache.
    /// </summary>
    /// <returns>The configured positive capacity.</returns>
    public int GetCapacity()
    {
        lock (SyncRoot)
        {
            return Capacity;
        }
    }

    /// <summary>
    /// Invalidates every entry with the same path identity, or the same logical name when no path is supplied.
    /// </summary>
    /// <param name="source">The source identity to invalidate.</param>
    /// <returns>The number of removed entries.</returns>
    public int InvalidateSource(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        lock (SyncRoot)
        {
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, TemplateSource> pair in EntrySources)
            {
                if (HasSameSourceIdentity(pair.Value, source))
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveInvalidatedEntries(keys);
        }
    }

    /// <summary>
    /// Invalidates every entry carrying the supplied source version.
    /// </summary>
    /// <param name="version">The version value to invalidate.</param>
    /// <returns>The number of removed entries.</returns>
    public int InvalidateVersion(string version)
    {
        if (version == null)
        {
            throw new ArgumentNullException("version");
        }

        lock (SyncRoot)
        {
            return RemoveInvalidatedEntries(FindMetadataKeys(version, 1));
        }
    }

    /// <summary>
    /// Invalidates every entry carrying the supplied content hash.
    /// </summary>
    /// <param name="contentHash">The content hash to invalidate.</param>
    /// <returns>The number of removed entries.</returns>
    public int InvalidateContentHash(string contentHash)
    {
        if (contentHash == null)
        {
            throw new ArgumentNullException("contentHash");
        }

        lock (SyncRoot)
        {
            return RemoveInvalidatedEntries(FindMetadataKeys(contentHash, 2));
        }
    }

    /// <summary>
    /// Invalidates every entry carrying the supplied source timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp value to invalidate.</param>
    /// <returns>The number of removed entries.</returns>
    public int InvalidateTimestamp(string timestamp)
    {
        if (timestamp == null)
        {
            throw new ArgumentNullException("timestamp");
        }

        lock (SyncRoot)
        {
            return RemoveInvalidatedEntries(FindMetadataKeys(timestamp, 3));
        }
    }

    /// <summary>
    /// Removes all stored entries and resets all cache counters.
    /// </summary>
    public void Clear()
    {
        lock (SyncRoot)
        {
            ClearCore();
        }
    }

    /// <summary>
    /// Returns the number of entries currently stored.
    /// </summary>
    /// <returns>The current entry count.</returns>
    public int Count()
    {
        lock (SyncRoot)
        {
            return Entries.Count;
        }
    }

    /// <summary>
    /// Creates a deterministic key from source identity, source text, timestamp, and every parse-affecting option.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="options">The options that control compilation.</param>
    /// <returns>The deterministic cache key.</returns>
    public string CreateKey(TemplateSource source, TemplateOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        source.Validate();
        lock (SyncRoot)
        {
            return CreateKeyCore(source, options);
        }
    }

    /// <summary>
    /// Returns the number of cache hits.
    /// </summary>
    /// <returns>The saturated hit count.</returns>
    public int GetHitCount()
    {
        lock (SyncRoot)
        {
            return HitCount;
        }
    }

    /// <summary>
    /// Returns the number of cache misses.
    /// </summary>
    /// <returns>The saturated miss count.</returns>
    public int GetMissCount()
    {
        lock (SyncRoot)
        {
            return MissCount;
        }
    }

    /// <summary>
    /// Returns the number of entries removed to satisfy capacity.
    /// </summary>
    /// <returns>The saturated eviction count.</returns>
    public int GetEvictionCount()
    {
        lock (SyncRoot)
        {
            return EvictionCount;
        }
    }

    /// <summary>
    /// Returns the number of entries removed by explicit invalidation operations.
    /// </summary>
    /// <returns>The saturated invalidation count.</returns>
    public int GetInvalidationCount()
    {
        lock (SyncRoot)
        {
            return InvalidationCount;
        }
    }

    internal string CreateKeyCore(TemplateSource source, TemplateOptions options)
    {
        StringBuilder builder = new StringBuilder();
        AppendSourceKey(builder, source);
        AppendOptionsKey(builder, options);
        return builder.ToString();
    }

    internal void AppendSourceKey(StringBuilder builder, TemplateSource source)
    {
        AppendKeyPart(builder, source.Name);
        AppendKeyPart(builder, source.Path);
        AppendKeyPart(builder, source.Version);
        AppendKeyPart(builder, source.ContentHash);
        AppendKeyPart(builder, source.Timestamp);
        AppendKeyPart(builder, source.Length.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, source.Text);
    }

    internal void AppendOptionsKey(StringBuilder builder, TemplateOptions options)
    {
        AppendKeyPart(builder, options.SyntaxProfile);
        ProfileRegistry.AppendProfileKey(builder, options.SyntaxProfile);
        AppendKeyPart(builder, ((int)options.CompatibilityMode).ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, GetBooleanKey(options.StrictMode));
        AppendKeyPart(builder, GetBooleanKey(options.StrictMissingValues));
        AppendKeyPart(builder, GetBooleanKey(options.StrictTypeConversions));
        AppendKeyPart(builder, GetBooleanKey(options.StrictUnknownFunctions));
        AppendKeyPart(builder, GetBooleanKey(options.StrictUnsafeMembers));
        AppendKeyPart(builder, ((int)options.WhitespaceMode).ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, GetBooleanKey(options.TrimFinalNewline));
        AppendKeyPart(builder, GetBooleanKey(options.AutoIndentMultilineValues));
        AppendKeyPart(builder, ((int)options.EscapingMode).ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, GetBooleanKey(options.AutoEscape));
        AppendKeyPart(builder, ((int)options.TruthinessMode).ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, ((int)options.NameLookupOrder).ToString(CultureInfo.InvariantCulture));
        if (options.Culture == null)
        {
            AppendKeyPart(builder, "");
        }
        else
        {
            AppendKeyPart(builder, options.Culture.Name);
        }

        AppendKeyPart(builder, options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumOutputLength.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumIncludeDepth.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumLoopIterations.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumExpressionDepth.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumRecursionDepth.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumTemplateCount.ToString(CultureInfo.InvariantCulture));
        AppendKeyPart(builder, options.MaximumRenderMilliseconds.ToString(CultureInfo.InvariantCulture));
    }

    internal void TouchEntry(string key)
    {
        UsageOrder.Remove(key);
        UsageOrder.Add(key);
    }

    internal void EvictOldestEntry()
    {
        if (UsageOrder.Count == 0)
        {
            return;
        }

        string key = UsageOrder[0];
        UsageOrder.RemoveAt(0);
        Entries.Remove(key);
        EntrySources.Remove(key);
        IncrementEvictionCount();
    }

    internal int RemoveInvalidatedEntries(List<string> keys)
    {
        int removed = 0;
        int index = 0;
        while (index < keys.Count)
        {
            string key = keys[index];
            if (Entries.Remove(key))
            {
                EntrySources.Remove(key);
                UsageOrder.Remove(key);
                IncrementInvalidationCount();
                removed = removed + 1;
            }

            index = index + 1;
        }

        return removed;
    }

    internal List<string> FindMetadataKeys(string value, int metadataKind)
    {
        List<string> keys = new List<string>();
        foreach (KeyValuePair<string, TemplateSource> pair in EntrySources)
        {
            string? candidate = pair.Value.Version;
            if (metadataKind == 2)
            {
                candidate = pair.Value.ContentHash;
            }
            else if (metadataKind == 3)
            {
                candidate = pair.Value.Timestamp;
            }

            if (string.Equals(candidate, value, StringComparison.Ordinal))
            {
                keys.Add(pair.Key);
            }
        }

        return keys;
    }

    internal bool HasSameSourceIdentity(TemplateSource left, TemplateSource right)
    {
        if (!string.IsNullOrEmpty(right.Path))
        {
            return string.Equals(left.Path, right.Path, StringComparison.Ordinal);
        }

        return string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    internal TemplateSource CopySource(TemplateSource source)
    {
        TemplateSource copy = new TemplateSource(source.Name, source.Path, source.Version, source.ContentHash, source.Text);
        copy.Timestamp = source.Timestamp;
        return copy;
    }

    internal void ClearCore()
    {
        Entries.Clear();
        EntrySources.Clear();
        UsageOrder.Clear();
        HitCount = 0;
        MissCount = 0;
        EvictionCount = 0;
        InvalidationCount = 0;
    }

    internal void IncrementHitCount()
    {
        if (HitCount < 2147483647)
        {
            HitCount = HitCount + 1;
        }
    }

    internal void IncrementMissCount()
    {
        if (MissCount < 2147483647)
        {
            MissCount = MissCount + 1;
        }
    }

    internal void IncrementEvictionCount()
    {
        if (EvictionCount < 2147483647)
        {
            EvictionCount = EvictionCount + 1;
        }
    }

    internal void IncrementInvalidationCount()
    {
        if (InvalidationCount < 2147483647)
        {
            InvalidationCount = InvalidationCount + 1;
        }
    }

    internal string GetBooleanKey(bool value)
    {
        if (value)
        {
            return "1";
        }

        return "0";
    }

    internal void AppendKeyPart(StringBuilder builder, string? value)
    {
        if (value == null)
        {
            builder.Append("-1:");
        }
        else
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(":");
            builder.Append(value);
        }

        builder.Append("|");
    }
}
