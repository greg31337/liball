using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeVerificationResult"/>.
/// </summary>
public interface IEmailSmimeVerificationResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the current value or state is detached.
    /// </summary>
    /// <returns><c>true</c> when the value or state is detached; otherwise, <c>false</c>.</returns>
    bool IsDetached();
    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    bool HasContent();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    byte[]? GetContent();
    /// <summary>
    /// Gets the signer count.
    /// </summary>
    /// <returns>The current signer count.</returns>
    int GetSignerCount();
    /// <summary>
    /// Gets the signer at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested signer.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailSmimeSignerVerificationResult GetSignerAt(int index);
    /// <summary>
    /// Gets the signers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signers.</returns>
    List<EmailSmimeSignerVerificationResult> GetSigners();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
