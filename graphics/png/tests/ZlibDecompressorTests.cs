using System.IO.Compression;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ZlibDecompressorTests
{
    [Test]
    public void Decompress_ArrayReturnsOriginalBytes()
    {
        byte[] original = new byte[] { 0, 1, 2, 3, 4, 250, 251, 252, 253, 254, 255 };
        byte[] compressed = Compress(original);

        byte[] result = PngZlibDecompressor.Decompress(compressed);

        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void Decompress_ArrayReadsOffsetAndLength()
    {
        byte[] original = CreateRepeatingBytes(128);
        byte[] compressed = Compress(original);
        byte[] wrapped = new byte[compressed.Length + 4];
        wrapped[0] = 99;
        wrapped[1] = 88;
        Array.Copy(compressed, 0, wrapped, 2, compressed.Length);
        wrapped[wrapped.Length - 2] = 77;
        wrapped[wrapped.Length - 1] = 66;

        byte[] result = PngZlibDecompressor.Decompress(wrapped, 2, compressed.Length);

        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void Decompress_StreamReturnsOriginalBytesAndLeavesStreamOpen()
    {
        byte[] original = CreateRepeatingBytes(32);
        byte[] compressed = Compress(original);
        MemoryStream stream = new MemoryStream(compressed);

        byte[] result = PngZlibDecompressor.Decompress(stream);

        Assert.That(result, Is.EqualTo(original));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void Decompress_HandlesEmptyPayload()
    {
        byte[] compressed = Compress(new byte[0]);

        byte[] result = PngZlibDecompressor.Decompress(compressed);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Decompress_EnforcesDecompressedByteLimit()
    {
        byte[] original = CreateRepeatingBytes(16);
        byte[] compressed = Compress(original);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 15;

        Assert.Throws<OverflowException>(() => PngZlibDecompressor.Decompress(compressed, limits));
    }

    [Test]
    public void Decompress_AllowsUnlimitedDecompressedBytesWhenLimitIsZero()
    {
        byte[] original = CreateRepeatingBytes(16);
        byte[] compressed = Compress(original);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 0;

        byte[] result = PngZlibDecompressor.Decompress(compressed, limits);

        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void Decompress_RejectsInvalidZlibData()
    {
        byte[] invalid = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<InvalidDataException>(() => PngZlibDecompressor.Decompress(invalid));
    }

    [Test]
    public void Decompress_RejectsInvalidInputs()
    {
        byte[] compressed = Compress(new byte[] { 1 });

        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress(compressed, null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress(compressed, 0, compressed.Length, null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibDecompressor.Decompress(new MemoryStream(compressed), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibDecompressor.Decompress(compressed, -1, compressed.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibDecompressor.Decompress(compressed, compressed.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibDecompressor.Decompress(compressed, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibDecompressor.Decompress(compressed, 1, compressed.Length));
    }

    static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        ZLibStream zlibStream = new ZLibStream(output, CompressionLevel.SmallestSize, true);
        zlibStream.Write(data, 0, data.Length);
        zlibStream.Dispose();
        byte[] result = output.ToArray();
        output.Dispose();
        return result;
    }

    static byte[] CreateRepeatingBytes(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(index % 251);
            index += 1;
        }

        return data;
    }
}
