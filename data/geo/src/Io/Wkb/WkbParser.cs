using System;

namespace geo;

internal class WkbTypeInfo
{
    internal GeometryType GeometryType;
    internal CoordinateLayout CoordinateLayout;
    internal bool HasSpatialReferenceId;
}

internal enum WkbComponentRequirement
{
    Any = 0,
    Point = 1,
    LineString = 2,
    Polygon = 3,
    Triangle = 4,
    CompoundCurvePart = 5,
    Curve = 6,
    Surface = 7
}

internal class WkbParser
{
    internal byte[] Source = [];
    internal WkbReadOptions Options = new WkbReadOptions();
    internal int Position;
    internal int CoordinateCount;
    internal int ComponentCount;
    internal ParseError? Failure;

    internal WkbParser(byte[] source, WkbReadOptions options)
    {
        if (source == null)
        {
            SetParseFailure(ParseErrorKind.UnexpectedEnd, 0, "WKB input is null at byte offset 0.");
        }
        else
        {
            Source = source;
        }

        if (options == null)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, 0, "WKB read options are null at byte offset 0.");
            return;
        }
        Options = options;
        ValidateOptions();
    }

    internal GeometryReadResult ParseDocument()
    {
        GeometryReadResult result = new GeometryReadResult();
        if (Failure != null)
        {
            SetFailureResult(result);
            return result;
        }

        Geometry? geometry = ParseGeometryValue(
            CoordinateLayout.XY, false, WkbComponentRequirement.Any,
            GeometryConstants.UnknownSpatialReferenceId, 0);
        if (Failure != null || geometry == null)
        {
            SetFailureResult(result);
            return result;
        }
        if (Options.RequireExactLength && Position != Source.Length)
        {
            SetParseFailure(ParseErrorKind.TrailingContent, Position,
                $"Unexpected bytes follow the WKB geometry at byte offset {Position}.");
            SetFailureResult(result);
            return result;
        }

        result.Success = true;
        result.Geometry = geometry;
        result.Consumed = Position;
        return result;
    }

    internal Geometry? ParseGeometryValue(
        CoordinateLayout requiredLayout, bool requireLayout, WkbComponentRequirement requirement,
        int inheritedSpatialReferenceId, int depth)
    {
        if (Failure != null) return null;
        int geometryOffset = Position;
        if (depth > Options.MaximumNestingDepth)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, geometryOffset,
                $"Maximum WKB nesting depth of {Options.MaximumNestingDepth} was exceeded at byte offset {geometryOffset}.");
            return null;
        }
        if (!EnsureAvailable(1, "a WKB byte-order marker")) return null;

        byte marker = Source[Position];
        Position++;
        ByteOrder byteOrder;
        if (marker == (byte)ByteOrder.BigEndian)
        {
            byteOrder = ByteOrder.BigEndian;
        }
        else if (marker == (byte)ByteOrder.LittleEndian)
        {
            byteOrder = ByteOrder.LittleEndian;
        }
        else
        {
            SetParseFailure(ParseErrorKind.InvalidWkb, geometryOffset,
                $"WKB byte-order marker {marker} is invalid at byte offset {geometryOffset}.");
            return null;
        }

        int typeOffset = Position;
        uint encodedType = ReadUnsignedInteger(byteOrder, "a WKB geometry type code");
        if (Failure != null) return null;
        WkbTypeInfo? typeInfo = DecodeType(encodedType, typeOffset);
        if (typeInfo == null) return null;
        if (requireLayout && typeInfo.CoordinateLayout != requiredLayout)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinateLayout, typeOffset,
                $"Nested WKB layout {typeInfo.CoordinateLayout} does not match required layout {requiredLayout} at byte offset {typeOffset}.");
            return null;
        }
        if (!ValidateRequiredType(typeInfo.GeometryType, requirement, geometryOffset)) return null;
        if (!RegisterComponent(geometryOffset)) return null;

        int spatialReferenceId = inheritedSpatialReferenceId;
        if (typeInfo.HasSpatialReferenceId)
        {
            int spatialReferenceIdOffset = Position;
            uint encodedSpatialReferenceId = ReadUnsignedInteger(byteOrder, "an EWKB spatial reference identifier");
            if (Failure != null) return null;
            if (encodedSpatialReferenceId > int.MaxValue)
            {
                SetParseFailure(ParseErrorKind.InvalidWkb, spatialReferenceIdOffset,
                    $"EWKB spatial reference identifier exceeds the supported signed range at byte offset {spatialReferenceIdOffset}.");
                return null;
            }
            spatialReferenceId = (int)encodedSpatialReferenceId;
            if (depth != 0 && spatialReferenceId != inheritedSpatialReferenceId)
            {
                SetParseFailure(ParseErrorKind.InvalidWkb, spatialReferenceIdOffset,
                    $"Nested EWKB spatial reference identifier {spatialReferenceId} does not match inherited identifier {inheritedSpatialReferenceId} at byte offset {spatialReferenceIdOffset}.");
                return null;
            }
        }

        switch (typeInfo.GeometryType)
        {
            case GeometryType.Point:
                Point? point = ParsePointPayload(typeInfo.CoordinateLayout, byteOrder);
                if (point == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, point);
            case GeometryType.LineString:
                LineString? line = ParseLineStringPayload(typeInfo.CoordinateLayout, byteOrder);
                if (line == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, line);
            case GeometryType.Polygon:
                Polygon? polygon = ParsePolygonPayload(typeInfo.CoordinateLayout, byteOrder);
                if (polygon == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, polygon);
            case GeometryType.MultiPoint:
                MultiPoint? multiPoint = ParseMultiPointPayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (multiPoint == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, multiPoint);
            case GeometryType.MultiLineString:
                MultiLineString? multiLine = ParseMultiLineStringPayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (multiLine == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, multiLine);
            case GeometryType.MultiPolygon:
                MultiPolygon? multiPolygon = ParseMultiPolygonPayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (multiPolygon == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, multiPolygon);
            case GeometryType.GeometryCollection:
                GeometryCollection? collection = ParseGeometryCollectionPayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (collection == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, collection);
            case GeometryType.Triangle:
                Triangle? triangle = ParseTrianglePayload(typeInfo.CoordinateLayout, byteOrder);
                if (triangle == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, triangle);
            case GeometryType.Tin:
                Tin? tin = ParseTinPayload(typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (tin == null) return null;
                return CreateGeometryValue(typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, tin);
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface? polyhedralSurface = ParsePolyhedralSurfacePayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (polyhedralSurface == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, polyhedralSurface);
            case GeometryType.CircularString:
                CircularString? circularString = ParseCircularStringPayload(typeInfo.CoordinateLayout, byteOrder);
                if (circularString == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, circularString);
            case GeometryType.CompoundCurve:
                CompoundCurve? compoundCurve = ParseCompoundCurvePayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (compoundCurve == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, compoundCurve);
            case GeometryType.CurvePolygon:
                CurvePolygon? curvePolygon = ParseCurvePolygonPayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (curvePolygon == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, curvePolygon);
            case GeometryType.MultiCurve:
                MultiCurve? multiCurve = ParseMultiCurvePayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (multiCurve == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, multiCurve);
            case GeometryType.MultiSurface:
                MultiSurface? multiSurface = ParseMultiSurfacePayload(
                    typeInfo.CoordinateLayout, byteOrder, spatialReferenceId, depth);
                if (multiSurface == null) return null;
                return CreateGeometryValue(
                    typeInfo.GeometryType, typeInfo.CoordinateLayout, spatialReferenceId, multiSurface);
            default:
                SetParseFailure(ParseErrorKind.UnsupportedType, typeOffset,
                    $"WKB geometry type {encodedType} is unsupported at byte offset {typeOffset}.");
                return null;
        }
    }

    internal Point? ParsePointPayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int coordinateOffset = Position;
        Coordinate? coordinate = ReadPointCoordinate(layout, byteOrder);
        if (Failure != null) return null;
        Point point = new Point();
        point.Coordinate = coordinate;
        if (coordinate != null && !RegisterCoordinates(1, coordinateOffset)) return null;
        return point;
    }

    internal LineString? ParseLineStringPayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int countOffset = Position;
        int count = ReadCount("LineString coordinate count", byteOrder);
        if (Failure != null) return null;
        if (count == 1)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, countOffset,
                $"A WKB LineString must be empty or contain at least two coordinates at byte offset {countOffset}.");
            return null;
        }

        Coordinate[]? coordinates = ReadCoordinateSequence(count, layout, byteOrder, countOffset);
        if (coordinates == null) return null;
        LineString line = new LineString();
        line.Coordinates = coordinates;
        return line;
    }

    internal Polygon? ParsePolygonPayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int countOffset = Position;
        int ringCount = ReadCount("Polygon ring count", byteOrder);
        if (Failure != null) return null;
        Polygon polygon = new Polygon();
        if (ringCount == 0) return polygon;
        if (!ReserveComponents(ringCount, countOffset)) return null;
        if (!EnsureItemHeaders(ringCount, 4, "polygon ring coordinate counts")) return null;

        LinearRing[] rings = new LinearRing[ringCount];
        int index = 0;
        while (index < ringCount && Failure == null)
        {
            LinearRing? ring = ParseLinearRingPayload(layout, byteOrder);
            if (ring == null) return null;
            rings[index] = ring;
            index++;
        }

        polygon.Shell = rings[0];
        polygon.Holes = new LinearRing[ringCount - 1];
        index = 1;
        while (index < ringCount)
        {
            polygon.Holes[index - 1] = rings[index];
            index++;
        }
        return polygon;
    }

    internal LinearRing? ParseLinearRingPayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int countOffset = Position;
        int count = ReadCount("Polygon ring coordinate count", byteOrder);
        if (Failure != null) return null;
        if (count < 4)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, countOffset,
                $"A WKB polygon ring must contain at least four coordinates at byte offset {countOffset}.");
            return null;
        }

        Coordinate[]? coordinates = ReadCoordinateSequence(count, layout, byteOrder, countOffset);
        if (coordinates == null) return null;
        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Length - 1];
        if (first.X != last.X || first.Y != last.Y)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, countOffset,
                $"A WKB polygon ring must be closed at byte offset {countOffset}.");
            return null;
        }
        if (!GeometryInspection.HasThreeDistinctPlanarCoordinates(coordinates))
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, countOffset,
                $"A WKB polygon ring requires three distinct planar positions at byte offset {countOffset}.");
            return null;
        }

        LinearRing ring = new LinearRing();
        ring.Coordinates = coordinates;
        return ring;
    }

    internal MultiPoint? ParseMultiPointPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("MultiPoint component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        MultiPoint multiPoint = new MultiPoint();
        multiPoint.Points = new Point[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            int componentOffset = Position;
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Point, spatialReferenceId, depth + 1);
            if (component == null) return null;
            if (component.GeometryType != GeometryType.Point || component.Point == null)
            {
                SetParseFailure(ParseErrorKind.InvalidGeometryType, componentOffset,
                    $"A WKB MultiPoint component must be a Point at byte offset {componentOffset}.");
                return null;
            }
            multiPoint.Points[index] = component.Point;
            index++;
        }
        return multiPoint;
    }

    internal MultiLineString? ParseMultiLineStringPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("MultiLineString component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        MultiLineString multiLine = new MultiLineString();
        multiLine.LineStrings = new LineString[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            int componentOffset = Position;
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.LineString, spatialReferenceId, depth + 1);
            if (component == null) return null;
            if (component.GeometryType != GeometryType.LineString || component.LineString == null)
            {
                SetParseFailure(ParseErrorKind.InvalidGeometryType, componentOffset,
                    $"A WKB MultiLineString component must be a LineString at byte offset {componentOffset}.");
                return null;
            }
            multiLine.LineStrings[index] = component.LineString;
            index++;
        }
        return multiLine;
    }

    internal MultiPolygon? ParseMultiPolygonPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("MultiPolygon component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        MultiPolygon multiPolygon = new MultiPolygon();
        multiPolygon.Polygons = new Polygon[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            int componentOffset = Position;
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Polygon, spatialReferenceId, depth + 1);
            if (component == null) return null;
            if (component.GeometryType != GeometryType.Polygon || component.Polygon == null)
            {
                SetParseFailure(ParseErrorKind.InvalidGeometryType, componentOffset,
                    $"A WKB MultiPolygon component must be a Polygon at byte offset {componentOffset}.");
                return null;
            }
            multiPolygon.Polygons[index] = component.Polygon;
            index++;
        }
        return multiPolygon;
    }

    internal GeometryCollection? ParseGeometryCollectionPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("GeometryCollection component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        GeometryCollection collection = new GeometryCollection();
        collection.Geometries = new Geometry[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Any, spatialReferenceId, depth + 1);
            if (component == null) return null;
            collection.Geometries[index] = component;
            index++;
        }
        return collection;
    }

    internal Triangle? ParseTrianglePayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int triangleOffset = Position;
        Polygon? polygon = ParsePolygonPayload(layout, byteOrder);
        if (polygon == null) return null;
        if (polygon.Shell.Coordinates.Length == 0)
        {
            return new Triangle();
        }
        if (polygon.Holes.Length != 0)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, triangleOffset,
                $"A WKB Triangle cannot contain interior rings at byte offset {triangleOffset}.");
            return null;
        }
        if (polygon.Shell.Coordinates.Length != 4)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, triangleOffset,
                $"A WKB Triangle must contain exactly three vertices and one closure coordinate at byte offset {triangleOffset}.");
            return null;
        }

        Triangle triangle = new Triangle();
        triangle.Shell = polygon.Shell;
        return triangle;
    }

    internal Tin? ParseTinPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("TIN patch count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        Tin tin = new Tin();
        tin.Triangles = new Triangle[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Triangle, spatialReferenceId, depth + 1);
            if (component == null || component.Triangle == null) return null;
            tin.Triangles[index] = component.Triangle;
            index++;
        }
        return tin;
    }

    internal PolyhedralSurface? ParsePolyhedralSurfacePayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("PolyhedralSurface patch count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        PolyhedralSurface surface = new PolyhedralSurface();
        surface.Patches = new Polygon[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Polygon, spatialReferenceId, depth + 1);
            if (component == null || component.Polygon == null) return null;
            surface.Patches[index] = component.Polygon;
            index++;
        }
        return surface;
    }

    internal CircularString? ParseCircularStringPayload(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int countOffset = Position;
        int count = ReadCount("CircularString coordinate count", byteOrder);
        if (Failure != null) return null;
        if (count != 0 && (count < 3 || count % 2 == 0))
        {
            SetParseFailure(ParseErrorKind.InvalidCount, countOffset,
                $"A WKB CircularString requires an odd number of at least three coordinates at byte offset {countOffset}.");
            return null;
        }

        Coordinate[]? coordinates = ReadCoordinateSequence(count, layout, byteOrder, countOffset);
        if (coordinates == null) return null;
        CircularString circularString = new CircularString();
        circularString.Coordinates = coordinates;
        return circularString;
    }

    internal CompoundCurve? ParseCompoundCurvePayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("CompoundCurve component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        CompoundCurve compoundCurve = new CompoundCurve();
        compoundCurve.Curves = new Geometry[count];
        Coordinate? previousEnd = null;
        int index = 0;
        while (index < count && Failure == null)
        {
            int componentOffset = Position;
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.CompoundCurvePart, spatialReferenceId, depth + 1);
            if (component == null) return null;
            Coordinate? start = GetCurveStartCoordinate(component);
            Coordinate? end = GetCurveEndCoordinate(component);
            if (start == null || end == null)
            {
                SetParseFailure(ParseErrorKind.InvalidCount, componentOffset,
                    $"A WKB CompoundCurve component cannot be empty at byte offset {componentOffset}.");
                return null;
            }
            if (previousEnd != null && (previousEnd.X != start.X || previousEnd.Y != start.Y))
            {
                SetParseFailure(ParseErrorKind.InvalidCoordinate, componentOffset,
                    $"WKB CompoundCurve components must be continuous at byte offset {componentOffset}.");
                return null;
            }
            compoundCurve.Curves[index] = component;
            previousEnd = end;
            index++;
        }
        return compoundCurve;
    }

    internal CurvePolygon? ParseCurvePolygonPayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("CurvePolygon ring count", byteOrder);
        if (Failure != null) return null;
        CurvePolygon polygon = new CurvePolygon();
        if (count == 0) return polygon;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        Geometry[] rings = new Geometry[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            int ringOffset = Position;
            Geometry? ring = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Curve, spatialReferenceId, depth + 1);
            if (ring == null) return null;
            if (!ValidateCurveRingGeometry(ring, ringOffset)) return null;
            rings[index] = ring;
            index++;
        }

        polygon.Shell = rings[0];
        polygon.Holes = new Geometry[count - 1];
        index = 1;
        while (index < count)
        {
            polygon.Holes[index - 1] = rings[index];
            index++;
        }
        return polygon;
    }

    internal MultiCurve? ParseMultiCurvePayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("MultiCurve component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        MultiCurve multiCurve = new MultiCurve();
        multiCurve.Curves = new Geometry[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Curve, spatialReferenceId, depth + 1);
            if (component == null) return null;
            multiCurve.Curves[index] = component;
            index++;
        }
        return multiCurve;
    }

    internal MultiSurface? ParseMultiSurfacePayload(
        CoordinateLayout layout, ByteOrder byteOrder, int spatialReferenceId, int depth)
    {
        int countOffset = Position;
        int count = ReadCount("MultiSurface component count", byteOrder);
        if (Failure != null) return null;
        if (!CheckNestedComponentCount(count, countOffset)) return null;
        MultiSurface multiSurface = new MultiSurface();
        multiSurface.Surfaces = new Geometry[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Geometry? component = ParseGeometryValue(
                layout, true, WkbComponentRequirement.Surface, spatialReferenceId, depth + 1);
            if (component == null) return null;
            multiSurface.Surfaces[index] = component;
            index++;
        }
        return multiSurface;
    }

    internal Coordinate[]? ReadCoordinateSequence(int count, CoordinateLayout layout, ByteOrder byteOrder, int countOffset)
    {
        if (!RegisterCoordinates(count, countOffset)) return null;
        if (!EnsureCoordinateBytes(count, layout)) return null;
        Coordinate[] coordinates = new Coordinate[count];
        int index = 0;
        while (index < count && Failure == null)
        {
            Coordinate? coordinate = ReadFiniteCoordinate(layout, byteOrder);
            if (coordinate == null) return null;
            coordinates[index] = coordinate;
            index++;
        }
        return coordinates;
    }

    internal Coordinate? ReadPointCoordinate(CoordinateLayout layout, ByteOrder byteOrder)
    {
        if (!EnsureCoordinateBytes(1, layout)) return null;
        int coordinateOffset = Position;
        double x = ReadDoubleValue(byteOrder, "a point X ordinate");
        double y = ReadDoubleValue(byteOrder, "a point Y ordinate");
        double z = GeometryConstants.NoDataOrdinate;
        double m = GeometryConstants.NoDataOrdinate;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            z = ReadDoubleValue(byteOrder, "a point Z ordinate");
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            m = ReadDoubleValue(byteOrder, "a point M ordinate");
        }
        if (Failure != null) return null;

        bool zIsEmpty = layout == CoordinateLayout.XY || layout == CoordinateLayout.XYM || double.IsNaN(z);
        bool mIsEmpty = layout == CoordinateLayout.XY || layout == CoordinateLayout.XYZ || double.IsNaN(m);
        bool isEmpty = double.IsNaN(x) && double.IsNaN(y) && zIsEmpty && mIsEmpty;
        if (isEmpty) return null;
        if (!AreFiniteOrAbsent(x, y, z, m, layout))
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, coordinateOffset,
                $"A non-empty WKB Point requires finite ordinates at byte offset {coordinateOffset}.");
            return null;
        }

        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        coordinate.Z = z;
        coordinate.M = m;
        return coordinate;
    }

    internal Coordinate? ReadFiniteCoordinate(CoordinateLayout layout, ByteOrder byteOrder)
    {
        int coordinateOffset = Position;
        double x = ReadDoubleValue(byteOrder, "an X ordinate");
        double y = ReadDoubleValue(byteOrder, "a Y ordinate");
        double z = GeometryConstants.NoDataOrdinate;
        double m = GeometryConstants.NoDataOrdinate;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            z = ReadDoubleValue(byteOrder, "a Z ordinate");
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            m = ReadDoubleValue(byteOrder, "an M ordinate");
        }
        if (Failure != null) return null;
        if (!AreFiniteOrAbsent(x, y, z, m, layout))
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, coordinateOffset,
                $"WKB coordinate ordinates must be finite at byte offset {coordinateOffset}.");
            return null;
        }

        Coordinate coordinate = new Coordinate();
        coordinate.X = x;
        coordinate.Y = y;
        coordinate.Z = z;
        coordinate.M = m;
        return coordinate;
    }

    internal bool ValidateRequiredType(
        GeometryType geometryType, WkbComponentRequirement requirement, int geometryOffset)
    {
        bool valid = requirement == WkbComponentRequirement.Any;
        if (requirement == WkbComponentRequirement.Point) valid = geometryType == GeometryType.Point;
        else if (requirement == WkbComponentRequirement.LineString) valid = geometryType == GeometryType.LineString;
        else if (requirement == WkbComponentRequirement.Polygon) valid = geometryType == GeometryType.Polygon;
        else if (requirement == WkbComponentRequirement.Triangle) valid = geometryType == GeometryType.Triangle;
        else if (requirement == WkbComponentRequirement.CompoundCurvePart)
        {
            valid = geometryType == GeometryType.LineString || geometryType == GeometryType.CircularString;
        }
        else if (requirement == WkbComponentRequirement.Curve)
        {
            valid = geometryType == GeometryType.LineString || geometryType == GeometryType.CircularString ||
                geometryType == GeometryType.CompoundCurve;
        }
        else if (requirement == WkbComponentRequirement.Surface)
        {
            valid = geometryType == GeometryType.Polygon || geometryType == GeometryType.CurvePolygon ||
                geometryType == GeometryType.Triangle || geometryType == GeometryType.Tin ||
                geometryType == GeometryType.PolyhedralSurface;
        }

        if (valid) return true;
        SetParseFailure(ParseErrorKind.InvalidGeometryType, geometryOffset,
            $"Nested WKB geometry type {geometryType} does not match required type {requirement} at byte offset {geometryOffset}.");
        return false;
    }

    internal bool ValidateCurveRingGeometry(Geometry geometry, int offset)
    {
        Coordinate? start = GetCurveStartCoordinate(geometry);
        Coordinate? end = GetCurveEndCoordinate(geometry);
        if (start == null || end == null || start.X != end.X || start.Y != end.Y)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, offset,
                $"A WKB CurvePolygon ring must be non-empty and closed at byte offset {offset}.");
            return false;
        }
        if (geometry.GeometryType == GeometryType.LineString && geometry.LineString != null)
        {
            Coordinate[] coordinates = geometry.LineString.Coordinates;
            if (coordinates.Length < 4 || !GeometryInspection.HasThreeDistinctPlanarCoordinates(coordinates))
            {
                SetParseFailure(ParseErrorKind.InvalidCoordinate, offset,
                    $"A linear WKB CurvePolygon ring requires four coordinates and three distinct planar positions at byte offset {offset}.");
                return false;
            }
        }
        return true;
    }

    internal Coordinate? GetCurveStartCoordinate(Geometry geometry)
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

    internal Coordinate? GetCurveEndCoordinate(Geometry geometry)
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

    internal uint ReadUnsignedInteger(ByteOrder byteOrder, string description)
    {
        if (!EnsureAvailable(4, description)) return 0;
        uint value = EndianReader.ReadUInt32FromSpan(Source, Position, byteOrder);
        Position += 4;
        return value;
    }

    internal double ReadDoubleValue(ByteOrder byteOrder, string description)
    {
        if (!EnsureAvailable(8, description)) return 0.0;
        double value = EndianReader.ReadDoubleFromSpan(Source, Position, byteOrder);
        Position += 8;
        return value;
    }

    internal int ReadCount(string description, ByteOrder byteOrder)
    {
        int countOffset = Position;
        uint encoded = ReadUnsignedInteger(byteOrder, description);
        if (Failure != null) return -1;
        if (encoded > int.MaxValue)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, countOffset,
                $"{description} exceeds the supported signed count range at byte offset {countOffset}.");
            return -1;
        }
        return (int)encoded;
    }

    internal WkbTypeInfo? DecodeType(uint encodedType, int offset)
    {
        uint baseCode = encodedType;
        CoordinateLayout layout = CoordinateLayout.XY;
        bool hasSpatialReferenceId = false;
        if (Options.Flavor == WkbFlavor.Ewkb)
        {
            baseCode = encodedType & WkbTypeCodes.EwkbTypeMask;
            bool hasZ = (encodedType & WkbTypeCodes.EwkbZFlag) != 0u;
            bool hasM = (encodedType & WkbTypeCodes.EwkbMFlag) != 0u;
            hasSpatialReferenceId = (encodedType & WkbTypeCodes.EwkbSridFlag) != 0u;
            if (hasZ && hasM) layout = CoordinateLayout.XYZM;
            else if (hasZ) layout = CoordinateLayout.XYZ;
            else if (hasM) layout = CoordinateLayout.XYM;
        }
        else if (encodedType >= 3000u && encodedType < 4000u)
        {
            baseCode = encodedType - 3000u;
            layout = CoordinateLayout.XYZM;
        }
        else if (encodedType >= 2000u && encodedType < 3000u)
        {
            baseCode = encodedType - 2000u;
            layout = CoordinateLayout.XYM;
        }
        else if (encodedType >= 1000u && encodedType < 2000u)
        {
            baseCode = encodedType - 1000u;
            layout = CoordinateLayout.XYZ;
        }

        if (baseCode == 13u || baseCode == 14u)
        {
            SetParseFailure(ParseErrorKind.UnsupportedType, offset,
                $"Abstract WKB geometry type {encodedType} is unsupported at byte offset {offset}.");
            return null;
        }
        if (baseCode < 1u || baseCode > 17u ||
            (Options.Flavor == WkbFlavor.Ogc && encodedType >= 4000u))
        {
            SetParseFailure(ParseErrorKind.InvalidGeometryType, offset,
                $"WKB geometry type code {encodedType} is invalid at byte offset {offset}.");
            return null;
        }

        WkbTypeInfo info = new WkbTypeInfo();
        info.GeometryType = (GeometryType)(int)baseCode;
        info.CoordinateLayout = layout;
        info.HasSpatialReferenceId = hasSpatialReferenceId;
        return info;
    }

    internal Geometry CreateGeometryValue(
        GeometryType geometryType, CoordinateLayout layout, int spatialReferenceId, object payload)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = geometryType;
        geometry.CoordinateLayout = layout;
        geometry.SpatialReferenceId = spatialReferenceId;
        switch (geometryType)
        {
            case GeometryType.Point:
                geometry.Point = (Point)payload;
                break;
            case GeometryType.LineString:
                geometry.LineString = (LineString)payload;
                break;
            case GeometryType.Polygon:
                geometry.Polygon = (Polygon)payload;
                break;
            case GeometryType.MultiPoint:
                geometry.MultiPoint = (MultiPoint)payload;
                break;
            case GeometryType.MultiLineString:
                geometry.MultiLineString = (MultiLineString)payload;
                break;
            case GeometryType.MultiPolygon:
                geometry.MultiPolygon = (MultiPolygon)payload;
                break;
            case GeometryType.GeometryCollection:
                geometry.GeometryCollection = (GeometryCollection)payload;
                break;
            case GeometryType.Triangle:
                geometry.Triangle = (Triangle)payload;
                break;
            case GeometryType.Tin:
                geometry.Tin = (Tin)payload;
                break;
            case GeometryType.PolyhedralSurface:
                geometry.PolyhedralSurface = (PolyhedralSurface)payload;
                break;
            case GeometryType.CircularString:
                geometry.CircularString = (CircularString)payload;
                break;
            case GeometryType.CompoundCurve:
                geometry.CompoundCurve = (CompoundCurve)payload;
                break;
            case GeometryType.CurvePolygon:
                geometry.CurvePolygon = (CurvePolygon)payload;
                break;
            case GeometryType.MultiCurve:
                geometry.MultiCurve = (MultiCurve)payload;
                break;
            case GeometryType.MultiSurface:
                geometry.MultiSurface = (MultiSurface)payload;
                break;
            default:
                break;
        }
        return geometry;
    }

    internal bool EnsureAvailable(int count, string description)
    {
        if (EndianReader.HasBytes(Source, Position, count)) return true;
        int missingOffset = Source.Length;
        SetParseFailure(ParseErrorKind.UnexpectedEnd, missingOffset,
            $"WKB input ended before {description} at byte offset {missingOffset}.");
        return false;
    }

    internal bool EnsureCoordinateBytes(int count, CoordinateLayout layout)
    {
        int bytesPerCoordinate = GetLayoutOrdinateCount(layout) * 8;
        int remaining = Source.Length - Position;
        if (count <= remaining / bytesPerCoordinate) return true;
        SetParseFailure(ParseErrorKind.UnexpectedEnd, Source.Length,
            $"WKB input ended before {count} complete {layout} coordinates at byte offset {Source.Length}.");
        return false;
    }

    internal bool EnsureItemHeaders(int count, int bytesPerItem, string description)
    {
        int remaining = Source.Length - Position;
        if (count <= remaining / bytesPerItem) return true;
        SetParseFailure(ParseErrorKind.UnexpectedEnd, Source.Length,
            $"WKB input ended before {count} {description} at byte offset {Source.Length}.");
        return false;
    }

    internal bool CheckNestedComponentCount(int count, int offset)
    {
        if (count > Options.MaximumComponents - ComponentCount)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, offset,
                $"Maximum WKB component count of {Options.MaximumComponents} would be exceeded at byte offset {offset}.");
            return false;
        }
        return EnsureItemHeaders(count, 5, "nested geometry headers");
    }

    internal bool ReserveComponents(int count, int offset)
    {
        if (count > Options.MaximumComponents - ComponentCount)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, offset,
                $"Maximum WKB component count of {Options.MaximumComponents} was exceeded at byte offset {offset}.");
            return false;
        }
        ComponentCount += count;
        return true;
    }

    internal bool RegisterComponent(int offset)
    {
        return ReserveComponents(1, offset);
    }

    internal bool RegisterCoordinates(int count, int offset)
    {
        if (count > Options.MaximumCoordinates - CoordinateCount)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, offset,
                $"Maximum WKB coordinate count of {Options.MaximumCoordinates} was exceeded at byte offset {offset}.");
            return false;
        }
        CoordinateCount += count;
        return true;
    }

    internal void ValidateOptions()
    {
        if (Options.Flavor != WkbFlavor.Ogc && Options.Flavor != WkbFlavor.Ewkb)
        {
            SetParseFailure(ParseErrorKind.UnsupportedType, 0,
                "WKB read flavor is invalid at byte offset 0.");
            return;
        }
        if (Options.MaximumCoordinates < 1)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, 0,
                "Maximum WKB coordinate count must be positive at byte offset 0.");
            return;
        }
        if (Options.MaximumComponents < 1)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, 0,
                "Maximum WKB component count must be positive at byte offset 0.");
            return;
        }
        if (Options.MaximumNestingDepth < 0)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, 0,
                "Maximum WKB nesting depth cannot be negative at byte offset 0.");
            return;
        }
        if (Options.MaximumInputBytes < 1)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, 0,
                "Maximum WKB input size must be positive at byte offset 0.");
            return;
        }
        if (Source.Length > Options.MaximumInputBytes)
        {
            SetParseFailure(ParseErrorKind.LimitExceeded, Options.MaximumInputBytes,
                $"Maximum WKB input size of {Options.MaximumInputBytes} bytes was exceeded at byte offset {Options.MaximumInputBytes}.");
        }
    }

    internal void SetParseFailure(ParseErrorKind kind, int offset, string message)
    {
        if (Failure != null) return;
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Offset = offset;
        error.Message = message;
        Failure = error;
    }

    internal void SetFailureResult(GeometryReadResult result)
    {
        if (Failure == null)
        {
            SetParseFailure(ParseErrorKind.InvalidWkb, Position,
                $"WKB parsing failed at byte offset {Position}.");
        }
        result.Error = Failure;
        result.Consumed = Failure == null ? 0 : (int)Failure.Offset;
    }

    internal static bool AreFiniteOrAbsent(double x, double y, double z, double m, CoordinateLayout layout)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) return false;
        if ((layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM) && !double.IsFinite(z)) return false;
        if ((layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM) && !double.IsFinite(m)) return false;
        return true;
    }

    internal static int GetLayoutOrdinateCount(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XY) return 2;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYM) return 3;
        return 4;
    }
}
