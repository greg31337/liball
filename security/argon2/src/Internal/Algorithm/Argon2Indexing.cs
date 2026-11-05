using System;

namespace argon2;

internal static class Argon2Indexing
{
    internal static void InitializeAddressInputBlock(
        Argon2Parameters parameters,
        long passIndex,
        int laneIndex,
        int sliceIndex,
        Span<ulong> inputBlock)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);
        ValidatePassAndSlice(passIndex, sliceIndex);

        if (parameters.Variant == Argon2Variant.D)
        {
            throw new Argon2ParameterException("Data-independent addressing is available only for Argon2i and Argon2id.");
        }

        if (passIndex >= parameters.Iterations)
        {
            throw new Argon2ParameterException("Argon2 pass index is outside the configured iteration count.");
        }

        if (laneIndex < 0 || laneIndex >= parameters.DegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 lane index is outside the configured lane count.");
        }

        if (inputBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 address input must contain exactly 128 words.");
        }

        inputBlock.Clear();
        inputBlock[0] = (ulong)passIndex;
        inputBlock[1] = (ulong)laneIndex;
        inputBlock[2] = (ulong)sliceIndex;
        inputBlock[3] = (ulong)Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters);
        inputBlock[4] = (ulong)parameters.Iterations;
        inputBlock[5] = (ulong)parameters.Variant;
    }

    internal static void GenerateNextAddressBlock(
        Span<ulong> inputBlock,
        Span<ulong> addressBlock,
        Span<ulong> temporaryBlock)
    {
        if (inputBlock.Length != Argon2Limits.WordsPerBlock
            || addressBlock.Length != Argon2Limits.WordsPerBlock
            || temporaryBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 address-generation blocks must each contain exactly 128 words.");
        }

        if (inputBlock.Overlaps(addressBlock)
            || inputBlock.Overlaps(temporaryBlock)
            || addressBlock.Overlaps(temporaryBlock))
        {
            throw new Argon2ParameterException("Argon2 address-generation blocks must not overlap.");
        }

        inputBlock[6] = ModularArithmetic.Add64(inputBlock[6], 1UL);
        temporaryBlock.Clear();
        Argon2Compression.CompressBlock(temporaryBlock, inputBlock, addressBlock);
        Argon2Compression.CompressBlock(temporaryBlock, addressBlock, addressBlock);
    }

    internal static bool UsesDataIndependentAddressing(
        Argon2Variant variant,
        long passIndex,
        int sliceIndex)
    {
        ValidatePassAndSlice(passIndex, sliceIndex);

        if (variant == Argon2Variant.I)
        {
            return true;
        }

        if (variant == Argon2Variant.Id)
        {
            return passIndex == 0L
                && sliceIndex < Argon2Limits.SynchronizationPoints / 2;
        }

        if (variant == Argon2Variant.D)
        {
            return false;
        }

        throw new Argon2ParameterException("The Argon2 addressing variant is not supported.");
    }

    internal static ulong GetPseudoRandomValue(
        Argon2Parameters parameters,
        long passIndex,
        int laneIndex,
        int sliceIndex,
        long indexInSegment,
        ReadOnlySpan<ulong> previousBlock,
        Span<ulong> inputBlock,
        Span<ulong> addressBlock,
        Span<ulong> temporaryBlock)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);
        ValidatePassAndSlice(passIndex, sliceIndex);

        if (passIndex >= parameters.Iterations)
        {
            throw new Argon2ParameterException("Argon2 pass index is outside the configured iteration count.");
        }

        if (laneIndex < 0 || laneIndex >= parameters.DegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 lane index is outside the configured lane count.");
        }

        long memoryBlockCount = Argon2ParameterValidator.GetNormalizedMemoryBlockCount(parameters);
        long segmentLength = memoryBlockCount
            / parameters.DegreeOfParallelism
            / Argon2Limits.SynchronizationPoints;
        if (indexInSegment < 0L || indexInSegment >= segmentLength)
        {
            throw new Argon2ParameterException("Argon2 index is outside the current segment.");
        }

        long startingIndex = 0L;
        if (passIndex == 0L && sliceIndex == 0)
        {
            startingIndex = Argon2Limits.InitialBlocksPerLane;
            if (indexInSegment < startingIndex)
            {
                throw new Argon2ParameterException("The first two Argon2 lane blocks are initialized separately.");
            }
        }

        bool dataIndependent = UsesDataIndependentAddressing(
            parameters.Variant,
            passIndex,
            sliceIndex);
        if (!dataIndependent)
        {
            if (previousBlock.Length != Argon2Limits.WordsPerBlock)
            {
                throw new Argon2ParameterException("The previous Argon2 block must contain exactly 128 words.");
            }

            return previousBlock[0];
        }

        if (inputBlock.Length != Argon2Limits.WordsPerBlock
            || addressBlock.Length != Argon2Limits.WordsPerBlock
            || temporaryBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 address-generation blocks must each contain exactly 128 words.");
        }

        if (indexInSegment == startingIndex)
        {
            InitializeAddressInputBlock(
                parameters,
                passIndex,
                laneIndex,
                sliceIndex,
                inputBlock);
            GenerateNextAddressBlock(inputBlock, addressBlock, temporaryBlock);
        }
        else if (indexInSegment % Argon2Limits.WordsPerBlock == 0L)
        {
            GenerateNextAddressBlock(inputBlock, addressBlock, temporaryBlock);
        }

        int addressIndex = (int)(indexInSegment % Argon2Limits.WordsPerBlock);
        return addressBlock[addressIndex];
    }

    internal static uint GetJ1(ulong pseudoRandomValue)
    {
        return (uint)(pseudoRandomValue & 0xFFFFFFFFUL);
    }

    internal static uint GetJ2(ulong pseudoRandomValue)
    {
        return (uint)((pseudoRandomValue >> 32) & 0xFFFFFFFFUL);
    }

    internal static int GetReferenceLane(
        uint j2,
        int laneCount,
        int currentLane,
        long passIndex,
        int sliceIndex)
    {
        if (laneCount < Argon2Limits.MinimumDegreeOfParallelism
            || laneCount > Argon2Limits.MaximumDegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 lane count is outside the supported range.");
        }

        if (currentLane < 0 || currentLane >= laneCount)
        {
            throw new Argon2ParameterException("Current Argon2 lane is outside the lane count.");
        }

        ValidatePassAndSlice(passIndex, sliceIndex);
        if (passIndex == 0L && sliceIndex == 0)
        {
            return currentLane;
        }

        return (int)(j2 % (uint)laneCount);
    }

    internal static long GetReferenceBlockIndex(
        uint j1,
        long passIndex,
        int sliceIndex,
        long indexInSegment,
        bool sameLane,
        long laneLength,
        long segmentLength)
    {
        ValidateReferencePosition(
            passIndex,
            sliceIndex,
            indexInSegment,
            sameLane,
            laneLength,
            segmentLength);

        long referenceAreaSize;
        if (passIndex == 0L)
        {
            if (sliceIndex == 0)
            {
                referenceAreaSize = indexInSegment - 1L;
            }
            else if (sameLane)
            {
                referenceAreaSize = (long)sliceIndex * segmentLength + indexInSegment - 1L;
            }
            else
            {
                referenceAreaSize = (long)sliceIndex * segmentLength;
                if (indexInSegment == 0L)
                {
                    referenceAreaSize = referenceAreaSize - 1L;
                }
            }
        }
        else if (sameLane)
        {
            referenceAreaSize = laneLength - segmentLength + indexInSegment - 1L;
        }
        else
        {
            referenceAreaSize = laneLength - segmentLength;
            if (indexInSegment == 0L)
            {
                referenceAreaSize = referenceAreaSize - 1L;
            }
        }

        ulong squared = (ulong)j1 * j1;
        ulong relative = squared >> 32;
        ulong scaled = ((ulong)referenceAreaSize * relative) >> 32;
        long relativePosition = referenceAreaSize - 1L - (long)scaled;

        long startPosition = 0L;
        if (passIndex != 0L && sliceIndex != Argon2Limits.SynchronizationPoints - 1)
        {
            startPosition = (long)(sliceIndex + 1) * segmentLength;
        }

        return (startPosition + relativePosition) % laneLength;
    }

    static void ValidatePassAndSlice(long passIndex, int sliceIndex)
    {
        if (passIndex < 0L || passIndex >= Argon2Limits.MaximumIterations)
        {
            throw new Argon2ParameterException("Argon2 pass index is outside the RFC range.");
        }

        if (sliceIndex < 0 || sliceIndex >= Argon2Limits.SynchronizationPoints)
        {
            throw new Argon2ParameterException("Argon2 slice index is outside the pass.");
        }
    }

    static void ValidateReferencePosition(
        long passIndex,
        int sliceIndex,
        long indexInSegment,
        bool sameLane,
        long laneLength,
        long segmentLength)
    {
        ValidatePassAndSlice(passIndex, sliceIndex);

        if (laneLength < Argon2Limits.MinimumMemorySizePerLaneKiB
            || laneLength > Argon2Limits.MaximumMemorySizeKiB
            || segmentLength < Argon2Limits.InitialBlocksPerLane
            || segmentLength > Argon2Limits.MaximumMemorySizeKiB
                / Argon2Limits.SynchronizationPoints
            || laneLength != segmentLength * Argon2Limits.SynchronizationPoints)
        {
            throw new Argon2ParameterException("Argon2 lane and segment geometry is invalid.");
        }

        if (indexInSegment < 0L || indexInSegment >= segmentLength)
        {
            throw new Argon2ParameterException("Argon2 index is outside the current segment.");
        }

        if (passIndex == 0L && sliceIndex == 0)
        {
            if (!sameLane)
            {
                throw new Argon2ParameterException("The first Argon2 slice can reference only the current lane.");
            }

            if (indexInSegment < Argon2Limits.InitialBlocksPerLane)
            {
                throw new Argon2ParameterException("The first two Argon2 lane blocks are initialized separately.");
            }
        }
    }
}
