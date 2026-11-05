namespace jpeg.Tests;

internal class AbbreviatedImageStreamTests
{
    [Test]
    public void Identify_UsesSuppliedTablesForAbbreviatedImageOnlyStream()
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        global::jpeg.JpegQuantizationTable quantizationTable = new global::jpeg.JpegQuantizationTableParser().Parse(IdentifierTests.CreateDqtPayload())[0];
        global::jpeg.JpegHuffmanTable huffmanTable = new global::jpeg.JpegHuffmanTableParser().Parse(IdentifierTests.CreateDhtPayload())[0];
        global::jpeg.JpegArithmeticConditioningTable arithmeticTable = new global::jpeg.JpegArithmeticConditioningTableParser().Parse(new byte[] { 0x00, 0xA3 })[0];
        options.TableStore.QuantizationTables.Add(quantizationTable);
        options.TableStore.HuffmanTables.Add(huffmanTable);
        options.TableStore.ArithmeticConditioningTables.Add(arithmeticTable);

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(IdentifierTests.CreateMinimalBaselineJpeg(), options);

        Assert.That(info.IsAbbreviatedImageStream, Is.True);
        Assert.That(info.UsedSuppliedTables, Is.True);
        Assert.That(info.IsAbbreviatedTableStream, Is.False);
        Assert.That(info.SawStartOfFrame, Is.True);
        Assert.That(info.SawStartOfScan, Is.True);
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTableCount, Is.EqualTo(1));
        Assert.That(info.QuantizationTables[0], Is.SameAs(quantizationTable));
        Assert.That(info.HuffmanTables[0], Is.SameAs(huffmanTable));
        Assert.That(info.ArithmeticConditioningTables[0], Is.SameAs(arithmeticTable));
    }

    [Test]
    public void Identify_DoesNotMarkImageOnlyStreamWithoutSuppliedTables()
    {
        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(IdentifierTests.CreateMinimalBaselineJpeg());

        Assert.That(info.IsAbbreviatedImageStream, Is.False);
        Assert.That(info.UsedSuppliedTables, Is.False);
        Assert.That(info.QuantizationTableCount, Is.EqualTo(0));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(0));
    }

    [Test]
    public void Identify_DoesNotOverrideTablesPresentInStream()
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.TableStore.QuantizationTables.Add(new global::jpeg.JpegQuantizationTableParser().Parse(IdentifierTests.CreateDqtPayload())[0]);
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, IdentifierTests.CreateDqtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.IsAbbreviatedImageStream, Is.False);
        Assert.That(info.UsedSuppliedTables, Is.False);
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.QuantizationTables.Count, Is.EqualTo(1));
    }
}
