using System;

namespace argon2;

internal static class Argon2LaneFiller
{
    internal static void FillSegment(
        Argon2Parameters parameters,
        Argon2Memory memory,
        long passIndex,
        int laneIndex,
        int sliceIndex,
        Span<ulong> inputBlock,
        Span<ulong> addressBlock,
        Span<ulong> temporaryBlock)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);
        if (memory == null)
        {
            throw new Argon2ParameterException("Argon2 memory is required for segment filling.");
        }

        if (passIndex < 0L || passIndex >= parameters.Iterations)
        {
            throw new Argon2ParameterException("Argon2 pass index is outside the configured iteration count.");
        }

        if (laneIndex < 0 || laneIndex >= parameters.DegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 lane index is outside the configured lane count.");
        }

        if (sliceIndex < 0 || sliceIndex >= Argon2Limits.SynchronizationPoints)
        {
            throw new Argon2ParameterException("Argon2 slice index is outside the pass.");
        }

        if (inputBlock.Length != Argon2Limits.WordsPerBlock
            || addressBlock.Length != Argon2Limits.WordsPerBlock
            || temporaryBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 lane-filling buffers must each contain exactly 128 words.");
        }

        if (inputBlock.Overlaps(addressBlock)
            || inputBlock.Overlaps(temporaryBlock)
            || addressBlock.Overlaps(temporaryBlock))
        {
            throw new Argon2ParameterException("Argon2 lane-filling buffers must not overlap.");
        }

        long normalizedBlockCount = Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters);
        long laneLength = normalizedBlockCount / parameters.DegreeOfParallelism;
        long segmentLength = laneLength / Argon2Limits.SynchronizationPoints;
        if (memory.GetBlockCount() != normalizedBlockCount
            || memory.GetLaneCount() != parameters.DegreeOfParallelism
            || memory.GetBlocksPerLane() != laneLength
            || memory.GetSegmentLength() != segmentLength)
        {
            throw new Argon2ParameterException("Argon2 memory geometry does not match the configured parameters.");
        }

        long indexInSegment = 0L;
        if (passIndex == 0L && sliceIndex == 0)
        {
            indexInSegment = Argon2Limits.InitialBlocksPerLane;
        }

        bool includeExistingBlock = passIndex != 0L
            && parameters.Version == Argon2Version.Version13;
        while (indexInSegment < segmentLength)
        {
            long currentBlockIndex = (long)sliceIndex * segmentLength + indexInSegment;
            long previousBlockIndex = currentBlockIndex - 1L;
            if (currentBlockIndex == 0L)
            {
                previousBlockIndex = laneLength - 1L;
            }

            Span<ulong> previousBlock = memory.GetLaneBlock(laneIndex, previousBlockIndex);
            ulong pseudoRandomValue = Argon2Indexing.GetPseudoRandomValue(
                parameters,
                passIndex,
                laneIndex,
                sliceIndex,
                indexInSegment,
                previousBlock,
                inputBlock,
                addressBlock,
                temporaryBlock);
            int referenceLane = Argon2Indexing.GetReferenceLane(
                Argon2Indexing.GetJ2(pseudoRandomValue),
                parameters.DegreeOfParallelism,
                laneIndex,
                passIndex,
                sliceIndex);
            long referenceBlockIndex = Argon2Indexing.GetReferenceBlockIndex(
                Argon2Indexing.GetJ1(pseudoRandomValue),
                passIndex,
                sliceIndex,
                indexInSegment,
                referenceLane == laneIndex,
                laneLength,
                segmentLength);
            Span<ulong> referenceBlock = memory.GetLaneBlock(
                referenceLane,
                referenceBlockIndex);
            Span<ulong> currentBlock = memory.GetLaneBlock(laneIndex, currentBlockIndex);

            if (includeExistingBlock)
            {
                Argon2Compression.CompressBlockWithXor(
                    previousBlock,
                    referenceBlock,
                    currentBlock,
                    currentBlock);
            }
            else
            {
                Argon2Compression.CompressBlock(previousBlock, referenceBlock, currentBlock);
            }

            indexInSegment = indexInSegment + 1L;
        }
    }
}
