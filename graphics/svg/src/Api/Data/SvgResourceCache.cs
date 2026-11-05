namespace svg;

/// <summary>Contains one independently owned normalized resource-cache entry.</summary>
public class SvgResourceCacheEntry
{
    /// <summary>The normalized fragment-free initial URI key.</summary>
    public string Key;
    /// <summary>The normalized fragment-free final URI key.</summary>
    public string FinalKey;
    /// <summary>The normalized initial URI.</summary>
    public string NormalizedUri;
    /// <summary>The normalized final URI.</summary>
    public string FinalUri;
    /// <summary>Whether the cached acquisition succeeded.</summary>
    public bool Success;
    /// <summary>The independently owned bytes, or an empty array for a cached failure.</summary>
    public byte[] Bytes;
    /// <summary>The normalized caller-declared media type.</summary>
    public string DeclaredMediaType;
    /// <summary>The normalized caller-detected media type.</summary>
    public string ActualMediaType;
    /// <summary>The caller-managed redirect count.</summary>
    public int RedirectCount;
    /// <summary>Whether the successful caller response was cacheable.</summary>
    public bool Cacheable;
    /// <summary>The independently owned callback image metadata, or <c>null</c>.</summary>
    public SvgImageMetadata? ImageMetadata;
    /// <summary>The ordered acquisition diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    internal SvgSourceSpan? RequestSource;
    internal long AccessOrder;

    internal SvgResourceCacheEntry(string key, string finalKey, SvgResourceAcquisitionResult? result, long accessOrder, SvgResourceCacheEntry? source)
    {
        if ((result == null) == (source == null)) throw new System.ArgumentException("Exactly one cache-entry source is required.");
        if (source != null)
        {
            Key = source.Key;
            FinalKey = source.FinalKey;
            NormalizedUri = source.NormalizedUri;
            FinalUri = source.FinalUri;
            Success = source.Success;
            Bytes = CopyBytes(source.Bytes);
            DeclaredMediaType = source.DeclaredMediaType;
            ActualMediaType = source.ActualMediaType;
            RedirectCount = source.RedirectCount;
            Cacheable = source.Cacheable;
            ImageMetadata = CopyImageMetadata(source.ImageMetadata);
            Diagnostics = CopyDiagnostics(source.Diagnostics);
            RequestSource = source.RequestSource;
            AccessOrder = source.AccessOrder;
            return;
        }
        if (result == null) throw new System.ArgumentNullException("result");
        Key = key;
        FinalKey = finalKey;
        NormalizedUri = result.NormalizedUri;
        FinalUri = result.FinalUri;
        Success = result.Success;
        Bytes = result.Success ? CopyBytes(result.Bytes) : System.Array.Empty<byte>();
        DeclaredMediaType = result.DeclaredMediaType;
        ActualMediaType = result.ActualMediaType;
        RedirectCount = result.RedirectCount;
        Cacheable = result.Cacheable;
        ImageMetadata = CopyImageMetadata(result.ImageMetadata);
        Diagnostics = CopyDiagnostics(result.Diagnostics);
        RequestSource = result.Request == null ? null : result.Request.Source;
        AccessOrder = accessOrder;
    }

    internal static SvgResourceCacheEntry Create(string key, string finalKey, SvgResourceAcquisitionResult result, long accessOrder)
    {
        return new SvgResourceCacheEntry(key, finalKey, result, accessOrder, null);
    }

    internal static SvgResourceCacheEntry Copy(SvgResourceCacheEntry source)
    {
        return new SvgResourceCacheEntry("", "", null, 0, source);
    }

