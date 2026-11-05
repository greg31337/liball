using raw;

namespace raw.Tests;

public class PreprocessingPipelineTests
{
    [Test]
    public void OddCropPhaseSurvivesNormalizationDefectGainPreviewAndDiagnostics()
    {
        RawImageInfo info = CfaInfo(6, 6, 10);
        uint[] storedSamples = new uint[36];
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                RawCfaColor color = (info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(x, y);
                storedSamples[y * 6 + x] = color == RawCfaColor.Red ? 100U : color == RawCfaColor.Blue ? 300U : 200U;
            }
        }
        storedSamples[2 * 6 + 2] = 400;
        RawStoredImage stored = new RawStoredImage(info, storedSamples, null, null, 6, true, null);
        RawNormalizationOptions normalization = new RawNormalizationOptions
        {
            UserCrop = new RawArea(RawAreaKind.UserCrop, 1, 1, 3, 3),
            WhiteLevels = new double[] { 400 }
        };

        RawSensorImage normalized = RawRenderer.Preprocess(stored, normalization);
        RawDefectOptions defects = new RawDefectOptions
        {
            CallerDefects = new RawDefect[] { new RawDefect(RawDefectKind.Pixel, RawDefectSource.Caller, 2, 2, 0) }
        };
        RawSensorImage repaired = RawRenderer.RepairDefects(normalized, defects);
        repaired.GainMaps = new RawGainMap[]
        {
            new RawGainMap(0, 0, 6, 6, 1, 1, 4, new double[] { 2, 1, 1, 1 })
        };
        RawSensorImage corrected = RawRenderer.CorrectLens(repaired, new RawLensCorrectionOptions { ApplyGainMaps = true });
        RawRgbImage preview = RawRenderer.CreateHalfSizePreview(corrected, new RawPreviewOptions());
        RawAnalysisResult analysis = RawRenderer.Analyze(corrected, new RawAnalysisOptions { IncludeCfaPhasePlane = true });

