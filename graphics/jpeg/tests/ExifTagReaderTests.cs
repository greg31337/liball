namespace jpeg.Tests;

internal class ExifTagReaderTests
{
    [Test]
    public void Parser_TraverseCommonExifTags()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateCommonExifTiff());
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);

        Assert.That(metadata.HasImageWidth, Is.True);
        Assert.That(metadata.ImageWidth, Is.EqualTo(4000));
        Assert.That(metadata.HasImageHeight, Is.True);
        Assert.That(metadata.ImageHeight, Is.EqualTo(3000));
        Assert.That(metadata.HasOrientation, Is.True);
        Assert.That(metadata.Orientation, Is.EqualTo(6));
        Assert.That(metadata.HasXResolution, Is.True);
        Assert.That(metadata.XResolution, Is.EqualTo(300.0));
        Assert.That(metadata.HasYResolution, Is.True);
        Assert.That(metadata.YResolution, Is.EqualTo(300.0));
        Assert.That(metadata.HasResolutionUnit, Is.True);
        Assert.That(metadata.ResolutionUnit, Is.EqualTo(2));
        Assert.That(metadata.HasDateTime, Is.True);
        Assert.That(metadata.DateTime, Is.EqualTo("2026:08:05 12:00:00"));
        Assert.That(metadata.HasMake, Is.True);
        Assert.That(metadata.Make, Is.EqualTo("CameraCo"));
        Assert.That(metadata.HasModel, Is.True);
        Assert.That(metadata.Model, Is.EqualTo("Model X"));
        Assert.That(metadata.HasSoftware, Is.True);
        Assert.That(metadata.Software, Is.EqualTo("jpeg-test"));
        Assert.That(metadata.HasExifIfd, Is.True);
        Assert.That(metadata.ExifIfdOffset, Is.EqualTo(300));
        Assert.That(metadata.HasGpsIfd, Is.True);
        Assert.That(metadata.GpsIfdOffset, Is.EqualTo(380));
        Assert.That(metadata.HasColorSpace, Is.True);
        Assert.That(metadata.ColorSpace, Is.EqualTo(1));
        Assert.That(metadata.HasPixelWidth, Is.True);
        Assert.That(metadata.PixelWidth, Is.EqualTo(4000));
        Assert.That(metadata.HasPixelHeight, Is.True);
        Assert.That(metadata.PixelHeight, Is.EqualTo(3000));
        Assert.That(metadata.HasDateTimeOriginal, Is.True);
        Assert.That(metadata.DateTimeOriginal, Is.EqualTo("2026:08:05 11:58:00"));
        Assert.That(metadata.HasLensModel, Is.True);
        Assert.That(metadata.LensModel, Is.EqualTo("50mm Prime"));
        Assert.That(metadata.GpsLatitudeRef, Is.EqualTo("N"));
        Assert.That(metadata.HasGpsLatitude, Is.True);
        Assert.That(metadata.GpsLatitude, Is.EqualTo(37.5).Within(0.000001));
        Assert.That(metadata.GpsLongitudeRef, Is.EqualTo("W"));
        Assert.That(metadata.HasGpsLongitude, Is.True);
        Assert.That(metadata.GpsLongitude, Is.EqualTo(-122.25).Within(0.000001));
    }

    [Test]
    public void Identifier_StoresTraversedExifTagsFromApp1()
    {
        byte[] payload = ExifMetadataParserTests.CreateExifPayload(CreateCommonExifTiff());
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.ExifMetadata.Orientation, Is.EqualTo(6));
        Assert.That(info.Metadata.ExifMetadata.Make, Is.EqualTo("CameraCo"));
        Assert.That(info.Metadata.ExifMetadata.LensModel, Is.EqualTo("50mm Prime"));
        Assert.That(info.Metadata.ExifMetadata.GpsLongitude, Is.EqualTo(-122.25).Within(0.000001));
    }

    [Test]
    public void Parser_RejectsMalformedCommonExifTagValues()
    {
        byte[] tiff = CreateCommonExifTiff();
        WriteUInt32(tiff, 62, 2);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                new global::jpeg.JpegExifMetadataParser().Parse(ExifMetadataParserTests.CreateExifPayload(tiff));
            });
    }

    public static byte[] CreateCommonExifTiff()
    {
        byte[] tiff = new byte[560];
        WriteTiffHeader(tiff);
        WriteIfd0(tiff);
        WriteExifIfd(tiff);
        WriteGpsIfd(tiff);
        return tiff;
    }

    public static void WriteTiffHeader(byte[] tiff)
    {
        tiff[0] = 0x49;
        tiff[1] = 0x49;
        WriteUInt16(tiff, 2, 42);
        WriteUInt32(tiff, 4, 8);
    }

    public static void WriteIfd0(byte[] tiff)
    {
        WriteUInt16(tiff, 8, 12);
        WriteEntry(tiff, 10, 0x0100, 4, 1, 4000);
        WriteEntry(tiff, 22, 0x0101, 3, 1, 3000);
        WriteEntry(tiff, 34, 0x010F, 2, 9, 180);
        WriteEntry(tiff, 46, 0x0110, 2, 8, 190);
        WriteEntry(tiff, 58, 0x0112, 3, 1, 6);
        WriteEntry(tiff, 70, 0x011A, 5, 1, 200);
        WriteEntry(tiff, 82, 0x011B, 5, 1, 208);
        WriteEntry(tiff, 94, 0x0128, 3, 1, 2);
        WriteEntry(tiff, 106, 0x0131, 2, 10, 216);
        WriteEntry(tiff, 118, 0x0132, 2, 20, 226);
        WriteEntry(tiff, 130, 0x8769, 4, 1, 300);
        WriteEntry(tiff, 142, 0x8825, 4, 1, 380);
        WriteAscii(tiff, 180, "CameraCo");
        WriteAscii(tiff, 190, "Model X");
        WriteRational(tiff, 200, 300, 1);
        WriteRational(tiff, 208, 300, 1);
        WriteAscii(tiff, 216, "jpeg-test");
        WriteAscii(tiff, 226, "2026:08:05 12:00:00");
    }

    public static void WriteExifIfd(byte[] tiff)
    {
        WriteUInt16(tiff, 300, 5);
        WriteEntry(tiff, 302, 0x9003, 2, 20, 460);
        WriteEntry(tiff, 314, 0xA001, 3, 1, 1);
        WriteEntry(tiff, 326, 0xA002, 4, 1, 4000);
        WriteEntry(tiff, 338, 0xA003, 3, 1, 3000);
        WriteEntry(tiff, 350, 0xA434, 2, 11, 482);
        WriteAscii(tiff, 460, "2026:08:05 11:58:00");
        WriteAscii(tiff, 482, "50mm Prime");
    }

    public static void WriteGpsIfd(byte[] tiff)
    {
        WriteUInt16(tiff, 380, 4);
        WriteEntry(tiff, 382, 0x0001, 2, 2, 0x4E);
        WriteEntry(tiff, 394, 0x0002, 5, 3, 500);
        WriteEntry(tiff, 406, 0x0003, 2, 2, 0x57);
        WriteEntry(tiff, 418, 0x0004, 5, 3, 524);
        WriteRational(tiff, 500, 37, 1);
        WriteRational(tiff, 508, 30, 1);
        WriteRational(tiff, 516, 0, 1);
        WriteRational(tiff, 524, 122, 1);
        WriteRational(tiff, 532, 15, 1);
        WriteRational(tiff, 540, 0, 1);
    }

    public static void WriteEntry(byte[] tiff, int offset, int tag, int type, long count, long value)
    {
        WriteUInt16(tiff, offset, tag);
        WriteUInt16(tiff, offset + 2, type);
        WriteUInt32(tiff, offset + 4, count);
        WriteUInt32(tiff, offset + 8, value);
    }

    public static void WriteAscii(byte[] tiff, int offset, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        System.Array.Copy(bytes, 0, tiff, offset, bytes.Length);
        tiff[offset + bytes.Length] = 0;
    }

    public static void WriteRational(byte[] tiff, int offset, long numerator, long denominator)
    {
        WriteUInt32(tiff, offset, numerator);
        WriteUInt32(tiff, offset + 4, denominator);
    }

    public static void WriteUInt16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
    }

    public static void WriteUInt32(byte[] data, int offset, long value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
        data[offset + 2] = (byte)((value >> 16) & 255);
        data[offset + 3] = (byte)((value >> 24) & 255);
    }
}
