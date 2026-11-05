using System;

namespace email;
/// <summary>
/// Specifies the supported S/MIME signature values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeSignatureAlgorithm
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the RSA pkcs 1 choice or state.
    /// </summary>
    RsaPkcs1 = 1,
    /// <summary>
    /// Represents the ECDSA choice or state.
    /// </summary>
    Ecdsa = 2
}
