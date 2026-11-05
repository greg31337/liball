namespace gltf;

/// <summary>Identifies the registry maturity of a glTF extension.</summary>
public enum GltfExtensionStatus
{
    /// <summary>The extension status is unknown.</summary>
    Unknown = 0,

    /// <summary>A single-vendor extension.</summary>
    Vendor = 1,

    /// <summary>An extension supported by multiple vendors.</summary>
    MultiVendor = 2,

    /// <summary>A draft extension proposal.</summary>
    Draft = 3,

    /// <summary>An extension release candidate.</summary>
    ReleaseCandidate = 4,

    /// <summary>A ratified Khronos extension.</summary>
    Ratified = 5,

    /// <summary>An archived extension retained for compatibility.</summary>
    Archived = 6
}
