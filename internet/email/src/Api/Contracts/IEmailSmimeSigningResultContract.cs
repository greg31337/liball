using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeSigningResult"/>.
/// </summary>
public interface IEmailSmimeSigningResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the CMS bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current CMS bytes.</returns>
    byte[] GetCmsBytes();
    /// <summary>
    /// Determines whether the current value or state is detached.
    /// </summary>
    /// <returns><c>true</c> when the value or state is detached; otherwise, <c>false</c>.</returns>
    bool IsDetached();
    /// <summary>
    /// Determines whether the current value or state is opaque.
    /// </summary>
    /// <returns><c>true</c> when the value or state is opaque; otherwise, <c>false</c>.</returns>
    bool IsOpaque();
    /// <summary>
    /// Gets the signer count.
    /// </summary>
    /// <returns>The current signer count.</returns>
    int GetSignerCount();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
