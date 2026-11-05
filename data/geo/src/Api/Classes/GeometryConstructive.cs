namespace geo;

/// <summary>Constructs owning robust planar geometry results without mutating inputs.</summary>
public class GeometryConstructive : IGeometryConstructive
{
    /// <summary>Creates a constructive geometry provider.</summary>
    public GeometryConstructive()
    {
    }

    /// <summary>Computes the narrowest convex geometry containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>An owning convex hull.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public Geometry ConvexHull(Geometry geometry)
    {
        return ConvexHullAlgorithm.GetConvexHull(geometry);
    }

    /// <summary>Computes a connected concave polygon containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="targetRatio">The finite concaveness ratio from zero through one.</param>
    /// <param name="allowHoles">Whether a valid interior hole may be retained.</param>
    /// <returns>An owning concave hull.</returns>
    /// <exception cref="InvalidGeometryException">The input or target ratio is invalid.</exception>
    /// <exception cref="TopologyException">A valid connected hull cannot be constructed.</exception>
    public Geometry ConcaveHull(
        Geometry geometry, double targetRatio, bool allowHoles)
    {
        return ConcaveHullAlgorithm.GetConcaveHullByRatio(
            geometry, targetRatio, allowHoles);
    }

    /// <summary>Constructs a planar buffer around a geometry.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="distance">The signed finite buffer distance.</param>
    /// <param name="parameters">The cap, join, resolution, and precision policies.</param>
    /// <returns>An owning polygonal buffer.</returns>
    /// <exception cref="InvalidGeometryException">The input, distance, or parameters are invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid polygonal buffer cannot be constructed.</exception>
    public Geometry Buffer(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        Geometry projected =
            SurfaceKernelProjector.ProjectGeometry(geometry);
        return BufferBuilder.GetBuffer(projected, distance, parameters);
    }

    /// <summary>Constructs a buffer on the signed side of lineal geometry.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite side distance.</param>
    /// <param name="parameters">The join, resolution, and precision policies.</param>
    /// <returns>An owning polygonal single-sided buffer.</returns>
    /// <exception cref="InvalidGeometryException">The input, distance, or parameters are invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid polygonal buffer cannot be constructed.</exception>
    public Geometry SingleSidedBuffer(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        OffsetSegmentGenerator.ValidateBufferParameters(parameters);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        OffsetCurveBuilder.ValidateLinealLeaves(leaves);
        BufferParameters copy =
            BufferCurveBuilder.CopyParameters(parameters, true);
        return BufferBuilder.GetBuffer(geometry, distance, copy);
    }

    /// <summary>Constructs the offset curve on the signed side of lineal geometry.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite offset distance.</param>
    /// <param name="parameters">The join, resolution, and precision policies.</param>
    /// <returns>An owning lineal offset result.</returns>
    /// <exception cref="InvalidGeometryException">The input, distance, or parameters are invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid offset curve cannot be constructed.</exception>
    public Geometry OffsetCurve(
        Geometry geometry, double distance, BufferParameters parameters)
    {
        return OffsetCurveBuilder.GetOffsetCurve(
            geometry, distance, parameters);
    }

    /// <summary>Computes the minimum-area rotated rectangle containing the input.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The narrowest owning rectangle, line, point, or empty result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public Geometry MinimumRotatedRectangle(Geometry geometry)
    {
        return MinimumShapes.GetMinimumRotatedRectangle(geometry);
    }

    /// <summary>Computes the minimum circle containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The owning circle and its support witnesses.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public CircleResult MinimumBoundingCircle(Geometry geometry)
    {
        return MinimumShapes.GetMinimumBoundingCircle(geometry);
    }

    /// <summary>Computes the minimum width and its supporting coordinate pair.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The minimum-width witness result.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or unsupported.</exception>
    public DistanceResult MinimumWidth(Geometry geometry)
    {
        return MinimumShapes.GetMinimumWidth(geometry);
    }

    /// <summary>Computes a maximum-radius circle contained by polygonal geometry.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <param name="tolerance">The finite positive search tolerance.</param>
    /// <returns>The owning inscribed circle and boundary witness.</returns>
    /// <exception cref="InvalidGeometryException">The input or tolerance is invalid.</exception>
    public CircleResult MaximumInscribedCircle(
        Geometry polygonal, double tolerance)
    {
        return InteriorCircleAlgorithms.GetMaximumInscribedCircle(
            polygonal, tolerance);
    }

    /// <summary>Computes a maximum-radius circle inside a boundary and outside obstacle geometry.</summary>
    /// <param name="obstacles">The point, line, or polygonal obstacles.</param>
    /// <param name="boundary">The polygonal containing boundary.</param>
    /// <param name="tolerance">The finite positive search tolerance.</param>
    /// <returns>The owning empty circle and active-constraint witness.</returns>
    /// <exception cref="InvalidGeometryException">An input, metadata combination, or tolerance is invalid.</exception>
    public CircleResult LargestEmptyCircle(
        Geometry obstacles, Geometry boundary, double tolerance)
    {
        return InteriorCircleAlgorithms.GetLargestEmptyCircle(
            obstacles, boundary, tolerance);
    }

