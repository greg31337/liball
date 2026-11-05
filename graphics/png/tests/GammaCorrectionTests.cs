using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class GammaCorrectionTests
{
    [Test]
    public void Build8BitTable_CreatesIdentityWhenDecodeExponentIsOne()
    {
        byte[] table = PngGammaCorrection.Build8BitTable(0.5, 2.0);

        Assert.That(table.Length, Is.EqualTo(256));
        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[1], Is.EqualTo(1));
        Assert.That(table[64], Is.EqualTo(64));
        Assert.That(table[128], Is.EqualTo(128));
        Assert.That(table[255], Is.EqualTo(255));
    }

    [Test]
    public void Build8BitTable_UsesPngGammaIntegerScale()
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = 50000;

        byte[] fromGamma = PngGammaCorrection.Build8BitTable(gamma, 2.0);
        byte[] fromInteger = PngGammaCorrection.Build8BitTable((uint)50000, 2.0);

        Assert.That(fromGamma, Is.EqualTo(PngGammaCorrection.BuildIdentity8BitTable()));
        Assert.That(fromInteger, Is.EqualTo(fromGamma));
    }

    [Test]
    public void Build8BitTable_LightensWhenDecodingExponentIsLessThanOne()
    {
        byte[] table = PngGammaCorrection.Build8BitTable(1.0, 2.0);

        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[64], Is.EqualTo(128));
        Assert.That(table[128], Is.EqualTo(181));
        Assert.That(table[255], Is.EqualTo(255));
    }

    [Test]
    public void Build8BitTable_DarkensWhenDecodingExponentIsGreaterThanOne()
    {
        byte[] table = PngGammaCorrection.Build8BitTable(0.5, 1.0);

        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[64], Is.EqualTo(16));
        Assert.That(table[128], Is.EqualTo(64));
        Assert.That(table[255], Is.EqualTo(255));
    }

    [Test]
    public void Build8BitTable_UsesUserExponent()
    {
        byte[] table = PngGammaCorrection.Build8BitTable(1.0, 2.0, 2.0);

        Assert.That(table, Is.EqualTo(PngGammaCorrection.BuildIdentity8BitTable()));
    }

    [Test]
    public void Build16BitTable_CreatesIdentityWhenDecodeExponentIsOne()
    {
        ushort[] table = PngGammaCorrection.Build16BitTable(0.5, 2.0);

        Assert.That(table.Length, Is.EqualTo(65536));
        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[1], Is.EqualTo(1));
        Assert.That(table[16384], Is.EqualTo(16384));
        Assert.That(table[32768], Is.EqualTo(32768));
        Assert.That(table[65535], Is.EqualTo(65535));
    }

    [Test]
    public void Build16BitTable_UsesPngGammaIntegerScale()
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = 50000;

        ushort[] fromGamma = PngGammaCorrection.Build16BitTable(gamma, 2.0);
        ushort[] fromInteger = PngGammaCorrection.Build16BitTable((uint)50000, 2.0);

        Assert.That(fromGamma[0], Is.EqualTo(0));
        Assert.That(fromGamma[32768], Is.EqualTo(32768));
        Assert.That(fromGamma[65535], Is.EqualTo(65535));
        Assert.That(fromInteger[32768], Is.EqualTo(fromGamma[32768]));
    }

    [Test]
    public void Build16BitTable_LightensWhenDecodingExponentIsLessThanOne()
    {
        ushort[] table = PngGammaCorrection.Build16BitTable(1.0, 2.0);

        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[16384], Is.EqualTo(32768));
        Assert.That(table[32768], Is.EqualTo(46341));
        Assert.That(table[49152], Is.EqualTo(56755));
        Assert.That(table[65535], Is.EqualTo(65535));
    }

    [Test]
    public void Build16BitTable_DarkensWhenDecodingExponentIsGreaterThanOne()
    {
        ushort[] table = PngGammaCorrection.Build16BitTable(0.5, 1.0);

        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[16384], Is.EqualTo(4096));
        Assert.That(table[32768], Is.EqualTo(16384));
        Assert.That(table[49152], Is.EqualTo(36865));
        Assert.That(table[65535], Is.EqualTo(65535));
    }

    [Test]
    public void Build16BitTable_UsesUserExponent()
    {
        ushort[] table = PngGammaCorrection.Build16BitTable(1.0, 2.0, 2.0);

        Assert.That(table[0], Is.EqualTo(0));
        Assert.That(table[32768], Is.EqualTo(32768));
        Assert.That(table[65535], Is.EqualTo(65535));
    }

    [Test]
    public void Apply8BitTable_AppliesToGrayscaleRowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale8, 3, 2, 4, new byte[] { 0, 10, 255, 99, 128, 64, 32, 99 });

        PngPixelBuffer result = PngGammaCorrection.Apply8BitTable(source, CreateInvertTable());

        AssertBuffer(result, PngPixelFormat.Grayscale8, 3, 2, 3, new byte[] { 255, 245, 0, 127, 191, 223 });
    }

    [Test]
    public void Apply8BitTable_AppliesToRgbSamplesAndLeavesAlphaUnchanged()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 0, 10, 255, 77, 128, 64, 32, 99 });

        PngPixelBuffer result = PngGammaCorrection.Apply8BitTable(source, CreateInvertTable());

        AssertBuffer(result, PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 255, 245, 0, 77, 127, 191, 223, 99 });
    }

    [Test]
    public void Apply8BitTable_LeavesGrayscaleAlphaAlphaUnchanged()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 0, 10, 255, 77 });

        PngPixelBuffer result = PngGammaCorrection.Apply8BitTable(source, CreateInvertTable());

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha8, 2, 1, 4, new byte[] { 255, 10, 0, 77 });
    }

    [Test]
    public void Apply8BitTable_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 0, 10, 255 });

        PngPixelBuffer result = PngGammaCorrection.Apply8BitTable(source, CreateInvertTable());
        source.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 255, 245, 0 });
    }

    [Test]
    public void Apply16BitTable_AppliesToGrayscaleRowsUsingStride()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale16, 3, 2, 8, CreateBigEndianSamples(0x0000, 0x1234, 0xFFFF, 0x9999, 0x8000, 0x4000, 0x0020, 0x9999));

        PngPixelBuffer result = PngGammaCorrection.Apply16BitTable(source, CreateInvert16Table());

        AssertBuffer(result, PngPixelFormat.Grayscale16, 3, 2, 6, CreateBigEndianSamples(0xFFFF, 0xEDCB, 0x0000, 0x7FFF, 0xBFFF, 0xFFDF));
    }

    [Test]
    public void Apply16BitTable_AppliesToRgbSamplesAndLeavesAlphaUnchanged()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0x0000, 0x1234, 0xFFFF, 0x7777, 0x8000, 0x4000, 0x0020, 0x9999));

        PngPixelBuffer result = PngGammaCorrection.Apply16BitTable(source, CreateInvert16Table());

        AssertBuffer(result, PngPixelFormat.Rgba16, 2, 1, 16, CreateBigEndianSamples(0xFFFF, 0xEDCB, 0x0000, 0x7777, 0x7FFF, 0xBFFF, 0xFFDF, 0x9999));
    }

    [Test]
    public void Apply16BitTable_LeavesGrayscaleAlphaAlphaUnchanged()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0x0000, 0x1234, 0xFFFF, 0x7777));

        PngPixelBuffer result = PngGammaCorrection.Apply16BitTable(source, CreateInvert16Table());

        AssertBuffer(result, PngPixelFormat.GrayscaleAlpha16, 2, 1, 8, CreateBigEndianSamples(0xFFFF, 0x1234, 0x0000, 0x7777));
    }

    [Test]
    public void Apply16BitTable_ReturnsIndependentBuffer()
    {
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgb16, 1, 1, 6, CreateBigEndianSamples(0x0000, 0x1234, 0xFFFF));

        PngPixelBuffer result = PngGammaCorrection.Apply16BitTable(source, CreateInvert16Table());
        source.Data[0] = 99;

        AssertBuffer(result, PngPixelFormat.Rgb16, 1, 1, 6, CreateBigEndianSamples(0xFFFF, 0xEDCB, 0x0000));
    }

    [Test]
    public void Build8BitTable_RejectsInvalidInputs()
    {
        PngGamma zeroGamma = new PngGamma();
        zeroGamma.Value = 0;

        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Build8BitTable(null!, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(zeroGamma, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable((uint)0, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(0.0, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(double.NaN, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(double.PositiveInfinity, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, 2.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, 2.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build8BitTable(1.0, 2.0, double.PositiveInfinity));
    }

    [Test]
    public void Build16BitTable_RejectsInvalidInputs()
    {
        PngGamma zeroGamma = new PngGamma();
        zeroGamma.Value = 0;

        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Build16BitTable(null!, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(zeroGamma, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable((uint)0, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(0.0, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(double.NaN, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(double.PositiveInfinity, 2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, 2.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, 2.0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Build16BitTable(1.0, 2.0, double.PositiveInfinity));
    }

    [Test]
    public void Apply8BitTable_RejectsInvalidInputs()
    {
        byte[] table = PngGammaCorrection.BuildIdentity8BitTable();

        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply8BitTable(null!, table));
        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, null!), table));
        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), new byte[255]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), new byte[257]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0, 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 0, 1, 1, new byte[] { 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 0, 1, new byte[] { 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 5, new byte[] { 0, 0, 0, 0, 0, 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply8BitTable(CreateBuffer(PngPixelFormat.Rgb8, 2, 2, 6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }), table));
    }

    [Test]
    public void Apply16BitTable_RejectsInvalidInputs()
    {
        ushort[] table = PngGammaCorrection.BuildIdentity16BitTable();

        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply16BitTable(null!, table));
        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, null!), table));
        Assert.Throws<ArgumentNullException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0, 0 }), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0, 0 }), new ushort[65535]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 1, 2, new byte[] { 0, 0 }), new ushort[65537]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Indexed8, 1, 1, 1, new byte[] { 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale8, 1, 1, 1, new byte[] { 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 0, 1, 2, new byte[] { 0, 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Grayscale16, 1, 0, 2, new byte[] { 0, 0 }), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Rgb16, 2, 1, 11, new byte[12]), table));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngGammaCorrection.Apply16BitTable(CreateBuffer(PngPixelFormat.Rgb16, 2, 2, 12, new byte[18]), table));
    }

    static void AssertBuffer(PngPixelBuffer result, PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        Assert.That(result.Format, Is.EqualTo(format));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Stride, Is.EqualTo(stride));
        Assert.That(result.Data, Is.EqualTo(data));
    }

    static byte[] CreateInvertTable()
    {
        byte[] table = new byte[256];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = (byte)(255 - index);
            index += 1;
        }

        return table;
    }

    static ushort[] CreateInvert16Table()
    {
        ushort[] table = new ushort[65536];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = (ushort)(65535 - index);
            index += 1;
        }

        return table;
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
