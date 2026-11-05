using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Formats supported structured header values using explicit folding and compatibility policy.
/// </summary>
public class EmailValueWriter : IEmailValueWriterContract
{
    internal static EmailValueFormatResult WriteAddressImplementationWithValue(EmailAddress value)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatWithAddress(value));
    }

    /// <summary>
    /// Writes the supplied address to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressWithValue(EmailAddress value)
    {
        return EmailValueWriter.WriteAddressImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteAddressImplementationWithValueAndOptions(EmailAddress value, EmailAddressFormatOptions options)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatWithAddressAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied address to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressWithValueAndOptions(EmailAddress value, EmailAddressFormatOptions options)
    {
        return EmailValueWriter.WriteAddressImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteAddressGroupImplementationWithValue(EmailAddressGroup value)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatGroupWithGroup(value));
    }

    /// <summary>
    /// Writes the supplied address group to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressGroupWithValue(EmailAddressGroup value)
    {
        return EmailValueWriter.WriteAddressGroupImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteAddressGroupImplementationWithValueAndOptions(EmailAddressGroup value, EmailAddressFormatOptions options)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatGroupWithGroupAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied address group to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressGroupWithValueAndOptions(EmailAddressGroup value, EmailAddressFormatOptions options)
    {
        return EmailValueWriter.WriteAddressGroupImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteAddressListImplementationWithValue(EmailAddressList value)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatListWithList(value));
    }

    /// <summary>
    /// Writes the supplied address list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressListWithValue(EmailAddressList value)
    {
        return EmailValueWriter.WriteAddressListImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteAddressListImplementationWithValueAndOptions(EmailAddressList value, EmailAddressFormatOptions options)
    {
        return ConvertWithResultUsingEmailAddressFormatResult(new EmailAddressFormatter().FormatListWithListAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied address list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAddressListWithValueAndOptions(EmailAddressList value, EmailAddressFormatOptions options)
    {
        return EmailValueWriter.WriteAddressListImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteDateImplementationWithValue(EmailDateTime value)
    {
        return ConvertWithResultAndInitialColumn(new EmailDateFormatter().FormatWithValue(value), 0);
    }

    /// <summary>
    /// Writes the supplied date to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDateWithValue(EmailDateTime value)
    {
        return EmailValueWriter.WriteDateImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteDateImplementationWithValueAndOptions(EmailDateTime value, EmailDateFormatOptions options)
    {
        return ConvertWithResultAndInitialColumn(new EmailDateFormatter().FormatWithValueAndOptions(value, options), 0);
    }

    /// <summary>
    /// Writes the supplied date to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDateWithValueAndOptions(EmailDateTime value, EmailDateFormatOptions options)
    {
        return EmailValueWriter.WriteDateImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteMessageIdImplementationWithValue(EmailMessageId value)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatWithIdentifier(value));
    }

    /// <summary>
    /// Writes the supplied message ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMessageIdWithValue(EmailMessageId value)
    {
        return EmailValueWriter.WriteMessageIdImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteMessageIdImplementationWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied message ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMessageIdWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options)
    {
        return EmailValueWriter.WriteMessageIdImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteContentIdImplementationWithValue(EmailMessageId value)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatContentIdWithIdentifier(value));
    }

    /// <summary>
    /// Writes the supplied content ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteContentIdWithValue(EmailMessageId value)
    {
        return EmailValueWriter.WriteContentIdImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteContentIdImplementationWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatContentIdWithIdentifierAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied content ID to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteContentIdWithValueAndOptions(EmailMessageId value, EmailMessageIdFormatOptions options)
    {
        return EmailValueWriter.WriteContentIdImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteMessageIdListImplementationWithValue(EmailMessageIdList value)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatListWithIdentifiers(value));
    }

    /// <summary>
    /// Writes the supplied message ID list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMessageIdListWithValue(EmailMessageIdList value)
    {
        return EmailValueWriter.WriteMessageIdListImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteMessageIdListImplementationWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return ConvertWithResultUsingEmailMessageIdFormatResult(new EmailMessageIdFormatter().FormatListWithIdentifiersAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied message ID list to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMessageIdListWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return EmailValueWriter.WriteMessageIdListImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteReferencesImplementationWithValue(EmailMessageIdList value)
    {
        return WriteMessageIdListImplementationWithValue(value);
    }

    /// <summary>
    /// Writes the supplied references to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReferencesWithValue(EmailMessageIdList value)
    {
        return EmailValueWriter.WriteReferencesImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteReferencesImplementationWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return WriteMessageIdListImplementationWithValueAndOptions(value, options);
    }

    /// <summary>
    /// Writes the supplied references to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReferencesWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return EmailValueWriter.WriteReferencesImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteInReplyToImplementationWithValue(EmailMessageIdList value)
    {
        return WriteMessageIdListImplementationWithValue(value);
    }

    /// <summary>
    /// Writes the supplied in reply to to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteInReplyToWithValue(EmailMessageIdList value)
    {
        return EmailValueWriter.WriteInReplyToImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteInReplyToImplementationWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return WriteMessageIdListImplementationWithValueAndOptions(value, options);
    }

    /// <summary>
    /// Writes the supplied in reply to to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteInReplyToWithValueAndOptions(EmailMessageIdList value, EmailMessageIdFormatOptions options)
    {
        return EmailValueWriter.WriteInReplyToImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteParametersImplementationWithValue(EmailParameterList value)
    {
        return WriteParametersImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Writes the supplied parameters to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteParametersWithValue(EmailParameterList value)
    {
        return EmailValueWriter.WriteParametersImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteParametersImplementationWithValueAndOptions(EmailParameterList value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatParameters(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Writes the supplied parameters to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteParametersWithValueAndOptions(EmailParameterList value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteParametersImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteMediaTypeImplementationWithValue(EmailMediaType? value)
    {
        return WriteMediaTypeImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Writes the supplied media type to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMediaTypeWithValue(EmailMediaType? value)
    {
        return EmailValueWriter.WriteMediaTypeImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteMediaTypeImplementationWithValueAndOptions(EmailMediaType? value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatMediaType(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Writes the supplied media type to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteMediaTypeWithValueAndOptions(EmailMediaType? value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteMediaTypeImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteDispositionImplementationWithValue(EmailDisposition value)
    {
        return WriteDispositionImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Writes the supplied disposition to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDispositionWithValue(EmailDisposition value)
    {
        return EmailValueWriter.WriteDispositionImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteDispositionImplementationWithValueAndOptions(EmailDisposition value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatDisposition(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Writes the supplied disposition to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDispositionWithValueAndOptions(EmailDisposition value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteDispositionImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteContentLanguageImplementationWithValue(EmailContentLanguage value)
    {
        return WriteContentLanguageImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Writes the supplied content language to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteContentLanguageWithValue(EmailContentLanguage value)
    {
        return EmailValueWriter.WriteContentLanguageImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteContentLanguageImplementationWithValueAndOptions(EmailContentLanguage value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatContentLanguage(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Writes the supplied content language to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteContentLanguageWithValueAndOptions(EmailContentLanguage value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteContentLanguageImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteReceivedImplementationWithValue(EmailReceived value)
    {
        return WriteReceivedImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>
    /// Writes the supplied received to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReceivedWithValue(EmailReceived value)
    {
        return EmailValueWriter.WriteReceivedImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteReceivedImplementationWithValueAndOptions(EmailReceived value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatReceived(value);
        return worker.CreateResult();
    }

    /// <summary>
    /// Writes the supplied received to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReceivedWithValueAndOptions(EmailReceived value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteReceivedImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteAuthenticationResultsImplementationWithValue(EmailAuthenticationResults value)
    {
        return ConvertWithResultUsingEmailAuthenticationFormatResult(new EmailAuthenticationFormatter().FormatWithValue(value));
    }

    /// <summary>
    /// Writes the supplied authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAuthenticationResultsWithValue(EmailAuthenticationResults value)
    {
        return EmailValueWriter.WriteAuthenticationResultsImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteAuthenticationResultsImplementationWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options)
    {
        return ConvertWithResultUsingEmailAuthenticationFormatResult(new EmailAuthenticationFormatter().FormatWithValueAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteAuthenticationResultsWithValueAndOptions(EmailAuthenticationResults value, EmailAuthenticationFormatOptions options)
    {
        return EmailValueWriter.WriteAuthenticationResultsImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteReceivedSpfImplementationWithValue(EmailReceivedSpf value)
    {
        return ConvertWithResultUsingEmailAuthenticationFormatResult(new EmailAuthenticationFormatter().FormatReceivedSpfWithValue(value));
    }

    /// <summary>
    /// Writes the supplied received spf to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReceivedSpfWithValue(EmailReceivedSpf value)
    {
        return EmailValueWriter.WriteReceivedSpfImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteReceivedSpfImplementationWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options)
    {
        return ConvertWithResultUsingEmailAuthenticationFormatResult(new EmailAuthenticationFormatter().FormatReceivedSpfWithValueAndOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied received spf to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteReceivedSpfWithValueAndOptions(EmailReceivedSpf value, EmailAuthenticationFormatOptions options)
    {
        return EmailValueWriter.WriteReceivedSpfImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteDkimSignatureImplementationWithValue(EmailDkimSignature value)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueUsingEmailDkimSignature(value));
    }

    /// <summary>
    /// Writes the supplied DKIM signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDkimSignatureWithValue(EmailDkimSignature value)
    {
        return EmailValueWriter.WriteDkimSignatureImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteDkimSignatureImplementationWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied DKIM signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteDkimSignatureWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        return EmailValueWriter.WriteDkimSignatureImplementationWithValueAndOptions(value, options);
    }

    /// <summary>
    /// Writes a deterministic canonical DKIM DNS key record.
    /// </summary>
    /// <param name = "value">The DKIM key to write.</param>
    /// <returns>The value format result.</returns>
    public EmailValueFormatResult WriteDkimKeyWithValue(EmailDkimKey value)
    {
        return new EmailDkimKeyFormatter().FormatWithValue(value);
    }

    /// <summary>
    /// Writes a deterministic canonical DKIM DNS key record with options.
    /// </summary>
    /// <param name = "value">The DKIM key to write.</param>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The value format result.</returns>
    public EmailValueFormatResult WriteDkimKeyWithValueAndOptions(EmailDkimKey value, EmailValueFormatOptions options)
    {
        return new EmailDkimKeyFormatter().FormatWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteArcMessageSignatureImplementationWithValue(EmailDkimSignature value)
    {
        return WriteArcMessageSignatureImplementationWithValueAndOptions(value, new EmailSignatureFormatOptions());
    }

    /// <summary>
    /// Writes the supplied ARC message signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteArcMessageSignatureWithValue(EmailDkimSignature value)
    {
        return EmailValueWriter.WriteArcMessageSignatureImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteArcMessageSignatureImplementationWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!value.IsArcMessageSignature())
        {
            throw new ArgumentException("An ARC-Message-Signature value is required.", "value");
        }

        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied ARC message signature to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailValueFormatResult WriteArcMessageSignatureWithValueAndOptions(EmailDkimSignature value, EmailSignatureFormatOptions options)
    {
        return EmailValueWriter.WriteArcMessageSignatureImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteArcSealImplementationWithValue(EmailArcSeal value)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueUsingEmailArcSeal(value));
    }

    /// <summary>
    /// Writes the supplied ARC seal to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteArcSealWithValue(EmailArcSeal value)
    {
        return EmailValueWriter.WriteArcSealImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteArcSealImplementationWithValueAndOptions(EmailArcSeal value, EmailSignatureFormatOptions options)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied ARC seal to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteArcSealWithValueAndOptions(EmailArcSeal value, EmailSignatureFormatOptions options)
    {
        return EmailValueWriter.WriteArcSealImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteArcAuthenticationResultsImplementationWithValue(EmailArcAuthenticationResults value)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueUsingEmailArcAuthenticationResults(value));
    }

    /// <summary>
    /// Writes the supplied ARC authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteArcAuthenticationResultsWithValue(EmailArcAuthenticationResults value)
    {
        return EmailValueWriter.WriteArcAuthenticationResultsImplementationWithValue(value);
    }

    internal static EmailValueFormatResult WriteArcAuthenticationResultsImplementationWithValueAndOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options)
    {
        return ConvertWithResultUsingEmailSignatureFormatResult(new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(value, options));
    }

    /// <summary>
    /// Writes the supplied ARC authentication results to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The value format result produced by the operation.</returns>
    public EmailValueFormatResult WriteArcAuthenticationResultsWithValueAndOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options)
    {
        return EmailValueWriter.WriteArcAuthenticationResultsImplementationWithValueAndOptions(value, options);
    }

    internal static EmailValueFormatResult WriteStandardHeaderImplementationWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        worker.FormatStandardHeader(value);
        return worker.CreateResult();
    }

    /// <summary>Writes any supported standard header value canonically.</summary>
    /// <param name = "value">The structured value.</param>
    /// <param name = "options">The format options.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult WriteStandardHeaderWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options)
    {
        return EmailValueWriter.WriteStandardHeaderImplementationWithValueAndOptions(value, options);
    }

    /// <summary>Writes any supported standard header value canonically.</summary>
    /// <param name = "value">The structured value.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult WriteStandardHeaderWithValue(EmailStandardHeaderValue value)
    {
        return EmailValueWriter.WriteStandardHeaderImplementationWithValueAndOptions(value, new EmailValueFormatOptions());
    }

    /// <summary>Writes a Return-Path value canonically.</summary>
    /// <param name = "value">The structured value.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult WriteReturnPathWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ReturnPath, new EmailValueFormatOptions()); }
    /// <summary>Writes a Return-Path value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult WriteReturnPathWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ReturnPath, options); }
    /// <summary>Writes a MIME-Version value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteMimeVersionWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.MimeVersion, new EmailValueFormatOptions()); }
    /// <summary>Writes a MIME-Version value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteMimeVersionWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.MimeVersion, options); }
    /// <summary>Writes a Content-Transfer-Encoding value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteContentTransferEncodingWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ContentTransferEncoding, new EmailValueFormatOptions()); }
    /// <summary>Writes a Content-Transfer-Encoding value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteContentTransferEncodingWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ContentTransferEncoding, options); }
    /// <summary>Writes a Content-Location value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteContentLocationWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ContentLocation, new EmailValueFormatOptions()); }
    /// <summary>Writes a Content-Location value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteContentLocationWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ContentLocation, options); }
    /// <summary>Writes a Newsgroups value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteNewsgroupsWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.Newsgroups, new EmailValueFormatOptions()); }
    /// <summary>Writes a Newsgroups value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteNewsgroupsWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.Newsgroups, options); }
    /// <summary>Writes a Followup-To value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteFollowupToWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.FollowupTo, new EmailValueFormatOptions()); }
    /// <summary>Writes a Followup-To value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteFollowupToWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.FollowupTo, options); }
    /// <summary>Writes a List-ID value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListIdWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ListId, new EmailValueFormatOptions()); }
    /// <summary>Writes a List-ID value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListIdWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.ListId, options); }
    /// <summary>Writes a common trace value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteTraceWithValue(EmailStandardHeaderValue value) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.Trace, new EmailValueFormatOptions()); }
    /// <summary>Writes a common trace value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteTraceWithValueAndOptions(EmailStandardHeaderValue value, EmailValueFormatOptions options) { return WriteExpectedStandard(value, EmailStandardHeaderValueKind.Trace, options); }

    /// <summary>Writes Keywords canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteKeywordsWithValue(EmailKeywordList value) { return WriteMetadata(value, new EmailValueFormatOptions(), 0); }
    /// <summary>Writes Keywords canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteKeywordsWithValueAndOptions(EmailKeywordList value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 0); }
    /// <summary>Writes Priority or X-Priority canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WritePriorityWithValue(EmailPriority value) { return WriteMetadata(value, new EmailValueFormatOptions(), 1); }
    /// <summary>Writes Priority or X-Priority canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WritePriorityWithValueAndOptions(EmailPriority value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 1); }
    /// <summary>Writes Importance canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteImportanceWithValue(EmailImportance value) { return WriteMetadata(value, new EmailValueFormatOptions(), 2); }
    /// <summary>Writes Importance canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteImportanceWithValueAndOptions(EmailImportance value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 2); }
    /// <summary>Writes Sensitivity canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteSensitivityWithValue(EmailSensitivity value) { return WriteMetadata(value, new EmailValueFormatOptions(), 3); }
    /// <summary>Writes Sensitivity canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteSensitivityWithValueAndOptions(EmailSensitivity value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 3); }
    /// <summary>Writes Auto-Submitted canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteAutoSubmittedWithValue(EmailAutoSubmitted value) { return WriteMetadata(value, new EmailValueFormatOptions(), 4); }
    /// <summary>Writes Auto-Submitted canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteAutoSubmittedWithValueAndOptions(EmailAutoSubmitted value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 4); }
    /// <summary>Writes a list-management value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListManagementWithValue(EmailListManagementValue value) { return WriteMetadata(value, new EmailValueFormatOptions(), 5); }
    /// <summary>Writes a list-management value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListManagementWithValueAndOptions(EmailListManagementValue value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 5); }
    /// <summary>Writes List-Unsubscribe-Post canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListUnsubscribePostWithValue(EmailListUnsubscribePost value) { return WriteMetadata(value, new EmailValueFormatOptions(), 6); }
    /// <summary>Writes List-Unsubscribe-Post canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteListUnsubscribePostWithValueAndOptions(EmailListUnsubscribePost value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 6); }
    /// <summary>Writes a resent field value canonically.</summary>
    /// <param name = "value">The structured value.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteResentFieldWithValue(EmailResentField value) { return WriteMetadata(value, new EmailValueFormatOptions(), 7); }
    /// <summary>Writes a resent field value canonically with options.</summary>
    /// <param name = "value">The structured value.</param><param name = "options">The format options.</param><returns>The format result.</returns>
    public EmailValueFormatResult WriteResentFieldWithValueAndOptions(EmailResentField value, EmailValueFormatOptions options) { return WriteMetadata(value, options, 7); }

    static EmailValueFormatResult WriteExpectedStandard(EmailStandardHeaderValue value, EmailStandardHeaderValueKind expected, EmailValueFormatOptions options)
    {
        Validate(value, options);
        if (value.GetKind() != expected) { throw new ArgumentException("The structured value has the wrong header family.", "value"); }
        return EmailValueWriter.WriteStandardHeaderImplementationWithValueAndOptions(value, options);
    }

    static EmailValueFormatResult WriteMetadata(object? value, EmailValueFormatOptions options, int operation)
    {
        Validate(value, options);
        EmailValueFormatterWorker worker = new EmailValueFormatterWorker(options.Clone());
        object nonNullValue = value ?? throw new InvalidOperationException();
        if (operation == 0) { worker.FormatKeywords((EmailKeywordList)nonNullValue); }
        else if (operation == 1) { worker.FormatPriority((EmailPriority)nonNullValue); }
        else if (operation == 2) { worker.FormatImportance((EmailImportance)nonNullValue); }
        else if (operation == 3) { worker.FormatSensitivity((EmailSensitivity)nonNullValue); }
        else if (operation == 4) { worker.FormatAutoSubmitted((EmailAutoSubmitted)nonNullValue); }
        else if (operation == 5) { worker.FormatListManagement((EmailListManagementValue)nonNullValue); }
        else if (operation == 6) { worker.FormatListUnsubscribePost((EmailListUnsubscribePost)nonNullValue); }
        else { worker.FormatResentField((EmailResentField)nonNullValue); }
        return worker.CreateResult();
    }

    static EmailValueFormatResult ConvertWithResultUsingEmailAddressFormatResult(EmailAddressFormatResult result)
    {
        return new EmailValueFormatResult(result.GetBytes(), result.GetIssues(), result.GetFoldCount(), result.GetFinalLineColumn());
    }

    static EmailValueFormatResult ConvertWithResultAndInitialColumn(EmailDateFormatResult result, int initialColumn)
    {
        byte[] bytes = result.GetBytes();
        return new EmailValueFormatResult(bytes, result.GetIssues(), CountFolds(bytes), GetFinalColumn(bytes, initialColumn));
    }

    static EmailValueFormatResult ConvertWithResultUsingEmailMessageIdFormatResult(EmailMessageIdFormatResult result)
    {
        return new EmailValueFormatResult(result.GetBytes(), result.GetIssues(), result.GetFoldCount(), result.GetFinalLineColumn());
    }

    static EmailValueFormatResult ConvertWithResultUsingEmailAuthenticationFormatResult(EmailAuthenticationFormatResult result)
    {
        return new EmailValueFormatResult(result.GetBytes(), result.GetIssues(), result.GetFoldCount(), result.GetFinalLineColumn());
    }

    static EmailValueFormatResult ConvertWithResultUsingEmailSignatureFormatResult(EmailSignatureFormatResult result)
    {
        return new EmailValueFormatResult(result.GetBytes(), result.GetIssues(), result.GetFoldCount(), result.GetFinalLineColumn());
    }

    static void Validate(object? value, EmailValueFormatOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    static int CountFolds(byte[] bytes)
    {
        int count = 0;
        int index = 0;
        while (index + 1 < bytes.Length)
        {
            if (bytes[index] == 13 && bytes[index + 1] == 10)
            {
                count++;
                index++;
            }

            index++;
        }

        return count;
    }

    static int GetFinalColumn(byte[] bytes, int initialColumn)
    {
        int column = initialColumn;
        int index = 0;
        while (index < bytes.Length)
        {
            column = bytes[index] == 10 ? 0 : column + 1;
            index++;
        }

        return column;
    }
}
