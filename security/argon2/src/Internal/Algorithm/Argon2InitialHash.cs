using System;
using System.Security.Cryptography;

namespace argon2;

internal static class Argon2InitialHash
{
    internal static void ComputeHash(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> associatedData,
        Argon2Parameters parameters,
        int tagLength,
        Span<byte> destination)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);
        Argon2ParameterValidator.ValidateInputLengths(
            password,
            salt,
            secret,
            associatedData,
            tagLength);

        if (destination.Length != Argon2Limits.PrehashDigestLength)
        {
            throw new Argon2ParameterException("Argon2 initial-hash destination must be exactly 64 bytes.");
        }

        byte[] littleEndianValue = new byte[4];
        Blake2b hash = new Blake2b(Argon2Limits.PrehashDigestLength, ReadOnlySpan<byte>.Empty);

        UpdateLittleEndian32(hash, littleEndianValue, (uint)parameters.DegreeOfParallelism);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)tagLength);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)parameters.MemorySizeKiB);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)parameters.Iterations);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)parameters.Version);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)parameters.Variant);

        UpdateLittleEndian32(hash, littleEndianValue, (uint)password.Length);
        hash.Update(password);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)salt.Length);
        hash.Update(salt);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)secret.Length);
        hash.Update(secret);
        UpdateLittleEndian32(hash, littleEndianValue, (uint)associatedData.Length);
        hash.Update(associatedData);

        hash.Finish(destination);
        CryptographicOperations.ZeroMemory(littleEndianValue);
    }

    internal static void ExpandFirstBlocksForLane(
        ReadOnlySpan<byte> initialHash,
        int laneIndex,
        Span<byte> firstBlock,
        Span<byte> secondBlock)
    {
        if (initialHash.Length != Argon2Limits.PrehashDigestLength)
        {
            throw new Argon2ParameterException("Argon2 initial hash must be exactly 64 bytes.");
        }

        if (laneIndex < 0 || laneIndex >= Argon2Limits.MaximumDegreeOfParallelism)
        {
            throw new Argon2ParameterException("Argon2 lane index is outside the supported range.");
        }

        if (firstBlock.Length != Argon2Limits.BlockSizeInBytes
            || secondBlock.Length != Argon2Limits.BlockSizeInBytes)
        {
            throw new Argon2ParameterException("Argon2 initial block destinations must be exactly 1024 bytes.");
        }

        if (firstBlock.Overlaps(secondBlock))
        {
            throw new Argon2ParameterException("Argon2 initial block destinations must not overlap.");
        }

        byte[] seed = new byte[Argon2Limits.PrehashSeedLength];
        initialHash.CopyTo(new Span<byte>(seed, 0, Argon2Limits.PrehashDigestLength));
        WriteLittleEndian32(seed, Argon2Limits.PrehashDigestLength, 0U);
        WriteLittleEndian32(seed, Argon2Limits.PrehashDigestLength + 4, (uint)laneIndex);
        Blake2bLong.ComputeHash(seed, firstBlock);

        WriteLittleEndian32(seed, Argon2Limits.PrehashDigestLength, 1U);
        Blake2bLong.ComputeHash(seed, secondBlock);
        CryptographicOperations.ZeroMemory(seed);
    }

    static void UpdateLittleEndian32(Blake2b hash, byte[] destination, uint value)
    {
        destination[0] = (byte)(value & 0xFFU);
        destination[1] = (byte)((value >> 8) & 0xFFU);
        destination[2] = (byte)((value >> 16) & 0xFFU);
        destination[3] = (byte)((value >> 24) & 0xFFU);
        hash.Update(destination);
    }

    static void WriteLittleEndian32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value & 0xFFU);
        destination[offset + 1] = (byte)((value >> 8) & 0xFFU);
        destination[offset + 2] = (byte)((value >> 16) & 0xFFU);
        destination[offset + 3] = (byte)((value >> 24) & 0xFFU);
    }
}
