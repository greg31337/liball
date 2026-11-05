using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP encryption operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpEncryptionResult : IEmailOpenPgpEncryptionResultContract
{
    byte[] mimeEntity;
    byte[] openPgpMessage;
    int recipientCount;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new openPGP encryption result.
    /// </summary>
    /// <param name = "mimeEntity">The mime entity used by the operation.</param>
    /// <param name = "openPgpMessage">The open PGP message used by the operation.</param>
    /// <param name = "recipientCount">The number of items or bytes to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpEncryptionResult(byte[] mimeEntity, byte[] openPgpMessage, int recipientCount, EmailIssueList issues)
    {
        if (mimeEntity == null || openPgpMessage == null || issues == null)
            throw new ArgumentNullException("mimeEntity");
        this.mimeEntity = new EmailOpenPgpSigningCredentialOperations().Copy(mimeEntity);
        this.openPgpMessage = new EmailOpenPgpSigningCredentialOperations().Copy(openPgpMessage);
        this.recipientCount = recipientCount;
        this.issues = new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return mimeEntity.Length > 0 && openPgpMessage.Length > 0 && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the MIME entity bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current MIME entity bytes.</returns>
    public byte[] GetMimeEntityBytes()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(mimeEntity);
    }

    /// <summary>
    /// Gets the openPGP message bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current openPGP message bytes.</returns>
    public byte[] GetOpenPgpMessageBytes()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(openPgpMessage);
    }

    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    public int GetRecipientCount()
    {
        return recipientCount;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
    }
}
