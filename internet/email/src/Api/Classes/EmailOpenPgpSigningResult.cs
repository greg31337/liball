using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP signing operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpSigningResult : IEmailOpenPgpSigningResultContract
{
    byte[] mimeEntity;
    byte[] signature;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new openPGP signing result.
    /// </summary>
    /// <param name = "mimeEntity">The mime entity used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpSigningResult(byte[] mimeEntity, byte[] signature, EmailIssueList issues)
    {
        if (mimeEntity == null || signature == null || issues == null)
            throw new ArgumentNullException("mimeEntity");
        this.mimeEntity = new EmailOpenPgpSigningCredentialOperations().Copy(mimeEntity);
        this.signature = new EmailOpenPgpSigningCredentialOperations().Copy(signature);
        this.issues = CopyIssuesImplementation(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return mimeEntity.Length > 0 && signature.Length > 0 && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
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
    /// Gets the signature packet bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signature packet bytes.</returns>
    public byte[] GetSignaturePacketBytes()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(signature);
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return CopyIssuesImplementation(issues);
    }

    internal static EmailIssueList CopyIssuesImplementation(EmailIssueList value)
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(value);
        return copy;
    }
}
