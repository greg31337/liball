using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class CrcTests
{
    [Test]
    public void CreateTable_ReturnsKnownPngCrcTableEntries()
    {
        uint[] table = PngCrc32.CreateTable();

        Assert.That(table, Has.Length.EqualTo(256));
        Assert.That(table[0], Is.EqualTo(0x00000000u));
        Assert.That(table[1], Is.EqualTo(0x77073096u));
        Assert.That(table[2], Is.EqualTo(0xEE0E612Cu));
        Assert.That(table[3], Is.EqualTo(0x990951BAu));
        Assert.That(table[255], Is.EqualTo(0x2D02EF8Du));
    }

    [Test]
    public void Calculate_ReturnsKnownCrc32Vectors()
    {
        byte[] empty = new byte[0];
        byte[] digits = new byte[] { 49, 50, 51, 52, 53, 54, 55, 56, 57 };
        byte[] sentence = new byte[]
        {
            84, 104, 101, 32, 113, 117, 105, 99, 107, 32,
            98, 114, 111, 119, 110, 32, 102, 111, 120, 32,
            106, 117, 109, 112, 115, 32, 111, 118, 101, 114,
            32, 116, 104, 101, 32, 108, 97, 122, 121, 32,
            100, 111, 103
        };

        Assert.That(PngCrc32.Calculate(empty), Is.EqualTo(0x00000000u));
        Assert.That(PngCrc32.Calculate(digits), Is.EqualTo(0xCBF43926u));
        Assert.That(PngCrc32.Calculate(sentence), Is.EqualTo(0x414FA339u));
    }

    [Test]
    public void Calculate_SupportsOffsetAndCount()
    {
        byte[] data = new byte[] { 0xAA, 49, 50, 51, 52, 53, 54, 55, 56, 57, 0xBB };

        Assert.That(PngCrc32.Calculate(data, 1, 9), Is.EqualTo(0xCBF43926u));
    }

    [Test]
    public void CalculateChunkCrc_UsesChunkTypeThenData()
    {
        byte[] noData = new byte[0];
        byte[] ihdrData = new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x08,
            0x02,
            0x00,
            0x00,
            0x00
        };

        Assert.That(PngCrc32.CalculateChunkCrc(PngConstants.ChunkIEND, noData), Is.EqualTo(0xAE426082u));
        Assert.That(PngCrc32.CalculateChunkCrc(PngConstants.ChunkIHDR, ihdrData), Is.EqualTo(0x907753DEu));
    }

    [Test]
    public void CalculateChunkCrc_ByteTypeMatchesStringType()
    {
        byte[] chunkType = new byte[] { 73, 69, 78, 68 };
        byte[] data = new byte[0];

        Assert.That(PngCrc32.CalculateChunkCrc(chunkType, data), Is.EqualTo(PngCrc32.CalculateChunkCrc("IEND", data)));
    }

    [Test]
    public void Calculate_RejectsInvalidInputs()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentNullException>(() => PngCrc32.Calculate(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCrc32.Calculate(data, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCrc32.Calculate(data, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngCrc32.Calculate(data, 2, 2));
    }

    [Test]
    public void CalculateChunkCrc_RejectsInvalidInputs()
    {
        byte[] data = new byte[0];
        byte[] chunkType = new byte[] { 73, 69, 78, 68 };

        Assert.Throws<ArgumentNullException>(() => PngCrc32.CalculateChunkCrc((string)null!, data));
        Assert.Throws<ArgumentNullException>(() => PngCrc32.CalculateChunkCrc("IEND", null!));
        Assert.Throws<ArgumentNullException>(() => PngCrc32.CalculateChunkCrc((byte[])null!, data));
        Assert.Throws<ArgumentNullException>(() => PngCrc32.CalculateChunkCrc(chunkType, null!));
        Assert.Throws<ArgumentException>(() => PngCrc32.CalculateChunkCrc("ABC", data));
        Assert.Throws<ArgumentException>(() => PngCrc32.CalculateChunkCrc("ABCDE", data));
        Assert.Throws<ArgumentException>(() => PngCrc32.CalculateChunkCrc("IH\u0100R", data));
        Assert.Throws<ArgumentException>(() => PngCrc32.CalculateChunkCrc(new byte[] { 1, 2, 3 }, data));
    }
}
