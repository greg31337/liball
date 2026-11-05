using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with encoded word.
/// </summary>
public class EmailEncodedWord : IEmailEncodedWordContract
{
    /// <summary>
    /// The maximum word bytes value exposed by this API.
    /// </summary>
    internal const int MaximumWordBytes = 75;
    /// <summary>
    /// The default maximum encoded words used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumEncodedWords = 128;
    /// <summary>
    /// The default maximum decoded bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumDecodedBytes = 1048576;
    internal static EmailEncodedWordEncodeResult EncodeImplementation(ReadOnlySpan<byte> input, EmailEncodedWordGenerationOptions options)
    {
        return new EmailEncodedWordGenerator().Encode(input, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEncodedWordEncodeResult Encode(ReadOnlySpan<byte> input, EmailEncodedWordGenerationOptions options)
    {
        return EmailEncodedWord.EncodeImplementation(input, options);
    }

    internal static EmailEncodedWordEncodeResult EncodeTextImplementation(string input, System.Text.Encoding characterEncoding, EmailEncodedWordGenerationOptions options)
    {
        return new EmailEncodedWordGenerator().EncodeText(input, characterEncoding, options);
    }

    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEncodedWordEncodeResult EncodeText(string input, System.Text.Encoding characterEncoding, EmailEncodedWordGenerationOptions options)
    {
        return EmailEncodedWord.EncodeTextImplementation(input, characterEncoding, options);
    }

    internal static EmailEncodedWordParseResult ParseImplementationWithInputAndModeAndContext(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context)
    {
        return ParseImplementationWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(input, mode, context, DefaultMaximumEncodedWords, DefaultMaximumDecodedBytes);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "context">The context used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEncodedWordParseResult ParseWithInputAndModeAndContext(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context)
    {
        return EmailEncodedWord.ParseImplementationWithInputAndModeAndContext(input, mode, context);
    }

    internal static EmailEncodedWordParseResult ParseImplementationWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context, int maximumEncodedWords, long maximumDecodedBytes)
    {
        ValidateArguments(mode, context, maximumEncodedWords, maximumDecodedBytes);
        EmailEncodedWordParseState state = new EmailEncodedWordParseState();
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();
        int index = 0;
        while (index < input.Length)
        {
            EmailSourcePosition position = tracker.GetPosition();
            if (input[index] != 61 || index + 1 >= input.Length || input[index + 1] != 63)
            {
                state.AppendLiteral(input.Slice(index, 1), position);
                tracker.Advance(input.Slice(index, 1));
                index++;
                continue;
            }

            EmailEncodedWordCandidate candidate = ScanCandidate(input, index);
            int consumed = candidate.GetConsumedByteCount();
            ReadOnlySpan<byte> raw = input.Slice(index, consumed);
            if (!candidate.IsComplete())
            {
                AddMalformedIssue(state, mode, position, "An encoded-word introducer was not followed by a complete encoded word.", "The malformed source bytes were retained as literal text.");
                state.AppendLiteral(raw, position);
                tracker.Advance(raw);
                index += consumed;
                continue;
            }

            string charset = GetAscii(input.Slice(candidate.GetCharsetOffset(), candidate.GetCharsetByteCount()));
            string? language = null;
            bool languagePresent = false;
            int star = FindCharacter(charset, '*');
            if (star >= 0)
            {
                languagePresent = true;
                language = charset.Substring(star + 1);
                charset = charset.Substring(0, star);
            }

            if (!IsCharset(charset) || !IsLanguage(language, languagePresent))
            {
                AddMalformedIssue(state, mode, position, "The encoded word has an invalid charset or language declaration.", "The malformed source bytes were retained as literal text.");
                state.AppendLiteral(raw, position);
                tracker.Advance(raw);
                index += consumed;
                continue;
            }

            EmailEncodedWordEncoding encoding = GetEncoding(input.Slice(candidate.GetEncodingOffset(), candidate.GetEncodingByteCount()));
            if (encoding == EmailEncodedWordEncoding.None)
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordUnsupportedEncoding, "The encoded word uses an unsupported transfer encoding.", position, "The source bytes were retained as literal text.");
                state.AppendLiteral(raw, position);
                tracker.Advance(raw);
                index += consumed;
                continue;
            }

            if (candidate.GetPayloadByteCount() == 0)
            {
                AddMalformedIssue(state, mode, position, "The encoded word has an empty encoded-text section.", "The malformed source bytes were retained as literal text.");
                state.AppendLiteral(raw, position);
                tracker.Advance(raw);
                index += consumed;
                continue;
            }

            if (context == EmailEncodedWordContext.Forbidden)
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordContextViolation, "Encoded words are not permitted in this header context.", position, "The source bytes were retained as literal text.");
                state.AppendLiteral(raw, position);
                tracker.Advance(raw);
                index += consumed;
                continue;
            }

