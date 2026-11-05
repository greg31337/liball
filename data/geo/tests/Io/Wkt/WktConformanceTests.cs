using geo;

namespace geo.Tests;

public class WktConformanceTests
{
    [Test]
    public void PublicCodecCanonicallyRoundTripsEveryTypeInEveryLayout()
    {
        IWktCodec codec = CreateCodec();

        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in SerializableTypes())
            {
                string source = NonEmptyText(type, layout);
                Geometry first = codec.ReadWkt(source);
                string canonical = codec.WriteWkt(first);
                Geometry second = codec.ReadWkt(canonical);

                Assert.Multiple(() =>
                {
                    Assert.That(first.GeometryType, Is.EqualTo(type), source);
                    Assert.That(first.CoordinateLayout, Is.EqualTo(layout), source);
                    Assert.That(second.GeometryType, Is.EqualTo(type), canonical);
                    Assert.That(second.CoordinateLayout, Is.EqualTo(layout), canonical);
                    Assert.That(GeometryNormalization.CompareGeometryValues(second, first), Is.Zero, canonical);
                    Assert.That(codec.WriteWkt(second), Is.EqualTo(canonical), canonical);
                });
            }
        }
    }

    [Test]
    public void MixedCaseAndWhitespaceAtEveryTokenBoundaryProduceCanonicalText()
    {
        IWktCodec codec = CreateCodec();
        string source =
            "\t gEoMeTrYcOlLeCtIoN \r\n zM \n( pOiNt\tZm ( +1\t.2\r3.0\n4e0 ) , " +
            "\r\n lInEsTrInG Zm (0 0 0 0 ,\t1 1 1 1 ) ) \r\n";

        Geometry geometry = codec.ReadWkt(source);
        string canonical = codec.WriteWkt(geometry);
        GeometryReadResult result = codec.TryReadWkt(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Error?.Message);
            Assert.That(result.Consumed, Is.EqualTo(source.Length));
            Assert.That(geometry.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(canonical, Is.EqualTo(
                "GEOMETRYCOLLECTION ZM (POINT ZM (1 0.2 3 4), " +
                "LINESTRING ZM (0 0 0 0, 1 1 1 1))"));
        });
    }

    [Test]
    public void CanonicalizesEmptyRootsAndEmptyCompositeMembers()
    {
        IWktCodec codec = CreateCodec();
        string[] inputs =
        [
            "multipoint z(empty,(1 2 3),empty)",
            "multilinestring m(empty,(0 0 5,1 1 6))",
            "multipolygon zm(empty,((0 0 1 2,4 0 1 2,4 4 1 2,0 4 1 2,0 0 1 2)))",
            "geometrycollection(point empty,linestring empty,polygon empty)",
            "tin(empty,((0 0,4 0,2 3,0 0)))",
            "polyhedralsurface(empty,((0 0,4 0,4 4,0 4,0 0)))",
            "multicurve(linestring empty,circularstring empty,(0 0,1 1))",
            "multisurface(polygon empty,curvepolygon empty,triangle empty)"
        ];
        string[] expected =
        [
            "MULTIPOINT Z (EMPTY, (1 2 3), EMPTY)",
            "MULTILINESTRING M (EMPTY, (0 0 5, 1 1 6))",
            "MULTIPOLYGON ZM (EMPTY, ((0 0 1 2, 4 0 1 2, 4 4 1 2, 0 4 1 2, 0 0 1 2)))",
            "GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING EMPTY, POLYGON EMPTY)",
            "TIN (EMPTY, ((0 0, 4 0, 2 3, 0 0)))",
            "POLYHEDRALSURFACE (EMPTY, ((0 0, 4 0, 4 4, 0 4, 0 0)))",
            "MULTICURVE (LINESTRING EMPTY, CIRCULARSTRING EMPTY, (0 0, 1 1))",
            "MULTISURFACE (POLYGON EMPTY, CURVEPOLYGON EMPTY, TRIANGLE EMPTY)"
        ];

        int index = 0;
        while (index < inputs.Length)
        {
            Geometry geometry = codec.ReadWkt(inputs[index]);
            string canonical = codec.WriteWkt(geometry);
            Geometry roundTrip = codec.ReadWkt(canonical);

            Assert.Multiple(() =>
            {
                Assert.That(canonical, Is.EqualTo(expected[index]), inputs[index]);
                Assert.That(GeometryNormalization.CompareGeometryValues(roundTrip, geometry), Is.Zero, inputs[index]);
            });
            index++;
        }

        foreach (CoordinateLayout layout in AllLayouts())
        {
            foreach (GeometryType type in SerializableTypes())
            {
                string empty = TypeName(type) + LayoutSuffix(layout) + " EMPTY";
                Assert.That(codec.WriteWkt(codec.ReadWkt(empty)), Is.EqualTo(empty), empty);
            }
        }
    }

    [Test]
    public void FiniteDoubleBoundariesRoundTripExactlyAndOverflowReportsItsStart()
    {
        IWktCodec codec = CreateCodec();
        string source =
            "POINT ZM (-1.7976931348623157E+308 1.7976931348623157E+308 5E-324 -5E-324)";

        Geometry geometry = codec.ReadWkt(source);
        string canonical = codec.WriteWkt(geometry);
        Geometry roundTrip = codec.ReadWkt(canonical);
        Coordinate coordinate = roundTrip.Point!.Coordinate!;
        string overflowText = "POINT (1.7976931348623159E+308 0)";
        GeometryReadResult overflow = codec.TryReadWkt(overflowText);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.X),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(-double.MaxValue)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.Y),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(double.MaxValue)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.Z),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(double.Epsilon)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.M),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(-double.Epsilon)));
            Assert.That(overflow.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidNumber));
            Assert.That(overflow.Error.Offset, Is.EqualTo(overflowText.IndexOf('1')));
            Assert.That(overflow.Consumed, Is.EqualTo(overflowText.IndexOf('1')));
        });
    }

    [Test]
    public void MalformedGrammarReportsTheFirstOffendingCharacter()
    {
        IWktCodec codec = CreateCodec();
        string missingPolygonClosure = "POLYGON ((0 0, 1 0, 1 1, 0 0)";
        string missingCollectionMember = "GEOMETRYCOLLECTION (POINT (1 2),)";

        AssertFailure(codec, "POINT (1, 2)", ParseErrorKind.InvalidCoordinate, 8);
        AssertFailure(codec, "POINT Z (1 2)", ParseErrorKind.InvalidCoordinate, 12);
        AssertFailure(codec, "POINT (1 2 3)", ParseErrorKind.InvalidCoordinateLayout, 11);
        AssertFailure(codec, "LINESTRING (0 0, 1)", ParseErrorKind.InvalidCoordinate, 18);
        AssertFailure(codec, missingPolygonClosure, ParseErrorKind.UnexpectedEnd, missingPolygonClosure.Length);
        AssertFailure(codec, missingCollectionMember, ParseErrorKind.InvalidGeometryType,
            missingCollectionMember.Length - 1);
        AssertFailure(codec, "POINT (1 2) POINT (3 4)", ParseErrorKind.TrailingContent, 12);
        AssertFailure(codec, "POINT (1 2]", ParseErrorKind.InvalidNumber, 10);
        AssertFailure(codec, "POINT Q (1 2)", ParseErrorKind.UnexpectedToken, 6);

        IWktCodec ewkt = new WktCodec(
            new WktReadOptions { AllowSpatialReferenceId = true }, new WktWriteOptions());
        AssertFailure(ewkt, "SRID=;POINT EMPTY", ParseErrorKind.UnexpectedToken, 5);
        AssertFailure(ewkt, "SRID=2147483648;POINT EMPTY", ParseErrorKind.InvalidNumber, 5);
    }

    [Test]
    public void CodecSnapshotsEveryReadAndWriteOptionBranch()
    {
        WktReadOptions readOptions = new WktReadOptions
        {
            MaximumCoordinates = 3,
            MaximumComponents = 1,
            MaximumNestingDepth = 1,
            AllowExtendedTypes = true,
            AllowSpatialReferenceId = true
        };
        WktWriteOptions writeOptions = new WktWriteOptions
        {
            DecimalPrecision = 3,
            TrimTrailingZeros = false,
            IncludeSpatialReferenceId = true,
            AllowExtendedTypes = true
        };
        IWktCodec codec = new WktCodec(readOptions, writeOptions);
        readOptions.MaximumCoordinates = 1;
        readOptions.MaximumComponents = 100;
        readOptions.MaximumNestingDepth = 0;
        readOptions.AllowExtendedTypes = false;
        readOptions.AllowSpatialReferenceId = false;
        writeOptions.DecimalPrecision = 0;
        writeOptions.TrimTrailingZeros = true;
        writeOptions.IncludeSpatialReferenceId = false;
        writeOptions.AllowExtendedTypes = false;

        Geometry geometry = codec.ReadWkt(
            "SRID=4326;CIRCULARSTRING (0 0, 1.25 2.5, 3 0)");
        string output = codec.WriteWkt(geometry);

        Assert.Multiple(() =>
        {
            Assert.That(geometry.GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(geometry.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(output, Is.EqualTo(
                "SRID=4326;CIRCULARSTRING (0.000 0.000, 1.250 2.500, 3.000 0.000)"));
        });
    }

    private static void AssertFailure(
        IWktCodec codec, string text, ParseErrorKind kind, int offset)
    {
        GeometryReadResult result = codec.TryReadWkt(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Geometry, Is.Null, text);
            Assert.That(result.Error, Is.Not.Null, text);
            Assert.That(result.Error!.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Error.Offset, Is.EqualTo(offset), text);
            Assert.That(result.Consumed, Is.EqualTo(offset), text);
            Assert.That(result.Error.Message, Does.Contain("offset " + offset), text);
        });
    }

    private static IWktCodec CreateCodec()
    {
        return new WktCodec(new WktReadOptions(), new WktWriteOptions());
    }

    private static CoordinateLayout[] AllLayouts()
    {
        return [CoordinateLayout.XY, CoordinateLayout.XYZ, CoordinateLayout.XYM, CoordinateLayout.XYZM];
    }

    private static GeometryType[] SerializableTypes()
    {
        return
        [
            GeometryType.Point,
            GeometryType.LineString,
            GeometryType.Polygon,
            GeometryType.MultiPoint,
            GeometryType.MultiLineString,
            GeometryType.MultiPolygon,
            GeometryType.GeometryCollection,
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
        string third = CoordinateText(layout, 4, 3);
        string fourth = CoordinateText(layout, 0, 3);
        string middle = CoordinateText(layout, 2, 3);
        string join = CoordinateText(layout, 1, 0);
        string ring = first + ", " + second + ", " + third + ", " + fourth + ", " + first;
        string triangle = first + ", " + second + ", " + middle + ", " + first;
        switch (type)
        {
            case GeometryType.Point:
                return "POINT" + suffix + " (" + first + ")";
            case GeometryType.LineString:
                return "LINESTRING" + suffix + " (" + first + ", " + third + ")";
            case GeometryType.Polygon:
                return "POLYGON" + suffix + " ((" + ring + "))";
            case GeometryType.MultiPoint:
                return "MULTIPOINT" + suffix + " ((" + first + "), (" + third + "))";
            case GeometryType.MultiLineString:
                return "MULTILINESTRING" + suffix + " ((" + first + ", " + second + "), (" + second + ", " + third + "))";
            case GeometryType.MultiPolygon:
                return "MULTIPOLYGON" + suffix + " (((" + ring + ")))";
            case GeometryType.GeometryCollection:
                return "GEOMETRYCOLLECTION" + suffix + " (POINT" + suffix + " (" + first + "), LINESTRING" + suffix + " (" + first + ", " + third + "))";
            case GeometryType.Triangle:
                return "TRIANGLE" + suffix + " ((" + triangle + "))";
            case GeometryType.Tin:
                return "TIN" + suffix + " (((" + triangle + ")))";
            case GeometryType.PolyhedralSurface:
                return "POLYHEDRALSURFACE" + suffix + " (((" + ring + ")))";
            case GeometryType.CircularString:
                return "CIRCULARSTRING" + suffix + " (" + first + ", " + middle + ", " + second + ")";
            case GeometryType.CompoundCurve:
                return "COMPOUNDCURVE" + suffix + " ((" + first + ", " + join + "), CIRCULARSTRING" + suffix + " (" + join + ", " + middle + ", " + second + "))";
            case GeometryType.CurvePolygon:
                return "CURVEPOLYGON" + suffix + " ((" + ring + "))";
            case GeometryType.MultiCurve:
                return "MULTICURVE" + suffix + " ((" + first + ", " + second + "), CIRCULARSTRING" + suffix + " (" + first + ", " + middle + ", " + second + "))";
            case GeometryType.MultiSurface:
                return "MULTISURFACE" + suffix + " (((" + ring + ")))";
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
                return x + " " + y + " 5";
            case CoordinateLayout.XYM:
                return x + " " + y + " 7";
            case CoordinateLayout.XYZM:
                return x + " " + y + " 5 7";
            default:
                return x + " " + y;
        }
    }
}
