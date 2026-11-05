using raw;

namespace raw.Tests;

public class ToneProcessingTests
{
    [Test]
    public void ToneCurveAndContrastProduceExactNormalizedSamples()
    {
        RawRenderOptions options = Options();
        options.ToneCurve = new double[] { 0.0, 0.25, 1.0 };
        options.Contrast = 2.0;

        RawRgbImage result = RawRenderer.ApplyToneAndGamut(Image(1, 1, new float[] { 0.25F, 0.5F, 0.75F }, false, RawTransferFunction.Linear), options);

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.0F, 0.25F, 1.0F }).Within(0.000001F));
    }

    [Test]
    public void GammaEncodingCanonicalizesBgrAndPreservesAlphaAndState()
    {
        RawRenderOptions options = Options();
        options.TransferFunction = RawTransferFunction.Gamma;
        options.Gamma = 2.0;
        RawRgbImage image = Image(1, 1, new float[] { 0.0625F, 0.25F, 1.0F, 0.4F }, true, RawTransferFunction.Linear);
        image.Orientation = RawOrientation.Rotate90Clockwise;

        RawRgbImage result = RawRenderer.ApplyToneAndGamut(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 1.0F, 0.5F, 0.25F, 0.4F }).Within(0.000001F));
            Assert.That(result.Channels.Select(channel => channel.Kind), Is.EqualTo(new[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue, RawChannelKind.Alpha }));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Gamma));
            Assert.That(result.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(result.OrientationApplied, Is.False);
            Assert.That(result.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
        });
    }

    [Test]
    public void GamutClipAndCompressionHaveDistinctDeterministicBehavior()
    {
        RawRgbImage image = Image(1, 1, new float[] { -0.2F, 0.4F, 0.8F }, false, RawTransferFunction.Linear);
        RawRenderOptions clipOptions = Options();
        clipOptions.GamutMode = RawGamutMode.Clip;
        RawRenderOptions compressionOptions = Options();
        compressionOptions.GamutMode = RawGamutMode.Compress;

        RawRgbImage clipped = RawRenderer.ApplyToneAndGamut(image, clipOptions);
        RawRgbImage compressed = RawRenderer.ApplyToneAndGamut(image, compressionOptions);

        double pivot = 0.2126 * -0.2 + 0.7152 * 0.4 + 0.0722 * 0.8;
        double scale = pivot / (pivot + 0.2);
        float expectedGreen = (float)(pivot + (0.4 - pivot) * scale);
        float expectedBlue = (float)(pivot + (0.8 - pivot) * scale);
        Assert.Multiple(() =>
        {
            Assert.That(clipped.FloatingSamples, Is.EqualTo(new float[] { 0.0F, 0.4F, 0.8F }).Within(0.000001F));
            Assert.That(compressed.FloatingSamples, Is.EqualTo(new float[] { 0.0F, expectedGreen, expectedBlue }).Within(0.000001F));
            Assert.That(compressed.FloatingSamples[1], Is.LessThan(clipped.FloatingSamples[1]));
            Assert.That(compressed.FloatingSamples[2], Is.LessThan(clipped.FloatingSamples[2]));
        });
    }

    [Test]
    public void NeutralToneSettingsPreserveFiniteOutOfGamutValues()
    {
        RawRgbImage image = Image(1, 1, new float[] { -0.25F, 0.5F, 1.25F }, false, RawTransferFunction.Linear);

        RawRgbImage result = RawRenderer.ApplyToneAndGamut(image, Options());

        Assert.That(result.FloatingSamples, Is.EqualTo(image.FloatingSamples));
    }

    [Test]
    public void OrderedDitheringUsesStableFourByFourPatternWithoutDitheringAlpha()
    {
        float[] samples = new float[4 * 4 * 4];
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] = 0.5F;
        }
        RawRgbImage image = Image(4, 4, samples, false, RawTransferFunction.Srgb);
        RawRenderOptions options = Options();
        options.OutputBitDepth = 8;
        RawRgbImage plain = RawRenderer.ConvertToEightBit(image, options);
        options.OrderedDithering = true;

        RawRgbImage dithered = RawRenderer.ConvertToEightBit(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(plain.ByteSamples, Has.All.EqualTo(128));
            int pixel = 0;
            while (pixel < 16)
            {
                int expected = (pixel + pixel / 4) % 2 == 0 ? 127 : 128;
                Assert.That(dithered.ByteSamples[pixel * 4], Is.EqualTo(expected));
                Assert.That(dithered.ByteSamples[pixel * 4 + 1], Is.EqualTo(expected));
                Assert.That(dithered.ByteSamples[pixel * 4 + 2], Is.EqualTo(expected));
                Assert.That(dithered.ByteSamples[pixel * 4 + 3], Is.EqualTo(128));
                pixel++;
            }
            Assert.That(dithered.FloatingSamples, Is.Empty);
            Assert.That(dithered.TransferFunction, Is.EqualTo(RawTransferFunction.Srgb));
            Assert.That(dithered.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void EightBitConversionClipsBoundsAndRoundsWithoutDither()
    {
        RawRgbImage image = Image(1, 1, new float[] { -1.0F, 0.5F, 2.0F }, false, RawTransferFunction.Linear);
        RawRenderOptions options = Options();
        options.OutputBitDepth = 8;

        RawRgbImage result = RawRenderer.ConvertToEightBit(image, options);

        Assert.That(result.ByteSamples, Is.EqualTo(new byte[] { 0, 128, 255 }));
    }

    [Test]
    public void InvalidToneInputsOutputDepthAndCancellationAreRejected()
    {
        RawRgbImage linear = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false, RawTransferFunction.Linear);
        RawRgbImage nonlinear = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false, RawTransferFunction.Srgb);
        RawRgbImage nonFinite = Image(1, 1, new float[] { float.NaN, 0.3F, 0.4F }, false, RawTransferFunction.Linear);
        RawRgbImage xyz = Image(1, 1, new float[] { 0.2F, 0.3F, 0.4F }, false, RawTransferFunction.Linear);
        xyz.ColorSpace = RawColorSpace.XyzD65;
        RawRenderOptions badTransfer = Options();
        badTransfer.ColorSpace = RawColorSpace.AcesCg;
        badTransfer.TransferFunction = RawTransferFunction.Srgb;
        linear.ColorSpace = RawColorSpace.AcesCg;
        RawRenderOptions wrongDepth = Options();
        wrongDepth.OutputBitDepth = 32;
        RawRenderOptions eightBit = Options();
        eightBit.OutputBitDepth = 8;
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawRenderOptions cancelled = Options();
        cancelled.CancellationToken = source.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyToneAndGamut(nonlinear, Options()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyToneAndGamut(xyz, Options()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ApplyToneAndGamut(linear, badTransfer));
            linear.ColorSpace = RawColorSpace.Srgb;
            Assert.Throws<RawFormatException>(() => RawRenderer.ApplyToneAndGamut(nonFinite, Options()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertToEightBit(linear, wrongDepth));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertToEightBit(nonFinite, eightBit));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ApplyToneAndGamut(linear, cancelled));
        });
    }

    private static RawRenderOptions Options()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.ColorSpace = RawColorSpace.Srgb;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = 32;
        return options;
    }

    private static RawRgbImage Image(int width, int height, float[] samples, bool bgr, RawTransferFunction transfer)
    {
        int channelCount = samples.Length / (width * height);
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
        return new RawRgbImage(width, height, channelCount, width * channelCount, null, null, samples, channels, RawColorSpace.Srgb, transfer, RawOrientation.Normal, false, true, null);
    }
}
