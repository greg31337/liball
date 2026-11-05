using System.Collections.Generic;

namespace qrcode;

internal class QualityGrader
{
    // Grade a single observation. Combining scan grades or claiming verifier
    // certification requires evidence not represented by this report.
    internal static QrQualityReport Grade(QrQualityReport measurements, QualityGradingProfile profile, QrQualityOptions options)
    {
        ValidateInput(measurements, profile, options);
        Dictionary<string, QualityMetricRule> rules = new Dictionary<string, QualityMetricRule>(global::System.StringComparer.Ordinal);
        foreach (QualityMetricRule rule in profile.Rules)
        {
            ValidateRule(rule);
            if (!rules.TryAdd(rule.Name, rule)) { throw new QrInputException("Metric rules must have unique names.", "Rules"); }
        }
        QrQualityReport result = new QrQualityReport
        {
            SymbolKind = measurements.SymbolKind,
            SampleCount = measurements.SampleCount,
            Validation = measurements.Validation,
            Metrics = new QrQualityMetric[measurements.Metrics.Length]
        };
        List<string> limitations = new List<string>(measurements.Limitations);
        limitations.Add("Grades apply only to the supplied observation and explicit profile rules; this report does not certify ISO verifier conformance.");
        Dictionary<string, QrQualityMetric> graded = new Dictionary<string, QrQualityMetric>(global::System.StringComparer.Ordinal);
        bool allCalibrated = true;
        int gradedCount = 0;
        for (int index = 0; index < measurements.Metrics.Length; index++)
        {
            QrQualityMetric source = measurements.Metrics[index];
            if (source == null || string.IsNullOrEmpty(source.Name) || source.IsCalculated && !double.IsFinite(source.Value))
            {
                throw new QrInputException("Named metrics with finite calculated values are required.", "Metrics");
            }
            QrQualityMetric metric = CopyMetric(source);
            if (!graded.TryAdd(metric.Name, metric)) { throw new QrInputException("Measurements must have unique names.", "Metrics"); }
            result.Metrics[index] = metric;
            if (!rules.ContainsKey(metric.Name))
            {
                AddLimitation(metric, "No grading rule is available for this metric in the selected profile.");
                continue;
            }
            QualityMetricRule rule = rules[metric.Name];
            GradeMetric(metric, rule, options);
            if (metric.Grade != QrQualityGrade.NotGraded)
            {
                gradedCount++;
                if (!metric.UsesCalibratedInputs) { allCalibrated = false; }
            }
        }
        result.UsesCalibratedInputs = gradedCount > 0 && allCalibrated;
        QrQualityGrade minimum = QrQualityGrade.Grade4;
        int requiredCount = 0;
        bool complete = true;
        foreach (QualityMetricRule rule in profile.Rules)
        {
            if (!rule.RequiredForOverall) { continue; }
            requiredCount++;
            if (!graded.ContainsKey(rule.Name) || graded[rule.Name].Grade == QrQualityGrade.NotGraded)
            {
                complete = false;
                limitations.Add($"The overall grade requires a graded {rule.Name} measurement.");
            }
            else if ((int)graded[rule.Name].Grade < (int)minimum) { minimum = graded[rule.Name].Grade; }
        }
        if (requiredCount == 0) { limitations.Add("The profile defines no required metrics; no overall grade can be established."); }
        else if (complete) { result.OverallGrade = minimum; }
        result.Limitations = limitations.ToArray();
        return result;
    }

    static void GradeMetric(QrQualityMetric metric, QualityMetricRule rule, QrQualityOptions options)
    {
        if (!IsEnabled(metric.Name, options))
        {
            AddLimitation(metric, "Grading is disabled for this metric.");
            return;
        }
        if (!metric.IsCalculated)
        {
            AddLimitation(metric, "No calculated measurement is available for grading.");
            return;
        }
        if (metric.Unit != rule.Unit || metric.Value < rule.MinimumValue || metric.Value > rule.MaximumValue)
        {
            throw new QrInputException("The measurement unit or value does not match its grading rule.", metric.Name);
        }
        if ((rule.RequiresCalibratedInputs || options.RequireCalibratedInputs) && !metric.UsesCalibratedInputs)
        {
            AddLimitation(metric, "Calibrated inputs required by this grading policy are absent.");
            return;
        }
        metric.Grade = QrQualityGrade.Grade0;
        for (int index = 0; index < rule.Boundaries.Length; index++)
        {
            double boundary = rule.Boundaries[index];
            if (rule.HigherIsBetter ? metric.Value >= boundary : metric.Value <= boundary)
            {
                metric.Grade = (QrQualityGrade)(4 - index);
                break;
            }
        }
        if (!metric.UsesCalibratedInputs) { AddLimitation(metric, "An uncalibrated diagnostic grade is permitted by this profile; it is not an ISO verifier grade."); }
    }

