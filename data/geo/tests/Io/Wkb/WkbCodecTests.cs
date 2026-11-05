using geo;
using System.IO;

namespace geo.Tests;

public class WkbCodecTests
{
    [Test]
    public void ReadsByteSpansAndUsesSnapshottedOptions()
    {
        WkbReadOptions readOptions = new WkbReadOptions { MaximumCoordinates = 2 };
        IWkbCodec codec = new WkbCodec(readOptions, new WkbWriteOptions());
        readOptions.MaximumCoordinates = 1;
        byte[] bytes = Convert.FromHexString(
            "0000000002000000020000000000000000000000000000000040000000000000004008000000000000");

        Geometry geometry = codec.ReadWkb(bytes.AsSpan());
        GeometryReadResult result = codec.TryReadWkb(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(geometry.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(geometry.LineString!.Coordinates[1].X, Is.EqualTo(2.0));
            Assert.That(geometry.LineString.Coordinates[1].Y, Is.EqualTo(3.0));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Consumed, Is.EqualTo(bytes.Length));
            Assert.That(result.Geometry!.LineString!.Coordinates.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public void TryReadReturnsDetailedFailureWhileReadThrowsIt()
    {
        IWkbCodec codec = CreateCodec();
        byte[] invalid = Convert.FromHexString("0101000000");

        GeometryReadResult result = codec.TryReadWkb(invalid);
        GeometryParseException exception = Assert.Throws<GeometryParseException>(
            () => codec.ReadWkb(invalid))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Geometry, Is.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(result.Error.Offset, Is.EqualTo(invalid.Length));
            Assert.That(result.Consumed, Is.EqualTo(invalid.Length));
            Assert.That(exception.Message, Is.EqualTo(result.Error.Message));
        });
    }

    [Test]
    public void ReadsStreamsWithinTheLimitAndLeavesThemOpen()
    {
        IWkbCodec codec = CreateCodec();
        byte[] bytes = Convert.FromHexString("0101000000000000000000F03F0000000000000040");
        MemoryStream stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        Geometry geometry = codec.ReadWkbStream(stream);
        stream.WriteByte(0xaa);

        Assert.Multiple(() =>
        {
            Assert.That(geometry.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(geometry.Point.Coordinate.Y, Is.EqualTo(2.0));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.Length, Is.EqualTo(bytes.Length + 1));
            Assert.Throws<GeometryParseException>(() => codec.ReadWkbStream(null!));
        });
    }

    [Test]
    public void StreamAndHexReadsEnforceBoundsAndStrictHexDigits()
    {
        WkbReadOptions options = new WkbReadOptions { MaximumInputBytes = 20 };
        IWkbCodec bounded = new WkbCodec(options, new WkbWriteOptions());
        byte[] point = Convert.FromHexString("0101000000000000000000F03F0000000000000040");

        Assert.Multiple(() =>
        {
            Assert.Throws<GeometryParseException>(() => bounded.ReadWkbStream(new MemoryStream(point)));
            Assert.Throws<GeometryParseException>(() => bounded.ReadWkbHex(Convert.ToHexString(point)));
            Assert.Throws<GeometryParseException>(() => CreateCodec().ReadWkbHex("010"));
            Assert.Throws<GeometryParseException>(() => CreateCodec().ReadWkbHex("01xz"));
            Assert.Throws<GeometryParseException>(() => CreateCodec().ReadWkbHex(null!));
        });

        Geometry lowerCase = CreateCodec().ReadWkbHex(Convert.ToHexString(point).ToLowerInvariant());
        Assert.That(lowerCase.Point!.Coordinate!.X, Is.EqualTo(1.0));
    }

    [Test]
    public void WritesOwningBytesHexAndExactConfiguredSize()
    {
        WkbWriteOptions writeOptions = new WkbWriteOptions { ByteOrder = ByteOrder.LittleEndian };
        IWkbCodec codec = new WkbCodec(new WkbReadOptions(), writeOptions);
        writeOptions.ByteOrder = ByteOrder.BigEndian;
        Geometry point = ParseWkt("POINT (1 2)");

        byte[] bytes = codec.WriteWkb(point);
        byte[] second = codec.WriteWkb(point);
        string hex = codec.WriteWkbHex(point);
        bytes[0] = 0xff;

        Assert.Multiple(() =>
        {
            Assert.That(second[0], Is.EqualTo((byte)ByteOrder.LittleEndian));
            Assert.That(hex, Is.EqualTo("0101000000000000000000F03F0000000000000040"));
            Assert.That(codec.GetWkbSize(point), Is.EqualTo(second.Length));
            Assert.That(second[0], Is.Not.EqualTo(bytes[0]));
        });
    }

    [Test]
    public void TryWriteReportsRequiredSizeWithoutPartialOutput()
    {
        IWkbCodec codec = CreateCodec();
        Geometry line = ParseWkt("LINESTRING (0 0, 2 3)");
        byte[] expected = codec.WriteWkb(line);
        byte[] exact = new byte[expected.Length];
        byte[] small = Enumerable.Repeat((byte)0xaa, expected.Length - 1).ToArray();

        WriteResult success = codec.TryWriteWkbBytes(line, exact);
        WriteResult failure = codec.TryWriteWkbBytes(line, small);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(success.WrittenSize, Is.EqualTo(expected.Length));
            Assert.That(success.Error, Is.Null);
            Assert.That(exact, Is.EqualTo(expected));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(failure.WrittenSize, Is.Zero);
            Assert.That(failure.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(small, Is.All.EqualTo(0xaa));
        });
    }

