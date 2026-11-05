namespace jpeg.Tests;

internal class JfifMetadataParserTests
{
    [Test]
    public void Parser_ReadsJfifApp0Metadata()
    {
        byte[] payload = CreateJfifPayload(1, 2, 1, 300, 200, 0, 0);
        global::jpeg.JpegJfifMetadataParser parser = new global::jpeg.JpegJfifMetadataParser();

        global::jpeg.JpegJfifMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.MajorVersion, Is.EqualTo(1));
        Assert.That(metadata.MinorVersion, Is.EqualTo(2));
        Assert.That(metadata.DensityUnit, Is.EqualTo(1));
        Assert.That(metadata.XDensity, Is.EqualTo(300));
        Assert.That(metadata.YDensity, Is.EqualTo(200));
        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(0));
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(0));
        Assert.That(metadata.ThumbnailData, Is.Empty);
        Assert.That(metadata.HasRgbThumbnail, Is.False);
        Assert.That(metadata.RgbThumbnailPixels, Is.Empty);
    }

    [Test]
    public void Parser_PreservesInlineThumbnailBytesAsRawData()
    {
        byte[] payload = CreateJfifPayload(1, 1, 0, 1, 1, 1, 1);
        payload[14] = 0x10;
        payload[15] = 0x20;
        payload[16] = 0x30;
        global::jpeg.JpegJfifMetadataParser parser = new global::jpeg.JpegJfifMetadataParser();

        global::jpeg.JpegJfifMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(1));
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(metadata.ThumbnailData, Is.EqualTo(new byte[] { 0x10, 0x20, 0x30 }));
        Assert.That(metadata.HasRgbThumbnail, Is.True);
        Assert.That(metadata.RgbThumbnailPixels, Is.EqualTo(new byte[] { 0x10, 0x20, 0x30 }));
    }

    [Test]
    public void Parser_ReadsInlineRgbThumbnailPixels()
    {
        byte[] payload = CreateJfifPayload(1, 2, 1, 72, 72, 2, 1);
        payload[14] = 0x01;
        payload[15] = 0x02;
        payload[16] = 0x03;
        payload[17] = 0x04;
        payload[18] = 0x05;
        payload[19] = 0x06;
        global::jpeg.JpegJfifMetadataParser parser = new global::jpeg.JpegJfifMetadataParser();

        global::jpeg.JpegJfifMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(metadata.HasRgbThumbnail, Is.True);
        Assert.That(metadata.ThumbnailData, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(metadata.RgbThumbnailPixels, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Identifier_StoresJfifMetadataFromApp0()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, CreateJfifPayload(1, 2, 2, 118, 118, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.Jfif.IsPresent, Is.True);
        Assert.That(info.Metadata.JfifMajorVersion, Is.EqualTo(1));
        Assert.That(info.Metadata.JfifMinorVersion, Is.EqualTo(2));
        Assert.That(info.Metadata.DensityUnit, Is.EqualTo(2));
        Assert.That(info.Metadata.XDensity, Is.EqualTo(118));
        Assert.That(info.Metadata.YDensity, Is.EqualTo(118));
    }

    [Test]
    public void Parser_IgnoresNonJfifPayloadWhenCheckingSignature()
    {
        global::jpeg.JpegJfifMetadataParser parser = new global::jpeg.JpegJfifMetadataParser();

        Assert.That(parser.IsJfif(null), Is.False);
        Assert.That(parser.IsJfif(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsJfif(new byte[] { 0x4A, 0x46, 0x58, 0x58, 0 }), Is.False);
    }

    [Test]
    public void Parser_RejectsMalformedJfifPayloads()
    {
        global::jpeg.JpegJfifMetadataParser parser = new global::jpeg.JpegJfifMetadataParser();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 2, 3 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] payload = CreateJfifPayload(1, 2, 3, 72, 72, 0, 0);
                parser.Parse(payload);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] payload = CreateJfifPayload(1, 2, 1, 72, 72, 1, 1);
                byte[] truncated = new byte[payload.Length - 1];
                System.Array.Copy(payload, 0, truncated, 0, truncated.Length);
                parser.Parse(truncated);
            });
    }

    public static byte[] CreateJfifPayload(int major, int minor, int densityUnit, int xDensity, int yDensity, int thumbnailWidth, int thumbnailHeight)
    {
        int thumbnailBytes = thumbnailWidth * thumbnailHeight * 3;
        byte[] payload = new byte[14 + thumbnailBytes];
        payload[0] = 0x4A;
        payload[1] = 0x46;
        payload[2] = 0x49;
        payload[3] = 0x46;
        payload[4] = 0;
        payload[5] = (byte)major;
        payload[6] = (byte)minor;
        payload[7] = (byte)densityUnit;
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 8, xDensity);
        global::jpeg.JpegByteWriter.WriteUInt16(payload, 10, yDensity);
        payload[12] = (byte)thumbnailWidth;
        payload[13] = (byte)thumbnailHeight;
        return payload;
    }
}
