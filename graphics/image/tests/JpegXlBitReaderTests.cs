using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class JpegXlBitReaderTests
{
    [Test]
    public void ReadBits_DecodesLeastSignificantBitFirstAcrossBytes()
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xca, 0x71 }, 0);

        uint first = reader.ReadBits(4);
        uint crossing = reader.ReadBits(8);
        uint last = reader.ReadBits(4);

        Assert.That(first, Is.EqualTo(0x0a));
        Assert.That(crossing, Is.EqualTo(0x1c));
        Assert.That(last, Is.EqualTo(0x07));
        Assert.That(reader.BitPosition, Is.EqualTo(16));
        Assert.That(reader.Valid, Is.True);
    }

    [Test]
    public void Constructor_StartsAtRequestedBitOffset()
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xf0, 0x0f }, 6);

        uint value = reader.ReadBits(6);

        Assert.That(value, Is.EqualTo(0x3f));
        Assert.That(reader.BitPosition, Is.EqualTo(12));
        Assert.That(reader.Valid, Is.True);
    }

    [Test]
    public void ReadBits_AllowsZeroBitsAtEndOfBuffer()
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xff }, 8);

        uint value = reader.ReadBits(0);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.BitPosition, Is.EqualTo(8));
        Assert.That(reader.Valid, Is.True);
    }

    [TestCase(-1)]
    [TestCase(33)]
    public void ReadBits_RejectsUnsupportedWidths(int count)
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xff }, 0);

        uint value = reader.ReadBits(count);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.BitPosition, Is.EqualTo(0));
        Assert.That(reader.Valid, Is.False);
    }

    [Test]
    public void ReadBits_WhenTruncated_InvalidatesWithoutAdvancing()
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xff }, 4);

        uint value = reader.ReadBits(5);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.BitPosition, Is.EqualTo(4));
        Assert.That(reader.Valid, Is.False);
    }

    [TestCase(-1)]
    [TestCase(9)]
    public void Constructor_RejectsOutOfRangeBitPosition(int bitPosition)
    {
        JpegXlBitReader reader = new JpegXlBitReader(new byte[] { 0xff }, bitPosition);

        uint value = reader.ReadBits(1);

        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.BitPosition, Is.EqualTo(bitPosition));
        Assert.That(reader.Valid, Is.False);
    }

    [Test]
    public void NullInput_AllowsOnlyEmptyReadAtZeroPosition()
    {
        JpegXlBitReader reader = new JpegXlBitReader(null, 0);

        uint empty = reader.ReadBits(0);
        uint value = reader.ReadBits(1);

        Assert.That(empty, Is.EqualTo(0));
        Assert.That(value, Is.EqualTo(0));
        Assert.That(reader.BitPosition, Is.EqualTo(0));
        Assert.That(reader.Valid, Is.False);
    }
}
