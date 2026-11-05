namespace uspto;

/// <summary>
/// Identifies a data access path considered by fallback policy.
/// </summary>
public enum FallbackAccess
{
    /// <summary>No fallback access path has been selected.</summary>
    Unknown = 0,

    /// <summary>A live online service request.</summary>
    Online = 1,

    /// <summary>A previously stored response-cache entry.</summary>
    Cache = 2,

    /// <summary>A locally mirrored dataset.</summary>
    Local = 3
}
