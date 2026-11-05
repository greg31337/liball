using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRgbPhotometricConverterTests
{
    [Test]
    public void ConvertRgbToRgb8_PassesThroughEightBitRgbSamples()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 10, 20, 30, 255, 128, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 10, 20, 30, 255, 128, 0 }));
    }

    [Test]
    public void ConvertRgbToRgb8_IgnoresExtraAlphaWhenRgbOutputIsRequested()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 10, 20, 30, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void ConvertRgbaToRgba8_PassesThroughEightBitRgbaSamples()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbaToRgba8(info, new double[] { 10, 20, 30, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 10, 20, 30, 128 }));
    }

    [Test]
    public void ConvertRgbToRgb16_ScalesFourBitRgbSamples()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 4, 4, 4 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertRgbToRgb16(info, new double[] { 0, 8, 15 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 0, 34952, 65535 }));
    }

    [Test]
    public void ConvertRgbToRgb8_MapsFloatingPointRgbSamples()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 32, 32, 32 }, CreateFormats(TiffSampleFormat.IeeeFloatingPoint, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 0d, 0.5d, 1d }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 128, 255 }));
    }

    [Test]
    public void ConvertRgbToRgb8_MapsSignedIntegerRgbSamples()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.SignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { -128, -1, 127 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 127, 255 }));
    }

    [Test]
    public void ConvertRgbaToRgba16_PassesThroughAlphaAsFourthChannel()
    {
        TiffImageInfo info = CreateInfo(4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertRgbaToRgba16(info, new double[] { 255, 128, 0, 64 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 65535, 32896, 0, 16448 }));
    }

    [Test]
    public void ConvertRgbToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        info.Photometric = TiffPhotometric.BlackIsZero;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 0, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertRgbToRgb8_ReportsSampleCountMismatch()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertRgbaToRgba8_ReportsMissingAlphaChannel()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbaToRgba8(info, new double[] { 0, 0, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertRgbToRgb8_ReportsChannelValueOutsideRange()
    {
        TiffImageInfo info = CreateInfo(3, new int[] { 2, 2, 2 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertRgbToRgb8(info, new double[] { 0, 4, 1 }, diagnostics);

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
        info.Photometric = TiffPhotometric.Rgb;
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
