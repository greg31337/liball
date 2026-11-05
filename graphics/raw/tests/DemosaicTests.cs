using raw;

namespace raw.Tests;

public class DemosaicTests
{
    [Test]
    public void MonochromeLinearRgbAndMosaicPassThroughWithoutDemosaic()
    {
        RawImageInfo grayInfo = GrayInfo(4, 2);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 0, 2, 2);
        RawSensorImage gray = new RawSensorImage(grayInfo, new ushort[] { 0, 65535, 32768, 16384, 65535, 0, 16384, 32768 }, null, 4, true, null, crop, null, null, null, null, null, null);
        gray.CropModel = new RawCropModel(grayInfo, crop);
        RawRenderOptions grayOptions = new RawRenderOptions { DemosaicMode = RawDemosaicMode.Bilinear };

        RawRgbImage grayResult = RawRenderer.Reconstruct(gray, grayOptions);

        RawImageInfo linearInfo = LinearRgbInfo(2, 1);
        float[] linearSamples = new float[] { 0.1F, 0.2F, 0.3F, 0.7F, 0.8F, 0.9F };
        RawSensorImage linear = Sensor(linearInfo, linearSamples, null);
        RawRgbImage linearResult = RawRenderer.Reconstruct(linear, new RawRenderOptions { DemosaicMode = RawDemosaicMode.None });

        RawImageInfo mosaicInfo = CfaInfo(3, 2, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        float[] mosaicSamples = new float[] { 0, 0.1F, 0.2F, 0.3F, 0.4F, 0.5F };
        RawSensorImage mosaic = Sensor(mosaicInfo, mosaicSamples, null);
        RawRgbImage mosaicResult = RawRenderer.Reconstruct(mosaic, new RawRenderOptions { DemosaicMode = RawDemosaicMode.None });

        Assert.Multiple(() =>
        {
            Assert.That(grayResult.Width, Is.EqualTo(2));
            Assert.That(grayResult.Height, Is.EqualTo(2));
            Assert.That(grayResult.ChannelCount, Is.EqualTo(1));
            Assert.That(grayResult.FloatingSamples, Is.EqualTo(new float[] { 1, 32768.0F / 65535.0F, 0, 16384.0F / 65535.0F }).Within(0.000001F));
            Assert.That(linearResult.FloatingSamples, Is.EqualTo(linearSamples));
            Assert.That(linearResult.FloatingSamples, Is.Not.SameAs(linearSamples));
            Assert.That(linearResult.ChannelCount, Is.EqualTo(3));
            Assert.That(mosaicResult.FloatingSamples, Is.EqualTo(mosaicSamples));
            Assert.That(mosaicResult.ChannelCount, Is.EqualTo(1));
            Assert.That(mosaicResult.ColorSpace, Is.EqualTo(RawColorSpace.RawCamera));
            Assert.That(mosaicResult.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(grayResult.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(linearResult.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(mosaicResult.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(RawCfaLayout.Rggb)]
    [TestCase(RawCfaLayout.Bggr)]
    [TestCase(RawCfaLayout.Grbg)]
    [TestCase(RawCfaLayout.Gbrg)]
    public void NearestNeighborUsesStoredBayerPhaseAcrossOddCrop(RawCfaLayout layout)
    {
        RawImageInfo info = CfaInfo(5, 5, RawCfaPattern.CreateBayer(layout));
        float[] samples = ColorSamples(info, 0.2F, 0.5F, 0.8F);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 1, 3, 3);
        RawSensorImage image = Sensor(info, samples, crop);
        RawRenderOptions options = new RawRenderOptions { DemosaicMode = RawDemosaicMode.NearestNeighbor };

        RawRgbImage result = RawRenderer.Reconstruct(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(3));
            Assert.That(result.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.2F, 0.5F, 0.8F }));
            Assert.That(result.OrientationApplied, Is.False);
            Assert.That(image.FloatingSamples, Is.SameAs(samples));
        });
    }

    [Test]
    public void BilinearBayerReconstructionMatchesInteriorAndBorderGoldenValues()
    {
        RawImageInfo info = CfaInfo(3, 3, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        RawSensorImage image = Sensor(info, new float[]
        {
            1.0F, 0.1F, 0.8F,
            0.3F, 0.2F, 0.5F,
            0.6F, 0.7F, 0.4F
        }, null);
        RawRenderOptions options = new RawRenderOptions { DemosaicMode = RawDemosaicMode.Bilinear };

        RawRgbImage result = RawRenderer.Reconstruct(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(result, 0, 0), Is.EqualTo(new float[] { 1.0F, 0.2F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(result, 1, 0), Is.EqualTo(new float[] { 0.9F, 0.1F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(result, 1, 1), Is.EqualTo(new float[] { 0.7F, 0.4F, 0.2F }).Within(0.000001F));
            Assert.That(Pixel(result, 2, 2), Is.EqualTo(new float[] { 0.4F, 0.6F, 0.2F }).Within(0.000001F));
            Assert.That(result.FloatingSamples[0], Is.EqualTo(1.0F));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void BilinearSupportsArbitraryTwoByTwoBayerPhaseAndSeparateGreen()
    {
        RawCfaPattern pattern = RawCfaPattern.CreateArbitrary(2, 2, new RawCfaColor[]
        {
            RawCfaColor.Green2, RawCfaColor.Blue,
            RawCfaColor.Red, RawCfaColor.Green
        });
        RawImageInfo info = CfaInfo(4, 4, pattern);
        RawSensorImage image = Sensor(info, ColorSamples(info, 0.15F, 0.45F, 0.85F), null);

        RawRgbImage result = RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = RawDemosaicMode.Bilinear });

        Assert.That(result.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.15F, 0.45F, 0.85F }).Within(0.000001F));
    }

    [TestCase(RawDemosaicMode.NearestNeighbor)]
    [TestCase(RawDemosaicMode.Bilinear)]
    [TestCase(RawDemosaicMode.Vng)]
    [TestCase(RawDemosaicMode.Ahd)]
    public void BayerEnumDispatchRoutesEveryImplementedMode(RawDemosaicMode mode)
    {
        RawImageInfo info = CfaInfo(5, 5, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        RawSensorImage image = Sensor(info, ColorSamples(info, 0.2F, 0.5F, 0.8F), null);
        RawRenderOptions options = new RawRenderOptions { DemosaicMode = mode };

        RawRgbImage dispatched = RawDemosaicer.ReconstructBayer(image, options);
        RawRgbImage expected;
        switch (mode)
        {
            case RawDemosaicMode.NearestNeighbor:
            case RawDemosaicMode.Bilinear:
                expected = RawDemosaicer.ReconstructSimpleBayer(image, options);
                break;
            case RawDemosaicMode.Vng:
                expected = RawVngDemosaicer.Reconstruct(image, options);
                break;
            default:
                expected = RawAhdDemosaicer.Reconstruct(image, options);
                break;
        }

        Assert.Multiple(() =>
        {
            Assert.That(dispatched.FloatingSamples, Is.EqualTo(expected.FloatingSamples));
            Assert.That(dispatched.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.2F, 0.5F, 0.8F }).Within(0.000001F));
        });
    }

    [Test]
    public void BayerEnumDispatchRejectsAnUndefinedStage()
    {
        RawImageInfo info = CfaInfo(2, 2, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        RawSensorImage image = Sensor(info, new float[] { 0.2F, 0.5F, 0.5F, 0.8F }, null);
        RawRenderOptions options = new RawRenderOptions { DemosaicMode = (RawDemosaicMode)999 };

        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => RawDemosaicer.ReconstructBayer(image, options));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedFormat));
    }

