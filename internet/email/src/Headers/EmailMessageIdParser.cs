using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailMessageIdPartValue
{
    internal bool success;
    internal string? value;
    internal bool domainLiteral;
    internal bool obsolete;

    internal EmailMessageIdPartValue()
    {
        success = false;
        value = null;
        domainLiteral = false;
        obsolete = false;
    }
}
internal class EmailMessageIdCandidate
{
    internal bool success;
    internal EmailMessageId? identifier;
    internal int nextCursor;

    internal EmailMessageIdCandidate()
    {
        success = false;
        identifier = null;
        nextCursor = 0;
    }
}
internal class EmailMessageIdParserWorker
{
    byte[] input;
    List<EmailHeaderToken> tokens;
    EmailMessageIdParserOptions options;
    EmailSourcePosition initialPosition;
    EmailIssueList issues;
    bool single;
    int cursor;
    int lexicalConsumedByteCount;
    bool stopped;
    bool ignoredTextReported;
    int stopByteCount;

    internal EmailMessageIdParserWorker(byte[] input, EmailHeaderLexicalResult lexical, EmailMessageIdParserOptions options, EmailSourcePosition initialPosition, bool single)
    {
        this.input = input;
        tokens = lexical.GetTokens();
        this.options = options;
        this.initialPosition = initialPosition;
        issues = lexical.GetIssues();
        this.single = single;
        cursor = 0;
        lexicalConsumedByteCount = lexical.GetConsumedByteCount();
        stopped = lexical.HasFatalError();
        ignoredTextReported = false;
        stopByteCount = -1;
    }

    internal EmailMessageIdParseResult Parse()
    {
        List<EmailMessageId> identifiers = new List<EmailMessageId>();
        while (cursor < tokens.Count && !stopped)
        {
            SkipCfws();
            if (cursor >= tokens.Count)
            {
                break;
            }

            if (IsSpecialAt(cursor, 60))
            {
                EmailMessageIdCandidate candidate = ParseAngleIdentifier();
                cursor = candidate.nextCursor;
                if (candidate.success)
                {
                    AddIdentifier(identifiers, candidate.identifier);
                }

                continue;
            }

            if (IsSpecialAt(cursor, 44) || IsSpecialAt(cursor, 62))
            {
                AddRecovery("A stray separator or angle bracket appears between identifiers.", tokens[cursor].GetSourcePosition(), "The stray token was ignored.");
                cursor++;
                continue;
            }

            int end = FindBareSegmentEnd(cursor);
            if (HasAtSign(cursor, end))
            {
                EmailMessageIdCandidate candidate = ParseCore(cursor, end, false, false);
                candidate.nextCursor = end;
                cursor = end;
                if (candidate.success)
                {
                    AddRecovery("An identifier is missing its surrounding angle brackets.", (candidate.identifier ?? throw new InvalidOperationException()).GetSourcePosition(), "The bare identifier was retained.");
                    AddIdentifier(identifiers, candidate.identifier);
                }

                continue;
            }

            ReportIgnoredText(tokens[cursor].GetSourcePosition());
            cursor = end > cursor ? end : cursor + 1;
        }

        if (!stopped && identifiers.Count == 0)
        {
            AddMalformed("The value does not contain a usable message identifier.", initialPosition, "No identifiers were produced.");
        }

        if (!stopped && single && identifiers.Count != 1)
        {
            AddMalformed("A single-value identifier field must contain exactly one message identifier.", initialPosition, "All successfully parsed identifiers were retained in the result list.");
        }

        int consumed = stopped ? GetConsumedByteCount() : input.Length;
        return new EmailMessageIdParseResult(new EmailMessageIdList(identifiers, CopyRange(0, consumed)), issues, consumed, input.Length);
    }

    EmailMessageIdCandidate ParseAngleIdentifier()
    {
        int open = cursor;
        int close = -1;
        int scan = cursor + 1;
        while (scan < tokens.Count)
        {
            if (IsSpecialAt(scan, 62))
            {
                close = scan;
                break;
            }

            if (IsSpecialAt(scan, 60))
            {
                break;
            }

            scan++;
        }

        bool closed = close >= 0;
        int end = closed ? close : scan;
        if (!closed && scan >= tokens.Count)
        {
            end = tokens.Count;
        }

        EmailMessageIdCandidate candidate = ParseCore(open + 1, end, true, closed);
        candidate.nextCursor = closed ? close + 1 : end;
        if (!closed)
        {
            AddRecovery("An identifier is missing its closing angle bracket.", tokens[open].GetSourcePosition(), "The identifier content before the next boundary was retained when usable.");
            if (candidate.success)
            {
                candidate.identifier = CopyWithRecovery(candidate.identifier, true);
            }
        }

        return candidate;
    }

