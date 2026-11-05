using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressListEntry"/>.
/// </summary>
public interface IEmailAddressListEntryContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailAddressListEntryKind GetKind();
    /// <summary>
    /// Determines whether the current value or state is mailbox.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailbox; otherwise, <c>false</c>.</returns>
    bool IsMailbox();
    /// <summary>
    /// Determines whether the current value or state is group.
    /// </summary>
    /// <returns><c>true</c> when the value or state is group; otherwise, <c>false</c>.</returns>
    bool IsGroup();
    /// <summary>
    /// Gets the mailbox.
    /// </summary>
    /// <returns>The current mailbox.</returns>
    EmailAddress? GetMailbox();
    /// <summary>
    /// Gets the group.
    /// </summary>
    /// <returns>The current group.</returns>
    EmailAddressGroup? GetGroup();
}
