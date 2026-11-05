using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Specifies the supported openPGP verification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpVerificationStatus
{
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 0,
    /// <summary>
    /// Represents the malformed choice or state.
    /// </summary>
    Malformed = 1,
    /// <summary>
    /// Represents the key not found choice or state.
    /// </summary>
    KeyNotFound = 2,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 3,
    /// <summary>
    /// Represents the policy rejected choice or state.
    /// </summary>
    PolicyRejected = 4,
    /// <summary>
    /// Represents the signature mismatch choice or state.
    /// </summary>
    SignatureMismatch = 5
}
