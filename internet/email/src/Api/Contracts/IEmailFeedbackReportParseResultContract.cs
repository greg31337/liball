using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedbackReportParseResult"/>.
/// </summary>
public interface IEmailFeedbackReportParseResultContract
{
    /// <summary>
    /// Determines whether the feedback report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the feedback report is available; otherwise, <c>false</c>.</returns>
    bool HasFeedbackReport();
    /// <summary>
    /// Gets the feedback report.
    /// </summary>
    /// <returns>The current feedback report.</returns>
    EmailFeedbackReport? GetFeedbackReport();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
