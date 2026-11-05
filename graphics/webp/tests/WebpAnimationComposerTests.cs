using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAnimationComposerTests
{
    [Test]
    public void ComposeFrames_ReplaceFramesAndDisposeToBackground()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame first = NewFrame(0, 0, 2, 1, new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255
        });
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        first.DurationMilliseconds = 10;
        frames.Add(first);

        WebpFrame second = NewFrame(1, 0, 1, 1, new byte[]
        {
            0, 0, 255, 255
        });
        second.BlendMode = WebpFrameBlendMode.Replace;
        second.DurationMilliseconds = 20;
        frames.Add(second);

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 3, 1, 0xFF112233U);

        Assert.That(composited, Has.Count.EqualTo(2));
        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255,
            0, 255, 0, 255,
            17, 34, 51, 255
        }));
        Assert.That(composited[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            17, 34, 51, 255,
            0, 0, 255, 255,
            17, 34, 51, 255
        }));
        Assert.That(composited[0].DurationMilliseconds, Is.EqualTo(10));
        Assert.That(composited[1].DurationMilliseconds, Is.EqualTo(20));
    }

    [Test]
    public void ComposeFrames_AlphaBlendsOverOpaqueCanvas()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame frame = NewFrame(0, 0, 1, 1, new byte[] { 255, 0, 0, 128 });
        frame.BlendMode = WebpFrameBlendMode.AlphaBlend;
        frames.Add(frame);

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 1, 1, 0xFF000000U);

        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[] { 128, 0, 0, 255 }));
    }

    [Test]
    public void ComposeFrames_AlphaBlendsOverTransparentCanvas()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame frame = NewFrame(0, 0, 1, 1, new byte[] { 255, 0, 0, 128 });
        frame.BlendMode = WebpFrameBlendMode.AlphaBlend;
        frames.Add(frame);

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 1, 1, 0);

        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[] { 255, 0, 0, 128 }));
    }

    [Test]
    public void ComposeFrames_ReplaceCopiesTransparentPixelsWithoutBlending()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        WebpFrame frame = NewFrame(0, 0, 1, 1, new byte[] { 255, 0, 0, 0 });
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frames.Add(frame);

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 1, 1, 0xFF010203U);

        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[] { 255, 0, 0, 0 }));
    }

    [Test]
    public void ComposeFrames_ReturnsFullCanvasIndependentFrameCopies()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(NewFrame(0, 0, 1, 1, new byte[] { 1, 2, 3, 255 }));
        frames.Add(NewFrame(0, 0, 1, 1, new byte[] { 4, 5, 6, 255 }));

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(frames, 1, 1, 0);
        composited[0].Pixels!.Rgba[0] = 99;

        Assert.That(composited[0].X, Is.EqualTo(0));
        Assert.That(composited[0].Y, Is.EqualTo(0));
        Assert.That(composited[0].Width, Is.EqualTo(1));
        Assert.That(composited[0].Height, Is.EqualTo(1));
        Assert.That(composited[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(composited[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DoNotDispose));
        Assert.That(composited[1].Pixels!.Rgba, Is.EqualTo(new byte[] { 4, 5, 6, 255 }));
    }

    [Test]
    public void ComposeFrames_ComposesFromImageModel()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewFrame(0, 0, 1, 1, new byte[] { 4, 5, 6, 255 }));

        List<WebpFrame> composited = WebpAnimationComposer.ComposeFrames(image);

        Assert.That(composited, Has.Count.EqualTo(1));
        Assert.That(composited[0].Pixels!.Rgba, Is.EqualTo(new byte[] { 4, 5, 6, 255 }));
    }

    [Test]
    public void ComposeFrames_RejectsInvalidFrameInput()
    {
        WebpFrame missingPixels = new WebpFrame();
        missingPixels.Width = 1;
        missingPixels.Height = 1;
        WebpFrame outside = NewFrame(1, 0, 1, 1, new byte[] { 1, 2, 3, 255 });
        WebpFrame mismatch = NewFrame(0, 0, 1, 1, new byte[] { 1, 2, 3, 255 });
        mismatch.Width = 2;

        Assert.Throws<ArgumentException>(() => WebpAnimationComposer.ComposeFrames(NewFrameList(missingPixels), 1, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpAnimationComposer.ComposeFrames(NewFrameList(outside), 1, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpAnimationComposer.ComposeFrames(NewFrameList(mismatch), 1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpAnimationComposer.ComposeFrames(NewFrameList(NewFrame(0, 0, 1, 1, new byte[] { 1, 2, 3, 255 })), 0, 1, 0));
    }

    static WebpFrame NewFrame(int x, int y, int width, int height, byte[] rgba)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        WebpPixelBuffer pixels = WebpPixelBufferHelper.AllocateRgba(width, height);
        Array.Copy(rgba, pixels.Rgba, rgba.Length);
        frame.Pixels = pixels;
        return frame;
    }

    static List<WebpFrame> NewFrameList(WebpFrame frame)
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(frame);
        return frames;
    }
}
