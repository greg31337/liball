namespace jpeg.Tests;

internal class RawMarkerPreservationTests
{
    [Test]
    public void RawMarkerCollection_PreservesApplicationAndCommentSegments()
    {
        byte[] data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xE1, 0x00, 0x04, 1, 2,
            0xFF, 0xFE, 0x00, 0x03, 3
        };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);
        global::jpeg.JpegRawMarkerCollection collection = new global::jpeg.JpegRawMarkerCollection();

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment app1 = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment comment = reader.ReadNextSegment();

        Assert.That(collection.AddIfPreserved(app1), Is.True);
        Assert.That(collection.AddIfPreserved(comment), Is.True);

        Assert.That(collection.Markers.Count, Is.EqualTo(2));
        Assert.That(collection.TotalPayloadBytes, Is.EqualTo(3));
        Assert.That(collection.Markers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(collection.Markers[0].IsApplicationMarker, Is.True);
        Assert.That(collection.Markers[0].Payload, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(collection.Markers[1].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(collection.Markers[1].IsComment, Is.True);
        Assert.That(collection.Markers[1].Payload, Is.EqualTo(new byte[] { 3 }));
    }

    [Test]
    public void RawMarkerCollection_PreservesUnknownLengthBearingMarkers()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xF0, 0x00, 0x03, 0x77 };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);
        global::jpeg.JpegRawMarkerCollection collection = new global::jpeg.JpegRawMarkerCollection();

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment unknown = reader.ReadNextSegment();

        Assert.That(collection.AddIfPreserved(unknown), Is.True);
        Assert.That(collection.Markers.Count, Is.EqualTo(1));
        Assert.That(collection.Markers[0].Value, Is.EqualTo(0xFFF0));
        Assert.That(collection.Markers[0].Code, Is.EqualTo(0xF0));
        Assert.That(collection.Markers[0].IsUnknownMarker, Is.True);
        Assert.That(collection.Markers[0].Payload, Is.EqualTo(new byte[] { 0x77 }));
    }

    [Test]
    public void RawMarkerCollection_SkipsKnownNonMetadataSegments()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x03, 0xAA };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);
        global::jpeg.JpegRawMarkerCollection collection = new global::jpeg.JpegRawMarkerCollection();

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment dqt = reader.ReadNextSegment();

        Assert.That(collection.AddIfPreserved(dqt), Is.False);
        Assert.That(collection.Markers, Is.Empty);
        Assert.That(collection.TotalPayloadBytes, Is.EqualTo(0));
        Assert.That(collection.IsUnknownMarker(global::jpeg.JpegConstants.MarkerDefineQuantizationTable), Is.False);
        Assert.That(collection.IsUnknownMarker(0x1234), Is.False);
    }

    [Test]
    public void RawMarkerCollection_CopiesPayloadBytes()
    {
        global::jpeg.JpegMarkerSegment segment = new global::jpeg.JpegMarkerSegment();
        segment.Value = global::jpeg.JpegConstants.MarkerApp2;
        segment.Marker = global::jpeg.JpegMarker.App2;
        segment.Code = 0xE2;
        segment.Order = 4;
        segment.MarkerOffset = 9;
        segment.Payload = new byte[] { 5, 6 };

        global::jpeg.JpegRawMarkerCollection collection = new global::jpeg.JpegRawMarkerCollection();
        collection.AddIfPreserved(segment);
        segment.Payload[0] = 99;

        Assert.That(collection.Markers[0].Order, Is.EqualTo(4));
        Assert.That(collection.Markers[0].Offset, Is.EqualTo(9));
        Assert.That(collection.Markers[0].Payload, Is.EqualTo(new byte[] { 5, 6 }));
    }

    [Test]
    public void JpegMetadata_HoldsRawMarkers()
    {
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        global::jpeg.JpegRawMarker marker = new global::jpeg.JpegRawMarker();
        marker.Value = global::jpeg.JpegConstants.MarkerComment;
        marker.Marker = global::jpeg.JpegMarker.Comment;
        marker.Payload = new byte[] { 1 };
        marker.PayloadLength = 1;

        metadata.RawMarkers.Add(marker);

        Assert.That(metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
    }

    [Test]
    public void RawMarkerCollection_RejectsNullSegments()
    {
        global::jpeg.JpegRawMarkerCollection collection = new global::jpeg.JpegRawMarkerCollection();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                collection.AddIfPreserved(null!);
            });
    }
}
