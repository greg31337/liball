namespace qrcode;

internal class QualityAnalysisPipeline
{
    // Stable metric order: contrast, modulation, fixed patterns, critical metadata,
    // axial/grid geometry, angular distortion, horizontal/vertical growth, quiet
    // zone completeness, and unused error correction.
    internal static QualityAnalysisState AnalyzeMatrix(QrMatrix matrix, QrQualityOptions options)
    {
        ValidateOptions(options);
        if (matrix == null) { throw new QrInputException("An observed matrix is required.", "matrix"); }
        int area = CheckedDimensions.CalculateArea(matrix.Width, matrix.Height, options.MaximumModuleSamples);
        CheckedDimensions.CalculateAllocationBytes(area, 256, QrLimits.DefaultMaximumAllocationBytes);
        QrDecodeOptions decoding = new QrDecodeOptions
        {
            AllowModel2 = options.AllowModel2 && (options.AutomaticSymbolKind || options.SymbolKind == QrSymbolKind.Model2),
            AllowMicro = options.AllowMicro && (options.AutomaticSymbolKind || options.SymbolKind == QrSymbolKind.Micro),
            AllowRectangularMicro = options.AllowRectangularMicro && (options.AutomaticSymbolKind || options.SymbolKind == QrSymbolKind.rMQR),
            AllowLegacyModel1 = options.AllowLegacyModel1 && (options.AutomaticSymbolKind || options.SymbolKind == QrSymbolKind.LegacyModel1),
            MaximumMatrixDimension = QrLimits.DefaultMaximumImageDimension
        };
        MatrixRecoveryResult recovery = MatrixDecodeRecovery.RecoverCodewords(matrix, decoding);
        QrDecodeResult decoded = MatrixDecodeRecovery.CreateDecodedResult(recovery, decoding);
        QrSymbol? symbol = decoded.Symbol;
        if (symbol == null || recovery.Codewords == null) { throw new QrInputException("Decoded symbol metadata is unavailable.", "matrix"); }
        // Reapply orientation to ORIGINAL observations, never to speculative
        // module repairs retained by the recovery pipeline.
        QrMatrix observed = recovery.IsInverted ? MatrixTransformer.InvertPolarity(matrix) : MatrixTransformer.Rotate(matrix, 0);
        if (recovery.IsMirrored) { observed = MatrixTransformer.Mirror(observed); }
        observed = MatrixTransformer.Rotate(observed, recovery.QuarterTurnsClockwise);
        observed.Roles = null;
        QrQualityReport report = new QrQualityReport
        {
            SymbolKind = symbol.SymbolKind, SampleCount = area,
            Metrics = new QrQualityMetric[]
            {
                Missing("SymbolContrast", "fraction"), Missing("Modulation", "fraction"),
                Missing("FixedPatternDamage", "fraction"), Missing("CriticalMetadataDamage", "fraction"),
                Missing("AxialNonUniformity", "fraction"), Missing("GridNonUniformity", "modules"),
                Missing("GeometricDistortion", "degrees"), Missing("HorizontalPrintGrowth", "modules"),
                Missing("VerticalPrintGrowth", "modules"), Missing("QuietZone", "boolean"),
                UnusedErrorCorrectionMeasurer.Measure(decoded, options).UnusedErrorCorrection
            },
            Limitations = new string[] { "Inputs are uncalibrated; ISO/IEC 15415 grades and verifier certification are unavailable." }
        };
        QualityAnalysisState state = new QualityAnalysisState { Report = report, Recovery = recovery };
        MeasureMatrixQuietZone(observed, symbol.SymbolKind, report.Metrics[9]);
        if (symbol.SymbolKind == QrSymbolKind.LegacyModel1)
        {
            report.Metrics[2].Limitation = "Legacy Model 1 reference reconstruction is unavailable.";
            report.Metrics[3].Limitation = report.Metrics[2].Limitation;
            return state;
        }
        // Matrix processing uses a table index for rMQR, while decoded public
        // metadata uses Version = 0 and explicit dimensions.
        symbol.Version = recovery.Codewords.Version;
        QrMatrix reference = new QrMatrixProcessor().BuildMatrix(symbol, 0);
        state.Reference = reference;
        symbol.Matrix = observed;
        report.Validation = MatrixValidator.ValidateSymbol(symbol);
        if (options.AnalyzeFixedPatterns)
        {
            int fixedCount = 0;
            int metadataCount = 0;
            QrModuleRole[] roles = new QrMatrixProcessor().ClassifyModules(reference, symbol.SymbolKind, symbol.Version, symbol.ErrorCorrectionLevel);
            foreach (QrModuleRole role in roles)
            {
                if (role == QrModuleRole.Format || role == QrModuleRole.Version) { metadataCount++; }
                else if ((int)role >= (int)QrModuleRole.Finder && (int)role <= (int)QrModuleRole.DarkModule) { fixedCount++; }
            }
            SetDamage(report.Metrics[2], report.Validation.FixedPatternDamageCount, fixedCount);
            SetDamage(report.Metrics[3], report.Validation.CriticalMetadataDamageCount, metadataCount);
        }
        else
        {
            report.Metrics[2].Limitation = "Fixed-pattern measurement is disabled.";
            report.Metrics[3].Limitation = report.Metrics[2].Limitation;
        }
        return state;
    }

