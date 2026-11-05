using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Specifies the supported openPGP operation values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailOpenPgpOperationStatus
{
    /// <summary>
    /// Represents the success choice or state.
    /// </summary>
    Success = 0,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 1,
    /// <summary>
    /// Represents the policy rejected choice or state.
    /// </summary>
    PolicyRejected = 2,
    /// <summary>
    /// Represents the invalid input choice or state.
    /// </summary>
    InvalidInput = 3,
    /// <summary>
    /// Represents the failed choice or state.
    /// </summary>
    Failed = 4,
    /// <summary>
    /// Represents the authentication failed choice or state.
    /// </summary>
    AuthenticationFailed = 5
}
