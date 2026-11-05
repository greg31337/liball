using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported DKIM verification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimVerificationStatus
{
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 0,
    /// <summary>
    /// Represents the signature syntax error choice or state.
    /// </summary>
    SignatureSyntaxError = 1,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 2,
    /// <summary>
    /// Represents the unsupported canonicalization choice or state.
    /// </summary>
    UnsupportedCanonicalization = 3,
    /// <summary>
    /// Represents the unsupported query method choice or state.
    /// </summary>
    UnsupportedQueryMethod = 4,
    /// <summary>
    /// Represents the key not found choice or state.
    /// </summary>
    KeyNotFound = 5,
    /// <summary>
    /// Represents the key syntax error choice or state.
    /// </summary>
    KeySyntaxError = 6,
    /// <summary>
    /// Represents the key revoked choice or state.
    /// </summary>
    KeyRevoked = 7,
    /// <summary>
    /// Represents the key size policy violation choice or state.
    /// </summary>
    KeySizePolicyViolation = 8,
    /// <summary>
    /// Represents the key service not allowed choice or state.
    /// </summary>
    KeyServiceNotAllowed = 9,
    /// <summary>
    /// Represents the key hash not allowed choice or state.
    /// </summary>
    KeyHashNotAllowed = 10,
    /// <summary>
    /// Represents the key algorithm mismatch choice or state.
    /// </summary>
    KeyAlgorithmMismatch = 11,
    /// <summary>
    /// Represents the identity domain mismatch choice or state.
    /// </summary>
    IdentityDomainMismatch = 12,
    /// <summary>
    /// Represents the timestamp in future choice or state.
    /// </summary>
    TimestampInFuture = 13,
    /// <summary>
    /// Represents the signature expired choice or state.
    /// </summary>
    SignatureExpired = 14,
    /// <summary>
    /// Represents the body length invalid choice or state.
    /// </summary>
    BodyLengthInvalid = 15,
    /// <summary>
    /// Represents the body hash mismatch choice or state.
    /// </summary>
    BodyHashMismatch = 16,
    /// <summary>
    /// Represents the signature mismatch choice or state.
    /// </summary>
    SignatureMismatch = 17,
    /// <summary>
    /// Represents the policy rejected choice or state.
    /// </summary>
    PolicyRejected = 18
}
