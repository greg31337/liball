using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSubByteSamplePackerTests
{
    [Test]
    public void PackSubByteSamples_PacksOneBitRowsAndZeroesRowPadding()
    {
        TiffImageInfo info = CreateInfo(5, 2, 2, 1, new int[] { 1 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 176, 88 }));
    }

    [Test]
    public void PackSubByteSamples_PacksTwoBitSamples()
    {
        TiffImageInfo info = CreateInfo(4, 1, 1, 1, new int[] { 2 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 1, 2, 3, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 108 }));
    }

    [Test]
    public void PackSubByteSamples_PacksFourBitContiguousSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 2, new int[] { 4, 4 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 10, 11, 1, 2 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 171, 18 }));
    }

    [Test]
    public void PackSubByteSamples_PacksMixedSubByteSamplesAcrossByteBoundary()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 2, new int[] { 1, 2 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 1, 2, 0, 3, 1, 1 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 206, 128 }));
    }

    [Test]
    public void PackSubByteSamples_UsesSeparatePlaneSampleBits()
    {
        TiffImageInfo info = CreateInfo(4, 1, 1, 2, new int[] { 4, 2 }, TiffPlanarConfiguration.Separate);
        TiffCodecContext context = CreateContext(info, 1, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 0, 1, 2, 3 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 27 }));
    }

    [Test]
    public void PackSubByteSamples_CopiesWhenNoSubByteSamplesExist()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 4, 5, 6 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void PackSubByteSamples_RoundTripsWithUnpacker()
    {
        TiffImageInfo info = CreateInfo(5, 2, 2, 1, new int[] { 1 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] source = new byte[] { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1 };

        byte[] packed = TiffCodec.PackSubByteSamples(context, source, diagnostics);
        byte[] unpacked = TiffCodec.UnpackSubByteSamples(context, packed, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(unpacked, Is.EqualTo(source));
    }

    [Test]
    public void PackSubByteSamples_ReportsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(5, 2, 2, 1, new int[] { 1 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 1, 0, 1 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void PackSubByteSamples_ReportsSampleValueOutsideBitRange()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 2 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.PackSubByteSamples(context, new byte[] { 4 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffCodecContext CreateContext(TiffImageInfo info, ulong segmentIndex, ulong expectedDecodedByteCount)
    {
        return TiffCodecContext.Create(info, false, segmentIndex, expectedDecodedByteCount, new TiffLimits());
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip, int samplesPerPixel, int[] bitsPerSample, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.Compression = TiffCompression.None;
        info.PlanarConfiguration = planarConfiguration;
        return info;
    }
}
