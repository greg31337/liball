using System;

namespace email;
/// <summary>
/// Represents an address list entry.
/// </summary>
public class EmailAddressListEntry : IEmailAddressListEntryContract
{
    EmailAddressListEntryKind kind;
    EmailAddress? mailbox;
    EmailAddressGroup? group;
    EmailAddressListEntry(EmailAddressListEntryKind kind, EmailAddress? mailbox, EmailAddressGroup? group)
    {
        this.kind = kind;
        this.mailbox = mailbox;
        this.group = group;
    }

    internal static EmailAddressListEntry CreateMailboxImplementation(EmailAddress? mailbox)
    {
        if (mailbox == null)
        {
            throw new ArgumentNullException("mailbox");
        }

        return new EmailAddressListEntry(EmailAddressListEntryKind.Mailbox, mailbox, null);
    }

    internal static EmailAddressListEntry CreateGroupImplementation(EmailAddressGroup group)
    {
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }

        return new EmailAddressListEntry(EmailAddressListEntryKind.Group, null, group);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailAddressListEntryKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the current value or state is mailbox.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailbox; otherwise, <c>false</c>.</returns>
    public bool IsMailbox()
    {
        return kind == EmailAddressListEntryKind.Mailbox;
    }

    /// <summary>
    /// Determines whether the current value or state is group.
    /// </summary>
    /// <returns><c>true</c> when the value or state is group; otherwise, <c>false</c>.</returns>
    public bool IsGroup()
    {
        return kind == EmailAddressListEntryKind.Group;
    }

    /// <summary>
    /// Gets the mailbox.
    /// </summary>
    /// <returns>The current mailbox.</returns>
    public EmailAddress? GetMailbox()
    {
        return mailbox;
    }

    /// <summary>
    /// Gets the group.
    /// </summary>
    /// <returns>The current group.</returns>
    public EmailAddressGroup? GetGroup()
    {
        return group;
    }
}
