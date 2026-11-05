using System;
using System.Collections.Generic;

namespace email;
internal class EmailHeaderLexicalFlags
{
    internal bool obsolete;
    internal bool malformed;
    internal int quotedPairCount;
    internal int maximumCommentDepth;

    internal EmailHeaderLexicalFlags()
    {
        obsolete = false;
        malformed = false;
        quotedPairCount = 0;
        maximumCommentDepth = 0;
    }
}
internal class EmailHeaderLexicalWorker
{
    byte[] input;
    EmailHeaderLexicalOptions options;
    EmailSourceTracker tracker;
    List<EmailHeaderToken> tokens;
    EmailIssueList issues;
    int index;
    bool stopped;

    internal EmailHeaderLexicalWorker(byte[] input, EmailHeaderLexicalOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        this.options = options;
        tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tokens = new List<EmailHeaderToken>();
        issues = new EmailIssueList();
        index = 0;
        stopped = false;
    }

    internal EmailHeaderLexicalResult Scan()
    {
        while (index < input.Length && !stopped)
        {
            if (tokens.Count >= options.GetMaximumTokens())
            {
                AddFatalIssue(EmailIssueCode.HeaderLexicalTokenLimitExceeded, "The header value contains more lexical tokens than the configured limit.", tracker.GetPosition(), "Scanning stopped before the next token.");
                stopped = true;
                break;
            }

            byte value = input[index];
            if (IsCfwsStart(index))
            {
                ScanCfws();
            }
            else if (value == 34)
            {
                ScanQuotedString();
            }
            else if (value == 91)
            {
                ScanDomainLiteral();
            }
            else if (IsAtext(value) || value == 46 && index + 1 < input.Length && IsAtext(input[index + 1]))
            {
                ScanAtomLike();
            }
            else if (IsSpecial(value))
            {
                ScanSpecial();
            }
            else if (IsObsoleteControl(value))
            {
                ScanObsoleteByte();
            }
            else
            {
                ScanInvalidByte();
            }
        }

        byte[] remaining = CopyRange(index, input.Length);
        return new EmailHeaderLexicalResult(tokens, issues, input.Length, index, remaining);
    }

    bool IsCfwsStart(int position)
    {
        if (input[position] == 40 || new EmailByteScanner().IsWsp(input[position]))
        {
            return true;
        }

        return IsFoldAt(position);
    }

