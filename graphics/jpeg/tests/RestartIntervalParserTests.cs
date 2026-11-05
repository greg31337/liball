namespace jpeg.Tests;

internal class RestartIntervalParserTests
{
    [Test]
    public void Parser_ReadsRestartInterval()
    {
        global::jpeg.JpegRestartIntervalParser parser = new global::jpeg.JpegRestartIntervalParser();

        int interval = parser.Parse(new byte[] { 0x01, 0x2C });

        Assert.That(interval, Is.EqualTo(300));
    }

    [Test]
    public void Identifier_StoresRestartIntervalAndRestartMarkerState()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineRestartInterval, new byte[] { 0x00, 0x04 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.Writer.WriteByte(0x11);
        writer.WriteStandaloneMarker(global::jpeg.JpegMarker.Restart0);
        writer.Writer.WriteByte(0x22);
        writer.WriteStandaloneMarker(global::jpeg.JpegMarker.Restart1);
        writer.Writer.WriteByte(0x33);
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(4));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(2));
        Assert.That(info.LastRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(info.ExpectedRestartMarkerIndex, Is.EqualTo(2));
        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Parser_RejectsMalformedRestartIntervalPayloads()
    {
        global::jpeg.JpegRestartIntervalParser parser = new global::jpeg.JpegRestartIntervalParser();

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
    }

    [Test]
    public void Identifier_RejectsMalformedRestartIntervalSegment()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineRestartInterval, new byte[] { 0x00 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }
}
