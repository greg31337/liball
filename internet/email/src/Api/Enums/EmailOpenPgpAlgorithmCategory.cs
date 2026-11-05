using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Specifies the supported openPGP algorithm category values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpAlgorithmCategory
{
    /// <summary>
    /// Represents the packet choice or state.
    /// </summary>
    Packet = 0,
    /// <summary>
    /// Represents the public key choice or state.
    /// </summary>
    PublicKey = 1,
    /// <summary>
    /// Represents the symmetric choice or state.
    /// </summary>
    Symmetric = 2,
    /// <summary>
    /// Represents the compression choice or state.
    /// </summary>
    Compression = 3,
    /// <summary>
    /// Represents the hash choice or state.
    /// </summary>
    Hash = 4,
    /// <summary>
    /// Represents the aead choice or state.
    /// </summary>
    Aead = 5,
    /// <summary>
    /// Represents the string to key choice or state.
    /// </summary>
    StringToKey = 6
}
