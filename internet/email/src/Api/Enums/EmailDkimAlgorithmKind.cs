using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported DKIM algorithm values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimAlgorithmKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the RSA sha 1 choice or state.
    /// </summary>
    RsaSha1 = 1,
    /// <summary>
    /// Represents the RSA sha 256 choice or state.
    /// </summary>
    RsaSha256 = 2,
    /// <summary>
    /// Represents the ed 25519 sha 256 choice or state.
    /// </summary>
    Ed25519Sha256 = 3,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 4
}
