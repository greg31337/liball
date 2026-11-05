using raw;

namespace raw.Tests;

public class HistogramTests
{
    [Test]
    public void StoredThirtyTwoBitHistogramUsesConfiguredBoundedBinsAndNativeDomain()
    {
        RawImageInfo info = StoredInfo(4, 1, 32, RawSampleKind.UnsignedInteger, GrayChannels());
        RawStoredImage image = new RawStoredImage(info, new uint[] { 0, 1, 2147483648, uint.MaxValue }, null, null, 4, true, null);
        RawHistogramOptions options = new RawHistogramOptions
        {
            HistogramBinCount = 8,
            Percentiles = new double[] { 0.0, 0.5, 1.0 }
        };

        RawHistogramResult result = RawRenderer.Histogram(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.HistogramBinCount, Is.EqualTo(8));
            Assert.That(result.Channels, Has.Length.EqualTo(1));
            Assert.That(result.Channels[0].Histogram, Is.EqualTo(new long[] { 2, 0, 0, 0, 1, 0, 0, 1 }));
            Assert.That(result.Channels[0].HistogramMinimum, Is.Zero);
            Assert.That(result.Channels[0].HistogramMaximum, Is.EqualTo(uint.MaxValue));
            Assert.That(result.Channels[0].PercentileValues, Is.EqualTo(new double[] { 0, 1, uint.MaxValue }));
            Assert.That(result.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void SignedStoredHistogramUsesDeclaredPrecisionDomain()
    {
        RawImageInfo info = StoredInfo(5, 1, 12, RawSampleKind.SignedInteger, GrayChannels());
        RawStoredImage image = new RawStoredImage(info, null, new int[] { -2048, -1024, 0, 1024, 2047 }, null, 5, true, null);
        RawHistogramOptions options = new RawHistogramOptions
        {
            HistogramBinCount = 4,
            Percentiles = new double[] { 0.25, 0.5, 0.75 }
        };

        RawHistogramChannel channel = RawRenderer.Histogram(image, options).Channels[0];

        Assert.Multiple(() =>
        {
            Assert.That(channel.HistogramMinimum, Is.EqualTo(-2048));
            Assert.That(channel.HistogramMaximum, Is.EqualTo(2047));
            Assert.That(channel.Histogram, Is.EqualTo(new long[] { 1, 1, 1, 2 }));
            Assert.That(channel.PercentileValues, Is.EqualTo(new double[] { -1024, 0, 1024 }));
        });
    }

    [Test]
    public void RenderedHistogramHonorsRegionChannelsAndPaddedStride()
    {
        RawChannelDescription[] channels = RgbChannels();
        byte[] samples = new byte[]
        {
            0, 10, 20, 30, 40, 50, 60, 70, 80, 200, 201, 202,
            90, 100, 110, 120, 130, 140, 150, 160, 170, 203, 204, 205
        };
        RawRgbImage image = new RawRgbImage(3, 2, 3, 12, samples, null, null, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, true, true, null);
        RawHistogramOptions options = new RawHistogramOptions
        {
            Area = new RawArea(RawAreaKind.UserCrop, 1, 0, 2, 2),
            HistogramBinCount = 4,
            Percentiles = new double[] { 0.0, 0.5, 1.0 }
        };

        RawHistogramResult result = RawRenderer.Histogram(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Area.X, Is.EqualTo(1));
            Assert.That(result.Area.Width, Is.EqualTo(2));
            Assert.That(result.Channels.Select(channel => channel.Name), Is.EqualTo(new[] { "R", "G", "B" }));
            Assert.That(result.Channels.Select(channel => channel.SampleCount), Is.All.EqualTo(4));
            Assert.That(result.Channels[0].Histogram, Is.EqualTo(new long[] { 2, 1, 1, 0 }));
            Assert.That(result.Channels[0].PercentileValues, Is.EqualTo(new double[] { 30, 60, 150 }));
            Assert.That(result.Channels[1].PercentileValues, Is.EqualTo(new double[] { 40, 70, 160 }));
            Assert.That(result.Channels[2].PercentileValues, Is.EqualTo(new double[] { 50, 80, 170 }));
            Assert.That(result.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void FloatingRenderHistogramUsesObservedPerChannelDomainsAndConstants()
    {
        RawRgbImage image = new RawRgbImage(2, 1, 3, 6, null, null, new float[] { -2, 5, 1, 2, 5, 9 }, RgbChannels(), RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
        RawHistogramOptions options = new RawHistogramOptions
        {
            HistogramBinCount = 4,
            Percentiles = new double[] { 0.0, 0.5, 1.0 }
        };

        RawHistogramResult result = RawRenderer.Histogram(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Channels[0].Histogram, Is.EqualTo(new long[] { 1, 0, 0, 1 }));
            Assert.That(result.Channels[0].HistogramMinimum, Is.EqualTo(-2));
            Assert.That(result.Channels[0].HistogramMaximum, Is.EqualTo(2));
            Assert.That(result.Channels[1].Histogram, Is.EqualTo(new long[] { 2, 0, 0, 0 }));
            Assert.That(result.Channels[1].HistogramMinimum, Is.EqualTo(5));
            Assert.That(result.Channels[1].HistogramMaximum, Is.EqualTo(5));
            Assert.That(result.Channels[1].PercentileValues, Is.All.EqualTo(5));
            Assert.That(result.Channels[2].PercentileValues, Is.EqualTo(new double[] { 1, 1, 9 }));
            Assert.That(result.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void SensorHistogramUsesResolvedCropAndExcludesMaskedPixels()
    {
        RawImageInfo info = StoredInfo(4, 2, 16, RawSampleKind.UnsignedInteger, GrayChannels());
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 0, 3, 2);
        RawArea masked = new RawArea(RawAreaKind.Masked, 3, 0, 1, 1);
        RawSensorImage image = new RawSensorImage(info, null, new float[] { 1, 0, 0.5F, 1, 1, 0.25F, 0.5F, 0.75F }, 4, true, null, crop, new RawArea[] { masked }, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, crop);
        RawHistogramOptions options = new RawHistogramOptions
        {
            HistogramBinCount = 4,
            Percentiles = new double[] { 0.0, 0.5, 1.0 }
        };

        RawHistogramResult result = RawRenderer.Histogram(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Area.X, Is.EqualTo(1));
            Assert.That(result.Channels[0].SampleCount, Is.EqualTo(5));
            Assert.That(result.Channels[0].Histogram, Is.EqualTo(new long[] { 1, 1, 2, 1 }));
            Assert.That(result.Channels[0].PercentileValues, Is.EqualTo(new double[] { 0, 0.5, 0.75 }));
            Assert.That(result.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void HistogramRejectsInvalidSamplesRegionsLimitsAndCancellation()
    {
        RawRgbImage floating = new RawRgbImage(1, 1, 1, 1, null, null, new float[] { float.NaN }, GrayChannels(), RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
        RawRgbImage bytes = new RawRgbImage(1, 1, 1, 1, new byte[] { 1 }, null, null, GrayChannels(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, true, true, null);
        RawHistogramOptions badArea = new RawHistogramOptions { Area = new RawArea(RawAreaKind.UserCrop, 1, 0, 1, 1) };
        RawHistogramOptions limited = new RawHistogramOptions { HistogramBinCount = 16 };
        limited.Limits.MaxAllocationBytes = 64;
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        RawHistogramOptions cancelled = new RawHistogramOptions { CancellationToken = source.Token };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.Histogram(floating, new RawHistogramOptions()));
            Assert.Throws<RawFormatException>(() => RawRenderer.Histogram(bytes, badArea));
            Assert.Throws<RawLimitException>(() => RawRenderer.Histogram(bytes, limited));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.Histogram(bytes, cancelled));
        });
    }

    private static RawImageInfo StoredInfo(int width, int height, int bitDepth, RawSampleKind sampleKind, RawChannelDescription[] channels)
    {
        RawPlaneInfo[] planes = new RawPlaneInfo[channels.Length];
        for (int index = 0; index < channels.Length; index++)
        {
            planes[index] = new RawPlaneInfo(index, channels[index].Name, channels[index].Kind, width, height, bitDepth, width);
        }
        RawPhotometricInterpretation photometric = channels.Length == 1 ? RawPhotometricInterpretation.BlackIsZero : RawPhotometricInterpretation.Rgb;
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, sampleKind, photometric, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawChannelDescription[] GrayChannels()
    {
        return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
    }

    private static RawChannelDescription[] RgbChannels()
    {
        return new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
    }
}
