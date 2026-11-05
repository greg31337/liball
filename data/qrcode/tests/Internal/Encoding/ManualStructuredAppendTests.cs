using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ManualStructuredAppendTests
{
    [Test]
    public void EncodesCompleteManualSequenceWithSharedIdentityAndGoldenParity()
    {
        string[] payloads = new string[] { "STRUCTURED-", "APPEND-", "VECTOR" };
        QrSegment[][] parts = CreateParts(payloads, 6, "fixture-message");
        QrEncodeOptions options = CreateOptions();

        QrSymbolSequence sequence = StructuredAppendSplitter.EncodeManualParts(parts, options);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Symbols, Has.Length.EqualTo(3));
            Assert.That(sequence.RawBytes,
                Is.EqualTo(global::System.Text.Encoding.UTF8.GetBytes(
                    "STRUCTURED-APPEND-VECTOR")));
            Assert.That(sequence.Text, Is.EqualTo("STRUCTURED-APPEND-VECTOR"));
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(sequence.StructuredAppend, Is.Not.Null);
            Assert.That(sequence.StructuredAppend!.Total, Is.EqualTo(3));
            Assert.That(sequence.StructuredAppend.Parity, Is.EqualTo(6));
            Assert.That(sequence.StructuredAppend.ApplicationIdentifier,
                Is.EqualTo("fixture-message"));
            Assert.That(sequence.StructuredAppend.IsComplete, Is.True);
            Assert.That(parts[0][0].StructuredAppend!.IsComplete, Is.False);
        });

        for (int index = 0; index < sequence.Symbols.Length; index++)
        {
            QrSymbol symbol = sequence.Symbols[index];
            QrStructuredAppendInfo info = symbol.Segments[0].StructuredAppend!;
            Assert.Multiple(() =>
            {
                Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
                Assert.That(symbol.IsConformant, Is.True);
                Assert.That(info.Index, Is.EqualTo(index));
                Assert.That(info.Total, Is.EqualTo(3));
                Assert.That(info.Parity, Is.EqualTo(6));
                Assert.That(info.ApplicationIdentifier, Is.EqualTo("fixture-message"));
                Assert.That(info.IsComplete, Is.True);
                Assert.That(info, Is.Not.SameAs(parts[index][0].StructuredAppend));
            });
        }
    }

    [Test]
    public void RejectsHeadersThatAreNotInTheirDeclaredPositions()
    {
        QrSegment[][] parts = CreateParts(new string[] { "A", "B", "C" }, 64, "message");
        parts[0][0].StructuredAppend!.Index = 1;
        parts[1][0].StructuredAppend!.Index = 0;

        QrStructuredAppendException exception =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(parts, CreateOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(exception.ExpectedPartCount, Is.EqualTo(3));
            Assert.That(exception.ActualPartCount, Is.EqualTo(3));
            Assert.That(exception.Parity, Is.EqualTo(64));
        });
    }

    [Test]
    public void RejectsIncompleteAndConflictingDeclaredTotals()
    {
        QrSegment[][] incomplete = CreateParts(new string[] { "A", "B" }, 3, "message");
        incomplete[0][0].StructuredAppend!.Total = 3;
        incomplete[1][0].StructuredAppend!.Total = 3;
        QrSegment[][] conflict = CreateParts(new string[] { "A", "B" }, 3, "message");
        conflict[1][0].StructuredAppend!.Total = 3;

        QrStructuredAppendException incompleteException =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(incomplete, CreateOptions()))!;
        QrStructuredAppendException conflictException =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(conflict, CreateOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(incompleteException.FailureReason,
                Is.EqualTo(QrFailureReason.IncompleteSequence));
            Assert.That(incompleteException.ExpectedPartCount, Is.EqualTo(3));
            Assert.That(incompleteException.ActualPartCount, Is.EqualTo(2));
            Assert.That(conflictException.FailureReason,
                Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(conflictException.ExpectedPartCount, Is.EqualTo(2));
            Assert.That(conflictException.ActualPartCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void RejectsConflictingParityAndApplicationIdentity()
    {
        QrSegment[][] parityConflict = CreateParts(
            new string[] { "A", "B" },
            3,
            "message");
        parityConflict[1][0].StructuredAppend!.Parity = 4;
        QrSegment[][] identityConflict = CreateParts(
            new string[] { "A", "B" },
            3,
            "message");
        identityConflict[1][0].StructuredAppend!.ApplicationIdentifier = "other";

        QrStructuredAppendException parityException =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(parityConflict, CreateOptions()))!;
        QrStructuredAppendException identityException =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(identityConflict, CreateOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(parityException.FailureReason,
                Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(parityException.Parity, Is.EqualTo(-1));
            Assert.That(identityException.FailureReason,
                Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(identityException.Parity, Is.EqualTo(3));
        });
    }

    [Test]
    public void RejectsParityThatDoesNotMatchCombinedPayload()
    {
        QrSegment[][] parts = CreateParts(new string[] { "A", "B" }, 0, "message");

        QrStructuredAppendException exception =
            (QrStructuredAppendException)Assert.Throws<QrStructuredAppendException>(() =>
                StructuredAppendSplitter.EncodeManualParts(parts, CreateOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(exception.ExpectedPartCount, Is.EqualTo(2));
            Assert.That(exception.ActualPartCount, Is.EqualTo(2));
            Assert.That(exception.Parity, Is.Zero);
        });
    }

    [Test]
    public void RequiresAHeaderFirstInEveryPartAndHonorsAggregateLimits()
    {
        QrSegment[][] missingHeader = CreateParts(new string[] { "A", "B" }, 3, null);
        missingHeader[1] = new QrSegment[] { CreatePayload("B") };
        QrSegment[][] misplacedHeader = CreateParts(new string[] { "A", "B" }, 3, null);
        misplacedHeader[1] = new QrSegment[]
        {
            CreatePayload("B"),
            misplacedHeader[1][0]
        };
        QrSegment[][] tooLarge = CreateParts(new string[] { "AB", "CD" }, 4, null);
        QrEncodeOptions limitedBytes = CreateOptions();
        limitedBytes.MaximumInputBytes = 3;
        QrEncodeOptions limitedParts = CreateOptions();
        limitedParts.MaximumStructuredAppendSymbols = 2;
        QrSegment[][] threeParts = CreateParts(new string[] { "A", "B", "C" }, 64, null);

        Assert.Multiple(() =>
        {
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(null!, CreateOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(
                    new QrSegment[0][],
                    CreateOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(missingHeader, CreateOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(
                    misplacedHeader,
                    CreateOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(tooLarge, limitedBytes),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => StructuredAppendSplitter.EncodeManualParts(threeParts, limitedParts),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    private static QrSegment[][] CreateParts(
        string[] payloads,
        int parity,
        string? applicationIdentifier)
    {
        QrSegment[][] parts = new QrSegment[payloads.Length][];
        for (int index = 0; index < payloads.Length; index++)
        {
            parts[index] = new QrSegment[]
            {
                new QrSegment
                {
                    Mode = QrEncodingMode.StructuredAppend,
                    StructuredAppend = new QrStructuredAppendInfo
                    {
                        Index = index,
                        Total = payloads.Length,
                        Parity = parity,
                        ApplicationIdentifier = applicationIdentifier
                    }
                },
                CreatePayload(payloads[index])
            };
        }

        return parts;
    }

    private static QrSegment CreatePayload(string text)
    {
        byte[] data = global::System.Text.Encoding.UTF8.GetBytes(text);
        return new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = data.Length,
            Data = data,
            Text = text
        };
    }

    private static QrEncodeOptions CreateOptions()
    {
        return new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            MinimumVersion = 1,
            MaximumVersion = 2,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = false,
            MaskPattern = QrMaskPattern.Zero
        };
    }
}
