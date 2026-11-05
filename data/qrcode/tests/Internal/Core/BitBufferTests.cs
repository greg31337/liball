using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BitBufferTests
{
    [Test]
    public void AppendsBitsAndBytesInMostSignificantBitFirstOrder()
    {
        BitBuffer buffer = new BitBuffer(0);

        buffer.AppendBits(0b101U, 3);
        buffer.AppendBit(false);
        buffer.AppendBits(0b11110000U, 8);
        buffer.AppendBytes(new byte[] { 0x5A });

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(20));
            Assert.That(buffer.GetByteLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xAF, 0x05, 0xA0 }));
            Assert.That(buffer.GetBit(0), Is.True);
            Assert.That(buffer.GetBit(3), Is.False);
            Assert.That(buffer.GetBit(19), Is.False);
        });
    }

    [Test]
    public void AlignedByteAppendAndSpanCopyPreserveCallerValues()
    {
        BitBuffer buffer = new BitBuffer(1);
        byte[] input = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        byte[] destination = new byte[] { 0, 0, 0, 0, 0x77 };

        buffer.AppendBytes(input);
        buffer.CopyBytes(destination);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(32));
            Assert.That(destination, Is.EqualTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x77 }));
            Assert.That(input, Is.EqualTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
        });
    }

    [Test]
    public void ClearRemovesLengthAndPreviouslySetBits()
    {
        BitBuffer buffer = new BitBuffer(1);
        buffer.AppendBits(0xFFU, 8);

        buffer.ClearBits();
        buffer.AppendBit(false);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(1));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0 }));
        });
    }

    [Test]
    public void RejectsInvalidBitWidthsValuesIndicesAndDestinations()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(1U, 1);

        QrInputException width = Assert.Throws<QrInputException>(() => buffer.AppendBits(0U, 33))!;
        QrInputException value = Assert.Throws<QrInputException>(() => buffer.AppendBits(4U, 2))!;
        QrInputException index = Assert.Throws<QrInputException>(() => buffer.GetBit(1))!;
        QrInputException destination = Assert.Throws<QrInputException>(() => buffer.CopyBytes(new byte[0]))!;

        Assert.Multiple(() =>
        {
            Assert.That(width.InputName, Is.EqualTo("bitCount"));
            Assert.That(value.InputName, Is.EqualTo("value"));
            Assert.That(index.InputName, Is.EqualTo("index"));
            Assert.That(destination.InputName, Is.EqualTo("destination"));
        });
    }
}
