using webp;

namespace webp.Tests;

[TestFixture]
public class BitPrimitiveTests
{
    [Test]
    public void LsbFirstReader_ReadsUnalignedBitsAcrossBytes()
    {
        byte[] data = new byte[] { 0xAC, 0x03 };
        WebpBitReader reader = WebpBitReader.FromBytes(data, WebpBitOrder.LeastSignificantBitFirst);

        Assert.That(reader.ReadBits(4), Is.EqualTo(0xCU));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xAU));
        Assert.That(reader.ReadBits(2), Is.EqualTo(0x3U));
        Assert.That(reader.BitPosition, Is.EqualTo(10));
        Assert.That(reader.BitsRemaining(), Is.EqualTo(6));
    }

    [Test]
    public void MsbFirstReader_ReadsUnalignedBitsAcrossBytes()
    {
        byte[] data = new byte[] { 0xAC, 0xC0 };
        WebpBitReader reader = WebpBitReader.FromBytes(data, WebpBitOrder.MostSignificantBitFirst);

        Assert.That(reader.ReadBits(4), Is.EqualTo(0xAU));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xCU));
        Assert.That(reader.ReadBits(2), Is.EqualTo(0x3U));
        Assert.That(reader.BitPosition, Is.EqualTo(10));
        Assert.That(reader.BitsRemaining(), Is.EqualTo(6));
    }

    [Test]
    public void Reader_AlignsToNextByteBoundary()
    {
        byte[] data = new byte[] { 0xE1, 0x5A };
        WebpBitReader reader = WebpBitReader.FromBytes(data, WebpBitOrder.MostSignificantBitFirst);

        Assert.That(reader.ReadBits(3), Is.EqualTo(0x7U));
        Assert.That(reader.IsByteAligned(), Is.False);

        reader.AlignToByte();

        Assert.That(reader.IsByteAligned(), Is.True);
        Assert.That(reader.BytePosition(), Is.EqualTo(1));
        Assert.That(reader.ReadBits(8), Is.EqualTo(0x5AU));
    }

    [Test]
    public void Reader_CopiesRequestedConstructorSlice()
    {
        byte[] data = new byte[] { 0x00, 0xAC, 0x03, 0xFF };
        WebpBitReader reader = new WebpBitReader(data, 1, 2, WebpBitOrder.LeastSignificantBitFirst);

        data[1] = 0;

        Assert.That(reader.ReadBits(4), Is.EqualTo(0xCU));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xAU));
    }

    [Test]
    public void LsbFirstWriter_WritesUnalignedBitsAcrossBytes()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        writer.WriteBits(0xCU, 4);
        writer.WriteBits(0xAU, 4);
        writer.WriteBits(0x3U, 2);
        writer.AlignToByte();

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xAC, 0x03 }));
        Assert.That(writer.BitPosition, Is.EqualTo(16));
        Assert.That(writer.ByteLength(), Is.EqualTo(2));
    }

    [Test]
    public void MsbFirstWriter_WritesUnalignedBitsAcrossBytes()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.MostSignificantBitFirst);

        writer.WriteBits(0xAU, 4);
        writer.WriteBits(0xCU, 4);
        writer.WriteBits(0x3U, 2);
        writer.AlignToByte();

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xAC, 0xC0 }));
        Assert.That(writer.BitPosition, Is.EqualTo(16));
        Assert.That(writer.ByteLength(), Is.EqualTo(2));
    }

    [Test]
    public void Writer_ZeroBitWriteDoesNotChangePositionOrOutput()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        writer.WriteBits(0xFFFFFFFFU, 0);

        Assert.That(writer.BitPosition, Is.EqualTo(0));
        Assert.That(writer.ByteLength(), Is.EqualTo(0));
        Assert.That(writer.ToArray(), Is.Empty);
    }

    [Test]
    public void Reader_RejectsInvalidOrUnavailableBitCounts()
    {
        byte[] data = new byte[] { 0xFF };
        WebpBitReader reader = WebpBitReader.FromBytes(data, WebpBitOrder.LeastSignificantBitFirst);

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBits(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBits(33));
        Assert.Throws<ArgumentException>(() => reader.ReadBits(9));
    }

    [Test]
    public void Writer_RejectsInvalidBitCountsAndBitValues()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.MostSignificantBitFirst);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBits(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBits(0, 33));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBit(2));
    }

    [Test]
    public void WriterOutput_RoundTripsThroughReader()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        writer.WriteBits(0x15U, 5);
        writer.WriteBits(0x2U, 2);
        writer.WriteBits(0x1FFU, 9);

        WebpBitReader reader = WebpBitReader.FromBytes(writer.ToArray(), WebpBitOrder.LeastSignificantBitFirst);

        Assert.That(reader.ReadBits(5), Is.EqualTo(0x15U));
        Assert.That(reader.ReadBits(2), Is.EqualTo(0x2U));
        Assert.That(reader.ReadBits(9), Is.EqualTo(0x1FFU));
        Assert.That(reader.IsByteAligned(), Is.True);
    }
}
