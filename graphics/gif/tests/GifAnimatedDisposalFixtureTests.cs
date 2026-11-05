using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifAnimatedDisposalFixtureTests
{
    [Test]
    public void AnimatedDisposalFixtures_HaveExpectedBytes()
    {
        AssertAnimatedDisposalBytes(GifFixtures.AnimatedDisposalMethod0Gif89a(), 0);
        AssertAnimatedDisposalBytes(GifFixtures.AnimatedDisposalMethod1Gif89a(), 1);
        AssertAnimatedDisposalBytes(GifFixtures.AnimatedDisposalMethod2Gif89a(), 2);
        AssertAnimatedDisposalBytes(GifFixtures.AnimatedDisposalMethod3Gif89a(), 3);
    }

    [Test]
    public void AnimatedDisposalFixtures_IdentifyAsThreeFrameGif89aAnimations()
    {
        AssertAnimatedDisposalIdentity(GifFixtures.AnimatedDisposalMethod0Gif89a());
        AssertAnimatedDisposalIdentity(GifFixtures.AnimatedDisposalMethod1Gif89a());
        AssertAnimatedDisposalIdentity(GifFixtures.AnimatedDisposalMethod2Gif89a());
        AssertAnimatedDisposalIdentity(GifFixtures.AnimatedDisposalMethod3Gif89a());
    }

    [Test]
    public void AnimatedDisposalFixtures_DecodeRawDisposalMethods()
    {
        AssertRawDisposalMethod(GifFixtures.AnimatedDisposalMethod0Gif89a(), GifDisposalMethod.Unspecified);
        AssertRawDisposalMethod(GifFixtures.AnimatedDisposalMethod1Gif89a(), GifDisposalMethod.DoNotDispose);
        AssertRawDisposalMethod(GifFixtures.AnimatedDisposalMethod2Gif89a(), GifDisposalMethod.RestoreToBackground);
        AssertRawDisposalMethod(GifFixtures.AnimatedDisposalMethod3Gif89a(), GifDisposalMethod.RestoreToPrevious);
    }

    [Test]
    public void AnimatedDisposalMethod0Fixture_CoalescesWithDefaultNoDisposeFallback()
    {
        AssertCoalescedFrames(
            GifFixtures.AnimatedDisposalMethod0Gif89a(),
            GifDisposalMethod.DoNotDispose,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 255
            });
    }

    [Test]
    public void AnimatedDisposalMethod1Fixture_CoalescesWithPreviousPixelsPreserved()
    {
        AssertCoalescedFrames(
            GifFixtures.AnimatedDisposalMethod1Gif89a(),
            GifDisposalMethod.DoNotDispose,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 255
            });
    }

    [Test]
    public void AnimatedDisposalMethod2Fixture_CoalescesWithBackgroundRestored()
    {
        AssertCoalescedFrames(
            GifFixtures.AnimatedDisposalMethod2Gif89a(),
            GifDisposalMethod.RestoreToBackground,
            new byte[]
            {
                0, 0, 0, 255,
                0, 255, 0, 255
            });
    }

    [Test]
    public void AnimatedDisposalMethod3Fixture_CoalescesWithPreviousCanvasRestored()
    {
        AssertCoalescedFrames(
            GifFixtures.AnimatedDisposalMethod3Gif89a(),
            GifDisposalMethod.RestoreToPrevious,
            new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 255
            });
    }

    [Test]
    public void AnimatedDisposalFixtures_RoundTripExactly()
    {
        AssertRoundTripsExactly(GifFixtures.AnimatedDisposalMethod0Gif89a());
        AssertRoundTripsExactly(GifFixtures.AnimatedDisposalMethod1Gif89a());
        AssertRoundTripsExactly(GifFixtures.AnimatedDisposalMethod2Gif89a());
        AssertRoundTripsExactly(GifFixtures.AnimatedDisposalMethod3Gif89a());
    }

    static void AssertAnimatedDisposalBytes(byte[] data, int disposalMethod)
    {
        Assert.That(data.Length, Is.EqualTo(95));
        Assert.That(data[0], Is.EqualTo(71));
        Assert.That(data[1], Is.EqualTo(73));
        Assert.That(data[2], Is.EqualTo(70));
        Assert.That(data[3], Is.EqualTo(56));
        Assert.That(data[4], Is.EqualTo(57));
        Assert.That(data[5], Is.EqualTo(97));
        Assert.That(data[10], Is.EqualTo(0x81));
        Assert.That(data[25], Is.EqualTo(0x21));
        Assert.That(data[26], Is.EqualTo(0xF9));
        Assert.That(data[51], Is.EqualTo(disposalMethod * 4));
        Assert.That(data[52], Is.EqualTo(1));
        Assert.That(data[68], Is.EqualTo(84));
        Assert.That(data[80], Is.EqualTo(1));
        Assert.That(data[data.Length - 1], Is.EqualTo(0x3B));
    }

    static void AssertAnimatedDisposalIdentity(byte[] data)
    {
        GifInfo info = Gif.Identify(data);

        Assert.That(info.Success, Is.True);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(3));
    }

    static void AssertRawDisposalMethod(byte[] data, GifDisposalMethod expected)
    {
        GifDocument document = Gif.Decode(data);

        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(document.Frames.Length, Is.EqualTo(3));
        Assert.That(document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[1].DisposalMethod, Is.EqualTo(expected));
        Assert.That(document.Frames[1].GraphicControl.DisposalMethod, Is.EqualTo(expected));
        Assert.That(document.Frames[2].DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(document.Frames[0].CompressedImageData, Is.EqualTo(new byte[] { 76, 1 }));
        Assert.That(document.Frames[1].CompressedImageData, Is.EqualTo(new byte[] { 84, 1 }));
        Assert.That(document.Frames[2].CompressedImageData, Is.EqualTo(new byte[] { 92, 1 }));
    }

    static void AssertCoalescedFrames(byte[] data, GifDisposalMethod expectedMiddleDisposal, byte[] expectedLastFrame)
    {
        GifAnimation animation = Gif.DecodeAnimation(data);

        Assert.That(animation.Width, Is.EqualTo(2));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(3));
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
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(expectedLastFrame));
    }

    static void AssertRoundTripsExactly(byte[] data)
    {
        GifDocument document = Gif.Decode(data);

        byte[] encoded = Gif.Encode(document);

        Assert.That(encoded, Is.EqualTo(data));
    }
}
