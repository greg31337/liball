using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class StructuredAppendAssemblyTests
{
    [Test]
    public void AssemblesAllSixteenIndicesIncludingAnEmptyPart()
    {
        byte[][] payloads = Enumerable.Range(0, 16).Select(i => i == 0 ? Array.Empty<byte>() : new byte[] { (byte)(64 + i) }).ToArray();
        QrDecodeResult[] parts = DecodeParts(payloads, Enumerable.Repeat(-1, 16).ToArray());
        StructuredAppendAssemblyResult result = Assemble(parts.Reverse().ToArray());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ActualPartCount, Is.EqualTo(16));
        Assert.That(result.Result!.RawBytes, Is.EqualTo(payloads.SelectMany(p => p)));
        Assert.That(result.Result.Text, Is.EqualTo("ABCDEFGHIJKLMNO"));
    }

    [Test]
    public void OrdersRealDecodedPartsCombinesBytesAndAggregatesUniqueDiagnostics()
    {
        QrDecodeResult[] parts = DecodeParts(["AB"u8.ToArray(), "CD"u8.ToArray(), "EF"u8.ToArray()], [-1, -1, -1]);
        parts[0].CorrectedErrorCount = 2;
        parts[0].Warnings = ["corrected"];
        parts[1].CorrectedErasureCount = 3;
        parts[1].Warnings = ["sampled"];
        foreach (QrDecodeResult part in parts) part.IsConformant = true;
        parts[2].IsConformant = false;
        StructuredAppendAssemblyResult result = Assemble([parts[2], parts[0], parts[1]]);
        Assert.That(result.Success, Is.True);
        Assert.That(result.OrderedParts, Is.EqualTo(parts));
        Assert.That(result.ActualPartCount, Is.EqualTo(3));
        Assert.That(result.ExpectedPartCount, Is.EqualTo(3));
        Assert.That(result.MissingIndices, Is.Empty);
        Assert.That(result.Result!.RawBytes, Is.EqualTo("ABCDEF"u8.ToArray()));
        Assert.That(result.Result.Text, Is.EqualTo("ABCDEF"));
        Assert.That(result.Result.CorrectedErrorCount, Is.EqualTo(2));
        Assert.That(result.Result.CorrectedErasureCount, Is.EqualTo(3));
        Assert.That(result.Result.Warnings, Is.EqualTo(new[] { "Part 0: corrected", "Part 1: sampled" }));
        Assert.That(result.Result.IsConformant, Is.False);
        Assert.That(result.Result.StructuredAppend!.IsComplete, Is.True);
        Assert.That(result.Result.StructuredAppend.Total, Is.EqualTo(3));
        Assert.That(result.Result.StructuredAppend.ApplicationIdentifier, Is.EqualTo("message"));
        Assert.That(result.Result.Symbol, Is.Null);
        Assert.That(result.Result.Detection, Is.Null);
        result.Result.RawBytes[0] = 0;
        result.Result.Segments[1].Data[0] = 0;
        result.Result.Segments[0].StructuredAppend!.Index = 9;
        Assert.That(parts[0].RawBytes, Is.EqualTo("AB"u8.ToArray()));
        Assert.That(parts[0].Segments[1].Data, Is.EqualTo("AB"u8.ToArray()));
        Assert.That(parts[0].StructuredAppend!.Index, Is.Zero);
    }

    [Test]
    public void EquivalentDuplicatesUseOneObservationWithTheFewestCorrections()
    {
        QrDecodeResult[] parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        QrDecodeResult duplicate = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1])[0];
        parts[0].CorrectedErrorCount = 4;
        parts[0].Warnings = ["noisy"];
        duplicate.CorrectedErrorCount = 1;
        duplicate.Warnings = ["clearer"];
        foreach (QrDecodeResult[] input in new[] { new[] { parts[0], parts[1], duplicate }, new[] { duplicate, parts[1], parts[0] } })
        {
            StructuredAppendAssemblyResult result = Assemble(input);
            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicateCount, Is.EqualTo(1));
            Assert.That(result.OrderedParts[0], Is.SameAs(duplicate));
            Assert.That(result.Result!.Text, Is.EqualTo("AB"));
            Assert.That(result.Result.CorrectedErrorCount, Is.EqualTo(1));
            Assert.That(result.Result.Warnings, Is.EqualTo(new[] { "Part 0: clearer", "Ignored 1 equivalent duplicate observations." }));
        }
    }

    [TestCase("bytes")]
    [TestCase("text")]
    [TestCase("eci")]
    [TestCase("segments")]
    public void ConflictingDuplicateSemanticsNeverProduceAPayload(string conflict)
    {
        QrDecodeResult[] parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        QrDecodeResult duplicate = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1])[0];
        if (conflict == "bytes") { duplicate.RawBytes[0] = 90; duplicate.Segments[1].Data[0] = 90; }
        if (conflict == "text") duplicate.Text = "different";
        if (conflict == "eci") duplicate.EciAssignments = [26];
        if (conflict == "segments") duplicate.Segments[1].EciAssignment = 26;
        StructuredAppendAssemblyResult result = Assemble([parts[0], duplicate, parts[1]]);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
        Assert.That(result.ConflictingIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(result.Result, Is.Null);
    }

    [Test]
    public void ReportsExactMissingIndicesWithoutTrustingCompletenessOrDuplicates()
    {
        QrDecodeResult[] parts = DecodeParts([new byte[] { 1 }, new byte[] { 2 }, new byte[] { 3 }, new byte[] { 4 }], [-1, -1, -1, -1]);
        parts[3].StructuredAppend!.IsComplete = true;
        StructuredAppendAssemblyResult result = Assemble([parts[3], parts[1], parts[1]]);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.IncompleteSequence));
        Assert.That(result.MissingIndices, Is.EqualTo(new[] { 0, 2 }));
        Assert.That(result.OrderedParts, Is.EqualTo(new[] { parts[1], parts[3] }));
        Assert.That(result.ActualPartCount, Is.EqualTo(2));
        Assert.That(result.ExpectedPartCount, Is.EqualTo(4));
        Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(result.Result, Is.Null);
        Assert.That(parts[3].StructuredAppend!.IsComplete, Is.True);
    }

    [Test]
    public void RejectsCompleteSetsWithWrongParityOrDifferentIdentities()
    {
        QrDecodeResult[] parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        foreach (QrDecodeResult part in parts) part.StructuredAppend!.Parity ^= 1;
        Assert.That(Assemble(parts).FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
        parts[0].StructuredAppend!.ApplicationIdentifier = "other";
        Assert.That(Assemble(parts).Result, Is.Null);
        Assert.That(Assemble([parts[1], new QrDecodeResult()]).FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
    }

    [Test]
    public void RecoversUtf8CharacterSplitAcrossCompatiblePartBoundaries()
    {
        QrDecodeResult[] parts = DecodeParts([new byte[] { 0xe2, 0x82 }, new byte[] { 0xac, 0x21 }], [26, 26]);
        Assert.That(parts.All(p => p.Text == null), Is.True);
        StructuredAppendAssemblyResult result = Assemble([parts[1], parts[0]]);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Text, Is.EqualTo("€!"));
        Assert.That(result.Result.RawBytes, Is.EqualTo(new byte[] { 0xe2, 0x82, 0xac, 0x21 }));
        Assert.That(result.Result.EciAssignments, Is.EqualTo(new[] { 26, 26 }));
        Assert.That(result.Result.Warnings, Has.Some.Contains("Recovered text"));
        Assert.That(parts.All(p => p.Text == null), Is.True);
        Assert.That(result.Result.Segments.Count(s => s.Mode == QrEncodingMode.StructuredAppend), Is.EqualTo(2));
    }

    [Test]
    public void EncodingChangesAndDefaultResetsPreventCrossBoundaryMisinterpretation()
    {
        QrDecodeResult[] mixed = DecodeParts(["é"u8.ToArray(), new byte[] { 0xe9 }], [26, -1]);
        QrDecodeResult result = Assemble(mixed).Result!;
        Assert.That(result.Text, Is.EqualTo("éé"));
        Assert.That(result.Segments.Last().EciAssignment, Is.EqualTo(3));
        QrDecodeResult[] split = DecodeParts([new byte[] { 0xc3 }, new byte[] { 0xa9 }], [26, -1]);
        StructuredAppendAssemblyResult failedText = Assemble(split);
        Assert.That(failedText.Success, Is.True);
        Assert.That(failedText.Result!.Text, Is.Null);
        Assert.That(failedText.Result.RawBytes, Is.EqualTo(new byte[] { 0xc3, 0xa9 }));
    }

    [Test]
    public void UnsupportedEciRetainsAllBytesWithoutFabricatingText()
    {
        QrDecodeResult[] parts = DecodeParts([new byte[] { 0, 255 }, new byte[] { 1, 254 }], [900, 900]);
        StructuredAppendAssemblyResult result = Assemble(parts);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.Text, Is.Null);
        Assert.That(result.Result.RawBytes, Is.EqualTo(new byte[] { 0, 255, 1, 254 }));
        Assert.That(result.Result.EciAssignments, Is.EqualTo(new[] { 900, 900 }));
        Assert.That(result.Result.Warnings, Has.Some.Contains("all sequence payload bytes"));
    }

    [TestCase(29, 29)]
    [TestCase(29, 65)]
    [TestCase(37, 65)]
    public void Fnc1InterpretationStaysWithinEachPart(int first, int second)
    {
        QrDecodeResult[] parts = DecodeParts([new byte[] { (byte)first }, new byte[] { (byte)second }], [-1, -1], true);
        StructuredAppendAssemblyResult result = Assemble(parts);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Result!.UsesFnc1First, Is.True);
        Assert.That(result.Result.Text, Is.EqualTo(new string([(char)first, (char)second])));
        byte[] expected = first == 37 ? [37, 37, (byte)second] : [37, second == 29 ? (byte)37 : (byte)second];
        Assert.That(result.Result.RawBytes, Is.EqualTo(expected));
        Assert.That(result.Result.SymbologyIdentifier, Is.EqualTo(QrSymbologyIdentifiers.Model2WithFnc1First));
    }

    [Test]
    public void MetadataOnlyPartsPreserveKnownTextAndReportUnknownText()
    {
        QrDecodeResult[] parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        foreach (QrDecodeResult part in parts) part.Segments = [];
        Assert.That(Assemble(parts).Result!.Text, Is.EqualTo("AB"));
        parts[1].Text = null;
        Assert.That(Assemble(parts).Result!.Text, Is.Null);
    }

    [Test]
    public void RejectsInconsistentPayloadBuffersAndEnforcesLimits()
    {
        QrDecodeResult[] parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        parts[0].RawBytes = [90];
        Assert.That(Assemble(parts).FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
        parts[0].RawBytes = null!;
        Assert.Throws<QrInputException>(() => Assemble(parts));
        Assert.Throws<QrInputException>(() => Assemble([]));
        Assert.Throws<QrInputException>(() => Assemble(null!));
        parts = DecodeParts(["A"u8.ToArray(), "B"u8.ToArray()], [-1, -1]);
        Assert.Throws<QrResourceLimitException>(() => StructuredAppendAssembly.Assemble(parts, 1, 1_000_000));
        Assert.Throws<QrResourceLimitException>(() => StructuredAppendAssembly.Assemble(parts, 256, 3000));
        parts[0].CorrectedErrorCount = int.MaxValue;
        parts[1].CorrectedErrorCount = 1;
        Assert.Throws<QrResourceLimitException>(() => Assemble(parts));
    }

    static StructuredAppendAssemblyResult Assemble(QrDecodeResult[] parts) => StructuredAppendAssembly.Assemble(parts, 256, 1_000_000);

    static QrDecodeResult[] DecodeParts(byte[][] payloads, int[] assignments, bool fnc1 = false)
    {
        int parity = payloads.SelectMany(p => p).Aggregate(0, (a, b) => a ^ b);
        QrDecodeResult[] parts = new QrDecodeResult[payloads.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            List<QrSegment> segments = [new QrSegment { Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = parts.Length, Parity = parity } }];
            if (fnc1) segments.Add(Fnc1Encoder.CreateFirstPositionControlSegment());
            if (assignments[index] >= 0) segments.Add(EciEncoder.CreateControlSegment(assignments[index]));
            segments.Add(fnc1 ? new QrSegment { Mode = QrEncodingMode.Alphanumeric,
                Text = System.Text.Encoding.ASCII.GetString(payloads[index]), Data = payloads[index], CharacterCount = payloads[index].Length }
                : new QrSegment { Mode = QrEncodingMode.Byte, Data = payloads[index], CharacterCount = payloads[index].Length, EciAssignment = assignments[index] });
            QrMatrix matrix;
            if (fnc1)
            {
                // Write the on-wire FNC1 alphanumeric representation directly.
                BitBuffer bits = new BitBuffer(19);
                bits.AppendBits(3, 4);
                bits.AppendBits((uint)index, 4);
                bits.AppendBits((uint)(parts.Length - 1), 4);
                bits.AppendBits((uint)parity, 8);
                bits.AppendBits(5, 4);
                bits.AppendBits(2, 4);
                string escaped = Fnc1Encoder.EscapeGs1AlphanumericText(System.Text.Encoding.ASCII.GetString(payloads[index]));
                bits.AppendBits((uint)escaped.Length, 9);
                AlphanumericEncoder.EncodeText(escaped, bits);
                while (bits.GetBitLength() < 152) bits.AppendBit(false);
                matrix = CodewordErasureRecoveryTests.Build(BlockInterleaver.CreateModel2Codewords(bits.GetBytes(), 1, 0), QrSymbolKind.Model2, 1, 0);
            }
            else
            {
                matrix = new QrEncoder().EncodeSegments(segments.ToArray(), new QrEncodeOptions
                {
                    ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0
                }).Matrix!;
            }
            parts[index] = new QrDecoder().DecodeMatrix(matrix, new QrDecodeOptions { Strict = !fnc1 });
            parts[index].StructuredAppend!.ApplicationIdentifier = "message";
        }
        return parts;
    }
}
