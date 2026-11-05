using System;
using System.Collections.Generic;

namespace random;

/// <summary>
/// Provides reproducible non-cryptographic byte and text generation from a deterministic generator.
/// </summary>
/// <remarks>
/// These helpers preserve deterministic generator state and are not suitable for secrets, credentials,
/// session identifiers, or security tokens. Use <see cref="SecureRandomGenerator"/> for those purposes.
/// </remarks>
internal static class RandomStrings
{
    /// <summary>Returns reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="length">The nonnegative byte count.</param>
    /// <returns>A byte array of exactly the requested length.</returns>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null or the length is negative.</exception>
    public static byte[] Bytes(IRandomSource generator, int length)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(length, "length");
        byte[] bytes = new byte[length];
        generator.FillBytes(bytes.AsSpan());
        return bytes;
    }

    /// <summary>Fills a destination with reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="destination">The destination bytes.</param>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static void FillBytes(IRandomSource generator, Span<byte> destination)
    {
        Guard.NotNull(generator, "generator");
        generator.FillBytes(destination);
    }

    /// <summary>Returns an uppercase hexadecimal encoding of reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="byteCount">The nonnegative source-byte count.</param>
    /// <returns>Exactly two hexadecimal characters per source byte.</returns>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the byte count is negative, or the result would be too large.</exception>
    public static string Hex(IRandomSource generator, int byteCount)
    {
        Guard.NotNull(generator, "generator");
        SecureToken.HexLength(byteCount);
        return SecureToken.EncodeHex(Bytes(generator, byteCount));
    }

    /// <summary>Returns an unpadded RFC 4648 base32 encoding of reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="byteCount">The nonnegative source-byte count.</param>
    /// <returns>An uppercase base32 string without trailing padding.</returns>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the byte count is negative, or the result would be too large.</exception>
    public static string Base32(IRandomSource generator, int byteCount)
    {
        Guard.NotNull(generator, "generator");
        SecureToken.Base32Length(byteCount);
        return SecureToken.EncodeBase32(Bytes(generator, byteCount));
    }

    /// <summary>Returns a padded RFC 4648 base64 encoding of reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="byteCount">The nonnegative source-byte count.</param>
    /// <returns>A standard base64 string including required trailing padding.</returns>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the byte count is negative, or the result would be too large.</exception>
    public static string Base64(IRandomSource generator, int byteCount)
    {
        Guard.NotNull(generator, "generator");
        SecureToken.Base64Length(byteCount);
        return SecureToken.EncodeBase64(Bytes(generator, byteCount));
    }

    /// <summary>Returns an unpadded RFC 4648 base64url encoding of reproducible random bytes.</summary>
    /// <param name="generator">The deterministic generator that supplies the bytes.</param>
    /// <param name="byteCount">The nonnegative source-byte count.</param>
    /// <returns>A URL-safe base64 string without trailing padding.</returns>
    /// <remarks>This method is deterministic and must not be used to create secrets or security tokens.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the byte count is negative, or the result would be too large.</exception>
    public static string Base64Url(IRandomSource generator, int byteCount)
    {
        Guard.NotNull(generator, "generator");
        SecureToken.Base64UrlLength(byteCount);
        return SecureToken.EncodeBase64Url(Bytes(generator, byteCount));
    }

    /// <summary>Returns reproducible independent selections from a caller-provided alphabet.</summary>
    /// <param name="generator">The deterministic generator that supplies selection indexes.</param>
    /// <param name="alphabet">A nonempty sequence of distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="length">The nonnegative output character count.</param>
    /// <returns>A string containing exactly the requested number of characters.</returns>
    /// <remarks>Selection is unbiased but non-cryptographic. Use <see cref="SecureRandomGenerator.String"/> for secrets.</remarks>
    /// <exception cref="RandomArgumentException">The generator or alphabet is null, the alphabet is empty, duplicated, or contains a surrogate, or length is negative.</exception>
    public static string String(IRandomSource generator, string alphabet, int length)
    {
        Guard.NotNull(generator, "generator");
        ValidateAlphabet(alphabet);
        Guard.Count(length, "length");
        char[] result = new char[length];
        FillCharacters(generator, alphabet, result);
        return new string(result);
    }

    /// <summary>Fills a destination with reproducible independent selections from an alphabet.</summary>
    /// <param name="generator">The deterministic generator that supplies selection indexes.</param>
    /// <param name="alphabet">A nonempty sequence of distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="destination">The destination characters.</param>
    /// <remarks>Selection is unbiased but non-cryptographic. Use <see cref="SecureRandomGenerator.String"/> for secrets.</remarks>
    /// <exception cref="RandomArgumentException">The generator or alphabet is null, or the alphabet is empty, duplicated, or contains a surrogate.</exception>
    public static void FillCharacters(IRandomSource generator, string alphabet, Span<char> destination)
    {
        Guard.NotNull(generator, "generator");
        int alphabetLength = ValidateAlphabet(alphabet);
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = alphabet[(int)IntegerUniform.NextUInt32(generator, (uint)alphabetLength)];
            index++;
        }
    }

    static int ValidateAlphabet(string alphabet)
    {
        Guard.NotNull(alphabet, "alphabet");
        if (alphabet.Length == 0)
        {
            throw RandomArgumentException.Create("alphabet", "The alphabet must not be empty.");
        }

        HashSet<char> characters = new HashSet<char>();
        int index = 0;
        while (index < alphabet.Length)
        {
            char character = alphabet[index];
            if (char.IsSurrogate(character))
            {
                throw RandomArgumentException.Create("alphabet", "The alphabet must not contain UTF-16 surrogate code units.");
            }

            if (!characters.Add(character))
            {
                throw RandomArgumentException.Create("alphabet", "The alphabet must not contain duplicate characters.");
            }

            index++;
        }

        return alphabet.Length;
    }
}
