using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailAuthenticationParserCore
{
    internal static EmailAuthenticationResultsParseResult ParseAuthenticationResults(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailAuthenticationParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = CreateLimitIssue(initialPosition);
            return new EmailAuthenticationResultsParseResult(null, issues, 0, source.Length);
        }

        EmailAuthenticationParserWorker worker = new EmailAuthenticationParserWorker(source, snapshot, initialPosition);
        return worker.ParseAuthenticationResults();
    }

    internal static EmailReceivedSpfParseResult ParseReceivedSpf(ReadOnlySpan<byte> input, EmailAuthenticationParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailAuthenticationParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList issues = CreateLimitIssue(initialPosition);
            return new EmailReceivedSpfParseResult(null, issues, 0, source.Length);
        }

        EmailAuthenticationParserWorker worker = new EmailAuthenticationParserWorker(source, snapshot, initialPosition);
        return worker.ParseReceivedSpf();
    }

    static void Validate(EmailAuthenticationParserOptions options, EmailSourcePosition initialPosition)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }
    }

    static EmailIssueList CreateLimitIssue(EmailSourcePosition position)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AuthenticationValueLimitExceeded, "The authentication field value exceeds the configured input-byte limit.", position, null, null, "No structured value was produced."));
        return issues;
    }
}
internal class EmailAuthenticationParsedText
{
    internal bool success;
    internal string? value;
    internal bool quoted;
    internal int end;

    internal EmailAuthenticationParsedText()
    {
        success = false;
        value = null;
        quoted = false;
        end = 0;
    }
}
internal class EmailAuthenticationParserWorker
{
    byte[] input;
    EmailAuthenticationParserOptions options;
    EmailSourcePosition initialPosition;
    List<EmailHeaderToken> tokens;
    EmailIssueList issues;
    int lexicalConsumedByteCount;
    int stopByteCount;

    internal EmailAuthenticationParserWorker(byte[] input, EmailAuthenticationParserOptions options, EmailSourcePosition initialPosition)
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

