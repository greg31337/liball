using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDecoderTests
{
    [TestCase("HELLO WORLD")]
    [TestCase("é")]
    [TestCase("漢字")]
    [TestCase("")]
    public void DecodesTextBeyondNumericMode(string text)
    {
        QrSymbol symbol = new QrEncoder().EncodeText(text, new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2
        });
        QrAttemptResult result = new QrDecoder().TryDecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Result!.Segments, Is.EqualTo(result.Segments));
    }

    [Test]
    public void Fnc1SecondPositionRetainsItsRawControlByte()
    {
        QrMatrix matrix = MatrixFromBits([(9, 4), (165, 8), (1, 4), (1, 10), (7, 4)]);
        QrAttemptResult result = new QrDecoder().TryDecodeMatrix(matrix, new QrDecodeOptions { Strict = false });
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo("7"));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 55 }));
        Assert.That(result.Segments[0].Data, Is.EqualTo(new byte[] { 165 }));
        Assert.That(result.Result!.Fnc1SecondApplicationIndicator, Is.EqualTo(165));
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Model2, 7, 2)]
    [TestCase(QrSymbolKind.Micro, 1, -1)]
    [TestCase(QrSymbolKind.Micro, 4, 1)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0)]
    public void BothMatrixApisDecodeAllFamiliesAndPreserveInput(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, version, level), kind, version, level);
        matrix = MatrixTransformer.Rotate(matrix, 1);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        IQrDecoder decoder = new QrDecoder();
        QrDecodeOptions options = new QrDecodeOptions();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, options);
        QrDecodeResult decoded = decoder.DecodeMatrix(matrix, options);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(attempt.CandidateCount, Is.EqualTo(1));
        Assert.That(attempt.AttemptCount, Is.InRange(1, options.MaximumRecoveryAttempts));
        Assert.That(attempt.Text, Is.EqualTo("123"));
        Assert.That(attempt.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51 }));
        Assert.That(attempt.Result!.Symbol!.SymbolKind, Is.EqualTo(kind));
        Assert.That(attempt.Segments.Select(s => s.Mode), Is.EqualTo(decoded.Segments.Select(s => s.Mode)));
        Assert.That(attempt.Result.Warnings, Is.EqualTo(decoded.Warnings));
        Assert.That(attempt.Result.Symbol.Codewords, Is.EqualTo(decoded.Symbol!.Codewords));
        decoded.Symbol.Matrix!.Values[0] = QrModuleValue.Unknown;
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(decoder.DecodeMatrix(matrix, options).Text, Is.EqualTo("123"));
    }

    [TestCaseSource(nameof(MalformedPayloads))]
    public void ExpectedPayloadFailuresAreReturnedWithExceptionParity((int, int)[] fields, QrFailureReason reason)
    {
        QrMatrix matrix = MatrixFromBits(fields);
        AssertFailure(matrix, new QrDecodeOptions { Strict = false, EnableHanziCompatibility = true }, reason);
    }

    static IEnumerable<TestCaseData> MalformedPayloads()
    {
        yield return Bad([(6, 4)], QrFailureReason.InvalidMode);
        yield return Bad([(1, 4), (3, 10), (1000, 10)]);
        yield return Bad([(1, 4), (2, 10), (100, 7)]);
        yield return Bad([(1, 4), (1, 10), (10, 4)]);
        yield return Bad([(2, 4), (2, 9), (2025, 11)]);
        yield return Bad([(2, 4), (1, 9), (45, 6)]);
        yield return Bad([(8, 4), (1, 8), (30 * 192 + 189, 13)]);
        yield return Bad([(13, 4), (1, 4), (1, 8), (0x1fff, 13)]);
        yield return Bad([(13, 4), (2, 4)], QrFailureReason.InvalidMode);
        yield return Bad([(7, 4), (0xe0, 8)]);
        yield return Bad([(7, 4), (0x807f, 16)]);
        yield return Bad([(7, 4), (0xc0007f, 24)]);
        yield return Bad([(9, 4), (100, 8)]);
        yield return Bad([(3, 4), (0, 4), (0, 4), (0, 8)]);
        yield return Bad([(3, 4), (2, 4), (1, 4), (0, 8)]);
        yield return Bad([(5, 4), (5, 4)]);
        yield return Bad([(7, 4), (26, 8), (5, 4)]);
        yield return Bad([(4, 4), (255, 8)]);
        foreach (int mode in new[] { 1, 3, 4, 7, 9, 13 })
        {
            List<(int, int)> fields = [(4, 4), (17, 8)];
            fields.AddRange(Enumerable.Repeat((65, 8), 17));
            fields.Add((mode, 4)); // Last four bits: no storage remains for this header.
            yield return Bad(fields.ToArray());
        }
    }

    static TestCaseData Bad((int, int)[] fields, QrFailureReason reason = QrFailureReason.InvalidData) => new TestCaseData(fields, reason);

    [Test]
    public void StrictPaddingAndSegmentLimitsDoNotExposeEarlierValidSegments()
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords(
            [new QrSegment { Mode = QrEncodingMode.Numeric, Text = "7", CharacterCount = 1 }], QrSymbolKind.Model2, 1, 0, false);
        data[^1] ^= 1;
        QrMatrix matrix = MatrixFromData(data);
        AssertFailure(matrix, new QrDecodeOptions(), QrFailureReason.InvalidData);
        Assert.That(new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions { Strict = false }).Text, Is.EqualTo("7"));
        AssertFailure(MatrixFromBits([(7, 4), (26, 8), (7, 4), (3, 8)]),
            new QrDecodeOptions { MaximumSegments = 1 }, QrFailureReason.ResourceLimit);
    }

    [Test]
    public void UnsupportedEciAndExplicitEncodingPoliciesHaveExceptionParity()
    {
        QrMatrix matrix = MatrixFromBits([(7, 4), (0x8384, 16), (4, 4), (1, 8), (88, 8)]); // ECI 900.
        QrDecodeOptions options = new QrDecodeOptions { Strict = false, PreserveUnsupportedEciBytes = false };
        AssertFailure(matrix, options, QrFailureReason.UnsupportedTextEncoding);
        options.PreserveUnsupportedEciBytes = true;
        QrAttemptResult retained = new QrDecoder().TryDecodeMatrix(matrix, options);
        Assert.That(retained.Success, Is.True);
        Assert.That(retained.Text, Is.Null);
        Assert.That(retained.RawBytes, Is.EqualTo(new byte[] { 88 }));
        Assert.That(retained.Result!.EciAssignments, Is.EqualTo(new[] { 900 }));
        Assert.That(retained.Result.Warnings, Has.Some.Contains("unsupported ECI"));
        QrMatrix utf8 = MatrixFromBits([(4, 4), (2, 8), (0xc3a9, 16)]);
        options.EnableByteEncodingInference = true;
        options.RequireExplicitEci = true;
        AssertFailure(utf8, options, QrFailureReason.InvalidData);
        options.RequireExplicitEci = false;
        Assert.That(new QrDecoder().DecodeMatrix(utf8, options).Text, Is.EqualTo("é"));
    }

    [Test]
    public void RecoveryDiagnosticsIncludeErrorsErasuresAndBudgetFailure()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.Model2, 1, 0), QrSymbolKind.Model2, 1, 0);
        int error = CodewordErasureRecoveryTests.Cells(QrSymbolKind.Model2, 1, 0, 0)[0];
        matrix.Values[error] = matrix.Values[error] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        matrix.Values[CodewordErasureRecoveryTests.Cells(QrSymbolKind.Model2, 1, 0, 1)[0]] = QrModuleValue.Unknown;
        QrAttemptResult result = new QrDecoder().TryDecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(1));
        Assert.That(result.Text, Is.EqualTo("123"));
        QrModuleRole[] roles = ModuleClassifier.CreateModel2RoleMap(1, 0);
        for (int i = 0; i < roles.Length; i++)
            if (roles[i] == QrModuleRole.Data || roles[i] == QrModuleRole.ErrorCorrection) matrix.Values[i] = QrModuleValue.Unknown;
        AssertFailure(matrix, new QrDecodeOptions { MaximumRecoveryAttempts = 2 }, QrFailureReason.ResourceLimit);
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void DetectionApisSampleFreshPixelsAndPreserveMetadata(QrSymbolKind kind)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("123", new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0 });
        var (image, detection) = ImageFixture(symbol);
        detection.SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] };
        byte[] before = (byte[])image.Pixels.Clone();
        IQrDecoder decoder = new QrDecoder();
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrDecodeOptions options = new QrDecodeOptions();
        QrAttemptResult attempt = decoder.TryDecodeDetection(image, detection, sampling, options);
        QrDecodeResult decoded = decoder.DecodeDetection(image, detection, sampling, options);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.Text, Is.EqualTo(decoder.DecodeMatrix(symbol.Matrix!, options).Text));
        Assert.That(attempt.AttemptCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(attempt.Result!.Detection!.CandidateIndex, Is.EqualTo(42));
        Assert.That(attempt.Result.Detection.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(decoded.RawBytes, Is.EqualTo(attempt.RawBytes));
        Assert.That(decoded.Warnings, Is.EqualTo(attempt.Result.Warnings));
        Assert.That(detection.SampledMatrix.Values, Has.Length.EqualTo(1));
        Assert.That(image.Pixels, Is.EqualTo(before));
        Array.Fill(image.Pixels, (byte)255);
        options.MaximumRecoveryAttempts = 2;
        QrAttemptResult failed = decoder.TryDecodeDetection(image, detection, sampling, options);
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeDetection(image, detection, sampling, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(failed.FailureReason));
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.CandidateIndex, Is.EqualTo(42));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(failed.AttemptCount));
        AssertEmpty(failed);
    }

    [Test]
    public void DetectionPayloadFailureRetainsImageAttemptCountAndCandidateIndex()
    {
        var (image, detection) = ImageFixture(new QrSymbol { SymbolKind = QrSymbolKind.Model2, Matrix = MatrixFromBits([(6, 4)]) });
        IQrDecoder decoder = new QrDecoder();
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrAttemptResult attempt = decoder.TryDecodeDetection(image, detection, sampling, new QrDecodeOptions());
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
        Assert.That(attempt.AttemptCount, Is.GreaterThan(0));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeDetection(image, detection, sampling, new QrDecodeOptions()))!;
        Assert.That(error.CandidateIndex, Is.EqualTo(42));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
        AssertEmpty(attempt);
    }

    [Test]
    public void InvalidCallerInputStillThrowsInTryApis()
    {
        IQrDecoder decoder = new QrDecoder();
        QrMatrix matrix = MatrixFromBits([(6, 4)]);
        Assert.Throws<QrInputException>(() => decoder.TryDecodeMatrix(null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => decoder.TryDecodeMatrix(matrix, null!));
        Assert.Throws<QrInputException>(() => decoder.TryDecodeMatrix(matrix, new QrDecodeOptions { MaximumSegments = 0 }));
        Assert.Throws<QrResourceLimitException>(() => decoder.TryDecodeMatrix(matrix, new QrDecodeOptions { MaximumAllocationBytes = 1024 }));
        var (image, detection) = ImageFixture(new QrSymbol { SymbolKind = QrSymbolKind.Model2, Matrix = matrix });
        Assert.Throws<QrInputException>(() => decoder.TryDecodeDetection(null!, detection, new QrDetectionOptions(), new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => decoder.TryDecodeDetection(image, null!, new QrDetectionOptions(), new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => decoder.TryDecodeDetection(image, detection, null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => decoder.TryDecodeDetection(image, detection, new QrDetectionOptions(), null!));
    }

    static void AssertFailure(QrMatrix matrix, QrDecodeOptions options, QrFailureReason reason)
    {
        IQrDecoder decoder = new QrDecoder();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, options);
        Assert.That(attempt.FailureReason, Is.EqualTo(reason));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeMatrix(matrix, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(reason));
        Assert.That(error.CandidateIndex, Is.EqualTo(-1));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
        AssertEmpty(attempt);
    }

    static void AssertEmpty(QrAttemptResult attempt)
    {
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.CandidateCount, Is.EqualTo(1));
        Assert.That(attempt.Result, Is.Null);
        Assert.That(attempt.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.Empty);
        Assert.That(attempt.Segments, Is.Empty);
    }

    static QrMatrix MatrixFromBits((int value, int bits)[] fields)
    {
        BitBuffer buffer = new BitBuffer(152);
        foreach (var field in fields) buffer.AppendBits((uint)field.value, field.bits);
        while (buffer.GetBitLength() < 152) buffer.AppendBits(0, 1);
        return MatrixFromData(buffer.GetBytes());
    }

    static QrMatrix MatrixFromData(byte[] data) => CodewordErasureRecoveryTests.Build(
        BlockInterleaver.CreateModel2Codewords(data, 1, 0), QrSymbolKind.Model2, 1, 0);

    static (QrImage, QrDetection) ImageFixture(QrSymbol symbol)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 4, QuietZoneModules = 4 });
        int right = 16 + 4 * symbol.Matrix!.Width;
        int bottom = 16 + 4 * symbol.Matrix.Height;
        return (new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels },
            new QrDetection
            {
                CandidateIndex = 42, EstimatedWidth = symbol.Matrix.Width, EstimatedHeight = symbol.Matrix.Height,
                HasSymbolKindEstimate = true, EstimatedSymbolKind = symbol.SymbolKind, ModuleSize = 4, Polarity = QrPolarity.DarkOnLight,
                Corners = new QrQuadrilateral
                {
                    TopLeft = new QrPoint { X = 16, Y = 16 }, TopRight = new QrPoint { X = right, Y = 16 },
                    BottomRight = new QrPoint { X = right, Y = bottom }, BottomLeft = new QrPoint { X = 16, Y = bottom }
                }
            });
    }
}
