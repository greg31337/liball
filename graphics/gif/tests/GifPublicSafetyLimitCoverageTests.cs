using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicSafetyLimitCoverageTests
{
    [Test]
    public void InputByteLimit_ReportsTheFirstCompletedStructurePastTheLimit()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxBytes = 12;
        MemoryStream stream = new MemoryStream(CreateTrailerOnlyGif(1, 1));

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.InputByteLimitExceeded, 0, "LogicalScreenDescriptor");
        Assert.That(exception.ByteOffset, Is.EqualTo(13));
        Assert.That(stream.Position, Is.EqualTo(13));
    }

    [Test]
    public void CompressedByteLimit_StopsBeforeReadingCompressedPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxCompressedBytes = 0;
        MemoryStream stream = new MemoryStream(CreateImageWithTwoCompressedBytesGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.CompressedByteLimitExceeded, 0, "DataSubBlocks");
        Assert.That(stream.Position, Is.EqualTo(25));
    }

    [Test]
    public void SubBlockCountLimit_StopsBeforeReadingTheFirstPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlocks = 0;
        MemoryStream stream = new MemoryStream(CreateCommentGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.SubBlockCountLimitExceeded, -1, "DataSubBlocks");
        Assert.That(stream.Position, Is.EqualTo(16));
    }

    [Test]
    public void SubBlockByteLimit_StopsBeforeReadingTheFirstPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxSubBlockBytes = 0;
        MemoryStream stream = new MemoryStream(CreateCommentGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.SubBlockByteLimitExceeded, -1, "DataSubBlocks");
        Assert.That(stream.Position, Is.EqualTo(16));
    }

    [TestCase(65535, 1, 1, 65535)]
    [TestCase(1, 65535, 65535, 1)]
    public void DimensionLimits_RejectHugeLogicalScreenBeforeReadingContent(int width, int height, int maxWidth, int maxHeight)
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxWidth = maxWidth;
        options.MaxHeight = maxHeight;
        MemoryStream stream = new MemoryStream(CreateTrailerOnlyGif(width, height));

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.DimensionLimitExceeded, -1, "LogicalScreenDescriptor");
        Assert.That(stream.Position, Is.EqualTo(13));
    }

    [Test]
    public void FrameLimit_RejectsTheNextFrameBeforeReadingItsDescriptor()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxFrames = 1;
        MemoryStream stream = new MemoryStream(CreateTwoOnePixelFramesGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));
        GifInfo partial = new GifCodec().IdentifyBytes(CreateTwoOnePixelFramesGif(), options);

        AssertFailure(exception, GifErrorCode.FrameLimitExceeded, 1, "ImageDescriptor");
        Assert.That(stream.Position, Is.EqualTo(28));
        Assert.That(partial.Success, Is.False);
        Assert.That(partial.FrameCount, Is.EqualTo(1));
    }

    [Test]
    public void DecodedPixelLimit_RejectsFrameBeforeReadingItsImagePayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxDecodedPixels = 1;
        MemoryStream stream = new MemoryStream(CreateTwoPixelFrameGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.DecodedPixelLimitExceeded, 0, "ImageData");
        Assert.That(stream.Position, Is.EqualTo(23));
    }

    [Test]
    public void DecodeMemoryLimit_StopsBeforeRetainingCompressedPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MaxMemoryBytes = 0;
        MemoryStream stream = new MemoryStream(CreateImageWithTwoCompressedBytesGif());

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().DecodeStream(stream, options));

        AssertFailure(exception, GifErrorCode.MemoryLimitExceeded, 0, "DataSubBlocks");
        Assert.That(stream.Position, Is.EqualTo(25));
    }

    [Test]
    public void EncodeMemoryLimit_RejectsHugeDeclaredBitmapBeforePixelBufferValidationOrAllocation()
    {
        GifCodec codec = new GifCodec();
        GifBitmap bitmap = codec.CreateIndexedBitmap(65535, 65535, new byte[0], CreatePalette());
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxMemoryBytes = 1;

        GifEncodeResult result = codec.EncodeIndexedStill(bitmap, options, false, -1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("GifEncode"));
        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public void OutputByteLimit_StopsAtLimitAndLeavesCallerDestinationUnchanged()
    {
        GifCodec codec = new GifCodec();
        IGifEncoder encoder = codec;
        GifBitmap bitmap = codec.CreateIndexedBitmap(1, 1, new byte[] { 0 }, CreatePalette());
        GifEncodeOptions options = new GifEncodeOptions();
        options.MaxBytes = 20;

        GifEncodeResult result = encoder.EncodeIndexedStill(bitmap, options, false, -1);
        MemoryStream destination = new MemoryStream(new byte[] { 9, 8, 7 });
        byte[] original = destination.ToArray();
        GifException exception = Assert.Throws<GifException>(() => encoder.Write(destination, result.Document, options));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
        Assert.That(result.Error.Position.ByteOffset, Is.LessThanOrEqualTo(options.MaxBytes));
        Assert.That(result.Data, Is.Empty);
        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
        Assert.That(exception.ByteOffset, Is.LessThanOrEqualTo(options.MaxBytes));
        Assert.That(destination.ToArray(), Is.EqualTo(original));
    }

    static void AssertFailure(GifException exception, GifErrorCode code, int frameIndex, string blockName)
    {
        Assert.That(exception.ErrorCode, Is.EqualTo(code));
        Assert.That(exception.FrameIndex, Is.EqualTo(frameIndex));
        Assert.That(exception.BlockName, Is.EqualTo(blockName));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
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

    static byte[] CreateCommentGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 2, 65, 66, 0,
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

    static byte[] CreateTwoPixelFrameGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 1, 77, 0,
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
}
