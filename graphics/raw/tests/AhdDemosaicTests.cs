using raw;

namespace raw.Tests;

public class AhdDemosaicTests
{
    [TestCase(RawCfaLayout.Rggb)]
    [TestCase(RawCfaLayout.Bggr)]
    [TestCase(RawCfaLayout.Grbg)]
    [TestCase(RawCfaLayout.Gbrg)]
    public void AhdPreservesFlatColorAcrossEveryBayerPhaseAndOddCrop(RawCfaLayout layout)
    {
        RawImageInfo info = CfaInfo(7, 7, RawCfaPattern.CreateBayer(layout));
        float[] samples = ColorSamples(info, 0.2F, 0.5F, 0.8F);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 1, 5, 5);
        RawSensorImage image = Sensor(info, samples, crop);

        RawRgbImage result = RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd });

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(5));
            Assert.That(result.Height, Is.EqualTo(5));
            Assert.That(result.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.2F, 0.5F, 0.8F }).Within(0.000001F));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(image.FloatingSamples, Is.SameAs(samples));
        });
    }

    [Test]
    public void AhdFollowsVerticalAndHorizontalMonochromeEdges()
    {
        RawImageInfo info = CfaInfo(8, 8, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        float[] verticalSamples = new float[64];
        float[] horizontalSamples = new float[64];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                verticalSamples[y * 8 + x] = x < 4 ? 0.1F : 0.9F;
                horizontalSamples[y * 8 + x] = y < 4 ? 0.1F : 0.9F;
            }
        }

        RawRgbImage vertical = RawRenderer.Reconstruct(Sensor(info, verticalSamples, null), new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd });
        RawRgbImage horizontal = RawRenderer.Reconstruct(Sensor(info, horizontalSamples, null), new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd });

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(vertical, 4, 2), Is.EqualTo(new float[] { 0.9F, 0.9F, 0.9F }).Within(0.000001F));
            Assert.That(Pixel(vertical, 3, 2), Is.EqualTo(new float[] { 0.1F, 0.1F, 0.1F }).Within(0.000001F));
            Assert.That(Pixel(horizontal, 2, 4), Is.EqualTo(new float[] { 0.9F, 0.9F, 0.9F }).Within(0.000001F));
            Assert.That(Pixel(horizontal, 2, 3), Is.EqualTo(new float[] { 0.1F, 0.1F, 0.1F }).Within(0.000001F));
        });
    }

    [Test]
    public void AhdSuppressesInterpolatedFalseColorAndPreservesKnownCfaSamples()
    {
        RawImageInfo info = CfaInfo(7, 7, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        float[] samples = Enumerable.Repeat(0.5F, 49).ToArray();
        samples[2 * 7 + 2] = 1.0F;
        RawSensorImage image = Sensor(info, samples, null);

        RawRgbImage bilinear = RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = RawDemosaicMode.Bilinear });
        RawRgbImage ahd = RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd });
        float bilinearNeighborRed = bilinear.FloatingSamples[PixelIndex(bilinear, 3, 2)];
        float ahdNeighborRed = ahd.FloatingSamples[PixelIndex(ahd, 3, 2)];

        Assert.Multiple(() =>
        {
            Assert.That(ahd.FloatingSamples[PixelIndex(ahd, 2, 2)], Is.EqualTo(1.0F));
            Assert.That(ahdNeighborRed, Is.LessThan(bilinearNeighborRed));
            Assert.That(ahdNeighborRed, Is.GreaterThanOrEqualTo(0.5F));
            Assert.That(ahd.FloatingSamples[PixelIndex(ahd, 3, 2) + 1], Is.EqualTo(0.5F));
            Assert.That(ahd.FloatingSamples[PixelIndex(ahd, 3, 2) + 2], Is.EqualTo(0.5F).Within(0.000001F));
        });
    }

    [Test]
    public void AhdRetainsKnownSeparateGreenPhases()
    {
        RawCfaPattern pattern = RawCfaPattern.CreateArbitrary(2, 2, new RawCfaColor[]
        {
            RawCfaColor.Red, RawCfaColor.Green,
            RawCfaColor.Green2, RawCfaColor.Blue
        });
        RawImageInfo info = CfaInfo(6, 6, pattern);
        float[] samples = new float[36];
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                RawCfaColor color = pattern.GetColor(x, y);
                samples[y * 6 + x] = color == RawCfaColor.Red ? 0.2F : color == RawCfaColor.Blue ? 0.8F : color == RawCfaColor.Green2 ? 0.65F : 0.35F;
            }
        }

        RawRgbImage result = RawRenderer.Reconstruct(Sensor(info, samples, null), new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd });

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[PixelIndex(result, 1, 0) + 1], Is.EqualTo(0.35F));
            Assert.That(result.FloatingSamples[PixelIndex(result, 0, 1) + 1], Is.EqualTo(0.65F));
            Assert.That(result.FloatingSamples[PixelIndex(result, 2, 1) + 1], Is.EqualTo(0.65F));
            Assert.That(result.FloatingSamples, Is.All.InRange(0.0F, 1.0F));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void AhdProducesDeterministicTwoByTwoBordersAndObservesCancellation()
    {
        RawImageInfo info = CfaInfo(2, 2, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        RawSensorImage image = Sensor(info, new float[] { 0.2F, 0.5F, 0.5F, 0.8F }, null);
        RawRenderOptions options = new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd };

        RawRgbImage first = RawRenderer.Reconstruct(image, options);
        RawRgbImage second = RawRenderer.Reconstruct(image, options);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(first.FloatingSamples, Is.EqualTo(second.FloatingSamples));
            Assert.That(first.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.2F, 0.5F, 0.8F }).Within(0.000001F));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = RawDemosaicMode.Ahd, CancellationToken = cancellation.Token }));
        });
    }

    private static int PixelIndex(RawRgbImage image, int x, int y)
    {
        return y * image.RowStrideSamples + x * 3;
    }

    private static float[] Pixel(RawRgbImage image, int x, int y)
    {
        int index = PixelIndex(image, x, y);
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }

    private static RawSensorImage Sensor(RawImageInfo info, float[] samples, RawArea? crop)
    {
        RawSensorImage image = new RawSensorImage(info, null, samples, info.Width, true, null, crop, null, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, crop);
        return image;
    }

    private static float[] ColorSamples(RawImageInfo info, float red, float green, float blue)
    {
        float[] samples = new float[info.Width * info.Height];
        for (int y = 0; y < info.Height; y++)
        {
            for (int x = 0; x < info.Width; x++)
            {
                RawCfaColor color = (info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(x, y);
                samples[y * info.Width + x] = color == RawCfaColor.Red ? red : color == RawCfaColor.Blue ? blue : green;
            }
        }
        return samples;
    }

    private static RawImageInfo CfaInfo(int width, int height, RawCfaPattern pattern)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, pattern, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), null, null);
    }
}
