using System.Text;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2MemoryGeometryTests
{
    [TestCase(8L, 1, 8L, 8L, 2L)]
    [TestCase(24L, 3, 24L, 8L, 2L)]
    [TestCase(35L, 4, 32L, 8L, 2L)]
    [TestCase(59L, 5, 40L, 8L, 2L)]
    [TestCase(132L, 3, 132L, 44L, 11L)]
    [TestCase(143L, 3, 132L, 44L, 11L)]
    public void ExactAndRoundedRequestsProduceExpectedLaneSliceGeometry(
        long requestedMemorySizeKiB,
        int laneCount,
        long expectedBlockCount,
        long expectedBlocksPerLane,
        long expectedSegmentLength)
    {
        Argon2Parameters parameters = CreateParameters(requestedMemorySizeKiB, laneCount);
        Argon2Memory memory = new Argon2Memory(parameters, 7);

        Assert.Multiple(() =>
        {
            Assert.That(
                Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters),
                Is.EqualTo(expectedBlockCount));
            Assert.That(memory.GetBlockCount(), Is.EqualTo(expectedBlockCount));
            Assert.That(memory.GetLaneCount(), Is.EqualTo(laneCount));
            Assert.That(memory.GetBlocksPerLane(), Is.EqualTo(expectedBlocksPerLane));
            Assert.That(memory.GetSegmentLength(), Is.EqualTo(expectedSegmentLength));
        });

        int laneIndex = 0;
        while (laneIndex < laneCount)
        {
            int sliceIndex = 0;
            while (sliceIndex < Argon2Limits.SynchronizationPoints)
            {
                long expectedStart = (long)laneIndex * expectedBlocksPerLane
                    + (long)sliceIndex * expectedSegmentLength;
                Assert.That(
                    memory.GetSegmentStartBlockIndex(laneIndex, sliceIndex),
                    Is.EqualTo(expectedStart));
                sliceIndex = sliceIndex + 1;
            }

            laneIndex = laneIndex + 1;
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(7)]
    public void MinimumMemoryIsExactlyEightBlocksPerLane(int laneCount)
    {
        long minimumMemorySizeKiB = Argon2Limits.MinimumMemorySizePerLaneKiB * laneCount;
        Argon2Parameters minimum = CreateParameters(minimumMemorySizeKiB, laneCount);
        Argon2Parameters belowMinimum = CreateParameters(minimumMemorySizeKiB - 1L, laneCount);
        Argon2Memory memory = new Argon2Memory(minimum, 3);

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlockCount(), Is.EqualTo(minimumMemorySizeKiB));
            Assert.That(memory.GetBlocksPerLane(), Is.EqualTo(8L));
            Assert.That(memory.GetSegmentLength(), Is.EqualTo(2L));
            Assert.Throws<Argon2ParameterException>(() => new Argon2Memory(belowMinimum, 3));
        });
    }

    [Test]
    public void FirstBlockOfLaterPassUsesLastLaneBlockAsPreviousBlock()
    {
        Argon2Parameters parameters = CreateParameters(32L, 1);
        parameters.Variant = Argon2Variant.D;
        parameters.Iterations = 2L;
        Argon2Memory memory = new Argon2Memory(parameters, 5);
        FillDeterministicMemory(memory);
        ulong[] previousBlock = memory.GetLaneBlock(0, 31L).ToArray();
        ulong[] existingBlock = memory.GetLaneBlock(0, 0L).ToArray();
        long referenceBlockIndex = Argon2Indexing.GetReferenceBlockIndex(
            Argon2Indexing.GetJ1(previousBlock[0]),
            1L,
            0,
            0L,
            true,
            memory.GetBlocksPerLane(),
            memory.GetSegmentLength());
        ulong[] referenceBlock = memory.GetLaneBlock(0, referenceBlockIndex).ToArray();
        ulong[] expectedBlock = new ulong[Argon2Limits.WordsPerBlock];
        Argon2Compression.CompressBlockWithXor(
            previousBlock,
            referenceBlock,
            existingBlock,
            expectedBlock);

        Argon2LaneFiller.FillSegment(
            parameters,
            memory,
            1L,
            0,
            0,
            new ulong[Argon2Limits.WordsPerBlock],
            new ulong[Argon2Limits.WordsPerBlock],
            new ulong[Argon2Limits.WordsPerBlock]);

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetLaneBlock(0, 0L).ToArray(), Is.EqualTo(expectedBlock));
            Assert.That(memory.GetLaneBlock(0, 31L).ToArray(), Is.EqualTo(previousBlock));
        });
    }

    [Test]
    public void RequestedMemoryIsPreservedInH0PhcMetadataAndFinalOutput()
    {
        byte[] password = Encoding.ASCII.GetBytes("password");
        byte[] salt = Encoding.ASCII.GetBytes("somesalt");
        Argon2Parameters exactParameters = CreateParameters(32L, 4);
        Argon2Parameters roundedParameters = CreateParameters(35L, 4);
        byte[] exactInitialHash = ComputeInitialHash(
            password,
            salt,
            exactParameters);
        byte[] roundedInitialHash = ComputeInitialHash(
            password,
            salt,
            roundedParameters);
        byte[] exactTag = ComputeTag(password, salt, exactParameters);
        byte[] roundedTag = ComputeTag(password, salt, roundedParameters);
        IArgon2EncodedHashCodec codec = new Argon2EncodedHashCodec();
        string exactEncoded = codec.EncodeHash(CreateHash(exactParameters, salt, exactTag));
        string roundedEncoded = codec.EncodeHash(CreateHash(roundedParameters, salt, roundedTag));
        Argon2EncodedHash decodedRounded = codec.DecodeHash(roundedEncoded);

        Assert.Multiple(() =>
        {
            Assert.That(
                Argon2ParameterValidator.GetNormalizedMemoryBlockCount(exactParameters),
                Is.EqualTo(32L));
            Assert.That(
                Argon2ParameterValidator.GetNormalizedMemoryBlockCount(roundedParameters),
                Is.EqualTo(32L));
            Assert.That(ToHex(exactInitialHash), Is.EqualTo(
                "d8c6bce35af5b07de013e69d1b540a9b1438cb6380e3cf994a283e5f22d46fa1" +
                "abf5d7a0d30d20322a035bbba92c78244d9c8a6887e5be5faf9c22d7c067af91"));
            Assert.That(ToHex(roundedInitialHash), Is.EqualTo(
                "12b24286d116875016467650af419e2d6ec2e81c33de50f49ce67f2d4f0c2a6f" +
                "5fe9a79046069e761ec55c940806056ff011e874612a08f1bf2aaafcade76a5a"));
            Assert.That(ToHex(exactTag), Is.EqualTo("31370705392c0ccd6646287647f923d1"));
            Assert.That(ToHex(roundedTag), Is.EqualTo("2d9acff48e487284bdde09398d31a5f9"));
            Assert.That(exactEncoded, Is.EqualTo(
                "$argon2id$v=19$m=32,t=2,p=4$c29tZXNhbHQ$MTcHBTksDM1mRih2R/kj0Q"));
            Assert.That(roundedEncoded, Is.EqualTo(
                "$argon2id$v=19$m=35,t=2,p=4$c29tZXNhbHQ$LZrP9I5IcoS93gk5jTGl+Q"));
            Assert.That(decodedRounded.Parameters.MemorySizeKiB, Is.EqualTo(35L));
        });
    }

    static Argon2Parameters CreateParameters(long memorySizeKiB, int laneCount)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.Iterations = 2L;
        parameters.DegreeOfParallelism = laneCount;
        return parameters;
    }

    static void FillDeterministicMemory(Argon2Memory memory)
    {
        long blockIndex = 0L;
        while (blockIndex < memory.GetBlockCount())
        {
            Span<ulong> block = memory.GetBlock(blockIndex);
            int wordIndex = 0;
            while (wordIndex < block.Length)
            {
                block[wordIndex] = (ulong)(blockIndex + 1L) * 0x100000001B3UL
                    + (ulong)(wordIndex + 1);
                wordIndex = wordIndex + 1;
            }

            blockIndex = blockIndex + 1L;
        }
    }

    static byte[] ComputeInitialHash(
        byte[] password,
        byte[] salt,
        Argon2Parameters parameters)
    {
        byte[] initialHash = new byte[Argon2Limits.PrehashDigestLength];
        Argon2InitialHash.ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            16,
            initialHash);
        return initialHash;
    }

    static byte[] ComputeTag(
        byte[] password,
        byte[] salt,
        Argon2Parameters parameters)
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = 4;
        byte[] tag = new byte[16];
        IArgon2Primitive primitive = new Argon2Primitive();
        primitive.ComputeHash(
            password,
            salt,
            ReadOnlySpan<byte>.Empty,
            ReadOnlySpan<byte>.Empty,
            parameters,
            executionOptions,
            tag);
        return tag;
    }

    static Argon2EncodedHash CreateHash(
        Argon2Parameters parameters,
        byte[] salt,
        byte[] tag)
    {
        Argon2EncodedHash hash = new Argon2EncodedHash();
        hash.Parameters = parameters;
        hash.Salt = salt;
        hash.Tag = tag;
        return hash;
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
