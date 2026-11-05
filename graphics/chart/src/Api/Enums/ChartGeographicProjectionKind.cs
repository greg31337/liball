namespace chart;
/// <summary>
/// Specifies the supported geographic projection formulas.
/// </summary>
public enum ChartGeographicProjectionKind
{
    /// <summary>
    /// Uses a spherical equirectangular projection.
    /// </summary>
    Equirectangular = 0,
    /// <summary>
    /// Uses a spherical Mercator projection.
    /// </summary>
    Mercator = 1,
    /// <summary>
    /// Uses a spherical orthographic projection of the visible hemisphere.
    /// </summary>
    Orthographic = 2,
    /// <summary>
    /// Uses a spherical Albers-like equal-area conic projection.
    /// </summary>
    AlbersLike = 3
}
