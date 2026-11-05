using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressFormatter"/>.
/// </summary>
public interface IEmailAddressFormatterContract
{
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "address">The address used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult FormatWithAddress(EmailAddress address);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "address">The address used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAddressFormatResult FormatWithAddressAndOptions(EmailAddress address, EmailAddressFormatOptions options);
    /// <summary>
    /// Formats the group.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult FormatGroupWithGroup(EmailAddressGroup group);
    /// <summary>
    /// Formats the group.
    /// </summary>
    /// <param name = "group">The group used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAddressFormatResult FormatGroupWithGroupAndOptions(EmailAddressGroup group, EmailAddressFormatOptions options);
    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "list">The list used by the operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult FormatListWithList(EmailAddressList list);
    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "list">The list used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailAddressFormatResult FormatListWithListAndOptions(EmailAddressList list, EmailAddressFormatOptions options);
}
