using System;
using System.Collections.Generic;

namespace email;

internal class EmailEncodedWordParseState
{
    List<EmailEncodedWordPart> parts;
    List<byte> literalBytes;
    EmailSourcePosition? literalPosition;
    EmailIssueList issues;
    int encodedWordCount;
    long decodedByteCount;

    internal EmailEncodedWordParseState()
    {
        parts = new List<EmailEncodedWordPart>();
        literalBytes = new List<byte>();
        literalPosition = null;
        issues = new EmailIssueList();
        encodedWordCount = 0;
        decodedByteCount = 0;
    }

    internal void AppendLiteral(ReadOnlySpan<byte> value, EmailSourcePosition position)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (literalBytes.Count == 0)
        {
            literalPosition = position;
        }

        int index = 0;
        while (index < value.Length)
        {
            literalBytes.Add(value[index]);
            index++;
        }
    }

    internal void AddEncodedWord(EmailEncodedWordPart part)
    {
        bool suppressWhitespace = false;
        if (parts.Count > 0 && parts[parts.Count - 1].IsEncodedWord() && LiteralIsLinearWhitespace())
        {
            suppressWhitespace = true;
        }

        if (suppressWhitespace)
        {
            literalBytes.Clear();
            literalPosition = null;
        }
        else
        {
            FlushLiteral();
        }

        parts.Add(part);
        encodedWordCount++;
        decodedByteCount = new EmailSafeArithmetic().AddNonNegative(decodedByteCount, part.GetByteCount());
    }

    internal void AddIssue(EmailIssue issue)
    {
        issues.Add(issue);
    }

    internal int GetEncodedWordCount()
    {
        return encodedWordCount;
    }

    internal long GetDecodedByteCount()
    {
        return decodedByteCount;
    }

    internal EmailEncodedWordParseResult CreateResult()
    {
        FlushLiteral();
        return new EmailEncodedWordParseResult(parts, issues, encodedWordCount, decodedByteCount);
    }

    void FlushLiteral()
    {
        if (literalBytes.Count == 0)
        {
            return;
        }

        parts.Add(new EmailEncodedWordPartOperations().CreateLiteral(literalBytes.ToArray(), literalPosition));
        literalBytes.Clear();
        literalPosition = null;
    }

    bool LiteralIsLinearWhitespace()
    {
        if (literalBytes.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < literalBytes.Count)
        {
            byte value = literalBytes[index];
            if (value != 9 && value != 10 && value != 13 && value != 32)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
