using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrBinarizerBehaviorTests
{
    [Test]
    public void ConvertsEveryFormatAndStrideWithoutReadingPadding(
        [Values] QrPixelFormat format, [Values(0, 1, 7)] int padding,
        [Values(false, true)] bool trailingPadding)
    {
        byte[] gray = [0, 1, 127, 128, 254, 255];
        QrImage image = Pack(gray, 3, 2, format, padding, trailingPadding);
        string before = Snapshot(image);
        QrDetectionOptions options = new();
        string optionsBefore = Snapshot(options);
        IQrBinarizer binarizer = new QrBinarizer();
        QrImage result = binarizer.CreateLuminanceImage(image, options);
        AssertLuminance(result, gray, 3, 2);
        Assert.That(result.Pixels, Is.Not.SameAs(image.Pixels));
        Assert.That(Snapshot(image), Is.EqualTo(before));
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
        result.Pixels[0] = 99;
        Assert.That(binarizer.CreateLuminanceImage(image, options).Pixels, Is.EqualTo(gray));
    }

    [Test]
    public void ColorWeightsAndEveryAlphaValueAreExact(
        [Values(QrPixelFormat.Rgb24, QrPixelFormat.Rgba32, QrPixelFormat.Bgra32)] QrPixelFormat format,
        [Values(0, 5)] int padding)
    {
        byte[][] colors = [[255, 0, 0], [0, 255, 0], [0, 0, 255], [0, 0, 0], [255, 255, 255], [127, 127, 127]];
        byte[] opaqueLuminance = [76, 150, 29, 0, 255, 127];
        int channels = format == QrPixelFormat.Rgb24 ? 3 : 4;
        int stride = colors.Length * channels + padding;
        byte[] pixels = Enumerable.Repeat((byte)13, stride * 255 + colors.Length * channels).ToArray();
        byte[] expected = new byte[256 * colors.Length];
        for (int alpha = 0; alpha < 256; alpha++)
            for (int i = 0; i < colors.Length; i++)
            {
                int offset = alpha * stride + i * channels;
                pixels[offset] = colors[i][format == QrPixelFormat.Bgra32 ? 2 : 0];
                pixels[offset + 1] = colors[i][1];
                pixels[offset + 2] = colors[i][format == QrPixelFormat.Bgra32 ? 0 : 2];
                if (channels == 4) pixels[offset + 3] = (byte)alpha;
                decimal opacity = channels == 4 ? alpha / 255m : 1m;
                expected[alpha * colors.Length + i] = (byte)Math.Round(
                    opaqueLuminance[i] * opacity + 255m * (1m - opacity), MidpointRounding.AwayFromZero);
            }
        QrImage image = new() { Width = 6, Height = 256, Stride = stride, PixelFormat = format, Pixels = pixels };
        string before = Snapshot(image);
        AssertLuminance(new QrBinarizer().CreateLuminanceImage(image, new QrDetectionOptions()), expected, 6, 256);
        Assert.That(Snapshot(image), Is.EqualTo(before));
    }

    [Test]
    public void ContrastNormalizationUsesOnlyLogicalPixelsAndRoundsExactly([Values] QrPixelFormat format)
    {
        QrImage image = Pack([100, 101, 102, 103, 104, 105, 106], 7, 1, format, 13, true);
        QrDetectionOptions options = new() { NormalizeContrast = true };
        string before = Snapshot(image);
        AssertLuminance(new QrBinarizer().CreateLuminanceImage(image, options), [0, 43, 85, 128, 170, 213, 255], 7, 1);
        Assert.That(Snapshot(image), Is.EqualTo(before));
        foreach (byte value in new byte[] { 0, 127, 255 })
        {
            byte[] uniform = Enumerable.Repeat(value, 6).ToArray();
            AssertLuminance(new QrBinarizer().CreateLuminanceImage(Pack(uniform, 3, 2, format, 9, false), options), uniform, 3, 2);
        }
    }

    [Test]
    public void EveryThresholdModeAndPolarityPreservesKnownBinaryPattern(
        [Values] QrBinarizationMode mode, [Values] QrPolarity polarity,
        [Values] QrPixelFormat format, [Values(0, 3)] int padding)
    {
        byte[] pixels = [0, 255, 0, 0, 255, 255, 255, 0, 255, 0, 0, 255];
        QrImage image = Pack(pixels, 4, 3, format, padding, false);
        QrDetectionOptions options = new() { BinarizationMode = mode, Polarity = polarity };
        string before = Snapshot(image);
        string optionsBefore = Snapshot(options);
        IQrBinarizer binarizer = new QrBinarizer();
        if (mode == QrBinarizationMode.SuppliedBinary && format != QrPixelFormat.Gray8)
        {
            Assert.That(() => binarizer.BinarizeImage(image, options), Throws.TypeOf<QrInputException>());
        }
        else
        {
            AssertMatrices(binarizer.BinarizeImage(image, options), pixels.Select(p => p == 0).ToArray(), 4, 3, polarity);
        }
        Assert.That(Snapshot(image), Is.EqualTo(before));
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
    }

    [TestCase(20, 220)]
    [TestCase(100, 101)]
    [TestCase(100, 123)]
    [TestCase(100, 124)]
    public void GlobalThresholdSeparatesLowContrastAndUsableContrastBoundaries(byte dark, byte light)
    {
        byte[] pixels = [dark, light, light, dark, dark, light];
        bool[] expected = [true, false, false, true, true, false];
        foreach (bool normalize in new[] { false, true })
        {
            QrDetectionOptions options = new() { BinarizationMode = QrBinarizationMode.GlobalHistogram, NormalizeContrast = normalize };
            AssertMatrices(new QrBinarizer().BinarizeImage(Pack(pixels, 3, 2, QrPixelFormat.Gray8, 2, true), options),
                expected, 3, 2, QrPolarity.Automatic);
        }
    }

    [Test]
    public void GlobalHistogramUsesWithinClassVarianceForMultimodalImages([Values(0, 1, 2)] int distribution)
    {
        byte[] pixels = distribution switch
        {
            0 => Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(),
            1 => Enumerable.Repeat((byte)10, 80).Concat(Enumerable.Repeat((byte)90, 48))
                .Concat(Enumerable.Repeat((byte)190, 112)).Concat(Enumerable.Repeat((byte)240, 16)).ToArray(),
            _ => Enumerable.Range(0, 256).Select(i => (byte)((i * 73 + 19) & 255)).ToArray()
        };
        // Independent exhaustive within-class squared-error reference, using decimal arithmetic.
        decimal best = decimal.MaxValue;
        List<int> thresholds = [];
        for (int threshold = pixels.Min(); threshold < pixels.Max(); threshold++)
        {
            decimal[] low = pixels.Where(p => p <= threshold).Select(p => (decimal)p).ToArray();
            decimal[] high = pixels.Where(p => p > threshold).Select(p => (decimal)p).ToArray();
            decimal lowMean = low.Average();
            decimal highMean = high.Average();
            decimal error = low.Sum(p => (p - lowMean) * (p - lowMean)) + high.Sum(p => (p - highMean) * (p - highMean));
            if (error < best) { best = error; thresholds.Clear(); }
            if (error == best) thresholds.Add(threshold);
        }
        int expectedThreshold = (thresholds[0] + thresholds[^1] + 1) / 2;
        QrDetectionOptions options = new() { BinarizationMode = QrBinarizationMode.GlobalHistogram };
        AssertMatrices(new QrBinarizer().BinarizeImage(Pack(pixels, 32, 8, QrPixelFormat.Gray8, 5, false), options),
            pixels.Select(p => p <= expectedThreshold).ToArray(), 32, 8, QrPolarity.Automatic);
    }

    [Test]
    public void HybridRecoversUnevenLightingAtBlockAndFallbackBoundaries(
        [Values(39, 40, 41, 47, 55)] int width,
        [Values(40, 43)] int height,
        [Values(QrBinarizationMode.Automatic, QrBinarizationMode.HybridLocal)] QrBinarizationMode mode,
        [Values(false, true)] bool inverted)
    {
        bool[] expected = new bool[width * height];
        byte[] pixels = new byte[expected.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool dark = (x / 2 + y / 2) % 2 == 0;
                // Local contrast exceeds the illumination change over each neighborhood,
                // while the full-image dark and light ranges still overlap.
                int illumination = 90 + x * 80 / (width - 1) + y * 8 / (height - 1);
                byte value = (byte)(illumination - (dark ? 60 : 0));
                pixels[y * width + x] = inverted ? (byte)(255 - value) : value;
                expected[y * width + x] = dark;
            }
        QrImage image = Pack(pixels, width, height, QrPixelFormat.Bgra32, 7, false);
        QrPolarity polarity = inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight;
        QrDetectionOptions options = new() { BinarizationMode = mode, Polarity = polarity };
        string before = Snapshot(image);
        IQrBinarizer binarizer = new QrBinarizer();
        QrMatrix[] actual = binarizer.BinarizeImage(image, options);
        if (width < 40)
        {
            options.BinarizationMode = QrBinarizationMode.GlobalHistogram;
            Assert.That(actual[0].Values, Is.EqualTo(binarizer.BinarizeImage(image, options)[0].Values));
        }
        else
        {
            // The requested polarity normalizes the inverted source back to the same pattern.
            AssertMatrices(actual, expected, width, height, QrPolarity.DarkOnLight);
            options.BinarizationMode = QrBinarizationMode.GlobalHistogram;
            Assert.That(binarizer.BinarizeImage(image, options)[0].Values, Is.Not.EqualTo(actual[0].Values));
        }
        Assert.That(Snapshot(image), Is.EqualTo(before));
    }

    [Test]
    public void NormalizationEnablesLocalThresholdingForLowContrastBlocks(
        [Values(QrBinarizationMode.Automatic, QrBinarizationMode.HybridLocal)] QrBinarizationMode mode)
    {
        bool[] expected = Enumerable.Range(0, 41 * 43).Select(i => ((i / 41) + (i % 41)) % 2 == 0).ToArray();
        byte[] pixels = expected.Select(dark => dark ? (byte)100 : (byte)104).ToArray();
        QrImage image = Pack(pixels, 41, 43, QrPixelFormat.Rgba32, 3, true);
        QrDetectionOptions options = new() { BinarizationMode = mode, NormalizeContrast = true };
        AssertMatrices(new QrBinarizer().BinarizeImage(image, options), expected, 41, 43, QrPolarity.Automatic);
    }

    [Test]
    public void SuppliedBinaryNeverNormalizesInvalidPixels([Values(1, 127, 254)] byte invalid)
    {
        QrDetectionOptions options = new() { BinarizationMode = QrBinarizationMode.SuppliedBinary, NormalizeContrast = true };
        QrImage image = Pack([0, invalid, 255, 0], 2, 2, QrPixelFormat.Gray8, 4, true);
        Assert.That(() => new QrBinarizer().BinarizeImage(image, options), Throws.TypeOf<QrInputException>());
    }

    [Test]
    public void OutputMatricesDoNotAliasInputOtherPolaritiesOrLaterCalls([Values] QrBinarizationMode mode)
    {
        IQrBinarizer binarizer = new QrBinarizer();
        QrImage image = Pack([0, 255, 255, 0], 2, 2, QrPixelFormat.Gray8, 3, false);
        QrDetectionOptions options = new() { BinarizationMode = mode };
        QrMatrix[] first = binarizer.BinarizeImage(image, options);
        QrMatrix[] second = binarizer.BinarizeImage(image, options);
        Assert.That(first[0].Values, Is.Not.SameAs(first[1].Values));
        Assert.That(first[0].Values, Is.Not.SameAs(second[0].Values));
        first[0].Values[0] = QrModuleValue.Unknown;
        image.Pixels[0] = 255;
        Assert.That(first[1].Values[0], Is.EqualTo(QrModuleValue.Light));
        Assert.That(second[0].Values[0], Is.EqualTo(QrModuleValue.Dark));
    }

    [Test]
    public void RejectsOneByteShortBuffersAndUndersizedStrides([Values] QrPixelFormat format, [Values(0, 5)] int padding)
    {
        QrImage image = Pack([0, 255, 255, 0, 0, 255], 3, 2, format, padding, false);
        Array.Resize(ref image.Pixels, image.Pixels.Length - 1);
        AssertInvalidImage(image);
        image = Pack([0, 255, 255, 0, 0, 255], 3, 2, format, padding, false);
        image.Stride = 3 * Channels(format) - 1;
        AssertInvalidImage(image);
    }

    [TestCaseSource(nameof(InvalidInputs))]
    public void InvalidInputsAndOptionsFailWithoutMutation(Action<QrImage, QrDetectionOptions> corrupt)
    {
        QrImage image = Pack([0, 255, 255, 0], 2, 2, QrPixelFormat.Gray8, 0, false);
        QrDetectionOptions options = new();
        corrupt(image, options);
        string before = Snapshot(image);
        string optionsBefore = Snapshot(options);
        IQrBinarizer binarizer = new QrBinarizer();
        Assert.That(() => binarizer.CreateLuminanceImage(image, options), Throws.TypeOf<QrInputException>());
        Assert.That(() => binarizer.BinarizeImage(image, options), Throws.TypeOf<QrInputException>());
        Assert.That(Snapshot(image), Is.EqualTo(before));
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
    }

    private static IEnumerable<TestCaseData> InvalidInputs()
    {
        (string name, Action<QrImage, QrDetectionOptions> corrupt)[] cases =
        [
            ("null pixels", (i, _) => i.Pixels = null!),
            ("unknown format", (i, _) => i.PixelFormat = (QrPixelFormat)4),
            ("negative format", (i, _) => i.PixelFormat = (QrPixelFormat)(-1)),
            ("zero width", (i, _) => i.Width = 0),
            ("negative height", (i, _) => i.Height = -1),
            ("zero stride", (i, _) => i.Stride = 0),
            ("negative stride", (i, _) => i.Stride = -1),
            ("invalid mode", (_, o) => o.BinarizationMode = (QrBinarizationMode)4),
            ("negative mode", (_, o) => o.BinarizationMode = (QrBinarizationMode)(-1)),
            ("invalid polarity", (_, o) => o.Polarity = (QrPolarity)3),
            ("negative polarity", (_, o) => o.Polarity = (QrPolarity)(-1)),
            ("invalid maximum width", (_, o) => o.MaximumImageWidth = 0),
            ("invalid maximum height", (_, o) => o.MaximumImageHeight = -1),
            ("invalid maximum pixels", (_, o) => o.MaximumImagePixels = 0)
        ];
        foreach (var item in cases) yield return new TestCaseData(item.corrupt).SetName($"BinarizerRejects_{item.name}");
    }

    [Test]
    public void ConfiguredImageLimitsAcceptTheBoundaryAndRejectOneLess([Values("width", "height", "pixels")] string limit)
    {
        QrImage image = Pack([0, 255, 255, 0, 0, 255], 3, 2, QrPixelFormat.Rgb24, 5, false);
        QrDetectionOptions options = new() { MaximumImageWidth = 3, MaximumImageHeight = 2, MaximumImagePixels = 6 };
        IQrBinarizer binarizer = new QrBinarizer();
        Assert.DoesNotThrow(() => binarizer.CreateLuminanceImage(image, options));
        Assert.DoesNotThrow(() => binarizer.BinarizeImage(image, options));
        if (limit == "width") options.MaximumImageWidth--;
        if (limit == "height") options.MaximumImageHeight--;
        if (limit == "pixels") options.MaximumImagePixels--;
        Assert.That(() => binarizer.CreateLuminanceImage(image, options), Throws.TypeOf<QrResourceLimitException>());
        Assert.That(() => binarizer.BinarizeImage(image, options), Throws.TypeOf<QrResourceLimitException>());
    }

    [Test]
    public void LayoutArithmeticOverflowFailsBeforeReadingPixels([Values(false, true)] bool rowSizeOverflow)
    {
        QrImage image = new()
        {
            Width = rowSizeOverflow ? int.MaxValue : 2, Height = rowSizeOverflow ? 1 : 3,
            Stride = int.MaxValue, PixelFormat = QrPixelFormat.Rgba32, Pixels = []
        };
        QrDetectionOptions options = new() { MaximumImageWidth = int.MaxValue, MaximumImagePixels = int.MaxValue };
        IQrBinarizer binarizer = new QrBinarizer();
        Assert.That(() => binarizer.CreateLuminanceImage(image, options), Throws.TypeOf<QrResourceLimitException>());
        Assert.That(() => binarizer.BinarizeImage(image, options), Throws.TypeOf<QrResourceLimitException>());
    }

    [Test]
    public void MatrixAllocationHonorsFourByteModuleStorage(
        [Values(false, true)] bool exceedsLimit, [Values] QrBinarizationMode mode)
    {
        int pixelCount = 4194304 + (exceedsLimit ? 1 : 0);
        QrImage image = new() { Width = pixelCount, Height = 1, Stride = pixelCount, Pixels = new byte[pixelCount], PixelFormat = QrPixelFormat.Gray8 };
        QrDetectionOptions options = new()
        {
            MaximumImageWidth = pixelCount, BinarizationMode = mode, Polarity = QrPolarity.DarkOnLight
        };
        IQrBinarizer binarizer = new QrBinarizer();
        Assert.That(binarizer.CreateLuminanceImage(image, options).Pixels, Has.Length.EqualTo(pixelCount));
        if (exceedsLimit)
        {
            QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() => binarizer.BinarizeImage(image, options))!;
            Assert.That(error.LimitName, Is.EqualTo("MaximumAllocationBytes"));
            Assert.That(error.RequestedValue, Is.EqualTo(pixelCount * 4));
            Assert.That(error.MaximumValue, Is.EqualTo(16777216));
        }
        else
        {
            QrMatrix[] matrices = binarizer.BinarizeImage(image, options);
            Assert.That(matrices, Has.Length.EqualTo(1));
            Assert.That(matrices[0].Values, Has.Length.EqualTo(pixelCount));
            Assert.That(matrices[0].Values[0], Is.EqualTo(QrModuleValue.Dark));
            Assert.That(matrices[0].Values[^1], Is.EqualTo(QrModuleValue.Dark));
        }
    }

    private static void AssertInvalidImage(QrImage image)
    {
        IQrBinarizer binarizer = new QrBinarizer();
        Assert.That(() => binarizer.CreateLuminanceImage(image, new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
        Assert.That(() => binarizer.BinarizeImage(image, new QrDetectionOptions()), Throws.TypeOf<QrInputException>());
    }

    private static void AssertLuminance(QrImage actual, byte[] expected, int width, int height)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Width, Is.EqualTo(width));
            Assert.That(actual.Height, Is.EqualTo(height));
            Assert.That(actual.Stride, Is.EqualTo(width));
            Assert.That(actual.PixelFormat, Is.EqualTo(QrPixelFormat.Gray8));
            Assert.That(actual.Pixels, Is.EqualTo(expected));
        });
    }

    private static void AssertMatrices(QrMatrix[] actual, bool[] dark, int width, int height, QrPolarity polarity)
    {
        Assert.That(actual, Has.Length.EqualTo(polarity == QrPolarity.Automatic ? 2 : 1));
        for (int index = 0; index < actual.Length; index++)
        {
            bool inverted = polarity == QrPolarity.LightOnDark || index == 1;
            QrModuleValue[] expected = dark.Select(d => d != inverted ? QrModuleValue.Dark : QrModuleValue.Light).ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(actual[index].Width, Is.EqualTo(width));
                Assert.That(actual[index].Height, Is.EqualTo(height));
                Assert.That(actual[index].Values, Is.EqualTo(expected));
                Assert.That(actual[index].Roles, Is.Null);
                Assert.That(actual[index].Confidence, Is.Null);
            });
        }
    }

    private static int Channels(QrPixelFormat format) => format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;

    private static QrImage Pack(byte[] gray, int width, int height, QrPixelFormat format, int padding, bool trailingPadding)
    {
        int channels = Channels(format);
        int stride = width * channels + padding;
        int length = (height - 1) * stride + width * channels + (trailingPadding ? padding + 11 : 0);
        byte[] pixels = Enumerable.Repeat((byte)13, length).ToArray();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int offset = y * stride + x * channels;
                for (int channel = 0; channel < Math.Min(channels, 3); channel++) pixels[offset + channel] = gray[y * width + x];
                if (channels == 4) pixels[offset + 3] = 255;
            }
        return new QrImage { Pixels = pixels, Width = width, Height = height, Stride = stride, PixelFormat = format };
    }

    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
