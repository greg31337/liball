using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailAddressPhraseValue
{
    internal string? value;
    internal bool success;
    internal bool obsolete;
    internal bool internationalized;

    internal EmailAddressPhraseValue()
    {
        value = null;
        success = false;
        obsolete = false;
        internationalized = false;
    }
}
internal class EmailAddressPartValue
{
    internal string? value;
    internal byte[] rawBytes;
    internal bool success;
    internal bool domainLiteral;
    internal bool obsolete;
    internal bool internationalized;

    internal EmailAddressPartValue()
    {
        value = null;
        rawBytes = new byte[0];
        success = false;
        domainLiteral = false;
        obsolete = false;
        internationalized = false;
    }
}
internal class EmailAddressMailboxValue
{
    internal EmailAddress? address;
    internal bool success;

    internal EmailAddressMailboxValue()
    {
        address = null;
        success = false;
    }
}
internal class EmailAddressParserWorker
{
    byte[] input;
    List<EmailHeaderToken> tokens;
    EmailAddressParserOptions options;
    EmailSourcePosition initialPosition;
    EmailIssueList issues;
    int cursor;
    int mailboxCount;
    bool stopped;
    int lexicalConsumedByteCount;

    internal EmailAddressParserWorker(byte[] input, EmailHeaderLexicalResult lexical, EmailAddressParserOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        tokens = lexical.GetTokens();
        this.options = options;
        this.initialPosition = initialPosition;
        issues = lexical.GetIssues();
        cursor = 0;
        mailboxCount = 0;
        stopped = lexical.HasFatalError();
        lexicalConsumedByteCount = lexical.GetConsumedByteCount();
    }

    internal EmailAddressParseResult ParseList()
    {
        List<EmailAddressListEntry> entries = new List<EmailAddressListEntry>();
        bool needsAddress = false;
        SkipCfws();
        while (cursor < tokens.Count && !stopped)
        {
            if (IsSpecialAt(cursor, 44))
            {
                AddObsolete("The address list contains an empty member.", GetPosition(), "The empty member was ignored.");
                cursor++;
                needsAddress = true;
                SkipCfws();
                continue;
            }

            if (IsGroupStart())
            {
                EmailAddressGroup? group = ParseGroup();
                if (group != null)
                {
                    entries.Add(new EmailAddressListEntryOperations().CreateGroup(group));
                }
            }
            else
            {
                EmailAddressMailboxValue mailbox = ParseMailbox(true);
                if (mailbox.success)
                {
                    entries.Add(new EmailAddressListEntryOperations().CreateMailbox(mailbox.address));
                }
            }

            if (stopped)
            {
                break;
            }

            needsAddress = false;
            SkipCfws();
            if (cursor < tokens.Count && IsSpecialAt(cursor, 44))
            {
                cursor++;
                needsAddress = true;
                SkipCfws();
                continue;
            }

            if (cursor < tokens.Count)
            {
                AddMalformed("Unexpected tokens follow an address-list entry.", GetPosition(), "Tokens through the next comma were skipped.");
                RecoverToListSeparator();
                if (cursor < tokens.Count && IsSpecialAt(cursor, 44))
                {
                    cursor++;
                    needsAddress = true;
                    SkipCfws();
                }
            }
        }

        if (needsAddress && !stopped)
        {
            AddObsolete("The address list ends with an empty member.", GetPosition(), "The empty member was ignored.");
        }

        if (entries.Count == 0 && !stopped)
        {
            AddMalformed("The value does not contain an address or group.", GetPosition(), "No address-list entries were produced.");
        }

        int consumed = input.Length;
        if (stopped)
        {
            consumed = cursor == 0 && lexicalConsumedByteCount < input.Length ? lexicalConsumedByteCount : GetCursorByteOffset();
        }
        byte[] raw = CopyRange(0, consumed);
        EmailAddressList list = new EmailAddressList(entries, raw);
        return new EmailAddressParseResult(list, issues, consumed, input.Length);
    }