        Assert.Multiple(() =>
        {
            Assert.That(((normalized.CropModel ?? throw new System.InvalidOperationException("A required value is unavailable.")).SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(1));
            Assert.That((normalized.CropModel.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Y, Is.EqualTo(1));
            Assert.That((info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(normalized.CropModel.SelectedArea.X, normalized.CropModel.SelectedArea.Y), Is.EqualTo(RawCfaColor.Blue));
            Assert.That(normalized.FloatingSamples[2 * 6 + 2], Is.EqualTo(1.0F));
            Assert.That(repaired.FloatingSamples[2 * 6 + 2], Is.EqualTo(0.25F).Within(0.000001F));
            Assert.That(corrected.FloatingSamples[2 * 6 + 2], Is.EqualTo(0.5F).Within(0.000001F));
            Assert.That(corrected.FloatingSamples[2], Is.EqualTo(0.5F).Within(0.000001F));
            Assert.That(corrected.FloatingSamples[1 * 6 + 1], Is.EqualTo(0.75F).Within(0.000001F));
            Assert.That(preview.Width, Is.EqualTo(2));
            Assert.That(preview.Height, Is.EqualTo(2));
            Assert.That(preview.FloatingSamples, Is.EqualTo(new float[]
            {
                0.5F, 0.5F, 0.75F,
                0.5F, 0.5F, 0.75F,
                0.5F, 0.5F, 0.75F,
                0.5F, 0.5F, 0.75F
            }).Within(0.000001F));
            Assert.That((analysis.Area ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(1));
            Assert.That(analysis.Area.Y, Is.EqualTo(1));
            Assert.That(analysis.DiagnosticPlanes.Single().Samples, Is.EqualTo(new float[] { 3, 2, 3, 1, 0, 1, 3, 2, 3 }));
            Assert.That(stored.UnsignedSamples[2 * 6 + 2], Is.EqualTo(400));
        });
    }

    [Test]
    public void OpticalBlackLinearizationFeedsClippingAndSelectedStatistics()
    {
        RawImageInfo info = GrayInfo(4, 3, 2, RawSampleKind.UnsignedInteger);
        info.ActiveArea = new RawArea(RawAreaKind.Active, 1, 0, 3, 3);
        RawStoredImage stored = new RawStoredImage(info, new uint[]
        {
            1, 1, 2, 3,
            1, 1, 2, 3,
            1, 1, 2, 3
        }, null, null, 4, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions
        {
            LinearizationTable = new ushort[] { 10, 20, 40, 80 },
            MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 1, 3) },
            UseOpticalBlack = true,
            WhiteLevels = new double[] { 80 }
        };

        RawSensorImage normalized = RawRenderer.Preprocess(stored, options);
        RawAnalysisResult analysis = RawRenderer.Analyze(normalized, new RawAnalysisOptions { HistogramBinCount = 4 });

        Assert.Multiple(() =>
        {
            Assert.That((normalized.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).Values, Is.EqualTo(new double[] { 20 }));
            Assert.That(normalized.FloatingSamples, Is.EqualTo(new float[]
            {
                0, 0, 1.0F / 3.0F, 1,
                0, 0, 1.0F / 3.0F, 1,
                0, 0, 1.0F / 3.0F, 1
            }).Within(0.000001F));
            Assert.That((normalized.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).IsBlackClipped(1, 0, 0), Is.True);
            Assert.That(normalized.ClippingMask.IsWhiteClipped(3, 2, 0), Is.True);
            Assert.That(normalized.ClippingMask.IsBlackClipped(2, 1, 0), Is.False);
            Assert.That((analysis.Area ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(1));
            Assert.That(analysis.Statistics.Single().SampleCount, Is.EqualTo(9));
            Assert.That(analysis.Statistics.Single().BlackClippedCount, Is.EqualTo(3));
            Assert.That(analysis.Statistics.Single().WhiteClippedCount, Is.EqualTo(3));
            Assert.That(analysis.Statistics.Single().Mean, Is.EqualTo(4.0 / 9.0).Within(0.000001));
            Assert.That(analysis.Statistics.Single().Histogram, Is.EqualTo(new long[] { 3, 3, 0, 3 }));
        });
    }

    [Test]
    public void DisabledDefectAndLensStagesPreserveSamplesDespiteAvailableMetadata()
    {
        RawImageInfo info = CfaInfo(4, 4, 16);
        float[] samples = Enumerable.Range(0, 16).Select(value => value / 15.0F).ToArray();
        RawSensorImage image = Sensor(info, samples);
        image.ClippingMask = new RawClippingMask(4, 4, 1, 4, new byte[16], new byte[16]);
        RawDefectOptions defectOptions = new RawDefectOptions
        {
            MetadataDefects = new RawDefect[] { new RawDefect(RawDefectKind.Row, RawDefectSource.Metadata, -1, 1, 0) },
            CallerDefects = new RawDefect[] { new RawDefect(RawDefectKind.Column, RawDefectSource.Caller, 2, -1, 0) },
            RepairRows = false,
            RepairColumns = false
        };

        RawSensorImage defectDisabled = RawRenderer.RepairDefects(image, defectOptions);
        defectDisabled.GainMaps = new RawGainMap[] { new RawGainMap(0, 0, 4, 4, 1, 1, 1, new double[] { 3 }) };
        defectDisabled.ChromaticAberration = new RawChromaticAberration(1.5, 1.5, 0.9, 1.1);
        RawSensorImage lensDisabled = RawRenderer.CorrectLens(defectDisabled, new RawLensCorrectionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(defectDisabled.FloatingSamples, Is.EqualTo(samples));
            Assert.That((defectDisabled.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).Defects, Is.Empty);
            Assert.That(defectDisabled.DefectMap.RepairedSamples, Is.All.Zero);
            Assert.That(lensDisabled.FloatingSamples, Is.EqualTo(samples));
            Assert.That(lensDisabled.GainMapsApplied, Is.False);
            Assert.That(lensDisabled.ChromaticAberrationApplied, Is.False);
            Assert.That((lensDisabled.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepairedSamples, Is.All.Zero);
            Assert.That((lensDisabled.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.All.Zero);
            Assert.That(image.FloatingSamples, Is.SameAs(samples));
        });
    }

    [Test]
    public void DisabledNormalizationTransformsPreserveFloatingSensorValues()
    {
        RawImageInfo info = GrayInfo(4, 1, 32, RawSampleKind.Single);
        float[] values = new float[] { 0.1F, 0.25F, 0.5F, 0.9F };
        RawStoredImage stored = new RawStoredImage(info, null, null, values, 4, true, null);
        RawNormalizationOptions options = new RawNormalizationOptions
        {
            ApplyLinearization = false,
            SubtractBlackLevel = false,
            ScaleWhiteLevel = false
        };

        RawSensorImage result = RawRenderer.Preprocess(stored, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.EqualTo(values));
            Assert.That(result.FloatingSamples, Is.Not.SameAs(values));
            Assert.That(result.StoredImage, Is.SameAs(stored));
            Assert.That((result.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped, Is.All.Zero);
            Assert.That(result.ClippingMask.WhiteClipped, Is.All.Zero);
        });
    }

    private static RawSensorImage Sensor(RawImageInfo info, float[] samples)
    {
        RawSensorImage image = new RawSensorImage(info, null, samples, info.Width, true, null, null, null, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, null);
        return image;
    }

    private static RawImageInfo CfaInfo(int width, int height, int bitDepth)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, bitDepth, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height, int bitDepth, RawSampleKind sampleKind)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, bitDepth, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, sampleKind, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
