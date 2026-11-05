using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationParseResult"/>.
/// </summary>
public interface IEmailDispositionNotificationParseResultContract
{
    /// <summary>
    /// Determines whether the notification is present or available.
    /// </summary>
    /// <returns><c>true</c> when the notification is available; otherwise, <c>false</c>.</returns>
    bool HasNotification();
    /// <summary>
    /// Gets the notification.
    /// </summary>
    /// <returns>The current notification.</returns>
    EmailDispositionNotification? GetNotification();
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
