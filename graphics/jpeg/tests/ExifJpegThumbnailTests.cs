namespace jpeg.Tests;

internal class ExifJpegThumbnailTests
{
    [Test]
    public void Parser_ExtractsJpegThumbnailFromExifIfd1()
    {
        byte[] thumbnail = new byte[] { 0xFF, 0xD8, 1, 2, 0xFF, 0xD9 };
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateExifTiffWithJpegThumbnail(thumbnail));

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);

        Assert.That(metadata.HasThumbnailIfd, Is.True);
        Assert.That(metadata.ThumbnailIfdOffset, Is.EqualTo(32));
        Assert.That(metadata.HasThumbnailCompression, Is.True);
        Assert.That(metadata.ThumbnailCompression, Is.EqualTo(6));
        Assert.That(metadata.HasJpegThumbnailOffset, Is.True);
        Assert.That(metadata.JpegThumbnailOffset, Is.EqualTo(80));
        Assert.That(metadata.HasJpegThumbnailLength, Is.True);
        Assert.That(metadata.JpegThumbnailLength, Is.EqualTo(thumbnail.Length));
        Assert.That(metadata.HasJpegThumbnail, Is.True);
        Assert.That(metadata.JpegThumbnailData, Is.EqualTo(thumbnail));
    }

    [Test]
    public void Identifier_StoresExifJpegThumbnailFromApp1()
    {
        byte[] thumbnail = new byte[] { 0xFF, 0xD8, 7, 8, 0xFF, 0xD9 };
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateExifTiffWithJpegThumbnail(thumbnail));
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.ExifMetadata.HasJpegThumbnail, Is.True);
        Assert.That(info.Metadata.ExifMetadata.JpegThumbnailData, Is.EqualTo(thumbnail));
    }

    [Test]
    public void Parser_IgnoresNonJpegCompressedThumbnailIfd()
    {
        byte[] tiff = CreateExifTiffWithJpegThumbnail(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        ExifTagReaderTests.WriteUInt32(tiff, 42, 1);

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(tiff));

        Assert.That(metadata.HasThumbnailIfd, Is.True);
        Assert.That(metadata.HasJpegThumbnail, Is.False);
        Assert.That(metadata.JpegThumbnailData, Is.EqualTo(new byte[0]));
    }

    [Test]
    public void Parser_RejectsOutOfRangeJpegThumbnail()
    {
        byte[] tiff = CreateExifTiffWithJpegThumbnail(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        ExifTagReaderTests.WriteUInt32(tiff, 66, 1000);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(tiff));
            });
    }

    public static byte[] CreateExifTiffWithJpegThumbnail(byte[] thumbnail)
    {
        byte[] tiff = new byte[80 + thumbnail.Length];
        ExifTagReaderTests.WriteTiffHeader(tiff);
        ExifTagReaderTests.WriteUInt16(tiff, 8, 0);
        ExifTagReaderTests.WriteUInt32(tiff, 10, 32);
        ExifTagReaderTests.WriteUInt16(tiff, 32, 3);
        ExifTagReaderTests.WriteEntry(tiff, 34, 0x0103, 3, 1, 6);
        ExifTagReaderTests.WriteEntry(tiff, 46, 0x0201, 4, 1, 80);
        ExifTagReaderTests.WriteEntry(tiff, 58, 0x0202, 4, 1, thumbnail.Length);
        if (thumbnail.Length > 0)
        {
            System.Array.Copy(thumbnail, 0, tiff, 80, thumbnail.Length);
        }

        return tiff;
    }
}
