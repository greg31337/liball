using System.IO.Compression;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ZlibCompressorTests
{
    [Test]
    public void Compress_RoundTripsThroughZlibDecompressor()
    {
        byte[] original = CreateRepeatingBytes(512);

        byte[] compressed = PngZlibCompressor.Compress(original);
        byte[] decompressed = PngZlibDecompressor.Decompress(compressed);

        Assert.That(compressed, Is.Not.Empty);
        Assert.That(decompressed, Is.EqualTo(original));
    }

    [Test]
    public void Compress_HandlesEmptyInput()
    {
        byte[] compressed = PngZlibCompressor.Compress(new byte[0]);

        byte[] decompressed = PngZlibDecompressor.Decompress(compressed);

        Assert.That(decompressed, Is.Empty);
    }

    [Test]
    public void Compress_ArrayReadsOffsetAndLength()
    {
        byte[] original = CreateRepeatingBytes(64);
        byte[] wrapped = new byte[original.Length + 4];
        wrapped[0] = 99;
        wrapped[1] = 88;
        Array.Copy(original, 0, wrapped, 2, original.Length);
        wrapped[wrapped.Length - 2] = 77;
        wrapped[wrapped.Length - 1] = 66;

        byte[] compressed = PngZlibCompressor.Compress(wrapped, 2, original.Length);
        byte[] decompressed = PngZlibDecompressor.Decompress(compressed);

        Assert.That(decompressed, Is.EqualTo(original));
    }

    [Test]
    public void Compress_UsesRequestedCompressionLevel()
    {
        byte[] original = CreateRepeatingBytes(128);

        byte[] compressed = PngZlibCompressor.Compress(original, CompressionLevel.NoCompression);
        byte[] decompressed = PngZlibDecompressor.Decompress(compressed);

        Assert.That(decompressed, Is.EqualTo(original));
    }

    [Test]
    public void Compress_EnforcesInputDecompressedByteLimit()
    {
        byte[] original = CreateRepeatingBytes(16);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 15;

        Assert.Throws<OverflowException>(() => PngZlibCompressor.Compress(original, limits));
    }

    [Test]
    public void Compress_EnforcesCompressedIdatLimit()
    {
        byte[] original = CreateRepeatingBytes(16);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = 1;

        Assert.Throws<OverflowException>(() => PngZlibCompressor.Compress(original, limits));
    }

    [Test]
    public void Compress_AllowsUnlimitedLimitsWhenZero()
    {
        byte[] original = CreateRepeatingBytes(16);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxDecompressedBytes = 0;
        limits.MaxIdatBytes = 0;

        byte[] compressed = PngZlibCompressor.Compress(original, limits);
        byte[] decompressed = PngZlibDecompressor.Decompress(compressed);

        Assert.That(decompressed, Is.EqualTo(original));
    }

    [Test]
    public void Compress_RejectsInvalidInputs()
    {
        byte[] data = new byte[] { 1, 2, 3 };

        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress(data, (PngSecurityLimits)null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress(null!, CompressionLevel.SmallestSize));
        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress(data, 0, data.Length, null!));
        Assert.Throws<ArgumentNullException>(() => PngZlibCompressor.Compress(data, 0, data.Length, CompressionLevel.SmallestSize, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibCompressor.Compress(data, -1, data.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibCompressor.Compress(data, data.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibCompressor.Compress(data, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngZlibCompressor.Compress(data, 1, data.Length));
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
