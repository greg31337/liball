namespace geo;

internal static class GeometryCopy
{
    internal static Geometry CopyGeometry(Geometry geometry)
    {
        return CopyGeometryBounded(geometry, GeometryConstants.DefaultMaximumNestingDepth, GeometryConstants.DefaultMaximumComponents);
    }

    internal static Geometry CopyGeometryBounded(Geometry geometry, int maximumDepth, int maximumComponents)
    {
        GeometryCycleGuard.EnsureAcyclic(geometry, maximumDepth, maximumComponents);
        return CopyGeometryCore(geometry);
    }

    internal static Geometry CopyGeometryCore(Geometry source)
    {
        object payload = GeometryDispatch.GetSelectedPayload(source);
        Geometry copy = new Geometry();
        copy.Point = null;
        copy.GeometryType = source.GeometryType;
        copy.CoordinateLayout = source.CoordinateLayout;
        copy.SpatialReferenceId = source.SpatialReferenceId;
        switch (source.GeometryType)
        {
            case GeometryType.Point:
                copy.Point = CopyPointValue((Point)payload);
                break;
            case GeometryType.LineString:
                copy.LineString = CopyLineStringValue((LineString)payload);
                break;
            case GeometryType.LinearRing:
                copy.LinearRing = CopyLinearRingValue((LinearRing)payload);
                break;
            case GeometryType.Polygon:
                copy.Polygon = CopyPolygonValue((Polygon)payload);
                break;
            case GeometryType.MultiPoint:
                copy.MultiPoint = CopyMultiPointValue((MultiPoint)payload);
                break;
            case GeometryType.MultiLineString:
                copy.MultiLineString = CopyMultiLineStringValue((MultiLineString)payload);
                break;
            case GeometryType.MultiPolygon:
                copy.MultiPolygon = CopyMultiPolygonValue((MultiPolygon)payload);
                break;
            case GeometryType.GeometryCollection:
                copy.GeometryCollection = CopyGeometryCollectionValue((GeometryCollection)payload);
                break;
            case GeometryType.Triangle:
                copy.Triangle = CopyTriangleValue((Triangle)payload);
                break;
            case GeometryType.Tin:
                copy.Tin = CopyTinValue((Tin)payload);
                break;
            case GeometryType.PolyhedralSurface:
                copy.PolyhedralSurface = CopyPolyhedralSurfaceValue((PolyhedralSurface)payload);
                break;
            case GeometryType.CircularString:
                copy.CircularString = CopyCircularStringValue((CircularString)payload);
                break;
            case GeometryType.CompoundCurve:
                copy.CompoundCurve = CopyCompoundCurveValue((CompoundCurve)payload);
                break;
            case GeometryType.CurvePolygon:
                copy.CurvePolygon = CopyCurvePolygonValue((CurvePolygon)payload);
                break;
            case GeometryType.MultiCurve:
                copy.MultiCurve = CopyMultiCurveValue((MultiCurve)payload);
                break;
            case GeometryType.MultiSurface:
                copy.MultiSurface = CopyMultiSurfaceValue((MultiSurface)payload);
                break;
            default:
                throw new InvalidGeometryException("Geometry type is not supported for copying.");
        }

        return copy;
    }

    internal static Point CopyPointValue(Point source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Point payload is null.");
        }

        Point copy = new Point();
        if (source.Coordinate != null)
        {
            copy.Coordinate = CoordinateSequences.CopyCoordinateValue(source.Coordinate);
        }

