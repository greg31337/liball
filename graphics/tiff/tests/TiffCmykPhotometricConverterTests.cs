using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCmykPhotometricConverterTests
{
    [Test]
    public void ConvertCmykToRgb8_ConvertsWhiteBlackAndPrimaryInkSamples()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(
            info,
            new double[]
            {
                0, 0, 0, 0,
                255, 0, 0, 0,
                0, 255, 0, 0,
                0, 0, 255, 0,
                0, 0, 0, 255
            },
            diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[]
        {
            255, 255, 255,
            0, 255, 255,
            255, 0, 255,
            255, 255, 0,
            0, 0, 0
        }));
    }

    [Test]
    public void ConvertCmykToRgb8_AppliesBlackChannel()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 0, 0, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 127, 127, 127 }));
    }

    [Test]
    public void ConvertCmykToRgb16_ConvertsFourBitSamples()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 4, 4, 4, 4 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertCmykToRgb16(info, new double[] { 0, 15, 15, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 65535, 0, 0 }));
    }

    [Test]
    public void ConvertCmykToRgb8_ConvertsFloatingPointSamples()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 32, 32, 32, 32 }, CreateFormats(TiffSampleFormat.IeeeFloatingPoint, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0.25d, 0.5d, 0d, 0.2d }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 153, 102, 204 }));
    }

    [Test]
    public void ConvertCmykToRgb8_IgnoresExtraSamples()
    {
        TiffImageInfo info = CreateInfo(5, new int[] { 8, 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 5));
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 255, 255, 0, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 0, 0 }));
    }

    [Test]
    public void ConvertCmykToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        info.Photometric = TiffPhotometric.Rgb;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 0, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertCmykToRgb8_ReportsMissingBlackChannel()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertCmykToRgb8_ReportsSampleCountMismatch()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertCmykToRgb8_ReportsChannelValueOutsideRange()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 2, 2, 2, 2 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertCmykToRgb8(info, new double[] { 0, 4, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.SampleFormats = sampleFormats;
        info.Photometric = TiffPhotometric.Cmyk;
        return info;
    }

    static TiffSampleFormat[] CreateFormats(TiffSampleFormat sampleFormat, int count)
    {
        TiffSampleFormat[] formats = new TiffSampleFormat[count];
        int index = 0;
        while (index < count)
        {
            formats[index] = sampleFormat;
            index = index + 1;
        }

        return formats;
    }
}
