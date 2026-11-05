using System;

namespace argon2;

internal static class Argon2Core
{
    internal static void ComputeHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        Span<byte> destination)
    {
        ValidateOperation(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            destination.Length);

        Argon2Workspace workspace = new Argon2Workspace(
            parameters,
            executionOptions,
            Argon2Memory.DefaultBlocksPerChunk);
        ComputeHashUsingWorkspace(
            password,
            salt,
            secret,
            associatedData,
            parameters,
            executionOptions,
            workspace,
            destination);
    }

    internal static void ComputeHashUsingWorkspace(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        Argon2Workspace workspace,
        Span<byte> destination)
    {
        if (workspace == null)
        {
            throw new Argon2ParameterException("An Argon2 workspace is required for core execution.");
        }

        using (workspace)
        {
            ValidateOperation(
                password,
                salt,
                secret,
                associatedData,
                parameters,
                executionOptions,
                destination.Length);

            SecureBuffer initialHashBuffer = workspace.GetInitialHashBuffer();
            Span<byte> initialHash = initialHashBuffer.GetSpan();
            Argon2InitialHash.ComputeHash(
                password,
                salt,
                secret,
                associatedData,
                parameters,
                destination.Length,
                initialHash);

            Argon2Memory memory = workspace.GetMemory();
            InitializeFirstBlocks(initialHash, memory);
            initialHash.Clear();
            Argon2Executor.FillMemory(parameters, executionOptions, workspace);
            Argon2Finalization.ComputeTag(
                memory,
                workspace.GetFinalBlock(),
                destination);
        }
    }

    static void InitializeFirstBlocks(
        ReadOnlySpan<byte> initialHash,
        Argon2Memory memory)
    {
        using (SecureBuffer firstBlockBuffer = new SecureBuffer(Argon2Limits.BlockSizeInBytes))
        using (SecureBuffer secondBlockBuffer = new SecureBuffer(Argon2Limits.BlockSizeInBytes))
        {
            Span<byte> firstBlock = firstBlockBuffer.GetSpan();
            Span<byte> secondBlock = secondBlockBuffer.GetSpan();
            int laneIndex = 0;
            while (laneIndex < memory.GetLaneCount())
            {
                Argon2InitialHash.ExpandFirstBlocksForLane(
                    initialHash,
                    laneIndex,
                    firstBlock,
                    secondBlock);
                long laneStartBlock = (long)laneIndex * memory.GetBlocksPerLane();
                memory.WriteBlockBytes(laneStartBlock, firstBlock);
                memory.WriteBlockBytes(laneStartBlock + 1L, secondBlock);
                firstBlock.Clear();
                secondBlock.Clear();
                laneIndex = laneIndex + 1;
            }
        }
    }

    static void ValidateOperation(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        int outputLength)
    {
        Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters);
        Argon2ParameterValidator.ValidateInputLengths(
            password,
            salt,
            secret,
            associatedData,
            outputLength);
    }
}
