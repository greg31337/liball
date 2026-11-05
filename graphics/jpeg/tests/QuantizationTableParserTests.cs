namespace jpeg.Tests;

internal class QuantizationTableParserTests
{
    [Test]
    public void Parser_ReadsEightBitQuantizationTable()
    {
        byte[] payload = CreateEightBitTablePayload(2);
        global::jpeg.JpegQuantizationTableParser parser = new global::jpeg.JpegQuantizationTableParser();

        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> tables = parser.Parse(payload);

        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(tables[0].Identifier, Is.EqualTo(2));
        Assert.That(tables[0].Precision, Is.EqualTo(8));
        Assert.That(tables[0].Is16Bit, Is.False);
        Assert.That(tables[0].ZigZagValues[0], Is.EqualTo(1));
        Assert.That(tables[0].ZigZagValues[2], Is.EqualTo(3));
        Assert.That(tables[0].Values[0], Is.EqualTo(1));
        Assert.That(tables[0].Values[1], Is.EqualTo(2));
        Assert.That(tables[0].Values[8], Is.EqualTo(3));
    }

    [Test]
    public void Parser_ReadsSixteenBitQuantizationTable()
    {
        byte[] payload = CreateSixteenBitTablePayload(1);
        global::jpeg.JpegQuantizationTableParser parser = new global::jpeg.JpegQuantizationTableParser();

        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> tables = parser.Parse(payload);

        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(tables[0].Identifier, Is.EqualTo(1));
        Assert.That(tables[0].Precision, Is.EqualTo(16));
        Assert.That(tables[0].Is16Bit, Is.True);
        Assert.That(tables[0].ZigZagValues[0], Is.EqualTo(256));
        Assert.That(tables[0].ZigZagValues[63], Is.EqualTo(319));
        Assert.That(tables[0].Values[0], Is.EqualTo(256));
        Assert.That(tables[0].Values[63], Is.EqualTo(319));
    }

    [Test]
    public void Parser_ReadsMultipleTablesFromOnePayload()
    {
        byte[] first = CreateEightBitTablePayload(0);
        byte[] second = CreateSixteenBitTablePayload(3);
        byte[] combined = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, combined, 0, first.Length);
        System.Array.Copy(second, 0, combined, first.Length, second.Length);
        global::jpeg.JpegQuantizationTableParser parser = new global::jpeg.JpegQuantizationTableParser();

        System.Collections.Generic.List<global::jpeg.JpegQuantizationTable> tables = parser.Parse(combined);

        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(tables[0].Identifier, Is.EqualTo(0));
        Assert.That(tables[1].Identifier, Is.EqualTo(3));
        Assert.That(tables[1].Precision, Is.EqualTo(16));
    }

    [Test]
    public void Identifier_StoresParsedQuantizationTables()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, CreateEightBitTablePayload(0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(info.QuantizationTables[0].Identifier, Is.EqualTo(0));
        Assert.That(info.QuantizationTables[0].Values[8], Is.EqualTo(3));
    }

    [Test]
    public void Parser_RejectsMalformedQuantizationPayloads()
    {
        global::jpeg.JpegQuantizationTableParser parser = new global::jpeg.JpegQuantizationTableParser();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x20 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x04 });
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });
    }

    public static byte[] CreateEightBitTablePayload(int identifier)
    {
        byte[] payload = new byte[65];
        payload[0] = (byte)identifier;
        int index = 0;
        while (index < 64)
        {
            payload[index + 1] = (byte)(index + 1);
            index = index + 1;
        }

        return payload;
    }

    public static byte[] CreateSixteenBitTablePayload(int identifier)
    {
        byte[] payload = new byte[129];
        payload[0] = (byte)(16 + identifier);
        int index = 0;
        int offset = 1;
        while (index < 64)
        {
            global::jpeg.JpegByteWriter.WriteUInt16(payload, offset, 256 + index);
            offset = offset + 2;
            index = index + 1;
        }

        return payload;
    }
}
