using System;

namespace email;
/// <summary>
/// Describes the outcome of a address format operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailAddressFormatResult : IEmailAddressFormatResultContract
{
    byte[] bytes;
    EmailIssueList issues;
    int foldCount;
    int finalLineColumn;
    /// <summary>
    /// Creates a new address format result.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "foldCount">The number of items or bytes to process.</param>
    /// <param name = "finalLineColumn">The final line column used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddressFormatResult(byte[] bytes, EmailIssueList issues, int foldCount, int finalLineColumn)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (foldCount < 0)
        {
            throw new ArgumentOutOfRangeException("foldCount");
        }

        if (finalLineColumn < 0)
        {
            throw new ArgumentOutOfRangeException("finalLineColumn");
        }

        this.bytes = new byte[bytes.Length];
        bytes.CopyTo(this.bytes, 0);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.foldCount = foldCount;
        this.finalLineColumn = finalLineColumn;
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
        byte[] copy = new byte[bytes.Length];
        bytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public int GetByteCount()
    {
        return bytes.Length;
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
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    public int GetFoldCount()
    {
        return foldCount;
    }

    /// <summary>
    /// Gets the final line column.
    /// </summary>
    /// <returns>The current final line column.</returns>
    public int GetFinalLineColumn()
    {
        return finalLineColumn;
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
