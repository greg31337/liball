using geo;
using System.Buffers.Binary;

namespace geo.Tests;

public class WkbParserTests
{
    [Test]
    public void ParsesKnownPointFixturesForBothByteOrders()
    {
        byte[] big = Convert.FromHexString("00000000013FF00000000000004000000000000000");
        byte[] little = Convert.FromHexString("0101000000000000000000F03F0000000000000040");

        Geometry bigPoint = ParseSuccess(big);
        Geometry littlePoint = ParseSuccess(little);

        Assert.Multiple(() =>
        {
            Assert.That(bigPoint.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(bigPoint.Point.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(littlePoint.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(littlePoint.Point.Coordinate.Y, Is.EqualTo(2.0));
        });
    }

    [Test]
    public void ParsesEveryCoreTypeLayoutAndRootByteOrder()
    {
        foreach (ByteOrder byteOrder in AllByteOrders())
        {
            foreach (CoordinateLayout layout in AllLayouts())
            {
                foreach (GeometryType type in CoreTypes())
                {
                    byte[] bytes = CreateCoreGeometry(type, layout, byteOrder);
                    GeometryReadResult result = Parse(bytes, new WkbReadOptions());

                    Assert.That(result.Success, Is.True, $"{type} {layout} {byteOrder}: {result.Error?.Message}");
                    Assert.That(result.Consumed, Is.EqualTo(bytes.Length), $"{type} {layout} {byteOrder}");
                    Assert.That(result.Geometry, Is.Not.Null, $"{type} {layout} {byteOrder}");
                    AssertCoreGeometry(result.Geometry!, type, layout);
                }
            }
        }
    }

    [Test]
    public void ParsesEmptyValuesAndEmptyHomogeneousComponents()
    {
        foreach (CoordinateLayout layout in AllLayouts())
        {
            Geometry point = ParseSuccess(CreatePoint(layout, ByteOrder.BigEndian, true));
            Assert.That(point.Point!.Coordinate, Is.Null, layout.ToString());

            foreach (GeometryType type in CoreTypes().Where(value => value != GeometryType.Point))
            {
                Geometry empty = ParseSuccess(CreateEmptyGeometry(type, layout, ByteOrder.LittleEndian));
                Assert.That(IsEmptyPayload(empty), Is.True, $"{type} {layout}");
            }
        }

        List<byte> multiPoint = StartGeometry(GeometryType.MultiPoint, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(multiPoint, 1u, ByteOrder.BigEndian);
        multiPoint.AddRange(CreatePoint(CoordinateLayout.XY, ByteOrder.LittleEndian, true));
        List<byte> multiLine = StartGeometry(GeometryType.MultiLineString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(multiLine, 1u, ByteOrder.BigEndian);
        multiLine.AddRange(CreateEmptyGeometry(GeometryType.LineString, CoordinateLayout.XY, ByteOrder.LittleEndian));
        List<byte> multiPolygon = StartGeometry(GeometryType.MultiPolygon, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(multiPolygon, 1u, ByteOrder.BigEndian);
        multiPolygon.AddRange(CreateEmptyGeometry(GeometryType.Polygon, CoordinateLayout.XY, ByteOrder.LittleEndian));

        Assert.Multiple(() =>
        {
            Assert.That(ParseSuccess(multiPoint.ToArray()).MultiPoint!.Points[0].Coordinate, Is.Null);
            Assert.That(ParseSuccess(multiLine.ToArray()).MultiLineString!.LineStrings[0].Coordinates, Is.Empty);
            Assert.That(ParseSuccess(multiPolygon.ToArray()).MultiPolygon!.Polygons[0].Shell.Coordinates, Is.Empty);
        });
    }

    [Test]
    public void HonorsExactConsumptionAndMaximumInputSize()
    {
        byte[] point = CreatePoint(CoordinateLayout.XY, ByteOrder.BigEndian, false);
        byte[] trailing = point.Concat(new byte[] { 0xaa, 0xbb }).ToArray();
        WkbReadOptions relaxed = new WkbReadOptions { RequireExactLength = false };
        WkbReadOptions bounded = new WkbReadOptions { MaximumInputBytes = point.Length - 1 };

        GeometryReadResult exactResult = Parse(trailing, new WkbReadOptions());
        GeometryReadResult relaxedResult = Parse(trailing, relaxed);
        GeometryReadResult boundedResult = Parse(point, bounded);

        Assert.Multiple(() =>
        {
            AssertFailure(exactResult, ParseErrorKind.TrailingContent, point.Length, "follow");
            Assert.That(relaxedResult.Success, Is.True);
            Assert.That(relaxedResult.Consumed, Is.EqualTo(point.Length));
            Assert.That(relaxedResult.Geometry!.Point!.Coordinate!.X, Is.EqualTo(1.0));
            AssertFailure(boundedResult, ParseErrorKind.LimitExceeded, point.Length - 1, "input size");
        });
    }

    [Test]
    public void EnforcesCoordinateComponentAndNestingLimitsBeforeAllocation()
    {
        byte[] line = CreateLineString(CoordinateLayout.XY, ByteOrder.BigEndian, false);
        byte[] multiPoint = CreateCoreGeometry(GeometryType.MultiPoint, CoordinateLayout.XY, ByteOrder.BigEndian);
        byte[] collection = CreateCoreGeometry(GeometryType.GeometryCollection, CoordinateLayout.XY, ByteOrder.BigEndian);
        byte[] polygon = CreatePolygon(CoordinateLayout.XY, ByteOrder.BigEndian, false);

        GeometryReadResult coordinateLimit = Parse(line, new WkbReadOptions { MaximumCoordinates = 1 });
        GeometryReadResult componentLimit = Parse(multiPoint, new WkbReadOptions { MaximumComponents = 1 });
        GeometryReadResult depthLimit = Parse(collection, new WkbReadOptions { MaximumNestingDepth = 0 });
        GeometryReadResult ringLimit = Parse(polygon, new WkbReadOptions { MaximumComponents = 1 });

        Assert.Multiple(() =>
        {
            AssertFailure(coordinateLimit, ParseErrorKind.LimitExceeded, 5, "coordinate count");
            AssertFailure(componentLimit, ParseErrorKind.LimitExceeded, 5, "component count");
            AssertFailure(depthLimit, ParseErrorKind.LimitExceeded, 9, "nesting depth");
            AssertFailure(ringLimit, ParseErrorKind.LimitExceeded, 5, "component count");
        });
    }

    [Test]
    public void RejectsTruncatedHeadersCoordinatesAndNestedValues()
    {
        AssertFailure(Parse([], new WkbReadOptions()), ParseErrorKind.UnexpectedEnd, 0, "byte-order");
        AssertFailure(Parse([0], new WkbReadOptions()), ParseErrorKind.UnexpectedEnd, 1, "type code");

        byte[] point = CreatePoint(CoordinateLayout.XY, ByteOrder.BigEndian, false);
        byte[] truncatedPoint = point.Take(point.Length - 1).ToArray();
        AssertFailure(Parse(truncatedPoint, new WkbReadOptions()), ParseErrorKind.UnexpectedEnd,
            truncatedPoint.Length, "coordinate");

        List<byte> line = StartGeometry(GeometryType.LineString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(line, 2u, ByteOrder.BigEndian);
        AssertFailure(Parse(line.ToArray(), new WkbReadOptions()), ParseErrorKind.UnexpectedEnd,
            line.Count, "coordinates");

        List<byte> collection = StartGeometry(GeometryType.GeometryCollection, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(collection, 1u, ByteOrder.BigEndian);
        collection.Add((byte)ByteOrder.LittleEndian);
        AssertFailure(Parse(collection.ToArray(), new WkbReadOptions()), ParseErrorKind.UnexpectedEnd,
            collection.Count, "nested geometry headers");
    }

    [Test]
    public void RejectsInvalidMarkersTypesAndCounts()
    {
        byte[] invalidMarker = { 2, 0, 0, 0, 1 };
        byte[] invalidType = Header(0u, ByteOrder.BigEndian);
        byte[] abstractType = Header(13u, ByteOrder.BigEndian);
        byte[] flaggedType = Header(WkbTypeCodes.EwkbZFlag | 1u, ByteOrder.LittleEndian);
        List<byte> hugeLine = StartGeometry(GeometryType.LineString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(hugeLine, uint.MaxValue, ByteOrder.BigEndian);
        List<byte> onePointLine = StartGeometry(GeometryType.LineString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(onePointLine, 1u, ByteOrder.BigEndian);
        AppendCoordinate(onePointLine, CoordinateLayout.XY, ByteOrder.BigEndian, 0.0, 0.0, 0.0, 0.0);
        List<byte> overLimitLine = StartGeometry(GeometryType.LineString, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(overLimitLine, 11u, ByteOrder.BigEndian);

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(invalidMarker, new WkbReadOptions()), ParseErrorKind.InvalidWkb, 0, "marker");
            AssertFailure(Parse(invalidType, new WkbReadOptions()), ParseErrorKind.InvalidGeometryType, 1, "type code");
            AssertFailure(Parse(abstractType, new WkbReadOptions()), ParseErrorKind.UnsupportedType, 1, "abstract");
            AssertFailure(Parse(flaggedType, new WkbReadOptions()), ParseErrorKind.InvalidGeometryType, 1, "type code");
            AssertFailure(Parse(hugeLine.ToArray(), new WkbReadOptions()), ParseErrorKind.InvalidCount, 5, "signed count");
            AssertFailure(Parse(onePointLine.ToArray(), new WkbReadOptions()), ParseErrorKind.InvalidCount, 5, "at least two");
            AssertFailure(Parse(overLimitLine.ToArray(), new WkbReadOptions { MaximumCoordinates = 10 }),
                ParseErrorKind.LimitExceeded, 5, "coordinate count");
        });
    }

    [Test]
    public void RejectsMalformedRingsAndNonFiniteCoordinates()
    {
        byte[] shortRing = CreatePolygonWithRing(
            [[0.0, 0.0], [1.0, 0.0], [0.0, 0.0]], ByteOrder.BigEndian);
        byte[] openRing = CreatePolygonWithRing(
            [[0.0, 0.0], [2.0, 0.0], [2.0, 2.0], [0.0, 2.0]], ByteOrder.BigEndian);
        byte[] repeatedRing = CreatePolygonWithRing(
            [[0.0, 0.0], [1.0, 0.0], [1.0, 0.0], [0.0, 0.0]], ByteOrder.BigEndian);
        byte[] infiniteLine = CreateLineWithCoordinates(
            [[0.0, 0.0], [double.PositiveInfinity, 1.0]], ByteOrder.LittleEndian);
        byte[] partialEmptyPoint = CreatePointWithOrdinates(
            CoordinateLayout.XY, ByteOrder.BigEndian, double.NaN, 2.0, double.NaN, double.NaN);

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(shortRing, new WkbReadOptions()), ParseErrorKind.InvalidCount, 9, "four");
            AssertFailure(Parse(openRing, new WkbReadOptions()), ParseErrorKind.InvalidCoordinate, 9, "closed");
            AssertFailure(Parse(repeatedRing, new WkbReadOptions()), ParseErrorKind.InvalidCoordinate, 9, "distinct");
            AssertFailure(Parse(infiniteLine, new WkbReadOptions()), ParseErrorKind.InvalidCoordinate, 25, "finite");
            AssertFailure(Parse(partialEmptyPoint, new WkbReadOptions()), ParseErrorKind.InvalidCoordinate, 5, "finite");
        });
    }

    [Test]
    public void RejectsNestedLayoutAndHomogeneousTypeMismatches()
    {
        List<byte> layoutMismatch = StartGeometry(GeometryType.MultiPoint, CoordinateLayout.XYZ, ByteOrder.BigEndian);
        AppendUInt32(layoutMismatch, 1u, ByteOrder.BigEndian);
        layoutMismatch.AddRange(CreatePoint(CoordinateLayout.XY, ByteOrder.LittleEndian, false));
        List<byte> typeMismatch = StartGeometry(GeometryType.MultiPoint, CoordinateLayout.XY, ByteOrder.BigEndian);
        AppendUInt32(typeMismatch, 1u, ByteOrder.BigEndian);
        typeMismatch.AddRange(CreateLineString(CoordinateLayout.XY, ByteOrder.LittleEndian, false));

        Assert.Multiple(() =>
        {
            AssertFailure(Parse(layoutMismatch.ToArray(), new WkbReadOptions()),
                ParseErrorKind.InvalidCoordinateLayout, 10, "does not match");
            AssertFailure(Parse(typeMismatch.ToArray(), new WkbReadOptions()),
                ParseErrorKind.InvalidGeometryType, 9, "required type Point");
        });
    }

    [Test]
    public void ReturnsStructuredFailuresForNullInputsAndInvalidOptions()
    {
        GeometryReadResult nullInput = Parse(null!, new WkbReadOptions());
        GeometryReadResult nullOptions = Parse(Header(1u, ByteOrder.BigEndian), null!);
        GeometryReadResult invalidFlavor = Parse(
            Header(1u, ByteOrder.BigEndian), new WkbReadOptions { Flavor = (WkbFlavor)99 });
        GeometryReadResult noCoordinates = Parse(Header(1u, ByteOrder.BigEndian), new WkbReadOptions { MaximumCoordinates = 0 });
        GeometryReadResult noComponents = Parse(Header(1u, ByteOrder.BigEndian), new WkbReadOptions { MaximumComponents = 0 });
        GeometryReadResult negativeDepth = Parse(Header(1u, ByteOrder.BigEndian), new WkbReadOptions { MaximumNestingDepth = -1 });
        GeometryReadResult noBytes = Parse(Header(1u, ByteOrder.BigEndian), new WkbReadOptions { MaximumInputBytes = 0 });

        Assert.Multiple(() =>
        {
            AssertFailure(nullInput, ParseErrorKind.UnexpectedEnd, 0, "null");
            AssertFailure(nullOptions, ParseErrorKind.LimitExceeded, 0, "options");
            AssertFailure(invalidFlavor, ParseErrorKind.UnsupportedType, 0, "flavor");
            AssertFailure(noCoordinates, ParseErrorKind.LimitExceeded, 0, "coordinate count");
            AssertFailure(noComponents, ParseErrorKind.LimitExceeded, 0, "component count");
            AssertFailure(negativeDepth, ParseErrorKind.LimitExceeded, 0, "nesting depth");
            AssertFailure(noBytes, ParseErrorKind.LimitExceeded, 0, "input size");
        });
    }

    private static GeometryReadResult Parse(byte[] bytes, WkbReadOptions options)
    {
        return new WkbParser(bytes, options).ParseDocument();
    }

    private static Geometry ParseSuccess(byte[] bytes)
    {
        GeometryReadResult result = Parse(bytes, new WkbReadOptions());
        Assert.That(result.Success, Is.True, result.Error?.Message);
        Assert.That(result.Geometry, Is.Not.Null);
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

    private static void AssertCoreGeometry(Geometry geometry, GeometryType type, CoordinateLayout layout)
    {
        Assert.That(geometry.GeometryType, Is.EqualTo(type));
        Assert.That(geometry.CoordinateLayout, Is.EqualTo(layout));
        Assert.That(geometry.SpatialReferenceId, Is.EqualTo(GeometryConstants.UnknownSpatialReferenceId));
        Coordinate coordinate;
        switch (type)
        {
            case GeometryType.Point:
                coordinate = geometry.Point!.Coordinate!;
                break;
            case GeometryType.LineString:
                Assert.That(geometry.LineString!.Coordinates, Has.Length.EqualTo(2));
                coordinate = geometry.LineString.Coordinates[0];
                break;
            case GeometryType.Polygon:
                Assert.That(geometry.Polygon!.Shell.Coordinates, Has.Length.EqualTo(5));
                Assert.That(geometry.Polygon.Holes, Has.Length.EqualTo(1));
                coordinate = geometry.Polygon.Shell.Coordinates[0];
                break;
            case GeometryType.MultiPoint:
                Assert.That(geometry.MultiPoint!.Points, Has.Length.EqualTo(1));
                coordinate = geometry.MultiPoint.Points[0].Coordinate!;
                break;
            case GeometryType.MultiLineString:
                Assert.That(geometry.MultiLineString!.LineStrings, Has.Length.EqualTo(1));
                coordinate = geometry.MultiLineString.LineStrings[0].Coordinates[0];
                break;
            case GeometryType.MultiPolygon:
                Assert.That(geometry.MultiPolygon!.Polygons, Has.Length.EqualTo(1));
                Assert.That(geometry.MultiPolygon.Polygons[0].Holes, Has.Length.EqualTo(1));
                coordinate = geometry.MultiPolygon.Polygons[0].Shell.Coordinates[0];
                break;
            default:
                Assert.That(geometry.GeometryCollection!.Geometries, Has.Length.EqualTo(2));
                coordinate = geometry.GeometryCollection.Geometries[0].Point!.Coordinate!;
                break;
        }

        Assert.Multiple(() =>
        {
            Assert.That(coordinate.X, Is.EqualTo(1.0));
            Assert.That(coordinate.Y, Is.EqualTo(2.0));
            Assert.That(coordinate.Z, layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM
                ? Is.EqualTo(3.0) : Is.NaN);
            Assert.That(coordinate.M, layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM
                ? Is.EqualTo(4.0) : Is.NaN);
        });
    }

    private static bool IsEmptyPayload(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.LineString) return geometry.LineString!.Coordinates.Length == 0;
        if (geometry.GeometryType == GeometryType.Polygon) return geometry.Polygon!.Shell.Coordinates.Length == 0;
        if (geometry.GeometryType == GeometryType.MultiPoint) return geometry.MultiPoint!.Points.Length == 0;
        if (geometry.GeometryType == GeometryType.MultiLineString) return geometry.MultiLineString!.LineStrings.Length == 0;
        if (geometry.GeometryType == GeometryType.MultiPolygon) return geometry.MultiPolygon!.Polygons.Length == 0;
        return geometry.GeometryCollection!.Geometries.Length == 0;
    }

    private static byte[] CreateCoreGeometry(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        if (type == GeometryType.Point) return CreatePoint(layout, byteOrder, false);
        if (type == GeometryType.LineString) return CreateLineString(layout, byteOrder, false);
        if (type == GeometryType.Polygon) return CreatePolygon(layout, byteOrder, false);

        List<byte> bytes = StartGeometry(type, layout, byteOrder);
        ByteOrder childOrder = byteOrder == ByteOrder.BigEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
        if (type == GeometryType.MultiPoint)
        {
            AppendUInt32(bytes, 1u, byteOrder);
            bytes.AddRange(CreatePoint(layout, childOrder, false));
        }
        else if (type == GeometryType.MultiLineString)
        {
            AppendUInt32(bytes, 1u, byteOrder);
            bytes.AddRange(CreateLineString(layout, childOrder, false));
        }
        else if (type == GeometryType.MultiPolygon)
        {
            AppendUInt32(bytes, 1u, byteOrder);
            bytes.AddRange(CreatePolygon(layout, childOrder, false));
        }
        else
        {
            AppendUInt32(bytes, 2u, byteOrder);
            bytes.AddRange(CreatePoint(layout, childOrder, false));
            bytes.AddRange(CreateLineString(layout, byteOrder, false));
        }
        return bytes.ToArray();
    }

    private static byte[] CreateEmptyGeometry(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        if (type == GeometryType.Point) return CreatePoint(layout, byteOrder, true);
        List<byte> bytes = StartGeometry(type, layout, byteOrder);
        AppendUInt32(bytes, 0u, byteOrder);
        return bytes.ToArray();
    }

    private static byte[] CreatePoint(CoordinateLayout layout, ByteOrder byteOrder, bool empty)
    {
        double value = empty ? double.NaN : 1.0;
        return CreatePointWithOrdinates(layout, byteOrder, value, empty ? double.NaN : 2.0,
            empty ? double.NaN : 3.0, empty ? double.NaN : 4.0);
    }

    private static byte[] CreatePointWithOrdinates(
        CoordinateLayout layout, ByteOrder byteOrder, double x, double y, double z, double m)
    {
        List<byte> bytes = StartGeometry(GeometryType.Point, layout, byteOrder);
        AppendCoordinate(bytes, layout, byteOrder, x, y, z, m);
        return bytes.ToArray();
    }

    private static byte[] CreateLineString(CoordinateLayout layout, ByteOrder byteOrder, bool empty)
    {
        List<byte> bytes = StartGeometry(GeometryType.LineString, layout, byteOrder);
        AppendUInt32(bytes, empty ? 0u : 2u, byteOrder);
        if (!empty)
        {
            AppendCoordinate(bytes, layout, byteOrder, 1.0, 2.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 5.0, 6.0, 7.0, 8.0);
        }
        return bytes.ToArray();
    }

    private static byte[] CreateLineWithCoordinates(double[][] coordinates, ByteOrder byteOrder)
    {
        List<byte> bytes = StartGeometry(GeometryType.LineString, CoordinateLayout.XY, byteOrder);
        AppendUInt32(bytes, (uint)coordinates.Length, byteOrder);
        foreach (double[] coordinate in coordinates)
        {
            AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder,
                coordinate[0], coordinate[1], double.NaN, double.NaN);
        }
        return bytes.ToArray();
    }

    private static byte[] CreatePolygon(CoordinateLayout layout, ByteOrder byteOrder, bool empty)
    {
        List<byte> bytes = StartGeometry(GeometryType.Polygon, layout, byteOrder);
        AppendUInt32(bytes, empty ? 0u : 2u, byteOrder);
        if (!empty)
        {
            AppendUInt32(bytes, 5u, byteOrder);
            AppendCoordinate(bytes, layout, byteOrder, 1.0, 2.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 5.0, 2.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 5.0, 6.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 1.0, 6.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 1.0, 2.0, 3.0, 4.0);
            AppendUInt32(bytes, 5u, byteOrder);
            AppendCoordinate(bytes, layout, byteOrder, 2.0, 3.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 3.0, 3.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 3.0, 4.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 2.0, 4.0, 3.0, 4.0);
            AppendCoordinate(bytes, layout, byteOrder, 2.0, 3.0, 3.0, 4.0);
        }
        return bytes.ToArray();
    }

    private static byte[] CreatePolygonWithRing(double[][] coordinates, ByteOrder byteOrder)
    {
        List<byte> bytes = StartGeometry(GeometryType.Polygon, CoordinateLayout.XY, byteOrder);
        AppendUInt32(bytes, 1u, byteOrder);
        AppendUInt32(bytes, (uint)coordinates.Length, byteOrder);
        foreach (double[] coordinate in coordinates)
        {
            AppendCoordinate(bytes, CoordinateLayout.XY, byteOrder,
                coordinate[0], coordinate[1], double.NaN, double.NaN);
        }
        return bytes.ToArray();
    }

    private static List<byte> StartGeometry(GeometryType type, CoordinateLayout layout, ByteOrder byteOrder)
    {
        List<byte> bytes = new List<byte> { (byte)byteOrder };
        AppendUInt32(bytes, TypeCode(type, layout), byteOrder);
        return bytes;
    }

    private static byte[] Header(uint typeCode, ByteOrder byteOrder)
    {
        List<byte> bytes = new List<byte> { (byte)byteOrder };
        AppendUInt32(bytes, typeCode, byteOrder);
        return bytes.ToArray();
    }

    private static void AppendCoordinate(
        List<byte> bytes, CoordinateLayout layout, ByteOrder byteOrder, double x, double y, double z, double m)
    {
        AppendDouble(bytes, x, byteOrder);
        AppendDouble(bytes, y, byteOrder);
        if (layout == CoordinateLayout.XYZ || layout == CoordinateLayout.XYZM) AppendDouble(bytes, z, byteOrder);
        if (layout == CoordinateLayout.XYM || layout == CoordinateLayout.XYZM) AppendDouble(bytes, m, byteOrder);
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

    private static ByteOrder[] AllByteOrders()
    {
        return [ByteOrder.BigEndian, ByteOrder.LittleEndian];
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
}
