using geo;
using System.IO;
using System.Text;

namespace geo.Tests;

public class GeoJsonCodecTests
{
    [Test]
    public void ReadsStringsAndUtf8SpansWithUnicodeAndSnapshottedLimits()
    {
        GeoJsonReadOptions readOptions = new GeoJsonReadOptions { MaximumCoordinates = 2 };
        IGeoJsonCodec codec = new GeoJsonCodec(readOptions, new GeoJsonWriteOptions());
        readOptions.MaximumCoordinates = 1;
        string json =
            """{"type":"Feature","id":"café-😀","properties":{"label":"東京"},"geometry":{"type":"LineString","coordinates":[[0,0],[1,1]]}}""";
        byte[] utf8 = Encoding.UTF8.GetBytes(json);

        GeoJsonDocument fromText = codec.ReadGeoJson(json);
        GeoJsonDocument fromUtf8 = codec.ReadGeoJsonUtf8(utf8);
        GeoJsonReadResult result = codec.TryReadGeoJson(json);

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Feature!.Id.GetString(), Is.EqualTo("café-😀"));
            Assert.That(fromText.Feature.Properties.GetProperty("label").GetString(), Is.EqualTo("東京"));
            Assert.That(fromText.Feature.Geometry!.Value!.LineString!.Coordinates, Has.Length.EqualTo(2));
            Assert.That(fromUtf8.Feature!.Id.GetString(), Is.EqualTo("café-😀"));
            Assert.That(result.Success, Is.True, result.Error?.Message);
            Assert.That(result.Consumed, Is.EqualTo(utf8.Length));
        });
    }

    [Test]
    public void TryReadReportsExactSemanticPathAndByteOffsetWhileReadThrowsTheMessage()
    {
        IGeoJsonCodec codec = CreateCodec();
        string json = """{"type":"Point","coordinates":[1,"bad"]}""";
        int expectedOffset = Encoding.UTF8.GetByteCount(
            json.Substring(0, json.IndexOf("\"bad\"", StringComparison.Ordinal)));

        GeoJsonReadResult result = codec.TryReadGeoJson(json);
        GeoJsonException exception = Assert.Throws<GeoJsonException>(
            () => codec.ReadGeoJson(json))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidNumber));
            Assert.That(result.Error.JsonPath, Is.EqualTo("$.coordinates[1]"));
            Assert.That(result.Error.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Consumed, Is.EqualTo(expectedOffset));
            Assert.That(result.Error.Message, Does.Contain("byte offset " + expectedOffset));
            Assert.That(exception.Message, Is.EqualTo(result.Error.Message));
        });
    }

    [Test]
    public void MissingMembersPointAtTheContainingObjectsClosingBrace()
    {
        IGeoJsonCodec codec = CreateCodec();
        string json = """{"type":"Feature","geometry":null}""";
        int expectedOffset = json.LastIndexOf('}');

        GeoJsonReadResult result = codec.TryReadGeoJson(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.InvalidGeoJson));
            Assert.That(result.Error.JsonPath, Is.EqualTo("$.properties"));
            Assert.That(result.Error.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Error.Message, Does.Contain("byte offset " + expectedOffset));
        });
    }

    [Test]
    public void TryReadReturnsPreciseSyntaxFailuresWithoutThrowing()
    {
        IGeoJsonCodec codec = CreateCodec();
        string json = """{"type":"Point","coordinates":[1,2],}""";
        int expectedOffset = json.LastIndexOf('}');

        GeoJsonReadResult result = codec.TryReadGeoJson(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(result.Error.JsonPath, Is.EqualTo("$"));
            Assert.That(result.Error.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Consumed, Is.EqualTo(expectedOffset));
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJson(json));
        });
    }

    [Test]
    public void TryReadHandlesNullAndUnpairedSurrogatesWithoutThrowing()
    {
        IGeoJsonCodec codec = CreateCodec();
        string invalid = "{\"type\":\"Point\",\"coordinates\":[1,2],\"x\":\"" + '\ud800' + "\"}";

        GeoJsonReadResult nullResult = codec.TryReadGeoJson(null!);
        GeoJsonReadResult surrogateResult = codec.TryReadGeoJson(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(nullResult.Error.Offset, Is.Zero);
            Assert.That(surrogateResult.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(surrogateResult.Error.JsonPath, Is.EqualTo("$"));
            Assert.That(surrogateResult.Error.Message, Does.Contain("unpaired high surrogate"));
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJson(null!));
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJson(invalid));
        });
    }

    [Test]
    public void Utf8ReadsRejectInvalidEncodingWithTheExactByteOffset()
    {
        IGeoJsonCodec codec = CreateCodec();
        byte[] prefix = Encoding.UTF8.GetBytes("{\"type\":\"Point\",\"coordinates\":[1,2],\"x\":\"");
        byte[] suffix = Encoding.UTF8.GetBytes("\"}");
        byte[] bytes = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(bytes, 0);
        bytes[prefix.Length] = 0xff;
        suffix.CopyTo(bytes, prefix.Length + 1);

        GeoJsonException exception = Assert.Throws<GeoJsonException>(
            () => codec.ReadGeoJsonUtf8(bytes))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("UTF-8"));
            Assert.That(exception.Message, Does.Contain("byte offset " + prefix.Length));
        });
    }

    [Test]
    public void ReadsBoundedStreamsAndLeavesThemOpen()
    {
        string json = """{"type":"Point","coordinates":[1,2]}""";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        IGeoJsonCodec codec = CreateCodec();
        MemoryStream stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        GeoJsonDocument document = codec.ReadGeoJsonStream(stream);
        stream.WriteByte(0xaa);
        IGeoJsonCodec bounded = new GeoJsonCodec(
            new GeoJsonReadOptions { MaximumInputBytes = bytes.Length - 1 },
            new GeoJsonWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.Geometry!.Value!.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(stream.Length, Is.EqualTo(bytes.Length + 1));
            Assert.Throws<GeoJsonException>(() => bounded.ReadGeoJsonStream(new MemoryStream(bytes)));
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJsonStream(null!));
        });
    }

    [Test]
    public void StringInputIsRejectedBeforeAllocatingPastTheConfiguredByteLimit()
    {
        string json = """{"type":"Point","coordinates":[1,2]}""";
        int maximum = Encoding.UTF8.GetByteCount(json) - 1;
        IGeoJsonCodec codec = new GeoJsonCodec(
            new GeoJsonReadOptions { MaximumInputBytes = maximum }, new GeoJsonWriteOptions());

        GeoJsonReadResult result = codec.TryReadGeoJson(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(result.Error.Offset, Is.EqualTo(maximum));
            Assert.That(result.Consumed, Is.EqualTo(maximum));
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJson(json));
        });
    }

    [Test]
    public void WritesStringsWithSnapshottedOptions()
    {
        GeoJsonWriteOptions writeOptions = new GeoJsonWriteOptions { CoordinatePrecision = 2 };
        IGeoJsonCodec codec = new GeoJsonCodec(new GeoJsonReadOptions(), writeOptions);
        writeOptions.CoordinatePrecision = 0;
        writeOptions.Indented = true;
        GeoJsonDocument document = codec.ReadGeoJson(
            """{"type":"Point","coordinates":[1.236,2]}""");

        string output = codec.WriteGeoJson(document);

        Assert.That(output, Is.EqualTo("{\"type\":\"Point\",\"coordinates\":[1.24,2]}"));
    }

    [Test]
    public void TryWriteReportsRequiredUtf8SizeWithoutPartialOutput()
    {
        IGeoJsonCodec codec = CreateCodec();
        GeoJsonDocument document = codec.ReadGeoJson(
            """{"type":"Feature","properties":{"name":"東京"},"geometry":null}""");
        byte[] expected = Encoding.UTF8.GetBytes(codec.WriteGeoJson(document));
        byte[] exact = new byte[expected.Length];
        byte[] small = Enumerable.Repeat((byte)0xaa, expected.Length - 1).ToArray();

        WriteResult success = codec.TryWriteGeoJsonUtf8(document, exact);
        WriteResult failure = codec.TryWriteGeoJsonUtf8(document, small);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(success.WrittenSize, Is.EqualTo(expected.Length));
            Assert.That(exact, Is.EqualTo(expected));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(failure.WrittenSize, Is.Zero);
            Assert.That(failure.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(small, Is.All.EqualTo(0xaa));
        });
    }

    [Test]
    public void StreamOutputMatchesStringOutputAndLeavesTheStreamOpen()
    {
        IGeoJsonCodec codec = CreateCodec();
        GeoJsonDocument document = codec.ReadGeoJson(
            """{"type":"Point","coordinates":[1,2]}""");
        byte[] expected = Encoding.UTF8.GetBytes(codec.WriteGeoJson(document));
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0xff);

        codec.WriteGeoJsonStream(document, stream);
        stream.WriteByte(0xee);

        Assert.Multiple(() =>
        {
            Assert.That(stream.ToArray().Skip(1).Take(expected.Length), Is.EqualTo(expected));
            Assert.That(stream.ToArray()[stream.ToArray().Length - 1], Is.EqualTo(0xee));
            Assert.Throws<GeoJsonException>(() => codec.WriteGeoJsonStream(document, null!));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidConfiguration()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(null!, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(new GeoJsonReadOptions(), null!));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(
                new GeoJsonReadOptions { MaximumCoordinates = 0 }, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(
                new GeoJsonReadOptions { MaximumComponents = 0 }, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(
                new GeoJsonReadOptions { MaximumNestingDepth = -1 }, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(
                new GeoJsonReadOptions { MaximumInputBytes = 0 }, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonCodec(
                new GeoJsonReadOptions(), new GeoJsonWriteOptions { CoordinatePrecision = 16 }));
        });
    }

    private static IGeoJsonCodec CreateCodec()
    {
        return new GeoJsonCodec(new GeoJsonReadOptions(), new GeoJsonWriteOptions());
    }
}
