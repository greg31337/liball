using raw;

namespace raw.Tests;

public class WhiteBalanceTests
{
    [Test]
    public void CameraModeNormalizesRgbAndRggbCalibrationsWithoutChangingInput()
    {
        RawRgbImage image = Rgb(1, 1, new float[] { 0.1F, 0.2F, 0.1F });
        RawRenderOptions options = Options(RawWhiteBalanceMode.Camera);
        RawWhiteBalance rgbCalibration = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 2.0, 1.0, 3.0 }, 0.0, 0.0);
        RawWhiteBalance rggbCalibration = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 2.0, 1.0, 3.0, 4.0 }, 0.0, 0.0);

        RawRgbImage rgb = RawRenderer.ApplyWhiteBalance(image, rgbCalibration, options);
        RawRgbImage rggb = RawRenderer.ApplyWhiteBalance(image, rggbCalibration, options);

        Assert.Multiple(() =>
        {
            Assert.That(rgb.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.2F, 0.3F }).Within(0.000001F));
            Assert.That(rggb.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.2F, 0.2F }).Within(0.000001F));
            Assert.That(image.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.2F, 0.1F }));
            Assert.That(rgb.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
            Assert.That(rgb.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void DaylightModeUsesStandardTemperatureOrCallerProfileMultipliers()
    {
        RawRgbImage image = Rgb(1, 1, new float[] { 0.25F, 0.25F, 0.25F });
        RawRenderOptions standardOptions = Options(RawWhiteBalanceMode.Daylight);
        RawRenderOptions profileOptions = Options(RawWhiteBalanceMode.Daylight);
        profileOptions.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Daylight, new double[] { 2.0, 1.0, 1.5 }, 6500.0, 0.0);
        double[] standardMultipliers = RawWhiteBalanceProcessor.TemperatureMultipliers(6500.0, 0.0);

        RawRgbImage standard = RawRenderer.ApplyWhiteBalance(image, null, standardOptions);
        RawRgbImage profile = RawRenderer.ApplyWhiteBalance(image, null, profileOptions);

        Assert.Multiple(() =>
        {
            Assert.That(standard.FloatingSamples, Is.EqualTo(standardMultipliers.Select(value => (float)(value * 0.25)).ToArray()).Within(0.000001F));
            Assert.That(profile.FloatingSamples, Is.EqualTo(new float[] { 0.5F, 0.25F, 0.375F }).Within(0.000001F));
        });
    }

    [Test]
    public void AutoModeUsesOnlyTheSelectedUnclippedRegion()
    {
        RawRgbImage image = Rgb(2, 2, new float[]
        {
            0.2F, 0.4F, 0.8F,
            0.2F, 0.4F, 0.8F,
            0.8F, 0.1F, 0.1F,
            0.8F, 0.1F, 0.1F
        });
        RawRenderOptions options = Options(RawWhiteBalanceMode.Auto);
        options.WhiteBalanceArea = new RawArea(RawAreaKind.UserCrop, 0, 0, 2, 1);

        RawRgbImage result = RawRenderer.ApplyWhiteBalance(image, null, options);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(result, 0, 0), Is.EqualTo(new float[] { 0.8F, 0.8F, 0.8F }).Within(0.000001F));
            Assert.That(Pixel(result, 1, 0), Is.EqualTo(new float[] { 0.8F, 0.8F, 0.8F }).Within(0.000001F));
            Assert.That(Pixel(result, 0, 1), Is.EqualTo(new float[] { 3.2F, 0.2F, 0.1F }).Within(0.000001F));
        });
    }

    [Test]
    public void UserModeBalancesRgbButPreservesAlphaAndNoneCopiesSamples()
    {
        RawRgbImage image = Rgba(new float[] { 0.2F, 0.4F, 0.8F, 0.33F });
        RawRenderOptions userOptions = Options(RawWhiteBalanceMode.UserMultipliers);
        userOptions.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 4.0, 2.0, 1.0 }, 0.0, 0.0);
        RawRenderOptions noneOptions = Options(RawWhiteBalanceMode.None);

        RawRgbImage balanced = RawRenderer.ApplyWhiteBalance(image, null, userOptions);
        RawRgbImage copied = RawRenderer.ApplyWhiteBalance(image, null, noneOptions);

        Assert.Multiple(() =>
        {
            Assert.That(balanced.FloatingSamples, Is.EqualTo(new float[] { 0.8F, 0.8F, 0.8F, 0.33F }).Within(0.000001F));
            Assert.That(copied.FloatingSamples, Is.EqualTo(image.FloatingSamples));
            Assert.That(copied.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
        });
    }

    [Test]
    public void TemperatureAndTintConversionMovesAlongBlueRedAndGreenAxes()
    {
        double[] warm = RawWhiteBalanceProcessor.TemperatureMultipliers(2500.0, 0.0);
        double[] cool = RawWhiteBalanceProcessor.TemperatureMultipliers(10000.0, 0.0);
        double[] neutral = RawWhiteBalanceProcessor.TemperatureMultipliers(6500.0, 0.0);
        double[] tinted = RawWhiteBalanceProcessor.TemperatureMultipliers(6500.0, 1.0);
        RawRenderOptions options = Options(RawWhiteBalanceMode.TemperatureAndTint);
        options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.TemperatureAndTint, null, 2500.0, 0.0);

        RawRgbImage result = RawRenderer.ApplyWhiteBalance(Rgb(1, 1, new float[] { 0.1F, 0.1F, 0.1F }), null, options);

        Assert.Multiple(() =>
        {
            Assert.That(warm[2], Is.GreaterThan(warm[0]));
            Assert.That(cool[0], Is.GreaterThan(cool[2]));
            Assert.That(tinted[1] / tinted[0], Is.EqualTo(neutral[1] / neutral[0] * 2.0).Within(0.000001));
            Assert.That(warm.Min(), Is.EqualTo(1.0).Within(0.000001));
            Assert.That(cool.Min(), Is.EqualTo(1.0).Within(0.000001));
            Assert.That(result.FloatingSamples, Is.EqualTo(warm.Select(value => (float)(value * 0.1)).ToArray()).Within(0.000001F));
        });
    }

    [Test]
    public void InvalidCalibrationRegionInputAndCancellationAreRejected()
    {
        RawRgbImage image = Rgb(1, 1, new float[] { 0.2F, 0.4F, 0.8F });
        RawRenderOptions missingUser = Options(RawWhiteBalanceMode.UserMultipliers);
        RawRenderOptions outside = Options(RawWhiteBalanceMode.Auto);
        outside.WhiteBalanceArea = new RawArea(RawAreaKind.UserCrop, 1, 0, 1, 1);
        RawRenderOptions darkAuto = Options(RawWhiteBalanceMode.Auto);
        RawRenderOptions invalid = Options(RawWhiteBalanceMode.UserMultipliers);
        invalid.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 1.0, double.NaN, 1.0 }, 0.0, 0.0);
        RawRenderOptions nonlinearOptions = Options(RawWhiteBalanceMode.Daylight);
        RawRgbImage nonlinear = Rgb(1, 1, new float[] { 0.2F, 0.4F, 0.8F });
        nonlinear.TransferFunction = RawTransferFunction.Srgb;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = Options(RawWhiteBalanceMode.Daylight);
        cancelled.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.That(missingUser.Validate().IsValid(), Is.False);
            Assert.That(invalid.Validate().IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyWhiteBalance(image, null, missingUser));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyWhiteBalance(image, null, Options(RawWhiteBalanceMode.Camera)));
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyWhiteBalance(image, null, outside));
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyWhiteBalance(Rgb(1, 1, new float[] { 0.0F, 0.0F, 0.0F }), null, darkAuto));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyWhiteBalance(nonlinear, null, nonlinearOptions));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ApplyWhiteBalance(image, null, cancelled));
        });
    }

    private static RawRenderOptions Options(RawWhiteBalanceMode mode)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.WhiteBalanceMode = mode;
        return options;
    }

    private static float[] Pixel(RawRgbImage image, int x, int y)
    {
        int index = y * image.RowStrideSamples + x * image.ChannelCount;
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }

    private static RawRgbImage Rgb(int width, int height, float[] samples)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(width, height, 3, width * 3, null, null, samples, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage Rgba(float[] samples)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        };
        return new RawRgbImage(1, 1, 4, 4, null, null, samples, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }
}
