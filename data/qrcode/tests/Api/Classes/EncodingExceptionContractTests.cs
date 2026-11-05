using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EncodingExceptionContractTests
{
    [TestCase("")]
    [TestCase("123")]
    [TestCase("ABC")]
    [TestCase("漢字")]
    [TestCase("abc")]
    public void TextSegmentationRejectsInvalidEciRegardlessOfSelectedMode(string text)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        foreach (int assignment in new[] { -2, 1000000 })
        {
            QrInputException error = Assert.Throws<QrInputException>(() =>
                segmenter.CreateTextSegments(text, assignment, false, 100))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
            Assert.That(error.InputName, Is.Not.Null.And.Not.Empty);
        }
        // A valid but unavailable transcoder is irrelevant to non-byte modes.
        if (text != "abc")
            Assert.DoesNotThrow(() => segmenter.CreateTextSegments(text, 899, false, 100));
    }

    [TestCase("CreateTextSegments")]
    [TestCase("CreateByteSegment")]
    [TestCase("OptimizeTextSegments")]
    [TestCase("ValidateSegments")]
    public void SegmentOperationsEnforceByteLimitAndSucceedAtTheBoundary(string member)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        TestDelegate Call(int limit) => () =>
        {
            switch (member)
            {
                case "CreateTextSegments":
                    Assert.That(segmenter.CreateTextSegments("abc", -1, false, limit)[0].Data,
                        Is.EqualTo(new byte[] { 97, 98, 99 }));
                    break;
                case "CreateByteSegment":
                    Assert.That(segmenter.CreateByteSegment([97, 98, 99], -1, limit).Data,
                        Is.EqualTo(new byte[] { 97, 98, 99 }));
                    break;
                case "OptimizeTextSegments":
                    Assert.That(segmenter.OptimizeTextSegments("abc", QrSymbolKind.Model2, 1,
                        [], false, -1, null, 10, limit), Is.Not.Empty);
                    break;
                case "ValidateSegments":
                    segmenter.ValidateSegments([Bytes([97, 98, 99])], QrSymbolKind.Model2, 1, false, 10, limit);
                    break;
                default: throw new AssertionException(member);
            }
        };
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(Call(2))!;
        AssertResource(error, "MaximumInputBytes", 3, 2);
        Assert.DoesNotThrow(Call(3));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TextSegmentationReportsUnsupportedTranscoder(bool optimize)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() =>
        {
            if (optimize)
                segmenter.OptimizeTextSegments("abc", QrSymbolKind.Model2, 1, [899], false, -1, null, 10, 100);
            else
                segmenter.CreateTextSegments("abc", 899, false, 100);
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
        Assert.That(error.FeatureName, Is.EqualTo("ECI"));
        Assert.That(error.ProtocolValue, Is.EqualTo(899));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SegmentLimitsIncludeControlSegments(bool optimize)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        TestDelegate Call(int limit) => () =>
        {
            if (optimize)
                segmenter.OptimizeTextSegments("123", QrSymbolKind.Model2, 1, [], true, -1, null, limit, 100);
            else
                segmenter.ValidateSegments([new QrSegment { Mode = QrEncodingMode.Fnc1First }, Bytes([1])],
                    QrSymbolKind.Model2, 1, false, limit, 100);
        };
        AssertResource(Assert.Throws<QrResourceLimitException>(Call(1))!, "MaximumSegments", 2, 1);
        Assert.DoesNotThrow(Call(2));
    }

    [TestCase("EncodeText")]
    [TestCase("EncodeBytes")]
    [TestCase("EncodeSegments")]
    public void OrdinaryEncodersReportCapacityResourceAndUnsupportedFailures(string member)
    {
        IQrEncoder encoder = new QrEncoder();
        QrSymbol Call(QrEncodeOptions options) => member switch
        {
            "EncodeText" => encoder.EncodeText(new string('a', 18), options),
            "EncodeBytes" => encoder.EncodeBytes(new byte[18], options),
            "EncodeSegments" => encoder.EncodeSegments([Bytes(new byte[18])], options),
            _ => throw new AssertionException(member)
        };
        QrEncodeOptions options = Model2();
        QrCapacityException capacity = Assert.Throws<QrCapacityException>(() => Call(options))!;
        Assert.That(capacity.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
        Assert.That(capacity.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
        Assert.That(capacity.RequiredBitCount, Is.EqualTo(156));
        Assert.That(capacity.AvailableBitCount, Is.EqualTo(152));
        options.MaximumVersion = 2;
        QrSymbol success = Call(options);
        Assert.That(success.Version, Is.EqualTo(2));
        Assert.That(new QrDecoder().DecodeMatrix(success.Matrix!, new QrDecodeOptions()).RawBytes, Has.Length.EqualTo(18));
        options.MaximumInputBytes = 17;
        AssertResource(Assert.Throws<QrResourceLimitException>(() => Call(options))!, "MaximumInputBytes", 18, 17);
        options = Model2();
        options.SymbolKind = QrSymbolKind.LegacyModel1;
        options.AllowLegacyModel1 = true;
        QrUnsupportedFeatureException unsupported = Assert.Throws<QrUnsupportedFeatureException>(() => Call(options))!;
        Assert.That(unsupported.FeatureName, Is.EqualTo("SymbolKind"));
        Assert.That(unsupported.ProtocolValue, Is.EqualTo((int)QrSymbolKind.LegacyModel1));
        Assert.Throws<QrInputException>(() => Call(null!));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TextEncoderReportsUnsupportedEciInBothSegmentationPaths(bool optimize)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Model2();
        options.OptimizeSegments = optimize;
        options.EciAssignment = 899;
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeText("abc", options))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
        Assert.That(error.ProtocolValue, Is.EqualTo(899));
    }

    [Test]
    public void UnrepresentableMicroTextReportsTheResponsibleEncodingFeature(
        [Values(false, true)] bool optimize, [Values(-1, 3, 26, 899)] int assignment)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = new()
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Micro,
            MaximumVersion = 4, EciAssignment = assignment, OptimizeSegments = optimize
        };
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() =>
            encoder.EncodeText("\U0001F600", options))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
        Assert.That(error.FeatureName, Is.EqualTo(assignment == 899 ? "ECI" : "MicroTextEncoding"));
        Assert.That(error.ProtocolValue, Is.EqualTo(assignment == 899 ? 899 : (int)QrSymbolKind.Micro));
        QrEncodeAttemptResult attempt = encoder.TryEncodeText("\U0001F600", options);
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(error.FailureReason));
        Assert.That(attempt.Symbol, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StructuredAppendEncodersValidateInputsLimitsAndCapacity(bool manual)
    {
        IQrEncoder encoder = new QrEncoder();
        QrSymbolSequence Call(int length, QrEncodeOptions options) => manual
            ? encoder.EncodeManualStructuredAppend(Parts(length, 2), options)
            : encoder.EncodeStructuredAppend(new byte[length * 2], options);
        QrEncodeOptions options = Model2();
        options.MaximumStructuredAppendSymbols = 2;
        QrSymbolSequence sequence = Call(15, options);
        Assert.That(sequence.IsComplete, Is.True);
        Assert.That(sequence.Symbols, Has.Length.EqualTo(2));
        QrDecodeResult[] decoded = sequence.Symbols.Select(s => new QrDecoder().DecodeMatrix(s.Matrix!, new QrDecodeOptions())).ToArray();
        Assert.That(new QrStructuredAppendAssembler().AssembleSequence(decoded, 2, 100000).RawBytes,
            Is.EqualTo(new byte[30]));
        QrCapacityException capacity = Assert.Throws<QrCapacityException>(() => Call(16, options))!;
        Assert.That(capacity.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
        Assert.That(capacity.RequiredBitCount, Is.GreaterThan(capacity.AvailableBitCount));
        options.MaximumInputBytes = 29;
        AssertResource(Assert.Throws<QrResourceLimitException>(() => Call(15, options))!, "MaximumInputBytes", 30, 29);
        options = Model2();
        options.SymbolKind = QrSymbolKind.Micro;
        Assert.Throws<QrInputException>(() => Call(1, options));
        Assert.Throws<QrInputException>(() => Call(1, null!));
        Assert.Throws<QrInputException>(() =>
        {
            if (manual) encoder.EncodeManualStructuredAppend(null!, Model2());
            else encoder.EncodeStructuredAppend(null!, Model2());
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ManualSequenceReportsIncompleteOrConflictingHeaders(bool incomplete)
    {
        QrSegment[][] parts = Parts(1, 2);
        foreach (QrSegment[] part in parts)
        {
            if (incomplete) part[0].StructuredAppend!.Total = 3;
            else part[0].StructuredAppend!.Parity = 1;
        }
        QrStructuredAppendException error = Assert.Throws<QrStructuredAppendException>(() =>
            new QrEncoder().EncodeManualStructuredAppend(parts, Model2()))!;
        Assert.That(error.FailureReason, Is.EqualTo(incomplete ? QrFailureReason.IncompleteSequence : QrFailureReason.ConflictingSequence));
        Assert.That(error.ExpectedPartCount, Is.EqualTo(incomplete ? 3 : 2));
        Assert.That(error.ActualPartCount, Is.EqualTo(2));
        Assert.That(error.Parity, Is.EqualTo(incomplete ? 0 : 1));
    }

    [Test]
    public void ManualSequenceEnforcesPartCountBeforeEncoding()
    {
        QrEncodeOptions options = Model2();
        options.MaximumStructuredAppendSymbols = 2;
        AssertResource(Assert.Throws<QrResourceLimitException>(() =>
            new QrEncoder().EncodeManualStructuredAppend(Parts(1, 3), options))!, "MaximumStructuredAppendSymbols", 3, 2);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CapacityOperationsRejectUnrepresentableBitTotals(bool select)
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        QrCapacityRequest request = new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Model2, Version = 1, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            EncodingMode = QrEncodingMode.Byte, CharacterCount = 1, PayloadBitCount = 8, ControlBitCount = int.MaxValue
        };
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() =>
        {
            if (select) calculator.SelectSymbolParameters(request, Model2());
            else calculator.CalculateCapacity(request);
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.LimitName, Is.EqualTo("CapacityBitCount"));
        request.ControlBitCount = 0;
        Assert.That(calculator.CalculateCapacity(request).RequiredBitCount, Is.EqualTo(20));
        QrEncodeOptions legacy = Model2();
        legacy.SymbolKind = QrSymbolKind.LegacyModel1;
        legacy.AllowLegacyModel1 = true;
        Assert.Throws<QrUnsupportedFeatureException>(() => calculator.SelectSymbolParameters(request, legacy));
        Assert.Throws<QrInputException>(() => calculator.SelectSymbolParameters(request, null!));
    }

    static QrSegment Bytes(byte[] data) => new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = data.Length, Data = data };

    static QrSegment[][] Parts(int length, int count) => Enumerable.Range(0, count).Select(index => new[]
    {
        new QrSegment { Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = count, Parity = 0 } },
        Bytes(new byte[length])
    }).ToArray();

    static QrEncodeOptions Model2() => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 1, MaximumVersion = 1,
        ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low, BoostErrorCorrectionLevel = false
    };

    static void AssertResource(QrResourceLimitException error, string name, int requested, int maximum)
    {
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.LimitName, Is.EqualTo(name));
        Assert.That(error.RequestedValue, Is.EqualTo(requested));
        Assert.That(error.MaximumValue, Is.EqualTo(maximum));
    }
}
