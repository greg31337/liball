using raw;

namespace raw.Tests;

public class DefectProcessorTests
{
    [Test]
    public void MetadataAndCallerPixelsUseSameColorNeighborsWithoutMutatingInput()
    {
        RawImageInfo info = CfaInfo(6, 6);
        float[] samples = CfaSamples(info, 0.2F, 0.4F, 0.8F);
        samples[2 * 6 + 2] = 1.0F;
        samples[2 * 6 + 3] = 0.0F;
        RawSensorImage image = Sensor(info, samples);
        RawDefectOptions options = new RawDefectOptions();
        options.MetadataDefects = new RawDefect[] { new RawDefect(RawDefectKind.Pixel, RawDefectSource.Metadata, 2, 2, -1) };
        options.CallerDefects = new RawDefect[] { new RawDefect(RawDefectKind.Pixel, RawDefectSource.Caller, 3, 2, 0) };

        RawSensorImage result = RawRenderer.RepairDefects(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[2 * 6 + 2], Is.EqualTo(0.2F).Within(0.000001F));
            Assert.That(result.FloatingSamples[2 * 6 + 3], Is.EqualTo(0.4F).Within(0.000001F));
            Assert.That(image.FloatingSamples[2 * 6 + 2], Is.EqualTo(1.0F));
            Assert.That(image.FloatingSamples[2 * 6 + 3], Is.Zero);
            Assert.That(result.FloatingSamples, Is.Not.SameAs(image.FloatingSamples));
            Assert.That((result.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).Defects.Select(defect => defect.Source), Is.EqualTo(new[] { RawDefectSource.Metadata, RawDefectSource.Caller }));
            Assert.That(result.DefectMap.RepairedSamples.Count(value => value == 1), Is.EqualTo(2));
            Assert.That(result.DefectMap.GreenEquilibriumApplied, Is.False);
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void DetectionRepairsIsolatedHotAndDeadPixelsButLeavesHighlightTexture()
    {
        RawImageInfo info = GrayInfo(11, 11);
        float[] samples = Enumerable.Repeat(0.5F, 121).ToArray();
        samples[1 * 11 + 1] = 1.0F;
        samples[9 * 11 + 9] = 0.0F;
        for (int y = 4; y <= 6; y++)
        {
            for (int x = 4; x <= 6; x++)
            {
                samples[y * 11 + x] = 0.99F;
            }
        }
        RawSensorImage image = Sensor(info, samples);
        RawDefectOptions options = new RawDefectOptions
        {
            DetectHotPixels = true,
            DetectDeadPixels = true
        };

        RawSensorImage result = RawRenderer.RepairDefects(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[1 * 11 + 1], Is.EqualTo(0.5F));
            Assert.That(result.FloatingSamples[9 * 11 + 9], Is.EqualTo(0.5F));
            Assert.That(result.FloatingSamples[5 * 11 + 5], Is.EqualTo(0.99F));
            Assert.That((result.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).Defects.Count(defect => defect.Kind == RawDefectKind.HotPixel), Is.EqualTo(1));
            Assert.That(result.DefectMap.Defects.Count(defect => defect.Kind == RawDefectKind.DeadPixel), Is.EqualTo(1));
            Assert.That(result.DefectMap.RepairedSamples[5 * 11 + 5], Is.Zero);
        });
    }

    [Test]
    public void DeclaredRowAndColumnDefectsAreCorrectedAcrossTheirIntersection()
    {
        RawImageInfo info = GrayInfo(6, 6);
        float[] samples = Enumerable.Repeat(0.4F, 36).ToArray();
        for (int x = 0; x < 6; x++)
        {
            samples[2 * 6 + x] = 1.0F;
        }
        for (int y = 0; y < 6; y++)
        {
            samples[y * 6 + 4] = 0.0F;
        }
        RawSensorImage image = Sensor(info, samples);
        RawDefectOptions options = new RawDefectOptions();
        options.MetadataDefects = new RawDefect[] { new RawDefect(RawDefectKind.Row, RawDefectSource.Metadata, -1, 2, -1) };
        options.CallerDefects = new RawDefect[] { new RawDefect(RawDefectKind.Column, RawDefectSource.Caller, 4, -1, -1) };

        RawSensorImage result = RawRenderer.RepairDefects(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.All.EqualTo(0.4F).Within(0.000001F));
            Assert.That((result.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepairedSamples.Count(value => value == 1), Is.EqualTo(11));
            Assert.That(image.FloatingSamples[2 * 6], Is.EqualTo(1.0F));
            Assert.That(image.FloatingSamples[4], Is.Zero);
        });
    }

    [Test]
    public void GreenEquilibriumEqualizesSmallCfaPhaseMismatchAndSkipsLargeMismatch()
    {
        RawImageInfo info = CfaInfo(6, 6);
        float[] samples = CfaSamples(info, 0.2F, 0.4F, 0.8F);
        SetSecondGreenPhase(info, samples, 0.44F);
        RawSensorImage image = Sensor(info, samples);
        RawDefectOptions options = new RawDefectOptions
        {
            ApplyGreenEquilibrium = true,
            GreenEquilibriumMaximumRelativeDifference = 0.1
        };

        RawSensorImage result = RawRenderer.RepairDefects(image, options);

        float[] largeSamples = CfaSamples(info, 0.2F, 0.2F, 0.8F);
        SetSecondGreenPhase(info, largeSamples, 0.8F);
        RawSensorImage large = Sensor(info, largeSamples);
        RawSensorImage skipped = RawRenderer.RepairDefects(large, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples[1], Is.EqualTo(0.42F).Within(0.000001F));
            Assert.That(result.FloatingSamples[6], Is.EqualTo(0.42F).Within(0.000001F));
            Assert.That(result.FloatingSamples[0], Is.EqualTo(0.2F));
            Assert.That(result.FloatingSamples[7], Is.EqualTo(0.8F));
            Assert.That(image.FloatingSamples[1], Is.EqualTo(0.4F));
            Assert.That(image.FloatingSamples[6], Is.EqualTo(0.44F));
            Assert.That((result.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).GreenEquilibriumApplied, Is.True);
            Assert.That(skipped.FloatingSamples, Is.EqualTo(largeSamples));
            Assert.That((skipped.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).GreenEquilibriumApplied, Is.False);
        });
    }

    [Test]
    public void InvalidDeclarationsNonCfaEquilibriumAndCancellationAreRejected()
    {
        RawImageInfo info = GrayInfo(3, 3);
        RawSensorImage image = Sensor(info, Enumerable.Repeat(0.5F, 9).ToArray());
        RawDefectOptions wrongSource = new RawDefectOptions();
        wrongSource.MetadataDefects = new RawDefect[] { new RawDefect(RawDefectKind.Pixel, RawDefectSource.Caller, 1, 1, 0) };
        RawDefectOptions green = new RawDefectOptions { ApplyGreenEquilibrium = true };
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawDefectOptions cancelled = new RawDefectOptions { CancellationToken = cancellation.Token };

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawRenderer.RepairDefects(image, wrongSource));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.RepairDefects(image, green));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.RepairDefects(image, cancelled));
        });
    }

    private static RawSensorImage Sensor(RawImageInfo info, float[] samples)
    {
        RawSensorImage image = new RawSensorImage(info, null, samples, info.Width, true, null, null, null, null, null, null, null, null);
        image.CropModel = new RawCropModel(info, null);
        return image;
    }

    private static float[] CfaSamples(RawImageInfo info, float red, float green, float blue)
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

    private static void SetSecondGreenPhase(RawImageInfo info, float[] samples, float value)
    {
        for (int y = 0; y < info.Height; y++)
        {
            for (int x = 0; x < info.Width; x++)
            {
                if (x % 2 == 0 && y % 2 == 1)
                {
                    samples[y * info.Width + x] = value;
                }
            }
        }
    }

    private static RawImageInfo CfaInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), null, null);
    }

    private static RawImageInfo GrayInfo(int width, int height)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, 16, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }
}
