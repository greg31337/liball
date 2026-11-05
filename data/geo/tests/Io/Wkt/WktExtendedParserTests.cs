using geo;

namespace geo.Tests;

public class WktExtendedParserTests
{
    [Test]
    public void ParsesEveryExtendedGeometryTypeInEveryCoordinateLayout()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in ExtendedTypes())
            {
                string text = NonEmptyText(type, layout);
                GeometryReadResult result = Parse(text, new WktReadOptions());
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, result.Error?.Message ?? text);
                    Assert.That(result.Error, Is.Null, text);
                    Assert.That(result.Consumed, Is.EqualTo(text.Length), text);
                    Assert.That(result.Geometry, Is.Not.Null, text);
                    Assert.That(result.Geometry!.GeometryType, Is.EqualTo(type), text);
                    Assert.That(result.Geometry.CoordinateLayout, Is.EqualTo(layout), text);
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
    public void ParsesEveryExtendedCanonicalEmptyInEveryLayout()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in ExtendedTypes())
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
    public void BuildsExactSurfacePayloadStructures()
    {
        Geometry triangle = ParseSuccess("TRIANGLE ((0 0, 4 0, 2 3, 0 0))");
        Geometry tin = ParseSuccess("TIN (((0 0, 4 0, 2 3, 0 0)), ((10 10, 14 10, 12 13, 10 10)))");
        Geometry polyhedral = ParseSuccess("POLYHEDRALSURFACE (((0 0, 6 0, 6 6, 0 6, 0 0), (1 1, 1 2, 2 2, 1 1)), ((10 10, 12 10, 12 12, 10 12, 10 10)))");

        Assert.Multiple(() =>
        {
            Assert.That(triangle.Triangle!.Shell.Coordinates, Has.Length.EqualTo(4));
            Assert.That(triangle.Triangle.Shell.Coordinates[2].Y, Is.EqualTo(3.0));
            Assert.That(tin.Tin!.Triangles, Has.Length.EqualTo(2));
            Assert.That(tin.Tin.Triangles[1].Shell.Coordinates[0].X, Is.EqualTo(10.0));
            Assert.That(polyhedral.PolyhedralSurface!.Patches, Has.Length.EqualTo(2));
            Assert.That(polyhedral.PolyhedralSurface.Patches[0].Holes, Has.Length.EqualTo(1));
            Assert.That(polyhedral.PolyhedralSurface.Patches[1].Shell.Coordinates[0].X, Is.EqualTo(10.0));
        });
    }

    [Test]
    public void BuildsExactCurvedPayloadStructures()
    {
        Geometry circular = ParseSuccess("CIRCULARSTRING (0 0, 2 3, 4 0, 6 -3, 8 0)");
        Geometry compound = ParseSuccess("COMPOUNDCURVE ((0 0, 1 0), CIRCULARSTRING (1 0, 2 2, 3 0))");
        Geometry curvePolygon = ParseSuccess("CURVEPOLYGON (CIRCULARSTRING (0 0, 4 4, 0 0), COMPOUNDCURVE ((1 1, 2 1), CIRCULARSTRING (2 1, 2 2, 1 1)))");
        Geometry multiCurve = ParseSuccess("MULTICURVE ((0 0, 1 1), CIRCULARSTRING (2 0, 3 2, 4 0), COMPOUNDCURVE ((5 0, 6 0), CIRCULARSTRING (6 0, 7 2, 8 0)))");
        Geometry multiSurface = ParseSuccess("MULTISURFACE (((0 0, 4 0, 4 4, 0 4, 0 0)), CURVEPOLYGON ((10 10, 14 10, 14 14, 10 14, 10 10)))");

        Assert.Multiple(() =>
        {
            Assert.That(circular.CircularString!.Coordinates, Has.Length.EqualTo(5));
            Assert.That(circular.CircularString.Coordinates[3].Y, Is.EqualTo(-3.0));
            Assert.That(compound.CompoundCurve!.Curves, Has.Length.EqualTo(2));
            Assert.That(compound.CompoundCurve.Curves[0].GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(compound.CompoundCurve.Curves[1].GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(curvePolygon.CurvePolygon!.Shell!.GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(curvePolygon.CurvePolygon.Holes, Has.Length.EqualTo(1));
            Assert.That(curvePolygon.CurvePolygon.Holes[0].GeometryType, Is.EqualTo(GeometryType.CompoundCurve));
            Assert.That(multiCurve.MultiCurve!.Curves, Has.Length.EqualTo(3));
            Assert.That(multiCurve.MultiCurve.Curves[2].GeometryType, Is.EqualTo(GeometryType.CompoundCurve));
            Assert.That(multiSurface.MultiSurface!.Surfaces, Has.Length.EqualTo(2));
            Assert.That(multiSurface.MultiSurface.Surfaces[0].GeometryType, Is.EqualTo(GeometryType.Polygon));
            Assert.That(multiSurface.MultiSurface.Surfaces[1].GeometryType, Is.EqualTo(GeometryType.CurvePolygon));
        });
    }

    [Test]
    public void ExtendedMembersInCollectionsPropagateLayoutAndSridRecursively()
    {
        string text = "SRID=4979;GEOMETRYCOLLECTION Z (TRIANGLE Z ((0 0 1, 4 0 1, 2 3 1, 0 0 1)), CIRCULARSTRING Z (0 0 2, 2 3 2, 4 0 2), MULTICURVE Z ((0 0 3, 1 1 3), CIRCULARSTRING Z (2 0 3, 3 2 3, 4 0 3)))";
        Geometry geometry = ParseSuccess(text, new WktReadOptions { AllowSpatialReferenceId = true });
        Geometry[] children = geometry.GeometryCollection!.Geometries;
        MultiCurve multiCurve = children[2].MultiCurve!;

        Assert.Multiple(() =>
        {
            Assert.That(geometry.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(geometry.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(children[0].GeometryType, Is.EqualTo(GeometryType.Triangle));
            Assert.That(children[1].GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(children[2].GeometryType, Is.EqualTo(GeometryType.MultiCurve));
            Assert.That(children[0].SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(multiCurve.Curves[0].SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(multiCurve.Curves[1].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
        });
    }

    [Test]
    public void RejectsInvalidExtendedStructureAndComponentLayouts()
    {
        string discontinuous = "COMPOUNDCURVE ((0 0, 1 0), CIRCULARSTRING (2 0, 3 2, 4 0))";
        string mismatched = "COMPOUNDCURVE Z ((0 0 1, 1 0 1), CIRCULARSTRING M (1 0 1, 2 2 1, 3 0 1))";
        string invalidSurface = "MULTISURFACE (POINT (1 2))";

        AssertFailure("TRIANGLE ((0 0, 4 0, 4 4, 0 4, 0 0))", ParseErrorKind.InvalidCount, 9, "exactly three vertices");
        AssertFailure("TRIANGLE ((0 0, 4 0, 2 3, 0 0), (1 1, 2 1, 1 2, 1 1))", ParseErrorKind.InvalidCount, 9, "interior rings");
        AssertFailure("TIN (((0 0, 4 0, 4 4, 0 4, 0 0)))", ParseErrorKind.InvalidCount, 5, "exactly three vertices");
        AssertFailure("CIRCULARSTRING (0 0, 1 1)", ParseErrorKind.InvalidCount, 15, "odd number");
        AssertFailure("CIRCULARSTRING (0 0, 1 1, 2 0, 3 1)", ParseErrorKind.InvalidCount, 15, "odd number");
        AssertFailure("COMPOUNDCURVE ()", ParseErrorKind.InvalidCount, 15, "EMPTY keyword");
        AssertFailure(discontinuous, ParseErrorKind.InvalidCoordinate, discontinuous.IndexOf("CIRCULARSTRING"), "continuous");
        AssertFailure("COMPOUNDCURVE (POINT (1 2))", ParseErrorKind.UnexpectedToken, 15, "curve component");
        AssertFailure("CURVEPOLYGON ((0 0, 1 0, 2 0))", ParseErrorKind.InvalidCoordinate, 14, "closed");
        AssertFailure("CURVEPOLYGON ((0 0, 1 0, 1 0, 0 0))", ParseErrorKind.InvalidCoordinate, 14, "distinct");
        AssertFailure(mismatched, ParseErrorKind.InvalidCoordinateLayout, mismatched.IndexOf(" M ") + 1, "does not match");
        AssertFailure(invalidSurface, ParseErrorKind.UnexpectedToken, invalidSurface.IndexOf("POINT"), "surface component");
    }

    [Test]
    public void RejectsDisabledReservedAndUnknownTypesDeterministically()
    {
        WktReadOptions disabled = new WktReadOptions { AllowExtendedTypes = false };
        foreach (GeometryType type in ExtendedTypes())
        {
            string text = TypeName(type) + " EMPTY";
            AssertFailure(text, disabled, ParseErrorKind.UnsupportedType, 0, "disabled");
        }

        AssertFailure("GEOMETRY EMPTY", ParseErrorKind.UnsupportedType, 0, "reserved");
        AssertFailure("CURVE EMPTY", ParseErrorKind.UnsupportedType, 0, "reserved");
        AssertFailure("SURFACE EMPTY", ParseErrorKind.UnsupportedType, 0, "reserved");
        AssertFailure("LINEARRING EMPTY", ParseErrorKind.UnsupportedType, 0, "reserved");
        AssertFailure("BANANA EMPTY", ParseErrorKind.InvalidGeometryType, 0, "Unknown");
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

    private static GeometryType[] ExtendedTypes()
    {
        return
        [
            GeometryType.Triangle,
            GeometryType.Tin,
            GeometryType.PolyhedralSurface,
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface
        ];
    }

    private static string NonEmptyText(GeometryType type, CoordinateLayout layout)
    {
        string suffix = LayoutSuffix(layout);
        string first = CoordinateText(layout, 0, 0);
        string second = CoordinateText(layout, 4, 0);
        string third = CoordinateText(layout, 2, 3);
        string fourth = CoordinateText(layout, 0, 3);
        string ring = $"{first}, {second}, {third}, {first}";
        string polygonRing = $"{first}, {second}, {CoordinateText(layout, 4, 3)}, {fourth}, {first}";
        switch (type)
        {
            case GeometryType.Triangle:
                return $"TRIANGLE{suffix} (({ring}))";
            case GeometryType.Tin:
                return $"TIN{suffix} ((({ring})))";
            case GeometryType.PolyhedralSurface:
                return $"POLYHEDRALSURFACE{suffix} ((({polygonRing})))";
            case GeometryType.CircularString:
                return $"CIRCULARSTRING{suffix} ({first}, {third}, {second})";
            case GeometryType.CompoundCurve:
                return $"COMPOUNDCURVE{suffix} (({first}, {CoordinateText(layout, 1, 0)}), CIRCULARSTRING{suffix} ({CoordinateText(layout, 1, 0)}, {third}, {second}))";
            case GeometryType.CurvePolygon:
                return $"CURVEPOLYGON{suffix} (({polygonRing}))";
            case GeometryType.MultiCurve:
                return $"MULTICURVE{suffix} (({first}, {second}), CIRCULARSTRING{suffix} ({first}, {third}, {second}))";
            case GeometryType.MultiSurface:
                return $"MULTISURFACE{suffix} ((({polygonRing})))";
            default:
                return "";
        }
    }

    private static string TypeName(GeometryType type)
    {
        switch (type)
        {
            case GeometryType.Triangle:
                return "TRIANGLE";
            case GeometryType.Tin:
                return "TIN";
            case GeometryType.PolyhedralSurface:
                return "POLYHEDRALSURFACE";
            case GeometryType.CircularString:
                return "CIRCULARSTRING";
            case GeometryType.CompoundCurve:
                return "COMPOUNDCURVE";
            case GeometryType.CurvePolygon:
                return "CURVEPOLYGON";
            case GeometryType.MultiCurve:
                return "MULTICURVE";
            case GeometryType.MultiSurface:
                return "MULTISURFACE";
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
