using webp;

namespace webp.Tests;

[TestFixture]
public class WebpPixelUtilitiesApiTests
{
    [Test]
    public void CopyRgbaToBgra_CompactsPaddedRowsAndSwapsChannels()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer source = NewStraightBuffer(1, 2, 8, new byte[]
        {
            1, 2, 3, 4, 91, 92, 93, 94,
            5, 6, 7, 8, 81, 82, 83, 84
        });

        byte[] result = utilities.CopyRgbaToBgra(source);

        Assert.That(result, Is.EqualTo(new byte[]
        {
            3, 2, 1, 4,
            7, 6, 5, 8
        }));
    }

    [Test]
    public void ConvertStraightToPremultipliedAlpha_HandlesAlphaEdgesAndPreservesPadding()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer source = NewStraightBuffer(3, 1, 16, new byte[]
        {
            200, 100, 50, 255,
            200, 100, 50, 128,
            200, 100, 50, 0,
            9, 8, 7, 6
        });

        WebpPremultipliedPixelBuffer result = utilities.ConvertStraightToPremultipliedAlpha(source);
        result.Rgba[0] = 77;

        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Height, Is.EqualTo(1));
        Assert.That(result.Stride, Is.EqualTo(16));
        Assert.That(result.Rgba, Is.EqualTo(new byte[]
        {
            77, 100, 50, 255,
            100, 50, 25, 128,
            0, 0, 0, 0,
            9, 8, 7, 6
        }));
        Assert.That(source.Rgba[0], Is.EqualTo(200));
    }

    [Test]
    public void ConvertPremultipliedToStraightAlpha_HandlesZeroAndClampsInvalidChannels()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPremultipliedPixelBuffer source = NewPremultipliedBuffer(3, 1, 16, new byte[]
        {
            100, 50, 25, 128,
            1, 2, 3, 0,
            200, 1, 0, 100,
            9, 8, 7, 6
        });

        WebpPixelBuffer result = utilities.ConvertPremultipliedToStraightAlpha(source);

        Assert.That(result.Rgba, Is.EqualTo(new byte[]
        {
            199, 100, 50, 128,
            0, 0, 0, 0,
            255, 3, 0, 100,
            9, 8, 7, 6
        }));
        Assert.That(result.Rgba, Is.Not.SameAs(source.Rgba));
    }

    [Test]
    public void HasNonOpaqueAlpha_InspectsPixelsButNotPadding()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer opaque = NewStraightBuffer(1, 1, 8, new byte[] { 1, 2, 3, 255, 0, 0, 0, 0 });
        WebpPixelBuffer transparent = NewStraightBuffer(1, 1, 8, new byte[] { 1, 2, 3, 254, 255, 255, 255, 255 });

        Assert.That(utilities.HasNonOpaqueAlpha(opaque), Is.False);
        Assert.That(utilities.HasNonOpaqueAlpha(transparent), Is.True);
    }

    [Test]
    public void BlendOverBackground_CompositesStraightAlphaWithoutChangingSource()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpPixelBuffer source = NewStraightBuffer(2, 1, 12, new byte[]
        {
            255, 0, 0, 128,
            9, 8, 7, 0,
            99, 98, 97, 96
        });

        WebpPixelBuffer result = utilities.BlendOverBackground(source, 0xFF112233U);

        Assert.That(result.Stride, Is.EqualTo(12));
        Assert.That(result.Rgba, Is.EqualTo(new byte[]
        {
            136, 17, 25, 255,
            17, 34, 51, 255,
            0, 0, 0, 0
        }));
        Assert.That(source.Rgba[0], Is.EqualTo(255));
    }

    [Test]
    public void FlattenAnimationFrames_AppliesDisposalAndReturnsIndependentCanvasFrames()
    {
        IWebpPixelUtilities utilities = new WebpCodec();
        WebpImage image = new WebpImage();
        image.CanvasWidth = 2;
        image.CanvasHeight = 1;
        image.BackgroundColorBgra = 0xFF010203U;

        WebpFrame first = NewFrame(0, new byte[] { 255, 0, 0, 255 });
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        first.DurationMilliseconds = 10;
        image.Frames.Add(first);

        WebpFrame second = NewFrame(1, new byte[] { 0, 0, 255, 128 });
        second.BlendMode = WebpFrameBlendMode.AlphaBlend;
        second.DurationMilliseconds = 20;
        image.Frames.Add(second);

        List<WebpFrame> result = utilities.FlattenAnimationFrames(image);
        result[0].Pixels!.Rgba[0] = 77;

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Width, Is.EqualTo(2));
        Assert.That(result[0].DurationMilliseconds, Is.EqualTo(10));
        Assert.That(result[1].DurationMilliseconds, Is.EqualTo(20));
        Assert.That(result[1].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            1, 2, 3, 255,
            0, 1, 129, 255
        }));
        Assert.That(first.Pixels!.Rgba[0], Is.EqualTo(255));
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

    static WebpPremultipliedPixelBuffer NewPremultipliedBuffer(int width, int height, int stride, byte[] rgba)
    {
        WebpPremultipliedPixelBuffer buffer = new WebpPremultipliedPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Rgba = rgba;
        return buffer;
    }

    static WebpFrame NewFrame(int x, byte[] rgba)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Width = 1;
        frame.Height = 1;
        frame.Pixels = NewStraightBuffer(1, 1, 4, rgba);
        return frame;
    }
}
