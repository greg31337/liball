using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a protected header construction operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailProtectedHeaderConstructionResult : IEmailProtectedHeaderConstructionResultContract
{
    MimeEntity payload;
    EmailHeaders outerHeaders;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new protected header construction result.
    /// </summary>
    /// <param name = "payload">The payload used by the operation.</param>
    /// <param name = "outerHeaders">The outer headers used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    public EmailProtectedHeaderConstructionResult(MimeEntity payload, EmailHeaders outerHeaders, EmailIssueList issues)
    {
        this.payload = payload;
        this.outerHeaders = outerHeaders.Clone();
        this.issues = CopyIssues(issues);
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
    /// Gets the payload.
    /// </summary>
    /// <returns>The current payload.</returns>
    public MimeEntity GetPayload()
    {
        return payload.Clone();
    }

    /// <summary>
    /// Gets the outer headers.
    /// </summary>
    /// <returns>The current outer headers.</returns>
    public EmailHeaders GetOuterHeaders()
    {
        return outerHeaders.Clone();
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return CopyIssues(issues);
    }

    static EmailIssueList CopyIssues(EmailIssueList source)
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(source);
        return copy;
    }
}
