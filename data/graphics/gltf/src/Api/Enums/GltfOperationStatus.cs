namespace gltf;

/// <summary>Identifies the outcome of a toolkit operation.</summary>
public enum GltfOperationStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,

    /// <summary>The operation was intentionally skipped.</summary>
    Skipped = 1,

    /// <summary>A required optional capability was unavailable.</summary>
    Unavailable = 2,

    /// <summary>The supplied input was invalid.</summary>
    Invalid = 3,

    /// <summary>The operation failed after it began.</summary>
    Failed = 4
}
