using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ByteEncoderTests
{
    [TestCase(0, 0)]
    [TestCase(1, 8)]
    [TestCase(2, 16)]
    [TestCase(255, 2040)]
    public void CalculatesExactBitLength(int byteCount, int expectedBitLength)
    {
        Assert.That(ByteEncoder.CalculateBitLength(byteCount), Is.EqualTo(expectedBitLength));
    }

    [Test]
    public void EncodesEveryByteValueAndEmbeddedZeroesExactly()
    {
        byte[] bytes = new byte[258];
        for (int value = 0; value < 256; value++)
        {
            bytes[value] = (byte)value;
        }

        bytes[256] = 0;
        bytes[257] = 255;
        BitBuffer buffer = new BitBuffer(0);

        ByteEncoder.EncodeBytes(bytes, buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(bytes.Length * 8));
            Assert.That(buffer.GetBytes(), Is.EqualTo(bytes));
        });
    }

    [Test]
    public void AppendsAtAnUnalignedPositionWithoutChangingPrefixBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        ByteEncoder.EncodeBytes(new byte[] { 0, 255 }, buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(19));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0, 0x1F, 0xE0 }));
        });
    }

    [Test]
    public void CopiesInputBeforeTheCallerCanMutateIt()
    {
        byte[] bytes = new byte[] { 1, 0, 2 };
        BitBuffer buffer = new BitBuffer(0);

        ByteEncoder.EncodeBytes(bytes, buffer);
        bytes[1] = 255;

        Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 1, 0, 2 }));
    }

    [Test]
    public void RejectsInvalidInputsAndBitLengthOverflow()
    {
        BitBuffer buffer = new BitBuffer(0);
        QrInputException nullBytes = Assert.Throws<QrInputException>(() =>
            ByteEncoder.EncodeBytes(null!, buffer))!;
        QrInputException nullDestination = Assert.Throws<QrInputException>(() =>
            ByteEncoder.EncodeBytes(new byte[0], null!))!;
        QrInputException negativeCount = Assert.Throws<QrInputException>(() =>
            ByteEncoder.CalculateBitLength(-1))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            ByteEncoder.CalculateBitLength(int.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullBytes.InputName, Is.EqualTo("bytes"));
            Assert.That(nullDestination.InputName, Is.EqualTo("destination"));
            Assert.That(negativeCount.InputName, Is.EqualTo("byteCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("ByteBitLength"));
        });
    }

    [Test]
    public void RejectsOverLimitInputBeforeChangingDestination()
    {
        byte[] bytes = new byte[QrLimits.DefaultMaximumInputBytes + 1];
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrResourceLimitException exception = Assert.Throws<QrResourceLimitException>(() =>
            ByteEncoder.EncodeBytes(bytes, buffer))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }
}
