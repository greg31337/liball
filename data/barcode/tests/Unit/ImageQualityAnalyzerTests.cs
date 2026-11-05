namespace barcode.Tests;

public class ImageQualityAnalyzerTests
{
    [Test]
    public void CorrectedTolerantResultRemainsNonconformantRecoveryEvidence()
    {
        ManagedGrayImage image = CreateStripedImage();
        ImageHistogramAnalysis histogram = HistogramAnalyzer.Analyze(image);
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.Codewords = new int[] { 1, 2, 3, 4, 5, 6 };
        metadata.ErrorCorrectionApplied = true;
        metadata.CorrectedErrorCount = 1;
        metadata.Confidence = 0.9;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = BarcodeFormat.DataMatrix;
        result.Dimension = BarcodeDimension.Matrix;
        result.Location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(20.0, 20.0),
            GeometryMath.CreatePoint(80.0, 20.0),
            GeometryMath.CreatePoint(80.0, 80.0),
            GeometryMath.CreatePoint(20.0, 80.0));
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Normal;
        result.Metadata = metadata;
        ImageValidationState state = new ImageValidationState(
            BarcodeValidationMode.Tolerant);

        ImageQualityValidator.AnalyzeDecodedImage(
            state,
            image,
            histogram,
            result);
        BarcodeValidationIssue recovery = state.Issues.Single(
            issue => issue.Code == "IMAGE_RECOVERY_APPLIED");

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Warning));
            Assert.That(state.Issues, Is.Not.Empty);
        });
    }

    [Test]
    public void DistortedLowConfidenceResultReportsGeometryAndIntegrityFailures()
    {
        ManagedGrayImage image = CreateStripedImage();
        ImageHistogramAnalysis histogram = HistogramAnalyzer.Analyze(image);
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.Codewords = new int[] { 1, 2, 3, 4 };
        metadata.CorrectedErrorCount = 2;
        metadata.Confidence = 0.1;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = BarcodeFormat.DataMatrix;
        result.Dimension = BarcodeDimension.Matrix;
        result.Location = CreateDistortedLocation();
        result.SkewDegrees = 12.0;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.Metadata = metadata;
        ImageValidationState state = new ImageValidationState(
            BarcodeValidationMode.Strict);

        ImageQualityValidator.AnalyzeDecodedImage(
            state,
            image,
            histogram,
            result);
        string[] codes = state.Issues.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("IMAGE_SKEW_EXCESSIVE"));
            Assert.That(codes, Does.Contain("IMAGE_PERSPECTIVE_EXCESSIVE"));
            Assert.That(codes, Does.Contain("IMAGE_AXIAL_NONUNIFORMITY"));
            Assert.That(codes, Does.Contain("IMAGE_DECODE_CONFIDENCE_LOW"));
            Assert.That(codes, Does.Contain("IMAGE_FIXED_PATTERN_DAMAGED"));
            Assert.That(codes, Does.Contain("IMAGE_ECC_MARGIN_LOW"));
            Assert.That(state.Metrics,
                Has.One.Matches<BarcodeQualityMetric>(
                    metric => metric.Name == "perspective_distortion" &&
                        !metric.MeetsThreshold && metric.IsHeuristic));
        });
    }

    static ManagedGrayImage CreateStripedImage()
    {
        ManagedGrayImage image = new ManagedGrayImage(100, 100);
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                image.WriteSample(x, y, x % 4 < 2 ? 20 : 240);
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static BarcodeQuadrilateral CreateDistortedLocation()
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(20.0, 20.0),
            GeometryMath.CreatePoint(80.0, 20.0),
            GeometryMath.CreatePoint(80.0, 80.0),
            GeometryMath.CreatePoint(50.0, 80.0));
    }
}
