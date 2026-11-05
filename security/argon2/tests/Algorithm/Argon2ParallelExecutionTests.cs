using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2ParallelExecutionTests
{
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version10,
        "c03e6ea388e8b206ee787353eebf6225f4a487546952f0fca2a76efb0ab3d8ae")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version10,
        "a5742f9e3bf59936648fffc596029c224361c5efe42ccc704d4b2f7bf4ac0403")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version10,
        "4ed472a832960d0fd677459f7926f9b4bf72df7ec7e55436d578dd5f24e85fd1")]
    [TestCase(
        Argon2Variant.D,
        Argon2Version.Version13,
        "d8c54d6283ca2dc14842a8509d7c84b9189b76293560d7c4775b2d1f0d69a968")]
    [TestCase(
        Argon2Variant.I,
        Argon2Version.Version13,
        "bd7549197d330319954b40c5f4fa0ffe798ca071331cecb282ec202086850ca8")]
    [TestCase(
        Argon2Variant.Id,
        Argon2Version.Version13,
        "bb0cc80a3e671149526915418c6eefe761bb19d5d2d567a017703e0cea6ab05c")]
    public void EveryWorkerCountThroughLaneCountProducesPinnedOutput(
        Argon2Variant variant,
        Argon2Version version,
        string expectedTag)
    {
        byte[] password = Encoding.ASCII.GetBytes("password");
        byte[] salt = Encoding.ASCII.GetBytes("somesalt");
        Argon2Parameters parameters = CreateParameters(variant, version, 32L, 3L);
        IArgon2Primitive primitive = new Argon2Primitive();
        int maximumWorkerCount = 1;
        while (maximumWorkerCount <= parameters.DegreeOfParallelism)
        {
            Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
            executionOptions.MaximumWorkerCount = maximumWorkerCount;
            byte[] tag = new byte[32];

            primitive.ComputeHash(
                password,
                salt,
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                executionOptions,
                tag);

            Assert.That(
                Convert.ToHexString(tag).ToLowerInvariant(),
                Is.EqualTo(expectedTag),
                "maximum worker count " + maximumWorkerCount);
            maximumWorkerCount = maximumWorkerCount + 1;
        }
    }

    [TestCase(Argon2Variant.D, Argon2Version.Version10)]
    [TestCase(Argon2Variant.I, Argon2Version.Version10)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version10)]
    [TestCase(Argon2Variant.D, Argon2Version.Version13)]
    [TestCase(Argon2Variant.I, Argon2Version.Version13)]
    [TestCase(Argon2Variant.Id, Argon2Version.Version13)]
    public void SliceSynchronizedOutputDoesNotDependOnLaneSchedulingOrder(
        Argon2Variant variant,
        Argon2Version version)
    {
        Argon2Parameters parameters = CreateParameters(variant, version, 128L, 2L);
        Argon2Memory forwardMemory = new Argon2Memory(parameters, 3);
        Argon2Memory reverseMemory = new Argon2Memory(parameters, 5);
        InitializeMemory(forwardMemory);
        InitializeMemory(reverseMemory);
        string initialDigest = GetMemoryDigest(forwardMemory);

        FillMemoryBySlices(parameters, forwardMemory, false);
        FillMemoryBySlices(parameters, reverseMemory, true);

        string forwardDigest = GetMemoryDigest(forwardMemory);
        string reverseDigest = GetMemoryDigest(reverseMemory);
        Assert.Multiple(() =>
        {
            Assert.That(forwardDigest, Is.Not.EqualTo(initialDigest));
            Assert.That(reverseDigest, Is.EqualTo(forwardDigest));
        });
    }

    static Argon2Parameters CreateParameters(
        Argon2Variant variant,
        Argon2Version version,
        long memorySizeKiB,
        long iterations)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.Iterations = iterations;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static void InitializeMemory(Argon2Memory memory)
    {
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
    }

    static void FillMemoryBySlices(
        Argon2Parameters parameters,
        Argon2Memory memory,
        bool reverseLaneOrder)
    {
        long passIndex = 0L;
        while (passIndex < parameters.Iterations)
        {
            int sliceIndex = 0;
            while (sliceIndex < Argon2Limits.SynchronizationPoints)
            {
                int laneStep = reverseLaneOrder ? -1 : 1;
                int laneIndex = reverseLaneOrder
                    ? parameters.DegreeOfParallelism - 1
                    : 0;
                int lanesFilled = 0;
                while (lanesFilled < parameters.DegreeOfParallelism)
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
                    laneIndex = laneIndex + laneStep;
                    lanesFilled = lanesFilled + 1;
                }

                sliceIndex = sliceIndex + 1;
            }

            passIndex = passIndex + 1L;
        }
    }

    static string GetMemoryDigest(Argon2Memory memory)
    {
        byte[] bytes = new byte[(int)memory.GetBlockCount() * Argon2Limits.BlockSizeInBytes];
        long blockIndex = 0L;
        while (blockIndex < memory.GetBlockCount())
        {
            Span<ulong> block = memory.GetBlock(blockIndex);
            int wordIndex = 0;
            while (wordIndex < block.Length)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(
                    bytes.AsSpan(
                        (int)blockIndex * Argon2Limits.BlockSizeInBytes + wordIndex * 8,
                        8),
                    block[wordIndex]);
                wordIndex = wordIndex + 1;
            }

            blockIndex = blockIndex + 1L;
        }

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
