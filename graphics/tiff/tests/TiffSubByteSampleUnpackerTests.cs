using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSubByteSampleUnpackerTests
{
    [Test]
    public void UnpackSubByteSamples_UnpacksOneBitRowsAndSkipsRowPadding()
    {
        TiffImageInfo info = CreateInfo(5, 2, 2, 1, new int[] { 1 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 176, 88 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 1, 0, 1, 1, 0, 0, 1, 0, 1, 1 }));
    }

    [Test]
    public void UnpackSubByteSamples_UnpacksTwoBitSamples()
    {
        TiffImageInfo info = CreateInfo(4, 1, 1, 1, new int[] { 2 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 108 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
    }

    [Test]
    public void UnpackSubByteSamples_UnpacksFourBitContiguousSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 2, new int[] { 4, 4 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 171, 18 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 10, 11, 1, 2 }));
    }

    [Test]
    public void UnpackSubByteSamples_UnpacksMixedSubByteSamplesAcrossByteBoundary()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 2, new int[] { 1, 2 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 206, 128 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 0, 3, 1, 1 }));
    }

    [Test]
    public void UnpackSubByteSamples_UsesSeparatePlaneSampleBits()
    {
        TiffImageInfo info = CreateInfo(4, 1, 1, 2, new int[] { 4, 2 }, TiffPlanarConfiguration.Separate);
        TiffCodecContext context = CreateContext(info, 1, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 27 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void UnpackSubByteSamples_CopiesWhenNoSubByteSamplesExist()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 8 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 4, 5, 6 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void UnpackSubByteSamples_ReportsByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(5, 2, 2, 1, new int[] { 1 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 176 }, diagnostics);

        Assert.That(result, Is.EqualTo(new byte[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void UnpackSubByteSamples_ReportsWideSampleInMixedUnpackLayout()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 2, new int[] { 4, 16 }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        byte[] result = TiffCodec.UnpackSubByteSamples(context, new byte[] { 160, 18, 52 }, diagnostics);

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
