using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMalformedMetadataTests
{
    [TestCase("ValidateSegments")]
    [TestCase("EncodeSegments")]
    [TestCase("TryEncodeSegments")]
    [TestCase("EncodeManualStructuredAppend")]
    public void MalformedManualSegmentsAreRejectedBeforeEncoding(string member)
    {
        Action<QrSegment>[] mutations =
        {
            s => s.CharacterCount = -1, s => s.CharacterCount = int.MaxValue,
            s => s.Data = null!, s => s.Mode = (QrEncodingMode)6,
            s => s.EciAssignment = -2, s => s.EciAssignment = 1000000,
            s => s.StructuredAppend = new QrStructuredAppendInfo(),
            s => s.Fnc1ApplicationIndicator = 1
        };
        foreach (Action<QrSegment> mutate in mutations)
        {
            QrSegment segment = new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [0] };
            mutate(segment);
            QrMalformedLayoutTests.AssertRejected(() =>
            {
                IQrEncoder encoder = new QrEncoder();
                QrEncodeOptions options = QrMalformedLayoutTests.Encoding();
                switch (member)
                {
                    case "ValidateSegments": new QrSegmenter().ValidateSegments([segment], QrSymbolKind.Model2, 1, false, 100, 1000); break;
                    case "EncodeSegments": encoder.EncodeSegments([segment], options); break;
                    case "TryEncodeSegments": encoder.TryEncodeSegments([segment], options); break;
                    case "EncodeManualStructuredAppend":
                        QrSegment[][] parts = QrMalformedLayoutTests.ManualParts();
                        parts[0][1] = segment;
                        encoder.EncodeManualStructuredAppend(parts, options);
                        break;
                }
            }, false);
        }
    }

    [TestCase("CreateTextSegments")]
    [TestCase("CreateByteSegment")]
    [TestCase("OptimizeTextSegments")]
    [TestCase("EncodeText")]
    [TestCase("TryEncodeText")]
    [TestCase("EncodeBytes")]
    [TestCase("EncodeStructuredAppend")]
    public void InvalidEciAssignmentsCannotBypassValidation(string member)
    {
        foreach (int assignment in new[] { int.MinValue, -2, 1000000, int.MaxValue })
        {
            QrEncodeOptions options = QrMalformedLayoutTests.Encoding();
            options.EciAssignment = assignment;
            QrMalformedLayoutTests.AssertRejected(() =>
            {
                IQrSegmenter segmenter = new QrSegmenter();
                IQrEncoder encoder = new QrEncoder();
                switch (member)
                {
                    case "CreateTextSegments": segmenter.CreateTextSegments("abc", assignment, false, 100); break;
                    case "CreateByteSegment": segmenter.CreateByteSegment([0], assignment, 100); break;
                    case "OptimizeTextSegments": segmenter.OptimizeTextSegments("abc", QrSymbolKind.Model2, 1, [assignment], false, -1, null, 100, 100); break;
                    case "EncodeText": encoder.EncodeText("abc", options); break;
                    case "TryEncodeText": encoder.TryEncodeText("abc", options); break;
                    case "EncodeBytes": encoder.EncodeBytes([0], options); break;
                    case "EncodeStructuredAppend": encoder.EncodeStructuredAppend(new byte[30], options); break;
                }
            }, false);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CapacityEntryPointsRejectImpossibleCountsAndOverflowingBitTotals(bool select)
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        // Exact capacity rejects an unrepresentable count field; selection first
        // bounds the payload arithmetic before considering any version.
        foreach (int count in new[] { int.MinValue, -1, int.MaxValue })
        {
            QrCapacityRequest request = new QrCapacityRequest
            {
                SymbolKind = QrSymbolKind.Model2, Version = 1, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
                EncodingMode = QrEncodingMode.Byte, CharacterCount = count, PayloadBitCount = 8
            };
            QrMalformedLayoutTests.AssertRejected(() =>
            {
                if (select) calculator.SelectSymbolParameters(request, QrMalformedLayoutTests.Encoding());
                else calculator.CalculateCapacity(request);
            }, select && count == int.MaxValue);
        }
        QrCapacityRequest overflow = new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Model2, Version = 1, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            EncodingMode = QrEncodingMode.Byte, CharacterCount = 1, PayloadBitCount = 8, ControlBitCount = int.MaxValue
        };
        QrMalformedLayoutTests.AssertRejected(() =>
        {
            if (select) calculator.SelectSymbolParameters(overflow, QrMalformedLayoutTests.Encoding());
            else calculator.CalculateCapacity(overflow);
        }, true);
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    public void MatrixConstructionRejectsTruncatedOrExtendedCodewordLayouts(QrSymbolKind kind, int version, int level)
    {
        byte[] words = CodewordErasureRecoveryTests.Words(kind, version, level);
        foreach (int length in new[] { 0, words.Length - 1, words.Length + 1 })
        {
            QrSymbol symbol = new QrSymbol
            {
                SymbolKind = kind, Version = version, ErrorCorrectionLevel = kind == QrSymbolKind.rMQR ? QrErrorCorrectionLevel.Medium : (QrErrorCorrectionLevel)level,
                Codewords = new byte[length]
            };
            QrMalformedLayoutTests.AssertRejected(() => new QrMatrixProcessor().BuildMatrix(symbol, 0), false);
            Assert.That(symbol.Matrix, Is.Null);
        }
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    public void ExplicitVersionMustMatchMatrixGeometry(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, version, level), kind, version, level);
        QrErrorCorrectionLevel ecc = kind == QrSymbolKind.rMQR ? QrErrorCorrectionLevel.Medium : (QrErrorCorrectionLevel)level;
        QrMaskPattern mask = kind == QrSymbolKind.Model2 ? QrMaskPattern.Five : kind == QrSymbolKind.Micro ? QrMaskPattern.Three : QrMaskPattern.Four;
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMalformedLayoutTests.AssertRejected(() => processor.ExtractCodewords(matrix, kind, version + 1, ecc, mask), false);
        QrMalformedLayoutTests.AssertRejected(() => processor.ClassifyModules(matrix, kind, version + 1, ecc), false);
        QrCapacityRequest request = new QrCapacityRequest
        {
            SymbolKind = kind, Version = version, Width = matrix.Width + 1, Height = matrix.Height,
            ErrorCorrectionLevel = ecc, EncodingMode = QrEncodingMode.Numeric, CharacterCount = 1, PayloadBitCount = 4
        };
        QrMalformedLayoutTests.AssertRejected(() => new QrCapacityCalculator().CalculateCapacity(request), false);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReedSolomonEntryPointsRejectInvalidBlockLengthsAndParityCounts(bool correct)
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        foreach (int parity in new[] { int.MinValue, -1, 0, 256, int.MaxValue })
            QrMalformedLayoutTests.AssertRejected(() =>
            {
                if (correct) codec.CorrectCodewords(new byte[26], parity, []);
                else codec.CreateErrorCorrection(new byte[19], parity);
            }, false);
        QrMalformedLayoutTests.AssertRejected(() =>
        {
            if (correct) codec.CorrectCodewords(new byte[256], 7, []);
            else codec.CreateErrorCorrection(new byte[249], 7);
        }, true);
        if (correct)
            foreach (int[] erasures in new[] { new[] { -1 }, new[] { 26 }, new[] { 0, 0 }, new[] { int.MaxValue } })
                QrMalformedLayoutTests.AssertRejected(() => codec.CorrectCodewords(new byte[26], 7, erasures), false);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AssemblersRejectMalformedContainersAndAllocationBudgets(bool attempt)
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] Parts() => Enumerable.Range(0, 2).Select(index => new QrDecodeResult
        {
            StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = 2, Parity = 0 }, RawBytes = [0]
        }).ToArray();
        void Call(QrDecodeResult[] parts, int budget)
        {
            if (attempt) assembler.TryAssembleSequence(parts, 16, budget);
            else assembler.AssembleSequence(parts, 16, budget);
        }
        foreach (Action<QrDecodeResult[]> mutate in new Action<QrDecodeResult[]>[]
        {
            p => p[0] = null!, p => p[0].Segments = null!, p => p[0].Segments = [null!]
        })
        {
            QrDecodeResult[] parts = Parts();
            mutate(parts);
            QrMalformedLayoutTests.AssertRejected(() => Call(parts, 100000), false);
        }
        QrMalformedLayoutTests.AssertRejected(() => Call(Parts(), 1), true);
        foreach (int total in new[] { int.MinValue, 0, 1, 17, int.MaxValue })
        {
            QrDecodeResult[] parts = Parts();
            parts[0].StructuredAppend!.Total = total;
            if (attempt)
            {
                QrStructuredAppendAttemptResult result = assembler.TryAssembleSequence(parts, 16, 100000);
                Assert.That(result.Success, Is.False);
                Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
                Assert.That(result.Result, Is.Null);
            }
            else
                Assert.That(Assert.Throws<QrStructuredAppendException>(() => Call(parts, 100000))!.FailureReason,
                    Is.EqualTo(QrFailureReason.ConflictingSequence));
        }
        Assert.DoesNotThrow(() => Call(Parts(), 100000));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConsistentVersionCopiesCannotOverrideContradictoryMatrixDimensions(bool imageInput)
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 7, 0), QrSymbolKind.Model2, 7, 0);
        int wrongCode = VersionCodeTable.GetCode(8);
        for (int bit = 0; bit < 18; bit++)
        {
            QrModuleValue value = (wrongCode & (1 << bit)) != 0 ? QrModuleValue.Dark : QrModuleValue.Light;
            matrix.Values[Model2PlacementTable.GetVersionPrimaryRow(7, bit) * matrix.Width + Model2PlacementTable.GetVersionPrimaryColumn(7, bit)] = value;
            matrix.Values[Model2PlacementTable.GetVersionSecondaryRow(7, bit) * matrix.Width + Model2PlacementTable.GetVersionSecondaryColumn(7, bit)] = value;
        }
        IQrDecoder decoder = new QrDecoder();
        QrDecodeOptions options = QrMalformedPayloadTests.Decoding();
        options.AllowLegacyModel1 = false;
        QrAttemptResult result = imageInput
            ? decoder.TryDecodeDetection(QrMalformedPayloadTests.Render(matrix), QrMalformedPayloadTests.Detection(matrix), QrMalformedPayloadTests.Sampling(), options)
            : decoder.TryDecodeMatrix(matrix, options);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidVersion));
        Assert.That(result.Result, Is.Null);
        Assert.That(result.RawBytes, Is.Empty);
    }
}
