using raw;

namespace raw.Tests;

public class FastProcessingTests
{
    [TestCase(RawCfaLayout.Rggb)]
    [TestCase(RawCfaLayout.Bggr)]
    [TestCase(RawCfaLayout.Grbg)]
    [TestCase(RawCfaLayout.Gbrg)]
    public void HalfSizePreviewReconstructsEveryBayerLayout(RawCfaLayout layout)
    {
        RawImageInfo info = CfaInfo(4, 4, layout);
        float[] samples = ColorSamples(info, 0.2F, 0.4F, 0.8F);
        RawSensorImage image = Sensor(info, samples, null);

        RawRgbImage result = RawRenderer.CreateHalfSizePreview(image, new RawPreviewOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.ChannelCount, Is.EqualTo(3));
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
            {
                0.2F, 0.4F, 0.8F,
                0.2F, 0.4F, 0.8F,
                0.2F, 0.4F, 0.8F,
                0.2F, 0.4F, 0.8F
            }).Within(0.000001F));
            Assert.That(result.ColorSpace, Is.EqualTo(RawColorSpace.RawCamera));
            Assert.That(result.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(result.OrientationApplied, Is.False);
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void HalfSizePreviewPreservesOddCropPhaseAndRepairsIncompleteBorderBlocks()
    {
        RawImageInfo info = CfaInfo(5, 5, RawCfaLayout.Rggb);
        float[] samples = ColorSamples(info, 0.1F, 0.5F, 0.9F);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 1, 3, 3);
        RawSensorImage image = Sensor(info, samples, crop);

        RawRgbImage result = RawRenderer.CreateHalfSizePreview(image, new RawPreviewOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.FloatingSamples, Is.EqualTo(new float[]
            {
                0.1F, 0.5F, 0.9F,
                0.1F, 0.5F, 0.9F,
                0.1F, 0.5F, 0.9F,
                0.1F, 0.5F, 0.9F
            }).Within(0.000001F));
            Assert.That(image.FloatingSamples, Is.SameAs(samples));
        });
    }

    [Test]
    public void HalfSizePreviewAveragesBothGreenSamples()
    {
        RawImageInfo info = CfaInfo(2, 2, RawCfaLayout.Rggb);
        RawSensorImage image = Sensor(info, new float[] { 0.1F, 0.2F, 0.6F, 0.9F }, null);

        RawRgbImage result = RawRenderer.CreateHalfSizePreview(image, new RawPreviewOptions());

        Assert.That(result.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.4F, 0.9F }).Within(0.000001F));
    }

    [Test]
    public void CfaAnalysisProducesPhaseHistogramsCountsAndRequestedPlanes()
    {
        RawImageInfo info = CfaInfo(4, 4, RawCfaLayout.Rggb);
        float[] values = new float[16];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                values[y * 4 + x] = (y % 2 * 2 + x % 2) * 0.25F;
            }
        }
        RawSensorImage image = Sensor(info, values, null);
        byte[] black = new byte[16];
        byte[] white = new byte[16];
        byte[] repaired = new byte[16];
        black[0] = 1;
        white[3] = 1;
        repaired[5] = 1;
        image.ClippingMask = new RawClippingMask(4, 4, 1, 4, black, white);
        image.DefectMap = new RawDefectMap(4, 4, 1, 4, repaired, Array.Empty<RawDefect>(), false);
        RawAnalysisOptions options = new RawAnalysisOptions
        {
            HistogramBinCount = 4,
            IncludeSamplePlanes = true,
            IncludeClippingPlanes = true,
            IncludeDefectPlane = true,
            IncludeCfaPhasePlane = true
        };

        RawAnalysisResult result = RawRenderer.Analyze(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Statistics.Length, Is.EqualTo(4));
            Assert.That(result.Statistics.Select(item => item.CfaPhase), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(result.Statistics.Select(item => item.CfaColor), Is.EqualTo(new[] { RawCfaColor.Red, RawCfaColor.Green, RawCfaColor.Green, RawCfaColor.Blue }));
            Assert.That(result.Statistics.Select(item => item.SampleCount), Is.All.EqualTo(4));
            Assert.That(result.Statistics.Select(item => item.Mean), Is.EqualTo(new double[] { 0, 0.25, 0.5, 0.75 }));
            Assert.That(result.Statistics[0].Histogram, Is.EqualTo(new long[] { 4, 0, 0, 0 }));
            Assert.That(result.Statistics[1].Histogram, Is.EqualTo(new long[] { 0, 4, 0, 0 }));
            Assert.That(result.Statistics[2].Histogram, Is.EqualTo(new long[] { 0, 0, 4, 0 }));
            Assert.That(result.Statistics[3].Histogram, Is.EqualTo(new long[] { 0, 0, 0, 4 }));
            Assert.That(result.Statistics[0].BlackClippedCount, Is.EqualTo(1));
            Assert.That(result.Statistics[1].WhiteClippedCount, Is.EqualTo(1));
            Assert.That(result.Statistics[3].RepairedCount, Is.EqualTo(1));
            Assert.That(result.DiagnosticPlanes.Length, Is.EqualTo(5));
            Assert.That(result.DiagnosticPlanes.Select(plane => plane.Kind), Is.EqualTo(new[] { RawDiagnosticPlaneKind.NormalizedSample, RawDiagnosticPlaneKind.BlackClipped, RawDiagnosticPlaneKind.WhiteClipped, RawDiagnosticPlaneKind.Repaired, RawDiagnosticPlaneKind.CfaPhase }));
            Assert.That(result.DiagnosticPlanes[4].Samples.Take(8), Is.EqualTo(new float[] { 0, 1, 0, 1, 2, 3, 2, 3 }));
            Assert.That(result.Validate().IsValid(), Is.True);
        });
    }

    [Test]
    public void NonCfaAnalysisUsesLogicalChannelsAndStablePopulationMoments()
    {
        RawImageInfo info = RgbInfo(2, 1);
        RawSensorImage image = new RawSensorImage(info, null, new float[] { 0, 0.25F, 0.5F, 1, 0.75F, 0.5F }, 6, true, null, null, null, null, null, null, null, null);
        RawAnalysisOptions options = new RawAnalysisOptions { HistogramBinCount = 4 };

        RawAnalysisResult result = RawRenderer.Analyze(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Statistics.Length, Is.EqualTo(3));
            Assert.That(result.Statistics.Select(item => item.Channel), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Statistics.Select(item => item.CfaPhase), Is.All.EqualTo(-1));
            Assert.That(result.Statistics.Select(item => item.Mean), Is.All.EqualTo(0.5));
            Assert.That(result.Statistics[0].StandardDeviation, Is.EqualTo(0.5));
            Assert.That(result.Statistics[1].StandardDeviation, Is.EqualTo(0.25));
            Assert.That(result.Statistics[2].StandardDeviation, Is.Zero);
            Assert.That(result.Statistics[0].Histogram, Is.EqualTo(new long[] { 1, 0, 0, 1 }));
        });
    }

    [Test]
    public void AnalysisUsesSelectedAreaAndExcludesMaskedSamples()
    {
        RawImageInfo info = GrayInfo(4, 2);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 0, 3, 2);
        RawSensorImage image = new RawSensorImage(info, null, new float[] { 1, 0, 0.5F, 1, 1, 0.25F, 0.5F, 0.75F }, 4, true, null, crop, new RawArea[] { new RawArea(RawAreaKind.Masked, 3, 0, 1, 1) }, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, crop);

        RawAnalysisResult result = RawRenderer.Analyze(image, new RawAnalysisOptions { HistogramBinCount = 4 });

        Assert.Multiple(() =>
        {
            Assert.That((result.Area ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(1));
            Assert.That(result.Area.Width, Is.EqualTo(3));
            Assert.That(result.Statistics.Single().SampleCount, Is.EqualTo(5));
            Assert.That(result.Statistics.Single().Minimum, Is.Zero);
            Assert.That(result.Statistics.Single().Maximum, Is.EqualTo(0.75));
            Assert.That(result.Statistics.Single().Mean, Is.EqualTo(0.4).Within(0.000001));
            Assert.That(result.Statistics.Single().Histogram, Is.EqualTo(new long[] { 1, 1, 2, 1 }));
        });
    }

    [Test]
    public void UnsupportedPreviewPlanesInvalidBinsAndCancellationAreRejected()
    {
        RawImageInfo grayInfo = GrayInfo(2, 2);
        RawSensorImage gray = new RawSensorImage(grayInfo, null, Enumerable.Repeat(0.5F, 4).ToArray(), 2, true, null, null, null, null, null, null, null, null);
        RawAnalysisOptions clipping = new RawAnalysisOptions { IncludeClippingPlanes = true };
        RawAnalysisOptions defect = new RawAnalysisOptions { IncludeDefectPlane = true };
        RawAnalysisOptions phase = new RawAnalysisOptions { IncludeCfaPhasePlane = true };
        RawAnalysisOptions bins = new RawAnalysisOptions { HistogramBinCount = 1 };
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawAnalysisOptions cancelled = new RawAnalysisOptions { CancellationToken = cancellation.Token };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.CreateHalfSizePreview(gray, new RawPreviewOptions()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Analyze(gray, clipping));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Analyze(gray, defect));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.Analyze(gray, phase));
            Assert.Throws<RawFormatException>(() => RawRenderer.Analyze(gray, bins));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.Analyze(gray, cancelled));
        });
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

    private static RawImageInfo CfaInfo(int width, int height, RawCfaLayout layout)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(layout), new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawImageInfo RgbInfo(int width, int height)
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
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
