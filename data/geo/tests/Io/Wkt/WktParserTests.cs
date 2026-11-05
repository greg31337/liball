using geo;

namespace geo.Tests;

public class WktParserTests
{
    [Test]
    public void ParsesEveryCoreGeometryTypeInEveryCoordinateLayout()
    {
        CoordinateLayout[] layouts =
        {
            CoordinateLayout.XY,
            CoordinateLayout.XYZ,
            CoordinateLayout.XYM,
            CoordinateLayout.XYZM
        };
        GeometryType[] types = CoreTypes();

        foreach (CoordinateLayout layout in layouts)
        {
            foreach (GeometryType type in types)
            {
                string text = NonEmptyText(type, layout);
                GeometryReadResult result = Parse(text, new WktReadOptions());
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, text);
                    Assert.That(result.Error, Is.Null, text);
                    Assert.That(result.Consumed, Is.EqualTo(text.Length), text);
                    Assert.That(result.Geometry, Is.Not.Null, text);
                    Assert.That(result.Geometry!.GeometryType, Is.EqualTo(type), text);
                    Assert.That(result.Geometry.CoordinateLayout, Is.EqualTo(layout), text);
                    Assert.That(result.Geometry.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId), text);
                    Assert.That(GeometryInspection.IsEmptyValue(result.Geometry), Is.False, text);
                });

                Coordinate[] coordinates = GeometryInspection.ExtractCoordinateCopies(result.Geometry!);
                foreach (Coordinate coordinate in coordinates)
                {
                    AssertCoordinateLayout(coordinate, layout, text);
                }
            }
        }
    }

    [Test]
    public void ParsesCanonicalEmptyForEveryCoreGeometryTypeAndLayout()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in CoreTypes())
            {
                string text = TypeName(type) + LayoutSuffix(layout) + " EMPTY";
                Geometry geometry = ParseSuccess(text);
                Assert.Multiple(() =>
                {
                    Assert.That(geometry.GeometryType, Is.EqualTo(type), text);
                    Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout), text);
                    Assert.That(GeometryInspection.IsEmptyValue(geometry), Is.True, text);
                    Assert.That(GeometryDispatch.CountPopulatedPayloads(geometry), Is.EqualTo(1), text);
                });
            }
        }
    }

    [Test]
    public void BuildsExactCorePayloadStructures()
    {
        Geometry point = ParseSuccess("POINT (1 2)");
        Geometry line = ParseSuccess("LINESTRING (0 0, 2 3, 4 5)");
        Geometry polygon = ParseSuccess("POLYGON ((0 0, 6 0, 6 6, 0 6, 0 0), (1 1, 1 2, 2 2, 1 1))");
        Geometry multiPoint = ParseSuccess("MULTIPOINT ((1 2), (3 4))");
        Geometry multiLine = ParseSuccess("MULTILINESTRING ((0 0, 1 1), (2 2, 3 3, 4 4))");
        Geometry multiPolygon = ParseSuccess("MULTIPOLYGON (((0 0, 2 0, 2 2, 0 2, 0 0)), ((3 3, 5 3, 5 5, 3 5, 3 3)))");
        Geometry collection = ParseSuccess("GEOMETRYCOLLECTION (POINT (1 2), LINESTRING (3 4, 5 6), POLYGON EMPTY)");

        Assert.Multiple(() =>
        {
            Assert.That(point.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(point.Point.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(line.LineString!.Coordinates, Has.Length.EqualTo(3));
            Assert.That(line.LineString.Coordinates[2].Y, Is.EqualTo(5.0));
            Assert.That(polygon.Polygon!.Shell.Coordinates, Has.Length.EqualTo(5));
            Assert.That(polygon.Polygon.Holes, Has.Length.EqualTo(1));
            Assert.That(polygon.Polygon.Holes[0].Coordinates, Has.Length.EqualTo(4));
            Assert.That(multiPoint.MultiPoint!.Points, Has.Length.EqualTo(2));
            Assert.That(multiPoint.MultiPoint.Points[1].Coordinate!.X, Is.EqualTo(3.0));
            Assert.That(multiLine.MultiLineString!.LineStrings, Has.Length.EqualTo(2));
            Assert.That(multiLine.MultiLineString.LineStrings[1].Coordinates, Has.Length.EqualTo(3));
            Assert.That(multiPolygon.MultiPolygon!.Polygons, Has.Length.EqualTo(2));
            Assert.That(multiPolygon.MultiPolygon.Polygons[1].Shell.Coordinates[0].X, Is.EqualTo(3.0));
            Assert.That(collection.GeometryCollection!.Geometries, Has.Length.EqualTo(3));
            Assert.That(collection.GeometryCollection.Geometries[0].GeometryType, Is.EqualTo(GeometryType.Point));
            Assert.That(collection.GeometryCollection.Geometries[1].GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(collection.GeometryCollection.Geometries[2].GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(GeometryInspection.IsEmptyValue(collection.GeometryCollection.Geometries[2]), Is.True);
        });
    }

    [Test]
    public void NestedGeometryCollectionsRequireExplicitMatchingLayouts()
    {
        string validText = "GEOMETRYCOLLECTION ZM (POINT ZM (1 2 3 4), GEOMETRYCOLLECTION ZM (LINESTRING ZM (0 0 5 6, 1 1 7 8), POLYGON ZM EMPTY))";
        Geometry valid = ParseSuccess(validText);
        Geometry nested = valid.GeometryCollection!.Geometries[1];

        Assert.Multiple(() =>
        {
            Assert.That(valid.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(valid.GeometryCollection.Geometries[0].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(nested.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(nested.GeometryCollection!.Geometries[0].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(nested.GeometryCollection.Geometries[1].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(nested.GeometryCollection.Geometries[0].LineString!.Coordinates[1].M, Is.EqualTo(8.0));
        });

        AssertFailure("GEOMETRYCOLLECTION Z (POINT (1 2))", ParseErrorKind.InvalidCoordinateLayout, 22, "does not match");
        AssertFailure("GEOMETRYCOLLECTION (POINT Z (1 2 3))", ParseErrorKind.InvalidCoordinateLayout, 26, "does not match");
        AssertFailure("GEOMETRYCOLLECTION M (GEOMETRYCOLLECTION M (POINT Z EMPTY))", ParseErrorKind.InvalidCoordinateLayout, 50, "does not match");
        AssertFailure("GEOMETRYCOLLECTION Z (POINT EMPTY)", ParseErrorKind.InvalidCoordinateLayout, 22, "does not match");
    }

    [Test]
    public void MapsThirdAndFourthOrdinatesAccordingToTheDeclaredLayout()
    {
        Geometry xy = ParseSuccess("POINT (1 2)");
        Geometry xyz = ParseSuccess("POINT Z (1 2 3)");
        Geometry xym = ParseSuccess("POINT M (1 2 4)");
        Geometry xyzm = ParseSuccess("POINT ZM (1 2 3 4)");

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(xy.Point!.Coordinate!.Z), Is.True);
            Assert.That(double.IsNaN(xy.Point.Coordinate.M), Is.True);
            Assert.That(xyz.Point!.Coordinate!.Z, Is.EqualTo(3.0));
            Assert.That(double.IsNaN(xyz.Point.Coordinate.M), Is.True);
            Assert.That(double.IsNaN(xym.Point!.Coordinate!.Z), Is.True);
            Assert.That(xym.Point.Coordinate.M, Is.EqualTo(4.0));
            Assert.That(xyzm.Point!.Coordinate!.Z, Is.EqualTo(3.0));
            Assert.That(xyzm.Point.Coordinate.M, Is.EqualTo(4.0));
        });
    }

    [Test]
    public void ParsesOptionalEnabledEwktSridAndPropagatesItRecursively()
    {
        string text = "  SRID=4326;GEOMETRYCOLLECTION (POINT (1 2), GEOMETRYCOLLECTION (LINESTRING (0 0, 1 1)))  ";
        WktReadOptions enabled = new WktReadOptions { AllowSpatialReferenceId = true };
        GeometryReadResult result = Parse(text, enabled);
        Geometry geometry = result.Geometry!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Consumed, Is.EqualTo(text.Length));
            Assert.That(geometry.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(geometry.GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(geometry.GeometryCollection.Geometries[1].SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(geometry.GeometryCollection.Geometries[1].GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(4326));
        });

        AssertFailure("SRID=4326;POINT (1 2)", new WktReadOptions(), ParseErrorKind.UnexpectedToken, 0, "not enabled");
        AssertFailure("SRID=-1;POINT (1 2)", enabled, ParseErrorKind.InvalidNumber, 5, "unsigned");
        AssertFailure("SRID=4326.0;POINT (1 2)", enabled, ParseErrorKind.InvalidNumber, 5, "unsigned");
        AssertFailure("SRID=2147483648;POINT (1 2)", enabled, ParseErrorKind.InvalidNumber, 5, "32-bit");
    }

    [Test]
    public void RejectsInvalidTypesArityCountsRingsAndStrictEmptySyntax()
    {
        AssertFailure("", ParseErrorKind.UnexpectedEnd, 0, "geometry type");
        AssertFailure("UNKNOWN EMPTY", ParseErrorKind.InvalidGeometryType, 0, "Unknown");
        AssertFailure("POINT EMPTY POINT EMPTY", ParseErrorKind.TrailingContent, 12, "follows");
        AssertFailure("POINT ()", ParseErrorKind.InvalidCoordinate, 7, "ordinate");
        AssertFailure("POINT (1 2 3)", ParseErrorKind.InvalidCoordinateLayout, 11, "more than 2");
        AssertFailure("POINT Z (1 2)", ParseErrorKind.InvalidCoordinate, 12, "ordinate 3");
        AssertFailure("LINESTRING (0 0)", ParseErrorKind.InvalidCount, 11, "at least two");
        AssertFailure("LINESTRING ()", ParseErrorKind.InvalidCount, 12, "EMPTY keyword");
        AssertFailure("POLYGON ()", ParseErrorKind.InvalidCount, 9, "EMPTY keyword");
        AssertFailure("POLYGON ((0 0, 1 0, 1 1, 0 2))", ParseErrorKind.InvalidCoordinate, 9, "closed");
        AssertFailure("POLYGON ((0 0, 1 0, 1 0, 0 0))", ParseErrorKind.InvalidCoordinate, 9, "distinct");
        AssertFailure("MULTIPOINT (0 0, 1 1)", ParseErrorKind.UnexpectedToken, 12, "before a point");
        AssertFailure("MULTIPOINT ()", ParseErrorKind.InvalidCount, 12, "EMPTY keyword");
        AssertFailure("MULTILINESTRING ()", ParseErrorKind.InvalidCount, 17, "EMPTY keyword");
        AssertFailure("MULTIPOLYGON ()", ParseErrorKind.InvalidCount, 14, "EMPTY keyword");
        AssertFailure("GEOMETRYCOLLECTION ()", ParseErrorKind.InvalidCount, 20, "EMPTY keyword");
        AssertFailure("POINT (1e999 2)", ParseErrorKind.InvalidNumber, 7, "finite double");
        AssertFailure("POINT (1 2", ParseErrorKind.UnexpectedEnd, 10, "parentheses");
    }

    [Test]
    public void EnforcesCoordinateComponentAndNestingLimits()
    {
        WktReadOptions coordinates = new WktReadOptions { MaximumCoordinates = 2 };
        WktReadOptions components = new WktReadOptions { MaximumComponents = 2 };
        WktReadOptions depth = new WktReadOptions { MaximumNestingDepth = 1 };

        AssertFailure("LINESTRING (0 0, 1 1, 2 2)", coordinates, ParseErrorKind.LimitExceeded, 22, "coordinate count");
        AssertFailure("MULTIPOINT ((0 0), (1 1))", components, ParseErrorKind.LimitExceeded, 19, "component count");
        AssertFailure("POLYGON ((0 0, 1 0, 1 1, 0 0))", depth, ParseErrorKind.LimitExceeded, 9, "nesting depth");

        Geometry point = ParseSuccess("POINT (1 2)", new WktReadOptions { MaximumCoordinates = 1, MaximumComponents = 1 });
        Assert.That(point.Point!.Coordinate!.X, Is.EqualTo(1.0));
    }

    [Test]
    public void ReturnsStructuredFailuresForNullInputAndOptions()
    {
        GeometryReadResult nullInput = Parse(null!, new WktReadOptions());
        GeometryReadResult nullOptions = Parse("POINT EMPTY", null!);

        Assert.Multiple(() =>
        {
            Assert.That(nullInput.Success, Is.False);
            Assert.That(nullInput.Geometry, Is.Null);
            Assert.That(nullInput.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(nullInput.Error.Offset, Is.EqualTo(0));
            Assert.That(nullOptions.Success, Is.False);
            Assert.That(nullOptions.Geometry, Is.Null);
            Assert.That(nullOptions.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(nullOptions.Error.Offset, Is.EqualTo(0));
        });
    }

    private static GeometryReadResult Parse(string text, WktReadOptions options)
    {
        return new WktParser(text, options).ParseDocument();
    }

    private static Geometry ParseSuccess(string text)
    {
        return ParseSuccess(text, new WktReadOptions());
    }

    private static Geometry ParseSuccess(string text, WktReadOptions options)
    {
        GeometryReadResult result = Parse(text, options);
        Assert.That(result.Success, Is.True, result.Error?.Message);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Geometry, Is.Not.Null);
        return result.Geometry!;
    }

    private static void AssertFailure(string text, ParseErrorKind kind, int offset, string messagePart)
    {
        AssertFailure(text, new WktReadOptions(), kind, offset, messagePart);
    }

    private static void AssertFailure(string text, WktReadOptions options, ParseErrorKind kind, int offset, string messagePart)
    {
        GeometryReadResult result = Parse(text, options);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Geometry, Is.Null, text);
            Assert.That(result.Error, Is.Not.Null, text);
            Assert.That(result.Error!.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Error.Offset, Is.EqualTo(offset), text);
            Assert.That(result.Consumed, Is.EqualTo(offset), text);
            Assert.That(result.Error.Message, Does.Contain(messagePart).IgnoreCase, text);
            Assert.That(result.Error.Message, Does.Contain($"offset {offset}"), text);
        });
    }

    private static CoordinateLayout[] AllLayouts()
    {
        return [CoordinateLayout.XY, CoordinateLayout.XYZ, CoordinateLayout.XYM, CoordinateLayout.XYZM];
    }

    private static GeometryType[] CoreTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection
        ];
    }

    private static string NonEmptyText(GeometryType type, CoordinateLayout layout)
    {
        string suffix = LayoutSuffix(layout);
        string first = CoordinateText(layout, 0, 0);
        string second = CoordinateText(layout, 4, 0);
        string third = CoordinateText(layout, 4, 3);
        string fourth = CoordinateText(layout, 0, 3);
        string ring = $"{first}, {second}, {third}, {fourth}, {first}";
        switch (type)
        {
            case GeometryType.Point:
                return $"POINT{suffix} ({first})";
            case GeometryType.LineString:
                return $"LINESTRING{suffix} ({first}, {third})";
            case GeometryType.Polygon:
                return $"POLYGON{suffix} (({ring}))";
            case GeometryType.MultiPoint:
                return $"MULTIPOINT{suffix} (({first}), ({third}))";
            case GeometryType.MultiLineString:
                return $"MULTILINESTRING{suffix} (({first}, {second}), ({second}, {third}))";
            case GeometryType.MultiPolygon:
                return $"MULTIPOLYGON{suffix} ((({ring})))";
            case GeometryType.GeometryCollection:
                return $"GEOMETRYCOLLECTION{suffix} (POINT{suffix} ({first}), LINESTRING{suffix} ({first}, {third}))";
            default:
                return "";
        }
    }

    private static string TypeName(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.Point:
                return "POINT";
            case GeometryType.LineString:
                return "LINESTRING";
            case GeometryType.Polygon:
                return "POLYGON";
            case GeometryType.MultiPoint:
                return "MULTIPOINT";
            case GeometryType.MultiLineString:
                return "MULTILINESTRING";
            case GeometryType.MultiPolygon:
                return "MULTIPOLYGON";
            case GeometryType.GeometryCollection:
                return "GEOMETRYCOLLECTION";
            default:
                return "";
        }
    }

    private static string LayoutSuffix(CoordinateLayout layout)
    {
        switch (layout)
        {
            case CoordinateLayout.XYZ:
                return " Z";
            case CoordinateLayout.XYM:
                return " M";
            case CoordinateLayout.XYZM:
                return " ZM";
            default:
                return "";
        }
    }

    private static string CoordinateText(CoordinateLayout layout, int x, int y)
    {
        switch (layout)
        {
            case CoordinateLayout.XYZ:
                return $"{x} {y} 5";
            case CoordinateLayout.XYM:
                return $"{x} {y} 7";
            case CoordinateLayout.XYZM:
                return $"{x} {y} 5 7";
            default:
                return $"{x} {y}";
        }
    }

    private static void AssertCoordinateLayout(Coordinate coordinate, CoordinateLayout layout, string message)
    {
        Assert.That(double.IsFinite(coordinate.X), Is.True, message);
        Assert.That(double.IsFinite(coordinate.Y), Is.True, message);
        Assert.That(double.IsFinite(coordinate.Z), Is.EqualTo(layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM), message);
        Assert.That(double.IsFinite(coordinate.M), Is.EqualTo(layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM), message);
    }
}
