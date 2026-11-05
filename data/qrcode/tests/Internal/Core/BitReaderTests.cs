using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BitReaderTests
{
    [Test]
    public void ReadsAcrossByteBoundariesInMostSignificantBitFirstOrder()
    {
        BitReader reader = new BitReader(new byte[] { 0xAF, 0x05, 0xA0 }, 20);

        uint first = reader.ReadBits(3);
        bool fourth = reader.ReadBit();
        uint middle = reader.ReadBits(8);
        uint final = reader.ReadBits(8);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(0b101U));
            Assert.That(fourth, Is.False);
            Assert.That(middle, Is.EqualTo(0xF0U));
            Assert.That(final, Is.EqualTo(0x5AU));
            Assert.That(reader.GetPosition(), Is.EqualTo(20));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void ReaderOwnsASnapshotOfTheInputBuffer()
    {
        byte[] bytes = new byte[] { 0x80 };
        BitReader reader = new BitReader(bytes, 1);
        bytes[0] = 0;

        Assert.That(reader.ReadBit(), Is.True);
    }

    [Test]
    public void SeekSkipAndReadBytesSupportUnalignedPositions()
    {
        BitReader reader = new BitReader(new byte[] { 0b10101100, 0b11110000 }, 16);
        byte[] destination = new byte[1];

        Assert.That(reader.ReadBits(3), Is.EqualTo(0b101U));
        reader.ReadBytes(1, destination);
        reader.SkipBits(2);
        Assert.That(reader.GetPosition(), Is.EqualTo(13));
        reader.SeekBitPosition(8);

        Assert.Multiple(() =>
        {
            Assert.That(destination[0], Is.EqualTo(0x67));
            Assert.That(reader.GetPosition(), Is.EqualTo(8));
            Assert.That(reader.ReadBits(4), Is.EqualTo(0xFU));
        });
    }

    [Test]
    public void RejectsInvalidLengthPositionAndDestinationRequests()
    {
        QrInputException constructor = Assert.Throws<QrInputException>(() =>
            new BitReader(new byte[] { 0 }, 9))!;
        BitReader reader = new BitReader(new byte[] { 0x80 }, 1);
        QrInputException width = Assert.Throws<QrInputException>(() => reader.ReadBits(33))!;
        QrInputException destination = Assert.Throws<QrInputException>(() =>
            reader.ReadBytes(1, new byte[0]))!;
        QrInputException position = Assert.Throws<QrInputException>(() => reader.SeekBitPosition(2))!;

        Assert.Multiple(() =>
        {
            Assert.That(constructor.InputName, Is.EqualTo("bitLength"));
            Assert.That(width.InputName, Is.EqualTo("bitCount"));
            Assert.That(destination.InputName, Is.EqualTo("destination"));
            Assert.That(position.InputName, Is.EqualTo("position"));
        });
    }

    [Test]
    public void EndOfEncodedDataUsesTheDecodeFailureTaxonomy()
    {
        BitReader reader = new BitReader(new byte[] { 0x80 }, 1);
        Assert.That(reader.ReadBit(), Is.True);

        QrDecodeException bitEnd = Assert.Throws<QrDecodeException>(() => reader.ReadBit())!;
        reader.SeekBitPosition(0);
        QrDecodeException rangeEnd = Assert.Throws<QrDecodeException>(() => reader.ReadBits(2))!;
        QrDecodeException skipEnd = Assert.Throws<QrDecodeException>(() => reader.SkipBits(2))!;

        Assert.Multiple(() =>
        {
            Assert.That(bitEnd.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(rangeEnd.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(skipEnd.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        });
    }
}
