namespace odf;

/// <summary>
/// Identifies an enhanced-geometry handle coordinate form.
/// </summary>
public enum OdfGeometryHandleKind
{
    /// <summary>The handle form is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>A Cartesian horizontal and vertical handle.</summary>
    Cartesian = 1,
    /// <summary>A polar pole, radius, and angle handle.</summary>
    Polar = 2,
    /// <summary>A deprecated combined-position handle.</summary>
    Legacy = 3
}
