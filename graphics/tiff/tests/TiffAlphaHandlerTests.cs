using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffAlphaHandlerTests
{
    [Test]
    public void NormalizeRgbaAlpha8_UnpremultipliesAssociatedAlpha()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.AssociatedAlpha);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 64, 32, 16, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 128, 64, 32, 128 }));
    }

    [Test]
    public void NormalizeRgbaAlpha8_ZerosColorsWhenAssociatedAlphaIsZero()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.AssociatedAlpha);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 20, 30, 40, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void NormalizeRgbaAlpha8_LeavesUnassociatedAlphaUnchanged()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.UnassociatedAlpha);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 64, 32, 16, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 64, 32, 16, 128 }));
    }

    [Test]
    public void NormalizeRgbaAlpha8_LeavesUnspecifiedAlphaUnchanged()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.Unspecified);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 1, 2, 3, 4 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void NormalizeRgbaAlpha8_LeavesMissingExtraSampleMetadataUnchanged()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.Unspecified);
        info.ExtraSamples = new TiffExtraSample[0];
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 1, 2, 3, 4 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void NormalizeRgbaAlpha16_UnpremultipliesAssociatedAlpha()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.AssociatedAlpha);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        ushort[] result = TiffCodec.NormalizeRgbaAlpha16(info, new ushort[] { 32768, 16384, 0, 32768 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new ushort[] { 65535, 32768, 0, 32768 }));
    }

    [Test]
    public void NormalizeRgbaAlpha8_ReportsIncompletePixels()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.AssociatedAlpha);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 1, 2, 3 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void NormalizeRgbaAlpha8_ReportsMissingAlphaSample()
    {
        TiffImageInfo info = CreateInfo(TiffExtraSample.AssociatedAlpha);
        info.SamplesPerPixel = 3;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 1, 2, 3, 4 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void NormalizeRgbaAlpha8_ReportsUnsupportedExtraSampleValue()
    {
        TiffImageInfo info = CreateInfo((TiffExtraSample)99);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.NormalizeRgbaAlpha8(info, new byte[] { 1, 2, 3, 4 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffImageInfo CreateInfo(TiffExtraSample extraSample)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = 4;
        info.BitsPerSample = new int[] { 8, 8, 8, 8 };
        info.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger, TiffSampleFormat.UnsignedInteger };
        info.Photometric = TiffPhotometric.Rgb;
        info.ExtraSamples = new TiffExtraSample[] { extraSample };
        return info;
    }
}
