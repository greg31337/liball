using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicOptionDefaultsTests
{
    [Test]
    public void EncodeDefaultsCoverStandardEmitProfilesButExcludeLegacyModel1()
    {
        QrEncodeOptions options = new QrEncodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowModel2, Is.True);
            Assert.That(options.AllowMicro, Is.True);
            Assert.That(options.AllowRectangularMicro, Is.True);
            Assert.That(options.AllowLegacyModel1, Is.False);
            Assert.That(options.ForceSymbolKind, Is.False);
            Assert.That(options.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(options.MinimumVersion, Is.EqualTo(1));
            Assert.That(options.MaximumVersion, Is.EqualTo(40));
            Assert.That(options.ForceErrorCorrectionLevel, Is.False);
            Assert.That(options.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(options.BoostErrorCorrectionLevel, Is.True);
            Assert.That(options.MaskPattern, Is.EqualTo(QrMaskPattern.Automatic));
            Assert.That(options.OptimizeSegments, Is.True);
        });
    }

    [Test]
    public void EncodeDefaultsUseProtocolSentinelsAndBoundStructuredAppend()
    {
        QrEncodeOptions options = new QrEncodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.EciAssignment, Is.EqualTo(-1));
            Assert.That(options.Fnc1SecondApplicationIndicator, Is.EqualTo(-1));
            Assert.That(options.QuietZoneModules, Is.EqualTo(-1));
            Assert.That(options.UseFnc1First, Is.False);
            Assert.That(options.EnableHanziCompatibility, Is.False);
            Assert.That(options.MaximumInputBytes, Is.GreaterThan(0));
            Assert.That(options.MaximumSegments, Is.GreaterThan(0));
            Assert.That(options.MaximumStructuredAppendSymbols, Is.EqualTo(16));
        });
    }

    [Test]
    public void DecodeDefaultsRecognizeEverySupportedFamilyAndPreserveRawPayloads()
    {
        QrDecodeOptions options = new QrDecodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowModel2, Is.True);
            Assert.That(options.AllowMicro, Is.True);
            Assert.That(options.AllowRectangularMicro, Is.True);
            Assert.That(options.AllowLegacyModel1, Is.True);
            Assert.That(options.Strict, Is.True);
            Assert.That(options.Polarity, Is.EqualTo(QrPolarity.Automatic));
            Assert.That(options.TryMirrored, Is.True);
            Assert.That(options.TryInverted, Is.True);
            Assert.That(options.EnableRecovery, Is.True);
            Assert.That(options.PreserveUnsupportedEciBytes, Is.True);
            Assert.That(options.EnableByteEncodingInference, Is.False);
            Assert.That(options.RequireExplicitEci, Is.False);
            Assert.That(options.EnableHanziCompatibility, Is.False);
            Assert.That(options.MaximumMatrixDimension, Is.EqualTo(177));
            Assert.That(options.MaximumRecoveryAttempts, Is.GreaterThan(0));
            Assert.That(options.MaximumAllocationBytes, Is.GreaterThan(0));
        });
    }

    [Test]
    public void DetectionDefaultsUseAutomaticInputHandlingAndBoundedSearch()
    {
        QrDetectionOptions options = new QrDetectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowModel2, Is.True);
            Assert.That(options.AllowMicro, Is.True);
            Assert.That(options.AllowRectangularMicro, Is.True);
            Assert.That(options.AllowLegacyModel1, Is.True);
            Assert.That(options.BinarizationMode, Is.EqualTo(QrBinarizationMode.Automatic));
            Assert.That(options.Polarity, Is.EqualTo(QrPolarity.Automatic));
            Assert.That(options.EnableDenoise, Is.False);
            Assert.That(options.EnableGapClosing, Is.False);
            Assert.That(options.NormalizeContrast, Is.False);
            Assert.That(options.AllowCroppedQuietZone, Is.True);
            Assert.That(options.DetectMultiple, Is.True);
            Assert.That(options.UseMultiPointSampling, Is.True);
            Assert.That(options.RefineGrid, Is.True);
            Assert.That(options.MinimumModuleSize, Is.EqualTo(1));
            Assert.That(options.MaximumCandidates, Is.GreaterThan(0));
            Assert.That(options.MaximumImageWidth, Is.GreaterThan(0));
            Assert.That(options.MaximumImageHeight, Is.GreaterThan(0));
            Assert.That(options.MaximumImagePixels, Is.GreaterThan(0));
            Assert.That(options.MaximumPyramidLevels, Is.GreaterThan(0));
        });
    }

    [Test]
    public void RenderDefaultsProduceUnscaledBlackOnWhiteProfileOutput()
    {
        QrRenderOptions options = new QrRenderOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(options.Scale, Is.EqualTo(1));
            Assert.That(options.IncludeQuietZone, Is.True);
            Assert.That(options.QuietZoneModules, Is.EqualTo(-1));
            Assert.That(options.DarkValue, Is.EqualTo(0));
            Assert.That(options.LightValue, Is.EqualTo(255));
            Assert.That(options.Invert, Is.False);
            Assert.That(options.RenderRoleMap, Is.False);
            Assert.That(options.HighlightRole, Is.EqualTo(QrModuleRole.Unknown));
        });
    }

    [Test]
    public void QualityDefaultsEnableApplicableMeasurementsWithoutClaimingCalibration()
    {
        QrQualityOptions options = new QrQualityOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowModel2, Is.True);
            Assert.That(options.AllowMicro, Is.True);
            Assert.That(options.AllowRectangularMicro, Is.True);
            Assert.That(options.AllowLegacyModel1, Is.False);
            Assert.That(options.AutomaticSymbolKind, Is.True);
            Assert.That(options.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(options.ApertureDiameterModules, Is.EqualTo(1.0));
            Assert.That(options.SamplesPerModule, Is.EqualTo(1));
            Assert.That(options.AnalyzeContrast, Is.True);
            Assert.That(options.AnalyzeModulation, Is.True);
            Assert.That(options.AnalyzeFixedPatterns, Is.True);
            Assert.That(options.AnalyzeGridUniformity, Is.True);
            Assert.That(options.AnalyzePrintGrowth, Is.True);
            Assert.That(options.AnalyzeUnusedErrorCorrection, Is.True);
            Assert.That(options.RequireCalibratedInputs, Is.False);
            Assert.That(options.MaximumModuleSamples, Is.GreaterThan(0));
        });
    }

    [Test]
    public void CallersCanOverrideFamilyAndProcessingSelections()
    {
        QrEncodeOptions encode = new QrEncodeOptions
        {
            AllowModel2 = false,
            AllowMicro = false,
            AllowRectangularMicro = true,
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.rMQR,
            MaskPattern = QrMaskPattern.Four,
            QuietZoneModules = 2
        };
        QrDetectionOptions detection = new QrDetectionOptions
        {
            AllowModel2 = false,
            AllowMicro = true,
            AllowRectangularMicro = false,
            AllowLegacyModel1 = false,
            BinarizationMode = QrBinarizationMode.SuppliedBinary,
            Polarity = QrPolarity.LightOnDark,
            DetectMultiple = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(encode.AllowModel2, Is.False);
            Assert.That(encode.AllowMicro, Is.False);
            Assert.That(encode.AllowRectangularMicro, Is.True);
            Assert.That(encode.ForceSymbolKind, Is.True);
            Assert.That(encode.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(encode.MaskPattern, Is.EqualTo(QrMaskPattern.Four));
            Assert.That(encode.QuietZoneModules, Is.EqualTo(2));
            Assert.That(detection.AllowModel2, Is.False);
            Assert.That(detection.AllowMicro, Is.True);
            Assert.That(detection.AllowRectangularMicro, Is.False);
            Assert.That(detection.AllowLegacyModel1, Is.False);
            Assert.That(detection.BinarizationMode, Is.EqualTo(QrBinarizationMode.SuppliedBinary));
            Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
            Assert.That(detection.DetectMultiple, Is.False);
        });
    }
}
