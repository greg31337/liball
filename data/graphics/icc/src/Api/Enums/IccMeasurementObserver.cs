namespace icc;

/// <summary>
/// Identifies an ICC standard colorimetric observer.
/// </summary>
public enum IccMeasurementObserver
{
    /// <summary>
    /// Indicates an unknown observer.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies the CIE 1931 two-degree standard observer.
    /// </summary>
    Cie1931TwoDegree = 1,

    /// <summary>
    /// Identifies the CIE 1964 ten-degree standard observer.
    /// </summary>
    Cie1964TenDegree = 2
}
