using System.IO;
using geo;

namespace geo.Tests;

public class WktFormatterTests
{
    [TestCase("point zm(1 2 3 4)", "POINT ZM (1 2 3 4)")]
    [TestCase("LINESTRING(0 0,2 3)", "LINESTRING (0 0, 2 3)")]
    [TestCase("POLYGON((0 0,4 0,4 4,0 4,0 0),(1 1,1 2,2 2,1 1))", "POLYGON ((0 0, 4 0, 4 4, 0 4, 0 0), (1 1, 1 2, 2 2, 1 1))")]
    [TestCase("MULTIPOINT(EMPTY,(1 2))", "MULTIPOINT (EMPTY, (1 2))")]
    [TestCase("MULTILINESTRING(EMPTY,(0 0,1 1))", "MULTILINESTRING (EMPTY, (0 0, 1 1))")]
    [TestCase("MULTIPOLYGON(EMPTY,((0 0,2 0,2 2,0 2,0 0)))", "MULTIPOLYGON (EMPTY, ((0 0, 2 0, 2 2, 0 2, 0 0)))")]
    [TestCase("GEOMETRYCOLLECTION(POINT EMPTY,LINESTRING(0 0,1 1))", "GEOMETRYCOLLECTION (POINT EMPTY, LINESTRING (0 0, 1 1))")]
    [TestCase("TRIANGLE((0 0,4 0,2 3,0 0))", "TRIANGLE ((0 0, 4 0, 2 3, 0 0))")]
    [TestCase("TIN(EMPTY,((0 0,4 0,2 3,0 0)))", "TIN (EMPTY, ((0 0, 4 0, 2 3, 0 0)))")]
    [TestCase("POLYHEDRALSURFACE(EMPTY,((0 0,4 0,4 4,0 4,0 0)))", "POLYHEDRALSURFACE (EMPTY, ((0 0, 4 0, 4 4, 0 4, 0 0)))")]
    [TestCase("CIRCULARSTRING(0 0,2 3,4 0)", "CIRCULARSTRING (0 0, 2 3, 4 0)")]
    [TestCase("COMPOUNDCURVE((0 0,1 0),CIRCULARSTRING(1 0,2 2,3 0))", "COMPOUNDCURVE ((0 0, 1 0), CIRCULARSTRING (1 0, 2 2, 3 0))")]
    [TestCase("CURVEPOLYGON((0 0,4 0,4 4,0 4,0 0))", "CURVEPOLYGON ((0 0, 4 0, 4 4, 0 4, 0 0))")]
    [TestCase("MULTICURVE(LINESTRING EMPTY,CIRCULARSTRING EMPTY,(0 0,1 1))", "MULTICURVE (LINESTRING EMPTY, CIRCULARSTRING EMPTY, (0 0, 1 1))")]
    [TestCase("MULTISURFACE(POLYGON EMPTY,CURVEPOLYGON EMPTY,TRIANGLE EMPTY)", "MULTISURFACE (POLYGON EMPTY, CURVEPOLYGON EMPTY, TRIANGLE EMPTY)")]
    public void ProducesCanonicalSpacingCasingAndComponentForms(string input, string expected)
    {
        Geometry geometry = ParseSuccess(input, new WktReadOptions());
        string text = new WktFormatter(geometry, new WktWriteOptions()).FormatString();
        Geometry roundTrip = ParseSuccess(text, new WktReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(expected));
            Assert.That(GeometryNormalization.CompareGeometryValues(roundTrip, geometry), Is.EqualTo(0));
            Assert.That(roundTrip.CoordinateLayout, Is.EqualTo(geometry.CoordinateLayout));
            Assert.That(roundTrip.SpatialReferenceId, Is.EqualTo(geometry.SpatialReferenceId));
        });
    }

    [Test]
    public void FormatsEverySerializableEmptyTypeInEveryLayout()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = layout });
            foreach (GeometryType type in SerializableTypes())
            {
                Geometry geometry = factory.CreateEmptyGeometry(type);
                string expected = WktFormatter.GetTypeName(type) + LayoutSuffix(layout) + " EMPTY";
                string text = new WktFormatter(geometry, new WktWriteOptions()).FormatString();
                Geometry parsed = ParseSuccess(text, new WktReadOptions());
                Assert.Multiple(() =>
                {
                    Assert.That(text, Is.EqualTo(expected), type.ToString());
                    Assert.That(parsed.GeometryType, Is.EqualTo(type), type.ToString());
                    Assert.That(parsed.CoordinateLayout, Is.EqualTo(layout), type.ToString());
                    Assert.That(GeometryInspection.IsEmptyValue(parsed), Is.True, type.ToString());
                });
            }
        }
    }

    [Test]
    public void DefaultNumbersRoundTripFiniteDoubleBitsAndCanonicalizeNegativeZero()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = CoordinateLayout.XYZM });
        Geometry point = factory.CreatePoint(new Coordinate
        {
            X = double.MaxValue,
            Y = double.Epsilon,
            Z = 0.10000000000000002,
            M = -0.0
        });

        string text = new WktFormatter(point, new WktWriteOptions()).FormatString();
        Geometry parsed = ParseSuccess(text, new WktReadOptions());
        Coordinate coordinate = parsed.Point!.Coordinate!;

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("POINT ZM (1.7976931348623157E+308 5E-324 0.10000000000000002 0)"));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.X), Is.EqualTo(BitConverter.DoubleToInt64Bits(double.MaxValue)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.Y), Is.EqualTo(BitConverter.DoubleToInt64Bits(double.Epsilon)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.Z), Is.EqualTo(BitConverter.DoubleToInt64Bits(0.10000000000000002)));
            Assert.That(BitConverter.DoubleToInt64Bits(coordinate.M), Is.EqualTo(BitConverter.DoubleToInt64Bits(0.0)));
        });
    }

    [Test]
    public void DecimalPrecisionControlsRoundingTrimmingAndPadding()
    {
        GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions());
        Geometry point = factory.CreatePoint(new Coordinate { X = 1.23456, Y = 2.0 });
        WktWriteOptions trimmed = new WktWriteOptions { DecimalPrecision = 3 };
        WktWriteOptions padded = new WktWriteOptions { DecimalPrecision = 3, TrimTrailingZeros = false };
        WktWriteOptions integer = new WktWriteOptions { DecimalPrecision = 0 };

        Assert.Multiple(() =>
        {
            Assert.That(new WktFormatter(point, trimmed).FormatString(), Is.EqualTo("POINT (1.235 2)"));
            Assert.That(new WktFormatter(point, padded).FormatString(), Is.EqualTo("POINT (1.235 2.000)"));
            Assert.That(new WktFormatter(point, integer).FormatString(), Is.EqualTo("POINT (1 2)"));
        });
    }

    [Test]
    public void EmitsOneEwktSridPrefixAndRepeatedNestedLayoutTokens()
    {
        string source = "SRID=4979;GEOMETRYCOLLECTION Z (POINT Z (1 2 3), CIRCULARSTRING Z (0 0 4, 1 2 4, 2 0 4))";
        Geometry geometry = ParseSuccess(source, new WktReadOptions { AllowSpatialReferenceId = true });
        WktWriteOptions options = new WktWriteOptions { IncludeSpatialReferenceId = true };

        string text = new WktFormatter(geometry, options).FormatString();
        Geometry parsed = ParseSuccess(text, new WktReadOptions { AllowSpatialReferenceId = true });

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("SRID=4979;GEOMETRYCOLLECTION Z (POINT Z (1 2 3), CIRCULARSTRING Z (0 0 4, 1 2 4, 2 0 4))"));
            Assert.That(parsed.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(parsed.GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(parsed.GeometryCollection.Geometries[1].CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
        });
    }

    [Test]
    public void CharacterSpanAndTextWriterOutputsMatchTheOwningString()
    {
        Geometry geometry = ParseSuccess("LINESTRING (0 0, 2 3)", new WktReadOptions());
        WktFormatter formatter = new WktFormatter(geometry, new WktWriteOptions());
        string expected = formatter.FormatString();
        char[] exact = new char[expected.Length];
        char[] small = new string('?', expected.Length - 1).ToCharArray();

        WriteResult success = formatter.TryWriteCharacters(exact);
        WriteResult failure = formatter.TryWriteCharacters(small);
        StringWriter writer = new StringWriter();
        formatter.WriteToTextWriter(writer);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(success.WrittenSize, Is.EqualTo(expected.Length));
            Assert.That(success.Error, Is.Null);
            Assert.That(new string(exact), Is.EqualTo(expected));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(failure.WrittenSize, Is.EqualTo(0));
            Assert.That(failure.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(new string(small), Is.EqualTo(new string('?', expected.Length - 1)));
            Assert.That(writer.ToString(), Is.EqualTo(expected));
            Assert.Throws<InvalidGeometryException>(() => formatter.WriteToTextWriter(null!));
        });
    }

    [Test]
    public void SnapshotsOptionsAndRejectsInvalidGeometryOrFormattingConfiguration()
    {
        Geometry point = ParseSuccess("POINT (1.25 2.5)", new WktReadOptions());
        WktWriteOptions source = new WktWriteOptions { DecimalPrecision = 2, IncludeSpatialReferenceId = true };
        WktFormatter formatter = new WktFormatter(point, source);
        source.DecimalPrecision = 0;
        source.IncludeSpatialReferenceId = false;

        Geometry malformedLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }] }
        };
        Geometry evenCircular = new Geometry
        {
            GeometryType = GeometryType.CircularString,
            Point = null,
            CircularString = new CircularString
            {
                Coordinates =
                [
                    new Coordinate { X = 0.0, Y = 0.0 },
                    new Coordinate { X = 1.0, Y = 1.0 },
                    new Coordinate { X = 2.0, Y = 0.0 },
                    new Coordinate { X = 3.0, Y = 1.0 }
                ]
            }
        };
        Geometry extended = ParseSuccess("GEOMETRYCOLLECTION (CIRCULARSTRING (0 0, 1 1, 2 0))", new WktReadOptions());
        Geometry linearRing = new Geometry
        {
            GeometryType = GeometryType.LinearRing,
            Point = null,
            LinearRing = new LinearRing()
        };

        Assert.Multiple(() =>
        {
            Assert.That(formatter.FormatString(), Is.EqualTo("SRID=0;POINT (1.25 2.5)"));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(null!, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(point, null!));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(point, new WktWriteOptions { DecimalPrecision = -1 }));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(point, new WktWriteOptions { DecimalPrecision = 18 }));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(malformedLine, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(evenCircular, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(extended, new WktWriteOptions { AllowExtendedTypes = false }));
            Assert.Throws<InvalidGeometryException>(() => new WktFormatter(linearRing, new WktWriteOptions()));
        });
    }

    private static Geometry ParseSuccess(string text, WktReadOptions options)
    {
        GeometryReadResult result = new WktParser(text, options).ParseDocument();
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Geometry!;
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
            GeometryType.CircularString,
            GeometryType.CompoundCurve,
            GeometryType.CurvePolygon,
            GeometryType.MultiCurve,
            GeometryType.MultiSurface,
            GeometryType.PolyhedralSurface,
            GeometryType.Tin,
            GeometryType.Triangle
        ];
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
}
