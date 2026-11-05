namespace barcode.Tests;

public class BoundedDiagnosticCollectorTests
{
    [Test]
    public void CollectorKeepsInsertionOrderWithinTheLimitAndCopiesEvidence()
    {
        BoundedDiagnosticCollector collector = new BoundedDiagnosticCollector(2);
        BarcodeRectangle region = GeometryMath.CreateRectangle(2, 3, 10, 12);

        collector.RecordTransform(
            DiagnosticCodes.PreprocessOriginal,
            "original",
            region);
        collector.RecordThreshold(127, region);
        collector.RecordCandidateRejection(
            DiagnosticCodes.CandidateNotFound,
            "none",
            1,
            region);
        region.X = 999;

        BarcodeDiagnostic[] firstCopy = collector.CopyDiagnostics();
        firstCopy[0].Region!.X = 777;
        BarcodeDiagnostic[] secondCopy = collector.CopyDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(collector.Count, Is.EqualTo(2));
            Assert.That(collector.DroppedCount, Is.EqualTo(1));
            Assert.That(firstCopy, Has.Length.EqualTo(2));
            Assert.That(firstCopy[0].Code, Is.EqualTo(DiagnosticCodes.PreprocessOriginal));
            Assert.That(firstCopy[1].Code, Is.EqualTo(DiagnosticCodes.BinarizationThreshold));
            Assert.That(firstCopy[1].HasNumericEvidence, Is.True);
            Assert.That(firstCopy[1].Value, Is.EqualTo(127));
            Assert.That(secondCopy[0].Region!.X, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecodeResultRecordsCorrectionConfidenceAndCoordinates()
    {
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.ErrorCorrectionApplied = true;
        metadata.CorrectedErrorCount = 2;
        metadata.ErasureCount = 1;
        metadata.Confidence = 0.82;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Metadata = metadata;
        result.Location = CreateLocation(4, 6, 20, 10);
        BoundedDiagnosticCollector collector = new BoundedDiagnosticCollector(10);

        collector.RecordDecodeResult(result);
        BarcodeDiagnostic[] diagnostics = collector.CopyDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(3));
            Assert.That(diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ErrorCorrection));
            Assert.That(diagnostics[0].Value, Is.EqualTo(3));
            Assert.That(diagnostics[1].Code, Is.EqualTo(DiagnosticCodes.ResultConfidence));
            Assert.That(diagnostics[1].Value, Is.EqualTo(0.82));
            Assert.That(diagnostics[2].Code, Is.EqualTo(DiagnosticCodes.ResultCoordinates));
            Assert.That(diagnostics[2].Location!.X, Is.EqualTo(14));
            Assert.That(diagnostics[2].Location!.Y, Is.EqualTo(11));
            Assert.That(diagnostics[2].Region!.Width, Is.EqualTo(20));
            Assert.That(diagnostics[2].Region!.Height, Is.EqualTo(10));
        });
    }

    [Test]
    public void ZeroUsesTheDefaultBoundAndCountsEveryDroppedEntry()
    {
        BoundedDiagnosticCollector collector = new BoundedDiagnosticCollector(0);
        int index = 0;
        while (index < BoundedDiagnosticCollector.DefaultMaximumDiagnostics + 2)
        {
            collector.RecordTransform(
                DiagnosticCodes.PreprocessSmoothed,
                "bounded",
                null);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(collector.MaximumDiagnostics,
                Is.EqualTo(BoundedDiagnosticCollector.DefaultMaximumDiagnostics));
            Assert.That(collector.CopyDiagnostics(),
                Has.Length.EqualTo(BoundedDiagnosticCollector.DefaultMaximumDiagnostics));
            Assert.That(collector.DroppedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidLimitsResultsAndNumericEvidenceAreRejected()
    {
        BoundedDiagnosticCollector collector = new BoundedDiagnosticCollector(2);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new BoundedDiagnosticCollector(-1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => collector.RecordDecodeResult(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => collector.RecordThreshold(double.NaN, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => collector.RecordConfidence(
                    "Decode",
                    DiagnosticCodes.ResultConfidence,
                    "invalid",
                    double.PositiveInfinity,
                    0,
                    null),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BarcodeQuadrilateral CreateLocation(
        double left,
        double top,
        double width,
        double height)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(left + width, top),
            GeometryMath.CreatePoint(left + width, top + height),
            GeometryMath.CreatePoint(left, top + height));
    }
}
