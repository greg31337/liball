using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReportParseResult"/>.
/// </summary>
public interface IEmailReportParseResultContract
{
    /// <summary>
    /// Determines whether the report is present or available.
    /// </summary>
    /// <returns><c>true</c> when the report is available; otherwise, <c>false</c>.</returns>
    bool HasReport();
    /// <summary>
    /// Gets the report.
    /// </summary>
    /// <returns>The current report.</returns>
    EmailMultipartReport? GetReport();
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