    internal static byte[] CopyBytes(byte[]? source)
    {
        if (source == null || source.Length == 0) return System.Array.Empty<byte>();
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, 0, copy, 0, source.Length);
        return copy;
    }

    internal static System.Collections.Generic.List<SvgDiagnostic> CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic>? source)
    {
        System.Collections.Generic.List<SvgDiagnostic> copy = new System.Collections.Generic.List<SvgDiagnostic>();
        if (source == null) return copy;
        int index = 0;
        while (index < source.Count)
        {
            SvgDiagnostic diagnostic = source[index];
            if (diagnostic != null) copy.Add(CopyDiagnostic(diagnostic));
            index = index + 1;
        }
        return copy;
    }

    internal static SvgDiagnostic CopyDiagnostic(SvgDiagnostic source)
    {
        SvgDiagnostic copy = new SvgDiagnostic(source.Severity, source.Code, source.Message, source.Source);
        copy.ResourcePath = source.ResourcePath;
        copy.InstancePath = source.InstancePath;
        copy.Sequence = source.Sequence;
        int index = 0;
        while (index < source.RelatedSpans.Count)
        {
            copy.AddRelatedSpan(source.RelatedSpans[index]);
            index = index + 1;
        }
        return copy;
    }

    internal static SvgImageMetadata? CopyImageMetadata(SvgImageMetadata? source)
    {
        if (source == null) return null;
        SvgImageMetadata result = new SvgImageMetadata();
        result.DecodedContentAvailable = source.DecodedContentAvailable;
        if (source.IntrinsicSize != null)
        {
            result.IntrinsicSize = new SvgIntrinsicSize();
            result.IntrinsicSize.HasWidth = source.IntrinsicSize.HasWidth;
            result.IntrinsicSize.Width = source.IntrinsicSize.Width;
            result.IntrinsicSize.HasHeight = source.IntrinsicSize.HasHeight;
            result.IntrinsicSize.Height = source.IntrinsicSize.Height;
            result.IntrinsicSize.HasAspectRatio = source.IntrinsicSize.HasAspectRatio;
            result.IntrinsicSize.AspectRatio = source.IntrinsicSize.AspectRatio;
        }
        return result;
    }
}

/// <summary>Contains one normalized resource-cache lookup result.</summary>
public class SvgResourceCacheLookup
{
    /// <summary>Whether a success or failure entry was found.</summary>
    public bool Found;
    /// <summary>The independent entry snapshot, or <c>null</c> after a miss.</summary>
    public SvgResourceCacheEntry? Entry;

    internal SvgResourceCacheLookup(bool found, SvgResourceCacheEntry? entry)
    {
        Found = found;
        Entry = entry;
    }
}

/// <summary>Contains a snapshot of bounded resource-cache activity.</summary>
public class SvgResourceCacheStatistics
{
    /// <summary>The configured maximum entry count.</summary>
    public int MaxEntries;
    /// <summary>The configured maximum successful-entry byte count.</summary>
    public long MaxBytes;
    /// <summary>The current entry count.</summary>
    public int EntryCount;
    /// <summary>The current successful-entry byte count.</summary>
    public long ByteCount;
    /// <summary>The current cached failure count.</summary>
    public int FailureEntryCount;
    /// <summary>The cumulative cache hit count.</summary>
    public long HitCount;
    /// <summary>The cumulative cache miss count.</summary>
    public long MissCount;
    /// <summary>The cumulative accepted addition count.</summary>
    public long AdditionCount;
    /// <summary>The cumulative capacity eviction count.</summary>
    public long EvictionCount;
    /// <summary>The cumulative explicit removal count.</summary>
    public long RemovalCount;

    internal SvgResourceCacheStatistics(int maxEntries, long maxBytes, int entryCount, long byteCount, int failureEntryCount, long hitCount, long missCount, long additionCount, long evictionCount, long removalCount)
    {
        MaxEntries = maxEntries;
        MaxBytes = maxBytes;
        EntryCount = entryCount;
        ByteCount = byteCount;
        FailureEntryCount = failureEntryCount;
        HitCount = hitCount;
        MissCount = missCount;
        AdditionCount = additionCount;
        EvictionCount = evictionCount;
        RemovalCount = removalCount;
    }
}