    EmailMessageIdCandidate ParseCore(int start, int end, bool hasOpeningBracket, bool hasClosingBracket)
    {
        EmailMessageIdCandidate candidate = new EmailMessageIdCandidate();
        int at = -1;
        int atCount = 0;
        int index = start;
        while (index < end)
        {
            if (IsSpecialAt(index, 64))
            {
                at = index;
                atCount++;
            }

            index++;
        }

        EmailSourcePosition position = start < tokens.Count ? tokens[start].GetSourcePosition() : initialPosition;
        if (atCount != 1)
        {
            AddMalformed("A message identifier must contain exactly one at-sign.", position, "The malformed identifier was skipped.");
            return candidate;
        }

        EmailMessageIdPartValue left = BuildPart(start, at, true);
        EmailMessageIdPartValue right = BuildPart(at + 1, end, false);
        if (!left.success || !right.success)
        {
            AddMalformed("A message identifier has an empty or malformed left or right side.", position, "The malformed identifier was skipped.");
            return candidate;
        }

        bool obsolete = left.obsolete || right.obsolete || HasCfws(start, end);
        bool recovered = !hasOpeningBracket || !hasClosingBracket;
        if (obsolete)
        {
            AddObsolete("A message identifier uses obsolete internal syntax.", position, "Internal CFWS and obsolete address forms were interpreted but will not be regenerated.");
        }

        int rawStartToken = hasOpeningBracket ? start - 1 : start;
        int rawEndToken = hasClosingBracket ? end : end - 1;
        int rawStart = GetTokenStart(rawStartToken);
        int rawEnd = rawEndToken >= rawStartToken ? GetTokenEnd(rawEndToken) : rawStart;
        byte[] raw = CopyRange(rawStart, rawEnd);
        EmailSourcePosition sourcePosition = tokens[rawStartToken].GetSourcePosition();
        candidate.identifier = new EmailMessageId(left.value, right.value, right.domainLiteral, raw, sourcePosition, obsolete, recovered);
        candidate.success = true;
        return candidate;
    }

    EmailMessageIdPartValue BuildPart(int start, int end, bool left)
    {
        EmailMessageIdPartValue part = new EmailMessageIdPartValue();
        List<int> significant = new List<int>();
        int index = start;
        while (index < end)
        {
            if (tokens[index].GetKind() != EmailHeaderTokenKind.Cfws)
            {
                significant.Add(index);
            }

            index++;
        }

        if (significant.Count == 0)
        {
            return part;
        }

        if (!left && significant.Count == 1 && tokens[significant[0]].GetKind() == EmailHeaderTokenKind.DomainLiteral && !tokens[significant[0]].IsMalformed())
        {
            EmailHeaderToken literal = tokens[significant[0]];
            part.success = true;
            part.value = Encoding.ASCII.GetString(literal.GetValueBytes());
            part.domainLiteral = true;
            part.obsolete = literal.IsObsolete() || HasWhitespaceOrEscape(literal.GetRawBytes()) || HasCfws(start, end);
            return part;
        }

        StringBuilder value = new StringBuilder();
        bool expectWord = true;
        index = 0;
        while (index < significant.Count)
        {
            EmailHeaderToken token = tokens[significant[index]];
            EmailHeaderTokenKind kind = token.GetKind();
            if (IsSpecialAt(significant[index], 46))
            {
                if (expectWord)
                {
                    return part;
                }

                value.Append('.');
                expectWord = true;
                index++;
                continue;
            }

            bool word = kind == EmailHeaderTokenKind.Atom || kind == EmailHeaderTokenKind.DotAtom || left && kind == EmailHeaderTokenKind.QuotedString;
            if (!word || !expectWord || token.IsMalformed())
            {
                return part;
            }

            string text = Encoding.ASCII.GetString(token.GetValueBytes());
            if ((kind == EmailHeaderTokenKind.Atom || kind == EmailHeaderTokenKind.DotAtom) && !IsDotAtomText(text))
            {
                return part;
            }

            value.Append(text);
            expectWord = false;
            if (kind == EmailHeaderTokenKind.QuotedString || token.IsObsolete())
            {
                part.obsolete = true;
            }

            index++;
        }

        if (expectWord || value.Length == 0)
        {
            return part;
        }

        part.success = true;
        part.value = value.ToString();
        part.domainLiteral = false;
        if (significant.Count != 1 || HasCfws(start, end))
        {
            part.obsolete = true;
        }

        return part;
    }

