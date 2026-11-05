namespace jpeg.Tests;

internal class JpegInspectorTests
{
    [Test]
    public void ReadMarkers_ReturnsOrderedMarkersAndEntropyBoundaries()
    {
        byte[] data = CreateJpegWithRestartMarkerInEntropy();

        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);

        Assert.That(markers.Length, Is.EqualTo(6));
        Assert.That(markers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(markers[1].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(markers[1].Payload, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(markers[2].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfFrame0));
        Assert.That(markers[3].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfScan));
        Assert.That(markers[4].Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        Assert.That(markers[4].FoundAfterEntropyData, Is.True);
        Assert.That(markers[4].EntropyBytesSkipped, Is.EqualTo(4));
        Assert.That(markers[5].Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(markers[5].FoundAfterEntropyData, Is.True);
        Assert.That(markers[5].EntropyBytesSkipped, Is.EqualTo(1));
    }

    [Test]
    public void ReadMarkers_ReadsFromStream()
    {
        byte[] data = CreateJpegWithRestartMarkerInEntropy();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(stream);

        Assert.That(markers.Length, Is.EqualTo(6));
        Assert.That(markers[0].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerStartOfImage));
        Assert.That(markers[5].Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerEndOfImage));
    }

    [Test]
    public void ReadMarkersFile_ReadsFromPath()
    {
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, CreateJpegWithRestartMarkerInEntropy());

        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkersFile(path);

        Assert.That(markers.Length, Is.EqualTo(6));
        Assert.That(markers[1].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(markers[4].Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));

        System.IO.File.Delete(path);
    }

    [Test]
    public void ReadMarkers_RejectsInvalidInputs()
    {
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadMarkers((byte[])null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadMarkers((System.IO.Stream)null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadMarkersFile(null!);
            });
    }

    [Test]
    public void ReadMarkers_RejectsNonJpegData()
    {
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadMarkers(new byte[] { 1, 2, 3 });
            });
    }

    public static byte[] CreateJpegWithRestartMarkerInEntropy()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, new byte[] { 1, 2 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteBytes(new byte[] { 0x11, 0xFF, 0x00, 0x22 });
        writer.Writer.WriteUInt16(global::jpeg.JpegConstants.MarkerRestart0);
        writer.Writer.WriteByte(0x33);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
