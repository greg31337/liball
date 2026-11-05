namespace jpeg.Tests;

internal class MarkerSegmentReaderTests
{
    [Test]
    public void SegmentReader_ReadsLengthPayloadAndOffsets()
    {
        byte[] data = new byte[]
        {
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x04, 0x12, 0x34,
            0xFF, 0xFE, 0x00, 0x02
        };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        global::jpeg.JpegMarkerSegment soi = reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment app0 = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment comment = reader.ReadNextSegment();

        Assert.That(soi.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(soi.HasLength, Is.False);
        Assert.That(soi.Order, Is.EqualTo(0));
        Assert.That(soi.MarkerOffset, Is.EqualTo(0));
        Assert.That(soi.EndOffset, Is.EqualTo(2));

        Assert.That(app0.Marker, Is.EqualTo(global::jpeg.JpegMarker.App0));
        Assert.That(app0.Order, Is.EqualTo(1));
        Assert.That(app0.HasLength, Is.True);
        Assert.That(app0.MarkerOffset, Is.EqualTo(2));
        Assert.That(app0.LengthOffset, Is.EqualTo(4));
        Assert.That(app0.PayloadOffset, Is.EqualTo(6));
        Assert.That(app0.EndOffset, Is.EqualTo(8));
        Assert.That(app0.SegmentLength, Is.EqualTo(4));
        Assert.That(app0.PayloadLength, Is.EqualTo(2));
        Assert.That(app0.Payload, Is.EqualTo(new byte[] { 0x12, 0x34 }));

        Assert.That(comment.Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(comment.PayloadLength, Is.EqualTo(0));
        Assert.That(comment.Payload, Is.Empty);
        Assert.That(comment.EndOffset, Is.EqualTo(12));
    }

    [Test]
    public void SegmentReader_TreatsStandaloneMarkersAsNoLengthSegments()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment eoi = reader.ReadNextSegment();

        Assert.That(eoi.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(eoi.HasLength, Is.False);
        Assert.That(eoi.LengthOffset, Is.EqualTo(-1));
        Assert.That(eoi.PayloadOffset, Is.EqualTo(-1));
        Assert.That(eoi.SegmentLength, Is.EqualTo(0));
        Assert.That(eoi.PayloadLength, Is.EqualTo(0));
        Assert.That(eoi.EndOffset, Is.EqualTo(4));
    }

    [Test]
    public void SegmentReader_RejectsInvalidSegmentLength()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x01 };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        reader.ReadStartOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ReadNextSegment();
            });
    }

    [Test]
    public void SegmentReader_ThrowsWhenPayloadIsTruncated()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE2, 0x00, 0x04, 0x99 };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        reader.ReadStartOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ReadNextSegment();
            });
    }

    [Test]
    public void SegmentReader_ReadsSegmentsFromStream()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x03, 0xAA };
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegMarkerSegmentReader reader = global::jpeg.JpegMarkerSegmentReader.FromStream(stream);

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();

        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineQuantizationTable));
        Assert.That(segment.MarkerOffset, Is.EqualTo(2));
        Assert.That(segment.PayloadOffset, Is.EqualTo(6));
        Assert.That(segment.Payload, Is.EqualTo(new byte[] { 0xAA }));
    }

    [Test]
    public void SegmentReader_ReadsStandaloneMarkerAfterEntropyData()
    {
        byte[] data = new byte[] { 0xFF, 0xD8, 0x11, 0xFF, 0x00, 0x22, 0xFF, 0xD0 };
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(data);

        reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment restart = reader.ReadEntropyMarkerSegment();

        Assert.That(restart.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        Assert.That(restart.HasLength, Is.False);
        Assert.That(restart.FoundAfterEntropyData, Is.True);
        Assert.That(restart.EntropyBytesSkipped, Is.EqualTo(4));
        Assert.That(restart.MarkerOffset, Is.EqualTo(6));
        Assert.That(restart.EndOffset, Is.EqualTo(8));
    }
}
