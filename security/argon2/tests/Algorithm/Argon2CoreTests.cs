using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2CoreTests
{
    [TestCase(
        Argon2Variant.D,
        "512b391b6f1162975371d30919734294f868e3be3984f3c1a13a4db9fabe4acb")]
    [TestCase(
        Argon2Variant.I,
        "c814d9d1dc7f37aa13f0d77f2494bda1c8de6b016dd388d29952a4c4672b6ce8")]
    [TestCase(
        Argon2Variant.Id,
        "0d640df58d78766c08c037a34a8b53c9d01ef0452d75b65eb52520e96b01e659")]
    public void CoreMatchesRfc9106Vectors(Argon2Variant variant, string expectedTag)
    {
        Argon2Parameters parameters = CreateRfcParameters(variant);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions(4, true);
        byte[] destination = new byte[32];

        Argon2Core.ComputeHash(
            CreateBytes(32, 0x01),
            CreateBytes(16, 0x02),
            CreateBytes(8, 0x03),
            CreateBytes(12, 0x04),
            parameters,
            executionOptions,
            destination);

        Assert.That(ToHex(destination), Is.EqualTo(expectedTag));
    }

    [Test]
    public void MaximumWorkerCountDoesNotParticipateInCoreOutput()
    {
        Argon2Parameters parameters = CreateRfcParameters(Argon2Variant.Id);
        byte[] sequential = new byte[32];
        byte[] parallel = new byte[32];

        Argon2Core.ComputeHash(
            CreateBytes(32, 0x01),
            CreateBytes(16, 0x02),
            CreateBytes(8, 0x03),
            CreateBytes(12, 0x04),
            parameters,
            CreateExecutionOptions(1, true),
            sequential);
        Argon2Core.ComputeHash(
            CreateBytes(32, 0x01),
            CreateBytes(16, 0x02),
            CreateBytes(8, 0x03),
            CreateBytes(12, 0x04),
            parameters,
            CreateExecutionOptions(4, true),
            parallel);

        Assert.That(parallel, Is.EqualTo(sequential));
    }

    [Test]
    public void CorePreservesEveryCallerOwnedInput()
    {
        Argon2Parameters parameters = CreateRfcParameters(Argon2Variant.Id);
        byte[] password = CreateBytes(32, 0x01);
        byte[] salt = CreateBytes(16, 0x02);
        byte[] secret = CreateBytes(8, 0x03);
        byte[] associatedData = CreateBytes(12, 0x04);
        byte[] originalPassword = (byte[])password.Clone();
        byte[] originalSalt = (byte[])salt.Clone();
        byte[] originalSecret = (byte[])secret.Clone();
        byte[] originalAssociatedData = (byte[])associatedData.Clone();

        Argon2Core.ComputeHash(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            CreateExecutionOptions(2, true),
            new byte[32]);

        Assert.Multiple(() =>
        {
            Assert.That(password, Is.EqualTo(originalPassword));
            Assert.That(salt, Is.EqualTo(originalSalt));
            Assert.That(secret, Is.EqualTo(originalSecret));
            Assert.That(associatedData, Is.EqualTo(originalAssociatedData));
        });
    }

    [Test]
    public void SuppliedWorkspaceIsDisposedAndClearedAfterExecutionFailure()
    {
        Argon2Parameters workspaceParameters = CreateSmallParameters(32L);
        Argon2Parameters mismatchedParameters = CreateSmallParameters(40L);
        Argon2ExecutionOptions executionOptions = CreateExecutionOptions(2, true);
        Argon2Workspace workspace = new Argon2Workspace(
            workspaceParameters,
            executionOptions,
            3);
        Argon2Memory memory = workspace.GetMemory();
        SecureBuffer initialHash = workspace.GetInitialHashBuffer();
        ulong[] addressBlock = workspace.GetAddressBlock(0);
        ulong[] secondAddressBlock = workspace.GetAddressBlock(1);
        ulong[] inputBlock = workspace.GetInputBlock(0);
        ulong[] secondInputBlock = workspace.GetInputBlock(1);
        ulong[] temporaryBlock = workspace.GetTemporaryBlock(0);
        ulong[] secondTemporaryBlock = workspace.GetTemporaryBlock(1);
        ulong[] finalBlock = workspace.GetFinalBlock();
        initialHash.GetSpan().Fill(0xA5);
        addressBlock.AsSpan().Fill(ulong.MaxValue);
        secondAddressBlock.AsSpan().Fill(ulong.MaxValue);
        inputBlock.AsSpan().Fill(ulong.MaxValue);
        secondInputBlock.AsSpan().Fill(ulong.MaxValue);
        temporaryBlock.AsSpan().Fill(ulong.MaxValue);
        secondTemporaryBlock.AsSpan().Fill(ulong.MaxValue);
        finalBlock.AsSpan().Fill(ulong.MaxValue);
        long blockIndex = 0L;
        while (blockIndex < memory.GetBlockCount())
        {
            memory.GetBlock(blockIndex).Fill(ulong.MaxValue);
            blockIndex = blockIndex + 1L;
        }

        Assert.Throws<Argon2ParameterException>(
            () => Argon2Core.ComputeHashUsingWorkspace(
                CreateBytes(8, 0x01),
                CreateBytes(8, 0x02),
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                mismatchedParameters,
                executionOptions,
                workspace,
                new byte[32]));

        Assert.Multiple(() =>
        {
            Assert.That(workspace.IsDisposed(), Is.True);
            Assert.That(initialHash.IsCleared(), Is.True);
            Assert.That(addressBlock, Is.All.Zero);
            Assert.That(secondAddressBlock, Is.All.Zero);
            Assert.That(inputBlock, Is.All.Zero);
            Assert.That(secondInputBlock, Is.All.Zero);
            Assert.That(temporaryBlock, Is.All.Zero);
            Assert.That(secondTemporaryBlock, Is.All.Zero);
            Assert.That(finalBlock, Is.All.Zero);
            long clearedBlockIndex = 0L;
            while (clearedBlockIndex < memory.GetBlockCount())
            {
                Assert.That(
                    memory.GetBlock(clearedBlockIndex).ToArray(),
                    Is.All.Zero,
                    $"Memory block {clearedBlockIndex} was not cleared after failure.");
                clearedBlockIndex = clearedBlockIndex + 1L;
            }
        });
    }

    [Test]
    public void InvalidInputIsRejectedBeforeDestinationMutation()
    {
        Argon2Parameters parameters = CreateSmallParameters(32L);
        byte[] destination = CreateBytes(32, 0xA5);
        byte[] originalDestination = (byte[])destination.Clone();

        Assert.Throws<Argon2ParameterException>(
            () => Argon2Core.ComputeHash(
                CreateBytes(8, 0x01),
                new byte[7],
                ReadOnlySpan<byte>.Empty,
                ReadOnlySpan<byte>.Empty,
                parameters,
                CreateExecutionOptions(2, true),
                destination));

        Assert.That(destination, Is.EqualTo(originalDestination));
    }

    static Argon2Parameters CreateRfcParameters(Argon2Variant variant)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 3L;
        parameters.MemorySizeKiB = 32L;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static Argon2Parameters CreateSmallParameters(long memorySizeKiB)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = Argon2Variant.Id;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 2L;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.DegreeOfParallelism = 2;
        return parameters;
    }

    static Argon2ExecutionOptions CreateExecutionOptions(
        int maximumWorkerCount,
        bool clearMemory)
    {
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        executionOptions.MaximumWorkerCount = maximumWorkerCount;
        executionOptions.ClearMemory = clearMemory;
        return executionOptions;
    }

    static byte[] CreateBytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
