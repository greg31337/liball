using System;

namespace argon2;

internal static class Argon2Compression
{
    internal static ulong BlaMka(ulong left, ulong right)
    {
        ulong lowerMask = 0xFFFFFFFFUL;
        ulong product = (left & lowerMask) * (right & lowerMask);
        ulong doubledProduct = product << 1;
        return ModularArithmetic.Add64(
            ModularArithmetic.Add64(left, right),
            doubledProduct);
    }

    internal static void XorBlocks(
        ReadOnlySpan<ulong> left,
        ReadOnlySpan<ulong> right,
        Span<ulong> destination)
    {
        if (left.Length != Argon2Limits.WordsPerBlock
            || right.Length != Argon2Limits.WordsPerBlock
            || destination.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 XOR operands must each contain exactly 128 words.");
        }

        ulong[] result = new ulong[Argon2Limits.WordsPerBlock];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = left[index] ^ right[index];
            index = index + 1;
        }

        new ReadOnlySpan<ulong>(result).CopyTo(destination);
        Array.Clear(result);
    }

    internal static void PermuteBlock(Span<ulong> block)
    {
        if (block.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 permutation input must contain exactly 128 words.");
        }

        int row = 0;
        while (row < 8)
        {
            ApplyRowRound(block, row * 16);
            row = row + 1;
        }

        int column = 0;
        while (column < 8)
        {
            ApplyColumnRound(block, column * 2);
            column = column + 1;
        }
    }

    internal static void CompressBlock(
        ReadOnlySpan<ulong> previousBlock,
        ReadOnlySpan<ulong> referenceBlock,
        Span<ulong> destination)
    {
        CompressCore(
            previousBlock,
            referenceBlock,
            ReadOnlySpan<ulong>.Empty,
            false,
            destination);
    }

    internal static void CompressBlockWithXor(
        ReadOnlySpan<ulong> previousBlock,
        ReadOnlySpan<ulong> referenceBlock,
        ReadOnlySpan<ulong> existingBlock,
        Span<ulong> destination)
    {
        CompressCore(previousBlock, referenceBlock, existingBlock, true, destination);
    }

    static void CompressCore(
        ReadOnlySpan<ulong> previousBlock,
        ReadOnlySpan<ulong> referenceBlock,
        ReadOnlySpan<ulong> existingBlock,
        bool includeExistingBlock,
        Span<ulong> destination)
    {
        if (previousBlock.Length != Argon2Limits.WordsPerBlock
            || referenceBlock.Length != Argon2Limits.WordsPerBlock
            || destination.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Argon2 compression blocks must each contain exactly 128 words.");
        }

        if (includeExistingBlock && existingBlock.Length != Argon2Limits.WordsPerBlock)
        {
            throw new Argon2ParameterException("Existing Argon2 block must contain exactly 128 words.");
        }

        ulong[] working = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] combined = new ulong[Argon2Limits.WordsPerBlock];
        int index = 0;
        while (index < working.Length)
        {
            ulong value = previousBlock[index] ^ referenceBlock[index];
            working[index] = value;
            combined[index] = value;
            if (includeExistingBlock)
            {
                combined[index] = combined[index] ^ existingBlock[index];
            }

            index = index + 1;
        }

        PermuteBlock(working);

        index = 0;
        while (index < working.Length)
        {
            destination[index] = working[index] ^ combined[index];
            index = index + 1;
        }

        Array.Clear(working);
        Array.Clear(combined);
    }

    static void ApplyRowRound(Span<ulong> block, int offset)
    {
        BlaMkaRound(block, offset, offset + 4, offset + 8, offset + 12);
        BlaMkaRound(block, offset + 1, offset + 5, offset + 9, offset + 13);
        BlaMkaRound(block, offset + 2, offset + 6, offset + 10, offset + 14);
        BlaMkaRound(block, offset + 3, offset + 7, offset + 11, offset + 15);
        BlaMkaRound(block, offset, offset + 5, offset + 10, offset + 15);
        BlaMkaRound(block, offset + 1, offset + 6, offset + 11, offset + 12);
        BlaMkaRound(block, offset + 2, offset + 7, offset + 8, offset + 13);
        BlaMkaRound(block, offset + 3, offset + 4, offset + 9, offset + 14);
    }

    static void ApplyColumnRound(Span<ulong> block, int offset)
    {
        BlaMkaRound(block, offset, offset + 32, offset + 64, offset + 96);
        BlaMkaRound(block, offset + 1, offset + 33, offset + 65, offset + 97);
        BlaMkaRound(block, offset + 16, offset + 48, offset + 80, offset + 112);
        BlaMkaRound(block, offset + 17, offset + 49, offset + 81, offset + 113);
        BlaMkaRound(block, offset, offset + 33, offset + 80, offset + 113);
        BlaMkaRound(block, offset + 1, offset + 48, offset + 81, offset + 96);
        BlaMkaRound(block, offset + 16, offset + 49, offset + 64, offset + 97);
        BlaMkaRound(block, offset + 17, offset + 32, offset + 65, offset + 112);
    }

    static void BlaMkaRound(Span<ulong> values, int a, int b, int c, int d)
    {
        values[a] = BlaMka(values[a], values[b]);
        values[d] = ModularArithmetic.RotateRight64(values[d] ^ values[a], 32);
        values[c] = BlaMka(values[c], values[d]);
        values[b] = ModularArithmetic.RotateRight64(values[b] ^ values[c], 24);
        values[a] = BlaMka(values[a], values[b]);
        values[d] = ModularArithmetic.RotateRight64(values[d] ^ values[a], 16);
        values[c] = BlaMka(values[c], values[d]);
        values[b] = ModularArithmetic.RotateRight64(values[b] ^ values[c], 63);
    }
}