    internal EmailAuthenticationResultsParseResult ParseAuthenticationResults()
    {
        if (HasFatal())
        {
            return new EmailAuthenticationResultsParseResult(null, issues, lexicalConsumedByteCount, input.Length);
        }

        int firstSemicolon = FindSpecial(0, tokens.Count, 59);
        int identityEnd = firstSemicolon >= 0 ? firstSemicolon : tokens.Count;
        List<int> identityTokens = GetSignificant(0, identityEnd);
        if (identityTokens.Count == 0)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "Authentication-Results has no authentication service identifier.", initialPosition, "No structured value was produced.");
            return new EmailAuthenticationResultsParseResult(null, issues, input.Length, input.Length);
        }

        EmailHeaderToken identifierToken = tokens[identityTokens[0]];
        string identifier = GetValue(identifierToken);
        bool identityValid = IsValueToken(identifierToken);
        if (!identityValid)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "The authentication service identifier is malformed.", identifierToken.GetSourcePosition(), "The original identifier token was retained.");
        }

        bool versionPresent = false;
        int version = 0;
        if (identityTokens.Count > 1)
        {
            string versionText = GetRawText(tokens[identityTokens[1]]);
            if (identityTokens.Count == 2 && IsDigits(versionText) && HasCfwsBetween(identityTokens[0], identityTokens[1]))
            {
                versionPresent = true;
                version = ParseDigits(versionText);
            }
            else
            {
                AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "The Authentication-Results version is malformed.", tokens[identityTokens[1]].GetSourcePosition(), "The unrecognized version bytes remain available in the raw value.");
            }
        }

        List<EmailAuthenticationComment> headerComments = GetComments(0, identityEnd);
        List<EmailAuthenticationMethodResult> results = new List<EmailAuthenticationMethodResult>();
        bool noResults = false;
        if (firstSemicolon < 0)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "Authentication-Results has no semicolon-delimited result.", identifierToken.GetSourcePosition(), "The authentication service identifier and raw value were retained.");
        }
        else
        {
            int separator = firstSemicolon;
            while (separator >= 0 && separator < tokens.Count && !HasFatal())
            {
                int nextSeparator = FindSpecial(separator + 1, tokens.Count, 59);
                int segmentEnd = nextSeparator >= 0 ? nextSeparator : tokens.Count;
                List<int> segment = GetSignificant(separator + 1, segmentEnd);
                if (segment.Count == 1 && IsAtomText(segment[0], "none"))
                {
                    if (results.Count > 0 || noResults || nextSeparator >= 0)
                    {
                        AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "The no-results marker cannot accompany other authentication results.", tokens[segment[0]].GetSourcePosition(), "The marker and all method results were retained.");
                    }

                    noResults = true;
                    headerComments.AddRange(GetComments(separator + 1, segmentEnd));
                }
                else if (segment.Count == 0)
                {
                    AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "Authentication-Results contains an empty result segment.", GetPositionAt(GetTokenOffset(separator) + tokens[separator].GetByteCount()), "The empty segment was ignored.");
                }
                else
                {
                    if (results.Count >= options.GetMaximumResults())
                    {
                        AddFatal("Authentication-Results exceeds the configured method-result limit.", tokens[segment[0]].GetSourcePosition());
                        break;
                    }

                    EmailAuthenticationMethodResult? result = ParseMethodResult(segment, separator, segmentEnd);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }

                separator = nextSeparator;
            }
        }

        if (noResults && results.Count > 0)
        {
            noResults = false;
        }

        EmailAuthenticationResults value = new EmailAuthenticationResults(identifier.Length == 0 ? "unknown" : identifier, versionPresent, version, noResults, results, headerComments, input, identifierToken.GetSourcePosition());
        int consumed = HasFatal() ? GetStopByteCount() : input.Length;
        return new EmailAuthenticationResultsParseResult(value, issues, consumed, input.Length);
    }

    internal EmailReceivedSpfParseResult ParseReceivedSpf()
    {
        if (HasFatal())
        {
            return new EmailReceivedSpfParseResult(null, issues, lexicalConsumedByteCount, input.Length);
        }

        List<int> significant = GetSignificant(0, tokens.Count);
        if (significant.Count == 0)
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "Received-SPF has no result token.", initialPosition, "No structured value was produced.");
            return new EmailReceivedSpfParseResult(null, issues, input.Length, input.Length);
        }

        int resultTokenIndex = significant[0];
        EmailHeaderToken resultToken = tokens[resultTokenIndex];
        string resultText = GetRawText(resultToken);
        EmailAuthenticationResultKind resultKind = GetResultKind(resultText);
        if (resultToken.GetKind() != EmailHeaderTokenKind.Atom || !IsSpfResult(resultKind))
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "Received-SPF contains an unknown or malformed result token.", resultToken.GetSourcePosition(), "The original result was retained as unknown.");
            resultKind = EmailAuthenticationResultKind.Unknown;
        }

        List<EmailAuthenticationComment> comments = GetComments(resultTokenIndex + 1, tokens.Count);
        List<EmailReceivedSpfParameter> parameters = new List<EmailReceivedSpfParameter>();
        int firstSeparator = FindSpecial(resultTokenIndex + 1, tokens.Count, 59);
        int firstEnd = firstSeparator >= 0 ? firstSeparator : tokens.Count;
        ParseSpfParameter(parameters, resultTokenIndex + 1, firstEnd, false);
        int separator = firstSeparator;
        while (separator >= 0 && separator < tokens.Count && !HasFatal())
        {
            int next = FindSpecial(separator + 1, tokens.Count, 59);
            int end = next >= 0 ? next : tokens.Count;
            bool trailing = next < 0 && GetSignificant(separator + 1, end).Count == 0;
            if (!trailing)
            {
                ParseSpfParameter(parameters, separator + 1, end, true);
            }

            separator = next;
        }

        EmailReceivedSpf value = new EmailReceivedSpf(resultText.Length == 0 ? "unknown" : resultText, resultKind, comments, parameters, input, resultToken.GetSourcePosition());
        int consumed = HasFatal() ? GetStopByteCount() : input.Length;
        return new EmailReceivedSpfParseResult(value, issues, consumed, input.Length);
    }

    EmailAuthenticationMethodResult? ParseMethodResult(List<int> segment, int separator, int segmentEnd)
    {
        int issueCount = issues.GetCount();
        int rawStartOffset = GetTokenOffset(separator) + tokens[separator].GetByteCount();
        int rawEndOffset = GetSegmentEndOffset(segmentEnd);
        int cursor = SkipCfwsBytes(rawStartOffset, rawEndOffset);
        int methodStart = cursor;
        int methodEnd = ReadNameEnd(cursor, rawEndOffset, true);
        string method = GetText(methodStart, methodEnd);
        EmailSourcePosition methodPosition = GetPositionAt(methodStart);
        if (method.Length == 0)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication method name is malformed.", methodPosition, "The result segment remains available in raw form.");
            return null;
        }

        cursor = SkipCfwsBytes(methodEnd, rawEndOffset);
        bool versionPresent = false;
        int version = 0;
        if (cursor < rawEndOffset && input[cursor] == 47)
        {
            cursor = SkipCfwsBytes(cursor + 1, rawEndOffset);
            int versionStart = cursor;
            while (cursor < rawEndOffset && input[cursor] >= 48 && input[cursor] <= 57)
            {
                cursor++;
            }

            string versionText = GetText(versionStart, cursor);
            if (versionText.Length == 0)
            {
                AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication method version is malformed.", GetPositionAt(versionStart), "The method was retained without a version.");
            }
            else
            {
                versionPresent = true;
                version = ParseDigits(versionText);
            }

            cursor = SkipCfwsBytes(cursor, rawEndOffset);
        }

        if (cursor >= rawEndOffset || input[cursor] != 61)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication method is missing its equals sign.", methodPosition, "The malformed result segment remains available in raw form.");
            return null;
        }

        cursor = SkipCfwsBytes(cursor + 1, rawEndOffset);
        int resultStart = cursor;
        int resultEnd = ReadNameEnd(cursor, rawEndOffset, false);
        string result = GetText(resultStart, resultEnd);
        if (result.Length == 0)
        {
            AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication method is missing its result token.", methodPosition, "The malformed result segment remains available in raw form.");
            return null;
        }

        EmailAuthenticationResultKind resultKind = GetResultKind(result);
        cursor = resultEnd;
        bool reasonPresent = false;
        string? reason = null;
        bool reasonQuoted = false;
        List<EmailAuthenticationProperty> properties = new List<EmailAuthenticationProperty>();
        while (cursor < rawEndOffset && !HasFatal())
        {
            cursor = SkipCfwsBytes(cursor, rawEndOffset);
            if (cursor >= rawEndOffset)
            {
                break;
            }

            int specificationStart = cursor;
            int keyEnd = ReadNameEnd(cursor, rawEndOffset, false);
            string key = GetText(cursor, keyEnd);
            cursor = SkipCfwsBytes(keyEnd, rawEndOffset);
            if (key.Length == 0 || cursor >= rawEndOffset || input[cursor] != 61)
            {
                AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "Unrecognized data appears in an authentication result segment.", GetPositionAt(specificationStart), "The bytes remain available in the raw method result.");
                cursor = keyEnd > specificationStart ? keyEnd : specificationStart + 1;
                continue;
            }

            cursor = SkipCfwsBytes(cursor + 1, rawEndOffset);
            int valueStart = cursor;
            EmailAuthenticationParsedText parsedValue = ReadValueBytes(cursor, rawEndOffset);
            if (!parsedValue.success)
            {
                AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication specification has no usable value.", GetPositionAt(specificationStart), "The incomplete specification was ignored.");
                break;
            }

            cursor = parsedValue.end;
            if (string.Equals(key, "reason", StringComparison.OrdinalIgnoreCase))
            {
                if (reasonPresent)
                {
                    AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication result contains more than one reason.", GetPositionAt(specificationStart), "The last reason value was retained.");
                }

                reasonPresent = true;
                reason = parsedValue.value;
                reasonQuoted = parsedValue.quoted;
                continue;
            }

            int dot = key.IndexOf('.');
            if (dot <= 0 || dot >= key.Length - 1)
            {
                AddMalformed(EmailIssueCode.AuthenticationResultsMalformed, "An authentication property does not have a type and property name.", GetPositionAt(specificationStart), "The malformed specification remains available in the raw method result.");
                continue;
            }

            if (properties.Count >= options.GetMaximumProperties())
            {
                AddFatal("An authentication result exceeds the configured property limit.", GetPositionAt(specificationStart));
                break;
            }

            properties.Add(new EmailAuthenticationProperty(key.Substring(0, dot), key.Substring(dot + 1), parsedValue.value, parsedValue.quoted, CopyRange(specificationStart, parsedValue.end), CopyRange(valueStart, parsedValue.end), GetPositionAt(specificationStart)));
        }

        List<EmailAuthenticationComment> comments = GetComments(separator + 1, segmentEnd);
        bool malformed = issues.GetCount() > issueCount;
        return new EmailAuthenticationMethodResult(method, versionPresent, version, result, resultKind, reasonPresent, reason, reasonQuoted, properties, comments, CopyRange(rawStartOffset, rawEndOffset), methodPosition, malformed);
    }

    void ParseSpfParameter(List<EmailReceivedSpfParameter> parameters, int tokenStart, int tokenEnd, bool requireValue)
    {
        int rawStart = tokenStart < tokens.Count ? GetTokenOffset(tokenStart) : input.Length;
        int rawEnd = tokenEnd < tokens.Count ? GetTokenOffset(tokenEnd) : input.Length;
        int cursor = SkipCfwsBytes(rawStart, rawEnd);
        if (cursor >= rawEnd)
        {
            if (requireValue)
            {
                AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "Received-SPF contains an empty parameter segment.", tokenStart < tokens.Count ? tokens[tokenStart].GetSourcePosition() : initialPosition, "The empty segment was ignored.");
            }

            return;
        }

        int parameterStart = cursor;
        int keyEnd = ReadNameEnd(cursor, rawEnd, false);
        string key = GetText(cursor, keyEnd);
        cursor = SkipCfwsBytes(keyEnd, rawEnd);
        if (key.Length == 0 || cursor >= rawEnd || input[cursor] != 61)
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "A Received-SPF key-value pair is malformed.", GetPositionAt(parameterStart), "The malformed pair remains available in the raw field value.");
            return;
        }

        if (parameterStart == rawStart && tokenStart > 0 && !IsSpecial(tokenStart - 1, 59))
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "The Received-SPF result and first parameter are not separated by folding whitespace.", GetPositionAt(parameterStart), "The parameter was retained.");
        }

        if (parameters.Count >= options.GetMaximumProperties())
        {
            AddFatal("Received-SPF exceeds the configured parameter limit.", GetPositionAt(parameterStart));
            return;
        }

        cursor = SkipCfwsBytes(cursor + 1, rawEnd);
        int valueStart = cursor;
        EmailAuthenticationParsedText parsedValue = ReadValueBytes(cursor, rawEnd);
        if (!parsedValue.success)
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "A Received-SPF key-value pair has no usable value.", GetPositionAt(parameterStart), "The malformed pair remains available in the raw field value.");
            return;
        }

        int remainder = SkipCfwsBytes(parsedValue.end, rawEnd);
        if (remainder < rawEnd)
        {
            AddMalformed(EmailIssueCode.ReceivedSpfMalformed, "Unexpected data follows a Received-SPF parameter value.", GetPositionAt(remainder), "The first value and complete raw pair were retained.");
        }

        parameters.Add(new EmailReceivedSpfParameter(key, parsedValue.value, parsedValue.quoted, CopyRange(parameterStart, rawEnd), CopyRange(valueStart, parsedValue.end), GetPositionAt(parameterStart)));
    }

    int SkipCfwsBytes(int start, int end)
    {
        int cursor = start;
        bool consumed = true;
        while (cursor < end && consumed)
        {
            consumed = false;
            while (cursor < end && (input[cursor] == 9 || input[cursor] == 32 || input[cursor] == 13 || input[cursor] == 10))
            {
                cursor++;
                consumed = true;
            }

            if (cursor < end && input[cursor] == 40)
            {
                int depth = 0;
                bool escaped = false;
                while (cursor < end)
                {
                    byte current = input[cursor];
                    cursor++;
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == 92)
                    {
                        escaped = true;
                    }
                    else if (current == 40)
                    {
                        depth++;
                    }
                    else if (current == 41)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            break;
                        }
                    }
                }

                consumed = true;
            }
        }

        return cursor;
    }

    int ReadNameEnd(int start, int end, bool stopAtSlash)
    {
        int cursor = start;
        while (cursor < end)
        {
            byte current = input[cursor];
            if (current == 9 || current == 32 || current == 13 || current == 10 || current == 40 || current == 61 || stopAtSlash && current == 47)
            {
                break;
            }

            cursor++;
        }

        return cursor;
    }

    EmailAuthenticationParsedText ReadValueBytes(int start, int end)
    {
        EmailAuthenticationParsedText parsed = new EmailAuthenticationParsedText();
        parsed.end = start;
        if (start >= end)
        {
            return parsed;
        }

        if (input[start] == 34)
        {
            StringBuilder value = new StringBuilder();
            int cursor = start + 1;
            bool escaped = false;
            while (cursor < end)
            {
                byte current = input[cursor];
                cursor++;
                if (escaped)
                {
                    value.Append((char)current);
                    escaped = false;
                }
                else if (current == 92)
                {
                    escaped = true;
                }
                else if (current == 34)
                {
                    parsed.success = true;
                    parsed.value = Unfold(value.ToString());
                    parsed.quoted = true;
                    parsed.end = cursor;
                    return parsed;
                }
                else
                {
                    value.Append((char)current);
                }
            }

            parsed.value = Unfold(value.ToString());
            parsed.quoted = true;
            parsed.end = end;
            return parsed;
        }

        int valueEnd = start;
        while (valueEnd < end)
        {
            byte current = input[valueEnd];
            if (current == 9 || current == 32 || current == 13 || current == 10 || current == 40)
            {
                break;
            }

            valueEnd++;
        }

        if (valueEnd > start)
        {
            parsed.success = true;
            parsed.value = GetText(start, valueEnd);
            parsed.quoted = false;
            parsed.end = valueEnd;
        }

        return parsed;
    }

    string GetText(int start, int end)
    {
        return Encoding.UTF8.GetString(CopyRange(start, end));
    }

    List<EmailAuthenticationComment> GetComments(int startToken, int endToken)
    {
        List<EmailAuthenticationComment> comments = new List<EmailAuthenticationComment>();
        int tokenIndex = startToken;
        while (tokenIndex < endToken)
        {
            EmailHeaderToken token = tokens[tokenIndex];
            if (token.GetKind() == EmailHeaderTokenKind.Cfws)
            {
                byte[] raw = token.GetRawBytes();
                int index = 0;
                while (index < raw.Length)
                {
                    if (raw[index] != 40)
                    {
                        index++;
                        continue;
                    }

                    int commentStart = index;
                    int depth = 0;
                    bool escaped = false;
                    StringBuilder value = new StringBuilder();
                    while (index < raw.Length)
                    {
                        byte current = raw[index];
                        if (escaped)
                        {
                            value.Append((char)current);
                            escaped = false;
                        }
                        else if (current == 92)
                        {
                            escaped = true;
                        }
                        else if (current == 40)
                        {
                            depth++;
                            if (depth > 1)
                            {
                                value.Append('(');
                            }
                        }
                        else if (current == 41)
                        {
                            depth--;
                            if (depth > 0)
                            {
                                value.Append(')');
                            }
                        }
                        else if (depth > 0)
                        {
                            value.Append((char)current);
                        }

                        index++;
                        if (depth == 0)
                        {
                            break;
                        }
                    }

                    int globalStart = GetTokenOffset(tokenIndex) + commentStart;
                    comments.Add(new EmailAuthenticationComment(Unfold(value.ToString()), CopyBytes(raw, commentStart, index), GetPositionAt(globalStart)));
                }
            }

            tokenIndex++;
        }

        return comments;
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

    int FindSpecial(int start, int end, byte value)
    {
        int index = start;
        while (index < end)
        {
            if (IsSpecial(index, value))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    bool IsSpecial(int tokenIndex, byte value)
    {
        if (tokenIndex < 0 || tokenIndex >= tokens.Count)
        {
            return false;
        }

        EmailHeaderToken token = tokens[tokenIndex];
        byte[] bytes = token.GetValueBytes();
        return token.GetKind() == EmailHeaderTokenKind.Special && bytes.Length == 1 && bytes[0] == value;
    }

    bool IsAtomText(int tokenIndex, string value)
    {
        return tokens[tokenIndex].GetKind() == EmailHeaderTokenKind.Atom && string.Equals(GetRawText(tokens[tokenIndex]), value, StringComparison.OrdinalIgnoreCase);
    }

    bool IsValueToken(EmailHeaderToken token)
    {
        EmailHeaderTokenKind kind = token.GetKind();
        return kind == EmailHeaderTokenKind.Atom || kind == EmailHeaderTokenKind.DotAtom || kind == EmailHeaderTokenKind.QuotedString || kind == EmailHeaderTokenKind.DomainLiteral;
    }

    bool HasCfwsBetween(int left, int right)
    {
        int index = left + 1;
        while (index < right)
        {
            if (tokens[index].GetKind() == EmailHeaderTokenKind.Cfws)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    EmailAuthenticationResultKind GetResultKind(string value)
    {
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.None;
        if (string.Equals(value, "pass", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.Pass;
        if (string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.Fail;
        if (string.Equals(value, "softfail", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.SoftFail;
        if (string.Equals(value, "policy", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.Policy;
        if (string.Equals(value, "neutral", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.Neutral;
        if (string.Equals(value, "temperror", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.TempError;
        if (string.Equals(value, "permerror", StringComparison.OrdinalIgnoreCase)) return EmailAuthenticationResultKind.PermError;
        return EmailAuthenticationResultKind.Unknown;
    }

    bool IsSpfResult(EmailAuthenticationResultKind kind)
    {
        return kind == EmailAuthenticationResultKind.None || kind == EmailAuthenticationResultKind.Pass || kind == EmailAuthenticationResultKind.Fail || kind == EmailAuthenticationResultKind.SoftFail || kind == EmailAuthenticationResultKind.Neutral || kind == EmailAuthenticationResultKind.TempError || kind == EmailAuthenticationResultKind.PermError;
    }

    string GetValue(EmailHeaderToken token)
    {
        return Encoding.UTF8.GetString(token.GetKind() == EmailHeaderTokenKind.QuotedString ? token.GetValueBytes() : token.GetRawBytes());
    }

    string GetRawText(EmailHeaderToken token)
    {
        return Encoding.UTF8.GetString(token.GetRawBytes());
    }

    bool IsDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    int ParseDigits(string value)
    {
        int parsed = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (parsed > (int.MaxValue - digit) / 10)
            {
                return int.MaxValue;
            }

            parsed = parsed * 10 + digit;
            index++;
        }

        return parsed;
    }

    string Unfold(string value)
    {
        StringBuilder output = new StringBuilder();
        bool whitespace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
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

    int GetSegmentEndOffset(int tokenEnd)
    {
        return tokenEnd < tokens.Count ? GetTokenOffset(tokenEnd) : input.Length;
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

    byte[] CopyBytes(byte[] value, int start, int end)
    {
        byte[] copy = new byte[end - start];
        value.AsSpan(start, end - start).CopyTo(copy);
        return copy;
    }

    bool HasFatal()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }

    void AddMalformed(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    void AddFatal(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.AuthenticationValueLimitExceeded, description, position, null, null, "Parsing stopped before the excessive item."));
        long relative = position.GetByteOffset() - initialPosition.GetByteOffset();
        stopByteCount = relative >= 0 && relative <= input.Length ? (int)relative : 0;
    }

    int GetStopByteCount()
    {
        return stopByteCount >= 0 ? stopByteCount : lexicalConsumedByteCount;
    }
}