    [Test]
    public void StreamOutputMatchesOwningOutputAndLeavesStreamOpen()
    {
        IWkbCodec codec = CreateCodec();
        Geometry point = ParseWkt("POINT (1 2)");
        byte[] expected = codec.WriteWkb(point);
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xff);

        codec.WriteWkbStream(point, stream);
        stream.WriteByte(0xee);

        Assert.Multiple(() =>
        {
            Assert.That(stream.ToArray().Skip(1).Take(expected.Length), Is.EqualTo(expected));
            Assert.That(stream.ToArray()[stream.ToArray().Length - 1], Is.EqualTo(0xee));
            Assert.Throws<InvalidGeometryException>(() => codec.WriteWkbStream(point, null!));
        });
    }

    [Test]
    public void EwkbRoundTripPreservesLayoutAndInheritedSrid()
    {
        WkbReadOptions readOptions = new WkbReadOptions { Flavor = WkbFlavor.Ewkb };
        WkbWriteOptions writeOptions = new WkbWriteOptions
        {
            ByteOrder = ByteOrder.LittleEndian,
            Flavor = WkbFlavor.Ewkb,
            IncludeSpatialReferenceId = true
        };
        IWkbCodec codec = new WkbCodec(readOptions, writeOptions);
        Geometry source = ParseWkt(
            "SRID=4979;GEOMETRYCOLLECTION ZM (POINT ZM (1 2 3 4), LINESTRING ZM (0 0 5 6, 7 8 9 10))",
            new WktReadOptions { AllowSpatialReferenceId = true });

        byte[] bytes = codec.WriteWkb(source);
        Geometry result = codec.ReadWkb(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.GeometryType, Is.EqualTo(GeometryType.GeometryCollection));
            Assert.That(result.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZM));
            Assert.That(result.SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(result.GeometryCollection!.Geometries[0].SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(result.GeometryCollection.Geometries[1].SpatialReferenceId, Is.EqualTo(4979));
            Assert.That(result.GeometryCollection.Geometries[0].Point!.Coordinate!.M, Is.EqualTo(4.0));
            Assert.That(codec.WriteWkbHex(source), Is.EqualTo(Convert.ToHexString(bytes)));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidConfiguration()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(null!, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(new WkbReadOptions(), null!));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions { Flavor = (WkbFlavor)99 }, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions { MaximumCoordinates = 0 }, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions { MaximumComponents = 0 }, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions { MaximumNestingDepth = -1 }, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions { MaximumInputBytes = 0 }, new WkbWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WkbCodec(
                new WkbReadOptions(), new WkbWriteOptions { Flavor = (WkbFlavor)99 }));
        });
    }

    private static IWkbCodec CreateCodec()
    {
        return new WkbCodec(new WkbReadOptions(), new WkbWriteOptions());
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
}
