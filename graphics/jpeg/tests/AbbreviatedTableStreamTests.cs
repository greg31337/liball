namespace jpeg.Tests;

internal class AbbreviatedTableStreamTests
{
    [Test]
    public void Identify_AcceptsAbbreviatedTableOnlyStream()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, IdentifierTests.CreateDqtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, IdentifierTests.CreateDhtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineArithmeticConditioning, new byte[] { 0x00, 0xA3 });
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.IsAbbreviatedTableStream, Is.True);
        Assert.That(info.SawStartOfImage, Is.True);
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.SawStartOfFrame, Is.False);
        Assert.That(info.SawStartOfScan, Is.False);
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(1));
        Assert.That(info.ArithmeticConditioningTableCount, Is.EqualTo(1));
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(0));
    }

    [Test]
    public void Identify_AcceptsTableOnlyStreamWithOnlyQuantizationTables()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, IdentifierTests.CreateDqtPayload());
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.IsAbbreviatedTableStream, Is.True);
        Assert.That(info.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(info.ScanCount, Is.EqualTo(0));
        Assert.That(info.ComponentCount, Is.EqualTo(0));
    }

    [Test]
    public void Identify_StillRejectsEmptyStreamEndingBeforeImageData()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void Identify_StillRejectsFrameOnlyStreamEndingBeforeScan()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, IdentifierTests.CreateDqtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }
}
