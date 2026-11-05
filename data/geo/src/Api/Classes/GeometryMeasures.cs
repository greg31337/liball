namespace geo;

/// <summary>Computes geometry measures and owning metric witness results without mutating inputs.</summary>
public class GeometryMeasures : IGeometryMeasures
{
    /// <summary>Creates a geometry measure provider.</summary>
    public GeometryMeasures()
    {
    }

    /// <summary>Computes unsigned projected polygonal and surface-patch area in X/Y.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated projected area.</returns>
    public double Area(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetProjectedArea(geometry);
    }

    /// <summary>Computes polygonal and surface-patch area in X/Y/Z, treating unavailable Z ordinates as zero.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated three-dimensional surface area.</returns>
    public double SurfaceArea3D(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetSurfaceArea3D(geometry);
    }

    /// <summary>Computes the total planar length of curves and polygon boundaries.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated planar length.</returns>
    public double Length(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetLength(geometry);
    }

    /// <summary>Computes the total planar boundary length of polygonal components.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The aggregated polygon perimeter.</returns>
    public double Perimeter(Geometry geometry)
    {
        return GeometryMeasureAlgorithms.GetPerimeter(geometry);
    }

    /// <summary>Computes brute-force minimum planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    public double Distance(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return DistanceAlgorithm.GetMinimumDistance(
            values[0], values[1]).Distance;
    }

    /// <summary>Computes minimum planar distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    public double DistanceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] linearized = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return DistanceAlgorithm.GetMinimumDistance(
            linearized[0], linearized[1]).Distance;
    }

    /// <summary>Computes segment-indexed minimum planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    public double IndexedDistance(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return DistanceAlgorithm.GetIndexedMinimumDistance(
            values[0], values[1]).Distance;
    }

    /// <summary>Computes indexed minimum planar distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The minimum distance, or positive infinity when either geometry is empty.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    public double IndexedDistanceAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] linearized = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return DistanceAlgorithm.GetIndexedMinimumDistance(
            linearized[0], linearized[1]).Distance;
    }

    /// <summary>Finds an owning nearest coordinate pair and its linear locations.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The indexed minimum-distance witness result.</returns>
    public DistanceResult NearestPoints(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return DistanceAlgorithm.GetIndexedMinimumDistance(
            values[0], values[1]);
    }

    /// <summary>Finds nearest points after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>The indexed minimum-distance witness result.</returns>
    /// <exception cref="InvalidGeometryException">An input or linearization policy is invalid.</exception>
    public DistanceResult NearestPointsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters)
    {
        Geometry[] linearized = CurveOperationLinearizer.LinearizePair(
            first, second, parameters);
        return DistanceAlgorithm.GetIndexedMinimumDistance(
            linearized[0], linearized[1]);
    }

    /// <summary>Computes discrete Hausdorff distance and an owning witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The discrete Hausdorff witness result.</returns>
    public DistanceResult HausdorffDistance(
        Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return HausdorffAlgorithm.GetDistance(values[0], values[1]);
    }

    /// <summary>Computes densified discrete Hausdorff distance and an owning witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="densifyFraction">The finite fraction greater than zero and at most one.</param>
    /// <returns>The densified discrete Hausdorff witness result.</returns>
    public DistanceResult DensifiedHausdorffDistance(
        Geometry first, Geometry second, double densifyFraction)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return HausdorffAlgorithm.GetDensifiedDistance(
            values[0], values[1], densifyFraction);
    }

    /// <summary>Computes discrete Fréchet distance and an owning ordered witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The discrete Fréchet witness result.</returns>
    public DistanceResult FrechetDistance(
        Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return FrechetAlgorithm.GetDistance(values[0], values[1]);
    }

    /// <summary>Computes densified discrete Fréchet distance and an owning ordered witness pair.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="densifyFraction">The finite fraction greater than zero and at most one.</param>
    /// <returns>The densified discrete Fréchet witness result.</returns>
    public DistanceResult DensifiedFrechetDistance(
        Geometry first, Geometry second, double densifyFraction)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return FrechetAlgorithm.GetDensifiedDistance(
            values[0], values[1], densifyFraction);
    }

    /// <summary>Computes an owning dimension-aware centroid coordinate.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The centroid, or null when the geometry is empty.</returns>
    public Coordinate? Centroid(Geometry geometry)
    {
        return CentroidAccumulator.GetCentroid(geometry);
    }

    /// <summary>Computes an owning representative coordinate on the geometry.</summary>
    /// <param name="geometry">The geometry to inspect.</param>
    /// <returns>A point on the geometry, or null when the geometry is empty.</returns>
    public Coordinate? PointOnSurface(Geometry geometry)
    {
        return RepresentativePointAlgorithm.GetPointOnSurface(geometry);
    }

    /// <summary>Computes minimum clearance and the owning segment that realizes it.</summary>
    /// <param name="geometry">The geometry to measure.</param>
    /// <returns>The clearance witness result, or a no-result value when clearance is undefined.</returns>
    public DistanceResult MinimumClearance(Geometry geometry)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return MinimumClearanceAlgorithm.GetMinimumClearance(projected);
    }

    /// <summary>Computes the minimum planar separation between geometry envelopes.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>The envelope distance, or no-data when either geometry is empty.</returns>
    public double EnvelopeDistance(Geometry first, Geometry second)
    {
        return DistanceAlgorithm.GetEnvelopeDistance(first, second);
    }
}