    EmailAddressGroup? ParseGroup()
    {
        int startToken = cursor;
        int startByte = GetTokenStart(startToken);
        EmailSourcePosition position = tokens[startToken].GetSourcePosition();
        int colon = FindSpecial(cursor, 58, false);
        if (colon < 0)
        {
            AddMalformed("An address group is missing its colon.", position, "The malformed entry was skipped.");
            RecoverToListSeparator();
            return null;
        }

        EmailAddressPhraseValue phrase = BuildPhrase(cursor, colon);
        if (!phrase.success)
        {
            AddMalformed("An address group has an empty or malformed display name.", position, "The group name was not accepted.");
        }

        cursor = colon + 1;
        List<EmailAddress> addresses = new List<EmailAddress>();
        bool obsolete = phrase.obsolete;
        bool closed = false;
        bool needsMailbox = false;
        SkipCfws();
        while (cursor < tokens.Count && !stopped)
        {
            if (IsSpecialAt(cursor, 59))
            {
                cursor++;
                closed = true;
                break;
            }

            if (IsSpecialAt(cursor, 44))
            {
                AddObsolete("An address group contains an empty mailbox member.", GetPosition(), "The empty member was ignored.");
                obsolete = true;
                cursor++;
                needsMailbox = true;
                SkipCfws();
                continue;
            }

            EmailAddressMailboxValue mailbox = ParseMailbox(true);
            if (mailbox.success)
            {
                addresses.Add((mailbox.address ?? throw new InvalidOperationException()));
                if (mailbox.address.UsesObsoleteSyntax())
                {
                    obsolete = true;
                }
            }

            if (stopped)
            {
                break;
            }

            needsMailbox = false;
            SkipCfws();
            if (cursor < tokens.Count && IsSpecialAt(cursor, 44))
            {
                cursor++;
                needsMailbox = true;
                SkipCfws();
                continue;
            }

            if (cursor < tokens.Count && IsSpecialAt(cursor, 59))
            {
                cursor++;
                closed = true;
                break;
            }

            if (cursor < tokens.Count)
            {
                AddMalformed("A group mailbox is not followed by a comma or semicolon.", GetPosition(), "Tokens through the group terminator were skipped.");
                RecoverToGroupSeparator();
            }
        }

        if (needsMailbox)
        {
            AddObsolete("An address group ends with an empty mailbox member.", GetPosition(), "The empty member was ignored.");
            obsolete = true;
        }

        if (!closed && !stopped)
        {
            AddMalformed("An address group is missing its terminating semicolon.", GetPosition(), "The available group members were retained.");
        }

        if (!phrase.success)
        {
            return null;
        }

        int endByte = GetCursorByteOffset();
        bool internationalized = phrase.internationalized;
        int addressIndex = 0;
        while (addressIndex < addresses.Count)
        {
            if (addresses[addressIndex].IsInternationalized())
            {
                internationalized = true;
            }

            addressIndex++;
        }

        return new EmailAddressGroup(phrase.value, addresses, CopyRange(startByte, endByte), position, internationalized, obsolete);
    }

