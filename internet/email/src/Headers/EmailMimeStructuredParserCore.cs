using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailMimePrimaryValue
{
    internal bool success;
    internal string? first;
    internal string? second;
    internal EmailSourcePosition? sourcePosition;
    internal EmailParameterList parameters;

    internal EmailMimePrimaryValue()
    {
        success = false;
        first = null;
        second = null;
        sourcePosition = null;
        parameters = new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }
}
internal class EmailMimeQuotedValue
{
    internal bool success;
    internal byte[] valueBytes;
    internal int rawStart;
    internal int rawEnd;

    internal EmailMimeQuotedValue()
    {
        success = false;
        valueBytes = new byte[0];
        rawStart = 0;
        rawEnd = 0;
    }
}
internal class EmailMimeStructuredParserCore
{
    internal static EmailParameterParseResult ParseParameters(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailStructuredValueParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            return new EmailParameterParseResult(new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), CreateLimitIssue("The parameter sequence exceeds the configured input-byte limit.", initialPosition), 0, source.Length);
        }

        EmailMimeStructuredParserWorker worker = new EmailMimeStructuredParserWorker(source, snapshot, initialPosition);
        EmailParameterList value = worker.ParseStandaloneParameters();
        return new EmailParameterParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailMediaTypeParseResult ParseMediaType(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailStructuredValueParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            EmailIssueList limited = CreateLimitIssue("The media type exceeds the configured input-byte limit.", initialPosition);
            return new EmailMediaTypeParseResult(new EmailMediaTypeOperations().CreateDefaultWith(), limited, 0, source.Length);
        }

        EmailMimeStructuredParserWorker worker = new EmailMimeStructuredParserWorker(source, snapshot, initialPosition);
        EmailMimePrimaryValue parsed = worker.ParsePrimary(true);
        if (!parsed.success)
        {
            worker.AddContextIssue(EmailIssueCode.MediaTypeDefaultApplied, "The invalid media type was replaced by the MIME default text/plain; charset=us-ascii.", initialPosition, "Use the default as a safe interpretation, while retaining the original field bytes separately.");
            return new EmailMediaTypeParseResult(new EmailMediaTypeOperations().CreateDefaultWithRawBytesAndPosition(source, initialPosition), worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
        }

        EmailMediaType value = new EmailMediaType(parsed.first, parsed.second, parsed.parameters, source, parsed.sourcePosition, false);
        return new EmailMediaTypeParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailDispositionParseResult ParseDisposition(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailStructuredValueParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            return new EmailDispositionParseResult(null, CreateLimitIssue("The disposition exceeds the configured input-byte limit.", initialPosition), 0, source.Length);
        }

        EmailMimeStructuredParserWorker worker = new EmailMimeStructuredParserWorker(source, snapshot, initialPosition);
        EmailMimePrimaryValue parsed = worker.ParsePrimary(false);
        EmailDisposition? value = null;
        if (parsed.success)
        {
            value = new EmailDisposition(parsed.first, parsed.parameters, source, parsed.sourcePosition);
        }

        return new EmailDispositionParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    internal static EmailContentLanguageParseResult ParseContentLanguage(ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition)
    {
        Validate(options, (initialPosition ?? throw new InvalidOperationException()));
        byte[] source = input.ToArray();
        EmailStructuredValueParserOptions snapshot = options.Clone();
        if (source.Length > snapshot.GetMaximumInputBytes())
        {
            return new EmailContentLanguageParseResult(new EmailContentLanguage(new List<string>(), new byte[0], initialPosition), CreateLimitIssue("The content-language value exceeds the configured input-byte limit.", initialPosition), 0, source.Length);
        }

        EmailMimeStructuredParserWorker worker = new EmailMimeStructuredParserWorker(source, snapshot, initialPosition);
        EmailSourcePosition position = worker.GetPosition();
        List<string> languages = worker.ParseLanguages();
        EmailContentLanguage value = new EmailContentLanguage(languages, source, position);
        return new EmailContentLanguageParseResult(value, worker.GetIssues(), worker.GetConsumedByteCount(), source.Length);
    }

    static void Validate(EmailStructuredValueParserOptions options, EmailSourcePosition initialPosition)
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

    static EmailIssueList CreateLimitIssue(string description, EmailSourcePosition position)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.StructuredParameterLimitExceeded, description, position, null, null, "No structured value bytes were parsed."));
        return issues;
    }
}
internal class EmailMimeStructuredParserWorker
{
    byte[] input;
    EmailStructuredValueParserOptions options;
    EmailSourceTracker tracker;
    EmailIssueList issues;
    List<EmailParameter> parameters;
    List<EmailRfc2231Parameter> rfcParameters;
    int index;
    bool stopped;

