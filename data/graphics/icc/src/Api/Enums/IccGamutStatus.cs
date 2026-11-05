namespace icc;

/// <summary>
/// Identifies the result of a gamut-membership operation.
/// </summary>
public enum IccGamutStatus
{
    /// <summary>
    /// Indicates that gamut membership is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates that the colour lies inside the gamut.
    /// </summary>
    Inside = 1,

    /// <summary>
    /// Indicates that the colour lies outside the gamut.
    /// </summary>
    Outside = 2,

    /// <summary>
    /// Indicates that the colour lies on the gamut boundary.
    /// </summary>
    Boundary = 3,

    /// <summary>
    /// Indicates that the profile cannot provide a gamut result.
    /// </summary>
    Unsupported = 4
}
