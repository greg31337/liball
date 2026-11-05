using raw;

namespace raw.Tests;

public class ExposureHighlightTests
{
    [Test]
    public void ExposureAndTaperedShadowScalingPreserveAlphaAndImageState()
    {
        RawRgbImage image = Image(1, 1, new float[] { 0.25F, 0.5F, 1.0F, 0.7F }, false);
        image.Orientation = RawOrientation.Rotate270Clockwise;
        image.OrientationApplied = true;
        RawRenderOptions options = Options(RawHighlightMode.Clip);
        options.ExposureCompensation = 1.0;
        options.ShadowScale = 2.0;

        RawRgbImage result = RawRenderer.ApplyExposureAndHighlights(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.875F, 1.0F, 1.0F, 0.7F }).Within(0.000001F));
            Assert.That(result.RowStrideSamples, Is.EqualTo(4));
            Assert.That(result.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue, RawChannelKind.Alpha }));
            Assert.That(result.ColorSpace, Is.EqualTo(RawColorSpace.Srgb));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(result.Orientation, Is.EqualTo(RawOrientation.Rotate270Clockwise));
            Assert.That(result.OrientationApplied, Is.True);
            Assert.That(result.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
        });
    }

    [Test]
    public void AutoBrightnessUsesConfiguredUpperPercentile()
    {
        RawRgbImage image = Image(4, 1, new float[]
        {
            0.1F, 0.1F, 0.1F,
            0.2F, 0.2F, 0.2F,
            0.5F, 0.5F, 0.5F,
            10.0F, 10.0F, 10.0F
        }, false);
        RawRenderOptions options = Options(RawHighlightMode.Clip);
        options.AutoBrightness = true;
        options.AutoBrightnessClippingThreshold = 0.25;

        RawRgbImage result = RawRenderer.ApplyExposureAndHighlights(image, options);

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
        {
            0.2F, 0.2F, 0.2F,
            0.4F, 0.4F, 0.4F,
            1.0F, 1.0F, 1.0F,
            1.0F, 1.0F, 1.0F
        }).Within(0.000001F));
    }

    [Test]
    public void PerChannelSaturationProtectionConstrainsExposureScale()
    {
        RawRgbImage image = Image(2, 1, new float[]
        {
            0.9F, 0.2F, 0.2F,
            0.8F, 0.1F, 0.1F
        }, false);
        RawRenderOptions options = Options(RawHighlightMode.Clip);
        options.ExposureCompensation = 1.0;
        options.AutoBrightnessClippingThreshold = 0.0;
        options.SaturationProtection = 1.0;

        RawRgbImage result = RawRenderer.ApplyExposureAndHighlights(image, options);

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
        {
            1.0F, 0.222222F, 0.222222F,
            0.888889F, 0.111111F, 0.111111F
        }).Within(0.000002F));
    }

    [Test]
    public void ClipBlendAndReconstructUseDistinctHighlightPolicies()
    {
        float[] samples = new float[]
        {
            0.8F, 0.4F, 0.2F,
            1.4F, 0.4F, 0.2F,
            0.8F, 0.4F, 0.2F
        };

        RawRgbImage clipped = RawRenderer.ApplyExposureAndHighlights(Image(3, 1, samples, false), Options(RawHighlightMode.Clip));
        RawRgbImage blended = RawRenderer.ApplyExposureAndHighlights(Image(3, 1, samples, false), Options(RawHighlightMode.Blend));
        RawRgbImage reconstructed = RawRenderer.ApplyExposureAndHighlights(Image(3, 1, samples, false), Options(RawHighlightMode.Reconstruct));

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(clipped, 1), Is.EqualTo(new float[] { 1.0F, 0.4F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(blended, 1), Is.EqualTo(new float[] { 1.0F, 0.285714F, 0.142857F }).Within(0.000002F));
            Assert.That(Pixel(reconstructed, 1), Is.EqualTo(new float[] { 0.8F, 0.4F, 0.2F }).Within(0.000002F));
            Assert.That(Pixel(reconstructed, 0), Is.EqualTo(new float[] { 0.8F, 0.4F, 0.2F }).Within(0.000001F));
        });
    }

    [Test]
    public void InputChannelOrderIsCanonicalizedAndNegativeLinearValuesRemainRepresentable()
    {
        RawRgbImage bgr = Image(1, 1, new float[] { 0.3F, -0.2F, 0.1F, 0.6F }, true);

        RawRgbImage result = RawRenderer.ApplyExposureAndHighlights(bgr, Options(RawHighlightMode.Clip));

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.1F, -0.2F, 0.3F, 0.6F }));
    }

    [Test]
    public void InvalidOptionsInputsSamplesOverflowAndCancellationAreRejected()
    {
        RawRgbImage image = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        RawRgbImage nonlinear = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        nonlinear.TransferFunction = RawTransferFunction.Srgb;
        RawRgbImage xyz = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false);
        xyz.ColorSpace = RawColorSpace.XyzD50;
        RawRgbImage nonFinite = Image(1, 1, new float[] { float.NaN, 0.3F, 0.4F }, false);
        RawRenderOptions invalid = Options(RawHighlightMode.Clip);
        invalid.AutoBrightnessClippingThreshold = 1.0;
        invalid.ShadowScale = 0.0;
        invalid.SaturationProtection = 2.0;
        RawRenderOptions overflow = Options(RawHighlightMode.Clip);
        overflow.ExposureCompensation = 1024.0;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = Options(RawHighlightMode.Clip);
        cancelled.CancellationToken = cancellation.Token;

        string[]? contexts = (invalid.Validate().Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Select(message => message.Context).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(contexts, Does.Contain("Render.AutoBrightnessClippingThreshold"));
            Assert.That(contexts, Does.Contain("Render.ShadowScale"));
            Assert.That(contexts, Does.Contain("Render.SaturationProtection"));
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyExposureAndHighlights(image, invalid));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyExposureAndHighlights(nonlinear, Options(RawHighlightMode.Clip)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyExposureAndHighlights(xyz, Options(RawHighlightMode.Clip)));
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyExposureAndHighlights(nonFinite, Options(RawHighlightMode.Clip)));
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyExposureAndHighlights(image, overflow));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ApplyExposureAndHighlights(image, cancelled));
        });
    }

    private static RawRenderOptions Options(RawHighlightMode highlightMode)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.HighlightMode = highlightMode;
        options.OutputBitDepth = 32;
        return options;
    }

    private static RawRgbImage Image(int width, int height, float[] samples, bool bgr)
    {
        int pixelCount = width * height;
        int channelCount = samples.Length == pixelCount * 4 ? 4 : 3;
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

    private static float[] Pixel(RawRgbImage image, int x)
    {
        int index = x * image.ChannelCount;
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }
}
