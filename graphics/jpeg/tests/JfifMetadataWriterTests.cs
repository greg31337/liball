namespace jpeg.Tests;

internal class JfifMetadataWriterTests
{
    [Test]
    public void Writer_PreservesParsedJfifPayload()
    {
        byte[] original = JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 300, 200, 1, 1);
        original[14] = 0x10;
        original[15] = 0x20;
        original[16] = 0x30;
        global::jpeg.JpegJfifMetadata metadata = new global::jpeg.JpegJfifMetadataParser().Parse(original);
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesConstructedJfifMetadataThatParsesBack()
    {
        global::jpeg.JpegJfifMetadata metadata = CreateJfifMetadata();
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegJfifMetadata parsed = new global::jpeg.JpegJfifMetadataParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0)));
        Assert.That(parsed.IsPresent, Is.True);
        Assert.That(parsed.MajorVersion, Is.EqualTo(1));
        Assert.That(parsed.MinorVersion, Is.EqualTo(2));
        Assert.That(parsed.XDensity, Is.EqualTo(72));
        Assert.That(parsed.YDensity, Is.EqualTo(96));
    }

    [Test]
    public void Writer_WritesApp0MarkerSegment()
    {
        global::jpeg.JpegJfifMetadata metadata = CreateJfifMetadata();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App0));
        Assert.That(segment.Payload, Is.EqualTo(JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0)));
    }

    [Test]
    public void Writer_WritesInlineRgbThumbnailFromRgbPixels()
    {
        global::jpeg.JpegJfifMetadata metadata = CreateJfifMetadata();
        metadata.ThumbnailWidth = 2;
        metadata.ThumbnailHeight = 1;
        metadata.ThumbnailData = new byte[0];
        metadata.HasRgbThumbnail = true;
        metadata.RgbThumbnailPixels = new byte[] { 1, 2, 3, 4, 5, 6 };
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegJfifMetadata parsed = new global::jpeg.JpegJfifMetadataParser().Parse(payload);

        Assert.That(payload.Length, Is.EqualTo(20));
        Assert.That(parsed.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(parsed.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(parsed.HasRgbThumbnail, Is.True);
        Assert.That(parsed.RgbThumbnailPixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Writer_RejectsInvalidJfifMetadata()
    {
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        global::jpeg.JpegJfifMetadata badVersion = CreateJfifMetadata();
        badVersion.MajorVersion = 256;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badVersion);
            });

        global::jpeg.JpegJfifMetadata badUnit = CreateJfifMetadata();
        badUnit.DensityUnit = 3;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badUnit);
            });

        global::jpeg.JpegJfifMetadata badDensity = CreateJfifMetadata();
        badDensity.XDensity = 65536;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badDensity);
            });

        global::jpeg.JpegJfifMetadata badThumbnailSize = CreateJfifMetadata();
        badThumbnailSize.ThumbnailWidth = 1;
        badThumbnailSize.ThumbnailHeight = 1;
        badThumbnailSize.ThumbnailData = new byte[] { 1, 2 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badThumbnailSize);
            });

        global::jpeg.JpegJfifMetadata missingRgbThumbnail = CreateJfifMetadata();
        missingRgbThumbnail.ThumbnailWidth = 1;
        missingRgbThumbnail.ThumbnailHeight = 1;
        missingRgbThumbnail.ThumbnailData = new byte[0];
        missingRgbThumbnail.RgbThumbnailPixels = new byte[] { 1, 2 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingRgbThumbnail);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, CreateJfifMetadata());
            });
    }

    public static global::jpeg.JpegJfifMetadata CreateJfifMetadata()
    {
        global::jpeg.JpegJfifMetadata metadata = new global::jpeg.JpegJfifMetadata();
        metadata.IsPresent = true;
        metadata.MajorVersion = 1;
        metadata.MinorVersion = 2;
        metadata.DensityUnit = 1;
        metadata.XDensity = 72;
        metadata.YDensity = 96;
        metadata.ThumbnailWidth = 0;
        metadata.ThumbnailHeight = 0;
        metadata.ThumbnailData = new byte[0];
        metadata.HasRgbThumbnail = false;
        metadata.RgbThumbnailPixels = new byte[0];
        return metadata;
    }
}
