using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifComplexInvalidInputTests
{
    [Test]
    public void Read_InvalidSecondFrameRectanglePreservesPriorValidBlocks()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(CreateGifWithInvalidSecondFrameRectangle());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("ImageDescriptor"));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(1));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(1));
        Assert.That(result.BytesRead, Is.EqualTo(43));
        Assert.That(result.Document.Comments.Length, Is.EqualTo(1));
        Assert.That(result.Document.Comments[0].Text, Is.EqualTo("OK"));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 77 }));
    }

    [Test]
    public void Read_TruncatedSecondFrameLocalColorTableDoesNotAppendPartialFrame()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadStream(new MemoryStream(CreateGifWithTruncatedSecondFrameLocalColorTable()));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("ColorTable"));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].Left, Is.EqualTo(0));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 77 }));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(1));
        Assert.That(result.HasTrailer, Is.False);
    }

    [Test]
    public void Read_UnterminatedSecondFrameImageDataPreservesFirstFrameAndPendingMetadata()
    {
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(CreateGifWithUnterminatedSecondFrameImageData());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnterminatedSubBlocks));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("DataSubBlocks"));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].DelayHundredths, Is.EqualTo(3));
        Assert.That(result.Document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.ExtensionCount, Is.EqualTo(2));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(1));
        Assert.That(result.HasPendingGraphicControl, Is.True);
        Assert.That(result.PendingGraphicControl.DelayHundredths, Is.EqualTo(9));
        Assert.That(result.PendingGraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
    }

    static byte[] CreateGifWithInvalidSecondFrameRectangle()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 2, 79, 75, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
    }

    static byte[] CreateGifWithTruncatedSecondFrameLocalColorTable()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0x81,
            255, 0, 0
        };
    }

    static byte[] CreateGifWithUnterminatedSecondFrameImageData()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 4, 3, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x21, 0xF9, 4, 8, 9, 0, 0, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 88
        };
    }
}
