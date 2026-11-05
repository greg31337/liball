using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifDecodeLimitTests
{
    [Test]
    public void Read_RejectsLogicalScreenAboveDimensionLimit()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxWidth = 2;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTrailerOnlyGif(3, 1), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DimensionLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("LogicalScreenDescriptor"));
        Assert.That(result.BytesRead, Is.EqualTo(13));
    }

    [Test]
    public void Read_RejectsWhenByteLimitIsExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxBytes = 12;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTrailerOnlyGif(1, 1), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InputByteLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("LogicalScreenDescriptor"));
        Assert.That(result.BytesRead, Is.EqualTo(13));
    }

    [Test]
    public void Read_FromStreamRejectsWhenByteLimitIsExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxBytes = 12;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadStreamWithOptions(new MemoryStream(CreateTrailerOnlyGif(1, 1)), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InputByteLimitExceeded));
        Assert.That(result.BytesRead, Is.EqualTo(13));
    }

    [Test]
    public void Read_RejectsSecondFrameWhenFrameLimitIsReached()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxFrames = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoOnePixelFramesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.FrameLimitExceeded));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.DecodeStatistics.FramesRead, Is.EqualTo(1));
    }

    [Test]
    public void Read_RejectsFrameWhenDecodedPixelLimitWouldBeExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxDecodedPixels = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoOnePixelFramesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(1));
        Assert.That(result.Document.DecodeStatistics.DecodedPixelCount, Is.EqualTo(1));
    }

    [Test]
    public void Read_TracksDecodedPixelCountOnSuccess()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(CreateTwoOnePixelFramesGif());

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(2));
        Assert.That(result.Document.DecodeStatistics.DecodedPixelCount, Is.EqualTo(2));
    }

    [Test]
    public void Read_RejectsImageDataAboveSubBlockByteLimit()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlockBytes = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateImageWithTwoCompressedBytesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockByteLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("DataSubBlocks"));
        Assert.That(result.FrameCount, Is.EqualTo(0));
    }

    [Test]
    public void Read_RejectsCommentAboveSubBlockCountLimit()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlocks = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateCommentWithTwoSubBlocksGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockCountLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("DataSubBlocks"));
        Assert.That(result.ExtensionCount, Is.EqualTo(0));
    }

    [Test]
    public void Read_RejectsCompressedImageBytesIndependentlyOfSubBlockBytes()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxCompressedBytes = 1;
        options.MaxSubBlockBytes = 2;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateImageWithTwoCompressedBytesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.CompressedByteLimitExceeded));
        Assert.That(result.ImageDataBytes, Is.EqualTo(0));
    }

    [Test]
    public void Read_AppliesCompressedImageByteLimitAcrossFrames()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxCompressedBytes = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoOnePixelFramesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.CompressedByteLimitExceeded));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(1));
    }

    [Test]
    public void Read_AppliesSubBlockCountLimitAcrossExtensionChains()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlocks = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoSingleBlockCommentsGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockCountLimitExceeded));
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.SubBlockCount, Is.EqualTo(1));
    }

    [Test]
    public void Read_AppliesSubBlockByteLimitAcrossExtensionChains()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlockBytes = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoSingleBlockCommentsGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.SubBlockByteLimitExceeded));
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.SubBlockBytes, Is.EqualTo(1));
    }

    [Test]
    public void Read_RejectsCompressedBufferBeforeConfiguredMemoryIsExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxCompressedBytes = 2;
        options.MaxSubBlockBytes = 2;
        options.MaxMemoryBytes = 1;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateImageWithTwoCompressedBytesGif(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.ImageDataBytes, Is.EqualTo(0));
    }

    [Test]
    public void DecodeIndexedPixels_RejectsOutputBeforeConfiguredMemoryIsExceeded()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxMemoryBytes = 0;
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult result = decoder.DecodeIndexedPixelsWithOptions(new byte[0], 2, 1, 1, false, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.IndexedPixels, Is.Empty);
    }

    [Test]
    public void PublicDecode_ExposesCompressedImageLimitCode()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxCompressedBytes = 1;

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeBytes(CreateImageWithTwoCompressedBytesGif(), options));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.CompressedByteLimitExceeded));
        Assert.That(exception.BlockName, Is.EqualTo("DataSubBlocks"));
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

    static byte[] CreateTwoOnePixelFramesGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }

    static byte[] CreateImageWithTwoCompressedBytesGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 77, 88, 0,
            0x3B
        };
    }

    static byte[] CreateCommentWithTwoSubBlocksGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 1, 65, 1, 66, 0,
            0x3B
        };
    }

    static byte[] CreateTwoSingleBlockCommentsGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 1, 65, 0,
            0x21, 0xFE, 1, 66, 0,
            0x3B
        };
    }
}
