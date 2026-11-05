using System;

namespace argon2;

/// <summary>
/// Contains the decoded components of an Argon2 encoded hash.
/// </summary>
public class Argon2EncodedHash
{
    /// <summary>
    /// The decoded algorithm parameters.
    /// </summary>
    public Argon2Parameters Parameters = new Argon2Parameters();

    /// <summary>
    /// The decoded salt bytes.
    /// </summary>
    public byte[] Salt = Array.Empty<byte>();

    /// <summary>
    /// The decoded tag bytes.
    /// </summary>
    public byte[] Tag = Array.Empty<byte>();
}
