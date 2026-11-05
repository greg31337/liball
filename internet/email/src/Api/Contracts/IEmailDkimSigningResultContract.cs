using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSigningResult"/>.
/// </summary>
public interface IEmailDkimSigningResultContract
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader GetHeader();
    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    EmailDkimSignature GetSignature();
    /// <summary>
    /// Gets the body canonicalization.
    /// </summary>
    /// <returns>The current body canonicalization.</returns>
    EmailDkimBodyCanonicalizationResult GetBodyCanonicalization();
    /// <summary>
    /// Gets the header canonicalization.
    /// </summary>
    /// <returns>The current header canonicalization.</returns>
    EmailDkimHeaderCanonicalizationResult GetHeaderCanonicalization();
    /// <summary>
    /// Gets the header hash bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current header hash bytes.</returns>
    byte[] GetHeaderHashBytes();
    /// <summary>
    /// Gets the signature bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signature bytes.</returns>
    byte[] GetSignatureBytes();
    /// <summary>
    /// Gets the body hash Base64.
    /// </summary>
    /// <returns>The current body hash Base64.</returns>
    string GetBodyHashBase64();
    /// <summary>
    /// Gets the signature Base64.
    /// </summary>
    /// <returns>The current signature Base64.</returns>
    string GetSignatureBase64();
    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    long GetTimestamp();
}
