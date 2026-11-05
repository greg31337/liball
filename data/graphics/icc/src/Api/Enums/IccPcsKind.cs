namespace icc;

/// <summary>
/// Identifies the active Profile Connection Space family.
/// </summary>
public enum IccPcsKind
{
    /// <summary>
    /// Indicates that no Profile Connection Space is defined.
    /// </summary>
    None = 0,

    /// <summary>
    /// Identifies the colorimetric CIE XYZ PCS.
    /// </summary>
    Xyz = 1,

    /// <summary>
    /// Identifies the colorimetric CIE Lab PCS.
    /// </summary>
    Lab = 2,

    /// <summary>
    /// Identifies an ICC.2 spectral PCS.
    /// </summary>
    Spectral = 3,

    /// <summary>
    /// Identifies an ICC.2 bi-spectral PCS.
    /// </summary>
    BiSpectral = 4,

    /// <summary>
    /// Identifies an ICC.2 Material Connection Space.
    /// </summary>
    Material = 5
}
