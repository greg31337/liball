using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimVerificationResult"/>.
/// </summary>
public interface IEmailDkimVerificationResultContract
{
    /// <summary>
    /// Gets the signature count.
    /// </summary>
    /// <returns>The current signature count.</returns>
    int GetSignatureCount();
    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current signature.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailDkimSignatureVerificationResult GetSignature(int index);
    /// <summary>
    /// Gets the signatures.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signatures.</returns>
    List<EmailDkimSignatureVerificationResult> GetSignatures();
    /// <summary>
    /// Gets the passing signature count.
    /// </summary>
    /// <returns>The current passing signature count.</returns>
    int GetPassingSignatureCount();
    /// <summary>
    /// Determines whether the passing signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the passing signature is available; otherwise, <c>false</c>.</returns>
    bool HasPassingSignature();
}
