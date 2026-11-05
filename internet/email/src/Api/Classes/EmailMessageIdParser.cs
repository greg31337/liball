using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses message ID into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailMessageIdParser : IEmailMessageIdParserContract
{
    internal static EmailMessageIdParseResult ParseMessageIdImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return ParseWithMode(input, mode, true);
    }

    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseMessageIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailMessageIdParser.ParseMessageIdImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseContentIdImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return ParseWithMode(input, mode, true);
    }

    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseContentIdWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailMessageIdParser.ParseContentIdImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseReferencesImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return ParseWithMode(input, mode, false);
    }

    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseReferencesWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailMessageIdParser.ParseReferencesImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseInReplyToImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return ParseWithMode(input, mode, false);
    }

    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseInReplyToWithInputAndMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode)
    {
        return EmailMessageIdParser.ParseInReplyToImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMessageIdParseResult ParseMessageIdImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), true);
    }

    /// <summary>
    /// Parses the supplied message ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseMessageIdWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return EmailMessageIdParser.ParseMessageIdImplementationWithInputAndOptions(input, options);
    }

    internal static EmailMessageIdParseResult ParseMessageIdImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, true);
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
        return EmailMessageIdParser.ParseMessageIdImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseContentIdImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), true);
    }

    /// <summary>
    /// Parses the supplied content ID data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseContentIdWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return EmailMessageIdParser.ParseContentIdImplementationWithInputAndOptions(input, options);
    }

    internal static EmailMessageIdParseResult ParseContentIdImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, true);
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
        return EmailMessageIdParser.ParseContentIdImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseReferencesImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), false);
    }

    /// <summary>
    /// Parses the supplied references data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseReferencesWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return EmailMessageIdParser.ParseReferencesImplementationWithInputAndOptions(input, options);
    }

    internal static EmailMessageIdParseResult ParseReferencesImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, false);
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
        return EmailMessageIdParser.ParseReferencesImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMessageIdParseResult ParseInReplyToImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), false);
    }

    /// <summary>
    /// Parses the supplied in reply to data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMessageIdParseResult ParseInReplyToWithInputAndOptions(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options)
    {
        return EmailMessageIdParser.ParseInReplyToImplementationWithInputAndOptions(input, options);
    }

    internal static EmailMessageIdParseResult ParseInReplyToImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, false);
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
        return EmailMessageIdParser.ParseInReplyToImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    static EmailMessageIdParseResult ParseWithMode(ReadOnlySpan<byte> input, EmailMessageIdParseMode mode, bool single)
    {
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();
        options.SetMode(mode);
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), single);
    }

    static EmailMessageIdParseResult Parse(ReadOnlySpan<byte> input, EmailMessageIdParserOptions options, EmailSourcePosition? initialPosition, bool single)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        byte[] source = input.ToArray();
        EmailMessageIdParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.MessageIdLimitExceeded, "The identifier value exceeds the configured input-byte limit.", initialPosition, null, null, "No identifiers were parsed."));
            return new EmailMessageIdParseResult(new EmailMessageIdList(new List<EmailMessageId>(), new byte[0]), issues, 0, source.Length);
        }

        EmailHeaderLexicalOptions lexicalOptions = new EmailHeaderLexicalOptions();
        lexicalOptions.SetMode(snapshot.GetMode() == EmailMessageIdParseMode.Strict ? EmailHeaderLexicalMode.Strict : EmailHeaderLexicalMode.Tolerant);
        lexicalOptions.SetAllowInternationalized(false);
        EmailHeaderLexicalResult lexical = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(source, lexicalOptions, initialPosition);
        EmailMessageIdParserWorker worker = new EmailMessageIdParserWorker(source, lexical, snapshot, initialPosition, single);
        return worker.Parse();
    }
}