    internal EmailMimeStructuredParserWorker(byte[] input, EmailStructuredValueParserOptions options, EmailSourcePosition initialPosition)
    {
        this.input = input;
        this.options = options;
        tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        issues = new EmailIssueList();
        parameters = new List<EmailParameter>();
        rfcParameters = new List<EmailRfc2231Parameter>();
        index = 0;
        stopped = false;
    }

    internal EmailMimePrimaryValue ParsePrimary(bool mediaType)
    {
        EmailMimePrimaryValue value = new EmailMimePrimaryValue();
        SkipCfws();
        value.sourcePosition = tracker.GetPosition();
        string first = ReadToken();
        if (first.Length == 0)
        {
            AddContextIssue(mediaType ? EmailIssueCode.MediaTypeMalformed : EmailIssueCode.DispositionMalformed, mediaType ? "A media type token is required." : "A disposition type token is required.", tracker.GetPosition(), "No explicit structured value was produced.");
            return value;
        }

        string? second = null;
        if (mediaType)
        {
            SkipCfws();
            if (!Consume((byte)'/'))
            {
                AddContextIssue(EmailIssueCode.MediaTypeMalformed, "A media type must contain a slash between type and subtype.", tracker.GetPosition(), "The invalid media type was not used.");
                RecoverToEnd();
                return value;
            }

            SkipCfws();
            second = ReadToken();
            if (second.Length == 0)
            {
                AddContextIssue(EmailIssueCode.MediaTypeMalformed, "A media subtype token is required.", tracker.GetPosition(), "The invalid media type was not used.");
                RecoverToEnd();
                return value;
            }
        }

        ParseParameterSequence(false);
        value.success = true;
        value.first = first;
        value.second = second;
        value.parameters = DecodeParameters();
        return value;
    }

    internal List<string> ParseLanguages()
    {
        List<string> languages = new List<string>();
        bool expectLanguage = true;
        SkipCfws();
        while (index < input.Length && !stopped)
        {
            if (input[index] == ',')
            {
                if (expectLanguage)
                {
                    AddContextIssue(EmailIssueCode.ContentLanguageMalformed, "The language list contains an empty member.", tracker.GetPosition(), "The empty member was ignored.");
                }

                Advance();
                expectLanguage = true;
                SkipCfws();
                continue;
            }

            EmailSourcePosition position = tracker.GetPosition();
            string language = ReadLanguageToken();
            if (language.Length == 0)
            {
                AddContextIssue(EmailIssueCode.ContentLanguageMalformed, "A content-language member is malformed.", position, "Bytes through the next comma were ignored.");
                RecoverToComma();
                continue;
            }

            if (languages.Count >= options.GetMaximumParameters())
            {
                AddFatal("The language list exceeds the configured item-count limit.", position);
                break;
            }

            languages.Add(language);
            expectLanguage = false;
            int tokenEnd = index;
            SkipCfws();
            if (index < input.Length && input[index] != ',')
            {
                if (index == tokenEnd)
                {
                    languages.RemoveAt(languages.Count - 1);
                }

                AddContextIssue(EmailIssueCode.ContentLanguageMalformed, "Language tags must be separated by commas.", tracker.GetPosition(), "Bytes through the next comma were ignored.");
                RecoverToComma();
            }
        }

        if (!stopped && (languages.Count == 0 || expectLanguage))
        {
            AddContextIssue(EmailIssueCode.ContentLanguageMalformed, languages.Count == 0 ? "At least one content-language tag is required." : "The language list ends with an empty member.", tracker.GetPosition(), "Only complete language tags were retained.");
        }

        return languages;
    }

    internal EmailParameterList ParseStandaloneParameters()
    {
        ParseParameterSequence(true);
        return DecodeParameters();
    }

    internal EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    internal int GetConsumedByteCount()
    {
        return stopped ? index : input.Length;
    }

    internal EmailSourcePosition GetPosition()
    {
        return tracker.GetPosition();
    }

