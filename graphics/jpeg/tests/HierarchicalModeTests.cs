namespace jpeg.Tests;

internal class HierarchicalModeTests
{
    [Test]
    public void SegmentReader_ReadsHierarchicalMarkersAsLengthBearingSegments()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHierarchicalProgression, new byte[] { 1, 2, 3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.ExpandReferenceComponents, new byte[] { 4, 5 });
        writer.WriteEndOfImage();
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(writer.ToArray());

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment dhp = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment exp = reader.ReadNextSegment();

        Assert.That(dhp.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineHierarchicalProgression));
        Assert.That(dhp.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerDefineHierarchicalProgression));
        Assert.That(dhp.HasLength, Is.True);
        Assert.That(dhp.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(exp.Marker, Is.EqualTo(global::jpeg.JpegMarker.ExpandReferenceComponents));
        Assert.That(exp.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerExpandReferenceComponents));
        Assert.That(exp.HasLength, Is.True);
        Assert.That(exp.Payload, Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void Identifier_ReportsUnsupportedHierarchicalMarkers()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHierarchicalProgression, new byte[] { 1, 2, 3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.ExpandReferenceComponents, new byte[] { 4, 5 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.HasHierarchicalProgression, Is.True);
        Assert.That(info.HierarchicalProgressionMarkerCount, Is.EqualTo(1));
        Assert.That(info.ExpandReferenceComponentMarkerCount, Is.EqualTo(1));
        Assert.That(info.DiagnosticCollection.WarningCount, Is.EqualTo(2));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedProcess));
        Assert.That(info.Diagnostics[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineHierarchicalProgression));
        Assert.That(info.Diagnostics[0].Message, Does.Contain("Hierarchical JPEG progression"));
        Assert.That(info.Diagnostics[1].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedProcess));
        Assert.That(info.Diagnostics[1].Marker, Is.EqualTo(global::jpeg.JpegMarker.ExpandReferenceComponents));
        Assert.That(info.Diagnostics[1].Message, Does.Contain("reference component expansion"));
    }

    [Test]
    public void Mode_RecognizesOnlyHierarchicalMarkers()
    {
        global::jpeg.JpegHierarchicalMode mode = new global::jpeg.JpegHierarchicalMode();

        Assert.That(mode.IsHierarchicalMarker(global::jpeg.JpegConstants.MarkerDefineHierarchicalProgression), Is.True);
        Assert.That(mode.IsHierarchicalMarker(global::jpeg.JpegConstants.MarkerExpandReferenceComponents), Is.True);
        Assert.That(mode.IsHierarchicalMarker(global::jpeg.JpegConstants.MarkerStartOfFrame0), Is.False);
        Assert.That(mode.GetUnsupportedMessage(global::jpeg.JpegConstants.MarkerDefineHierarchicalProgression), Does.Contain("not supported"));
        Assert.That(mode.GetUnsupportedMessage(global::jpeg.JpegConstants.MarkerExpandReferenceComponents), Does.Contain("not supported"));
    }
}