    EmailAddressMailboxValue ParseMailbox(bool stopAtSemicolon)
    {
        EmailAddressMailboxValue result = new EmailAddressMailboxValue();
        SkipCfws();
        if (cursor >= tokens.Count)
        {
            AddMalformed("A mailbox was expected but the value ended.", GetPosition(), "No mailbox was produced.");
            return result;
        }

        int startToken = cursor;
        int startByte = GetTokenStart(startToken);
        EmailSourcePosition position = tokens[startToken].GetSourcePosition();
        int boundary = FindMailboxBoundary(cursor, stopAtSemicolon);
        int angle = FindSpecialBefore(cursor, 60, boundary);
        string? displayName = null;
        bool obsolete = false;
        bool internationalized = false;
        List<string> route = new List<string>();
        EmailAddressPartValue local;
        EmailAddressPartValue domain;

        if (angle >= 0)
        {
            EmailAddressPhraseValue phrase = BuildPhrase(cursor, angle);
            if (phrase.success)
            {
                displayName = phrase.value;
                obsolete = phrase.obsolete;
                internationalized = phrase.internationalized;
            }
            else if (angle > cursor)
            {
                AddMalformed("A name-address contains a malformed display name.", position, "The angle address was parsed without a display name.");
            }

            cursor = angle + 1;
            SkipCfws();
            if (cursor < tokens.Count && IsSpecialAt(cursor, 64))
            {
                ParseSourceRoute(route);
                obsolete = true;
            }

            int angleBoundary = FindMailboxBoundary(cursor, stopAtSemicolon);
            int greater = FindSpecialBefore(cursor, 62, angleBoundary);
            int addressEnd = greater >= 0 ? greater : angleBoundary;
            int at = FindSpecialBefore(cursor, 64, addressEnd);
            if (at < 0)
            {
                AddMalformed("An angle address is missing the addr-spec at-sign.", GetPosition(), "The malformed mailbox was skipped.");
                cursor = addressEnd;
                if (greater >= 0)
                {
                    cursor++;
                }

                return result;
            }

            local = ParseLocalPart(cursor, at);
            domain = ParseDomain(at + 1, addressEnd);
            cursor = addressEnd;
            if (greater >= 0 && cursor < tokens.Count && IsSpecialAt(cursor, 62))
            {
                cursor++;
            }
            else
            {
                AddMalformed("An angle address is missing its closing greater-than sign.", GetPosition(), "The available addr-spec was retained.");
            }
        }
        else
        {
            int at = FindSpecialBefore(cursor, 64, boundary);
            if (at < 0)
            {
                AddMalformed("A mailbox addr-spec is missing its at-sign.", position, "The malformed list entry was skipped.");
                cursor = boundary;
                return result;
            }

            local = ParseLocalPart(cursor, at);
            domain = ParseDomain(at + 1, boundary);
            cursor = boundary;
        }

        if (!local.success || !domain.success)
        {
            return result;
        }

        if (!IncrementMailboxCount(position))
        {
            cursor = startToken;
            return result;
        }

        obsolete = obsolete || local.obsolete || domain.obsolete || route.Count > 0;
        internationalized = internationalized || local.internationalized || domain.internationalized;
        int endByte = GetCursorByteOffset();
        result.address = new EmailAddress(displayName, local.value, domain.value, CopyRange(startByte, endByte), local.rawBytes, domain.rawBytes, position, domain.domainLiteral, internationalized, obsolete, route);
        result.success = true;
        return result;
    }

    void ParseSourceRoute(List<string> route)
    {
        EmailSourcePosition position = GetPosition();
        AddObsolete("The angle address contains an obsolete source route.", position, "The route domains were retained separately from the destination addr-spec.");
        bool complete = false;
        while (cursor < tokens.Count && !stopped)
        {
            SkipCfws();
            if (cursor < tokens.Count && IsSpecialAt(cursor, 64))
            {
                cursor++;
            }
            else
            {
                AddMalformed("A source-route domain is missing its at-sign.", GetPosition(), "Parsing continued at the route domain.");
            }

            int end = FindRouteSeparator(cursor);
            EmailAddressPartValue domain = ParseDomain(cursor, end);
            if (domain.success)
            {
                route.Add((domain.value ?? throw new InvalidOperationException()));
            }

            cursor = end;
            SkipCfws();
            if (cursor < tokens.Count && IsSpecialAt(cursor, 58))
            {
                cursor++;
                complete = true;
                break;
            }

            if (cursor < tokens.Count && IsSpecialAt(cursor, 44))
            {
                cursor++;
                continue;
            }

            break;
        }

        if (!complete)
        {
            AddMalformed("An obsolete source route is missing its terminating colon.", GetPosition(), "The available route domains were retained.");
        }

        SkipCfws();
    }

    EmailAddressPartValue ParseLocalPart(int start, int end)
    {
        return ParsePart(start, end, false);
    }

    EmailAddressPartValue ParseDomain(int start, int end)
    {
        return ParsePart(start, end, true);
    }

