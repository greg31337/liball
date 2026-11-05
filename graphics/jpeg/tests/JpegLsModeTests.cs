namespace jpeg.Tests;

internal class JpegLsModeTests
{
    [Test]
    public void FrameParser_ReadsJpegLsStartOfFrameMarker()
    {
        global::jpeg.JpegFrameHeaderParser parser = new global::jpeg.JpegFrameHeaderParser();

        global::jpeg.JpegFrameHeader frame = parser.Parse(
            global::jpeg.JpegConstants.MarkerJpegLs,
            IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerJpegLs, 1));

        Assert.That(frame.Marker, Is.EqualTo(global::jpeg.JpegMarker.JpegLs));
        Assert.That(frame.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerJpegLs));
        Assert.That(frame.Process, Is.EqualTo(global::jpeg.JpegProcess.JpegLs));
        Assert.That(frame.Width, Is.EqualTo(32));
        Assert.That(frame.Height, Is.EqualTo(16));
        Assert.That(frame.ComponentCount, Is.EqualTo(1));
        Assert.That(frame.Progressive, Is.False);
        Assert.That(frame.ArithmeticCoding, Is.False);
    }

    [Test]
    public void SegmentReader_ReadsJpegLsMarkerAsLengthBearingSegment()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegLs, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerJpegLs, 1));
        writer.WriteEndOfImage();
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(writer.ToArray());

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();

        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.JpegLs));
        Assert.That(segment.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerJpegLs));
        Assert.That(segment.HasLength, Is.True);
        Assert.That(segment.PayloadLength, Is.EqualTo(9));
    }

    [Test]
    public void Identifier_ReportsUnsupportedJpegLsFrame()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegLs, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerJpegLs, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateJpegLsLikeSosPayload());
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.JpegLs));
        Assert.That(info.IsJpegLs, Is.True);
        Assert.That(info.JpegLsMarkerCount, Is.EqualTo(1));
        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedProcess));
        Assert.That(info.Diagnostics[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.JpegLs));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("JPEG-LS"));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("not supported"));
    }

    [Test]
    public void Mode_RecognizesOnlyJpegLsMarker()
    {
        global::jpeg.JpegLsMode mode = new global::jpeg.JpegLsMode();

        Assert.That(mode.IsJpegLsMarker(global::jpeg.JpegConstants.MarkerJpegLs), Is.True);
        Assert.That(mode.IsJpegLsMarker(global::jpeg.JpegConstants.MarkerStartOfFrame0), Is.False);
        Assert.That(mode.GetUnsupportedMessage(), Does.Contain("JPEG-LS"));
        Assert.That(mode.GetUnsupportedMessage(), Does.Contain("not supported"));
    }

    public static byte[] CreateJpegLsLikeSosPayload()
    {
        return new byte[] { 1, 1, 0, 0, 0, 0 };
    }
}
