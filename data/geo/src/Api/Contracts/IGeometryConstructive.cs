namespace geo;

/// <summary>Constructs owning hull, buffer, minimum-shape, circle, triangulation, tangent, Voronoi, and tree results.</summary>
public interface IGeometryConstructive
{
    /// <summary>Computes the narrowest convex geometry containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>An owning convex hull.</returns>
    Geometry ConvexHull(Geometry geometry);

    /// <summary>Computes a connected concave polygon containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="targetRatio">The finite concaveness ratio from zero through one.</param>
    /// <param name="allowHoles">Whether a valid interior hole may be retained.</param>
    /// <returns>An owning concave hull.</returns>
    Geometry ConcaveHull(
        Geometry geometry, double targetRatio, bool allowHoles);

    /// <summary>Constructs a planar buffer around a geometry.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="distance">The signed finite buffer distance.</param>
    /// <param name="parameters">The cap, join, resolution, and precision policies.</param>
    /// <returns>An owning polygonal buffer.</returns>
    Geometry Buffer(
        Geometry geometry, double distance, BufferParameters parameters);

    /// <summary>Constructs a buffer on the signed side of lineal geometry.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite side distance.</param>
    /// <param name="parameters">The join, resolution, and precision policies.</param>
    /// <returns>An owning polygonal single-sided buffer.</returns>
    Geometry SingleSidedBuffer(
        Geometry geometry, double distance, BufferParameters parameters);

    /// <summary>Constructs the offset curve on the signed side of lineal geometry.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite offset distance.</param>
    /// <param name="parameters">The join, resolution, and precision policies.</param>
    /// <returns>An owning lineal offset result.</returns>
    Geometry OffsetCurve(
        Geometry geometry, double distance, BufferParameters parameters);

    /// <summary>Computes the minimum-area rotated rectangle containing the input.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The narrowest owning rectangle, line, point, or empty result.</returns>
    Geometry MinimumRotatedRectangle(Geometry geometry);

    /// <summary>Computes the minimum circle containing every input coordinate.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The owning circle and its support witnesses.</returns>
    CircleResult MinimumBoundingCircle(Geometry geometry);

    /// <summary>Computes the minimum width and its supporting coordinate pair.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <returns>The minimum-width witness result.</returns>
    DistanceResult MinimumWidth(Geometry geometry);

    /// <summary>Computes a maximum-radius circle contained by polygonal geometry.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <param name="tolerance">The finite positive search tolerance.</param>
    /// <returns>The owning inscribed circle and boundary witness.</returns>
    CircleResult MaximumInscribedCircle(
        Geometry polygonal, double tolerance);

    /// <summary>Computes a maximum-radius circle inside a boundary and outside obstacle geometry.</summary>
    /// <param name="obstacles">The point, line, or polygonal obstacles.</param>
    /// <param name="boundary">The polygonal containing boundary.</param>
    /// <param name="tolerance">The finite positive search tolerance.</param>
    /// <returns>The owning empty circle and active-constraint witness.</returns>
    CircleResult LargestEmptyCircle(
        Geometry obstacles, Geometry boundary, double tolerance);

    /// <summary>Constructs the two supporting tangent segments from an external point to a polygon.</summary>
    /// <param name="polygonal">The polygonal target.</param>
    /// <param name="point">The external tangent origin.</param>
    /// <returns>An owning two-member multi-line result.</returns>
    Geometry PolygonTangents(Geometry polygonal, Coordinate point);

    /// <summary>Computes a Delaunay triangulation of the unique input coordinates.</summary>
    /// <param name="geometry">The geometry supplying sites.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    Geometry Delaunay(Geometry geometry);

    /// <summary>Computes a boundary-constrained Delaunay triangulation of polygonal geometry.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    Geometry ConstrainedDelaunay(Geometry polygonal);

    /// <summary>Computes Voronoi cells clipped to an axis-aligned envelope.</summary>
    /// <param name="geometry">The geometry supplying sites.</param>
    /// <param name="clipEnvelope">The finite non-empty clipping envelope.</param>
    /// <returns>An owning collection of clipped polygon cells.</returns>
    Geometry Voronoi(Geometry geometry, Envelope clipEnvelope);

    /// <summary>Triangulates polygonal geometry while preserving shell and hole boundaries.</summary>
    /// <param name="polygonal">The polygonal domain.</param>
    /// <returns>An owning triangulated irregular network.</returns>
    Geometry TriangulatePolygon(Geometry polygonal);

    /// <summary>Computes a deterministic Euclidean minimum spanning tree of unique input coordinates.</summary>
    /// <param name="geometry">The geometry supplying tree vertices.</param>
    /// <returns>An owning multi-line tree.</returns>
    Geometry MinimumSpanningTree(Geometry geometry);

    /// <summary>Constructs a buffer after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source geometry.</param>
    /// <param name="distance">The signed finite buffer distance.</param>
    /// <param name="bufferParameters">The cap, join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning polygonal buffer of the linearized input.</returns>
    Geometry BufferAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters);

    /// <summary>Constructs a single-sided buffer after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite side distance.</param>
    /// <param name="bufferParameters">The join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning polygonal single-sided buffer of the linearized input.</returns>
    Geometry SingleSidedBufferAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters);

    /// <summary>Constructs an offset curve after explicitly bounded curve linearization.</summary>
    /// <param name="geometry">The source lineal geometry.</param>
    /// <param name="distance">The signed finite offset distance.</param>
    /// <param name="bufferParameters">The join, resolution, and precision policies.</param>
    /// <param name="curveParameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>An owning lineal offset of the linearized input.</returns>
    Geometry OffsetCurveAfterCurveLinearization(
        Geometry geometry, double distance,
        BufferParameters bufferParameters,
        CurveLinearizationParameters curveParameters);
}
