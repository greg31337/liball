using System;

namespace email;
/// <summary>
/// Describes the outcome of a percent decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailPercentDecodeResult : IEmailPercentDecodeResultContract
{
    byte[] decodedBytes;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new percent decode result.
    /// </summary>
    /// <param name = "decodedBytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPercentDecodeResult(byte[] decodedBytes, EmailIssueList issues)
    {
        if (decodedBytes == null)
        {
            throw new ArgumentNullException("decodedBytes");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        this.decodedBytes = new byte[decodedBytes.Length];
        decodedBytes.CopyTo(this.decodedBytes, 0);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the decoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current decoded bytes.</returns>
    public byte[] GetDecodedBytes()
    {
        byte[] copy = new byte[decodedBytes.Length];
        decodedBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedBytes.Length;
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
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
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
