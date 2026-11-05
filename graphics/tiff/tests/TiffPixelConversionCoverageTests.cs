using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPixelConversionCoverageTests
{
    [Test]
    public void ConvertPixelsToRgba8_CoversBlackIsZeroAndWhiteIsZero()
    {
        TiffDiagnostics blackDiagnostics = new TiffDiagnostics();
        TiffDiagnostics whiteDiagnostics = new TiffDiagnostics();

        byte[] blackIsZero = ConvertToRgba8(CreateInfo(TiffPhotometric.BlackIsZero, 1, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255 }, blackDiagnostics);
        byte[] whiteIsZero = ConvertToRgba8(CreateInfo(TiffPhotometric.WhiteIsZero, 1, 8, TiffSampleFormat.UnsignedInteger), new double[] { 0 }, whiteDiagnostics);

        Assert.That(blackDiagnostics.HasErrors(), Is.False);
        Assert.That(whiteDiagnostics.HasErrors(), Is.False);
        Assert.That(blackIsZero, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
        Assert.That(whiteIsZero, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversPalette()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.Palette, 1, 8, TiffSampleFormat.UnsignedInteger);
        info.ColorMap = CreatePaletteColorMap();
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(info, new double[] { 1 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversRgb()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.Rgb, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 128, 0, 255 }));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversCmyk()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.Cmyk, 4, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 0, 0, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 255, 255, 255 }));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversYCbCr()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.YCbCr, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 76, 85, 255 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversCieLab()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.CieLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 138, 209, 198 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.GreaterThanOrEqualTo(240));
        Assert.That(result[1], Is.LessThanOrEqualTo(20));
        Assert.That(result[2], Is.LessThanOrEqualTo(20));
        Assert.That(result[3], Is.EqualTo(255));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversIccLab()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.IccLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 128, 128, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(118, 122));
        Assert.That(result[1], Is.InRange(118, 122));
        Assert.That(result[2], Is.InRange(118, 122));
        Assert.That(result[3], Is.EqualTo(255));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversItuLab()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.ItuLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 96 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 255, 255, 255 }).Within(1));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversLogL()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.LogL, 1, 16, TiffSampleFormat.UnsignedInteger), new double[] { 15872 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 128, 128, 128, 255 }).Within(1));
    }

    [Test]
    public void ConvertPixelsToRgba8_CoversLogLuv()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = ConvertToRgba8(CreateInfo(TiffPhotometric.LogLuv, 1, 32, TiffSampleFormat.UnsignedInteger), new double[] { CreatePackedLogLuv32(16384, 86, 194) }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(250, 255));
        Assert.That(result[1], Is.InRange(250, 255));
        Assert.That(result[2], Is.InRange(250, 255));
        Assert.That(result[3], Is.EqualTo(255));
    }

    static byte[] ConvertToRgba8(TiffImageInfo info, double[] samples, TiffDiagnostics diagnostics)
    {
        return TiffRgba8Reader.ConvertSamplesToRgba8(info, samples, new TiffLimits(), diagnostics);
    }

    static TiffImageInfo CreateInfo(TiffPhotometric photometric, int samplesPerPixel, int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = CreateFilledBits(bitsPerSample, samplesPerPixel);
        info.SampleFormats = CreateFilledFormats(sampleFormat, samplesPerPixel);
        info.Photometric = photometric;
        return info;
    }

    static int[] CreateFilledBits(int bitsPerSample, int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = bitsPerSample;
            index = index + 1;
        }

        return values;
    }

    static TiffSampleFormat[] CreateFilledFormats(TiffSampleFormat sampleFormat, int count)
    {
        TiffSampleFormat[] values = new TiffSampleFormat[count];
        int index = 0;
        while (index < count)
        {
            values[index] = sampleFormat;
            index = index + 1;
        }

        return values;
    }

    static ushort[] CreatePaletteColorMap()
    {
        ushort[] colorMap = new ushort[256 * 3];
        colorMap[1] = 65535;
        return colorMap;
    }

    static double CreatePackedLogLuv32(int lightness, int u, int v)
    {
        int encoded = lightness << 16 | u << 8 | v;
        return (double)encoded;
    }
}
