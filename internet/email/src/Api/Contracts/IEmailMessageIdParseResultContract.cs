using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageIdParseResult"/>.
/// </summary>
public interface IEmailMessageIdParseResultContract
{
    /// <summary>
    /// Gets the identifiers.
    /// </summary>
    /// <returns>The current identifiers.</returns>
    EmailMessageIdList GetIdentifiers();
    /// <summary>
    /// Determines whether the single identifier is present or available.
    /// </summary>
    /// <returns><c>true</c> when the single identifier is available; otherwise, <c>false</c>.</returns>
    bool HasSingleIdentifier();
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <returns>The current identifier.</returns>
    EmailMessageId? GetIdentifier();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    int GetConsumedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    int GetSourceByteCount();
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
