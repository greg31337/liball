using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailParseResult"/>.
/// </summary>
public interface IEmailParseResultContract
{
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    long GetConsumedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
