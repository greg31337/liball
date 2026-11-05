using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifFrameDelayEncodeTests
{
    [Test]
    public void ConvertMillisecondsToHundredths_CeilsPositiveDelays()
    {
        GifFrameDelayEncoder encoder = new GifFrameDelayEncoder();

        Assert.That(encoder.ConvertMillisecondsToHundredths(0), Is.EqualTo(0));
        Assert.That(encoder.ConvertMillisecondsToHundredths(1), Is.EqualTo(1));
        Assert.That(encoder.ConvertMillisecondsToHundredths(10), Is.EqualTo(1));
        Assert.That(encoder.ConvertMillisecondsToHundredths(25), Is.EqualTo(3));
        Assert.That(encoder.ConvertMillisecondsToHundredths(655350), Is.EqualTo(65535));
    }

    [Test]
    public void EncodeIndexedAnimation_WritesFrameDelaysAsGraphicControlExtensions()
    {
        GifPalette palette = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
        GifAnimationFrame first = CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette), 50);
        GifAnimationFrame second = CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 1 }, palette), 120);
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[] { first, second };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.ExtensionCount, Is.EqualTo(2));
        Assert.That(parsed.Document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(parsed.Document.Frames[1].DelayHundredths, Is.EqualTo(12));
        Assert.That(parsed.Document.Frames[0].GraphicControl.DelayHundredths, Is.EqualTo(5));
        Assert.That(parsed.Document.Frames[1].GraphicControl.DelayHundredths, Is.EqualTo(12));
    }

    [Test]
    public void EncodeRgbaAnimation_PreservesFrameDelaysThroughQuantization()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 1;
        animation.Height = 1;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 255, 0, 0, 255 }), 30),
            CreateRgbaFrame(new GifBitmap(1, 1, new byte[] { 0, 0, 255, 255 }), 70)
        };

        GifEncodeResult encoded = Gif.EncodeRgbaAnimation(animation);
        GifBlockReadResult parsed = new GifBlockReader().Read(encoded.Data);

        Assert.That(encoded.Success, Is.True);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(parsed.Document.Frames[0].DelayHundredths, Is.EqualTo(3));
        Assert.That(parsed.Document.Frames[1].DelayHundredths, Is.EqualTo(7));
    }

    [Test]
    public void EncodeIndexedAnimation_RejectsInvalidFrameDelay()
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
            CreateIndexedFrame(GifBitmap.CreateIndexed(1, 1, new byte[] { 0 }, palette), 655351)
        };

        GifEncodeResult encoded = Gif.EncodeIndexedAnimation(animation);

        Assert.That(encoded.Success, Is.False);
        Assert.That(encoded.Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(encoded.Error.Position.FrameIndex, Is.EqualTo(0));
        Assert.That(encoded.Data, Is.Empty);
    }

    static GifAnimationFrame CreateIndexedFrame(GifBitmap bitmap, int delayMilliseconds)
    {
        return CreateFrame(bitmap, delayMilliseconds);
    }

    static GifAnimationFrame CreateRgbaFrame(GifBitmap bitmap, int delayMilliseconds)
    {
        return CreateFrame(bitmap, delayMilliseconds);
    }

    static GifAnimationFrame CreateFrame(GifBitmap bitmap, int delayMilliseconds)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = bitmap;
        frame.SourceLeft = 0;
        frame.SourceTop = 0;
        frame.SourceWidth = bitmap.Width;
        frame.SourceHeight = bitmap.Height;
        frame.DelayMilliseconds = delayMilliseconds;
        return frame;
    }
}
