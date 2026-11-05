namespace jpeg.Tests;

internal class ExifGeneralModelAndReplacementTests
{
    [Test]
    public void Parser_ExposesGeneralIfdsTagsAndDecodedValues()
    {
        byte[] tiff = ExifTagReaderTests.CreateCommonExifTiff();
        ExifTagReaderTests.WriteUInt16(tiff, 8, 13);
        ExifTagReaderTests.WriteEntry(tiff, 154, 0xC001, 7, 5, 170);
        ExifTagReaderTests.WriteUInt32(tiff, 166, 0);
        tiff[170] = 9;
        tiff[171] = 8;
        tiff[172] = 7;
        tiff[173] = 6;
        tiff[174] = 5;

        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(tiff));
        global::jpeg.JpegExifIfd primary = metadata.Ifds.Single(item => item.Kind == global::jpeg.JpegExifIfdKind.Primary);
        global::jpeg.JpegExifIfd exif = metadata.Ifds.Single(item => item.Kind == global::jpeg.JpegExifIfdKind.Exif);
        global::jpeg.JpegExifIfd gps = metadata.Ifds.Single(item => item.Kind == global::jpeg.JpegExifIfdKind.Gps);
        global::jpeg.JpegExifTag make = primary.Tags.Single(item => item.Identifier == 0x010F);
        global::jpeg.JpegExifTag orientation = primary.Tags.Single(item => item.Identifier == 0x0112);
        global::jpeg.JpegExifTag resolution = primary.Tags.Single(item => item.Identifier == 0x011A);
        global::jpeg.JpegExifTag undefined = primary.Tags.Single(item => item.Identifier == 0xC001);

        Assert.That(metadata.Ifds.Count, Is.EqualTo(3));
        Assert.That(primary.Offset, Is.EqualTo(8));
        Assert.That(primary.NextIfdOffset, Is.EqualTo(0));
        Assert.That(primary.Tags.Count, Is.EqualTo(13));
        Assert.That(exif.Tags.Count, Is.EqualTo(5));
        Assert.That(gps.Tags.Count, Is.EqualTo(4));
        Assert.That(make.Type, Is.EqualTo(global::jpeg.JpegExifTagType.Ascii));
        Assert.That(make.Value.Text, Is.EqualTo("CameraCo"));
        Assert.That(orientation.Type, Is.EqualTo(global::jpeg.JpegExifTagType.Short));
        Assert.That(orientation.Value.UnsignedValues, Is.EqualTo(new long[] { 6 }));
        Assert.That(resolution.Type, Is.EqualTo(global::jpeg.JpegExifTagType.Rational));
        Assert.That(resolution.Value.RationalNumerators, Is.EqualTo(new long[] { 300 }));
        Assert.That(resolution.Value.RationalDenominators, Is.EqualTo(new long[] { 1 }));
        Assert.That(resolution.Value.RationalValues, Is.EqualTo(new double[] { 300.0 }));
        Assert.That(undefined.Type, Is.EqualTo(global::jpeg.JpegExifTagType.Undefined));
        Assert.That(undefined.Value.RawBytes, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
    }

    [Test]
    public void MetadataEditor_ReplacesCommonExifFieldsAndGpsWithoutReencodingPixels()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        byte[] source = CreateJpegWithExifPayload(payload);
        global::jpeg.JpegExifMetadata metadata = global::jpeg.Jpeg.Identify(source).Metadata.ExifMetadata;
        metadata.ImageWidth = 1920;
        metadata.ImageHeight = 1080;
        metadata.Orientation = 3;
        metadata.XResolution = 150.5;
        metadata.YResolution = 151.25;
        metadata.ResolutionUnit = 2;
        metadata.Make = "NewCam";
        metadata.Model = "M2";
        metadata.Software = "jpeg-new";
        metadata.DateTime = "2026:09:01 08:30:00";
        metadata.DateTimeOriginal = "2026:09:01 08:29:00";
        metadata.ColorSpace = 65535;
        metadata.PixelWidth = 1920;
        metadata.PixelHeight = 1080;
        metadata.LensModel = "Lens 2";
        metadata.GpsLatitude = -45.25;
        metadata.GpsLongitude = 73.5;

        byte[] result = new global::jpeg.JpegMetadataEditor().ReplaceExifFields(source, metadata);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(result);

