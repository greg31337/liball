using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifFirstFrameOnlyTests
{
    [Test]
    public void Read_FirstFrameOnlyStopsAfterFirstImageFromBytes()
    {
        byte[] data = CreateTwoFrameGifWithCommentBetweenFrames();
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 77 }));
        Assert.That(result.ExtensionCount, Is.EqualTo(0));
        Assert.That(result.HasTrailer, Is.False);
        Assert.That(result.BytesRead, Is.EqualTo(27));
        Assert.That(result.BytesRead, Is.LessThan(data.Length));
        Assert.That(result.Document.DecodeStatistics.FramesRead, Is.EqualTo(1));
    }

    [Test]
    public void Read_WithoutFirstFrameOnlyReadsRemainingBlocks()
    {
        byte[] data = CreateTwoFrameGifWithCommentBetweenFrames();
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.BytesRead, Is.EqualTo(data.Length));
        Assert.That(result.Document.Comments.Length, Is.EqualTo(1));
    }

    [Test]
    public void Read_FirstFrameOnlyStopsStreamAtFirstFrameBoundary()
    {
        byte[] data = CreateTwoFrameGifWithCommentBetweenFrames();
        MemoryStream stream = new MemoryStream(data);
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadStreamWithOptions(stream, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.BytesRead, Is.EqualTo(27));
        Assert.That(stream.Position, Is.EqualTo(27));
        Assert.That(result.HasTrailer, Is.False);
    }

    [Test]
    public void Read_FirstFrameOnlyWithMetadataOnlyDoesNotKeepCompressedPayload()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;
        options.MetadataOnly = true;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(CreateTwoFrameGifWithCommentBetweenFrames(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.Empty);
        Assert.That(result.BytesRead, Is.EqualTo(27));
    }

    [Test]
    public void Read_FirstFrameOnlyIgnoresInvalidBlocksAfterFirstImage()
    {
        byte[] data = CreateGifWithInvalidSecondFrame();
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(result.BytesRead, Is.EqualTo(27));
    }

    static byte[] CreateTwoFrameGifWithCommentBetweenFrames()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x21, 0xFE, 2, 79, 75, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }

    static byte[] CreateGifWithInvalidSecondFrame()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 0, 0, 0, 0, 3, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }
}
