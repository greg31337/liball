using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Specifies the supported S/MIME content encryption values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeContentEncryptionAlgorithm
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the aes 128 CBC choice or state.
    /// </summary>
    Aes128Cbc = 1,
    /// <summary>
    /// Represents the aes 256 CBC choice or state.
    /// </summary>
    Aes256Cbc = 2,
    /// <summary>
    /// Represents the aes 128 GCM choice or state.
    /// </summary>
    Aes128Gcm = 3,
    /// <summary>
    /// Represents the aes 256 GCM choice or state.
    /// </summary>
    Aes256Gcm = 4
}
