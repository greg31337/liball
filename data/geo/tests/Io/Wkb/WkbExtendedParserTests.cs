using geo;
using System.Buffers.Binary;

namespace geo.Tests;

public class WkbExtendedParserTests
{
    [Test]
    public void ParsesAndSizesEveryExtendedTypeLayoutAndByteOrder()
    {
        foreach (ByteOrder byteOrder in ExtendedWkbFixtures.AllByteOrders())
        {
            foreach (CoordinateLayout layout in ExtendedWkbFixtures.AllLayouts())
            {
                foreach (GeometryType type in ExtendedWkbFixtures.ExtendedTypes())
                {
                    byte[] bytes = ExtendedWkbFixtures.CreateExtended(type, layout, byteOrder);
                    GeometryReadResult result = Parse(bytes);

                    Assert.That(result.Success, Is.True, $"{type} {layout} {byteOrder}: {result.Error?.Message}");
                    Assert.That(result.Consumed, Is.EqualTo(bytes.Length));
                    Assert.That(result.Geometry, Is.Not.Null);
                    Geometry geometry = result.Geometry!;
                    Assert.That(geometry.GeometryType, Is.EqualTo(type));
                    Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout));
                    AssertExtendedPayload(geometry);
                    Assert.That(WkbSizer.GetSize(geometry), Is.EqualTo(bytes.Length), $"{type} {layout} {byteOrder}");
                }
            }
        }
    }

    [Test]
    public void ParsesAndSizesEveryExtendedEmptyValue()
    {
        foreach (CoordinateLayout layout in ExtendedWkbFixtures.AllLayouts())
        {
            foreach (GeometryType type in ExtendedWkbFixtures.ExtendedTypes())
            {
                byte[] bytes = ExtendedWkbFixtures.CreateEmptyExtended(type, layout, ByteOrder.LittleEndian);
                Geometry geometry = ParseSuccess(bytes);

                Assert.Multiple(() =>
                {
                    Assert.That(geometry.GeometryType, Is.EqualTo(type));
                    Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout));
                    Assert.That(GeometryInspection.IsEmptyValue(geometry), Is.True);
                    Assert.That(WkbSizer.GetSize(geometry), Is.EqualTo(9));
                });
            }
        }
    }

    [Test]
    public void RejectsInvalidTriangleCircularAndAbstractTypes()
    {
        byte[] triangleWithFiveCoordinates = ExtendedWkbFixtures.CreateTriangleWithRingCount(5);
        byte[] triangleWithHole = ExtendedWkbFixtures.CreateTriangleWithTwoRings();
        byte[] evenCircular = ExtendedWkbFixtures.CreateCircularWithCount(4);
        byte[] shortCircular = ExtendedWkbFixtures.CreateCircularWithCount(2);
        byte[] abstractCurve = ExtendedWkbFixtures.Header(13u, ByteOrder.BigEndian);
        byte[] abstractSurface = ExtendedWkbFixtures.Header(3014u, ByteOrder.LittleEndian);

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(triangleWithFiveCoordinates), ParseErrorKind.InvalidCount, 5, "exactly three");
            AssertFailure(Parse(triangleWithHole), ParseErrorKind.InvalidCount, 5, "interior rings");
            AssertFailure(Parse(evenCircular), ParseErrorKind.InvalidCount, 5, "odd number");
            AssertFailure(Parse(shortCircular), ParseErrorKind.InvalidCount, 5, "odd number");
            AssertFailure(Parse(abstractCurve), ParseErrorKind.UnsupportedType, 1, "abstract");
            AssertFailure(Parse(abstractSurface), ParseErrorKind.UnsupportedType, 1, "abstract");
        });
    }

    [Test]
    public void RejectsInvalidTaggedComponentTypesAndLayoutsBeforePayloads()
    {
        byte[] polygon = ExtendedWkbFixtures.CreatePolygon(
            CoordinateLayout.XY, ByteOrder.LittleEndian, false);
        byte[] triangle = ExtendedWkbFixtures.CreateTriangle(
            CoordinateLayout.XY, ByteOrder.LittleEndian, false);
        byte[] point = ExtendedWkbFixtures.CreatePoint(
            CoordinateLayout.XY, ByteOrder.LittleEndian);
        byte[] lineZ = ExtendedWkbFixtures.CreateLine(
            CoordinateLayout.XYZ, ByteOrder.LittleEndian, 0.0, 0.0, 1.0, 0.0);

        byte[] badTin = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.Tin, CoordinateLayout.XY, ByteOrder.BigEndian, polygon);
        byte[] badPolyhedral = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.PolyhedralSurface, CoordinateLayout.XY, ByteOrder.BigEndian, triangle);
        byte[] badMultiCurve = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.MultiCurve, CoordinateLayout.XY, ByteOrder.BigEndian, point);
        byte[] badMultiSurface = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.MultiSurface, CoordinateLayout.XY, ByteOrder.BigEndian, point);
        byte[] badCompound = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.CompoundCurve, CoordinateLayout.XY, ByteOrder.BigEndian,
            ExtendedWkbFixtures.CreateExtended(GeometryType.CompoundCurve,
                CoordinateLayout.XY, ByteOrder.LittleEndian));
        byte[] mismatchedLayout = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.MultiCurve, CoordinateLayout.XY, ByteOrder.BigEndian, lineZ);

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(badTin), ParseErrorKind.InvalidGeometryType, 9, "Triangle");
            AssertFailure(Parse(badPolyhedral), ParseErrorKind.InvalidGeometryType, 9, "Polygon");
            AssertFailure(Parse(badMultiCurve), ParseErrorKind.InvalidGeometryType, 9, "Curve");
            AssertFailure(Parse(badMultiSurface), ParseErrorKind.InvalidGeometryType, 9, "Surface");
            AssertFailure(Parse(badCompound), ParseErrorKind.InvalidGeometryType, 9, "CompoundCurvePart");
            AssertFailure(Parse(mismatchedLayout), ParseErrorKind.InvalidCoordinateLayout, 10, "does not match");
        });
    }

    [Test]
    public void RejectsEmptyDiscontinuousAndOpenCurveComponents()
    {
        byte[] emptyLine = ExtendedWkbFixtures.CreateLine(
            CoordinateLayout.XY, ByteOrder.LittleEndian, 0.0, 0.0, 0.0, 0.0, true);
        byte[] emptyCompound = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.CompoundCurve, CoordinateLayout.XY, ByteOrder.BigEndian, emptyLine);
        byte[] discontinuous = ExtendedWkbFixtures.CreateDiscontinuousCompoundCurve();
        byte[] openLine = ExtendedWkbFixtures.CreateLine(
            CoordinateLayout.XY, ByteOrder.LittleEndian, 0.0, 0.0, 1.0, 0.0);
        byte[] openCurvePolygon = ExtendedWkbFixtures.CreateCollectionWithChild(
            GeometryType.CurvePolygon, CoordinateLayout.XY, ByteOrder.BigEndian, openLine);

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(emptyCompound), ParseErrorKind.InvalidCount, 9, "cannot be empty");
            AssertFailure(Parse(discontinuous), ParseErrorKind.InvalidCoordinate,
                ExtendedWkbFixtures.DiscontinuousSecondOffset(), "continuous");
            AssertFailure(Parse(openCurvePolygon), ParseErrorKind.InvalidCoordinate, 9, "closed");
        });
    }

    private static GeometryReadResult Parse(byte[] bytes)
    {
        return new WkbParser(bytes, new WkbReadOptions()).ParseDocument();
    }

    private static Geometry ParseSuccess(byte[] bytes)
    {
        GeometryReadResult result = Parse(bytes);
        Assert.That(result.Success, Is.True, result.Error?.Message);
        return result.Geometry!;
    }

    private static void AssertFailure(GeometryReadResult result, ParseErrorKind kind, int offset, string messagePart)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Geometry, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(kind));
            Assert.That(result.Error.Offset, Is.EqualTo(offset));
            Assert.That(result.Consumed, Is.EqualTo(offset));
            Assert.That(result.Error.Message, Does.Contain(messagePart).IgnoreCase);
            Assert.That(result.Error.Message, Does.Contain($"offset {offset}"));
        });
    }

    private static void AssertExtendedPayload(Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Triangle:
                Assert.That(geometry.Triangle!.Shell.Coordinates, Has.Length.EqualTo(4));
                break;
            case GeometryType.Tin:
                Assert.That(geometry.Tin!.Triangles, Has.Length.EqualTo(1));
                break;
            case GeometryType.PolyhedralSurface:
                Assert.That(geometry.PolyhedralSurface!.Patches, Has.Length.EqualTo(1));
                break;
            case GeometryType.CircularString:
                Assert.That(geometry.CircularString!.Coordinates, Has.Length.EqualTo(3));
                break;
            case GeometryType.CompoundCurve:
                Assert.That(geometry.CompoundCurve!.Curves, Has.Length.EqualTo(2));
                break;
            case GeometryType.CurvePolygon:
                Assert.That(geometry.CurvePolygon!.Shell, Is.Not.Null);
                break;
            case GeometryType.MultiCurve:
                Assert.That(geometry.MultiCurve!.Curves, Has.Length.EqualTo(3));
                break;
            case GeometryType.MultiSurface:
                Assert.That(geometry.MultiSurface!.Surfaces, Has.Length.EqualTo(5));
                break;
        }
    }
}

