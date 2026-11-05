namespace icc;

/// <summary>
/// Identifies an ICC tone-curve representation.
/// </summary>
public enum IccCurveKind
{
    /// <summary>
    /// Identifies an identity curve.
    /// </summary>
    Identity = 0,

    /// <summary>
    /// Identifies a single-gamma curve.
    /// </summary>
    Gamma = 1,

    /// <summary>
    /// Identifies a sampled curve.
    /// </summary>
    Sampled = 2,

    /// <summary>
    /// Identifies a parametric curve.
    /// </summary>
    Parametric = 3,

    /// <summary>
    /// Identifies a segmented curve.
    /// </summary>
    Segmented = 4,

    /// <summary>
    /// Identifies an ICC.2 single sampled curve with an explicit domain and value encoding.
    /// </summary>
    SingleSampled = 5
}
