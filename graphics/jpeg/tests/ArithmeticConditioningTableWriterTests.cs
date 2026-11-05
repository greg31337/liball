namespace jpeg.Tests;

internal class ArithmeticConditioningTableWriterTests
{
    [Test]
    public void Writer_PreservesParsedDacPayloadOrder()
    {
        byte[] original = CreateDacPayload();
        global::jpeg.JpegArithmeticConditioningTable table = new global::jpeg.JpegArithmeticConditioningTableParser().Parse(original)[0];
        global::jpeg.JpegArithmeticConditioningTableWriter writer = new global::jpeg.JpegArithmeticConditioningTableWriter();

        byte[] payload = writer.WritePayload(table);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x00, 0xA3 }));
    }

    [Test]
    public void Writer_WritesConstructedTablesThatParseBack()
    {
        System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable> tables = new System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable>();
        tables.Add(CreateDcTable(2, 4, 11));
        tables.Add(CreateAcTable(3, 35));
        global::jpeg.JpegArithmeticConditioningTableWriter writer = new global::jpeg.JpegArithmeticConditioningTableWriter();

        byte[] payload = writer.WritePayloads(tables);
        System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable> parsed = new global::jpeg.JpegArithmeticConditioningTableParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x02, 0xB4, 0x13, 0x23 }));
        Assert.That(parsed.Count, Is.EqualTo(2));
        Assert.That(parsed[0].Identifier, Is.EqualTo(2));
        Assert.That(parsed[0].Lower, Is.EqualTo(4));
        Assert.That(parsed[0].Upper, Is.EqualTo(11));
        Assert.That(parsed[1].Identifier, Is.EqualTo(3));
        Assert.That(parsed[1].Kx, Is.EqualTo(35));
    }

    [Test]
    public void Writer_WritesMultipleTablesToDacMarkerSegment()
    {
        System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable> tables = new System.Collections.Generic.List<global::jpeg.JpegArithmeticConditioningTable>();
        tables.Add(CreateDcTable(0, 3, 10));
        tables.Add(CreateAcTable(1, 42));
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegArithmeticConditioningTableWriter writer = new global::jpeg.JpegArithmeticConditioningTableWriter();

        writer.WriteMarkerSegment(segmentWriter, tables);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineArithmeticConditioning));
        Assert.That(segment.PayloadLength, Is.EqualTo(4));
        Assert.That(new global::jpeg.JpegArithmeticConditioningTableParser().Parse(segment.Payload).Count, Is.EqualTo(2));
    }

    [Test]
    public void Writer_RejectsInvalidArithmeticConditioningTables()
    {
        global::jpeg.JpegArithmeticConditioningTableWriter writer = new global::jpeg.JpegArithmeticConditioningTableWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload((global::jpeg.JpegArithmeticConditioningTable)null!);
            });

        global::jpeg.JpegArithmeticConditioningTable badClass = CreateDcTable(0, 0, 0);
        badClass.TableClass = 2;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badClass);
            });

        global::jpeg.JpegArithmeticConditioningTable badIdentifier = CreateDcTable(4, 0, 0);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badIdentifier);
            });

        global::jpeg.JpegArithmeticConditioningTable badDcBounds = CreateDcTable(0, 12, 4);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badDcBounds);
            });

        global::jpeg.JpegArithmeticConditioningTable badDcValue = CreateDcTable(0, 1, 4);
        badDcValue.ConditioningValue = 0x42;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badDcValue);
            });

        global::jpeg.JpegArithmeticConditioningTable badAcZero = CreateAcTable(0, 0);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badAcZero);
            });

        global::jpeg.JpegArithmeticConditioningTable badAcLarge = CreateAcTable(0, 64);
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badAcLarge);
            });

        global::jpeg.JpegArithmeticConditioningTable badAcValue = CreateAcTable(0, 12);
        badAcValue.ConditioningValue = 13;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badAcValue);
            });
    }

    public static byte[] CreateDacPayload()
    {
        return new byte[] { 0x00, 0xA3, 0x11, 0x2A };
    }

    public static global::jpeg.JpegArithmeticConditioningTable CreateDcTable(int identifier, int lower, int upper)
    {
        global::jpeg.JpegArithmeticConditioningTable table = new global::jpeg.JpegArithmeticConditioningTable();
        table.TableClass = 0;
        table.Identifier = identifier;
        table.Lower = lower;
        table.Upper = upper;
        return table;
    }

    public static global::jpeg.JpegArithmeticConditioningTable CreateAcTable(int identifier, int kx)
    {
        global::jpeg.JpegArithmeticConditioningTable table = new global::jpeg.JpegArithmeticConditioningTable();
        table.TableClass = 1;
        table.Identifier = identifier;
        table.Kx = kx;
        return table;
    }
}
