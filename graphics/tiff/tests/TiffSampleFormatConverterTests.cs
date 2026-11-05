using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSampleFormatConverterTests
{
    [Test]
    public void ConvertSamplesToDouble_ConvertsUnsignedIntegerSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 4);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 52, 18, 255, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { 4660, 255 }));
    }

    [Test]
    public void ConvertSamplesToDouble_ConvertsSignedIntegerSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 8 }, new TiffSampleFormat[] { TiffSampleFormat.SignedInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 254, 0, 127 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { -2, 0, 127 }));
    }

    [Test]
    public void ConvertSamplesToDouble_ConvertsSubByteSignedIntegerSamples()
    {
        TiffImageInfo info = CreateInfo(3, 1, 1, 1, new int[] { 2 }, new TiffSampleFormat[] { TiffSampleFormat.SignedInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 3);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 0, 1, 3 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { 0, 1, -1 }));
    }

    [Test]
    public void ReadSignedInteger_ConvertsSixtyFourBitHighBitPatterns()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        long negativeOne = TiffSampleFormatConverter.ReadSignedInteger(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, 0, 64, 8, diagnostics);
        long minimum = TiffSampleFormatConverter.ReadSignedInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 }, 0, 64, 8, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(negativeOne, Is.EqualTo(-1));
        Assert.That(minimum, Is.EqualTo(long.MinValue));
    }

    [Test]
    public void ConvertSamplesToDouble_ConvertsThirtyTwoBitFloatSamples()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 32 }, new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 8);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] source = new byte[8];
        TiffBinaryWriter.WriteSingle(source.AsSpan(0, 4), 1.5f, TiffByteOrder.LittleEndian);
        TiffBinaryWriter.WriteSingle(source.AsSpan(4, 4), -2.25f, TiffByteOrder.LittleEndian);

        double[] result = TiffCodec.ConvertSamplesToDouble(context, source, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.EqualTo(1.5d).Within(0.000001d));
        Assert.That(result[1], Is.EqualTo(-2.25d).Within(0.000001d));
    }

    [Test]
    public void ConvertSamplesToDouble_ConvertsSixtyFourBitFloatSamples()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 64 }, new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 8);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] source = new byte[8];
        TiffBinaryWriter.WriteDouble(source.AsSpan(0, 8), 3.25d, TiffByteOrder.LittleEndian);

        double[] result = TiffCodec.ConvertSamplesToDouble(context, source, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { 3.25d }));
    }

    [Test]
    public void ConvertSamplesToDouble_TreatsUndefinedSamplesAsRawUnsignedValues()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 4 }, new TiffSampleFormat[] { TiffSampleFormat.Undefined }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 10, 3 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { 10, 3 }));
    }

    [Test]
    public void ConvertSamplesToDouble_ConvertsMixedContiguousSampleFormats()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 3, new int[] { 8, 8, 32 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger, TiffSampleFormat.IeeeFloatingPoint }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 6);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] source = new byte[6];
        source[0] = 10;
        source[1] = 254;
        TiffBinaryWriter.WriteSingle(source.AsSpan(2, 4), 1.5f, TiffByteOrder.LittleEndian);

        double[] result = TiffCodec.ConvertSamplesToDouble(context, source, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result[0], Is.EqualTo(10d));
        Assert.That(result[1], Is.EqualTo(-2d));
        Assert.That(result[2], Is.EqualTo(1.5d).Within(0.000001d));
    }

    [Test]
    public void ConvertSamplesToDouble_UsesSeparatePlaneSampleFormat()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 2, new int[] { 8, 16 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger, TiffSampleFormat.SignedInteger }, TiffPlanarConfiguration.Separate);
        TiffCodecContext context = CreateContext(info, 1, 4);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 254, 255, 1, 0 }, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False);
        Assert.That(result, Is.EqualTo(new double[] { -2, 1 }));
    }

    [Test]
    public void ConvertSamplesToDouble_ReportsSourceByteCountMismatch()
    {
        TiffImageInfo info = CreateInfo(2, 1, 1, 1, new int[] { 16 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 4);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 1, 2 }, diagnostics);

        Assert.That(result, Is.EqualTo(new double[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void ConvertSamplesToDouble_ReportsUnsupportedSampleFormat()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 16 }, new TiffSampleFormat[] { TiffSampleFormat.ComplexInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 2);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 1, 0 }, diagnostics);

        Assert.That(result, Is.EqualTo(new double[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void ConvertSamplesToDouble_ReportsSubByteValueOutsideBitRange()
    {
        TiffImageInfo info = CreateInfo(1, 1, 1, 1, new int[] { 2 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger }, TiffPlanarConfiguration.Contiguous);
        TiffCodecContext context = CreateContext(info, 0, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] result = TiffCodec.ConvertSamplesToDouble(context, new byte[] { 4 }, diagnostics);

        Assert.That(result, Is.EqualTo(new double[0]));
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static TiffCodecContext CreateContext(TiffImageInfo info, ulong segmentIndex, ulong expectedDecodedByteCount)
    {
        return TiffCodecContext.Create(info, false, segmentIndex, expectedDecodedByteCount, new TiffLimits());
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, ulong rowsPerStrip, int samplesPerPixel, int[] bitsPerSample, TiffSampleFormat[] sampleFormats, TiffPlanarConfiguration planarConfiguration)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.RowsPerStrip = rowsPerStrip;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = bitsPerSample;
        info.SampleFormats = sampleFormats;
        info.Compression = TiffCompression.None;
        info.PlanarConfiguration = planarConfiguration;
        return info;
    }
}
