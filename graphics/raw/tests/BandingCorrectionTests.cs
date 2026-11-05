using raw;

namespace raw.Tests;

public class BandingCorrectionTests
{
    [Test]
    public void RowCorrectionUsesCompleteMaskedCoverageAndReportsAppliedOffsets()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = RowInfo();
        uint[] samples = new uint[]
        {
            8, 58, 58, 58,
            10, 60, 60, 60,
            13, 63, 63, 63,
            11, 61, 61, 61
        };
        RawStoredImage stored = new RawStoredImage(info, samples, null, null, 4, true, null);
        RawNormalizationOptions disabledOptions = RowOptions(false);
        RawNormalizationOptions enabledOptions = RowOptions(true);

        RawSensorImage disabled = codec.PreprocessStoredImage(stored, disabledOptions);
        RawSensorImage corrected = codec.PreprocessStoredImage(stored, enabledOptions);
        RawSensorImage repeated = codec.PreprocessStoredImage(stored, enabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.BandingCorrection, Is.Null);
            Assert.That(disabled.FloatingSamples[1], Is.EqualTo(0.48F).Within(0.000001F));
            Assert.That(disabled.FloatingSamples[5], Is.EqualTo(0.5F).Within(0.000001F));
            Assert.That(disabled.FloatingSamples[9], Is.EqualTo(0.53F).Within(0.000001F));
            Assert.That(disabled.FloatingSamples[13], Is.EqualTo(0.51F).Within(0.000001F));
            Assert.That((corrected.BandingCorrection ?? throw new System.InvalidOperationException("A required value is unavailable.")).RowCorrectionApplied, Is.True);
            Assert.That(corrected.BandingCorrection.ColumnCorrectionApplied, Is.False);
            Assert.That(corrected.BandingCorrection.RowOffsets, Is.EqualTo(new double[] { -2, 0, 3, 1 }));
            Assert.That(corrected.BandingCorrection.ColumnOffsets, Is.Empty);
            Assert.That(corrected.BandingCorrection.CalibrationSampleCount, Is.EqualTo(4));
            Assert.That(corrected.BandingCorrection.MaximumAbsoluteCorrection, Is.EqualTo(3));
            Assert.That(new float[] { corrected.FloatingSamples[1], corrected.FloatingSamples[5], corrected.FloatingSamples[9], corrected.FloatingSamples[13] }, Is.All.EqualTo(0.5F).Within(0.000001F));
            Assert.That(repeated.FloatingSamples, Is.EqualTo(corrected.FloatingSamples));
            Assert.That((repeated.BandingCorrection ?? throw new System.InvalidOperationException("A required value is unavailable.")).RowOffsets, Is.EqualTo(corrected.BandingCorrection.RowOffsets));
            Assert.That(corrected.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ColumnCorrectionFlowsThroughExistingSensorPreprocessing()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = ColumnInfo();
        uint[] samples = new uint[]
        {
            8, 10, 13, 11,
            58, 60, 63, 61,
            58, 60, 63, 61,
            58, 60, 63, 61
        };
        RawStoredImage stored = new RawStoredImage(info, samples, null, null, 4, true, null);
        RawArea masked = new RawArea(RawAreaKind.Masked, 0, 0, 4, 1);
        RawBlackLevel black = new RawBlackLevel(1, 1, 1, new double[] { 10 }, null, null);
        RawSensorImage decoded = new RawSensorImage(info, new ushort[16], null, 4, true, stored, null, new RawArea[] { masked }, black, new double[] { 110 }, null, null, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.ApplyColumnBandingCorrection = true;
        options.MaximumBandingCorrection = 4;

        RawSensorImage corrected = codec.PreprocessSensorImage(decoded, options);

        Assert.Multiple(() =>
        {
            Assert.That((corrected.BandingCorrection ?? throw new System.InvalidOperationException("A required value is unavailable.")).RowCorrectionApplied, Is.False);
            Assert.That(corrected.BandingCorrection.ColumnCorrectionApplied, Is.True);
            Assert.That(corrected.BandingCorrection.ColumnOffsets, Is.EqualTo(new double[] { -2, 0, 3, 1 }));
            Assert.That(corrected.BandingCorrection.CalibrationSampleCount, Is.EqualTo(4));
            Assert.That(corrected.FloatingSamples.Skip(4), Is.All.EqualTo(0.5F).Within(0.000001F));
            Assert.That(options.BlackLevel, Is.Null);
            Assert.That(options.MaskedAreas, Is.Empty);
            Assert.That(options.WhiteLevels, Is.Empty);
        });
    }

    [Test]
    public void IncompleteCalibrationExcessiveCorrectionAndCancellationAreRejected()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = RowInfo();
        RawStoredImage stored = new RawStoredImage(info, new uint[]
        {
            8, 58, 58, 58,
            10, 60, 60, 60,
            13, 63, 63, 63,
            11, 61, 61, 61
        }, null, null, 4, true, null);
        RawNormalizationOptions incomplete = RowOptions(true);
        incomplete.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.Masked, 0, 0, 1, 3) };
        RawNormalizationOptions limited = RowOptions(true);
        limited.MaximumBandingCorrection = 2;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawNormalizationOptions cancelled = RowOptions(true);
        cancelled.CancellationToken = cancellation.Token;
        RawNormalizationOptions conflicting = RowOptions(true);
        conflicting.EstimateBlackRowDeltas = true;
        conflicting.UseOpticalBlack = true;
        conflicting.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 1, 4) };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => codec.PreprocessStoredImage(stored, incomplete));
            Assert.Throws<RawLimitException>(() => codec.PreprocessStoredImage(stored, limited));
            Assert.Throws<OperationCanceledException>(() => codec.PreprocessStoredImage(stored, cancelled));
            Assert.Throws<RawFormatException>(() => codec.PreprocessStoredImage(stored, conflicting));
        });
    }

    private static RawNormalizationOptions RowOptions(bool enabled)
    {
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.BlackLevel = new RawBlackLevel(1, 1, 1, new double[] { 10 }, null, null);
        options.WhiteLevels = new double[] { 110 };
        options.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.Masked, 0, 0, 1, 4) };
        options.ApplyRowBandingCorrection = enabled;
        options.MaximumBandingCorrection = 4;
        return options;
    }

    private static RawImageInfo RowInfo()
    {
        return Info(new RawArea(RawAreaKind.Active, 1, 0, 3, 4));
    }

    private static RawImageInfo ColumnInfo()
    {
        return Info(new RawArea(RawAreaKind.Active, 0, 1, 4, 3));
    }

    private static RawImageInfo Info(RawArea active)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, 4, 4, 8, 4) };
        return new RawImageInfo(0, RawImageKind.Raw, 4, 4, 8, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 4), active, null, null);
    }
}
