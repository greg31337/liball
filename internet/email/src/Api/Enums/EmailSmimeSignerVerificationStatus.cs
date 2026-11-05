using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Specifies the supported S/MIME signer verification values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSmimeSignerVerificationStatus
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
    /// Represents the certificate not found choice or state.
    /// </summary>
    CertificateNotFound = 2,
    /// <summary>
    /// Represents the unsupported algorithm choice or state.
    /// </summary>
    UnsupportedAlgorithm = 3,
    /// <summary>
    /// Represents the digest mismatch choice or state.
    /// </summary>
    DigestMismatch = 4,
    /// <summary>
    /// Represents the signature mismatch choice or state.
    /// </summary>
    SignatureMismatch = 5,
    /// <summary>
    /// Represents the certificate chain invalid choice or state.
    /// </summary>
    CertificateChainInvalid = 6,
    /// <summary>
    /// Represents the policy rejected choice or state.
    /// </summary>
    PolicyRejected = 7
}
