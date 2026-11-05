namespace uspto;

/// <summary>
/// Configures deterministic selection among online, cached, and locally mirrored data.
/// </summary>
public class DataFallbackPolicy
{
    /// <summary>The access paths to consider, in caller-selected priority order.</summary>
    public FallbackAccess[] AccessOrder = new FallbackAccess[]
    {
        FallbackAccess.Online,
        FallbackAccess.Cache,
        FallbackAccess.Local
    };

    /// <summary>The authority category that candidates must satisfy.</summary>
    public FallbackAuthority Authority = FallbackAuthority.AuthoritativeOnly;

    /// <summary>The maximum accepted age of a cache candidate, in seconds.</summary>
    public int MaximumCacheAgeSeconds = UsptoDefaults.CacheLifetimeSeconds;

    /// <summary>Whether <see cref="MaximumCacheAgeSeconds"/> is enforced.</summary>
    public bool HasMaximumCacheAge = true;

    /// <summary>The maximum accepted age of a local-mirror candidate, in seconds.</summary>
    public int MaximumLocalAgeSeconds = 86400;

    /// <summary>Whether <see cref="MaximumLocalAgeSeconds"/> is enforced.</summary>
    public bool HasMaximumLocalAge = true;
}

/// <summary>
/// Describes whether one access path can provide the requested operation and data authority.
/// </summary>
public class DataFallbackCapability
{
    /// <summary>The access path described by this capability.</summary>
    public FallbackAccess Access = FallbackAccess.Unknown;

    /// <summary>Whether the service, cache entry, or local dataset is currently available.</summary>
    public bool IsAvailable = false;

    /// <summary>Whether the source supports the requested operation.</summary>
    public bool SupportsOperation = false;

    /// <summary>The source authority, delivery path, and freshness timestamps.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Holds one value loaded from a fallback access path together with its provenance.
/// </summary>
public class DataFallbackValue
{
    /// <summary>The source-specific result value.</summary>
    public object? Value = null;

    /// <summary>The loaded value's source authority, delivery path, and freshness timestamps.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}

/// <summary>
/// Reports the capability check and selection outcome for one fallback access path.
/// </summary>
public class DataFallbackAttempt
{
    /// <summary>The considered access path.</summary>
    public FallbackAccess Access = FallbackAccess.Unknown;

    /// <summary>Whether the access path's capability was checked.</summary>
    public bool CapabilityChecked = false;

    /// <summary>Whether the source reported that it was available.</summary>
    public bool IsAvailable = false;

    /// <summary>Whether the source reported support for the requested operation.</summary>
    public bool SupportsOperation = false;

    /// <summary>Whether the source satisfied the caller's authority requirement.</summary>
    public bool AuthorityAccepted = false;

    /// <summary>Whether the candidate satisfied the applicable freshness limit.</summary>
    public bool IsFresh = false;

    /// <summary>Whether loading was attempted after policy checks succeeded.</summary>
    public bool LoadAttempted = false;

    /// <summary>Whether this access path supplied the selected result.</summary>
    public bool IsSelected = false;

    /// <summary>The stable reason code when this path was not selected.</summary>
    public string? RejectionCode = null;

    /// <summary>The checked or loaded candidate provenance.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The safely captured capability or load failure.</summary>
    public SectionError? Error = null;
}

/// <summary>
/// Holds the selected fallback value and the ordered outcomes of every considered path.
/// </summary>
public class DataFallbackResult
{
    /// <summary>The source-specific selected value.</summary>
    public object? Value = null;

    /// <summary>The selected value's provenance.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The access path that supplied the selected value.</summary>
    public FallbackAccess SelectedAccess = FallbackAccess.Unknown;

    /// <summary>The access paths checked in caller preference order.</summary>
    public DataFallbackAttempt[] Attempts = new DataFallbackAttempt[0];

    /// <summary>The safely captured capability and load failures.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>Whether one candidate passed every policy check and was selected.</summary>
    public bool IsSuccessful = false;
}
