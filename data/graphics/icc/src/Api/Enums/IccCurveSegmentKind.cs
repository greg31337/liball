namespace icc;

/// <summary>
/// Identifies an ICC segmented-curve segment representation.
/// </summary>
public enum IccCurveSegmentKind
{
    /// <summary>
    /// Identifies a formula curve segment.
    /// </summary>
    Formula = 0,

    /// <summary>
    /// Identifies a sampled curve segment.
    /// </summary>
    Sampled = 1
}
