using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrStructuredAppendConformanceTests
{
    static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    [Test]
    public void EverySequenceSizePreservesOrderedPayloadAndDiagnostics([Range(2, 16)] int count)
    {
        byte[][] payloads = Enumerable.Range(0, count).Select(i => i == 0 ? Array.Empty<byte>() : Encoding.ASCII.GetBytes($"P{i};")).ToArray();
        QrDecodeResult[] parts = Decode(payloads);
        for (int i = 0; i < count; i++)
        {
            parts[i].CorrectedErrorCount = i;
            parts[i].CorrectedErasureCount = count - i;
            parts[i].Warnings = [$"observation {i}", "shared warning"];
            parts[i].IsConformant = i != count - 1;
        }
        string before = Snapshot(parts);
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        for (int start = 0; start < count; start++)
            foreach (bool reverse in new[] { false, true })
            {
                QrDecodeResult[] input = Enumerable.Range(0, count).Select(i => parts[(start + i) % count]).ToArray();
                if (reverse) Array.Reverse(input);
                QrStructuredAppendAttemptResult attempt = assembler.TryAssembleSequence(input, count, 1_000_000);
                AssertComplete(attempt, count, Parity(payloads), 0);
                QrDecodeResult result = attempt.Result!;
                Assert.That(result.RawBytes, Is.EqualTo(payloads.SelectMany(p => p)));
                Assert.That(result.Text, Is.EqualTo(Encoding.ASCII.GetString(payloads.SelectMany(p => p).ToArray())));
                Assert.That(Snapshot(result.Segments), Is.EqualTo(Snapshot(parts.SelectMany(p => p.Segments).ToArray())));
                Assert.That(result.Warnings, Is.EqualTo(Enumerable.Range(0, count).SelectMany(i => new[] { $"Part {i}: observation {i}", $"Part {i}: shared warning" })));
                Assert.That(result.CorrectedErrorCount, Is.EqualTo(count * (count - 1) / 2));
                Assert.That(result.CorrectedErasureCount, Is.EqualTo(count * (count + 1) / 2));
                Assert.That(result.IsConformant, Is.False);
                Assert.That(result.StructuredAppend!.ApplicationIdentifier, Is.EqualTo("message"));
                Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q1"));
                Assert.That(result.EciAssignments, Is.Empty);
                Assert.That(result.ErrorCorrectionBlocks, Is.Empty);
                Assert.That(result.Symbol, Is.Null);
                Assert.That(result.Detection, Is.Null);
                Assert.That(Snapshot(assembler.AssembleSequence(input, count, 1_000_000)), Is.EqualTo(Snapshot(result)));
            }
        Assert.That(Snapshot(parts), Is.EqualTo(before));
    }

    [Test]
    public void MissingPositionsRemainMissingDespiteDuplicatesAndCompletenessFlags([Values(2, 3, 4, 8, 16)] int count)
    {
        QrDecodeResult[] parts = Decode(Enumerable.Range(0, count).Select(i => new[] { (byte)(65 + i) }).ToArray());
        foreach (QrDecodeResult part in parts) part.StructuredAppend!.IsComplete = true;
        int[][] missingSets = Enumerable.Range(0, count).Select(i => new[] { i })
            .Append(Enumerable.Range(0, count).Where(i => i % 2 == 0).ToArray()).ToArray();
        foreach (int[] missing in missingSets)
        {
            QrDecodeResult[] present = parts.Where((_, i) => !missing.Contains(i)).ToArray();
            QrDecodeResult[] input = present.Reverse().Concat(present).ToArray();
            string before = Snapshot(input);
            QrStructuredAppendAttemptResult result = AssertFailure(input, QrFailureReason.IncompleteSequence);
            Assert.That(result.ExpectedPartCount, Is.EqualTo(count));
            Assert.That(result.ActualPartCount, Is.EqualTo(present.Length));
            Assert.That(result.MissingIndices, Is.EqualTo(missing));
            Assert.That(result.ConflictingIndices, Is.Empty);
            Assert.That(result.DuplicateCount, Is.EqualTo(present.Length));
            Assert.That(Snapshot(input), Is.EqualTo(before));
        }
    }

    [Test]
    public void EquivalentDuplicateSelectionUsesTotalRepairsThenArrivalOrder([Values(false, true)] bool tie)
    {
        QrDecodeResult[] parts = Decode([[65], [66]]);
        parts[1].IsConformant = true;
        QrDecodeResult[] observations = [parts[0], Copy(parts[0]), Copy(parts[0])];
        observations[0].CorrectedErrorCount = 4;
        observations[1].CorrectedErrorCount = 1;
        observations[1].CorrectedErasureCount = 2;
        observations[2].CorrectedErasureCount = tie ? 3 : 2;
        for (int i = 0; i < observations.Length; i++)
        {
            observations[i].Warnings = [$"copy {i}"];
            observations[i].IsConformant = i != 1;
        }
        foreach (int[] order in Permutations([0, 1, 2, 3]))
        {
            QrDecodeResult[] input = order.Select(i => i == 3 ? parts[1] : observations[i]).ToArray();
            QrDecodeResult selected = input.Where(p => p.StructuredAppend!.Index == 0)
                .OrderBy(p => p.CorrectedErrorCount + p.CorrectedErasureCount).First();
            QrStructuredAppendAttemptResult result = new QrStructuredAppendAssembler().TryAssembleSequence(input, 4, 1_000_000);
            AssertComplete(result, 2, 3, 2);
            Assert.That(result.Result!.Text, Is.EqualTo("AB"));
            Assert.That(result.Result.CorrectedErrorCount, Is.EqualTo(selected.CorrectedErrorCount));
            Assert.That(result.Result.CorrectedErasureCount, Is.EqualTo(selected.CorrectedErasureCount));
            Assert.That(result.Result.IsConformant, Is.EqualTo(selected.IsConformant));
            Assert.That(result.Result.Warnings, Is.EqualTo(new[] { $"Part 0: {selected.Warnings[0]}", "Ignored 2 equivalent duplicate observations." }));
        }
    }

    [Test]
    public void ConflictingDuplicateSemanticsReportAllConflictsAndMissingPositions(
        [Values("bytes", "text", "hanzi", "eci-list", "eci-order", "segment-count", "mode", "count", "segment-text", "segment-eci", "segment-fnc1", "segment-hanzi")] string field)
    {
        QrDecodeResult[] parts = Decode([[65], [66], [67], [68]]);
        QrDecodeResult duplicate = Copy(parts[0]);
        QrSegment payload = duplicate.Segments[^1];
        switch (field)
        {
            case "bytes": duplicate.RawBytes[0] = 90; payload.Data[0] = 90; break;
            case "text": duplicate.Text = "Z"; break;
            case "hanzi": duplicate.UsesHanziCompatibility = true; break;
            case "eci-list": duplicate.EciAssignments = [26]; break;
            case "eci-order": parts[0].EciAssignments = [3, 26]; duplicate.EciAssignments = [26, 3]; break;
            case "segment-count": duplicate.Segments = []; break;
            case "mode": payload.Mode = QrEncodingMode.Alphanumeric; break;
            case "count": payload.CharacterCount++; break;
            case "segment-text": payload.Text = "Z"; break;
            case "segment-eci": payload.EciAssignment = 26; break;
            case "segment-fnc1": payload.Fnc1ApplicationIndicator = 42; break;
            case "segment-hanzi": payload.IsHanziCompatibilityMode = true; break;
        }
        QrDecodeResult another = Copy(parts[3]);
        another.Text = "conflict";
        foreach (bool reverse in new[] { false, true })
        {
            QrDecodeResult[] input = [parts[3], duplicate, parts[0], another, duplicate];
            if (reverse) Array.Reverse(input);
            string before = Snapshot(input);
            QrStructuredAppendAttemptResult result = AssertFailure(input, QrFailureReason.ConflictingSequence);
            Assert.That(result.ExpectedPartCount, Is.EqualTo(4));
            Assert.That(result.ActualPartCount, Is.EqualTo(2));
            Assert.That(result.ConflictingIndices, Is.EqualTo(new[] { 0, 3 }));
            Assert.That(result.MissingIndices, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.DuplicateCount, Is.EqualTo(3));
            Assert.That(Snapshot(input), Is.EqualTo(before));
        }
    }

    [Test]
    public void MixedIdentitiesCannotBeSilentlySelectedFromACompleteMessage(
        [Values("parity", "total", "application", "case", "null-empty", "unicode", "ordinary")] string difference)
    {
        QrDecodeResult[] parts = Decode([[65], [66]]);
        QrDecodeResult extra = Copy(parts[0]);
        switch (difference)
        {
            case "parity": extra.StructuredAppend!.Parity ^= 1; extra.Segments[0].StructuredAppend!.Parity ^= 1; break;
            case "total": extra.StructuredAppend!.Total = 3; extra.Segments[0].StructuredAppend!.Total = 3; break;
            case "application": extra.StructuredAppend!.ApplicationIdentifier = "another"; break;
            case "case": extra.StructuredAppend!.ApplicationIdentifier = "MESSAGE"; break;
            case "null-empty": foreach (var part in parts) part.StructuredAppend!.ApplicationIdentifier = null; extra.StructuredAppend!.ApplicationIdentifier = ""; break;
            case "unicode": foreach (var part in parts) part.StructuredAppend!.ApplicationIdentifier = "é"; extra.StructuredAppend!.ApplicationIdentifier = "e\u0301"; break;
            case "ordinary": extra.StructuredAppend = null; extra.Segments = [extra.Segments[^1]]; break;
        }
        foreach (int[] order in Permutations([0, 1, 2]))
        {
            QrDecodeResult[] all = [parts[0], parts[1], extra];
            QrStructuredAppendAttemptResult result = AssertFailure(order.Select(i => all[i]).ToArray(), QrFailureReason.ConflictingSequence);
            Assert.That(result.ExpectedPartCount, Is.EqualTo(difference == "ordinary" ? 2 : 0));
            Assert.That(result.ActualPartCount, Is.EqualTo(difference == "ordinary" ? 2 : 0));
            Assert.That(result.Parity, Is.EqualTo(difference == "ordinary" ? 3 : -1));
        }
    }

    [Test]
    public void WrongParityIsRejectedEvenWhenEveryRetainedHeaderAgrees([Values(0, 1, 128, 255)] int parity)
    {
        QrDecodeResult[] parts = Decode([[0], [(byte)parity]]);
        AssertComplete(new QrStructuredAppendAssembler().TryAssembleSequence(parts, 2, 1_000_000), 2, parity, 0);
        foreach (var part in parts)
        {
            part.StructuredAppend!.Parity = parity ^ 1;
            part.Segments[0].StructuredAppend!.Parity = parity ^ 1;
        }
        QrStructuredAppendAttemptResult result = AssertFailure(parts, QrFailureReason.ConflictingSequence);
        Assert.That(result.ActualPartCount, Is.EqualTo(2));
        Assert.That(result.Parity, Is.EqualTo(parity ^ 1));
        Assert.That(result.MissingIndices, Is.Empty);
        Assert.That(result.ConflictingIndices, Is.Empty);
    }

    static IEnumerable<TestCaseData> SplitEncodings()
    {
        yield return new TestCaseData(26, new byte[] { 0xF0, 0x9F, 0x98, 0x80 }, "😀");
        yield return new TestCaseData(25, new byte[] { 0xD8, 0x3D, 0xDE, 0x00 }, "😀");
        yield return new TestCaseData(33, new byte[] { 0x3D, 0xD8, 0x00, 0xDE }, "😀");
        yield return new TestCaseData(34, new byte[] { 0x00, 0x01, 0xF6, 0x00 }, "😀");
        yield return new TestCaseData(35, new byte[] { 0x00, 0xF6, 0x01, 0x00 }, "😀");
        yield return new TestCaseData(20, new byte[] { 0x8A, 0xBF }, "漢");
        yield return new TestCaseData(29, new byte[] { 0xBA, 0xBA }, "汉");
    }

    [TestCaseSource(nameof(SplitEncodings))]
    public void CompatibleEciRunsRecoverCharactersAcrossEveryByteBoundary(int assignment, byte[] bytes, string text)
    {
        for (int split = 1; split < bytes.Length; split++)
        {
            QrDecodeResult[] parts = Decode([bytes[..split], [], bytes[split..]], [assignment, assignment, assignment]);
            string before = Snapshot(parts);
            Assert.That(parts[0].Text == null || parts[2].Text == null, Is.True);
            QrStructuredAppendAttemptResult attempt = new QrStructuredAppendAssembler().TryAssembleSequence([parts[2], parts[0], parts[1]], 3, 1_000_000);
            AssertComplete(attempt, 3, Parity([bytes]), 0);
            Assert.That(attempt.Result!.Text, Is.EqualTo(text));
            Assert.That(attempt.Result.RawBytes, Is.EqualTo(bytes));
            Assert.That(attempt.Result.EciAssignments, Is.EqualTo(new[] { assignment, assignment, assignment }));
            Assert.That(attempt.Result.Warnings, Has.Member("Recovered text across compatible byte-segment boundaries."));
            Assert.That(Snapshot(attempt.Result.Segments), Is.EqualTo(Snapshot(parts.SelectMany(p => p.Segments).ToArray())));
            Assert.That(Snapshot(parts), Is.EqualTo(before));
        }
    }

    [Test]
    public void IncompatibleEciRunsRetainBytesWithoutInventingText([Values(-1, 3, 27, 899, 900)] int secondAssignment)
    {
        QrDecodeResult[] parts = Decode([[0xC3], [0xA9]], [26, secondAssignment]);
        QrStructuredAppendAttemptResult result = new QrStructuredAppendAssembler().TryAssembleSequence(parts, 2, 1_000_000);
        AssertComplete(result, 2, 0xC3 ^ 0xA9, 0);
        Assert.That(result.Result!.Text, Is.Null);
        Assert.That(result.Result.RawBytes, Is.EqualTo(new byte[] { 0xC3, 0xA9 }));
        Assert.That(result.Result.Warnings, Has.Member("Complete text is unavailable; all sequence payload bytes were retained."));
        Assert.That(result.Result.Segments[^1].EciAssignment, Is.EqualTo(secondAssignment < 0 ? 3 : secondAssignment));
    }

    [Test]
    public void Gs1EscapesAreInterpretedWithinEachPartAndParityUsesOriginalPayload(
        [Values(-1, 0, 99, 165)] int fnc1, [Values(false, true)] bool alphanumeric, [Values(-1, 26)] int assignment)
    {
        string[] text = ["A%", "%B", "\u001d", "\u001d", "%", "1"];
        byte[][] payloads = text.Select(Encoding.ASCII.GetBytes).ToArray();
        QrDecodeResult[] parts = Decode(payloads, Enumerable.Repeat(assignment, text.Length).ToArray(), fnc1, alphanumeric);
        string before = Snapshot(parts);
        QrStructuredAppendAttemptResult attempt = new QrStructuredAppendAssembler().TryAssembleSequence(parts.Reverse().ToArray(), 6, 1_000_000);
        AssertComplete(attempt, 6, Parity(payloads), 0);
        QrDecodeResult result = attempt.Result!;
        Assert.That(result.Text, Is.EqualTo(string.Concat(text)));
        byte[] expectedRaw = alphanumeric
            ? Encoding.ASCII.GetBytes(string.Concat(text.Select(t => t.Replace("%", "%%").Replace("\u001d", "%"))))
            : payloads.SelectMany(p => p).ToArray();
        Assert.That(result.RawBytes, Is.EqualTo(expectedRaw));
        Assert.That(result.UsesFnc1First, Is.EqualTo(fnc1 == -1));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(fnc1));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(fnc1 == -1 ? assignment < 0 ? "]Q3" : "]Q4" : assignment < 0 ? "]Q5" : "]Q6"));
        Assert.That(Snapshot(result.Segments), Is.EqualTo(Snapshot(parts.SelectMany(p => p.Segments).ToArray())));
        Assert.That(Snapshot(parts), Is.EqualTo(before));
    }

    [Test]
    public void ReturnedBuffersAndHeadersAreIndependentOfAllObservations()
    {
        QrDecodeResult[] parts = Decode([[65], [66]], [26, 26]);
        parts[0].Warnings = ["original"];
        QrStructuredAppendAssembler assembler = new();
        QrDecodeResult first = assembler.AssembleSequence(parts, 2, 1_000_000);
        string before = Snapshot(parts);
        string expected = Snapshot(first);
        first.RawBytes[0] = 0;
        first.Segments[^1].Data[0] = 0;
        first.Segments[0].StructuredAppend!.Index = 15;
        first.StructuredAppend!.ApplicationIdentifier = "changed";
        first.EciAssignments[0] = 900;
        first.Warnings[0] = "changed";
        Assert.That(Snapshot(parts), Is.EqualTo(before));
        QrDecodeResult second = assembler.AssembleSequence(parts, 2, 1_000_000);
        Assert.That(Snapshot(second), Is.EqualTo(expected));
        parts[0].RawBytes[0] = 0;
        parts[0].Segments[^1].Data[0] = 0;
        parts[0].Segments[0].StructuredAppend!.Index = 15;
        parts[0].EciAssignments[0] = 900;
        parts[0].Warnings[0] = "changed";
        Assert.That(Snapshot(second), Is.EqualTo(expected));
    }

    [Test]
    public void MixedTextModesRetainSegmentsAndAggregateHanziCompatibility()
    {
        QrEncodingMode[] modes = [QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Kanji, QrEncodingMode.Hanzi];
        string[] text = ["123", "ABC", "漢", "汉"];
        byte[][] bytes = ["123"u8.ToArray(), "ABC"u8.ToArray(), [0x8A, 0xBF], [0xBA, 0xBA]];
        QrSegment[][] segments = Enumerable.Range(0, 4).Select(i => new[]
        {
            new QrSegment { Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = new QrStructuredAppendInfo { Index = i, Total = 4, Parity = Parity(bytes) } },
            new QrSegment { Mode = modes[i], Text = text[i], CharacterCount = text[i].Length,
                IsHanziCompatibilityMode = modes[i] == QrEncodingMode.Hanzi }
        }).ToArray();
        QrSymbolSequence sequence = new QrEncoder().EncodeManualStructuredAppend(segments, new QrEncodeOptions
            { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0, EnableHanziCompatibility = true });
        QrDecodeResult[] parts = sequence.Symbols.Select(s => new QrDecoder().DecodeMatrix(s.Matrix!,
            new QrDecodeOptions { EnableHanziCompatibility = true })).ToArray();
        QrStructuredAppendAttemptResult attempt = new QrStructuredAppendAssembler().TryAssembleSequence(parts.Reverse().ToArray(), 4, 1_000_000);
        AssertComplete(attempt, 4, Parity(bytes), 0);
        Assert.That(attempt.Result!.Text, Is.EqualTo("123ABC漢汉"));
        Assert.That(attempt.Result.RawBytes, Is.EqualTo(bytes.SelectMany(b => b)));
        Assert.That(attempt.Result.UsesHanziCompatibility, Is.True);
        Assert.That(attempt.Result.IsConformant, Is.EqualTo(parts.All(p => p.IsConformant)));
        Assert.That(attempt.Result.Warnings, Is.EqualTo(parts.SelectMany((p, i) => p.Warnings.Select(w => $"Part {i}: {w}"))));
        Assert.That(Snapshot(attempt.Result.Segments), Is.EqualTo(Snapshot(parts.SelectMany(p => p.Segments).ToArray())));
    }

    [Test]
    public void EciChangesAndUnspecifiedAssignmentsStartIndependentTextRuns()
    {
        QrDecodeResult[] parts = Decode([[0xC3, 0xA9], [0xE9], [0x41]], [26, -1, 27]);
        QrStructuredAppendAttemptResult attempt = new QrStructuredAppendAssembler().TryAssembleSequence(parts.Reverse().ToArray(), 3, 1_000_000);
        AssertComplete(attempt, 3, 0xC3 ^ 0xA9 ^ 0xE9 ^ 0x41, 0);
        Assert.That(attempt.Result!.Text, Is.EqualTo("ééA"));
        Assert.That(attempt.Result.EciAssignments, Is.EqualTo(new[] { 26, 27 }));
        Assert.That(attempt.Result.Segments.Where(s => s.Mode == QrEncodingMode.Byte).Select(s => s.EciAssignment), Is.EqualTo(new[] { 26, 3, 27 }));
        Assert.That(attempt.Result.SymbologyIdentifier, Is.EqualTo("]Q2"));
    }

    static QrDecodeResult[] Decode(byte[][] payloads, int[]? assignments = null, int fnc1 = -2, bool alphanumeric = false)
    {
        int parity = Parity(payloads);
        QrDecodeResult[] result = new QrDecodeResult[payloads.Length];
        for (int i = 0; i < result.Length; i++)
        {
            int assignment = assignments == null ? -1 : assignments[i];
            List<QrSegment> segments = [new QrSegment { Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = new QrStructuredAppendInfo { Index = i, Total = result.Length, Parity = parity } }];
            if (fnc1 == -1) segments.Add(new QrSegment { Mode = QrEncodingMode.Fnc1First });
            if (fnc1 >= 0) segments.Add(new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = fnc1, Data = [(byte)fnc1] });
            if (assignment >= 0) segments.Add(EciEncoder.CreateControlSegment(assignment));
            segments.Add(new QrSegment { Mode = alphanumeric ? QrEncodingMode.Alphanumeric : QrEncodingMode.Byte,
                Data = payloads[i], CharacterCount = payloads[i].Length, EciAssignment = alphanumeric ? -1 : assignment,
                Text = alphanumeric ? Encoding.ASCII.GetString(payloads[i]) : null });
            QrSymbol symbol = new QrEncoder().EncodeSegments(segments.ToArray(), new QrEncodeOptions
            {
                ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0,
                MinimumVersion = 1 + i % 2, MaximumVersion = 4,
                ErrorCorrectionLevel = new[] { QrErrorCorrectionLevel.Low, QrErrorCorrectionLevel.Medium, QrErrorCorrectionLevel.Quartile, QrErrorCorrectionLevel.High }[i % 4]
            });
            result[i] = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
            result[i].StructuredAppend!.ApplicationIdentifier = "message";
        }
        return result;
    }

    static int Parity(byte[][] payloads) => payloads.SelectMany(p => p).Aggregate(0, (p, b) => p ^ b);
    static string Snapshot(object value) => JsonSerializer.Serialize(value, Json);
    static QrDecodeResult Copy(QrDecodeResult value) => JsonSerializer.Deserialize<QrDecodeResult>(Snapshot(value), Json)!;
    static IEnumerable<int[]> Permutations(int[] values) => values.Length == 0 ? new[] { Array.Empty<int>() }
        : values.SelectMany(v => Permutations(values.Where(x => x != v).ToArray()).Select(tail => new[] { v }.Concat(tail).ToArray()));

    static void AssertComplete(QrStructuredAppendAttemptResult attempt, int count, int parity, int duplicates)
    {
        Assert.That(attempt.Success, Is.True, attempt.FailureReason.ToString());
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(attempt.ExpectedPartCount, Is.EqualTo(count));
        Assert.That(attempt.ActualPartCount, Is.EqualTo(count));
        Assert.That(attempt.Parity, Is.EqualTo(parity));
        Assert.That(attempt.DuplicateCount, Is.EqualTo(duplicates));
        Assert.That(attempt.MissingIndices, Is.Empty);
        Assert.That(attempt.ConflictingIndices, Is.Empty);
        Assert.That(attempt.Result!.StructuredAppend!.Total, Is.EqualTo(count));
        Assert.That(attempt.Result.StructuredAppend.Parity, Is.EqualTo(parity));
        Assert.That(attempt.Result.StructuredAppend.IsComplete, Is.True);
    }

    static QrStructuredAppendAttemptResult AssertFailure(QrDecodeResult[] input, QrFailureReason reason)
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrStructuredAppendAttemptResult result = assembler.TryAssembleSequence(input, input.Length, 1_000_000);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Result, Is.Null);
        Assert.That(result.FailureReason, Is.EqualTo(reason));
        QrStructuredAppendException error = Assert.Throws<QrStructuredAppendException>(() => assembler.AssembleSequence(input, input.Length, 1_000_000))!;
        Assert.That(error.FailureReason, Is.EqualTo(reason));
        Assert.That(error.ExpectedPartCount, Is.EqualTo(result.ExpectedPartCount));
        Assert.That(error.ActualPartCount, Is.EqualTo(result.ActualPartCount));
        Assert.That(error.Parity, Is.EqualTo(result.Parity));
        return result;
    }
}
