using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP decryption operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpDecryptionResult : IDisposable, IEmailOpenPgpDecryptionResultContract
{
    EmailOpenPgpDecryptionStatus status;
    byte[]? content;
    byte[] recipientIdentifier;
    bool authenticated;
    EmailIssueList issues;
    bool disposed;
    /// <summary>
    /// Creates a new openPGP decryption result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipientIdentifier">The recipient identifier used by the operation.</param>
    /// <param name = "authenticated">Whether to enable the authenticated behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpDecryptionResult(EmailOpenPgpDecryptionStatus status, byte[]? content, byte[] recipientIdentifier, bool authenticated, EmailIssueList issues)
    {
        if (recipientIdentifier == null || issues == null)
            throw new ArgumentNullException("recipientIdentifier");
        if (status == EmailOpenPgpDecryptionStatus.Success && content == null || status != EmailOpenPgpDecryptionStatus.Success && content != null)
            throw new ArgumentException("Only successful decryption results can expose plaintext.", "content");
        this.status = status;
        this.content = content == null ? null : new EmailOpenPgpSigningCredentialOperations().Copy(content);
        this.recipientIdentifier = new EmailOpenPgpSigningCredentialOperations().Copy(recipientIdentifier);
        this.authenticated = authenticated;
        this.issues = new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
        disposed = false;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !disposed && status == EmailOpenPgpDecryptionStatus.Success && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailOpenPgpDecryptionStatus GetStatus()
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
        return content == null ? null : new EmailOpenPgpSigningCredentialOperations().Copy(content);
    }

    /// <summary>
    /// Gets the recipient identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipient identifier.</returns>
    public byte[] GetRecipientIdentifier()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(recipientIdentifier);
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
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
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
            throw new ObjectDisposedException("EmailOpenPgpDecryptionResult");
    }
}
