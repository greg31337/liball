namespace icc;

/// <summary>
/// Identifies a supported ICC profile generation.
/// </summary>
public enum IccVersionKind
{
    /// <summary>
    /// Indicates an unknown or unsupported profile generation.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates an ICC.1 version 2 profile.
    /// </summary>
    Version2 = 2,

    /// <summary>
    /// Indicates an ICC.1 version 4 profile.
    /// </summary>
    Version4 = 4,

    /// <summary>
    /// Indicates an ICC.2 version 5 profile.
    /// </summary>
    Version5 = 5
}
