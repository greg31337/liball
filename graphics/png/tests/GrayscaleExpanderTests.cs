using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class GrayscaleExpanderTests
{
    [Test]
    public void IsLowBitGrayscaleFormat_AcceptsOnlyPackedGrayscaleFormats()
    {
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Grayscale1), Is.True);
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Grayscale2), Is.True);
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Grayscale4), Is.True);
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Grayscale8), Is.False);
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Indexed1), Is.False);
        Assert.That(PngGrayscaleExpander.IsLowBitGrayscaleFormat(PngPixelFormat.Rgba8), Is.False);
    }

    [Test]
    public void ExpandLowBitToGrayscale8_Expands1BitRowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale1, 5, 2, 2, new byte[] { 0b10101000, 99, 0b01010000, 99 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandLowBitToGrayscale8(source);

        AssertBuffer(result, 5, 2, new byte[] { 255, 0, 255, 0, 255, 0, 255, 0, 255, 0 });
    }

    [Test]
    public void ExpandLowBitToGrayscale8_Expands2BitRowsToFullRange()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale2, 4, 1, 1, new byte[] { 0b00011011 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandLowBitToGrayscale8(source);

        AssertBuffer(result, 4, 1, new byte[] { 0, 85, 170, 255 });
    }

    [Test]
    public void ExpandLowBitToGrayscale8_Expands4BitRowsToFullRange()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale4, 3, 1, 2, new byte[] { 0x1F, 0xA0 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandLowBitToGrayscale8(source);

        AssertBuffer(result, 3, 1, new byte[] { 17, 255, 170 });
    }

    [Test]
    public void ExpandLowBitToGrayscale8_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale1, 1, 1, 1, new byte[] { 0b10000000 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandLowBitToGrayscale8(source);
        source.Data[0] = 0;

        AssertBuffer(result, 1, 1, new byte[] { 255 });
    }

    [Test]
    public void ExpandToRgb_ExpandsGrayscale8RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 7, 8, 9, 99, 10, 11, 12, 99 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandToRgb(source);

        AssertBuffer(result, PngPixelFormat.Rgb8, 3, 2, 9, new byte[]
        {
            7, 7, 7, 8, 8, 8, 9, 9, 9,
            10, 10, 10, 11, 11, 11, 12, 12, 12
        });
    }

    [Test]
    public void ExpandToRgb_ExpandsPackedGrayscaleToRgb8()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale2, 4, 1, 1, new byte[] { 0b00011011 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandToRgb(source);

        AssertBuffer(result, PngPixelFormat.Rgb8, 4, 1, 12, new byte[]
        {
            0, 0, 0,
            85, 85, 85,
            170, 170, 170,
            255, 255, 255
        });
    }

    [Test]
    public void ExpandToRgb8_ExpandsLowBitAndGrayscale8Inputs()
    {
        PngPixelBuffer lowBit = CreateBuffer(PngPixelFormat.Grayscale4, 2, 1, 1, new byte[] { 0xAF });
        PngPixelBuffer grayscale8 = CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 2, new byte[] { 3, 4 });

        PngPixelBuffer lowBitResult = PngGrayscaleExpander.ExpandToRgb8(lowBit);
        PngPixelBuffer grayscale8Result = PngGrayscaleExpander.ExpandToRgb8(grayscale8);

        AssertBuffer(lowBitResult, PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 170, 170, 170, 255, 255, 255 });
        AssertBuffer(grayscale8Result, PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 3, 3, 3, 4, 4, 4 });
    }

    [Test]
    public void ExpandToRgb_ExpandsGrayscale16ToRgb16()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x1234, 0x8001));

        PngPixelBuffer result = PngGrayscaleExpander.ExpandToRgb(source);

        AssertBuffer(result, PngPixelFormat.Rgb16, 2, 1, 12, new byte[]
        {
            18, 52, 18, 52, 18, 52,
            128, 1, 128, 1, 128, 1
        });
    }

    [Test]
    public void ExpandToRgb_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 12 });

        PngPixelBuffer result = PngGrayscaleExpander.ExpandToRgb(source);
        source.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 12, 12, 12 });
    }

    [Test]
    public void ExpandLowBitToGrayscale8_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(null!));
        Assert.Throws<ArgumentNullException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale1, 1, 1, 1, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Indexed1, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale1, 0, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale1, 1, 0, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale1, 9, 1, 1, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandLowBitToGrayscale8(CreateBuffer(PngPixelFormat.Grayscale1, 8, 2, 1, new byte[] { 0 })));
    }

    [Test]
    public void ExpandToRgb_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngGrayscaleExpander.ExpandToRgb(null!));
        Assert.Throws<ArgumentNullException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Grayscale8, 0, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Grayscale8, 1, 0, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 1, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb(CreateBuffer(PngPixelFormat.Grayscale8, 2, 2, 2, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGrayscaleExpander.ExpandToRgb8(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, CreateBigEndianSamples(1))));
    }

    static void AssertBuffer(PngPixelBuffer result, int width, int height, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(PngPixelFormat.Grayscale8));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(width));
        Assert.That(result.Data, Is.EqualTo(data));
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
