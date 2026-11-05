namespace svg;

/// <summary>Configures one synchronous caller-mediated resource acquisition.</summary>
public class SvgResourceAcquisitionOptions
{
    /// <summary>The optional synchronous provider for non-data resources.</summary>
    public ISvgResourceProvider? ResourceProvider;
    /// <summary>The resource byte, URI, redirect, and callback limits.</summary>
    public SvgResourceLimits Limits;
    /// <summary>The synchronous caller cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>Whether a caller response byte array is copied before returning.</summary>
    public bool CopyInputData;
    /// <summary>An additional per-request byte bound.</summary>
    public long MaxBytes;
    /// <summary>The resource bytes already acquired by the containing operation.</summary>
    public long AlreadyLoadedBytes;
    /// <summary>Whether data URLs are decoded internally without invoking a callback.</summary>
    public bool DecodeDataUrls;
    /// <summary>An optional bounded cache shared by related acquisitions.</summary>
    public SvgResourceCache? Cache;
    /// <summary>The normalized active reference stack used for cycle checks.</summary>
    public System.Collections.Generic.List<string> ReferenceStack;
    /// <summary>The resource count already charged to the containing operation.</summary>
    public int AlreadyLoadedCount;
    /// <summary>Whether deterministic acquisition failures are retained in the cache.</summary>
    public bool CacheFailures;

    /// <summary>Creates secure synchronous acquisition defaults.</summary>
    public SvgResourceAcquisitionOptions()
    {
        ResourceProvider = null;
        Limits = new SvgResourceLimits();
        CancellationToken = System.Threading.CancellationToken.None;
        CopyInputData = true;
        MaxBytes = 33554432;
        AlreadyLoadedBytes = 0;
        DecodeDataUrls = true;
        Cache = null;
        ReferenceStack = new System.Collections.Generic.List<string>();
        AlreadyLoadedCount = 0;
        CacheFailures = true;
    }
}

/// <summary>Contains one acquired resource snapshot and exact callback metadata.</summary>
public class SvgResourceAcquisitionResult
{
    /// <summary>Whether resource acquisition completed successfully.</summary>
    public bool Success;
    /// <summary>The original request.</summary>
    public SvgResourceRequest Request;
    /// <summary>The caller response, or <c>null</c> for a built-in data URL.</summary>
    public SvgResourceResponse? Response;
    /// <summary>The acquired bytes, or an empty array after failure.</summary>
    public byte[] Bytes;
    /// <summary>The normalized initial URI.</summary>
    public string NormalizedUri;
    /// <summary>The normalized final URI.</summary>
    public string FinalUri;
    /// <summary>The caller-declared media type, or an empty string.</summary>
    public string DeclaredMediaType;
    /// <summary>The caller-detected media type, or an empty string.</summary>
    public string ActualMediaType;
    /// <summary>The effective actual, declared, or requested media type.</summary>
    public string EffectiveMediaType;
    /// <summary>The caller-managed redirect count.</summary>
    public int RedirectCount;
    /// <summary>Whether the returned bytes may be cached by later stages.</summary>
    public bool Cacheable;
    /// <summary>Whether the caller resource provider was invoked.</summary>
    public bool ProviderInvoked;
    /// <summary>Whether a data URL was decoded internally.</summary>
    public bool IsDataUrl;
    /// <summary>Whether the byte snapshot is independent from caller mutation.</summary>
    public bool IsIndependentSnapshot;
    /// <summary>Ordered callback and acquisition diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The provider invocation count.</summary>
    public int RequestCount;
    /// <summary>The successful acquisition count.</summary>
    public int LoadedCount;
    /// <summary>The count charged against the containing operation's resource budget.</summary>
    public int BudgetCount;
    /// <summary>Whether this result was restored from a success or failure cache entry.</summary>
    public bool CacheHit;
    /// <summary>Whether the restored cache entry represents a failure.</summary>
    public bool FailureCacheHit;
    /// <summary>The normalized fragment-free resource cache key.</summary>
    public string CacheKey;
    /// <summary>The source identity for the acquired root or nested resource.</summary>
    public SvgSourceUri? ResourceSource;
    /// <summary>The independent callback image metadata snapshot, or <c>null</c>.</summary>
    public SvgImageMetadata? ImageMetadata;

    internal SvgResourceAcquisitionResult(SvgResourceRequest request)
    {
        Success = false;
        Request = request;
        Response = null;
        Bytes = System.Array.Empty<byte>();
        NormalizedUri = "";
        FinalUri = "";
        DeclaredMediaType = "";
        ActualMediaType = "";
        EffectiveMediaType = "";
        RedirectCount = 0;
        Cacheable = false;
        ProviderInvoked = false;
        IsDataUrl = false;
        IsIndependentSnapshot = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        RequestCount = 0;
        LoadedCount = 0;
        BudgetCount = 0;
        CacheHit = false;
        FailureCacheHit = false;
        CacheKey = "";
        ResourceSource = null;
        ImageMetadata = null;
    }
}
