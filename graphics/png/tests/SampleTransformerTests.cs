using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SampleTransformerTests
{
    [Test]
    public void ByteSwap16BitSamples_SwapsGrayscale16RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 3, 2, 8, new byte[]
        {
            0x12, 0x34, 0xAB, 0xCD, 0x00, 0x01, 99, 99,
            0x00, 0xFF, 0xFF, 0x00, 0x80, 0x01, 99, 99
        });

        PngPixelBuffer result = PngSampleTransformer.ByteSwap16BitSamples(source);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 3, 2, 6, new byte[]
        {
            0x34, 0x12, 0xCD, 0xAB, 0x01, 0x00,
            0xFF, 0x00, 0x00, 0xFF, 0x01, 0x80
        });
    }

    [Test]
    public void ByteSwap16BitSamples_SwapsRgb16Samples()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 12, new byte[]
        {
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC,
            0x00, 0x01, 0x00, 0xFF, 0xFF, 0x00
        });

        PngPixelBuffer result = PngSampleTransformer.ByteSwap16BitSamples(source);

        AssertBuffer(result, PngPixelFormat.Rgb16, 2, 1, 12, new byte[]
        {
            0x34, 0x12, 0x78, 0x56, 0xBC, 0x9A,
            0x01, 0x00, 0xFF, 0x00, 0x00, 0xFF
        });
    }

    [Test]
    public void ByteSwap16BitSamples_SwapsGrayscaleAlpha16Samples()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            0x12, 0x34, 0x56, 0x78,
            0x9A, 0xBC, 0xDE, 0xF0
        });

        PngPixelBuffer result = PngSampleTransformer.ByteSwap16BitSamples(source);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, new byte[]
        {
            0x34, 0x12, 0x78, 0x56,
            0xBC, 0x9A, 0xF0, 0xDE
        });
    }

    [Test]
    public void ByteSwap16BitSamples_SwapsRgba16Samples()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, new byte[]
        {
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        });

        PngPixelBuffer result = PngSampleTransformer.ByteSwap16BitSamples(source);

        AssertBuffer(result, PngPixelFormat.Rgba16, 1, 1, 8, new byte[]
        {
            0x34, 0x12, 0x78, 0x56, 0xBC, 0x9A, 0xF0, 0xDE
        });
    }

    [Test]
    public void ByteSwap16BitSamples_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0x12, 0x34 });

        PngPixelBuffer result = PngSampleTransformer.ByteSwap16BitSamples(source);
        source.Data[1] = 99;

        AssertBuffer(result, PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0x34, 0x12 });
    }

    [Test]
    public void ByteSwap16BitSamples_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.ByteSwap16BitSamples(null!));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Grayscale16, 0, 1, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Grayscale16, 1, 0, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 11, new byte[12])));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.ByteSwap16BitSamples(CreateBuffer(PngPixelFormat.Rgb16, 2, 2, 12, new byte[18])));
    }

    [Test]
    public void CanReduce16BitTo8Bit_DetectsDuplicatedSampleBytesForAll16BitFormats()
    {
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale16, 2, 1, 4, new byte[] { 0x12, 0x12, 0xFF, 0xFF });
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb16, 1, 1, 6, new byte[] { 0x01, 0x01, 0x80, 0x80, 0xFE, 0xFE });
        PngPixelBuffer grayscaleAlpha = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 1, 1, 4, new byte[] { 0x44, 0x44, 0xCC, 0xCC });
        PngPixelBuffer rgba = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, new byte[] { 0, 0, 0x11, 0x11, 0x22, 0x22, 0xFF, 0xFF });

        Assert.That(PngSampleTransformer.CanReduce16BitTo8Bit(grayscale), Is.True);
        Assert.That(PngSampleTransformer.CanReduce16BitTo8Bit(rgb), Is.True);
        Assert.That(PngSampleTransformer.CanReduce16BitTo8Bit(grayscaleAlpha), Is.True);
        Assert.That(PngSampleTransformer.CanReduce16BitTo8Bit(rgba), Is.True);
    }

    [Test]
    public void CanReduce16BitTo8Bit_ReturnsFalseWhenAnySampleBytesDiffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, new byte[] { 0x12, 0x12, 0x34, 0x35, 0x56, 0x56, 0xFF, 0xFF });

        Assert.That(PngSampleTransformer.CanReduce16BitTo8Bit(source), Is.False);
    }

    [Test]
    public void Reduce16BitTo8BitIfLossless_ReducesGrayscale16RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 3, 2, 8, new byte[]
        {
            0x00, 0x00, 0x12, 0x12, 0xFF, 0xFF, 99, 99,
            0x80, 0x80, 0x34, 0x34, 0x7F, 0x7F, 88, 88
        });

        PngPixelBuffer result = PngSampleTransformer.Reduce16BitTo8BitIfLossless(source);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 0x00, 0x12, 0xFF, 0x80, 0x34, 0x7F });
    }

    [Test]
    public void Reduce16BitTo8BitIfLossless_ReducesRgba16ToRgba8()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, new byte[]
        {
            0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x04,
            0xFE, 0xFE, 0x80, 0x80, 0x7F, 0x7F, 0xFF, 0xFF
        });

        PngPixelBuffer result = PngSampleTransformer.Reduce16BitTo8BitIfLossless(source);

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFE, 0x80, 0x7F, 0xFF });
    }

    [Test]
    public void Reduce16BitTo8BitIfLossless_ReturnsIndependentTightCopyWhenNotReducible()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb16, 1, 1, 8, new byte[] { 0x12, 0x12, 0x34, 0x35, 0x56, 0x56, 99, 99 });

        PngPixelBuffer result = PngSampleTransformer.Reduce16BitTo8BitIfLossless(source);
        source.Data[0] = 77;

        AssertBuffer(result, PngPixelFormat.Rgb16, 1, 1, 6, new byte[] { 0x12, 0x12, 0x34, 0x35, 0x56, 0x56 });
    }

    [Test]
    public void Reduce16BitTo8BitIfLossless_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(null!));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Grayscale16, 0, 1, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Grayscale16, 1, 0, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 11, new byte[12])));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.Reduce16BitTo8BitIfLossless(CreateBuffer(PngPixelFormat.Rgb16, 2, 2, 12, new byte[18])));
    }

    [Test]
    public void CanReduce16BitTo8Bit_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.CanReduce16BitTo8Bit(null!));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.CanReduce16BitTo8Bit(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.CanReduce16BitTo8Bit(CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.CanReduce16BitTo8Bit(CreateBuffer(PngPixelFormat.Grayscale16, 0, 1, 2, new byte[] { 0, 0 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.CanReduce16BitTo8Bit(CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 11, new byte[12])));
    }

    [Test]
    public void DownshiftToSignificantBits_DownshiftsGrayscale8RowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 0, 8, 255, 99, 128, 200, 7, 99 });
        PngSignificantBits bits = CreateGrayBits(5);

        PngPixelBuffer result = PngSampleTransformer.DownshiftToSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 0, 1, 31, 16, 25, 0 });
    }

    [Test]
    public void UpscaleFromSignificantBits_UpscalesGrayscale8ToFullRange()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 4, 1, 4, new byte[] { 0, 1, 16, 31 });
        PngSignificantBits bits = CreateGrayBits(5);

        PngPixelBuffer result = PngSampleTransformer.UpscaleFromSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 4, 1, 4, new byte[] { 0, 8, 132, 255 });
    }

    [Test]
    public void DownshiftToSignificantBits_DownshiftsRgb8WithPerChannelBits()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 255, 255, 255, 128, 64, 32 });
        PngSignificantBits bits = CreateRgbBits(5, 6, 7);

        PngPixelBuffer result = PngSampleTransformer.DownshiftToSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 31, 63, 127, 16, 16, 16 });
    }

    [Test]
    public void DownshiftToSignificantBits_DownshiftsGrayscaleAlpha8()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 255, 255, 128, 64 });
        PngSignificantBits bits = CreateGrayAlphaBits(5, 4);

        PngPixelBuffer result = PngSampleTransformer.DownshiftToSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 31, 15, 16, 4 });
    }

    [Test]
    public void DownshiftToSignificantBits_DownshiftsGrayscale16()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0xFFFF, 0x1234));
        PngSignificantBits bits = CreateGrayBits(12);

        PngPixelBuffer result = PngSampleTransformer.DownshiftToSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Grayscale16, 2, 1, 4, CreateBigEndianSamples(0x0FFF, 0x0123));
    }

    [Test]
    public void UpscaleFromSignificantBits_UpscalesRgba16WithPerChannelBits()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x0FFF, 0x03FF, 0xFFFF, 0x00FF, 0, 0, 0, 0));
        PngSignificantBits bits = CreateRgbaBits(12, 10, 16, 8);

        PngPixelBuffer result = PngSampleTransformer.UpscaleFromSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0, 0, 0, 0));
    }

    [Test]
    public void UpscaleFromSignificantBits_ClampsInputSamplesToSignificantRange()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 255 });
        PngSignificantBits bits = CreateGrayBits(5);

        PngPixelBuffer result = PngSampleTransformer.UpscaleFromSignificantBits(source, bits);

        AssertBuffer(result, PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 255 });
    }

    [Test]
    public void DownshiftToSignificantBits_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 255 });
        PngSignificantBits bits = CreateGrayBits(5);

        PngPixelBuffer result = PngSampleTransformer.DownshiftToSignificantBits(source, bits);
        source.Data[0] = 0;

        AssertBuffer(result, PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 31 });
    }

    [Test]
    public void SignificantBitsScaling_RejectsInvalidInputs()
    {
        PngSignificantBits valid = CreateGrayBits(5);
        PngSignificantBits zero = CreateGrayBits(0);
        PngSignificantBits tooLarge = CreateGrayBits(9);

        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.DownshiftToSignificantBits(null!, valid));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, null!), valid));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale1, 1, 1, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 0, 1, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 1, 0, 1, new byte[] { 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 1, new byte[] { 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 2, 2, 2, new byte[] { 0, 0, 0 }), valid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSampleTransformer.DownshiftToSignificantBits(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), tooLarge));
        Assert.Throws<ArgumentNullException>(() => PngSampleTransformer.UpscaleFromSignificantBits(null!, valid));
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

    static PngSignificantBits CreateGrayBits(int gray)
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = (byte)gray;
        return bits;
    }

    static PngSignificantBits CreateGrayAlphaBits(int gray, int alpha)
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = (byte)gray;
        bits.Alpha = (byte)alpha;
        return bits;
    }

    static PngSignificantBits CreateRgbBits(int red, int green, int blue)
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Red = (byte)red;
        bits.Green = (byte)green;
        bits.Blue = (byte)blue;
        return bits;
    }

    static PngSignificantBits CreateRgbaBits(int red, int green, int blue, int alpha)
    {
        PngSignificantBits bits = CreateRgbBits(red, green, blue);
        bits.Alpha = (byte)alpha;
        return bits;
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
