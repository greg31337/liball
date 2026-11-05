using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicDecodeAnimationTests
{
    [Test]
    public void DecodeAnimation_FromBytesReturnsCoalescedAnimation()
    {
        GifEncodeResult encoded = CreateEncodedAnimation(50, 70);

        GifAnimation animation = Gif.DecodeAnimation(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.LoopCount, Is.EqualTo(2));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(120));
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(50));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(70));
        Assert.That(animation.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
    }

    [Test]
    public void DecodeAnimation_FromStreamReturnsCoalescedAnimation()
    {
        GifEncodeResult encoded = CreateEncodedAnimation(30, 40);
        MemoryStream stream = new MemoryStream(encoded.Data);

        GifAnimation animation = Gif.DecodeAnimation(stream);

        Assert.That(encoded.Success, Is.True);
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(70));
    }

    [Test]
    public void DecodeAnimation_HonorsFirstFrameOnlyOption()
    {
        GifEncodeResult encoded = CreateEncodedAnimation(50, 70);
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;

        GifAnimation animation = Gif.DecodeAnimation(encoded.Data, options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(50));
    }

    [Test]
    public void DecodeAnimation_NullOptionsUseDefaults()
    {
        GifAnimation animation = Gif.DecodeAnimation(GifFixtures.MinimalGif89a(), null);

        Assert.That(animation.Width, Is.EqualTo(1));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void DecodeAnimation_StreamHonorsMinimumDelayOption()
    {
        GifEncodeResult encoded = CreateEncodedAnimation(0, 0);
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 25;

        GifAnimation animation = Gif.DecodeAnimation(new MemoryStream(encoded.Data), options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(animation.Frames.Length, Is.EqualTo(2));
        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(50));
        Assert.That(animation.Warnings.Length, Is.EqualTo(2));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
        Assert.That(animation.Warnings[1].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
    }

    [Test]
    public void DecodeAnimation_InvalidBytesReturnEmptyAnimation()
    {
        GifAnimation animation = Gif.DecodeAnimation(new byte[] { 1, 2, 3 });

        Assert.That(animation.Width, Is.EqualTo(0));
        Assert.That(animation.Height, Is.EqualTo(0));
        Assert.That(animation.Frames, Is.Empty);
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(0));
    }

    static GifEncodeResult CreateEncodedAnimation(int firstDelayMilliseconds, int secondDelayMilliseconds)
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette), firstDelayMilliseconds),
            CreateFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette), secondDelayMilliseconds)
        };
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = 2;
        return Gif.EncodeIndexedAnimation(animation, options);
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int delayMilliseconds)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.DelayMilliseconds = delayMilliseconds;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        return frame;
    }
}