internal static class ExtendedWkbFixtures
{
    internal static byte[] CreateExtended(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        if (type == GeometryType.Triangle) return CreateTriangle(layout, byteOrder, false);
        if (type == GeometryType.Tin) return CreateTin(layout, byteOrder);
        if (type == GeometryType.PolyhedralSurface) return CreatePolyhedralSurface(layout, byteOrder);
        if (type == GeometryType.CircularString) return CreateCircular(layout, byteOrder, 0.0, 0.0, 2.0, 2.0, 4.0, 0.0);
        if (type == GeometryType.CompoundCurve) return CreateCompoundCurve(layout, byteOrder);
        if (type == GeometryType.CurvePolygon) return CreateCurvePolygon(layout, byteOrder);
        if (type == GeometryType.MultiCurve) return CreateMultiCurve(layout, byteOrder);
        return CreateMultiSurface(layout, byteOrder);
    }

    internal static byte[] CreateEmptyExtended(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(type, layout, byteOrder);
        AppendUInt32(bytes, 0u, byteOrder);
        return bytes.ToArray();
    }

    internal static byte[] CreateTriangle(CoordinateLayout layout, ByteOrder byteOrder, bool empty)
    {
        List<byte> bytes = Start(GeometryType.Triangle, layout, byteOrder);
        AppendUInt32(bytes, empty ? 0u : 1u, byteOrder);
        if (!empty)
        {
            AppendUInt32(bytes, 4u, byteOrder);
            AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
            AppendCoordinate(bytes, layout, byteOrder, 4.0, 0.0);
            AppendCoordinate(bytes, layout, byteOrder, 2.0, 3.0);
            AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
        }
        return bytes.ToArray();
    }

