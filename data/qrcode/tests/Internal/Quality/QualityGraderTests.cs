using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QualityGraderTests
{
    // Deliberately synthetic application rules exercise the engine without
    // presenting an unreviewed table as an ISO or symbology-specific policy.
    static QualityMetricRule Rule(string name = "SymbolContrast", bool higher = true) => new QualityMetricRule
    {
        Name = name, Unit = "fraction", HigherIsBetter = higher,
        Boundaries = higher ? new[] { 0.8, 0.6, 0.4, 0.2 } : new[] { 0.2, 0.4, 0.6, 0.8 }
    };

    static QrQualityMetric Metric(string name = "SymbolContrast", double value = 0.9, bool calibrated = true) => new QrQualityMetric
    {
        Name = name, Value = value, Unit = "fraction", IsCalculated = true, UsesCalibratedInputs = calibrated
    };

    static QrQualityReport Grade(QrQualityMetric[] metrics, QualityMetricRule[] rules, QrQualityOptions? options = null) => QualityGrader.Grade(
        new QrQualityReport { Metrics = metrics }, new QualityGradingProfile { Rules = rules }, options ?? new QrQualityOptions());

    static IEnumerable<TestCaseData> Boundaries()
    {
        foreach (bool higher in new[] { true, false })
            for (int index = 0; index < 4; index++)
                foreach (int side in new[] { -1, 0, 1 })
                {
                    double boundary = Rule(higher: higher).Boundaries[index];
                    double value = side < 0 ? Math.BitDecrement(boundary) : side > 0 ? Math.BitIncrement(boundary) : boundary;
                    bool worse = higher ? side < 0 : side > 0;
                    yield return new TestCaseData(higher, value, (QrQualityGrade)(4 - index - (worse ? 1 : 0)));
                }
    }

    [TestCaseSource(nameof(Boundaries))]
    public void UsesInclusiveBoundariesWithoutRoundingAcrossGradeBands(bool higher, double value, QrQualityGrade expected)
    {
        QrQualityReport report = Grade(new[] { Metric(value: value) }, new[] { Rule(higher: higher) });
        Assert.That(report.Metrics[0].Grade, Is.EqualTo(expected));
        Assert.That(report.OverallGrade, Is.EqualTo(expected));
        Assert.That(report.UsesCalibratedInputs, Is.True);
        Assert.That(report.IsVerifierCertified, Is.False);
    }

    static IEnumerable<TestCaseData> MetricBoundaries()
    {
        // These are synthetic diagnostic policies, not ISO grade thresholds.
        (string name, string unit, double minimum, double maximum, bool higher)[] definitions =
        {
            ("SymbolContrast", "fraction", 0, 1, true),
            ("Modulation", "fraction", 0, 1, true),
            ("FixedPatternDamage", "fraction", 0, 1, false),
            ("CriticalMetadataDamage", "fraction", 0, 1, false),
            ("AxialNonUniformity", "fraction", 0, 2, false),
            ("GridNonUniformity", "modules", 0, 2, false),
            ("GeometricDistortion", "degrees", 0, 90, false),
            ("HorizontalPrintGrowth", "modules", -1, 1, false),
            ("VerticalPrintGrowth", "modules", -1, 1, false),
            ("UnusedErrorCorrection", "fraction", 0, 1, true)
        };
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
            foreach (var definition in definitions)
            {
                double range = definition.maximum - definition.minimum;
                double[] boundaries = Enumerable.Range(1, 4)
                    .Select(i => definition.minimum + range * (definition.higher ? 5 - i : i) / 5).ToArray();
                for (int boundary = 0; boundary < boundaries.Length; boundary++)
                    foreach (int side in new[] { -1, 0, 1 })
                    {
                        double value = boundaries[boundary];
                        value = side < 0 ? Math.BitDecrement(value) : side > 0 ? Math.BitIncrement(value) : value;
                        bool worse = definition.higher ? side < 0 : side > 0;
                        yield return new TestCaseData(kind, definition.name, definition.unit, definition.minimum,
                            definition.maximum, definition.higher, boundaries, value,
                            (QrQualityGrade)(4 - boundary - (worse ? 1 : 0)));
                    }
            }
    }

    [TestCaseSource(nameof(MetricBoundaries))]
    public void EveryMetricAndFamilyHonorsExactAndAdjacentGradeBoundaries(QrSymbolKind kind, string name, string unit,
        double minimum, double maximum, bool higher, double[] boundaries, double value, QrQualityGrade expected)
    {
        QrQualityMetric metric = Metric(name, value);
        metric.Unit = unit;
        QualityMetricRule rule = new QualityMetricRule
        {
            Name = name, Unit = unit, MinimumValue = minimum, MaximumValue = maximum,
            HigherIsBetter = higher, Boundaries = boundaries
        };
        QrQualityReport input = new QrQualityReport { SymbolKind = kind, Metrics = new[] { metric } };
        QualityGradingProfile profile = new QualityGradingProfile { SymbolKind = kind, Rules = new[] { rule } };
        QrQualityOptions options = new QrQualityOptions { AllowLegacyModel1 = true };
        QrQualityReport result = QualityGrader.Grade(input, profile, options);
        Assert.That(result.Metrics[0].Grade, Is.EqualTo(expected));
        Assert.That(result.OverallGrade, Is.EqualTo(expected));
        Assert.That(result.IsVerifierCertified, Is.False);
        // The same value must remain ungraded when calibration is absent.
        metric.UsesCalibratedInputs = false;
        QrQualityReport uncalibrated = QualityGrader.Grade(input, profile, options);
        Assert.That(uncalibrated.Metrics[0].Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(uncalibrated.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
    }

    [TestCase(QrSymbolKind.Model2, 0, QrQualityGrade.Grade0)]
    [TestCase(QrSymbolKind.Model2, 1, QrQualityGrade.Grade4)]
    [TestCase(QrSymbolKind.Micro, 0, QrQualityGrade.Grade0)]
    [TestCase(QrSymbolKind.Micro, 1, QrQualityGrade.Grade4)]
    [TestCase(QrSymbolKind.rMQR, 0, QrQualityGrade.Grade0)]
    [TestCase(QrSymbolKind.rMQR, 1, QrQualityGrade.Grade4)]
    [TestCase(QrSymbolKind.LegacyModel1, 0, QrQualityGrade.Grade0)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, QrQualityGrade.Grade4)]
    public void QuietZonePassFailPolicyUsesOnlyItsTwoGrades(QrSymbolKind kind, double value, QrQualityGrade expected)
    {
        QrQualityMetric metric = Metric("QuietZone", value);
        metric.Unit = "boolean";
        QualityMetricRule rule = Rule("QuietZone");
        rule.Unit = "boolean";
        rule.Boundaries = new[] { 1.0, 1.0, 1.0, 1.0 };
        QrQualityReport result = QualityGrader.Grade(new QrQualityReport { SymbolKind = kind, Metrics = new[] { metric } },
            new QualityGradingProfile { SymbolKind = kind, Rules = new[] { rule } },
            new QrQualityOptions { AllowLegacyModel1 = true });
        Assert.That(result.Metrics[0].Grade, Is.EqualTo(expected));
        Assert.That(result.OverallGrade, Is.EqualTo(expected));
    }

    [TestCase(0, QrQualityGrade.Grade0)]
    [TestCase(1, QrQualityGrade.Grade4)]
    public void SupportsProfilePassFailRules(double value, QrQualityGrade expected)
    {
        QualityMetricRule rule = Rule("Decode");
        rule.Boundaries = new[] { 1.0, 1.0, 1.0, 1.0 };
        Assert.That(Grade(new[] { Metric("Decode", value) }, new[] { rule }).OverallGrade, Is.EqualTo(expected));
    }

    [Test]
    public void OverallGradeIsTheWorstRequiredMetricRegardlessOfOrder()
    {
        QrQualityMetric[] metrics = { Metric("SymbolContrast"), Metric("Modulation", 0.3), Metric("AxialNonUniformity", 0.5) };
        QualityMetricRule[] rules = { Rule("AxialNonUniformity", false), Rule("Modulation"), Rule() };
        QrQualityReport report = Grade(metrics, rules);
        Assert.That(report.Metrics.Select(m => m.Grade), Is.EqualTo(new[] { QrQualityGrade.Grade4, QrQualityGrade.Grade1, QrQualityGrade.Grade2 }));
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.Grade1));
        Assert.That(Grade(metrics.Reverse().ToArray(), rules.Reverse().ToArray()).OverallGrade, Is.EqualTo(report.OverallGrade));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingRequiredCalibrationCannotBeBypassedByOptionsOrExistingGrades(bool require)
    {
        QrQualityMetric uncalibrated = Metric(calibrated: false);
        uncalibrated.Grade = QrQualityGrade.Grade4;
        QrQualityReport report = Grade(new[] { uncalibrated }, new[] { Rule() }, new QrQualityOptions { RequireCalibratedInputs = require });
        Assert.That(report.Metrics[0].Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(report.Metrics[0].IsCalculated, Is.True);
        Assert.That(report.Metrics[0].Value, Is.EqualTo(0.9));
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(report.UsesCalibratedInputs, Is.False);
        Assert.That(report.Metrics[0].Limitation, Does.Contain("Calibrated"));
        Assert.That(uncalibrated.Grade, Is.EqualTo(QrQualityGrade.Grade4));
    }

    [Test]
    public void ExplicitDiagnosticPolicyCanGradeUncalibratedValuesUnlessStrictModeIsRequested()
    {
        QualityMetricRule rule = Rule();
        rule.RequiresCalibratedInputs = false;
        QrQualityMetric[] metrics = { Metric(calibrated: false) };
        QrQualityReport diagnostic = Grade(metrics, new[] { rule });
        Assert.That(diagnostic.OverallGrade, Is.EqualTo(QrQualityGrade.Grade4));
        Assert.That(diagnostic.UsesCalibratedInputs, Is.False);
        Assert.That(diagnostic.IsVerifierCertified, Is.False);
        Assert.That(diagnostic.Metrics[0].Limitation, Does.Contain("diagnostic"));
        Assert.That(Grade(metrics, new[] { rule }, new QrQualityOptions { RequireCalibratedInputs = true }).OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
    }

    [Test]
    public void MissingOrUncalculatedRequiredMetricPreventsPartialOverallGradeEvenWhenAnotherMetricFails()
    {
        QualityMetricRule[] rules = { Rule(), Rule("Modulation") };
        Assert.That(Grade(new[] { Metric(value: 0.1) }, rules).OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        QrQualityMetric missing = Metric("Modulation");
        missing.IsCalculated = false;
        missing.Grade = QrQualityGrade.Grade4;
        QrQualityReport result = Grade(new[] { Metric(), missing }, rules);
        Assert.That(result.Metrics[1].Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(result.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(result.Limitations.Any(l => l.Contains("Modulation")), Is.True);
    }

    [Test]
    public void EmptyProfilesAndInformationalMetricsCannotInventAnOverallGrade()
    {
        Assert.That(Grade(Array.Empty<QrQualityMetric>(), Array.Empty<QualityMetricRule>()).OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
        QrQualityMetric unknown = Metric("GeometricDistortion");
        unknown.Grade = QrQualityGrade.Grade0;
        QrQualityReport report = Grade(new[] { Metric(), unknown }, new[] { Rule() });
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.Grade4));
        Assert.That(report.Metrics[1].Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        QualityMetricRule optional = Rule();
        optional.RequiredForOverall = false;
        Assert.That(Grade(new[] { Metric() }, new[] { optional }).OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
    }

    [Test]
    public void ProfileSelectsRequiredMetricsAndThresholdsWithoutBorrowingAnotherProfilesGrades()
    {
        QrQualityMetric[] metrics = { Metric(), Metric("UnusedErrorCorrection", 0) };
        QualityMetricRule contrast = Rule();
        QualityMetricRule ecc = Rule("UnusedErrorCorrection");
        QrQualityReport correcting = Grade(metrics, new[] { contrast, ecc });
        Assert.That(correcting.OverallGrade, Is.EqualTo(QrQualityGrade.Grade0));
        // A detection-only profile can omit the correcting-capacity requirement.
        ecc.RequiredForOverall = false;
        contrast.Boundaries = new[] { 1.0, 0.95, 0.85, 0.75 };
        QrQualityReport detectionOnly = Grade(metrics, new[] { contrast, ecc });
        Assert.That(detectionOnly.OverallGrade, Is.EqualTo(QrQualityGrade.Grade2));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    [TestCase(QrSymbolKind.LegacyModel1)]
    public void RequiresMatchingEnabledFamily(QrSymbolKind kind)
    {
        QrQualityReport input = new QrQualityReport { SymbolKind = kind, Metrics = new[] { Metric() } };
        QualityGradingProfile profile = new QualityGradingProfile { SymbolKind = kind, Rules = new[] { Rule() } };
        QrQualityOptions options = new QrQualityOptions { AllowLegacyModel1 = true };
        Assert.That(QualityGrader.Grade(input, profile, options).OverallGrade, Is.EqualTo(QrQualityGrade.Grade4));
        options.AutomaticSymbolKind = false;
        options.SymbolKind = kind;
        Assert.That(QualityGrader.Grade(input, profile, options).SymbolKind, Is.EqualTo(kind));
        options.SymbolKind = (QrSymbolKind)(((int)kind + 1) % 4);
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, options));
        options.AutomaticSymbolKind = true;
        profile.SymbolKind = options.SymbolKind;
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, options));
        profile.SymbolKind = kind;
        options.AllowModel2 = options.AllowMicro = options.AllowRectangularMicro = options.AllowLegacyModel1 = false;
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, options));
    }

    [TestCase("SymbolContrast")]
    [TestCase("Modulation")]
    [TestCase("FixedPatternDamage")]
    [TestCase("CriticalMetadataDamage")]
    [TestCase("AxialNonUniformity")]
    [TestCase("GridNonUniformity")]
    [TestCase("GeometricDistortion")]
    [TestCase("HorizontalPrintGrowth")]
    [TestCase("VerticalPrintGrowth")]
    [TestCase("UnusedErrorCorrection")]
    public void DisablingRequiredMeasurementPreventsOverallGrade(string name)
    {
        QrQualityOptions options = new QrQualityOptions
        {
            AnalyzeContrast = false, AnalyzeModulation = false, AnalyzeFixedPatterns = false,
            AnalyzeGridUniformity = false, AnalyzePrintGrowth = false, AnalyzeUnusedErrorCorrection = false
        };
        QrQualityReport report = Grade(new[] { Metric(name) }, new[] { Rule(name) }, options);
        Assert.That(report.Metrics[0].Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
    }

    [Test]
    public void RetainsObservationDataAndLimitationsWithoutMutatingCallerOwnedReports()
    {
        QrQualityMetric metric = Metric();
        metric.Limitation = "Original limitation.";
        QrValidationResult validation = new QrValidationResult { IsConformant = true };
        QrQualityReport input = new QrQualityReport
        {
            Metrics = new[] { metric }, Limitations = new[] { "Original report limitation." }, SampleCount = 12,
            Validation = validation, OverallGrade = QrQualityGrade.Grade0, IsVerifierCertified = true
        };
        QrQualityReport result = QualityGrader.Grade(input, new QualityGradingProfile { Rules = new[] { Rule() } }, new QrQualityOptions());
        Assert.That(result.Metrics, Is.Not.SameAs(input.Metrics));
        Assert.That(result.Metrics[0], Is.Not.SameAs(metric));
        Assert.That(result.Metrics[0].Limitation, Is.EqualTo(metric.Limitation));
        Assert.That(result.SampleCount, Is.EqualTo(12));
        Assert.That(result.Validation, Is.SameAs(validation));
        Assert.That(result.Limitations, Does.Contain("Original report limitation."));
        Assert.That(result.IsVerifierCertified, Is.False);
        Assert.That(input.IsVerifierCertified, Is.True);
        Assert.That(input.OverallGrade, Is.EqualTo(QrQualityGrade.Grade0));
        Assert.That(metric.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
        result.Metrics[0].Value = 0;
        result.Limitations[0] = "changed";
        Assert.That(metric.Value, Is.EqualTo(0.9));
        Assert.That(input.Limitations[0], Is.EqualTo("Original report limitation."));
    }

    [Test]
    public void RealUncalibratedMeasurementsStayCalculatedButUngraded()
    {
        GrayscaleQualityMeasurement measurement = GrayscaleQualityMeasurer.Measure(
            new QrRaster { Width = 2, Height = 2, Stride = 2, Pixels = new byte[] { 0, 255, 0, 255 } }, new QrQualityOptions());
        QrQualityReport report = Grade(new[] { measurement.SymbolContrast, measurement.Modulation }, new[] { Rule(), Rule("Modulation") });
        Assert.That(report.Metrics.All(m => m.IsCalculated), Is.True);
        Assert.That(report.Metrics.All(m => m.Grade == QrQualityGrade.NotGraded), Is.True);
        Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
    }

    [Test]
    public void RejectsAmbiguousOrMalformedMeasurements()
    {
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.01, 1.01 })
            Assert.Throws<QrInputException>(() => Grade(new[] { Metric(value: value) }, new[] { Rule() }));
        QrQualityMetric wrongUnit = Metric();
        wrongUnit.Unit = "percent";
        Assert.Throws<QrInputException>(() => Grade(new[] { wrongUnit }, new[] { Rule() }));
        Assert.Throws<QrInputException>(() => Grade(new[] { Metric(), Metric() }, new[] { Rule() }));
        Assert.Throws<QrInputException>(() => Grade(new[] { Metric("") }, new[] { Rule() }));
        Assert.Throws<QrInputException>(() => Grade(new QrQualityMetric[] { null! }, new[] { Rule() }));
    }

    [Test]
    public void RejectsMalformedOrAmbiguousProfileRules()
    {
        Action<QualityMetricRule>[] mutations =
        {
            r => r.Name = "", r => r.Boundaries = null!, r => r.Boundaries = new[] { 0.5 },
            r => r.Boundaries[0] = double.NaN, r => r.Boundaries[0] = 1.1,
            r => r.Boundaries[2] = 0.9, r => r.HigherIsBetter = false,
            r => r.MinimumValue = double.NegativeInfinity, r => r.MaximumValue = double.PositiveInfinity,
            r => r.MinimumValue = 2, r => r.MinimumValue = 1
        };
        foreach (Action<QualityMetricRule> mutate in mutations)
        {
            QualityMetricRule rule = Rule();
            mutate(rule);
            Assert.Throws<QrInputException>(() => Grade(new[] { Metric() }, new[] { rule }));
        }
        Assert.Throws<QrInputException>(() => Grade(new[] { Metric() }, new[] { Rule(), Rule() }));
        Assert.Throws<QrInputException>(() => Grade(new[] { Metric() }, new QualityMetricRule[] { null! }));
    }

    [Test]
    public void ValidatesRequiredContainersAndSampleLimits()
    {
        QrQualityReport input = new QrQualityReport { Metrics = new[] { Metric() } };
        QualityGradingProfile profile = new QualityGradingProfile { Rules = new[] { Rule() } };
        QrQualityOptions options = new QrQualityOptions { MaximumModuleSamples = 4 };
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(null!, profile, options));
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, null!, options));
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, null!));
        Assert.Throws<QrInputException>(() => Grade(null!, new[] { Rule() }));
        Assert.Throws<QrInputException>(() => Grade(new[] { Metric() }, null!));
        input.SampleCount = 4;
        Assert.That(QualityGrader.Grade(input, profile, options).SampleCount, Is.EqualTo(4));
        input.SampleCount = 5;
        Assert.Throws<QrResourceLimitException>(() => QualityGrader.Grade(input, profile, options));
        input.SampleCount = -1;
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, options));
        input.SampleCount = 0;
        options.MaximumModuleSamples = 0;
        Assert.Throws<QrInputException>(() => QualityGrader.Grade(input, profile, options));
    }
}
