using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Fnc1DecoderTests
{
    [Test]
    public void EveryIndicatorByteIsAcceptedOrRejectedWithoutLosingReaderPosition()
    {
        for (int value = 0; value < 256; value++)
        {
            BitBuffer buffer = new BitBuffer(0);
            buffer.AppendBits(5, 3);
            buffer.AppendBits((uint)value, 8);
            buffer.AppendBits(6, 3);
            BitReader reader = new BitReader(buffer.GetBytes(), 14);
            reader.SkipBits(3);
            bool assigned = value <= 99 || value >= 165 && value <= 190;
            Assert.That(Fnc1Decoder.TryDecodeSecondPositionIndicator(reader), Is.EqualTo(assigned ? value : -1), $"indicator {value}");
            Assert.That(reader.GetPosition(), Is.EqualTo(assigned ? 11 : 3));
            if (assigned)
            {
                Assert.That(reader.ReadBits(3), Is.EqualTo(6));
                if (value >= 165) Assert.That(Fnc1Decoder.GetSecondPositionIndicatorLetter(value), Is.EqualTo((char)('A' + value - 165)));
            }
            else
            {
                Assert.That(Assert.Throws<QrDecodeException>(() => Fnc1Decoder.DecodeSecondPositionIndicator(reader))!.FailureReason,
                    Is.EqualTo(QrFailureReason.InvalidData));
                Assert.That(reader.GetPosition(), Is.EqualTo(3));
                Assert.Throws<QrInputException>(() => Fnc1Encoder.CreateSecondPositionControlSegment(value));
            }
        }
    }

    [Test]
    public void EveryTruncatedIndicatorAtEveryBitOffsetLeavesTheReaderUnchanged()
    {
        for (int offset = 0; offset < 8; offset++)
            for (int available = 0; available < 8; available++)
            {
                BitReader reader = new BitReader(new byte[] { 0, 0 }, offset + available);
                reader.SkipBits(offset);
                Assert.That(Fnc1Decoder.TryDecodeSecondPositionIndicator(reader), Is.EqualTo(-1));
                Assert.That(reader.GetPosition(), Is.EqualTo(offset));
                Assert.That(Assert.Throws<QrDecodeException>(() => Fnc1Decoder.DecodeSecondPositionIndicator(reader))!.FailureReason,
                    Is.EqualTo(QrFailureReason.InvalidData));
                Assert.That(reader.GetPosition(), Is.EqualTo(offset));
            }
    }

    [TestCase("", "")]
    [TestCase("ABC123", "ABC123")]
    [TestCase("%", "\u001D")]
    [TestCase("%%", "%")]
    [TestCase("%%%", "%\u001D")]
    [TestCase("%%%%", "%%")]
    [TestCase("A%B%%C", "A\u001DB%C")]
    public void DecodesPercentEscapesAndTransmitsGroupSeparators(string encoded, string expected)
    {
        Assert.That(Fnc1Decoder.DecodeGs1AlphanumericText(encoded), Is.EqualTo(expected));
    }

    [Test]
    public void RoundTripsMixedGs1TextLosslessly()
    {
        string text = "0101234567890128" + Fnc1Encoder.GroupSeparator + "10ABC%25";
        string encoded = Fnc1Encoder.EscapeGs1AlphanumericText(text);

        string decoded = Fnc1Decoder.DecodeGs1AlphanumericText(encoded);

        Assert.That(decoded, Is.EqualTo(text));
    }

    [Test]
    public void DecodesKnownAlphanumericBitsWithGs1Processing()
    {
        BitReader reader = new BitReader(new byte[] { 0xDA, 0x9A, 0xE0 }, 22);

        string decoded = Fnc1Decoder.DecodeFirstPositionAlphanumeric(reader, 4);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo("%" + Fnc1Encoder.GroupSeparator + "A"));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void CreatesControlSegmentAndAppliesFirstPositionMetadata()
    {
        QrSegment segment = Fnc1Decoder.CreateFirstPositionControlSegment();
        QrDecodeResult withoutEci = new QrDecodeResult();
        QrDecodeResult withEci = new QrDecodeResult();

        Fnc1Decoder.ApplyFirstPositionMetadata(withoutEci, false);
        Fnc1Decoder.ApplyFirstPositionMetadata(withEci, true);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Fnc1First));
            Assert.That(segment.Data, Is.Empty);
            Assert.That(withoutEci.UsesFnc1First, Is.True);
            Assert.That(withoutEci.SymbologyIdentifier,
                Is.EqualTo(QrSymbologyIdentifiers.Model2WithFnc1First));
            Assert.That(withEci.UsesFnc1First, Is.True);
            Assert.That(withEci.SymbologyIdentifier,
                Is.EqualTo(QrSymbologyIdentifiers.Model2WithEciAndFnc1First));
        });
    }

    [Test]
    public void RejectsInvalidOrNullEncodedTextAndNullMetadataTarget()
    {
        QrInputException invalid = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.DecodeGs1AlphanumericText("ABCd"))!;
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.DecodeGs1AlphanumericText(null!))!;
        QrInputException nullResult = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.ApplyFirstPositionMetadata(null!, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.InputName, Is.EqualTo("character"));
            Assert.That(nullText.InputName, Is.EqualTo("encodedText"));
            Assert.That(nullResult.InputName, Is.EqualTo("result"));
        });
    }

    [Test]
    public void TruncatedAlphanumericPayloadDoesNotConsumeReaderBits()
    {
        BitReader reader = new BitReader(new byte[] { 0, 0 }, 10);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            Fnc1Decoder.DecodeFirstPositionAlphanumeric(reader, 2))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [TestCase(0)]
    [TestCase(42)]
    [TestCase(99)]
    [TestCase(165)]
    [TestCase(190)]
    public void DecodesAndPreservesAssignedApplicationIndicators(int applicationIndicator)
    {
        BitBuffer buffer = new BitBuffer(0);
        Fnc1Encoder.EncodeSecondPositionIndicator(applicationIndicator, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), 8);

        int decoded = Fnc1Decoder.DecodeSecondPositionIndicator(reader);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(applicationIndicator));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [TestCase(165, 'A')]
    [TestCase(177, 'M')]
    [TestCase(190, 'Z')]
    public void RestoresAlphabeticApplicationIndicators(int applicationIndicator, char expected)
    {
        Assert.That(
            Fnc1Decoder.GetSecondPositionIndicatorLetter(applicationIndicator),
            Is.EqualTo(expected));
    }

    [Test]
    public void CreatesSecondPositionSegmentWithExactRawIndicator()
    {
        BitReader reader = new BitReader(new byte[] { 177 }, 8);

        QrSegment segment = Fnc1Decoder.CreateSecondPositionControlSegment(reader);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Fnc1Second));
            Assert.That(segment.CharacterCount, Is.EqualTo(0));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 177 }));
            Assert.That(segment.Fnc1ApplicationIndicator, Is.EqualTo(177));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsTruncatedAndUnassignedIndicatorsWithoutConsumption()
    {
        BitReader truncated = new BitReader(new byte[] { 99 }, 7);
        BitReader unassigned = new BitReader(new byte[] { 100 }, 8);

        QrDecodeException truncatedException = Assert.Throws<QrDecodeException>(() =>
            Fnc1Decoder.DecodeSecondPositionIndicator(truncated))!;
        QrDecodeException unassignedException = Assert.Throws<QrDecodeException>(() =>
            Fnc1Decoder.DecodeSecondPositionIndicator(unassigned))!;

        Assert.Multiple(() =>
        {
            Assert.That(truncatedException.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(unassignedException.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(truncated.GetPosition(), Is.EqualTo(0));
            Assert.That(unassigned.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void AppliesSecondPositionIndicatorAndSymbologyMetadata()
    {
        QrDecodeResult withoutEci = new QrDecodeResult();
        QrDecodeResult withEci = new QrDecodeResult();

        Fnc1Decoder.ApplySecondPositionMetadata(withoutEci, 42, false);
        Fnc1Decoder.ApplySecondPositionMetadata(withEci, 177, true);

        Assert.Multiple(() =>
        {
            Assert.That(withoutEci.Fnc1SecondApplicationIndicator, Is.EqualTo(42));
            Assert.That(withoutEci.SymbologyIdentifier,
                Is.EqualTo(QrSymbologyIdentifiers.Model2WithFnc1Second));
            Assert.That(withEci.Fnc1SecondApplicationIndicator, Is.EqualTo(177));
            Assert.That(withEci.SymbologyIdentifier,
                Is.EqualTo(QrSymbologyIdentifiers.Model2WithEciAndFnc1Second));
        });
    }

    [Test]
    public void RejectsInvalidSecondPositionHelperInputs()
    {
        QrInputException nullReader = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.DecodeSecondPositionIndicator(null!))!;
        QrInputException numericAsLetter = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.GetSecondPositionIndicatorLetter(42))!;
        QrInputException nullResult = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.ApplySecondPositionMetadata(null!, 42, false))!;
        QrInputException invalidIndicator = Assert.Throws<QrInputException>(() =>
            Fnc1Decoder.ApplySecondPositionMetadata(new QrDecodeResult(), 100, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullReader.InputName, Is.EqualTo("reader"));
            Assert.That(numericAsLetter.InputName, Is.EqualTo("applicationIndicator"));
            Assert.That(nullResult.InputName, Is.EqualTo("result"));
            Assert.That(invalidIndicator.InputName, Is.EqualTo("applicationIndicator"));
        });
    }
}
