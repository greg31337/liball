namespace jpeg.Tests;

internal class QuantizationTableWriterTests
{
    [Test]
    public void Writer_WritesEightBitNaturalOrderTableThatParsesBack()
    {
        global::jpeg.JpegQuantizationTable table = CreateNaturalOrderTable(0, 8, 1);
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        byte[] payload = writer.WritePayload(table);
        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> parsed = new global::jpeg.JpegQuantizationTableParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(65));
        Assert.That(payload[0], Is.EqualTo(0));
        Assert.That(parsed.Count, Is.EqualTo(1));
        Assert.That(parsed[0].Identifier, Is.EqualTo(0));
        Assert.That(parsed[0].Precision, Is.EqualTo(8));
        Assert.That(parsed[0].Values, Is.EqualTo(table.Values));
    }

    [Test]
    public void Writer_PreservesParsedZigZagPayloadOrder()
    {
        byte[] original = QuantizationTableParserTests.CreateEightBitTablePayload(2);
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTableParser().Parse(original)[0];
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        byte[] payload = writer.WritePayload(table);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesSixteenBitTableThatParsesBack()
    {
        global::jpeg.JpegQuantizationTable table = CreateNaturalOrderTable(1, 16, 1000);
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        byte[] payload = writer.WritePayload(table);
        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> parsed = new global::jpeg.JpegQuantizationTableParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(129));
        Assert.That(payload[0], Is.EqualTo(0x11));
        Assert.That(parsed[0].Identifier, Is.EqualTo(1));
        Assert.That(parsed[0].Precision, Is.EqualTo(16));
        Assert.That(parsed[0].Values, Is.EqualTo(table.Values));
    }

    [Test]
    public void Writer_WritesMultipleTablesToDqtMarkerSegment()
    {
        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> tables = new System.Collections.Generic.List<global::jpeg.JpegQuantizationTable>();
        tables.Add(CreateNaturalOrderTable(0, 8, 1));
        tables.Add(CreateNaturalOrderTable(1, 8, 2));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        writer.WriteMarkerSegment(segmentWriter, tables);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineQuantizationTable));
        Assert.That(segment.PayloadLength, Is.EqualTo(130));
        Assert.That(new global::jpeg.JpegQuantizationTableParser().Parse(segment.Payload).Count, Is.EqualTo(2));
    }

    [Test]
    public void Writer_RejectsInvalidTablesAndValues()
    {
        global::jpeg.JpegQuantizationTableWriter writer = new global::jpeg.JpegQuantizationTableWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload((global::jpeg.JpegQuantizationTable)null!);
            });

        global::jpeg.JpegQuantizationTable badIdentifier = CreateNaturalOrderTable(4, 8, 1);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badIdentifier);
            });

        global::jpeg.JpegQuantizationTable badPrecision = CreateNaturalOrderTable(0, 12, 1);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badPrecision);
            });

        global::jpeg.JpegQuantizationTable zeroValue = CreateNaturalOrderTable(0, 8, 1);
        zeroValue.Values[0] = 0;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(zeroValue);
            });

        global::jpeg.JpegQuantizationTable tooLarge = CreateNaturalOrderTable(0, 8, 1);
        tooLarge.Values[0] = 256;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(tooLarge);
            });
    }

    public static global::jpeg.JpegQuantizationTable CreateNaturalOrderTable(int identifier, int precision, int firstValue)
    {
        global::jpeg.JpegQuantizationTable table = new global::jpeg.JpegQuantizationTable();
        table.Identifier = identifier;
        table.Precision = precision;
        table.Is16Bit = precision == 16;
        int index = 0;
        while (index < 64)
        {
            table.Values[index] = firstValue + index;
            index = index + 1;
        }

        return table;
    }
}