    internal void AddContextIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    void ParseParameterSequence(bool allowFirstWithoutSemicolon)
    {
        SkipCfws();
        bool first = true;
        while (index < input.Length && !stopped)
        {
            if (input[index] != ';')
            {
                if (!allowFirstWithoutSemicolon || !first)
                {
                    AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "Unexpected bytes appear between structured parameters.", tracker.GetPosition(), "Bytes through the next semicolon were ignored.");
                    RecoverToSemicolon();
                    first = false;
                    continue;
                }
            }
            else
            {
                Advance();
                SkipCfws();
            }

            if (index >= input.Length)
            {
                AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A trailing semicolon has no parameter.", tracker.GetPosition(), "The empty parameter was ignored.");
                break;
            }

            first = false;
            int parameterStart = index;
            EmailSourcePosition position = tracker.GetPosition();
            string name = ReadToken();
            if (name.Length == 0)
            {
                AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A parameter name is missing or malformed.", position, "The malformed parameter was skipped.");
                RecoverToSemicolon();
                continue;
            }

            SkipCfws();
            if (!Consume((byte)'='))
            {
                AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A parameter is missing its equals sign.", tracker.GetPosition(), "The malformed parameter was skipped.");
                RecoverToSemicolon();
                continue;
            }

            SkipCfws();
            int rawValueStart = index;
            byte[] valueBytes;
            bool quoted = false;
            int rawValueEnd;
            if (index < input.Length && input[index] == 34)
            {
                quoted = true;
                EmailMimeQuotedValue quotedValue = ReadQuotedValue();
                valueBytes = quotedValue.valueBytes;
                rawValueEnd = quotedValue.rawEnd;
                if (!quotedValue.success)
                {
                    AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A quoted parameter value is not terminated.", position, "The available quoted value was retained for tolerant recovery.");
                }
            }
            else
            {
                string token = ReadToken();
                valueBytes = Encoding.ASCII.GetBytes(token);
                rawValueEnd = index;
                if (token.Length == 0)
                {
                    AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A parameter value is missing or malformed.", tracker.GetPosition(), "The malformed parameter was skipped.");
                    RecoverToSemicolon();
                    continue;
                }
            }

            if (parameters.Count >= options.GetMaximumParameters())
            {
                AddFatal("The structured value exceeds the configured parameter-count limit.", position);
                break;
            }

            byte[] rawValue = CopyRange(rawValueStart, rawValueEnd);
            byte[] rawParameter = CopyRange(parameterStart, rawValueEnd);
            parameters.Add(new EmailParameter(name, rawValue, valueBytes, quoted, rawParameter, position));
            rfcParameters.Add(new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuotedAndSourcePosition(name, valueBytes, quoted, position));
            SkipCfws();
        }
    }

    EmailParameterList DecodeParameters()
    {
        EmailRfc2231DecodeMode mode = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailRfc2231DecodeMode.Strict : EmailRfc2231DecodeMode.Tolerant;
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndModeAndMaximumSegmentsAndMaximumDecodedBytes(rfcParameters, mode, options.GetMaximumParameters(), options.GetMaximumDecodedParameterBytes());
        issues.AddRange(decoded.GetIssues());
        return new EmailParameterList(parameters, decoded.GetParameters());
    }

    EmailMimeQuotedValue ReadQuotedValue()
    {
        EmailMimeQuotedValue result = new EmailMimeQuotedValue();
        result.rawStart = index;
        List<byte> value = new List<byte>();
        Advance();
        bool pendingSpace = false;
        while (index < input.Length)
        {
            byte current = input[index];
            if (current == 34)
            {
                if (pendingSpace)
                {
                    value.Add(32);
                    pendingSpace = false;
                }

                Advance();
                result.success = true;
                break;
            }

            if (current == 92)
            {
                Advance();
                if (index >= input.Length)
                {
                    break;
                }

                if (pendingSpace)
                {
                    value.Add(32);
                    pendingSpace = false;
                }

                value.Add(input[index]);
                Advance();
                continue;
            }

            if (current == 9 || current == 32 || IsFold())
            {
                pendingSpace = true;
                ConsumeFws();
                continue;
            }

            if (current < 32 || current == 127 || current > 127)
            {
                AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A quoted parameter contains a prohibited byte.", tracker.GetPosition(), "The byte was retained for tolerant recovery.");
            }

            if (pendingSpace)
            {
                value.Add(32);
                pendingSpace = false;
            }

            value.Add(current);
            Advance();
        }

        result.rawEnd = index;
        result.valueBytes = value.ToArray();
        return result;
    }

    string ReadToken()
    {
        int start = index;
        while (index < input.Length && IsMimeTokenByte(input[index]))
        {
            Advance();
        }

        return Encoding.ASCII.GetString(CopyRange(start, index));
    }

    string ReadLanguageToken()
    {
        int start = index;
        bool previousHyphen = false;
        bool saw = false;
        while (index < input.Length)
        {
            byte current = input[index];
            bool alphaNumeric = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9';
            if (!alphaNumeric && current != '-')
            {
                break;
            }

            if (current == '-' && (!saw || previousHyphen))
            {
                return "";
            }

            previousHyphen = current == '-';
            saw = true;
            Advance();
        }

        if (!saw || previousHyphen)
        {
            return "";
        }

        string value = Encoding.ASCII.GetString(CopyRange(start, index));
        return IsLanguageTag(value) ? value : "";
    }

    void SkipCfws()
    {
        bool consumed = true;
        while (consumed && index < input.Length && !stopped)
        {
            consumed = false;
            if (input[index] == 9 || input[index] == 32 || IsFold())
            {
                ConsumeFws();
                consumed = true;
            }

            if (index < input.Length && input[index] == 40)
            {
                ConsumeComment();
                consumed = true;
            }
        }
    }

    void ConsumeComment()
    {
        EmailSourcePosition position = tracker.GetPosition();
        int depth = 0;
        while (index < input.Length && !stopped)
        {
            byte current = input[index];
            if (current == 40)
            {
                depth++;
                if (depth > EmailParsingLimitsConstants.DefaultMaxNestingDepth)
                {
                    AddFatal("A structured comment exceeds the nesting-depth limit.", tracker.GetPosition());
                    return;
                }

                Advance();
                continue;
            }

            if (current == 41)
            {
                Advance();
                depth--;
                if (depth == 0)
                {
                    return;
                }

                continue;
            }

            if (current == 92)
            {
                Advance();
                if (index < input.Length)
                {
                    Advance();
                }

                continue;
            }

            Advance();
        }

        if (!stopped)
        {
            AddContextIssue(EmailIssueCode.StructuredParameterMalformed, "A structured comment is not terminated.", position, "The available comment bytes were ignored.");
        }
    }

    void ConsumeFws()
    {
        while (index < input.Length)
        {
            if (input[index] == 9 || input[index] == 32)
            {
                Advance();
            }
            else if (IsFold())
            {
                Advance();
                Advance();
            }
            else
            {
                break;
            }
        }
    }

    bool IsFold()
    {
        return index + 2 < input.Length && input[index] == 13 && input[index + 1] == 10 && (input[index + 2] == 9 || input[index + 2] == 32);
    }

    bool Consume(byte expected)
    {
        if (index >= input.Length || input[index] != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    void RecoverToSemicolon()
    {
        while (index < input.Length && input[index] != ';')
        {
            Advance();
        }
    }

    void RecoverToComma()
    {
        while (index < input.Length && input[index] != ',')
        {
            Advance();
        }
    }

    void RecoverToEnd()
    {
        while (index < input.Length)
        {
            Advance();
        }
    }

    void AddFatal(string description, EmailSourcePosition position)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.StructuredParameterLimitExceeded, description, position, null, null, "Parsing stopped at the configured resource boundary."));
        stopped = true;
    }

    void Advance()
    {
        tracker.Advance(input.AsSpan(index, 1));
        index++;
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
        int cursor = start;
        while (cursor < end)
        {
            copy[cursor - start] = input[cursor];
            cursor++;
        }

        return copy;
    }

    static bool IsMimeTokenByte(byte value)
    {
        if (value < 33 || value > 126)
        {
            return false;
        }

        return value != 40 && value != 41 && value != 60 && value != 62 && value != 64 && value != 44 && value != 59 && value != 58 && value != 92 && value != 34 && value != 47 && value != 91 && value != 93 && value != 63 && value != 61;
    }

    static bool IsLanguageTag(string value)
    {
        int segmentLength = 0;
        int segment = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '-')
            {
                if (segmentLength < 1 || segmentLength > 8)
                {
                    return false;
                }

                segment++;
                segmentLength = 0;
            }
            else
            {
                bool letter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                if (!letter && (!digit || segment == 0))
                {
                    return false;
                }

                segmentLength++;
            }

            index++;
        }

        return segmentLength >= 1 && segmentLength <= 8;
    }
}