    internal static byte[] CreateTin(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.Tin, layout, byteOrder);
        AppendUInt32(bytes, 1u, byteOrder);
        bytes.AddRange(CreateTriangle(layout, Opposite(byteOrder), false));
        return bytes.ToArray();
    }

    internal static byte[] CreatePolyhedralSurface(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.PolyhedralSurface, layout, byteOrder);
        AppendUInt32(bytes, 1u, byteOrder);
        bytes.AddRange(CreatePolygon(layout, Opposite(byteOrder), false));
        return bytes.ToArray();
    }

    internal static byte[] CreateCircular(
        CoordinateLayout layout, ByteOrder byteOrder,
        double firstX, double firstY, double middleX, double middleY, double lastX, double lastY)
    {
        List<byte> bytes = Start(GeometryType.CircularString, layout, byteOrder);
        AppendUInt32(bytes, 3u, byteOrder);
        AppendCoordinate(bytes, layout, byteOrder, firstX, firstY);
        AppendCoordinate(bytes, layout, byteOrder, middleX, middleY);
        AppendCoordinate(bytes, layout, byteOrder, lastX, lastY);
        return bytes.ToArray();
    }

    internal static byte[] CreateCompoundCurve(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.CompoundCurve, layout, byteOrder);
        AppendUInt32(bytes, 2u, byteOrder);
        bytes.AddRange(CreateLine(layout, Opposite(byteOrder), 0.0, 0.0, 1.0, 0.0));
        bytes.AddRange(CreateCircular(layout, byteOrder, 1.0, 0.0, 2.0, 2.0, 3.0, 0.0));
        return bytes.ToArray();
    }

    internal static byte[] CreateCurvePolygon(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.CurvePolygon, layout, byteOrder);
        AppendUInt32(bytes, 1u, byteOrder);
        bytes.AddRange(CreateClosedLine(layout, Opposite(byteOrder)));
        return bytes.ToArray();
    }

    internal static byte[] CreateMultiCurve(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.MultiCurve, layout, byteOrder);
        AppendUInt32(bytes, 3u, byteOrder);
        bytes.AddRange(CreateLine(layout, Opposite(byteOrder), 0.0, 0.0, 1.0, 0.0));
        bytes.AddRange(CreateCircular(layout, byteOrder, 0.0, 0.0, 2.0, 2.0, 4.0, 0.0));
        bytes.AddRange(CreateCompoundCurve(layout, Opposite(byteOrder)));
        return bytes.ToArray();
    }

    internal static byte[] CreateMultiSurface(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.MultiSurface, layout, byteOrder);
        AppendUInt32(bytes, 5u, byteOrder);
        bytes.AddRange(CreatePolygon(layout, Opposite(byteOrder), false));
        bytes.AddRange(CreateCurvePolygon(layout, byteOrder));
        bytes.AddRange(CreateTriangle(layout, Opposite(byteOrder), false));
        bytes.AddRange(CreateTin(layout, byteOrder));
        bytes.AddRange(CreatePolyhedralSurface(layout, Opposite(byteOrder)));
        return bytes.ToArray();
    }

    internal static byte[] CreatePoint(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.Point, layout, byteOrder);
        AppendCoordinate(bytes, layout, byteOrder, 1.0, 2.0);
        return bytes.ToArray();
    }

    internal static byte[] CreateLine(
        CoordinateLayout layout, ByteOrder byteOrder,
        double firstX, double firstY, double lastX, double lastY, bool empty = false)
    {
        List<byte> bytes = Start(GeometryType.LineString, layout, byteOrder);
        AppendUInt32(bytes, empty ? 0u : 2u, byteOrder);
        if (!empty)
        {
            AppendCoordinate(bytes, layout, byteOrder, firstX, firstY);
            AppendCoordinate(bytes, layout, byteOrder, lastX, lastY);
        }
        return bytes.ToArray();
    }

    internal static byte[] CreateClosedLine(CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = Start(GeometryType.LineString, layout, byteOrder);
        AppendUInt32(bytes, 5u, byteOrder);
        AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
        AppendCoordinate(bytes, layout, byteOrder, 4.0, 0.0);
        AppendCoordinate(bytes, layout, byteOrder, 4.0, 4.0);
        AppendCoordinate(bytes, layout, byteOrder, 0.0, 4.0);
        AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
        return bytes.ToArray();
    }

    internal static byte[] CreatePolygon(CoordinateLayout layout, ByteOrder byteOrder, bool empty)
    {
        List<byte> bytes = Start(GeometryType.Polygon, layout, byteOrder);
        AppendUInt32(bytes, empty ? 0u : 1u, byteOrder);
        if (!empty)
        {
            AppendUInt32(bytes, 5u, byteOrder);
            AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
            AppendCoordinate(bytes, layout, byteOrder, 4.0, 0.0);
            AppendCoordinate(bytes, layout, byteOrder, 4.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 0.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 0.0, 0.0);
        }
        return bytes.ToArray();
    }

    internal static byte[] CreateCollectionWithChild(
        GeometryType type, CoordinateLayout layout, ByteOrder byteOrder, byte[] child)
    {
        List<byte> bytes = Start(type, layout, byteOrder);
        AppendUInt32(bytes, 1u, byteOrder);
        bytes.AddRange(child);
        return bytes.ToArray();
    }

    internal static byte[] CreateTriangleWithRingCount(int coordinateCount)
    {
        List<byte> bytes = Start(GeometryType.Triangle, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(bytes, 1u, ByteOrder.BigEndian);
        AppendUInt32(bytes, (uint)coordinateCount, ByteOrder.BigEndian);
        int index = 0;
        while (index < coordinateCount)
        {
            double x = index == coordinateCount - 1 ? 0.0 : index;
            AppendCoordinate(bytes, CoordinateLayout.XY, ByteOrder.BigEndian, x, index % 2);
            index++;
        }
        return bytes.ToArray();
    }

    internal static byte[] CreateTriangleWithTwoRings()
    {
        List<byte> bytes = Start(GeometryType.Triangle, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(bytes, 2u, ByteOrder.BigEndian);
        AppendTriangleRing(bytes, ByteOrder.BigEndian, 0.0);
        AppendTriangleRing(bytes, ByteOrder.BigEndian, 1.0);
        return bytes.ToArray();
    }

    internal static byte[] CreateCircularWithCount(int count)
    {
        List<byte> bytes = Start(GeometryType.CircularString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(bytes, (uint)count, ByteOrder.BigEndian);
        int index = 0;
        while (index < count)
        {
            AppendCoordinate(bytes, CoordinateLayout.XY, ByteOrder.BigEndian, index, index % 2);
            index++;
        }
        return bytes.ToArray();
    }

    internal static byte[] CreateDiscontinuousCompoundCurve()
    {
        List<byte> bytes = Start(GeometryType.CompoundCurve, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(bytes, 2u, ByteOrder.BigEndian);
        bytes.AddRange(CreateLine(CoordinateLayout.XY, ByteOrder.LittleEndian, 0.0, 0.0, 1.0, 0.0));
        bytes.AddRange(CreateCircular(
            CoordinateLayout.XY, ByteOrder.LittleEndian, 2.0, 0.0, 3.0, 2.0, 4.0, 0.0));
        return bytes.ToArray();
    }

    internal static int DiscontinuousSecondOffset()
    {
        return 9 + CreateLine(CoordinateLayout.XY, ByteOrder.LittleEndian, 0.0, 0.0, 1.0, 0.0).Length;
    }

    internal static byte[] Header(uint typeCode, ByteOrder byteOrder)
    {
        List<byte> bytes = new List<byte> { (byte)byteOrder };
        AppendUInt32(bytes, typeCode, byteOrder);
        return bytes.ToArray();
    }

    internal static ByteOrder[] AllByteOrders()
    {
        return [ByteOrder.BigEndian, ByteOrder.LittleEndian];
    }

    internal static CoordinateLayout[] AllLayouts()
    {
        return [CoordinateLayout.XY, CoordinateLayout.XYZ, CoordinateLayout.XYM, CoordinateLayout.XYZM];
    }

    internal static GeometryType[] ExtendedTypes()
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

    private static List<byte> Start(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = new List<byte> { (byte)byteOrder };
        AppendUInt32(bytes, TypeCode(type, layout), byteOrder);
        return bytes;
    }

    private static void AppendTriangleRing(List<byte> bytes, ByteOrder byteOrder, double offset)
    {
        AppendUInt32(bytes, 4u, byteOrder);
        AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder, offset, offset);
        AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder, offset + 2.0, offset);
        AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder, offset + 1.0, offset + 2.0);
        AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder, offset, offset);
    }

    private static void AppendCoordinate(
        List<byte> bytes, CoordinateLayout layout, ByteOrder byteOrder, double x, double y)
    {
        AppendDouble(bytes, x, byteOrder);
        AppendDouble(bytes, y, byteOrder);
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM)
        {
            AppendDouble(bytes, 3.0, byteOrder);
        }
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM)
        {
            AppendDouble(bytes, 4.0, byteOrder);
        }
    }

    private static void AppendUInt32(List<byte> bytes, uint value, ByteOrder byteOrder)
    {
        byte[] encoded = new byte[4];
        if (byteOrder == ByteOrder.BigEndian) BinaryPrimitives.WriteUInt32BigEndian(encoded, value);
        else BinaryPrimitives.WriteUInt32LittleEndian(encoded, value);
        bytes.AddRange(encoded);
    }

    private static void AppendDouble(List<byte> bytes, double value, ByteOrder byteOrder)
    {
        byte[] encoded = new byte[8];
        long bits = BitConverter.DoubleToInt64Bits(value);
        if (byteOrder == ByteOrder.BigEndian) BinaryPrimitives.WriteInt64BigEndian(encoded, bits);
        else BinaryPrimitives.WriteInt64LittleEndian(encoded, bits);
        bytes.AddRange(encoded);
    }

    private static uint TypeCode(GeometryType type, CoordinateLayout layout)
    {
        uint offset = 0u;
        if (layout == CoordinateLayout.XYZ) offset = 1000u;
        else if (layout == CoordinateLayout.XYM) offset = 2000u;
        else if (layout == CoordinateLayout.XYZM) offset = 3000u;
        return (uint)type + offset;
    }

    private static ByteOrder Opposite(ByteOrder byteOrder)
    {
        return byteOrder == ByteOrder.BigEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
    }
}
