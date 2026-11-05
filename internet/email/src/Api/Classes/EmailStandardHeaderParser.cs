using System;

namespace email;
/// <summary>
/// Parses the standard structured header families represented by <see cref = "EmailStandardHeaderValue"/>.
/// </summary>
public class EmailStandardHeaderParser : IEmailStandardHeaderParserContract
{
    internal static EmailStandardHeaderParseResult ParseImplementationWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMode(mode);
        return ParseImplementationWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, null);
    }

    /// <summary>Parses a supported field value.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "input">The field value bytes.</param>
    /// <param name = "mode">The parse mode.</param>
    /// <returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailStandardHeaderParser.ParseImplementationWithFieldNameAndInputAndMode(fieldName, input, mode);
    }

    internal static EmailStandardHeaderParseResult ParseImplementationWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        if (fieldName == null) { throw new ArgumentNullException("fieldName"); }
        if (options == null) { throw new ArgumentNullException("options"); }
        EmailStandardHeaderValueKind kind = GetKind(fieldName);
        byte[] source = input.ToArray();
        EmailSourcePosition position = initialPosition ?? new EmailSourcePosition(0, 1, 1);
        if (source.Length > options.GetMaximumInputBytes())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(CreateIssue(EmailIssueSeverity.Fatal, EmailIssueCode.MetadataValueLimitExceeded, "The structured field value exceeds the configured input-byte limit.", position, fieldName, "No structured value was produced."));
            return new EmailStandardHeaderParseResult(null, issues, 0, source.Length);
        }

        return new EmailStandardHeaderParserWorker(fieldName, kind, source, options.GetMode(), position).Parse();
    }

    /// <summary>Parses a supported field value with explicit options.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "input">The field value bytes.</param>
    /// <param name = "options">The parser options.</param>
    /// <param name = "initialPosition">The initial source position.</param>
    /// <returns>The parse result.</returns>
    public EmailStandardHeaderParseResult ParseWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailStandardHeaderParser.ParseImplementationWithFieldNameAndInputAndOptionsAndInitialPosition(fieldName, input, options, initialPosition);
    }

    internal static EmailStandardHeaderValueKind GetKind(string fieldName)
    {
        if (fieldName == null) { throw new ArgumentNullException("fieldName"); }
        if (EqualsName(fieldName, "Return-Path")) { return EmailStandardHeaderValueKind.ReturnPath; }
        if (EqualsName(fieldName, "MIME-Version")) { return EmailStandardHeaderValueKind.MimeVersion; }
        if (EqualsName(fieldName, "Content-Transfer-Encoding")) { return EmailStandardHeaderValueKind.ContentTransferEncoding; }
        if (EqualsName(fieldName, "Content-Location")) { return EmailStandardHeaderValueKind.ContentLocation; }
        if (EqualsName(fieldName, "Newsgroups")) { return EmailStandardHeaderValueKind.Newsgroups; }
        if (EqualsName(fieldName, "Followup-To")) { return EmailStandardHeaderValueKind.FollowupTo; }
        if (EqualsName(fieldName, "List-ID")) { return EmailStandardHeaderValueKind.ListId; }
        if (IsTraceName(fieldName)) { return EmailStandardHeaderValueKind.Trace; }
        throw new ArgumentException("The field name is not supported by this structured parser.", "fieldName");
    }

    internal static bool IsTraceName(string fieldName)
    {
        return EqualsName(fieldName, "Delivered-To") || EqualsName(fieldName, "X-Original-To") || EqualsName(fieldName, "Envelope-To") || EqualsName(fieldName, "X-Envelope-To") || EqualsName(fieldName, "Errors-To") || EqualsName(fieldName, "Mail-Reply-To") || EqualsName(fieldName, "Mail-Followup-To") || EqualsName(fieldName, "X-Originating-IP");
    }

    internal static EmailIssue CreateIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition position, string fieldName, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, fieldName, recovery);
    }

    static bool EqualsName(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
