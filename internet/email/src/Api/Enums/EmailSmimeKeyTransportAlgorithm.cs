using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Specifies the supported S/MIME key transport values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeKeyTransportAlgorithm
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
    /// Represents the rsa OAEP sha 256 choice or state.
    /// </summary>
    RsaOaepSha256 = 2
}
