namespace argon2;

/// <summary>
/// Defines encoding and decoding of Argon2 PHC strings.
/// </summary>
public interface IArgon2EncodedHashCodec
{
    /// <summary>
    /// Encodes decoded hash components in canonical PHC form.
    /// </summary>
    /// <param name="hash">The decoded hash components.</param>
    /// <returns>The canonical encoded hash.</returns>
    string EncodeHash(Argon2EncodedHash hash);

    /// <summary>
    /// Decodes an Argon2 encoded hash.
    /// </summary>
    /// <param name="encodedHash">The encoded hash to decode.</param>
    /// <returns>The decoded hash components.</returns>
    /// <exception cref="Argon2FormatException">The encoded hash is malformed or unsupported.</exception>
    Argon2EncodedHash DecodeHash(string encodedHash);

    /// <summary>
    /// Inspects an encoded hash without throwing for malformed input.
    /// </summary>
    /// <param name="encodedHash">The encoded hash to inspect.</param>
    /// <returns>The decode outcome.</returns>
    Argon2DecodeResult DecodeHashWithoutException(string encodedHash);

    /// <summary>
    /// Calculates the exact canonical encoded length of decoded hash components.
    /// </summary>
    /// <param name="hash">The decoded hash components.</param>
    /// <returns>The canonical encoded length in characters.</returns>
    int GetEncodedHashLength(Argon2EncodedHash hash);
}