    internal static QrQualityReport Complete(QrQualityReport report, QrQualityOptions options)
    {
        return QualityGrader.Grade(report, new QualityGradingProfile { SymbolKind = report.SymbolKind }, options);
    }

    internal static void ValidateOptions(QrQualityOptions options)
    {
        if (options == null || options.MaximumModuleSamples <= 0 || !double.IsFinite(options.ApertureDiameterModules)
            || options.ApertureDiameterModules <= 0 || options.ApertureDiameterModules > 1
            || options.SamplesPerModule < 1 || options.SamplesPerModule > 16
            || (int)options.SymbolKind < 0 || (int)options.SymbolKind > 3)
        {
            throw new QrInputException("Quality options require a positive sample limit, aperture in (0, 1], and one to sixteen samples per module axis.", "options");
        }
        bool allowed = options.AllowModel2 || options.AllowMicro || options.AllowRectangularMicro || options.AllowLegacyModel1;
        if (!options.AutomaticSymbolKind)
        {
            allowed = options.SymbolKind == QrSymbolKind.Model2 ? options.AllowModel2
                : options.SymbolKind == QrSymbolKind.Micro ? options.AllowMicro
                : options.SymbolKind == QrSymbolKind.rMQR ? options.AllowRectangularMicro : options.AllowLegacyModel1;
        }
        if (!allowed) { throw new QrInputException("At least the selected QR family must be enabled.", "SymbolKind"); }
    }

    static QrQualityMetric Missing(string name, string unit)
    {
        return new QrQualityMetric { Name = name, Unit = unit, Limitation = "The required image or reference evidence was not supplied." };
    }

    static void SetDamage(QrQualityMetric metric, int damaged, int count)
    {
        metric.IsCalculated = count > 0;
        metric.Value = count > 0 ? (double)damaged / count : 0;
        metric.Limitation = "Observed module differences from verified reconstruction; this fraction is not a calibrated fixed-pattern grade.";
    }

    static void MeasureMatrixQuietZone(QrMatrix matrix, QrSymbolKind kind, QrQualityMetric metric)
    {
        int required = kind == QrSymbolKind.Micro || kind == QrSymbolKind.rMQR ? 2 : 4;
        bool complete = matrix.QuietZoneLeft >= required && matrix.QuietZoneTop >= required
            && matrix.QuietZoneRight >= required && matrix.QuietZoneBottom >= required;
        bool light = true;
        for (int row = 0; row < matrix.Height; row++)
        {
            for (int column = 0; column < matrix.Width; column++)
            {
                bool quiet = column < matrix.QuietZoneLeft || column >= matrix.Width - matrix.QuietZoneRight
                    || row < matrix.QuietZoneTop || row >= matrix.Height - matrix.QuietZoneBottom;
                if (quiet && matrix.Values[row * matrix.Width + column] != QrModuleValue.Light) { light = false; }
            }
        }
        metric.IsCalculated = true;
        metric.Value = complete && light ? 1 : 0;
        metric.Limitation = "Checks declared matrix margins only; a missing margin does not establish the surrounding image background.";
    }
}
