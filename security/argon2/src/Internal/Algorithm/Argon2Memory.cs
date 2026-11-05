using System;

namespace argon2;

internal class Argon2Memory
{
    internal const int DefaultBlocksPerChunk = 65536;

    ulong[][] chunks;
    long blockCount;
    int laneCount;
    long blocksPerLane;
    long segmentLength;
    int blocksPerChunk;

    internal Argon2Memory(Argon2Parameters parameters, int blocksPerChunk)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);

        if (blocksPerChunk < 1)
        {
            throw new Argon2ParameterException("Argon2 memory chunk must contain at least one block.");
        }

        long maximumBlocksPerChunk = Array.MaxLength / Argon2Limits.WordsPerBlock;
        if (blocksPerChunk > maximumBlocksPerChunk)
        {
            throw new Argon2ParameterException("Argon2 memory chunk exceeds the managed array-length limit.");
        }

        this.blocksPerChunk = blocksPerChunk;
        blockCount = Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters);
        laneCount = parameters.DegreeOfParallelism;
        blocksPerLane = blockCount / laneCount;
        segmentLength = blocksPerLane / Argon2Limits.SynchronizationPoints;

        long chunkCountValue = blockCount / blocksPerChunk;
        if (blockCount % blocksPerChunk != 0L)
        {
            chunkCountValue = chunkCountValue + 1L;
        }

        int chunkCount = Argon2ParameterValidator.GetManagedArrayLength(chunkCountValue);
        chunks = new ulong[chunkCount][];

        long remainingBlocks = blockCount;
        int chunkIndex = 0;
        while (chunkIndex < chunks.Length)
        {
            long blocksInChunk = remainingBlocks < blocksPerChunk
                ? remainingBlocks
                : blocksPerChunk;
            long wordsInChunk = blocksInChunk * Argon2Limits.WordsPerBlock;
            int wordCount = Argon2ParameterValidator.GetManagedArrayLength(wordsInChunk);
            chunks[chunkIndex] = new ulong[wordCount];
            remainingBlocks = remainingBlocks - blocksInChunk;
            chunkIndex = chunkIndex + 1;
        }
    }

    internal long GetBlockCount()
    {
        return blockCount;
    }

    internal int GetLaneCount()
    {
        return laneCount;
    }

    internal long GetBlocksPerLane()
    {
        return blocksPerLane;
    }

    internal long GetSegmentLength()
    {
        return segmentLength;
    }

    internal int GetBlocksPerChunk()
    {
        return blocksPerChunk;
    }

    internal int GetChunkCount()
    {
        return chunks.Length;
    }

    internal Span<ulong> GetBlock(long blockIndex)
    {
        ValidateBlockIndex(blockIndex);
        long chunkIndexValue = blockIndex / blocksPerChunk;
        long blockIndexInChunk = blockIndex % blocksPerChunk;
        long wordOffsetValue = blockIndexInChunk * Argon2Limits.WordsPerBlock;
        int chunkIndex = (int)chunkIndexValue;
        int wordOffset = (int)wordOffsetValue;
        return new Span<ulong>(
            chunks[chunkIndex],
            wordOffset,
            Argon2Limits.WordsPerBlock);
    }

    internal Span<ulong> GetLaneBlock(int laneIndex, long blockIndexInLane)
    {
        ValidateLaneIndex(laneIndex);
        if (blockIndexInLane < 0L || blockIndexInLane >= blocksPerLane)
        {
            throw new Argon2ParameterException("Argon2 block index within the lane is outside memory.");
        }

        long blockIndex = (long)laneIndex * blocksPerLane + blockIndexInLane;
        return GetBlock(blockIndex);
    }

    internal long GetSegmentStartBlockIndex(int laneIndex, int sliceIndex)
    {
        ValidateLaneIndex(laneIndex);
        if (sliceIndex < 0 || sliceIndex >= Argon2Limits.SynchronizationPoints)
        {
            throw new Argon2ParameterException("Argon2 slice index is outside the pass.");
        }

        return (long)laneIndex * blocksPerLane + (long)sliceIndex * segmentLength;
    }

    internal void WriteBlockBytes(long blockIndex, ReadOnlySpan<byte> source)
    {
        if (source.Length != Argon2Limits.BlockSizeInBytes)
        {
            throw new Argon2ParameterException("Argon2 block source must be exactly 1024 bytes.");
        }

        Span<ulong> block = GetBlock(blockIndex);
        int wordIndex = 0;
        while (wordIndex < block.Length)
        {
            int byteOffset = wordIndex * 8;
            block[wordIndex] = ReadLittleEndian64(source, byteOffset);
            wordIndex = wordIndex + 1;
        }
    }

    internal void ReadBlockBytes(long blockIndex, Span<byte> destination)
    {
        if (destination.Length != Argon2Limits.BlockSizeInBytes)
        {
            throw new Argon2ParameterException("Argon2 block destination must be exactly 1024 bytes.");
        }

        Span<ulong> block = GetBlock(blockIndex);
        int wordIndex = 0;
        while (wordIndex < block.Length)
        {
            WriteLittleEndian64(block[wordIndex], destination, wordIndex * 8);
            wordIndex = wordIndex + 1;
        }
    }

    internal void Clear()
    {
        int chunkIndex = 0;
        while (chunkIndex < chunks.Length)
        {
            Array.Clear(chunks[chunkIndex]);
            chunkIndex = chunkIndex + 1;
        }
    }

    void ValidateBlockIndex(long blockIndex)
    {
        if (blockIndex < 0L || blockIndex >= blockCount)
        {
            throw new Argon2ParameterException("Argon2 block index is outside memory.");
        }
    }

    void ValidateLaneIndex(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= laneCount)
        {
            throw new Argon2ParameterException("Argon2 lane index is outside memory.");
        }
    }

    static ulong ReadLittleEndian64(ReadOnlySpan<byte> source, int offset)
    {
        return (ulong)source[offset]
            | ((ulong)source[offset + 1] << 8)
            | ((ulong)source[offset + 2] << 16)
            | ((ulong)source[offset + 3] << 24)
            | ((ulong)source[offset + 4] << 32)
            | ((ulong)source[offset + 5] << 40)
            | ((ulong)source[offset + 6] << 48)
            | ((ulong)source[offset + 7] << 56);
    }

    static void WriteLittleEndian64(ulong value, Span<byte> destination, int offset)
    {
        destination[offset] = (byte)(value & 0xFFUL);
        destination[offset + 1] = (byte)((value >> 8) & 0xFFUL);
        destination[offset + 2] = (byte)((value >> 16) & 0xFFUL);
        destination[offset + 3] = (byte)((value >> 24) & 0xFFUL);
        destination[offset + 4] = (byte)((value >> 32) & 0xFFUL);
        destination[offset + 5] = (byte)((value >> 40) & 0xFFUL);
        destination[offset + 6] = (byte)((value >> 48) & 0xFFUL);
        destination[offset + 7] = (byte)((value >> 56) & 0xFFUL);
    }
}
