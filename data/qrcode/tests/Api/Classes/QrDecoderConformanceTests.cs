using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDecoderConformanceTests
{
    static readonly QrEncodingMode[] DataModes = [QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji];

    static IEnumerable<TestCaseData> DataCases()
    {
        foreach (int version in new[] { 1, 6, 7, 9, 10, 26, 27, 40 })
            for (int level = 0; level < 4; level++)
                foreach (QrEncodingMode mode in DataModes) yield return new(QrSymbolKind.Model2, version, level, mode);
        foreach (int version in new[] { 1, 9, 10, 14 })
            for (int level = 0; level < 4; level++)
                foreach (QrEncodingMode mode in DataModes) yield return new(QrSymbolKind.LegacyModel1, version, level, mode);
        for (int version = 1; version <= 4; version++)
            foreach (int level in version == 1 ? new[] { -1 } : version == 4 ? new[] { 0, 1, 2 } : new[] { 0, 1 })
                foreach (QrEncodingMode mode in DataModes.Take(version == 1 ? 1 : version == 2 ? 2 : 4))
                    yield return new(QrSymbolKind.Micro, version, level, mode);
        for (int version = 0; version < 32; version++)
            for (int level = 0; level < 2; level++)
                foreach (QrEncodingMode mode in DataModes) yield return new(QrSymbolKind.rMQR, version, level, mode);
    }

    [TestCaseSource(nameof(DataCases))]
    public void BothMatrixApisPreserveEveryDataModeAcrossProfilesAndBoundaries(
        QrSymbolKind kind, int version, int level, QrEncodingMode mode)
    {
        QrSegment segment = Segment(mode);
        byte[] data = CodewordFinalizer.CreateDataCodewords([segment], kind, version, level, false);
        byte[] words = BlockInterleaver.CreateCodewords(data, kind, version, level);
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(words, kind, version, level);
        string before = Snapshot(matrix);
        IQrDecoder decoder = new QrDecoder();
        QrDecodeOptions options = new();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, options);
        Assert.That(attempt.Success, Is.True, attempt.FailureReason.ToString());
        QrDecodeResult result = decoder.DecodeMatrix(matrix, options);
        Assert.That(Snapshot(result), Is.EqualTo(Snapshot(attempt.Result!)));
        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.EqualTo(segment.Text));
            Assert.That(result.RawBytes, Is.EqualTo(segment.Data));
            Assert.That(result.Segments, Has.Length.EqualTo(1));
            Assert.That(result.Segments[0].Mode, Is.EqualTo(mode));
            Assert.That(result.Segments[0].CharacterCount, Is.EqualTo(segment.CharacterCount));
            Assert.That(result.Segments[0].Text, Is.EqualTo(segment.Text));
            Assert.That(result.Segments[0].Data, Is.EqualTo(segment.Data));
            Assert.That(result.Symbol!.SymbolKind, Is.EqualTo(kind));
            Assert.That(result.Symbol.Version, Is.EqualTo(kind == QrSymbolKind.rMQR ? 0 : version));
            Assert.That(result.Symbol.Width, Is.EqualTo(matrix.Width));
            Assert.That(result.Symbol.Height, Is.EqualTo(matrix.Height));
            Assert.That(result.Symbol.ErrorCorrectionLevel, Is.EqualTo(kind == QrSymbolKind.rMQR
                ? level == 0 ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.High : (QrErrorCorrectionLevel)level));
            Assert.That(result.Symbol.Codewords, Is.EqualTo(words));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Detection, Is.Null);
            Assert.That(result.EciAssignments, Is.Empty);
            Assert.That(result.SymbologyIdentifier, Is.EqualTo(kind == QrSymbolKind.LegacyModel1 ? "]Q0" : "]Q1"));
            Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(attempt.CandidateCount, Is.EqualTo(1));
            Assert.That(attempt.AttemptCount, Is.InRange(1, options.MaximumRecoveryAttempts));
        });
        var (image, detection) = ImageFixture(matrix, kind);
        QrDecodeResult sampled = decoder.DecodeDetection(image, detection,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, options);
        Assert.That(sampled.Text, Is.EqualTo(result.Text));
        Assert.That(sampled.RawBytes, Is.EqualTo(result.RawBytes));
        Assert.That(Snapshot(sampled.Segments), Is.EqualTo(Snapshot(result.Segments)));
        Assert.That(sampled.Symbol!.Codewords, Is.EqualTo(words));
        result.Segments[0].Data[0] ^= 255;
        result.Symbol!.Codewords[0] ^= 255;
        result.Symbol.Matrix!.Values[0] = QrModuleValue.Unknown;
        Assert.That(Snapshot(matrix), Is.EqualTo(before));
        Assert.That(attempt.RawBytes, Is.EqualTo(segment.Data));
        Assert.That(attempt.Segments[0].Data, Is.EqualTo(segment.Data));
        Assert.That(attempt.Result!.Symbol!.Codewords, Is.EqualTo(words));
    }

    static IEnumerable<TestCaseData> OrientationCases()
    {
        foreach (var profile in new[] { (QrSymbolKind.Model2, 1), (QrSymbolKind.Model2, 40),
            (QrSymbolKind.Micro, 1), (QrSymbolKind.Micro, 4), (QrSymbolKind.rMQR, 0), (QrSymbolKind.rMQR, 31),
            (QrSymbolKind.LegacyModel1, 1), (QrSymbolKind.LegacyModel1, 14) })
            for (int turn = 0; turn < 4; turn++)
                foreach (bool mirror in new[] { false, true })
                    foreach (bool invert in new[] { false, true })
                        yield return new(profile.Item1, profile.Item2, turn, mirror, invert);
    }

    [TestCaseSource(nameof(OrientationCases))]
    public void OrientationPoliciesAndDetectionDecodingPreserveTheSamePayload(
        QrSymbolKind kind, int version, int turn, bool mirror, bool invert)
    {
        int level = kind == QrSymbolKind.Micro && version == 1 ? -1 : 0;
        QrMatrix original = Build([Segment(QrEncodingMode.Numeric)], kind, version, level);
        QrMatrix matrix = MatrixTransformer.Rotate(original, turn);
        if (mirror) matrix = MatrixTransformer.Mirror(matrix);
        if (invert) matrix = MatrixTransformer.InvertPolarity(matrix);
        IQrDecoder decoder = new QrDecoder();
        QrDecodeOptions options = new() { EnableRecovery = false };
        if (kind == QrSymbolKind.LegacyModel1 && (mirror || invert))
        {
            // Legacy Model 1 intentionally accepts only ordinary polarity and
            // rotations, even when modern-profile orientation retries are enabled.
            QrAttemptResult rejected = decoder.TryDecodeMatrix(matrix, options);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.FailureReason, Is.Not.EqualTo(QrFailureReason.None));
            AssertFailure(matrix, options, rejected.FailureReason);
            return;
        }
        QrDecodeResult direct = decoder.DecodeMatrix(matrix, options);
        Assert.That(direct.Text, Is.EqualTo("007"));
        Assert.That(direct.Symbol!.Matrix!.Values, Is.EqualTo(original.Values));
        Assert.That(direct.Warnings.Any(w => w.Contains("mirrored")), Is.EqualTo(mirror));
        Assert.That(direct.Warnings.Any(w => w.Contains("reversed-reflectance")), Is.EqualTo(invert));
        if (mirror)
            Assert.That(decoder.TryDecodeMatrix(matrix, new QrDecodeOptions { EnableRecovery = false, TryMirrored = false }).Success, Is.False);
        if (invert)
        {
            Assert.That(decoder.TryDecodeMatrix(matrix, new QrDecodeOptions { EnableRecovery = false, TryInverted = false }).Success, Is.False);
            Assert.That(decoder.DecodeMatrix(matrix, new QrDecodeOptions { TryInverted = false, Polarity = QrPolarity.LightOnDark }).Text, Is.EqualTo("007"));
        }

        var (image, detection) = ImageFixture(matrix, kind);
        detection.Polarity = invert ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight;
        string before = Snapshot(detection);
        byte[] pixels = (byte[])image.Pixels.Clone();
        QrDetectionOptions sampling = new() { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrAttemptResult attempt = decoder.TryDecodeDetection(image, detection, sampling, options);
        Assert.That(attempt.Success, Is.True, attempt.FailureReason.ToString());
        QrDecodeResult sampled = decoder.DecodeDetection(image, detection, sampling, options);
        Assert.That(Snapshot(sampled), Is.EqualTo(Snapshot(attempt.Result!)));
        Assert.That(sampled.Text, Is.EqualTo(direct.Text));
        Assert.That(sampled.RawBytes, Is.EqualTo(direct.RawBytes));
        Assert.That(Snapshot(sampled.Segments), Is.EqualTo(Snapshot(direct.Segments)));
        Assert.That(sampled.Symbol!.Codewords, Is.EqualTo(direct.Symbol.Codewords));
        Assert.That(sampled.Symbol.SymbolKind, Is.EqualTo(kind));
        Assert.That(sampled.Detection!.CandidateIndex, Is.EqualTo(42));
        Assert.That(sampled.Detection, Is.Not.SameAs(detection));
        Assert.That(Snapshot(detection), Is.EqualTo(before));
        Assert.That(image.Pixels, Is.EqualTo(pixels));
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Model2, 40, 3)]
    [TestCase(QrSymbolKind.Micro, 1, -1)]
    [TestCase(QrSymbolKind.Micro, 2, 0)]
    [TestCase(QrSymbolKind.Micro, 3, 0)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void StrictPaddingRejectsEveryChangedPaddingBitButLenientModeRetainsPayload(QrSymbolKind kind, int version, int level)
    {
        QrSegment[] segments = [Segment(QrEncodingMode.Numeric)];
        byte[] data = CodewordFinalizer.CreateDataCodewords(segments, kind, version, level, false);
        int payload = CodewordFinalizer.CalculateRequiredBitCount(segments, kind, version, false, 10, 100);
        int terminator = kind == QrSymbolKind.Micro ? MicroVersionTable.GetTerminatorBitCount(version) : kind == QrSymbolKind.rMQR ? 3 : 4;
        int prefix = kind == QrSymbolKind.LegacyModel1 ? 4 : 0;
        int capacity = kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataBitCapacity(version, level) : data.Length * 8 - prefix;
        // First/last padding positions and every bit in the first two pad bytes
        // cover alignment zeros, EC/11 alternation and Micro's partial final byte.
        int start = prefix + payload + terminator;
        int end = prefix + capacity;
        int[] changedBits = Enumerable.Range(start, Math.Min(20, end - start)).Append(end - 1).Distinct().ToArray();
        foreach (int bit in changedBits)
        {
            byte[] changed = (byte[])data.Clone();
            changed[bit / 8] ^= (byte)(1 << (7 - bit % 8));
            QrMatrix matrix = FromData(changed, kind, version, level);
            AssertFailure(matrix, new QrDecodeOptions(), QrFailureReason.InvalidData);
            QrDecodeResult lenient = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions { Strict = false });
            Assert.That(lenient.Text, Is.EqualTo("007"), $"padding bit {bit}");
            Assert.That(lenient.RawBytes, Is.EqualTo(new byte[] { 48, 48, 55 }));
            Assert.That(lenient.CorrectedErrorCount, Is.Zero);
        }
    }

    [Test]
    public void MixedModesAndEciChangesPreserveControlOrderAndRawRepresentations(
        [Values(QrSymbolKind.Model2, QrSymbolKind.rMQR)] QrSymbolKind kind, [Values(0, 1, 2)] int fnc1)
    {
        List<QrSegment> segments = [];
        if (fnc1 != 0) segments.Add(new QrSegment { Mode = fnc1 == 1 ? QrEncodingMode.Fnc1First : QrEncodingMode.Fnc1Second,
            Fnc1ApplicationIndicator = fnc1 == 2 ? 165 : -1, Data = fnc1 == 2 ? [165] : [] });
        segments.Add(Segment(QrEncodingMode.Numeric));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = fnc1 == 0 ? "X%%%" : "X%\u001d", CharacterCount = fnc1 == 0 ? 4 : 3 });
        segments.Add(EciEncoder.CreateControlSegment(26));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 2, Data = [0xc3, 0xa9] });
        segments.Add(EciEncoder.CreateControlSegment(3));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [0xe9] });
        segments.Add(Segment(QrEncodingMode.Kanji));
        QrMatrix matrix = Build(segments.ToArray(), kind, kind == QrSymbolKind.Model2 ? 3 : 31, 0);
        IQrDecoder decoder = new QrDecoder();
        QrDecodeResult result = decoder.DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(segments.Select(s => s.Mode)));
        Assert.That(result.Text, Is.EqualTo((fnc1 == 0 ? "007X%%%" : "007X%\u001d") + "éé漢"));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 48, 48, 55, 88, 37, 37, 37, 0xc3, 0xa9, 0xe9, 0x8a, 0xbf }));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 26, 3 }));
        Assert.That(result.UsesFnc1First, Is.EqualTo(fnc1 == 1));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(fnc1 == 2 ? 165 : -1));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(fnc1 == 0 ? "]Q2" : fnc1 == 1 ? "]Q4" : "]Q6"));
        QrSegment alpha = result.Segments.Single(s => s.Mode == QrEncodingMode.Alphanumeric);
        Assert.That(alpha.CharacterCount, Is.EqualTo(4));
        Assert.That(alpha.Data, Is.EqualTo(new byte[] { 88, 37, 37, 37 }));
        Assert.That(result.Segments.Where(s => s.Mode == QrEncodingMode.Byte).Select(s => s.EciAssignment), Is.EqualTo(new[] { 26, 3 }));
        Assert.That(result.Segments.Where(s => s.Mode == QrEncodingMode.Eci).SelectMany(s => s.Data), Is.EqualTo(new byte[] { 26, 3 }));
        if (fnc1 == 2) Assert.That(result.Segments[0].Data, Is.EqualTo(new byte[] { 165 }));
        var (image, detection) = ImageFixture(matrix, kind);
        QrDecodeResult sampled = decoder.DecodeDetection(image, detection,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, new QrDecodeOptions());
        Assert.That(Snapshot(sampled.Segments), Is.EqualTo(Snapshot(result.Segments)));
        Assert.That(sampled.RawBytes, Is.EqualTo(result.RawBytes));
        Assert.That(sampled.Text, Is.EqualTo(result.Text));
        Assert.That(sampled.SymbologyIdentifier, Is.EqualTo(result.SymbologyIdentifier));
        AssertFailure(matrix, new QrDecodeOptions { MaximumSegments = segments.Count - 1 }, QrFailureReason.ResourceLimit);
        Assert.That(decoder.DecodeMatrix(matrix, new QrDecodeOptions { MaximumSegments = segments.Count }).Text, Is.EqualTo(result.Text));
    }

    [Test]
    public void StructuredAppendHeadersRetainBoundaryIndicesWithoutEnteringPayloadBytes(
        [Values(1, 40)] int version,
        [Values(0, 15)] int index, [Values(0, 255)] int parity)
    {
        QrSymbolKind kind = QrSymbolKind.Model2;
        QrMatrix matrix = Build([
            new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = 16, Parity = parity } },
            Segment(QrEncodingMode.Numeric)], kind, version, 0);
        QrDecodeResult result = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("007"));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 48, 48, 55 }));
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.StructuredAppend, QrEncodingMode.Numeric }));
        Assert.That(result.Segments[0].Data, Is.Empty);
        Assert.That(result.StructuredAppend!.Index, Is.EqualTo(index));
        Assert.That(result.StructuredAppend.Total, Is.EqualTo(16));
        Assert.That(result.StructuredAppend.Parity, Is.EqualTo(parity));
        Assert.That(result.StructuredAppend.IsComplete, Is.False);
        var (image, detection) = ImageFixture(matrix, kind);
        QrDecodeResult sampled = new QrDecoder().DecodeDetection(image, detection,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, new QrDecodeOptions());
        Assert.That(Snapshot(sampled.StructuredAppend!), Is.EqualTo(Snapshot(result.StructuredAppend)));
        Assert.That(Snapshot(sampled.Segments), Is.EqualTo(Snapshot(result.Segments)));
    }

    [Test]
    public void UnsupportedEciWidthsPreserveBytesAndLaterSegmentsWithoutPartialText(
        [Values(QrSymbolKind.Model2, QrSymbolKind.rMQR)] QrSymbolKind kind,
        [Values(127, 128, 16383, 16384, 999999)] int assignment)
    {
        QrMatrix matrix = Build([
            EciEncoder.CreateControlSegment(assignment),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 2, Data = [0, 255] },
            EciEncoder.CreateControlSegment(3),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [65] }
        ], kind, kind == QrSymbolKind.Model2 ? 1 : 31, 0);
        QrDecodeResult result = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0, 255, 65 }));
        Assert.That(result.Segments[1].Text, Is.Null);
        Assert.That(result.Segments[3].Text, Is.EqualTo("A"));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { assignment, 3 }));
        byte[] designator = assignment < 128 ? [(byte)assignment]
            : assignment < 16384 ? [(byte)(0x80 | (assignment >> 8)), (byte)(assignment & 255)]
            : [(byte)(0xc0 | (assignment >> 16)), (byte)((assignment >> 8) & 255), (byte)(assignment & 255)];
        Assert.That(result.Segments[0].Data, Is.EqualTo(designator));
        Assert.That(result.Warnings, Has.Some.Contains($"unsupported ECI {assignment}"));
        AssertFailure(matrix, new QrDecodeOptions { PreserveUnsupportedEciBytes = false }, QrFailureReason.UnsupportedTextEncoding);
    }

    [Test]
    public void HanziCompatibilityIsExplicitAndPreservesGb2312(
        [Values(1, 9, 10, 26, 27, 40)] int version)
    {
        QrSegment hanzi = new() { Mode = QrEncodingMode.Hanzi, Text = "汉", CharacterCount = 1, IsHanziCompatibilityMode = true };
        QrMatrix matrix = Build([hanzi], QrSymbolKind.Model2, version, 0, true);
        AssertFailure(matrix, new QrDecodeOptions(), QrFailureReason.InvalidMode);
        QrDecodeOptions options = new() { EnableHanziCompatibility = true };
        IQrDecoder decoder = new QrDecoder();
        QrDecodeResult result = decoder.DecodeMatrix(matrix, options);
        Assert.That(result.Text, Is.EqualTo("汉"));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0xba, 0xba }));
        Assert.That(result.UsesHanziCompatibility, Is.True);
        Assert.That(result.Symbol!.UsesHanziCompatibility, Is.True);
        Assert.That(result.Segments.Single().IsHanziCompatibilityMode, Is.True);
        Assert.That(result.Segments[0].Mode, Is.EqualTo(QrEncodingMode.Hanzi));
        var (image, detection) = ImageFixture(matrix, QrSymbolKind.Model2);
        QrDecodeResult sampled = decoder.DecodeDetection(image, detection,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }, options);
        Assert.That(Snapshot(sampled.Segments), Is.EqualTo(Snapshot(result.Segments)));
    }

    [TestCase(QrSymbolKind.Model2, 1, 0, 41)]
    [TestCase(QrSymbolKind.Micro, 1, -1, 5)]
    [TestCase(QrSymbolKind.rMQR, 0, 0, 12)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0, 40)]
    public void FullCapacityAndTruncatedTerminatorsAreAccepted(QrSymbolKind kind, int version, int level, int count)
    {
        string text = new('7', count);
        QrMatrix matrix = Build([new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = count, Text = text }], kind, version, level);
        QrDecodeResult result = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Symbol!.UnusedDataBitCount, Is.InRange(0, 1));
        Assert.That(result.Segments.Single().CharacterCount, Is.EqualTo(count));
    }

    [Test]
    public void LegacyReservedControlsRemainInvalidInLenientMode(
        [Values(3, 5, 7, 9, 13)] int mode, [Values(false, true)] bool strict)
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords([Segment(QrEncodingMode.Numeric)], QrSymbolKind.LegacyModel1, 1, 0, false);
        data[0] = (byte)mode;
        QrMatrix matrix = FromData(data, QrSymbolKind.LegacyModel1, 1, 0);
        AssertFailure(matrix, new QrDecodeOptions { Strict = strict, EnableHanziCompatibility = true }, QrFailureReason.InvalidMode);
    }

    [TestCase(QrSymbolKind.Model2, 1, 0)]
    [TestCase(QrSymbolKind.Micro, 1, -1)]
    [TestCase(QrSymbolKind.Micro, 4, 0)]
    [TestCase(QrSymbolKind.rMQR, 0, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 1, 0)]
    public void TerminatorOnlyPayloadIsEmptyAndFamilyOptOutRejectsIt(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Build([], kind, version, level);
        IQrDecoder decoder = new QrDecoder();
        QrDecodeResult result = decoder.DecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.RawBytes, Is.Empty);
        Assert.That(result.Segments, Is.Empty);
        Assert.That(result.Symbol!.UsedDataBitCount, Is.Zero);
        QrDecodeOptions disabled = new()
        {
            AllowModel2 = kind != QrSymbolKind.Model2, AllowMicro = kind != QrSymbolKind.Micro,
            AllowRectangularMicro = kind != QrSymbolKind.rMQR, AllowLegacyModel1 = kind != QrSymbolKind.LegacyModel1,
            EnableRecovery = false
        };
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, disabled);
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.Result, Is.Null);
    }

    static QrSegment Segment(QrEncodingMode mode) => mode switch
    {
        QrEncodingMode.Numeric => new() { Mode = mode, Text = "007", CharacterCount = 3, Data = [48, 48, 55] },
        QrEncodingMode.Alphanumeric => new() { Mode = mode, Text = "A%", CharacterCount = 2, Data = [65, 37] },
        QrEncodingMode.Byte => new() { Mode = mode, Text = "\0é", CharacterCount = 2, Data = [0, 0xe9] },
        _ => new() { Mode = mode, Text = "漢", CharacterCount = 1, Data = [0x8a, 0xbf] }
    };

    static QrMatrix Build(QrSegment[] segments, QrSymbolKind kind, int version, int level, bool hanzi = false) =>
        FromData(CodewordFinalizer.CreateDataCodewords(segments, kind, version, level, hanzi), kind, version, level);

    static QrMatrix FromData(byte[] data, QrSymbolKind kind, int version, int level) =>
        CodewordErasureRecoveryTests.Build(BlockInterleaver.CreateCodewords(data, kind, version, level), kind, version, level);

    static void AssertFailure(QrMatrix matrix, QrDecodeOptions options, QrFailureReason reason)
    {
        IQrDecoder decoder = new QrDecoder();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, options);
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(reason));
        Assert.That(attempt.Result, Is.Null);
        Assert.That(attempt.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.Empty);
        Assert.That(attempt.Segments, Is.Empty);
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => decoder.DecodeMatrix(matrix, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(reason));
        Assert.That(error.CandidateIndex, Is.EqualTo(-1));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
    }

    static (QrImage, QrDetection) ImageFixture(QrMatrix matrix, QrSymbolKind kind)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(new QrSymbol { Matrix = matrix, SymbolKind = kind },
            new QrRenderOptions { Scale = 3, QuietZoneModules = 4 });
        int right = 12 + 3 * matrix.Width, bottom = 12 + 3 * matrix.Height;
        return (new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels },
            new QrDetection
            {
                CandidateIndex = 42, EstimatedWidth = matrix.Width, EstimatedHeight = matrix.Height,
                HasSymbolKindEstimate = true, EstimatedSymbolKind = kind, ModuleSize = 3, Polarity = QrPolarity.DarkOnLight,
                SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] },
                Corners = new QrQuadrilateral
                {
                    TopLeft = new QrPoint { X = 12, Y = 12 }, TopRight = new QrPoint { X = right, Y = 12 },
                    BottomRight = new QrPoint { X = right, Y = bottom }, BottomLeft = new QrPoint { X = 12, Y = bottom }
                }
            });
    }

    static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
