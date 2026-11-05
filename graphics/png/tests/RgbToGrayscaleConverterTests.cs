using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class RgbToGrayscaleConverterTests
{
    [Test]
    public void CalculateLuminance8_UsesIntegerLuminanceWeights()
    {
        Assert.That(PngRgbToGrayscaleConverter.RedWeight, Is.EqualTo(6968));
        Assert.That(PngRgbToGrayscaleConverter.GreenWeight, Is.EqualTo(23434));
        Assert.That(PngRgbToGrayscaleConverter.BlueWeight, Is.EqualTo(2366));
        Assert.That(PngRgbToGrayscaleConverter.WeightScale, Is.EqualTo(32768));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance8(0, 0, 0), Is.EqualTo(0));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance8(255, 255, 255), Is.EqualTo(255));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance8(255, 0, 0), Is.EqualTo(54));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance8(0, 255, 0), Is.EqualTo(182));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance8(0, 0, 255), Is.EqualTo(18));
    }

    [Test]
    public void CalculateLuminance16_UsesIntegerLuminanceWeights()
    {
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance16(0, 0, 0), Is.EqualTo(0));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance16(65535, 65535, 65535), Is.EqualTo(65535));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance16(65535, 0, 0), Is.EqualTo(13936));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance16(0, 65535, 0), Is.EqualTo(46867));
        Assert.That(PngRgbToGrayscaleConverter.CalculateLuminance16(0, 0, 65535), Is.EqualTo(4732));
    }

    [Test]
    public void Convert_ConvertsRgb8RowsUsingStride()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 3, 2, 10, new byte[]
        {
            255, 0, 0, 0, 255, 0, 0, 0, 255, 99,
            255, 255, 255, 0, 0, 0, 10, 20, 30, 99
        });

        PngPixelBuffer result = PngRgbToGrayscaleConverter.Convert(rgb);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 54, 182, 18, 255, 0, 19 });
    }

    [Test]
    public void Convert_ConvertsRgb16RowsUsingStride()
    {
        byte[] data = new byte[14];
        WriteSamples(data, 0, 65535, 0, 0);
        WriteSamples(data, 6, 0, 65535, 0);
        data[12] = 99;
        data[13] = 88;
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 14, data);

        PngPixelBuffer result = PngRgbToGrayscaleConverter.Convert(rgb);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(13936, 46867));
    }

    [Test]
    public void Convert_ConvertsRgba8AndPreservesAlpha()
    {
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 255, 0, 0, 7, 0, 0, 255, 9 });

        PngPixelBuffer result = PngRgbToGrayscaleConverter.Convert(rgba);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 54, 7, 18, 9 });
    }

    [Test]
    public void Convert_ConvertsRgba16AndPreservesAlpha()
    {
        byte[] data = new byte[16];
        WriteSamples(data, 0, 65535, 0, 0);
        PngBinaryWriter.WriteUInt16(data, 6, 0x1234);
        WriteSamples(data, 8, 0, 0, 65535);
        PngBinaryWriter.WriteUInt16(data, 14, 0xABCD);
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, data);

        PngPixelBuffer result = PngRgbToGrayscaleConverter.Convert(rgba);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            54, 112, 18, 52,
            18, 124, 171, 205
        });
    }

    [Test]
    public void Convert_ReturnsIndependentBuffer()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 255, 0, 0 });

        PngPixelBuffer result = PngRgbToGrayscaleConverter.Convert(rgb);
        rgb.Data[0] = 0;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 54 });
    }

    [Test]
    public void Convert_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.Convert(null!));
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.Convert(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void IsGrayscale_DetectsGrayRgbAndRgbaRowsUsingStride()
    {
        PngPixelBuffer rgb8 = CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 8, new byte[]
        {
            1, 1, 1, 2, 2, 2, 99, 99,
            3, 3, 3, 4, 4, 4, 88, 88
        });
        PngPixelBuffer rgba8 = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 10, new byte[] { 5, 5, 5, 1, 6, 6, 6, 2, 99, 99 });
        PngPixelBuffer rgb16 = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 12, CreateBigEndianSamples(0x1234, 0x1234, 0x1234, 0x8001, 0x8001, 0x8001));
        PngPixelBuffer rgba16 = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, CreateBigEndianSamples(0x0102, 0x0102, 0x0102, 0xABCD));

        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgb8), Is.True);
        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgba8), Is.True);
        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgb16), Is.True);
        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgba16), Is.True);
    }

    [Test]
    public void IsGrayscale_ReturnsFalseWhenAnyPixelHasDifferentChannels()
    {
        PngPixelBuffer rgb8 = CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 1, 1, 1, 2, 3, 2 });
        PngPixelBuffer rgba16 = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, CreateBigEndianSamples(0x0102, 0x0103, 0x0102, 0xFFFF));

        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgb8), Is.False);
        Assert.That(PngRgbToGrayscaleConverter.IsGrayscale(rgba16), Is.False);
    }

    [Test]
    public void ConvertIfGrayscale_ConvertsGrayRgb8RowsUsingStride()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 3, 2, 10, new byte[]
        {
            1, 1, 1, 2, 2, 2, 3, 3, 3, 99,
            4, 4, 4, 5, 5, 5, 6, 6, 6, 88
        });

        PngPixelBuffer result = PngRgbToGrayscaleConverter.ConvertIfGrayscale(rgb);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void ConvertIfGrayscale_ConvertsGrayRgba16AndPreservesAlpha()
    {
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x1234, 0x1234, 0x1234, 0x0001, 0x8001, 0x8001, 0x8001, 0xFFFF));

        PngPixelBuffer result = PngRgbToGrayscaleConverter.ConvertIfGrayscale(rgba);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0x1234, 0x0001, 0x8001, 0xFFFF));
    }

    [Test]
    public void ConvertIfGrayscale_ReturnsIndependentTightCopyWhenInputIsNotGray()
    {
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 8, new byte[] { 1, 1, 1, 2, 3, 2, 99, 99 });

        PngPixelBuffer result = PngRgbToGrayscaleConverter.ConvertIfGrayscale(rgb);
        rgb.Data[0] = 77;

        AssertBuffer(result, PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 1, 1, 1, 2, 3, 2 });
    }

    [Test]
    public void ConvertIfGrayscale_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(null!));
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 1, 0, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.ConvertIfGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 })));
    }

    [Test]
    public void IsGrayscale_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.IsGrayscale(null!));
        Assert.Throws<ArgumentNullException>(() => PngRgbToGrayscaleConverter.IsGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.IsGrayscale(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.IsGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 0, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngRgbToGrayscaleConverter.IsGrayscale(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 })));
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

    static void WriteSamples(byte[] data, int offset, int red, int green, int blue)
    {
        PngBinaryWriter.WriteUInt16(data, offset, (ushort)red);
        PngBinaryWriter.WriteUInt16(data, offset + 2, (ushort)green);
        PngBinaryWriter.WriteUInt16(data, offset + 4, (ushort)blue);
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
