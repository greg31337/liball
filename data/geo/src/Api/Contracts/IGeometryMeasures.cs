namespace geo;

/// <summary>Computes planar measures, explicit three-dimensional surface area, and owning metric witnesses.</summary>
public interface IGeometryMeasures
{
    /// <summary>Computes unsigned projected polygonal and surface-patch area in X/Y.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated projected area.</returns>
    double Area(Geometry geometry);

    /// <summary>Computes polygonal and surface-patch area in X/Y/Z, treating unavailable Z ordinates as zero.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated three-dimensional surface area.</returns>
    double SurfaceArea3D(Geometry geometry);

    /// <summary>Computes the total planar length of curves and polygon boundaries.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated planar length.</returns>
    double Length(Geometry geometry);

    /// <summary>Computes the total planar boundary length of polygonal components.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated polygon perimeter.</returns>
    double Perimeter(Geometry geometry);

    /// <summary>Computes brute-force minimum planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    double Distance(Geometry first, Geometry second);

    /// <summary>Computes minimum planar distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    double DistanceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Computes segment-indexed minimum planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    double IndexedDistance(Geometry first, Geometry second);

    /// <summary>Computes indexed minimum planar distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    double IndexedDistanceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Finds an owning nearest coordinate pair and its linear locations.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The indexed minimum-distance witness result.</returns>
    DistanceResult NearestPoints(Geometry first, Geometry second);

    /// <summary>Finds nearest points after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The indexed minimum-distance witness result.</returns>
    DistanceResult NearestPointsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Computes discrete Hausdorff distance and an owning witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The discrete Hausdorff witness result.</returns>
    DistanceResult HausdorffDistance(Geometry first, Geometry second);

    /// <summary>Computes densified discrete Hausdorff distance and an owning witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="densifyFraction">The finite fraction greater than zero and at most one.</param>
    /// <returns>The densified discrete Hausdorff witness result.</returns>
    DistanceResult DensifiedHausdorffDistance(
        Geometry first, Geometry second, double densifyFraction);

    /// <summary>Computes discrete Fréchet distance and an owning ordered witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The discrete Fréchet witness result.</returns>
    DistanceResult FrechetDistance(Geometry first, Geometry second);

    /// <summary>Computes densified discrete Fréchet distance and an owning ordered witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="densifyFraction">The finite fraction greater than zero and at most one.</param>
    /// <returns>The densified discrete Fréchet witness result.</returns>
    DistanceResult DensifiedFrechetDistance(
        Geometry first, Geometry second, double densifyFraction);

    /// <summary>Computes an owning dimension-aware centroid coordinate.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The centroid, or null when the geometry is empty.</returns>
    Coordinate? Centroid(Geometry geometry);

    /// <summary>Computes an owning representative coordinate on the geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>A point on the geometry, or null when the geometry is empty.</returns>
    Coordinate? PointOnSurface(Geometry geometry);

    /// <summary>Computes minimum clearance and the owning segment that realizes it.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The clearance witness result, or a no-result value when clearance is undefined.</returns>
    DistanceResult MinimumClearance(Geometry geometry);

    /// <summary>Computes the minimum planar separation between geometry envelopes.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The envelope distance, or no-data when either geometry is empty.</returns>
    double EnvelopeDistance(Geometry first, Geometry second);
}
