namespace jpeg.Tests;

internal class JfxxMetadataParserTests
{
    [Test]
    public void Parser_ReadsJfxxExtensionHeaderAndData()
    {
        byte[] payload = CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

        global::jpeg.JpegJfxxMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.ExtensionCode, Is.EqualTo(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail));
        Assert.That(metadata.IsJpegThumbnail, Is.True);
        Assert.That(metadata.IsIndexedThumbnail, Is.False);
        Assert.That(metadata.IsRgbThumbnail, Is.False);
        Assert.That(metadata.JpegThumbnailData, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }));
        Assert.That(metadata.ExtensionData, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }));
    }

    [Test]
    public void Parser_ClassifiesStandardJfxxExtensionCodes()
    {
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

        global::jpeg.JpegJfxxMetadata indexed = parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, CreateIndexedExtensionData(0, 0, new byte[768], new byte[0])));
        global::jpeg.JpegJfxxMetadata rgb = parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 0, 0 }));

        Assert.That(indexed.IsIndexedThumbnail, Is.True);
        Assert.That(indexed.IsJpegThumbnail, Is.False);
        Assert.That(indexed.IsRgbThumbnail, Is.False);
        Assert.That(rgb.IsRgbThumbnail, Is.True);
        Assert.That(rgb.IsJpegThumbnail, Is.False);
        Assert.That(rgb.IsIndexedThumbnail, Is.False);
    }

    [Test]
    public void Parser_ReadsJfxxRgbThumbnailPixels()
    {
        byte[] payload = CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 2, 1, 10, 20, 30, 40, 50, 60 });
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

        global::jpeg.JpegJfxxMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(metadata.RgbThumbnailPixels, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        Assert.That(metadata.ExtensionData, Is.EqualTo(new byte[] { 2, 1, 10, 20, 30, 40, 50, 60 }));
    }

    [Test]
    public void Parser_ReadsJfxxIndexedThumbnailPaletteAndPixels()
    {
        byte[] palette = CreatePalette();
        byte[] pixels = new byte[] { 0, 1 };
        byte[] extensionData = CreateIndexedExtensionData(2, 1, palette, pixels);
        byte[] payload = CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, extensionData);
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

        global::jpeg.JpegJfxxMetadata metadata = parser.Parse(payload);

        Assert.That(metadata.ThumbnailWidth, Is.EqualTo(2));
        Assert.That(metadata.ThumbnailHeight, Is.EqualTo(1));
        Assert.That(metadata.IndexedThumbnailPalette, Is.EqualTo(palette));
        Assert.That(metadata.IndexedThumbnailPixels, Is.EqualTo(pixels));
        Assert.That(metadata.ExtensionData, Is.EqualTo(extensionData));
    }

    [Test]
    public void Identifier_StoresJpegJfxxThumbnailFromApp0()
    {
        byte[] thumbnail = CreateJpegThumbnailData();
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, thumbnail));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasJfxx, Is.True);
        Assert.That(info.Metadata.Jfxx.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfxx[0].IsJpegThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[0].JpegThumbnailData, Is.EqualTo(thumbnail));
    }

    [Test]
    public void Identifier_StoresIndexedJfxxThumbnailFromApp0()
    {
        byte[] palette = CreatePalette();
        byte[] pixels = new byte[] { 7, 8 };
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, CreateIndexedExtensionData(2, 1, palette, pixels)));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasJfxx, Is.True);
        Assert.That(info.Metadata.Jfxx.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfxx[0].IsIndexedThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[0].ThumbnailWidth, Is.EqualTo(2));
        Assert.That(info.Metadata.Jfxx[0].ThumbnailHeight, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfxx[0].IndexedThumbnailPalette, Is.EqualTo(palette));
        Assert.That(info.Metadata.Jfxx[0].IndexedThumbnailPixels, Is.EqualTo(pixels));
    }

    [Test]
    public void Identifier_StoresJfxxMetadataFromApp0()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 2, 1, 10, 20, 30, 40, 50, 60 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasJfxx, Is.True);
        Assert.That(info.Metadata.Jfxx.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfxx[0].ExtensionCode, Is.EqualTo(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail));
        Assert.That(info.Metadata.Jfxx[0].IsRgbThumbnail, Is.True);
        Assert.That(info.Metadata.Jfxx[0].ThumbnailWidth, Is.EqualTo(2));
        Assert.That(info.Metadata.Jfxx[0].ThumbnailHeight, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfxx[0].RgbThumbnailPixels, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        Assert.That(info.Metadata.Jfxx[0].ExtensionData, Is.EqualTo(new byte[] { 2, 1, 10, 20, 30, 40, 50, 60 }));
    }

    [Test]
    public void Parser_IgnoresNonJfxxPayloadWhenCheckingSignature()
    {
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

        Assert.That(parser.IsJfxx(null), Is.False);
        Assert.That(parser.IsJfxx(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsJfxx(new byte[] { 0x4A, 0x46, 0x49, 0x46, 0 }), Is.False);
    }

    [Test]
    public void Parser_RejectsMalformedJfxxPayloads()
    {
        global::jpeg.JpegJfxxMetadataParser parser = new global::jpeg.JpegJfxxMetadataParser();

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
                parser.Parse(new byte[] { 0x4A, 0x46, 0x58, 0x58, 0 });
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(0x12, new byte[] { 1 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 1 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionRgbThumbnail, new byte[] { 2, 1, 1, 2, 3 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, new byte[] { 1, 1 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionIndexedThumbnail, CreateIndexedExtensionData(2, 1, new byte[768], new byte[] { 1 })));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, new byte[0]));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateJfxxPayload(global::jpeg.JpegConstants.JfxxExtensionJpegThumbnail, new byte[] { 0xFF, 0xD8, 0 }));
            });
    }

    public static byte[] CreateJfxxPayload(int extensionCode, byte[] extensionData)
    {
        byte[] payload = new byte[6 + extensionData.Length];
        payload[0] = 0x4A;
        payload[1] = 0x46;
        payload[2] = 0x58;
        payload[3] = 0x58;
        payload[4] = 0;
        payload[5] = (byte)extensionCode;
        System.Array.Copy(extensionData, 0, payload, 6, extensionData.Length);
        return payload;
    }

    public static byte[] CreateIndexedExtensionData(int width, int height, byte[] palette, byte[] pixels)
    {
        byte[] extensionData = new byte[770 + pixels.Length];
        extensionData[0] = (byte)width;
        extensionData[1] = (byte)height;
        System.Array.Copy(palette, 0, extensionData, 2, 768);
        System.Array.Copy(pixels, 0, extensionData, 770, pixels.Length);
        return extensionData;
    }

    public static byte[] CreatePalette()
    {
        byte[] palette = new byte[768];
        palette[0] = 10;
        palette[1] = 20;
        palette[2] = 30;
        palette[3] = 40;
        palette[4] = 50;
        palette[5] = 60;
        return palette;
    }

    public static byte[] CreateJpegThumbnailData()
    {
        return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 2, 0xFF, 0xD9 };
    }
}
