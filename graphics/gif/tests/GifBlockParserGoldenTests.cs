using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifBlockParserGoldenTests
{
    [Test]
    public void Read_MinimalFixturesMatchBlockParserGoldens()
    {
        GifBlockReadResult gif87a = new GifBlockReader().Read(GifFixtures.MinimalGif87a());
        GifBlockReadResult gif89a = new GifBlockReader().ReadStream(new MemoryStream(GifFixtures.MinimalGif89a()));

        AssertBlockEnvelope(gif87a, GifVersion.Gif87a, 1, 1, 1, 0, 1, 2, 1, GifFixtures.MinimalGif87a().Length);
        AssertBlockEnvelope(gif89a, GifVersion.Gif89a, 1, 1, 1, 0, 1, 2, 1, GifFixtures.MinimalGif89a().Length);
        Assert.That(gif87a.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(gif89a.Document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(gif87a.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 68, 1 }));
        Assert.That(gif89a.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 68, 1 }));
    }

    [Test]
    public void Read_TableAndInterlaceFixturesMatchBlockParserGoldens()
    {
        GifBlockReadResult interlaced = new GifBlockReader().Read(GifFixtures.InterlacedGif89a());
        GifBlockReadResult local = new GifBlockReader().ReadStream(new MemoryStream(GifFixtures.LocalColorTableGif89a()));

        AssertBlockEnvelope(interlaced, GifVersion.Gif89a, 2, 4, 1, 0, 1, 7, 8, GifFixtures.InterlacedGif89a().Length);
        AssertBlockEnvelope(local, GifVersion.Gif89a, 1, 1, 1, 0, 1, 2, 1, GifFixtures.LocalColorTableGif89a().Length);
        Assert.That(interlaced.Document.Frames[0].Interlace, Is.True);
        Assert.That(interlaced.Document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 4, 65, 81, 12, 195, 113, 5 }));
        Assert.That(local.Document.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(local.Document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(local.Document.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
    }

    [Test]
    public void Read_GraphicControlFixturesMatchBlockParserGoldens()
    {
        GifBlockReadResult transparent = new GifBlockReader().Read(GifFixtures.TransparentGif89a());
        GifBlockReadResult disposal = new GifBlockReader().ReadStream(new MemoryStream(GifFixtures.AnimatedDisposalMethod3Gif89a()));

        AssertBlockEnvelope(transparent, GifVersion.Gif89a, 1, 1, 1, 1, 1, 2, 1, GifFixtures.TransparentGif89a().Length);
        AssertBlockEnvelope(disposal, GifVersion.Gif89a, 2, 1, 3, 3, 3, 6, 3, GifFixtures.AnimatedDisposalMethod3Gif89a().Length);
        Assert.That(transparent.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(transparent.Document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(disposal.Document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(disposal.Document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(disposal.Document.Frames[2].Left, Is.EqualTo(1));
    }

    [Test]
    public void Read_MetadataExtensionFixturesMatchBlockParserGoldens()
    {
        GifBlockReadResult loop = new GifBlockReader().Read(GifFixtures.NetscapeLoopGif89a());
        GifBlockReadResult comment = new GifBlockReader().ReadStream(new MemoryStream(GifFixtures.CommentGif89a()));
        GifBlockReadResult application = new GifBlockReader().Read(GifFixtures.ApplicationExtensionGif89a());
        GifBlockReadResult plainText = new GifBlockReader().ReadStream(new MemoryStream(GifFixtures.PlainTextGif89a()));

        AssertBlockEnvelope(loop, GifVersion.Gif89a, 1, 1, 1, 1, 1, 2, 1, GifFixtures.NetscapeLoopGif89a().Length);
        AssertBlockEnvelope(comment, GifVersion.Gif89a, 1, 1, 1, 1, 1, 2, 1, GifFixtures.CommentGif89a().Length);
        AssertBlockEnvelope(application, GifVersion.Gif89a, 1, 1, 1, 1, 1, 2, 1, GifFixtures.ApplicationExtensionGif89a().Length);
        AssertBlockEnvelope(plainText, GifVersion.Gif89a, 1, 1, 1, 1, 1, 2, 1, GifFixtures.PlainTextGif89a().Length);
        Assert.That(loop.Document.LoopCount, Is.EqualTo(5));
        Assert.That(loop.Document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(comment.Document.Comments[0].Text, Is.EqualTo("fixture"));
        Assert.That(application.Document.ApplicationExtensions[0].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(application.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(plainText.Document.PlainTextExtensions[0].Text, Is.EqualTo("plain"));
    }

    static void AssertBlockEnvelope(GifBlockReadResult result, GifVersion version, int width, int height, int frameCount, int extensionCount, int imageDataBlockCount, int imageDataBytes, long decodedPixelCount, int bytesRead)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.Document.Version, Is.EqualTo(version));
        Assert.That(result.Document.LogicalScreen.Width, Is.EqualTo(width));
        Assert.That(result.Document.LogicalScreen.Height, Is.EqualTo(height));
        Assert.That(result.FrameCount, Is.EqualTo(frameCount));
        Assert.That(result.ExtensionCount, Is.EqualTo(extensionCount));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(imageDataBlockCount));
        Assert.That(result.ImageDataBytes, Is.EqualTo(imageDataBytes));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(decodedPixelCount));
        Assert.That(result.BytesRead, Is.EqualTo(bytesRead));
        Assert.That(result.Document.DecodeStatistics.FramesRead, Is.EqualTo(frameCount));
        Assert.That(result.Document.DecodeStatistics.ExtensionCount, Is.EqualTo(extensionCount));
        Assert.That(result.Document.DecodeStatistics.CompressedBytesRead, Is.EqualTo(imageDataBytes));
        Assert.That(result.Document.DecodeStatistics.DecodedPixelCount, Is.EqualTo(decodedPixelCount));
    }
}
