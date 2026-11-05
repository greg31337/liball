namespace geo;

internal static class GeometryDispatch
{
    internal static int CountPopulatedPayloads(Geometry geometry)
    {
        int count = 0;
        if (geometry.Point != null) count++;
        if (geometry.LineString != null) count++;
        if (geometry.LinearRing != null) count++;
        if (geometry.Polygon != null) count++;
        if (geometry.MultiPoint != null) count++;
        if (geometry.MultiLineString != null) count++;
        if (geometry.MultiPolygon != null) count++;
        if (geometry.GeometryCollection != null) count++;
        if (geometry.Triangle != null) count++;
        if (geometry.Tin != null) count++;
        if (geometry.PolyhedralSurface != null) count++;
        if (geometry.CircularString != null) count++;
        if (geometry.CompoundCurve != null) count++;
        if (geometry.CurvePolygon != null) count++;
        if (geometry.MultiCurve != null) count++;
        if (geometry.MultiSurface != null) count++;
        return count;
    }

    internal static object GetSelectedPayload(Geometry geometry)
    {
        if (geometry == null)
        {
            throw new InvalidGeometryException("Geometry is null.");
        }

        if (CountPopulatedPayloads(geometry) != 1)
        {
            throw new InvalidGeometryException("Geometry must contain exactly one payload.");
        }

        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                if (geometry.Point != null) return geometry.Point;
                break;
            case GeometryType.LineString:
                if (geometry.LineString != null) return geometry.LineString;
                break;
            case GeometryType.LinearRing:
                if (geometry.LinearRing != null) return geometry.LinearRing;
                break;
            case GeometryType.Polygon:
                if (geometry.Polygon != null) return geometry.Polygon;
                break;
            case GeometryType.MultiPoint:
                if (geometry.MultiPoint != null) return geometry.MultiPoint;
                break;
            case GeometryType.MultiLineString:
                if (geometry.MultiLineString != null) return geometry.MultiLineString;
                break;
            case GeometryType.MultiPolygon:
                if (geometry.MultiPolygon != null) return geometry.MultiPolygon;
                break;
            case GeometryType.GeometryCollection:
                if (geometry.GeometryCollection != null) return geometry.GeometryCollection;
                break;
            case GeometryType.Triangle:
                if (geometry.Triangle != null) return geometry.Triangle;
                break;
            case GeometryType.Tin:
                if (geometry.Tin != null) return geometry.Tin;
                break;
            case GeometryType.PolyhedralSurface:
                if (geometry.PolyhedralSurface != null) return geometry.PolyhedralSurface;
                break;
            case GeometryType.CircularString:
                if (geometry.CircularString != null) return geometry.CircularString;
                break;
            case GeometryType.CompoundCurve:
                if (geometry.CompoundCurve != null) return geometry.CompoundCurve;
                break;
            case GeometryType.CurvePolygon:
                if (geometry.CurvePolygon != null) return geometry.CurvePolygon;
                break;
            case GeometryType.MultiCurve:
                if (geometry.MultiCurve != null) return geometry.MultiCurve;
                break;
            case GeometryType.MultiSurface:
                if (geometry.MultiSurface != null) return geometry.MultiSurface;
                break;
            default:
                break;
        }

        throw new InvalidGeometryException("Geometry type does not match its populated payload.");
    }

    internal static TopologicalDimension GetDeclaredDimension(Geometry geometry)
    {
        GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return TopologicalDimension.Point;
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.MultiLineString:
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
            case GeometryType.MultiCurve:
                return TopologicalDimension.Curve;
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
            case GeometryType.Triangle:
            case GeometryType.Tin:
            case GeometryType.PolyhedralSurface:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiSurface:
                return TopologicalDimension.Surface;
            case GeometryType.GeometryCollection:
                return TopologicalDimension.Empty;
            default:
                throw new InvalidGeometryException("Geometry type is not supported.");
        }
    }
}