        return copy;
    }

    internal static LineString CopyLineStringValue(LineString source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("LineString payload is null.");
        }

        LineString copy = new LineString();
        copy.Coordinates = CoordinateSequences.CopyCoordinateSequence(source.Coordinates);
        return copy;
    }

    internal static LinearRing CopyLinearRingValue(LinearRing source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("LinearRing payload is null.");
        }

        LinearRing copy = new LinearRing();
        copy.Coordinates = CoordinateSequences.CopyCoordinateSequence(source.Coordinates);
        return copy;
    }

    internal static Polygon CopyPolygonValue(Polygon source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Polygon payload is null.");
        }

        Polygon copy = new Polygon();
        copy.Shell = CopyLinearRingValue(source.Shell);
        if (source.Holes == null)
        {
            throw new InvalidGeometryException("Polygon hole array is null.");
        }

        copy.Holes = new LinearRing[source.Holes.Length];
        int index = 0;
        while (index < source.Holes.Length)
        {
            copy.Holes[index] = CopyLinearRingValue(source.Holes[index]);
            index++;
        }

        return copy;
    }

    internal static MultiPoint CopyMultiPointValue(MultiPoint source)
    {
        if (source == null || source.Points == null)
        {
            throw new InvalidGeometryException("MultiPoint payload or component array is null.");
        }

        MultiPoint copy = new MultiPoint();
        copy.Points = new Point[source.Points.Length];
        int index = 0;
        while (index < source.Points.Length)
        {
            copy.Points[index] = CopyPointValue(source.Points[index]);
            index++;
        }

        return copy;
    }

    internal static MultiLineString CopyMultiLineStringValue(MultiLineString source)
    {
        if (source == null || source.LineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString payload or component array is null.");
        }

        MultiLineString copy = new MultiLineString();
        copy.LineStrings = new LineString[source.LineStrings.Length];
        int index = 0;
        while (index < source.LineStrings.Length)
        {
            copy.LineStrings[index] = CopyLineStringValue(source.LineStrings[index]);
            index++;
        }

        return copy;
    }

    internal static MultiPolygon CopyMultiPolygonValue(MultiPolygon source)
    {
        if (source == null || source.Polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon payload or component array is null.");
        }

        MultiPolygon copy = new MultiPolygon();
        copy.Polygons = new Polygon[source.Polygons.Length];
        int index = 0;
        while (index < source.Polygons.Length)
        {
            copy.Polygons[index] = CopyPolygonValue(source.Polygons[index]);
            index++;
        }

        return copy;
    }

    internal static GeometryCollection CopyGeometryCollectionValue(GeometryCollection source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("GeometryCollection payload is null.");
        }

        GeometryCollection copy = new GeometryCollection();
        copy.Geometries = CopyGeometryArray(source.Geometries);
        return copy;
    }

    internal static Triangle CopyTriangleValue(Triangle source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Triangle payload is null.");
        }

        Triangle copy = new Triangle();
        copy.Shell = CopyLinearRingValue(source.Shell);
        return copy;
    }

    internal static Tin CopyTinValue(Tin source)
    {
        if (source == null || source.Triangles == null)
        {
            throw new InvalidGeometryException("TIN payload or patch array is null.");
        }

        Tin copy = new Tin();
        copy.Triangles = new Triangle[source.Triangles.Length];
        int index = 0;
        while (index < source.Triangles.Length)
        {
            copy.Triangles[index] = CopyTriangleValue(source.Triangles[index]);
            index++;
        }

        return copy;
    }

    internal static PolyhedralSurface CopyPolyhedralSurfaceValue(PolyhedralSurface source)
    {
        if (source == null || source.Patches == null)
        {
            throw new InvalidGeometryException("PolyhedralSurface payload or patch array is null.");
        }

        PolyhedralSurface copy = new PolyhedralSurface();
        copy.Patches = new Polygon[source.Patches.Length];
        int index = 0;
        while (index < source.Patches.Length)
        {
            copy.Patches[index] = CopyPolygonValue(source.Patches[index]);
            index++;
        }

        return copy;
    }

    internal static CircularString CopyCircularStringValue(CircularString source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("CircularString payload is null.");
        }

        CircularString copy = new CircularString();
        copy.Coordinates = CoordinateSequences.CopyCoordinateSequence(source.Coordinates);
        return copy;
    }

    internal static CompoundCurve CopyCompoundCurveValue(CompoundCurve source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("CompoundCurve payload is null.");
        }

        CompoundCurve copy = new CompoundCurve();
        copy.Curves = CopyGeometryArray(source.Curves);
        return copy;
    }

    internal static CurvePolygon CopyCurvePolygonValue(CurvePolygon source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("CurvePolygon payload is null.");
        }

        CurvePolygon copy = new CurvePolygon();
        if (source.Shell != null)
        {
            copy.Shell = CopyGeometryCore(source.Shell);
        }

        copy.Holes = CopyGeometryArray(source.Holes);
        return copy;
    }

    internal static MultiCurve CopyMultiCurveValue(MultiCurve source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("MultiCurve payload is null.");
        }

        MultiCurve copy = new MultiCurve();
        copy.Curves = CopyGeometryArray(source.Curves);
        return copy;
    }

    internal static MultiSurface CopyMultiSurfaceValue(MultiSurface source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("MultiSurface payload is null.");
        }

        MultiSurface copy = new MultiSurface();
        copy.Surfaces = CopyGeometryArray(source.Surfaces);
        return copy;
    }

    internal static Geometry[] CopyGeometryArray(Geometry[] source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }

        Geometry[] copy = new Geometry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyGeometryCore(source[index]);
            index++;
        }

        return copy;
    }
}
