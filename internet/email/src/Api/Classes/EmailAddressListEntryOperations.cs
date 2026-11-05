using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailAddressListEntry"/>.
/// </summary>
public class EmailAddressListEntryOperations : IEmailAddressListEntryOperationsContract
{
    /// <summary>
    /// Creates a mailbox configured for the requested behavior.
    /// </summary>
    /// <param name = "mailbox">The mailbox used by the operation.</param>
    /// <returns>The address list entry produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressListEntry CreateMailbox(EmailAddress? mailbox)
    {
        return EmailAddressListEntry.CreateMailboxImplementation(mailbox);
    }

    /// <summary>
    /// Creates a group configured for the requested behavior.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <returns>The address list entry produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressListEntry CreateGroup(EmailAddressGroup group)
    {
        return EmailAddressListEntry.CreateGroupImplementation(group);
    }
}
