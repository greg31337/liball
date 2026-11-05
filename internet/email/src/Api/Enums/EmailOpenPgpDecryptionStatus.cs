using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Specifies the supported openPGP decryption values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpDecryptionStatus
{
    /// <summary>
    /// Represents the success choice or state.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Represents the malformed choice or state.
    /// </summary>
    Malformed = 1,
    /// <summary>
    /// Represents the recipient not found choice or state.
    /// </summary>
    RecipientNotFound = 2,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 3,
    /// <summary>
    /// Represents the key unwrap failed choice or state.
    /// </summary>
    KeyUnwrapFailed = 4,
    /// <summary>
    /// Represents the authentication failed choice or state.
    /// </summary>
    AuthenticationFailed = 5
}
