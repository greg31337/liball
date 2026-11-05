using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSecurityMultipartParseResult"/>.
/// </summary>
public interface IEmailSecurityMultipartParseResultContract
{
    /// <summary>
    /// Determines whether the current value or state is recognized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is recognized; otherwise, <c>false</c>.</returns>
    bool IsRecognized();
    /// <summary>
    /// Determines whether the security multipart is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security multipart is available; otherwise, <c>false</c>.</returns>
    bool HasSecurityMultipart();
    /// <summary>
    /// Gets the security multipart.
    /// </summary>
    /// <returns>The current security multipart.</returns>
    EmailSecurityMultipart? GetSecurityMultipart();
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
