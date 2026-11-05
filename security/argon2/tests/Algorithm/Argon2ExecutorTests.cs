using System.Buffers.Binary;
using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2ExecutorTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void EveryWorkerLimitMatchesIndependentSliceSynchronizedFixture(int maximumWorkerCount)
    {
        Argon2Parameters parameters = CreateParameters(128L);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions(maximumWorkerCount);

        using (Argon2Workspace workspace = new Argon2Workspace(parameters, executionOptions, 3))
        {
            InitializeMemory(workspace.GetMemory());

            Argon2Executor.FillMemory(parameters, executionOptions, workspace);

            Assert.Multiple(() =>
            {
                Assert.That(workspace.GetWorkerCount(), Is.EqualTo(maximumWorkerCount));
                Assert.That(GetMemoryDigest(workspace.GetMemory()), Is.EqualTo(
                    "3f4a82579024aae89a7afaeab78b344225a1489f4bbe898d604f9924002819f0"));
                Assert.That(
                    GetBlockDigest(workspace.GetMemory().GetLaneBlock(0, 31L)),
                    Is.EqualTo("af0e6c7a3ff0391526d1d278eed41172123ff8b6467042bcbcc1963d14407e74"));
                Assert.That(
                    GetBlockDigest(workspace.GetMemory().GetLaneBlock(3, 31L)),
                    Is.EqualTo("273b41701f4895f60313eae932b5c054bd9d46d5096ec9aaeb62f5db056eb84d"));
            });
        }
    }

    [Test]
    public void ParallelFailuresPropagateOriginalExceptionDeterministically()
    {
        Argon2Parameters workspaceParameters = CreateParameters(128L);
        Argon2Parameters mismatchedParameters = CreateParameters(144L);
        Argon2ExecutionOptions sequentialOptions = CreateExecutionOptions(1);
        Argon2ExecutionOptions parallelOptions = CreateExecutionOptions(4);

        using (Argon2Workspace sequentialWorkspace = new Argon2Workspace(
            workspaceParameters,
            sequentialOptions,
            3))
        using (Argon2Workspace parallelWorkspace = new Argon2Workspace(
            workspaceParameters,
            parallelOptions,
            3))
        {
            Argon2ParameterException sequentialException = Assert.Throws<Argon2ParameterException>(
                () => Argon2Executor.FillMemory(
                    mismatchedParameters,
                    sequentialOptions,
                    sequentialWorkspace));
            Argon2ParameterException parallelException = Assert.Throws<Argon2ParameterException>(
                () => Argon2Executor.FillMemory(
                    mismatchedParameters,
                    parallelOptions,
                    parallelWorkspace));

            Assert.Multiple(() =>
            {
                Assert.That(parallelException, Is.Not.InstanceOf<AggregateException>());
                Assert.That(parallelException.Message, Is.EqualTo(sequentialException.Message));
                Assert.That(parallelException.Message, Is.EqualTo(
                    "Argon2 memory geometry does not match the configured parameters."));
            });
        }
    }

    [Test]
    public void ExecutorRejectsWorkspaceWithTooFewWorkerBuffers()
    {
        Argon2Parameters parameters = CreateParameters(128L);
        Argon2ExecutionOptions workspaceOptions = CreateExecutionOptions(1);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions(2);

        using (Argon2Workspace workspace = new Argon2Workspace(parameters, workspaceOptions, 3))
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Executor.FillMemory(parameters, executionOptions, workspace));
        }
    }

    static Argon2Parameters CreateParameters(long memorySizeKiB)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 2L;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static Argon2ExecutionOptions CreateExecutionOptions(int maximumWorkerCount)
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = maximumWorkerCount;
        executionOptions.ClearMemory = false;
        return executionOptions;
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

    static string GetMemoryDigest(Argon2Memory memory)
    {
        int byteLength = (int)(memory.GetBlockCount() * Argon2Limits.BlockSizeInBytes);
        byte[] bytes = new byte[byteLength];
        long blockIndex = 0L;
        while (blockIndex < memory.GetBlockCount())
        {
            WriteBlock(memory.GetBlock(blockIndex), bytes, (int)blockIndex * Argon2Limits.BlockSizeInBytes);
            blockIndex = blockIndex + 1L;
        }

        return ToHex(SHA256.HashData(bytes));
    }

    static string GetBlockDigest(ReadOnlySpan<ulong> block)
    {
        byte[] bytes = new byte[Argon2Limits.BlockSizeInBytes];
        WriteBlock(block, bytes, 0);
        return ToHex(SHA256.HashData(bytes));
    }

    static void WriteBlock(ReadOnlySpan<ulong> block, byte[] destination, int offset)
    {
        int wordIndex = 0;
        while (wordIndex < block.Length)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(
                destination.AsSpan(offset + wordIndex * 8, 8),
                block[wordIndex]);
            wordIndex = wordIndex + 1;
        }
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
