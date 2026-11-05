using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicOutputDataTests
{
    [Test]
    public void OutputCollectionsStartEmptyAndProtocolSentinelsAreUnambiguous()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new QrSymbol().Codewords, Is.Empty);
            Assert.That(new QrSymbol().Segments, Is.Empty);
            Assert.That(new QrSymbol().ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.None));
            Assert.That(new QrSymbol().MaskPattern, Is.EqualTo(QrMaskPattern.Automatic));
            Assert.That(new QrSymbolSequence().Symbols, Is.Empty);
            Assert.That(new QrSymbolSequence().RawBytes, Is.Empty);
            Assert.That(new QrDetection().Diagnostics, Is.Empty);
            Assert.That(new QrDetectionSet().Detections, Is.Empty);
            Assert.That(new QrDetectionSet().Thresholds, Is.Empty);
            Assert.That(new QrDecodeResult().RawBytes, Is.Empty);
            Assert.That(new QrDecodeResult().Segments, Is.Empty);
            Assert.That(new QrDecodeResult().EciAssignments, Is.Empty);
            Assert.That(new QrDecodeResult().Fnc1SecondApplicationIndicator, Is.EqualTo(-1));
            Assert.That(new QrDecodeSet().Results, Is.Empty);
            Assert.That(new QrValidationResult().FailureReasons, Is.Empty);
            Assert.That(new QrValidationResult().DamagedModuleIndices, Is.Empty);
            Assert.That(new QrQualityMetric().Grade, Is.EqualTo(QrQualityGrade.NotGraded));
            Assert.That(new QrQualityReport().Metrics, Is.Empty);
            Assert.That(new QrQualityReport().OverallGrade, Is.EqualTo(QrQualityGrade.NotGraded));
            Assert.That(new QrQualityReport().IsVerifierCertified, Is.False);
        });
    }

    [Test]
    public void SymbolRetainsRawConstructionAndCapacityMetadata()
    {
        byte[] codewords = new byte[] { 0x40, 0x34, 0x14, 0x20 };
        QrSegment segment = new QrSegment { Mode = QrEncodingMode.Byte, Data = new byte[] { 0x41 } };
        QrMatrix matrix = new QrMatrix { Width = 21, Height = 21 };
        QrValidationResult validation = new QrValidationResult { IsConformant = true };
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Version = 1,
            Width = 21,
            Height = 21,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            MaskPattern = QrMaskPattern.Three,
            Matrix = matrix,
            Codewords = codewords,
            Segments = new QrSegment[] { segment },
            DataCodewordCount = 19,
            ErrorCorrectionCodewordCount = 7,
            TotalCodewordCount = 26,
            RemainderBitCount = 0,
            DataBitCapacity = 152,
            UsedDataBitCount = 44,
            UnusedDataBitCount = 108,
            IsConformant = true,
            Validation = validation,
            UsesHanziCompatibility = false
        };

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(symbol.Version, Is.EqualTo(1));
            Assert.That(symbol.Matrix, Is.SameAs(matrix));
            Assert.That(symbol.Codewords, Is.SameAs(codewords));
            Assert.That(symbol.Segments, Has.Exactly(1).SameAs(segment));
            Assert.That(symbol.DataCodewordCount, Is.EqualTo(19));
            Assert.That(symbol.ErrorCorrectionCodewordCount, Is.EqualTo(7));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(26));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(152));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(44));
            Assert.That(symbol.UnusedDataBitCount, Is.EqualTo(108));
            Assert.That(symbol.Validation, Is.SameAs(validation));
        });
    }

    [Test]
    public void SequenceAndEncodeAttemptRetainOrderedSymbolsAndCombinedPayload()
    {
        QrSymbol first = new QrSymbol { Version = 2 };
        QrSymbol second = new QrSymbol { Version = 2 };
        QrStructuredAppendInfo append = new QrStructuredAppendInfo
        {
            Total = 2,
            Parity = 0x5A,
            IsComplete = true
        };
        byte[] combined = new byte[] { 1, 2, 3, 4 };
        QrSymbolSequence sequence = new QrSymbolSequence
        {
            Symbols = new QrSymbol[] { first, second },
            StructuredAppend = append,
            RawBytes = combined,
            Text = "part onepart two",
            IsComplete = true,
            FailureReason = QrFailureReason.None
        };
        QrEncodeAttemptResult attempt = new QrEncodeAttemptResult
        {
            Success = true,
            FailureReason = QrFailureReason.None,
            Sequence = sequence
        };

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Symbols, Has.Exactly(2).Items);
            Assert.That(sequence.Symbols[0], Is.SameAs(first));
            Assert.That(sequence.Symbols[1], Is.SameAs(second));
            Assert.That(sequence.StructuredAppend, Is.SameAs(append));
            Assert.That(sequence.RawBytes, Is.SameAs(combined));
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(attempt.Sequence, Is.SameAs(sequence));
            Assert.That(attempt.Symbol, Is.Null);
        });
    }

    [Test]
    public void DetectionSetRetainsGeometrySamplingAndPreprocessingDiagnostics()
    {
        QrQuadrilateral corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 10, Y = 20 },
            BottomRight = new QrPoint { X = 110, Y = 120 }
        };
        QrMatrix sampled = new QrMatrix { Width = 21, Height = 21 };
        string[] detectionDiagnostics = new string[] { "alignment refined" };
        QrDetection detection = new QrDetection
        {
            Corners = corners,
            SampledMatrix = sampled,
            HasSymbolKindEstimate = true,
            EstimatedSymbolKind = QrSymbolKind.Model2,
            EstimatedVersion = 1,
            EstimatedWidth = 21,
            EstimatedHeight = 21,
            Polarity = QrPolarity.DarkOnLight,
            IsMirrored = true,
            Confidence = 0.875,
            ModuleSize = 4.5,
            CandidateIndex = 3,
            Diagnostics = detectionDiagnostics
        };
        QrRaster binary = new QrRaster { Pixels = new byte[] { 0, 255 }, Width = 2, Height = 1, Stride = 2 };
        int[] thresholds = new int[] { 92, 105 };
        string[] preprocessingDiagnostics = new string[] { "hybrid threshold" };
        QrDetectionSet set = new QrDetectionSet
        {
            Detections = new QrDetection[] { detection },
            BinarizationMode = QrBinarizationMode.HybridLocal,
            Polarity = QrPolarity.DarkOnLight,
            BinaryRaster = binary,
            Thresholds = thresholds,
            CandidateCount = 7,
            Diagnostics = preprocessingDiagnostics
        };

        Assert.Multiple(() =>
        {
            Assert.That(detection.Corners, Is.SameAs(corners));
            Assert.That(detection.SampledMatrix, Is.SameAs(sampled));
            Assert.That(detection.HasSymbolKindEstimate, Is.True);
            Assert.That(detection.EstimatedVersion, Is.EqualTo(1));
            Assert.That(detection.IsMirrored, Is.True);
            Assert.That(detection.Confidence, Is.EqualTo(0.875));
            Assert.That(detection.Diagnostics, Is.SameAs(detectionDiagnostics));
            Assert.That(set.Detections, Has.Exactly(1).SameAs(detection));
            Assert.That(set.BinaryRaster, Is.SameAs(binary));
            Assert.That(set.Thresholds, Is.SameAs(thresholds));
            Assert.That(set.CandidateCount, Is.EqualTo(7));
            Assert.That(set.Diagnostics, Is.SameAs(preprocessingDiagnostics));
        });
    }

    [Test]
    public void DecodeOutputsRetainLosslessPayloadProtocolStateAndAttemptIdentity()
    {
        byte[] rawBytes = new byte[] { 0x41, 0x00, 0xFF };
        QrSegment segment = new QrSegment { Mode = QrEncodingMode.Byte, Data = rawBytes };
        QrSymbol symbol = new QrSymbol { SymbolKind = QrSymbolKind.Micro };
        QrDetection detection = new QrDetection { EstimatedSymbolKind = QrSymbolKind.Micro };
        QrStructuredAppendInfo append = new QrStructuredAppendInfo { Index = 0, Total = 2 };
        int[] assignments = new int[] { 26, 3 };
        string[] warnings = new string[] { "quiet zone cropped" };
        QrDecodeResult result = new QrDecodeResult
        {
            Text = "A\0�",
            RawBytes = rawBytes,
            Segments = new QrSegment[] { segment },
            Symbol = symbol,
            Detection = detection,
            StructuredAppend = append,
            SymbologyIdentifier = "]Q1",
            EciAssignments = assignments,
            UsesFnc1First = true,
            Fnc1SecondApplicationIndicator = 17,
            CorrectedErrorCount = 2,
            CorrectedErasureCount = 1,
            IsConformant = false,
            UsesHanziCompatibility = true,
            Warnings = warnings
        };
        QrAttemptResult attempt = new QrAttemptResult { Success = true, Result = result };
        QrDecodeSet set = new QrDecodeSet
        {
            Results = new QrDecodeResult[] { result },
            UnassembledResults = new QrDecodeResult[] { result },
            AssembledResults = new QrDecodeResult[] { result },
            CandidateCount = 4,
            Diagnostics = new string[] { "one sequence incomplete" }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.RawBytes, Is.SameAs(rawBytes));
            Assert.That(result.Segments, Has.Exactly(1).SameAs(segment));
            Assert.That(result.Symbol, Is.SameAs(symbol));
            Assert.That(result.Detection, Is.SameAs(detection));
            Assert.That(result.StructuredAppend, Is.SameAs(append));
            Assert.That(result.EciAssignments, Is.SameAs(assignments));
            Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(17));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(2));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(1));
            Assert.That(result.Warnings, Is.SameAs(warnings));
            Assert.That(attempt.Result, Is.SameAs(result));
            Assert.That(set.Results, Has.Exactly(1).SameAs(result));
            Assert.That(set.UnassembledResults, Has.Exactly(1).SameAs(result));
            Assert.That(set.AssembledResults, Has.Exactly(1).SameAs(result));
            Assert.That(set.CandidateCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void ValidationResultRetainsMachineReadableFailuresAndDamageLocations()
    {
        QrFailureReason[] reasons = new QrFailureReason[]
        {
            QrFailureReason.InvalidFormat,
            QrFailureReason.InvalidGeometry
        };
        string[] failures = new string[] { "format copies disagree", "quiet zone missing" };
        int[] indices = new int[] { 0, 20, 440 };
        QrModuleRole[] roles = new QrModuleRole[] { QrModuleRole.Finder, QrModuleRole.Format };
        QrValidationResult result = new QrValidationResult
        {
            IsConformant = false,
            FailureReasons = reasons,
            Failures = failures,
            DamagedModuleIndices = indices,
            DamagedRoles = roles,
            FixedPatternDamageCount = 3,
            UnknownModuleCount = 2,
            FormatInformationConsistent = false,
            VersionInformationConsistent = true,
            QuietZoneValid = false,
            IsRecoverable = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.FailureReasons, Is.SameAs(reasons));
            Assert.That(result.Failures, Is.SameAs(failures));
            Assert.That(result.DamagedModuleIndices, Is.SameAs(indices));
            Assert.That(result.DamagedRoles, Is.SameAs(roles));
            Assert.That(result.FixedPatternDamageCount, Is.EqualTo(3));
            Assert.That(result.UnknownModuleCount, Is.EqualTo(2));
            Assert.That(result.FormatInformationConsistent, Is.False);
            Assert.That(result.VersionInformationConsistent, Is.True);
            Assert.That(result.QuietZoneValid, Is.False);
            Assert.That(result.IsRecoverable, Is.True);
        });
    }

    [Test]
    public void QualityReportSeparatesMeasurementsCalibrationAndCertification()
    {
        QrQualityMetric contrast = new QrQualityMetric
        {
            Name = "symbol contrast",
            Value = 0.78,
            Grade = QrQualityGrade.Grade3,
            IsCalculated = true,
            UsesCalibratedInputs = false,
            Unit = "ratio",
            Limitation = "uncalibrated grayscale samples"
        };
        QrValidationResult validation = new QrValidationResult { IsConformant = true };
        string[] limitations = new string[] { "not a calibrated verifier report" };
        QrQualityReport report = new QrQualityReport
        {
            SymbolKind = QrSymbolKind.Model2,
            Metrics = new QrQualityMetric[] { contrast },
            OverallGrade = QrQualityGrade.Grade3,
            UsesCalibratedInputs = false,
            IsVerifierCertified = false,
            SampleCount = 441,
            Validation = validation,
            Limitations = limitations
        };

        Assert.Multiple(() =>
        {
            Assert.That(report.Metrics, Has.Exactly(1).SameAs(contrast));
            Assert.That(contrast.Name, Is.EqualTo("symbol contrast"));
            Assert.That(contrast.Value, Is.EqualTo(0.78));
            Assert.That(contrast.Grade, Is.EqualTo(QrQualityGrade.Grade3));
            Assert.That(contrast.IsCalculated, Is.True);
            Assert.That(contrast.UsesCalibratedInputs, Is.False);
            Assert.That(report.OverallGrade, Is.EqualTo(QrQualityGrade.Grade3));
            Assert.That(report.UsesCalibratedInputs, Is.False);
            Assert.That(report.IsVerifierCertified, Is.False);
            Assert.That(report.Validation, Is.SameAs(validation));
            Assert.That(report.Limitations, Is.SameAs(limitations));
        });
    }
}
