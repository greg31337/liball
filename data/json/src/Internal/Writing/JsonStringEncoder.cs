namespace json;

internal class JsonStringEncoder
{
    JsonStringEscapeMode escapeMode;
    JsonUnicodePolicy unicodePolicy;
    bool escapeSolidus;
    bool uppercaseHexadecimal;
    int maximumDecodedStringBytes;
    int maximumDecodedStringCodeUnits;
    int maximumOutputBytes;
    byte[] output;
    int outputCount;

    internal JsonStringEncoder(JsonWriterOptions options)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        escapeMode = normalized.StringEscapeMode;
        unicodePolicy = normalized.UnicodePolicy;
        escapeSolidus = normalized.EscapeSolidus;
        uppercaseHexadecimal = normalized.UseUppercaseHexadecimalEscapes;
        maximumDecodedStringBytes = limits.MaximumDecodedStringBytes;
        maximumDecodedStringCodeUnits =
            limits.MaximumDecodedStringCodeUnits;
        maximumOutputBytes = limits.MaximumOutputBytes;
        int initialCapacity = 64;
        if (initialCapacity > maximumOutputBytes)
        {
            initialCapacity = maximumOutputBytes;
        }

        output = new byte[initialCapacity];
    }

    internal byte[] EncodeString(string value)
    {
        return EncodeValue(value, false);
    }

    internal byte[] EncodeJcsString(string value)
    {
        return EncodeValue(value, true);
    }

    byte[] EncodeValue(string value, bool useJcsRules)
    {
        ValidateInput(value, useJcsRules);
        outputCount = 0;
        AppendByte(JsonSyntaxBytes.StringDelimiter);

        int index = 0;
        while (index < value.Length)
        {
            int codeUnit = value[index];
            if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
            {
                if (index + 1 < value.Length)
                {
                    int lowSurrogate = value[index + 1];
                    if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
                    {
                        if (!useJcsRules &&
                            escapeMode == JsonStringEscapeMode.AsciiOnly)
                        {
                            AppendUnicodeEscape(
                                codeUnit,
                                uppercaseHexadecimal);
                            AppendUnicodeEscape(
                                lowSurrogate,
                                uppercaseHexadecimal);
                        }
                        else
                        {
                            int scalar = 0x10000 +
                                ((codeUnit - 0xD800) << 10) +
                                (lowSurrogate - 0xDC00);
                            AppendUtf8Scalar(scalar);
                        }

                        index += 2;
                        continue;
                    }
                }

                AppendUnicodeEscape(codeUnit, uppercaseHexadecimal);
                index++;
                continue;
            }

            if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
            {
                AppendUnicodeEscape(codeUnit, uppercaseHexadecimal);
                index++;
                continue;
            }

            AppendCodeUnit(codeUnit, useJcsRules);
            index++;
        }

        AppendByte(JsonSyntaxBytes.StringDelimiter);
        byte[] encoded = new byte[outputCount];
        global::System.Buffer.BlockCopy(
            output,
            0,
            encoded,
            0,
            outputCount);
        return encoded;
    }

    void ValidateInput(string? value, bool useJcsRules)
    {
        if (value == null)
        {
            throw CreateWriteFailure(
                JsonDiagnosticCode.InvalidModel,
                "A decoded string value is required for JSON encoding.");
        }

        if (value.Length > maximumDecodedStringCodeUnits)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded,
                "The configured decoded string UTF-16 code-unit limit was exceeded.");
        }

        long utf8ByteCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            int codeUnit = value[index];
            if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
            {
                if (index + 1 < value.Length)
                {
                    int lowSurrogate = value[index + 1];
                    if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
                    {
                        utf8ByteCount += 4;
                        index += 2;
                        continue;
                    }
                }

                if (useJcsRules ||
                    unicodePolicy == JsonUnicodePolicy.UnicodeScalar)
                {
                    throw CreateWriteFailure(
                        JsonDiagnosticCode.InvalidSurrogatePair,
                        "A high surrogate requires a following low surrogate.");
                }

                utf8ByteCount += 3;
                index++;
                continue;
            }

            if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
            {
                if (useJcsRules ||
                    unicodePolicy == JsonUnicodePolicy.UnicodeScalar)
                {
                    throw CreateWriteFailure(
                        JsonDiagnosticCode.InvalidSurrogatePair,
                        "A low surrogate requires a preceding high surrogate.");
                }

                utf8ByteCount += 3;
                index++;
                continue;
            }

            if (codeUnit <= 0x7F)
            {
                utf8ByteCount++;
            }
            else if (codeUnit <= 0x7FF)
            {
                utf8ByteCount += 2;
            }
            else
            {
                utf8ByteCount += 3;
            }

            if (utf8ByteCount > maximumDecodedStringBytes)
            {
                throw CreateLimitFailure(
                    JsonDiagnosticCode.DecodedStringByteLimitExceeded,
                    "The configured decoded string UTF-8 byte limit was exceeded.");
            }

            index++;
        }

        if (utf8ByteCount > maximumDecodedStringBytes)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.DecodedStringByteLimitExceeded,
                "The configured decoded string UTF-8 byte limit was exceeded.");
        }
    }

    void AppendCodeUnit(int value, bool useJcsRules)
    {
        if (value == 0x22)
        {
            AppendSimpleEscape(0x22);
            return;
        }

        if (value == 0x5C)
        {
            AppendSimpleEscape(0x5C);
            return;
        }

        if (value == 0x2F && escapeSolidus && !useJcsRules)
        {
            AppendSimpleEscape(0x2F);
            return;
        }

        if (value == 0x08)
        {
            AppendSimpleEscape(0x62);
            return;
        }

        if (value == 0x09)
        {
            AppendSimpleEscape(0x74);
            return;
        }

        if (value == 0x0A)
        {
            AppendSimpleEscape(0x6E);
            return;
        }

        if (value == 0x0C)
        {
            AppendSimpleEscape(0x66);
            return;
        }

        if (value == 0x0D)
        {
            AppendSimpleEscape(0x72);
            return;
        }

        if (value < 0x20)
        {
            AppendUnicodeEscape(value, useJcsRules
                ? false
                : uppercaseHexadecimal);
            return;
        }

        if (!useJcsRules && escapeMode == JsonStringEscapeMode.HtmlSafe &&
            IsHtmlSensitive(value))
        {
            AppendUnicodeEscape(value, uppercaseHexadecimal);
            return;
        }

        if (!useJcsRules && escapeMode == JsonStringEscapeMode.AsciiOnly &&
            value > 0x7F)
        {
            AppendUnicodeEscape(value, uppercaseHexadecimal);
            return;
        }

        AppendUtf8Scalar(value);
    }

    bool IsHtmlSensitive(int value)
    {
        return value == 0x26 ||
            value == 0x27 ||
            value == 0x3C ||
            value == 0x3D ||
            value == 0x3E ||
            value == 0x2028 ||
            value == 0x2029;
    }

    void AppendSimpleEscape(byte escapeName)
    {
        EnsureOutputCapacity(2);
        output[outputCount] = JsonSyntaxBytes.Escape;
        output[outputCount + 1] = escapeName;
        outputCount += 2;
    }

    void AppendUnicodeEscape(int value, bool uppercase)
    {
        EnsureOutputCapacity(6);
        output[outputCount] = JsonSyntaxBytes.Escape;
        output[outputCount + 1] = 0x75;
        output[outputCount + 2] = GetHexadecimalByte(
            (value >> 12) & 0x0F,
            uppercase);
        output[outputCount + 3] = GetHexadecimalByte(
            (value >> 8) & 0x0F,
            uppercase);
        output[outputCount + 4] = GetHexadecimalByte(
            (value >> 4) & 0x0F,
            uppercase);
        output[outputCount + 5] = GetHexadecimalByte(
            value & 0x0F,
            uppercase);
        outputCount += 6;
    }

    byte GetHexadecimalByte(int value, bool uppercase)
    {
        if (value < 10)
        {
            return (byte)(0x30 + value);
        }

        return (byte)((uppercase ? 0x41 : 0x61) + value - 10);
    }

    void AppendUtf8Scalar(int value)
    {
        if (value <= 0x7F)
        {
            AppendByte((byte)value);
            return;
        }

        if (value <= 0x7FF)
        {
            EnsureOutputCapacity(2);
            output[outputCount] = (byte)(0xC0 | (value >> 6));
            output[outputCount + 1] = (byte)(0x80 | (value & 0x3F));
            outputCount += 2;
            return;
        }

        if (value <= 0xFFFF)
        {
            EnsureOutputCapacity(3);
            output[outputCount] = (byte)(0xE0 | (value >> 12));
            output[outputCount + 1] =
                (byte)(0x80 | ((value >> 6) & 0x3F));
            output[outputCount + 2] = (byte)(0x80 | (value & 0x3F));
            outputCount += 3;
            return;
        }

        EnsureOutputCapacity(4);
        output[outputCount] = (byte)(0xF0 | (value >> 18));
        output[outputCount + 1] =
            (byte)(0x80 | ((value >> 12) & 0x3F));
        output[outputCount + 2] =
            (byte)(0x80 | ((value >> 6) & 0x3F));
        output[outputCount + 3] = (byte)(0x80 | (value & 0x3F));
        outputCount += 4;
    }

    void AppendByte(byte value)
    {
        EnsureOutputCapacity(1);
        output[outputCount] = value;
        outputCount++;
    }

    void EnsureOutputCapacity(int appendLength)
    {
        if (appendLength > maximumOutputBytes - outputCount)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.OutputByteLimitExceeded,
                "The configured output byte limit was exceeded.");
        }

        int required = outputCount + appendLength;
        if (required <= output.Length)
        {
            return;
        }

        int newCapacity = output.Length;
        if (newCapacity == 0)
        {
            newCapacity = 1;
        }

        while (newCapacity < required)
        {
            if (newCapacity > maximumOutputBytes / 2)
            {
                newCapacity = maximumOutputBytes;
            }
            else
            {
                newCapacity *= 2;
            }
        }

        byte[] replacement = new byte[newCapacity];
        global::System.Buffer.BlockCopy(
            output,
            0,
            replacement,
            0,
            outputCount);
        output = replacement;
    }

    JsonWriteException CreateWriteFailure(
        JsonDiagnosticCode code,
        string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = "String";
        return new JsonWriteException(diagnostic);
    }

    JsonLimitException CreateLimitFailure(
        JsonDiagnosticCode code,
        string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = "String";
        return new JsonLimitException(diagnostic);
    }
}
