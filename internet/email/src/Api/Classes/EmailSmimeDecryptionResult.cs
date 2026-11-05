using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME decryption operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeDecryptionResult : IDisposable, IEmailSmimeDecryptionResultContract
{
    EmailSmimeDecryptionStatus status;
    byte[]? content;
    EmailIssueList issues;
    EmailSmimeContentEncryptionAlgorithm contentAlgorithm;
    EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm;
    EmailSmimeCertificateInfo? recipientCertificate;
    bool authenticated;
    bool disposed;
    /// <summary>
    /// Creates a new S/MIME decryption result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "contentAlgorithm">The content algorithm used by the operation.</param>
    /// <param name = "keyTransportAlgorithm">The key transport algorithm used by the operation.</param>
    /// <param name = "recipientCertificate">The recipient certificate used by the operation.</param>
    /// <param name = "authenticated">Whether to enable the authenticated behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeDecryptionResult(EmailSmimeDecryptionStatus status, byte[]? content, EmailIssueList issues, EmailSmimeContentEncryptionAlgorithm contentAlgorithm, EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm, EmailSmimeCertificateInfo? recipientCertificate, bool authenticated)
    {
        if (status < EmailSmimeDecryptionStatus.Success || status > EmailSmimeDecryptionStatus.DecryptionFailed)
            throw new ArgumentOutOfRangeException("status");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (status == EmailSmimeDecryptionStatus.Success && content == null || status != EmailSmimeDecryptionStatus.Success && content != null)
            throw new ArgumentException("Only successful decryption results can contain plaintext.", "content");
        this.status = status;
        this.content = content == null ? null : Copy(content);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.contentAlgorithm = contentAlgorithm;
        this.keyTransportAlgorithm = keyTransportAlgorithm;
        this.recipientCertificate = recipientCertificate;
        this.authenticated = authenticated;
        disposed = false;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !disposed && status == EmailSmimeDecryptionStatus.Success && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailSmimeDecryptionStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    public bool HasContent()
    {
        return !disposed && content != null;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    public byte[]? GetContent()
    {
        ThrowIfDisposed();
        return content == null ? null : Copy(content);
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Gets the content algorithm.
    /// </summary>
    /// <returns>The current content algorithm.</returns>
    public EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm()
    {
        return contentAlgorithm;
    }

    /// <summary>
    /// Gets the key transport algorithm.
    /// </summary>
    /// <returns>The current key transport algorithm.</returns>
    public EmailSmimeKeyTransportAlgorithm GetKeyTransportAlgorithm()
    {
        return keyTransportAlgorithm;
    }

    /// <summary>
    /// Determines whether the recipient certificate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the recipient certificate is available; otherwise, <c>false</c>.</returns>
    public bool HasRecipientCertificate()
    {
        return recipientCertificate != null;
    }

    /// <summary>
    /// Gets the recipient certificate.
    /// </summary>
    /// <returns>The current recipient certificate.</returns>
    public EmailSmimeCertificateInfo? GetRecipientCertificate()
    {
        return recipientCertificate;
    }

    /// <summary>
    /// Determines whether the current value or state is authenticated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is authenticated; otherwise, <c>false</c>.</returns>
    public bool IsAuthenticated()
    {
        return authenticated;
    }

    /// <summary>
    /// Determines whether the current value or state is disposed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is disposed; otherwise, <c>false</c>.</returns>
    public bool IsDisposed()
    {
        return disposed;
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        if (content != null)
            CryptographicOperations.ZeroMemory(content);
        content = null;
        disposed = true;
    }

    void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException("EmailSmimeDecryptionResult");
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
