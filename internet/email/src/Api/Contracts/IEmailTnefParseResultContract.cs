using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefParseResult"/>.
/// </summary>
public interface IEmailTnefParseResultContract
{
    /// <summary>
    /// Determines whether the container is present or available.
    /// </summary>
    /// <returns><c>true</c> when the container is available; otherwise, <c>false</c>.</returns>
    bool HasContainer();
    /// <summary>
    /// Gets the container.
    /// </summary>
    /// <returns>The current container.</returns>
    EmailTnefContainer? GetContainer();
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailTnefMessage? GetMessage();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
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
