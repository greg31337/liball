using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Formats address for email output.
/// </summary>
public class EmailAddressFormatter : IEmailAddressFormatterContract
{
    internal static EmailAddressFormatResult FormatImplementationWithAddress(EmailAddress address)
    {
        return FormatImplementationWithAddressAndOptions(address, new EmailAddressFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "address">The address used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult FormatWithAddress(EmailAddress address)
    {
        return EmailAddressFormatter.FormatImplementationWithAddress(address);
    }

    internal static EmailAddressFormatResult FormatImplementationWithAddressAndOptions(EmailAddress address, EmailAddressFormatOptions options)
    {
        if (address == null)
        {
            throw new ArgumentNullException("address");
        }

        EmailAddressFormatterWorker worker = CreateWorker(options);
        worker.FormatMailbox(address);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "address">The address used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressFormatResult FormatWithAddressAndOptions(EmailAddress address, EmailAddressFormatOptions options)
    {
        return EmailAddressFormatter.FormatImplementationWithAddressAndOptions(address, options);
    }

    internal static EmailAddressFormatResult FormatGroupImplementationWithGroup(EmailAddressGroup group)
    {
        return FormatGroupImplementationWithGroupAndOptions(group, new EmailAddressFormatOptions());
    }

    /// <summary>
    /// Formats the group.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult FormatGroupWithGroup(EmailAddressGroup group)
    {
        return EmailAddressFormatter.FormatGroupImplementationWithGroup(group);
    }

    internal static EmailAddressFormatResult FormatGroupImplementationWithGroupAndOptions(EmailAddressGroup group, EmailAddressFormatOptions options)
    {
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }

        EmailAddressFormatterWorker worker = CreateWorker(options);
        worker.FormatGroup(group);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the group.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressFormatResult FormatGroupWithGroupAndOptions(EmailAddressGroup group, EmailAddressFormatOptions options)
    {
        return EmailAddressFormatter.FormatGroupImplementationWithGroupAndOptions(group, options);
    }

    internal static EmailAddressFormatResult FormatListImplementationWithList(EmailAddressList list)
    {
        return FormatListImplementationWithListAndOptions(list, new EmailAddressFormatOptions());
    }

    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "list">The list used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult FormatListWithList(EmailAddressList list)
    {
        return EmailAddressFormatter.FormatListImplementationWithList(list);
    }

    internal static EmailAddressFormatResult FormatListImplementationWithListAndOptions(EmailAddressList list, EmailAddressFormatOptions options)
    {
        if (list == null)
        {
            throw new ArgumentNullException("list");
        }

        EmailAddressFormatterWorker worker = CreateWorker(options);
        worker.FormatList(list);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "list">The list used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddressFormatResult FormatListWithListAndOptions(EmailAddressList list, EmailAddressFormatOptions options)
    {
        return EmailAddressFormatter.FormatListImplementationWithListAndOptions(list, options);
    }

    static EmailAddressFormatterWorker CreateWorker(EmailAddressFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return new EmailAddressFormatterWorker(options.Clone());
    }
}
