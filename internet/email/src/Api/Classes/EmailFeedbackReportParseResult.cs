using System;

namespace email;
/// <summary>
/// Describes the outcome of a feedback report parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailFeedbackReportParseResult : IEmailFeedbackReportParseResultContract
{
    EmailFeedbackReport? report;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new feedback report parse result.
    /// </summary>
    /// <param name = "report">The report used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFeedbackReportParseResult(EmailFeedbackReport? report, EmailIssueList issues)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.report = report;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the feedback report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the feedback report is available; otherwise, <c>false</c>.</returns>
    public bool HasFeedbackReport()
    {
        return report != null;
    }

    /// <summary>
    /// Gets the feedback report.
    /// </summary>
    /// <returns>The current feedback report.</returns>
    public EmailFeedbackReport? GetFeedbackReport()
    {
        return report;
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
        return report != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
