using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeEncryptionResult"/>.
/// </summary>
public interface IEmailSmimeEncryptionResultContract
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
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the content algorithm.
    /// </summary>
    /// <returns>The current content algorithm.</returns>
    EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm();
    /// <summary>
    /// Gets the key transport algorithm.
    /// </summary>
    /// <returns>The current key transport algorithm.</returns>
    EmailSmimeKeyTransportAlgorithm GetKeyTransportAlgorithm();
    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    int GetRecipientCount();
    /// <summary>
    /// Determines whether the current value or state is authenticated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is authenticated; otherwise, <c>false</c>.</returns>
    bool IsAuthenticated();
}
