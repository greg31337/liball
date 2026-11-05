using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSgiLogPhotometricConverterTests
{
    [Test]
    public void ConvertLogLToRgb8_DecodesBlackMiddleAndWhite()
    {
        TiffImageInfo info = CreateLogLInfo(16, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLToRgb8(info, new double[] { 0, 15872, 16384 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[]
        {
            0, 0, 0,
            128, 128, 128,
            255, 255, 255
        }).Within(1));
    }

    [Test]
    public void ConvertLogLToRgb16_DecodesMiddleGray()
    {
        TiffImageInfo info = CreateLogLInfo(16, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.ConvertLogLToRgb16(info, new double[] { 15872 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(32600, 32950));
        Assert.That(result[1], Is.InRange(32600, 32950));
        Assert.That(result[2], Is.InRange(32600, 32950));
    }

    [Test]
    public void ConvertLogLuvToRgb8_DecodesPackedThirtyTwoBitNeutralWhite()
    {
        TiffImageInfo info = CreatePackedLogLuvInfo(32, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        double packedWhite = CreatePackedLogLuv32(16384, 86, 194);

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 0, packedWhite }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.EqualTo(0));
        Assert.That(result[1], Is.EqualTo(0));
        Assert.That(result[2], Is.EqualTo(0));
        Assert.That(result[3], Is.InRange(250, 255));
        Assert.That(result[4], Is.InRange(250, 255));
        Assert.That(result[5], Is.InRange(250, 255));
    }

    [Test]
    public void ConvertLogLuvToRgb16_DecodesPackedThirtyTwoBitNeutralWhite()
    {
        TiffImageInfo info = CreatePackedLogLuvInfo(32, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        double packedWhite = CreatePackedLogLuv32(16384, 86, 194);

        ushort[] result = TiffCodec.ConvertLogLuvToRgb16(info, new double[] { packedWhite }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(64250, 65535));
        Assert.That(result[1], Is.InRange(64250, 65535));
        Assert.That(result[2], Is.InRange(64250, 65535));
    }

    [Test]
    public void ConvertLogLuvToRgb8_DecodesSignedLuvTriplet()
    {
        TiffImageInfo info = CreateLuvTripletInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 16384, 6899, 15522 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(250, 255));
        Assert.That(result[1], Is.InRange(250, 255));
        Assert.That(result[2], Is.InRange(250, 255));
    }

    [Test]
    public void ConvertLogLuvToRgb8_IgnoresExtraSamples()
    {
        TiffImageInfo info = CreateLuvTripletInfo();
        info.SamplesPerPixel = 4;
        info.BitsPerSample = new int[] { 16, 16, 16, 16 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.UnsignedInteger };
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 16384, 6899, 15522, 12 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.InRange(250, 255));
        Assert.That(result[1], Is.InRange(250, 255));
        Assert.That(result[2], Is.InRange(250, 255));
    }

    [Test]
    public void ConvertLogLToRgb8_ReportsUnsupportedPhotometric()
    {
        TiffImageInfo info = CreateLogLInfo(16, TiffSampleFormat.UnsignedInteger);
        info.Photometric = TiffPhotometric.BlackIsZero;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLToRgb8(info, new double[] { 16384 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertLogLToRgb8_ReportsInvalidBitsPerSample()
    {
        TiffImageInfo info = CreateLogLInfo(8, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLToRgb8(info, new double[] { 255 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertLogLToRgb8_ReportsSampleOutsideRange()
    {
        TiffImageInfo info = CreateLogLInfo(16, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLToRgb8(info, new double[] { 65536 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertLogLuvToRgb8_ReportsTwentyFourBitPackedLogLuvUnsupported()
    {
        TiffImageInfo info = CreatePackedLogLuvInfo(24, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    [Test]
    public void ConvertLogLuvToRgb8_ReportsSampleCountMismatch()
    {
        TiffImageInfo info = CreateLuvTripletInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 16384, 6899 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertLogLuvToRgb8_ReportsUnsupportedSampleFormat()
    {
        TiffImageInfo info = CreatePackedLogLuvInfo(32, TiffSampleFormat.IeeeFloatingPoint);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertLogLuvToRgb8_ReportsSampleOutsideRange()
    {
        TiffImageInfo info = CreatePackedLogLuvInfo(32, TiffSampleFormat.UnsignedInteger);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.ConvertLogLuvToRgb8(info, new double[] { 4294967296d }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateLogLInfo(int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { bitsPerSample };
        info.SampleFormats = new TiffSampleFormat[] { sampleFormat };
        info.Photometric = TiffPhotometric.LogL;
        return info;
    }

    static TiffImageInfo CreatePackedLogLuvInfo(int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 1;
        info.BitsPerSample = new int[] { bitsPerSample };
        info.SampleFormats = new TiffSampleFormat[] { sampleFormat };
        info.Photometric = TiffPhotometric.LogLuv;
        return info;
    }

    static TiffImageInfo CreateLuvTripletInfo()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 16, 16, 16 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.SignedInteger };
        info.Photometric = TiffPhotometric.LogLuv;
        return info;
    }

    static double CreatePackedLogLuv32(int lightness, int u, int v)
    {
        int encoded = lightness << 16 | u << 8 | v;
        return (double)encoded;
    }
}
