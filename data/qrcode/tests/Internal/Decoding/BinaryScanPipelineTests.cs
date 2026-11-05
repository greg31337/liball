using qrcode;
using static qrcode.Tests.ImageScanPipelineTests;

namespace qrcode.Tests;

[TestFixture]
public class BinaryScanPipelineTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    [TestCase(QrSymbolKind.LegacyModel1, false)]
    public void DecodesPixelMatricesWithTheSameGeometryAndPayloadAsRawImages(QrSymbolKind kind, bool inverted)
    {
        QrSymbol symbol;
        if (kind == QrSymbolKind.LegacyModel1)
        {
            QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, 1, 0), kind, 1, 0);
            symbol = new QrSymbol { SymbolKind = kind, Version = 1, Width = matrix.Width, Height = matrix.Height, Matrix = matrix };
        }
        else symbol = Encode("12345", kind);
        QrImage image = Render(symbol, inverted);
        QrDecodeSet binary = Scan(Binary(image));
        QrDecodeSet raw = ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = false }, true);
        Assert.That(binary.Results, Has.Length.EqualTo(1));
        AssertEquivalent(binary, raw);
        Assert.That(binary.Results[0].Symbol!.SymbolKind, Is.EqualTo(kind));
        Assert.That(binary.Results[0].Text, Is.EqualTo(kind == QrSymbolKind.LegacyModel1 ? "123" : "12345"));
        Assert.That(binary.Results[0].Detection!.SampledMatrix, Is.Not.Null);
        Assert.That(binary.Results[0].Detection!.Polarity, Is.EqualTo(inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
    }

    [TestCase(QrBinarizationMode.Automatic)]
    [TestCase(QrBinarizationMode.GlobalHistogram)]
    [TestCase(QrBinarizationMode.HybridLocal)]
    [TestCase(QrBinarizationMode.SuppliedBinary)]
    public void BypassesThresholdSettingsWithoutMutatingOptions(QrBinarizationMode mode)
    {
        QrDetectionOptions detection = new QrDetectionOptions
        {
            BinarizationMode = mode, NormalizeContrast = true, DetectMultiple = false,
            EnableDenoise = true, EnableGapClosing = true
        };
        QrDecodeSet result = ImageScanPipeline.ScanBinaryMatrix(Binary(Render(Encode("12345"))), detection, new QrDecodeOptions(), true);
        Assert.That(result.Results.Single().Text, Is.EqualTo("12345"));
        Assert.That(result.Diagnostics, Is.EqualTo(new[] { "Supplied binary: 1 detections." }));
        Assert.That(result.CandidateCount, Is.EqualTo(1));
        Assert.That(detection.BinarizationMode, Is.EqualTo(mode));
        Assert.That(detection.NormalizeContrast, Is.True);
        Assert.That(detection.DetectMultiple, Is.False);
    }

    [Test]
    public void KeepsEqualPayloadsAtDistinctLocationsInStableSpatialOrder()
    {
        QrImage image = Join(Render(Encode("12345")), Render(Encode("12345"), true));
        QrMatrix matrix = Binary(image);
        QrDecodeSet first = Scan(matrix);
        QrDecodeSet second = Scan(matrix);
        QrDecodeSet raw = ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = false }, true);
        Assert.That(first.Results.Select(r => r.Text), Is.EqualTo(new[] { "12345", "12345" }));
        Assert.That(first.Results[0].Detection!.Corners!.TopLeft!.X, Is.LessThan(first.Results[1].Detection!.Corners!.TopLeft!.X));
        AssertEquivalent(first, raw);
        AssertEquivalent(first, second);
        Assert.That(first.Diagnostics, Is.EqualTo(second.Diagnostics));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ContinuesPastFailuresForSingleResultsAndMatchesRawFailureDiagnostics(bool retainFailures)
    {
        QrImage image = Join(Render(DamagedSymbol()), Render(Encode("22222")));
        QrDetectionOptions detection = new QrDetectionOptions { DetectMultiple = false };
        QrDecodeOptions decoding = new QrDecodeOptions { EnableRecovery = false, MaximumRecoveryAttempts = 2 };
        QrDecodeSet binary = ImageScanPipeline.ScanBinaryMatrix(Binary(image), detection, decoding, retainFailures);
        QrDecodeSet raw = ImageScanPipeline.ScanImage(image, detection, decoding, retainFailures);
        Assert.That(binary.Results.Single().Text, Is.EqualTo("22222"));
        Assert.That(binary.CandidateCount, Is.EqualTo(2));
        Assert.That(binary.FailedAttempts, Has.Length.EqualTo(retainFailures ? 1 : 0));
        AssertEquivalent(binary, raw);
        Assert.That(binary.Diagnostics.Skip(1), Is.EqualTo(raw.Diagnostics.Skip(1)));
    }

    [Test]
    public void RecoveryBudgetDoesNotCauseAdditionalBinaryDetectionPasses()
    {
        QrDecodeSet result = ImageScanPipeline.ScanBinaryMatrix(Binary(Render(DamagedSymbol())), new QrDetectionOptions(),
            new QrDecodeOptions { MaximumRecoveryAttempts = 2 }, true);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.CandidateCount, Is.EqualTo(1));
        QrAttemptResult failure = result.FailedAttempts.Single();
        Assert.That(failure.Success, Is.False);
        Assert.That(failure.Result, Is.Null);
        Assert.That(failure.AttemptCount, Is.InRange(1, 2));
        Assert.That(result.Diagnostics.Count(d => d.StartsWith("Supplied binary:")), Is.EqualTo(1));
        Assert.That(result.Diagnostics.Any(d => d.Contains("Threshold")), Is.False);
    }

    [TestCase(QrModuleValue.Light, false)]
    [TestCase(QrModuleValue.Light, true)]
    [TestCase(QrModuleValue.Dark, false)]
    [TestCase(QrModuleValue.Dark, true)]
    public void BlankMatricesReturnEmptySets(QrModuleValue value, bool retainFailures)
    {
        QrMatrix matrix = new QrMatrix { Width = 80, Height = 80, Values = Enumerable.Repeat(value, 6400).ToArray() };
        QrDecodeSet result = ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(), new QrDecodeOptions(), retainFailures);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.AssembledResults, Is.Empty);
        Assert.That(result.UnassembledResults, Is.Empty);
        Assert.That(result.FailedAttempts, Is.Empty);
        Assert.That(result.CandidateCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StructuredAppendAssemblyAndDiagnosticsMatchRawScanning(bool complete)
    {
        QrImage image = Join(Render(Part(1, "B")), Render(complete ? Part(0, "A") : Encode("12345")));
        QrDecodeSet binary = Scan(Binary(image));
        QrDecodeSet raw = ImageScanPipeline.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = false }, true);
        AssertEquivalent(binary, raw);
        Assert.That(binary.Results, Has.Length.EqualTo(2));
        Assert.That(binary.Diagnostics.Skip(1), Is.EqualTo(raw.Diagnostics.Skip(1)));
        if (complete)
        {
            Assert.That(binary.AssembledResults.Single().Text, Is.EqualTo("AB"));
            Assert.That(binary.AssembledResults[0].RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
            Assert.That(binary.UnassembledResults, Is.Empty);
        }
        else
        {
            Assert.That(binary.AssembledResults, Is.Empty);
            Assert.That(binary.UnassembledResults.Single().Text, Is.EqualTo("B"));
            Assert.That(binary.Diagnostics.Any(d => d.Contains("missing 1 parts")), Is.True);
        }
    }

    [Test]
    public void UsesOnlyPixelValuesAndDoesNotMutateOrRetainCallerBuffers()
    {
        QrMatrix matrix = Binary(Render(Encode("12345")));
        QrModuleValue[] values = (QrModuleValue[])matrix.Values.Clone();
        matrix.Confidence = new byte[] { 0 };
        matrix.Roles = new[] { QrModuleRole.Unknown };
        matrix.QuietZoneLeft = matrix.QuietZoneTop = matrix.QuietZoneRight = matrix.QuietZoneBottom = 999;
        QrDecodeSet result = Scan(matrix);
        Assert.That(result.Results.Single().Text, Is.EqualTo("12345"));
        Assert.That(matrix.Values, Is.EqualTo(values));
        Assert.That(matrix.Confidence, Is.EqualTo(new byte[] { 0 }));
        Assert.That(matrix.Roles, Is.EqualTo(new[] { QrModuleRole.Unknown }));
        Assert.That(matrix.QuietZoneLeft, Is.EqualTo(999));
        Array.Fill(result.Results[0].Detection!.SampledMatrix!.Values, QrModuleValue.Unknown);
        Assert.That(matrix.Values, Is.EqualTo(values));
        Assert.That(Scan(matrix).Results.Single().Text, Is.EqualTo("12345"));
    }

    [Test]
    public void RepairsDamagedPayloadCodewordsFromBinaryPixels()
    {
        QrSymbol symbol = Encode("12345");
        QrMatrix modules = symbol.Matrix!;
        int index = Array.FindIndex(modules.Roles!, r => r == QrModuleRole.Data);
        modules.Values[index] = modules.Values[index] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        QrDecodeSet result = Scan(Binary(Render(symbol)));
        Assert.That(result.Results.Single().Text, Is.EqualTo("12345"));
        Assert.That(result.Results[0].CorrectedErrorCount, Is.GreaterThan(0));
    }

    [Test]
    public void HonorsFamilyAndPolarityRestrictions()
    {
        QrMatrix matrix = Binary(Render(Encode("12345"), true));
        Assert.That(ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(),
            new QrDecodeOptions { Polarity = QrPolarity.DarkOnLight }, true).Results, Is.Empty);
        Assert.That(ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions { Polarity = QrPolarity.LightOnDark },
            new QrDecodeOptions(), true).Results.Single().Text, Is.EqualTo("12345"));
        QrDecodeOptions disabled = new QrDecodeOptions { AllowModel2 = false, AllowLegacyModel1 = false, AllowMicro = false, AllowRectangularMicro = false };
        Assert.That(ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(), disabled, true).CandidateCount, Is.Zero);
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix,
            new QrDetectionOptions { Polarity = QrPolarity.LightOnDark }, new QrDecodeOptions { Polarity = QrPolarity.DarkOnLight }, true));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void RejectsMalformedBinaryBuffersEvenWhenAllFamiliesAreDisabled(int invalid)
    {
        QrMatrix matrix = Binary(Render(Encode("12345")));
        if (invalid == 0) matrix.Values = null!;
        if (invalid == 1) matrix.Values = new QrModuleValue[matrix.Values.Length - 1];
        if (invalid == 2) matrix.Values = new QrModuleValue[matrix.Values.Length + 1];
        if (invalid == 3) matrix.Values[^1] = QrModuleValue.Unknown;
        if (invalid == 4) matrix.Values[^1] = (QrModuleValue)99;
        QrDetectionOptions detection = new QrDetectionOptions { AllowModel2 = false, AllowLegacyModel1 = false, AllowMicro = false, AllowRectangularMicro = false };
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix, detection, new QrDecodeOptions(), true));
    }

    [Test]
    public void ValidatesArgumentsAndEnforcesImageAndDecodeLimits()
    {
        QrMatrix matrix = Binary(Render(Encode("12345")));
        Assert.Throws<QrInputException>(() => Scan(null!));
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix, null!, new QrDecodeOptions(), true));
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(), null!, true));
        Assert.Throws<QrResourceLimitException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix,
            new QrDetectionOptions { MaximumImageWidth = matrix.Width - 1 }, new QrDecodeOptions(), true));
        Assert.Throws<QrResourceLimitException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix,
            new QrDetectionOptions(), new QrDecodeOptions { MaximumAllocationBytes = 1024 }, true));
        QrDecodeSet limited = ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(),
            new QrDecodeOptions { MaximumMatrixDimension = 10 }, true);
        Assert.That(limited.Results, Is.Empty);
        Assert.That(limited.FailedAttempts.Single().FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(limited.FailedAttempts[0].AttemptCount, Is.Zero);
        Array.Fill(matrix.Values, QrModuleValue.Light);
        Assert.Throws<QrInputException>(() => ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(),
            new QrDecodeOptions { MaximumRecoveryAttempts = 0 }, true));
    }

    static QrDecodeSet Scan(QrMatrix matrix) => ImageScanPipeline.ScanBinaryMatrix(matrix, new QrDetectionOptions(), new QrDecodeOptions(), true);

    static QrMatrix Binary(QrImage image) => new QrMatrix
    {
        Width = image.Width, Height = image.Height,
        Values = image.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
    };

    static QrSymbol DamagedSymbol()
    {
        QrSymbol symbol = Encode("11111");
        QrMatrix matrix = symbol.Matrix!;
        for (int i = 0; i < matrix.Values.Length; i++)
            if (matrix.Roles![i] is QrModuleRole.Data or QrModuleRole.ErrorCorrection) matrix.Values[i] = QrModuleValue.Light;
        return symbol;
    }

    static void AssertEquivalent(QrDecodeSet binary, QrDecodeSet other)
    {
        Assert.That(binary.Results.Select(r => r.Text), Is.EqualTo(other.Results.Select(r => r.Text)));
        Assert.That(binary.Results.Select(r => r.RawBytes), Is.EqualTo(other.Results.Select(r => r.RawBytes)));
        Assert.That(binary.Results.Select(r => r.Detection!.CandidateIndex), Is.EqualTo(other.Results.Select(r => r.Detection!.CandidateIndex)));
        Assert.That(binary.Results.Select(r => r.Detection!.Corners!.TopLeft!.X), Is.EqualTo(other.Results.Select(r => r.Detection!.Corners!.TopLeft!.X)));
        Assert.That(binary.Results.Select(r => r.Detection!.Corners!.TopLeft!.Y), Is.EqualTo(other.Results.Select(r => r.Detection!.Corners!.TopLeft!.Y)));
        Assert.That(binary.FailedAttempts.Select(f => (f.FailureReason, f.AttemptCount, f.CandidateCount)),
            Is.EqualTo(other.FailedAttempts.Select(f => (f.FailureReason, f.AttemptCount, f.CandidateCount))));
        Assert.That(binary.AssembledResults.Select(r => r.RawBytes), Is.EqualTo(other.AssembledResults.Select(r => r.RawBytes)));
        Assert.That(binary.UnassembledResults.Select(r => r.Text), Is.EqualTo(other.UnassembledResults.Select(r => r.Text)));
        Assert.That(binary.CandidateCount, Is.EqualTo(other.CandidateCount));
    }
}
