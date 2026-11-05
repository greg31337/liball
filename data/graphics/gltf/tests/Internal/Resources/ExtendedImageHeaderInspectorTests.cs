namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ExtendedImageHeaderInspectorTests
{
    [Test]
    public void InspectImage_ReadsKtx2DimensionsWithoutDecodingPayloads()
    {
        GltfImageInfo result = Inspect(BuildKtx2(1024, 512));

        AssertImageDimensions(result, GltfImageFormat.Ktx2, GltfMimeTypes.Ktx2, 1024, 512);
    }

    [Test]
    public void InspectImage_ReadsWebPExtendedCanvasAndFeatureFlags()
    {
        GltfImageInfo result = Inspect(BuildWebP("VP8X", 701, 399, 0x22));

        AssertImageDimensions(result, GltfImageFormat.WebP, GltfMimeTypes.WebP, 701, 399);
        Assert.Multiple(() =>
        {
            Assert.That(result.Animated, Is.True);
            Assert.That(result.HasUnsupportedColorProfile, Is.True);
        });
    }

    [Test]
    public void InspectImage_ReadsWebPLossyFrameDimensions()
    {
        GltfImageInfo result = Inspect(BuildWebP("VP8 ", 320, 241, 0));

        AssertImageDimensions(result, GltfImageFormat.WebP, GltfMimeTypes.WebP, 320, 241);
    }

    [Test]
    public void InspectImage_ReadsWebPLosslessFrameDimensions()
    {
        GltfImageInfo result = Inspect(BuildWebP("VP8L", 16384, 8193, 0));

        AssertImageDimensions(result, GltfImageFormat.WebP, GltfMimeTypes.WebP, 16384, 8193);
    }

    [Test]
    public void InspectImage_ReadsAvifSpatialExtentsFromItemProperties()
    {
        GltfImageInfo result = Inspect(BuildAvif(1920, 1080, false, true));

        AssertImageDimensions(result, GltfImageFormat.Avif, GltfMimeTypes.Avif, 1920, 1080);
        Assert.That(result.Animated, Is.False);
    }

    [Test]
    public void InspectImage_RecognizesAvifSequenceWithoutSpatialExtents()
    {
        GltfImageInfo result = Inspect(BuildAvif(0, 0, true, false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfImageFormat.Avif));
            Assert.That(result.MimeType, Is.EqualTo(GltfMimeTypes.Avif));
            Assert.That(result.HasDimensions, Is.False);
            Assert.That(result.Animated, Is.True);
        });
    }

    [Test]
    public void InspectImage_ReadsDdsDimensions()
    {
        GltfImageInfo result = Inspect(BuildDds(2048, 1024));

        AssertImageDimensions(result, GltfImageFormat.Dds, GltfMimeTypes.Dds, 2048, 1024);
    }

    [Test]
    public void InspectImage_ReadsAstcDimensions()
    {
        GltfImageInfo result = Inspect(BuildAstc(513, 257));

        AssertImageDimensions(result, GltfImageFormat.Astc, null, 513, 257);
    }

    [Test]
    public void InspectImage_RejectsExtendedFormatMimeMismatch()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildKtx2(16, 16);
        request.MimeType = GltfMimeTypes.WebP;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Image signature media type 'image/ktx2' does not match declared media type 'image/webp'."));
    }

    [TestCase(GltfImageFormat.Ktx2, GltfMimeTypes.Ktx2)]
    [TestCase(GltfImageFormat.WebP, GltfMimeTypes.WebP)]
    [TestCase(GltfImageFormat.Avif, GltfMimeTypes.Avif)]
    [TestCase(GltfImageFormat.Dds, GltfMimeTypes.Dds)]
    [TestCase(GltfImageFormat.Astc, null)]
    public void InspectImage_RejectsMissingSignatureForDeclaredExtendedFormat(
        GltfImageFormat format,
        string? mimeType)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
        request.SourceFormat = format;
        request.MimeType = mimeType;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>());
    }

    [Test]
    public void InspectImage_RejectsTruncatedKtx2Header()
    {
        byte[] bytes = new byte[] {
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
            0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        };

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "KTX2 header is truncated; expected at least 80 bytes but found 12."));
    }

    [Test]
    public void InspectImage_RejectsTruncatedWebPRiffData()
    {
        byte[] bytes = new byte[12];
        WriteCode(bytes, 0, "RIFF");
        WriteUInt32LittleEndian(bytes, 4, 22);
        WriteCode(bytes, 8, "WEBP");

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "WebP RIFF data is truncated; it declares 30 total bytes but contains 12."));
    }

    [Test]
    public void InspectImage_RejectsTruncatedAvifFileTypeBox()
    {
        byte[] bytes = new byte[16];
        WriteUInt32BigEndian(bytes, 0, 24);
        WriteCode(bytes, 4, "ftyp");
        WriteCode(bytes, 8, "avif");

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "AVIF file type box at byte 0 is truncated; it declares length 24."));
    }

    [Test]
    public void InspectImage_RejectsTruncatedDdsHeader()
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes("DDS ");

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "DDS header is truncated; expected at least 128 bytes but found 4."));
    }

    [Test]
    public void InspectImage_RejectsTruncatedAstcHeader()
    {
        byte[] bytes = [0x13, 0xAB, 0xA1, 0x5C];

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "ASTC header is truncated; expected 16 bytes but found 4."));
    }

    [TestCase(
        new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57 },
        "WebP RIFF header is truncated.")]
    [TestCase(
        new byte[] { 0, 0, 0, 24, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 },
        "AVIF file type box is truncated.")]
    public void InspectImage_RejectsPartialExtendedFormatSignatures(
        byte[] bytes,
        string message)
    {
        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void InspectImage_RejectsInvalidWebPFrameSynchronizationCode()
    {
        byte[] bytes = BuildWebP("VP8 ", 32, 32, 0);
        bytes[23] = 0;

        Assert.That(
            () => Inspect(bytes),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "WebP VP8 frame synchronization code is invalid."));
    }

    private static GltfImageInfo Inspect(byte[] bytes)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = bytes;
        return new ImageHeaderInspector().InspectImage(request);
    }

    private static void AssertImageDimensions(
        GltfImageInfo result,
        GltfImageFormat format,
        string? mimeType,
        int width,
        int height)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(format));
            Assert.That(result.MimeType, Is.EqualTo(mimeType));
            Assert.That(result.HasDimensions, Is.True);
            Assert.That(result.Width, Is.EqualTo(width));
            Assert.That(result.Height, Is.EqualTo(height));
            Assert.That(result.HasComponentCount, Is.False);
            Assert.That(result.HasBitDepth, Is.False);
        });
    }

    private static byte[] BuildKtx2(int width, int height)
    {
        byte[] bytes = new byte[80];
        byte[] signature = new byte[] {
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
            0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        };
        Array.Copy(signature, bytes, signature.Length);
        WriteUInt32LittleEndian(bytes, 16, 1);
        WriteUInt32LittleEndian(bytes, 20, width);
        WriteUInt32LittleEndian(bytes, 24, height);
        WriteUInt32LittleEndian(bytes, 36, 1);
        WriteUInt32LittleEndian(bytes, 40, 1);
        return bytes;
    }

    private static byte[] BuildWebP(string chunkType, int width, int height, int flags)
    {
        byte[] payload;
        if (chunkType == "VP8X")
        {
            payload = new byte[10];
            payload[0] = (byte)flags;
            WriteUInt24LittleEndian(payload, 4, width - 1);
            WriteUInt24LittleEndian(payload, 7, height - 1);
        }
        else if (chunkType == "VP8 ")
        {
            payload = new byte[10];
            payload[3] = 0x9D;
            payload[4] = 0x01;
            payload[5] = 0x2A;
            WriteUInt16LittleEndian(payload, 6, width);
            WriteUInt16LittleEndian(payload, 8, height);
        }
        else
        {
            payload = new byte[5];
            payload[0] = 0x2F;
            uint bits = (uint)(width - 1) | ((uint)(height - 1) << 14);
            WriteUInt32LittleEndian(payload, 1, bits);
        }

        int paddingByteLength = payload.Length & 1;
        byte[] bytes = new byte[20 + payload.Length + paddingByteLength];
        WriteCode(bytes, 0, "RIFF");
        WriteUInt32LittleEndian(bytes, 4, bytes.Length - 8);
        WriteCode(bytes, 8, "WEBP");
        WriteCode(bytes, 12, chunkType);
        WriteUInt32LittleEndian(bytes, 16, payload.Length);
        Array.Copy(payload, 0, bytes, 20, payload.Length);
        return bytes;
    }

    private static byte[] BuildAvif(int width, int height, bool animated, bool includeDimensions)
    {
        byte[] fileTypePayload = new byte[16];
        WriteCode(fileTypePayload, 0, animated ? "avis" : "avif");
        WriteCode(fileTypePayload, 8, "mif1");
        WriteCode(fileTypePayload, 12, animated ? "avis" : "avif");
        byte[] fileType = BuildIsoBox("ftyp", fileTypePayload);
        if (!includeDimensions)
        {
            return fileType;
        }

        byte[] spatialExtentsPayload = new byte[12];
        WriteUInt32BigEndian(spatialExtentsPayload, 4, width);
        WriteUInt32BigEndian(spatialExtentsPayload, 8, height);
        byte[] spatialExtents = BuildIsoBox("ispe", spatialExtentsPayload);
        byte[] propertyContainer = BuildIsoBox("ipco", spatialExtents);
        byte[] itemProperties = BuildIsoBox("iprp", propertyContainer);
        byte[] metaPayload = new byte[itemProperties.Length + 4];
        Array.Copy(itemProperties, 0, metaPayload, 4, itemProperties.Length);
        byte[] meta = BuildIsoBox("meta", metaPayload);
        byte[] bytes = new byte[fileType.Length + meta.Length];
        Array.Copy(fileType, bytes, fileType.Length);
        Array.Copy(meta, 0, bytes, fileType.Length, meta.Length);
        return bytes;
    }

    private static byte[] BuildIsoBox(string type, byte[] payload)
    {
        byte[] bytes = new byte[payload.Length + 8];
        WriteUInt32BigEndian(bytes, 0, bytes.Length);
        WriteCode(bytes, 4, type);
        Array.Copy(payload, 0, bytes, 8, payload.Length);
        return bytes;
    }

    private static byte[] BuildDds(int width, int height)
    {
        byte[] bytes = new byte[128];
        WriteCode(bytes, 0, "DDS ");
        WriteUInt32LittleEndian(bytes, 4, 124);
        WriteUInt32LittleEndian(bytes, 12, height);
        WriteUInt32LittleEndian(bytes, 16, width);
        WriteUInt32LittleEndian(bytes, 76, 32);
        return bytes;
    }

    private static byte[] BuildAstc(int width, int height)
    {
        byte[] bytes = new byte[16];
        bytes[0] = 0x13;
        bytes[1] = 0xAB;
        bytes[2] = 0xA1;
        bytes[3] = 0x5C;
        bytes[4] = 4;
        bytes[5] = 4;
        bytes[6] = 1;
        WriteUInt24LittleEndian(bytes, 7, width);
        WriteUInt24LittleEndian(bytes, 10, height);
        WriteUInt24LittleEndian(bytes, 13, 1);
        return bytes;
    }

    private static void WriteCode(byte[] bytes, int offset, string value)
    {
        byte[] code = System.Text.Encoding.ASCII.GetBytes(value);
        Array.Copy(code, 0, bytes, offset, code.Length);
    }

    private static void WriteUInt16LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt24LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
    }

    private static void WriteUInt32LittleEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteUInt32LittleEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteUInt32BigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