    EmailAddressPartValue ParsePart(int start, int end, bool domainPart)
    {
        EmailAddressPartValue result = new EmailAddressPartValue();
        int first = TrimStart(start, end);
        int last = TrimEnd(first, end);
        if (first >= last)
        {
            AddMalformed(domainPart ? "An address has an empty domain." : "An address has an empty local part.", GetPositionAt(first), "The mailbox was not produced.");
            return result;
        }

        result.rawBytes = CopyTokenRange(first, last);
        List<byte> semantic = new List<byte>();
        int meaningfulCount = 0;
        bool sawInternalCfws = false;
        bool pendingCfws = false;
        bool onlyQuoted = true;
        int index = first;
        while (index < last)
        {
            EmailHeaderToken token = tokens[index];
            if (token.GetKind() == EmailHeaderTokenKind.Cfws)
            {
                if (meaningfulCount > 0)
                {
                    pendingCfws = true;
                }

                index++;
                continue;
            }

            if (pendingCfws)
            {
                sawInternalCfws = true;
                pendingCfws = false;
            }

            if (token.GetKind() == EmailHeaderTokenKind.Atom || token.GetKind() == EmailHeaderTokenKind.DotAtom || !domainPart && token.GetKind() == EmailHeaderTokenKind.QuotedString)
            {
                Append(semantic, token.GetValueBytes());
                meaningfulCount++;
                if (token.GetKind() != EmailHeaderTokenKind.QuotedString)
                {
                    onlyQuoted = false;
                }
            }
            else if (domainPart && token.GetKind() == EmailHeaderTokenKind.DomainLiteral && meaningfulCount == 0 && index + 1 == last)
            {
                Append(semantic, token.GetValueBytes());
                meaningfulCount++;
                result.domainLiteral = true;
                onlyQuoted = false;
            }
            else if (IsSpecialAt(index, 46))
            {
                semantic.Add(46);
                meaningfulCount++;
                onlyQuoted = false;
            }
            else if (IsRecoverableDotFragment(token))
            {
                Append(semantic, token.GetRawBytes());
                meaningfulCount++;
                onlyQuoted = false;
                result.obsolete = true;
            }
            else
            {
                AddMalformed(domainPart ? "The domain contains an invalid lexical token." : "The local part contains an invalid lexical token.", token.GetSourcePosition(), "The mailbox was not produced.");
                return result;
            }

            if (token.IsObsolete())
            {
                result.obsolete = true;
            }

            if (token.IsInternationalized())
            {
                result.internationalized = true;
            }

            index++;
        }

        if (meaningfulCount == 0)
        {
            AddMalformed(domainPart ? "An address has an empty domain." : "An address has an empty local part.", GetPositionAt(first), "The mailbox was not produced.");
            return result;
        }

        bool canonical = meaningfulCount == 1 && !sawInternalCfws;
        if (domainPart && result.domainLiteral)
        {
            canonical = true;
        }

        if (!domainPart && onlyQuoted && meaningfulCount == 1)
        {
            canonical = true;
        }

        if (!canonical)
        {
            result.obsolete = true;
            AddObsolete(domainPart ? "The domain uses obsolete tokens or comments around dots." : "The local part uses obsolete words or comments around dots.", GetPositionAt(first), "The tokens were combined into their semantic addr-spec value.");
        }

        byte[] valueBytes = semantic.ToArray();
        if (!result.domainLiteral && HasInvalidDotPlacement(valueBytes))
        {
            AddMalformed(domainPart ? "The domain has an invalid dot placement." : "The local part has an invalid dot placement.", GetPositionAt(first), "The mailbox was not produced.");
            return result;
        }

        if (EmailHeaderUtf8.GetInvalidIndex(valueBytes) >= 0)
        {
            AddMalformed(domainPart ? "The domain contains malformed UTF-8." : "The local part contains malformed UTF-8.", GetPositionAt(first), "The mailbox was not produced.");
            return result;
        }

        result.value = EmailHeaderUtf8.GetString(valueBytes);
        result.internationalized = result.internationalized || EmailHeaderUtf8.ContainsNonAscii(valueBytes);
        result.success = result.value.Length > 0;
        return result;
    }

    EmailAddressPhraseValue BuildPhrase(int start, int end)
    {
        EmailAddressPhraseValue result = new EmailAddressPhraseValue();
        List<byte> value = new List<byte>();
        bool pendingSpace = false;
        bool hasWord = false;
        int index = start;
        while (index < end)
        {
            EmailHeaderToken token = tokens[index];
            if (token.GetKind() == EmailHeaderTokenKind.Cfws)
            {
                if (hasWord)
                {
                    pendingSpace = true;
                }

                index++;
                continue;
            }

            bool accepted = token.GetKind() == EmailHeaderTokenKind.Atom || token.GetKind() == EmailHeaderTokenKind.DotAtom || token.GetKind() == EmailHeaderTokenKind.QuotedString;
            if (!accepted && IsSpecialAt(index, 46))
            {
                accepted = true;
                result.obsolete = true;
                AddObsolete("A display name uses an obsolete period token.", token.GetSourcePosition(), "The period was retained in the display name.");
            }

            if (!accepted && IsRecoverableDotFragment(token))
            {
                accepted = true;
                result.obsolete = true;
                AddObsolete("A display name uses an obsolete word containing a period token.", token.GetSourcePosition(), "The word and period were retained in the display name.");
            }

            if (!accepted)
            {
                return result;
            }

            if (pendingSpace && value.Count > 0)
            {
                value.Add(32);
            }

            pendingSpace = false;
            Append(value, token.GetValueBytes());
            hasWord = true;
            if (token.IsObsolete())
            {
                result.obsolete = true;
            }

            if (token.IsInternationalized())
            {
                result.internationalized = true;
            }

            index++;
        }

        byte[] bytes = value.ToArray();
        if (!hasWord || EmailHeaderUtf8.GetInvalidIndex(bytes) >= 0)
        {
            return result;
        }

        result.value = DecodeDisplayName(bytes, GetPositionAt(start));
        result.success = result.value.Length > 0;
        return result;
    }