    void ScanCfws()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        bool consumed = true;
        while (consumed && !stopped)
        {
            consumed = false;
            if (index < input.Length && (new EmailByteScanner().IsWsp(input[index]) || IsFoldAt(index)))
            {
                ScanFws(start, flags, null, false);
                consumed = true;
            }

            if (!stopped && index < input.Length && input[index] == 40)
            {
                ScanComment(start, flags);
                consumed = true;
            }
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.Cfws, start, position, new byte[] { 32 }, flags);
        }
    }

    void ScanComment(int tokenStart, EmailHeaderLexicalFlags flags)
    {
        int depth = 0;
        bool closed = false;
        while (index < input.Length && !stopped)
        {
            byte value = input[index];
            if (value == 40)
            {
                if (depth >= options.GetMaximumCommentDepth())
                {
                    AddFatalIssue(EmailIssueCode.HeaderLexicalCommentDepthExceeded, "A comment exceeds the configured nesting-depth limit.", tracker.GetPosition(), "Scanning stopped before the excessive nested comment.");
                    stopped = true;
                    break;
                }

                if (!CanConsume(tokenStart))
                {
                    break;
                }

                Advance();
                depth++;
                if (depth > flags.maximumCommentDepth)
                {
                    flags.maximumCommentDepth = depth;
                }

                continue;
            }

            if (value == 41)
            {
                if (!CanConsume(tokenStart))
                {
                    break;
                }

                Advance();
                depth--;
                if (depth == 0)
                {
                    closed = true;
                    break;
                }

                continue;
            }

            if (value == 92)
            {
                ScanQuotedPair(tokenStart, flags, null);
                continue;
            }

            if (new EmailByteScanner().IsWsp(value) || IsFoldAt(index))
            {
                ScanFws(tokenStart, flags, null, false);
                continue;
            }

            if (IsCommentText(value) || options.GetAllowInternationalized() && value >= 128)
            {
                if (!CanConsume(tokenStart))
                {
                    break;
                }

                Advance();
                continue;
            }

            EmailSourcePosition defectPosition = tracker.GetPosition();
            if (IsObsoleteControl(value))
            {
                MarkObsolete(flags, defectPosition, "A comment contains obsolete control text.", "The obsolete byte was retained in the comment token.");
            }
            else
            {
                MarkMalformed(flags, defectPosition, "A comment contains a byte that is not permitted by RFC 5322.", "The byte was retained in the comment token.");
            }

            if (!CanConsume(tokenStart))
            {
                break;
            }

            Advance();
        }

        if (!closed && !stopped)
        {
            MarkMalformed(flags, tracker.GetPosition(), "A comment is missing its closing parenthesis.", "The available comment bytes were retained as one token.");
        }
    }

    void ScanQuotedString()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        List<byte> valueBytes = new List<byte>();
        bool closed = false;
        if (CanConsume(start))
        {
            Advance();
        }

        while (index < input.Length && !stopped)
        {
            byte value = input[index];
            if (value == 34)
            {
                if (!CanConsume(start))
                {
                    break;
                }

                Advance();
                closed = true;
                break;
            }

            if (value == 92)
            {
                ScanQuotedPair(start, flags, valueBytes);
                continue;
            }

            if (new EmailByteScanner().IsWsp(value) || IsFoldAt(index))
            {
                ScanFws(start, flags, valueBytes, true);
                continue;
            }

            EmailSourcePosition defectPosition = tracker.GetPosition();
            if (IsQuotedText(value) || options.GetAllowInternationalized() && value >= 128)
            {
                if (!CanConsume(start))
                {
                    break;
                }

                valueBytes.Add(value);
                Advance();
                continue;
            }

            if (IsObsoleteControl(value))
            {
                MarkObsolete(flags, defectPosition, "A quoted string contains obsolete control text.", "The obsolete byte was retained in the decoded token value.");
            }
            else
            {
                MarkMalformed(flags, defectPosition, "A quoted string contains a byte that is not permitted by RFC 5322.", "The byte was retained in the decoded token value.");
            }

            if (!CanConsume(start))
            {
                break;
            }

            valueBytes.Add(value);
            Advance();
        }

        if (!closed && !stopped)
        {
            MarkMalformed(flags, tracker.GetPosition(), "A quoted string is missing its closing quotation mark.", "The available bytes were decoded as one quoted-string token.");
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.QuotedString, start, position, valueBytes.ToArray(), flags);
        }
    }

    void ScanDomainLiteral()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        List<byte> valueBytes = new List<byte>();
        bool closed = false;
        if (CanConsume(start))
        {
            Advance();
        }

        while (index < input.Length && !stopped)
        {
            byte value = input[index];
            if (value == 93)
            {
                if (!CanConsume(start))
                {
                    break;
                }

                Advance();
                closed = true;
                break;
            }

            if (value == 92)
            {
                EmailSourcePosition pairPosition = tracker.GetPosition();
                bool canonicalPair = index + 1 < input.Length && (new EmailByteScanner().IsVisibleAscii(input[index + 1]) || new EmailByteScanner().IsWsp(input[index + 1]));
                ScanQuotedPair(start, flags, valueBytes);
                if (canonicalPair)
                {
                    MarkObsolete(flags, pairPosition, "A domain literal uses an obsolete quoted pair.", "The quoted-pair escape was removed from the decoded token value.");
                }

                continue;
            }

            if (new EmailByteScanner().IsWsp(value) || IsFoldAt(index))
            {
                ScanFws(start, flags, valueBytes, true);
                continue;
            }

            EmailSourcePosition defectPosition = tracker.GetPosition();
            if (IsDomainText(value) || options.GetAllowInternationalized() && value >= 128)
            {
                if (!CanConsume(start))
                {
                    break;
                }

                valueBytes.Add(value);
                Advance();
                continue;
            }

            if (IsObsoleteControl(value))
            {
                MarkObsolete(flags, defectPosition, "A domain literal contains obsolete control text.", "The obsolete byte was retained in the decoded token value.");
            }
            else
            {
                MarkMalformed(flags, defectPosition, "A domain literal contains a byte that is not permitted by RFC 5322.", "The byte was retained in the decoded token value.");
            }

            if (!CanConsume(start))
            {
                break;
            }

            valueBytes.Add(value);
            Advance();
        }

        if (!closed && !stopped)
        {
            MarkMalformed(flags, tracker.GetPosition(), "A domain literal is missing its closing bracket.", "The available bytes were decoded as one domain-literal token.");
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.DomainLiteral, start, position, valueBytes.ToArray(), flags);
        }
    }

    void ScanAtomLike()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        bool hasDot = false;
        bool malformed = false;
        bool previousDot = false;
        while (index < input.Length && (IsAtext(input[index]) || input[index] == 46) && !stopped)
        {
            byte value = input[index];
            if (value == 46)
            {
                hasDot = true;
                if (index == start || previousDot)
                {
                    malformed = true;
                }

                previousDot = true;
            }
            else
            {
                previousDot = false;
            }

            if (!CanConsume(start))
            {
                break;
            }

            Advance();
        }

        if (previousDot)
        {
            malformed = true;
        }

        EmailHeaderTokenKind kind = EmailHeaderTokenKind.Atom;
        if (hasDot)
        {
            kind = EmailHeaderTokenKind.DotAtom;
        }

        if (malformed)
        {
            kind = EmailHeaderTokenKind.Invalid;
            MarkMalformed(flags, position, "A dot-atom has a leading, trailing, or repeated dot.", "The complete malformed dot-atom was retained as one token.");
        }

        if (index > start)
        {
            AddToken(kind, start, position, CopyRange(start, index), flags);
        }
    }

    void ScanSpecial()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        if (CanConsume(start))
        {
            Advance();
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.Special, start, position, CopyRange(start, index), flags);
        }
    }

    void ScanObsoleteByte()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        MarkObsolete(flags, position, "The header value contains an obsolete control byte.", "The byte was retained as an obsolete token.");
        if (CanConsume(start))
        {
            Advance();
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.Obsolete, start, position, CopyRange(start, index), flags);
        }
    }

    void ScanInvalidByte()
    {
        int start = index;
        EmailSourcePosition position = tracker.GetPosition();
        EmailHeaderLexicalFlags flags = new EmailHeaderLexicalFlags();
        MarkMalformed(flags, position, "The header value contains a byte that is not part of the RFC 5322 lexical grammar.", "The byte was retained as an invalid token.");
        if (CanConsume(start))
        {
            Advance();
        }

        if (index > start)
        {
            AddToken(EmailHeaderTokenKind.Invalid, start, position, CopyRange(start, index), flags);
        }
    }

    void ScanQuotedPair(int tokenStart, EmailHeaderLexicalFlags flags, List<byte>? valueBytes)
    {
        EmailSourcePosition position = tracker.GetPosition();
        if (!CanConsume(tokenStart))
        {
            return;
        }

        Advance();
        flags.quotedPairCount++;
        if (index >= input.Length)
        {
            MarkMalformed(flags, position, "A quoted-pair escape has no following byte.", "The trailing reverse solidus was retained only in the raw token.");
            return;
        }

        byte escaped = input[index];
        if (!new EmailByteScanner().IsVisibleAscii(escaped) && !new EmailByteScanner().IsWsp(escaped))
        {
            if (escaped <= 127)
            {
                MarkObsolete(flags, position, "A quoted pair uses the obsolete quoted-pair grammar.", "The escaped byte was retained in the decoded token value.");
            }
            else
            {
                MarkMalformed(flags, position, "A quoted pair escapes a non-ASCII byte.", "The escaped byte was retained in the decoded token value.");
            }
        }

        if (!CanConsume(tokenStart))
        {
            return;
        }

        if (valueBytes != null)
        {
            valueBytes.Add(escaped);
        }

        Advance();
    }

    void ScanFws(int tokenStart, EmailHeaderLexicalFlags flags, List<byte>? valueBytes, bool appendWhitespace)
    {
        int foldCount = 0;
        bool scanning = true;
        while (scanning && !stopped)
        {
            while (index < input.Length && new EmailByteScanner().IsWsp(input[index]) && !stopped)
            {
                byte value = input[index];
                if (!CanConsume(tokenStart))
                {
                    break;
                }

                if (appendWhitespace && valueBytes != null)
                {
                    valueBytes.Add(value);
                }

                Advance();
            }

            if (!stopped && IsFoldAt(index))
            {
                EmailSourcePosition foldPosition = tracker.GetPosition();
                if (!CanConsume(tokenStart))
                {
                    break;
                }

                Advance();
                if (!CanConsume(tokenStart))
                {
                    break;
                }

                Advance();
                foldCount++;
                if (foldCount > 1)
                {
                    MarkObsolete(flags, foldPosition, "Folding white space contains multiple consecutive folds from the obsolete grammar.", "The folds were unfolded while preserving surrounding white space.");
                }
            }
            else
            {
                scanning = false;
            }
        }
    }

    bool CanConsume(int tokenStart)
    {
        if (index - tokenStart < options.GetMaximumTokenBytes())
        {
            return true;
        }

        AddFatalIssue(EmailIssueCode.HeaderLexicalTokenByteLimitExceeded, "A lexical token exceeds the configured byte limit.", tracker.GetPosition(), "Only the prefix within the configured limit was retained.");
        stopped = true;
        return false;
    }

    void AddToken(EmailHeaderTokenKind kind, int start, EmailSourcePosition position, byte[] valueBytes, EmailHeaderLexicalFlags flags)
    {
        byte[] rawBytes = CopyRange(start, index);
        int invalidUtf8 = EmailHeaderUtf8.GetInvalidIndex(rawBytes);
        if (options.GetAllowInternationalized() && invalidUtf8 >= 0)
        {
            EmailSourceTracker invalidTracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(position);
            invalidTracker.Advance(new ReadOnlySpan<byte>(rawBytes, 0, invalidUtf8));
            MarkMalformed(flags, invalidTracker.GetPosition(), "An internationalized lexical token contains malformed UTF-8.", "The malformed source bytes were retained in the token.");
        }

        tokens.Add(new EmailHeaderToken(kind, rawBytes, valueBytes, position, flags.obsolete, flags.malformed, flags.quotedPairCount, flags.maximumCommentDepth));
    }

    void MarkObsolete(EmailHeaderLexicalFlags flags, EmailSourcePosition position, string description, string recovery)
    {
        flags.obsolete = true;
        AddRecoverableIssue(EmailIssueCode.HeaderLexicalObsolete, description, position, recovery);
    }

    void MarkMalformed(EmailHeaderLexicalFlags flags, EmailSourcePosition position, string description, string recovery)
    {
        flags.malformed = true;
        AddRecoverableIssue(EmailIssueCode.HeaderLexicalMalformed, description, position, recovery);
    }

    void AddRecoverableIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = EmailIssueSeverity.Error;
        if (options.GetMode() == EmailHeaderLexicalMode.Tolerant)
        {
            severity = EmailIssueSeverity.Warning;
        }

        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    void AddFatalIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, code, description, position, null, null, recovery));
    }

    void Advance()
    {
        tracker.Advance(new ReadOnlySpan<byte>(input, index, 1));
        index++;
    }

    byte[] CopyRange(int start, int end)
    {
        int length = end - start;
        byte[] copy = new byte[length];
        if (length > 0)
        {
            Array.Copy(input, start, copy, 0, length);
        }

        return copy;
    }

    bool IsFoldAt(int position)
    {
        return position + 2 < input.Length && input[position] == 13 && input[position + 1] == 10 && new EmailByteScanner().IsWsp(input[position + 2]);
    }

    bool IsAtext(byte value)
    {
        return new EmailByteScanner().IsAtext(value) || options.GetAllowInternationalized() && value >= 128;
    }

    static bool IsSpecial(byte value)
    {
        return value == 40 || value == 41 || value == 60 || value == 62 || value == 91 || value == 93 || value == 58 || value == 59 || value == 64 || value == 92 || value == 44 || value == 46 || value == 34;
    }

    static bool IsCommentText(byte value)
    {
        return value >= 33 && value <= 39 || value >= 42 && value <= 91 || value >= 93 && value <= 126;
    }

    static bool IsQuotedText(byte value)
    {
        return value == 33 || value >= 35 && value <= 91 || value >= 93 && value <= 126;
    }

    static bool IsDomainText(byte value)
    {
        return value >= 33 && value <= 90 || value >= 94 && value <= 126;
    }

    static bool IsObsoleteControl(byte value)
    {
        return value >= 1 && value <= 8 || value == 11 || value == 12 || value >= 14 && value <= 31 || value == 127;
    }
}
