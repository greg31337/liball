namespace barcode.Tests;

public class ImageBinarizerTests
{
    [Test]
    public void HistogramReportsDistributionContrastAndDarkPolarity()
    {
        ManagedGrayImage image = CreateGrayImage(
            3,
            3,
            new int[]
            {
                240, 240, 240,
                240, 20, 240,
                240, 240, 240
            });

        ImageHistogramAnalysis analysis = HistogramAnalyzer.Analyze(image);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Histogram[20], Is.EqualTo(1));
            Assert.That(analysis.Histogram[240], Is.EqualTo(8));
            Assert.That(analysis.Minimum, Is.EqualTo(20));
            Assert.That(analysis.Maximum, Is.EqualTo(240));
            Assert.That(analysis.Median, Is.EqualTo(240));
            Assert.That(analysis.GlobalThreshold, Is.EqualTo(130));
            Assert.That(analysis.OtsuThreshold, Is.EqualTo(20));
            Assert.That(analysis.Contrast, Is.EqualTo(220));
            Assert.That(analysis.DarkPixelCount, Is.EqualTo(1));
            Assert.That(analysis.LightPixelCount, Is.EqualTo(8));
            Assert.That(analysis.Mean,
                Is.EqualTo(1940.0 / 9.0).Within(0.0000001));
            Assert.That(analysis.BorderMean, Is.EqualTo(240.0));
            Assert.That(analysis.EstimatedPolarity,
                Is.EqualTo(BarcodePolarity.DarkOnLight));
        });
    }

    [Test]
    public void HistogramDetectsLightMarksOnADarkBorder()
    {
        ManagedGrayImage image = CreateGrayImage(
            3,
            3,
            new int[]
            {
                10, 10, 10,
                10, 230, 10,
                10, 10, 10
            });

        ImageHistogramAnalysis analysis = HistogramAnalyzer.Analyze(image);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.BorderMean, Is.EqualTo(10.0));
            Assert.That(analysis.Mean,
                Is.EqualTo(310.0 / 9.0).Within(0.0000001));
            Assert.That(analysis.EstimatedPolarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
        });
    }

    [Test]
    public void GlobalAndOtsuThresholdsHonorExplicitPolarity()
    {
        ManagedGrayImage image = CreateGrayImage(
            4,
            1,
            new int[] { 0, 20, 200, 255 });

        ModuleMatrix dark = GlobalBinarizer.BinarizeThreshold(
            image,
            100,
            BarcodePolarity.DarkOnLight);
        ModuleMatrix light = GlobalBinarizer.BinarizeThreshold(
            image,
            100,
            BarcodePolarity.LightOnDark);
        ModuleMatrix otsu = OtsuBinarizer.BinarizeOtsu(
            image,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(dark.CopyModules(),
                Is.EqualTo(new bool[] { true, true, false, false }));
            Assert.That(light.CopyModules(),
                Is.EqualTo(new bool[] { false, false, true, true }));
            Assert.That(otsu.CopyModules(),
                Is.EqualTo(new bool[] { true, true, false, false }));
        });
    }

    [Test]
    public void RowBinarizationAdaptsToIndependentIlluminationLevels()
    {
        ManagedGrayImage image = CreateGrayImage(
            4,
            2,
            new int[]
            {
                10, 20, 100, 110,
                140, 150, 230, 240
            });

        RowBinarizationData result = RowBinarizer.BinarizeRows(
            image,
            100,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(result.Thresholds, Is.EqualTo(new int[] { 20, 150 }));
            Assert.That(result.Modules.CopyModules(), Is.EqualTo(new bool[]
            {
                true, true, false, false,
                true, true, false, false
            }));
        });
    }

    [Test]
    public void AdaptiveTilesResolveUnevenIllumination()
    {
        ManagedGrayImage image = CreateTwoTileImage(
            70,
            100,
            200,
            230);

        AdaptiveBinarizationData result =
            AdaptiveBinarizer.BinarizeAdaptive(
                image,
                100,
                8,
                BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(result.TileColumns, Is.EqualTo(2));
            Assert.That(result.TileRows, Is.EqualTo(1));
            Assert.That(result.Thresholds, Is.EqualTo(new int[] { 85, 215 }));
            Assert.That(result.Modules.GetModule(0, 0), Is.True);
            Assert.That(result.Modules.GetModule(1, 0), Is.False);
            Assert.That(result.Modules.GetModule(8, 0), Is.True);
            Assert.That(result.Modules.GetModule(9, 0), Is.False);
        });
    }

    [Test]
    public void HybridUsesGlobalSupportForLowContrastTiles()
    {
        ManagedGrayImage image = CreateTwoTileImage(
            95,
            100,
            20,
            220);

        AdaptiveBinarizationData adaptive =
            AdaptiveBinarizer.BinarizeAdaptive(
                image,
                100,
                8,
                BarcodePolarity.DarkOnLight);
        AdaptiveBinarizationData hybrid =
            AdaptiveBinarizer.BinarizeHybrid(
                image,
                100,
                8,
                BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(adaptive.Thresholds,
                Is.EqualTo(new int[] { 97, 120 }));
            Assert.That(hybrid.Thresholds,
                Is.EqualTo(new int[] { 100, 120 }));
            Assert.That(adaptive.Modules.GetModule(1, 0), Is.False);
            Assert.That(hybrid.Modules.GetModule(1, 0), Is.True);
        });
    }

    [TestCase(BarcodeBinarizationMode.Global)]
    [TestCase(BarcodeBinarizationMode.Otsu)]
    [TestCase(BarcodeBinarizationMode.Row)]
    [TestCase(BarcodeBinarizationMode.Adaptive)]
    [TestCase(BarcodeBinarizationMode.Hybrid)]
    public void PipelineDispatchesEveryExplicitModeWithoutMutatingSamples(
        BarcodeBinarizationMode mode)
    {
        ManagedGrayImage image = CreateGrayImage(
            4,
            2,
            new int[]
            {
                10, 20, 200, 220,
                15, 25, 205, 225
            });
        int[] original = image.CopySamples();

        ImageBinarizationResult result = ImageBinarizer.Binarize(
            image,
            mode,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo(mode));
            Assert.That(result.Polarity,
                Is.EqualTo(BarcodePolarity.DarkOnLight));
            Assert.That(result.Modules.Width, Is.EqualTo(4));
            Assert.That(result.Modules.Height, Is.EqualTo(2));
            Assert.That(image.CopySamples(), Is.EqualTo(original));
        });
    }

    [Test]
    public void AutomaticModeSelectsStrategyAndPolarityDeterministically()
    {
        ManagedGrayImage highContrast = CreateGrayImage(
            3,
            3,
            new int[]
            {
                10, 10, 10,
                10, 230, 10,
                10, 10, 10
            });
        ManagedGrayImage lowContrast = CreateTwoTileImage(
            100,
            110,
            100,
            110);
        ManagedGrayImage moderate = CreateFilledImage(16, 16, 100);
        moderate.WriteSample(8, 8, 160);

        ImageBinarizationResult high = ImageBinarizer.Binarize(
            highContrast,
            BarcodeBinarizationMode.Automatic,
            BarcodePolarity.Automatic);
        ImageBinarizationResult low = ImageBinarizer.Binarize(
            lowContrast,
            BarcodeBinarizationMode.Automatic,
            BarcodePolarity.DarkOnLight);
        ImageBinarizationResult middle = ImageBinarizer.Binarize(
            moderate,
            BarcodeBinarizationMode.Automatic,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(high.Mode, Is.EqualTo(BarcodeBinarizationMode.Otsu));
            Assert.That(high.Polarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(high.Modules.GetModule(1, 1), Is.True);
            Assert.That(low.Mode,
                Is.EqualTo(BarcodeBinarizationMode.Adaptive));
            Assert.That(middle.Mode,
                Is.EqualTo(BarcodeBinarizationMode.Hybrid));
        });
    }

    [Test]
    public void UniformInputProducesAnEmptyForeground()
    {
        ManagedGrayImage image = CreateFilledImage(5, 4, 127);

        ImageBinarizationResult result = ImageBinarizer.Binarize(
            image,
            BarcodeBinarizationMode.Otsu,
            BarcodePolarity.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(result.Analysis.Contrast, Is.Zero);
            Assert.That(result.Modules.CopyModules(),
                Is.All.EqualTo(false));
        });
    }

    [Test]
    public void InvalidImagesModesPolaritiesAndThresholdSettingsAreRejected()
    {
        ManagedGrayImage image = CreateFilledImage(2, 2, 100);
        ManagedGrayImage wrongLength = CreateFilledImage(2, 2, 100);
        wrongLength.Samples = new int[3];
        ManagedGrayImage badSample = CreateFilledImage(1, 1, 100);
        badSample.Samples[0] = 256;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => HistogramAnalyzer.Analyze(null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => HistogramAnalyzer.Analyze(wrongLength),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => HistogramAnalyzer.Analyze(badSample),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => HistogramAnalyzer.CalculateOtsuThreshold(
                    new int[2],
                    1,
                    0L,
                    0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => GlobalBinarizer.BinarizeThreshold(
                    image,
                    -1,
                    BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => GlobalBinarizer.BinarizeThreshold(
                    image,
                    100,
                    BarcodePolarity.Automatic),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => RowBinarizer.BinarizeRows(
                    image,
                    256,
                    BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => AdaptiveBinarizer.BinarizeAdaptive(
                    image,
                    100,
                    0,
                    BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageBinarizer.Binarize(
                    image,
                    (BarcodeBinarizationMode)99,
                    BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageBinarizer.Binarize(
                    image,
                    BarcodeBinarizationMode.Otsu,
                    (BarcodePolarity)99),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static ManagedGrayImage CreateGrayImage(
        int width,
        int height,
        int[] samples)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                image.WriteSample(
                    column,
                    row,
                    samples[(row * width) + column]);
                column = column + 1;
            }

            row = row + 1;
        }

        return image;
    }

    static ManagedGrayImage CreateTwoTileImage(
        int firstLow,
        int firstHigh,
        int secondLow,
        int secondHigh)
    {
        ManagedGrayImage image = new ManagedGrayImage(16, 8);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int value;
                if (column < 8)
                {
                    value = column % 2 == 0 ? firstLow : firstHigh;
                }
                else
                {
                    value = column % 2 == 0 ? secondLow : secondHigh;
                }

                image.WriteSample(column, row, value);
                column = column + 1;
            }

            row = row + 1;
        }

        return image;
    }

    static ManagedGrayImage CreateFilledImage(
        int width,
        int height,
        int value)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                image.WriteSample(column, row, value);
                column = column + 1;
            }

            row = row + 1;
        }

        return image;
    }
}