    string DecodeDisplayName(byte[] bytes, EmailSourcePosition position)
    {
        if (!ContainsEncodedWordIntroducer(bytes))
        {
            return EmailHeaderUtf8.GetString(bytes);
        }

        EmailEncodedWordParseMode mode = options.GetMode() == EmailAddressParseMode.Strict ? EmailEncodedWordParseMode.Strict : EmailEncodedWordParseMode.Tolerant;
        EmailEncodedWordParseResult parsed = new EmailEncodedWord().ParseWithInputAndModeAndContext(bytes, mode, EmailEncodedWordContext.Phrase);
        EmailIssueList encodedIssues = parsed.GetIssues();
        if (encodedIssues.GetCount() > 0)
        {
            AddMalformed("A display name contains a malformed encoded word.", position, "Every recoverable display-name byte was retained or decoded.");
        }

        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions decodeOptions = new EmailCharsetDecodeOptions();
        if (options.GetMode() == EmailAddressParseMode.Strict)
        {
            decodeOptions.SetFallbackPolicy(EmailCharsetFallbackPolicy.Strict);
        }

        StringBuilder text = new StringBuilder();
        int index = 0;
        while (index < parsed.GetPartCount())
        {
            EmailEncodedWordPart part = parsed.GetPart(index);
            if (part.IsEncodedWord())
            {
                EmailCharsetDecodeResult decoded = registry.Decode(part.GetBytes(), part.GetCharset(), decodeOptions);
                if (!decoded.IsSuccess())
                {
                    AddMalformed("A display-name encoded word uses an unsupported or malformed charset value.", position, "Any successfully decoded display-name text was retained.");
                }

                text.Append(decoded.GetText());
            }
            else
            {
                byte[] literal = part.GetBytes();
                if (EmailHeaderUtf8.GetInvalidIndex(literal) >= 0)
                {
                    AddMalformed("A display name contains malformed UTF-8 literal text.", position, "The malformed literal was omitted from the display name.");
                }
                else
                {
                    text.Append(EmailHeaderUtf8.GetString(literal));
                }
            }

            index++;
        }

        return text.ToString();
    }

    bool IsGroupStart()
    {
        int index = cursor;
        while (index < tokens.Count)
        {
            if (IsSpecialAt(index, 58))
            {
                return true;
            }

            if (IsSpecialAt(index, 60) || IsSpecialAt(index, 64) || IsSpecialAt(index, 44) || IsSpecialAt(index, 59))
            {
                return false;
            }

            index++;
        }

        return false;
    }

    int FindMailboxBoundary(int start, bool stopAtSemicolon)
    {
        int index = start;
        while (index < tokens.Count)
        {
            if (IsSpecialAt(index, 44) || stopAtSemicolon && IsSpecialAt(index, 59))
            {
                return index;
            }

            index++;
        }

        return tokens.Count;
    }

    int FindRouteSeparator(int start)
    {
        int index = start;
        while (index < tokens.Count)
        {
            if (IsSpecialAt(index, 44) || IsSpecialAt(index, 58) || IsSpecialAt(index, 62))
            {
                return index;
            }

            index++;
        }

        return tokens.Count;
    }

