using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEncoderRoundTripTests
{
    [TestCaseSource(typeof(Model2CapacityCalculatorTests), nameof(Model2CapacityCalculatorTests.PublicBoundaryCases))]
    [TestCaseSource(typeof(MicroCapacityCalculatorTests), nameof(MicroCapacityCalculatorTests.PublicBoundaryCases))]
    [TestCaseSource(typeof(RectangularMicroCapacityCalculatorTests), nameof(RectangularMicroCapacityCalculatorTests.PublicBoundaryCases))]
    public void MaximumSingleModePayloadRoundTripsWithExactForcedParameters(
        QrCapacityRequest request, int countWidth, int headerBits, int capacity,
        int dataCodewords, int totalCodewords, int remainderBits)
    {
        // Capacity and count-width expectations come from independent test-owned tables.
        int count = 0;
        while (count + 1 < (1 << countWidth) && headerBits + PayloadBits(request.EncodingMode, count + 1) <= capacity) count++;
        QrSegment segment = Payload(request.EncodingMode, count);
        QrEncodeOptions options = Forced(request);
        string beforeOptions = Snapshot(options);
        string beforeSegment = Snapshot(segment);
        IQrEncoder encoder = new QrEncoder();

        QrSymbol symbol = encoder.EncodeSegments([segment], options);
        QrDecodeResult decoded = Decode(symbol);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(request.SymbolKind));
            Assert.That(symbol.Version, Is.EqualTo(request.SymbolKind == QrSymbolKind.rMQR
                ? RectangularVersion(request.Width, request.Height) : request.Version));
            Assert.That(symbol.Width, Is.EqualTo(options.MinimumWidth));
            Assert.That(symbol.Height, Is.EqualTo(options.MinimumHeight));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(request.ErrorCorrectionLevel));
            Assert.That(symbol.MaskPattern, Is.EqualTo(options.MaskPattern));
            Assert.That(symbol.DataBitCapacity, Is.EqualTo(capacity));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(headerBits + PayloadBits(segment.Mode, count)));
            Assert.That(symbol.UnusedDataBitCount, Is.EqualTo(capacity - symbol.UsedDataBitCount));
            Assert.That(symbol.DataCodewordCount, Is.EqualTo(dataCodewords));
            Assert.That(symbol.TotalCodewordCount, Is.EqualTo(totalCodewords));
            Assert.That(symbol.RemainderBitCount, Is.EqualTo(remainderBits));
            Assert.That(decoded.RawBytes, Is.EqualTo(ExpectedBytes(segment)));
            Assert.That(decoded.Text, Is.EqualTo(segment.Mode == QrEncodingMode.Byte ? Encoding.Latin1.GetString(segment.Data) : segment.Text));
            Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(new[] { segment.Mode }));
            Assert.That(decoded.Segments[0].CharacterCount, Is.EqualTo(count));
            Assert.That(decoded.UsesHanziCompatibility, Is.EqualTo(segment.Mode == QrEncodingMode.Hanzi));
            Assert.That(Snapshot(options), Is.EqualTo(beforeOptions));
            Assert.That(Snapshot(segment), Is.EqualTo(beforeSegment));
        });

        // Exercise the convenience APIs at the same maxima without invoking text optimization
        // on thousands of characters. Hanzi stays explicit because it overlaps Kanji's repertoire.
        QrSymbol repeated = segment.Mode == QrEncodingMode.Byte ? encoder.EncodeBytes(segment.Data, options)
            : segment.Mode == QrEncodingMode.Hanzi ? new QrEncoder().EncodeSegments([segment], options)
            : encoder.EncodeText(segment.Text!, options);
        AssertSameSymbol(symbol, repeated);
        QrSegment oversized = Payload(segment.Mode, count + 1);
        QrCapacityException error = Assert.Throws<QrCapacityException>(() => encoder.EncodeSegments([oversized], options))!;
        // A count-field overflow has no eligible candidate from which to report bit capacity.
        if (count + 1 < (1 << countWidth))
        {
            Assert.That(error.AvailableBitCount, Is.EqualTo(capacity));
            Assert.That(error.RequiredBitCount, Is.EqualTo(headerBits + PayloadBits(segment.Mode, count + 1)));
        }
    }

    [TestCaseSource(nameof(TextCases))]
    public void TextRoundTripsWithAndWithoutOptimization(QrSymbolKind kind, string text, bool optimize)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = RepresentativeOptions(kind);
        options.OptimizeSegments = optimize;
        if (text.Contains("😀")) options.EciAssignment = 26;
        QrSymbol symbol = encoder.EncodeText(text, options);
        Assert.That(Decode(symbol).Text, Is.EqualTo(text));
        AssertSameSymbol(symbol, encoder.EncodeText(text, options));
    }

    static IEnumerable<TestCaseData> TextCases()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR })
            foreach (string text in new[] { "", "0", "A", "hello", "éÿ\0", "漢字", "12345a6789" })
                foreach (bool optimize in new[] { false, true })
                    yield return new TestCaseData(kind, text, optimize);
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
            foreach (bool optimize in new[] { false, true })
                yield return new TestCaseData(kind, "😀e\u0301\0Ω", optimize);
    }

    [TestCase(QrSymbolKind.Model2, 256)]
    [TestCase(QrSymbolKind.Micro, 15)]
    [TestCase(QrSymbolKind.rMQR, 128)]
    public void EveryByteValueIncludingNulAndInvalidUtf8IsPreserved(QrSymbolKind kind, int chunkSize)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = RepresentativeOptions(kind);
        if (kind == QrSymbolKind.Model2)
        {
            options.MinimumVersion = options.MaximumVersion = 10;
            options.MinimumWidth = options.MaximumWidth = options.MinimumHeight = options.MaximumHeight = 57;
        }
        foreach (byte[] data in Enumerable.Range(0, 256).Select(i => (byte)i).Chunk(chunkSize).Prepend(Array.Empty<byte>()))
        {
            byte[] before = (byte[])data.Clone();
            QrSymbol symbol = encoder.EncodeBytes(data, options);
            Assert.That(Decode(symbol).RawBytes, Is.EqualTo(before));
            Assert.That(data, Is.EqualTo(before));
            AssertSameSymbol(symbol, encoder.EncodeBytes(data, options));
            if (data.Length > 0)
            {
                data[0] ^= 255;
                Assert.That(symbol.Segments.Single().Data, Is.EqualTo(before));
            }
        }
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void OptimizedMixedTextRoundTripsAcrossModeTransitions(QrSymbolKind kind)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = RepresentativeOptions(kind);
        options.OptimizeSegments = true;
        string text = "1234567890a1234567890";
        QrSymbol symbol = encoder.EncodeText(text, options);
        QrDecodeResult decoded = Decode(symbol);
        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes(text)));
        Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(new[]
        {
            QrEncodingMode.Numeric, QrEncodingMode.Byte, QrEncodingMode.Numeric
        }));
        AssertSameSymbol(symbol, encoder.EncodeText(text, options));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void ExplicitMixedModesRetainOrderPayloadAndCharacterCounts(QrSymbolKind kind)
    {
        IQrEncoder encoder = new QrEncoder();
        QrSegment[] segments =
        [
            new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 },
            new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = "AB", CharacterCount = 2 },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = [0, 255, 128], CharacterCount = 3 },
            new QrSegment { Mode = QrEncodingMode.Kanji, Text = "漢", CharacterCount = 1 }
        ];
        string before = Snapshot(segments);
        QrSymbol symbol = encoder.EncodeSegments(segments, RepresentativeOptions(kind));
        QrDecodeResult decoded = Decode(symbol);
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("123AB\0ÿ\u0080漢"));
            Assert.That(decoded.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51, 65, 66, 0, 255, 128, 0x8A, 0xBF }));
            Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(segments.Select(s => s.Mode)));
            Assert.That(decoded.Segments.Select(s => s.CharacterCount), Is.EqualTo(segments.Select(s => s.CharacterCount)));
            Assert.That(Snapshot(segments), Is.EqualTo(before));
        });
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.rMQR)]
    public void MixedEciAndPayloadModesRoundTripThroughThePublicEncoder(QrSymbolKind kind)
    {
        QrSegment[] segments =
        [
            new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 },
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 26, Data = [26] },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = Encoding.UTF8.GetBytes("😀"), CharacterCount = 4, EciAssignment = 26 },
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 3, Data = [3] },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = [0xE9], CharacterCount = 1, EciAssignment = 3 }
        ];
        IQrEncoder encoder = new QrEncoder();
        QrDecodeResult decoded = Decode(encoder.EncodeSegments(segments, RepresentativeOptions(kind)));
        Assert.That(decoded.Text, Is.EqualTo("123😀é"));
        Assert.That(decoded.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51, 0xF0, 0x9F, 0x98, 0x80, 0xE9 }));
        Assert.That(decoded.EciAssignments, Is.EqualTo(new[] { 26, 3 }));
        Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(segments.Select(s => s.Mode)));
    }

    [TestCase(QrSymbolKind.Model2, 8)]
    [TestCase(QrSymbolKind.Micro, 4)]
    [TestCase(QrSymbolKind.rMQR, 1)]
    public void ForcedAndAutomaticMasksAreRepeatableAndDecodeToTheSamePayload(QrSymbolKind kind, int maskCount)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = RepresentativeOptions(kind);
        options.OptimizeSegments = true;
        foreach (int mask in Enumerable.Range(-1, maskCount + 1))
        {
            options.MaskPattern = (QrMaskPattern)(kind == QrSymbolKind.rMQR && mask >= 0 ? 4 : mask);
            string before = Snapshot(options);
            QrSymbol first = encoder.EncodeText("123ABC", options);
            encoder.EncodeBytes([1, 2, 3], options); // Interleave another operation on the same instance.
            QrSymbol second = encoder.EncodeText("123ABC", options);
            QrSymbol fresh = new QrEncoder().EncodeText("123ABC", options);
            AssertSameSymbol(first, second);
            AssertSameSymbol(first, fresh);
            Assert.That(Decode(first).Text, Is.EqualTo("123ABC"));
            if (mask >= 0) Assert.That(first.MaskPattern, Is.EqualTo(options.MaskPattern));
            Assert.That(Snapshot(options), Is.EqualTo(before));
            QrModuleValue saved = second.Matrix!.Values[0];
            first.Matrix!.Values[0] = QrModuleValue.Unknown;
            first.Codewords[0] ^= 255;
            Assert.That(second.Matrix.Values[0], Is.EqualTo(saved));
            Assert.That(second.Codewords, Is.EqualTo(fresh.Codewords));
        }
    }

    [Test]
    public void AutomaticFamilySelectionRoundTripsAndRemainsDeterministic()
    {
        IQrEncoder encoder = new QrEncoder();
        foreach (string text in new[] { "", "12345", "ABCD1234", "a" + new string('1', 80), "é漢字😀" })
        {
            QrEncodeOptions options = new QrEncodeOptions();
            QrSymbol first = encoder.EncodeText(text, options);
            Assert.That(Decode(first).Text, Is.EqualTo(text));
            AssertSameSymbol(first, encoder.EncodeText(text, options));
        }
    }

    [Test]
    public void LegacyModel1IsRejectedByEveryOrdinaryEncodingEntryPoint()
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.LegacyModel1, AllowLegacyModel1 = true
        };
        Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeText("123", options));
        Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeBytes([1, 2, 3], options));
        Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeSegments([Payload(QrEncodingMode.Numeric, 3)], options));
    }

    static QrEncodeOptions RepresentativeOptions(QrSymbolKind kind) => Forced(new QrCapacityRequest
    {
        SymbolKind = kind, Version = kind == QrSymbolKind.Model2 ? 5 : kind == QrSymbolKind.Micro ? 4 : 0,
        Width = 139, Height = 17,
        ErrorCorrectionLevel = kind == QrSymbolKind.rMQR ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.Low
    });

    static QrEncodeOptions Forced(QrCapacityRequest request)
    {
        int width = request.SymbolKind == QrSymbolKind.Model2 ? 17 + 4 * request.Version
            : request.SymbolKind == QrSymbolKind.Micro ? 9 + 2 * request.Version : request.Width;
        int height = request.SymbolKind == QrSymbolKind.rMQR ? request.Height : width;
        return new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = request.SymbolKind,
            MinimumVersion = request.Version, MaximumVersion = request.Version == 0 ? 40 : request.Version,
            MinimumWidth = width, MaximumWidth = width, MinimumHeight = height, MaximumHeight = height,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = request.ErrorCorrectionLevel,
            BoostErrorCorrectionLevel = false, OptimizeSegments = false,
            EnableHanziCompatibility = request.EncodingMode == QrEncodingMode.Hanzi,
            MaskPattern = request.SymbolKind == QrSymbolKind.rMQR ? QrMaskPattern.Four
                : (QrMaskPattern)(request.Version % (request.SymbolKind == QrSymbolKind.Micro ? 4 : 8)),
            QuietZoneModules = 0
        };
    }

    static QrDecodeResult Decode(QrSymbol symbol)
    {
        IQrDecoder decoder = new QrDecoder();
        QrMatrix source = symbol.Matrix!;
        // Decode samples only: encoder-side segments, roles and codewords are not supplied.
        QrMatrix samples = new QrMatrix
        {
            Width = source.Width, Height = source.Height, Values = (QrModuleValue[])source.Values.Clone(),
            QuietZoneLeft = source.QuietZoneLeft, QuietZoneTop = source.QuietZoneTop,
            QuietZoneRight = source.QuietZoneRight, QuietZoneBottom = source.QuietZoneBottom
        };
        QrDecodeResult decoded = decoder.DecodeMatrix(samples, new QrDecodeOptions
        {
            EnableHanziCompatibility = symbol.UsesHanziCompatibility,
            EnableRecovery = false, TryInverted = false, TryMirrored = false, MaximumMatrixDimension = 185
        });
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Symbol!.SymbolKind, Is.EqualTo(symbol.SymbolKind));
            // Decoded rMQR metadata exposes dimensions and the zero version sentinel;
            // encoded symbols retain the placement-table index for matrix processing.
            Assert.That(decoded.Symbol.Version, Is.EqualTo(symbol.SymbolKind == QrSymbolKind.rMQR ? 0 : symbol.Version));
            Assert.That(decoded.Symbol.Width, Is.EqualTo(symbol.Width));
            Assert.That(decoded.Symbol.Height, Is.EqualTo(symbol.Height));
            Assert.That(decoded.Symbol.ErrorCorrectionLevel, Is.EqualTo(symbol.ErrorCorrectionLevel));
            Assert.That(decoded.Symbol.MaskPattern, Is.EqualTo(symbol.MaskPattern));
            Assert.That(decoded.Symbol.Codewords, Is.EqualTo(symbol.Codewords));
            Assert.That(decoded.CorrectedErrorCount, Is.Zero);
            Assert.That(decoded.CorrectedErasureCount, Is.Zero);
            Assert.That(samples.Values, Is.EqualTo(source.Values));
        });
        return decoded;
    }

    static QrSegment Payload(QrEncodingMode mode, int count)
    {
        string alphabet = mode switch
        {
            QrEncodingMode.Numeric => "0123456789", QrEncodingMode.Alphanumeric => "AB12 $%*+-./:",
            QrEncodingMode.Kanji => "漢字", QrEncodingMode.Hanzi => "中文", _ => ""
        };
        return new QrSegment
        {
            Mode = mode, CharacterCount = count, IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi,
            Text = mode == QrEncodingMode.Byte ? null : new string(Enumerable.Range(0, count).Select(i => alphabet[i % alphabet.Length]).ToArray()),
            Data = mode == QrEncodingMode.Byte ? Enumerable.Range(0, count).Select(i => (byte)((i * 73) % 256)).ToArray() : []
        };
    }

    static int RectangularVersion(int width, int height)
    {
        int index = 0;
        foreach (int h in new[] { 7, 9, 11, 13, 15, 17 })
            foreach (int w in new[] { 27, 43, 59, 77, 99, 139 })
            {
                if (w == 27 && h != 11 && h != 13) continue;
                if (w == width && h == height) return index;
                index++;
            }
        throw new AssertionException("Unexpected rMQR dimensions");
    }

    static byte[] ExpectedBytes(QrSegment segment) => segment.Mode switch
    {
        QrEncodingMode.Byte => segment.Data,
        QrEncodingMode.Kanji => Enumerable.Range(0, segment.CharacterCount).SelectMany(i => i % 2 == 0 ? new byte[] { 0x8A, 0xBF } : [0x8E, 0x9A]).ToArray(),
        QrEncodingMode.Hanzi => Enumerable.Range(0, segment.CharacterCount).SelectMany(i => i % 2 == 0 ? new byte[] { 0xD6, 0xD0 } : [0xCE, 0xC4]).ToArray(),
        _ => Encoding.ASCII.GetBytes(segment.Text!)
    };

    static int PayloadBits(QrEncodingMode mode, int count) => mode switch
    {
        QrEncodingMode.Numeric => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 == 1 ? 4 : 7),
        QrEncodingMode.Alphanumeric => count / 2 * 11 + count % 2 * 6,
        QrEncodingMode.Byte => count * 8, _ => count * 13
    };

    static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });

    static void AssertSameSymbol(QrSymbol first, QrSymbol second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(second.SymbolKind, Is.EqualTo(first.SymbolKind));
            Assert.That(second.Version, Is.EqualTo(first.Version));
            Assert.That(second.ErrorCorrectionLevel, Is.EqualTo(first.ErrorCorrectionLevel));
            Assert.That(second.MaskPattern, Is.EqualTo(first.MaskPattern));
            Assert.That(second.Codewords, Is.EqualTo(first.Codewords));
            Assert.That(second.Matrix!.Values, Is.EqualTo(first.Matrix!.Values));
            Assert.That(second.Matrix.Roles, Is.EqualTo(first.Matrix.Roles));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
            Assert.That(second.Matrix.Values, Is.Not.SameAs(first.Matrix.Values));
        });
    }
}
