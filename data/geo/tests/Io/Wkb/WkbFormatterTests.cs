using geo;
using System.IO;

namespace geo.Tests;

public class WkbFormatterTests
{
    [Test]
    public void WritesKnownPointFixturesForBothByteOrders()
    {
        Geometry point = ParseWkt("POINT (1 2)");
        WkbFormatter big = new WkbFormatter(point, new WkbWriteOptions { ByteOrder = ByteOrder.BigEndian });
        WkbFormatter little = new WkbFormatter(point, new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian });

        Assert.Multiple(() =>
        {
            Assert.That(big.FormatHex(), Is.EqualTo("00000000013FF00000000000004000000000000000"));
            Assert.That(little.FormatHex(), Is.EqualTo("0101000000000000000000F03F0000000000000040"));
            Assert.That(big.FormatBytes(), Is.EqualTo(Convert.FromHexString(big.FormatHex())));
            Assert.That(little.FormatBytes(), Is.EqualTo(Convert.FromHexString(little.FormatHex())));
        });
    }

    [Test]
    public void OgcOutputRoundTripsEveryTypeLayoutAndByteOrder()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            foreach (CoordinateLayout layout in AllLayouts())
            {
                foreach (GeometryType type in SerializableTypes())
                {
                    Geometry source = ParseWkt(NonEmptyText(type, layout));
                    WkbWriteOptions options = new WkbWriteOptions { ByteOrder = byteOrder };
                    WkbFormatter formatter = new WkbFormatter(source, options);
                    byte[] bytes = formatter.FormatBytes();
                    GeometryReadResult result = new WkbParser(bytes, new WkbReadOptions()).ParseDocument();

                    Assert.That(result.Success, Is.True,
                        $"{type} {layout} {byteOrder}: {result.Error?.Message}");
                    Assert.That(result.Geometry, Is.Not.Null);
                    Assert.That(bytes[0], Is.EqualTo((byte)byteOrder));
                    Assert.That(EndianReader.ReadUInt32FromArray(bytes, 1, byteOrder),
                        Is.EqualTo(OgcTypeCode(type, layout)));
                    Assert.That(bytes.Length, Is.EqualTo(WkbSizer.GetConfiguredSize(source, options)));
                    Assert.That(GeometryNormalization.CompareGeometryValues(result.Geometry!, source), Is.EqualTo(0));
                }
            }
        }
    }

    [Test]
    public void WritesAndRoundTripsEveryEmptyTypeAndLayout()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            GeometryFactory factory = new GeometryFactory(new GeometryFactoryOptions { CoordinateLayout = layout });
            foreach (GeometryType type in SerializableTypes())
            {
                Geometry source = factory.CreateEmptyGeometry(type);
                byte[] bytes = new WkbFormatter(source, new WkbWriteOptions()).FormatBytes();
                GeometryReadResult result = new WkbParser(bytes, new WkbReadOptions()).ParseDocument();
                int expectedSize = type == GeometryType.Point ? 5 + OrdinateCount(layout) * 8 : 9;

                Assert.Multiple(() =>
                {
                    Assert.That(bytes.Length, Is.EqualTo(expectedSize), $"{type} {layout}");
                    Assert.That(result.Success, Is.True, result.Error?.Message);
                    Assert.That(result.Geometry!.GeometryType, Is.EqualTo(type));
                    Assert.That(result.Geometry.CoordinateLayout, Is.EqualTo(layout));
                    Assert.That(GeometryInspection.IsEmptyValue(result.Geometry), Is.True);
                });
            }
        }
    }

    [Test]
    public void ArraysSpansStreamsAndHexExposeTheSameOwningEncoding()
    {
        Geometry geometry = ParseWkt("LINESTRING (0 0, 2 3)");
        WkbFormatter formatter = new WkbFormatter(
            geometry, new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian });
        byte[] expected = formatter.FormatBytes();
        byte[] second = formatter.FormatBytes();
        byte[] exact = new byte[expected.Length];
        byte[] small = Enumerable.Repeat((byte)0xaa, expected.Length - 1).ToArray();
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xff);

        expected[0] = 0xff;
        WriteResult success = formatter.TryWriteBytes(exact);
        WriteResult failure = formatter.TryWriteBytes(small);
        formatter.WriteToStream(stream);

        Assert.Multiple(() =>
        {
            Assert.That(second[0], Is.EqualTo((byte)ByteOrder.LittleEndian));
            Assert.That(success.Success, Is.True);
            Assert.That(success.RequiredSize, Is.EqualTo(second.Length));
            Assert.That(success.WrittenSize, Is.EqualTo(second.Length));
            Assert.That(success.Error, Is.Null);
            Assert.That(exact, Is.EqualTo(second));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.RequiredSize, Is.EqualTo(second.Length));
            Assert.That(failure.WrittenSize, Is.EqualTo(0));
            Assert.That(failure.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(small, Is.All.EqualTo(0xaa));
            Assert.That(stream.ToArray().Skip(1), Is.EqualTo(second));
            Assert.That(formatter.FormatHex(), Is.EqualTo(Convert.ToHexString(second)));
        });

        stream.WriteByte(0xee);
        Assert.That(stream.Length, Is.EqualTo(second.Length + 2));
    }

    [Test]
    public void EwkbUsesDimensionalFlagsAndOptionalRootSrid()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            foreach (CoordinateLayout layout in AllLayouts())
            {
                Geometry point = ParseWkt(
                    "SRID=4326;" + NonEmptyText(GeometryType.Point, layout),
                    new WktReadOptions { AllowSpatialReferenceId = true });
                WkbWriteOptions options = new WkbWriteOptions
                {
                    ByteOrder = byteOrder,
                    Flavor = WkbFlavor.Ewkb,
                    IncludeSpatialReferenceId = true
                };
                WkbFormatter formatter = new WkbFormatter(point, options);
                byte[] bytes = formatter.FormatBytes();
                uint expectedType = EwkbTypeCode(GeometryType.Point, layout) | WkbTypeCodes.EwkbSridFlag;

                Assert.Multiple(() =>
                {
                    Assert.That(EndianReader.ReadUInt32FromArray(bytes, 1, byteOrder), Is.EqualTo(expectedType));
                    Assert.That(EndianReader.ReadUInt32FromArray(bytes, 5, byteOrder), Is.EqualTo(4326u));
                    Assert.That(bytes.Length, Is.EqualTo(9 + OrdinateCount(layout) * 8));
                    Assert.That(bytes.Length, Is.EqualTo(WkbSizer.GetConfiguredSize(point, options)));
                });
            }
        }
    }

    [Test]
    public void EwkbWritesSridOnlyOnTheRootGeometry()
    {
        string text = "SRID=4979;GEOMETRYCOLLECTION Z (POINT Z (1 2 3), LINESTRING Z (0 0 0, 1 1 1))";
        Geometry geometry = ParseWkt(text, new WktReadOptions { AllowSpatialReferenceId = true });
        WkbWriteOptions options = new WkbWriteOptions
        {
            ByteOrder = ByteOrder.LittleEndian,
            Flavor = WkbFlavor.Ewkb,
            IncludeSpatialReferenceId = true
        };
        byte[] bytes = new WkbFormatter(geometry, options).FormatBytes();
        uint rootType = EndianReader.ReadUInt32FromArray(bytes, 1, ByteOrder.LittleEndian);
        uint rootSrid = EndianReader.ReadUInt32FromArray(bytes, 5, ByteOrder.LittleEndian);
        uint componentCount = EndianReader.ReadUInt32FromArray(bytes, 9, ByteOrder.LittleEndian);
        int firstChildOffset = 13;
        uint childType = EndianReader.ReadUInt32FromArray(bytes, firstChildOffset + 1, ByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            Assert.That(rootType, Is.EqualTo(
                (uint)GeometryType.GeometryCollection | WkbTypeCodes.EwkbZFlag | WkbTypeCodes.EwkbSridFlag));
            Assert.That(rootSrid, Is.EqualTo(4979u));
            Assert.That(componentCount, Is.EqualTo(2u));
            Assert.That(bytes[firstChildOffset], Is.EqualTo((byte)ByteOrder.LittleEndian));
            Assert.That(childType, Is.EqualTo((uint)GeometryType.Point | WkbTypeCodes.EwkbZFlag));
            Assert.That(childType & WkbTypeCodes.EwkbSridFlag, Is.Zero);
        });
    }

    [Test]
    public void EwkbWithoutSridHasTheStrictPayloadSizeAndNoSridFlag()
    {
        Geometry point = ParseWkt("POINT M (1 2 3)");
        WkbWriteOptions options = new WkbWriteOptions
        {
            ByteOrder = ByteOrder.BigEndian,
            Flavor = WkbFlavor.Ewkb
        };
        byte[] bytes = new WkbFormatter(point, options).FormatBytes();

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Length, Is.EqualTo(29));
            Assert.That(EndianReader.ReadUInt32FromArray(bytes, 1, ByteOrder.BigEndian),
                Is.EqualTo((uint)GeometryType.Point | WkbTypeCodes.EwkbMFlag));
            Assert.That(WkbSizer.GetConfiguredSize(point, options), Is.EqualTo(29));
        });
    }

    [Test]
    public void SnapshotsOptionsAndRejectsInvalidInputs()
    {
        Geometry point = ParseWkt("POINT (1 2)");
        WkbWriteOptions source = new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian };
        WkbFormatter formatter = new WkbFormatter(point, source);
        source.ByteOrder = ByteOrder.BigEndian;
        source.Flavor = WkbFlavor.Ewkb;
        source.IncludeSpatialReferenceId = true;
        Geometry malformedLine = new Geometry
        {
            GeometryType = GeometryType.LineString,
            Point = null,
            LineString = new LineString { Coordinates = [new Coordinate { X = 0.0, Y = 0.0 }] }
        };
        MemoryStream readOnly = new MemoryStream(new byte[1], false);

        Assert.Multiple(() =>
        {
            Assert.That(formatter.FormatBytes()[0], Is.EqualTo((byte)ByteOrder.LittleEndian));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(null!, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(point, null!));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(
                point, new WkbWriteOptions { ByteOrder = (ByteOrder)99 }));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(
                point, new WkbWriteOptions { Flavor = (WkbFlavor)99 }));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(
                point, new WkbWriteOptions { IncludeSpatialReferenceId = true }));
            Assert.Throws<InvalidGeometryException>(() => new WkbFormatter(malformedLine, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => formatter.WriteToStream(null!));
            Assert.Throws<InvalidGeometryException>(() => formatter.WriteToStream(readOnly));
        });
    }

    private static Geometry ParseWkt(string text)
    {
        return ParseWkt(text, new WktReadOptions());
    }

    private static Geometry ParseWkt(string text, WktReadOptions options)
    {
        GeometryReadResult result = new WktParser(text, options).ParseDocument();
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Geometry!;
    }

    private static string NonEmptyText(GeometryType type, CoordinateLayout layout)
    {
        string suffix = LayoutSuffix(layout);
        string first = CoordinateText(layout, 0, 0);
        string second = CoordinateText(layout, 4, 0);
        string third = CoordinateText(layout, 2, 3);
        string fourth = CoordinateText(layout, 0, 3);
        string triangleRing = $"{first}, {second}, {third}, {first}";
        string polygonRing = $"{first}, {second}, {CoordinateText(layout, 4, 3)}, {fourth}, {first}";
        switch (type)
        {
            case GeometryType.Point:
                return $"POINT{suffix} ({first})";
            case GeometryType.LineString:
                return $"LINESTRING{suffix} ({first}, {second})";
            case GeometryType.Polygon:
                return $"POLYGON{suffix} (({polygonRing}))";
            case GeometryType.MultiPoint:
                return $"MULTIPOINT{suffix} (({first}), ({second}))";
            case GeometryType.MultiLineString:
                return $"MULTILINESTRING{suffix} (({first}, {second}))";
            case GeometryType.MultiPolygon:
                return $"MULTIPOLYGON{suffix} ((({polygonRing})))";
            case GeometryType.GeometryCollection:
                return $"GEOMETRYCOLLECTION{suffix} (POINT{suffix} ({first}), LINESTRING{suffix} ({first}, {second}))";
            case GeometryType.Triangle:
                return $"TRIANGLE{suffix} (({triangleRing}))";
            case GeometryType.Tin:
                return $"TIN{suffix} ((({triangleRing})))";
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
            default:
                return $"MULTISURFACE{suffix} ((({polygonRing})), TRIANGLE{suffix} (({triangleRing})))";
        }
    }

    private static string CoordinateText(CoordinateLayout layout, int x, int y)
    {
        if (layout == CoordinateLayout.XY) return $"{x} {y}";
        if (layout == CoordinateLayout.XYZ) return $"{x} {y} 7";
        if (layout == CoordinateLayout.XYM) return $"{x} {y} 8";
        return $"{x} {y} 7 8";
    }

    private static string LayoutSuffix(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XYZ) return " Z";
        if (layout == CoordinateLayout.XYM) return " M";
        if (layout == CoordinateLayout.XYZM) return " ZM";
        return "";
    }

    private static int OrdinateCount(CoordinateLayout layout)
    {
        return layout == CoordinateLayout.XY ? 2 : layout == CoordinateLayout.XYZM ? 4 : 3;
    }

    private static uint OgcTypeCode(GeometryType type, CoordinateLayout layout)
    {
        uint code = (uint)type;
        if (layout == CoordinateLayout.XYZ) code += (uint)WkbTypeCodes.ZOffset;
        else if (layout == CoordinateLayout.XYM) code += (uint)WkbTypeCodes.MOffset;
        else if (layout == CoordinateLayout.XYZM) code += (uint)WkbTypeCodes.ZmOffset;
        return code;
    }

    private static uint EwkbTypeCode(GeometryType type, CoordinateLayout layout)
    {
        uint code = (uint)type;
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            code |= WkbTypeCodes.EwkbZFlag;
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            code |= WkbTypeCodes.EwkbMFlag;
        }
        return code;
    }

    private static ByteOrder[] AllByteOrders()
    {
        return [ByteOrder.BigEndian, ByteOrder.LittleEndian];
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
}
