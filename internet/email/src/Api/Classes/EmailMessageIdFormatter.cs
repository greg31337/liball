using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Formats message ID for email output.
/// </summary>
public class EmailMessageIdFormatter : IEmailMessageIdFormatterContract
{
    internal static EmailMessageIdFormatResult FormatImplementationWithIdentifier(EmailMessageId? identifier)
    {
        return FormatImplementationWithIdentifierAndOptions(identifier, new EmailMessageIdFormatOptions());
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatWithIdentifier(EmailMessageId? identifier)
    {
        return EmailMessageIdFormatter.FormatImplementationWithIdentifier(identifier);
    }

    internal static EmailMessageIdFormatResult FormatImplementationWithIdentifierAndOptions(EmailMessageId? identifier, EmailMessageIdFormatOptions options)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException("identifier");
        }

        EmailMessageIdFormatterWorker worker = CreateWorker(options);
        worker.FormatIdentifier(identifier);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageIdFormatResult FormatWithIdentifierAndOptions(EmailMessageId? identifier, EmailMessageIdFormatOptions options)
    {
        return EmailMessageIdFormatter.FormatImplementationWithIdentifierAndOptions(identifier, options);
    }

    internal static EmailMessageIdFormatResult FormatContentIdImplementationWithIdentifier(EmailMessageId identifier)
    {
        return FormatImplementationWithIdentifier(identifier);
    }

    /// <summary>
    /// Formats the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatContentIdWithIdentifier(EmailMessageId identifier)
    {
        return EmailMessageIdFormatter.FormatContentIdImplementationWithIdentifier(identifier);
    }

    internal static EmailMessageIdFormatResult FormatContentIdImplementationWithIdentifierAndOptions(EmailMessageId identifier, EmailMessageIdFormatOptions options)
    {
        return FormatImplementationWithIdentifierAndOptions(identifier, options);
    }

    /// <summary>
    /// Formats the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatContentIdWithIdentifierAndOptions(EmailMessageId identifier, EmailMessageIdFormatOptions options)
    {
        return EmailMessageIdFormatter.FormatContentIdImplementationWithIdentifierAndOptions(identifier, options);
    }

    internal static EmailMessageIdFormatResult FormatListImplementationWithIdentifiers(EmailMessageIdList identifiers)
    {
        return FormatListImplementationWithIdentifiersAndOptions(identifiers, new EmailMessageIdFormatOptions());
    }

    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatListWithIdentifiers(EmailMessageIdList identifiers)
    {
        return EmailMessageIdFormatter.FormatListImplementationWithIdentifiers(identifiers);
    }

    internal static EmailMessageIdFormatResult FormatListImplementationWithIdentifiersAndOptions(EmailMessageIdList identifiers, EmailMessageIdFormatOptions options)
    {
        if (identifiers == null)
        {
            throw new ArgumentNullException("identifiers");
        }

        EmailMessageIdFormatterWorker worker = CreateWorker(options);
        worker.FormatList(identifiers);
        return worker.CreateResult();
    }

    /// <summary>
    /// Formats the list.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageIdFormatResult FormatListWithIdentifiersAndOptions(EmailMessageIdList identifiers, EmailMessageIdFormatOptions options)
    {
        return EmailMessageIdFormatter.FormatListImplementationWithIdentifiersAndOptions(identifiers, options);
    }

    static EmailMessageIdFormatterWorker CreateWorker(EmailMessageIdFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return new EmailMessageIdFormatterWorker(options.Clone());
    }
}
