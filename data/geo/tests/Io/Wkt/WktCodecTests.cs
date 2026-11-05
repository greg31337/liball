using geo;
using System.IO;

namespace geo.Tests;

public class WktCodecTests
{
    [Test]
    public void ReadsStringsAndCharacterSpansUsingSnapshottedOptions()
    {
        WktReadOptions readOptions = new WktReadOptions
        {
            AllowSpatialReferenceId = true,
            MaximumCoordinates = 2
        };
        IWktCodec codec = new WktCodec(readOptions, new WktWriteOptions());
        readOptions.AllowSpatialReferenceId = false;
        readOptions.MaximumCoordinates = 1;
        string text = "SRID=4326;LINESTRING Z (1 2 3, 4 5 6)";

        Geometry fromString = codec.ReadWkt(text);
        Geometry fromSpan = codec.ReadWktCharacters(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(fromString.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(fromString.CoordinateLayout, Is.EqualTo(CoordinateLayout.XYZ));
            Assert.That(fromString.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(fromString.LineString!.Coordinates[1].Z, Is.EqualTo(6.0));
            Assert.That(fromSpan.GeometryType, Is.EqualTo(GeometryType.LineString));
            Assert.That(fromSpan.SpatialReferenceId, Is.EqualTo(4326));
            Assert.That(fromSpan.LineString!.Coordinates[0].X, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void TryReadReturnsDetailedFailureWhileReadThrowsIt()
    {
        IWktCodec codec = CreateCodec();
        string invalid = "POINT (1)";

        GeometryReadResult result = codec.TryReadWkt(invalid);
        GeometryParseException exception = Assert.Throws<GeometryParseException>(() => codec.ReadWkt(invalid))!;
        GeometryParseException spanException = Assert.Throws<GeometryParseException>(
            () => codec.ReadWktCharacters(invalid.AsSpan()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Geometry, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidCoordinate));
            Assert.That(result.Error.Offset, Is.EqualTo(8));
            Assert.That(result.Consumed, Is.EqualTo(8));
            Assert.That(exception.Message, Is.EqualTo(result.Error.Message));
            Assert.That(spanException.Message, Is.EqualTo(result.Error.Message));
        });
    }

    [Test]
    public void TryReadHandlesNullInputWithoutThrowing()
    {
        IWktCodec codec = CreateCodec();

        GeometryReadResult result = codec.TryReadWkt(null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Geometry, Is.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(result.Error.Offset, Is.EqualTo(0));
            Assert.Throws<GeometryParseException>(() => codec.ReadWkt(null!));
        });
    }

    [Test]
    public void WritesCanonicalStringsUsingSnapshottedOptions()
    {
        WktReadOptions readOptions = new WktReadOptions { AllowSpatialReferenceId = true };
        WktWriteOptions writeOptions = new WktWriteOptions
        {
            DecimalPrecision = 2,
            IncludeSpatialReferenceId = true
        };
        IWktCodec codec = new WktCodec(readOptions, writeOptions);
        writeOptions.DecimalPrecision = 0;
        writeOptions.IncludeSpatialReferenceId = false;
        Geometry geometry = codec.ReadWkt("SRID=4326;POINT (1.234 2) ");

        string text = codec.WriteWkt(geometry);

        Assert.That(text, Is.EqualTo("SRID=4326;POINT (1.23 2)"));
    }

    [Test]
    public void TryWriteReportsRequiredSizeAndDoesNotPartiallyWrite()
    {
        IWktCodec codec = CreateCodec();
        Geometry geometry = codec.ReadWkt("LINESTRING (0 0, 2 3)");
        string expected = codec.WriteWkt(geometry);
        char[] exact = new char[expected.Length];
        char[] small = new string('?', expected.Length - 1).ToCharArray();

        WriteResult success = codec.TryWriteWktCharacters(geometry, exact);
        WriteResult failure = codec.TryWriteWktCharacters(geometry, small);

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
        });
    }

    [Test]
    public void TextWriterOutputLeavesTheWriterOpen()
    {
        IWktCodec codec = CreateCodec();
        Geometry geometry = codec.ReadWkt("POINT (1 2)");
        StringWriter writer = new StringWriter();

        codec.WriteWktToTextWriter(geometry, writer);
        writer.Write("|tail");

        Assert.Multiple(() =>
        {
            Assert.That(writer.ToString(), Is.EqualTo("POINT (1 2)|tail"));
            Assert.Throws<InvalidGeometryException>(() => codec.WriteWktToTextWriter(geometry, null!));
            Assert.Throws<InvalidGeometryException>(() => codec.WriteWkt(null!));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidConfiguration()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(null!, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(new WktReadOptions(), null!));
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(
                new WktReadOptions { MaximumCoordinates = 0 }, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(
                new WktReadOptions { MaximumComponents = 0 }, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(
                new WktReadOptions { MaximumNestingDepth = -1 }, new WktWriteOptions()));
            Assert.Throws<InvalidGeometryException>(() => new WktCodec(
                new WktReadOptions(), new WktWriteOptions { DecimalPrecision = 18 }));
        });
    }

    private static IWktCodec CreateCodec()
    {
        return new WktCodec(new WktReadOptions(), new WktWriteOptions());
    }
}
