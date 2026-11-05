using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class AlphanumericDecoderTests
{
    [TestCase("")]
    [TestCase("A")]
    [TestCase("AB")]
    [TestCase("ABC")]
    [TestCase("HELLO WORLD")]
    [TestCase("$%*+-./:")]
    [TestCase("A%BC%%12")]
    [TestCase("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:")]
    public void RoundTripsEvenOddAndFullRepertoireText(string text)
    {
        BitBuffer buffer = new BitBuffer(0);
        AlphanumericEncoder.EncodeText(text, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        string decoded = AlphanumericDecoder.DecodeText(reader, text.Length);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(text));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesMaximumAssignedPairAndTerminalValues()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(2024U, 11);
        buffer.AppendBits(44U, 6);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        Assert.Multiple(() =>
        {
            Assert.That(AlphanumericDecoder.DecodeText(reader, 2), Is.EqualTo("::"));
            Assert.That(AlphanumericDecoder.DecodeText(reader, 1), Is.EqualTo(":"));
        });
    }

    [Test]
    public void TruncatedSegmentIsRejectedBeforeReaderPositionChanges()
    {
        BitReader reader = new BitReader(new byte[] { 0, 0 }, 10);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            AlphanumericDecoder.DecodeText(reader, 2))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsUnassignedPairValue()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(2025U, 11);
        BitReader reader = new BitReader(buffer.GetBytes(), 11);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            AlphanumericDecoder.DecodeText(reader, 2))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void RejectsUnassignedTerminalValue()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(45U, 6);
        BitReader reader = new BitReader(buffer.GetBytes(), 6);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            AlphanumericDecoder.DecodeText(reader, 1))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void RejectsInvalidAndOverLimitCharacterCounts()
    {
        BitReader reader = new BitReader(new byte[0], 0);
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            AlphanumericDecoder.DecodeText(reader, -1))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            AlphanumericDecoder.DecodeText(reader, QrLimits.DefaultMaximumInputBytes + 1))!;

        Assert.Multiple(() =>
        {
            Assert.That(negative.InputName, Is.EqualTo("characterCount"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }
}
