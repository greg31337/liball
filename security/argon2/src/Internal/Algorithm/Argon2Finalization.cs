using System;

namespace argon2;

internal static class Argon2Finalization
{
    internal static void ComputeTag(
        Argon2Memory memory,
        ulong[] finalBlock,
        Span<byte> destination)
    {
        ValidateMemoryAndFinalBlock(memory, finalBlock);
        if (destination.Length < Argon2Limits.MinimumTagLength)
        {
            throw new Argon2ParameterException("Argon2 tag output must contain at least 4 bytes.");
        }

        using (SecureWordBuffer finalBuffer = new SecureWordBuffer(finalBlock))
        using (SecureBuffer serializedBuffer = new SecureBuffer(Argon2Limits.BlockSizeInBytes))
        {
            Span<ulong> combinedBlock = finalBuffer.GetSpan();
            CombineFinalLaneBlocks(memory, combinedBlock);
            Span<byte> serializedBlock = serializedBuffer.GetSpan();
            SerializeFinalBlock(combinedBlock, serializedBlock);
            Blake2bLong.ComputeHash(serializedBlock, destination);
        }
    }

    internal static void CombineFinalLaneBlocks(
        Argon2Memory memory,
        Span<ulong> destination)
    {
        if (memory == null)
        {
            throw new Argon2ParameterException("Argon2 memory is required for finalization.");
        }

        if (destination.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("The Argon2 final block must contain exactly 128 words.");
        }

        destination.Clear();
        long lastBlockIndex = memory.GetBlocksPerLane() - 1L;
        int laneIndex = 0;
        while (laneIndex < memory.GetLaneCount())
        {
            Span<ulong> laneBlock = memory.GetLaneBlock(laneIndex, lastBlockIndex);
            int wordIndex = 0;
            while (wordIndex < destination.Length)
            {
                destination[wordIndex] = destination[wordIndex] ^ laneBlock[wordIndex];
                wordIndex = wordIndex + 1;
            }

            laneIndex = laneIndex + 1;
        }
    }

    internal static void SerializeFinalBlock(
        ReadOnlySpan<ulong> finalBlock,
        Span<byte> destination)
    {
        if (finalBlock.Length != Argon2Limits.WordsPerBlock
            || destination.Length != Argon2Limits.BlockSizeInBytes)
        {
            throw new Argon2ParameterException("Argon2 final-block serialization requires exactly 128 words and 1024 bytes.");
        }

        int wordIndex = 0;
        while (wordIndex < finalBlock.Length)
        {
            ulong value = finalBlock[wordIndex];
            int byteOffset = wordIndex * 8;
            destination[byteOffset] = (byte)(value & 0xFFUL);
            destination[byteOffset + 1] = (byte)((value >> 8) & 0xFFUL);
            destination[byteOffset + 2] = (byte)((value >> 16) & 0xFFUL);
            destination[byteOffset + 3] = (byte)((value >> 24) & 0xFFUL);
            destination[byteOffset + 4] = (byte)((value >> 32) & 0xFFUL);
            destination[byteOffset + 5] = (byte)((value >> 40) & 0xFFUL);
            destination[byteOffset + 6] = (byte)((value >> 48) & 0xFFUL);
            destination[byteOffset + 7] = (byte)((value >> 56) & 0xFFUL);
            wordIndex = wordIndex + 1;
        }
    }

    static void ValidateMemoryAndFinalBlock(Argon2Memory memory, ulong[] finalBlock)
    {
        if (memory == null)
        {
            throw new Argon2ParameterException("Argon2 memory is required for finalization.");
        }

        if (finalBlock == null || finalBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("The Argon2 final block must contain exactly 128 words.");
        }
    }
}
