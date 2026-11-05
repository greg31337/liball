using raw;

namespace raw.Tests;

public class DetailProcessingTests
{
    [Test]
    public void HotPixelModeReplacesOnlyIsolatedChannelSpike()
    {
        float[] samples = ConstantImage(3, 3, 0.2F, 0.2F, 0.2F);
        samples[(1 * 3 + 1) * 3] = 1.0F;
        RawRenderOptions options = Options();
        options.DenoiseMode = RawDenoiseMode.HotPixel;
        options.DenoiseStrength = 1.0;

        RawRgbImage result = RawRenderer.EnhanceDetails(Image(3, 3, samples, false), null, options);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(result, 1, 1), Is.EqualTo(new float[] { 0.2F, 0.2F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(result, 0, 0), Is.EqualTo(new float[] { 0.2F, 0.2F, 0.2F }).Within(0.000001F));
        });
    }

    [Test]
    public void MedianChromaRemovesColorImpulseAndPreservesLuminance()
    {
        float[] samples = ConstantImage(3, 3, 0.5F, 0.5F, 0.5F);
        int center = (1 * 3 + 1) * 3;
        samples[center] = 1.0F;
        samples[center + 1] = 0.40184563F;
        samples[center + 2] = 0.0F;
        double before = Luminance(samples[center], samples[center + 1], samples[center + 2]);
        RawRenderOptions options = Options();
        options.DenoiseMode = RawDenoiseMode.MedianChroma;
        options.DenoiseStrength = 1.0;

        RawRgbImage result = RawRenderer.EnhanceDetails(Image(3, 3, samples, false), null, options);
        float[] pixel = Pixel(result, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(pixel, Is.EqualTo(new float[] { 0.5F, 0.5F, 0.5F }).Within(0.000002F));
            Assert.That(Luminance(pixel[0], pixel[1], pixel[2]), Is.EqualTo(before).Within(0.0000001));
        });
    }

    [Test]
    public void WaveletLikeModeSoftThresholdsSmallLuminanceNoise()
    {
        float[] samples = ConstantImage(3, 3, 0.5F, 0.5F, 0.5F);
        int center = (1 * 3 + 1) * 3;
        samples[center] = 0.55F;
        samples[center + 1] = 0.55F;
        samples[center + 2] = 0.55F;
        RawRenderOptions options = Options();
        options.DenoiseMode = RawDenoiseMode.WaveletLike;
        options.DenoiseStrength = 1.0;

        RawRgbImage result = RawRenderer.EnhanceDetails(Image(3, 3, samples, false), null, options);
        float[] pixel = Pixel(result, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(pixel, Is.EqualTo(new float[] { 0.52F, 0.52F, 0.52F }).Within(0.000002F));
            Assert.That(pixel[0], Is.GreaterThan(0.5F));
            Assert.That(pixel[0], Is.LessThan(0.55F));
        });
    }

    [Test]
    public void FalseColorSuppressionWorksWithoutGeneralDenoise()
    {
        float[] samples = ConstantImage(3, 3, 0.4F, 0.4F, 0.4F);
        int center = (1 * 3 + 1) * 3;
        samples[center] = 0.9F;
        samples[center + 1] = 0.29175055F;
        samples[center + 2] = 0.0F;
        RawRenderOptions options = Options();
        options.DenoiseMode = RawDenoiseMode.None;
        options.FalseColorSuppression = 1.0;

        RawRgbImage result = RawRenderer.EnhanceDetails(Image(3, 3, samples, false), null, options);

        Assert.That(Pixel(result, 1, 1), Is.EqualTo(new float[] { 0.4F, 0.4F, 0.4F }).Within(0.000002F));
    }

    [Test]
    public void PostDemosaicChromaticAberrationResamplesRedAndBlueOnly()
    {
        RawRgbImage image = Image(3, 1, new float[]
        {
            0.0F, 0.1F, 0.2F,
            0.5F, 0.1F, 0.2F,
            1.0F, 0.1F, 0.2F
        }, false);
        RawChromaticAberration calibration = new RawChromaticAberration(0.0, 0.0, 0.5, 1.0);
        RawRenderOptions options = Options();
        options.ApplyPostDemosaicChromaticAberration = true;

        RawRgbImage result = RawRenderer.EnhanceDetails(image, calibration, options);

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
        {
            0.0F, 0.1F, 0.2F,
            0.25F, 0.1F, 0.2F,
            0.5F, 0.1F, 0.2F
        }).Within(0.000001F));
    }

    [Test]
    public void CaptureSharpeningEnhancesLuminanceDetailWithoutChangingChroma()
    {
        RawRgbImage image = Image(3, 1, new float[]
        {
            0.2F, 0.2F, 0.2F,
            0.5F, 0.5F, 0.5F,
            0.2F, 0.2F, 0.2F
        }, false);
        RawRenderOptions options = Options();
        options.CaptureSharpening = 1.0;

        RawRgbImage result = RawRenderer.EnhanceDetails(image, null, options);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(result, 0, 0), Is.EqualTo(new float[] { 0.125F, 0.125F, 0.125F }).Within(0.000002F));
            Assert.That(Pixel(result, 1, 0), Is.EqualTo(new float[] { 0.65F, 0.65F, 0.65F }).Within(0.000002F));
            Assert.That(Pixel(result, 2, 0), Is.EqualTo(new float[] { 0.125F, 0.125F, 0.125F }).Within(0.000002F));
        });
    }

    [Test]
    public void NeutralProcessingCanonicalizesChannelsAndPreservesAlphaAndState()
    {
        RawRgbImage image = Image(1, 1, new float[] { 0.3F, 0.2F, 0.1F, 0.75F }, true);
        image.Orientation = RawOrientation.MirrorVertical;
        image.OrientationApplied = true;

        RawRgbImage result = RawRenderer.EnhanceDetails(image, null, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.2F, 0.3F, 0.75F }));
            Assert.That(result.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue, RawChannelKind.Alpha }));
            Assert.That(result.Orientation, Is.EqualTo(RawOrientation.MirrorVertical));
            Assert.That(result.OrientationApplied, Is.True);
            Assert.That(result.ColorSpace, Is.EqualTo(image.ColorSpace));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
        });
    }

    [Test]
    public void InvalidControlsInputsCalibrationSamplesAndCancellationAreRejected()
    {
        RawRgbImage image = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        RawRgbImage nonlinear = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        nonlinear.TransferFunction = RawTransferFunction.Srgb;
        RawRgbImage xyz = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        xyz.ColorSpace = RawColorSpace.XyzD65;
        RawRgbImage nonFinite = Image(1, 1, new float[] { 0.2F, float.NaN, 0.4F }, false);
        RawRenderOptions invalid = Options();
        invalid.DenoiseMode = (RawDenoiseMode)99;
        invalid.DenoiseStrength = -1.0;
        invalid.FalseColorSuppression = 2.0;
        invalid.CaptureSharpening = 5.0;
        RawRenderOptions correction = Options();
        correction.ApplyPostDemosaicChromaticAberration = true;
        RawChromaticAberration outside = new RawChromaticAberration(2.0, 0.0, 1.0, 1.0);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = Options();
        cancelled.CancellationToken = cancellation.Token;

        string[]? contexts = (invalid.Validate().Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(contexts, Does.Contain("Render.DenoiseMode"));
            Assert.That(contexts, Does.Contain("Render.DenoiseStrength"));
            Assert.That(contexts, Does.Contain("Render.FalseColorSuppression"));
            Assert.That(contexts, Does.Contain("Render.CaptureSharpening"));
            Assert.Throws<RawFormatException>(() => RawRenderer.EnhanceDetails(image, null, invalid));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.EnhanceDetails(nonlinear, null, Options()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.EnhanceDetails(xyz, null, Options()));
            Assert.Throws<RawFormatException>(() => RawRenderer.EnhanceDetails(nonFinite, null, Options()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.EnhanceDetails(image, null, correction));
            Assert.Throws<RawFormatException>(() => RawRenderer.EnhanceDetails(image, outside, correction));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.EnhanceDetails(image, null, cancelled));
        });
    }

    private static RawRenderOptions Options()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.OutputBitDepth = 32;
        return options;
    }

    private static RawRgbImage Image(int width, int height, float[] samples, bool bgr)
    {
        int pixels = width * height;
        int channelCount = samples.Length == pixels * 4 ? 4 : 3;
        RawChannelDescription[] channels;
        if (bgr)
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else if (channelCount == 4)
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue)
            };
        }
        return new RawRgbImage(width, height, channelCount, width * channelCount, null, null, samples, channels, RawColorSpace.Srgb, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static float[] ConstantImage(int width, int height, float red, float green, float blue)
    {
        float[] samples = new float[width * height * 3];
        int pixel = 0;
        while (pixel < width * height)
        {
            samples[pixel * 3] = red;
            samples[pixel * 3 + 1] = green;
            samples[pixel * 3 + 2] = blue;
            pixel++;
        }
        return samples;
    }

    private static float[] Pixel(RawRgbImage image, int x, int y)
    {
        int index = (y * image.Width + x) * image.ChannelCount;
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }

    private static double Luminance(double red, double green, double blue)
    {
        return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    }
}
