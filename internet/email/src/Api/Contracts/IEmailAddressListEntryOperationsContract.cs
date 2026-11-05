using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressListEntry"/>.
/// </summary>
public interface IEmailAddressListEntryOperationsContract
{
    /// <summary>
    /// Creates a mailbox configured for the requested behavior.
    /// </summary>
    /// <param name = "mailbox">The mailbox used by the operation.</param>
    /// <returns>The address list entry produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAddressListEntry CreateMailbox(EmailAddress? mailbox);
    /// <summary>
    /// Creates a group configured for the requested behavior.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <returns>The address list entry produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAddressListEntry CreateGroup(EmailAddressGroup group);
}
