using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifSolidBackgroundDecodeTests
{
    [Test]
    public void DecodeOptionsDefaultToOpaqueBlackSolidBackgroundColor()
    {
        GifDecodeOptions options = new GifDecodeOptions();

        Assert.That(options.SolidBackgroundColor.Red, Is.EqualTo(0));
        Assert.That(options.SolidBackgroundColor.Green, Is.EqualTo(0));
        Assert.That(options.SolidBackgroundColor.Blue, Is.EqualTo(0));
        Assert.That(options.SolidBackgroundColor.Alpha, Is.EqualTo(255));
    }

    [Test]
    public void DecodeAnimationUsesSolidRgbaForInitialCanvasAndBackgroundDisposal()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateSolidOptions();

        GifAnimation animation = decoder.DecodeAnimationBytes(GifFixtures.AnimatedDisposalMethod2Gif89a(), options);

        Assert.That(animation.Frames.Length, Is.EqualTo(3));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            12, 34, 56, 78
        }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 255, 255,
            12, 34, 56, 78
        }));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            12, 34, 56, 78,
            0, 255, 0, 255
        }));
    }

    [Test]
    public void SelectedFrameAndCallerBufferPathsRetainSolidRgbaConfiguration()
    {
        IGifDecoder decoder = new GifCodec();
        GifDecodeOptions options = CreateSolidOptions();
        byte[] source = GifFixtures.AnimatedDisposalMethod2Gif89a();
        byte[] destination = new byte[8];

        GifAnimation selected = decoder.DecodeCompositedFrameMemory(new ReadOnlyMemory<byte>(source), 2, options);
        int written = decoder.DecodeCompositedFrameBytesToSpan(source, 2, destination, options);

        byte[] expected = new byte[]
        {
            12, 34, 56, 78,
            0, 255, 0, 255
        };
        Assert.That(selected.Frames.Length, Is.EqualTo(1));
        Assert.That(selected.Frames[0].FrameIndex, Is.EqualTo(2));
        Assert.That(selected.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(expected));
        Assert.That(written, Is.EqualTo(8));
        Assert.That(destination, Is.EqualTo(expected));
    }

    static GifDecodeOptions CreateSolidOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.SolidColor;
        options.SolidBackgroundColor = new GifRgbaColor(12, 34, 56, 78);
        return options;
    }
}
