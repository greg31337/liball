using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailMetadataParserWorker
{
    byte[] input;
    EmailMetadataParserOptions options;
    EmailSourcePosition initialPosition;
    List<EmailHeaderToken> tokens;
    EmailIssueList issues;
    int lexicalConsumedByteCount;
    int stopByteCount;

    internal EmailMetadataParserWorker(byte[] input, EmailMetadataParserOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        this.options = options;
        this.initialPosition = initialPosition;
        EmailHeaderLexicalOptions lexicalOptions = new EmailHeaderLexicalOptions();
        lexicalOptions.SetMode(options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailHeaderLexicalMode.Strict : EmailHeaderLexicalMode.Tolerant);
        lexicalOptions.SetAllowInternationalized(true);
        lexicalOptions.SetMaximumTokenBytes(options.GetMaximumInputBytes());
        EmailHeaderLexicalResult lexical = new EmailHeaderLexicalScanner().ScanWithInputAndOptionsAndInitialPosition(input, lexicalOptions, initialPosition);
        tokens = lexical.GetTokens();
        issues = lexical.GetIssues();
        lexicalConsumedByteCount = lexical.GetConsumedByteCount();
        stopByteCount = -1;
    }

    internal EmailMetadataParseResult ParseKeywords()
    {
        if (HasFatal())
        {
            return Result(EmailMetadataValueKind.Keywords, null, lexicalConsumedByteCount);
        }

        List<EmailKeyword> keywords = new List<EmailKeyword>();
        int segmentStart = 0;
        int tokenStart = 0;
        int index = 0;
        while (index < tokens.Count)
        {
            if (IsSpecial(index, 44))
            {
                ParseKeywordSegment(keywords, segmentStart, GetTokenOffset(index), tokenStart, index);
                segmentStart = GetTokenOffset(index) + tokens[index].GetByteCount();
                tokenStart = index + 1;
                if (HasFatal())
                {
                    break;
                }
            }

            index++;
        }

        if (!HasFatal())
        {
            ParseKeywordSegment(keywords, segmentStart, input.Length, tokenStart, tokens.Count);
        }

        EmailKeywordList value = new EmailKeywordList(keywords, input);
        int consumed = HasFatal() ? GetStoppedByteCount(segmentStart) : input.Length;
        return Result(EmailMetadataValueKind.Keywords, value, consumed);
    }

    internal EmailMetadataParseResult ParsePriority(bool xPriority)
    {
        List<int> significant = GetSignificant(0, tokens.Count);
        EmailSourcePosition position = significant.Count == 0 ? initialPosition : tokens[significant[0]].GetSourcePosition();
        string original = GetSingleToken(significant, EmailIssueCode.MetadataValueMalformed, xPriority ? "X-Priority must contain one numeric token." : "Priority must contain one token.");
        EmailPriorityKind kind = EmailPriorityKind.Unknown;
        int numeric = 0;
        if (xPriority)
        {
            if (original.Length == 1 && original[0] >= '1' && original[0] <= '5')
            {
                numeric = original[0] - '0';
                kind = numeric <= 2 ? EmailPriorityKind.Urgent : numeric == 3 ? EmailPriorityKind.Normal : EmailPriorityKind.NonUrgent;
            }
            else if (!HasFatal())
            {
                AddIssue(EmailIssueCode.MetadataValueMalformed, "X-Priority must use a number from 1 through 5.", position, "The original priority token was retained as unknown.");
            }
        }
        else if (string.Equals(original, "non-urgent", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailPriorityKind.NonUrgent;
        }
        else if (string.Equals(original, "normal", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailPriorityKind.Normal;
        }
        else if (string.Equals(original, "urgent", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailPriorityKind.Urgent;
        }
        else if (original.Length > 0 && !HasFatal())
        {
            AddIssue(EmailIssueCode.MetadataValueMalformed, "Priority is not normal, non-urgent, or urgent.", position, "The original priority token was retained as unknown.");
        }

        EmailPriority value = new EmailPriority(kind, original, xPriority, numeric, input, position);
        return Result(EmailMetadataValueKind.Priority, value, HasFatal() ? lexicalConsumedByteCount : input.Length);
    }

    internal EmailMetadataParseResult ParseImportance()
    {
        List<int> significant = GetSignificant(0, tokens.Count);
        EmailSourcePosition position = significant.Count == 0 ? initialPosition : tokens[significant[0]].GetSourcePosition();
        string original = GetSingleToken(significant, EmailIssueCode.MetadataValueMalformed, "Importance must contain one token.");
        EmailImportanceKind kind = EmailImportanceKind.Unknown;
        if (string.Equals(original, "low", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailImportanceKind.Low;
        }
        else if (string.Equals(original, "normal", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailImportanceKind.Normal;
        }
        else if (string.Equals(original, "high", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailImportanceKind.High;
        }
        else if (original.Length > 0 && !HasFatal())
        {
            AddIssue(EmailIssueCode.MetadataValueMalformed, "Importance is not low, normal, or high.", position, "The original importance token was retained as unknown.");
        }

        return Result(EmailMetadataValueKind.Importance, new EmailImportance(kind, original, input, position), HasFatal() ? lexicalConsumedByteCount : input.Length);
    }

    internal EmailMetadataParseResult ParseSensitivity()
    {
        List<int> significant = GetSignificant(0, tokens.Count);
        EmailSourcePosition position = significant.Count == 0 ? initialPosition : tokens[significant[0]].GetSourcePosition();
        string original = GetSingleToken(significant, EmailIssueCode.MetadataValueMalformed, "Sensitivity must contain one token.");
        EmailSensitivityKind kind = EmailSensitivityKind.Unknown;
        if (string.Equals(original, "normal", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailSensitivityKind.Normal;
        }
        else if (string.Equals(original, "personal", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailSensitivityKind.Personal;
        }
        else if (string.Equals(original, "private", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailSensitivityKind.Private;
        }
        else if (string.Equals(original, "company-confidential", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailSensitivityKind.CompanyConfidential;
        }
        else if (original.Length > 0 && !HasFatal())
        {
            AddIssue(EmailIssueCode.MetadataValueMalformed, "Sensitivity is not a recognized sensitivity token.", position, "The original sensitivity token was retained as unknown.");
        }

        return Result(EmailMetadataValueKind.Sensitivity, new EmailSensitivity(kind, original, input, position), HasFatal() ? lexicalConsumedByteCount : input.Length);
    }

    internal EmailMetadataParseResult ParseAutoSubmitted()
    {
        if (HasFatal())
        {
            return Result(EmailMetadataValueKind.AutoSubmitted, null, lexicalConsumedByteCount);
        }

        List<int> significant = GetSignificant(0, tokens.Count);
        if (significant.Count == 0)
        {
            AddIssue(EmailIssueCode.AutoSubmittedMalformed, "Auto-Submitted requires a disposition token.", initialPosition, "No structured value was produced.");
            return Result(EmailMetadataValueKind.AutoSubmitted, null, input.Length);
        }

        int primaryIndex = significant[0];
        EmailHeaderToken primary = tokens[primaryIndex];
        string original = GetTokenText(primary, false);
        if (primary.GetKind() != EmailHeaderTokenKind.Atom)
        {
            AddIssue(EmailIssueCode.AutoSubmittedMalformed, "The Auto-Submitted disposition is not a MIME token.", primary.GetSourcePosition(), "The original token bytes were retained as an extension value.");
        }

        EmailAutoSubmittedKind kind = EmailAutoSubmittedKind.Extension;
        if (string.Equals(original, "no", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailAutoSubmittedKind.No;
        }
        else if (string.Equals(original, "auto-generated", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailAutoSubmittedKind.AutoGenerated;
        }
        else if (string.Equals(original, "auto-replied", StringComparison.OrdinalIgnoreCase))
        {
            kind = EmailAutoSubmittedKind.AutoReplied;
        }

        int suffixStart = GetTokenOffset(primaryIndex) + primary.GetByteCount();
        if (significant.Count > 1 && !IsSpecial(significant[1], 59))
        {
            AddIssue(EmailIssueCode.AutoSubmittedMalformed, "Auto-Submitted parameters must be introduced by semicolons.", tokens[significant[1]].GetSourcePosition(), "The recoverable parameter data and original bytes were retained.");
        }

        EmailStructuredValueParserOptions parameterOptions = new EmailStructuredValueParserOptions();
        parameterOptions.SetMode(options.GetMode());
        parameterOptions.SetMaximumParameters(options.GetMaximumItems());
        parameterOptions.SetMaximumInputBytes(options.GetMaximumInputBytes());
        EmailParameterParseResult parameterResult = new EmailParameterParser().ParseWithInputAndOptionsAndInitialPosition(input.AsSpan(suffixStart), parameterOptions, GetPositionAt(suffixStart));
        issues.AddRange(parameterResult.GetIssues());
        if (parameterResult.GetIssues().GetCount() > 0 && !parameterResult.HasFatalError())
        {
            AddIssue(EmailIssueCode.AutoSubmittedMalformed, "The Auto-Submitted parameter sequence is malformed.", GetPositionAt(suffixStart), "All successfully parsed parameters and the original bytes were retained.");
        }

        int consumed = parameterResult.HasFatalError() ? suffixStart + parameterResult.GetConsumedByteCount() : input.Length;
        EmailAutoSubmitted value = new EmailAutoSubmitted(kind, original, parameterResult.GetValue(), input, primary.GetSourcePosition());
        return Result(EmailMetadataValueKind.AutoSubmitted, value, consumed);
    }

    internal EmailMetadataParseResult ParseListManagement(EmailListManagementKind kind)
    {
        if (HasFatal())
        {
            return Result(EmailMetadataValueKind.ListManagement, null, lexicalConsumedByteCount);
        }

        List<EmailListCommand> commands = new List<EmailListCommand>();
        bool postingProhibited = false;
        int postingProhibitionCount = 0;
        int segmentStart = 0;
        int tokenStart = 0;
        bool inAngle = false;
        int index = 0;
        while (index < tokens.Count)
        {
            if (IsSpecial(index, 60))
            {
                inAngle = true;
            }
            else if (IsSpecial(index, 62))
            {
                inAngle = false;
            }
            else if (IsSpecial(index, 44) && !inAngle)
            {
                bool noPost = ParseListSegment(commands, kind, segmentStart, GetTokenOffset(index), tokenStart, index);
                postingProhibited = postingProhibited || noPost;
                if (noPost)
                {
                    postingProhibitionCount++;
                }

                segmentStart = GetTokenOffset(index) + tokens[index].GetByteCount();
                tokenStart = index + 1;
                if (HasFatal())
                {
                    break;
                }
            }

            index++;
        }

        if (!HasFatal())
        {
            bool noPost = ParseListSegment(commands, kind, segmentStart, input.Length, tokenStart, tokens.Count);
            postingProhibited = postingProhibited || noPost;
            if (noPost)
            {
                postingProhibitionCount++;
            }
        }

        if (!HasFatal() && postingProhibited && commands.Count > 0)
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "List-Post cannot combine NO with posting commands.", initialPosition, "The commands and posting prohibition marker were retained.");
        }

        if (!HasFatal() && postingProhibitionCount > 1)
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "List-Post contains more than one NO marker.", initialPosition, "The posting prohibition was retained once.");
        }

        if (!HasFatal() && !postingProhibited && commands.Count == 0)
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "A list-management field requires at least one angle-delimited URI.", initialPosition, "The original field value was retained without commands.");
        }

        EmailListManagementValue value = new EmailListManagementValue(kind, commands, postingProhibited, input, GetFirstPosition());
        return Result(EmailMetadataValueKind.ListManagement, value, HasFatal() ? GetStoppedByteCount(lexicalConsumedByteCount) : input.Length);
    }

    internal EmailMetadataParseResult ParseListUnsubscribePost()
    {
        string text = Unfold(input).Trim();
        bool oneClick = string.Equals(text, "List-Unsubscribe=One-Click", StringComparison.OrdinalIgnoreCase);
        if (!oneClick && !HasFatal())
        {
            AddIssue(EmailIssueCode.ListUnsubscribePostMalformed, "List-Unsubscribe-Post does not contain the RFC 8058 one-click argument.", GetFirstPosition(), "The original field value was retained and one-click behavior was not enabled.");
        }

        EmailListUnsubscribePost value = new EmailListUnsubscribePost(oneClick, input, GetFirstPosition());
        return Result(EmailMetadataValueKind.ListUnsubscribePost, value, HasFatal() ? lexicalConsumedByteCount : input.Length);
    }

    internal EmailMetadataParseResult ParseResentField(string? fieldName)
    {
        EmailResentFieldKind kind = GetResentKind(fieldName);
        EmailDateTime? date = null;
        EmailAddressList? addresses = null;
        EmailMessageId? messageId = null;
        bool obsolete = kind == EmailResentFieldKind.ReplyTo;
        int consumed = input.Length;
        if (obsolete)
        {
            AddModeIssue(EmailIssueCode.ResentFieldObsolete, "Resent-Reply-To is obsolete RFC syntax.", initialPosition, "The address list was parsed and marked obsolete.");
        }

        if (kind == EmailResentFieldKind.Date)
        {
            EmailDateParserOptions nested = new EmailDateParserOptions();
            nested.SetMode(GetDateMode());
            nested.SetMaximumInputBytes(options.GetMaximumInputBytes());
            EmailDateParseResult parsed = new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(input, nested, initialPosition);
            issues.AddRange(parsed.GetIssues());
            consumed = parsed.GetConsumedByteCount();
            date = parsed.GetValue();
        }
        else if (kind == EmailResentFieldKind.MessageId)
        {
            EmailMessageIdParserOptions nested = new EmailMessageIdParserOptions();
            nested.SetMode(GetMessageIdMode());
            nested.SetMaximumIdentifiers(1);
            nested.SetMaximumInputBytes(options.GetMaximumInputBytes());
            EmailMessageIdParseResult parsed = new EmailMessageIdParser().ParseMessageIdWithInputAndOptionsAndInitialPosition(input, nested, initialPosition);
            issues.AddRange(parsed.GetIssues());
            consumed = parsed.GetConsumedByteCount();
            messageId = parsed.GetIdentifier();
        }
        else if (IsAddressResentKind(kind))
        {
            if (kind == EmailResentFieldKind.Bcc && GetSignificant(0, tokens.Count).Count == 0)
            {
                addresses = new EmailAddressList(new List<EmailAddressListEntry>(), input);
            }
            else
            {
                EmailAddressParserOptions nested = new EmailAddressParserOptions();
                nested.SetMode(GetAddressMode());
                nested.SetMaximumAddresses(options.GetMaximumItems());
                nested.SetMaximumInputBytes(options.GetMaximumInputBytes());
                nested.SetAllowInternationalized(true);
                EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndOptionsAndInitialPosition(input, nested, initialPosition);
                issues.AddRange(parsed.GetIssues());
                consumed = parsed.GetConsumedByteCount();
                addresses = parsed.GetAddressList();
                ValidateResentAddresses(kind, addresses);
            }
        }
        else if (kind == EmailResentFieldKind.Extension && !(fieldName ?? throw new InvalidOperationException()).StartsWith("Resent-", StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(EmailIssueCode.ResentFieldMalformed, "The supplied field name is not a Resent field.", initialPosition, "The field name and raw value were retained as an extension.");
        }

        EmailResentField value = new EmailResentField(kind, fieldName, date, addresses, messageId, input, initialPosition, obsolete);
        return Result(EmailMetadataValueKind.ResentField, value, consumed);
    }

    void ParseKeywordSegment(List<EmailKeyword> keywords, int rawStart, int rawEnd, int tokenStart, int tokenEnd)
    {
        List<int> significant = GetSignificant(tokenStart, tokenEnd);
        if (significant.Count == 0)
        {
            AddIssue(EmailIssueCode.KeywordMalformed, "The Keywords value contains an empty list member.", GetPositionAt(rawStart), "The empty member was ignored.");
            return;
        }

        if (keywords.Count >= options.GetMaximumItems())
        {
            AddFatal("The Keywords value exceeds the configured item-count limit.", tokens[significant[0]].GetSourcePosition());
            return;
        }

        StringBuilder text = new StringBuilder();
        bool malformed = false;
        int index = 0;
        while (index < significant.Count)
        {
            EmailHeaderToken token = tokens[significant[index]];
            bool word = token.GetKind() == EmailHeaderTokenKind.Atom || token.GetKind() == EmailHeaderTokenKind.QuotedString;
            if (!word)
            {
                malformed = true;
            }

            if (text.Length > 0)
            {
                text.Append(' ');
            }

            text.Append(GetTokenText(token, word));
            index++;
        }

        if (malformed)
        {
            AddIssue(EmailIssueCode.KeywordMalformed, "A Keywords member is not an RFC phrase.", tokens[significant[0]].GetSourcePosition(), "The member bytes were retained and exposed as a recovered phrase.");
        }

        keywords.Add(new EmailKeyword(text.ToString(), CopyRange(rawStart, rawEnd), tokens[significant[0]].GetSourcePosition(), malformed));
    }

    bool ParseListSegment(List<EmailListCommand> commands, EmailListManagementKind kind, int rawStart, int rawEnd, int tokenStart, int tokenEnd)
    {
        List<int> significant = GetSignificant(tokenStart, tokenEnd);
        if (significant.Count == 0)
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "The list-management value contains an empty member.", GetPositionAt(rawStart), "The empty member was ignored.");
            return false;
        }

        if (kind == EmailListManagementKind.Post && significant.Count == 1 && tokens[significant[0]].GetKind() == EmailHeaderTokenKind.Atom && string.Equals(GetTokenText(tokens[significant[0]], false), "NO", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int first = significant[0];
        int last = significant[significant.Count - 1];
        if (!IsSpecial(first, 60) || !IsSpecial(last, 62))
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "A list-management command is not enclosed in angle brackets.", tokens[first].GetSourcePosition(), "The malformed member was retained only in the raw field value.");
            return false;
        }

        int uriStart = GetTokenOffset(first) + tokens[first].GetByteCount();
        int uriEnd = GetTokenOffset(last);
        if (uriEnd <= uriStart)
        {
            AddIssue(EmailIssueCode.ListManagementMalformed, "A list-management command has an empty URI.", tokens[first].GetSourcePosition(), "The empty command was ignored.");
            return false;
        }

        if (commands.Count >= options.GetMaximumItems())
        {
            AddFatal("The list-management value exceeds the configured command-count limit.", tokens[first].GetSourcePosition());
            return false;
        }

        byte[] uriBytes = CopyRange(uriStart, uriEnd);
        commands.Add(new EmailListCommand(Encoding.UTF8.GetString(uriBytes), CopyRange(rawStart, rawEnd), uriBytes, tokens[first].GetSourcePosition()));
        return false;
    }

    string GetSingleToken(List<int> significant, EmailIssueCode code, string description)
    {
        if (HasFatal())
        {
            return "";
        }

        if (significant.Count != 1 || significant.Count == 1 && tokens[significant[0]].GetKind() != EmailHeaderTokenKind.Atom)
        {
            AddIssue(code, description, GetFirstPosition(), "The original bytes were retained as an unknown value.");
            return significant.Count == 0 ? "" : Unfold(input).Trim();
        }

        return GetTokenText(tokens[significant[0]], false);
    }

    void ValidateResentAddresses(EmailResentFieldKind kind, EmailAddressList addresses)
    {
        if (kind == EmailResentFieldKind.Sender && (addresses.GetCount() != 1 || !addresses.GetAt(0).IsMailbox()))
        {
            AddIssue(EmailIssueCode.ResentFieldMalformed, "Resent-Sender must contain exactly one mailbox.", initialPosition, "All successfully parsed addresses were retained.");
            return;
        }

        if (kind == EmailResentFieldKind.From)
        {
            int index = 0;
            while (index < addresses.GetCount())
            {
                if (!addresses.GetAt(index).IsMailbox())
                {
                    AddIssue(EmailIssueCode.ResentFieldMalformed, "Resent-From must contain mailboxes rather than groups.", initialPosition, "All successfully parsed address entries were retained.");
                    return;
                }

                index++;
            }
        }
    }

    EmailResentFieldKind GetResentKind(string? fieldName)
    {
        if (string.Equals(fieldName, "Resent-Date", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.Date;
        }

        if (string.Equals(fieldName, "Resent-From", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.From;
        }

        if (string.Equals(fieldName, "Resent-Sender", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.Sender;
        }

        if (string.Equals(fieldName, "Resent-To", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.To;
        }

        if (string.Equals(fieldName, "Resent-Cc", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.Cc;
        }

        if (string.Equals(fieldName, "Resent-Bcc", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.Bcc;
        }

        if (string.Equals(fieldName, "Resent-Message-ID", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.MessageId;
        }

        if (string.Equals(fieldName, "Resent-Reply-To", StringComparison.OrdinalIgnoreCase))
        {
            return EmailResentFieldKind.ReplyTo;
        }

        return EmailResentFieldKind.Extension;
    }

    bool IsAddressResentKind(EmailResentFieldKind kind)
    {
        return kind == EmailResentFieldKind.From || kind == EmailResentFieldKind.Sender || kind == EmailResentFieldKind.To || kind == EmailResentFieldKind.Cc || kind == EmailResentFieldKind.Bcc || kind == EmailResentFieldKind.ReplyTo;
    }

    EmailDateParseMode GetDateMode()
    {
        return options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailDateParseMode.Strict : EmailDateParseMode.Tolerant;
    }

    EmailMessageIdParseMode GetMessageIdMode()
    {
        return options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailMessageIdParseMode.Strict : EmailMessageIdParseMode.Tolerant;
    }

    EmailAddressParseMode GetAddressMode()
    {
        return options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailAddressParseMode.Strict : EmailAddressParseMode.Tolerant;
    }

    List<int> GetSignificant(int start, int end)
    {
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

        return significant;
    }

    bool IsSpecial(int tokenIndex, byte value)
    {
        EmailHeaderToken token = tokens[tokenIndex];
        byte[] bytes = token.GetValueBytes();
        return token.GetKind() == EmailHeaderTokenKind.Special && bytes.Length == 1 && bytes[0] == value;
    }

    string GetTokenText(EmailHeaderToken token, bool decoded)
    {
        return Encoding.UTF8.GetString(decoded ? token.GetValueBytes() : token.GetRawBytes());
    }

    EmailSourcePosition GetFirstPosition()
    {
        List<int> significant = GetSignificant(0, tokens.Count);
        return significant.Count == 0 ? initialPosition : tokens[significant[0]].GetSourcePosition();
    }

    int GetTokenOffset(int tokenIndex)
    {
        long relative = tokens[tokenIndex].GetSourcePosition().GetByteOffset() - initialPosition.GetByteOffset();
        return relative >= 0 && relative <= input.Length ? (int)relative : 0;
    }

    EmailSourcePosition GetPositionAt(int offset)
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tracker.Advance(input.AsSpan(0, offset));
        return tracker.GetPosition();
    }

    byte[] CopyRange(int start, int end)
    {
        byte[] copy = new byte[end - start];
        input.AsSpan(start, end - start).CopyTo(copy);
        return copy;
    }

    string Unfold(byte[] value)
    {
        string text = Encoding.UTF8.GetString(value);
        StringBuilder output = new StringBuilder();
        bool whitespace = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                whitespace = true;
            }
            else
            {
                if (whitespace && output.Length > 0)
                {
                    output.Append(' ');
                }

                whitespace = false;
                output.Append(current);
            }

            index++;
        }

        return output.ToString();
    }

    bool HasFatal()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }

    void AddIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    void AddModeIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        AddIssue(code, description, position, recovery);
    }

    void AddFatal(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.MetadataValueLimitExceeded, description, position, null, null, "Parsing stopped before the excessive item."));
        long relative = position.GetByteOffset() - initialPosition.GetByteOffset();
        stopByteCount = relative >= 0 && relative <= input.Length ? (int)relative : 0;
    }

    int GetStoppedByteCount(int fallback)
    {
        return stopByteCount >= 0 ? stopByteCount : fallback;
    }

    EmailMetadataParseResult Result(EmailMetadataValueKind kind, object? value, int consumed)
    {
        return new EmailMetadataParseResult(kind, value, issues, consumed, input.Length);
    }
}