    void AddIdentifier(List<EmailMessageId> identifiers, EmailMessageId? identifier)
    {
        if (identifiers.Count >= options.GetMaximumIdentifiers())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.MessageIdLimitExceeded, "The identifier list exceeds the configured identifier-count limit.", (identifier ?? throw new InvalidOperationException()).GetSourcePosition(), null, null, "Parsing stopped before the excessive identifier."));
            long relative = identifier.GetSourcePosition().GetByteOffset() - initialPosition.GetByteOffset();
            stopByteCount = relative >= 0 && relative <= input.Length ? (int)relative : 0;
            stopped = true;
            return;
        }

        identifiers.Add((identifier ?? throw new InvalidOperationException()));
    }

    void ReportIgnoredText(EmailSourcePosition position)
    {
        if (ignoredTextReported)
        {
            return;
        }

        if (single)
        {
            AddRecovery("Text outside the message identifier was ignored.", position, "Only usable identifier syntax was retained.");
        }
        else
        {
            AddObsolete("An identifier list contains obsolete phrase text.", position, "The phrase was ignored as required by RFC 5322 obsolete syntax.");
        }

        ignoredTextReported = true;
    }

    void SkipCfws()
    {
        while (cursor < tokens.Count && tokens[cursor].GetKind() == EmailHeaderTokenKind.Cfws)
        {
            cursor++;
        }
    }

    int FindBareSegmentEnd(int start)
    {
        int index = start;
        while (index < tokens.Count)
        {
            if (tokens[index].GetKind() == EmailHeaderTokenKind.Cfws || IsSpecialAt(index, 44) || IsSpecialAt(index, 60) || IsSpecialAt(index, 62))
            {
                break;
            }

            index++;
        }

        return index;
    }

    bool HasAtSign(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (IsSpecialAt(index, 64))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool HasCfws(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (tokens[index].GetKind() == EmailHeaderTokenKind.Cfws)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool IsSpecialAt(int index, byte expected)
    {
        if (index < 0 || index >= tokens.Count)
        {
            return false;
        }

        EmailHeaderToken token = tokens[index];
        byte[] value = token.GetValueBytes();
        return token.GetKind() == EmailHeaderTokenKind.Special && value.Length == 1 && value[0] == expected;
    }

    int GetConsumedByteCount()
    {
        if (stopByteCount >= 0)
        {
            return stopByteCount;
        }

        if (cursor >= tokens.Count)
        {
            return lexicalConsumedByteCount;
        }

        return GetTokenStart(cursor);
    }

    int GetTokenStart(int index)
    {
        if (index < 0 || index >= tokens.Count)
        {
            return input.Length;
        }

        long relative = tokens[index].GetSourcePosition().GetByteOffset() - initialPosition.GetByteOffset();
        if (relative < 0 || relative > input.Length)
        {
            return input.Length;
        }

        return (int)relative;
    }

    int GetTokenEnd(int index)
    {
        int start = GetTokenStart(index);
        int length = tokens[index].GetByteCount();
        return start + length > input.Length ? input.Length : start + length;
    }

    byte[] CopyRange(int start, int end)
    {
        if (start < 0)
        {
            start = 0;
        }

        if (end < start)
        {
            end = start;
        }

        if (end > input.Length)
        {
            end = input.Length;
        }

        byte[] copy = new byte[end - start];
        int index = start;
        while (index < end)
        {
            copy[index - start] = input[index];
            index++;
        }

        return copy;
    }

    EmailMessageId CopyWithRecovery(EmailMessageId? value, bool recovered)
    {
        return new EmailMessageId((value ?? throw new InvalidOperationException()).GetIdLeft(), value.GetIdRight(), value.IsRightDomainLiteral(), value.GetRawBytes(), value.GetSourcePosition(), value.UsesObsoleteSyntax(), recovered);
    }

    void AddMalformed(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.MessageIdMalformed, description, position, recovery);
    }

    void AddObsolete(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.MessageIdObsoleteSyntax, description, position, recovery);
    }

    void AddRecovery(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.MessageIdRecoveryApplied, description, position, recovery);
    }

    void AddRecoverable(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailMessageIdParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static bool IsDotAtomText(string value)
    {
        if (value == null || value.Length == 0 || value[0] == '.' || value[value.Length - 1] == '.')
        {
            return false;
        }

        bool previousDot = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                if (previousDot)
                {
                    return false;
                }

                previousDot = true;
            }
            else
            {
                if (character > 127 || !new EmailByteScanner().IsAtext((byte)character))
                {
                    return false;
                }

                previousDot = false;
            }

            index++;
        }

        return true;
    }

    static bool HasWhitespaceOrEscape(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 9 || value[index] == 10 || value[index] == 13 || value[index] == 32 || value[index] == 92)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
