using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses metadata into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailMetadataParser : IEmailMetadataParserContract
{
    internal static EmailMetadataParseResult ParseKeywordsImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 0, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied keywords data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseKeywordsWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseKeywordsImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseKeywordsImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 0, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied keywords data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseKeywordsWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseKeywordsImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParsePriorityImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 1, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParsePriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParsePriorityImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParsePriorityImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 1, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParsePriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParsePriorityImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseXPriorityImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 2, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied x priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseXPriorityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseXPriorityImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseXPriorityImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 2, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied x priority data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseXPriorityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseXPriorityImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseImportanceImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 3, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied importance data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseImportanceWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseImportanceImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseImportanceImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 3, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied importance data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseImportanceWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseImportanceImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseSensitivityImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 4, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied sensitivity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseSensitivityWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseSensitivityImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseSensitivityImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 4, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied sensitivity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseSensitivityWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseSensitivityImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseAutoSubmittedImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 5, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied auto submitted data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseAutoSubmittedWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseAutoSubmittedImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseAutoSubmittedImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 5, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied auto submitted data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseAutoSubmittedImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseListManagementImplementationWithInputAndKindAndMode(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 6, null, kind);
    }

    /// <summary>
    /// Parses the supplied list management data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseListManagementWithInputAndKindAndMode(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseListManagementImplementationWithInputAndKindAndMode(input, kind, mode);
    }

    internal static EmailMetadataParseResult ParseListManagementImplementationWithInputAndKindAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 6, null, kind);
    }

    /// <summary>
    /// Parses the supplied list management data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseListManagementWithInputAndKindAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailListManagementKind kind, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseListManagementImplementationWithInputAndKindAndOptionsAndInitialPosition(input, kind, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseListUnsubscribePostImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 7, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied list unsubscribe post data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseListUnsubscribePostWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseListUnsubscribePostImplementationWithInputAndMode(input, mode);
    }

    internal static EmailMetadataParseResult ParseListUnsubscribePostImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 7, null, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied list unsubscribe post data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseListUnsubscribePostImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailMetadataParseResult ParseResentFieldImplementationWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return Parse(input, CreateOptions(mode), new EmailSourcePosition(0, 1, 1), 8, fieldName, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied resent field data.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailMetadataParser.ParseResentFieldImplementationWithFieldNameAndInputAndMode(fieldName, input, mode);
    }

    internal static EmailMetadataParseResult ParseResentFieldImplementationWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return Parse(input, options, initialPosition, 8, fieldName, EmailListManagementKind.Help);
    }

    /// <summary>
    /// Parses the supplied resent field data.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailMetadataParseResult ParseResentFieldWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailMetadataParser.ParseResentFieldImplementationWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, initialPosition);
    }

    static EmailMetadataParserOptions CreateOptions(EmailStructuredValueParseMode mode)
    {
        EmailMetadataParserOptions options = new EmailMetadataParserOptions();
        options.SetMode(mode);
        return options;
    }

    static EmailMetadataParseResult Parse(ReadOnlySpan<byte> input, EmailMetadataParserOptions options, EmailSourcePosition? initialPosition, int operation, string? fieldName, EmailListManagementKind listKind)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        if (operation == 6 && (listKind < EmailListManagementKind.Help || listKind > EmailListManagementKind.Archive))
        {
            throw new ArgumentOutOfRangeException("listKind");
        }

        if (operation == 8 && (fieldName == null || fieldName.Length == 0))
        {
            throw new ArgumentException("A resent field name is required.", "fieldName");
        }

        byte[] source = input.ToArray();
        EmailMetadataParserOptions snapshot = options.Clone();
        EmailMetadataValueKind resultKind = GetResultKind(operation);
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.MetadataValueLimitExceeded, "The structured field value exceeds the configured input-byte limit.", initialPosition, null, fieldName, "No structured value was produced."));
            return new EmailMetadataParseResult(resultKind, null, issues, 0, source.Length);
        }

        EmailMetadataParserWorker worker = new EmailMetadataParserWorker(source, snapshot, initialPosition);
        if (operation == 0)
        {
            return worker.ParseKeywords();
        }

        if (operation == 1)
        {
            return worker.ParsePriority(false);
        }

        if (operation == 2)
        {
            return worker.ParsePriority(true);
        }

        if (operation == 3)
        {
            return worker.ParseImportance();
        }

        if (operation == 4)
        {
            return worker.ParseSensitivity();
        }

        if (operation == 5)
        {
            return worker.ParseAutoSubmitted();
        }

        if (operation == 6)
        {
            return worker.ParseListManagement(listKind);
        }

        if (operation == 7)
        {
            return worker.ParseListUnsubscribePost();
        }

        return worker.ParseResentField(fieldName);
    }

    static EmailMetadataValueKind GetResultKind(int operation)
    {
        if (operation <= 0)
        {
            return EmailMetadataValueKind.Keywords;
        }

        if (operation <= 2)
        {
            return EmailMetadataValueKind.Priority;
        }

        return (EmailMetadataValueKind)(operation - 1);
    }
}
