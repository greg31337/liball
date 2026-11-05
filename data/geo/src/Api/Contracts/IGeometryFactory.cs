namespace geo;

/// <summary>Constructs owning geometry values using a configured layout, spatial reference, and precision model.</summary>
public interface IGeometryFactory
{
    /// <summary>Gets an independent copy of the factory configuration.</summary>
    /// <returns>The factory configuration.</returns>
    GeometryFactoryOptions GetFactoryOptions();

    /// <summary>Creates an empty value of a supported geometry kind.</summary>
    /// <param name="geometryType">The geometry kind.</param>
    /// <returns>The empty geometry.</returns>
    Geometry CreateEmptyGeometry(GeometryType geometryType);

    /// <summary>Creates a point, or an empty point when its coordinate is null.</summary>
    /// <param name="coordinate">The point coordinate.</param>
    /// <returns>The point geometry.</returns>
    Geometry CreatePoint(Coordinate? coordinate);

    /// <summary>Creates a line string from an empty or two-or-more-position sequence.</summary>
    /// <param name="coordinates">The line coordinates.</param>
    /// <returns>The line string geometry.</returns>
    Geometry CreateLineString(Coordinate[] coordinates);

    /// <summary>Creates a polygon from a shell and interior rings.</summary>
    /// <param name="shell">The exterior ring.</param>
    /// <param name="holes">The interior rings.</param>
    /// <returns>The polygon geometry.</returns>
    Geometry CreatePolygon(LinearRing shell, LinearRing[] holes);

    /// <summary>Creates a homogeneous collection of point payloads.</summary>
    /// <param name="points">The point payloads.</param>
    /// <returns>The multi-point geometry.</returns>
    Geometry CreateMultiPoint(Point[] points);

    /// <summary>Creates a homogeneous collection of line string payloads.</summary>
    /// <param name="lineStrings">The line string payloads.</param>
    /// <returns>The multi-line string geometry.</returns>
    Geometry CreateMultiLineString(LineString[] lineStrings);

    /// <summary>Creates a homogeneous collection of polygon payloads.</summary>
    /// <param name="polygons">The polygon payloads.</param>
    /// <returns>The multi-polygon geometry.</returns>
    Geometry CreateMultiPolygon(Polygon[] polygons);

    /// <summary>Creates a heterogeneous collection from compatible geometries.</summary>
    /// <param name="geometries">The geometry components.</param>
    /// <returns>The geometry collection.</returns>
    Geometry CreateGeometryCollection(Geometry[] geometries);

    /// <summary>Creates a circular string from an empty or valid arc-control sequence.</summary>
    /// <param name="coordinates">The arc control coordinates.</param>
    /// <returns>The circular string geometry.</returns>
    Geometry CreateCircularString(Coordinate[] coordinates);

    /// <summary>Creates a continuous compound curve from linear and circular components.</summary>
    /// <param name="curves">The ordered simple curve components.</param>
    /// <returns>The compound curve geometry.</returns>
    Geometry CreateCompoundCurve(Geometry[] curves);

    /// <summary>Creates a curve polygon from a closed shell and closed hole curves.</summary>
    /// <param name="shell">The shell curve, or null for an empty curve polygon.</param>
    /// <param name="holes">The closed hole curves.</param>
    /// <returns>The curve polygon geometry.</returns>
    Geometry CreateCurvePolygon(Geometry? shell, Geometry[] holes);

    /// <summary>Creates a collection of linear, circular, and compound curves.</summary>
    /// <param name="curves">The curve components.</param>
    /// <returns>The multi-curve geometry.</returns>
    Geometry CreateMultiCurve(Geometry[] curves);

    /// <summary>Creates a collection of polygon, triangle, and curve-polygon surfaces.</summary>
    /// <param name="surfaces">The surface components.</param>
    /// <returns>The multi-surface geometry.</returns>
    Geometry CreateMultiSurface(Geometry[] surfaces);

    /// <summary>Creates an axis-aligned rectangle from opposite corners carrying compatible extra ordinates.</summary>
    /// <param name="minimum">The minimum X/Y corner.</param>
    /// <param name="maximum">The maximum X/Y corner.</param>
    /// <returns>The rectangle polygon.</returns>
    Geometry CreateRectangle(Coordinate minimum, Coordinate maximum);

    /// <summary>Creates an XY polygon from a non-empty envelope.</summary>
    /// <param name="envelope">The source envelope.</param>
    /// <returns>The envelope polygon.</returns>
    Geometry CreateEnvelopePolygon(Envelope envelope);

    /// <summary>Creates a polygonal circle approximation.</summary>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The positive planar radius.</param>
    /// <param name="quadrantSegments">The positive number of segments per quadrant.</param>
    /// <returns>The circle polygon.</returns>
    Geometry CreateCircle(Coordinate center, double radius, int quadrantSegments);

    /// <summary>Creates a regular polygon centered on a coordinate.</summary>
    /// <param name="center">The polygon center.</param>
    /// <param name="radius">The positive circumradius.</param>
    /// <param name="vertexCount">The number of vertices, which must be at least three.</param>
    /// <returns>The regular polygon.</returns>
    Geometry CreateRegularPolygon(Coordinate center, double radius, int vertexCount);

    /// <summary>Creates a two-position line segment.</summary>
    /// <param name="start">The segment start.</param>
    /// <param name="end">The segment end.</param>
    /// <returns>The line string segment.</returns>
    Geometry CreateSegment(Coordinate start, Coordinate end);

    /// <summary>Copies geometries into the narrowest valid core homogeneous result.</summary>
    /// <param name="geometries">The geometry values to promote.</param>
    /// <returns>A singleton, homogeneous multi-geometry, or geometry collection.</returns>
    Geometry CreateNarrowestCollection(Geometry[] geometries);
}
