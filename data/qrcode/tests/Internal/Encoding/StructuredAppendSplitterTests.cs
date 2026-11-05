using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class StructuredAppendSplitterTests
{
    [Test]
    public void CalculatesStandardXorParityForTheCompleteUnslicedMessage()
    {
        byte[] message = global::System.Text.Encoding.UTF8.GetBytes(
            "STRUCTURED-APPEND-VECTOR");

        int parity = StructuredAppendSplitter.CalculateParity(message);

        Assert.That(parity, Is.EqualTo(6));
    }

    [Test]
    public void ShortMessageProducesTwoCompleteOrderedModel2Parts()
    {
        byte[] message = new byte[] { 0x41, 0x42 };
        QrEncodeOptions options = CreateVersionOneOptions(16);

        QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(message, options);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Symbols, Has.Length.EqualTo(2));
            Assert.That(sequence.RawBytes, Is.EqualTo(message));
            Assert.That(sequence.RawBytes, Is.Not.SameAs(message));
            Assert.That(sequence.Text, Is.Null);
            Assert.That(sequence.IsComplete, Is.True);
            Assert.That(sequence.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(sequence.StructuredAppend, Is.Not.Null);
            Assert.That(sequence.StructuredAppend!.Total, Is.EqualTo(2));
            Assert.That(sequence.StructuredAppend.Parity, Is.EqualTo(3));
            Assert.That(sequence.StructuredAppend.IsComplete, Is.True);
        });

        for (int index = 0; index < sequence.Symbols.Length; index++)
        {
            QrSymbol symbol = sequence.Symbols[index];
            QrStructuredAppendInfo info = symbol.Segments[0].StructuredAppend!;
            Assert.Multiple(() =>
            {
                Assert.That(symbol.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
                Assert.That(symbol.Version, Is.EqualTo(1));
                Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Low));
                Assert.That(symbol.MaskPattern, Is.EqualTo(QrMaskPattern.Zero));
                Assert.That(symbol.UsedDataBitCount, Is.EqualTo(40));
                Assert.That(symbol.IsConformant, Is.True);
                Assert.That(symbol.Segments, Has.Length.EqualTo(2));
                Assert.That(symbol.Segments[0].Mode, Is.EqualTo(QrEncodingMode.StructuredAppend));
                Assert.That(symbol.Segments[0].StructuredAppend, Is.Not.Null);
                Assert.That(info.Index, Is.EqualTo(index));
                Assert.That(info.Total, Is.EqualTo(2));
                Assert.That(info.Parity, Is.EqualTo(3));
                Assert.That(info.IsComplete, Is.True);
                Assert.That(symbol.Segments[1].Mode, Is.EqualTo(QrEncodingMode.Byte));
                Assert.That(symbol.Segments[1].Data, Is.EqualTo(new byte[] { message[index] }));
            });
        }
    }

    [Test]
    public void SelectsEveryLegalPartCountFromTwoThroughSixteen()
    {
        for (int expectedCount = 2; expectedCount <= 16; expectedCount++)
        {
            int messageLength = 1;
            if (expectedCount > 2)
            {
                messageLength = (expectedCount - 1) * 15 + 1;
            }

            byte[] message = CreateMessage(messageLength);
            QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(
                message,
                CreateVersionOneOptions(16));

            Assert.Multiple(() =>
            {
                Assert.That(sequence.Symbols, Has.Length.EqualTo(expectedCount));
                Assert.That(sequence.StructuredAppend!.Total, Is.EqualTo(expectedCount));
                Assert.That(sequence.StructuredAppend.Parity,
                    Is.EqualTo(StructuredAppendSplitter.CalculateParity(message)));
                Assert.That(CombinePayloads(sequence.Symbols), Is.EqualTo(message));
            });

            for (int index = 0; index < sequence.Symbols.Length; index++)
            {
                QrSymbol symbol = sequence.Symbols[index];
                QrStructuredAppendInfo info = symbol.Segments[0].StructuredAppend!;
                Assert.Multiple(() =>
                {
                    Assert.That(symbol.IsConformant, Is.True);
                    Assert.That(info.Index, Is.EqualTo(index));
                    Assert.That(info.Total, Is.EqualTo(expectedCount));
                    Assert.That(symbol.Segments[1].Data.Length, Is.LessThanOrEqualTo(15));
                });
            }
        }
    }

    [Test]
    public void EmptyAndSingleByteMessagesRemainLosslessAcrossTwoParts()
    {
        QrSymbolSequence empty = StructuredAppendSplitter.SplitBytes(
            new byte[0],
            CreateVersionOneOptions(16));
        QrSymbolSequence oneByte = StructuredAppendSplitter.SplitBytes(
            new byte[] { 0xA5 },
            CreateVersionOneOptions(16));

        Assert.Multiple(() =>
        {
            Assert.That(empty.Symbols, Has.Length.EqualTo(2));
            Assert.That(CombinePayloads(empty.Symbols), Is.Empty);
            Assert.That(empty.StructuredAppend!.Parity, Is.Zero);
            Assert.That(oneByte.Symbols, Has.Length.EqualTo(2));
            Assert.That(CombinePayloads(oneByte.Symbols), Is.EqualTo(new byte[] { 0xA5 }));
            Assert.That(oneByte.Symbols[0].Segments[1].Data, Is.EqualTo(new byte[] { 0xA5 }));
            Assert.That(oneByte.Symbols[1].Segments[1].Data, Is.Empty);
        });
    }

    [Test]
    public void OptimizesBoundariesAcrossVersionCapacityThresholds()
    {
        byte[] message = CreateMessage(81);
        QrEncodeOptions options = CreateVersionOneOptions(16);
        options.MaximumVersion = 40;

        QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(message, options);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Symbols, Has.Length.EqualTo(2));
            Assert.That(sequence.Symbols[0].Segments[1].Data, Has.Length.EqualTo(30));
            Assert.That(sequence.Symbols[1].Segments[1].Data, Has.Length.EqualTo(51));
            Assert.That(sequence.Symbols[0].Version, Is.EqualTo(2));
            Assert.That(sequence.Symbols[1].Version, Is.EqualTo(3));
            Assert.That(
                sequence.Symbols[0].Width * sequence.Symbols[0].Height
                    + sequence.Symbols[1].Width * sequence.Symbols[1].Height,
                Is.EqualTo(1466));
            Assert.That(CombinePayloads(sequence.Symbols), Is.EqualTo(message));
        });
    }

    [Test]
    public void RepeatsEciAndFnc1FirstSemanticsInEveryPart()
    {
        byte[] message = CreateMessage(20);
        QrEncodeOptions options = CreateVersionOneOptions(16);
        options.EciAssignment = QrEciAssignments.Utf8;
        options.UseFnc1First = true;

        QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(message, options);

        Assert.That(sequence.Symbols, Has.Length.EqualTo(2));
        for (int index = 0; index < sequence.Symbols.Length; index++)
        {
            QrSymbol symbol = sequence.Symbols[index];
            Assert.Multiple(() =>
            {
                Assert.That(symbol.Segments, Has.Length.EqualTo(4));
                Assert.That(symbol.Segments[0].Mode,
                    Is.EqualTo(QrEncodingMode.StructuredAppend));
                Assert.That(symbol.Segments[1].Mode, Is.EqualTo(QrEncodingMode.Fnc1First));
                Assert.That(symbol.Segments[2].Mode, Is.EqualTo(QrEncodingMode.Eci));
                Assert.That(symbol.Segments[2].EciAssignment,
                    Is.EqualTo(QrEciAssignments.Utf8));
                Assert.That(symbol.Segments[3].Mode, Is.EqualTo(QrEncodingMode.Byte));
                Assert.That(symbol.Segments[3].EciAssignment,
                    Is.EqualTo(QrEciAssignments.Utf8));
                Assert.That(symbol.Segments[3].Data, Has.Length.EqualTo(10));
                Assert.That(symbol.UsedDataBitCount, Is.EqualTo(128));
                Assert.That(symbol.IsConformant, Is.True);
            });
        }

        Assert.That(CombineLastPayloads(sequence.Symbols), Is.EqualTo(message));
    }

    [Test]
    public void RepeatsFnc1SecondSemanticsAndChargesItsIndicator()
    {
        byte[] message = CreateMessage(20);
        QrEncodeOptions options = CreateVersionOneOptions(16);
        options.Fnc1SecondApplicationIndicator = 42;

        QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(message, options);

        Assert.That(sequence.Symbols, Has.Length.EqualTo(2));
        for (int index = 0; index < sequence.Symbols.Length; index++)
        {
            QrSymbol symbol = sequence.Symbols[index];
            Assert.Multiple(() =>
            {
                Assert.That(symbol.Segments, Has.Length.EqualTo(3));
                Assert.That(symbol.Segments[1].Mode, Is.EqualTo(QrEncodingMode.Fnc1Second));
                Assert.That(symbol.Segments[1].Fnc1ApplicationIndicator, Is.EqualTo(42));
                Assert.That(symbol.Segments[1].Data, Is.EqualTo(new byte[] { 42 }));
                Assert.That(symbol.Segments[2].Data, Has.Length.EqualTo(10));
                Assert.That(symbol.UsedDataBitCount, Is.EqualTo(124));
                Assert.That(symbol.IsConformant, Is.True);
            });
        }

        Assert.That(CombineLastPayloads(sequence.Symbols), Is.EqualTo(message));
    }

    [Test]
    public void RejectsMessagesThatNeedTooManyConfiguredPartsWithExactCapacity()
    {
        byte[] message = CreateMessage(31);
        QrEncodeOptions options = CreateVersionOneOptions(2);

        QrCapacityException exception = (QrCapacityException)Assert.Throws<QrCapacityException>(() =>
            StructuredAppendSplitter.SplitBytes(message, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(248));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(240));
        });
    }

    [Test]
    public void ResultsDoNotAliasCallerInputOrOtherPartMetadata()
    {
        byte[] message = new byte[] { 1, 2, 3, 4 };
        QrSymbolSequence sequence = StructuredAppendSplitter.SplitBytes(
            message,
            CreateVersionOneOptions(16));

        message[0] = 99;
        sequence.RawBytes[1] = 88;
        sequence.Symbols[0].Segments[0].StructuredAppend!.Parity = 77;
        sequence.Symbols[0].Segments[1].Data[0] = 66;

        Assert.Multiple(() =>
        {
            Assert.That(sequence.RawBytes[0], Is.EqualTo(1));
            Assert.That(sequence.Symbols[0].Segments[1].Data[1], Is.EqualTo(2));
            Assert.That(sequence.Symbols[1].Segments[0].StructuredAppend!.Parity,
                Is.EqualTo(4));
            Assert.That(sequence.StructuredAppend!.Parity, Is.EqualTo(4));
        });
    }

    [Test]
    public void RejectsInvalidInputsLimitsFamiliesAndControlOptions()
    {
        QrEncodeOptions tooFewParts = CreateVersionOneOptions(1);
        QrEncodeOptions tooManyParts = CreateVersionOneOptions(17);
        QrEncodeOptions tooFewSegments = CreateVersionOneOptions(16);
        tooFewSegments.MaximumSegments = 1;
        QrEncodeOptions tooFewBytes = CreateVersionOneOptions(16);
        tooFewBytes.MaximumInputBytes = 1;
        QrEncodeOptions disabled = CreateVersionOneOptions(16);
        disabled.AllowModel2 = false;
        QrEncodeOptions wrongFamily = CreateVersionOneOptions(16);
        wrongFamily.SymbolKind = QrSymbolKind.Micro;
        QrEncodeOptions invalidEci = CreateVersionOneOptions(16);
        invalidEci.EciAssignment = -2;
        QrEncodeOptions bothFnc1 = CreateVersionOneOptions(16);
        bothFnc1.UseFnc1First = true;
        bothFnc1.Fnc1SecondApplicationIndicator = 42;
        QrEncodeOptions invalidFnc1Second = CreateVersionOneOptions(16);
        invalidFnc1Second.Fnc1SecondApplicationIndicator = 100;
        QrEncodeOptions tooFewControlSegments = CreateVersionOneOptions(16);
        tooFewControlSegments.EciAssignment = QrEciAssignments.Utf8;
        tooFewControlSegments.UseFnc1First = true;
        tooFewControlSegments.MaximumSegments = 3;
        QrEncodeOptions impossibleDimensions = CreateVersionOneOptions(16);
        impossibleDimensions.MinimumWidth = 22;

        Assert.Multiple(() =>
        {
            Assert.That(() => StructuredAppendSplitter.CalculateParity(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(null!, new QrEncodeOptions()),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], tooFewParts),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], tooManyParts),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], tooFewSegments),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[2], tooFewBytes),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], disabled),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], wrongFamily),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], invalidEci),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(new byte[0], bothFnc1),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(
                    new byte[0],
                    invalidFnc1Second),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(
                    new byte[0],
                    tooFewControlSegments),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => StructuredAppendSplitter.SplitBytes(
                    new byte[0],
                    impossibleDimensions),
                Throws.TypeOf<QrCapacityException>());
        });
    }

    private static QrEncodeOptions CreateVersionOneOptions(int maximumSymbols)
    {
        return new QrEncodeOptions
        {
            ForceSymbolKind = true,
            SymbolKind = QrSymbolKind.Model2,
            MinimumVersion = 1,
            MaximumVersion = 1,
            ForceErrorCorrectionLevel = true,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = false,
            MaskPattern = QrMaskPattern.Zero,
            MaximumStructuredAppendSymbols = maximumSymbols
        };
    }

    private static byte[] CreateMessage(int length)
    {
        byte[] message = new byte[length];
        for (int index = 0; index < message.Length; index++)
        {
            message[index] = (byte)((index * 37 + 11) & 255);
        }

        return message;
    }

    private static byte[] CombinePayloads(QrSymbol[] symbols)
    {
        int length = 0;
        for (int index = 0; index < symbols.Length; index++)
        {
            length += symbols[index].Segments[1].Data.Length;
        }

        byte[] combined = new byte[length];
        int offset = 0;
        for (int index = 0; index < symbols.Length; index++)
        {
            byte[] part = symbols[index].Segments[1].Data;
            for (int byteIndex = 0; byteIndex < part.Length; byteIndex++)
            {
                combined[offset] = part[byteIndex];
                offset++;
            }
        }

        return combined;
    }

    private static byte[] CombineLastPayloads(QrSymbol[] symbols)
    {
        int length = 0;
        for (int index = 0; index < symbols.Length; index++)
        {
            QrSegment[] segments = symbols[index].Segments;
            length += segments[segments.Length - 1].Data.Length;
        }

        byte[] combined = new byte[length];
        int offset = 0;
        for (int index = 0; index < symbols.Length; index++)
        {
            QrSegment[] segments = symbols[index].Segments;
            byte[] part = segments[segments.Length - 1].Data;
            for (int byteIndex = 0; byteIndex < part.Length; byteIndex++)
            {
                combined[offset] = part[byteIndex];
                offset++;
            }
        }

        return combined;
    }
}