    static bool IsEnabled(string name, QrQualityOptions options)
    {
        switch (name)
        {
            case "SymbolContrast": return options.AnalyzeContrast;
            case "Modulation": return options.AnalyzeModulation;
            case "FixedPatternDamage": return options.AnalyzeFixedPatterns;
            case "CriticalMetadataDamage": return options.AnalyzeFixedPatterns;
            case "AxialNonUniformity": return options.AnalyzeGridUniformity;
            case "GridNonUniformity": return options.AnalyzeGridUniformity;
            case "GeometricDistortion": return options.AnalyzeGridUniformity;
            case "HorizontalPrintGrowth": return options.AnalyzePrintGrowth;
            case "VerticalPrintGrowth": return options.AnalyzePrintGrowth;
            case "UnusedErrorCorrection": return options.AnalyzeUnusedErrorCorrection;
            default: return true;
        }
    }

    static QrQualityMetric CopyMetric(QrQualityMetric source)
    {
        // Always discard incoming grades so stale or caller-populated grades
        // cannot bypass recalculation or calibration requirements.
        return new QrQualityMetric
        {
            Name = source.Name, Value = source.Value, IsCalculated = source.IsCalculated,
            UsesCalibratedInputs = source.UsesCalibratedInputs, Unit = source.Unit, Limitation = source.Limitation
        };
    }

    static void AddLimitation(QrQualityMetric metric, string limitation)
    {
        metric.Limitation = string.IsNullOrEmpty(metric.Limitation) ? limitation : metric.Limitation + " " + limitation;
    }

    static void ValidateRule(QualityMetricRule rule)
    {
        if (rule == null || string.IsNullOrEmpty(rule.Name) || rule.Boundaries == null || rule.Boundaries.Length != 4
            || !double.IsFinite(rule.MinimumValue) || !double.IsFinite(rule.MaximumValue) || rule.MinimumValue >= rule.MaximumValue)
        {
            throw new QrInputException("A named metric rule with a finite value range and four grade boundaries is required.", "Rules");
        }
        for (int index = 0; index < rule.Boundaries.Length; index++)
        {
            double boundary = rule.Boundaries[index];
            if (!double.IsFinite(boundary) || boundary < rule.MinimumValue || boundary > rule.MaximumValue
                || index > 0 && (rule.HigherIsBetter ? boundary > rule.Boundaries[index - 1] : boundary < rule.Boundaries[index - 1]))
            {
                throw new QrInputException("Grade boundaries must be finite, in range, and ordered from best to worst.", "Boundaries");
            }
        }
    }

    static void ValidateInput(QrQualityReport measurements, QualityGradingProfile profile, QrQualityOptions options)
    {
        if (measurements == null || profile == null || options == null || measurements.Metrics == null
            || measurements.Limitations == null || profile.Rules == null)
        {
            throw new QrInputException("Measurements, profile rules, and quality options are required.", "measurements");
        }
        bool allowed = false;
        switch (measurements.SymbolKind)
        {
            case QrSymbolKind.Model2: allowed = options.AllowModel2; break;
            case QrSymbolKind.Micro: allowed = options.AllowMicro; break;
            case QrSymbolKind.rMQR: allowed = options.AllowRectangularMicro; break;
            case QrSymbolKind.LegacyModel1: allowed = options.AllowLegacyModel1; break;
        }
        if (!allowed || profile.SymbolKind != measurements.SymbolKind
            || !options.AutomaticSymbolKind && options.SymbolKind != measurements.SymbolKind)
        {
            throw new QrInputException("The selected grading profile does not match an enabled symbol family.", "SymbolKind");
        }
        if (options.MaximumModuleSamples <= 0 || measurements.SampleCount < 0)
        {
            throw new QrInputException("Sample counts must be nonnegative and their limit must be positive.", "SampleCount");
        }
        if (measurements.SampleCount > options.MaximumModuleSamples)
        {
            throw new QrResourceLimitException("The sample count exceeds the quality limit.", "MaximumModuleSamples", measurements.SampleCount, options.MaximumModuleSamples);
        }
        int count = CheckedDimensions.CalculateSum(measurements.Metrics.Length, profile.Rules.Length, int.MaxValue, "Metrics");
        count = CheckedDimensions.CalculateSum(count, measurements.Limitations.Length, int.MaxValue, "Limitations");
        CheckedDimensions.CalculateAllocationBytes(count, 512, QrLimits.DefaultMaximumAllocationBytes);
    }
}
