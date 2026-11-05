using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8RgbaToYuvConverterTests
{
    [Test]
    public void ConvertRgbaToYuv420_ConvertsNeutralBlackAndWhite()
    {
        byte[] rgba = new byte[]
        {
            0, 0, 0, 255,
            255, 255, 255, 255
        };

        WebpVp8Yuv420Buffer yuv = WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(rgba, 2, 1, 8);

        Assert.That(yuv.Width, Is.EqualTo(2));
        Assert.That(yuv.Height, Is.EqualTo(1));
        Assert.That(yuv.Y, Is.EqualTo(new byte[] { 16, 235 }));
        Assert.That(yuv.U, Is.EqualTo(new byte[] { 128 }));
        Assert.That(yuv.V, Is.EqualTo(new byte[] { 128 }));
    }

    [Test]
    public void ConvertRgbaToYuv420_AveragesChromaForTwoByTwoBlocksAndOddEdges()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(3, 3);
        SetPixel(buffer, 0, 0, 255, 0, 0, 255);
        SetPixel(buffer, 1, 0, 0, 255, 0, 255);
        SetPixel(buffer, 2, 0, 0, 0, 255, 255);
        SetPixel(buffer, 0, 1, 255, 255, 0, 255);
        SetPixel(buffer, 1, 1, 0, 255, 255, 255);
        SetPixel(buffer, 2, 1, 255, 0, 255, 255);
        SetPixel(buffer, 0, 2, 20, 40, 60, 255);
        SetPixel(buffer, 1, 2, 80, 100, 120, 255);
        SetPixel(buffer, 2, 2, 140, 160, 180, 255);

        WebpVp8Yuv420Buffer yuv = WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(buffer);

        Assert.That(yuv.Y[0], Is.EqualTo(WebpVp8RgbaToYuvConverter.RgbToY(255, 0, 0)));
        Assert.That(yuv.UvStride, Is.EqualTo(2));
        Assert.That(yuv.U, Is.EqualTo(new byte[]
        {
            WebpVp8RgbaToYuvConverter.RgbToU(128, 191, 64),
            WebpVp8RgbaToYuvConverter.RgbToU(128, 0, 255),
            WebpVp8RgbaToYuvConverter.RgbToU(50, 70, 90),
            WebpVp8RgbaToYuvConverter.RgbToU(140, 160, 180)
        }));
        Assert.That(yuv.V, Is.EqualTo(new byte[]
        {
            WebpVp8RgbaToYuvConverter.RgbToV(128, 191, 64),
            WebpVp8RgbaToYuvConverter.RgbToV(128, 0, 255),
            WebpVp8RgbaToYuvConverter.RgbToV(50, 70, 90),
            WebpVp8RgbaToYuvConverter.RgbToV(140, 160, 180)
        }));
    }

    [Test]
    public void ConvertRgbaToYuv420_IgnoresAlphaAndPreservesDestinationStrideInput()
    {
        byte[] rgba = NewFilledBytes(32, 99);
        rgba[0] = 10;
        rgba[1] = 20;
        rgba[2] = 30;
        rgba[3] = 0;
        rgba[4] = 10;
        rgba[5] = 20;
        rgba[6] = 30;
        rgba[7] = 255;
        rgba[16] = 40;
        rgba[17] = 50;
        rgba[18] = 60;
        rgba[19] = 128;

        WebpVp8Yuv420Buffer yuv = WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(rgba, 2, 2, 16);

        Assert.That(yuv.Y[0], Is.EqualTo(yuv.Y[1]));
        Assert.That(yuv.Y[2], Is.EqualTo(WebpVp8RgbaToYuvConverter.RgbToY(40, 50, 60)));
        Assert.That(yuv.U.Length, Is.EqualTo(1));
        Assert.That(yuv.V.Length, Is.EqualTo(1));
    }

    [Test]
    public void RgbToYuv_UsesFixedPointClamping()
    {
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToY(0, 0, 0), Is.EqualTo(16));
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToY(255, 255, 255), Is.EqualTo(235));
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToU(255, 0, 0), Is.EqualTo(90));
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToV(255, 0, 0), Is.EqualTo(240));
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToU(0, 0, 255), Is.EqualTo(240));
        Assert.That(WebpVp8RgbaToYuvConverter.RgbToV(0, 0, 255), Is.EqualTo(110));
    }

    [Test]
    public void RgbaToYuvConverter_RejectsInvalidInputs()
    {
        byte[] rgba = new byte[16];

        Assert.Throws<ArgumentNullException>(() => WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420((WebpPixelBuffer)null!));
        Assert.Throws<ArgumentNullException>(() => WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(null!, 1, 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(rgba, 0, 1, 4));
        Assert.Throws<ArgumentException>(() => WebpVp8RgbaToYuvConverter.ConvertRgbaToYuv420(new byte[15], 2, 2, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8RgbaToYuvConverter.RgbToY(-1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8RgbaToYuvConverter.RgbToU(0, 256, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8RgbaToYuvConverter.RgbToV(0, 0, 256));
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = (y * buffer.Stride) + (x * 4);
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }

    static byte[] NewFilledBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index = index + 1;
        }

        return data;
    }
}
