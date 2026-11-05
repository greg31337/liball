using System.Buffers.Binary;
using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2CompressionTests
{
    [Test]
    public void BlaMkaMatchesIndependentWraparoundFixtures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Compression.BlaMka(0UL, 0UL), Is.EqualTo(0UL));
            Assert.That(Argon2Compression.BlaMka(1UL, 2UL), Is.EqualTo(7UL));
            Assert.That(
                Argon2Compression.BlaMka(ulong.MaxValue, 1UL),
                Is.EqualTo(0x00000001FFFFFFFEUL));
            Assert.That(
                Argon2Compression.BlaMka(ulong.MaxValue, ulong.MaxValue),
                Is.EqualTo(0xFFFFFFFC00000000UL));
            Assert.That(
                Argon2Compression.BlaMka(0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL),
                Is.EqualTo(0x7F44F06FCAC319DFUL));
        });
    }

    [Test]
    public void BlockCompressionMatchesIndependentFixture()
    {
        ulong[] previous = CreatePreviousBlock();
        ulong[] reference = CreateReferenceBlock();
        ulong[] destination = new ulong[128];

        Argon2Compression.CompressBlock(previous, reference, destination);

        Assert.Multiple(() =>
        {
            Assert.That(ToHex(SHA256.HashData(ToLittleEndianBytes(destination))), Is.EqualTo(
                "4a01f8c8b3be3a2eca3885bb2e660495c0ec8668297c50764c23bd227dc86463"));
            Assert.That(destination[0], Is.EqualTo(0xC7BADAC8F638E88CUL));
            Assert.That(destination[1], Is.EqualTo(0xC7330F3B4F702D05UL));
            Assert.That(destination[126], Is.EqualTo(0x89F20D7141BC5325UL));
            Assert.That(destination[127], Is.EqualTo(0x3472EFE04860CF3BUL));
        });
    }

    [Test]
    public void ExistingBlockXorCompressionMatchesIndependentFixture()
    {
        ulong[] previous = CreatePreviousBlock();
        ulong[] reference = CreateReferenceBlock();
        ulong[] existing = CreateExistingBlock();
        ulong[] destination = new ulong[128];

        Argon2Compression.CompressBlockWithXor(previous, reference, existing, destination);

        Assert.Multiple(() =>
        {
            Assert.That(ToHex(SHA256.HashData(ToLittleEndianBytes(destination))), Is.EqualTo(
                "8f9c708eb8b44faf0d26225b478e1d8f9f86108841d91705ceb147c0a0a6dc10"));
            Assert.That(destination[0], Is.EqualTo(0x621F7F6D539D4D29UL));
            Assert.That(destination[1], Is.EqualTo(0x6296AA9EEAD588A1UL));
            Assert.That(destination[126], Is.EqualTo(0x2C57A8D4E419F6FEUL));
            Assert.That(destination[127], Is.EqualTo(0x91D74A45EDC56AE1UL));
        });
    }

    [Test]
    public void ExistingBlockPathXorsTheOverwriteResult()
    {
        ulong[] previous = CreatePreviousBlock();
        ulong[] reference = CreateReferenceBlock();
        ulong[] existing = CreateExistingBlock();
        ulong[] overwrite = new ulong[128];
        ulong[] withXor = new ulong[128];

        Argon2Compression.CompressBlock(previous, reference, overwrite);
        Argon2Compression.CompressBlockWithXor(previous, reference, existing, withXor);

        int index = 0;
        while (index < overwrite.Length)
        {
            Assert.That(withXor[index], Is.EqualTo(overwrite[index] ^ existing[index]));
            index = index + 1;
        }
    }

    [Test]
    public void CompressionPreservesInputsAndSupportsDestinationAliasing()
    {
        ulong[] previous = CreatePreviousBlock();
        ulong[] reference = CreateReferenceBlock();
        ulong[] originalPrevious = (ulong[])previous.Clone();
        ulong[] originalReference = (ulong[])reference.Clone();
        ulong[] expected = new ulong[128];

        Argon2Compression.CompressBlock(previous, reference, expected);
        Argon2Compression.CompressBlock(previous, reference, previous);

        Assert.Multiple(() =>
        {
            Assert.That(previous, Is.EqualTo(expected));
            Assert.That(reference, Is.EqualTo(originalReference));
            Assert.That(originalPrevious, Is.Not.EqualTo(expected));
        });
    }

    [Test]
    public void BlockXorSupportsAliasedDestination()
    {
        ulong[] left = CreatePreviousBlock();
        ulong[] right = CreateReferenceBlock();
        ulong[] expected = new ulong[128];
        int index = 0;
        while (index < expected.Length)
        {
            expected[index] = left[index] ^ right[index];
            index = index + 1;
        }

        Argon2Compression.XorBlocks(left, right, left);

        Assert.That(left, Is.EqualTo(expected));
    }

    [Test]
    public void CompressionRejectsIncorrectBlockLengths()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Compression.CompressBlock(new ulong[127], new ulong[128], new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Compression.CompressBlock(new ulong[128], new ulong[128], new ulong[129]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Compression.PermuteBlock(new ulong[127]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Compression.XorBlocks(new ulong[128], new ulong[127], new ulong[128]));
        });
    }

    static ulong[] CreatePreviousBlock()
    {
        ulong[] block = new ulong[128];
        block[0] = 0x0102030405060708UL;
        int index = 1;
        while (index < block.Length)
        {
            block[index] = ModularArithmetic.Add64(block[index - 1], 0x0102030405060708UL);
            index = index + 1;
        }

        return block;
    }

    static ulong[] CreateReferenceBlock()
    {
        ulong[] block = new ulong[128];
        block[0] = 0xFEDCBA9876543210UL;
        int index = 1;
        while (index < block.Length)
        {
            block[index] = ModularArithmetic.Add64(block[index - 1], 0x1111111111111111UL);
            index = index + 1;
        }

        return block;
    }

    static ulong[] CreateExistingBlock()
    {
        ulong[] block = new ulong[128];
        int index = 0;
        while (index < block.Length)
        {
            block[index] = 0xA5A5A5A5A5A5A5A5UL ^ (ulong)index;
            index = index + 1;
        }

        return block;
    }

    static byte[] ToLittleEndianBytes(ulong[] words)
    {
        byte[] bytes = new byte[1024];
        int index = 0;
        while (index < words.Length)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(index * 8, 8), words[index]);
            index = index + 1;
        }

        return bytes;
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
