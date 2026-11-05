using System;

namespace email;
/// <summary>
/// Describes the outcome of a transfer validation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTransferValidationResult : IEmailTransferValidationResultContract
{
    EmailTransferEncodingKind kind;
    long sourceByteCount;
    long copiedByteCount;
    bool cancelled;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new transfer validation result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "copiedByteCount">The number of items or bytes to process.</param>
    /// <param name = "cancelled">Whether to enable the cancelled behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTransferValidationResult(EmailTransferEncodingKind kind, long sourceByteCount, long copiedByteCount, bool cancelled, EmailIssueList issues)
    {
        new EmailTransferEncoding().ValidateIdentityKind(kind);
        if (sourceByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("sourceByteCount");
        }

        if (copiedByteCount < 0 || copiedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("copiedByteCount");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        this.kind = kind;
        this.sourceByteCount = sourceByteCount;
        this.copiedByteCount = copiedByteCount;
        this.cancelled = cancelled;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailTransferEncodingKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the copied byte count.
    /// </summary>
    /// <returns>The current copied byte count.</returns>
    public long GetCopiedByteCount()
    {
        return copiedByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is cancelled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cancelled; otherwise, <c>false</c>.</returns>
    public bool IsCancelled()
    {
        return cancelled;
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !cancelled && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
