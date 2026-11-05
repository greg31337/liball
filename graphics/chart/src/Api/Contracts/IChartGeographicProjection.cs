namespace chart;
/// <summary>Defines spherical geographic projection operations.</summary>
public interface IChartGeographicProjection
{
    /// <summary>
    /// Projects a longitude and latitude expressed in degrees.
    /// </summary>
    /// <param name = "longitude">The longitude in degrees.</param>
    /// <param name = "latitude">The latitude in degrees.</param>
    /// <returns>A dimensionless projected point.</returns>
    ChartPoint Project(double longitude, double latitude);
    /// <summary>
    /// Inverts a dimensionless projected point to longitude and latitude in degrees.
    /// </summary>
    /// <param name = "point">The projected point.</param>
    /// <returns>A point whose x value is longitude and whose y value is latitude, in degrees.</returns>
    ChartPoint Invert(ChartPoint point);
    /// <summary>Returns the configured projection kind.</summary>
    /// <returns>The projection kind.</returns>
    ChartGeographicProjectionKind GetKind();
    /// <summary>Returns the central longitude in degrees.</summary>
    /// <returns>The central longitude.</returns>
    double GetCentralLongitude();
    /// <summary>Returns the central latitude in degrees.</summary>
    /// <returns>The central latitude.</returns>
    double GetCentralLatitude();
    /// <summary>Returns the first standard parallel in degrees.</summary>
    /// <returns>The first standard parallel.</returns>
    double GetFirstStandardParallel();
    /// <summary>Returns the second standard parallel in degrees.</summary>
    /// <returns>The second standard parallel.</returns>
    double GetSecondStandardParallel();
}
