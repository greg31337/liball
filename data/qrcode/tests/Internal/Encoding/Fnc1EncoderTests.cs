using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Fnc1EncoderTests
{
    [Test]
    public void RejectsAmbiguousEscapingBeforeMutatingEitherPositionPayload()
    {
        foreach (string text in new[] { "\u001d%", "\u001d\u001d", "A\u001d%%B", "%\u001d%B" })
        {
            BitBuffer buffer = new BitBuffer(0);
            buffer.AppendBits(5, 3);
            Assert.Throws<QrInputException>(() => Fnc1Encoder.CalculateEscapedLength(text));
            Assert.Throws<QrInputException>(() => Fnc1Encoder.EscapeGs1AlphanumericText(text));
            Assert.Throws<QrInputException>(() => Fnc1Encoder.EncodeFirstPositionText(text, buffer));
            Assert.Throws<QrInputException>(() => Fnc1Encoder.EncodeSecondPositionText(text, buffer));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xa0 }));
        }
    }

    [TestCase("", "")]
    [TestCase("ABC123", "ABC123")]
    [TestCase("%", "%%")]
    [TestCase("%%", "%%%%")]
    [TestCase("A%B", "A%%B")]
    public void EscapesLiteralPercentCharactersExactly(string text, string expected)
    {
        Assert.That(Fnc1Encoder.EscapeGs1AlphanumericText(text), Is.EqualTo(expected));
    }

    [Test]
    public void ConvertsGroupSeparatorsToSinglePercentCharacters()
    {
        string text = "0101234567890128" + Fnc1Encoder.GroupSeparator + "17270101%10ABC";

        string escaped = Fnc1Encoder.EscapeGs1AlphanumericText(text);

        Assert.That(escaped, Is.EqualTo("0101234567890128%17270101%%10ABC"));
    }

    [Test]
    public void CalculatesLengthIncludingOnlyLiteralPercentExpansion()
    {
        string text = "A%" + Fnc1Encoder.GroupSeparator + "B%%";

        Assert.That(Fnc1Encoder.CalculateEscapedLength(text), Is.EqualTo(9));
    }

    [Test]
    public void EncodesEscapedTextWithExactAlphanumericBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        string text = "%" + Fnc1Encoder.GroupSeparator + "A";

        Fnc1Encoder.EncodeFirstPositionText(text, buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(22));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xDA, 0x9A, 0xE0 }));
        });
    }

    [Test]
    public void AppendsAtAnUnalignedPositionAndRoundTrips()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);
        string text = "ABC%" + Fnc1Encoder.GroupSeparator + "17";
        string escaped = Fnc1Encoder.EscapeGs1AlphanumericText(text);

        Fnc1Encoder.EncodeFirstPositionText(text, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());
        reader.SkipBits(3);

        Assert.Multiple(() =>
        {
            Assert.That(Fnc1Decoder.DecodeFirstPositionAlphanumeric(reader, escaped.Length), Is.EqualTo(text));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void CreatesAZeroPayloadFirstPositionControlSegment()
    {
        QrSegment segment = Fnc1Encoder.CreateFirstPositionControlSegment();

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Fnc1First));
            Assert.That(segment.CharacterCount, Is.EqualTo(0));
            Assert.That(segment.Data, Is.Empty);
            Assert.That(segment.Fnc1ApplicationIndicator, Is.EqualTo(-1));
        });
    }

    [Test]
    public void RejectsInvalidTextBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrInputException lowercase = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.EncodeFirstPositionText("ABCd", buffer))!;
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.EncodeFirstPositionText(null!, buffer))!;
        QrInputException nullDestination = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.EncodeFirstPositionText("ABC", null!))!;

        Assert.Multiple(() =>
        {
            Assert.That(lowercase.InputName, Is.EqualTo("character"));
            Assert.That(nullText.InputName, Is.EqualTo("text"));
            Assert.That(nullDestination.InputName, Is.EqualTo("destination"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(99)]
    [TestCase(165)]
    [TestCase(190)]
    public void AcceptsAssignedSecondPositionIndicators(int applicationIndicator)
    {
        Assert.That(Fnc1Encoder.IsValidSecondPositionIndicator(applicationIndicator), Is.True);
    }

    [TestCase(-1)]
    [TestCase(100)]
    [TestCase(164)]
    [TestCase(191)]
    [TestCase(255)]
    public void RejectsUnassignedSecondPositionIndicators(int applicationIndicator)
    {
        Assert.That(Fnc1Encoder.IsValidSecondPositionIndicator(applicationIndicator), Is.False);
        QrInputException exception = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.ValidateSecondPositionIndicator(applicationIndicator))!;
        Assert.That(exception.InputName, Is.EqualTo("applicationIndicator"));
    }

    [TestCase('A', 165)]
    [TestCase('M', 177)]
    [TestCase('Z', 190)]
    public void ConvertsUppercaseLettersToApplicationIndicators(char letter, int expected)
    {
        Assert.That(Fnc1Encoder.GetLetterApplicationIndicator(letter), Is.EqualTo(expected));
    }

    [Test]
    public void EncodesSecondPositionIndicatorAtAnUnalignedPosition()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        Fnc1Encoder.EncodeSecondPositionIndicator(177, buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(11));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xB6, 0x20 }));
        });
    }

    [Test]
    public void InvalidSecondPositionIndicatorDoesNotChangeDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrInputException invalid = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.EncodeSecondPositionIndicator(100, buffer))!;
        QrInputException nullDestination = Assert.Throws<QrInputException>(() =>
            Fnc1Encoder.EncodeSecondPositionIndicator(0, null!))!;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.InputName, Is.EqualTo("applicationIndicator"));
            Assert.That(nullDestination.InputName, Is.EqualTo("destination"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void CreatesSecondPositionControlSegmentWithRawIndicator()
    {
        QrSegment numeric = Fnc1Encoder.CreateSecondPositionControlSegment(7);
        QrSegment alphabetic = Fnc1Encoder.CreateSecondPositionControlSegment(
            Fnc1Encoder.GetLetterApplicationIndicator('A'));

        Assert.Multiple(() =>
        {
            Assert.That(numeric.Mode, Is.EqualTo(QrEncodingMode.Fnc1Second));
            Assert.That(numeric.Data, Is.EqualTo(new byte[] { 7 }));
            Assert.That(numeric.Fnc1ApplicationIndicator, Is.EqualTo(7));
            Assert.That(alphabetic.Data, Is.EqualTo(new byte[] { 165 }));
            Assert.That(alphabetic.Fnc1ApplicationIndicator, Is.EqualTo(165));
        });
    }

    [Test]
    public void SecondPositionTextUsesTheSameFnc1EscapingRules()
    {
        string text = "ABC%" + Fnc1Encoder.GroupSeparator + "17";
        string escaped = Fnc1Encoder.EscapeGs1AlphanumericText(text);
        BitBuffer buffer = new BitBuffer(0);

        Fnc1Encoder.EncodeSecondPositionText(text, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        Assert.That(
            Fnc1Decoder.DecodeSecondPositionAlphanumeric(reader, escaped.Length),
            Is.EqualTo(text));
    }
}
