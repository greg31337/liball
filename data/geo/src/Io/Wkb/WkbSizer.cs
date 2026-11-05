namespace geo;

internal static class WkbSizer
{
    internal static int GetSize(Geometry geometry)
    {
        GeometryTransforms.ValidateEditedGeometryStructure(geometry);
        GeometryInspection.ExtractValidatedCoordinates(geometry);
        ValidateSerializationTree(geometry);
        return GetGeometrySizeValue(geometry);
    }

    internal static int GetConfiguredSize(Geometry geometry, WkbWriteOptions options)
    {
        ValidateWriteOptions(options);
        int size = GetSize(geometry);
        if (options.Flavor == WkbFlavor.Ewkb && options.IncludeSpatialReferenceId)
        {
            size = AddSize(size, 4);
        }
        return size;
    }

    internal static void ValidateWriteOptions(WkbWriteOptions options)
    {
        if (options == null)
        {
            throw new InvalidGeometryException("WKB write options are null.");
        }
        if (options.ByteOrder != ByteOrder.BigEndian && options.ByteOrder != ByteOrder.LittleEndian)
        {
            throw new InvalidGeometryException("WKB byte order must be big-endian or little-endian.");
        }
        if (options.Flavor != WkbFlavor.Ogc && options.Flavor != WkbFlavor.Ewkb)
        {
            throw new InvalidGeometryException("WKB flavor must be OGC or EWKB.");
        }
        if (options.Flavor == WkbFlavor.Ogc && options.IncludeSpatialReferenceId)
        {
            throw new InvalidGeometryException("Strict OGC WKB cannot include an SRID.");
        }
    }

