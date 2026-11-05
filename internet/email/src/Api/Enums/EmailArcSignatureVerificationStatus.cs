using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported ARC signature verification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailArcSignatureVerificationStatus
{
    /// <summary>
    /// Represents the not evaluated choice or state.
    /// </summary>
    NotEvaluated = 0,
    /// <summary>
    /// Represents the pass choice or state.
    /// </summary>
    Pass = 1,
    /// <summary>
    /// Represents the signature syntax error choice or state.
    /// </summary>
    SignatureSyntaxError = 2,
    /// <summary>
    /// Represents the prohibited signed header choice or state.
    /// </summary>
    ProhibitedSignedHeader = 3,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 4,
    /// <summary>
    /// Represents the unsupported canonicalization choice or state.
    /// </summary>
    UnsupportedCanonicalization = 5,
    /// <summary>
    /// Represents the unsupported query method choice or state.
    /// </summary>
    UnsupportedQueryMethod = 6,
    /// <summary>
    /// Represents the key not found choice or state.
    /// </summary>
    KeyNotFound = 7,
    /// <summary>
    /// Represents the key syntax error choice or state.
    /// </summary>
    KeySyntaxError = 8,
    /// <summary>
    /// Represents the key revoked choice or state.
    /// </summary>
    KeyRevoked = 9,
    /// <summary>
    /// Represents the key size policy violation choice or state.
    /// </summary>
    KeySizePolicyViolation = 10,
    /// <summary>
    /// Represents the key service not allowed choice or state.
    /// </summary>
    KeyServiceNotAllowed = 11,
    /// <summary>
    /// Represents the key hash not allowed choice or state.
    /// </summary>
    KeyHashNotAllowed = 12,
    /// <summary>
    /// Represents the key algorithm mismatch choice or state.
    /// </summary>
    KeyAlgorithmMismatch = 13,
    /// <summary>
    /// Represents the timestamp in future choice or state.
    /// </summary>
    TimestampInFuture = 14,
    /// <summary>
    /// Represents the signature expired choice or state.
    /// </summary>
    SignatureExpired = 15,
    /// <summary>
    /// Represents the body length invalid choice or state.
    /// </summary>
    BodyLengthInvalid = 16,
    /// <summary>
    /// Represents the body hash mismatch choice or state.
    /// </summary>
    BodyHashMismatch = 17,
    /// <summary>
    /// Represents the signature mismatch choice or state.
    /// </summary>
    SignatureMismatch = 18
}
