using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifComplexValidFixtureTests
{
    [Test]
    public void ComplexValidGif89aFixture_ReadsMixedExtensionsAndFrames()
    {
        byte[] data = GifFixtures.ComplexValidGif89a();
        GifBlockReadResult result = new GifBlockReader().ReadStream(new MemoryStream(data));

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasTrailer, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(3));
        Assert.That(result.ExtensionCount, Is.EqualTo(5));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(5));
        Assert.That(result.ImageDataBytes, Is.EqualTo(8));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(12));
        Assert.That(result.BytesRead, Is.EqualTo(142));
        Assert.That(result.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(result.Document.LogicalScreen.Width, Is.EqualTo(3));
        Assert.That(result.Document.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(result.Document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(result.Document.LoopCount, Is.EqualTo(2));
        Assert.That(result.Document.Comments.Length, Is.EqualTo(1));
        Assert.That(result.Document.Comments[0].Text, Is.EqualTo("complex"));
        Assert.That(result.Document.Comments[0].Data, Is.EqualTo(new byte[] { 99, 111, 109, 112, 108, 101, 120 }));
        Assert.That(result.Document.ApplicationExtensions.Length, Is.EqualTo(1));
        Assert.That(result.Document.ApplicationExtensions[0].Identifier, Is.EqualTo("NETSCAPE"));
        Assert.That(result.Document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(result.Document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 1, 2, 0 }));
        Assert.That(result.Document.ApplicationExtensions[0].IsLoopExtension, Is.True);
    }

    [Test]
    public void ComplexValidGif89aFixture_DecodesRawFramesWithLocalPaletteOffsetsAndTransparency()
    {
        GifDocument document = Gif.Decode(GifFixtures.ComplexValidGif89a());
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();

        GifRawFrameDecodeResult first = decoder.DecodeFrame(document.Frames[0]);
        GifRawFrameDecodeResult second = decoder.DecodeFrame(document.Frames[1]);
        GifRawFrameDecodeResult third = decoder.DecodeFrame(document.Frames[2]);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(third.Success, Is.True);
        Assert.That(document.Frames[0].Left, Is.EqualTo(0));
        Assert.That(document.Frames[0].Top, Is.EqualTo(0));
        Assert.That(document.Frames[0].Width, Is.EqualTo(3));
        Assert.That(document.Frames[0].Height, Is.EqualTo(2));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 12, 126, 5 }));
        Assert.That(first.IndexedPixels, Is.EqualTo(new byte[] { 1, 0, 0, 0, 0, 0 }));

        Assert.That(document.Frames[1].Left, Is.EqualTo(1));
        Assert.That(document.Frames[1].Width, Is.EqualTo(2));
        Assert.That(document.Frames[1].Height, Is.EqualTo(2));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(7));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(document.Frames[1].HasTransparency, Is.True);
        Assert.That(document.Frames[1].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[1].LocalColorTable.Count, Is.EqualTo(4));
        Assert.That(document.Frames[1].LocalColorTable.Colors[1].Green, Is.EqualTo(255));
        Assert.That(document.Frames[1].CompressedImageData, Is.EqualTo(new byte[] { 68, 52, 5 }));
        Assert.That(second.IndexedPixels, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));

        Assert.That(document.Frames[2].Left, Is.EqualTo(2));
        Assert.That(document.Frames[2].Width, Is.EqualTo(1));
        Assert.That(document.Frames[2].Height, Is.EqualTo(2));
        Assert.That(document.Frames[2].DelayHundredths, Is.EqualTo(11));
        Assert.That(document.Frames[2].HasTransparency, Is.True);
        Assert.That(document.Frames[2].HasLocalColorTable, Is.False);
        Assert.That(document.Frames[2].CompressedImageData, Is.EqualTo(new byte[] { 196, 10 }));
        Assert.That(third.IndexedPixels, Is.EqualTo(new byte[] { 0, 3 }));
    }

    [Test]
    public void ComplexValidGif89aFixture_DecodesCoalescedAnimationAcrossRestoreToPrevious()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.ComplexValidGif89a(), options);

        Assert.That(animation.Width, Is.EqualTo(3));
        Assert.That(animation.Height, Is.EqualTo(2));
        Assert.That(animation.LoopCount, Is.EqualTo(2));
        Assert.That(animation.Frames.Length, Is.EqualTo(3));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(230));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(50));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(70));
        Assert.That(animation.Frames[2].DelayMilliseconds, Is.EqualTo(110));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255
        }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 255, 255, 255, 0, 255,
            0, 0, 0, 255, 255, 0, 255, 255, 0, 255, 255, 255
        }));
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 255, 255
        }));
    }

    [Test]
    public void ComplexValidGif89aFixture_IdentifiesAggregateMetadata()
    {
        GifInfo info = Gif.Identify(GifFixtures.ComplexValidGif89a());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(3));
        Assert.That(info.Height, Is.EqualTo(2));
        Assert.That(info.FrameCount, Is.EqualTo(3));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasTransparency, Is.True);
        Assert.That(info.LoopCount, Is.EqualTo(2));
        Assert.That(info.HasComments, Is.True);
        Assert.That(info.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "NETSCAPE" }));
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(48));
    }

    [Test]
    public void ComplexValidGif89aFixture_MetadataOnlyPreservesFrameMetadataWithoutCompressedPayloads()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = true;

        GifBlockReadResult result = new GifBlockReader().ReadBytesWithOptions(GifFixtures.ComplexValidGif89a(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FrameCount, Is.EqualTo(3));
        Assert.That(result.ExtensionCount, Is.EqualTo(5));
        Assert.That(result.ImageDataBlockCount, Is.EqualTo(5));
        Assert.That(result.ImageDataBytes, Is.EqualTo(8));
        Assert.That(result.DecodedPixelCount, Is.EqualTo(12));
        Assert.That(result.Document.Frames[0].CompressedImageData, Is.Empty);
        Assert.That(result.Document.Frames[1].CompressedImageData, Is.Empty);
        Assert.That(result.Document.Frames[2].CompressedImageData, Is.Empty);
        Assert.That(result.Document.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(result.Document.Frames[1].LocalColorTable.Count, Is.EqualTo(4));
        Assert.That(result.Document.Frames[1].HasTransparency, Is.True);
        Assert.That(result.Document.Frames[2].HasTransparency, Is.True);
        Assert.That(result.Document.LoopCount, Is.EqualTo(2));
        Assert.That(result.Document.Comments[0].Text, Is.EqualTo("complex"));
    }

    [Test]
    public void ComplexValidGif89aFixture_SemanticallyReencodesAsValidGif()
    {
        GifDocument document = Gif.Decode(GifFixtures.ComplexValidGif89a());

        byte[] encoded = Gif.Encode(document);
        GifDocument decoded = Gif.Decode(encoded);

        Assert.That(encoded, Is.Not.Empty);
        Assert.That(decoded.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(decoded.LogicalScreen.Width, Is.EqualTo(3));
        Assert.That(decoded.LogicalScreen.Height, Is.EqualTo(2));
        Assert.That(decoded.Frames.Length, Is.EqualTo(3));
        Assert.That(decoded.LoopCount, Is.EqualTo(2));
        Assert.That(decoded.Comments.Length, Is.EqualTo(1));
        Assert.That(decoded.Comments[0].Text, Is.EqualTo("complex"));
        Assert.That(decoded.Frames[1].HasLocalColorTable, Is.True);
        Assert.That(decoded.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToPrevious));
        Assert.That(decoded.Frames[2].HasTransparency, Is.True);
    }
}
