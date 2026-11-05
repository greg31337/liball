using System.Buffers.Binary;
using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2InitialBlockTests
{
    [Test]
    public void LaneZeroBlocksMatchIndependentChecksums()
    {
        byte[] initialHash = CreateRfcInitialHash();
        byte[] firstBlock = new byte[1024];
        byte[] secondBlock = new byte[1024];

        Argon2InitialHash.ExpandFirstBlocksForLane(initialHash, 0, firstBlock, secondBlock);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(SHA256.HashData(firstBlock)),
                Is.EqualTo("c0f566f42852da99f282d68a66393d78932dad108a47bbaa32a419823a642ad4"));
            Assert.That(
                ToHex(SHA256.HashData(secondBlock)),
                Is.EqualTo("b352a688d76bdcdcf5d4ca27b8ee767edc9a9257aa92902f0ed4e4a23472aa81"));
        });
    }

    [Test]
    public void LaneZeroFirstBlockMatchesRfcPublishedWords()
    {
        byte[] firstBlock = new byte[1024];
        byte[] secondBlock = new byte[1024];

        Argon2InitialHash.ExpandFirstBlocksForLane(
            CreateRfcInitialHash(),
            0,
            firstBlock,
            secondBlock);

        Assert.Multiple(() =>
        {
            Assert.That(BinaryPrimitives.ReadUInt64LittleEndian(firstBlock), Is.EqualTo(0x6B2E09F10671BD43UL));
            Assert.That(
                BinaryPrimitives.ReadUInt64LittleEndian(firstBlock.AsSpan(8)),
                Is.EqualTo(0xF69F5C27918A21BEUL));
            Assert.That(
                BinaryPrimitives.ReadUInt64LittleEndian(firstBlock.AsSpan(16)),
                Is.EqualTo(0xDEA7810EA41290E1UL));
            Assert.That(
                BinaryPrimitives.ReadUInt64LittleEndian(firstBlock.AsSpan(24)),
                Is.EqualTo(0x6787F7171870F893UL));
        });
    }

    [Test]
    public void LaneSuffixChangesBothInitialBlocks()
    {
        byte[] initialHash = CreateRfcInitialHash();
        byte[] laneThreeFirst = new byte[1024];
        byte[] laneThreeSecond = new byte[1024];

        Argon2InitialHash.ExpandFirstBlocksForLane(
            initialHash,
            3,
            laneThreeFirst,
            laneThreeSecond);

        Assert.Multiple(() =>
        {
            Assert.That(
                ToHex(SHA256.HashData(laneThreeFirst)),
                Is.EqualTo("8471f37ea700623728e1175850e423ed4a0b9cdf7bc0bc0a12a003b7be10094e"));
            Assert.That(
                ToHex(SHA256.HashData(laneThreeSecond)),
                Is.EqualTo("c6670d8775d1365b9b1bf2bb802acaa37c122815ba851e7e758be7b0eb6cecb2"));
        });
    }

    [Test]
    public void InitialHashIsNotModified()
    {
        byte[] initialHash = CreateRfcInitialHash();
        byte[] original = (byte[])initialHash.Clone();

        Argon2InitialHash.ExpandFirstBlocksForLane(
            initialHash,
            1,
            new byte[1024],
            new byte[1024]);

        Assert.That(initialHash, Is.EqualTo(original));
    }

    [Test]
    public void LaneAndDestinationBoundariesAreValidated()
    {
        byte[] initialHash = CreateRfcInitialHash();

        Assert.DoesNotThrow(
            () => Argon2InitialHash.ExpandFirstBlocksForLane(
                initialHash,
                Argon2Limits.MaximumDegreeOfParallelism - 1,
                new byte[1024],
                new byte[1024]));
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2InitialHash.ExpandFirstBlocksForLane(
                    initialHash,
                    -1,
                    new byte[1024],
                    new byte[1024]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2InitialHash.ExpandFirstBlocksForLane(
                    initialHash,
                    Argon2Limits.MaximumDegreeOfParallelism,
                    new byte[1024],
                    new byte[1024]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2InitialHash.ExpandFirstBlocksForLane(
                    initialHash,
                    0,
                    new byte[1023],
                    new byte[1024]));
        });
    }

    [Test]
    public void InitialBlockDestinationsMustNotOverlap()
    {
        byte[] destination = new byte[1024];

        Assert.Throws<Argon2ParameterException>(
            () => Argon2InitialHash.ExpandFirstBlocksForLane(
                CreateRfcInitialHash(),
                0,
                destination,
                destination));
    }

    static byte[] CreateRfcInitialHash()
    {
        return Convert.FromHexString(
            "2889de487eb42ae500c0007ed9252f1069eadec40d5765b485de6dc2437a67b8" +
            "546a2f0acc1a0882db8fcf74714b472e94df421a5da1112ffa11434370a1e997");
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
