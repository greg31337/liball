namespace icc;

/// <summary>
/// Identifies an ICC measurement geometry.
/// </summary>
public enum IccMeasurementGeometry
{
    /// <summary>
    /// Indicates an unknown measurement geometry.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies a 0/45 or 45/0 geometry.
    /// </summary>
    Degree0Or45 = 1,

    /// <summary>
    /// Identifies a 0/d or d/0 geometry.
    /// </summary>
    Degree0OrDiffuse = 2
}
