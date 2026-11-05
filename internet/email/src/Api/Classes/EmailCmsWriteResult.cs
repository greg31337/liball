using System;

namespace email;
/// <summary>
/// Describes the outcome of a CMS write operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCmsWriteResult : IEmailCmsWriteResultContract
{
    byte[] bytes;
    EmailIssueList issues;
    bool normalizedFromBer;
    /// <summary>
    /// Creates a new CMS write result.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "normalizedFromBer">Whether to enable the normalized from ber behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCmsWriteResult(byte[] bytes, EmailIssueList issues, bool normalizedFromBer)
    {
        if (bytes == null || issues == null)
            throw new ArgumentNullException("bytes");
        this.bytes = Copy(bytes);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.normalizedFromBer = normalizedFromBer;
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    public byte[] GetBytes()
    {
        return Copy(bytes);
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
        return bytes.Length > 0 && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the operation or source value was normalized from ber.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasNormalizedFromBer()
    {
        return normalizedFromBer;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
