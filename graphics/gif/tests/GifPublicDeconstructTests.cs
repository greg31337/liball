using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicDeconstructTests
{
    [Test]
    public void Deconstruct_CropsFramesToChangedBounds()
    {
        GifAnimation animation = new GifAnimation();
        animation.Width = 3;
        animation.Height = 2;
        animation.LoopCount = 2;
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255
            }, 10, 0),
            CreateFrame(3, 2, new byte[]
            {
                0, 0, 0, 255,
                9, 9, 9, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                0, 0, 0, 255,
                8, 8, 8, 255
            }, 20, 1)
        };

        GifAnimation deconstructed = Gif.Deconstruct(animation);

        Assert.That(deconstructed.Width, Is.EqualTo(3));
        Assert.That(deconstructed.Height, Is.EqualTo(2));
        Assert.That(deconstructed.LoopCount, Is.EqualTo(2));
        Assert.That(deconstructed.TotalDurationMilliseconds, Is.EqualTo(30));
        Assert.That(deconstructed.Frames.Length, Is.EqualTo(2));
        AssertFrameRectangle(deconstructed.Frames[0], 0, 0, 3, 2);
        AssertFrameRectangle(deconstructed.Frames[1], 1, 0, 2, 2);
        Assert.That(deconstructed.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            9, 9, 9, 255,
            0, 0, 0, 255,
            0, 0, 0, 255,
            8, 8, 8, 255
        }));
    }

    [Test]
    public void Deconstruct_UsesTransparentPlaceholderForUnchangedFrame()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0),
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 20, 1)
        };

        GifAnimation deconstructed = Gif.Deconstruct(animation);

        Assert.That(deconstructed.Frames.Length, Is.EqualTo(2));
        AssertFrameRectangle(deconstructed.Frames[1], 0, 0, 1, 1);
        Assert.That(deconstructed.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(deconstructed.Frames[1].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(deconstructed.TotalDurationMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void Deconstruct_DoesNotMutateSourceFramesOrBitmaps()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }, 10, 0)
        };

        GifAnimation deconstructed = Gif.Deconstruct(animation);

        Assert.That(deconstructed.Frames[0], Is.Not.SameAs(animation.Frames[0]));
        Assert.That(deconstructed.Frames[0].Bitmap, Is.Not.SameAs(animation.Frames[0].Bitmap));
        Assert.That(deconstructed.Frames[0].Bitmap.RgbaPixels, Is.Not.SameAs(animation.Frames[0].Bitmap.RgbaPixels));

        deconstructed.Frames[0].Bitmap.RgbaPixels[0] = 99;

        Assert.That(animation.Frames[0].Bitmap.RgbaPixels[0], Is.EqualTo(1));
    }

    [Test]
    public void Deconstruct_NullAnimationReturnsEmptyAnimation()
    {
        GifAnimation deconstructed = Gif.Deconstruct(null);

        Assert.That(deconstructed.Width, Is.EqualTo(0));
        Assert.That(deconstructed.Height, Is.EqualTo(0));
        Assert.That(deconstructed.Frames, Is.Empty);
    }

    [Test]
    public void Deconstruct_InvalidAnimationReturnsEmptyAnimation()
    {
        GifAnimation animation = new GifAnimation();
        animation.Frames = new GifAnimationFrame[]
        {
            CreateFrame(new byte[] { 1, 2, 3, 255 }),
            CreateFrame(2, 1, new byte[] { 1, 2, 3, 255, 1, 2, 3, 255 }, 0, 1)
        };

        GifAnimation deconstructed = Gif.Deconstruct(animation);

        Assert.That(deconstructed.Frames, Is.Empty);
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels)
    {
        return CreateFrame(rgbaPixels, 0, -1);
    }

    static GifAnimationFrame CreateFrame(byte[] rgbaPixels, int delayMilliseconds, int frameIndex)
    {
        return CreateFrame(1, 1, rgbaPixels, delayMilliseconds, frameIndex);
    }

    static GifAnimationFrame CreateFrame(int width, int height, byte[] rgbaPixels, int delayMilliseconds, int frameIndex)
    {
        GifAnimationFrame frame = new GifAnimationFrame();
        frame.Bitmap = new GifBitmap(width, height, rgbaPixels);
        frame.DelayMilliseconds = delayMilliseconds;
        frame.FrameIndex = frameIndex;
        return frame;
    }

    static void AssertFrameRectangle(GifAnimationFrame frame, int left, int top, int width, int height)
    {
        Assert.That(frame.SourceLeft, Is.EqualTo(left));
        Assert.That(frame.SourceTop, Is.EqualTo(top));
        Assert.That(frame.SourceWidth, Is.EqualTo(width));
        Assert.That(frame.SourceHeight, Is.EqualTo(height));
        Assert.That(frame.Bitmap.Width, Is.EqualTo(width));
        Assert.That(frame.Bitmap.Height, Is.EqualTo(height));
    }
}
