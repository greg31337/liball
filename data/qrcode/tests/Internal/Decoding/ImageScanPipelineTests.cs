using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ImageScanPipelineTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void DecodesProfilesAndPolaritiesWithFreshSamples(QrSymbolKind kind, bool inverted)
    {
        QrImage image = Render(Encode("12345", kind), inverted);
        byte[] original = (byte[])image.Pixels.Clone();
        QrDecodeSet result = Scan(image);
        Assert.That(result.Results, Has.Length.EqualTo(1));
        QrDecodeResult decoded = result.Results[0];
        Assert.That(decoded.Text, Is.EqualTo("12345"));
        Assert.That(decoded.Symbol!.SymbolKind, Is.EqualTo(kind));
        Assert.That(decoded.Detection!.Polarity, Is.EqualTo(inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
        Assert.That(decoded.Detection.SampledMatrix, Is.Not.Null);
        Assert.That(result.CandidateCount, Is.InRange(1, 4 * 256));
        Assert.That(result.AssembledResults, Is.Empty);
        Assert.That(result.UnassembledResults, Is.Empty);
        Assert.That(image.Pixels, Is.EqualTo(original));
    }

    [TestCase(QrPixelFormat.Gray8)]
    [TestCase(QrPixelFormat.Rgb24)]
    [TestCase(QrPixelFormat.Rgba32)]
    [TestCase(QrPixelFormat.Bgra32)]
    public void ConvertsPaddedRawImagesAndPreservesCallerBuffers(QrPixelFormat format)
    {
        QrImage gray = Render(Encode("12345"));
        int channels = format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;
        int stride = gray.Width * channels + 7;
        byte[] pixels = Enumerable.Repeat((byte)17, (gray.Height - 1) * stride + gray.Width * channels).ToArray();
        for (int y = 0; y < gray.Height; y++)
            for (int x = 0; x < gray.Width; x++)
                for (int c = 0; c < channels; c++)
                    pixels[y * stride + x * channels + c] = c == 3 ? (byte)255 : gray.Pixels[y * gray.Stride + x];
        byte[] original = (byte[])pixels.Clone();
        QrDecodeSet result = Scan(new QrImage { Pixels = pixels, Width = gray.Width, Height = gray.Height, Stride = stride, PixelFormat = format });
        Assert.That(result.Results.Single().Text, Is.EqualTo("12345"));
        Assert.That(pixels, Is.EqualTo(original));
    }

    [Test]
    public void DeduplicatesThresholdPassesButRetainsIdenticalPayloadsAtDistinctLocations()
    {
        QrImage image = Join(Render(Encode("12345")), Render(Encode("12345"), true));
        QrDecodeSet first = Scan(image);
        QrDecodeSet second = Scan(image);
        Assert.That(first.Results.Select(r => r.Text), Is.EqualTo(new[] { "12345", "12345" }));
        Assert.That(first.Results[0].Detection!.Corners!.TopLeft!.X, Is.LessThan(first.Results[1].Detection!.Corners!.TopLeft!.X));
        Assert.That(first.Results.Select(r => r.Detection!.CandidateIndex), Is.EqualTo(second.Results.Select(r => r.Detection!.CandidateIndex)));
        Assert.That(first.Diagnostics, Is.EqualTo(second.Diagnostics));
        Assert.That(first.Results.Select(r => r.Detection!.Polarity), Is.EqualTo(new[] { QrPolarity.DarkOnLight, QrPolarity.LightOnDark }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReturnsEmptyForBlankImagesWithOptionalFailures(bool retainFailures)
    {
        QrImage image = new QrImage { Width = 80, Height = 80, Stride = 80, Pixels = Enumerable.Repeat((byte)255, 6400).ToArray() };
        QrDecodeSet result = ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions(), retainFailures);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.FailedAttempts, Is.Empty);
        Assert.That(result.CandidateCount, Is.Zero);
        Assert.That(result.AssembledResults, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SingleResultSearchContinuesPastFailedCandidateAndOptionallyRetainsFailures(bool retainFailures)
    {
        QrSymbol damaged = Encode("11111");
        QrMatrix matrix = damaged.Matrix!;
        for (int i = 0; i < matrix.Values.Length; i++)
            if (matrix.Roles![i] is QrModuleRole.Data or QrModuleRole.ErrorCorrection)
                matrix.Values[i] = QrModuleValue.Light;
        QrImage image = Join(Render(damaged), Render(Encode("22222")));
        QrDetectionOptions detection = new QrDetectionOptions { DetectMultiple = false };
        QrDecodeOptions decoding = new QrDecodeOptions { EnableRecovery = false, MaximumRecoveryAttempts = 2 };
        QrDecodeSet result = ImageScanPipeline.ScanImage(image, detection, decoding, retainFailures);
        Assert.That(result.Results.Single().Text, Is.EqualTo("22222"));
        Assert.That(result.CandidateCount, Is.EqualTo(2));
        Assert.That(result.FailedAttempts, Has.Length.EqualTo(retainFailures ? 1 : 0));
        foreach (QrAttemptResult failure in result.FailedAttempts)
        {
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Result, Is.Null);
            Assert.That(failure.AttemptCount, Is.InRange(1, 2));
        }
        Assert.That(detection.DetectMultiple, Is.False);
    }

    [Test]
    public void AssemblesPartsInSequenceOrderWhilePreservingSpatialIndividualOrder()
    {
        QrDecodeSet result = Scan(Join(Render(Part(1, "B")), Render(Part(0, "A"))));
        Assert.That(result.Results.Select(r => r.Text), Is.EqualTo(new[] { "B", "A" }));
        Assert.That(result.AssembledResults.Single().Text, Is.EqualTo("AB"));
        Assert.That(result.AssembledResults[0].RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
        Assert.That(result.AssembledResults[0].Detection, Is.Null);
        Assert.That(result.UnassembledResults, Is.Empty);
    }

    [Test]
    public void ReportsIncompleteSequenceWithoutLosingOrdinarySymbols()
    {
        QrDecodeSet result = Scan(Join(Render(Part(1, "B")), Render(Encode("12345"))));
        Assert.That(result.Results, Has.Length.EqualTo(2));
        Assert.That(result.AssembledResults, Is.Empty);
        Assert.That(result.UnassembledResults.Single().Text, Is.EqualTo("B"));
        Assert.That(result.Diagnostics.Any(d => d.Contains("missing 1 parts")), Is.True);
    }

    [Test]
    public void AppliesFamilyAndPolarityPoliciesBeforeDecoding()
    {
        QrImage image = Render(Encode("12345"), true);
        Assert.That(ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { Polarity = QrPolarity.DarkOnLight }, true).Results, Is.Empty);
        QrDecodeOptions decoding = new QrDecodeOptions { AllowModel2 = false, AllowLegacyModel1 = false, AllowMicro = false, AllowRectangularMicro = false };
        Assert.That(ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), decoding, true).CandidateCount, Is.Zero);
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanImage(image,
            new QrDetectionOptions { Polarity = QrPolarity.LightOnDark }, new QrDecodeOptions { Polarity = QrPolarity.DarkOnLight }, true));
    }

    [Test]
    public void DetectsAndDecodesLegacyModel1()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.LegacyModel1, 1, 0), QrSymbolKind.LegacyModel1, 1, 0);
        QrSymbol symbol = new QrSymbol { SymbolKind = QrSymbolKind.LegacyModel1, Version = 1, Width = matrix.Width, Height = matrix.Height, Matrix = matrix };
        QrDecodeSet result = Scan(Render(symbol));
        Assert.That(result.Results.Single().Text, Is.EqualTo("123"));
        Assert.That(result.Results[0].Symbol!.SymbolKind, Is.EqualTo(QrSymbolKind.LegacyModel1));
    }

    [TestCase(120, 80)]
    [TestCase(160, 100)]
    public void RetriesDetectionThresholdsForUnevenIllumination(int gradient, int contrast)
    {
        QrImage image = Render(Encode("12345"));
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                int i = y * image.Stride + x;
                image.Pixels[i] = (byte)Math.Min(255, 20 + x * gradient / image.Width + (image.Pixels[i] == 0 ? 0 : contrast));
            }
        QrDetectionOptions detection = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.GlobalHistogram, RefineGrid = false };
        QrDecodeSet withoutRecovery = ImageScanPipeline.ScanImage(image, detection, new QrDecodeOptions { EnableRecovery = false }, true);
        Assert.That(withoutRecovery.Results, Is.Empty);
        QrDecodeSet recovered = ImageScanPipeline.ScanImage(image, detection, new QrDecodeOptions(), true);
        Assert.That(recovered.Results, Has.Length.EqualTo(1), string.Join(";", recovered.Diagnostics));
        Assert.That(recovered.Results[0].Text, Is.EqualTo("12345"));
        Assert.That(recovered.Results[0].Warnings, Does.Contain("Image recovery used threshold candidate 1."));
        Assert.That(detection.BinarizationMode, Is.EqualTo(QrBinarizationMode.GlobalHistogram));
    }

    [Test]
    public void EnforcesLimitsAndValidatesOptionsEvenWithoutSymbols()
    {
        QrImage image = Render(Encode("12345"));
        QrDecodeSet result = ImageScanPipeline.ScanImage(image, new QrDetectionOptions { MaximumCandidates = 32 },
            new QrDecodeOptions { MaximumMatrixDimension = 10 }, true);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.CandidateCount, Is.InRange(1, 128));
        Assert.That(result.FailedAttempts.All(f => f.FailureReason == QrFailureReason.ResourceLimit && f.AttemptCount == 0), Is.True);
        Assert.Throws<QrResourceLimitException>(() => ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { MaximumAllocationBytes = 1024 }, true));
        Array.Fill(image.Pixels, (byte)255);
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { MaximumRecoveryAttempts = 0 }, true));
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanImage(image, new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, new QrDecodeOptions(), true));
    }

    static QrDecodeSet Scan(QrImage image) => ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions(), true);

    internal static QrSymbol Encode(string text, QrSymbolKind kind = QrSymbolKind.Model2) => new QrEncoder().EncodeText(text,
        new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0 });

    internal static QrSymbol Part(int index, string text) => new QrEncoder().EncodeSegments([
        new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = 2, Parity = 65 ^ 66 } },
        new QrSegment { Mode = QrEncodingMode.Byte, Data = System.Text.Encoding.ASCII.GetBytes(text), CharacterCount = text.Length }
    ], new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0 });

    internal static QrImage Render(QrSymbol symbol, bool inverted = false)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 4, QuietZoneModules = 5, Invert = inverted });
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels, PixelFormat = QrPixelFormat.Gray8 };
    }

    internal static QrImage Join(QrImage left, QrImage right)
    {
        int width = left.Width + right.Width + 40;
        int height = Math.Max(left.Height, right.Height);
        byte[] pixels = Enumerable.Repeat((byte)255, width * height).ToArray();
        for (int y = 0; y < left.Height; y++) Array.Copy(left.Pixels, y * left.Stride, pixels, y * width, left.Width);
        for (int y = 0; y < right.Height; y++) Array.Copy(right.Pixels, y * right.Stride, pixels, y * width + left.Width + 40, right.Width);
        return new QrImage { Width = width, Height = height, Stride = width, Pixels = pixels, PixelFormat = QrPixelFormat.Gray8 };
    }
}
