using System;

namespace sdp;

internal static class SdpEncryptionKeyLineParser
{
    internal static void ParseEncryptionKey(SdpParseContext context, SdpRawLine line)
    {
        int occurrenceIndex;
        if (context.Scope == SdpScope.Media)
        {
            occurrenceIndex = context.MediaEncryptionKeyCount;
            context.MediaEncryptionKeyCount = context.MediaEncryptionKeyCount + 1;
        }
        else
        {
            occurrenceIndex = context.SessionEncryptionKeyCount;
            context.SessionEncryptionKeyCount = context.SessionEncryptionKeyCount + 1;
        }

        if (occurrenceIndex > 0)
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, "An SDP scope may contain only one encryption-key field.", 0);
            return;
        }

        ReadOnlySpan<char> rawText = line.Text.AsSpan().Slice(2);
        int colonOffset = rawText.IndexOf(':');
        int methodLength = colonOffset < 0 ? rawText.Length : colonOffset;
        ReadOnlySpan<char> methodText = rawText.Slice(0, methodLength);
        if (IsEncryptionKeyComponentTooLong(context, methodText))
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "The SDP encryption-key method exceeds the configured token-length limit.", 0);
            context.Stop = true;
            return;
        }

        bool hasValue = colonOffset >= 0;
        ReadOnlySpan<char> valueText = hasValue ? rawText.Slice(colonOffset + 1) : ReadOnlySpan<char>.Empty;
        string method = methodText.ToString();
        if ((method == "base64" || method == "uri") && IsEncryptionKeyComponentTooLong(context, valueText))
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "The SDP encryption-key value exceeds the configured token-length limit.", colonOffset + 1);
            context.Stop = true;
            return;
        }

        SdpEncryptionKey encryptionKey = new SdpEncryptionKey();
        encryptionKey.Method = method;
        encryptionKey.HasValue = hasValue;
        encryptionKey.Value = valueText.ToString();
        encryptionKey.RawText = rawText.ToString();
        encryptionKey.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        if (context.Scope == SdpScope.Media)
        {
            if (context.CurrentMedia != null)
            {
                context.CurrentMedia.EncryptionKey = encryptionKey;
            }
        }
        else
        {
            context.Description.EncryptionKey = encryptionKey;
        }

        if (context.Options.Mode == SdpParseMode.Strict)
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationObsoleteSyntax, SdpValidationSeverity.Warning, "The obsolete SDP encryption-key field should not be used.", 0);
        }

        if (!ValidateEncryptionKeyMethod(context, line, occurrenceIndex, method))
        {
            return;
        }
        if (method == "prompt")
        {
            if (hasValue)
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The prompt encryption-key method must not contain a key value.", colonOffset);
            }
            return;
        }
        if (method == "clear")
        {
            if (!hasValue || valueText.Length == 0)
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The clear encryption-key method requires a nonempty key value.", rawText.Length);
            }
            return;
        }
        if (method == "base64")
        {
            if (!hasValue)
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The base64 encryption-key method requires a colon-delimited value.", rawText.Length);
                return;
            }
            ValidateBase64Value(context, line, occurrenceIndex, valueText, colonOffset + 1);
            return;
        }
        if (method == "uri")
        {
            if (!hasValue)
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The URI encryption-key method requires a colon-delimited value.", rawText.Length);
                return;
            }
            ValidateUriValue(context, line, occurrenceIndex, valueText, colonOffset + 1);
        }
    }

    internal static bool ValidateEncryptionKeyMethod(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string method)
    {
        if (method.Length == 0)
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP encryption-key method must not be empty.", 0);
            return false;
        }
        int index = 0;
        while (index < method.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(method[index]))
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP encryption-key method contains a character outside the token grammar.", index);
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void ValidateBase64Value(SdpParseContext context, SdpRawLine line, int occurrenceIndex, ReadOnlySpan<char> value, int valueOffset)
    {
        if (value.Length % 4 != 0)
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP base64 encryption key has invalid group or padding length.", valueOffset + value.Length);
            return;
        }

        int paddingLength = 0;
        if (value.Length > 0 && value[value.Length - 1] == '=')
        {
            paddingLength = 1;
        }
        if (value.Length > 1 && value[value.Length - 2] == '=')
        {
            paddingLength = 2;
        }
        int dataLength = value.Length - paddingLength;
        int index = 0;
        while (index < dataLength)
        {
            if (!IsBase64Character(value[index]))
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP base64 encryption key contains an invalid character.", valueOffset + index);
                return;
            }
            index = index + 1;
        }
        if (paddingLength == 1 && dataLength % 4 != 3 || paddingLength == 2 && dataLength % 4 != 2)
        {
            AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP base64 encryption key has invalid padding.", valueOffset + dataLength);
        }
    }

    internal static bool IsBase64Character(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '+' || value == '/';
    }

    internal static void ValidateUriValue(SdpParseContext context, SdpRawLine line, int occurrenceIndex, ReadOnlySpan<char> value, int valueOffset)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || !SdpSessionLineParser.IsUriHexDigit(value[index + 1]) || !SdpSessionLineParser.IsUriHexDigit(value[index + 2]))
                {
                    AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP encryption-key URI contains an invalid percent-encoded octet.", valueOffset + index);
                    return;
                }
                index = index + 3;
                continue;
            }
            if (!SdpSessionLineParser.IsUriReferenceCharacter(character))
            {
                AddEncryptionKeyDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP encryption-key URI contains a character outside the URI-reference grammar.", valueOffset + index);
                return;
            }
            index = index + 1;
        }
    }

    internal static bool IsEncryptionKeyComponentTooLong(SdpParseContext context, ReadOnlySpan<char> value)
    {
        if (context.InputIsUtf8)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(value);
            return byteLength > context.Options.MaximumTokenLength;
        }
        return value.Length > context.Options.MaximumTokenLength;
    }

    internal static void AddEncryptionKeyDiagnostic(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string code, SdpValidationSeverity severity, string detail, int relativeOffset)
    {
        SdpSourceLocation location = CreateEncryptionKeyValueLocation(line, relativeOffset);
        context.AddDiagnostic(code, severity, context.Scope, 'k', context.MediaIndex, occurrenceIndex, detail, location);
    }

    internal static SdpSourceLocation CreateEncryptionKeyValueLocation(SdpRawLine line, int relativeOffset)
    {
        SdpSourceLocation result = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        result.CharacterOffset = result.CharacterOffset + 2 + relativeOffset;
        if (result.ByteOffset >= 0)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(line.Text.AsSpan(0, 2 + relativeOffset));
            result.ByteOffset = result.ByteOffset + byteLength;
        }
        result.ColumnNumber = 3 + relativeOffset;
        return result;
    }
}
