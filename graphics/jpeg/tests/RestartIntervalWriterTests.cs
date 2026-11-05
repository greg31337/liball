namespace jpeg.Tests;

internal class RestartIntervalWriterTests
{
    [Test]
    public void Writer_WritesRestartIntervalPayloadThatParsesBack()
    {
        global::jpeg.JpegRestartIntervalWriter writer = new global::jpeg.JpegRestartIntervalWriter();

        byte[] payload = writer.WritePayload(300);
        int parsed = new global::jpeg.JpegRestartIntervalParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x01, 0x2C }));
        Assert.That(parsed, Is.EqualTo(300));
    }

    [Test]
    public void Writer_WritesZeroRestartInterval()
    {
        global::jpeg.JpegRestartIntervalWriter writer = new global::jpeg.JpegRestartIntervalWriter();

        byte[] payload = writer.WritePayload(0);

        Assert.That(payload, Is.EqualTo(new byte[] { 0x00, 0x00 }));
    }

    [Test]
    public void Writer_WritesRestartIntervalMarkerSegment()
    {
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegRestartIntervalWriter writer = new global::jpeg.JpegRestartIntervalWriter();

        writer.WriteMarkerSegment(segmentWriter, 4);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.DefineRestartInterval));
        Assert.That(segment.PayloadLength, Is.EqualTo(2));
        Assert.That(segment.Payload, Is.EqualTo(new byte[] { 0x00, 0x04 }));
    }

    [Test]
    public void Writer_RejectsInvalidRestartIntervals()
    {
        global::jpeg.JpegRestartIntervalWriter writer = new global::jpeg.JpegRestartIntervalWriter();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(-1);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(65536);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, 1);
            });
    }
}
