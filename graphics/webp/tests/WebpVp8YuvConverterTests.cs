using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8YuvConverterTests
{
    [Test]
    public void ConvertYuv420ToRgba_ConvertsNeutralBlackAndWhiteWithOpaqueAlpha()
    {
        byte[] y = new byte[] { 16, 235 };
        byte[] u = new byte[] { 128 };
        byte[] v = new byte[] { 128 };

        WebpPixelBuffer buffer = WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 2, 1, 2, 1);

        Assert.That(buffer.Width, Is.EqualTo(2));
        Assert.That(buffer.Height, Is.EqualTo(1));
        Assert.That(buffer.Rgba, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255, 255, 255 }));
    }

    [Test]
    public void ConvertYuv420ToRgba_ReusesChromaSamplesAcrossTwoByTwoPixelsAndHandlesOddDimensions()
    {
        byte[] y = new byte[]
        {
            80, 81, 82,
            83, 84, 85,
            86, 87, 88
        };
        byte[] u = new byte[]
        {
            90, 120,
            150, 180
        };
        byte[] v = new byte[]
        {
            200, 210,
            220, 230
        };

        WebpPixelBuffer buffer = WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 3, 3, 3, 2);

        AssertPixel(buffer.Rgba, 0, 80, 90, 200);
        AssertPixel(buffer.Rgba, 4, 81, 90, 200);
        AssertPixel(buffer.Rgba, 8, 82, 120, 210);
        AssertPixel(buffer.Rgba, 12, 83, 90, 200);
        AssertPixel(buffer.Rgba, 16, 84, 90, 200);
        AssertPixel(buffer.Rgba, 20, 85, 120, 210);
        AssertPixel(buffer.Rgba, 24, 86, 150, 220);
        AssertPixel(buffer.Rgba, 28, 87, 150, 220);
        AssertPixel(buffer.Rgba, 32, 88, 180, 230);
    }

    [Test]
    public void ConvertYuv420ToRgba_WritesToDestinationStrideAndLeavesPaddingUntouched()
    {
        byte[] y = new byte[] { 16, 235, 128, 128 };
        byte[] u = new byte[] { 128 };
        byte[] v = new byte[] { 128 };
        byte[] rgba = NewFilledBytes(32, 99);

        WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 2, 2, 2, 1, rgba, 16);

        Assert.That(rgba[0], Is.EqualTo(0));
        Assert.That(rgba[4], Is.EqualTo(255));
        Assert.That(rgba[8], Is.EqualTo(99));
        Assert.That(rgba[16], Is.EqualTo(WebpVp8YuvConverter.YuvToR(128, 128)));
        Assert.That(rgba[24], Is.EqualTo(99));
    }

    [Test]
    public void ConvertPixelToRgba_UsesExactFixedPointClamping()
    {
        byte[] rgba = new byte[8];

        WebpVp8YuvConverter.ConvertPixelToRgba(255, 255, 255, rgba, 0);
        WebpVp8YuvConverter.ConvertPixelToRgba(0, 0, 0, rgba, 4);

        Assert.That(rgba, Is.EqualTo(new byte[] { 255, 125, 255, 255, 0, 136, 0, 255 }));
    }

    [Test]
    public void YuvConverter_RejectsInvalidInputs()
    {
        byte[] y = new byte[4];
        byte[] u = new byte[1];
        byte[] v = new byte[1];
        byte[] rgba = new byte[16];

        Assert.Throws<ArgumentNullException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(null!, u, v, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, null!, v, 1, 1, 1, 1));
        Assert.Throws<ArgumentNullException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, null!, 1, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 0, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 2, 2, 1, 1));
        Assert.Throws<ArgumentException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(new byte[3], u, v, 2, 2, 2, 1));
        Assert.Throws<ArgumentException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, new byte[0], v, 2, 2, 2, 1));
        Assert.Throws<ArgumentException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, new byte[0], 2, 2, 2, 1));
        Assert.Throws<ArgumentNullException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 1, 1, 1, 1, null!, 4));
        Assert.Throws<ArgumentException>(() => WebpVp8YuvConverter.ConvertYuv420ToRgba(y, u, v, 2, 2, 2, 1, new byte[15], 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8YuvConverter.ConvertPixelToRgba(-1, 0, 0, rgba, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8YuvConverter.ConvertPixelToRgba(0, 0, 0, rgba, 13));
    }

    static void AssertPixel(byte[] rgba, int offset, int y, int u, int v)
    {
        Assert.That(rgba[offset], Is.EqualTo(WebpVp8YuvConverter.YuvToR(y, v)));
        Assert.That(rgba[offset + 1], Is.EqualTo(WebpVp8YuvConverter.YuvToG(y, u, v)));
        Assert.That(rgba[offset + 2], Is.EqualTo(WebpVp8YuvConverter.YuvToB(y, u)));
        Assert.That(rgba[offset + 3], Is.EqualTo(255));
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
