namespace jpeg.Tests;

internal class TolerantParserModeTests
{
    [Test]
    public void TolerantIdentify_RecordsWarningForScanBeforeFrameAndContinuesToEoi()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = CreateTolerantOptions();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.SawStartOfScan, Is.True);
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.DiagnosticCollection.Items[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.InvalidMarkerOrder));
        Assert.That(info.Diagnostics.Count, Is.EqualTo(1));
    }

    [Test]
    public void TolerantIdentify_RecordsWarningForEndOfImageBeforeScan()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = CreateTolerantOptions();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.SawStartOfFrame, Is.True);
        Assert.That(info.SawStartOfScan, Is.False);
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.DiagnosticCollection.HasWarnings, Is.True);
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
    }

    [Test]
    public void TolerantIdentify_RecordsWarningForDuplicateFrameAndContinues()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = CreateTolerantOptions();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.ComponentCount, Is.EqualTo(1));
    }

    [Test]
    public void TolerantIdentify_RecordsWarningForRestartOutsideEntropyAndContinuesInMarkerMode()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteStandaloneMarker(global::jpeg.JpegMarker.Restart0);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = CreateTolerantOptions();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.SawStartOfScan, Is.True);
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.DiagnosticCollection.Items[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
    }

    [Test]
    public void TolerantIdentify_RecordsWarningForRepeatedStartOfImage()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        global::jpeg.JpegReadOptions options = CreateTolerantOptions();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray(), options);

        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(1));
        Assert.That(info.DiagnosticCollection.Items[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
    }

    public static global::jpeg.JpegReadOptions CreateTolerantOptions()
    {
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.ErrorPolicy = global::jpeg.JpegErrorPolicy.Tolerant;
        return options;
    }
}
