using qrcode;
using static qrcode.Tests.ImageScanPipelineTests;

namespace qrcode.Tests;

[TestFixture]
public class QrScannerTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void OrdinaryNoSymbolImagesReturnEmptySets(bool binary, bool retainFailures)
    {
        QrImage image = Render(Encode("12345"));
        Array.Fill(image.Pixels, (byte)255);
        QrDecodeSet result = Scan(new QrScanner(), image, binary, new QrDetectionOptions(), new QrDecodeOptions(), retainFailures);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.AssembledResults, Is.Empty);
        Assert.That(result.UnassembledResults, Is.Empty);
        Assert.That(result.FailedAttempts, Is.Empty);
        Assert.That(result.CandidateCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReturnsOrderedResultsAndCanBeReusedWithoutRetainingCallerState(bool binary)
    {
        IQrScanner scanner = new QrScanner();
        QrImage image = Join(Render(Encode("11111")), Render(Encode("22222"), true));
        QrMatrix matrix = Binary(image);
        byte[] before = (byte[])image.Pixels.Clone();
        QrModuleValue[] beforeMatrix = (QrModuleValue[])matrix.Values.Clone();
        QrDetectionOptions detection = new QrDetectionOptions();
        QrDecodeOptions decoding = new QrDecodeOptions();
        QrDecodeSet first = binary ? scanner.ScanBinaryMatrix(matrix, detection, decoding, true)
            : scanner.ScanImage(image, detection, decoding, true);
        Assert.That(first.Results.Select(r => r.Text), Is.EqualTo(new[] { "11111", "22222" }));
        Assert.That(first.Results[0].Detection!.Corners!.TopLeft!.X, Is.LessThan(first.Results[1].Detection!.Corners!.TopLeft!.X));
        Assert.That(first.Results[1].Detection!.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
        first.Results[0].Text = "changed";
        Array.Fill(first.Results[0].Detection!.SampledMatrix!.Values, QrModuleValue.Unknown);
        QrDecodeSet second = binary ? scanner.ScanBinaryMatrix(matrix, detection, decoding, false)
            : scanner.ScanImage(image, detection, decoding, false);
        Assert.That(second.Results.Select(r => r.Text), Is.EqualTo(new[] { "11111", "22222" }));
        Assert.That(second.Results[0], Is.Not.SameAs(first.Results[0]));
        Assert.That(image.Pixels, Is.EqualTo(before));
        Assert.That(matrix.Values, Is.EqualTo(beforeMatrix));
        Assert.That(detection.BinarizationMode, Is.EqualTo(QrBinarizationMode.Automatic));
        Assert.That(decoding.Polarity, Is.EqualTo(QrPolarity.Automatic));
        Array.Fill(image.Pixels, (byte)255);
        Assert.That(Scan(scanner, image, binary, detection, decoding, true).Results, Is.Empty);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ExpectedFailuresAreOptionalAndDoNotStopSingleResultSearch(bool binary, bool retainFailures)
    {
        QrSymbol damaged = Encode("11111");
        QrMatrix modules = damaged.Matrix!;
        for (int i = 0; i < modules.Values.Length; i++)
            if (modules.Roles![i] is QrModuleRole.Data or QrModuleRole.ErrorCorrection) modules.Values[i] = QrModuleValue.Light;
        QrImage image = Join(Render(damaged), Render(Encode("22222")));
        QrDecodeSet result = Scan(new QrScanner(), image, binary, new QrDetectionOptions { DetectMultiple = false },
            new QrDecodeOptions { EnableRecovery = false, MaximumRecoveryAttempts = 2 }, retainFailures);
        Assert.That(result.Results.Single().Text, Is.EqualTo("22222"));
        Assert.That(result.CandidateCount, Is.EqualTo(2));
        Assert.That(result.FailedAttempts, Has.Length.EqualTo(retainFailures ? 1 : 0));
        Assert.That(result.Diagnostics.Any(d => d.StartsWith("Candidate 0:")), Is.EqualTo(retainFailures));
        if (retainFailures)
        {
            QrAttemptResult failure = result.FailedAttempts[0];
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.FailureReason, Is.Not.EqualTo(QrFailureReason.None));
            Assert.That(failure.Result, Is.Null);
            Assert.That(failure.RawBytes, Is.Empty);
            Assert.That(failure.CandidateCount, Is.EqualTo(1));
            Assert.That(failure.AttemptCount, Is.InRange(1, 2));
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ReturnsCompleteAndIncompleteStructuredAppendOutputs(bool binary, bool complete)
    {
        QrImage image = Join(Render(Part(1, "B")), Render(complete ? Part(0, "A") : Encode("12345")));
        QrDecodeSet result = Scan(new QrScanner(), image, binary, new QrDetectionOptions(), new QrDecodeOptions(), false);
        Assert.That(result.Results.Select(r => r.Text), Is.EqualTo(new[] { "B", complete ? "A" : "12345" }));
        Assert.That(result.FailedAttempts, Is.Empty);
        if (complete)
        {
            Assert.That(result.AssembledResults.Single().Text, Is.EqualTo("AB"));
            Assert.That(result.AssembledResults[0].RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
            Assert.That(result.UnassembledResults, Is.Empty);
        }
        else
        {
            Assert.That(result.AssembledResults, Is.Empty);
            Assert.That(result.UnassembledResults.Single(), Is.SameAs(result.Results[0]));
            Assert.That(result.Diagnostics.Any(d => d.Contains("missing 1 parts")), Is.True);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PreservesInputExceptionsAndReportsCandidateResourceFailures(bool binary)
    {
        IQrScanner scanner = new QrScanner();
        QrImage image = Render(Encode("12345"));
        Assert.Throws<QrInputException>(() => Scan(scanner, image, binary, null!, new QrDecodeOptions(), false));
        Assert.Throws<QrInputException>(() => Scan(scanner, image, binary, new QrDetectionOptions(), null!, false));
        Assert.Throws<QrResourceLimitException>(() => Scan(scanner, image, binary, new QrDetectionOptions(),
            new QrDecodeOptions { MaximumAllocationBytes = 1024 }, false));
        QrDecodeSet limited = Scan(scanner, image, binary, new QrDetectionOptions(),
            new QrDecodeOptions { MaximumMatrixDimension = 10, EnableRecovery = false }, true);
        Assert.That(limited.Results, Is.Empty);
        Assert.That(limited.FailedAttempts.Single().FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(limited.FailedAttempts[0].AttemptCount, Is.Zero);
        Array.Fill(image.Pixels, (byte)255);
        Assert.Throws<QrInputException>(() => Scan(scanner, image, binary, new QrDetectionOptions(),
            new QrDecodeOptions { MaximumRecoveryAttempts = 0 }, false));
    }

    [Test]
    public void RejectsMissingSourcesAndInvalidBinaryPixels()
    {
        IQrScanner scanner = new QrScanner();
        Assert.Throws<QrInputException>(() => scanner.ScanImage(null!, new QrDetectionOptions(), new QrDecodeOptions(), false));
        Assert.Throws<QrInputException>(() => scanner.ScanBinaryMatrix(null!, new QrDetectionOptions(), new QrDecodeOptions(), false));
        QrImage image = Render(Encode("12345"));
        Assert.Throws<QrInputException>(() => scanner.ScanImage(image,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, new QrDecodeOptions(), false));
        QrMatrix matrix = Binary(image);
        matrix.Values[^1] = QrModuleValue.Unknown;
        Assert.Throws<QrInputException>(() => scanner.ScanBinaryMatrix(matrix, new QrDetectionOptions(), new QrDecodeOptions(), true));
    }

    static QrDecodeSet Scan(IQrScanner scanner, QrImage image, bool binary, QrDetectionOptions detection,
        QrDecodeOptions decoding, bool retainFailures) => binary
            ? scanner.ScanBinaryMatrix(Binary(image), detection, decoding, retainFailures)
            : scanner.ScanImage(image, detection, decoding, retainFailures);

    static QrMatrix Binary(QrImage image) => new QrMatrix
    {
        Width = image.Width, Height = image.Height,
        Values = image.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
    };
}
