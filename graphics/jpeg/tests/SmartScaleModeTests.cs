namespace jpeg.Tests;

internal class SmartScaleModeTests
{
    [Test]
    public void SegmentReader_ReadsJpegExtensionMarkerAsLengthBearingSegment()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegExtension, new byte[] { 1, 2, 3, 4 });
        writer.WriteEndOfImage();
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(writer.ToArray());

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();

        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.JpegExtension));
        Assert.That(segment.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerJpegExtension));
        Assert.That(segment.HasLength, Is.True);
        Assert.That(segment.Payload, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Identifier_ReportsUnsupportedSmartScaleMarker()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.JpegExtension, new byte[] { 0, 8 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HasSmartScale, Is.True);
        Assert.That(info.SmartScaleMarkerCount, Is.EqualTo(1));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedProcess));
        Assert.That(info.Diagnostics[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.JpegExtension));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("SmartScale"));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("not supported"));
    }

    [Test]
    public void Mode_RecognizesOnlyJpegExtensionMarker()
    {
        global::jpeg.JpegSmartScaleMode mode = new global::jpeg.JpegSmartScaleMode();

        Assert.That(mode.IsSmartScaleMarker(global::jpeg.JpegConstants.MarkerJpegExtension), Is.True);
        Assert.That(mode.IsSmartScaleMarker(global::jpeg.JpegConstants.MarkerStartOfFrame0), Is.False);
        Assert.That(mode.GetUnsupportedMessage(), Does.Contain("SmartScale"));
        Assert.That(mode.GetUnsupportedMessage(), Does.Contain("not supported"));
    }
}
