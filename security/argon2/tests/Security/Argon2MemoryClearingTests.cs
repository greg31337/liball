using argon2;

namespace argon2.Tests.Security;

public class Argon2MemoryClearingTests
{
    [Test]
    public void SecureBufferClearsOnNormalUsingExit()
    {
        SecureBuffer buffer = new SecureBuffer(64);
        byte[] ownedArray = buffer.GetArray();

        using (buffer)
        {
            buffer.GetSpan().Fill(0xA5);
            Assert.That(buffer.IsCleared(), Is.False);
        }

        Assert.Multiple(() =>
        {
            Assert.That(buffer.IsDisposed(), Is.True);
            Assert.That(buffer.IsCleared(), Is.True);
            Assert.That(ownedArray, Is.All.Zero);
            Assert.Throws<InvalidOperationException>(() => buffer.GetSpan());
            Assert.Throws<InvalidOperationException>(() => buffer.GetArray());
        });
    }

    [Test]
    public void SecureBufferClearsOnExceptionalUsingExit()
    {
        SecureBuffer buffer = new SecureBuffer(32);

        Assert.Throws<InvalidOperationException>(() => FillBufferAndThrow(buffer));

        Assert.Multiple(() =>
        {
            Assert.That(buffer.IsDisposed(), Is.True);
            Assert.That(buffer.IsCleared(), Is.True);
        });
    }

    [Test]
    public void WorkspaceClearsMemoryAndEveryTemporary()
    {
        Argon2Workspace workspace = CreateWorkspace(true);
        Argon2Memory memory = workspace.GetMemory();
        SecureBuffer initialHash = workspace.GetInitialHashBuffer();
        ulong[] addressBlock = workspace.GetAddressBlock(0);
        ulong[] secondAddressBlock = workspace.GetAddressBlock(1);
        ulong[] inputBlock = workspace.GetInputBlock(0);
        ulong[] temporaryBlock = workspace.GetTemporaryBlock(0);
        ulong[] finalBlock = workspace.GetFinalBlock();
        initialHash.GetSpan().Fill(0x5A);
        addressBlock[0] = ulong.MaxValue;
        secondAddressBlock[127] = ulong.MaxValue;
        inputBlock[10] = ulong.MaxValue;
        temporaryBlock[20] = ulong.MaxValue;
        finalBlock[30] = ulong.MaxValue;
        memory.GetBlock(0L)[0] = ulong.MaxValue;
        memory.GetBlock(31L)[127] = ulong.MaxValue;

        workspace.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(workspace.IsDisposed(), Is.True);
            Assert.That(initialHash.IsCleared(), Is.True);
            Assert.That(addressBlock, Is.All.Zero);
            Assert.That(secondAddressBlock, Is.All.Zero);
            Assert.That(inputBlock, Is.All.Zero);
            Assert.That(temporaryBlock, Is.All.Zero);
            Assert.That(finalBlock, Is.All.Zero);
            Assert.That(memory.GetBlock(0L).ToArray(), Is.All.Zero);
            Assert.That(memory.GetBlock(31L).ToArray(), Is.All.Zero);
        });
    }

    [Test]
    public void WorkspaceClearsEveryAllocatedMemoryChunk()
    {
        Argon2Workspace workspace = CreateWorkspace(true);
        Argon2Memory memory = workspace.GetMemory();
        long blockIndex = 0L;
        while (blockIndex < memory.GetBlockCount())
        {
            memory.GetBlock(blockIndex).Fill((ulong)(blockIndex + 1L));
            blockIndex = blockIndex + 1L;
        }

        workspace.Dispose();

        Assert.That(memory.GetChunkCount(), Is.GreaterThan(1));
        Assert.Multiple(() =>
        {
            long clearedBlockIndex = 0L;
            while (clearedBlockIndex < memory.GetBlockCount())
            {
                Assert.That(
                    memory.GetBlock(clearedBlockIndex).ToArray(),
                    Is.All.Zero,
                    $"Memory block {clearedBlockIndex} was not cleared.");
                clearedBlockIndex = clearedBlockIndex + 1L;
            }
        });
    }

    [Test]
    public void WorkspaceClearsOnExceptionalUsingExit()
    {
        Argon2Workspace workspace = CreateWorkspace(true);
        Argon2Memory memory = workspace.GetMemory();
        SecureBuffer initialHash = workspace.GetInitialHashBuffer();
        ulong[] addressBlock = workspace.GetAddressBlock(0);

        Assert.Throws<InvalidOperationException>(() => FillWorkspaceAndThrow(workspace));

        Assert.Multiple(() =>
        {
            Assert.That(workspace.IsDisposed(), Is.True);
            Assert.That(initialHash.IsCleared(), Is.True);
            Assert.That(addressBlock, Is.All.Zero);
            Assert.That(memory.GetBlock(0L).ToArray(), Is.All.Zero);
        });
    }

    [Test]
    public void MemoryClearingCanBeExplicitlyDisabledWhileTemporariesStillClear()
    {
        Argon2Workspace workspace = CreateWorkspace(false);
        Argon2Memory memory = workspace.GetMemory();
        SecureBuffer initialHash = workspace.GetInitialHashBuffer();
        ulong[] addressBlock = workspace.GetAddressBlock(0);
        memory.GetBlock(0L)[0] = 42UL;
        initialHash.GetSpan().Fill(1);
        addressBlock[0] = 42UL;

        workspace.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(memory.GetBlock(0L)[0], Is.EqualTo(42UL));
            Assert.That(initialHash.IsCleared(), Is.True);
            Assert.That(addressBlock, Is.All.Zero);
        });
    }

    [Test]
    public void DisposalIsIdempotentAndWorkspaceAccessIsRejectedAfterward()
    {
        Argon2Workspace workspace = CreateWorkspace(true);

        workspace.Dispose();

        Assert.DoesNotThrow(() => workspace.Dispose());
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => workspace.GetMemory());
            Assert.Throws<InvalidOperationException>(() => workspace.GetAddressBlock(0));
        });
    }

    [Test]
    public void WorkerScratchIndexIsValidated()
    {
        using (Argon2Workspace workspace = CreateWorkspace(true))
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<Argon2ParameterException>(() => workspace.GetAddressBlock(-1));
                Assert.Throws<Argon2ParameterException>(() => workspace.GetInputBlock(2));
                Assert.Throws<Argon2ParameterException>(() => workspace.GetTemporaryBlock(2));
            });
        }
    }

    static void FillBufferAndThrow(SecureBuffer buffer)
    {
        using (buffer)
        {
            buffer.GetSpan().Fill(0xA5);
            throw new InvalidOperationException("Expected test failure.");
        }
    }

    static void FillWorkspaceAndThrow(Argon2Workspace workspace)
    {
        using (workspace)
        {
            workspace.GetInitialHashBuffer().GetSpan().Fill(0xA5);
            workspace.GetAddressBlock(0)[0] = ulong.MaxValue;
            workspace.GetMemory().GetBlock(0L)[0] = ulong.MaxValue;
            throw new InvalidOperationException("Expected test failure.");
        }
    }

    static Argon2Workspace CreateWorkspace(bool clearMemory)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        Argon2ExecutionOptions executionOptions = new Argon2ExecutionOptions();
        parameters.MemorySizeKiB = 32L;
        parameters.DegreeOfParallelism = 2;
        executionOptions.MaximumWorkerCount = 2;
        executionOptions.ClearMemory = clearMemory;
        return new Argon2Workspace(parameters, executionOptions, 3);
    }
}
