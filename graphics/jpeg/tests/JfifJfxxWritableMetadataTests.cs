namespace jpeg.Tests;

internal class JfifJfxxWritableMetadataTests
{
    [Test]
    public void PublicWriter_WritesCompleteJfifAndJfxxModels()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        global::jpeg.JpegJfifMetadata jfif = JfifMetadataWriterTests.CreateJfifMetadata();
        jfif.MajorVersion = 1;
        jfif.MinorVersion = 2;
        jfif.DensityUnit = 2;
        jfif.XDensity = 118;
        jfif.YDensity = 79;
        jfif.ThumbnailWidth = 1;
        jfif.ThumbnailHeight = 1;
        jfif.HasRgbThumbnail = true;
        jfif.RgbThumbnailPixels = new byte[] { 10, 20, 30 };
        source.Metadata.HasJfif = true;
        source.Metadata.Jfif = jfif;
        source.Metadata.HasJfxx = true;
        source.Metadata.Jfxx.Add(JfxxMetadataWriterTests.CreateJpegJfxxMetadata());
        source.Metadata.Jfxx.Add(JfxxMetadataWriterTests.CreateIndexedJfxxMetadata());
        source.Metadata.Jfxx.Add(JfxxMetadataWriterTests.CreateRgbJfxxMetadata());

