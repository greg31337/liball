using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses address into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailAddressParser : IEmailAddressParserContract
{
    internal static EmailAddressParseResult ParseMailboxImplementation(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        EmailAddressParserOptions options = CreateOptions(mode);
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), 1);
    }

    /// <summary>
    /// Parses the supplied mailbox data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseMailbox(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailAddressParser.ParseMailboxImplementation(input, mode);
    }

    internal static EmailAddressParseResult ParseGroupImplementation(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        EmailAddressParserOptions options = CreateOptions(mode);
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), 2);
    }

    /// <summary>
    /// Parses the supplied group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailAddressParser.ParseGroupImplementation(input, mode);
    }

    internal static EmailAddressParseResult ParseListImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        EmailAddressParserOptions options = CreateOptions(mode);
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), 0);
    }

    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseListWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode)
    {
        return EmailAddressParser.ParseListImplementationWithInputAndMode(input, mode);
    }

    internal static EmailAddressParseResult ParseListImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailAddressParserOptions options)
    {
        return Parse(input, options, new EmailSourcePosition(0, 1, 1), 0);
    }

    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseListWithInputAndOptions(ReadOnlySpan<byte> input, EmailAddressParserOptions options)
    {
        return EmailAddressParser.ParseListImplementationWithInputAndOptions(input, options);
    }

    internal static EmailAddressParseResult ParseListImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 0);
    }

    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailAddressParseResult ParseListWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailAddressParser.ParseListImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    static EmailAddressParserOptions CreateOptions(EmailAddressParseMode mode)
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMode(mode);
        return options;
    }

    static EmailAddressParseResult Parse(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition, int expectedKind)
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
        EmailAddressParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AddressLimitExceeded, "The address value exceeds the configured input-byte limit.", initialPosition, null, null, "No address tokens were parsed."));
            return new EmailAddressParseResult(new EmailAddressList(new List<EmailAddressListEntry>(), new byte[0]), issues, 0, source.Length);
        }

        EmailHeaderLexicalOptions lexicalOptions = new EmailHeaderLexicalOptions();
        lexicalOptions.SetMode(snapshot.GetMode() == EmailAddressParseMode.Strict ? EmailHeaderLexicalMode.Strict : EmailHeaderLexicalMode.Tolerant);
        lexicalOptions.SetAllowInternationalized(snapshot.GetAllowInternationalized());
        EmailHeaderLexicalResult lexical = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(source, lexicalOptions, initialPosition);
        EmailAddressParserWorker worker = new EmailAddressParserWorker(source, lexical, snapshot, initialPosition);
        EmailAddressParseResult result = worker.ParseList();
        return ValidateExpectedKind(result, expectedKind, snapshot.GetMode(), initialPosition);
    }

    static EmailAddressParseResult ValidateExpectedKind(EmailAddressParseResult result, int expectedKind, EmailAddressParseMode mode, EmailSourcePosition position)
    {
        bool matches = expectedKind == 0 || expectedKind == 1 && result.HasMailbox() || expectedKind == 2 && result.HasGroup();
        if (matches)
        {
            return result;
        }

        EmailIssueList issues = result.GetIssues();
        EmailIssueSeverity severity = mode == EmailAddressParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        string description = expectedKind == 1 ? "The value is not exactly one mailbox." : "The value is not exactly one address group.";
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.AddressMalformed, description, position, null, null, "Every successfully parsed address-list entry was retained."));
        return new EmailAddressParseResult(result.GetAddressList(), issues, result.GetConsumedByteCount(), result.GetSourceByteCount());
    }
}
