using System.Text;
using qrcode;
using static qrcode.Tests.QrRandomizedRoundTripTests;

namespace qrcode.Tests;

[TestFixture]
public class QrRandomizedControlTests
{
    static IEnumerable<TestCaseData> Controls()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
            for (int sample = 0; sample < 12; sample++)
            {
                int seed = 1530000 + (int)kind * 100 + sample;
                yield return new TestCaseData(kind, seed).SetName($"RandomControls_{kind}_seed{seed}");
            }
    }

    [TestCaseSource(nameof(Controls))]
    public void RandomFnc1AndEciTransitionsPreserveTextBytesAndControlOrder(QrSymbolKind kind, int seed)
    {
        Random random = new Random(seed);
        bool first = random.Next(2) == 1;
        int indicator = random.Next(2) == 0 ? random.Next(100) : random.Next(165, 191);
        QrSegment fnc = first ? new QrSegment { Mode = QrEncodingMode.Fnc1First }
            : new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = indicator, Data = [(byte)indicator] };
        string gs1 = Segment(random, QrEncodingMode.Alphanumeric, 4).Text + "%X\u001dZ" + Segment(random, QrEncodingMode.Alphanumeric, 3).Text;
        string[] tokens = { "😀", "Ω", "e\u0301", "漢", "\0" };
        string unicode = string.Concat(Enumerable.Range(0, random.Next(1, 5)).Select(_ => tokens[random.Next(tokens.Length)]));
        byte[] utf8 = Encoding.UTF8.GetBytes(unicode);
        byte[] latin1 = Enumerable.Range(0, random.Next(1, 6)).Select(_ => (byte)random.Next(160, 256)).ToArray();
        QrSegment[] segments =
        [
            fnc, new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = gs1, CharacterCount = gs1.Length },
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 26, Data = [26] },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = utf8, CharacterCount = utf8.Length, EciAssignment = 26 },
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 3, Data = [3] },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = latin1, CharacterCount = latin1.Length, EciAssignment = 3 }
        ];
        QrEncodeOptions options = Options(random, kind, kind == QrSymbolKind.Model2 ? 5 : 31, 0);
        string before = Snapshot(segments);
        TestContext.Out.WriteLine($"seed={seed}; options={Snapshot(options)}; segments={before}");
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments, options);
        QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, Decoding());
        byte[] expected = Encoding.ASCII.GetBytes(gs1.Replace("%", "%%").Replace("\u001d", "%")).Concat(utf8).Concat(latin1).ToArray();
        Assert.That(result.RawBytes, Is.EqualTo(expected));
        Assert.That(result.Text, Is.EqualTo(gs1 + unicode + Encoding.Latin1.GetString(latin1)));
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(segments.Select(s => s.Mode)));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 26, 3 }));
        Assert.That(result.UsesFnc1First, Is.EqualTo(first));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(first ? -1 : indicator));
        Assert.That(Snapshot(segments), Is.EqualTo(before));
    }

    static IEnumerable<TestCaseData> Sequences()
    {
        for (int count = 2; count <= 16; count++)
        {
            int seed = 1531000 + count;
            yield return new TestCaseData(count, seed).SetName($"RandomStructuredAppend_{count}_seed{seed}");
        }
    }

    [TestCaseSource(nameof(Sequences))]
    public void RandomSequencePartitionsAndArrivalOrdersReassembleLosslessly(int count, int seed)
    {
        Random random = new Random(seed);
        byte[][] payloads = Enumerable.Range(0, count).Select(_ => new byte[random.Next(1, 13)]).ToArray();
        foreach (byte[] payload in payloads) random.NextBytes(payload);
        byte[] expected = payloads.SelectMany(p => p).ToArray();
        int parity = expected.Aggregate(0, (value, item) => value ^ item);
        QrSegment[][] parts = payloads.Select((payload, index) => new[]
        {
            new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = count, Parity = parity } },
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 899, Data = [0x83, 0x83] },
            new QrSegment { Mode = QrEncodingMode.Byte, Data = payload, CharacterCount = payload.Length, EciAssignment = 899 }
        }).ToArray();
        string before = Snapshot(parts);
        QrEncodeOptions options = Options(random, QrSymbolKind.Model2, 2, 0);
        TestContext.Out.WriteLine($"seed={seed}; lengths={string.Join(',', payloads.Select(p => p.Length))}; payload={Convert.ToHexString(expected)}");
        IQrEncoder encoder = new QrEncoder();
        QrSymbolSequence sequence = encoder.EncodeManualStructuredAppend(parts, options);
        Assert.That(sequence.Symbols, Has.Length.EqualTo(count));
        QrDecodeResult[] decoded = sequence.Symbols.Select(s => new QrDecoder().DecodeMatrix(s.Matrix!, Decoding())).ToArray();
        QrDecodeResult[] arrivals = decoded.Concat(new[] { decoded[random.Next(count)] }).ToArray();
        random.Shuffle(arrivals);
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult result = assembler.AssembleSequence(arrivals, 32, 1000000);
        QrStructuredAppendAttemptResult attempt = assembler.TryAssembleSequence(arrivals, 32, 1000000);
        Assert.That(result.RawBytes, Is.EqualTo(expected));
        Assert.That(result.Text, Is.Null, "The explicitly unsupported ECI must not produce invented text.");
        Assert.That(result.StructuredAppend!.IsComplete, Is.True);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.Result!.RawBytes, Is.EqualTo(expected));
        Assert.That(attempt.DuplicateCount, Is.EqualTo(1));
        Assert.That(Snapshot(parts), Is.EqualTo(before));
        // Also exercise automatic partitioning with the same random bytes.
        options = Options(random, QrSymbolKind.Model2, 1, 0);
        QrSymbolSequence automatic = encoder.EncodeStructuredAppend(expected, options);
        QrDecodeResult[] automaticParts = automatic.Symbols.Select(s => new QrDecoder().DecodeMatrix(s.Matrix!, Decoding())).ToArray();
        random.Shuffle(automaticParts);
        Assert.That(assembler.AssembleSequence(automaticParts, 32, 1000000).RawBytes, Is.EqualTo(expected));
    }

    [TestCase("1漢1")]
    [TestCase("11漢")]
    [TestCase("é漢漢")]
    public void UnoptimizedMicroMixedTextReportsExpectedRepresentationFailure(string text)
    {
        // Minimized regressions from seeds 1525101, 1525105 and 1525108.
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Micro, OptimizeSegments = false,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low
        };
        QrEncodeAttemptResult attempt = encoder.TryEncodeText(text, options);
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
        Assert.That(attempt.Symbol, Is.Null);
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeText(text, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(attempt.FailureReason));
        Assert.That(error.FeatureName, Is.EqualTo("MicroTextEncoding"));
        options.OptimizeSegments = true;
        Assert.That(new QrDecoder().DecodeMatrix(encoder.EncodeText(text, options).Matrix!, Decoding()).Text, Is.EqualTo(text));
    }
}