        Assert.That(info.Metadata.ExifMetadata.ImageWidth, Is.EqualTo(1920));
        Assert.That(info.Metadata.ExifMetadata.ImageHeight, Is.EqualTo(1080));
        Assert.That(info.Metadata.ExifMetadata.Orientation, Is.EqualTo(3));
        Assert.That(info.Metadata.ExifMetadata.XResolution, Is.EqualTo(150.5).Within(0.000001));
        Assert.That(info.Metadata.ExifMetadata.YResolution, Is.EqualTo(151.25).Within(0.000001));
        Assert.That(info.Metadata.ExifMetadata.Make, Is.EqualTo("NewCam"));
        Assert.That(info.Metadata.ExifMetadata.Model, Is.EqualTo("M2"));
        Assert.That(info.Metadata.ExifMetadata.Software, Is.EqualTo("jpeg-new"));
        Assert.That(info.Metadata.ExifMetadata.DateTime, Is.EqualTo("2026:09:01 08:30:00"));
        Assert.That(info.Metadata.ExifMetadata.DateTimeOriginal, Is.EqualTo("2026:09:01 08:29:00"));
        Assert.That(info.Metadata.ExifMetadata.ColorSpace, Is.EqualTo(65535));
        Assert.That(info.Metadata.ExifMetadata.PixelWidth, Is.EqualTo(1920));
        Assert.That(info.Metadata.ExifMetadata.PixelHeight, Is.EqualTo(1080));
        Assert.That(info.Metadata.ExifMetadata.LensModel, Is.EqualTo("Lens 2"));
        Assert.That(info.Metadata.ExifMetadata.GpsLatitudeRef, Is.EqualTo("S"));
        Assert.That(info.Metadata.ExifMetadata.GpsLatitude, Is.EqualTo(-45.25).Within(0.000001));
        Assert.That(info.Metadata.ExifMetadata.GpsLongitudeRef, Is.EqualTo("E"));
        Assert.That(info.Metadata.ExifMetadata.GpsLongitude, Is.EqualTo(73.5).Within(0.000001));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void MetadataEditor_ReplacesJpegRgbAndYCbCrExifThumbnails()
    {
        byte[] jpegSource = CreateJpegWithExifPayload(ExifMetadataParserTests.CreateExifPayload(
            ExifJpegThumbnailTests.CreateExifTiffWithJpegThumbnail(new byte[] { 0xFF, 0xD8, 1, 2, 0xFF, 0xD9 })));
        global::jpeg.JpegExifMetadata jpegMetadata = global::jpeg.Jpeg.Identify(jpegSource).Metadata.ExifMetadata;
        jpegMetadata.JpegThumbnailData = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        byte[] jpegResult = new global::jpeg.JpegMetadataEditor().ReplaceExifFields(jpegSource, jpegMetadata);

        byte[] rgbSource = CreateJpegWithExifPayload(ExifMetadataParserTests.CreateExifPayload(
            ExifUncompressedThumbnailTests.CreateExifTiffWithUncompressedThumbnail(2, new byte[] { 1, 2, 3, 4, 5, 6 }, 2, 1)));
        global::jpeg.JpegExifMetadata rgbMetadata = global::jpeg.Jpeg.Identify(rgbSource).Metadata.ExifMetadata;
        rgbMetadata.RgbThumbnailPixels = new byte[] { 10, 20, 30, 40, 50, 60 };
        byte[] rgbResult = new global::jpeg.JpegMetadataEditor().ReplaceExifFields(rgbSource, rgbMetadata);

        byte[] yCbCrSource = CreateJpegWithExifPayload(ExifMetadataParserTests.CreateExifPayload(
            ExifUncompressedThumbnailTests.CreateExifTiffWithUncompressedThumbnail(6, new byte[] { 80, 128, 128, 90, 129, 127 }, 2, 1)));
        global::jpeg.JpegExifMetadata yCbCrMetadata = global::jpeg.Jpeg.Identify(yCbCrSource).Metadata.ExifMetadata;
        yCbCrMetadata.YCbCrThumbnailPixels = new byte[] { 100, 120, 130, 110, 121, 131 };
        byte[] yCbCrResult = new global::jpeg.JpegMetadataEditor().ReplaceExifFields(yCbCrSource, yCbCrMetadata);

        Assert.That(global::jpeg.Jpeg.Identify(jpegResult).Metadata.ExifMetadata.JpegThumbnailData, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }));
        Assert.That(global::jpeg.Jpeg.Identify(rgbResult).Metadata.ExifMetadata.RgbThumbnailPixels, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        Assert.That(global::jpeg.Jpeg.Identify(yCbCrResult).Metadata.ExifMetadata.YCbCrThumbnailPixels, Is.EqualTo(new byte[] { 100, 120, 130, 110, 121, 131 }));
    }

    [Test]
    public void MetadataEditor_RejectsOutOfRangeGpsAndOversizedThumbnailReplacement()
    {
        byte[] commonSource = CreateJpegWithExifPayload(ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff()));
        global::jpeg.JpegExifMetadata gps = global::jpeg.Jpeg.Identify(commonSource).Metadata.ExifMetadata;
        gps.GpsLatitude = 91.0;

        byte[] thumbnailSource = CreateJpegWithExifPayload(ExifMetadataParserTests.CreateExifPayload(
            ExifJpegThumbnailTests.CreateExifTiffWithJpegThumbnail(new byte[] { 0xFF, 0xD8, 1, 2, 0xFF, 0xD9 })));
        global::jpeg.JpegExifMetadata thumbnail = global::jpeg.Jpeg.Identify(thumbnailSource).Metadata.ExifMetadata;
        thumbnail.JpegThumbnailData = new byte[] { 0xFF, 0xD8, 1, 2, 3, 4, 0xFF, 0xD9 };
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();

        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => editor.ReplaceExifFields(commonSource, gps));
        Assert.Throws<global::jpeg.JpegWriteValidationException>(() => editor.ReplaceExifFields(thumbnailSource, thumbnail));
    }

    public static byte[] CreateJpegWithExifPayload(byte[] payload)
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        source.Metadata.HasExif = true;
        source.Metadata.ExifMetadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        return new global::jpeg.JpegWriter().Write(source);
    }
}
