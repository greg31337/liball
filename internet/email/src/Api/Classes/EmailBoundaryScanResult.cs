using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a boundary scan operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailBoundaryScanResult : IEmailBoundaryScanResultContract
{
    List<EmailBoundaryDelimiter> delimiters;
    EmailIssueList issues;
    long sourceByteCount;
    bool closingBoundaryFound;
    /// <summary>
    /// Creates a new boundary scan result.
    /// </summary>
    /// <param name = "delimiters">The delimiters used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "closingBoundaryFound">Whether to enable the closing boundary found behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBoundaryScanResult(List<EmailBoundaryDelimiter>? delimiters, EmailIssueList? issues, long sourceByteCount, bool closingBoundaryFound)
    {
        if (delimiters == null || issues == null)
        {
            throw new ArgumentNullException("delimiters");
        }

        if (sourceByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("sourceByteCount");
        }

        this.delimiters = new List<EmailBoundaryDelimiter>(delimiters);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.sourceByteCount = sourceByteCount;
        this.closingBoundaryFound = closingBoundaryFound;
    }

    /// <summary>
    /// Gets the delimiter count.
    /// </summary>
    /// <returns>The current delimiter count.</returns>
    public int GetDelimiterCount()
    {
        return delimiters.Count;
    }

    /// <summary>
    /// Gets the delimiter at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested delimiter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBoundaryDelimiter GetDelimiterAt(int index)
    {
        if (index < 0 || index >= delimiters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return delimiters[index];
    }

    /// <summary>
    /// Gets the delimiters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current delimiters.</returns>
    public List<EmailBoundaryDelimiter> GetDelimiters()
    {
        return new List<EmailBoundaryDelimiter>(delimiters);
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
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Determines whether the closing boundary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the closing boundary is available; otherwise, <c>false</c>.</returns>
    public bool HasClosingBoundary()
    {
        return closingBoundaryFound;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return closingBoundaryFound;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return closingBoundaryFound && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
