namespace jpeg.Tests;

internal class NumberOfLinesParserTests
{
    [Test]
    public void Parser_ReadsDefinedNumberOfLines()
    {
        global::jpeg.JpegNumberOfLinesParser parser = new global::jpeg.JpegNumberOfLinesParser();

        int lineCount = parser.Parse(new byte[] { 0x00, 0x20 });

        Assert.That(lineCount, Is.EqualTo(32));
    }

    [Test]
    public void Identifier_AppliesDnlToZeroHeightFrameAndContinuesEntropyScan()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayloadWithHeight(0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.Writer.WriteByte(0x11);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineNumberOfLines, new byte[] { 0x00, 0x10 });
        writer.Writer.WriteByte(0x22);
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HasDefinedNumberOfLines, Is.True);
        Assert.That(info.DefinedNumberOfLines, Is.EqualTo(16));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.EstimatedOutputPixels, Is.EqualTo(512));
        Assert.That(info.EstimatedOutputBytes, Is.EqualTo(1536));
        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Identifier_StoresDnlWithoutChangingKnownHeight()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayloadWithHeight(16));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineNumberOfLines, new byte[] { 0x00, 0x20 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HasDefinedNumberOfLines, Is.True);
        Assert.That(info.DefinedNumberOfLines, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
    }

    [Test]
    public void Parser_RejectsMalformedNumberOfLinesPayloads()
    {
        global::jpeg.JpegNumberOfLinesParser parser = new global::jpeg.JpegNumberOfLinesParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x00 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x00, 0x01, 0x02 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x00, 0x00 });
            });
    }

    [Test]
    public void Identifier_EnforcesMaxHeightWhenDnlDefinesLateHeight()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayloadWithHeight(0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineNumberOfLines, new byte[] { 0x00, 0x10 });
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxHeight = 15;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);
            });
    }

    public static byte[] CreateSofPayloadWithHeight(int height)
    {
        byte[] payload = IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 1, height);
        return payload;
    }
}
