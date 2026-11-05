namespace jpeg.Tests;

internal class MarkerSegmentWriterTests
{
    [Test]
    public void SegmentWriter_WritesStandaloneAndLengthBearingSegments()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = global::jpeg.JpegMarkerSegmentWriter.WithCapacity(1);

        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, new byte[] { 0x12, 0x34 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, new byte[0]);
        writer.WriteEndOfImage();

        Assert.That(writer.Order, Is.EqualTo(4));
        Assert.That(
            writer.ToArray(),
            Is.EqualTo(new byte[]
            {
                0xFF, 0xD8,
                0xFF, 0xE0, 0x00, 0x04, 0x12, 0x34,
                0xFF, 0xFE, 0x00, 0x02,
                0xFF, 0xD9
            }));
    }

    [Test]
    public void SegmentWriter_OutputRoundTripsThroughSegmentReader()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new byte[] { 0xAA });
        writer.WriteEndOfImage();

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(writer.ToArray());
        global::jpeg.JpegMarkerSegment soi = reader.ReadStartOfImage();
        global::jpeg.JpegMarkerSegment dqt = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment eoi = reader.ReadNextSegment();

        Assert.That(soi.Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(dqt.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineQuantizationTable));
        Assert.That(dqt.SegmentLength, Is.EqualTo(3));
        Assert.That(dqt.Payload, Is.EqualTo(new byte[] { 0xAA }));
        Assert.That(eoi.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
    }

    [Test]
    public void SegmentWriter_WritesToStream()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        global::jpeg.JpegMarkerSegmentWriter writer = global::jpeg.JpegMarkerSegmentWriter.FromStream(stream);

        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, new byte[] { 1, 2, 3 });

        Assert.That(writer.Order, Is.EqualTo(2));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF, 0xEE, 0x00, 0x05, 1, 2, 3 }));
    }

    [Test]
    public void SegmentWriter_WritesExistingSegmentObjects()
    {
        global::jpeg.JpegMarkerSegment segment = new global::jpeg.JpegMarkerSegment();
        segment.Value = global::jpeg.JpegConstants.MarkerApp1;
        segment.Marker = global::jpeg.JpegMarker.App1;
        segment.HasLength = true;
        segment.Payload = new byte[] { 9, 8 };

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteSegment(segment);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0xE1, 0x00, 0x04, 9, 8 }));
    }

    [Test]
    public void SegmentWriter_RejectsOversizedPayloads()
    {
        byte[] payload = new byte[global::jpeg.JpegConstants.MaximumMarkerPayloadLength + 1];
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, payload);
            });
    }

    [Test]
    public void SegmentWriter_RejectsInvalidMarkerForms()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteMarkerSegmentValue(global::jpeg.JpegConstants.MarkerStartOfImage, new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteStandaloneMarkerValue(global::jpeg.JpegConstants.MarkerApp0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteStandaloneMarkerValue(0x00E0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteStandaloneMarkerValue(0xFF00);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteStandaloneMarkerValue(0xFFFF);
            });
    }
}
