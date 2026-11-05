namespace jpeg.Tests;

internal class CommentParserTests
{
    [Test]
    public void Parser_DecodesCommentPayloadAsLatin1()
    {
        global::jpeg.JpegCommentParser parser = new global::jpeg.JpegCommentParser();

        string comment = parser.Parse(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0xE9 });

        Assert.That(comment, Is.EqualTo("Hello \u00E9"));
    }

    [Test]
    public void Parser_DecodesEmptyCommentPayload()
    {
        global::jpeg.JpegCommentParser parser = new global::jpeg.JpegCommentParser();

        string comment = parser.Parse(new byte[0]);

        Assert.That(comment, Is.EqualTo(""));
    }

    [Test]
    public void Identifier_StoresMultipleCommentsInOrder()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, System.Text.Encoding.Latin1.GetBytes("first"));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, System.Text.Encoding.Latin1.GetBytes("second"));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.CommentCount, Is.EqualTo(2));
        Assert.That(info.Metadata.Comments.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.Comments[0], Is.EqualTo("first"));
        Assert.That(info.Metadata.Comments[1], Is.EqualTo("second"));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(info.Metadata.RawMarkers[1].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
    }

    [Test]
    public void Parser_RejectsNullCommentPayload()
    {
        global::jpeg.JpegCommentParser parser = new global::jpeg.JpegCommentParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });
    }
}
