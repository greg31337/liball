using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGrayscalePhotometricConverterTests
{
    [Test]
    public void ConvertGrayscaleToRgb8_MapsBlackIsZeroUnsignedSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 8, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0, 128, 255 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 128, 128, 128, 255, 255, 255 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb8_InvertsWhiteIsZeroSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.WhiteIsZero, 1, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0, 1 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 255, 255, 0, 0, 0 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb16_MapsUnsignedSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 4, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertGrayscaleToRgb16(info, new double[] { 0, 8, 15 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 0, 0, 0, 34952, 34952, 34952, 65535, 65535, 65535 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb8_MapsFloatingPointSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 32, TiffSampleFormat.IeeeFloatingPoint);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0d, 0.5d, 1d }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 128, 128, 128, 255, 255, 255 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb8_MapsSignedIntegerSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 8, TiffSampleFormat.SignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { -128, -1, 127 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 127, 127, 127, 255, 255, 255 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb8_TreatsUndefinedAsUnsigned()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 2, TiffSampleFormat.Undefined);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0, 3 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255 }));
    }

    [Test]
    public void ConvertGrayscaleToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.Rgb, 8, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertGrayscaleToRgb8_ReportsMultiSampleImages()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 8, TiffSampleFormat.UnsignedInteger);
        info.SamplesPerPixel = 2;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 0, 1 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertGrayscaleToRgb8_ReportsIntegerValueOutsideRange()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 2, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 4 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertGrayscaleToRgb8_ReportsFloatValueOutsideRange()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.BlackIsZero, 32, TiffSampleFormat.IeeeFloatingPoint);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertGrayscaleToRgb8(info, new double[] { 1.5d }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(TiffPhotometric photometric, int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { bitsPerSample };
        info.SampleFormats = new TiffSampleFormat[] { sampleFormat };
        info.Photometric = photometric;
        return info;
    }
}
