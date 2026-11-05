using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class NumericEncoderTests
{
    [TestCase(0, 0)]
    [TestCase(1, 4)]
    [TestCase(2, 7)]
    [TestCase(3, 10)]
    [TestCase(4, 14)]
    [TestCase(5, 17)]
    [TestCase(6, 20)]
    [TestCase(7, 24)]
    [TestCase(8, 27)]
    public void CalculatesExactBitLengthForEveryTerminalGroupSize(int characterCount, int expectedBitLength)
    {
        Assert.That(NumericEncoder.CalculateBitLength(characterCount), Is.EqualTo(expectedBitLength));
    }

    [Test]
    public void EncodesKnownGroupsMostSignificantBitFirst()
    {
        BitBuffer buffer = new BitBuffer(0);

        NumericEncoder.EncodeDigits("01234567", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(27));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0x03, 0x15, 0x98, 0x60 }));
        });
    }

    [Test]
    public void AppendsWithoutChangingExistingPrefixBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        NumericEncoder.EncodeDigits("42", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(10));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xAA, 0x80 }));
        });
    }

    [Test]
    public void RejectsNonAsciiDigitsBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0xAU, 4);
        byte[] original = buffer.GetBytes();

        QrInputException letter = Assert.Throws<QrInputException>(() =>
            NumericEncoder.EncodeDigits("12A3", buffer))!;
        QrInputException sign = Assert.Throws<QrInputException>(() =>
            NumericEncoder.EncodeDigits("-12", buffer))!;
        QrInputException unicodeDigit = Assert.Throws<QrInputException>(() =>
            NumericEncoder.EncodeDigits("١٢", buffer))!;

        Assert.Multiple(() =>
        {
            Assert.That(letter.InputName, Is.EqualTo("digits"));
            Assert.That(sign.InputName, Is.EqualTo("digits"));
            Assert.That(unicodeDigit.InputName, Is.EqualTo("digits"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(4));
            Assert.That(buffer.GetBytes(), Is.EqualTo(original));
        });
    }

    [Test]
    public void RejectsNullNegativeCountAndBitLengthOverflow()
    {
        QrInputException nullDigits = Assert.Throws<QrInputException>(() =>
            NumericEncoder.ValidateDigits(null!))!;
        QrInputException negativeCount = Assert.Throws<QrInputException>(() =>
            NumericEncoder.CalculateBitLength(-1))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            NumericEncoder.CalculateBitLength(int.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullDigits.InputName, Is.EqualTo("digits"));
            Assert.That(negativeCount.InputName, Is.EqualTo("characterCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("NumericBitLength"));
        });
    }
}
