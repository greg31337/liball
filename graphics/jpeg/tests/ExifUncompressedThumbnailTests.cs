namespace jpeg.Tests;

internal class ExifUncompressedThumbnailTests
{
    [Test]
    public void Parser_ExtractsRgbThumbnailFromExifIfd1()
    {
        byte[] pixels = new byte[] { 10, 20, 30, 40, 50, 60 };
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateExifTiffWithUncompressedThumbnail(2, pixels, 2, 1));

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);

        Assert.That(metadata.HasThumbnailIfd, Is.True);
        Assert.That(metadata.HasThumbnailWidth, Is.True);
        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(metadata.HasThumbnailHeight, Is.True);
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(metadata.HasThumbnailPhotometricInterpretation, Is.True);
        Assert.That(metadata.ThumbnailPhotometricInterpretation, Is.EqualTo(2));
        Assert.That(metadata.HasThumbnailSamplesPerPixel, Is.True);
        Assert.That(metadata.ThumbnailSamplesPerPixel, Is.EqualTo(3));
        Assert.That(metadata.HasThumbnailStripOffset, Is.True);
        Assert.That(metadata.ThumbnailStripOffset, Is.EqualTo(128));
        Assert.That(metadata.HasThumbnailStripByteCount, Is.True);
        Assert.That(metadata.ThumbnailStripByteCount, Is.EqualTo(pixels.Length));
        Assert.That(metadata.HasRgbThumbnail, Is.True);
        Assert.That(metadata.RgbThumbnailPixels, Is.EqualTo(pixels));
        Assert.That(metadata.HasYCbCrThumbnail, Is.False);
    }

    [Test]
    public void Parser_ExtractsYCbCrThumbnailFromExifIfd1()
    {
        byte[] pixels = new byte[] { 80, 128, 128, 90, 129, 127 };
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateExifTiffWithUncompressedThumbnail(6, pixels, 2, 1));

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);

        Assert.That(metadata.ThumbnailPhotometricInterpretation, Is.EqualTo(6));
        Assert.That(metadata.HasYCbCrThumbnail, Is.True);
        Assert.That(metadata.YCbCrThumbnailPixels, Is.EqualTo(pixels));
        Assert.That(metadata.HasRgbThumbnail, Is.False);
    }

    [Test]
    public void Parser_IgnoresUnsupportedUncompressedThumbnailPhotometricInterpretation()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateExifTiffWithUncompressedThumbnail(1, new byte[] { 1, 2, 3 }, 1, 1));

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);

        Assert.That(metadata.HasThumbnailIfd, Is.True);
        Assert.That(metadata.HasRgbThumbnail, Is.False);
        Assert.That(metadata.HasYCbCrThumbnail, Is.False);
    }

    [Test]
    public void Parser_RejectsShortUncompressedThumbnailData()
    {
        byte[] tiff = CreateExifTiffWithUncompressedThumbnail(2, new byte[] { 1, 2, 3, 4, 5, 6 }, 2, 1);
        ExifTagReaderTests.WriteUInt32(tiff, 114, 5);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(tiff));
            });
    }

    public static byte[] CreateExifTiffWithUncompressedThumbnail(int photometricInterpretation, byte[] pixels, int width, int height)
    {
        byte[] tiff = new byte[128 + pixels.Length];
        ExifTagReaderTests.WriteTiffHeader(tiff);
        ExifTagReaderTests.WriteUInt16(tiff, 8, 0);
        ExifTagReaderTests.WriteUInt32(tiff, 10, 32);
        ExifTagReaderTests.WriteUInt16(tiff, 32, 7);
        ExifTagReaderTests.WriteEntry(tiff, 34, 0x0100, 4, 1, width);
        ExifTagReaderTests.WriteEntry(tiff, 46, 0x0101, 4, 1, height);
        ExifTagReaderTests.WriteEntry(tiff, 58, 0x0103, 3, 1, 1);
        ExifTagReaderTests.WriteEntry(tiff, 70, 0x0106, 3, 1, photometricInterpretation);
        ExifTagReaderTests.WriteEntry(tiff, 82, 0x0111, 4, 1, 128);
        ExifTagReaderTests.WriteEntry(tiff, 94, 0x0115, 3, 1, 3);
        ExifTagReaderTests.WriteEntry(tiff, 106, 0x0117, 4, 1, pixels.Length);
        if (pixels.Length > 0)
        {
            System.Array.Copy(pixels, 0, tiff, 128, pixels.Length);
        }

        return tiff;
    }
}
