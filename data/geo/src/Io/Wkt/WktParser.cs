using System.Collections.Generic;

namespace geo;

internal class WktParser
{
    internal WktTokenizer Tokenizer;
    internal WktReadOptions Options;
    internal WktToken Current = new WktToken();
    internal ParseError? Failure;
    internal int SpatialReferenceId;
    internal int CoordinateCount;
    internal int ComponentCount;

    internal WktParser(string source, WktReadOptions options)
    {
        Options = options;
        Tokenizer = new WktTokenizer(source, options);
        AdvanceToken();
    }

    internal GeometryReadResult ParseDocument()
    {
        GeometryReadResult result = new GeometryReadResult();
        if (Failure != null)
        {
            SetFailureResult(result);
            return result;
        }

        ParseSpatialReferencePrefix();
        Geometry? geometry = ParseTaggedGeometry(CoordinateLayout.XY, false);
        if (Failure != null || geometry == null)
        {
            SetFailureResult(result);
            return result;
        }

        if (Current.Kind != WktTokenKind.End)
        {
            SetParseFailure(ParseErrorKind.TrailingContent, Current.Offset,
                $"Unexpected content follows the WKT geometry at character offset {Current.Offset}.");
            SetFailureResult(result);
            return result;
        }

        result.Success = true;
        result.Geometry = geometry;
        result.Consumed = Tokenizer.Position;
        return result;
    }

    internal void ParseSpatialReferencePrefix()
    {
        if (Failure != null || Current.Kind != WktTokenKind.Keyword || Current.Text != "SRID")
        {
            return;
        }

        int prefixOffset = Current.Offset;
        if (!Options.AllowSpatialReferenceId)
        {
            SetParseFailure(ParseErrorKind.UnexpectedToken, prefixOffset,
                $"An SRID prefix is not enabled at character offset {prefixOffset}.");
            return;
        }

        AdvanceToken();
        if (!ConsumeExpectedToken(WktTokenKind.Equals, "'=' after SRID")) return;
        if (Current.Kind != WktTokenKind.Number)
        {
            SetExpectedTokenFailure("an unsigned integer SRID");
            return;
        }

        WktToken sridToken = Current;
        if (!IsUnsignedIntegerText(sridToken.Text) || sridToken.Number > int.MaxValue)
        {
            SetParseFailure(ParseErrorKind.InvalidNumber, sridToken.Offset,
                $"SRID must be an unsigned 32-bit integer at character offset {sridToken.Offset}.");
            return;
        }

        SpatialReferenceId = (int)sridToken.Number;
        AdvanceToken();
        ConsumeExpectedToken(WktTokenKind.Semicolon, "';' after the SRID value");
    }

