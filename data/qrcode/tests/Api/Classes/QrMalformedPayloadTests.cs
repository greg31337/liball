using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMalformedPayloadTests
{
    static IEnumerable<TestCaseData> Payloads()
    {
        foreach (int mode in new[] { 6, 10, 11, 12, 14, 15 })
            yield return Case($"ReservedMode{mode}", [(mode, 4)], QrFailureReason.InvalidMode);
        foreach (int mode in new[] { 1, 2, 4, 8 })
        {
            int countWidth = mode == 1 ? 10 : mode == 2 ? 9 : 8;
            yield return Case($"ImpossibleCount{mode}", [(mode, 4), ((1 << countWidth) - 1, countWidth)]);
            yield return Case($"TruncatedCount{mode}", Tail([(mode, 4)], 17));
        }
        yield return Case("TruncatedByteData", Tail([(4, 4), (1, 8)], 16));
        yield return Case("TruncatedEciFirstByte", Tail([(7, 4)], 17));
        yield return Case("TruncatedEciSecondByte", Tail([(7, 4), (0x80, 8)], 16));
        yield return Case("TruncatedEciThirdByte", Tail([(7, 4), (0xc0, 8), (0x40, 8)], 15));
        yield return Case("ReservedEciPrefix", [(7, 4), (0xe0, 8)]);
        yield return Case("EciAboveMaximum", [(7, 4), (0xcf4240, 24)]);
        yield return Case("OverlongTwoByteEci", [(7, 4), (0x807f, 16)]);
        yield return Case("OverlongThreeByteEci", [(7, 4), (0xc03fff, 24)]);
        yield return Case("InvalidNumericTriple", [(1, 4), (3, 10), (1000, 10)]);
        yield return Case("InvalidNumericPair", [(1, 4), (2, 10), (100, 7)]);
        yield return Case("InvalidNumericDigit", [(1, 4), (1, 10), (10, 4)]);
        yield return Case("InvalidAlphanumericPair", [(2, 4), (2, 9), (2025, 11)]);
        yield return Case("InvalidAlphanumericCharacter", [(2, 4), (1, 9), (45, 6)]);
        yield return Case("InvalidFnc1Indicator", [(9, 4), (100, 8)]);
        yield return Case("InvalidAppendIndex", [(3, 4), (2, 4), (1, 4), (0, 8)]);
        yield return Case("InvalidAppendTotal", [(3, 4), (0, 4), (0, 4), (0, 8)]);
    }

    [TestCaseSource(nameof(Payloads))]
    public void MalformedPayloadsFailDeterministicallyAcrossDecoderEntryPoints(string name, (int value, int width)[] fields, QrFailureReason reason)
    {
        QrSymbol symbol = Build(fields, QrSymbolKind.Model2, 1, 0);
        QrModuleValue[] before = (QrModuleValue[])symbol.Matrix!.Values.Clone();
        QrImage image = Render(symbol.Matrix);
        byte[] pixels = (byte[])image.Pixels.Clone();
        IQrDecoder decoder = new QrDecoder();
        foreach (bool strict in new[] { false, true })
            foreach (bool imageInput in new[] { false, true })
            {
                QrDecodeOptions options = Decoding();
                options.Strict = strict;
                QrDetection detection = Detection(symbol.Matrix);
                QrAttemptResult Attempt() => imageInput
                    ? decoder.TryDecodeDetection(image, detection, Sampling(), options)
                    : decoder.TryDecodeMatrix(symbol.Matrix, options);
                QrAttemptResult attempt = Attempt();
                AssertFailed(attempt, reason);
                QrDecodeException error = Assert.Throws<QrDecodeException>(() =>
                {
                    if (imageInput) decoder.DecodeDetection(image, detection, Sampling(), options);
                    else decoder.DecodeMatrix(symbol.Matrix, options);
                })!;
                Assert.That(error.FailureReason, Is.EqualTo(reason));
                Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
                QrAttemptResult repeated = Attempt();
                AssertFailed(repeated, reason);
                Assert.That(repeated.AttemptCount, Is.EqualTo(attempt.AttemptCount));
            }
        Assert.That(symbol.Matrix.Values, Is.EqualTo(before));
        Assert.That(image.Pixels, Is.EqualTo(pixels));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void ReservedMicroModesCannotBecomePartialPayloads(int mode)
    {
        QrSymbol symbol = Build([(mode, 3)], QrSymbolKind.Micro, 4, 0);
        AssertFailed(new QrDecoder().TryDecodeMatrix(symbol.Matrix!, Decoding()), QrFailureReason.InvalidMode);
    }

    [TestCase(QrSymbolKind.rMQR, 0, 0, 3, 3, 7)]
    [TestCase(QrSymbolKind.Micro, 4, 0, 2, 3, 31)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0, 4, 4, 255)]
    public void ProfileSpecificImpossibleCountsAreRejected(QrSymbolKind kind, int version, int level, int mode, int modeWidth, int count)
    {
        int countWidth = kind == QrSymbolKind.rMQR ? 3 : kind == QrSymbolKind.Micro ? 5 : 8;
        // rMQR's smallest symbol has room for five bytes, Micro M4 for sixteen.
        QrSymbol symbol = Build([(mode, modeWidth), (count, countWidth)], kind, version, level);
        AssertFailed(new QrDecoder().TryDecodeMatrix(symbol.Matrix!, Decoding()), QrFailureReason.InvalidData);
    }

    [TestCase(0xe0, 8)]
    [TestCase(0x807f, 16)]
    [TestCase(0xc03fff, 24)]
    [TestCase(0xcf4240, 24)]
    public void RectangularEciDesignatorsUseTheSameStrictValidation(int designator, int width)
    {
        QrSymbol symbol = Build([(7, 3), (designator, width)], QrSymbolKind.rMQR, 0, 0);
        AssertFailed(new QrDecoder().TryDecodeMatrix(symbol.Matrix!, Decoding()), QrFailureReason.InvalidData);
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(9)]
    [TestCase(13)]
    [TestCase(15)]
    public void LegacyProfileRejectsUnsupportedControlAndReservedModes(int mode)
    {
        QrSymbol symbol = Build([(mode, 4)], QrSymbolKind.LegacyModel1, 1, 0);
        AssertFailed(new QrDecoder().TryDecodeMatrix(symbol.Matrix!, Decoding()), QrFailureReason.InvalidMode);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ScannersRetainMalformedCandidateFailureWithoutReturningItsValidPrefix(bool imageInput)
    {
        QrSymbol symbol = Build(Tail([(6, 4)], 17), QrSymbolKind.Model2, 1, 0);
        QrImage image = Render(symbol.Matrix!);
        QrMatrix binary = new QrBinarizer().BinarizeImage(image, Sampling())[0];
        IQrScanner scanner = new QrScanner();
        QrDecodeSet result = imageInput ? scanner.ScanImage(image, Sampling(), Decoding(), true)
            : scanner.ScanBinaryMatrix(binary, Sampling(), Decoding(), true);
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.FailedAttempts, Is.Not.Empty);
        Assert.That(result.FailedAttempts.Any(f => f.FailureReason == QrFailureReason.InvalidMode), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void QualityAnalysisRejectsMalformedPayloadEvenWithCorrectParity(bool imageInput)
    {
        QrSymbol symbol = Build([(4, 4), (255, 8)], QrSymbolKind.Model2, 1, 0);
        IQrQualityAnalyzer analyzer = new QrQualityAnalyzer();
        QrDecodeException error = Assert.Throws<QrDecodeException>(() =>
        {
            if (imageInput) analyzer.AnalyzeImageQuality(Render(symbol.Matrix!), Detection(symbol.Matrix!), Sampling(), new QrQualityOptions());
            else analyzer.AnalyzeMatrixQuality(symbol.Matrix!, new QrQualityOptions());
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    static TestCaseData Case(string name, (int, int)[] fields, QrFailureReason reason = QrFailureReason.InvalidData) =>
        new TestCaseData(name, fields, reason).SetName($"MalformedPayload_{name}");
    static (int, int)[] Tail((int, int)[] suffix, int prefixBytes) =>
        new[] { (4, 4), (prefixBytes, 8) }.Concat(Enumerable.Repeat((65, 8), prefixBytes)).Concat(suffix).ToArray();

    static QrSymbol Build((int value, int width)[] fields, QrSymbolKind kind, int version, int level)
    {
        int capacity = kind == QrSymbolKind.Model2 ? EccBlockTable.GetDataCodewordCount(version, level) * 8
            : kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataBitCapacity(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetDataBitCapacity(version, level)
            : LegacyModel1Table.GetDataCodewordCount(version, level) * 8;
        BitBuffer bits = new BitBuffer(capacity);
        if (kind == QrSymbolKind.LegacyModel1) bits.AppendBits(0, 4);
        foreach (var field in fields) bits.AppendBits((uint)field.value, field.width);
        Assert.That(bits.GetBitLength(), Is.LessThanOrEqualTo(capacity));
        while (bits.GetBitLength() < capacity) bits.AppendBits(0, 1);
        byte[] words = BlockInterleaver.CreateCodewords(bits.GetBytes(), kind, version, level);
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, level);
        return new QrSymbol { SymbolKind = kind, Version = version, Matrix = matrix };
    }
    internal static QrDecodeOptions Decoding() => new QrDecodeOptions
    {
        EnableRecovery = false, TryMirrored = false, TryInverted = false, AllowLegacyModel1 = true
    };
    internal static QrDetectionOptions Sampling() => new QrDetectionOptions
    {
        Polarity = QrPolarity.DarkOnLight, BinarizationMode = QrBinarizationMode.GlobalHistogram,
        MaximumPyramidLevels = 1, AllowMicro = false, AllowRectangularMicro = false, AllowLegacyModel1 = false
    };
    internal static QrImage Render(QrMatrix matrix)
    {
        int width = (matrix.Width + 8) * 4, height = (matrix.Height + 8) * 4;
        QrImage image = new QrImage { Width = width, Height = height, Stride = width,
            Pixels = Enumerable.Repeat((byte)255, width * height).ToArray() };
        for (int y = 0; y < matrix.Height * 4; y++)
            for (int x = 0; x < matrix.Width * 4; x++)
                image.Pixels[(y + 16) * width + x + 16] = matrix.Values[y / 4 * matrix.Width + x / 4] == QrModuleValue.Dark ? (byte)0 : (byte)255;
        return image;
    }
    internal static QrDetection Detection(QrMatrix matrix) => new QrDetection
    {
        EstimatedWidth = matrix.Width, EstimatedHeight = matrix.Height, Polarity = QrPolarity.DarkOnLight,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 16, Y = 16 }, TopRight = new QrPoint { X = 16 + matrix.Width * 4, Y = 16 },
            BottomRight = new QrPoint { X = 16 + matrix.Width * 4, Y = 16 + matrix.Height * 4 },
            BottomLeft = new QrPoint { X = 16, Y = 16 + matrix.Height * 4 }
        }
    };
    static void AssertFailed(QrAttemptResult attempt, QrFailureReason reason)
    {
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(reason));
        Assert.That(attempt.Result, Is.Null);
        Assert.That(attempt.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.Empty);
        Assert.That(attempt.Segments, Is.Empty);
        Assert.That(attempt.AttemptCount, Is.GreaterThan(0));
    }
}
