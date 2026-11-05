using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MonochromeTransformerTests
{
    [Test]
    public void Invert_InvertsGrayscale1RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale1, 5, 2, 2, new byte[] { 0b10101000, 99, 0b01010000, 99 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.Grayscale1, 5, 2, 1, new byte[] { 0b01010000, 0b10101000 });
    }

    [Test]
    public void Invert_InvertsGrayscale2ToMaximumSample()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale2, 4, 1, 1, new byte[] { 0b00011011 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.Grayscale2, 4, 1, 1, new byte[] { 0b11100100 });
    }

    [Test]
    public void Invert_InvertsGrayscale4AndClearsPaddingBits()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale4, 3, 1, 2, new byte[] { 0x1F, 0xA0 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.Grayscale4, 3, 1, 2, new byte[] { 0xE0, 0x50 });
    }

    [Test]
    public void Invert_InvertsGrayscale8RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 0, 10, 255, 99, 128, 200, 7, 99 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 255, 245, 0, 127, 55, 248 });
    }

    [Test]
    public void Invert_InvertsGrayscale16()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 3, 1, 6, CreateBigEndianSamples(0x0000, 0x1234, 0xFFFF));

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 3, 1, 6, new byte[]
        {
            255, 255,
            237, 203,
            0, 0
        });
    }

    [Test]
    public void Invert_InvertsGrayscaleAlpha8AndPreservesAlpha()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 10, 1, 250, 200 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 245, 1, 5, 200 });
    }

    [Test]
    public void Invert_InvertsGrayscaleAlpha16AndPreservesAlpha()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            18, 52, 171, 205,
            0, 1, 1, 2
        });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            237, 203, 171, 205,
            255, 254, 1, 2
        });
    }

    [Test]
    public void Invert_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 12 });

        PngPixelBuffer result = PngMonochromeTransformer.Invert(source);
        source.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 243 });
    }

    [Test]
    public void Invert_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngMonochromeTransformer.Invert(null!));
        Assert.Throws<ArgumentNullException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Grayscale8, 0, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.Grayscale8, 1, 0, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 1, 3, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngMonochromeTransformer.Invert(CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 2, 4, new byte[] { 0, 0, 0, 0, 0, 0, 0 })));
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

    static byte[] CreateBigEndianSamples(params int[] samples)
    {
        byte[] data = new byte[samples.Length * 2];
        int index = 0;
        int offset = 0;
        while (index < samples.Length)
        {
            PngBinaryWriter.WriteUInt16(data, offset, (ushort)samples[index]);
            offset += 2;
            index += 1;
        }

        return data;
    }
}
