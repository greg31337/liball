using System;

namespace argon2;

/// <summary>
/// Provides canonical encoding and strict decoding of Argon2 PHC strings.
/// </summary>
public class Argon2EncodedHashCodec : IArgon2EncodedHashCodec
{
    /// <summary>
    /// Initializes a stateless Argon2 encoded-hash codec.
    /// </summary>
    public Argon2EncodedHashCodec()
    {
    }

    /// <summary>
    /// Encodes decoded hash components in canonical PHC form.
    /// </summary>
    /// <param name="hash">The decoded hash components.</param>
    /// <returns>The canonical encoded hash.</returns>
    /// <exception cref="Argon2ParameterException">A decoded component is invalid.</exception>
    public string EncodeHash(Argon2EncodedHash hash)
    {
        return Argon2PhcFormatter.Format(hash);
    }

    /// <summary>
    /// Decodes an Argon2 encoded hash.
    /// </summary>
    /// <param name="encodedHash">The encoded hash to decode.</param>
    /// <returns>The decoded hash components.</returns>
    /// <exception cref="Argon2FormatException">The encoded hash is malformed or unsupported.</exception>
    public Argon2EncodedHash DecodeHash(string encodedHash)
    {
        Argon2DecodeResult result = DecodeHashWithoutException(encodedHash);
        Argon2EncodedHash? decodedHash = result.EncodedHash;
        if (result.Status != Argon2DecodeStatus.Success || decodedHash == null)
        {
            throw new Argon2FormatException("The Argon2 encoded hash is malformed or unsupported.");
        }

        return decodedHash;
    }

    /// <summary>
    /// Inspects an encoded hash without throwing for malformed input.
    /// </summary>
    /// <param name="encodedHash">The encoded hash to inspect.</param>
    /// <returns>The decode outcome.</returns>
    public Argon2DecodeResult DecodeHashWithoutException(string encodedHash)
    {
        if (encodedHash == null)
        {
            return new Argon2DecodeResult();
        }

        return Argon2PhcParser.Parse(encodedHash.AsSpan());
    }

    /// <summary>
    /// Calculates the exact canonical encoded length of decoded hash components.
    /// </summary>
    /// <param name="hash">The decoded hash components.</param>
    /// <returns>The canonical encoded length in characters.</returns>
    /// <exception cref="Argon2ParameterException">A decoded component is invalid.</exception>
    public int GetEncodedHashLength(Argon2EncodedHash hash)
    {
        return Argon2PhcFormatter.GetEncodedLength(hash);
    }
}
