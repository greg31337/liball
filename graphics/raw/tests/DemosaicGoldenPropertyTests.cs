using raw;

namespace raw.Tests;

public class DemosaicGoldenPropertyTests
{
    [TestCaseSource(nameof(AllModesAndLayouts))]
    public void TwoByTwoBordersMatchFlatColorGoldenForEveryModeAndBayerPhase(RawDemosaicMode mode, RawCfaLayout layout)
    {
        RawImageInfo info = CfaInfo(2, 2, RawCfaPattern.CreateBayer(layout));
        RawSensorImage image = Sensor(info, ColorSamples(info, 0.15F, 0.45F, 0.85F), null);

        RawRgbImage first = Reconstruct(image, mode);
        RawRgbImage second = Reconstruct(image, mode);

        Assert.Multiple(() =>
        {
            Assert.That(first.FloatingSamples.Chunk(3), Is.All.EqualTo(new float[] { 0.15F, 0.45F, 0.85F }).Within(0.000001F));
            Assert.That(first.FloatingSamples, Is.EqualTo(second.FloatingSamples));
            Assert.That(first.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCaseSource(nameof(AllModesAndLayouts))]
    public void OddCropInteriorMatchesFullReconstructionForEveryModeAndBayerPhase(RawDemosaicMode mode, RawCfaLayout layout)
    {
        RawImageInfo info = CfaInfo(11, 11, RawCfaPattern.CreateBayer(layout));
        float[] samples = FrequencySamples(info, 2);
        RawArea crop = new RawArea(RawAreaKind.UserCrop, 1, 1, 9, 9);

        RawRgbImage full = Reconstruct(Sensor(info, samples, null), mode);
        RawRgbImage cropped = Reconstruct(Sensor(info, samples, crop), mode);

        Assert.Multiple(() =>
        {
            Assert.That(cropped.Width, Is.EqualTo(9));
            Assert.That(cropped.Height, Is.EqualTo(9));
            int y = 1;
            while (y < cropped.Height - 1)
            {
                int x = 1;
                while (x < cropped.Width - 1)
                {
                    Assert.That(Pixel(cropped, x, y), Is.EqualTo(Pixel(full, crop.X + x, crop.Y + y)).Within(0.000001F));
                    x++;
                }
                y++;
            }
        });
    }

    [TestCaseSource(nameof(EdgeAwareModesLayoutsAndDirections))]
    public void EdgeAwareModesPreserveSaturatedMonochromeEdges(RawDemosaicMode mode, RawCfaLayout layout, bool vertical)
    {
        RawImageInfo info = CfaInfo(10, 10, RawCfaPattern.CreateBayer(layout));
        float[] samples = new float[100];
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                samples[y * 10 + x] = vertical ? x < 5 ? 0.0F : 1.0F : y < 5 ? 0.0F : 1.0F;
            }
        }

        RawRgbImage result = Reconstruct(Sensor(info, samples, null), mode);
        float[] dark = Pixel(result, 4, 4);
        float[] bright = vertical ? Pixel(result, 5, 4) : Pixel(result, 4, 5);

        Assert.Multiple(() =>
        {
            Assert.That(dark, Is.EqualTo(new float[] { 0.0F, 0.0F, 0.0F }).Within(0.000001F));
            Assert.That(bright, Is.EqualTo(new float[] { 1.0F, 1.0F, 1.0F }).Within(0.000001F));
            Assert.That(result.FloatingSamples, Is.All.InRange(0.0F, 1.0F));
        });
    }

    [TestCase(RawDemosaicMode.NearestNeighbor)]
    [TestCase(RawDemosaicMode.Bilinear)]
    [TestCase(RawDemosaicMode.Vng)]
    [TestCase(RawDemosaicMode.Ahd)]
    public void EveryModeRetainsDistinctSampledGreenPhases(RawDemosaicMode mode)
    {
        RawCfaPattern pattern = RawCfaPattern.CreateArbitrary(2, 2, new RawCfaColor[]
        {
            RawCfaColor.Red, RawCfaColor.Green,
            RawCfaColor.Green2, RawCfaColor.Blue
        });
        RawImageInfo info = CfaInfo(8, 8, pattern);
        float[] samples = ColorSamples(info, 0.1F, 0.25F, 0.9F);
        for (int y = 1; y < 8; y += 2)
        {
            for (int x = 0; x < 8; x += 2)
            {
                samples[y * 8 + x] = 0.75F;
            }
        }

        RawRgbImage result = Reconstruct(Sensor(info, samples, null), mode);

        Assert.Multiple(() =>
        {
            Assert.That(Pixel(result, 1, 0)[1], Is.EqualTo(0.25F));
            Assert.That(Pixel(result, 0, 1)[1], Is.EqualTo(0.75F));
            Assert.That(Pixel(result, 5, 6)[1], Is.EqualTo(0.25F));
            Assert.That(Pixel(result, 4, 7)[1], Is.EqualTo(0.75F));
        });
    }

    [TestCaseSource(nameof(AllFrequencyCases))]
    public void SyntheticFrequencyPatternsAreDeterministicBoundedAndPreserveCfaSamples(RawDemosaicMode mode, RawCfaLayout layout, int patternIndex)
    {
        RawImageInfo info = CfaInfo(9, 9, RawCfaPattern.CreateBayer(layout));
        float[] samples = FrequencySamples(info, patternIndex);
        RawSensorImage image = Sensor(info, samples, null);

        RawRgbImage first = Reconstruct(image, mode);
        RawRgbImage second = Reconstruct(image, mode);

        Assert.Multiple(() =>
        {
            Assert.That(first.FloatingSamples, Is.EqualTo(second.FloatingSamples));
            Assert.That(first.FloatingSamples, Is.All.InRange(0.0F, 1.0F));
            Assert.That(first.Validate(new RawLimits()).IsValid(), Is.True);
            int y = 0;
            while (y < info.Height)
            {
                int x = 0;
                while (x < info.Width)
                {
                    RawCfaColor color = (info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(x, y);
                    int channel = color == RawCfaColor.Red ? 0 : color == RawCfaColor.Blue ? 2 : 1;
                    Assert.That(Pixel(first, x, y)[channel], Is.EqualTo(samples[y * info.Width + x]).Within(0.000001F));
                    x++;
                }
                y++;
            }
        });
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData> AllModesAndLayouts()
    {
        RawDemosaicMode[] modes = Modes();
        RawCfaLayout[] layouts = Layouts();
        foreach (RawDemosaicMode mode in modes)
        {
            foreach (RawCfaLayout layout in layouts)
            {
                yield return new TestCaseData(mode, layout);
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData> EdgeAwareModesLayoutsAndDirections()
    {
        RawDemosaicMode[] modes = new RawDemosaicMode[] { RawDemosaicMode.Vng, RawDemosaicMode.Ahd };
        RawCfaLayout[] layouts = Layouts();
        foreach (RawDemosaicMode mode in modes)
        {
            foreach (RawCfaLayout layout in layouts)
            {
                yield return new TestCaseData(mode, layout, false);
                yield return new TestCaseData(mode, layout, true);
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData> AllFrequencyCases()
    {
        foreach (TestCaseData modeAndLayout in AllModesAndLayouts())
        {
            object?[] arguments = modeAndLayout.Arguments;
            int patternIndex = 0;
            while (patternIndex < 3)
            {
                yield return new TestCaseData(arguments[0], arguments[1], patternIndex);
                patternIndex++;
            }
        }
    }

    private static RawDemosaicMode[] Modes()
    {
        return new RawDemosaicMode[] { RawDemosaicMode.NearestNeighbor, RawDemosaicMode.Bilinear, RawDemosaicMode.Vng, RawDemosaicMode.Ahd };
    }

    private static RawCfaLayout[] Layouts()
    {
        return new RawCfaLayout[] { RawCfaLayout.Rggb, RawCfaLayout.Bggr, RawCfaLayout.Grbg, RawCfaLayout.Gbrg };
    }

    private static RawRgbImage Reconstruct(RawSensorImage image, RawDemosaicMode mode)
    {
        return RawRenderer.Reconstruct(image, new RawRenderOptions { DemosaicMode = mode });
    }

    private static float[] Pixel(RawRgbImage image, int x, int y)
    {
        int index = y * image.RowStrideSamples + x * 3;
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

    private static float[] FrequencySamples(RawImageInfo info, int patternIndex)
    {
        float[] samples = new float[info.Width * info.Height];
        for (int y = 0; y < info.Height; y++)
        {
            for (int x = 0; x < info.Width; x++)
            {
                if (patternIndex == 0)
                {
                    samples[y * info.Width + x] = (x + y) % 2 == 0 ? 0.0F : 1.0F;
                }
                else if (patternIndex == 1)
                {
                    samples[y * info.Width + x] = (x / 2 + y / 3) % 2 == 0 ? 0.125F : 0.875F;
                }
                else
                {
                    samples[y * info.Width + x] = ((x * 3 + y * 5 + x * y) % 17) / 16.0F;
                }
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
