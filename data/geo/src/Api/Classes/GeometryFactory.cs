namespace geo;

/// <summary>Constructs independent geometry values after validating and applying configured precision.</summary>
public class GeometryFactory : IGeometryFactory
{
    internal GeometryFactoryOptions Options;

    /// <summary>Creates a factory that snapshots the supplied configuration.</summary>
    /// <param name="options">The construction configuration.</param>
    public GeometryFactory(GeometryFactoryOptions options)
    {
        Options = GeometryBuilder.CopyAndValidateOptions(options);
    }

    /// <summary>Gets an independent copy of the factory configuration.</summary>
    /// <returns>The factory configuration.</returns>
    public GeometryFactoryOptions GetFactoryOptions()
    {
        return GeometryBuilder.CopyOptions(Options);
    }

    /// <summary>Creates an empty value of a supported geometry kind.</summary>
    /// <param name="geometryType">The geometry kind.</param>
    /// <returns>The empty geometry.</returns>
    public Geometry CreateEmptyGeometry(GeometryType geometryType)
    {
        return GeometryBuilder.CreateEmptyValue(geometryType, Options);
    }

    /// <summary>Creates a point, or an empty point when its coordinate is null.</summary>
    /// <param name="coordinate">The point coordinate.</param>
    /// <returns>The point geometry.</returns>
    public Geometry CreatePoint(Coordinate? coordinate)
    {
        return GeometryBuilder.CreatePointValue(coordinate, Options);
    }

    /// <summary>Creates a line string from an empty or two-or-more-position sequence.</summary>
    /// <param name="coordinates">The line coordinates.</param>
    /// <returns>The line string geometry.</returns>
    public Geometry CreateLineString(Coordinate[] coordinates)
    {
        return GeometryBuilder.CreateLineStringValue(coordinates, Options);
    }

    /// <summary>Creates a polygon from a shell and interior rings.</summary>
    /// <param name="shell">The exterior ring.</param>
    /// <param name="holes">The interior rings.</param>
    /// <returns>The polygon geometry.</returns>
    public Geometry CreatePolygon(LinearRing shell, LinearRing[] holes)
    {
        return GeometryBuilder.CreatePolygonValue(shell, holes, Options);
    }

    /// <summary>Creates a homogeneous collection of point payloads.</summary>
    /// <param name="points">The point payloads.</param>
    /// <returns>The multi-point geometry.</returns>
    public Geometry CreateMultiPoint(Point[] points)
    {
        return GeometryBuilder.CreateMultiPointValue(points, Options);
    }

    /// <summary>Creates a homogeneous collection of line string payloads.</summary>
    /// <param name="lineStrings">The line string payloads.</param>
    /// <returns>The multi-line string geometry.</returns>
    public Geometry CreateMultiLineString(LineString[] lineStrings)
    {
        return GeometryBuilder.CreateMultiLineStringValue(lineStrings, Options);
    }

    /// <summary>Creates a homogeneous collection of polygon payloads.</summary>
    /// <param name="polygons">The polygon payloads.</param>
    /// <returns>The multi-polygon geometry.</returns>
    public Geometry CreateMultiPolygon(Polygon[] polygons)
    {
        return GeometryBuilder.CreateMultiPolygonValue(polygons, Options);
    }

    /// <summary>Creates a heterogeneous collection from compatible geometries.</summary>
    /// <param name="geometries">The geometry components.</param>
    /// <returns>The geometry collection.</returns>
    public Geometry CreateGeometryCollection(Geometry[] geometries)
    {
        return GeometryBuilder.CreateGeometryCollectionValue(geometries, Options);
    }

    /// <summary>Creates a circular string from an empty or valid arc-control sequence.</summary>
    /// <param name="coordinates">The arc control coordinates.</param>
    /// <returns>The circular string geometry.</returns>
    public Geometry CreateCircularString(Coordinate[] coordinates)
    {
        return ExtendedGeometryBuilder.CreateCircularStringValue(
            coordinates, Options);
    }

