namespace gltf.Tests.Internal.Containers;

[TestFixture]
public class GlbReaderTests
{
    [Test]
    public void ReadHeader_ReadsLittleEndianVersionAndDeclaredLength()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 2U, 20U, 20);

        GlbHeader header = reader.ReadHeader(bytes, 20);

        Assert.Multiple(() =>
        {
            Assert.That(header.Version, Is.EqualTo(2));
            Assert.That(header.DeclaredByteLength, Is.EqualTo(20));
        });
    }

    [Test]
    public void ReadHeader_AllowsBytesAfterTheDeclaredContainerEnd()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 2U, 20U, 24);

        GlbHeader header = reader.ReadHeader(bytes, 24);

        Assert.That(header.DeclaredByteLength, Is.EqualTo(20));
    }

    [Test]
    public void ReadHeader_ReportsTruncatedHeaderAtTheExactInputOffset()
    {
        GlbReader reader = new GlbReader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadHeader([], 12),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB header is truncated at byte 0; 12 bytes are required."));
            Assert.That(
                () => reader.ReadHeader(new byte[11], 12),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("GLB header is truncated at byte 11; 12 bytes are required."));
        });
    }

    [Test]
    public void ReadHeader_RejectsInvalidMagicAtByteZero()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(0x46546C68U, 2U, 20U, 20);

        Assert.That(
            () => reader.ReadHeader(bytes, 20),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("Invalid GLB magic at byte 0."));
    }

    [Test]
    public void ReadHeader_RejectsUnsupportedVersionAtByteFour()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 1U, 20U, 20);

        Assert.That(
            () => reader.ReadHeader(bytes, 20),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("Unsupported GLB version 1 at byte 4; expected version 2."));
    }

    [Test]
    public void ReadHeader_RejectsDeclaredLengthOutsideSignedRange()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 2U, 0x80000000U, 12);

        Assert.That(
            () => reader.ReadHeader(bytes, int.MaxValue),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo(
                    "GLB declared length 2147483648 at byte 8 exceeds the supported signed length."));
    }

    [Test]
    public void ReadHeader_RejectsDeclaredLengthBelowMinimum()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 2U, 19U, 19);

        Assert.That(
            () => reader.ReadHeader(bytes, 20),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("GLB declared length 19 at byte 8 is smaller than the minimum 20."));
    }

    [Test]
    public void ReadHeader_EnforcesActualAndDeclaredInputLimits()
    {
        GlbReader reader = new GlbReader();
        byte[] actualTooLarge = CreateGlb(GltfConstants.GlbMagic, 2U, 20U, 21);
        byte[] declaredTooLarge = CreateGlb(GltfConstants.GlbMagic, 2U, 100U, 20);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadHeader(actualTooLarge, 20),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "GLB input length 21 exceeds the configured maximum 20 at byte 20."));
            Assert.That(
                () => reader.ReadHeader(declaredTooLarge, 64),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo(
                        "GLB declared length 100 at byte 8 exceeds the configured maximum 64."));
            Assert.That(
                () => reader.ReadHeader([], -1),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("maximumInputByteLength"));
        });
    }

    [Test]
    public void ReadHeader_ReportsTruncatedContainerAtTheAvailableByteOffset()
    {
        GlbReader reader = new GlbReader();
        byte[] bytes = CreateGlb(GltfConstants.GlbMagic, 2U, 20U, 12);

        Assert.That(
            () => reader.ReadHeader(bytes, 20),
            Throws.TypeOf<GltfParseException>()
                .With.Message.EqualTo("GLB is truncated at byte 12; declared length is 20."));
    }

    [Test]
    public void ReaderBytes_ValidatesGlbHeaderBeforeDocumentParsing()
    {
        GltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();
        byte[] validHeader = CreateGlb(GltfConstants.GlbMagic, 2U, 48U, 48);
        WriteUInt32LittleEndian(validHeader, 12, 28U);
        WriteUInt32LittleEndian(validHeader, 16, GltfConstants.GlbJsonChunkType);
        byte[] json = System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}} ");
        json.CopyTo(validHeader, 20);
        byte[] invalidVersion = CreateGlb(GltfConstants.GlbMagic, 1U, 20U, 20);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadBytes(invalidVersion, options),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("Unsupported GLB version 1 at byte 4; expected version 2."));
            Assert.That(reader.ReadBytes(validHeader, options).Document, Is.Not.Null);
        });
    }

    private static byte[] CreateGlb(uint magic, uint version, uint declaredLength, int actualLength)
    {
        byte[] bytes = new byte[actualLength];
        WriteUInt32LittleEndian(bytes, 0, magic);
        WriteUInt32LittleEndian(bytes, 4, version);
        WriteUInt32LittleEndian(bytes, 8, declaredLength);
        return bytes;
    }

    private static void WriteUInt32LittleEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }
}
