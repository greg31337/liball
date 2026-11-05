using System;

namespace email;
/// <summary>
/// Parses supported structured header values independently of a complete message.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailValueParser : IEmailValueParserContract
{
    internal static EmailAddressParseResult ParseMailboxImplementation(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return new EmailAddressParser().ParseMailbox(input, mode);
    }

    /// <summary>
    /// Parses the supplied mailbox data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseMailbox(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailValueParser.ParseMailboxImplementation(input, mode);
    }

    internal static EmailAddressParseResult ParseAddressGroupImplementation(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return new EmailAddressParser().ParseGroup(input, mode);
    }

    /// <summary>
    /// Parses the supplied address group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseAddressGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailValueParser.ParseAddressGroupImplementation(input, mode);
    }

    internal static EmailAddressParseResult ParseGroupImplementation(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return ParseAddressGroupImplementation(input, mode);
    }

    /// <summary>
    /// Parses the supplied group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailValueParser.ParseGroupImplementation(input, mode);
    }

    internal static EmailAddressParseResult ParseAddressListImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return new EmailAddressParser().ParseListWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied address list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseAddressListWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailValueParser.ParseAddressListImplementationWithInputAndMode(input, mode);
    }

    internal static EmailAddressParseResult ParseAddressListImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailAddressParser().ParseListWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied address list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseAddressListWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseAddressListImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailDateParseResult ParseDateImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailDateParseMode mode)
    {
        return new EmailDateParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied date data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDateParseResult ParseDateWithInputAndMode(ReadOnlySpan<byte> input, EmailDateParseMode mode)
    {
        return EmailValueParser.ParseDateImplementationWithInputAndMode(input, mode);
    }

    internal static EmailDateParseResult ParseDateImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDateParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied date data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDateParseResult ParseDateWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDateParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseDateImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseMessageIdImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return new EmailMessageIdParser().ParseMessageIdWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseMessageIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailValueParser.ParseMessageIdImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseMessageIdImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailMessageIdParser().ParseMessageIdWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseMessageIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseMessageIdImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseContentIdImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return new EmailMessageIdParser().ParseContentIdWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseContentIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailValueParser.ParseContentIdImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseContentIdImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailMessageIdParser().ParseContentIdWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseContentIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseContentIdImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseReferencesImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return new EmailMessageIdParser().ParseReferencesWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseReferencesWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailValueParser.ParseReferencesImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseReferencesImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailMessageIdParser().ParseReferencesWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseReferencesWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseReferencesImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseInReplyToImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return new EmailMessageIdParser().ParseInReplyToWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseInReplyToWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailValueParser.ParseInReplyToImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseInReplyToImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailMessageIdParser().ParseInReplyToWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseInReplyToWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseInReplyToImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailParameterParseResult ParseParametersImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailParameterParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied parameters data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParameterParseResult ParseParametersWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseParametersImplementationWithInputAndMode(input, mode);
    }

    internal static EmailParameterParseResult ParseParametersImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailParameterParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied parameters data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailParameterParseResult ParseParametersWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseParametersImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMediaTypeParseResult ParseMediaTypeImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailMediaTypeParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied media type data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMediaTypeParseResult ParseMediaTypeWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseMediaTypeImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMediaTypeParseResult ParseMediaTypeImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailMediaTypeParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied media type data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMediaTypeParseResult ParseMediaTypeWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseMediaTypeImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailDispositionParseResult ParseDispositionImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailDispositionParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied disposition data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDispositionParseResult ParseDispositionWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseDispositionImplementationWithInputAndMode(input, mode);
    }

    internal static EmailDispositionParseResult ParseDispositionImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailDispositionParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied disposition data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDispositionParseResult ParseDispositionWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseDispositionImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailContentLanguageParseResult ParseContentLanguageImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailContentLanguageParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied content language data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailContentLanguageParseResult ParseContentLanguageWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseContentLanguageImplementationWithInputAndMode(input, mode);
    }

    internal static EmailContentLanguageParseResult ParseContentLanguageImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailContentLanguageParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied content language data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailContentLanguageParseResult ParseContentLanguageWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseContentLanguageImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailReceivedParseResult ParseReceivedImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailReceivedParseMode mode)
    {
        return new EmailReceivedParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied received data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedParseResult ParseReceivedWithInputAndMode(ReadOnlySpan<byte> input, EmailReceivedParseMode mode)
    {
        return EmailValueParser.ParseReceivedImplementationWithInputAndMode(input, mode);
    }

    internal static EmailReceivedParseResult ParseReceivedImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReceivedParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailReceivedParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied received data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedParseResult ParseReceivedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailReceivedParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseReceivedImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailAuthenticationResultsParseResult ParseAuthenticationResultsImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailAuthenticationResultsParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAuthenticationResultsParseResult ParseAuthenticationResultsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseAuthenticationResultsImplementationWithInputAndMode(input, mode);
    }

    internal static EmailAuthenticationResultsParseResult ParseAuthenticationResultsImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailAuthenticationResultsParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAuthenticationResultsParseResult ParseAuthenticationResultsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseAuthenticationResultsImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailReceivedSpfParseResult ParseReceivedSpfImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailReceivedSpfParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied received spf data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedSpfParseResult ParseReceivedSpfWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseReceivedSpfImplementationWithInputAndMode(input, mode);
    }

    internal static EmailReceivedSpfParseResult ParseReceivedSpfImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailReceivedSpfParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied received spf data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailReceivedSpfParseResult ParseReceivedSpfWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseReceivedSpfImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailDkimSignatureParseResult ParseDkimSignatureImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailDkimSignatureParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied DKIM signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDkimSignatureParseResult ParseDkimSignatureWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseDkimSignatureImplementationWithInputAndMode(input, mode);
    }

    internal static EmailDkimSignatureParseResult ParseDkimSignatureImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailDkimSignatureParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied DKIM signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDkimSignatureParseResult ParseDkimSignatureWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseDkimSignatureImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailArcMessageSignatureParseResult ParseArcMessageSignatureImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailArcMessageSignatureParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied ARC message signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcMessageSignatureParseResult ParseArcMessageSignatureWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseArcMessageSignatureImplementationWithInputAndMode(input, mode);
    }

    internal static EmailArcMessageSignatureParseResult ParseArcMessageSignatureImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailArcMessageSignatureParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied ARC message signature data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcMessageSignatureParseResult ParseArcMessageSignatureWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseArcMessageSignatureImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailArcSealParseResult ParseArcSealImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailArcSealParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied ARC seal data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcSealParseResult ParseArcSealWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseArcSealImplementationWithInputAndMode(input, mode);
    }

    internal static EmailArcSealParseResult ParseArcSealImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailArcSealParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied ARC seal data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcSealParseResult ParseArcSealWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseArcSealImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return new EmailArcAuthenticationResultsParser().ParseWithInputAndMode(input, mode);
    }

    /// <summary>
    /// Parses the supplied ARC authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailValueParser.ParseArcAuthenticationResultsImplementationWithInputAndMode(input, mode);
    }

    internal static EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return new EmailArcAuthenticationResultsParser().ParseWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>
    /// Parses the supplied ARC authentication results data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailArcAuthenticationResultsParseResult ParseArcAuthenticationResultsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailSignatureParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailValueParser.ParseArcAuthenticationResultsImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    /// <summary>Parses Return-Path.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseReturnPathWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("Return-Path", input, mode); }
    /// <summary>Parses Return-Path with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseReturnPathWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Return-Path", input, options, initialPosition); }
    /// <summary>Parses MIME-Version.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseMimeVersionWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("MIME-Version", input, mode); }
    /// <summary>Parses MIME-Version with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseMimeVersionWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("MIME-Version", input, options, initialPosition); }
    /// <summary>Parses Content-Transfer-Encoding.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseContentTransferEncodingWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("Content-Transfer-Encoding", input, mode); }
    /// <summary>Parses Content-Transfer-Encoding with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseContentTransferEncodingWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Content-Transfer-Encoding", input, options, initialPosition); }
    /// <summary>Parses Content-Location.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseContentLocationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("Content-Location", input, mode); }
    /// <summary>Parses Content-Location with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseContentLocationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Content-Location", input, options, initialPosition); }
    /// <summary>Parses Newsgroups.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseNewsgroupsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("Newsgroups", input, mode); }
    /// <summary>Parses Newsgroups with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseNewsgroupsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Newsgroups", input, options, initialPosition); }
    /// <summary>Parses Followup-To.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseFollowupToWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("Followup-To", input, mode); }
    /// <summary>Parses Followup-To with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseFollowupToWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("Followup-To", input, options, initialPosition); }
    /// <summary>Parses List-ID.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseListIdWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode("List-ID", input, mode); }
    /// <summary>Parses List-ID with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseListIdWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition("List-ID", input, options, initialPosition); }
    /// <summary>Parses a common trace field.</summary>
    /// <param name = "fieldName">The trace field name.</param><param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseTraceWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndMode(fieldName, input, mode); }
    /// <summary>Parses a common trace field with options.</summary>
    /// <param name = "fieldName">The trace field name.</param><param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseTraceWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition) { return new EmailStandardHeaderParser().ParseWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, initialPosition); }

    /// <summary>Parses Keywords.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseKeywordsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseKeywordsWithInputAndMode(input, mode); }
    /// <summary>Parses Keywords with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseKeywordsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseKeywordsWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses Priority.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParsePriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParsePriorityWithInputAndMode(input, mode); }
    /// <summary>Parses Priority with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParsePriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParsePriorityWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses X-Priority.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseXPriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseXPriorityWithInputAndMode(input, mode); }
    /// <summary>Parses X-Priority with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseXPriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseXPriorityWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses Importance.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseImportanceWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseImportanceWithInputAndMode(input, mode); }
    /// <summary>Parses Importance with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseImportanceWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseImportanceWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses Sensitivity.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseSensitivityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseSensitivityWithInputAndMode(input, mode); }
    /// <summary>Parses Sensitivity with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseSensitivityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseSensitivityWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses Auto-Submitted.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseAutoSubmittedWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseAutoSubmittedWithInputAndMode(input, mode); }
    /// <summary>Parses Auto-Submitted with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses a list-management value.</summary>
    /// <param name = "input">The value bytes.</param><param name = "kind">The list field family.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseListManagementWithInputAndKindAndMode(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseListManagementWithInputAndKindAndMode(input, kind, mode); }
    /// <summary>Parses a list-management value with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "kind">The list field family.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(input, kind, options, initialPosition); }
    /// <summary>Parses List-Unsubscribe-Post.</summary>
    /// <param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseListUnsubscribePostWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseListUnsubscribePostWithInputAndMode(input, mode); }
    /// <summary>Parses List-Unsubscribe-Post with options.</summary>
    /// <param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(input, options, initialPosition); }
    /// <summary>Parses a resent field.</summary>
    /// <param name = "fieldName">The resent field name.</param><param name = "input">The value bytes.</param><param name = "mode">The parse mode.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode) { return new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndMode(fieldName, input, mode); }
    /// <summary>Parses a resent field with options.</summary>
    /// <param name = "fieldName">The resent field name.</param><param name = "input">The value bytes.</param><param name = "options">The parse options.</param><param name = "initialPosition">The initial position.</param><returns>The parse result.</returns>
    public EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition) { return new EmailMetadataParser().ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, initialPosition); }
}
