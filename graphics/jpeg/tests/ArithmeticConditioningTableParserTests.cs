namespace jpeg.Tests;

internal class ArithmeticConditioningTableParserTests
{
    [Test]
    public void Parser_ReadsDcAndAcConditioningTables()
    {
        byte[] payload = new byte[] { 0x00, 0xA3, 0x11, 0x2A };
        global::jpeg.JpegArithmeticConditioningTableParser parser = new global::jpeg.JpegArithmeticConditioningTableParser();

        System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable> tables = parser.Parse(payload);

        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(tables[0].TableClass, Is.EqualTo(0));
        Assert.That(tables[0].Identifier, Is.EqualTo(0));
        Assert.That(tables[0].ConditioningValue, Is.EqualTo(0xA3));
        Assert.That(tables[0].Lower, Is.EqualTo(3));
        Assert.That(tables[0].Upper, Is.EqualTo(10));
        Assert.That(tables[0].Kx, Is.EqualTo(0));
        Assert.That(tables[1].TableClass, Is.EqualTo(1));
        Assert.That(tables[1].Identifier, Is.EqualTo(1));
        Assert.That(tables[1].ConditioningValue, Is.EqualTo(0x2A));
        Assert.That(tables[1].Lower, Is.EqualTo(0));
        Assert.That(tables[1].Upper, Is.EqualTo(0));
        Assert.That(tables[1].Kx, Is.EqualTo(42));
    }

    [Test]
    public void Identifier_StoresParsedArithmeticConditioningTables()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xA3, 0x11, 0x2A });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame10, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame10, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.ArithmeticCoding, Is.True);
        Assert.That(info.ArithmeticConditioningTableCount, Is.EqualTo(2));
        Assert.That(info.ArithmeticConditioningTables.Count, Is.EqualTo(2));
        Assert.That(info.ArithmeticConditioningTables[0].Upper, Is.EqualTo(10));
        Assert.That(info.ArithmeticConditioningTables[1].Kx, Is.EqualTo(42));
    }

    [Test]
    public void Parser_RejectsMalformedArithmeticConditioningPayloads()
    {
        global::jpeg.JpegArithmeticConditioningTableParser parser = new global::jpeg.JpegArithmeticConditioningTableParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x00 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x20, 0x11 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x04, 0x11 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x00, 0x23 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x10, 0x00 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x10, 0x40 });
            });
    }
}
