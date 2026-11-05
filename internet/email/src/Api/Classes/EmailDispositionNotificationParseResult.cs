using System;

namespace email;
/// <summary>
/// Describes the outcome of a disposition notification parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDispositionNotificationParseResult : IEmailDispositionNotificationParseResultContract
{
    EmailDispositionNotification? notification;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new disposition notification parse result.
    /// </summary>
    /// <param name = "notification">The notification used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDispositionNotificationParseResult(EmailDispositionNotification? notification, EmailIssueList issues)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.notification = notification;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the notification is present or available.
    /// </summary>
    /// <returns><c>true</c> when the notification is available; otherwise, <c>false</c>.</returns>
    public bool HasNotification()
    {
        return notification != null;
    }

    /// <summary>
    /// Gets the notification.
    /// </summary>
    /// <returns>The current notification.</returns>
    public EmailDispositionNotification? GetNotification()
    {
        return notification;
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
        return notification != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
