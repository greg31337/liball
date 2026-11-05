using raw;

namespace raw.Tests;

public class OutputProcessingTests
{
    [TestCase(RawOrientation.Normal, 2, 3, "1,2,3,4,5,6")]
    [TestCase(RawOrientation.MirrorHorizontal, 2, 3, "2,1,4,3,6,5")]
    [TestCase(RawOrientation.Rotate180, 2, 3, "6,5,4,3,2,1")]
    [TestCase(RawOrientation.MirrorVertical, 2, 3, "5,6,3,4,1,2")]
    [TestCase(RawOrientation.Transpose, 3, 2, "1,3,5,2,4,6")]
    [TestCase(RawOrientation.Rotate90Clockwise, 3, 2, "5,3,1,6,4,2")]
    [TestCase(RawOrientation.Transverse, 3, 2, "6,4,2,5,3,1")]
    [TestCase(RawOrientation.Rotate270Clockwise, 3, 2, "2,4,6,1,3,5")]
    public void EveryExifOrientationMapsCoordinatesExactly(RawOrientation orientation, int expectedWidth, int expectedHeight, string expectedText)
    {
        RawRgbImage image = Gray(2, 3, new float[] { 1, 2, 3, 4, 5, 6 });
        image.Orientation = orientation;

        RawRgbImage result = RawRenderer.CreateOutput(image, null, Options(32));

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(expectedWidth));
            Assert.That(result.Height, Is.EqualTo(expectedHeight));
            Assert.That(result.FloatingSamples, Is.EqualTo(expectedText.Split(',').Select(float.Parse)));
            Assert.That(result.Orientation, Is.EqualTo(orientation));
            Assert.That(result.OrientationApplied, Is.True);
        });
    }

    [Test]
    public void CropIsBakedBeforeOrientation()
    {
        RawRgbImage image = Gray(4, 3, new float[]
        {
            0, 1, 2, 3,
            4, 5, 6, 7,
            8, 9, 10, 11
        });
        image.Orientation = RawOrientation.Rotate90Clockwise;
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 0, 2, 3);

        RawRgbImage result = RawRenderer.CreateOutput(image, crop, Options(32));

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 9, 5, 1, 10, 6, 2 }));
        });
    }

    [Test]
    public void DisabledOrientationRetainsCoordinatesAndAppliedState()
    {
        RawRgbImage image = Gray(2, 1, new float[] { 0.25F, 0.75F });
        image.Orientation = RawOrientation.MirrorHorizontal;
        RawRenderOptions options = Options(32);
        options.ApplyOrientation = false;

        RawRgbImage result = RawRenderer.CreateOutput(image, null, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(image.FloatingSamples));
            Assert.That(result.Orientation, Is.EqualTo(RawOrientation.MirrorHorizontal));
            Assert.That(result.OrientationApplied, Is.False);
        });
    }

    [Test]
    public void NearestAndBilinearResamplingUsePixelCenterCoordinates()
    {
        RawRgbImage image = Gray(2, 1, new float[] { 0.0F, 1.0F });
        RawRenderOptions nearest = Options(32);
        nearest.OutputWidth = 4;
        nearest.OutputHeight = 1;
        nearest.Resampler = RawResampler.NearestNeighbor;
        RawRenderOptions bilinear = Options(32);
        bilinear.OutputWidth = 3;
        bilinear.OutputHeight = 1;
        bilinear.Resampler = RawResampler.Bilinear;

        RawRgbImage nearestResult = RawRenderer.CreateOutput(image, null, nearest);
        RawRgbImage bilinearResult = RawRenderer.CreateOutput(image, null, bilinear);

        Assert.Multiple(() =>
        {
            Assert.That(nearestResult.FloatingSamples, Is.EqualTo(new float[] { 0, 0, 1, 1 }));
            Assert.That(bilinearResult.FloatingSamples, Is.EqualTo(new float[] { 0, 0.5F, 1 }).Within(0.000001F));
        });
    }

    [TestCase(RawResampler.Bicubic)]
    [TestCase(RawResampler.Lanczos)]
    public void HighQualityResamplersPreserveConstantsAndImpulseSymmetry(RawResampler resampler)
    {
        RawRenderOptions constantOptions = Options(32);
        constantOptions.OutputWidth = 7;
        constantOptions.OutputHeight = 5;
        constantOptions.Resampler = resampler;
        RawRgbImage constant = RawRenderer.CreateOutput(Gray(3, 2, Enumerable.Repeat(0.375F, 6).ToArray()), null, constantOptions);
        RawRenderOptions impulseOptions = Options(32);
        impulseOptions.OutputWidth = 9;
        impulseOptions.OutputHeight = 1;
        impulseOptions.Resampler = resampler;
        RawRgbImage impulse = RawRenderer.CreateOutput(Gray(5, 1, new float[] { 0, 0, 1, 0, 0 }), null, impulseOptions);

        Assert.Multiple(() =>
        {
            Assert.That(constant.FloatingSamples, Has.All.EqualTo(0.375F).Within(0.000001F));
            Assert.That(impulse.FloatingSamples[4], Is.EqualTo(1.0F).Within(0.000001F));
            Assert.That(impulse.FloatingSamples[0], Is.EqualTo(impulse.FloatingSamples[8]).Within(0.000001F));
            Assert.That(impulse.FloatingSamples[1], Is.EqualTo(impulse.FloatingSamples[7]).Within(0.000001F));
            Assert.That(impulse.FloatingSamples[2], Is.EqualTo(impulse.FloatingSamples[6]).Within(0.000001F));
            Assert.That(impulse.FloatingSamples[3], Is.EqualTo(impulse.FloatingSamples[5]).Within(0.000001F));
        });
    }

    [Test]
    public void OutputDepthsCanonicalizeBgraAndQuantizeDeterministically()
    {
        RawRgbImage image = Bgra(new float[] { 0.25F, 0.5F, 1.25F, 0.5F });

        RawRgbImage bytes = RawRenderer.CreateOutput(image, null, Options(8));
        RawRgbImage words = RawRenderer.CreateOutput(image, null, Options(16));
        RawRgbImage floating = RawRenderer.CreateOutput(image, null, Options(32));

        Assert.Multiple(() =>
        {
            Assert.That(bytes.ByteSamples, Is.EqualTo(new byte[] { 255, 128, 64, 128 }));
            Assert.That(words.UShortSamples, Is.EqualTo(new ushort[] { 65535, 32768, 16384, 32768 }));
            Assert.That(floating.FloatingSamples, Is.EqualTo(new float[] { 1.25F, 0.5F, 0.25F, 0.5F }));
            Assert.That(bytes.Channels.Select(channel => channel.Kind), Is.EqualTo(new[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue, RawChannelKind.Alpha }));
            Assert.That(bytes.RowStrideSamples, Is.EqualTo(4));
            Assert.That(bytes.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(words.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(floating.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void GrayOutputSupportsEveryDepthAndOrderedDither()
    {
        RawRgbImage image = Gray(1, 1, new float[] { 0.5F });
        RawRenderOptions byteOptions = Options(8);
        byteOptions.OrderedDithering = true;

        RawRgbImage bytes = RawRenderer.CreateOutput(image, null, byteOptions);
        RawRgbImage words = RawRenderer.CreateOutput(image, null, Options(16));
        RawRgbImage floating = RawRenderer.CreateOutput(image, null, Options(32));

        Assert.Multiple(() =>
        {
            Assert.That(bytes.ByteSamples, Is.EqualTo(new byte[] { 127 }));
            Assert.That(words.UShortSamples, Is.EqualTo(new ushort[] { 32768 }));
            Assert.That(floating.FloatingSamples, Is.EqualTo(new float[] { 0.5F }));
            Assert.That(bytes.ChannelCount, Is.EqualTo(1));
            Assert.That(bytes.Channels.Single().Kind, Is.EqualTo(RawChannelKind.Gray));
        });
    }

    [Test]
    public void InvalidCropRepresentationSamplesLimitsAndCancellationAreRejected()
    {
        RawRgbImage valid = Gray(2, 2, new float[] { 0, 0.25F, 0.5F, 1 });
        RawArea outside = new RawArea(RawAreaKind.UserCrop, 1, 1, 2, 2);
        RawRgbImage unknown = Gray(1, 1, new float[] { 0.5F });
        unknown.Orientation = RawOrientation.Unknown;
        RawRgbImage bytes = new RawRgbImage(1, 1, 1, 1, new byte[] { 1 }, null, null, RawThumbnail.GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
        RawRgbImage wrongGray = Gray(1, 1, new float[] { 0.5F });
        wrongGray.Channels[0] = new RawChannelDescription("Custom", RawChannelKind.Custom);
        RawRgbImage nonFinite = Gray(1, 1, new float[] { float.NaN });
        RawRenderOptions limited = Options(32);
        limited.OutputWidth = 3;
        limited.OutputHeight = 1;
        limited.Limits.MaxWidth = 2;
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawRenderOptions cancelled = Options(32);
        cancelled.CancellationToken = source.Token;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.CreateOutput(valid, outside, Options(32)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CreateOutput(unknown, null, Options(32)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CreateOutput(bytes, null, Options(8)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CreateOutput(wrongGray, null, Options(32)));
            Assert.Throws<RawFormatException>(() => RawRenderer.CreateOutput(nonFinite, null, Options(32)));
            Assert.Throws<RawFormatException>(() => RawRenderer.CreateOutput(valid, null, limited));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.CreateOutput(valid, null, cancelled));
        });
    }

    private static RawRenderOptions Options(int bitDepth)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.OutputBitDepth = bitDepth;
        options.OutputWidth = 0;
        options.OutputHeight = 0;
        options.ApplyOrientation = true;
        return options;
    }

    private static RawRgbImage Gray(int width, int height, float[] samples)
    {
        return new RawRgbImage(width, height, 1, width, null, null, samples, RawThumbnail.GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage Bgra(float[] samples)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        };
        return new RawRgbImage(1, 1, 4, 4, null, null, samples, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, false, true, null);
    }
}
