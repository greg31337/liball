namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ImageHeaderInspectorTests
{
    [TestCase(0, 1, 1)]
    [TestCase(0, 16, 1)]
    [TestCase(2, 8, 3)]
    [TestCase(3, 4, 1)]
    [TestCase(4, 16, 2)]
    [TestCase(6, 8, 4)]
    public void InspectImage_ReadsPngHeaderMetadata(
        int colorType,
        int bitDepth,
        int expectedComponentCount)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildPng(800, 600, bitDepth, colorType, false, false);

        GltfImageInfo result = new ImageHeaderInspector().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfImageFormat.Png));
            Assert.That(result.MimeType, Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.HasDimensions, Is.True);
            Assert.That(result.Width, Is.EqualTo(800));
            Assert.That(result.Height, Is.EqualTo(600));
            Assert.That(result.HasComponentCount, Is.True);
            Assert.That(result.ComponentCount, Is.EqualTo(expectedComponentCount));
            Assert.That(result.HasBitDepth, Is.True);
            Assert.That(result.BitDepth, Is.EqualTo(bitDepth));
            Assert.That(result.Animated, Is.False);
            Assert.That(result.HasUnsupportedColorProfile, Is.False);
        });
    }

    [Test]
    public void InspectImage_RecognizesPngAnimationAndColorProfileChunks()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildPng(16, 32, 8, 6, true, true);

        GltfImageInfo result = new ImageHeaderInspector().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Animated, Is.True);
            Assert.That(result.HasUnsupportedColorProfile, Is.True);
        });
    }

    [TestCase(0xC0)]
    [TestCase(0xC2)]
    public void InspectImage_ReadsJpegFrameMetadata(int frameMarker)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildJpeg(400, 300, 8, 3, frameMarker, false);

        GltfImageInfo result = new ImageHeaderInspector().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfImageFormat.Jpeg));
            Assert.That(result.MimeType, Is.EqualTo(GltfMimeTypes.Jpeg));
            Assert.That(result.HasDimensions, Is.True);
            Assert.That(result.Width, Is.EqualTo(400));
            Assert.That(result.Height, Is.EqualTo(300));
            Assert.That(result.HasComponentCount, Is.True);
            Assert.That(result.ComponentCount, Is.EqualTo(3));
            Assert.That(result.HasBitDepth, Is.True);
            Assert.That(result.BitDepth, Is.EqualTo(8));
            Assert.That(result.Animated, Is.False);
            Assert.That(result.HasUnsupportedColorProfile, Is.False);
        });
    }

    [Test]
    public void InspectImage_RecognizesJpegIccProfileBeforeTheFrame()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildJpeg(64, 48, 12, 1, 0xC1, true);

        GltfImageInfo result = new ImageHeaderInspector().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.ComponentCount, Is.EqualTo(1));
            Assert.That(result.BitDepth, Is.EqualTo(12));
            Assert.That(result.HasUnsupportedColorProfile, Is.True);
        });
    }

    [Test]
    public void InspectImage_ReturnsUnknownMetadataForAnUnrecognizedHeader()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];

        GltfImageInfo result = new ImageHeaderInspector().InspectImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfImageFormat.Unknown));
            Assert.That(result.MimeType, Is.Null);
            Assert.That(result.HasDimensions, Is.False);
            Assert.That(result.HasComponentCount, Is.False);
            Assert.That(result.HasBitDepth, Is.False);
        });
    }

    [Test]
    public void InspectImage_RejectsMimeTypeThatDisagreesWithTheSignature()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildPng(1, 1, 8, 6, false, false);
        request.MimeType = GltfMimeTypes.Jpeg;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Image signature media type 'image/png' does not match declared media type 'image/jpeg'."));
    }

    [Test]
    public void InspectImage_RejectsSourceFormatThatDisagreesWithTheSignature()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildJpeg(1, 1, 8, 3, 0xC0, false);
        request.SourceFormat = GltfImageFormat.Png;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Image signature format Jpeg does not match declared source format Png."));
    }

    [TestCase(new byte[] { 0x89, 0x50 }, "PNG signature is truncated.")]
    [TestCase(new byte[] { 0xFF }, "JPEG signature is truncated.")]
    public void InspectImage_RejectsTruncatedSignatures(byte[] bytes, string message)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = bytes;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void InspectImage_RejectsTruncatedPngChunkHeader()
    {
        List<byte> bytes = new List<byte>(BuildPng(1, 1, 8, 6, false, false));
        bytes.RemoveRange(bytes.Count - 8, 8);
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = bytes.ToArray();

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "PNG chunk header is truncated at byte 33."));
    }

    [TestCase(2, 4, "PNG color type 2 does not support bit depth 4.")]
    [TestCase(5, 8, "PNG color type 5 is unsupported.")]
    public void InspectImage_RejectsUnsupportedPngHeaderMetadata(
        int colorType,
        int bitDepth,
        string message)
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = BuildPng(1, 1, bitDepth, colorType, false, false);

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void InspectImage_RejectsTruncatedJpegSegment()
    {
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x0A];

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "JPEG segment at byte 2 is truncated; it declares length 10."));
    }

    [Test]
    public void InspectImage_RejectsJpegFrameWithInconsistentComponentLayout()
    {
        byte[] bytes = BuildJpeg(8, 8, 8, 3, 0xC0, false);
        bytes[11] = 4;
        GltfImageRequest request = new GltfImageRequest();
        request.Bytes = bytes;

        Assert.That(
            () => new ImageHeaderInspector().InspectImage(request),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "JPEG start-of-frame component layout does not match segment length 17."));
    }

    private static byte[] BuildPng(
        int width,
        int height,
        int bitDepth,
        int colorType,
        bool animated,
        bool colorProfile)
    {
        List<byte> bytes = new List<byte>(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        byte[] header = new byte[13];
        WriteUInt32(header, 0, width);
        WriteUInt32(header, 4, height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        AddPngChunk(bytes, "IHDR", header);
        if (animated)
        {
            AddPngChunk(bytes, "acTL", new byte[8]);
        }

        if (colorProfile)
        {
            AddPngChunk(bytes, "iCCP", new byte[] { 0 });
        }

        AddPngChunk(bytes, "IEND", Array.Empty<byte>());
        return bytes.ToArray();
    }

    private static byte[] BuildJpeg(
        int width,
        int height,
        int bitDepth,
        int componentCount,
        int frameMarker,
        bool colorProfile)
    {
        List<byte> bytes = new List<byte>(new byte[] { 0xFF, 0xD8 });
        if (colorProfile)
        {
            bytes.Add(0xFF);
            bytes.Add(0xE2);
            bytes.Add(0x00);
            bytes.Add(0x0E);
            bytes.AddRange(new byte[] {
                0x49, 0x43, 0x43, 0x5F, 0x50, 0x52,
                0x4F, 0x46, 0x49, 0x4C, 0x45, 0x00
            });
        }

        int segmentLength = 8 + (componentCount * 3);
        bytes.Add(0xFF);
        bytes.Add((byte)frameMarker);
        bytes.Add((byte)(segmentLength >> 8));
        bytes.Add((byte)segmentLength);
        bytes.Add((byte)bitDepth);
        bytes.Add((byte)(height >> 8));
        bytes.Add((byte)height);
        bytes.Add((byte)(width >> 8));
        bytes.Add((byte)width);
        bytes.Add((byte)componentCount);
        int componentIndex = 0;
        while (componentIndex < componentCount)
        {
            bytes.Add((byte)(componentIndex + 1));
            bytes.Add(0x11);
            bytes.Add(0x00);
            componentIndex++;
        }

        return bytes.ToArray();
    }

    private static void AddPngChunk(List<byte> bytes, string type, byte[] data)
    {
        byte[] length = new byte[4];
        WriteUInt32(length, 0, data.Length);
        bytes.AddRange(length);
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(type));
        bytes.AddRange(data);
        bytes.AddRange(new byte[4]);
    }

    private static void WriteUInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
