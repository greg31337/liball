using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrStructuredAppendAssemblerTests
{
    [Test]
    public void PublicEncoderDecoderAndBothAssemblerApisRoundTripAnUnorderedSequence()
    {
        byte[] original = Enumerable.Range(0, 60).Select(i => (byte)(65 + i % 26)).ToArray();
        QrSymbolSequence sequence = new QrEncoder().EncodeStructuredAppend(original, new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 1, MaximumVersion = 1,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low, QuietZoneModules = 0
        });
        IQrDecoder decoder = new QrDecoder();
        QrDecodeResult[] parts = sequence.Symbols.Select(s => decoder.DecodeMatrix(s.Matrix!, new QrDecodeOptions())).ToArray();
        QrDecodeResult duplicate = decoder.DecodeMatrix(sequence.Symbols[0].Matrix!, new QrDecodeOptions());
        QrDecodeResult[] input = parts.Reverse().Append(duplicate).ToArray();
        parts[1].CorrectedErrorCount = 2;
        parts[1].CorrectedErasureCount = 1;
        parts[1].Warnings = ["retained diagnostic"];
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrStructuredAppendAttemptResult attempt = assembler.TryAssembleSequence(input, 256, 1_000_000);
        QrDecodeResult result = assembler.AssembleSequence(input, 256, 1_000_000);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(attempt.ExpectedPartCount, Is.EqualTo(parts.Length));
        Assert.That(attempt.ActualPartCount, Is.EqualTo(parts.Length));
        Assert.That(attempt.Parity, Is.EqualTo(sequence.StructuredAppend!.Parity));
        Assert.That(attempt.DuplicateCount, Is.EqualTo(1));
        Assert.That(attempt.MissingIndices, Is.Empty);
        Assert.That(attempt.ConflictingIndices, Is.Empty);
        Assert.That(result.RawBytes, Is.EqualTo(original));
        Assert.That(result.Text, Is.EqualTo(System.Text.Encoding.ASCII.GetString(original)));
        Assert.That(result.RawBytes, Is.EqualTo(attempt.Result!.RawBytes));
        Assert.That(result.Warnings, Is.EqualTo(attempt.Result.Warnings));
        Assert.That(result.Warnings, Has.Some.Contains("retained diagnostic"));
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(2));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(1));
        Assert.That(result.StructuredAppend!.IsComplete, Is.True);
        Assert.That(result.Symbol, Is.Null);
        Assert.That(result.Detection, Is.Null);
        result.RawBytes[0] = 0;
        QrSegment payload = result.Segments.First(s => s.Mode == QrEncodingMode.Byte);
        payload.Data[0] = 0;
        result.Segments[0].StructuredAppend!.Index = 15;
        Assert.That(parts[0].RawBytes[0], Is.EqualTo(original[0]));
        Assert.That(parts[0].StructuredAppend!.Index, Is.Zero);
        Assert.That(attempt.Result.RawBytes, Is.EqualTo(original));
        Assert.That(assembler.AssembleSequence(input, 256, 1_000_000).RawBytes, Is.EqualTo(original));
    }

    [Test]
    public void MissingPartsHaveMatchingExceptionDiagnosticsAndIndependentIndexArrays()
    {
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray(), "C"u8.ToArray()]);
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] input = [parts[2], parts[2]];
        QrStructuredAppendAttemptResult attempt = AssertFailure(assembler, input, QrFailureReason.IncompleteSequence);
        Assert.That(attempt.ExpectedPartCount, Is.EqualTo(3));
        Assert.That(attempt.ActualPartCount, Is.EqualTo(1));
        Assert.That(attempt.Parity, Is.EqualTo(65 ^ 66 ^ 67));
        Assert.That(attempt.MissingIndices, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(attempt.ConflictingIndices, Is.Empty);
        Assert.That(attempt.DuplicateCount, Is.EqualTo(1));
        attempt.MissingIndices[0] = 99;
        Assert.That(assembler.TryAssembleSequence(input, 256, 1_000_000).MissingIndices, Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void DuplicatePayloadConflictsReturnNoPayloadAndIdentifyThePosition()
    {
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        QrDecodeResult[] collision = Parts(["Z"u8.ToArray(), "Y"u8.ToArray()]); // Same parity, different data.
        QrStructuredAppendAttemptResult attempt = AssertFailure(new QrStructuredAppendAssembler(),
            [parts[1], collision[0], parts[0]], QrFailureReason.ConflictingSequence);
        Assert.That(attempt.ExpectedPartCount, Is.EqualTo(2));
        Assert.That(attempt.ActualPartCount, Is.EqualTo(2));
        Assert.That(attempt.Parity, Is.EqualTo(3));
        Assert.That(attempt.ConflictingIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(attempt.DuplicateCount, Is.EqualTo(1));
    }

    [Test]
    public void ConflictingApplicationControlsPreserveKnownIdentityDiagnostics()
    {
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        foreach (QrDecodeResult part in parts) part.Segments = [];
        parts[0].UsesFnc1First = true;
        QrStructuredAppendAttemptResult attempt = AssertFailure(new QrStructuredAppendAssembler(), parts, QrFailureReason.ConflictingSequence);
        Assert.That(attempt.ExpectedPartCount, Is.EqualTo(2));
        Assert.That(attempt.ActualPartCount, Is.EqualTo(2));
        Assert.That(attempt.Parity, Is.EqualTo(3));
    }

    [Test]
    public void ParityMismatchAndMixedIdentitiesAreExpectedSequenceFailures()
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        foreach (QrDecodeResult part in parts) part.StructuredAppend!.Parity ^= 1;
        QrStructuredAppendAttemptResult mismatch = AssertFailure(assembler, parts, QrFailureReason.ConflictingSequence);
        Assert.That(mismatch.ActualPartCount, Is.EqualTo(2));
        Assert.That(mismatch.Parity, Is.EqualTo(2));
        parts[0].StructuredAppend!.ApplicationIdentifier = "another message";
        QrStructuredAppendAttemptResult mixed = AssertFailure(assembler, parts, QrFailureReason.ConflictingSequence);
        Assert.That(mixed.ExpectedPartCount, Is.Zero);
        Assert.That(mixed.Parity, Is.EqualTo(-1));
        AssertFailure(assembler, [parts[1], new QrDecodeResult()], QrFailureReason.ConflictingSequence);
    }

    [Test]
    public void EncodingBoundariesCanRecoverTextWhileUnknownEncodingsRetainBytes()
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] utf8 = Parts([new byte[] { 0xe2, 0x82 }, new byte[] { 0xac }]);
        Assert.That(utf8.All(p => p.Text == null), Is.True);
        QrStructuredAppendAttemptResult recovered = assembler.TryAssembleSequence(utf8, 256, 1_000_000);
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.Result!.Text, Is.EqualTo("€"));
        QrDecodeResult[] unsupported = Parts([new byte[] { 0, 255 }, new byte[] { 1, 254 }], 900);
        QrDecodeResult result = assembler.AssembleSequence(unsupported, 256, 1_000_000);
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0, 255, 1, 254 }));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 900, 900 }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BothApisRejectInvalidInputsAndHonorConfiguredLimits(bool attempt)
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        void Run(QrDecodeResult[] input, int count, int bytes)
        {
            if (attempt) assembler.TryAssembleSequence(input, count, bytes);
            else assembler.AssembleSequence(input, count, bytes);
        }
        Assert.Throws<QrInputException>(() => Run(null!, 256, 1_000_000));
        Assert.Throws<QrInputException>(() => Run([], 256, 1_000_000));
        Assert.Throws<QrInputException>(() => Run([null!], 256, 1_000_000));
        Assert.Throws<QrInputException>(() => Run(parts, 0, 1_000_000));
        Assert.Throws<QrInputException>(() => Run(parts, 256, 0));
        Assert.Throws<QrResourceLimitException>(() => Run(parts, 1, 1_000_000));
        Assert.Throws<QrResourceLimitException>(() => Run(parts, 256, 1024));
        Run(parts, 2, 1_000_000);
        Assert.Throws<QrResourceLimitException>(() => Run([parts[0], parts[1], parts[0]], 2, 1_000_000));
        parts[0].RawBytes = null!;
        Assert.Throws<QrInputException>(() => Run(parts, 256, 1_000_000));
    }

    [Test]
    public void OneAssemblerCanBeReusedConcurrentlyWithoutSharingResults()
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        QrDecodeResult[] outputs = new QrDecodeResult[8];
        Parallel.For(0, outputs.Length, i => outputs[i] = assembler.AssembleSequence(parts, 256, 1_000_000));
        Assert.That(outputs.Select(r => r.Text), Is.All.EqualTo("AB"));
        outputs[0].RawBytes[0] = 0;
        Assert.That(outputs.Skip(1).Select(r => r.RawBytes[0]), Is.All.EqualTo((byte)65));
        Assert.That(parts[0].RawBytes, Is.EqualTo("A"u8.ToArray()));
    }

    [Test]
    public void AttemptFailuresDoNotRetainPayloadOrDiagnosticsFromEarlierCalls()
    {
        IQrStructuredAppendAssembler assembler = new QrStructuredAppendAssembler();
        QrDecodeResult[] parts = Parts(["A"u8.ToArray(), "B"u8.ToArray()]);
        QrStructuredAppendAttemptResult complete = assembler.TryAssembleSequence(parts, 10, 1_000_000);
        Assert.That(complete.Success, Is.True);
        Assert.That(complete.Result!.RawBytes, Is.EqualTo("AB"u8.ToArray()));

        QrStructuredAppendAttemptResult missing = AssertFailure(assembler, [parts[0]], QrFailureReason.IncompleteSequence);
        Assert.That(missing.MissingIndices, Is.EqualTo(new[] { 1 }));
        // These messages have equal XOR parity, so the duplicate conflicts within one sequence identity.
        QrDecodeResult[] conflicting = Parts(["C"u8.ToArray(), "@"u8.ToArray()]);
        QrStructuredAppendAttemptResult conflict = AssertFailure(assembler, [parts[0], conflicting[0], parts[1]], QrFailureReason.ConflictingSequence);
        Assert.That(conflict.ConflictingIndices, Is.EqualTo(new[] { 0 }));
        Assert.That(conflict.MissingIndices, Is.Empty);
        Assert.That(missing.MissingIndices, Is.EqualTo(new[] { 1 }));
        Assert.That(complete.Result.RawBytes, Is.EqualTo("AB"u8.ToArray()));

        QrStructuredAppendAttemptResult again = assembler.TryAssembleSequence(parts, 10, 1_000_000);
        QrDecodeResult thrownPath = assembler.AssembleSequence(parts, 10, 1_000_000);
        Assert.That(again.Success, Is.True);
        Assert.That(again.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(again.MissingIndices, Is.Empty);
        Assert.That(again.ConflictingIndices, Is.Empty);
        Assert.That(again.DuplicateCount, Is.Zero);
        Assert.That(again.Result!.Text, Is.EqualTo(thrownPath.Text).And.EqualTo("AB"));
        Assert.That(again.Result.RawBytes, Is.EqualTo(thrownPath.RawBytes));
        complete.Result.RawBytes[0] = 0;
        Assert.That(again.Result.RawBytes, Is.EqualTo("AB"u8.ToArray()));
    }

    static QrStructuredAppendAttemptResult AssertFailure(IQrStructuredAppendAssembler assembler, QrDecodeResult[] parts, QrFailureReason reason)
    {
        QrStructuredAppendAttemptResult attempt = assembler.TryAssembleSequence(parts, 256, 1_000_000);
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(reason));
        Assert.That(attempt.Result, Is.Null);
        QrStructuredAppendException error = Assert.Throws<QrStructuredAppendException>(() => assembler.AssembleSequence(parts, 256, 1_000_000))!;
        Assert.That(error.FailureReason, Is.EqualTo(attempt.FailureReason));
        Assert.That(error.ExpectedPartCount, Is.EqualTo(attempt.ExpectedPartCount));
        Assert.That(error.ActualPartCount, Is.EqualTo(attempt.ActualPartCount));
        Assert.That(error.Parity, Is.EqualTo(attempt.Parity));
        return attempt;
    }

    static QrDecodeResult[] Parts(byte[][] payloads, int assignment = 26)
    {
        int parity = payloads.SelectMany(b => b).Aggregate(0, (p, b) => p ^ b);
        QrDecodeResult[] results = new QrDecodeResult[payloads.Length];
        for (int index = 0; index < payloads.Length; index++)
        {
            QrSegment[] segments =
            [
                new QrSegment { Mode = QrEncodingMode.StructuredAppend,
                    StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = payloads.Length, Parity = parity } },
                new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = assignment,
                    Data = assignment < 128 ? [(byte)assignment] : [(byte)(0x80 | assignment >> 8), (byte)(assignment & 255)] },
                new QrSegment { Mode = QrEncodingMode.Byte, EciAssignment = assignment, Data = payloads[index], CharacterCount = payloads[index].Length }
            ];
            QrSymbol symbol = new QrEncoder().EncodeSegments(segments, new QrEncodeOptions
            {
                ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0
            });
            results[index] = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
        }
        return results;
    }
}
