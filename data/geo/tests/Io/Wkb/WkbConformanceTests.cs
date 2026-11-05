using geo;

namespace geo.Tests;

public class WkbConformanceTests
{
    [Test]
    public void PublicCodecRoundTripsEveryOgcTypeLayoutAndByteOrder()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            IWkbCodec codec = new WkbCodec(
                new WkbReadOptions(), new WkbWriteOptions { ByteOrder = byteOrder });
            foreach (CoordinateLayout layout in AllLayouts())
            {
                foreach (GeometryType type in SerializableTypes())
                {
                    Geometry source = ParseWkt(NonEmptyText(type, layout));
                    byte[] bytes = codec.WriteWkb(source);
                    Geometry fromBytes = codec.ReadWkb(bytes);
                    Geometry fromHex = codec.ReadWkbHex(codec.WriteWkbHex(source));
                    GeometryReadResult tried = codec.TryReadWkb(bytes);

                    Assert.Multiple(() =>
                    {
                        Assert.That(bytes[0], Is.EqualTo((byte)byteOrder), type + " " + layout);
                        Assert.That(ReadUInt32(bytes, 1, byteOrder),
                            Is.EqualTo(OgcTypeCode(type, layout)), type + " " + layout);
                        Assert.That(bytes.Length, Is.EqualTo(codec.GetWkbSize(source)), type + " " + layout);
                        Assert.That(codec.WriteWkbHex(source), Is.EqualTo(Convert.ToHexString(bytes)));
                        Assert.That(tried.Success, Is.True, tried.Error?.Message);
                        Assert.That(tried.Consumed, Is.EqualTo(bytes.Length));
                        Assert.That(GeometryNormalization.CompareGeometryValues(fromBytes, source), Is.Zero);
                        Assert.That(GeometryNormalization.CompareGeometryValues(fromHex, source), Is.Zero);
                    });
                }
            }
        }
    }

    [Test]
    public void PublicCodecRoundTripsEveryEwkbTypeLayoutAndByteOrderWithSrid()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            WkbReadOptions readOptions = new WkbReadOptions { Flavor = WkbFlavor.Ewkb };
            WkbWriteOptions writeOptions = new WkbWriteOptions
            {
                ByteOrder = byteOrder,
                Flavor = WkbFlavor.Ewkb,
                IncludeSpatialReferenceId = true
            };
            IWkbCodec codec = new WkbCodec(readOptions, writeOptions);
            foreach (CoordinateLayout layout in AllLayouts())
            {
                foreach (GeometryType type in SerializableTypes())
                {
                    Geometry source = ParseWkt(
                        "SRID=4326;" + NonEmptyText(type, layout),
                        new WktReadOptions { AllowSpatialReferenceId = true });
                    byte[] bytes = codec.WriteWkb(source);
                    Geometry result = codec.ReadWkb(bytes);
                    uint expectedType = EwkbTypeCode(type, layout) | WkbTypeCodes.EwkbSridFlag;

                    Assert.Multiple(() =>
                    {
                        Assert.That(bytes[0], Is.EqualTo((byte)byteOrder), type + " " + layout);
                        Assert.That(ReadUInt32(bytes, 1, byteOrder), Is.EqualTo(expectedType));
                        Assert.That(ReadUInt32(bytes, 5, byteOrder), Is.EqualTo(4326u));
                        Assert.That(bytes.Length, Is.EqualTo(codec.GetWkbSize(source)));
                        Assert.That(result.GeometryType, Is.EqualTo(type));
                        Assert.That(result.CoordinateLayout, Is.EqualTo(layout));
                        Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
                        Assert.That(GeometryNormalization.CompareGeometryValues(result, source), Is.Zero);
                    });
                }
            }
        }
    }

    [Test]
    public void WritesIndependentExactByteFixtures()
    {
        Geometry line = ParseWkt("LINESTRING (0 0, 2 3)");
        Geometry xyzPoint = ParseWkt("POINT Z (1 2 3)");
        Geometry xymPoint = ParseWkt("POINT M (1 2 3)");
        Geometry emptyPolygon = ParseWkt("POLYGON EMPTY");
        IWkbCodec big = new WkbCodec(
            new WkbReadOptions(), new WkbWriteOptions { ByteOrder = ByteOrder.BigEndian });
        IWkbCodec little = new WkbCodec(
            new WkbReadOptions(), new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian });

        Assert.Multiple(() =>
        {
            Assert.That(big.WriteWkbHex(line), Is.EqualTo(
                "0000000002000000020000000000000000000000000000000040000000000000004008000000000000"));
            Assert.That(big.WriteWkbHex(xyzPoint), Is.EqualTo(
                "00000003E93FF000000000000040000000000000004008000000000000"));
            Assert.That(little.WriteWkbHex(xymPoint), Is.EqualTo(
                "01D1070000000000000000F03F00000000000000400000000000000840"));
            Assert.That(little.WriteWkbHex(emptyPolygon), Is.EqualTo("010300000000000000"));
        });
    }

    [Test]
    public void EveryEmptyTypeUsesItsMinimalEncodingInBothByteOrders()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            IWkbCodec codec = new WkbCodec(
                new WkbReadOptions(), new WkbWriteOptions { ByteOrder = byteOrder });
            foreach (CoordinateLayout layout in AllLayouts())
            {
                GeometryFactory factory = new GeometryFactory(
                    new GeometryFactoryOptions { CoordinateLayout = layout });
                foreach (GeometryType type in SerializableTypes())
                {
                    Geometry source = factory.CreateEmptyGeometry(type);
                    byte[] bytes = codec.WriteWkb(source);
                    Geometry result = codec.ReadWkb(bytes);
                    int expectedSize = type == GeometryType.Point ?
                        5 + OrdinateCount(layout) * 8 : 9;

                    Assert.Multiple(() =>
                    {
                        Assert.That(bytes.Length, Is.EqualTo(expectedSize), type + " " + layout);
                        Assert.That(bytes[0], Is.EqualTo((byte)byteOrder), type + " " + layout);
                        Assert.That(ReadUInt32(bytes, 1, byteOrder),
                            Is.EqualTo(OgcTypeCode(type, layout)), type + " " + layout);
                        Assert.That(result.GeometryType, Is.EqualTo(type));
                        Assert.That(result.CoordinateLayout, Is.EqualTo(layout));
                        Assert.That(GeometryInspection.IsEmptyValue(result), Is.True);
                    });
                }
            }
        }
    }

    [Test]
    public void EveryTruncatedPrefixReportsItsExactEndOffset()
    {
        string text =
            "GEOMETRYCOLLECTION ZM (MULTIPOLYGON ZM (((0 0 1 2, 4 0 1 2, 4 4 1 2, " +
            "0 4 1 2, 0 0 1 2))), COMPOUNDCURVE ZM ((0 0 1 2, 1 0 1 2), " +
            "CIRCULARSTRING ZM (1 0 1 2, 2 2 1 2, 4 0 1 2)))";
        Geometry geometry = ParseWkt(text);

        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            IWkbCodec codec = new WkbCodec(
                new WkbReadOptions(), new WkbWriteOptions { ByteOrder = byteOrder });
            byte[] complete = codec.WriteWkb(geometry);
            int length = 0;
            while (length < complete.Length)
            {
                GeometryReadResult result = codec.TryReadWkb(complete.AsSpan(0, length));
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False, byteOrder + " prefix " + length);
                    Assert.That(result.Geometry, Is.Null, byteOrder + " prefix " + length);
                    Assert.That(result.Error, Is.Not.Null, byteOrder + " prefix " + length);
                    Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd),
                        byteOrder + " prefix " + length);
                    Assert.That(result.Error.Offset, Is.EqualTo(length), byteOrder + " prefix " + length);
                    Assert.That(result.Consumed, Is.EqualTo(length), byteOrder + " prefix " + length);
                });
                length++;
            }
            Assert.That(codec.TryReadWkb(complete).Success, Is.True);
        }
    }

    [Test]
    public void RejectsMaliciousCountsAndDepthBeforeAllocation()
    {
        IWkbCodec codec = new WkbCodec(new WkbReadOptions(), new WkbWriteOptions());
        byte[] unsignedCount = Convert.FromHexString("0102000000FFFFFFFF");
        byte[] enormousCollection = Convert.FromHexString("0107000000FFFFFF7F");
        byte[] missingHundredCoordinates = Convert.FromHexString("010200000064000000");
        GeometryReadResult unsignedResult = codec.TryReadWkb(unsignedCount);
        GeometryReadResult enormousResult = codec.TryReadWkb(enormousCollection);
        GeometryReadResult missingResult = codec.TryReadWkb(missingHundredCoordinates);

        Geometry nested = ParseWkt(
            "GEOMETRYCOLLECTION (GEOMETRYCOLLECTION (GEOMETRYCOLLECTION (POINT (1 2))))");
        byte[] nestedBytes = codec.WriteWkb(nested);
        IWkbCodec shallow = new WkbCodec(
            new WkbReadOptions { MaximumNestingDepth = 1 }, new WkbWriteOptions());
        GeometryReadResult depthResult = shallow.TryReadWkb(nestedBytes);

        Assert.Multiple(() =>
        {
            AssertFailure(unsignedResult, ParseErrorKind.InvalidCount, 5);
            AssertFailure(enormousResult, ParseErrorKind.LimitExceeded, 5);
            AssertFailure(missingResult, ParseErrorKind.UnexpectedEnd, missingHundredCoordinates.Length);
            Assert.That(depthResult.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(depthResult.Error.Message, Does.Contain("nesting depth"));
        });
    }

    [Test]
    public void CodecSnapshotsEveryReadAndWriteOptionBranch()
    {
        WkbReadOptions readOptions = new WkbReadOptions
        {
            Flavor = WkbFlavor.Ewkb,
            MaximumCoordinates = 3,
            MaximumComponents = 1,
            MaximumNestingDepth = 0,
            MaximumInputBytes = 512,
            RequireExactLength = false
        };
        WkbWriteOptions writeOptions = new WkbWriteOptions
        {
            ByteOrder = ByteOrder.LittleEndian,
            Flavor = WkbFlavor.Ewkb,
            IncludeSpatialReferenceId = true
        };
        IWkbCodec codec = new WkbCodec(readOptions, writeOptions);
        readOptions.Flavor = WkbFlavor.Ogc;
        readOptions.MaximumCoordinates = 1;
        readOptions.MaximumComponents = 100;
        readOptions.MaximumNestingDepth = 100;
        readOptions.MaximumInputBytes = 1;
        readOptions.RequireExactLength = true;
        writeOptions.ByteOrder = ByteOrder.BigEndian;
        writeOptions.Flavor = WkbFlavor.Ogc;
        writeOptions.IncludeSpatialReferenceId = false;

        Geometry source = ParseWkt(
            "SRID=4326;CIRCULARSTRING (0 0, 1 2, 3 0)",
            new WktReadOptions { AllowSpatialReferenceId = true });
        byte[] bytes = codec.WriteWkb(source);
        byte[] trailing = new byte[bytes.Length + 2];
        bytes.CopyTo(trailing, 0);
        trailing[bytes.Length] = 0xaa;
        trailing[bytes.Length + 1] = 0xbb;
        Geometry result = codec.ReadWkb(trailing);

        Assert.Multiple(() =>
        {
            Assert.That(bytes[0], Is.EqualTo((byte)ByteOrder.LittleEndian));
            Assert.That(ReadUInt32(bytes, 1, ByteOrder.LittleEndian), Is.EqualTo(
                (uint)GeometryType.CircularString | WkbTypeCodes.EwkbSridFlag));
            Assert.That(ReadUInt32(bytes, 5, ByteOrder.LittleEndian), Is.EqualTo(4326u));
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.CircularString));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4326));
        });
    }

    private static void AssertFailure(GeometryReadResult result, ParseErrorKind kind, int offset)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Geometry, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Kind, Is.EqualTo(kind));
        Assert.That(result.Error.Offset, Is.EqualTo(offset));
        Assert.That(result.Consumed, Is.EqualTo(offset));
    }

    private static Geometry ParseWkt(string text)
    {
        return ParseWkt(text, new WktReadOptions());
    }

    private static Geometry ParseWkt(string text, WktReadOptions options)
    {
        IWktCodec codec = new WktCodec(options, new WktWriteOptions());
        return codec.ReadWkt(text);
    }

    private static uint ReadUInt32(byte[] bytes, int offset, ByteOrder byteOrder)
    {
        return EndianReader.ReadUInt32FromArray(bytes, offset, byteOrder);
    }

    private static uint OgcTypeCode(GeometryType type, CoordinateLayout layout)
    {
        uint code = (uint)type;
        if (layout == CoordinateLayout.XYZ) return code + (uint)WkbTypeCodes.ZOffset;
        if (layout == CoordinateLayout.XYM) return code + (uint)WkbTypeCodes.MOffset;
        if (layout == CoordinateLayout.XYZM) return code + (uint)WkbTypeCodes.ZmOffset;
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

    private static int OrdinateCount(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XY) return 2;
        if (layout == CoordinateLayout.XYZM) return 4;
        return 3;
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

    private static string LayoutSuffix(CoordinateLayout layout)
    {
        if (layout == CoordinateLayout.XYZ) return " Z";
        if (layout == CoordinateLayout.XYM) return " M";
        if (layout == CoordinateLayout.XYZM) return " ZM";
        return "";
    }

    private static string CoordinateText(CoordinateLayout layout, int x, int y)
    {
        if (layout == CoordinateLayout.XYZ) return x + " " + y + " 5";
        if (layout == CoordinateLayout.XYM) return x + " " + y + " 7";
        if (layout == CoordinateLayout.XYZM) return x + " " + y + " 5 7";
        return x + " " + y;
    }
}
