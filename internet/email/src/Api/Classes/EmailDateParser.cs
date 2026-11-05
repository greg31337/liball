using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses date into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailDateParser : IEmailDateParserContract
{
    internal static EmailDateParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailDateParseMode mode)
    {
        EmailDateParserOptions options = new EmailDateParserOptions();
        options.SetMode(mode);
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDateParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailDateParseMode mode)
    {
        return EmailDateParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailDateParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailDateParserOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDateParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailDateParserOptions options)
    {
        return EmailDateParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailDateParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDateParserOptions options, EmailSourcePosition? initialPosition)
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
        EmailDateParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DateInputLimitExceeded, "The date value exceeds the configured input-byte limit.", initialPosition, null, null, "No date tokens were parsed."));
            return new EmailDateParseResult(null, issues, 0, source.Length);
        }

        EmailHeaderLexicalOptions lexicalOptions = new EmailHeaderLexicalOptions();
        lexicalOptions.SetMode(snapshot.GetMode() == EmailDateParseMode.Strict ? EmailHeaderLexicalMode.Strict : EmailHeaderLexicalMode.Tolerant);
        lexicalOptions.SetAllowInternationalized(false);
        EmailHeaderLexicalResult lexical = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(source, lexicalOptions, initialPosition);
        EmailDateParserWorker worker = new EmailDateParserWorker(source, lexical, snapshot, initialPosition);
        return worker.Parse();
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDateParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDateParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailDateParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
