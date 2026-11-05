using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffLabPhotometricConverterTests
{
    [Test]
    public void ConvertLabToRgb8_ConvertsCieLabNeutralBlackAndWhite()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 0, 128, 128, 255, 128, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 255, 255, 255 }).Within(1));
    }

    [Test]
    public void ConvertLabToRgb8_ConvertsCieLabApproximateRed()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 138, 209, 198 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.GreaterThanOrEqualTo(240));
        Assert.That(result[1], Is.LessThanOrEqualTo(20));
        Assert.That(result[2], Is.LessThanOrEqualTo(20));
    }

    [Test]
    public void ConvertLabToRgb8_ConvertsSignedCieLabChroma()
    {
        TiffImageInfo info = CreateInfo(
            TiffPhotometric.CieLab,
            3,
            new int[] { 8, 8, 8 },
            new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.SignedInteger });
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 0, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 255, 255 }).Within(1));
    }

    [Test]
    public void ConvertLabToRgb16_ConvertsIccLabSixteenBitNeutralGray()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.IccLab, 3, new int[] { 16, 16, 16 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertLabToRgb16(info, new double[] { 32640, 32768, 32768 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(30500, 31500));
        Assert.That(result[1], Is.InRange(30500, 31500));
        Assert.That(result[2], Is.InRange(30500, 31500));
    }

    [Test]
    public void ConvertLabToRgb8_ConvertsItuLabDefaultNeutralWhite()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.ItuLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128, 96 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 255, 255 }).Within(1));
    }

    [Test]
    public void ConvertLabToRgb8_ConvertsLightnessOnlyAsNeutralColor()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 1, new int[] { 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 1));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(118, 121));
        Assert.That(result[1], Is.InRange(118, 121));
        Assert.That(result[2], Is.InRange(118, 121));
    }

    [Test]
    public void ConvertLabToRgb8_IgnoresExtraSamples()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.IccLab, 4, new int[] { 8, 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 4));
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128, 128, 64 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 255, 255, 255 }).Within(1));
    }

    [Test]
    public void ConvertLabToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.Rgb, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertLabToRgb8_ReportsInvalidTwoSampleLab()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 2, new int[] { 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 2));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertLabToRgb8_ReportsSampleCountMismatch()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertLabToRgb8_ReportsUnsignedSampleOutsideRange()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.UnsignedInteger, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 256, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertLabToRgb8_ReportsUnsupportedSampleFormat()
    {
        TiffImageInfo info = CreateInfo(TiffPhotometric.CieLab, 3, new int[] { 8, 8, 8 }, CreateFormats(TiffSampleFormat.Unknown, 3));
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLabToRgb8(info, new double[] { 255, 128, 128 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(TiffPhotometric photometric, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.SampleFormats = sampleFormats;
        info.Photometric = photometric;
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