        byte[] data = new global::jpeg.JpegWriter().Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.Jfif.MajorVersion, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfif.MinorVersion, Is.EqualTo(2));
        Assert.That(info.Metadata.Jfif.DensityUnit, Is.EqualTo(2));
        Assert.That(info.Metadata.Jfif.XDensity, Is.EqualTo(118));
        Assert.That(info.Metadata.Jfif.YDensity, Is.EqualTo(79));
        Assert.That(info.Metadata.Jfif.HasDensityDpi, Is.True);
        Assert.That(info.Metadata.Jfif.RgbThumbnailPixels, Is.EqualTo(new byte[] { 10, 20, 30 }));
        Assert.That(info.Metadata.Jfxx.Count, Is.EqualTo(3));
        Assert.That(info.Metadata.Jfxx[0].IsJpegThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[1].IsIndexedThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[1].IndexedThumbnailPixels, Is.EqualTo(new byte[] { 0, 1 }));
        Assert.That(info.Metadata.Jfxx[2].IsRgbThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[2].RgbThumbnailPixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Writers_UseEditedTypedThumbnailFieldsInsteadOfStaleParsedBuffers()
    {
        byte[] jfifPayload = JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 1, 1);
        jfifPayload[14] = 1;
        jfifPayload[15] = 2;
        jfifPayload[16] = 3;
        global::jpeg.JpegJfifMetadata jfif = new global::jpeg.JpegJfifMetadataParser().Parse(jfifPayload);
        jfif.RgbThumbnailPixels = new byte[] { 4, 5, 6 };

        byte[] rgbPayload = JfxxMetadataParserTests.CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 1, 1, 10, 20, 30 });
        global::jpeg.JpegJfxxMetadata rgb = new global::jpeg.JpegJfxxMetadataParser().Parse(rgbPayload);
        rgb.RgbThumbnailPixels = new byte[] { 40, 50, 60 };

        byte[] palette = JfxxMetadataParserTests.CreatePalette();
        byte[] indexedPayload = JfxxMetadataParserTests.CreateJfxxPayload(
            global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail,
            JfxxMetadataParserTests.CreateIndexedExtensionData(1, 1, palette, new byte[] { 0 }));
        global::jpeg.JpegJfxxMetadata indexed = new global::jpeg.JpegJfxxMetadataParser().Parse(indexedPayload);
        indexed.IndexedThumbnailPalette[0] = 99;
        indexed.IndexedThumbnailPixels[0] = 7;

        byte[] jpegPayload = JfxxMetadataParserTests.CreateJfxxPayload(
            global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail,
            JfxxMetadataParserTests.CreateJpegThumbnailData());
        global::jpeg.JpegJfxxMetadata jpeg = new global::jpeg.JpegJfxxMetadataParser().Parse(jpegPayload);
        jpeg.JpegThumbnailData = new byte[] { 0xFF, 0xD8, 9, 0xFF, 0xD9 };

        global::jpeg.JpegJfifMetadata reparsedJfif = new global::jpeg.JpegJfifMetadataParser().Parse(new global::jpeg.JpegJfifMetadataWriter().WritePayload(jfif));
        global::jpeg.JpegJfxxMetadata reparsedRgb = new global::jpeg.JpegJfxxMetadataParser().Parse(new global::jpeg.JpegJfxxMetadataWriter().WritePayload(rgb));
        global::jpeg.JpegJfxxMetadata reparsedIndexed = new global::jpeg.JpegJfxxMetadataParser().Parse(new global::jpeg.JpegJfxxMetadataWriter().WritePayload(indexed));
        global::jpeg.JpegJfxxMetadata reparsedJpeg = new global::jpeg.JpegJfxxMetadataParser().Parse(new global::jpeg.JpegJfxxMetadataWriter().WritePayload(jpeg));

        Assert.That(reparsedJfif.RgbThumbnailPixels, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(reparsedRgb.RgbThumbnailPixels, Is.EqualTo(new byte[] { 40, 50, 60 }));
        Assert.That(reparsedIndexed.IndexedThumbnailPalette[0], Is.EqualTo(99));
        Assert.That(reparsedIndexed.IndexedThumbnailPixels, Is.EqualTo(new byte[] { 7 }));
        Assert.That(reparsedJpeg.JpegThumbnailData, Is.EqualTo(new byte[] { 0xFF, 0xD8, 9, 0xFF, 0xD9 }));
    }

    [Test]
    public void JfifWriter_EnforcesExactThumbnailShapeAndApp0SizeLimit()
    {
        global::jpeg.JpegJfifMetadata valid = CreateJfifThumbnail(255, 85);
        global::jpeg.JpegJfifMetadata oversized = CreateJfifThumbnail(255, 86);
        global::jpeg.JpegJfifMetadata extraPixelByte = CreateJfifThumbnail(1, 1);
        extraPixelByte.RgbThumbnailPixels = new byte[] { 1, 2, 3, 4 };
        global::jpeg.JpegJfifMetadataWriter writer = new global::jpeg.JpegJfifMetadataWriter();

        byte[] payload = writer.WritePayload(valid);

        Assert.That(payload.Length, Is.LessThanOrEqualTo(global::jpeg.JpegConstants.MaximumMarkerPayloadLength));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(oversized));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(extraPixelByte));
    }

    [Test]
    public void JfxxWriter_EnforcesEveryThumbnailMarkerSizeLimit()
    {
        global::jpeg.JpegJfxxMetadata validRgb = CreateRgbThumbnail(255, 85);
        global::jpeg.JpegJfxxMetadata oversizedRgb = CreateRgbThumbnail(255, 86);
        global::jpeg.JpegJfxxMetadata validIndexed = CreateIndexedThumbnail(253, 255);
        global::jpeg.JpegJfxxMetadata oversizedIndexed = CreateIndexedThumbnail(254, 255);
        global::jpeg.JpegJfxxMetadata validJpeg = CreateJpegThumbnail(global::jpeg.JpegConstants.MaximumMarkerPayloadLength - 6);
        global::jpeg.JpegJfxxMetadata oversizedJpeg = CreateJpegThumbnail(global::jpeg.JpegConstants.MaximumMarkerPayloadLength - 5);
        global::jpeg.JpegJfxxMetadata extraRgbByte = CreateRgbThumbnail(1, 1);
        extraRgbByte.RgbThumbnailPixels = new byte[] { 1, 2, 3, 4 };
        global::jpeg.JpegJfxxMetadata extraPaletteByte = CreateIndexedThumbnail(1, 1);
        extraPaletteByte.IndexedThumbnailPalette = new byte[769];
        global::jpeg.JpegJfxxMetadataWriter writer = new global::jpeg.JpegJfxxMetadataWriter();

        Assert.That(writer.WritePayload(validRgb).Length, Is.LessThanOrEqualTo(global::jpeg.JpegConstants.MaximumMarkerPayloadLength));
        Assert.That(writer.WritePayload(validIndexed).Length, Is.LessThanOrEqualTo(global::jpeg.JpegConstants.MaximumMarkerPayloadLength));
        Assert.That(writer.WritePayload(validJpeg).Length, Is.EqualTo(global::jpeg.JpegConstants.MaximumMarkerPayloadLength));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(oversizedRgb));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(oversizedIndexed));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(oversizedJpeg));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(extraRgbByte));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => writer.WritePayload(extraPaletteByte));
    }

    public static global::jpeg.JpegJfifMetadata CreateJfifThumbnail(int width, int height)
    {
        global::jpeg.JpegJfifMetadata metadata = JfifMetadataWriterTests.CreateJfifMetadata();
        metadata.ThumbnailWidth = width;
        metadata.ThumbnailHeight = height;
        metadata.HasRgbThumbnail = true;
        metadata.RgbThumbnailPixels = new byte[width * height * 3];
        return metadata;
    }

    public static global::jpeg.JpegJfxxMetadata CreateRgbThumbnail(int width, int height)
    {
        global::jpeg.JpegJfxxMetadata metadata = JfxxMetadataWriterTests.CreateRgbJfxxMetadata();
        metadata.ThumbnailWidth = width;
        metadata.ThumbnailHeight = height;
        metadata.RgbThumbnailPixels = new byte[width * height * 3];
        return metadata;
    }

    public static global::jpeg.JpegJfxxMetadata CreateIndexedThumbnail(int width, int height)
    {
        global::jpeg.JpegJfxxMetadata metadata = JfxxMetadataWriterTests.CreateIndexedJfxxMetadata();
        metadata.ThumbnailWidth = width;
        metadata.ThumbnailHeight = height;
        metadata.IndexedThumbnailPixels = new byte[width * height];
        return metadata;
    }

    public static global::jpeg.JpegJfxxMetadata CreateJpegThumbnail(int length)
    {
        global::jpeg.JpegJfxxMetadata metadata = JfxxMetadataWriterTests.CreateJpegJfxxMetadata();
        metadata.JpegThumbnailData = new byte[length];
        metadata.JpegThumbnailData[0] = 0xFF;
        metadata.JpegThumbnailData[1] = 0xD8;
        metadata.JpegThumbnailData[length - 2] = 0xFF;
        metadata.JpegThumbnailData[length - 1] = 0xD9;
        return metadata;
    }
}
