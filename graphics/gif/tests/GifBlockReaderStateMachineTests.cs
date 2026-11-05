using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifBlockReaderStateMachineTests
{
    [Test]
    public void Read_ParsesHeaderLogicalScreenGlobalColorTableExtensionsImageDataAndTrailer()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 2, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFE, 2, 79, 75, 0,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 2, 0, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 2, 0, 2, 0, 8, 8, 1, 0, 1, 120, 0,
            0x21, 0x99, 1, 42, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 2, 0, 0,
            2, 2, 76, 1, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.BytesRead, Is.EqualTo(data.Length));
        Assert.That(result.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(result.Document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(result.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(result.Document.Comments.Length, Is.EqualTo(1));
        Assert.That(result.Document.Comments[0].Text, Is.EqualTo("OK"));
        Assert.That(result.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(result.Document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(result.Document.LoopCount, Is.EqualTo(2));
        Assert.That(result.Document.Metadata.LoopCount, Is.EqualTo(2));
        Assert.That(result.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(result.Document.PlainTextExtensions[0].Text, Is.EqualTo("x"));
        Assert.That(result.Document.UnknownExtensions.Length, Is.EqualTo(1));
        Assert.That(result.Document.UnknownExtensions[0].Label, Is.EqualTo(0x99));
        Assert.That(result.Document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 42 }));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].Width, Is.EqualTo(2));
        Assert.That(result.Document.Frames[0].LzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 76, 1 }));
        Assert.That(result.FrameCount, Is.EqualTo(1));
        Assert.That(result.ExtensionCount, Is.EqualTo(4));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(1));
        Assert.That(result.ImageDataBytes, Is.EqualTo(2));
        Assert.That(result.Document.DecodeStatistics.BytesRead, Is.EqualTo(data.Length));
        Assert.That(result.Document.DecodeStatistics.FramesRead, Is.EqualTo(1));
        Assert.That(result.Document.DecodeStatistics.ExtensionCount, Is.EqualTo(4));
        Assert.That(result.Document.DecodeStatistics.CompressedBytesRead, Is.EqualTo(2));
    }

    [Test]
    public void Read_FromStreamParsesLocalColorTableAndImageData()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0x80,
            10, 20, 30, 40, 50, 60,
            2, 1, 77, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadStream(new MemoryStream(data));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Document.GlobalColorTable.Count, Is.EqualTo(0));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(result.Document.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(result.Document.Frames[0].LocalColorTable.Colors[0].Red, Is.EqualTo(10));
        Assert.That(result.Document.Frames[0].LocalColorTable.Colors[1].Blue, Is.EqualTo(60));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 77 }));
        Assert.That(result.BytesRead, Is.EqualTo(data.Length));
    }

    [Test]
    public void Read_DispatchesGraphicControlExtensionAndSupportsNoImageStreams()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 0, 1, 0, 0, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(0));
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.Document.Frames, Is.Empty);
    }

    [Test]
    public void Read_SkipsUnknownExtensionWhenPreservationIsDisabled()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0x99, 1, 42, 0,
            0x3B
        };
        GifDecodeOptions options = new GifDecodeOptions();
        options.PreserveUnknownExtensions = false;
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtensionCount, Is.EqualTo(1));
        Assert.That(result.Document.UnknownExtensions, Is.Empty);
        Assert.That(result.Document.Metadata.UnknownExtensions, Is.Empty);
    }

    [Test]
    public void Read_RejectsUnknownTopLevelBlockType()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x00
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnknownBlockType));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("BlockReader"));
    }

    [Test]
    public void Read_RejectsStreamMissingTrailer()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Trailer"));
    }

    [Test]
    public void Read_RejectsImageDataWithInvalidLzwMinimumCodeSize()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            1, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(result.Document.Frames, Is.Empty);
    }

    [Test]
    public void Read_RejectsImageRectangleOutsideLogicalScreen()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(result.Document.Frames, Is.Empty);
    }

    [Test]
    public void ImageDataReader_ReadsMinimumCodeSizeAndSubBlocks()
    {
        GifMemoryByteReader byteReader = new GifMemoryByteReader(new byte[] { 8, 2, 1, 2, 1, 3, 0 });
        GifImageDataReader reader = new GifImageDataReader();

        GifImageDataReadResult result = reader.Read(byteReader);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LzwMinimumCodeSize, Is.EqualTo(8));
        Assert.That(result.CompressedData, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(result.BlockCount, Is.EqualTo(2));
        Assert.That(result.TotalBytes, Is.EqualTo(3));
        Assert.That(byteReader.ByteOffset, Is.EqualTo(7));
    }
}