    /// <summary>Creates a continuous compound curve from linear and circular components.</summary>
    /// <param name="curves">The ordered simple curve components.</param>
    /// <returns>The compound curve geometry.</returns>
    public Geometry CreateCompoundCurve(Geometry[] curves)
    {
        return ExtendedGeometryBuilder.CreateCompoundCurveValue(
            curves, Options);
    }

    /// <summary>Creates a curve polygon from a closed shell and closed hole curves.</summary>
    /// <param name="shell">The shell curve, or null for an empty curve polygon.</param>
    /// <param name="holes">The closed hole curves.</param>
    /// <returns>The curve polygon geometry.</returns>
    public Geometry CreateCurvePolygon(
        Geometry? shell, Geometry[] holes)
    {
        return ExtendedGeometryBuilder.CreateCurvePolygonValue(
            shell, holes, Options);
    }

    /// <summary>Creates a collection of linear, circular, and compound curves.</summary>
    /// <param name="curves">The curve components.</param>
    /// <returns>The multi-curve geometry.</returns>
    public Geometry CreateMultiCurve(Geometry[] curves)
    {
        return ExtendedGeometryBuilder.CreateMultiCurveValue(
            curves, Options);
    }

    /// <summary>Creates a collection of polygon, triangle, and curve-polygon surfaces.</summary>
    /// <param name="surfaces">The surface components.</param>
    /// <returns>The multi-surface geometry.</returns>
    public Geometry CreateMultiSurface(Geometry[] surfaces)
    {
        return ExtendedGeometryBuilder.CreateMultiSurfaceValue(
            surfaces, Options);
    }

    /// <summary>Creates an axis-aligned rectangle from opposite corners carrying compatible extra ordinates.</summary>
    /// <param name="minimum">The minimum X/Y corner.</param>
    /// <param name="maximum">The maximum X/Y corner.</param>
    /// <returns>The rectangle polygon.</returns>
    public Geometry CreateRectangle(Coordinate minimum, Coordinate maximum)
    {
        return FactoryConstructions.CreateRectangleValue(minimum, maximum, Options);
    }

    /// <summary>Creates an XY polygon from a non-empty envelope.</summary>
    /// <param name="envelope">The source envelope.</param>
    /// <returns>The envelope polygon.</returns>
    public Geometry CreateEnvelopePolygon(Envelope envelope)
    {
        return FactoryConstructions.CreateEnvelopePolygonValue(envelope, Options);
    }

    /// <summary>Creates a polygonal circle approximation.</summary>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The positive planar radius.</param>
    /// <param name="quadrantSegments">The positive number of segments per quadrant.</param>
    /// <returns>The circle polygon.</returns>
    public Geometry CreateCircle(Coordinate center, double radius, int quadrantSegments)
    {
        return FactoryConstructions.CreateCircleValue(center, radius, quadrantSegments, Options);
    }

    /// <summary>Creates a regular polygon centered on a coordinate.</summary>
    /// <param name="center">The polygon center.</param>
    /// <param name="radius">The positive circumradius.</param>
    /// <param name="vertexCount">The number of vertices, which must be at least three.</param>
    /// <returns>The regular polygon.</returns>
    public Geometry CreateRegularPolygon(Coordinate center, double radius, int vertexCount)
    {
        return FactoryConstructions.CreateRegularPolygonValue(center, radius, vertexCount, Options);
    }

    /// <summary>Creates a two-position line segment.</summary>
    /// <param name="start">The segment start.</param>
    /// <param name="end">The segment end.</param>
    /// <returns>The line string segment.</returns>
    public Geometry CreateSegment(Coordinate start, Coordinate end)
    {
        return FactoryConstructions.CreateSegmentValue(start, end, Options);
    }

    /// <summary>Copies geometries into the narrowest valid core homogeneous result.</summary>
    /// <param name="geometries">The geometry values to promote.</param>
    /// <returns>A singleton, homogeneous multi-geometry, or geometry collection.</returns>
    public Geometry CreateNarrowestCollection(Geometry[] geometries)
    {
        return FactoryConstructions.CreateNarrowestCollectionValue(geometries, Options);
    }
}