    internal Geometry? ParseTaggedGeometry(CoordinateLayout requiredLayout, bool requireLayout)
    {
        if (Failure != null) return null;
        if (Current.Kind != WktTokenKind.Keyword)
        {
            SetExpectedGeometryTypeFailure();
            return null;
        }

        int typeOffset = Current.Offset;
        string typeName = Current.Text;
        GeometryType geometryType = ParseGeometryTypeName(typeName);
        if ((int)geometryType < 0)
        {
            if (IsReservedTypeName(typeName))
            {
                SetParseFailure(ParseErrorKind.UnsupportedType, typeOffset,
                    $"WKT geometry type '{typeName}' is reserved and unsupported at character offset {typeOffset}.");
            }
            else
            {
                SetParseFailure(ParseErrorKind.InvalidGeometryType, typeOffset,
                    $"Unknown WKT geometry type '{typeName}' at character offset {typeOffset}.");
            }
            return null;
        }
        if (IsExtendedGeometryType(geometryType) && !Options.AllowExtendedTypes)
        {
            SetParseFailure(ParseErrorKind.UnsupportedType, typeOffset,
                $"Extended WKT geometry type '{typeName}' is disabled at character offset {typeOffset}.");
            return null;
        }

        AdvanceToken();
        CoordinateLayout layout = CoordinateLayout.XY;
        int layoutOffset = typeOffset;
        if (Current.Kind == WktTokenKind.Keyword && IsLayoutToken(Current.Text))
        {
            layout = ParseLayoutToken(Current.Text);
            layoutOffset = Current.Offset;
            AdvanceToken();
        }

        if (requireLayout && layout != requiredLayout)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinateLayout, layoutOffset,
                $"Nested WKT geometry layout {layout} does not match required layout {requiredLayout} at character offset {layoutOffset}.");
            return null;
        }

        if (!RegisterComponent(typeOffset)) return null;
        if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
        {
            AdvanceToken();
            return CreateEmptyGeometry(geometryType, layout);
        }

        switch (geometryType)
        {
            case GeometryType.Point:
                Point? point = ParsePointPayload(layout);
                if (point == null) return null;
                return CreateGeometryValue(geometryType, layout, point);
            case GeometryType.LineString:
                Coordinate[]? lineCoordinates = ParseLineCoordinateSequence(layout);
                if (lineCoordinates == null) return null;
                LineString line = new LineString();
                line.Coordinates = lineCoordinates;
                return CreateGeometryValue(geometryType, layout, line);
            case GeometryType.Polygon:
                Polygon? polygon = ParsePolygonText(layout);
                if (polygon == null) return null;
                return CreateGeometryValue(geometryType, layout, polygon);
            case GeometryType.MultiPoint:
                MultiPoint? multiPoint = ParseMultiPointPayload(layout);
                if (multiPoint == null) return null;
                return CreateGeometryValue(geometryType, layout, multiPoint);
            case GeometryType.MultiLineString:
                MultiLineString? multiLine = ParseMultiLinePayload(layout);
                if (multiLine == null) return null;
                return CreateGeometryValue(geometryType, layout, multiLine);
            case GeometryType.MultiPolygon:
                MultiPolygon? multiPolygon = ParseMultiPolygonPayload(layout);
                if (multiPolygon == null) return null;
                return CreateGeometryValue(geometryType, layout, multiPolygon);
            case GeometryType.GeometryCollection:
                GeometryCollection? collection = ParseGeometryCollectionPayload(layout);
                if (collection == null) return null;
                return CreateGeometryValue(geometryType, layout, collection);
            case GeometryType.Triangle:
                Triangle? triangle = ParseTriangleText(layout);
                if (triangle == null) return null;
                return CreateGeometryValue(geometryType, layout, triangle);
            case GeometryType.Tin:
                Tin? tin = ParseTinPayload(layout);
                if (tin == null) return null;
                return CreateGeometryValue(geometryType, layout, tin);
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface? polyhedralSurface = ParsePolyhedralSurfacePayload(layout);
                if (polyhedralSurface == null) return null;
                return CreateGeometryValue(geometryType, layout, polyhedralSurface);
            case GeometryType.CircularString:
                CircularString? circularString = ParseCircularStringPayload(layout);
                if (circularString == null) return null;
                return CreateGeometryValue(geometryType, layout, circularString);
            case GeometryType.CompoundCurve:
                CompoundCurve? compoundCurve = ParseCompoundCurvePayload(layout);
                if (compoundCurve == null) return null;
                return CreateGeometryValue(geometryType, layout, compoundCurve);
            case GeometryType.CurvePolygon:
                CurvePolygon? curvePolygon = ParseCurvePolygonPayload(layout);
                if (curvePolygon == null) return null;
                return CreateGeometryValue(geometryType, layout, curvePolygon);
            case GeometryType.MultiCurve:
                MultiCurve? multiCurve = ParseMultiCurvePayload(layout);
                if (multiCurve == null) return null;
                return CreateGeometryValue(geometryType, layout, multiCurve);
            case GeometryType.MultiSurface:
                MultiSurface? multiSurface = ParseMultiSurfacePayload(layout);
                if (multiSurface == null) return null;
                return CreateGeometryValue(geometryType, layout, multiSurface);
            default:
                SetParseFailure(ParseErrorKind.InvalidGeometryType, typeOffset,
                    $"WKT geometry type is invalid at character offset {typeOffset}.");
                return null;
        }
    }

    internal Point? ParsePointPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before a point coordinate")) return null;
        Coordinate? coordinate = ParseCoordinate(layout);
        if (coordinate == null) return null;
        if (!ConsumeExpectedToken(WktTokenKind.RightParenthesis, "')' after a point coordinate")) return null;
        Point point = new Point();
        point.Coordinate = coordinate;
        return point;
    }

    internal Coordinate[]? ParseLineCoordinateSequence(CoordinateLayout layout)
    {
        int sequenceOffset = Current.Offset;
        Coordinate[]? coordinates = ParseCoordinateSequence(layout);
        if (coordinates == null) return null;
        if (coordinates.Length < 2)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, sequenceOffset,
                $"A WKT LineString must contain at least two coordinates at character offset {sequenceOffset}.");
            return null;
        }
        return coordinates;
    }

    internal Coordinate[]? ParseRingCoordinateSequence(CoordinateLayout layout)
    {
        int sequenceOffset = Current.Offset;
        Coordinate[]? coordinates = ParseCoordinateSequence(layout);
        if (coordinates == null) return null;
        if (coordinates.Length < 4)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, sequenceOffset,
                $"A WKT polygon ring must contain at least four coordinates at character offset {sequenceOffset}.");
            return null;
        }

        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Length - 1];
        if (first.X != last.X || first.Y != last.Y)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, sequenceOffset,
                $"A WKT polygon ring must be closed at character offset {sequenceOffset}.");
            return null;
        }
        if (!GeometryInspection.HasThreeDistinctPlanarCoordinates(coordinates))
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, sequenceOffset,
                $"A WKT polygon ring requires three distinct planar positions at character offset {sequenceOffset}.");
            return null;
        }
        return coordinates;
    }

    internal Coordinate[]? ParseCoordinateSequence(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before a coordinate sequence")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty coordinate sequence must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Coordinate> coordinates = new List<Coordinate>();
        while (Failure == null)
        {
            Coordinate? coordinate = ParseCoordinate(layout);
            if (coordinate == null) return null;
            coordinates.Add(coordinate);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                return coordinates.ToArray();
            }

            SetExpectedTokenFailure("',' or ')' after a coordinate");
            return null;
        }

        return null;
    }

    internal Coordinate? ParseCoordinate(CoordinateLayout layout)
    {
        int coordinateOffset = Current.Offset;
        int ordinateCount = GetLayoutOrdinateCount(layout);
        double[] ordinates = new double[ordinateCount];
        int index = 0;
        while (index < ordinateCount)
        {
            if (Current.Kind != WktTokenKind.Number)
            {
                SetParseFailure(ParseErrorKind.InvalidCoordinate, Current.Offset,
                    $"Expected ordinate {index + 1} of {ordinateCount} at character offset {Current.Offset}.");
                return null;
            }
            ordinates[index] = Current.Number;
            AdvanceToken();
            index++;
        }

        if (Current.Kind == WktTokenKind.Number)
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinateLayout, Current.Offset,
                $"Coordinate contains more than {ordinateCount} ordinates for layout {layout} at character offset {Current.Offset}.");
            return null;
        }
        if (!RegisterCoordinate(coordinateOffset)) return null;

        Coordinate coordinate = new Coordinate();
        coordinate.X = ordinates[0];
        coordinate.Y = ordinates[1];
        if (layout == CoordinateLayout.XYZ)
        {
            coordinate.Z = ordinates[2];
        }
        else if (layout == CoordinateLayout.XYM)
        {
            coordinate.M = ordinates[2];
        }
        else if (layout == CoordinateLayout.XYZM)
        {
            coordinate.Z = ordinates[2];
            coordinate.M = ordinates[3];
        }
        return coordinate;
    }

    internal Polygon? ParsePolygonText(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before polygon rings")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty polygon must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<LinearRing> rings = new List<LinearRing>();
        while (Failure == null)
        {
            int ringOffset = Current.Offset;
            if (!RegisterComponent(ringOffset)) return null;
            Coordinate[]? coordinates = ParseRingCoordinateSequence(layout);
            if (coordinates == null) return null;
            LinearRing ring = new LinearRing();
            ring.Coordinates = coordinates;
            rings.Add(ring);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                Polygon polygon = new Polygon();
                polygon.Shell = rings[0];
                if (rings.Count > 1)
                {
                    LinearRing[] holes = new LinearRing[rings.Count - 1];
                    int index = 1;
                    while (index < rings.Count)
                    {
                        holes[index - 1] = rings[index];
                        index++;
                    }
                    polygon.Holes = holes;
                }
                return polygon;
            }

            SetExpectedTokenFailure("',' or ')' after a polygon ring");
            return null;
        }

        return null;
    }

    internal MultiPoint? ParseMultiPointPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before MultiPoint components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty MultiPoint must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Point> points = new List<Point>();
        while (Failure == null)
        {
            int pointOffset = Current.Offset;
            if (!RegisterComponent(pointOffset)) return null;
            Point? point;
            if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
            {
                AdvanceToken();
                point = new Point();
            }
            else
            {
                point = ParsePointPayload(layout);
                if (point == null) return null;
            }
            points.Add(point);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                MultiPoint multiPoint = new MultiPoint();
                multiPoint.Points = points.ToArray();
                return multiPoint;
            }

            SetExpectedTokenFailure("',' or ')' after a MultiPoint component");
            return null;
        }

        return null;
    }

    internal MultiLineString? ParseMultiLinePayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before MultiLineString components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty MultiLineString must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<LineString> lines = new List<LineString>();
        while (Failure == null)
        {
            int lineOffset = Current.Offset;
            if (!RegisterComponent(lineOffset)) return null;
            LineString line = new LineString();
            if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
            {
                AdvanceToken();
            }
            else
            {
                Coordinate[]? coordinates = ParseLineCoordinateSequence(layout);
                if (coordinates == null) return null;
                line.Coordinates = coordinates;
            }
            lines.Add(line);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                MultiLineString multiLine = new MultiLineString();
                multiLine.LineStrings = lines.ToArray();
                return multiLine;
            }

            SetExpectedTokenFailure("',' or ')' after a MultiLineString component");
            return null;
        }

        return null;
    }

    internal MultiPolygon? ParseMultiPolygonPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before MultiPolygon components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty MultiPolygon must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Polygon> polygons = new List<Polygon>();
        while (Failure == null)
        {
            int polygonOffset = Current.Offset;
            if (!RegisterComponent(polygonOffset)) return null;
            Polygon? polygon;
            if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
            {
                AdvanceToken();
                polygon = new Polygon();
            }
            else
            {
                polygon = ParsePolygonText(layout);
                if (polygon == null) return null;
            }
            polygons.Add(polygon);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                MultiPolygon multiPolygon = new MultiPolygon();
                multiPolygon.Polygons = polygons.ToArray();
                return multiPolygon;
            }

            SetExpectedTokenFailure("',' or ')' after a MultiPolygon component");
            return null;
        }

        return null;
    }

    internal GeometryCollection? ParseGeometryCollectionPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before GeometryCollection components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty GeometryCollection must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Geometry> geometries = new List<Geometry>();
        while (Failure == null)
        {
            Geometry? geometry = ParseTaggedGeometry(layout, true);
            if (geometry == null) return null;
            geometries.Add(geometry);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                GeometryCollection collection = new GeometryCollection();
                collection.Geometries = geometries.ToArray();
                return collection;
            }

            SetExpectedTokenFailure("',' or ')' after a GeometryCollection component");
            return null;
        }

        return null;
    }

    internal Triangle? ParseTriangleText(CoordinateLayout layout)
    {
        int triangleOffset = Current.Offset;
        Polygon? polygon = ParsePolygonText(layout);
        if (polygon == null) return null;
        if (polygon.Holes.Length != 0)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, triangleOffset,
                $"A WKT Triangle cannot contain interior rings at character offset {triangleOffset}.");
            return null;
        }
        if (polygon.Shell.Coordinates.Length != 4)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, triangleOffset,
                $"A WKT Triangle must contain exactly three vertices and one closure coordinate at character offset {triangleOffset}.");
            return null;
        }

        Triangle triangle = new Triangle();
        triangle.Shell = polygon.Shell;
        return triangle;
    }

    internal Tin? ParseTinPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before TIN patches")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty TIN must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Triangle> triangles = new List<Triangle>();
        while (Failure == null)
        {
            int triangleOffset = Current.Offset;
            if (!RegisterComponent(triangleOffset)) return null;
            Triangle? triangle;
            if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
            {
                AdvanceToken();
                triangle = new Triangle();
            }
            else
            {
                triangle = ParseTriangleText(layout);
                if (triangle == null) return null;
            }
            triangles.Add(triangle);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                Tin tin = new Tin();
                tin.Triangles = triangles.ToArray();
                return tin;
            }

            SetExpectedTokenFailure("',' or ')' after a TIN patch");
            return null;
        }

        return null;
    }

    internal PolyhedralSurface? ParsePolyhedralSurfacePayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before PolyhedralSurface patches")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty PolyhedralSurface must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Polygon> patches = new List<Polygon>();
        while (Failure == null)
        {
            int patchOffset = Current.Offset;
            if (!RegisterComponent(patchOffset)) return null;
            Polygon? polygon;
            if (Current.Kind == WktTokenKind.Keyword && Current.Text == "EMPTY")
            {
                AdvanceToken();
                polygon = new Polygon();
            }
            else
            {
                polygon = ParsePolygonText(layout);
                if (polygon == null) return null;
            }
            patches.Add(polygon);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                PolyhedralSurface surface = new PolyhedralSurface();
                surface.Patches = patches.ToArray();
                return surface;
            }

            SetExpectedTokenFailure("',' or ')' after a PolyhedralSurface patch");
            return null;
        }

        return null;
    }

    internal CircularString? ParseCircularStringPayload(CoordinateLayout layout)
    {
        int curveOffset = Current.Offset;
        Coordinate[]? coordinates = ParseCoordinateSequence(layout);
        if (coordinates == null) return null;
        if (coordinates.Length < 3 || coordinates.Length % 2 == 0)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, curveOffset,
                $"A WKT CircularString requires an odd number of at least three control coordinates at character offset {curveOffset}.");
            return null;
        }

        CircularString circularString = new CircularString();
        circularString.Coordinates = coordinates;
        return circularString;
    }

    internal CompoundCurve? ParseCompoundCurvePayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before CompoundCurve components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty CompoundCurve must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Geometry> curves = new List<Geometry>();
        while (Failure == null)
        {
            int curveOffset = Current.Offset;
            Geometry? curve = ParseCurveComponent(layout, false, false);
            if (curve == null) return null;
            Coordinate? start = GetCurveStartCoordinate(curve);
            Coordinate? end = GetCurveEndCoordinate(curve);
            if (start == null || end == null)
            {
                SetParseFailure(ParseErrorKind.InvalidCount, curveOffset,
                    $"A CompoundCurve component cannot be empty at character offset {curveOffset}.");
                return null;
            }
            if (curves.Count != 0)
            {
                Coordinate? previousEnd = GetCurveEndCoordinate(curves[curves.Count - 1]);
                if (previousEnd == null || previousEnd.X != start.X || previousEnd.Y != start.Y)
                {
                    SetParseFailure(ParseErrorKind.InvalidCoordinate, curveOffset,
                        $"CompoundCurve components must be continuous at character offset {curveOffset}.");
                    return null;
                }
            }
            curves.Add(curve);

            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                CompoundCurve compoundCurve = new CompoundCurve();
                compoundCurve.Curves = curves.ToArray();
                return compoundCurve;
            }

            SetExpectedTokenFailure("',' or ')' after a CompoundCurve component");
            return null;
        }

        return null;
    }

    internal CurvePolygon? ParseCurvePolygonPayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before CurvePolygon rings")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty CurvePolygon must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Geometry> rings = new List<Geometry>();
        while (Failure == null)
        {
            int ringOffset = Current.Offset;
            Geometry? ring = ParseCurveComponent(layout, true, true);
            if (ring == null) return null;
            if (!ValidateCurveRingValue(ring, ringOffset))
            {
                return null;
            }
            rings.Add(ring);

            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                CurvePolygon polygon = new CurvePolygon();
                polygon.Shell = rings[0];
                if (rings.Count > 1)
                {
                    Geometry[] holes = new Geometry[rings.Count - 1];
                    int index = 1;
                    while (index < rings.Count)
                    {
                        holes[index - 1] = rings[index];
                        index++;
                    }
                    polygon.Holes = holes;
                }
                return polygon;
            }

            SetExpectedTokenFailure("',' or ')' after a CurvePolygon ring");
            return null;
        }

        return null;
    }

    internal MultiCurve? ParseMultiCurvePayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before MultiCurve components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty MultiCurve must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Geometry> curves = new List<Geometry>();
        while (Failure == null)
        {
            Geometry? curve = ParseCurveComponent(layout, true, true);
            if (curve == null) return null;
            curves.Add(curve);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                MultiCurve multiCurve = new MultiCurve();
                multiCurve.Curves = curves.ToArray();
                return multiCurve;
            }

            SetExpectedTokenFailure("',' or ')' after a MultiCurve component");
            return null;
        }

        return null;
    }

    internal MultiSurface? ParseMultiSurfacePayload(CoordinateLayout layout)
    {
        if (!ConsumeExpectedToken(WktTokenKind.LeftParenthesis, "'(' before MultiSurface components")) return null;
        if (Current.Kind == WktTokenKind.RightParenthesis)
        {
            SetParseFailure(ParseErrorKind.InvalidCount, Current.Offset,
                $"An empty MultiSurface must use the EMPTY keyword at character offset {Current.Offset}.");
            return null;
        }

        List<Geometry> surfaces = new List<Geometry>();
        while (Failure == null)
        {
            Geometry? surface = ParseSurfaceComponent(layout);
            if (surface == null) return null;
            surfaces.Add(surface);
            if (Current.Kind == WktTokenKind.Comma)
            {
                AdvanceToken();
                continue;
            }
            if (Current.Kind == WktTokenKind.RightParenthesis)
            {
                AdvanceToken();
                MultiSurface multiSurface = new MultiSurface();
                multiSurface.Surfaces = surfaces.ToArray();
                return multiSurface;
            }

            SetExpectedTokenFailure("',' or ')' after a MultiSurface component");
            return null;
        }

        return null;
    }

    internal Geometry? ParseCurveComponent(CoordinateLayout layout, bool allowCompoundCurve, bool allowTaggedLine)
    {
        int componentOffset = Current.Offset;
        if (Current.Kind == WktTokenKind.LeftParenthesis)
        {
            if (!RegisterComponent(componentOffset)) return null;
            Coordinate[]? coordinates = ParseLineCoordinateSequence(layout);
            if (coordinates == null) return null;
            LineString line = new LineString();
            line.Coordinates = coordinates;
            return CreateGeometryValue(GeometryType.LineString, layout, line);
        }

        if (Current.Kind == WktTokenKind.Keyword &&
            (Current.Text == "CIRCULARSTRING" || (allowCompoundCurve && Current.Text == "COMPOUNDCURVE") ||
            (allowTaggedLine && Current.Text == "LINESTRING")))
        {
            return ParseTaggedGeometry(layout, true);
        }

        SetParseFailure(ParseErrorKind.UnexpectedToken, componentOffset,
            $"Expected a linear, circular, or compound curve component at character offset {componentOffset}.");
        return null;
    }

    internal Geometry? ParseSurfaceComponent(CoordinateLayout layout)
    {
        int componentOffset = Current.Offset;
        if (Current.Kind == WktTokenKind.LeftParenthesis)
        {
            if (!RegisterComponent(componentOffset)) return null;
            Polygon? polygon = ParsePolygonText(layout);
            if (polygon == null) return null;
            return CreateGeometryValue(GeometryType.Polygon, layout, polygon);
        }

        if (Current.Kind == WktTokenKind.Keyword &&
            (Current.Text == "POLYGON" || Current.Text == "CURVEPOLYGON" || Current.Text == "TRIANGLE" ||
            Current.Text == "TIN" || Current.Text == "POLYHEDRALSURFACE"))
        {
            return ParseTaggedGeometry(layout, true);
        }

        SetParseFailure(ParseErrorKind.UnexpectedToken, componentOffset,
            $"Expected a polygon or CurvePolygon surface component at character offset {componentOffset}.");
        return null;
    }

    internal bool IsClosedCurveValue(Geometry geometry)
    {
        Coordinate? start = GetCurveStartCoordinate(geometry);
        Coordinate? end = GetCurveEndCoordinate(geometry);
        return start != null && end != null && start.X == end.X && start.Y == end.Y;
    }

    internal bool ValidateCurveRingValue(Geometry geometry, int offset)
    {
        if (!IsClosedCurveValue(geometry))
        {
            SetParseFailure(ParseErrorKind.InvalidCoordinate, offset,
                $"A CurvePolygon ring must be non-empty and closed at character offset {offset}.");
            return false;
        }
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(geometry);
            if (line.Coordinates.Length < 4 || !GeometryInspection.HasThreeDistinctPlanarCoordinates(line.Coordinates))
            {
                SetParseFailure(ParseErrorKind.InvalidCoordinate, offset,
                    $"A linear CurvePolygon ring requires four coordinates and three distinct planar positions at character offset {offset}.");
                return false;
            }
        }
        return true;
    }

    internal Coordinate? GetCurveStartCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(geometry);
            Coordinate[] coordinates = line.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CircularString)
        {
            CircularString circularString = (CircularString)GeometryDispatch.GetSelectedPayload(geometry);
            Coordinate[] coordinates = circularString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[0];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve)
        {
            CompoundCurve compoundCurve = (CompoundCurve)GeometryDispatch.GetSelectedPayload(geometry);
            Geometry[] curves = compoundCurve.Curves;
            return curves.Length == 0 ? null : GetCurveStartCoordinate(curves[0]);
        }
        return null;
    }

    internal Coordinate? GetCurveEndCoordinate(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString)
        {
            LineString line = (LineString)GeometryDispatch.GetSelectedPayload(geometry);
            Coordinate[] coordinates = line.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CircularString)
        {
            CircularString circularString = (CircularString)GeometryDispatch.GetSelectedPayload(geometry);
            Coordinate[] coordinates = circularString.Coordinates;
            return coordinates.Length == 0 ? null : coordinates[coordinates.Length - 1];
        }
        if (geometry.GeometryType == GeometryType.CompoundCurve)
        {
            CompoundCurve compoundCurve = (CompoundCurve)GeometryDispatch.GetSelectedPayload(geometry);
            Geometry[] curves = compoundCurve.Curves;
            return curves.Length == 0 ? null : GetCurveEndCoordinate(curves[curves.Length - 1]);
        }
        return null;
    }

    internal Geometry CreateEmptyGeometry(GeometryType geometryType, CoordinateLayout layout)
    {
        return GeometryBuilder.CreateEmptyValue(geometryType, CreateGeometryOptions(layout));
    }

    internal Geometry CreateGeometryValue(GeometryType geometryType, CoordinateLayout layout, object payload)
    {
        Geometry geometry = CreateEmptyGeometry(geometryType, layout);
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

    internal GeometryFactoryOptions CreateGeometryOptions(CoordinateLayout layout)
    {
        GeometryFactoryOptions options = new GeometryFactoryOptions();
        options.CoordinateLayout = layout;
        options.SpatialReferenceId = SpatialReferenceId;
        return options;
    }

    internal bool RegisterCoordinate(int offset)
    {
        CoordinateCount++;
        if (CoordinateCount <= Options.MaximumCoordinates)
        {
            return true;
        }

        SetParseFailure(ParseErrorKind.LimitExceeded, offset,
            $"Maximum WKT coordinate count of {Options.MaximumCoordinates} was exceeded at character offset {offset}.");
        return false;
    }

    internal bool RegisterComponent(int offset)
    {
        ComponentCount++;
        if (ComponentCount <= Options.MaximumComponents)
        {
            return true;
        }

        SetParseFailure(ParseErrorKind.LimitExceeded, offset,
            $"Maximum WKT component count of {Options.MaximumComponents} was exceeded at character offset {offset}.");
        return false;
    }

    internal bool ConsumeExpectedToken(WktTokenKind kind, string expectation)
    {
        if (Failure != null) return false;
        if (Current.Kind != kind)
        {
            SetExpectedTokenFailure(expectation);
            return false;
        }

        AdvanceToken();
        return Failure == null;
    }

    internal void AdvanceToken()
    {
        if (Failure != null) return;
        Current = Tokenizer.ReadToken();
        if (Current.Kind == WktTokenKind.Invalid)
        {
            Failure = Current.Error;
            if (Failure == null)
            {
                SetParseFailure(ParseErrorKind.InvalidWkt, Current.Offset,
                    $"WKT tokenization failed at character offset {Current.Offset}.");
            }
        }
    }

    internal void SetExpectedTokenFailure(string expectation)
    {
        ParseErrorKind kind = Current.Kind == WktTokenKind.End ? ParseErrorKind.UnexpectedEnd : ParseErrorKind.UnexpectedToken;
        SetParseFailure(kind, Current.Offset,
            $"Expected {expectation} at character offset {Current.Offset}.");
    }

    internal void SetExpectedGeometryTypeFailure()
    {
        ParseErrorKind kind = Current.Kind == WktTokenKind.End ? ParseErrorKind.UnexpectedEnd : ParseErrorKind.InvalidGeometryType;
        SetParseFailure(kind, Current.Offset,
            $"Expected a WKT geometry type at character offset {Current.Offset}.");
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
            SetParseFailure(ParseErrorKind.InvalidWkt, Current.Offset,
                $"WKT parsing failed at character offset {Current.Offset}.");
        }
        result.Error = Failure;
        result.Consumed = Failure == null ? 0 : (int)Failure.Offset;
    }

    internal static GeometryType ParseGeometryTypeName(string text)
    {
        if (text == "POINT") return GeometryType.Point;
        if (text == "LINESTRING") return GeometryType.LineString;
        if (text == "POLYGON") return GeometryType.Polygon;
        if (text == "MULTIPOINT") return GeometryType.MultiPoint;
        if (text == "MULTILINESTRING") return GeometryType.MultiLineString;
        if (text == "MULTIPOLYGON") return GeometryType.MultiPolygon;
        if (text == "GEOMETRYCOLLECTION") return GeometryType.GeometryCollection;
        if (text == "CIRCULARSTRING") return GeometryType.CircularString;
        if (text == "COMPOUNDCURVE") return GeometryType.CompoundCurve;
        if (text == "CURVEPOLYGON") return GeometryType.CurvePolygon;
        if (text == "MULTICURVE") return GeometryType.MultiCurve;
        if (text == "MULTISURFACE") return GeometryType.MultiSurface;
        if (text == "POLYHEDRALSURFACE") return GeometryType.PolyhedralSurface;
        if (text == "TIN") return GeometryType.Tin;
        if (text == "TRIANGLE") return GeometryType.Triangle;
        return (GeometryType)(-1);
    }

    internal static bool IsReservedTypeName(string text)
    {
        return text == "GEOMETRY" || text == "CURVE" || text == "SURFACE" || text == "LINEARRING";
    }

    internal static bool IsExtendedGeometryType(GeometryType geometryType)
    {
        return geometryType == GeometryType.Triangle || geometryType == GeometryType.Tin ||
            geometryType == GeometryType.PolyhedralSurface || geometryType == GeometryType.CircularString ||
            geometryType == GeometryType.CompoundCurve || geometryType == GeometryType.CurvePolygon ||
            geometryType == GeometryType.MultiCurve || geometryType == GeometryType.MultiSurface;
    }

    internal static bool IsLayoutToken(string text)
    {
        return text == "Z" || text == "M" || text == "ZM";
    }

    internal static CoordinateLayout ParseLayoutToken(string text)
    {
        if (text == "Z") return CoordinateLayout.XYZ;
        if (text == "M") return CoordinateLayout.XYM;
        if (text == "ZM") return CoordinateLayout.XYZM;
        return CoordinateLayout.XY;
    }

    internal static int GetLayoutOrdinateCount(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XY) return 2;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYM) return 3;
        return 4;
    }

    internal static bool IsUnsignedIntegerText(string text)
    {
        if (text.Length == 0) return false;
        int index = 0;
        while (index < text.Length)
        {
            if (!WktTokenizer.IsAsciiDigit(text[index])) return false;
            index++;
        }
        return true;
    }
}
