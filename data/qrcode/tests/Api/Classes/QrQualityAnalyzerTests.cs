using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrQualityAnalyzerTests
{
    static readonly string[] Names = { "SymbolContrast", "Modulation", "FixedPatternDamage", "CriticalMetadataDamage",
        "AxialNonUniformity", "GridNonUniformity", "GeometricDistortion", "HorizontalPrintGrowth", "VerticalPrintGrowth", "QuietZone", "UnusedErrorCorrection" };

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 7, 3)]
    public void AnalyzesEachFamilyThroughPublicContractWithoutInventingImageEvidence(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Matrix(kind, version, level, true);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrQualityReport report = analyzer.AnalyzeMatrixQuality(matrix, Options());
        Assert.That(report.SymbolKind, Is.EqualTo(kind));
        Assert.That(report.SampleCount, Is.EqualTo(matrix.Width * matrix.Height));
        Assert.That(report.Metrics.Select(m => m.Name), Is.EqualTo(Names));
        Assert.That(Metric(report, "QuietZone").Value, Is.EqualTo(1));
        Assert.That(Metric(report, "UnusedErrorCorrection").Value, Is.EqualTo(1));
        Assert.That(Metric(report, "UnusedErrorCorrection").IsCalculated, Is.True);
        foreach (string name in new[] { "SymbolContrast", "Modulation", "GridNonUniformity", "HorizontalPrintGrowth" })
            Assert.That(Metric(report, name).IsCalculated, Is.False);
        if (kind != QrSymbolKind.LegacyModel1)
        {
            Assert.That(report.Validation, Is.Not.Null);
            Assert.That(report.Validation!.IsConformant, Is.True);
            Assert.That(Metric(report, "FixedPatternDamage").IsCalculated, Is.True);
            Assert.That(Metric(report, "FixedPatternDamage").Value, Is.Zero);
        }
        else
        {
            Assert.That(report.Validation, Is.Null);
            Assert.That(Metric(report, "FixedPatternDamage").Limitation, Does.Contain("Legacy"));
        }
        Assert.That(matrix.Values, Is.EqualTo(before));
        AssertUngraded(report);
    }

    [Test]
    public void PreservesDamageWhileNormalizingRotatedMirroredAndInvertedObservations()
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 2, 0, false);
        matrix.Values[0] = QrModuleValue.Light;
        matrix.Values[8] = matrix.Values[8] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        matrix = MatrixTransformer.AddQuietZone(matrix, QrSymbolKind.Model2, 4);
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, matrix.Values.Length).ToArray();
        QrQualityReport original = new QrQualityAnalyzer().AnalyzeMatrixQuality(matrix, Options());
        matrix = MatrixTransformer.InvertPolarity(MatrixTransformer.Rotate(MatrixTransformer.Mirror(matrix), 1));
        QrQualityReport transformed = new QrQualityAnalyzer().AnalyzeMatrixQuality(matrix, Options());
        Assert.That(original.Validation!.FixedPatternDamageCount, Is.EqualTo(1));
        Assert.That(original.Validation.CriticalMetadataDamageCount, Is.EqualTo(1));
        Assert.That(transformed.Validation!.FixedPatternDamageCount, Is.EqualTo(1));
        Assert.That(transformed.Validation.CriticalMetadataDamageCount, Is.EqualTo(1));
        Assert.That(Metric(original, "FixedPatternDamage").Value, Is.GreaterThan(0));
        Assert.That(Metric(transformed, "FixedPatternDamage").Value, Is.EqualTo(Metric(original, "FixedPatternDamage").Value));
        Assert.That(Metric(transformed, "QuietZone").Value, Is.EqualTo(1));
    }

    [Test]
    public void UsesActualCorrectionCountsAndLeavesDetectionOnlyCapacityUnavailable()
    {
        byte[] words = CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 1, 0);
        words[0] ^= 0x55;
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, QrSymbolKind.Model2, 1, 0);
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeMatrixQuality(matrix, Options());
        Assert.That(Metric(report, "UnusedErrorCorrection").Value, Is.EqualTo(5.0 / 7));
        Assert.That(report.Validation!.PayloadDamageCount, Is.GreaterThan(0));
        QrQualityReport m1 = new QrQualityAnalyzer().AnalyzeMatrixQuality(Matrix(QrSymbolKind.Micro, 1, -1, true), Options());
        Assert.That(Metric(m1, "UnusedErrorCorrection").IsCalculated, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MeasuresMissingAndContaminatedMatrixQuietZones(bool contaminate)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, contaminate);
        if (contaminate) matrix.Values[0] = QrModuleValue.Dark;
        QrQualityMetric quiet = Metric(new QrQualityAnalyzer().AnalyzeMatrixQuality(matrix, Options()), "QuietZone");
        Assert.That(quiet.IsCalculated, Is.True);
        Assert.That(quiet.Value, Is.Zero);
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 4, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 2, 0)]
    public void ImageReportUsesFreshSamplingAndOriginalLuminance(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Matrix(kind, version, level, false);
        QrImage image = Render(matrix, 4, 6, 60, 180);
        QrDetection detection = Detection(matrix, 4, 6);
        detection.SampledMatrix = new QrMatrix();
        detection.QuietZone = new QrQuietZoneMeasurement { IsConformant = false };
        byte[] before = (byte[])image.Pixels.Clone();
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeImageQuality(image, detection,
            new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight, NormalizeContrast = true }, Options());
        Assert.That(Metric(report, "SymbolContrast").Value, Is.EqualTo(120.0 / 255).Within(1e-12));
        Assert.That(Metric(report, "Modulation").Value, Is.EqualTo(1));
        Assert.That(Metric(report, "QuietZone").IsCalculated, Is.True);
        Assert.That(Metric(report, "QuietZone").Value, Is.EqualTo(1));
        Assert.That(Metric(report, "AxialNonUniformity").Value, Is.Zero.Within(1e-10));
        Assert.That(Metric(report, "GridNonUniformity").IsCalculated, Is.False);
        Assert.That(report.SampleCount, Is.GreaterThan(matrix.Values.Length));
        if (kind != QrSymbolKind.LegacyModel1)
        {
            Assert.That(Metric(report, "HorizontalPrintGrowth").IsCalculated, Is.True);
            Assert.That(Metric(report, "HorizontalPrintGrowth").Value, Is.Zero.Within(0.01));
        }
        Assert.That(detection.SampledMatrix.Width, Is.Zero);
        Assert.That(detection.QuietZone.IsConformant, Is.False);
        Assert.That(image.Pixels, Is.EqualTo(before));
        AssertUngraded(report);
    }

    [Test]
    public void SupportsInvertedRgbImagesAndMultiSampleApertures()
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrImage gray = Render(matrix, 4, 6, 220, 20);
        QrImage rgb = new QrImage { Width = gray.Width, Height = gray.Height, Stride = gray.Width * 3 + 7, PixelFormat = QrPixelFormat.Rgb24 };
        rgb.Pixels = new byte[rgb.Stride * rgb.Height];
        for (int row = 0; row < rgb.Height; row++)
            for (int column = 0; column < rgb.Width; column++)
                for (int channel = 0; channel < 3; channel++) rgb.Pixels[row * rgb.Stride + column * 3 + channel] = gray.Pixels[row * gray.Stride + column];
        QrQualityOptions options = Options();
        options.SamplesPerModule = 3;
        options.ApertureDiameterModules = 0.5;
        QrDetection detection = Detection(matrix, 4, 6);
        detection.Polarity = QrPolarity.LightOnDark;
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeImageQuality(rgb, detection,
            new QrDetectionOptions { Polarity = QrPolarity.LightOnDark, BinarizationMode = QrBinarizationMode.GlobalHistogram }, options);
        Assert.That(Metric(report, "SymbolContrast").Value, Is.EqualTo(200.0 / 255).Within(1e-12));
        Assert.That(Metric(report, "QuietZone").Value, Is.EqualTo(1));
        Assert.That(report.SampleCount, Is.GreaterThan(matrix.Values.Length * 9));
    }

    [Test]
    public void ImageQuietZoneDoesNotReuseAbsentSampledMatrixMargins()
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeImageQuality(Render(matrix, 0, 5, 0, 255), Detection(matrix, 0, 5),
            new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight }, Options());
        Assert.That(Metric(report, "QuietZone").IsCalculated, Is.False);
        Assert.That(Metric(report, "QuietZone").Limitation, Does.Contain("cropped"));
    }

    [Test]
    public void ApertureSamplingAveragesOriginalPixelsAtTheRequestedModuleOffsets()
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrImage image = Render(matrix, 4, 6, 0, 200);
        for (int row = 0; row < matrix.Height; row++)
            for (int column = 0; column < matrix.Width; column++)
                if (matrix.Values[row * matrix.Width + column] == QrModuleValue.Dark)
                    foreach (int y in new[] { 1, 4 })
                        foreach (int x in new[] { 1, 4 }) image.Pixels[((row + 4) * 6 + y) * image.Stride + (column + 4) * 6 + x] = 100;
        QrDetectionOptions sampling = new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight, BinarizationMode = QrBinarizationMode.GlobalHistogram };
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrQualityReport center = analyzer.AnalyzeImageQuality(image, Detection(matrix, 4, 6), sampling, Options());
        QrQualityOptions options = Options();
        options.SamplesPerModule = 2;
        QrQualityReport aperture = analyzer.AnalyzeImageQuality(image, Detection(matrix, 4, 6), sampling, options);
        Assert.That(Metric(center, "SymbolContrast").Value, Is.EqualTo(200.0 / 255).Within(1e-12));
        Assert.That(Metric(aperture, "SymbolContrast").Value, Is.EqualTo(100.0 / 255).Within(1e-12));
        Assert.That(aperture.SampleCount - center.SampleCount, Is.EqualTo(matrix.Values.Length * 3));
    }

    [Test]
    public void BoundaryGeometryEstimatesDetectUnequalAxisScaling()
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrImage source = Render(matrix, 4, 5, 0, 255);
        QrImage stretched = new QrImage { Width = source.Width, Height = source.Height * 2, Stride = source.Stride, Pixels = new byte[source.Pixels.Length * 2] };
        for (int row = 0; row < stretched.Height; row++) Array.Copy(source.Pixels, row / 2 * source.Stride, stretched.Pixels, row * stretched.Stride, source.Stride);
        QrDetection detection = Detection(matrix, 4, 5);
        foreach (QrPoint point in new[] { detection.Corners!.TopLeft!, detection.Corners.TopRight!, detection.Corners.BottomRight!, detection.Corners.BottomLeft! }) point.Y *= 2;
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeImageQuality(stretched, detection,
            new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight, BinarizationMode = QrBinarizationMode.SuppliedBinary }, Options());
        Assert.That(Metric(report, "AxialNonUniformity").Value, Is.EqualTo(2.0 / 3).Within(1e-10));
        Assert.That(Metric(report, "AxialNonUniformity").Limitation, Does.Contain("boundary"));
        Assert.That(Metric(report, "GeometricDistortion").Value, Is.Zero.Within(1e-8));
    }

    [Test]
    public void RejectsMissingImageEvidenceAndInvalidGeometry()
    {
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrImage image = Render(matrix, 4, 5, 0, 255);
        QrDetection detection = Detection(matrix, 4, 5);
        QrDetectionOptions sampling = new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight };
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeImageQuality(null!, detection, sampling, Options()));
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeImageQuality(image, null!, sampling, Options()));
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeImageQuality(image, detection, null!, Options()));
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeImageQuality(image, detection, sampling, null!));
        detection.Corners!.TopLeft!.X = double.NaN;
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeImageQuality(image, detection, sampling, Options()));
    }

    [Test]
    public void DisabledMeasurementsAndStrictCalibrationAreHonored()
    {
        QrQualityOptions options = new QrQualityOptions
        {
            AnalyzeContrast = false, AnalyzeModulation = false, AnalyzeFixedPatterns = false,
            AnalyzeGridUniformity = false, AnalyzePrintGrowth = false, AnalyzeUnusedErrorCorrection = false,
            RequireCalibratedInputs = true
        };
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        QrQualityReport report = new QrQualityAnalyzer().AnalyzeImageQuality(Render(matrix, 4, 5, 0, 255), Detection(matrix, 4, 5),
            new QrDetectionOptions { Polarity = QrPolarity.DarkOnLight }, options);
        Assert.That(report.Metrics.Where(m => m.Name != "QuietZone").All(m => !m.IsCalculated), Is.True);
        AssertUngraded(report);
    }

    [Test]
    public void RejectsInvalidOptionsProfilesMissingInputsAndUnreadableSymbols()
    {
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        foreach (Action<QrQualityOptions> mutate in new Action<QrQualityOptions>[]
        {
            o => o.ApertureDiameterModules = double.NaN, o => o.ApertureDiameterModules = 0,
            o => o.ApertureDiameterModules = 1.1, o => o.SamplesPerModule = 0,
            o => o.SamplesPerModule = 17, o => o.MaximumModuleSamples = 0,
            o => o.SymbolKind = (QrSymbolKind)99,
            o => { o.AutomaticSymbolKind = false; o.AllowModel2 = false; }
        })
        {
            QrQualityOptions options = Options();
            mutate(options);
            Assert.Throws<QrInputException>(() => analyzer.AnalyzeMatrixQuality(matrix, options));
        }
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeMatrixQuality(null!, Options()));
        Assert.Throws<QrInputException>(() => analyzer.AnalyzeMatrixQuality(matrix, null!));
        Assert.Throws<QrDecodeException>(() => analyzer.AnalyzeMatrixQuality(matrix, new QrQualityOptions { AutomaticSymbolKind = false, SymbolKind = QrSymbolKind.Micro }));
        Array.Fill(matrix.Values, QrModuleValue.Light);
        Assert.Throws<QrDecodeException>(() => analyzer.AnalyzeMatrixQuality(matrix, Options()));
    }

    [Test]
    public void EnforcesSampleLimitsBeforeImageSamplingAndReturnsIndependentReports()
    {
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0, false);
        Assert.Throws<QrResourceLimitException>(() => analyzer.AnalyzeMatrixQuality(matrix, new QrQualityOptions { MaximumModuleSamples = 440 }));
        Assert.Throws<QrResourceLimitException>(() => analyzer.AnalyzeImageQuality(Render(matrix, 4, 5, 0, 255), Detection(matrix, 4, 5),
            new QrDetectionOptions(), new QrQualityOptions { SamplesPerModule = 16, MaximumModuleSamples = 1000 }));
        QrQualityReport first = analyzer.AnalyzeMatrixQuality(matrix, Options());
        first.Metrics[10].Value = -1;
        first.Limitations[0] = "changed";
        QrQualityReport second = analyzer.AnalyzeMatrixQuality(matrix, Options());
        Assert.That(second.Metrics[10].Value, Is.EqualTo(1));
        Assert.That(second.Limitations[0], Is.Not.EqualTo("changed"));
    }

    static QrQualityOptions Options() => new QrQualityOptions { AllowLegacyModel1 = true };
    static QrQualityMetric Metric(QrQualityReport report, string name) => report.Metrics.Single(m => m.Name == name);
    static QrMatrix Matrix(QrSymbolKind kind, int version, int level, bool quiet)
    {
        QrMatrix core = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, version, level), kind, version, level);
        return quiet ? MatrixTransformer.AddQuietZone(core, kind, -1) : core;
    }
    static QrDetection Detection(QrMatrix matrix, int quiet, int scale) => new QrDetection
    {
        EstimatedWidth = matrix.Width, EstimatedHeight = matrix.Height, Polarity = QrPolarity.DarkOnLight,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = quiet * scale, Y = quiet * scale },
            TopRight = new QrPoint { X = (quiet + matrix.Width) * scale, Y = quiet * scale },
            BottomRight = new QrPoint { X = (quiet + matrix.Width) * scale, Y = (quiet + matrix.Height) * scale },
            BottomLeft = new QrPoint { X = quiet * scale, Y = (quiet + matrix.Height) * scale }
        }
    };
    static QrImage Render(QrMatrix matrix, int quiet, int scale, byte dark, byte light)
    {
        int width = (matrix.Width + 2 * quiet) * scale, height = (matrix.Height + 2 * quiet) * scale;
        QrImage result = new QrImage { Width = width, Height = height, Stride = width, PixelFormat = QrPixelFormat.Gray8, Pixels = Enumerable.Repeat(light, width * height).ToArray() };
        for (int row = 0; row < matrix.Height; row++)
            for (int column = 0; column < matrix.Width; column++)
                for (int y = 0; y < scale; y++)
                    for (int x = 0; x < scale; x++) result.Pixels[((row + quiet) * scale + y) * width + (column + quiet) * scale + x] = matrix.Values[row * matrix.Width + column] == QrModuleValue.Dark ? dark : light;
        return result;
    }
    static void AssertUngraded(QrQualityReport report)
    {
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(report.Metrics.All(m => m.Grade == QrQualityGrade.NotGraded), Is.True);
        Assert.That(report.UsesCalibratedInputs, Is.False);
        Assert.That(report.IsVerifierCertified, Is.False);
        Assert.That(report.Limitations, Is.Not.Empty);
    }
}
