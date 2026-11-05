using System.Collections.Generic;

namespace jpeg.Tests;

internal class CommentWriterTests
{
    [Test]
    public void Writer_EncodesCommentPayloadAsLatin1()
    {
        global::jpeg.JpegCommentWriter writer = new global::jpeg.JpegCommentWriter();

        byte[] payload = writer.WritePayload("Hello \u00E9");

        Assert.That(payload, Is.EqualTo(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0xE9 }));
        Assert.That(new global::jpeg.JpegCommentParser().Parse(payload), Is.EqualTo("Hello \u00E9"));
    }

    [Test]
    public void Writer_WritesComMarkerSegment()
    {
        global::jpeg.JpegCommentWriter writer = new global::jpeg.JpegCommentWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        writer.WriteMarkerSegment(segmentWriter, "note");

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(segment.Payload, Is.EqualTo(System.Text.Encoding.Latin1.GetBytes("note")));
    }

    [Test]
    public void Writer_WritesMultipleComMarkerSegments()
    {
        global::jpeg.JpegCommentWriter writer = new global::jpeg.JpegCommentWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        List<string> comments = new List<string>();
        comments.Add("first");
        comments.Add("second");

        writer.WriteMarkerSegments(segmentWriter, comments);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment first = reader.ReadNextSegment();
        global::jpeg.JpegMarkerSegment second = reader.ReadNextSegment();
        Assert.That(first.Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(second.Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(first.Payload, Is.EqualTo(System.Text.Encoding.Latin1.GetBytes("first")));
        Assert.That(second.Payload, Is.EqualTo(System.Text.Encoding.Latin1.GetBytes("second")));
    }

    [Test]
    public void Writer_RejectsInvalidComments()
    {
        global::jpeg.JpegCommentWriter writer = new global::jpeg.JpegCommentWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload("\u20AC");
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(new string('a', global::jpeg.JpegConstants.MaximumMarkerPayloadLength + 1));
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, "note");
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegments(new global::jpeg.JpegMarkerSegmentWriter(), null!);
            });
    }
}
