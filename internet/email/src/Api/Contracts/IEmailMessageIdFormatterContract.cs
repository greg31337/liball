using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageIdFormatter"/>.
/// </summary>
public interface IEmailMessageIdFormatterContract
{
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatWithIdentifier(EmailMessageId? identifier);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMessageIdFormatResult FormatWithIdentifierAndOptions(EmailMessageId? identifier, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Formats the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatContentIdWithIdentifier(EmailMessageId identifier);
    /// <summary>
    /// Formats the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatContentIdWithIdentifierAndOptions(EmailMessageId identifier, EmailMessageIdFormatOptions options);
    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatListWithIdentifiers(EmailMessageIdList identifiers);
    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMessageIdFormatResult FormatListWithIdentifiersAndOptions(EmailMessageIdList identifiers, EmailMessageIdFormatOptions options);
}
