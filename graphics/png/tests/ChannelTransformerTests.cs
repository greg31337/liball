using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChannelTransformerTests
{
    [Test]
    public void SwapRgbBgr_SwapsRgb8RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 7, new byte[]
        {
            1, 2, 3, 4, 5, 6, 99,
            7, 8, 9, 10, 11, 12, 99
        });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbBgr(source);

        AssertBuffer(result, PngPixelFormat.Bgr8, 2, 2, 6, new byte[]
        {
            3, 2, 1, 6, 5, 4,
            9, 8, 7, 12, 11, 10
        });
    }

    [Test]
    public void SwapRgbBgr_SwapsRgb16()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 12, new byte[]
        {
            17, 34, 51, 68, 85, 102,
            128, 1, 0, 255, 255, 0
        });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbBgr(source);

        AssertBuffer(result, PngPixelFormat.Bgr16, 2, 1, 12, new byte[]
        {
            85, 102, 51, 68, 17, 34,
            255, 0, 0, 255, 128, 1
        });
    }

    [Test]
    public void SwapRgbBgr_SwapsRgba8AndPreservesAlpha()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 40, 4, 5, 6, 50 });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbBgr(source);

        AssertBuffer(result, PngPixelFormat.Bgra8, 2, 1, 8, new byte[] { 3, 2, 1, 40, 6, 5, 4, 50 });
    }

    [Test]
    public void SwapRgbBgr_SwapsRgba16AndPreservesAlpha()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            17, 34, 51, 68, 85, 102, 119, 136,
            128, 1, 0, 255, 255, 0, 18, 52
        });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbBgr(source);

        AssertBuffer(result, PngPixelFormat.Bgra16, 2, 1, 16, new byte[]
        {
            85, 102, 51, 68, 17, 34, 119, 136,
            255, 0, 0, 255, 128, 1, 18, 52
        });
    }

    [Test]
    public void SwapRgbBgr_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbBgr(source);
        source.Data[2] = 99;

        AssertBuffer(result, PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 3, 2, 1 });
    }

    [Test]
    public void SwapRgbBgr_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngChannelTransformer.SwapRgbBgr(null!));
        Assert.Throws<ArgumentNullException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbBgr(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void SwapRgbaArgb_MovesRgba8AlphaFirstRowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 10, new byte[]
        {
            1, 2, 3, 40, 4, 5, 6, 50, 99, 99,
            7, 8, 9, 60, 10, 11, 12, 70, 99, 99
        });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbaArgb(source);

        AssertBuffer(result, PngPixelFormat.Argb8, 2, 2, 8, new byte[]
        {
            40, 1, 2, 3, 50, 4, 5, 6,
            60, 7, 8, 9, 70, 10, 11, 12
        });
    }

    [Test]
    public void SwapRgbaArgb_MovesRgba16AlphaFirst()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            17, 34, 51, 68, 85, 102, 119, 136,
            128, 1, 0, 255, 255, 0, 18, 52
        });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbaArgb(source);

        AssertBuffer(result, PngPixelFormat.Argb16, 2, 1, 16, new byte[]
        {
            119, 136, 17, 34, 51, 68, 85, 102,
            18, 52, 128, 1, 0, 255, 255, 0
        });
    }

    [Test]
    public void SwapRgbaArgb_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 });

        PngPixelBuffer result = PngChannelTransformer.SwapRgbaArgb(source);
        source.Data[3] = 99;

        AssertBuffer(result, PngPixelFormat.Argb8, 1, 1, 4, new byte[] { 4, 1, 2, 3 });
    }

    [Test]
    public void ChannelSwaps_AreReversibleThroughPublicFormats()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        PngPixelBuffer bgr = transformer.SwapRgbBgr(rgb);
        PngPixelBuffer rgbRoundTrip = transformer.SwapRgbBgr(bgr);
        PngPixelBuffer bgra = transformer.SwapRgbBgr(rgba);
        PngPixelBuffer rgbaFromBgra = transformer.SwapRgbBgr(bgra);
        PngPixelBuffer argb = transformer.SwapRgbaArgb(rgba);
        PngPixelBuffer rgbaFromArgb = transformer.SwapRgbaArgb(argb);

        AssertBuffer(bgr, PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 3, 2, 1 });
        AssertBuffer(rgbRoundTrip, PngPixelFormat.Rgb8, 1, 1, 3, rgb.Data);
        AssertBuffer(bgra, PngPixelFormat.Bgra16, 1, 1, 8, new byte[] { 5, 6, 3, 4, 1, 2, 7, 8 });
        AssertBuffer(rgbaFromBgra, PngPixelFormat.Rgba16, 1, 1, 8, rgba.Data);
        AssertBuffer(argb, PngPixelFormat.Argb16, 1, 1, 8, new byte[] { 7, 8, 1, 2, 3, 4, 5, 6 });
        AssertBuffer(rgbaFromArgb, PngPixelFormat.Rgba16, 1, 1, 8, rgba.Data);
    }

    [Test]
    public void SwapRgbaArgb_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngChannelTransformer.SwapRgbaArgb(null!));
        Assert.Throws<ArgumentNullException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgba8, 0, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgba8, 1, 0, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 7, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChannelTransformer.SwapRgbaArgb(CreateBuffer(PngPixelFormat.Rgba8, 2, 2, 8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    static void AssertBuffer(PngPixelBuffer result, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(format));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(stride));
        Assert.That(result.Data, Is.EqualTo(data));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }
}
