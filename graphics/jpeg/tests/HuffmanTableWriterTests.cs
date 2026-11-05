namespace jpeg.Tests;

internal class HuffmanTableWriterTests
{
    [Test]
    public void Writer_PreservesParsedHuffmanPayloadOrder()
    {
        byte[] original = HuffmanTableParserTests.CreateCanonicalDcPayload();
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTableParser().Parse(original)[0];
        global::jpeg.JpegHuffmanTableWriter writer = new global::jpeg.JpegHuffmanTableWriter();

        byte[] payload = writer.WritePayload(table);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesConstructedTableThatParsesBack()
    {
        global::jpeg.JpegHuffmanTable table = CreateAcTable(2);
        global::jpeg.JpegHuffmanTableWriter writer = new global::jpeg.JpegHuffmanTableWriter();

        byte[] payload = writer.WritePayload(table);
        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> parsed = new global::jpeg.JpegHuffmanTableParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(20));
        Assert.That(payload[0], Is.EqualTo(0x12));
        Assert.That(parsed.Count, Is.EqualTo(1));
        Assert.That(parsed[0].TableClass, Is.EqualTo(1));
        Assert.That(parsed[0].Identifier, Is.EqualTo(2));
        Assert.That(parsed[0].Symbols, Is.EqualTo(new int[] { 0x11, 0x22, 0x33 }));
        Assert.That(parsed[0].Codes, Is.EqualTo(new int[] { 0, 1, 4 }));
    }

    [Test]
    public void Writer_WritesMultipleTablesToDhtMarkerSegment()
    {
        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> tables = new System.Collections.Generic.List<global::jpeg.JpegHuffmanTable>();
        tables.Add(new global::jpeg.JpegHuffmanTableParser().Parse(HuffmanTableParserTests.CreateCanonicalDcPayload())[0]);
        tables.Add(CreateAcTable(1));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegHuffmanTableWriter writer = new global::jpeg.JpegHuffmanTableWriter();

        writer.WriteMarkerSegment(segmentWriter, tables);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineHuffmanTable));
        Assert.That(segment.PayloadLength, Is.EqualTo(40));
        Assert.That(new global::jpeg.JpegHuffmanTableParser().Parse(segment.Payload).Count, Is.EqualTo(2));
    }

    [Test]
    public void Writer_RejectsInvalidTablesCountsAndSymbols()
    {
        global::jpeg.JpegHuffmanTableWriter writer = new global::jpeg.JpegHuffmanTableWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload((global::jpeg.JpegHuffmanTable)null!);
            });

        global::jpeg.JpegHuffmanTable badClass = CreateAcTable(0);
        badClass.TableClass = 2;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badClass);
            });

        global::jpeg.JpegHuffmanTable badIdentifier = CreateAcTable(4);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badIdentifier);
            });

        global::jpeg.JpegHuffmanTable badCounts = CreateAcTable(0);
        badCounts.CodeCounts = new int[15];
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badCounts);
            });

        global::jpeg.JpegHuffmanTable mismatchedCount = CreateAcTable(0);
        mismatchedCount.SymbolCount = 2;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(mismatchedCount);
            });

        global::jpeg.JpegHuffmanTable missingSymbol = CreateAcTable(0);
        missingSymbol.Symbols = new int[] { 1, 2 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingSymbol);
            });

        global::jpeg.JpegHuffmanTable largeSymbol = CreateAcTable(0);
        largeSymbol.Symbols[0] = 256;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(largeSymbol);
            });

        global::jpeg.JpegHuffmanTable oversubscribed = new global::jpeg.JpegHuffmanTableParser().Parse(HuffmanTableParserTests.CreateSingleCodePayload(0, 1))[0];
        oversubscribed.CodeCounts[0] = 3;
        oversubscribed.SymbolCount = 3;
        oversubscribed.Symbols = new int[] { 1, 2, 3 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(oversubscribed);
            });
    }

    public static global::jpeg.JpegHuffmanTable CreateAcTable(int identifier)
    {
        global::jpeg.JpegHuffmanTable table = new global::jpeg.JpegHuffmanTable();
        table.TableClass = 1;
        table.Identifier = identifier;
        table.CodeCounts[1] = 2;
        table.CodeCounts[2] = 1;
        table.SymbolCount = 3;
        table.Symbols = new int[] { 0x11, 0x22, 0x33 };
        return table;
    }
}
