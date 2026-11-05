using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>
/// Implements the SHA-256 Concat KDF used by JOSE ECDH-ES key agreement.
/// </summary>
internal static class ConcatKdfOperations
{
    /// <summary>The maximum derived-key size accepted by this implementation.</summary>
    internal const int MaximumKeySizeInBits = ConcatKdf.MaximumKeySizeInBits;

    /// <summary>Derives key bytes from a shared secret and JOSE agreement parameters.</summary>
    /// <param name="sharedSecret">The raw key-agreement output.</param>
    /// <param name="algorithmId">The JOSE AlgorithmID value.</param>
    /// <param name="partyUInfo">The decoded agreement PartyUInfo value.</param>
    /// <param name="partyVInfo">The decoded agreement PartyVInfo value.</param>
    /// <param name="keySizeInBits">The requested derived-key size in bits.</param>
    /// <returns>A result containing the derived key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult DeriveKey(
        byte[] sharedSecret,
        string algorithmId,
        byte[] partyUInfo,
        byte[] partyVInfo,
        int keySizeInBits)
    {
        if (sharedSecret == null)
        {
            throw new ArgumentNullException("sharedSecret");
        }

        if (partyUInfo == null)
        {
            throw new ArgumentNullException("partyUInfo");
        }

        if (partyVInfo == null)
        {
            throw new ArgumentNullException("partyVInfo");
        }

        return DeriveKey(
            sharedSecret.AsSpan(),
            algorithmId,
            partyUInfo.AsSpan(),
            partyVInfo.AsSpan(),
            keySizeInBits);
    }

    /// <summary>Derives key bytes from shared-secret and PartyInfo spans.</summary>
    /// <param name="sharedSecret">The raw key-agreement output.</param>
    /// <param name="algorithmId">The JOSE AlgorithmID value.</param>
    /// <param name="partyUInfo">The decoded agreement PartyUInfo value.</param>
    /// <param name="partyVInfo">The decoded agreement PartyVInfo value.</param>
    /// <param name="keySizeInBits">The requested derived-key size in bits.</param>
    /// <returns>A result containing the derived key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithmId</c> is null.</exception>
    internal static TokenBytesResult DeriveKey(
        ReadOnlySpan<byte> sharedSecret,
        string algorithmId,
        ReadOnlySpan<byte> partyUInfo,
        ReadOnlySpan<byte> partyVInfo,
        int keySizeInBits)
    {
        if (algorithmId == null)
        {
            throw new ArgumentNullException("algorithmId");
        }

        if (sharedSecret.Length == 0)
        {
            return Failure(
                "invalid_shared_secret",
                "The Concat KDF shared secret must not be empty.");
        }

        if (algorithmId.Length == 0)
        {
            return Failure(
                "invalid_algorithm_id",
                "The Concat KDF AlgorithmID must not be empty.");
        }

        if (keySizeInBits <= 0
            || keySizeInBits % 8 != 0
            || keySizeInBits > MaximumKeySizeInBits)
        {
            return Failure(
                "invalid_key_size",
                "The Concat KDF key size is outside the permitted range.");
        }

        byte[] algorithmBytes = Encoding.UTF8.GetBytes(algorithmId);
        long otherInfoLength = 4L + algorithmBytes.Length
            + 4L + partyUInfo.Length
            + 4L + partyVInfo.Length
            + 4L;
        if (otherInfoLength > int.MaxValue)
        {
            CryptographicOperations.ZeroMemory(algorithmBytes);
            return Failure(
                "input_too_large",
                "The Concat KDF agreement information is too large.");
        }

        byte[] otherInfo = new byte[(int)otherInfoLength];
        int offset = 0;
        WriteUInt32(otherInfo.AsSpan(offset, 4), algorithmBytes.Length);
        offset += 4;
        algorithmBytes.CopyTo(otherInfo, offset);
        offset += algorithmBytes.Length;
        WriteUInt32(otherInfo.AsSpan(offset, 4), partyUInfo.Length);
        offset += 4;
        partyUInfo.CopyTo(otherInfo.AsSpan(offset));
        offset += partyUInfo.Length;
        WriteUInt32(otherInfo.AsSpan(offset, 4), partyVInfo.Length);
        offset += 4;
        partyVInfo.CopyTo(otherInfo.AsSpan(offset));
        offset += partyVInfo.Length;
        WriteUInt32(otherInfo.AsSpan(offset, 4), keySizeInBits);
        CryptographicOperations.ZeroMemory(algorithmBytes);

        int outputLength = keySizeInBits / 8;
        byte[] output = new byte[outputLength];
        byte[] hashInput = new byte[4 + sharedSecret.Length + otherInfo.Length];
        sharedSecret.CopyTo(hashInput.AsSpan(4));
        otherInfo.CopyTo(hashInput, 4 + sharedSecret.Length);
        int outputOffset = 0;
        int counter = 1;
        while (outputOffset < output.Length)
        {
            WriteUInt32(hashInput.AsSpan(0, 4), counter);
            byte[] digest = SHA256.HashData(hashInput);
            int remaining = output.Length - outputOffset;
            int count = remaining < digest.Length ? remaining : digest.Length;
            digest.AsSpan(0, count).CopyTo(output.AsSpan(outputOffset));
            CryptographicOperations.ZeroMemory(digest);
            outputOffset += count;
            counter += 1;
        }

        CryptographicOperations.ZeroMemory(otherInfo);
        CryptographicOperations.ZeroMemory(hashInput);
        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = output;
        return result;
    }

    static void WriteUInt32(Span<byte> destination, int value)
    {
        destination[0] = (byte)((value >> 24) & 255);
        destination[1] = (byte)((value >> 16) & 255);
        destination[2] = (byte)((value >> 8) & 255);
        destination[3] = (byte)(value & 255);
    }

    static TokenBytesResult Failure(string code, string message)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
