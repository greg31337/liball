namespace units;

/// <summary>
/// Identifies the conversion mechanics required by a unit.
/// </summary>
public enum UnitConversionKind
{
    /// <summary>
    /// Indicates that ordinary unit conversion is not supported.
    /// </summary>
    Unsupported = 0,
    /// <summary>
    /// Uses a multiplicative scale factor.
    /// </summary>
    Scale = 1,
    /// <summary>
    /// Uses a scale factor followed by an offset.
    /// </summary>
    Affine = 2,
    /// <summary>
    /// Requires explicitly selected special conversion metadata.
    /// </summary>
    Special = 3
}
