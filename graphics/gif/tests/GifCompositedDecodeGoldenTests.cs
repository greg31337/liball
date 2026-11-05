using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifCompositedDecodeGoldenTests
{
    [Test]
    public void DecodeAnimation_SinglePixelGlobalPaletteFixturesMatchCompositedGoldens()
    {
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.MinimalGif87a()), -1);
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.MinimalGif89a()), -1);
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.NetscapeLoopGif89a()), 5);
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.CommentGif89a()), -1);
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.ApplicationExtensionGif89a()), -1);
        AssertSingleBlackFrame(Gif.DecodeAnimation(GifFixtures.PlainTextGif89a()), -1);
    }

    [Test]
    public void DecodeAnimation_TransparencyAndLocalPaletteFixturesMatchCompositedGoldens()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.BackgroundMode = GifBackgroundMode.Transparent;

        GifAnimation transparent = Gif.DecodeAnimation(GifFixtures.TransparentGif89a(), options);
        GifAnimation local = Gif.DecodeAnimation(GifFixtures.LocalColorTableGif89a(), options);

        Assert.That(transparent.Width, Is.EqualTo(1));
        Assert.That(transparent.Height, Is.EqualTo(1));
        Assert.That(transparent.Frames.Length, Is.EqualTo(1));
        Assert.That(transparent.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(local.Width, Is.EqualTo(1));
        Assert.That(local.Height, Is.EqualTo(1));
        Assert.That(local.Frames.Length, Is.EqualTo(1));
        Assert.That(local.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 255, 255 }));
    }

    [Test]
    public void DecodeAnimation_InterlacedFixtureMatchesCompositedRowGolden()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.InterlacedGif89a());

        Assert.That(animation.Width, Is.EqualTo(2));
        Assert.That(animation.Height, Is.EqualTo(4));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255, 0, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255,
            0, 255, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 0, 0, 255, 255
        }));
    }

    [Test]
    public void DecodeAnimation_AnimatedDisposalFixturesMatchCompositedGoldens()
    {
        AssertAnimatedDisposalGolden(
            GifFixtures.AnimatedDisposalMethod0Gif89a(),
            GifDisposalMethod.DoNotDispose,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 255
            });
        AssertAnimatedDisposalGolden(
            GifFixtures.AnimatedDisposalMethod1Gif89a(),
            GifDisposalMethod.DoNotDispose,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 255
            });
        AssertAnimatedDisposalGolden(
            GifFixtures.AnimatedDisposalMethod2Gif89a(),
            GifDisposalMethod.RestoreToBackground,
            new byte[]
            {
                0, 0, 0, 255,
                0, 255, 0, 255
            });
        AssertAnimatedDisposalGolden(
            GifFixtures.AnimatedDisposalMethod3Gif89a(),
            GifDisposalMethod.RestoreToPrevious,
            new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            });
    }

    static void AssertSingleBlackFrame(GifAnimation animation, int loopCount)
    {
        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.LoopCount, Is.EqualTo(loopCount));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(0));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(0));
        Assert.That(animation.Frames[0].SourceLeft, Is.EqualTo(0));
        Assert.That(animation.Frames[0].SourceTop, Is.EqualTo(0));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    static void AssertAnimatedDisposalGolden(byte[] data, GifDisposalMethod expectedMiddleDisposal, byte[] expectedLastFrame)
    {
        GifAnimation animation = Gif.DecodeAnimation(data);

        Assert.That(animation.Width, Is.EqualTo(2));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.LoopCount, Is.EqualTo(-1));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(30));
        Assert.That(animation.Frames.Length, Is.EqualTo(3));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.Frames[2].DelayMilliseconds, Is.EqualTo(10));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            0, 0, 0, 255
        }));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 255, 255,
            0, 0, 0, 255
        }));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(expectedMiddleDisposal));
        Assert.That(animation.Frames[2].SourceLeft, Is.EqualTo(1));
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(expectedLastFrame));
    }
}