    internal static int GetGeometrySizeValue(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int size = 5;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return AddCoordinateBytes(size, 1, geometry.CoordinateLayout);
            case GeometryType.LineString:
                return GetLineTaggedSize((LineString)payload, geometry.CoordinateLayout);
            case GeometryType.Polygon:
                return AddSize(size, GetPolygonPayloadSize((Polygon)payload, geometry.CoordinateLayout));
            case GeometryType.MultiPoint:
                return GetMultiPointSize((MultiPoint)payload, geometry.CoordinateLayout);
            case GeometryType.MultiLineString:
                return GetMultiLineSize((MultiLineString)payload, geometry.CoordinateLayout);
            case GeometryType.MultiPolygon:
                return GetMultiPolygonSize((MultiPolygon)payload, geometry.CoordinateLayout);
            case GeometryType.GeometryCollection:
                return GetGeometryArraySize(((GeometryCollection)payload).Geometries, size);
            case GeometryType.Triangle:
                return AddSize(size, GetTrianglePayloadSize((Triangle)payload, geometry.CoordinateLayout));
            case GeometryType.Tin:
                return GetTinSize((Tin)payload, geometry.CoordinateLayout);
            case GeometryType.PolyhedralSurface:
                return GetPolyhedralSurfaceSize((PolyhedralSurface)payload, geometry.CoordinateLayout);
            case GeometryType.CircularString:
                return GetCircularTaggedSize((CircularString)payload, geometry.CoordinateLayout);
            case GeometryType.CompoundCurve:
                return GetGeometryArraySize(((CompoundCurve)payload).Curves, size);
            case GeometryType.CurvePolygon:
                return GetCurvePolygonSize((CurvePolygon)payload, size);
            case GeometryType.MultiCurve:
                return GetGeometryArraySize(((MultiCurve)payload).Curves, size);
            case GeometryType.MultiSurface:
                return GetGeometryArraySize(((MultiSurface)payload).Surfaces, size);
            default:
                throw new InvalidGeometryException("Geometry type cannot be sized as WKB.");
        }
    }

    internal static int GetLineTaggedSize(LineString line, CoordinateLayout layout)
    {
        int size = 9;
        return AddCoordinateBytes(size, line.Coordinates.Length, layout);
    }

    internal static int GetCircularTaggedSize(CircularString circularString, CoordinateLayout layout)
    {
        int size = 9;
        return AddCoordinateBytes(size, circularString.Coordinates.Length, layout);
    }

    internal static int GetPolygonPayloadSize(Polygon polygon, CoordinateLayout layout)
    {
        int size = 4;
        if (polygon.Shell.Coordinates.Length == 0) return size;
        size = AddRingSize(size, polygon.Shell, layout);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            size = AddRingSize(size, polygon.Holes[index], layout);
            index++;
        }
        return size;
    }

    internal static int GetTrianglePayloadSize(Triangle triangle, CoordinateLayout layout)
    {
        int size = 4;
        if (triangle.Shell.Coordinates.Length == 0) return size;
        return AddRingSize(size, triangle.Shell, layout);
    }

    internal static int AddRingSize(int size, LinearRing ring, CoordinateLayout layout)
    {
        size = AddSize(size, 4);
        return AddCoordinateBytes(size, ring.Coordinates.Length, layout);
    }

    internal static int GetMultiPointSize(MultiPoint multiPoint, CoordinateLayout layout)
    {
        int size = 9;
        int componentSize = AddCoordinateBytes(5, 1, layout);
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            size = AddSize(size, componentSize);
            index++;
        }
        return size;
    }

    internal static int GetMultiLineSize(MultiLineString multiLine, CoordinateLayout layout)
    {
        int size = 9;
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            size = AddSize(size, GetLineTaggedSize(multiLine.LineStrings[index], layout));
            index++;
        }
        return size;
    }

    internal static int GetMultiPolygonSize(MultiPolygon multiPolygon, CoordinateLayout layout)
    {
        int size = 9;
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            int componentSize = AddSize(5, GetPolygonPayloadSize(multiPolygon.Polygons[index], layout));
            size = AddSize(size, componentSize);
            index++;
        }
        return size;
    }

    internal static int GetGeometryArraySize(Geometry[] geometries, int taggedHeaderSize)
    {
        int size = AddSize(taggedHeaderSize, 4);
        int index = 0;
        while (index < geometries.Length)
        {
            size = AddSize(size, GetGeometrySizeValue(geometries[index]));
            index++;
        }
        return size;
    }

    internal static int GetTinSize(Tin tin, CoordinateLayout layout)
    {
        int size = 9;
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            int componentSize = AddSize(5, GetTrianglePayloadSize(tin.Triangles[index], layout));
            size = AddSize(size, componentSize);
            index++;
        }
        return size;
    }

    internal static int GetPolyhedralSurfaceSize(PolyhedralSurface surface, CoordinateLayout layout)
    {
        int size = 9;
        int index = 0;
        while (index < surface.Patches.Length)
        {
            int componentSize = AddSize(5, GetPolygonPayloadSize(surface.Patches[index], layout));
            size = AddSize(size, componentSize);
            index++;
        }
        return size;
    }

    internal static int GetCurvePolygonSize(CurvePolygon polygon, int taggedHeaderSize)
    {
        int size = AddSize(taggedHeaderSize, 4);
        if (polygon.Shell == null) return size;
        size = AddSize(size, GetGeometrySizeValue(polygon.Shell));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            size = AddSize(size, GetGeometrySizeValue(polygon.Holes[index]));
            index++;
        }
        return size;
    }

    internal static int AddCoordinateBytes(int size, int count, CoordinateLayout layout)
    {
        int ordinateCount = WkbParser.GetLayoutOrdinateCount(layout);
        int bytesPerCoordinate = ordinateCount * 8;
        if (count > (int.MaxValue - size) / bytesPerCoordinate)
        {
            throw new InvalidGeometryException("WKB size exceeds the supported signed size range.");
        }
        return size + count * bytesPerCoordinate;
    }

    internal static int AddSize(int size, int amount)
    {
        if (size < 0 || amount < 0 || amount > int.MaxValue - size)
        {
            throw new InvalidGeometryException("WKB size exceeds the supported signed size range.");
        }
        return size + amount;
    }

    internal static void ValidateSerializationTree(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.GeometryCollection:
                ValidateGeometryArray(((GeometryCollection)payload).Geometries);
                break;
            case GeometryType.Triangle:
                ValidateTrianglePayload((Triangle)payload);
                break;
            case GeometryType.Tin:
                ValidateTriangleArray(((Tin)payload).Triangles);
                break;
            case GeometryType.PolyhedralSurface:
                ValidatePolygonArray(((PolyhedralSurface)payload).Patches, geometry.CoordinateLayout);
                break;
            case GeometryType.CircularString:
                ValidateCircularStringPayload((CircularString)payload);
                break;
            case GeometryType.CompoundCurve:
                ValidateCompoundCurvePayload((CompoundCurve)payload);
                break;
            case GeometryType.CurvePolygon:
                ValidateCurvePolygonPayload((CurvePolygon)payload);
                break;
            case GeometryType.MultiCurve:
                ValidateMultiCurvePayload((MultiCurve)payload);
                break;
            case GeometryType.MultiSurface:
                ValidateMultiSurfacePayload((MultiSurface)payload);
                break;
            default:
                break;
        }
    }

    internal static void ValidateGeometryArray(Geometry[] geometries)
    {
        int index = 0;
        while (index < geometries.Length)
        {
            ValidateSerializationTree(geometries[index]);
            index++;
        }
    }

    internal static void ValidateTrianglePayload(Triangle triangle)
    {
        if (triangle.Shell.Coordinates.Length != 0 && triangle.Shell.Coordinates.Length != 4)
        {
            throw new InvalidGeometryException("A non-empty Triangle must contain exactly four ring coordinates.");
        }
    }

    internal static void ValidateTriangleArray(Triangle[] triangles)
    {
        int index = 0;
        while (index < triangles.Length)
        {
            ValidateTrianglePayload(triangles[index]);
            index++;
        }
    }

    internal static void ValidatePolygonArray(Polygon[] polygons, CoordinateLayout layout)
    {
        int index = 0;
        while (index < polygons.Length)
        {
            Polygon polygon = polygons[index];
            Geometry holder = new Geometry();
            holder.Point = null;
            holder.GeometryType = GeometryType.Polygon;
            holder.CoordinateLayout = layout;
            holder.Polygon = polygon;
            GeometryTransforms.ValidateEditedGeometryStructure(holder);
            index++;
        }
    }

    internal static void ValidateCircularStringPayload(CircularString circularString)
    {
        int count = circularString.Coordinates.Length;
        if (count != 0 && (count < 3 || count % 2 == 0))
        {
            throw new InvalidGeometryException(
                "A non-empty CircularString requires an odd number of at least three coordinates.");
        }
    }

    internal static void ValidateCompoundCurvePayload(CompoundCurve compoundCurve)
    {
        Coordinate? previousEnd = null;
        int index = 0;
        while (index < compoundCurve.Curves.Length)
        {
            Geometry curve = compoundCurve.Curves[index];
            if (curve.GeometryType != GeometryType.LineString && curve.GeometryType != GeometryType.CircularString)
            {
                throw new InvalidGeometryException(
                    "CompoundCurve components must be LineString or CircularString values.");
            }
            ValidateSerializationTree(curve);
            Coordinate? start = GetCurveStartCoordinate(curve);
            Coordinate? end = GetCurveEndCoordinate(curve);
            if (start == null || end == null)
            {
                throw new InvalidGeometryException("CompoundCurve components cannot be empty.");
            }
            if (previousEnd != null && (previousEnd.X != start.X || previousEnd.Y != start.Y))
            {
                throw new InvalidGeometryException("CompoundCurve components must be continuous.");
            }
            previousEnd = end;
            index++;
        }
    }

    internal static void ValidateCurvePolygonPayload(CurvePolygon polygon)
    {
        if (polygon.Shell == null)
        {
            if (polygon.Holes.Length != 0)
            {
                throw new InvalidGeometryException("An empty CurvePolygon cannot contain holes.");
            }
            return;
        }

        ValidateCurveRing(polygon.Shell);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            ValidateCurveRing(polygon.Holes[index]);
            index++;
        }
    }

    internal static void ValidateCurveRing(Geometry geometry)
    {
        ValidateCurveType(geometry, true);
        Coordinate? start = GetCurveStartCoordinate(geometry);
        Coordinate? end = GetCurveEndCoordinate(geometry);
        if (start == null || end == null || start.X != end.X || start.Y != end.Y)
        {
            throw new InvalidGeometryException("CurvePolygon rings must be non-empty and closed.");
        }
        if (geometry.GeometryType == GeometryType.LineString && geometry.LineString != null)
        {
            Coordinate[] coordinates = geometry.LineString.Coordinates;
            if (coordinates.Length < 4 || !GeometryInspection.HasThreeDistinctPlanarCoordinates(coordinates))
            {
                throw new InvalidGeometryException(
                    "A linear CurvePolygon ring requires four coordinates and three distinct positions.");
            }
        }
    }

    internal static void ValidateMultiCurvePayload(MultiCurve multiCurve)
    {
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            ValidateCurveType(multiCurve.Curves[index], true);
            index++;
        }
    }

    internal static void ValidateCurveType(Geometry geometry, bool allowCompoundCurve)
    {
        if (geometry.GeometryType != GeometryType.LineString && geometry.GeometryType != GeometryType.CircularString &&
            (!allowCompoundCurve || geometry.GeometryType != GeometryType.CompoundCurve))
        {
            throw new InvalidGeometryException("A curve component has an unsupported geometry type.");
        }
        ValidateSerializationTree(geometry);
    }

    internal static void ValidateMultiSurfacePayload(MultiSurface multiSurface)
    {
        int index = 0;
        while (index < multiSurface.Surfaces.Length)
        {
            Geometry surface = multiSurface.Surfaces[index];
            if (surface.GeometryType != GeometryType.Polygon && surface.GeometryType != GeometryType.CurvePolygon &&
                surface.GeometryType != GeometryType.Triangle && surface.GeometryType != GeometryType.Tin &&
                surface.GeometryType != GeometryType.PolyhedralSurface)
            {
                throw new InvalidGeometryException("A MultiSurface component has an unsupported geometry type.");
            }
            ValidateSerializationTree(surface);
            index++;
        }
    }

    internal static Coordinate? GetCurveStartCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString && geometry.LineString != null)
        {
            Coordinate[] coordinates = geometry.LineString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CircularString && geometry.CircularString != null)
        {
            Coordinate[] coordinates = geometry.CircularString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve && geometry.CompoundCurve != null)
        {
            Geometry[] curves = geometry.CompoundCurve.Curves;
            return curves.Length == 0 ? null : GetCurveStartCoordinate(curves[0]);
        }
        return null;
    }

    internal static Coordinate? GetCurveEndCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString && geometry.LineString != null)
        {
            Coordinate[] coordinates = geometry.LineString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CircularString && geometry.CircularString != null)
        {
            Coordinate[] coordinates = geometry.CircularString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve && geometry.CompoundCurve != null)
        {
            Geometry[] curves = geometry.CompoundCurve.Curves;
            return curves.Length == 0 ? null : GetCurveEndCoordinate(curves[curves.Length - 1]);
        }
        return null;
    }
}
