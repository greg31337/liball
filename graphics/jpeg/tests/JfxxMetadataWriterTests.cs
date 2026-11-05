namespace jpeg.Tests;

internal class JfxxMetadataWriterTests
{
    [Test]
    public void Writer_PreservesParsedRgbExtensionPayload()
    {
        byte[] original = JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 1, 1, 10, 20, 30 });
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadataParser().Parse(original);
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesConstructedRgbThumbnailThatParsesBack()
    {
        global::jpeg.JpegJfxxMetadata metadata = CreateRgbJfxxMetadata();
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegJfxxMetadata parsed = new global::jpeg.JpegJfxxMetadataParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 2, 1, 1, 2, 3, 4, 5, 6 })));
        Assert.That(parsed.IsRgbThumbnail, Is.True);
        Assert.That(parsed.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(parsed.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(parsed.RgbThumbnailPixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Writer_PreservesParsedIndexedExtensionPayload()
    {
        byte[] palette = JfxxMetadataParserTests.CreatePalette();
        byte[] pixels = new byte[] { 0, 1 };
        byte[] original = JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, JfxxMetadataParserTests.CreateIndexedExtensionData(2, 1, palette, pixels));
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadataParser().Parse(original);
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_PreservesParsedJpegExtensionPayload()
    {
        byte[] thumbnail = JfxxMetadataParserTests.CreateJpegThumbnailData();
        byte[] original = JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, thumbnail);
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadataParser().Parse(original);
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);

        Assert.That(payload, Is.EqualTo(original));
    }

    [Test]
    public void Writer_WritesConstructedIndexedThumbnailThatParsesBack()
    {
        byte[] palette = JfxxMetadataParserTests.CreatePalette();
        global::jpeg.JpegJfxxMetadata metadata = CreateIndexedJfxxMetadata();
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegJfxxMetadata parsed = new global::jpeg.JpegJfxxMetadataParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, JfxxMetadataParserTests.CreateIndexedExtensionData(2, 1, palette, new byte[] { 0, 1 }))));
        Assert.That(parsed.IsIndexedThumbnail, Is.True);
        Assert.That(parsed.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(parsed.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(parsed.IndexedThumbnailPalette, Is.EqualTo(palette));
        Assert.That(parsed.IndexedThumbnailPixels, Is.EqualTo(new byte[] { 0, 1 }));
    }

    [Test]
    public void Writer_WritesConstructedJpegThumbnailThatParsesBack()
    {
        byte[] thumbnail = JfxxMetadataParserTests.CreateJpegThumbnailData();
        global::jpeg.JpegJfxxMetadata metadata = CreateJpegJfxxMetadata();
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        byte[] payload = writer.WritePayload(metadata);
        global::jpeg.JpegJfxxMetadata parsed = new global::jpeg.JpegJfxxMetadataParser().Parse(payload);

        Assert.That(payload, Is.EqualTo(JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, thumbnail)));
        Assert.That(parsed.IsJpegThumbnail, Is.True);
        Assert.That(parsed.JpegThumbnailData, Is.EqualTo(thumbnail));
    }

    [Test]
    public void Writer_WritesApp0MarkerSegment()
    {
        global::jpeg.JpegJfxxMetadata metadata = CreateRgbJfxxMetadata();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App0));
        Assert.That(segment.Payload, Is.EqualTo(JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 2, 1, 1, 2, 3, 4, 5, 6 })));
    }

    [Test]
    public void Writer_RejectsInvalidRgbThumbnailMetadata()
    {
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        global::jpeg.JpegJfxxMetadata badCode = CreateRgbJfxxMetadata();
        badCode.ExtensionCode = 0x12;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badCode);
            });

        global::jpeg.JpegJfxxMetadata badWidth = CreateRgbJfxxMetadata();
        badWidth.ThumbnailWidth = 256;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(badWidth);
            });

        global::jpeg.JpegJfxxMetadata missingPixels = CreateRgbJfxxMetadata();
        missingPixels.RgbThumbnailPixels = new byte[] { 1, 2 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingPixels);
            });

        global::jpeg.JpegJfxxMetadata nullPixels = CreateRgbJfxxMetadata();
        nullPixels.RgbThumbnailPixels = null!;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(nullPixels);
            });

        global::jpeg.JpegJfxxMetadata missingPalette = CreateIndexedJfxxMetadata();
        missingPalette.IndexedThumbnailPalette = new byte[] { 1, 2 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingPalette);
            });

        global::jpeg.JpegJfxxMetadata missingIndexedPixels = CreateIndexedJfxxMetadata();
        missingIndexedPixels.IndexedThumbnailPixels = new byte[] { 1 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingIndexedPixels);
            });

        global::jpeg.JpegJfxxMetadata missingJpegData = CreateJpegJfxxMetadata();
        missingJpegData.JpegThumbnailData = new byte[] { 0xFF, 0xD8, 0 };
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(missingJpegData);
            });

        global::jpeg.JpegJfxxMetadata nullJpegData = CreateJpegJfxxMetadata();
        nullJpegData.JpegThumbnailData = null!;
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(nullJpegData);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, CreateRgbJfxxMetadata());
            });
    }

    public static global::jpeg.JpegJfxxMetadata CreateRgbJfxxMetadata()
    {
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadata();
        metadata.IsPresent = true;
        metadata.ExtensionCode = global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail;
        metadata.IsRgbThumbnail = true;
        metadata.ThumbnailWidth = 2;
        metadata.ThumbnailHeight = 1;
        metadata.RgbThumbnailPixels = new byte[] { 1, 2, 3, 4, 5, 6 };
        metadata.ExtensionData = new byte[0];
        return metadata;
    }

    public static global::jpeg.JpegJfxxMetadata CreateIndexedJfxxMetadata()
    {
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadata();
        metadata.IsPresent = true;
        metadata.ExtensionCode = global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail;
        metadata.IsIndexedThumbnail = true;
        metadata.ThumbnailWidth = 2;
        metadata.ThumbnailHeight = 1;
        metadata.IndexedThumbnailPalette = JfxxMetadataParserTests.CreatePalette();
        metadata.IndexedThumbnailPixels = new byte[] { 0, 1 };
        metadata.ExtensionData = new byte[0];
        return metadata;
    }

    public static global::jpeg.JpegJfxxMetadata CreateJpegJfxxMetadata()
    {
        global::jpeg.JpegJfxxMetadata metadata = new global::jpeg.JpegJfxxMetadata();
        metadata.IsPresent = true;
        metadata.ExtensionCode = global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail;
        metadata.IsJpegThumbnail = true;
        metadata.JpegThumbnailData = JfxxMetadataParserTests.CreateJpegThumbnailData();
        metadata.ExtensionData = new byte[0];
        return metadata;
    }
}
