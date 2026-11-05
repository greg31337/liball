using System;
using System.Text;

namespace email;

internal class EmailHeaderDecodeResult
{
    string value;
    EmailIssueList issues;

    internal EmailHeaderDecodeResult(string value, EmailIssueList issues)
    {
        this.value = value;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    internal string GetValue()
    {
        return value;
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }
}
internal class EmailHeaderDecoder
{
    static readonly EmailCharsetRegistry charsetRegistry = new EmailCharsetRegistry();

    internal static EmailHeaderDecodeResult Decode(byte[]? input, EmailHeaderValueKind kind, EmailStructuredValueParseMode mode, EmailSourcePosition? position, bool allowBrokenEncodedWords)
    {
        EmailEncodedWordContext context = GetContext(kind);
        EmailEncodedWordParseMode wordMode = mode == EmailStructuredValueParseMode.Strict || !allowBrokenEncodedWords ? EmailEncodedWordParseMode.Strict : EmailEncodedWordParseMode.Tolerant;
        EmailEncodedWordParseResult parsed = new EmailEncodedWord().ParseWithInputAndModeAndContext(input, wordMode, context);
        EmailIssueList issues = new EmailIssueList();
        AddAdjustedIssues(issues, parsed.GetIssues(), position);
        if (parsed.GetIssues().GetCount() > 0 && allowBrokenEncodedWords)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.EncodedWordCompatibilityRecovery, "A broken encoded word was decoded by an explicitly enabled compatibility rule.", position, null, null, "The decoded header value contains the usable bytes recovered from the malformed encoded word."));
        }

        if (parsed.GetIssues().GetCount() > 0 && !allowBrokenEncodedWords)
        {
            return new EmailHeaderDecodeResult(TrimWhitespace(Encoding.UTF8.GetString(input ?? throw new InvalidOperationException())), issues);
        }

        StringBuilder text = new StringBuilder();
        int index = 0;
        while (index < parsed.GetPartCount())
        {
            EmailEncodedWordPart part = parsed.GetPart(index);
            byte[] bytes = part.GetBytes();
            string? charset = part.IsEncodedWord() ? part.GetCharset() : "utf-8";
            EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
            options.SetFallbackPolicy(EmailCharsetFallbackPolicy.Replacement);
            EmailCharsetDecodeResult decoded = charsetRegistry.Decode(bytes, charset, options);
            text.Append(decoded.GetText());
            EmailSourcePosition partPosition = AdjustPosition(position, part.GetSourcePosition());
            AddAdjustedIssues(issues, decoded.GetIssues(), partPosition);
            index++;
        }

        if (parsed.GetPartCount() == 0 && (input ?? throw new InvalidOperationException()).Length > 0)
        {
            string fallback = Encoding.UTF8.GetString(input);
            text.Append(fallback);
            if (EmailHeaderUtf8.GetInvalidIndex(input) >= 0)
            {
                EmailIssueSeverity severity = mode == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.HeaderMalformed, "The header value contains invalid UTF-8 text.", position, null, null, "Invalid byte sequences were replaced in the decoded value."));
            }
        }

        return new EmailHeaderDecodeResult(TrimWhitespace(text.ToString()), issues);
    }

    static void AddAdjustedIssues(EmailIssueList target, EmailIssueList additions, EmailSourcePosition? basePosition)
    {
        int index = 0;
        while (index < additions.GetCount())
        {
            EmailIssue issue = additions.GetAt(index);
            EmailSourcePosition? issuePosition = issue.HasSourcePosition() ? AdjustPosition(basePosition, issue.GetSourcePosition()) : basePosition;
            target.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(issue.GetSeverity(), issue.GetCode(), issue.GetDescription(), issuePosition, issue.GetEntityPath(), issue.GetFieldName(), issue.GetRecoveryDescription()));
            index++;
        }
    }

    static EmailSourcePosition AdjustPosition(EmailSourcePosition? basePosition, EmailSourcePosition? relativePosition)
    {
        long byteOffset = (basePosition ?? throw new InvalidOperationException()).GetByteOffset();
        if (new EmailSafeArithmetic().CanAddNonNegative(byteOffset, (relativePosition ?? throw new InvalidOperationException()).GetByteOffset()))
        {
            byteOffset += relativePosition.GetByteOffset();
        }

        long relativeLines = relativePosition.GetLineNumber() - 1;
        long lineNumber = basePosition.GetLineNumber();
        if (new EmailSafeArithmetic().CanAddNonNegative(lineNumber, relativeLines))
        {
            lineNumber += relativeLines;
        }

        long columnNumber = relativePosition.GetColumnNumber();
        if (relativeLines == 0)
        {
            long relativeColumns = relativePosition.GetColumnNumber() - 1;
            columnNumber = basePosition.GetColumnNumber();
            if (new EmailSafeArithmetic().CanAddNonNegative(columnNumber, relativeColumns))
            {
                columnNumber += relativeColumns;
            }
        }

        return new EmailSourcePosition(byteOffset, lineNumber, columnNumber);
    }

    static EmailEncodedWordContext GetContext(EmailHeaderValueKind kind)
    {
        if (kind == EmailHeaderValueKind.Mailbox || kind == EmailHeaderValueKind.AddressGroup || kind == EmailHeaderValueKind.AddressList)
        {
            return EmailEncodedWordContext.Phrase;
        }

        if (kind == EmailHeaderValueKind.Unstructured || kind == EmailHeaderValueKind.Keywords)
        {
            return EmailEncodedWordContext.UnstructuredText;
        }

        return EmailEncodedWordContext.Forbidden;
    }

    static string TrimWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t'))
        {
            start++;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t'))
        {
            end--;
        }

        return value.Substring(start, end - start);
    }
}
