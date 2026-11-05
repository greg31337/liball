using System.Buffers.Binary;
using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2LaneFillerTests
{
    [Test]
    public void FillSegmentUsesNormalizedMemoryAndStopsAtSynchronizationBoundary()
    {
        Argon2Parameters parameters = CreateParameters(Argon2Variant.D, Argon2Version.Version13);
        Argon2Memory memory = CreateInitializedMemory(parameters);
        ulong[] originalFirst = memory.GetLaneBlock(0, 0L).ToArray();
        ulong[] originalSecond = memory.GetLaneBlock(0, 1L).ToArray();
        ulong[] originalNextSlice = memory.GetLaneBlock(0, 8L).ToArray();
        ulong[] originalOtherLane = memory.GetLaneBlock(1, 2L).ToArray();

        FillSegment(parameters, memory, 0L, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlockCount(), Is.EqualTo(64L));
            Assert.That(memory.GetBlocksPerLane(), Is.EqualTo(32L));
            Assert.That(memory.GetSegmentLength(), Is.EqualTo(8L));
            Assert.That(memory.GetLaneBlock(0, 0L).ToArray(), Is.EqualTo(originalFirst));
            Assert.That(memory.GetLaneBlock(0, 1L).ToArray(), Is.EqualTo(originalSecond));
            Assert.That(memory.GetLaneBlock(0, 8L).ToArray(), Is.EqualTo(originalNextSlice));
            Assert.That(memory.GetLaneBlock(1, 2L).ToArray(), Is.EqualTo(originalOtherLane));
            Assert.That(GetSegmentDigest(memory, 0, 0), Is.EqualTo(
                "03001be44e77cbcb5457e6df6e8b777cb98064838a3ee00a8a7bbc6726980076"));
            Assert.That(memory.GetLaneBlock(0, 2L)[0], Is.EqualTo(0x75CCAB8FF9D0EF70UL));
            Assert.That(memory.GetLaneBlock(0, 7L)[127], Is.EqualTo(0x81E9A64C0D78A7DBUL));
        });
    }

    [TestCase(
        Argon2Variant.I,
        "f06ab72bfa12714b4dbc0410793476b0b684e26c2d759dce552179e6d493fc68",
        0x8BC1ED7073D02290UL)]
    [TestCase(
        Argon2Variant.Id,
        "a35e099b1ff0346fff081e306c5e16872a2b0fa56005a9fcff2350802fcf03dc",
        0xBF3F4F7D324D6E7CUL)]
    public void IndependentAddressingVariantsMatchSegmentFixtures(
        Argon2Variant variant,
        string expectedDigest,
        ulong expectedLastWord)
    {
        Argon2Parameters parameters = CreateParameters(variant, Argon2Version.Version13);
        Argon2Memory memory = CreateInitializedMemory(parameters);

        FillSegment(parameters, memory, 0L, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(GetSegmentDigest(memory, 0, 0), Is.EqualTo(expectedDigest));
            Assert.That(memory.GetLaneBlock(0, 7L)[127], Is.EqualTo(expectedLastWord));
        });
    }

    [Test]
    public void LaterPassWrapsPreviousBlockAndUsesVersionSpecificUpdateRule()
    {
        Argon2Parameters version10Parameters = CreateParameters(
            Argon2Variant.D,
            Argon2Version.Version10);
        Argon2Parameters version13Parameters = CreateParameters(
            Argon2Variant.D,
            Argon2Version.Version13);
        Argon2Memory version10Memory = CreateInitializedMemory(version10Parameters);
        Argon2Memory version13Memory = CreateInitializedMemory(version13Parameters);
        ulong[] originalFirstBlock = version13Memory.GetLaneBlock(0, 0L).ToArray();

        FillSegment(version10Parameters, version10Memory, 1L, 0, 0);
        FillSegment(version13Parameters, version13Memory, 1L, 0, 0);

        ulong[] version10FirstBlock = version10Memory.GetLaneBlock(0, 0L).ToArray();
        ulong[] version13FirstBlock = version13Memory.GetLaneBlock(0, 0L).ToArray();
        int wordIndex = 0;
        while (wordIndex < Argon2Limits.WordsPerBlock)
        {
            Assert.That(
                version13FirstBlock[wordIndex],
                Is.EqualTo(version10FirstBlock[wordIndex] ^ originalFirstBlock[wordIndex]));
            wordIndex = wordIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(GetSegmentDigest(version10Memory, 0, 0), Is.EqualTo(
                "5932c8946c551d2a613e690fd922e67cf5a9867471c0030581de247a887f1050"));
            Assert.That(GetSegmentDigest(version13Memory, 0, 0), Is.EqualTo(
                "1a79f4aff3f3b01c7a39c6334045ddc8f3d1af3c3e15c42e0cd03d632ebc4d09"));
            Assert.That(version10FirstBlock[0], Is.EqualTo(0x8DE2D2DAB36E4C3CUL));
            Assert.That(version13FirstBlock[0], Is.EqualTo(0x13D5A863CC2431F4UL));
        });
    }

    [Test]
    public void FillSegmentRejectsInvalidPositionBuffersAndMemoryGeometry()
    {
        Argon2Parameters parameters = CreateParameters(Argon2Variant.D, Argon2Version.Version13);
        Argon2Memory memory = CreateInitializedMemory(parameters);
        Argon2Parameters mismatchedParameters = CreateParameters(
            Argon2Variant.D,
            Argon2Version.Version13);
        mismatchedParameters.MemorySizeKiB = 80L;
        ulong[] block = new ulong[Argon2Limits.WordsPerBlock];

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(parameters, memory, 2L, 0, 0, block, new ulong[128], new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(parameters, memory, 0L, 2, 0, block, new ulong[128], new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(parameters, memory, 0L, 0, 4, block, new ulong[128], new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(parameters, memory, 0L, 0, 0, new ulong[127], new ulong[128], new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(parameters, memory, 0L, 0, 0, block, block, new ulong[128]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2LaneFiller.FillSegment(mismatchedParameters, memory, 0L, 0, 0, block, new ulong[128], new ulong[128]));
        });
    }

    static void FillSegment(
        Argon2Parameters parameters,
        Argon2Memory memory,
        long passIndex,
        int laneIndex,
        int sliceIndex)
    {
        Argon2LaneFiller.FillSegment(
            parameters,
            memory,
            passIndex,
            laneIndex,
            sliceIndex,
            new ulong[Argon2Limits.WordsPerBlock],
            new ulong[Argon2Limits.WordsPerBlock],
            new ulong[Argon2Limits.WordsPerBlock]);
    }

    static Argon2Parameters CreateParameters(
        Argon2Variant variant,
        Argon2Version version)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.Iterations = 2L;
        parameters.MemorySizeKiB = 71L;
        parameters.DegreeOfParallelism = 2;
        return parameters;
    }

    static Argon2Memory CreateInitializedMemory(Argon2Parameters parameters)
    {
        Argon2Memory memory = new Argon2Memory(parameters, 3);
        int laneIndex = 0;
        while (laneIndex < memory.GetLaneCount())
        {
            ulong laneValue = (ulong)(laneIndex + 1) * 0x100000001B3UL;
            ulong blockValue = laneValue;
            long blockIndex = 0L;
            while (blockIndex < memory.GetBlocksPerLane())
            {
                blockValue = ModularArithmetic.Add64(blockValue, 0x9E3779B97F4A7C15UL);
                ulong wordValue = blockValue;
                Span<ulong> block = memory.GetLaneBlock(laneIndex, blockIndex);
                int wordIndex = 0;
                while (wordIndex < block.Length)
                {
                    block[wordIndex] = wordValue;
                    wordValue = ModularArithmetic.Add64(wordValue, 0xD6E8FEB86659FD93UL);
                    wordIndex = wordIndex + 1;
                }

                blockIndex = blockIndex + 1L;
            }

            laneIndex = laneIndex + 1;
        }

        return memory;
    }

    static string GetSegmentDigest(Argon2Memory memory, int laneIndex, int sliceIndex)
    {
        int byteLength = (int)(memory.GetSegmentLength() * Argon2Limits.BlockSizeInBytes);
        byte[] bytes = new byte[byteLength];
        long firstBlock = (long)sliceIndex * memory.GetSegmentLength();
        long blockOffset = 0L;
        while (blockOffset < memory.GetSegmentLength())
        {
            Span<ulong> block = memory.GetLaneBlock(laneIndex, firstBlock + blockOffset);
            int wordIndex = 0;
            while (wordIndex < block.Length)
            {
                int byteOffset = (int)(blockOffset * Argon2Limits.BlockSizeInBytes)
                    + wordIndex * 8;
                BinaryPrimitives.WriteUInt64LittleEndian(
                    bytes.AsSpan(byteOffset, 8),
                    block[wordIndex]);
                wordIndex = wordIndex + 1;
            }

            blockOffset = blockOffset + 1L;
        }

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