    int FindSpecial(int start, byte value, bool stopAtComma)
    {
        int index = start;
        while (index < tokens.Count)
        {
            if (IsSpecialAt(index, value))
            {
                return index;
            }

            if (stopAtComma && IsSpecialAt(index, 44))
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    int FindSpecialBefore(int start, byte value, int end)
    {
        int index = start;
        while (index < end)
        {
            if (IsSpecialAt(index, value))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    void RecoverToListSeparator()
    {
        while (cursor < tokens.Count && !IsSpecialAt(cursor, 44))
        {
            cursor++;
        }
    }

    void RecoverToGroupSeparator()
    {
        while (cursor < tokens.Count && !IsSpecialAt(cursor, 44) && !IsSpecialAt(cursor, 59))
        {
            cursor++;
        }
    }

    void SkipCfws()
    {
        while (cursor < tokens.Count && tokens[cursor].GetKind() == EmailHeaderTokenKind.Cfws)
        {
            cursor++;
        }
    }

    int TrimStart(int start, int end)
    {
        while (start < end && tokens[start].GetKind() == EmailHeaderTokenKind.Cfws)
        {
            start++;
        }

        return start;
    }

    int TrimEnd(int start, int end)
    {
        while (end > start && tokens[end - 1].GetKind() == EmailHeaderTokenKind.Cfws)
        {
            end--;
        }

        return end;
    }

    bool IncrementMailboxCount(EmailSourcePosition position)
    {
        if (mailboxCount >= options.GetMaximumAddresses())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AddressLimitExceeded, "The address count exceeds the configured limit.", position, null, null, "Parsing stopped before the excessive mailbox."));
            stopped = true;
            return false;
        }

        mailboxCount++;
        return true;
    }

    void AddMalformed(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.AddressMalformed, description, position, recovery);
    }

    void AddObsolete(string description, EmailSourcePosition position, string recovery)
    {
        AddRecoverable(EmailIssueCode.AddressObsoleteSyntax, description, position, recovery);
    }

    void AddRecoverable(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailAddressParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    EmailSourcePosition GetPosition()
    {
        return GetPositionAt(cursor);
    }

    EmailSourcePosition GetPositionAt(int tokenIndex)
    {
        if (tokenIndex >= 0 && tokenIndex < tokens.Count)
        {
            return tokens[tokenIndex].GetSourcePosition();
        }

        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        tracker.Advance(input);
        return tracker.GetPosition();
    }

    int GetCursorByteOffset()
    {
        if (cursor >= 0 && cursor < tokens.Count)
        {
            return GetTokenStart(cursor);
        }

        return input.Length;
    }

    int GetTokenStart(int tokenIndex)
    {
        long relative = tokens[tokenIndex].GetSourcePosition().GetByteOffset() - initialPosition.GetByteOffset();
        return (int)relative;
    }

    byte[] CopyTokenRange(int start, int end)
    {
        int byteStart = GetTokenStart(start);
        int byteEnd = end < tokens.Count ? GetTokenStart(end) : input.Length;
        return CopyRange(byteStart, byteEnd);
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

        byte[] value = new byte[end - start];
        if (value.Length > 0)
        {
            Array.Copy(input, start, value, 0, value.Length);
        }

        return value;
    }

    static void Append(List<byte> destination, byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            destination.Add(value[index]);
            index++;
        }
    }

    static bool HasInvalidDotPlacement(byte[] value)
    {
        if (value.Length == 0 || value[0] == 46 || value[value.Length - 1] == 46)
        {
            return true;
        }

        int index = 1;
        while (index < value.Length)
        {
            if (value[index] == 46 && value[index - 1] == 46)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool ContainsEncodedWordIntroducer(byte[] value)
    {
        int index = 0;
        while (index + 1 < value.Length)
        {
            if (value[index] == 61 && value[index + 1] == 63)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsRecoverableDotFragment(EmailHeaderToken token)
    {
        if (token.GetKind() != EmailHeaderTokenKind.Invalid)
        {
            return false;
        }

        byte[] raw = token.GetRawBytes();
        bool hasDot = false;
        int index = 0;
        while (index < raw.Length)
        {
            if (raw[index] == 46)
            {
                hasDot = true;
            }
            else if (!new EmailByteScanner().IsAtext(raw[index]) && raw[index] < 128)
            {
                return false;
            }

            index++;
        }

        return hasDot;
    }

    bool IsSpecialAt(int tokenIndex, byte value)
    {
        if (tokenIndex < 0 || tokenIndex >= tokens.Count || tokens[tokenIndex].GetKind() != EmailHeaderTokenKind.Special)
        {
            return false;
        }

        byte[] raw = tokens[tokenIndex].GetRawBytes();
        return raw.Length == 1 && raw[0] == value;
    }
}
