using System;

namespace email;
/// <summary>
/// Describes the outcome of a disposition notification request parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDispositionNotificationRequestParseResult : IEmailDispositionNotificationRequestParseResultContract
{
    EmailDispositionNotificationRequest? request;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new disposition notification request parse result.
    /// </summary>
    /// <param name = "request">The request used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationRequestParseResult(EmailDispositionNotificationRequest? request, EmailIssueList issues)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.request = request;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the request is present or available.
    /// </summary>
    /// <returns><c>true</c> when the request is available; otherwise, <c>false</c>.</returns>
    public bool HasRequest()
    {
        return request != null;
    }

    /// <summary>
    /// Gets the request.
    /// </summary>
    /// <returns>The current request.</returns>
    public EmailDispositionNotificationRequest? GetRequest()
    {
        return request;
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
        return request != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
