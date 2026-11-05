using System;

namespace email;
/// <summary>
/// Reports the result of parsing one standard structured header value.
/// </summary>
public class EmailStandardHeaderParseResult : IEmailStandardHeaderParseResultContract
{
    EmailStandardHeaderValue? value;
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;

    /// <summary>Creates a standard header parse result.</summary>
    /// <param name = "value">The parsed value.</param>
    /// <param name = "issues">The parse issues.</param>
    /// <param name = "consumedByteCount">The consumed byte count.</param>
    /// <param name = "sourceByteCount">The source byte count.</param>
    /// <exception cref = "ArgumentNullException">Thrown when issues is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a count is invalid.</exception>
    public EmailStandardHeaderParseResult(EmailStandardHeaderValue? value, EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        if (issues == null) { throw new ArgumentNullException("issues"); }
        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount) { throw new ArgumentOutOfRangeException("consumedByteCount"); }
        this.value = value;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
    }

    /// <summary>Gets the parsed value.</summary>
    /// <returns>The parsed value, or null.</returns>
    public EmailStandardHeaderValue? GetValue() { return value; }
    /// <summary>Gets the issues.</summary>
    /// <returns>A snapshot of the issues.</returns>
    public EmailIssueList GetIssues() { EmailIssueList copy = new EmailIssueList(); copy.AddRange(issues); return copy; }
    /// <summary>Gets the consumed byte count.</summary>
    /// <returns>The consumed byte count.</returns>
    public int GetConsumedByteCount() { return consumedByteCount; }
    /// <summary>Gets the source byte count.</summary>
    /// <returns>The source byte count.</returns>
    public int GetSourceByteCount() { return sourceByteCount; }
    /// <summary>Determines whether parsing succeeded.</summary>
    /// <returns><c>true</c> when no error was reported.</returns>
    public bool IsSuccess() { return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error); }
    /// <summary>Determines whether a fatal error occurred.</summary>
    /// <returns><c>true</c> when a fatal error occurred.</returns>
    public bool HasFatalError() { return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal); }
}
