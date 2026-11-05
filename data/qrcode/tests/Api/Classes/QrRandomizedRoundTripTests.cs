using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrRandomizedRoundTripTests
{
    // Each case owns its PRNG. The explicit seed is part of the NUnit case name,
    // so execution order and parallel execution cannot change generated inputs.
    static IEnumerable<TestCaseData> Profiles()
    {
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
        {
            int first = kind == QrSymbolKind.rMQR ? 0 : 1;
            int last = kind == QrSymbolKind.Model2 ? 40 : kind == QrSymbolKind.Micro ? 4 : kind == QrSymbolKind.rMQR ? 31 : 14;
            for (int version = first; version <= last; version++)
                foreach (QrEncodingMode mode in new[] { QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji, QrEncodingMode.Hanzi })
                {
                    if (mode == QrEncodingMode.Hanzi && kind != QrSymbolKind.Model2) continue;
                    if (kind == QrSymbolKind.Micro && (version == 1 && mode != QrEncodingMode.Numeric
                        || version == 2 && mode is QrEncodingMode.Byte or QrEncodingMode.Kanji)) continue;
                    int seed = 1520000 + (int)kind * 10000 + version * 100 + (int)mode;
                    yield return new TestCaseData(kind, version, mode, seed)
                        .SetName($"RandomRoundTrip_{kind}_v{version}_{mode}_seed{seed}");
                }
        }
    }

    [TestCaseSource(nameof(Profiles))]
    public void RandomPayloadsAndForcedOptionsRoundTripThroughTransforms(QrSymbolKind kind, int version, QrEncodingMode mode, int seed)
    {
        Random random = new Random(seed);
        int level = Level(random, kind, version);
        QrEncodeOptions options = Options(random, kind, version, level);
        int capacity = kind == QrSymbolKind.Model2 ? EccBlockTable.GetDataCodewordCount(version, level) * 8
            : kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataBitCapacity(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetDataBitCapacity(version, level)
            : LegacyModel1Table.GetDataCodewordCount(version, level) * 8 - 4;
        int maximum = capacity < 50 ? 1 : Math.Min(8, (capacity - 24) / 13);
        QrSegment segment = Segment(random, mode, random.Next(1, maximum + 1));
        string input = Snapshot(segment), configuration = Snapshot(options);
        TestContext.Out.WriteLine($"seed={seed}; options={configuration}; segment={input}");
        QrSymbol symbol = Build([segment], options, version, level);
        QrSymbol repeated = Build([segment], options, version, level);
        Assert.That(repeated.Codewords, Is.EqualTo(symbol.Codewords));
        Assert.That(repeated.Matrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(Snapshot(segment), Is.EqualTo(input));
        Assert.That(Snapshot(options), Is.EqualTo(configuration));
        Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(options.ErrorCorrectionLevel));
        if (options.MaskPattern != QrMaskPattern.Automatic) Assert.That(symbol.MaskPattern, Is.EqualTo(options.MaskPattern));
        QrMatrix source = symbol.Matrix;
        source.Confidence = Enumerable.Range(0, source.Values.Length).Select(_ => (byte)random.Next(128, 256)).ToArray();
        string before = Snapshot(source);
        int turns = random.Next(4), quiet = random.Next(6);
        bool mirror = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        bool invert = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        TestContext.Out.WriteLine($"turns={turns}; mirror={mirror}; invert={invert}; quiet={quiet}");
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix transformed = processor.TransformMatrix(source, kind, turns, mirror, invert, 0, 0, 0, 0, true, quiet, 1);
        QrDecodeOptions decoding = Decoding();
        decoding.Strict = random.Next(2) == 1;
        QrDecodeResult result = new QrDecoder().DecodeMatrix(transformed, decoding);
        Assert.That(result.RawBytes, Is.EqualTo(Bytes(segment)));
        Assert.That(result.Text, Is.EqualTo(mode == QrEncodingMode.Byte ? Encoding.Latin1.GetString(segment.Data) : segment.Text));
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(new[] { mode }));
        Assert.That(result.Symbol!.SymbolKind, Is.EqualTo(kind));
        Assert.That(result.Symbol.Codewords, Is.EqualTo(symbol.Codewords));
        Assert.That(result.CorrectedErrorCount, Is.Zero);
        Assert.That(result.CorrectedErasureCount, Is.Zero);
        // The inverse must restore values and confidence; strip added margins
        // separately because quiet-zone replacement changes the geometry.
        QrMatrix restored = processor.TransformMatrix(transformed, kind, mirror ? turns : (4 - turns) % 4,
            mirror, invert, 0, 0, 0, 0, false, 0, 1);
        restored = processor.TransformMatrix(restored, kind, 0, false, false, 0, 0, 0, 0, true, 0, 1);
        Assert.That(restored.Values, Is.EqualTo(source.Values));
        Assert.That(restored.Confidence, Is.EqualTo(source.Confidence));
        if (source.Roles != null) Assert.That(restored.Roles, Is.EqualTo(source.Roles));
        Assert.That(Snapshot(source), Is.EqualTo(before));
    }

    static IEnumerable<TestCaseData> MixedCases()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR })
            for (int sample = 0; sample < 12; sample++)
            {
                int seed = 1525000 + (int)kind * 100 + sample;
                yield return new TestCaseData(kind, seed).SetName($"{{m}}_{kind}_seed{seed}");
            }
    }

    [TestCaseSource(nameof(MixedCases))]
    public void RandomMixedSegmentsPreserveBoundariesAndIndependentExpectedBytes(QrSymbolKind kind, int seed)
    {
        Random random = new Random(seed);
        int version = kind == QrSymbolKind.Model2 ? random.Next(3, 9) : kind == QrSymbolKind.Micro ? 4 : 31;
        int level = kind == QrSymbolKind.rMQR ? random.Next(2) : 0;
        QrEncodeOptions options = Options(random, kind, version, level);
        QrEncodingMode[] modes = { QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji };
        random.Shuffle(modes);
        QrSegment[] segments = modes.Select(mode => Segment(random, mode, kind == QrSymbolKind.Micro ? 1 : random.Next(1, 5))).ToArray();
        TestContext.Out.WriteLine($"seed={seed}; options={Snapshot(options)}; segments={Snapshot(segments)}");
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments, options);
        QrDecodeResult decoded = new QrDecoder().DecodeMatrix(symbol.Matrix!, Decoding());
        Assert.That(decoded.RawBytes, Is.EqualTo(segments.SelectMany(Bytes).ToArray()));
        Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(modes));
        Assert.That(decoded.Segments.Select(s => s.CharacterCount), Is.EqualTo(segments.Select(s => s.CharacterCount)));
    }

    [TestCaseSource(nameof(MixedCases))]
    public void RandomTextAgreesAcrossOptimizationAndAutomaticSelection(QrSymbolKind kind, int seed)
    {
        Random random = new Random(seed);
        string[] tokens = kind == QrSymbolKind.Micro ? ["1", "A", "a", "é", "漢"] : ["123", "A%", "abc", "é", "漢", "😀", "e\u0301", "\0"];
        string text = string.Concat(Enumerable.Range(0, kind == QrSymbolKind.Micro ? 3 : random.Next(3, 9)).Select(_ => tokens[random.Next(tokens.Length)]));
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind, BoostErrorCorrectionLevel = random.Next(2) == 1,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            QuietZoneModules = random.Next(5), EciAssignment = kind == QrSymbolKind.Micro ? -1 : 26
        };
        if (kind == QrSymbolKind.rMQR) options.ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium;
        TestContext.Out.WriteLine($"seed={seed}; text={Snapshot(text)}; options={Snapshot(options)}");
        IQrEncoder encoder = new QrEncoder();
        foreach (bool optimize in new[] { false, true })
        {
            options.OptimizeSegments = optimize;
            QrEncodeAttemptResult attempt = encoder.TryEncodeText(text, options);
            bool requiresMixedModes = kind == QrSymbolKind.Micro && !optimize
                && text.Contains('漢') && text.Any(c => c != '漢');
            if (requiresMixedModes)
            {
                Assert.That(attempt.Success, Is.False);
                Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
                Assert.That(attempt.Symbol, Is.Null);
                Assert.That(attempt.Matrix, Is.Null);
                Assert.That(Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeText(text, options))!.FailureReason,
                    Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
                continue;
            }
            Assert.That(attempt.Success, Is.True);
            QrSymbol symbol = encoder.EncodeText(text, options);
            Assert.That(symbol.Matrix!.Values, Is.EqualTo(attempt.Matrix!.Values));
            Assert.That(new QrDecoder().DecodeMatrix(symbol.Matrix, Decoding()).Text, Is.EqualTo(text));
        }
        options.ForceSymbolKind = false;
        Assert.That(new QrDecoder().DecodeMatrix(encoder.EncodeText(text, options).Matrix!, Decoding()).Text, Is.EqualTo(text));
    }

    internal static int Level(Random random, QrSymbolKind kind, int version) => kind == QrSymbolKind.Micro
        ? version == 1 ? -1 : random.Next(version == 4 ? 3 : 2) : random.Next(kind == QrSymbolKind.rMQR ? 2 : 4);

    internal static QrEncodeOptions Options(Random random, QrSymbolKind kind, int version, int level)
    {
        bool automaticMask = kind != QrSymbolKind.LegacyModel1 && random.Next(4) == 0;
        return new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind, MinimumVersion = kind == QrSymbolKind.rMQR ? 1 : version,
            MaximumVersion = kind == QrSymbolKind.rMQR ? 40 : version,
            MinimumWidth = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetWidth(version) : 0,
            MaximumWidth = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetWidth(version) : 0,
            MinimumHeight = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetHeight(version) : 0,
            MaximumHeight = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetHeight(version) : 0,
            ErrorCorrectionLevel = kind == QrSymbolKind.rMQR ? level == 0 ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.High : (QrErrorCorrectionLevel)level,
            ForceErrorCorrectionLevel = true, BoostErrorCorrectionLevel = false, QuietZoneModules = 0,
            EnableHanziCompatibility = true,
            MaskPattern = automaticMask ? QrMaskPattern.Automatic : kind == QrSymbolKind.rMQR ? QrMaskPattern.Four : (QrMaskPattern)random.Next(kind == QrSymbolKind.Micro ? 4 : 8)
        };
    }

    internal static QrSymbol Build(QrSegment[] segments, QrEncodeOptions options, int version, int level)
    {
        if (options.SymbolKind != QrSymbolKind.LegacyModel1) return new QrEncoder().EncodeSegments(segments, options);
        // Legacy is decode-only in the public API; synthesize a test fixture
        // using existing test-side placement and compare with the input payload.
        byte[] data = CodewordFinalizer.CreateDataCodewords(segments, QrSymbolKind.LegacyModel1, version, level, false);
        byte[] words = BlockInterleaver.CreateLegacyModel1Codewords(data, version, level);
        QrMatrix matrix = LegacyModel1DecoderTests.Build(words, version, level, (int)options.MaskPattern, false);
        return new QrSymbol { SymbolKind = QrSymbolKind.LegacyModel1, Version = version, ErrorCorrectionLevel = (QrErrorCorrectionLevel)level,
            MaskPattern = options.MaskPattern, Matrix = matrix, Width = matrix.Width, Height = matrix.Height, Codewords = words };
    }

    internal static QrSegment Segment(Random random, QrEncodingMode mode, int count)
    {
        string alphabet = mode switch
        {
            QrEncodingMode.Numeric => "0123456789", QrEncodingMode.Alphanumeric => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:",
            QrEncodingMode.Kanji => "漢字", QrEncodingMode.Hanzi => "中文", _ => ""
        };
        byte[] data = mode == QrEncodingMode.Byte ? new byte[count] : [];
        random.NextBytes(data);
        return new QrSegment { Mode = mode, CharacterCount = count, Data = data, IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi,
            Text = mode == QrEncodingMode.Byte ? null : new string(Enumerable.Range(0, count).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray()) };
    }
    internal static byte[] Bytes(QrSegment segment) => segment.Mode switch
    {
        QrEncodingMode.Byte => segment.Data,
        QrEncodingMode.Kanji => segment.Text!.SelectMany(c => c == '漢' ? new byte[] { 0x8a, 0xbf } : [0x8e, 0x9a]).ToArray(),
        QrEncodingMode.Hanzi => segment.Text!.SelectMany(c => c == '中' ? new byte[] { 0xd6, 0xd0 } : [0xce, 0xc4]).ToArray(),
        _ => Encoding.ASCII.GetBytes(segment.Text!)
    };
    internal static QrDecodeOptions Decoding() => new QrDecodeOptions
    {
        AllowLegacyModel1 = true, EnableHanziCompatibility = true, EnableRecovery = false, MaximumMatrixDimension = 200
    };
    internal static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
