using webp;

namespace webp.Tests;

[TestFixture]
public class BitReaderTests
{
    [Test]
    public void ReadBits_LeastSignificantBitFirstTracksPositionAndRemainingBits()
    {
        WebpBitReader reader = WebpBitReader.FromBytes(new byte[] { 0b10110010 }, WebpBitOrder.LeastSignificantBitFirst);

        uint first = reader.ReadBits(3);
        uint second = reader.ReadBits(5);

        Assert.That(first, Is.EqualTo(0b010U));
        Assert.That(second, Is.EqualTo(0b10110U));
        Assert.That(reader.BitPosition, Is.EqualTo(8));
        Assert.That(reader.BitsRemaining(), Is.EqualTo(0));
        Assert.That(reader.BytePosition(), Is.EqualTo(1));
        Assert.That(reader.IsByteAligned(), Is.True);
    }

    [Test]
    public void ReadBits_MostSignificantBitFirstReadsNaturalBitOrder()
    {
        WebpBitReader reader = WebpBitReader.FromBytes(new byte[] { 0b10110010 }, WebpBitOrder.MostSignificantBitFirst);

        uint first = reader.ReadBits(4);
        int next = reader.ReadBit();
        uint rest = reader.ReadBits(3);

        Assert.That(first, Is.EqualTo(0b1011U));
        Assert.That(next, Is.EqualTo(0));
        Assert.That(rest, Is.EqualTo(0b010U));
        Assert.That(reader.IsByteAligned(), Is.True);
    }

    [Test]
    public void FromBytes_CopiesInput()
    {
        byte[] data = new byte[] { 0x0F };
        WebpBitReader reader = WebpBitReader.FromBytes(data, WebpBitOrder.LeastSignificantBitFirst);
        data[0] = 0;

        Assert.That(reader.ReadBits(8), Is.EqualTo(0x0FU));
    }

    [Test]
    public void Constructor_CopiesRequestedSlice()
    {
        byte[] data = new byte[] { 0xFF, 0x0F, 0xAA };
        WebpBitReader reader = new WebpBitReader(data, 1, 1, WebpBitOrder.LeastSignificantBitFirst);
        data[1] = 0;

        uint lowNibble = reader.ReadBits(4);
        uint highNibble = reader.ReadBits(4);

        Assert.That(lowNibble, Is.EqualTo(0xFU));
        Assert.That(highNibble, Is.EqualTo(0U));
    }

    [Test]
    public void AlignToByte_SkipsRemainingBitsInCurrentByte()
    {
        WebpBitReader reader = WebpBitReader.FromBytes(new byte[] { 0xFF, 0x12 }, WebpBitOrder.LeastSignificantBitFirst);

        reader.ReadBits(3);
        reader.AlignToByte();

        Assert.That(reader.BitPosition, Is.EqualTo(8));
        Assert.That(reader.BytePosition(), Is.EqualTo(1));
        Assert.That(reader.ReadBits(8), Is.EqualTo(0x12U));
    }

    [Test]
    public void Reader_RejectsInvalidInputsAndOverreads()
    {
        WebpBitReader reader = WebpBitReader.FromBytes(new byte[] { 0 }, WebpBitOrder.LeastSignificantBitFirst);

        Assert.Throws<ArgumentNullException>(() => WebpBitReader.FromBytes(null!, WebpBitOrder.LeastSignificantBitFirst));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpBitReader(new byte[] { 1 }, -1, 1, WebpBitOrder.LeastSignificantBitFirst));
        Assert.Throws<ArgumentException>(() => new WebpBitReader(new byte[] { 1 }, 0, 2, WebpBitOrder.LeastSignificantBitFirst));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.HasBits(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBits(33));
        Assert.Throws<ArgumentException>(() => reader.ReadBits(9));
    }
}
