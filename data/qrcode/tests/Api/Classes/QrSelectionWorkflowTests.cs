using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrSelectionWorkflowTests
{
    static IEnumerable<TestCaseData> FamilyMaskCases()
    {
        for (int families = 1; families < 8; families++)
            for (int mask = -1; mask < 8; mask++)
                yield return new TestCaseData(families, mask);
    }

    [TestCaseSource(nameof(FamilyMaskCases))]
    public void CapacitySelectionAndEncodingRespectTheSameFamilyAndMaskRestrictions(int families, int mask)
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            AllowModel2 = (families & 1) != 0, AllowMicro = (families & 2) != 0,
            AllowRectangularMicro = (families & 4) != 0, MaskPattern = (QrMaskPattern)mask,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium, BoostErrorCorrectionLevel = false, QuietZoneModules = 0
        };
        QrSymbolKind expected = (families & 2) != 0 && mask <= 3 ? QrSymbolKind.Micro
            : (families & 4) != 0 && (mask == -1 || mask == 4) ? QrSymbolKind.rMQR : QrSymbolKind.Model2;
        bool fits = expected != QrSymbolKind.Model2 || (families & 1) != 0;
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        IQrEncoder encoder = new QrEncoder();
        QrSegment[] segments = [Numeric("1")];
        QrCapacityRequest request = new QrCapacityRequest { EncodingMode = QrEncodingMode.Numeric, CharacterCount = 1, PayloadBitCount = 4 };
        if (!fits)
        {
            Assert.Throws<QrCapacityException>(() => calculator.SelectSymbolParameters(request, options));
            Assert.Throws<QrCapacityException>(() => encoder.EncodeSegments(segments, options));
            QrEncodeAttemptResult attempt = encoder.TryEncodeSegments(segments, options);
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
            Assert.That(attempt.Symbol, Is.Null);
            return;
        }

        QrCapacityRequest selected = calculator.SelectSymbolParameters(request, options);
        QrSymbol symbol = encoder.EncodeSegments(segments, options);
        Assert.That(selected.SymbolKind, Is.EqualTo(expected));
        Assert.That(symbol.SymbolKind, Is.EqualTo(expected));
        Assert.That((selected.Width, selected.Height), Is.EqualTo((symbol.Width, symbol.Height)));
        Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(symbol.ErrorCorrectionLevel));
        if (mask >= 0) Assert.That(symbol.MaskPattern, Is.EqualTo((QrMaskPattern)mask));
        Assert.That(new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions()).Text, Is.EqualTo("1"));
    }

    [TestCase(-2)]
    [TestCase(8)]
    [TestCase(int.MaxValue)]
    public void UndefinedMasksAreInvalidForBothPublicSelectionPaths(int mask)
    {
        QrEncodeOptions options = new QrEncodeOptions { MaskPattern = (QrMaskPattern)mask };
        QrCapacityRequest request = new QrCapacityRequest { EncodingMode = QrEncodingMode.Numeric, CharacterCount = 1, PayloadBitCount = 4 };
        Assert.That(Assert.Throws<QrInputException>(() => new QrCapacityCalculator().SelectSymbolParameters(request, options))!.InputName,
            Is.EqualTo("MaskPattern"));
        Assert.That(Assert.Throws<QrInputException>(() => new QrEncoder().TryEncodeSegments([Numeric("1")], options))!.InputName,
            Is.EqualTo("MaskPattern"));
    }

    [Test]
    public void EqualAreaRectanglesChooseTheShorterLongestSideConsistently()
    {
        // R7x99 and R9x77 both have 693 modules. R7x77 cannot hold 22 bytes at M.
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.rMQR, MinimumWidth = 77, MaximumWidth = 99,
            MinimumHeight = 7, MaximumHeight = 9, ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
            BoostErrorCorrectionLevel = false, QuietZoneModules = 0
        };
        QrCapacityRequest request = new QrCapacityRequest { EncodingMode = QrEncodingMode.Byte, CharacterCount = 22, PayloadBitCount = 176 };
        byte[] data = Enumerable.Range(0, 22).Select(i => (byte)i).ToArray();
        QrSymbol first = new QrEncoder().EncodeBytes(data, options);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            QrCapacityRequest selected = new QrCapacityCalculator().SelectSymbolParameters(request, options);
            QrSymbol symbol = new QrEncoder().EncodeBytes(data, options);
            Assert.That((selected.Width, selected.Height), Is.EqualTo((77, 9)));
            Assert.That((symbol.Width, symbol.Height), Is.EqualTo((77, 9)));
            Assert.That(symbol.Matrix!.Values, Is.EqualTo(first.Matrix!.Values));
        }
    }

    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 7, QrErrorCorrectionLevel.High)]
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 8, QrErrorCorrectionLevel.Quartile)]
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 11, QrErrorCorrectionLevel.Quartile)]
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 13, QrErrorCorrectionLevel.Medium)]
    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 15, QrErrorCorrectionLevel.Low)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Low, 9, QrErrorCorrectionLevel.Quartile)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Low, 10, QrErrorCorrectionLevel.Medium)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Low, 14, QrErrorCorrectionLevel.Low)]
    [TestCase(QrSymbolKind.rMQR, 0, QrErrorCorrectionLevel.Medium, 1, QrErrorCorrectionLevel.High)]
    [TestCase(QrSymbolKind.rMQR, 0, QrErrorCorrectionLevel.Medium, 3, QrErrorCorrectionLevel.Medium)]
    public void EncoderBoostsOnlyWithinSelectedSizeAndHonorsBothDisableOptions(
        QrSymbolKind kind, int version, QrErrorCorrectionLevel minimum, int count, QrErrorCorrectionLevel strongest)
    {
        QrEncodeOptions options = FixedOptions(kind, version, minimum);
        byte[] payload = new byte[count];
        foreach (bool force in new[] { false, true })
            foreach (bool boost in new[] { false, true })
            {
                options.ForceErrorCorrectionLevel = force;
                options.BoostErrorCorrectionLevel = boost;
                QrSymbol symbol = new QrEncoder().EncodeBytes(payload, options);
                Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(boost && !force ? strongest : minimum));
                Assert.That((symbol.Width, symbol.Height), Is.EqualTo((options.MinimumWidth, options.MinimumHeight)));
                Assert.That(new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions()).RawBytes, Is.EqualTo(payload));
            }
    }

    [TestCase(QrSymbolKind.Model2, 1, QrErrorCorrectionLevel.Low, 8)]
    [TestCase(QrSymbolKind.Model2, 7, QrErrorCorrectionLevel.High, 8)]
    [TestCase(QrSymbolKind.Micro, 1, QrErrorCorrectionLevel.None, 4)]
    [TestCase(QrSymbolKind.Micro, 2, QrErrorCorrectionLevel.Low, 4)]
    [TestCase(QrSymbolKind.Micro, 3, QrErrorCorrectionLevel.Medium, 4)]
    [TestCase(QrSymbolKind.Micro, 4, QrErrorCorrectionLevel.Quartile, 4)]
    public void EveryForcedMaskPreservesCodewordsAndDecodesItsFormatMetadata(QrSymbolKind kind, int version, QrErrorCorrectionLevel level, int maskCount)
    {
        QrEncodeOptions options = FixedOptions(kind, version, level);
        QrSymbol? first = null;
        HashSet<string> matrices = new HashSet<string>();
        for (int mask = 0; mask < maskCount; mask++)
        {
            options.MaskPattern = (QrMaskPattern)mask;
            QrSymbol symbol = new QrEncoder().EncodeSegments([Numeric("12345")], options);
            first ??= symbol;
            Assert.That(symbol.MaskPattern, Is.EqualTo((QrMaskPattern)mask));
            Assert.That(symbol.Codewords, Is.EqualTo(first.Codewords));
            QrDecodeResult decoded = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
            Assert.That(decoded.Text, Is.EqualTo("12345"));
            Assert.That(decoded.Symbol!.MaskPattern, Is.EqualTo((QrMaskPattern)mask));
            matrices.Add(string.Concat(symbol.Matrix!.Values.Select(v => (int)v)));
        }
        Assert.That(matrices.Count, Is.EqualTo(maskCount));
    }

    static QrSegment Numeric(string text) => new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = text.Length, Text = text };

    [TestCase(17, 1, 1)]
    [TestCase(18, 1, 2)]
    [TestCase(230, 9, 9)]
    [TestCase(231, 9, 10)]
    [TestCase(1367, 26, 26)]
    [TestCase(1368, 26, 27)]
    public void BytePayloadCrossesVersionAndCountHeaderBoundaries(int count, int minimumVersion, int expectedVersion)
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            MinimumVersion = minimumVersion, BoostErrorCorrectionLevel = false, MaskPattern = QrMaskPattern.Zero, QuietZoneModules = 0
        };
        QrCapacityRequest request = new QrCapacityRequest { EncodingMode = QrEncodingMode.Byte, CharacterCount = count, PayloadBitCount = count * 8 };
        Assert.That(new QrCapacityCalculator().SelectSymbolParameters(request, options).Version, Is.EqualTo(expectedVersion));
        QrSymbol symbol = new QrEncoder().EncodeBytes(new byte[count], options);
        Assert.That(symbol.Version, Is.EqualTo(expectedVersion));
        Assert.That(symbol.UsedDataBitCount, Is.EqualTo(count * 8 + (expectedVersion < 10 ? 12 : 20)));
    }

    [TestCase(QrSymbolKind.Model2, QrErrorCorrectionLevel.None)]
    [TestCase(QrSymbolKind.Micro, QrErrorCorrectionLevel.High)]
    [TestCase(QrSymbolKind.rMQR, QrErrorCorrectionLevel.Low)]
    [TestCase(QrSymbolKind.rMQR, QrErrorCorrectionLevel.Quartile)]
    public void UnsupportedForcedFamilyAndEccCannotBeRescuedByBoosting(QrSymbolKind kind, QrErrorCorrectionLevel level)
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind, ErrorCorrectionLevel = level, BoostErrorCorrectionLevel = true
        };
        QrCapacityRequest request = new QrCapacityRequest { EncodingMode = QrEncodingMode.Numeric, CharacterCount = 1, PayloadBitCount = 4 };
        Assert.Throws<QrCapacityException>(() => new QrCapacityCalculator().SelectSymbolParameters(request, options));
        Assert.Throws<QrCapacityException>(() => new QrEncoder().EncodeSegments([Numeric("1")], options));
        Assert.That(new QrEncoder().TryEncodeSegments([Numeric("1")], options).Success, Is.False);
    }

    [TestCase(QrSymbolKind.Model2, 1, "88", 2, 6)]
    [TestCase(QrSymbolKind.Micro, 2, "9", 1, 2)]
    public void AutomaticMaskTiesChooseTheLowestMaskDeterministically(QrSymbolKind kind, int version, string text, int winner, int tied)
    {
        QrEncodeOptions options = FixedOptions(kind, version, QrErrorCorrectionLevel.Low);
        int[] scores = new int[kind == QrSymbolKind.Model2 ? 8 : 4];
        QrSymbol[] symbols = new QrSymbol[scores.Length];
        for (int mask = 0; mask < scores.Length; mask++)
        {
            options.MaskPattern = (QrMaskPattern)mask;
            QrSymbol symbol = new QrEncoder().EncodeSegments([Numeric(text)], options);
            symbols[mask] = symbol;
            BitMatrixBuffer buffer = new BitMatrixBuffer(symbol.Width, symbol.Height);
            for (int row = 0; row < symbol.Height; row++)
                for (int column = 0; column < symbol.Width; column++)
                    buffer.SetModule(row, column, symbol.Matrix!.Values[row * symbol.Width + column] == QrModuleValue.Dark);
            scores[mask] = kind == QrSymbolKind.Model2 ? MaskEvaluator.CalculateModel2Penalty(buffer) : MaskEvaluator.CalculateMicroScore(buffer);
        }
        int best = kind == QrSymbolKind.Model2 ? scores.Min() : scores.Max();
        Assert.That(scores[winner], Is.EqualTo(best));
        Assert.That(scores[tied], Is.EqualTo(best));
        Assert.That(Array.IndexOf(scores, best), Is.EqualTo(winner));
        options.MaskPattern = QrMaskPattern.Automatic;
        for (int repeat = 0; repeat < 3; repeat++)
        {
            QrSymbol symbol = new QrEncoder().EncodeSegments([Numeric(text)], options);
            Assert.That(symbol.MaskPattern, Is.EqualTo((QrMaskPattern)winner));
            Assert.That(symbol.Matrix!.Values, Is.EqualTo(symbols[winner].Matrix!.Values));
        }
    }

    static QrEncodeOptions FixedOptions(QrSymbolKind kind, int version, QrErrorCorrectionLevel level)
    {
        int width = kind == QrSymbolKind.Model2 ? 17 + version * 4 : kind == QrSymbolKind.Micro ? 9 + version * 2 : 43;
        int height = kind == QrSymbolKind.rMQR ? 7 : width;
        return new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind, MinimumVersion = version, MaximumVersion = version == 0 ? 40 : version,
            MinimumWidth = width, MaximumWidth = width, MinimumHeight = height, MaximumHeight = height,
            ErrorCorrectionLevel = level, BoostErrorCorrectionLevel = false, QuietZoneModules = 0
        };
    }
}
