using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffYCbCrPhotometricConverterTests
{
    [Test]
    public void ConvertYCbCrToRgb8_ConvertsNeutralGrayWithDefaults()
    {
        TiffImageInfo info = CreateInfo(3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 128, 128, 128 }));
    }

    [Test]
    public void ConvertYCbCrToRgb8_ConvertsRedWithDefaultCoefficients()
    {
        TiffImageInfo info = CreateInfo(3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 76, 85, 255 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 0, 0 }));
    }

    [Test]
    public void ConvertYCbCrToRgb8_UsesReferenceBlackWhiteRanges()
    {
        TiffImageInfo info = CreateInfo(3);
        info.ReferenceBlackWhite = new double[] { 16, 235, 128, 240, 128, 240 };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 16, 128, 128, 235, 128, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255 }));
    }

    [Test]
    public void ConvertYCbCrToRgb16_ConvertsNeutralGray()
    {
        TiffImageInfo info = CreateInfo(3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertYCbCrToRgb16(info, new double[] { 128, 128, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 32896, 32896, 32896 }));
    }

    [Test]
    public void ConvertYCbCrToRgb8_IgnoresExtraSamples()
    {
        TiffImageInfo info = CreateInfo(4);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128, 128, 64 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 128, 128, 128 }));
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateInfo(3);
        info.Photometric = TiffPhotometric.Rgb;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsMissingChromaChannels()
    {
        TiffImageInfo info = CreateInfo(2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsSampleCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsInvalidReferenceBlackWhiteRange()
    {
        TiffImageInfo info = CreateInfo(3);
        info.ReferenceBlackWhite = new double[] { 0, 0, 128, 255, 128, 255 };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 128, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsSampleValueOutsideRange()
    {
        TiffImageInfo info = CreateInfo(3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 256, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertYCbCrToRgb8_ReportsUnsupportedSampleFormat()
    {
        TiffImageInfo info = CreateInfo(3);
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint, TiffSampleFormat.IeeeFloatingPoint, TiffSampleFormat.IeeeFloatingPoint };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertYCbCrToRgb8(info, new double[] { 0.5d, 0.5d, 0.5d }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(int samplesPerPixel)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = CreateFilledBits(8, samplesPerPixel);
        info.SampleFormats = CreateFilledFormats(TiffSampleFormat.UnsignedInteger, samplesPerPixel);
        info.Photometric = TiffPhotometric.YCbCr;
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
}
