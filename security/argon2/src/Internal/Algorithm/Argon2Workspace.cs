using System;

namespace argon2;

internal class Argon2Workspace : System.IDisposable
{
    Argon2Memory memory;
    SecureBuffer initialHashBuffer;
    ulong[][] addressBlocks;
    ulong[][] inputBlocks;
    ulong[][] temporaryBlocks;
    ulong[] finalBlock;
    bool clearMemory;
    bool disposed;

    internal Argon2Workspace(
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        int blocksPerChunk)
    {
        Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters);

        memory = new Argon2Memory(parameters, blocksPerChunk);
        initialHashBuffer = new SecureBuffer(Argon2Limits.PrehashDigestLength);
        addressBlocks = new ulong[executionOptions.MaximumWorkerCount][];
        inputBlocks = new ulong[executionOptions.MaximumWorkerCount][];
        temporaryBlocks = new ulong[executionOptions.MaximumWorkerCount][];
        finalBlock = new ulong[Argon2Limits.WordsPerBlock];
        clearMemory = executionOptions.ClearMemory;

        int workerIndex = 0;
        while (workerIndex < addressBlocks.Length)
        {
            addressBlocks[workerIndex] = new ulong[Argon2Limits.WordsPerBlock];
            inputBlocks[workerIndex] = new ulong[Argon2Limits.WordsPerBlock];
            temporaryBlocks[workerIndex] = new ulong[Argon2Limits.WordsPerBlock];
            workerIndex = workerIndex + 1;
        }
    }

    internal Argon2Memory GetMemory()
    {
        EnsureNotDisposed();
        return memory;
    }

    internal SecureBuffer GetInitialHashBuffer()
    {
        EnsureNotDisposed();
        return initialHashBuffer;
    }

    internal ulong[] GetAddressBlock(int workerIndex)
    {
        EnsureWorkerIndex(workerIndex);
        return addressBlocks[workerIndex];
    }

    internal ulong[] GetInputBlock(int workerIndex)
    {
        EnsureWorkerIndex(workerIndex);
        return inputBlocks[workerIndex];
    }

    internal ulong[] GetTemporaryBlock(int workerIndex)
    {
        EnsureWorkerIndex(workerIndex);
        return temporaryBlocks[workerIndex];
    }

    internal ulong[] GetFinalBlock()
    {
        EnsureNotDisposed();
        return finalBlock;
    }

    internal int GetWorkerCount()
    {
        EnsureNotDisposed();
        return addressBlocks.Length;
    }

    internal bool IsDisposed()
    {
        return disposed;
    }

    /// <summary>
    /// Clears owned cryptographic temporaries and the configured memory matrix.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            initialHashBuffer.Dispose();

            int workerIndex = 0;
            while (workerIndex < addressBlocks.Length)
            {
                Array.Clear(addressBlocks[workerIndex]);
                Array.Clear(inputBlocks[workerIndex]);
                Array.Clear(temporaryBlocks[workerIndex]);
                workerIndex = workerIndex + 1;
            }

            Array.Clear(finalBlock);
            if (clearMemory)
            {
                memory.Clear();
            }

            disposed = true;
        }
    }

    void EnsureNotDisposed()
    {
        if (disposed)
        {
            throw new System.InvalidOperationException("The Argon2 workspace has been disposed.");
        }
    }

    void EnsureWorkerIndex(int workerIndex)
    {
        EnsureNotDisposed();
        if (workerIndex < 0 || workerIndex >= addressBlocks.Length)
        {
            throw new Argon2ParameterException("Argon2 workspace worker index is outside the allocated range.");
        }
    }
}
