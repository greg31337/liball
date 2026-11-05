using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrQualityAnalyzerBoundaryTests
{
    static readonly IQrQualityAnalyzer Analyzer = new QrQualityAnalyzer();

    [TestCase(QrSymbolKind.Model2, 7, 3)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    public void ReportsExactDamageFractionsForEachFixedAndMetadataRole(QrSymbolKind kind, int version, int level)
    {
        QrMatrix pristine = Matrix(kind, version, level);
        QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, level);
        int fixedCount = roles.Count(IsFixed);
        int metadataCount = roles.Count(r => r is QrModuleRole.Format or QrModuleRole.Version);
        foreach (QrModuleRole role in roles.Distinct().Where(r => IsFixed(r) || r is QrModuleRole.Format or QrModuleRole.Version))
        {
            QrMatrix damaged = Matrix(kind, version, level);
            int cell = Array.IndexOf(roles, role);
            damaged.Values[cell] = damaged.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
            QrModuleValue[] before = (QrModuleValue[])damaged.Values.Clone();
            QrQualityReport report = Analyzer.AnalyzeMatrixQuality(damaged, Options());
            bool fixedRole = IsFixed(role);
            AssertMetric(report, "FixedPatternDamage", fixedRole ? 1.0 / fixedCount : 0, "fraction");
            AssertMetric(report, "CriticalMetadataDamage", fixedRole ? 0 : 1.0 / metadataCount, "fraction");
            AssertMetric(report, "UnusedErrorCorrection", 1, "fraction");
            Assert.That(report.Validation!.FixedPatternDamageCount, Is.EqualTo(fixedRole ? 1 : 0), role.ToString());
            Assert.That(report.Validation.CriticalMetadataDamageCount, Is.EqualTo(fixedRole ? 0 : 1), role.ToString());
            Assert.That(damaged.Values, Is.EqualTo(before));
            AssertUngraded(report);
        }
        QrQualityReport clean = Analyzer.AnalyzeMatrixQuality(pristine, Options());
        AssertMetric(clean, "FixedPatternDamage", 0, "fraction");
        AssertMetric(clean, "CriticalMetadataDamage", 0, "fraction");
    }

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 7, 3)]
    public void MeasuresWorstBlockAtEveryErrorCountAndAtMixedErasureCapacity(QrSymbolKind kind, int version, int level)
    {
        int capacity = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        List<int> positions = CodewordErasureRecoveryTests.BlockPositions(kind, version, level)[0];
        for (int errors = 0; errors <= capacity / 2; errors++)
            foreach (bool exhaust in new[] { false, true })
            {
                int erasures = exhaust ? capacity - 2 * errors : 0;
                QrMatrix matrix = Matrix(kind, version, level);
                matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
                for (int word = 0; word < errors + erasures; word++)
                    foreach (int cell in CodewordErasureRecoveryTests.Cells(kind, version, level, positions[word]))
                    {
                        matrix.Values[cell] = matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
                        if (word >= errors) matrix.Confidence[cell] = 0;
                    }
                QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
                QrQualityReport report = Analyzer.AnalyzeMatrixQuality(matrix, Options());
                AssertMetric(report, "UnusedErrorCorrection", (capacity - 2.0 * errors - erasures) / capacity, "fraction");
                Assert.That(matrix.Values, Is.EqualTo(before));
                AssertUngraded(report);
            }
    }

    [TestCase(QrSymbolKind.Model2, 1, 0, 4)]
    [TestCase(QrSymbolKind.Micro, 3, 1, 2)]
    [TestCase(QrSymbolKind.rMQR, 4, 0, 2)]
    [TestCase(QrSymbolKind.LegacyModel1, 2, 0, 4)]
    public void QuietZoneRequiresEveryMarginAtTheProfileMinimum(QrSymbolKind kind, int version, int level, int required)
    {
        QrMatrix core = Matrix(kind, version, level);
        foreach (int margin in new[] { required - 1, required, required + 1 })
        {
            QrMatrix matrix = MatrixTransformer.AddQuietZone(core, kind, margin);
            AssertMetric(Analyzer.AnalyzeMatrixQuality(matrix, Options()), "QuietZone", margin >= required ? 1 : 0, "boolean");
            QrImage image = Render(core, margin, 6, 0, 255);
            QrQualityReport report = AnalyzeImage(image, Detection(core, margin, 6));
            QrQualityMetric quiet = Metric(report, "QuietZone");
            Assert.That(quiet.IsCalculated, Is.EqualTo(margin >= required));
            Assert.That(quiet.Value, Is.EqualTo(margin >= required ? 1 : 0));
            AssertUngraded(report);
        }
        foreach (int side in Enumerable.Range(0, 4))
            foreach (QrModuleValue contamination in new[] { QrModuleValue.Dark, QrModuleValue.Unknown })
            {
                QrMatrix matrix = MatrixTransformer.AddQuietZone(core, kind, required);
                int cell = side switch
                {
                    0 => matrix.Width / 2,
                    1 => matrix.Width * (matrix.Height / 2),
                    2 => matrix.Width * (matrix.Height / 2) + matrix.Width - 1,
                    _ => matrix.Width * (matrix.Height - 1) + matrix.Width / 2
                };
                matrix.Values[cell] = contamination;
                AssertMetric(Analyzer.AnalyzeMatrixQuality(matrix, Options()), "QuietZone", 0, "boolean");
            }
    }

    [TestCase(0)]
    [TestCase(49)]
    [TestCase(99)]
    [TestCase(100)]
    [TestCase(101)]
    [TestCase(151)]
    [TestCase(200)]
    public void ModulationTracksTheWeakestModuleOnBothSidesOfTheThreshold(int luminance)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrImage image = Render(matrix, 4, 6, 0, 200);
        int cell = CodewordErasureRecoveryTests.Cells(QrSymbolKind.Model2, 1, 0, 0)[0];
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 6; x++)
                image.Pixels[((cell / matrix.Width + 4) * 6 + y) * image.Stride + (cell % matrix.Width + 4) * 6 + x] = (byte)luminance;
        QrQualityReport report = AnalyzeImage(image, Detection(matrix, 4, 6));
        AssertMetric(report, "SymbolContrast", 200.0 / 255, "fraction");
        AssertMetric(report, "Modulation", Math.Abs(luminance - 100) / 100.0, "fraction");
        AssertUngraded(report);
    }

    [TestCase(0, 255)]
    [TestCase(40, 200)]
    [TestCase(100, 160)]
    [TestCase(255, 0)]
    [TestCase(200, 40)]
    public void ContrastUsesOriginalIntensityRangeRegardlessOfPolarity(byte dark, byte light)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrDetection detection = Detection(matrix, 4, 6);
        detection.Polarity = dark > light ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight;
        QrQualityReport report = AnalyzeImage(Render(matrix, 4, 6, dark, light), detection);
        AssertMetric(report, "SymbolContrast", Math.Abs(dark - light) / 255.0, "fraction");
        AssertMetric(report, "Modulation", 1, "fraction");
        AssertUngraded(report);
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    public void PrintGrowthPreservesSignedChangesInBothImageAxes(int growthX, int growthY)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrImage image = Render(matrix, 5, 10, 0, 255);
        ApplyGrowth(image, growthX, true);
        ApplyGrowth(image, growthY, false);
        QrQualityReport report = AnalyzeImage(image, Detection(matrix, 5, 10));
        AssertMetric(report, "HorizontalPrintGrowth", growthX / 5.0, "modules", 0.001);
        AssertMetric(report, "VerticalPrintGrowth", growthY / 5.0, "modules", 0.001);
        AssertUngraded(report);
    }

    [TestCase(1, 0)]
    [TestCase(2, 0)]
    [TestCase(1, 1)]
    public void GeometryMeasuresIndependentAxisStretchAndSkewWithoutInventingGridEvidence(int stretchY, int shear)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrImage source = Render(matrix, 4, 6, 0, 255);
        int width = source.Width + shear * source.Height;
        QrImage image = new QrImage { Width = width, Height = source.Height * stretchY, Stride = width,
            Pixels = Enumerable.Repeat((byte)255, width * source.Height * stretchY).ToArray() };
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < source.Width; x++)
                image.Pixels[y * width + x + shear * (y / stretchY)] = source.Pixels[y / stretchY * source.Stride + x];
        QrDetection detection = Detection(matrix, 4, 6);
        foreach (QrPoint corner in new[] { detection.Corners!.TopLeft!, detection.Corners.TopRight!, detection.Corners.BottomRight!, detection.Corners.BottomLeft! })
        {
            corner.X += shear * corner.Y;
            corner.Y *= stretchY;
        }
        QrQualityReport report = AnalyzeImage(image, detection);
        double vertical = Math.Sqrt(stretchY * stretchY + shear * shear);
        AssertMetric(report, "AxialNonUniformity", 2 * (vertical - 1) / (vertical + 1), "fraction");
        AssertMetric(report, "GeometricDistortion", Math.Atan2(shear, stretchY) * 180 / Math.PI, "degrees", 1e-8);
        Assert.That(Metric(report, "GridNonUniformity").IsCalculated, Is.False);
        Assert.That(Metric(report, "GridNonUniformity").Limitation, Does.Contain("Independent module centers"));
        AssertUngraded(report);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CalibrationPolicyCannotTurnMatrixOrImageEvidenceIntoGrades(bool requireCalibration)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrQualityOptions options = Options();
        options.RequireCalibratedInputs = requireCalibration;
        AssertUngraded(Analyzer.AnalyzeMatrixQuality(matrix, options));
        AssertUngraded(AnalyzeImage(Render(matrix, 4, 6, 0, 255), Detection(matrix, 4, 6), options));
    }

    [TestCase("AnalyzeContrast", new[] { "SymbolContrast" })]
    [TestCase("AnalyzeModulation", new[] { "Modulation" })]
    [TestCase("AnalyzeFixedPatterns", new[] { "FixedPatternDamage", "CriticalMetadataDamage" })]
    [TestCase("AnalyzeGridUniformity", new[] { "AxialNonUniformity", "GridNonUniformity", "GeometricDistortion" })]
    [TestCase("AnalyzePrintGrowth", new[] { "HorizontalPrintGrowth", "VerticalPrintGrowth" })]
    [TestCase("AnalyzeUnusedErrorCorrection", new[] { "UnusedErrorCorrection" })]
    public void DisablingOneMeasurementPreservesAllOtherResults(string field, string[] disabled)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrImage image = Render(matrix, 4, 6, 0, 255);
        QrDetection detection = Detection(matrix, 4, 6);
        QrQualityOptions options = Options();
        typeof(QrQualityOptions).GetField(field)!.SetValue(options, false);
        foreach (bool fromImage in new[] { false, true })
        {
            QrQualityReport baseline = fromImage ? AnalyzeImage(image, detection) : Analyzer.AnalyzeMatrixQuality(matrix, Options());
            QrQualityReport report = fromImage ? AnalyzeImage(image, detection, options) : Analyzer.AnalyzeMatrixQuality(matrix, options);
            foreach (QrQualityMetric metric in report.Metrics)
            {
                if (disabled.Contains(metric.Name)) Assert.That(metric.IsCalculated, Is.False, metric.Name);
                else
                {
                    Assert.That(metric.IsCalculated, Is.EqualTo(Metric(baseline, metric.Name).IsCalculated), metric.Name);
                    Assert.That(metric.Value, Is.EqualTo(Metric(baseline, metric.Name).Value), metric.Name);
                }
            }
            AssertUngraded(report);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SampleLimitAcceptsExactUsageAndRejectsOneLess(bool fromImage)
    {
        QrMatrix matrix = Matrix(QrSymbolKind.Model2, 1, 0);
        QrImage image = Render(matrix, 4, 6, 0, 255);
        QrDetection detection = Detection(matrix, 4, 6);
        QrQualityOptions options = Options();
        QrQualityReport Analyze() => fromImage ? AnalyzeImage(image, detection, options) : Analyzer.AnalyzeMatrixQuality(matrix, options);
        int count = Analyze().SampleCount;
        options.MaximumModuleSamples = count;
        Assert.That(Analyze().SampleCount, Is.EqualTo(count));
        options.MaximumModuleSamples = count - 1;
        Assert.Throws<QrResourceLimitException>(() => Analyze());
    }

    static bool IsFixed(QrModuleRole role) => role is QrModuleRole.Finder or QrModuleRole.Separator
        or QrModuleRole.Timing or QrModuleRole.Alignment or QrModuleRole.DarkModule;
    static QrQualityOptions Options() => new QrQualityOptions { AllowLegacyModel1 = true };
    static QrMatrix Matrix(QrSymbolKind kind, int version, int level) =>
        CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, version, level), kind, version, level);
    static QrQualityMetric Metric(QrQualityReport report, string name) => report.Metrics.Single(m => m.Name == name);
    static void AssertMetric(QrQualityReport report, string name, double value, string unit, double tolerance = 1e-10)
    {
        QrQualityMetric metric = Metric(report, name);
        Assert.That(metric.IsCalculated, Is.True, name);
        Assert.That(metric.Value, Is.EqualTo(value).Within(tolerance), name);
        Assert.That(metric.Unit, Is.EqualTo(unit), name);
    }
    static void AssertUngraded(QrQualityReport report)
    {
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(report.UsesCalibratedInputs, Is.False);
        Assert.That(report.IsVerifierCertified, Is.False);
        foreach (QrQualityMetric metric in report.Metrics)
        {
            Assert.That(metric.Grade, Is.EqualTo(QrQualityGrade.NotGraded), metric.Name);
            Assert.That(metric.UsesCalibratedInputs, Is.False, metric.Name);
            Assert.That(metric.Limitation, Is.Not.Null.And.Not.Empty, metric.Name);
        }
    }
    static QrQualityReport AnalyzeImage(QrImage image, QrDetection detection, QrQualityOptions? options = null) =>
        Analyzer.AnalyzeImageQuality(image, detection, new QrDetectionOptions
        {
            Polarity = detection.Polarity, BinarizationMode = QrBinarizationMode.GlobalHistogram
        }, options ?? Options());
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
        QrImage image = new QrImage { Width = width, Height = height, Stride = width, Pixels = Enumerable.Repeat(light, width * height).ToArray() };
        for (int y = 0; y < matrix.Height * scale; y++)
            for (int x = 0; x < matrix.Width * scale; x++)
                image.Pixels[(y + quiet * scale) * width + x + quiet * scale] = matrix.Values[y / scale * matrix.Width + x / scale] == QrModuleValue.Dark ? dark : light;
        return image;
    }
    static void ApplyGrowth(QrImage image, int growth, bool horizontal)
    {
        if (growth == 0) return;
        byte[] before = (byte[])image.Pixels.Clone();
        int step = horizontal ? 1 : image.Stride;
        for (int y = 1; y < image.Height - 1; y++)
            for (int x = 1; x < image.Width - 1; x++)
            {
                int cell = y * image.Stride + x;
                image.Pixels[cell] = growth > 0
                    ? Math.Min(before[cell], Math.Min(before[cell - step], before[cell + step]))
                    : Math.Max(before[cell], Math.Max(before[cell - step], before[cell + step]));
            }
    }
}
