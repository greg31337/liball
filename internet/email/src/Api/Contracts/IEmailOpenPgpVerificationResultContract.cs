using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpVerificationResult"/>.
/// </summary>
public interface IEmailOpenPgpVerificationResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailOpenPgpVerificationStatus GetStatus();
    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    EmailOpenPgpSignedFormat GetFormat();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    byte[] GetContent();
    /// <summary>
    /// Gets the signer identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signer identifier.</returns>
    byte[] GetSignerIdentifier();
    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    int GetHashAlgorithm();
    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    int GetPublicKeyAlgorithm();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
