using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2MemoryTests
{
    [Test]
    public void GeometryUsesNormalizedMemoryAndConfiguredChunks()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(37L, 2), 3);

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlockCount(), Is.EqualTo(32L));
            Assert.That(memory.GetLaneCount(), Is.EqualTo(2));
            Assert.That(memory.GetBlocksPerLane(), Is.EqualTo(16L));
            Assert.That(memory.GetSegmentLength(), Is.EqualTo(4L));
            Assert.That(memory.GetBlocksPerChunk(), Is.EqualTo(3));
            Assert.That(memory.GetChunkCount(), Is.EqualTo(11));
        });
    }

    [Test]
    public void BlocksRemainIndependentAcrossChunkBoundaries()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(32L, 2), 3);
        long[] blockIndices = new long[] { 0L, 2L, 3L, 5L, 30L, 31L };

        int index = 0;
        while (index < blockIndices.Length)
        {
            Span<ulong> block = memory.GetBlock(blockIndices[index]);
            block[0] = (ulong)(index + 1);
            block[127] = (ulong)(100 + index);
            index = index + 1;
        }

        index = 0;
        while (index < blockIndices.Length)
        {
            Span<ulong> block = memory.GetBlock(blockIndices[index]);
            ulong firstWord = block[0];
            ulong lastWord = block[127];
            int expectedIndex = index;
            Assert.Multiple(() =>
            {
                Assert.That(firstWord, Is.EqualTo((ulong)(expectedIndex + 1)));
                Assert.That(lastWord, Is.EqualTo((ulong)(100 + expectedIndex)));
            });
            index = index + 1;
        }
    }

    [Test]
    public void LaneAndSegmentMappingUseNormalizedGeometry()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(37L, 2), 5);

        memory.GetLaneBlock(1, 0L)[7] = 0x123456789ABCDEF0UL;

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlock(16L)[7], Is.EqualTo(0x123456789ABCDEF0UL));
            Assert.That(memory.GetSegmentStartBlockIndex(0, 0), Is.EqualTo(0L));
            Assert.That(memory.GetSegmentStartBlockIndex(0, 3), Is.EqualTo(12L));
            Assert.That(memory.GetSegmentStartBlockIndex(1, 0), Is.EqualTo(16L));
            Assert.That(memory.GetSegmentStartBlockIndex(1, 2), Is.EqualTo(24L));
        });
    }

    [Test]
    public void ByteImportAndExportAreExplicitlyLittleEndian()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(8L, 1), 2);
        byte[] source = Enumerable.Range(0, 1024).Select(value => (byte)value).ToArray();
        byte[] destination = new byte[1024];

        memory.WriteBlockBytes(2L, source);
        memory.ReadBlockBytes(2L, destination);

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlock(2L)[0], Is.EqualTo(0x0706050403020100UL));
            Assert.That(memory.GetBlock(2L)[1], Is.EqualTo(0x0F0E0D0C0B0A0908UL));
            Assert.That(destination, Is.EqualTo(source));
        });
    }

    [Test]
    public void ClearZerosEveryAllocatedChunk()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(32L, 2), 3);
        memory.GetBlock(0L)[0] = ulong.MaxValue;
        memory.GetBlock(15L)[64] = ulong.MaxValue;
        memory.GetBlock(31L)[127] = ulong.MaxValue;

        memory.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlock(0L).ToArray(), Is.All.Zero);
            Assert.That(memory.GetBlock(15L).ToArray(), Is.All.Zero);
            Assert.That(memory.GetBlock(31L).ToArray(), Is.All.Zero);
        });
    }

    [Test]
    public void InvalidBlockLaneAndSliceIndicesAreRejected()
    {
        Argon2Memory memory = new Argon2Memory(CreateParameters(32L, 2), 3);

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => memory.GetBlock(-1L));
            Assert.Throws<Argon2ParameterException>(() => memory.GetBlock(32L));
            Assert.Throws<Argon2ParameterException>(() => memory.GetLaneBlock(-1, 0L));
            Assert.Throws<Argon2ParameterException>(() => memory.GetLaneBlock(2, 0L));
            Assert.Throws<Argon2ParameterException>(() => memory.GetLaneBlock(0, 16L));
            Assert.Throws<Argon2ParameterException>(() => memory.GetSegmentStartBlockIndex(0, -1));
            Assert.Throws<Argon2ParameterException>(() => memory.GetSegmentStartBlockIndex(0, 4));
        });
    }

    [Test]
    public void InvalidChunkAndByteBlockLengthsAreRejectedBeforeUse()
    {
        Argon2Parameters parameters = CreateParameters(8L, 1);
        Argon2Memory memory = new Argon2Memory(parameters, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => new Argon2Memory(parameters, 0));
            Assert.Throws<Argon2ParameterException>(
                () => new Argon2Memory(parameters, Array.MaxLength / 128 + 1));
            Assert.Throws<Argon2ParameterException>(() => memory.WriteBlockBytes(0L, new byte[1023]));
            Assert.Throws<Argon2ParameterException>(() => memory.ReadBlockBytes(0L, new byte[1025]));
        });
    }

    [Test]
    public void UnaddressableChunkTableIsRejectedBeforeLargeAllocation()
    {
        Argon2Parameters parameters = CreateParameters(Argon2Limits.MaximumMemorySizeKiB, 1);

        Assert.Throws<Argon2ParameterException>(() => new Argon2Memory(parameters, 1));
    }

    static Argon2Parameters CreateParameters(long memorySizeKiB, int lanes)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.DegreeOfParallelism = lanes;
        return parameters;
    }
}
