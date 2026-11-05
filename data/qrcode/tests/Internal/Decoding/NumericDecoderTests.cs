using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class NumericDecoderTests
{
    [TestCase("")]
    [TestCase("7")]
    [TestCase("42")]
    [TestCase("123")]
    [TestCase("0000")]
    [TestCase("00102")]
    [TestCase("012345")]
    [TestCase("00012345")]
    public void RoundTripsAllTerminalGroupSizesAndLeadingZeroes(string digits)
    {
        BitBuffer buffer = new BitBuffer(0);
        NumericEncoder.EncodeDigits(digits, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        string decoded = NumericDecoder.DecodeDigits(reader, digits.Length);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(digits));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesKnownMaximumValuesForEachGroupWidth()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(999U, 10);
        buffer.AppendBits(99U, 7);
        buffer.AppendBits(9U, 4);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        Assert.Multiple(() =>
        {
            Assert.That(NumericDecoder.DecodeDigits(reader, 3), Is.EqualTo("999"));
            Assert.That(NumericDecoder.DecodeDigits(reader, 2), Is.EqualTo("99"));
            Assert.That(NumericDecoder.DecodeDigits(reader, 1), Is.EqualTo("9"));
        });
    }

    [Test]
    public void TruncatedSegmentIsRejectedBeforeReaderPositionChanges()
    {
        BitReader reader = new BitReader(new byte[] { 0, 0 }, 9);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            NumericDecoder.DecodeDigits(reader, 3))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsOutOfRangeThreeDigitGroup()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(1000U, 10);
        BitReader reader = new BitReader(buffer.GetBytes(), 10);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            NumericDecoder.DecodeDigits(reader, 3))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void RejectsOutOfRangeTwoDigitGroup()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(100U, 7);
        BitReader reader = new BitReader(buffer.GetBytes(), 7);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            NumericDecoder.DecodeDigits(reader, 2))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void RejectsOutOfRangeOneDigitGroup()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(10U, 4);
        BitReader reader = new BitReader(buffer.GetBytes(), 4);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            NumericDecoder.DecodeDigits(reader, 1))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void RejectsInvalidAndOverLimitCharacterCounts()
    {
        BitReader reader = new BitReader(new byte[0], 0);
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            NumericDecoder.DecodeDigits(reader, -1))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            NumericDecoder.DecodeDigits(reader, QrLimits.DefaultMaximumInputBytes + 1))!;

        Assert.Multiple(() =>
        {
            Assert.That(negative.InputName, Is.EqualTo("characterCount"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }
}
