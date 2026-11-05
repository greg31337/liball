using webp;

namespace webp.Tests;

[TestFixture]
public class BitWriterTests
{
    [Test]
    public void WriteBits_LeastSignificantBitFirstPacksBitsAndAligns()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        writer.WriteBits(0b101, 3);
        writer.WriteBit(1);
        writer.AlignToByte();

        Assert.That(writer.BitPosition, Is.EqualTo(8));
        Assert.That(writer.ByteLength(), Is.EqualTo(1));
        Assert.That(writer.IsByteAligned(), Is.True);
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x0D }));
    }

    [Test]
    public void WriteBits_MostSignificantBitFirstPacksBitsAndAligns()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.MostSignificantBitFirst);

        writer.WriteBits(0b101, 3);
        writer.WriteBit(1);
        writer.AlignToByte();

        Assert.That(writer.BitPosition, Is.EqualTo(8));
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xB0 }));
    }

    [Test]
    public void WriteBits_RoundTripsThroughBitReaderForBothOrders()
    {
        AssertRoundTrip(WebpBitOrder.LeastSignificantBitFirst, 0xB3U, 8);
        AssertRoundTrip(WebpBitOrder.MostSignificantBitFirst, 0xB3U, 8);
        AssertRoundTrip(WebpBitOrder.LeastSignificantBitFirst, 0x15U, 5);
        AssertRoundTrip(WebpBitOrder.MostSignificantBitFirst, 0x15U, 5);
    }

    [Test]
    public void Writer_RejectsInvalidBitsAndBitCounts()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBit(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBits(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBits(0, 33));
    }

    static void AssertRoundTrip(WebpBitOrder order, uint value, int bitCount)
    {
        WebpBitWriter writer = new WebpBitWriter(order);
        writer.WriteBits(value, bitCount);

        WebpBitReader reader = WebpBitReader.FromBytes(writer.ToArray(), order);
        uint decoded = reader.ReadBits(bitCount);

        Assert.That(decoded, Is.EqualTo(value));
    }
}
