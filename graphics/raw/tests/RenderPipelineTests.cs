using raw;

namespace raw.Tests;

public class RenderPipelineTests
{
    [Test]
    public void RawCameraPipelineAppliesWhiteBalanceBeforeExposureAndOutputConversion()
    {
        RawSensorImage sensor = RgbSensor(1, 1, new float[] { 0.1F, 0.2F, 0.3F }, RawOrientation.Normal, null, null);
        RawRenderOptions options = RawOptions(8);
        options.WhiteBalanceMode = RawWhiteBalanceMode.UserMultipliers;
        options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 2.0, 1.0, 1.0 }, 0.0, 0.0);
        options.ExposureCompensation = 1.0;
        options.GamutMode = RawGamutMode.Clip;

        RawRgbImage result = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ByteSamples, Is.EqualTo(new byte[] { 102, 102, 153 }));
            Assert.That(result.ColorSpace, Is.EqualTo(RawColorSpace.RawCamera));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void BuiltInColorPipelineMatchesDocumentedManualStageOrder()
    {
        RawWhiteBalance cameraBalance = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 1.6, 1.0, 1.2 }, 5200.0, 0.0);
        RawColorProfile profile = Profile();
        RawSensorImage sensor = RgbSensor(2, 1, new float[] { 0.1F, 0.2F, 0.3F, 0.4F, 0.3F, 0.2F }, RawOrientation.MirrorHorizontal, cameraBalance, new RawColorProfile[] { profile });
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.Camera;
        options.ColorSpace = RawColorSpace.Srgb;
        options.TransferFunction = RawTransferFunction.Gamma;
        options.Gamma = 2.0;
        options.ExposureCompensation = 0.25;
        options.CaptureSharpening = 0.25;
        options.ToneCurve = new double[] { 0.0, 0.4, 1.0 };
        options.Contrast = 1.1;
        options.GamutMode = RawGamutMode.Compress;
        options.OutputBitDepth = 8;
        options.OutputWidth = 3;
        options.OutputHeight = 1;
        options.ApplyOrientation = true;

        RawRgbImage reconstructed = RawRenderer.Reconstruct(sensor, options);
        RawRgbImage balanced = RawRenderer.ApplyWhiteBalance(reconstructed, sensor.WhiteBalance, options);
        RawRgbImage detailed = RawRenderer.EnhanceDetails(balanced, null, options);
        RawRgbImage exposed = RawRenderer.ApplyExposureAndHighlights(detailed, options);
        RawRenderOptions xyzOptions = RawRenderPipeline.CopyOptions(options);
        xyzOptions.ColorSpace = RawColorSpace.XyzD65;
        xyzOptions.TransferFunction = RawTransferFunction.Linear;
        RawRgbImage xyz = RawRenderer.ConvertCameraToXyz(exposed, sensor.ColorProfiles, 5200.0, xyzOptions);
        RawRenderOptions linearOptions = RawRenderPipeline.CopyOptions(options);
        linearOptions.TransferFunction = RawTransferFunction.Linear;
        RawRgbImage outputLinear = RawRenderer.ConvertOutputColor(xyz, linearOptions);
        RawRgbImage toned = RawRenderer.ApplyToneAndGamut(outputLinear, options);
        RawRgbImage expected = RawRenderer.CreateOutput(toned, null, options);

        RawRgbImage actual = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(actual.ByteSamples, Is.EqualTo(expected.ByteSamples));
            Assert.That(actual.Width, Is.EqualTo(3));
            Assert.That(actual.Height, Is.EqualTo(1));
            Assert.That(actual.OrientationApplied, Is.True);
            Assert.That(actual.ColorSpace, Is.EqualTo(RawColorSpace.Srgb));
            Assert.That(actual.TransferFunction, Is.EqualTo(RawTransferFunction.Gamma));
        });
    }

    [Test]
    public void HalfSizeCfaBranchCombinesOneBayerCellBeforeRemainingStages()
    {
        RawSensorImage sensor = CfaSensor(2, 2, new float[] { 0.2F, 0.4F, 0.6F, 0.8F }, RawOrientation.Normal);
        RawRenderOptions options = RawOptions(32);
        options.HalfSize = true;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;

        RawRgbImage result = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(1));
            Assert.That(result.Height, Is.EqualTo(1));
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.5F, 0.8F }).Within(0.000001F));
        });
    }

    [Test]
    public void GrayscaleBranchBakesOrientationAndCreatesRequestedDepth()
    {
        RawSensorImage sensor = GraySensor(2, 1, new float[] { 0.25F, 0.75F }, RawOrientation.MirrorHorizontal);
        RawRenderOptions options = RawOptions(8);
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;

        RawRgbImage result = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ByteSamples, Is.EqualTo(new byte[] { 191, 64 }));
            Assert.That(result.ChannelCount, Is.EqualTo(1));
            Assert.That(result.Channels[0].Kind, Is.EqualTo(RawChannelKind.Gray));
            Assert.That(result.OrientationApplied, Is.True);
        });
    }

    [Test]
    public void IlluminantTemperatureUsesCallerThenCameraThenDocumentedDefault()
    {
        RawWhiteBalance camera = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 1.0, 1.0, 1.0 }, 3200.0, 0.0);
        RawSensorImage sensor = RgbSensor(1, 1, new float[] { 0.1F, 0.2F, 0.3F }, RawOrientation.Normal, camera, null);
        RawRenderOptions options = RawOptions(32);
        options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Daylight, new double[] { 1.0, 1.0, 1.0 }, 4800.0, 0.0);

        double caller = RawRenderPipeline.ResolveIlluminantTemperature(sensor, options);
        options.WhiteBalance = null;
        double sensorValue = RawRenderPipeline.ResolveIlluminantTemperature(sensor, options);
        sensor.WhiteBalance = null;
        double fallback = RawRenderPipeline.ResolveIlluminantTemperature(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(caller, Is.EqualTo(4800.0));
            Assert.That(sensorValue, Is.EqualTo(3200.0));
            Assert.That(fallback, Is.EqualTo(6500.0));
        });
    }

    [Test]
    public void PostDemosaicCalibrationIsRebasedToTheSelectedCrop()
    {
        RawSensorImage sensor = CfaSensor(4, 4, Enumerable.Repeat(0.5F, 16).ToArray(), RawOrientation.Normal);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 1, 2, 2);
        sensor.UserCrop = crop;
        sensor.CropModel = new RawCropModel(sensor.Info, crop);
        sensor.ChromaticAberration = new RawChromaticAberration(1.5, 1.5, 1.0, 1.0);
        RawRenderOptions options = RawOptions(32);
        options.DemosaicMode = RawDemosaicMode.Bilinear;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ApplyPostDemosaicChromaticAberration = true;

        RawRgbImage result = RawRenderer.Render(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.FloatingSamples, Has.All.EqualTo(0.5F).Within(0.000001F));
        });
    }

    [Test]
    public void IncompatiblePipelineRequestsAreRejectedExplicitly()
    {
        RawSensorImage rgb = RgbSensor(1, 1, new float[] { 0.1F, 0.2F, 0.3F }, RawOrientation.Normal, null, null);
        RawSensorImage gray = GraySensor(1, 1, new float[] { 0.5F }, RawOrientation.Normal);
        RawSensorImage cfa = CfaSensor(2, 2, new float[] { 0.2F, 0.4F, 0.6F, 0.8F }, RawOrientation.Normal);
        RawRenderOptions xyz = RawOptions(32);
        xyz.ColorSpace = RawColorSpace.XyzD65;
        RawRenderOptions halfLinear = RawOptions(32);
        halfLinear.HalfSize = true;
        RawRenderOptions mosaic = RawOptions(32);
        mosaic.DemosaicMode = RawDemosaicMode.None;
        RawRenderOptions grayWhiteBalance = RawOptions(32);
        grayWhiteBalance.WhiteBalanceMode = RawWhiteBalanceMode.Camera;
        RawRenderOptions grayTone = RawOptions(32);
        grayTone.WhiteBalanceMode = RawWhiteBalanceMode.None;
        grayTone.Contrast = 1.5;
        RawRenderOptions doubleAberration = RawOptions(32);
        doubleAberration.WhiteBalanceMode = RawWhiteBalanceMode.None;
        doubleAberration.ApplyPostDemosaicChromaticAberration = true;
        cfa.ChromaticAberration = new RawChromaticAberration(0.5, 0.5, 1.0, 1.0);
        cfa.ChromaticAberrationApplied = true;
        RawRenderOptions needsProfile = new RawRenderOptions();
        needsProfile.DemosaicMode = RawDemosaicMode.None;
        needsProfile.WhiteBalanceMode = RawWhiteBalanceMode.None;
        needsProfile.ColorSpace = RawColorSpace.Srgb;
        needsProfile.TransferFunction = RawTransferFunction.Srgb;
        needsProfile.OutputBitDepth = 8;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(rgb, xyz));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(rgb, halfLinear));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(cfa, mosaic));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(gray, grayWhiteBalance));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(gray, grayTone));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Render(cfa, doubleAberration));
            Assert.Throws<RawProfileException>(() => RawRenderer.Render(rgb, needsProfile));
        });
    }

    [Test]
    public void PipelineChecksCancellationBeforeStartingStages()
    {
        RawSensorImage sensor = RgbSensor(1, 1, new float[] { 0.1F, 0.2F, 0.3F }, RawOrientation.Normal, null, null);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawRenderOptions options = RawOptions(32);
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.CancellationToken = source.Token;

        Assert.Throws<OperationCanceledException>(() => RawRenderer.Render(sensor, options));
    }

    private static RawRenderOptions RawOptions(int bitDepth)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = bitDepth;
        options.ApplyOrientation = true;
        return options;
    }

    private static RawSensorImage RgbSensor(int width, int height, float[] samples, RawOrientation orientation, RawWhiteBalance? whiteBalance, RawColorProfile[]? profiles)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        RawPlaneInfo[] planes = new RawPlaneInfo[]
        {
            new RawPlaneInfo(0, "R", RawChannelKind.Red, width, height, 16, width),
            new RawPlaneInfo(1, "G", RawChannelKind.Green, width, height, 16, width),
            new RawPlaneInfo(2, "B", RawChannelKind.Blue, width, height, 16, width)
        };
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, orientation, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
        return new RawSensorImage(info, null, samples, width * 3, true, null, null, null, null, null, whiteBalance, profiles, null);
    }

    private static RawSensorImage GraySensor(int width, int height, float[] samples, RawOrientation orientation)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, 16, width) };
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, orientation, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
        return new RawSensorImage(info, null, samples, width, true, null, null, null, null, null, null, null, null);
    }

    private static RawSensorImage CfaSensor(int width, int height, float[] samples, RawOrientation orientation)
    {
        RawCfaPattern cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, 16, width) };
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, orientation, channels, planes, cfa, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
        return new RawSensorImage(info, null, samples, width, true, null, null, null, null, null, null, null, null);
    }

    private static RawColorProfile Profile()
    {
        return new RawColorProfile("Identity D65", RawColorIlluminant.D65, 0.0, RawMatrixMath.Identity(), null, null, null, RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0), null);
    }
}