            if (state.GetEncodedWordCount() >= maximumEncodedWords)
            {
                state.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.EncodedWordLimitExceeded, "The encoded-word count exceeds the configured limit.", position, null, null, "The remaining source bytes were retained as literal text."));
                state.AppendLiteral(input.Slice(index), position);
                tracker.Advance(input.Slice(index));
                index = input.Length;
                continue;
            }

            if (consumed > MaximumWordBytes)
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordTooLong, "The encoded word exceeds the RFC 2047 limit of 75 characters.", position, "The word was decoded while the length defect was retained as an issue.");
            }

            if (!HasLegalSeparation(input, index, consumed))
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordSeparationViolation, "The encoded word is not separated from adjacent text by linear whitespace.", position, "The word was decoded while the separation defect was retained as an issue.");
            }

            ReadOnlySpan<byte> payload = input.Slice(candidate.GetPayloadOffset(), candidate.GetPayloadByteCount());
            bool contextViolation = HasPayloadContextViolation(payload, encoding, context);
            if (contextViolation)
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordContextViolation, "The Q-encoded text contains a byte that is not permitted in this header context.", position, "The word was decoded while the context defect was retained as an issue.");
            }

            long remaining = maximumDecodedBytes - state.GetDecodedByteCount();
            EmailEncodedWordDecodedValue decoded = Decode(payload, encoding, remaining);
            if (decoded.HasInvalidPayload())
            {
                AddIssue(state, mode, EmailIssueCode.EncodedWordInvalidPayload, "The encoded word contains malformed or non-canonical encoded text.", position, "Every recoverable payload byte was decoded.");
            }

            if (decoded.HasReachedLimit())
            {
                state.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.EncodedWordDecodedByteLimitExceeded, "Decoded encoded-word content exceeds the configured byte limit.", position, null, null, "Only the prefix within the configured limit was retained."));
            }

            byte[] rawBytes = raw.ToArray();
            EmailEncodedWordPart part = new EmailEncodedWordPartOperations().CreateEncodedWord(decoded.GetBytes(), rawBytes, charset, language, languagePresent, encoding, position);
            state.AddEncodedWord(part);
            tracker.Advance(raw);
            index += consumed;
            if (decoded.HasReachedLimit() && index < input.Length)
            {
                EmailSourcePosition remainderPosition = tracker.GetPosition();
                state.AppendLiteral(input.Slice(index), remainderPosition);
                tracker.Advance(input.Slice(index));
                index = input.Length;
            }
        }

        return state.CreateResult();
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "context">The context used by the operation.</param>
    /// <param name = "maximumEncodedWords">The maximum encoded words used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEncodedWordParseResult ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailEncodedWordParseMode mode, EmailEncodedWordContext context, int maximumEncodedWords, long maximumDecodedBytes)
    {
        return EmailEncodedWord.ParseImplementationWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(input, mode, context, maximumEncodedWords, maximumDecodedBytes);
    }

    static void ValidateArguments(EmailEncodedWordParseMode mode, EmailEncodedWordContext context, int maximumEncodedWords, long maximumDecodedBytes)
    {
        if (mode < EmailEncodedWordParseMode.Strict || mode > EmailEncodedWordParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (context < EmailEncodedWordContext.UnstructuredText || context > EmailEncodedWordContext.Forbidden)
        {
            throw new ArgumentOutOfRangeException("context");
        }

        if (maximumEncodedWords < 0)
        {
            throw new ArgumentOutOfRangeException("maximumEncodedWords");
        }

        if (maximumDecodedBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDecodedBytes");
        }
    }

    static EmailEncodedWordCandidate ScanCandidate(ReadOnlySpan<byte> input, int start)
    {
        int charsetEnd = FindByte(input, start + 2, 63);
        if (charsetEnd < 0)
        {
            return CreateIncompleteCandidate(input, start);
        }

        int encodingEnd = FindByte(input, charsetEnd + 1, 63);
        if (encodingEnd < 0)
        {
            return CreateIncompleteCandidate(input, start);
        }

        int end = FindTerminator(input, encodingEnd + 1);
        if (end < 0)
        {
            return CreateIncompleteCandidate(input, start);
        }

        return new EmailEncodedWordCandidate(end + 2 - start, true, start + 2, charsetEnd - start - 2, charsetEnd + 1, encodingEnd - charsetEnd - 1, encodingEnd + 1, end - encodingEnd - 1);
    }

    static EmailEncodedWordCandidate CreateIncompleteCandidate(ReadOnlySpan<byte> input, int start)
    {
        int end = start + 2;
        while (end < input.Length)
        {
            if (IsLinearWhitespace(input[end]))
            {
                break;
            }

            if (end > start + 2 && input[end] == 61 && end + 1 < input.Length && input[end + 1] == 63)
            {
                break;
            }

            end++;
        }

        return new EmailEncodedWordCandidate(end - start, false, 0, 0, 0, 0, 0, 0);
    }

    static int FindByte(ReadOnlySpan<byte> input, int start, byte value)
    {
        int index = start;
        while (index < input.Length)
        {
            if (input[index] == value)
            {
                return index;
            }

            if (IsLinearWhitespace(input[index]))
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    static int FindTerminator(ReadOnlySpan<byte> input, int start)
    {
        int index = start;
        while (index + 1 < input.Length)
        {
            if (input[index] == 63 && input[index + 1] == 61)
            {
                return index;
            }

            if (IsLinearWhitespace(input[index]))
            {
                return -1;
            }

            if (input[index] == 61 && input[index + 1] == 63 && index + 2 < input.Length && input[index + 2] != 61)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    static EmailEncodedWordEncoding GetEncoding(ReadOnlySpan<byte> value)
    {
        if (value.Length != 1)
        {
            return EmailEncodedWordEncoding.None;
        }

        if (value[0] == 66 || value[0] == 98)
        {
            return EmailEncodedWordEncoding.Base64;
        }

        if (value[0] == 81 || value[0] == 113)
        {
            return EmailEncodedWordEncoding.QuotedPrintable;
        }

        return EmailEncodedWordEncoding.None;
    }

    static EmailEncodedWordDecodedValue Decode(ReadOnlySpan<byte> payload, EmailEncodedWordEncoding encoding, long maximumDecodedBytes)
    {
        bool basicInvalid = !HasValidPayloadCharacters(payload);
        if (encoding == EmailEncodedWordEncoding.Base64)
        {
            EmailBase64DecodeResult result = new EmailBase64().Decode(payload, EmailBase64DecodeMode.Strict, maximumDecodedBytes);
            bool invalid = basicInvalid || result.GetIssues().GetCount() > 0;
            return new EmailEncodedWordDecodedValue(result.GetDecodedBytes(), invalid, result.HasFatalError());
        }

        List<byte> output = new List<byte>();
        bool invalidPayload = basicInvalid;
        bool limitReached = false;
        int index = 0;
        while (index < payload.Length)
        {
            byte decoded;
            int consumed = 1;
            if (payload[index] == 95)
            {
                decoded = 32;
            }
            else if (payload[index] == 61)
            {
                if (index + 2 < payload.Length && new EmailByteScanner().IsHexDigit(payload[index + 1]) && new EmailByteScanner().IsHexDigit(payload[index + 2]))
                {
                    decoded = (byte)new EmailByteScanner().DecodeHexPair(payload[index + 1], payload[index + 2]);
                    consumed = 3;
                    if (IsLowerHex(payload[index + 1]) || IsLowerHex(payload[index + 2]))
                    {
                        invalidPayload = true;
                    }
                }
                else
                {
                    decoded = 61;
                    invalidPayload = true;
                }
            }
            else
            {
                decoded = payload[index];
            }

            if (output.Count < maximumDecodedBytes)
            {
                output.Add(decoded);
            }
            else
            {
                limitReached = true;
            }

            index += consumed;
        }

        return new EmailEncodedWordDecodedValue(output.ToArray(), invalidPayload, limitReached);
    }

    static bool HasValidPayloadCharacters(ReadOnlySpan<byte> payload)
    {
        int index = 0;
        while (index < payload.Length)
        {
            if (payload[index] < 33 || payload[index] > 126 || payload[index] == 63)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool HasPayloadContextViolation(ReadOnlySpan<byte> payload, EmailEncodedWordEncoding encoding, EmailEncodedWordContext context)
    {
        if (encoding != EmailEncodedWordEncoding.QuotedPrintable)
        {
            return false;
        }

        int index = 0;
        while (index < payload.Length)
        {
            byte value = payload[index];
            if (context == EmailEncodedWordContext.Comment && (value == 40 || value == 41 || value == 92))
            {
                return true;
            }

            if (context == EmailEncodedWordContext.Phrase && !IsPhraseQCharacter(value))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsPhraseQCharacter(byte value)
    {
        if (value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57)
        {
            return true;
        }

        return value == 33 || value == 42 || value == 43 || value == 45 || value == 47 || value == 61 || value == 95;
    }

    static bool HasLegalSeparation(ReadOnlySpan<byte> input, int start, int count)
    {
        if (start > 0 && !IsLinearWhitespace(input[start - 1]))
        {
            return false;
        }

        int end = start + count;
        if (end < input.Length && !IsLinearWhitespace(input[end]))
        {
            return false;
        }

        return true;
    }

    static bool IsLinearWhitespace(byte value)
    {
        return value == 9 || value == 10 || value == 13 || value == 32;
    }

    static bool IsCharset(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 33 || character > 126 || IsTokenSpecial(character) || character == '*')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsLanguage(string? value, bool present)
    {
        if (!present)
        {
            return true;
        }

        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            char character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsTokenSpecial(char value)
    {
        return value == '(' || value == ')' || value == '<' || value == '>' || value == '@' || value == ',' || value == ';' || value == ':' || value == '\\' || value == '"' || value == '/' || value == '[' || value == ']' || value == '?' || value == '=';
    }

    static int FindCharacter(string value, char character)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static string GetAscii(ReadOnlySpan<byte> value)
    {
        char[] characters = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            characters[index] = (char)value[index];
            index++;
        }

        return new string (characters);
    }

    static bool IsLowerHex(byte value)
    {
        return value >= 97 && value <= 102;
    }

    static void AddMalformedIssue(EmailEncodedWordParseState state, EmailEncodedWordParseMode mode, EmailSourcePosition position, string description, string recovery)
    {
        AddIssue(state, mode, EmailIssueCode.EncodedWordMalformed, description, position, recovery);
    }

    static void AddIssue(EmailEncodedWordParseState state, EmailEncodedWordParseMode mode, EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = EmailIssueSeverity.Error;
        if (mode == EmailEncodedWordParseMode.Tolerant)
        {
            severity = EmailIssueSeverity.Warning;
        }

        state.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }
}
