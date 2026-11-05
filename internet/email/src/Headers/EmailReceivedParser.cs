using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailReceivedParserWorker
{
    byte[] input;
    List<EmailHeaderToken> tokens;
    EmailReceivedParserOptions options;
    EmailSourcePosition initialPosition;
    EmailIssueList issues;
    int lexicalConsumedByteCount;
    int traceEnd;
    int dateStart;
    int delimiterToken;
    int lastKnownRank;
    int stopByteCount;

    internal EmailReceivedParserWorker(byte[] input, EmailHeaderLexicalResult lexical, EmailReceivedParserOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        tokens = lexical.GetTokens();
        this.options = options;
        this.initialPosition = initialPosition;
        issues = lexical.GetIssues();
        lexicalConsumedByteCount = lexical.GetConsumedByteCount();
        traceEnd = input.Length;
        dateStart = input.Length;
        delimiterToken = -1;
        lastKnownRank = -1;
        stopByteCount = -1;
    }

    internal EmailReceivedParseResult Parse()
    {
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal))
        {
            return new EmailReceivedParseResult(null, issues, lexicalConsumedByteCount, input.Length);
        }

        FindDateDelimiter();
        List<int> significant = GetTraceSignificantTokens();
        List<EmailReceivedClause> clauses = ParseClauses(significant);
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal))
        {
            int consumed = stopByteCount >= 0 ? stopByteCount : lexicalConsumedByteCount;
            EmailReceived partial = new EmailReceived(clauses, null, delimiterToken >= 0, CopyRange(0, traceEnd), delimiterToken >= 0 ? CopyRange(dateStart, input.Length) : new byte[0], input, initialPosition);
            return new EmailReceivedParseResult(partial, issues, consumed, input.Length);
        }

        EmailDateTime? date = ParseDate();
        EmailReceived value = new EmailReceived(clauses, date, delimiterToken >= 0, CopyRange(0, traceEnd), delimiterToken >= 0 ? CopyRange(dateStart, input.Length) : new byte[0], input, initialPosition);
        return new EmailReceivedParseResult(value, issues, input.Length, input.Length);
    }

    void FindDateDelimiter()
    {
        int index = tokens.Count - 1;
        while (index >= 0)
        {
            if (IsSpecial(index, 59))
            {
                delimiterToken = index;
                traceEnd = GetTokenOffset(index);
                dateStart = traceEnd + tokens[index].GetByteCount();
                return;
            }

            index--;
        }
    }

    List<int> GetTraceSignificantTokens()
    {
        List<int> significant = new List<int>();
        int index = 0;
        while (index < tokens.Count)
        {
            int offset = GetTokenOffset(index);
            if (offset >= traceEnd)
            {
                break;
            }

            if (tokens[index].GetKind() != EmailHeaderTokenKind.Cfws)
            {
                significant.Add(index);
            }

            index++;
        }

        return significant;
    }

    List<EmailReceivedClause> ParseClauses(List<int> significant)
    {
        List<EmailReceivedClause> clauses = new List<EmailReceivedClause>();
        int cursor = 0;
        while (cursor < significant.Count)
        {
            int nameTokenIndex = significant[cursor];
            EmailHeaderToken nameToken = tokens[nameTokenIndex];
            EmailReceivedClauseKind kind = GetClauseKind(nameToken);
            string originalName = kind == EmailReceivedClauseKind.Extension && nameToken.GetKind() != EmailHeaderTokenKind.Atom ? "" : GetTokenText(nameToken);
            bool malformed = false;
            int next = cursor + 1;

            if (nameToken.GetKind() != EmailHeaderTokenKind.Atom)
            {
                malformed = true;
                AddMalformed("A Received clause does not begin with an atom name.", nameToken.GetSourcePosition(), "The unrecognized token was retained as an anonymous extension clause.");
            }
            else
            {
                int rank = GetKnownRank(kind);
                if (rank >= 0)
                {
                    if (rank <= lastKnownRank)
                    {
                        malformed = true;
                        AddMalformed("A standard Received clause is duplicated or appears out of order.", nameToken.GetSourcePosition(), "The clause was retained in its original position.");
                    }

                    if (rank > lastKnownRank)
                    {
                        lastKnownRank = rank;
                    }
                }

                if (next >= significant.Count || IsStandardIntroducer(significant[next]))
                {
                    malformed = true;
                    AddMalformed("A Received clause has no value.", nameToken.GetSourcePosition(), "The empty clause was retained.");
                }
                else
                {
                    if (!HasCfwsBetween(nameTokenIndex, significant[next]))
                    {
                        malformed = true;
                        AddMalformed("A Received clause name is not separated from its value by folding whitespace.", nameToken.GetSourcePosition(), "The adjacent value token was retained.");
                    }

                    int issueCount = issues.GetCount();
                    next = FindValueEnd(significant, next, kind);
                    if (issues.GetCount() > issueCount)
                    {
                        malformed = true;
                    }
                }
            }

            if (clauses.Count >= options.GetMaximumClauses())
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.ReceivedLimitExceeded, "The Received value exceeds the configured clause-count limit.", nameToken.GetSourcePosition(), null, "Received", "Parsing stopped before the excessive clause."));
                stopByteCount = GetTokenOffset(nameTokenIndex);
                break;
            }

            int rawStart = clauses.Count == 0 ? 0 : GetTokenOffset(nameTokenIndex);
            int rawEnd = next < significant.Count ? GetTokenOffset(significant[next]) : traceEnd;
            int nameEnd = GetTokenOffset(nameTokenIndex) + nameToken.GetByteCount();
            if (nameEnd > rawEnd)
            {
                nameEnd = rawEnd;
            }

            clauses.Add(new EmailReceivedClause(kind, originalName, CopyRange(rawStart, rawEnd), CopyRange(nameEnd, rawEnd), nameToken.GetSourcePosition(), malformed));
            cursor = next > cursor ? next : cursor + 1;
        }

        return clauses;
    }

    int FindValueEnd(List<int> significant, int valueCursor, EmailReceivedClauseKind kind)
    {
        int firstToken = significant[valueCursor];
        if ((kind == EmailReceivedClauseKind.Id || kind == EmailReceivedClauseKind.For) && IsSpecial(firstToken, 60))
        {
            int cursor = valueCursor + 1;
            while (cursor < significant.Count)
            {
                if (IsSpecial(significant[cursor], 62))
                {
                    return cursor + 1;
                }

                cursor++;
            }

            AddMalformed("An angle-delimited Received clause value is missing its closing bracket.", tokens[firstToken].GetSourcePosition(), "The available value bytes were retained.");
            return significant.Count;
        }

        if (kind == EmailReceivedClauseKind.Id || kind == EmailReceivedClauseKind.For)
        {
            int cursor = valueCursor + 1;
            int previousToken = firstToken;
            while (cursor < significant.Count && !HasCfwsBetween(previousToken, significant[cursor]))
            {
                previousToken = significant[cursor];
                cursor++;
            }

            return cursor;
        }

        return valueCursor + 1;
    }

    EmailDateTime? ParseDate()
    {
        if (delimiterToken < 0)
        {
            AddDateIssue(EmailIssueCode.ReceivedDateMissing, "The Received value has no semicolon-delimited timestamp.", initialPosition, "The trace clauses and original value were retained without a date.");
            return null;
        }

        EmailSourcePosition position = GetPositionAt(dateStart);
        if (dateStart >= input.Length)
        {
            AddDateIssue(EmailIssueCode.ReceivedDateMissing, "The Received value has an empty timestamp after its semicolon.", position, "The trace clauses and original value were retained without a date.");
            return null;
        }

        EmailDateParserOptions dateOptions = new EmailDateParserOptions();
        dateOptions.SetMode(options.GetMode() == EmailReceivedParseMode.Strict ? EmailDateParseMode.Strict : EmailDateParseMode.Tolerant);
        dateOptions.SetMaximumInputBytes(options.GetMaximumInputBytes());
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(input.AsSpan(dateStart), dateOptions, position);
        issues.AddRange(result.GetIssues());
        if (!result.HasValue())
        {
            AddDateIssue(EmailIssueCode.ReceivedDateMalformed, "The Received timestamp is malformed.", position, "The timestamp bytes were retained without a structured date.");
            return null;
        }

        return result.GetValue();
    }

    EmailReceivedClauseKind GetClauseKind(EmailHeaderToken token)
    {
        if (token.GetKind() != EmailHeaderTokenKind.Atom)
        {
            return EmailReceivedClauseKind.Extension;
        }

        string text = GetTokenText(token);
        if (string.Equals(text, "from", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.From;
        }

        if (string.Equals(text, "by", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.By;
        }

        if (string.Equals(text, "via", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.Via;
        }

        if (string.Equals(text, "with", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.With;
        }

        if (string.Equals(text, "id", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.Id;
        }

        if (string.Equals(text, "for", StringComparison.OrdinalIgnoreCase))
        {
            return EmailReceivedClauseKind.For;
        }

        return EmailReceivedClauseKind.Extension;
    }

    int GetKnownRank(EmailReceivedClauseKind kind)
    {
        if (kind == EmailReceivedClauseKind.Extension)
        {
            return -1;
        }

        return (int)kind;
    }

    bool IsStandardIntroducer(int tokenIndex)
    {
        return GetClauseKind(tokens[tokenIndex]) != EmailReceivedClauseKind.Extension;
    }

    bool HasCfwsBetween(int leftToken, int rightToken)
    {
        int index = leftToken + 1;
        while (index < rightToken)
        {
            if (tokens[index].GetKind() == EmailHeaderTokenKind.Cfws)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool IsSpecial(int tokenIndex, byte value)
    {
        EmailHeaderToken token = tokens[tokenIndex];
        byte[] bytes = token.GetValueBytes();
        return token.GetKind() == EmailHeaderTokenKind.Special && bytes.Length == 1 && bytes[0] == value;
    }

    string GetTokenText(EmailHeaderToken token)
    {
        return Encoding.ASCII.GetString(token.GetRawBytes());
    }

    int GetTokenOffset(int tokenIndex)
    {
        return GetOffset(tokens[tokenIndex].GetSourcePosition());
    }

    int GetOffset(EmailSourcePosition position)
    {
        long relative = position.GetByteOffset() - initialPosition.GetByteOffset();
        if (relative < 0 || relative > input.Length)
        {
            return 0;
        }

        return (int)relative;
    }

    EmailSourcePosition GetPositionAt(int offset)
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tracker.Advance(input.AsSpan(0, offset));
        return tracker.GetPosition();
    }

    void AddMalformed(string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailReceivedParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.ReceivedMalformed, description, position, null, "Received", recovery));
    }

    void AddDateIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailReceivedParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, "Received", recovery));
    }

    byte[] CopyRange(int start, int end)
    {
        int length = end - start;
        byte[] copy = new byte[length];
        input.AsSpan(start, length).CopyTo(copy);
        return copy;
    }
}
