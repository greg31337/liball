namespace jpeg.Tests;

internal class HuffmanTableParserTests
{
    [Test]
    public void Parser_BuildsCanonicalDecodeRanges()
    {
        byte[] payload = CreateCanonicalDcPayload();
        global::jpeg.JpegHuffmanTableParser parser = new global::jpeg.JpegHuffmanTableParser();

        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> tables = parser.Parse(payload);
        global::jpeg.JpegHuffmanTable table = tables[0];

        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(table.TableClass, Is.EqualTo(0));
        Assert.That(table.Identifier, Is.EqualTo(0));
        Assert.That(table.SymbolCount, Is.EqualTo(3));
        Assert.That(table.CodeCounts[1], Is.EqualTo(2));
        Assert.That(table.CodeCounts[2], Is.EqualTo(1));
        Assert.That(table.Codes, Is.EqualTo(new int[] { 0, 1, 4 }));
        Assert.That(table.CodeLengths, Is.EqualTo(new int[] { 2, 2, 3 }));
        Assert.That(table.MinCode[2], Is.EqualTo(0));
        Assert.That(table.MaxCode[2], Is.EqualTo(1));
        Assert.That(table.MinCode[3], Is.EqualTo(4));
        Assert.That(table.MaxCode[3], Is.EqualTo(4));
        Assert.That(parser.DecodeSymbol(table, 0, 2), Is.EqualTo(0x0A));
        Assert.That(parser.DecodeSymbol(table, 1, 2), Is.EqualTo(0x0B));
        Assert.That(parser.DecodeSymbol(table, 4, 3), Is.EqualTo(0x0C));
        Assert.That(parser.DecodeSymbol(table, 5, 3), Is.EqualTo(-1));
    }

    [Test]
    public void Parser_ReadsMultipleHuffmanTables()
    {
        byte[] first = CreateCanonicalDcPayload();
        byte[] second = CreateSingleCodePayload(0x10, 0xEE);
        byte[] combined = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, combined, 0, first.Length);
        System.Array.Copy(second, 0, combined, first.Length, second.Length);
        global::jpeg.JpegHuffmanTableParser parser = new global::jpeg.JpegHuffmanTableParser();

        System.Collections.Generic.List<global::jpeg.JpegHuffmanTable> tables = parser.Parse(combined);

        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(tables[0].TableClass, Is.EqualTo(0));
        Assert.That(tables[1].TableClass, Is.EqualTo(1));
        Assert.That(tables[1].Symbols[0], Is.EqualTo(0xEE));
    }

    [Test]
    public void Identifier_StoresParsedHuffmanTables()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, CreateCanonicalDcPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HuffmanTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTables.Count, Is.EqualTo(1));
        Assert.That(info.HuffmanTables[0].SymbolCount, Is.EqualTo(3));
        Assert.That(new global::jpeg.JpegHuffmanTableParser().DecodeSymbol(info.HuffmanTables[0], 4, 3), Is.EqualTo(0x0C));
    }

    [Test]
    public void Parser_RejectsMalformedHuffmanPayloads()
    {
        global::jpeg.JpegHuffmanTableParser parser = new global::jpeg.JpegHuffmanTableParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0, 1 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateHeaderOnlyPayload(0x20));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateHeaderOnlyPayload(0x04));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateTruncatedSymbolsPayload());
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateOversubscribedPayload());
            });
    }

    public static byte[] CreateCanonicalDcPayload()
    {
        byte[] payload = new byte[20];
        payload[0] = 0x00;
        payload[2] = 2;
        payload[3] = 1;
        payload[17] = 0x0A;
        payload[18] = 0x0B;
        payload[19] = 0x0C;
        return payload;
    }

    public static byte[] CreateSingleCodePayload(int tableInfo, int symbol)
    {
        byte[] payload = new byte[18];
        payload[0] = (byte)tableInfo;
        payload[1] = 1;
        payload[17] = (byte)symbol;
        return payload;
    }

    public static byte[] CreateHeaderOnlyPayload(int tableInfo)
    {
        byte[] payload = new byte[17];
        payload[0] = (byte)tableInfo;
        return payload;
    }

    public static byte[] CreateTruncatedSymbolsPayload()
    {
        byte[] payload = new byte[17];
        payload[0] = 0;
        payload[1] = 1;
        return payload;
    }

    public static byte[] CreateOversubscribedPayload()
    {
        byte[] payload = new byte[20];
        payload[0] = 0;
        payload[1] = 3;
        payload[17] = 1;
        payload[18] = 2;
        payload[19] = 3;
        return payload;
    }
}