    /// <summary>Constructs the two supporting tangent segments from an external point to a polygon.</summary>
    /// <param name="polygonal">The polygonal target.</param>
    /// <param name="point">The external tangent origin.</param>
    /// <returns>An owning two-member multi-line result.</returns>
    /// <exception cref="InvalidGeometryException">The input, point, or metadata is invalid.</exception>
    public Geometry PolygonTangents(
        Geometry polygonal, Coordinate point)
    {
        return InteriorCircleAlgorithms.GetPolygonTangents(
            polygonal, point);
    }

    /// <summary>Computes a Delaunay triangulation of the unique input coordinates.</summary>
    /// <param name="geometry">The geometry supplying sites.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or outside the finite working range.</exception>
    public Geometry Delaunay(Geometry geometry)
    {
        return DelaunayBuilder.GetDelaunayTriangulation(geometry);
    }

    /// <summary>Computes a boundary-constrained Delaunay triangulation of polygonal geometry.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    /// <exception cref="InvalidGeometryException">The polygonal input is invalid.</exception>
    /// <exception cref="TopologyException">The polygon cannot be triangulated.</exception>
    public Geometry ConstrainedDelaunay(Geometry polygonal)
    {
        return ConstrainedDelaunayBuilder
            .GetConstrainedDelaunayTriangulation(polygonal);
    }

    /// <summary>Computes Voronoi cells clipped to an axis-aligned envelope.</summary>
    /// <param name="geometry">The geometry supplying sites.</param>
    /// <param name="clipEnvelope">The finite non-empty clipping envelope.</param>
    /// <returns>An owning collection of clipped polygon cells.</returns>
    /// <exception cref="InvalidGeometryException">The input or clipping envelope is invalid.</exception>
    public Geometry Voronoi(
        Geometry geometry, Envelope clipEnvelope)
    {
        return VoronoiBuilder.GetVoronoiDiagram(
            geometry, clipEnvelope);
    }

    /// <summary>Triangulates polygonal geometry while preserving shell and hole boundaries.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    /// <exception cref="InvalidGeometryException">The polygonal input is invalid.</exception>
    /// <exception cref="TopologyException">The polygon cannot be triangulated.</exception>
    public Geometry TriangulatePolygon(Geometry polygonal)
    {
        return PolygonTriangulator.GetPolygonTriangulation(polygonal);
    }

    /// <summary>Computes a deterministic Euclidean minimum spanning tree of unique input coordinates.</summary>
    /// <param name="geometry">The geometry supplying tree vertices.</param>
    /// <returns>An owning multi-line tree.</returns>
    /// <exception cref="InvalidGeometryException">The input is invalid or exceeds the supported work limit.</exception>
    public Geometry MinimumSpanningTree(Geometry geometry)
    {
        return MinimumSpanningTreeBuilder.GetMinimumSpanningTree(geometry);
    }

    /// <summary>Constructs a buffer after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="distance">The signed finite buffer distance.</param>
    /// <param name="bufferParameters">The cap, join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning polygonal buffer of the linearized input.</returns>
    /// <exception cref="InvalidGeometryException">An input, distance, or parameter value is invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid polygonal buffer cannot be constructed.</exception>
    public Geometry BufferAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters)
    {
        Geometry linearized = CurveOperationLinearizer.LinearizeOne(
            geometry, curveParameters);
        return Buffer(linearized, distance, bufferParameters);
    }

    /// <summary>Constructs a single-sided buffer after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite side distance.</param>
    /// <param name="bufferParameters">The join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning polygonal single-sided buffer of the linearized input.</returns>
    /// <exception cref="InvalidGeometryException">An input, distance, or parameter value is invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid polygonal buffer cannot be constructed.</exception>
    public Geometry SingleSidedBufferAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters)
    {
        Geometry linearized = CurveOperationLinearizer.LinearizeOne(
            geometry, curveParameters);
        OffsetSegmentGenerator.ValidateBufferParameters(bufferParameters);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            linearized,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        OffsetCurveBuilder.ValidateLinealLeaves(leaves);
        BufferParameters copy =
            BufferCurveBuilder.CopyParameters(bufferParameters, true);
        return BufferBuilder.GetBuffer(linearized, distance, copy);
    }

    /// <summary>Constructs an offset curve after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite offset distance.</param>
    /// <param name="bufferParameters">The join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning lineal offset of the linearized input.</returns>
    /// <exception cref="InvalidGeometryException">An input, distance, or parameter value is invalid.</exception>
    /// <exception cref="PrecisionException">The requested precision model is invalid.</exception>
    /// <exception cref="TopologyException">A valid offset curve cannot be constructed.</exception>
    public Geometry OffsetCurveAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters)
    {
        Geometry linearized = CurveOperationLinearizer.LinearizeOne(
            geometry, curveParameters);
        return OffsetCurveBuilder.GetOffsetCurve(
            linearized, distance, bufferParameters);
    }
}
