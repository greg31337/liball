using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPixelUtilityConformanceTests
{
    [Test]
    public void PublicConversionsRespectPaddedRowsChannelOrderAndAlphaEdges()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer source = NewStraightBuffer(4, 1, 20, new byte[]
        {
            1, 2, 3, 255,
            255, 127, 1, 128,
            255, 128, 64, 1,
            9, 8, 7, 0,
            91, 92, 93, 94
        });

        byte[] bgra = utilities.CopyRgbaToBgra(source);
        WebpPremultipliedPixelBuffer premultiplied = utilities.ConvertStraightToPremultipliedAlpha(source);
        WebpPixelBuffer straight = utilities.ConvertPremultipliedToStraightAlpha(premultiplied);

        Assert.That(bgra, Is.EqualTo(new byte[]
        {
            3, 2, 1, 255,
            1, 127, 255, 128,
            64, 128, 255, 1,
            7, 8, 9, 0
        }));
        Assert.That(premultiplied.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            128, 64, 1, 128,
            1, 1, 0, 1,
            0, 0, 0, 0,
            91, 92, 93, 94
        }));
        Assert.That(straight.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            255, 128, 2, 128,
            255, 255, 0, 1,
            0, 0, 0, 0,
            91, 92, 93, 94
        }));
        Assert.That(source.Rgba[12], Is.EqualTo(9));
    }

    [Test]
    public void PublicAlphaScanIgnoresPaddingAcrossRows()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer opaque = NewStraightBuffer(2, 2, 12, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 0, 0, 0, 0,
            7, 8, 9, 255, 10, 11, 12, 255, 0, 0, 0, 0
        });
        WebpPixelBuffer transparent = NewStraightBuffer(2, 2, 12, new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 255, 255, 255, 255,
            7, 8, 9, 0, 10, 11, 12, 255, 255, 255, 255, 255
        });

        Assert.That(utilities.HasNonOpaqueAlpha(opaque), Is.False);
        Assert.That(utilities.HasNonOpaqueAlpha(transparent), Is.True);
    }

    [Test]
    public void PublicBackgroundBlendHandlesTransparentPartialAndOpaquePixelsWithPadding()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer source = NewStraightBuffer(3, 1, 16, new byte[]
        {
            200, 100, 50, 128,
            9, 8, 7, 0,
            1, 2, 3, 255,
            99, 98, 97, 96
        });

        WebpPixelBuffer result = utilities.BlendOverBackground(source, 0x800A141EU);

        Assert.That(result.Stride, Is.EqualTo(16));
        Assert.That(result.Rgba, Is.EqualTo(new byte[]
        {
            137, 73, 43, 192,
            10, 20, 30, 128,
            1, 2, 3, 255,
            0, 0, 0, 0
        }));
        Assert.That(source.Rgba[0], Is.EqualTo(200));
    }

    [Test]
    public void PublicFlattenClipsWorkToPartialRectanglesAndDisposesOnlyTheirArea()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 2;
        image.BackgroundColorBgra = 0xFF010203U;
        WebpFrame first = new WebpFrame();
        first.X = 1;
        first.Y = 0;
        first.Width = 2;
        first.Height = 2;
        first.DurationMilliseconds = 10;
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        first.Pixels = NewStraightBuffer(2, 2, 12, new byte[]
        {
            255, 0, 0, 255, 9, 8, 7, 0, 91, 92, 93, 94,
            0, 0, 255, 255, 255, 255, 0, 128, 81, 82, 83, 84
        });
        WebpFrame second = NewFrame(0, 1, 1, 1, 20, new byte[] { 0, 255, 0, 255 });
        second.BlendMode = WebpFrameBlendMode.Replace;
        image.Frames.Add(first);
        image.Frames.Add(second);

        List<WebpFrame> result = utilities.FlattenAnimationFrames(image);

        Assert.That(result[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255, 255, 0, 0, 255, 9, 8, 7, 0,
            1, 2, 3, 255, 0, 0, 255, 255, 255, 255, 0, 128
        }));
        Assert.That(result[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255, 1, 2, 3, 255, 1, 2, 3, 255,
            0, 255, 0, 255, 1, 2, 3, 255, 1, 2, 3, 255
        }));
        Assert.That(first.Pixels.Rgba[8], Is.EqualTo(91));
    }

    [Test]
    public void PublicFlattenKeepsClippedEmptyFramesAsTimingSnapshots()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpImage image = new WebpImage();
        image.CanvasWidth = 2;
        image.CanvasHeight = 1;
        image.BackgroundColorBgra = 0xFF010203U;
        WebpFrame clipped = new WebpFrame();
        clipped.Width = 0;
        clipped.Height = 0;
        clipped.DurationMilliseconds = 10;
        clipped.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        clipped.Pixels = new WebpPixelBuffer();
        WebpFrame visible = NewFrame(1, 0, 1, 1, 20, new byte[] { 255, 0, 0, 255 });
        visible.BlendMode = WebpFrameBlendMode.Replace;
        image.Frames.Add(clipped);
        image.Frames.Add(visible);

        List<WebpFrame> result = utilities.FlattenAnimationFrames(image);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].DurationMilliseconds, Is.EqualTo(10));
        Assert.That(result[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            1, 2, 3, 255
        }));
        Assert.That(result[1].DurationMilliseconds, Is.EqualTo(20));
        Assert.That(result[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            255, 0, 0, 255
        }));
    }

    [Test]
    public void PublicFlattenRejectsInvalidBlendAndDisposalModes()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpImage invalidBlend = NewOneFrameImage();
        invalidBlend.Frames[0].BlendMode = (WebpFrameBlendMode)99;
        WebpImage invalidDispose = NewOneFrameImage();
        invalidDispose.Frames[0].DisposeMode = (WebpFrameDisposeMode)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => utilities.FlattenAnimationFrames(invalidBlend));
        Assert.Throws<ArgumentOutOfRangeException>(() => utilities.FlattenAnimationFrames(invalidDispose));
    }

    static WebpImage NewOneFrameImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 1;
        image.CanvasHeight = 1;
        image.Frames.Add(NewFrame(0, 0, 1, 1, 1, new byte[] { 1, 2, 3, 255 }));
        return image;
    }

    static WebpFrame NewFrame(int x, int y, int width, int height, int duration, byte[] rgba)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = y;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = duration;
        frame.Pixels = NewStraightBuffer(width, height, width * 4, rgba);
        return frame;
    }

    static WebpPixelBuffer NewStraightBuffer(int width, int height, int stride, byte[] rgba)
    {
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Rgba = rgba;
        return buffer;
    }
}
