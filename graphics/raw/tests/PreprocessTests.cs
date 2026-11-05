using raw;

namespace raw.Tests;

public class PreprocessTests
{
    [Test]
    public void PreprocessAppliesLinearizationBlackAndWhiteAndRetainsExactValues()
    {
        RawImageInfo info = CfaInfo(4, 2, 3);
        info.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 3, 2);
        info.DefaultCrop = new RawArea(RawAreaKind.DefaultCrop, 1, 0, 2, 2);
        uint[] storedValues = new uint[] { 0, 1, 2, 3, uint.MaxValue, 4, 5, 6, 7 };
        RawStoredImage stored = new RawStoredImage(info, storedValues, null, null, 5, false, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.UserCrop = new RawArea(RawAreaKind.UserCrop, 0, 1, 3, 1);
        options.LinearizationTable = new ushort[] { 10, 20, 30, 40, 50, 60, 70, 80 };
        options.BlackLevel = RawBlackLevel.CreateScalar(20, 1);
        options.WhiteLevels = new double[] { 70 };
        options.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 3, 0, 1, 2) };

        RawSensorImage result = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0, 0, 0.2F, 0.4F, 0.6F, 0.8F, 1, 1 }).Within(0.000001F));
            Assert.That(result.IntegerSamples, Is.Empty);
            Assert.That(result.RowStrideSamples, Is.EqualTo(4));
            Assert.That(result.StoredImage, Is.SameAs(stored));
            Assert.That(((result.CropModel ?? throw new System.InvalidOperationException("A required value is unavailable.")).StoredArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, Is.EqualTo(4));
            Assert.That((result.CropModel.DefaultCrop ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(1));
            Assert.That((result.CropModel.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.UserCrop));
            Assert.That(result.CropModel.SelectedArea.Y, Is.EqualTo(1));
            Assert.That(result.UserCrop, Is.Not.SameAs(options.UserCrop));
            Assert.That(result.MaskedAreas.Single(), Is.Not.SameAs(options.MaskedAreas[0]));
            Assert.That((result.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.EqualTo(new byte[] { 1, 1, 0, 0, 0, 0, 0, 0 }));
            Assert.That(result.ClippingMask.WhiteClipped, Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 1, 1 }));
            Assert.That(result.ClippingMask.IsBlackClipped(0, 0, 0), Is.True);
            Assert.That(result.ClippingMask.IsWhiteClipped(3, 1, 0), Is.True);
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void PerChannelCalibrationProducesIndependentClippingMasks()
    {
        RawImageInfo info = RgbInfo(2, 1, 12);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 10, 20, 30, 110, 220, 330 }, null, null, 6, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.BlackLevel = RawBlackLevel.CreatePerChannel(new double[] { 10, 20, 30 });
        options.WhiteLevels = new double[] { 110, 220, 330 };

        RawSensorImage result = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0, 0, 0, 1, 1, 1 }));
            Assert.That((result.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.EqualTo(new byte[] { 1, 1, 1, 0, 0, 0 }));
            Assert.That(result.ClippingMask.WhiteClipped, Is.EqualTo(new byte[] { 0, 0, 0, 1, 1, 1 }));
            Assert.That(result.WhiteLevels, Is.EqualTo(new double[] { 110, 220, 330 }));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void DisabledCalibrationUsesDeclaredNumericRangeAndCanDropOriginalValues()
    {
        RawImageInfo info = GrayInfo(3, 1, 10, RawSampleKind.UnsignedInteger);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 0, 511, 1023 }, null, null, 3, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions();
        options.ApplyLinearization = false;
        options.SubtractBlackLevel = false;
        options.ScaleWhiteLevel = false;
        options.PreserveOriginalValues = false;

        RawSensorImage result = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[0], Is.Zero);
            Assert.That(result.FloatingSamples[1], Is.EqualTo(511.0F / 1023.0F).Within(0.000001F));
            Assert.That(result.FloatingSamples[2], Is.EqualTo(1.0F));
            Assert.That(result.StoredImage, Is.Null);
            Assert.That((result.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(0, 0, 0), Is.Zero);
            Assert.That(result.WhiteLevels, Is.EqualTo(new double[] { 1023 }));
        });
    }

    [Test]
    public void SignedAndFloatingStoredDomainsNormalizeWithoutIntegerReinterpretation()
    {
        RawImageInfo signedInfo = GrayInfo(3, 1, 4, RawSampleKind.SignedInteger);
        RawStoredImage signed = new RawStoredImage(signedInfo, null, new int[] { -8, 0, 7 }, null, 3, true, null);
        RawImageInfo signedOneBitInfo = GrayInfo(2, 1, 1, RawSampleKind.SignedInteger);
        RawStoredImage signedOneBit = new RawStoredImage(signedOneBitInfo, null, new int[] { -1, 0 }, null, 2, true, null);
        RawImageInfo floatingInfo = GrayInfo(3, 1, 32, RawSampleKind.Single);
        RawStoredImage floating = new RawStoredImage(floatingInfo, null, null, new float[] { 0, 0.25F, 1 }, 3, true, null);

        RawSensorImage signedResult = RawRenderer.Preprocess(signed, new RawNormalizationOptions());
        RawSensorImage signedOneBitResult = RawRenderer.Preprocess(signedOneBit, new RawNormalizationOptions());
        RawSensorImage floatingResult = RawRenderer.Preprocess(floating, new RawNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(signedResult.FloatingSamples[0], Is.Zero);
            Assert.That(signedResult.FloatingSamples[1], Is.EqualTo(8.0F / 15.0F).Within(0.000001F));
            Assert.That(signedResult.FloatingSamples[2], Is.EqualTo(1.0F));
            Assert.That(signedOneBitResult.FloatingSamples, Is.EqualTo(new float[] { 0, 1 }));
            Assert.That(floatingResult.FloatingSamples, Is.EqualTo(new float[] { 0, 0.25F, 1 }));
            Assert.That(signedResult.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(signedOneBitResult.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(floatingResult.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void CropPrecedenceFallsBackFromUserToDefaultActiveAndStored()
    {
        RawImageInfo info = CfaInfo(6, 4, 12);
        info.ActiveArea = new RawArea(RawAreaKind.Active, 1, 1, 4, 3);
        info.DefaultCrop = new RawArea(RawAreaKind.DefaultCrop, 2, 1, 2, 2);

        RawCropModel user = RawRenderer.GetCropModel(info, new RawArea(RawAreaKind.UserCrop, 1, 2, 3, 1));
        RawCropModel camera = RawRenderer.GetCropModel(info, null);
        info.DefaultCrop = null;
        RawCropModel active = RawRenderer.GetCropModel(info, null);
        info.ActiveArea = null;
        RawCropModel stored = RawRenderer.GetCropModel(info, null);

        Assert.Multiple(() =>
        {
            Assert.That((user.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.UserCrop));
            Assert.That((camera.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.DefaultCrop));
            Assert.That((active.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.Active));
            Assert.That((stored.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.StoredImage));
        });

        info.ActiveArea = new RawArea(RawAreaKind.Active, 1, 1, 4, 3);
        Assert.Throws<RawFormatException>(() => RawRenderer.GetCropModel(info, new RawArea(RawAreaKind.UserCrop, 0, 0, 2, 2)));
    }

    [Test]
    public void InvalidLinearizationAndCalibrationAreRejectedAndCancellationIsObserved()
    {
        RawImageInfo info = GrayInfo(2, 1, 4, RawSampleKind.UnsignedInteger);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 0, 3 }, null, null, 2, true, null);
        RawNormalizationOptions table = new RawNormalizationOptions();
        table.LinearizationTable = new ushort[] { 0, 100, 200 };
        RawNormalizationOptions range = new RawNormalizationOptions();
        range.BlackLevel = RawBlackLevel.CreateScalar(100, 1);
        range.WhiteLevels = new double[] { 100 };
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawNormalizationOptions cancelled = new RawNormalizationOptions();
        cancelled.CancellationToken = source.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.Preprocess(stored, table));
            Assert.Throws<RawFormatException>(() => RawRenderer.Preprocess(stored, range));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.Preprocess(stored, cancelled));
        });
    }

    [Test]
    public void SensorImageOverloadUsesRetainedCalibrationWithoutMutatingOptions()
    {
        RawImageInfo info = GrayInfo(2, 1, 8, RawSampleKind.UnsignedInteger);
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 10, 110 }, null, null, 2, true, null);
        RawWhiteBalance whiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 1.0 }, 5000.0, 0.0);
        RawColorProfile profile = new RawColorProfile("Test", RawColorIlluminant.D65, 0.0, RawMatrixMath.Identity(), null, null, null, RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0), null);
        RawSensorImage decoded = new RawSensorImage(info, new ushort[] { 2570, 28270 }, null, 2, true, stored, null, null, RawBlackLevel.CreateScalar(10, 1), new double[] { 110 }, whiteBalance, new RawColorProfile[] { profile }, null);
        decoded.ChromaticAberration = new RawChromaticAberration(0.5, 0.0, 1.0, 1.0);
        RawNormalizationOptions options = new RawNormalizationOptions();

        RawSensorImage result = RawRenderer.Preprocess(decoded, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0, 1 }));
            Assert.That(options.BlackLevel, Is.Null);
            Assert.That(options.WhiteLevels, Is.Empty);
            Assert.That(result.StoredImage, Is.SameAs(stored));
            Assert.That(result.WhiteBalance, Is.SameAs(whiteBalance));
            Assert.That(result.ColorProfiles.Single(), Is.SameAs(profile));
            Assert.That(result.ChromaticAberration, Is.SameAs(decoded.ChromaticAberration));
        });
    }

    private static RawImageInfo CfaInfo(int width, int height, int bitDepth)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, bitDepth, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height), null);
    }

    private static RawImageInfo RgbInfo(int width, int height, int bitDepth)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        RawPlaneInfo[] planes = new RawPlaneInfo[]
        {
            new RawPlaneInfo(0, "R", RawChannelKind.Red, width, height, bitDepth, width),
            new RawPlaneInfo(1, "G", RawChannelKind.Green, width, height, bitDepth, width),
            new RawPlaneInfo(2, "B", RawChannelKind.Blue, width, height, bitDepth, width)
        };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height, int bitDepth, RawSampleKind sampleKind)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, bitDepth, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, sampleKind, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
