using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrStructuredAppendEncodingTests
{
    [Test]
    public void AutomaticEncodingRoundTripsEveryPartCountAndLevel(
        [Range(2, 16)] int count,
        [Values(QrErrorCorrectionLevel.Low, QrErrorCorrectionLevel.Medium,
            QrErrorCorrectionLevel.Quartile, QrErrorCorrectionLevel.High)] QrErrorCorrectionLevel level)
    {
        // Version 1 has 19/16/13/9 data codewords. SA + byte headers cost 32 bits.
        int capacity = new[] { 15, 12, 9, 5 }[(int)level];
        byte[] payload = Message((count - 1) * capacity + 1);
        QrEncodeOptions options = Options();
        options.ErrorCorrectionLevel = level;
        options.MaximumStructuredAppendSymbols = count;
        string before = Snapshot(options);
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(payload, options);

        AssertSequence(sequence, payload, count);
        Assert.That(Snapshot(options), Is.EqualTo(before));
        foreach (QrSymbol symbol in sequence.Symbols)
        {
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(symbol.Segments[^1].Data.Length, Is.LessThanOrEqualTo(capacity));
            Assert.That(symbol.Version, Is.EqualTo(1));
        }
        if (count > 2)
        {
            options.MaximumStructuredAppendSymbols = count - 1;
            AssertCapacityFailure(payload, options, (count - 1) * capacity);
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(255)]
    public void ParityPreservesEveryBitInTheWireHeader(int parity)
    {
        byte[] payload = [0xA5, (byte)(0xA5 ^ parity)];
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(payload, Options());
        AssertSequence(sequence, payload, 2);
        for (int index = 0; index < 2; index++)
        {
            // Version 1 has one RS block, so its first data bytes are not interleaved.
            byte[] codewords = sequence.Symbols[index].Codewords;
            Assert.That(codewords[0], Is.EqualTo(0x30 | index));
            Assert.That(codewords[1], Is.EqualTo(0x10 | (parity >> 4)));
            Assert.That(codewords[2] >> 4, Is.EqualTo(parity & 15));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    public void EmptyAndSingleByteInputsRoundTripIncludingTheEmptyPart(int length)
    {
        byte[] payload = Message(length);
        AssertSequence(new QrEncoder().EncodeStructuredAppend(payload, Options()), payload, 2);
    }

    [Test]
    public void AutomaticControlsAreRepeatedAndChargedAtEveryEciWidthBoundary(
        [Values(-1, 3, 26, 127, 128, 16383, 16384, 999999)] int assignment,
        [Values(0, 1, 2)] int fnc1)
    {
        QrEncodeOptions options = Options();
        options.EciAssignment = assignment;
        options.UseFnc1First = fnc1 == 1;
        options.Fnc1SecondApplicationIndicator = fnc1 == 2 ? 42 : -1;
        options.MaximumStructuredAppendSymbols = 3;
        int eciBits = assignment < 0 ? 0 : assignment < 128 ? 12 : assignment < 16384 ? 20 : 28;
        int headerBits = 32 + eciBits + (fnc1 == 1 ? 4 : fnc1 == 2 ? 12 : 0);
        int capacity = (152 - headerBits) / 8;
        // ASCII is valid for both Latin-1 and UTF-8; unknown assignments retain bytes.
        byte[] payload = Enumerable.Repeat((byte)'A', capacity * 3).ToArray();
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(payload, options);
        AssertSequence(sequence, payload, 3);
        foreach (QrSymbol symbol in sequence.Symbols)
        {
            QrDecodeResult decoded = Decode(symbol);
            Assert.Multiple(() =>
            {
                Assert.That(decoded.EciAssignments, Is.EqualTo(assignment < 0 ? Array.Empty<int>() : new[] { assignment }));
                Assert.That(decoded.UsesFnc1First, Is.EqualTo(fnc1 == 1));
                Assert.That(decoded.Fnc1SecondApplicationIndicator, Is.EqualTo(fnc1 == 2 ? 42 : -1));
                Assert.That(symbol.UsedDataBitCount, Is.EqualTo(headerBits + capacity * 8));
                Assert.That(symbol.Segments[^1].Data, Has.Length.EqualTo(capacity));
            });
            QrEncodingMode[] expected = new[] { QrEncodingMode.StructuredAppend }
                .Concat(fnc1 == 0 ? [] : new[] { fnc1 == 1 ? QrEncodingMode.Fnc1First : QrEncodingMode.Fnc1Second })
                .Concat(assignment < 0 ? [] : new[] { QrEncodingMode.Eci })
                .Append(QrEncodingMode.Byte).ToArray();
            Assert.That(decoded.Segments.Select(s => s.Mode), Is.EqualTo(expected));
        }
        AssertCapacityFailure(new byte[payload.Length + 1], options, payload.Length);
        options.MaximumSegments = 2 + (assignment < 0 ? 0 : 1) + (fnc1 == 0 ? 0 : 1) - 1;
        Assert.That(() => new QrEncoder().EncodeStructuredAppend(payload, options),
            Throws.TypeOf<QrResourceLimitException>());
    }

    [TestCase(QrErrorCorrectionLevel.Low, 2956)]
    [TestCase(QrErrorCorrectionLevel.Medium, 2334)]
    [TestCase(QrErrorCorrectionLevel.Quartile, 1666)]
    [TestCase(QrErrorCorrectionLevel.High, 1276)]
    public void MaximumSixteenPartPayloadFitsExactlyAndOneMoreByteFails(
        QrErrorCorrectionLevel level, int dataCodewords)
    {
        QrEncodeOptions options = Options();
        options.MinimumVersion = 40;
        options.MaximumVersion = 40;
        options.ErrorCorrectionLevel = level;
        // SA 20 + byte mode 4 + version-40 count 16 = 40 bits.
        int capacity = dataCodewords - 5;
        byte[] payload = Message(16 * capacity);
        options.MaximumInputBytes = payload.Length + 1;
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(payload, options);
        AssertSequence(sequence, payload, 16);
        foreach (QrSymbol symbol in sequence.Symbols)
        {
            Assert.That(symbol.Version, Is.EqualTo(40));
            Assert.That(symbol.Segments[^1].Data.Length, Is.EqualTo(capacity));
            Assert.That(symbol.UsedDataBitCount, Is.EqualTo(dataCodewords * 8));
            Assert.That(symbol.UnusedDataBitCount, Is.Zero);
        }
        AssertCapacityFailure(new byte[payload.Length + 1], options, payload.Length);
        options.MaximumInputBytes = payload.Length - 1;
        QrResourceLimitException exception = Assert.Throws<QrResourceLimitException>(() =>
            new QrEncoder().EncodeStructuredAppend(payload, options))!;
        Assert.That(exception.LimitName, Is.EqualTo("MaximumInputBytes"));
        Assert.That(exception.RequestedValue, Is.EqualTo(payload.Length));
    }

    [Test]
    public void PartitionAreaMatchesExhaustivePayloadBoundarySearch(
        [Values(29, 30, 31, 45, 46, 60, 61, 81, 82, 106, 107, 155, 157)] int length,
        [Values(false, true)] bool restrictDimensions)
    {
        QrEncodeOptions options = Options();
        options.MaximumVersion = 4;
        if (restrictDimensions)
        {
            options.MinimumWidth = 25;
            options.MaximumHeight = 29;
        }
        int[] capacities = restrictDimensions ? [30, 51] : [15, 30, 51, 76];
        int[] areas = restrictDimensions ? [625, 841] : [441, 625, 841, 1089];
        int count = Math.Max(2, (length + capacities[^1] - 1) / capacities[^1]);
        // Independently enumerate every possible byte boundary, including partial parts.
        int[,] best = new int[count + 1, length + 1];
        for (int part = 0; part <= count; part++)
            for (int used = 0; used <= length; used++)
                best[part, used] = int.MaxValue;
        best[0, 0] = 0;
        for (int part = 1; part <= count; part++)
            for (int used = 0; used <= length; used++)
                for (int take = 0; take <= Math.Min(used, capacities[^1]); take++)
                    if (best[part - 1, used - take] != int.MaxValue)
                    {
                        int candidate = Array.FindIndex(capacities, c => c >= take);
                        best[part, used] = Math.Min(best[part, used], best[part - 1, used - take] + areas[candidate]);
                    }
        byte[] payload = Message(length);
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(payload, options);
        AssertSequence(sequence, payload, count);
        Assert.That(sequence.Symbols.Sum(s => s.Width * s.Height), Is.EqualTo(best[count, length]));
        QrSymbolSequence repeated = new QrEncoder().EncodeStructuredAppend(payload, options);
        Assert.That(repeated.Symbols.Select(s => s.Segments[^1].Data.Length),
            Is.EqualTo(sequence.Symbols.Select(s => s.Segments[^1].Data.Length)));
    }

    [Test]
    public void ManualPartsRoundTripWithIndependentControlsAndNoInputMutation([Range(2, 16)] int count)
    {
        byte[][] payloads = Enumerable.Range(0, count)
            .Select(i => Encoding.ASCII.GetBytes($"PART-{i}")).ToArray();
        QrSegment[][] parts = ManualParts(payloads);
        for (int i = 0; i < count; i++)
        {
            QrSegment fnc1 = i % 2 == 0
                ? new QrSegment { Mode = QrEncodingMode.Fnc1First }
                : new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = 42, Data = [42] };
            int assignment = i % 2 == 0 ? 3 : 26;
            parts[i] = [parts[i][0], fnc1,
                new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = assignment, Data = [(byte)assignment] },
                parts[i][1]];
        }
        string before = Snapshot(parts);
        QrSymbolSequence sequence = new QrEncoder().EncodeManualStructuredAppend(parts, Options());
        AssertSequence(sequence, payloads.SelectMany(p => p).ToArray(), count);
        Assert.That(Snapshot(parts), Is.EqualTo(before));
        for (int i = 0; i < count; i++)
        {
            QrDecodeResult decoded = Decode(sequence.Symbols[i]);
            Assert.That(decoded.EciAssignments, Is.EqualTo(new[] { i % 2 == 0 ? 3 : 26 }));
            Assert.That(decoded.UsesFnc1First, Is.EqualTo(i % 2 == 0));
            Assert.That(decoded.Fnc1SecondApplicationIndicator, Is.EqualTo(i % 2 == 0 ? -1 : 42));
            Assert.That(sequence.Symbols[i].Segments[0].StructuredAppend!.ApplicationIdentifier, Is.EqualTo("manual"));
        }
    }

    [Test]
    public void ManualTextModesComputeParityAndBytesFromTheEncodedText(
        [Values(QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Kanji, QrEncodingMode.Hanzi)] QrEncodingMode mode,
        [Values(false, true)] bool staleData)
    {
        string text = mode switch
        {
            QrEncodingMode.Numeric => "123",
            QrEncodingMode.Alphanumeric => "ABC",
            QrEncodingMode.Kanji => "漢",
            _ => "汉"
        };
        byte[] bytes = mode switch
        {
            QrEncodingMode.Kanji => [0x8A, 0xBF],
            QrEncodingMode.Hanzi => [0xBA, 0xBA],
            _ => Encoding.ASCII.GetBytes(text)
        };
        QrSegment[][] parts = ManualParts([bytes, [0x41]]);
        parts[1][1].Text = "A";
        parts[0][1] = new QrSegment
        {
            Mode = mode, Text = text, CharacterCount = text.Length,
            Data = staleData ? [0xFF] : [], IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi
        };
        string before = Snapshot(parts);
        QrEncodeOptions options = Options();
        options.EnableHanziCompatibility = true;
        byte[] expected = bytes.Append((byte)0x41).ToArray();
        QrSymbolSequence sequence = new QrEncoder().EncodeManualStructuredAppend(parts, options);
        AssertSequence(sequence, expected, 2);
        Assert.That(sequence.Text, Is.EqualTo(text + "A"));
        Assert.That(Snapshot(parts), Is.EqualTo(before));
        options.MaximumInputBytes = expected.Length - 1;
        QrResourceLimitException limit = Assert.Throws<QrResourceLimitException>(() =>
            new QrEncoder().EncodeManualStructuredAppend(parts, options))!;
        Assert.That(limit.LimitName, Is.EqualTo("MaximumInputBytes"));
        Assert.That(limit.RequestedValue, Is.EqualTo(expected.Length));
        Assert.That(limit.MaximumValue, Is.EqualTo(expected.Length - 1));

        options.MaximumInputBytes = expected.Length;
        foreach (QrSegment[] part in parts) part[0].StructuredAppend!.Parity ^= 1;
        Assert.That(() => new QrEncoder().EncodeManualStructuredAppend(parts, options),
            Throws.TypeOf<QrStructuredAppendException>());
    }

    [Test]
    public void RejectsIllegalFamiliesForBothStructuredAppendEntryPoints(
        [Values(QrSymbolKind.Micro, QrSymbolKind.rMQR)] QrSymbolKind kind,
        [Values(false, true)] bool manual,
        [Values(false, true)] bool forced)
    {
        QrEncodeOptions options = Options();
        options.SymbolKind = kind;
        options.ForceSymbolKind = forced;
        options.AllowModel2 = forced;
        Assert.That(() =>
        {
            IQrEncoder encoder = new QrEncoder();
            if (manual) encoder.EncodeManualStructuredAppend(ManualParts([[1], [2]]), options);
            else encoder.EncodeStructuredAppend([1, 2], options);
        }, Throws.TypeOf<QrInputException>());
    }

    [TestCase(1)]
    [TestCase(17)]
    public void RejectsIllegalPartLimitsAndManualPartCounts(int count)
    {
        QrEncodeOptions options = Options();
        options.MaximumStructuredAppendSymbols = count;
        IQrEncoder encoder = new QrEncoder();
        Assert.That(() => encoder.EncodeStructuredAppend([1, 2], options), Throws.TypeOf<QrInputException>());
        Assert.That(() => encoder.EncodeManualStructuredAppend(ManualParts([[1], [2]]), options),
            Throws.TypeOf<QrInputException>());
        QrSegment[][] parts = ManualParts(Enumerable.Range(0, count).Select(_ => new byte[] { 1 }).ToArray());
        Assert.That(() => encoder.EncodeManualStructuredAppend(parts, Options()), Throws.TypeOf<QrInputException>());
    }

    private static void AssertSequence(QrSymbolSequence sequence, byte[] expected, int count)
    {
        int parity = expected.Aggregate(0, (value, b) => value ^ b);
        Assert.Multiple(() =>
        {
            Assert.That(sequence.Symbols, Has.Length.EqualTo(count));
            Assert.That(sequence.RawBytes, Is.EqualTo(expected));
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(sequence.StructuredAppend!.Total, Is.EqualTo(count));
            Assert.That(sequence.StructuredAppend.Parity, Is.EqualTo(parity));
        });
        List<byte> actual = [];
        for (int index = 0; index < count; index++)
        {
            QrDecodeResult decoded = Decode(sequence.Symbols[index]);
            Assert.Multiple(() =>
            {
                Assert.That(decoded.StructuredAppend, Is.Not.Null);
                Assert.That(decoded.StructuredAppend!.Index, Is.EqualTo(index));
                Assert.That(decoded.StructuredAppend.Total, Is.EqualTo(count));
                Assert.That(decoded.StructuredAppend.Parity, Is.EqualTo(parity));
                Assert.That(decoded.CorrectedErrorCount, Is.Zero);
                Assert.That(decoded.CorrectedErasureCount, Is.Zero);
                Assert.That(decoded.Symbol!.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            });
            actual.AddRange(decoded.RawBytes);
        }
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static QrDecodeResult Decode(QrSymbol symbol)
    {
        QrMatrix source = symbol.Matrix!;
        QrMatrix samples = new()
        {
            Width = source.Width, Height = source.Height, Values = (QrModuleValue[])source.Values.Clone(),
            QuietZoneLeft = source.QuietZoneLeft, QuietZoneRight = source.QuietZoneRight,
            QuietZoneTop = source.QuietZoneTop, QuietZoneBottom = source.QuietZoneBottom
        };
        return new QrDecoder().DecodeMatrix(samples, new QrDecodeOptions
        {
            MaximumMatrixDimension = 185, EnableRecovery = false, TryMirrored = false,
            TryInverted = false, EnableHanziCompatibility = true
        });
    }

    private static void AssertCapacityFailure(byte[] payload, QrEncodeOptions options, int availableBytes)
    {
        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            new QrEncoder().EncodeStructuredAppend(payload, options))!;
        Assert.That(exception.RequiredBitCount, Is.EqualTo(payload.Length * 8));
        Assert.That(exception.AvailableBitCount, Is.EqualTo(availableBytes * 8));
    }

    private static QrSegment[][] ManualParts(byte[][] payloads)
    {
        int parity = payloads.SelectMany(p => p).Aggregate(0, (value, b) => value ^ b);
        return payloads.Select((payload, index) => new[]
        {
            new QrSegment
            {
                Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = new QrStructuredAppendInfo
                { Index = index, Total = payloads.Length, Parity = parity, ApplicationIdentifier = "manual" }
            },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = payload, CharacterCount = payload.Length }
        }).ToArray();
    }

    private static QrEncodeOptions Options() => new()
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2,
        MinimumVersion = 1, MaximumVersion = 1,
        ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
        BoostErrorCorrectionLevel = false, MaskPattern = QrMaskPattern.Zero
    };

    private static byte[] Message(int count) => Enumerable.Range(0, count).Select(i => (byte)((i * 73 + 0xA5) & 255)).ToArray();
    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
