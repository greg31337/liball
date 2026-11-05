using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressParseResult"/>.
/// </summary>
public interface IEmailAddressParseResultContract
{
    /// <summary>
    /// Gets the address list.
    /// </summary>
    /// <returns>The current address list.</returns>
    EmailAddressList GetAddressList();
    /// <summary>
    /// Determines whether the mailbox is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mailbox is available; otherwise, <c>false</c>.</returns>
    bool HasMailbox();
    /// <summary>
    /// Gets the mailbox.
    /// </summary>
    /// <returns>The current mailbox.</returns>
    EmailAddress? GetMailbox();
    /// <summary>
    /// Determines whether the group is present or available.
    /// </summary>
    /// <returns><c>true</c> when the group is available; otherwise, <c>false</c>.</returns>
    bool HasGroup();
    /// <summary>
    /// Gets the group.
    /// </summary>
    /// <returns>The current group.</returns>
    EmailAddressGroup? GetGroup();
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
