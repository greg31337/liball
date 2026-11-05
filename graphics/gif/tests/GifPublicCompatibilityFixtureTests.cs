using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicCompatibilityFixtureTests
{
    // These fixed GIF vectors and expected canvases were independently checked with
    // ImageMagick 7.1.2-30 using `magick input.gif -coalesce -depth 8 rgba:-`.

    [Test]
    public void InterlacedTransparentLocalPaletteFixtureHasExpectedIndexedAndRgbaPixels()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] fixture = InterlacedTransparentLocalPaletteFixture();
        GifDecodeOptions options = TransparentCanvasOptions();

        GifDocument document = decoder.DecodeFrameBytes(fixture, 0, options);
        GifAnimation animation = decoder.DecodeAnimationBytes(fixture, options);

        Assert.That(document.Frames, Has.Length.EqualTo(1));
        Assert.That(document.Frames[0].Interlace, Is.True);
        Assert.That(document.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(document.Frames[0].HasTransparency, Is.True);
        Assert.That(document.Frames[0].TransparentColorIndex, Is.EqualTo(0));
        Assert.That(document.Frames[0].IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3
        }));
        Assert.That(animation.Frames, Has.Length.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            255, 0, 0, 255, 255, 0, 0, 255,
            0, 255, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 0, 0, 255, 255
        }));
    }

    [Test]
    public void ExtensionScopingFixtureHasExpectedMetadataIndexedAndRgbaOutput()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] fixture = ExtensionScopingFixture();
        GifDecodeOptions options = TransparentCanvasOptions();
        options.PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Expose;

        GifDocument document = decoder.DecodeBytes(fixture, options);
        GifDocument firstRaw = decoder.DecodeFrameBytes(fixture, 0, options);
        GifDocument secondRaw = decoder.DecodeFrameBytes(fixture, 1, options);
        GifAnimation animation = decoder.DecodeAnimationBytes(fixture, options);

        Assert.That(document.Comments, Has.Length.EqualTo(1));
        Assert.That(document.ApplicationExtensions, Has.Length.EqualTo(1));
        Assert.That(document.UnknownExtensions, Has.Length.EqualTo(1));
        Assert.That(document.PlainTextExtensions, Has.Length.EqualTo(1));
        Assert.That(document.Frames, Has.Length.EqualTo(2));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[0].HasTransparency, Is.True);
        Assert.That(document.PlainTextExtensions[0].GraphicControl.DelayHundredths, Is.EqualTo(7));
        Assert.That(document.PlainTextExtensions[0].GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[1].DelayHundredths, Is.EqualTo(0));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(document.Frames[1].HasTransparency, Is.False);
        Assert.That(firstRaw.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(secondRaw.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 2 }));
        Assert.That(animation.PlainTextGraphics, Has.Length.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            0, 0, 0, 0
        }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            0, 0, 255, 255
        }));
    }

    [Test]
    public void DisposalFixturesHaveExpectedIndexedAndRgbaOutputForMethodsZeroThroughThree()
    {
        IGifDecoder decoder = new GifCodec();

        AssertDisposalFixture(decoder, 0, GifDisposalMethod.Unspecified, new byte[]
        {
            0, 0, 255, 255, 0, 255, 0, 255
        });
        AssertDisposalFixture(decoder, 1, GifDisposalMethod.DoNotDispose, new byte[]
        {
            0, 0, 255, 255, 0, 255, 0, 255
        });
        AssertDisposalFixture(decoder, 2, GifDisposalMethod.RestoreToBackground, new byte[]
        {
            0, 0, 0, 0, 0, 255, 0, 255
        });
        AssertDisposalFixture(decoder, 3, GifDisposalMethod.RestoreToPrevious, new byte[]
        {
            255, 0, 0, 255, 0, 255, 0, 255
        });
    }

    [Test]
    public void FiniteAndInfiniteLoopFixturesHaveExpectedIndexedAndRgbaOutput()
    {
        IGifDecoder decoder = new GifCodec();

        AssertLoopFixture(decoder, 5);
        AssertLoopFixture(decoder, 0);
    }

    static void AssertDisposalFixture(IGifDecoder decoder, int disposalValue, GifDisposalMethod expectedDisposal, byte[] expectedFinalRgba)
    {
        byte[] fixture = DisposalFixture(disposalValue);
        GifDecodeOptions options = TransparentCanvasOptions();
        GifDocument document = decoder.DecodeBytes(fixture, options);
        GifDocument firstRaw = decoder.DecodeFrameBytes(fixture, 0, options);
        GifDocument secondRaw = decoder.DecodeFrameBytes(fixture, 1, options);
        GifDocument thirdRaw = decoder.DecodeFrameBytes(fixture, 2, options);
        GifAnimation animation = decoder.DecodeAnimationBytes(fixture, options);

        Assert.That(document.Frames, Has.Length.EqualTo(3));
        Assert.That(firstRaw.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(secondRaw.Frames[1].IndexedPixels, Is.EqualTo(new byte[] { 2 }));
        Assert.That(thirdRaw.Frames[2].IndexedPixels, Is.EqualTo(new byte[] { 3 }));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(expectedDisposal));
        Assert.That(animation.Frames, Has.Length.EqualTo(3));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 0
        }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 255, 255, 0, 0, 0, 0
        }));
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(expectedFinalRgba));
    }

    static void AssertLoopFixture(IGifDecoder decoder, int loopCount)
    {
        byte[] fixture = LoopFixture(loopCount);
        GifDocument document = decoder.DecodeFrameBytes(fixture, 0, null);
        GifAnimation animation = decoder.DecodeAnimationBytes(fixture, null);

        Assert.That(document.LoopCount, Is.EqualTo(loopCount));
        Assert.That(document.ApplicationExtensions, Has.Length.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(document.ApplicationExtensions[0].LoopCount, Is.EqualTo(loopCount));
        Assert.That(document.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 0 }));
        Assert.That(animation.LoopCount, Is.EqualTo(loopCount));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    static GifDecodeOptions TransparentCanvasOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;
        return options;
    }

    static byte[] InterlacedTransparentLocalPaletteFixture()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 4, 0, 0, 0, 0,
            0x21, 0xF9, 4, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 4, 0, 0xC1,
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            2, 7, 4, 65, 81, 12, 195, 113, 5, 0,
            0x3B
        };
    }

    static byte[] ExtensionScopingFixture()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0x81, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 0, 255, 0, 255, 0,
            0x21, 0xF9, 4, 5, 5, 0, 0, 0,
            0x21, 0xFE, 1, 99, 0,
            0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49,
            1, 7, 0,
            0x21, 0x99, 1, 85, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 76, 1, 0,
            0x21, 0xF9, 4, 4, 7, 0, 0, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 2, 0, 1, 0, 8, 8, 1, 0,
            1, 65, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 84, 1, 0,
            0x3B
        };
    }

    static byte[] DisposalFixture(int disposalValue)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0x81, 0, 0,
            0, 0, 0, 255, 0, 0, 0, 0, 255, 0, 255, 0,
            0x21, 0xF9, 4, 4, 1, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 76, 1, 0,
            0x21, 0xF9, 4, (byte)(disposalValue * 4), 1, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 84, 1, 0,
            0x21, 0xF9, 4, 4, 1, 0, 0, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 92, 1, 0,
            0x3B
        };
    }

    static byte[] LoopFixture(int loopCount)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48,
            3, 1, (byte)loopCount, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }
}
