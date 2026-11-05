using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifOversizedInputSafetyTests
{
    [Test]
    public void Read_RejectsLogicalScreenAboveDefaultDimensionLimit()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(CreateTrailerOnlyGif(32768, 1));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DimensionLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("LogicalScreenDescriptor"));
        Assert.That(result.FrameCount, Is.EqualTo(0));
    }

    [Test]
    public void Read_RejectsOversizedFrameBeforeReadingImageData()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(CreateHugeFrameWithoutImageDataGif());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("ImageData"));
        Assert.That(result.FrameCount, Is.EqualTo(0));
        Assert.That(result.ImageDataBytes, Is.EqualTo(0));
        Assert.That(result.BytesRead, Is.EqualTo(23));
    }

    [Test]
    public void Read_RejectsOversizedImageSubBlockBeforeConsumingPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = true;
        options.MaxSubBlockBytes = 16;
        GifBlockReader reader = new GifBlockReader();
        byte[] data = CreateImageWithOversizedSubBlockGif(64);

        GifBlockReadResult memoryResult = reader.ReadBytesWithOptions(data, options);
        GifBlockReadResult streamResult = reader.ReadStreamWithOptions(new MemoryStream(data), options);

        AssertOversizedSubBlockResult(memoryResult, data.Length);
        AssertOversizedSubBlockResult(streamResult, data.Length);
    }

    [Test]
    public void Read_RejectsOversizedInputBeforeTrailerWhenByteLimitIsExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxBytes = 64;
        GifBlockReader reader = new GifBlockReader();
        byte[] data = CreateCommentPayloadGif(80);

        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InputByteLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("DataSubBlocks"));
        Assert.That(result.ExtensionCount, Is.EqualTo(0));
        Assert.That(result.HasTrailer, Is.False);
        Assert.That(result.BytesRead, Is.LessThan(data.Length));
    }

    [Test]
    public void DecodeIndexedPixels_RejectsHugeOutputBeforeAllocatingPixels()
    {
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixels(new byte[0], 2, 46341, 46341, false);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.IndexedPixels, Is.Empty);
    }

    static void AssertOversizedSubBlockResult(GifBlockReadResult result, int inputLength)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockByteLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("DataSubBlocks"));
        Assert.That(result.FrameCount, Is.EqualTo(0));
        Assert.That(result.ImageDataBytes, Is.EqualTo(0));
        Assert.That(result.BytesRead, Is.LessThan(inputLength));
    }

    static byte[] CreateTrailerOnlyGif(int width, int height)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            (byte)(width & 255), (byte)((width >> 8) & 255),
            (byte)(height & 255), (byte)((height >> 8) & 255),
            0, 0, 0,
            0x3B
        };
    }

    static byte[] CreateHugeFrameWithoutImageDataGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            255, 127, 255, 127, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 255, 127, 255, 127, 0
        };
    }

    static byte[] CreateImageWithOversizedSubBlockGif(int payloadLength)
    {
        byte[] data = new byte[26 + payloadLength];
        WriteHeaderAndLogicalScreen(data, 0, 1, 1);
        int offset = 13;
        data[offset] = 0x2C;
        WriteLittleEndianUInt16(data, offset + 1, 0);
        WriteLittleEndianUInt16(data, offset + 3, 0);
        WriteLittleEndianUInt16(data, offset + 5, 1);
        WriteLittleEndianUInt16(data, offset + 7, 1);
        data[offset + 9] = 0;
        data[offset + 10] = 2;
        data[offset + 11] = (byte)payloadLength;

        int index = 0;
        while (index < payloadLength)
        {
            data[offset + 12 + index] = 77;
            index = index + 1;
        }

        data[data.Length - 1] = 0;
        return data;
    }

    static byte[] CreateCommentPayloadGif(int payloadLength)
    {
        byte[] data = new byte[18 + payloadLength];
        WriteHeaderAndLogicalScreen(data, 0, 1, 1);
        data[13] = 0x21;
        data[14] = 0xFE;
        data[15] = (byte)payloadLength;

        int index = 0;
        while (index < payloadLength)
        {
            data[16 + index] = 65;
            index = index + 1;
        }

        data[16 + payloadLength] = 0;
        data[17 + payloadLength] = 0x3B;
        return data;
    }

    static void WriteHeaderAndLogicalScreen(byte[] data, int offset, int width, int height)
    {
        data[offset] = 71;
        data[offset + 1] = 73;
        data[offset + 2] = 70;
        data[offset + 3] = 56;
        data[offset + 4] = 57;
        data[offset + 5] = 97;
        WriteLittleEndianUInt16(data, offset + 6, width);
        WriteLittleEndianUInt16(data, offset + 8, height);
        data[offset + 10] = 0;
        data[offset + 11] = 0;
        data[offset + 12] = 0;
    }

    static void WriteLittleEndianUInt16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
    }
}
