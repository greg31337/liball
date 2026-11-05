using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecryptionResult"/>.
/// </summary>
public interface IEmailSmimeDecryptionResultContract
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
    EmailSmimeDecryptionStatus GetStatus();
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
    /// Determines whether the recipient certificate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the recipient certificate is available; otherwise, <c>false</c>.</returns>
    bool HasRecipientCertificate();
    /// <summary>
    /// Gets the recipient certificate.
    /// </summary>
    /// <returns>The current recipient certificate.</returns>
    EmailSmimeCertificateInfo? GetRecipientCertificate();
    /// <summary>
    /// Determines whether the current value or state is authenticated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is authenticated; otherwise, <c>false</c>.</returns>
    bool IsAuthenticated();
    /// <summary>
    /// Determines whether the current value or state is disposed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposed; otherwise, <c>false</c>.</returns>
    bool IsDisposed();
}