    [Test]
    public void UnsupportedCfaModesInvalidSamplesAndCancellationAreRejected()
    {
        RawImageInfo bayerInfo = CfaInfo(3, 3, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb));
        RawSensorImage bayer = Sensor(bayerInfo, Enumerable.Repeat(0.5F, 9).ToArray(), null);
        RawCfaPattern nonBayerPattern = RawCfaPattern.CreateArbitrary(3, 1, new RawCfaColor[] { RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Blue });
        RawImageInfo nonBayerInfo = CfaInfo(3, 3, nonBayerPattern);
        RawSensorImage nonBayer = Sensor(nonBayerInfo, Enumerable.Repeat(0.5F, 9).ToArray(), null);
        RawSensorImage invalid = Sensor(bayerInfo, Enumerable.Repeat(0.5F, 9).ToArray(), null);
        invalid.FloatingSamples[4] = 1.1F;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawRenderOptions cancelled = new RawRenderOptions { CancellationToken = cancellation.Token };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Reconstruct(nonBayer, new RawRenderOptions()));
            Assert.Throws<RawFormatException>(() => RawRenderer.Reconstruct(invalid, new RawRenderOptions()));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.Reconstruct(bayer, cancelled));
        });
    }

    private static float[] Pixel(RawRgbImage image, int x, int y)
    {
        int index = y * image.RowStrideSamples + x * image.ChannelCount;
        return new float[] { image.FloatingSamples[index], image.FloatingSamples[index + 1], image.FloatingSamples[index + 2] };
    }

    private static RawSensorImage Sensor(RawImageInfo info, float[] samples, RawArea? crop)
    {
        RawSensorImage image = new RawSensorImage(info, null, samples, info.Width * info.GetChannelCount(), true, null, crop, null, null, null, null, null, null);
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

    private static RawImageInfo GrayInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawImageInfo LinearRgbInfo(int width, int height)
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
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.LinearRaw, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
