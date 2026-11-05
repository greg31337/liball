using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDispositionNotificationRequestParseResult"/>.
/// </summary>
public interface IEmailDispositionNotificationRequestParseResultContract
{
    /// <summary>
    /// Determines whether the request is present or available.
    /// </summary>
    /// <returns><c>true</c> when the request is available; otherwise, <c>false</c>.</returns>
    bool HasRequest();
    /// <summary>
    /// Gets the request.
    /// </summary>
    /// <returns>The current request.</returns>
    EmailDispositionNotificationRequest? GetRequest();
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
