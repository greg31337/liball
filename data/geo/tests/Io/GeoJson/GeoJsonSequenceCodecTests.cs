using geo;
using System.Text;

namespace geo.Tests;

public class GeoJsonSequenceCodecTests
{
    [Test]
    public void WritesExactRfc8142FramingAndReadsDocumentsInOrder()
    {
        IGeoJsonCodec documentCodec = CreateDocumentCodec();
        IGeoJsonSequenceCodec sequenceCodec = CreateSequenceCodec();
        GeoJsonDocument[] documents =
        [
            documentCodec.ReadGeoJson("""{"type":"Point","coordinates":[1,2]}"""),
            documentCodec.ReadGeoJson("""{"type":"Point","coordinates":[3,4]}""")
        ];
        MemoryStream stream = new MemoryStream();

        sequenceCodec.WriteGeoJsonSequence(documents, stream);
        byte[] bytes = stream.ToArray();
        string expected =
            "\u001e{\"type\":\"Point\",\"coordinates\":[1,2]}\n" +
            "\u001e{\"type\":\"Point\",\"coordinates\":[3,4]}\n";
        stream.Position = 0;
        GeoJsonReadResult[] results = sequenceCodec.ReadGeoJsonSequence(stream);
        stream.WriteByte(0xaa);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(Encoding.UTF8.GetBytes(expected)));
            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results[0].Success, Is.True, results[0].Error?.Message);
            Assert.That(results[1].Success, Is.True, results[1].Error?.Message);
            Assert.That(results[0].Document!.Geometry!.Value!.Point!.Coordinate!.X, Is.EqualTo(1.0));
            Assert.That(results[1].Document!.Geometry!.Value!.Point!.Coordinate!.X, Is.EqualTo(3.0));
            Assert.That(stream.Length, Is.EqualTo(bytes.Length + 1));
        });
    }

    [Test]
    public void IndentedRecordsCanContainLineFeedsBeforeTheirTerminators()
    {
        IGeoJsonCodec documentCodec = CreateDocumentCodec();
        IGeoJsonSequenceCodec sequenceCodec = new GeoJsonSequenceCodec(
            new GeoJsonReadOptions(), new GeoJsonWriteOptions { Indented = true });
        GeoJsonDocument document = documentCodec.ReadGeoJson(
            """{"type":"LineString","coordinates":[[0,0],[1,1]]}""");
        MemoryStream stream = new MemoryStream();

        sequenceCodec.WriteGeoJsonSequence([document], stream);
        byte[] bytes = stream.ToArray();
        stream.Position = 0;
        GeoJsonReadResult[] results = sequenceCodec.ReadGeoJsonSequence(stream);

        Assert.Multiple(() =>
        {
            Assert.That(bytes[0], Is.EqualTo(0x1e));
            Assert.That(bytes[bytes.Length - 1], Is.EqualTo((byte)'\n'));
            Assert.That(bytes.Count(value => value == (byte)'\n'), Is.GreaterThan(1));
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Success, Is.True, results[0].Error?.Message);
            Assert.That(results[0].Document!.Geometry!.Value!.LineString!.Coordinates, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void RecoversAtTheNextRecordSeparatorAfterMalformedAndUnterminatedRecords()
    {
        string first = """{"type":"Point","coordinates":[1,2]}""";
        string malformed = """{"type":"Point","coordinates":[1,]}""";
        string last = """{"type":"Point","coordinates":[5,6]}""";
        byte[] sequence = Encoding.UTF8.GetBytes(
            "\u001e" + first + "\n" +
            "\u001e" + malformed + "\n" +
            "\u001e" + first +
            "\u001e" + last + "\n");

        GeoJsonReadResult[] results = CreateSequenceCodec().ReadGeoJsonSequence(
            new MemoryStream(sequence));

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(4));
            Assert.That(results[0].Success, Is.True);
            Assert.That(results[1].Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(results[1].Error!.ComponentPath, Is.EqualTo("Record[1]"));
            Assert.That(results[2].Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(results[2].Error!.ComponentPath, Is.EqualTo("Record[2]"));
            Assert.That(results[3].Success, Is.True, results[3].Error?.Message);
            Assert.That(results[3].Document!.Geometry!.Value!.Point!.Coordinate!.X, Is.EqualTo(5.0));
        });
    }

    [Test]
    public void OversizedRecordsAreBoundedAndDoNotPreventLaterRecovery()
    {
        string point = """{"type":"Point","coordinates":[1,2]}""";
        string oversized =
            "{\"type\":\"Feature\",\"properties\":{\"value\":\"" +
            new string('x', 200) + "\"},\"geometry\":null}";
        byte[] sequence = Encoding.UTF8.GetBytes(
            "\u001e" + point + "\n\u001e" + oversized + "\n\u001e" + point + "\n");
        GeoJsonReadOptions options = new GeoJsonReadOptions { MaximumInputBytes = point.Length };
        IGeoJsonSequenceCodec codec = new GeoJsonSequenceCodec(options, new GeoJsonWriteOptions());
        options.MaximumInputBytes = 1;

        GeoJsonReadResult[] results = codec.ReadGeoJsonSequence(new MemoryStream(sequence));

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results[0].Success, Is.True, results[0].Error?.Message);
            Assert.That(results[1].Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
            Assert.That(results[1].Error!.Offset, Is.EqualTo(point.Length));
            Assert.That(results[1].Error!.ComponentPath, Is.EqualTo("Record[1]"));
            Assert.That(results[2].Success, Is.True, results[2].Error?.Message);
        });
    }

    [Test]
    public void TryReadRecordValidatesFramingBoundsAndPayload()
    {
        string point = """{"type":"Point","coordinates":[1,2]}""";
        byte[] framed = Encoding.UTF8.GetBytes("\u001e" + point + "\n");
        IGeoJsonSequenceCodec codec = CreateSequenceCodec();
        IGeoJsonSequenceCodec bounded = new GeoJsonSequenceCodec(
            new GeoJsonReadOptions { MaximumInputBytes = point.Length - 1 },
            new GeoJsonWriteOptions());

        GeoJsonReadResult valid = codec.TryReadGeoJsonRecord(framed);
        GeoJsonReadResult missingSeparator = codec.TryReadGeoJsonRecord(
            Encoding.UTF8.GetBytes(point + "\n"));
        GeoJsonReadResult missingTerminator = codec.TryReadGeoJsonRecord(
            Encoding.UTF8.GetBytes("\u001e" + point));
        GeoJsonReadResult multiple = codec.TryReadGeoJsonRecord(
            Encoding.UTF8.GetBytes("\u001e" + point + "\n\u001e" + point + "\n"));
        GeoJsonReadResult tooLarge = bounded.TryReadGeoJsonRecord(framed);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.Error?.Message);
            Assert.That(missingSeparator.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(missingTerminator.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedEnd));
            Assert.That(multiple.Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(tooLarge.Error!.Kind, Is.EqualTo(ParseErrorKind.LimitExceeded));
        });
    }

    [Test]
    public void ReportsUnframedInputAndHandlesEmptySequences()
    {
        IGeoJsonSequenceCodec codec = CreateSequenceCodec();
        GeoJsonReadResult[] unframed = codec.ReadGeoJsonSequence(
            new MemoryStream(Encoding.UTF8.GetBytes("not a sequence")));
        GeoJsonReadResult[] empty = codec.ReadGeoJsonSequence(new MemoryStream());
        MemoryStream output = new MemoryStream();

        codec.WriteGeoJsonSequence([], output);

        Assert.Multiple(() =>
        {
            Assert.That(unframed, Has.Length.EqualTo(1));
            Assert.That(unframed[0].Error!.Kind, Is.EqualTo(ParseErrorKind.UnexpectedToken));
            Assert.That(unframed[0].Error!.ComponentPath, Is.EqualTo("SequencePreamble"));
            Assert.That(empty, Is.Empty);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void RejectsInvalidArgumentsWithoutClosingWritableStreams()
    {
        IGeoJsonCodec documentCodec = CreateDocumentCodec();
        IGeoJsonSequenceCodec codec = CreateSequenceCodec();
        GeoJsonDocument document = documentCodec.ReadGeoJson(
            """{"type":"Point","coordinates":[1,2]}""");
        MemoryStream readOnly = new MemoryStream(new byte[1], false);

        Assert.Multiple(() =>
        {
            Assert.Throws<GeoJsonException>(() => codec.ReadGeoJsonSequence(null!));
            Assert.Throws<GeoJsonException>(() => codec.WriteGeoJsonSequence(null!, new MemoryStream()));
            Assert.Throws<GeoJsonException>(() => codec.WriteGeoJsonSequence([document], null!));
            Assert.Throws<GeoJsonException>(() => codec.WriteGeoJsonSequence([document], readOnly));
            Assert.Throws<GeoJsonException>(() => codec.WriteGeoJsonSequence([document, null!], new MemoryStream()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonSequenceCodec(null!, new GeoJsonWriteOptions()));
            Assert.Throws<GeoJsonException>(() => new GeoJsonSequenceCodec(new GeoJsonReadOptions(), null!));
        });
    }

    private static IGeoJsonSequenceCodec CreateSequenceCodec()
    {
        return new GeoJsonSequenceCodec(new GeoJsonReadOptions(), new GeoJsonWriteOptions());
    }

    private static IGeoJsonCodec CreateDocumentCodec()
    {
        return new GeoJsonCodec(new GeoJsonReadOptions(), new GeoJsonWriteOptions());
    }
}
