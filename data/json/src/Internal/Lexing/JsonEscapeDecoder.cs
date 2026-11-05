namespace json;

internal class JsonEscapeDecoder
{
    byte[] rawBytes = new byte[16];
    int[] decodedValues = new int[16];
    JsonUnicodePolicy unicodePolicy;
    int maximumDiagnosticExcerptBytes =
        JsonDefaults.MaximumDiagnosticExcerptBytes;
    int rawByteCount;
    int decodedValueCount;
    int escapeState;
    int hexadecimalDigitCount;
    int unicodeValue;
    int pendingHighSurrogate;

    internal JsonEscapeDecoder(JsonUnicodePolicy unicodePolicy)
    {
        this.unicodePolicy = unicodePolicy;
        if (unicodePolicy != JsonUnicodePolicy.SyntaxCompatible &&
            unicodePolicy != JsonUnicodePolicy.UnicodeScalar)
        {
            throw CreateDecoderFailure(JsonDiagnosticCode.InvalidOptions, "The Unicode policy is invalid.");
        }
    }

    internal bool PushByte(byte value)
    {
        AppendRawByte(value);
        if (escapeState == 0)
        {
            if (value != 0x5C)
            {
                throw CreateDecoderFailure(JsonDiagnosticCode.InvalidEscape, "A JSON escape must begin with a reverse solidus.");
            }

            escapeState = 1;
            return false;
        }

        if (escapeState == 1)
        {
            if (value == 0x75)
            {
                escapeState = 2;
                hexadecimalDigitCount = 0;
                unicodeValue = 0;
                return false;
            }

            int decodedValue = DecodeSimpleEscape(value);
            if (decodedValue < 0)
            {
                throw CreateDecoderFailure(JsonDiagnosticCode.InvalidEscape, "The JSON escape name is invalid.");
            }

            ResolvePendingHighSurrogate();
            AppendDecodedValue(decodedValue);
            escapeState = 0;
            return true;
        }

        int hexadecimalValue = DecodeHexadecimalByte(value);
        if (hexadecimalValue < 0)
        {
            throw CreateDecoderFailure(JsonDiagnosticCode.InvalidUnicodeEscape, "A JSON Unicode escape contains a non-hexadecimal byte.");
        }

        unicodeValue = (unicodeValue << 4) | hexadecimalValue;
        hexadecimalDigitCount++;
        if (hexadecimalDigitCount < 4)
        {
            return false;
        }

        escapeState = 0;
        ConsumeUnicodeValue();
        return true;
    }

    internal void SetMaximumDiagnosticExcerptBytes(int maximumBytes)
    {
        maximumDiagnosticExcerptBytes = maximumBytes;
    }

    internal void CompleteDecoder()
    {
        if (escapeState == 1)
        {
            throw CreateDecoderFailure(JsonDiagnosticCode.InvalidEscape, "The JSON escape is incomplete.");
        }

        if (escapeState == 2)
        {
            throw CreateDecoderFailure(JsonDiagnosticCode.InvalidUnicodeEscape, "The JSON Unicode escape requires four hexadecimal digits.");
        }

        ResolvePendingHighSurrogate();
    }

    internal bool HasPendingEscapeBytes()
    {
        return escapeState != 0;
    }

    internal bool HasPendingHighSurrogate()
    {
        return pendingHighSurrogate != 0;
    }

    internal int GetRawByteCount()
    {
        return rawByteCount;
    }

    internal byte[] CopyRawBytes()
    {
        byte[] result = new byte[rawByteCount];
        if (rawByteCount > 0)
        {
            global::System.Buffer.BlockCopy(rawBytes, 0, result, 0, rawByteCount);
        }

        return result;
    }

    internal int GetDecodedValueCount()
    {
        return decodedValueCount;
    }

    internal int GetDecodedValueAt(int index)
    {
        if (index < 0 || index >= decodedValueCount)
        {
            throw new global::System.ArgumentOutOfRangeException("index");
        }

        return decodedValues[index];
    }

    internal int[] CopyDecodedValues()
    {
        int[] result = new int[decodedValueCount];
        if (decodedValueCount > 0)
        {
            global::System.Array.Copy(decodedValues, 0, result, 0, decodedValueCount);
        }

        return result;
    }

    internal void ResetDecoder()
    {
        rawByteCount = 0;
        decodedValueCount = 0;
        escapeState = 0;
        hexadecimalDigitCount = 0;
        unicodeValue = 0;
        pendingHighSurrogate = 0;
    }

    void ConsumeUnicodeValue()
    {
        int value = unicodeValue;
        unicodeValue = 0;
        hexadecimalDigitCount = 0;

        if (value >= 0xD800 && value <= 0xDBFF)
        {
            if (pendingHighSurrogate != 0)
            {
                ResolvePendingHighSurrogate();
            }

            pendingHighSurrogate = value;
            return;
        }

        if (value >= 0xDC00 && value <= 0xDFFF)
        {
            if (pendingHighSurrogate == 0)
            {
                if (unicodePolicy == JsonUnicodePolicy.UnicodeScalar)
                {
                    throw CreateDecoderFailure(JsonDiagnosticCode.InvalidSurrogatePair, "A low surrogate has no preceding high surrogate.");
                }

                AppendDecodedValue(value);
                return;
            }

            int scalar = 0x10000 + ((pendingHighSurrogate - 0xD800) << 10) + (value - 0xDC00);
            pendingHighSurrogate = 0;
            AppendDecodedValue(scalar);
            return;
        }

        ResolvePendingHighSurrogate();
        AppendDecodedValue(value);
    }

    void ResolvePendingHighSurrogate()
    {
        if (pendingHighSurrogate == 0)
        {
            return;
        }

        if (unicodePolicy == JsonUnicodePolicy.UnicodeScalar)
        {
            throw CreateDecoderFailure(JsonDiagnosticCode.InvalidSurrogatePair, "A high surrogate is not followed by a low surrogate.");
        }

        int value = pendingHighSurrogate;
        pendingHighSurrogate = 0;
        AppendDecodedValue(value);
    }

    int DecodeSimpleEscape(byte value)
    {
        if (value == 0x22 || value == 0x5C || value == 0x2F)
        {
            return value;
        }

        if (value == 0x62)
        {
            return 0x08;
        }

        if (value == 0x66)
        {
            return 0x0C;
        }

        if (value == 0x6E)
        {
            return 0x0A;
        }

        if (value == 0x72)
        {
            return 0x0D;
        }

        if (value == 0x74)
        {
            return 0x09;
        }

        return -1;
    }

    int DecodeHexadecimalByte(byte value)
    {
        if (value >= 0x30 && value <= 0x39)
        {
            return value - 0x30;
        }

        if (value >= 0x41 && value <= 0x46)
        {
            return value - 0x41 + 10;
        }

        if (value >= 0x61 && value <= 0x66)
        {
            return value - 0x61 + 10;
        }

        return -1;
    }

    void AppendRawByte(byte value)
    {
        if (rawByteCount == rawBytes.Length)
        {
            int newCapacity = rawBytes.Length * 2;
            byte[] replacement = new byte[newCapacity];
            global::System.Buffer.BlockCopy(rawBytes, 0, replacement, 0, rawByteCount);
            rawBytes = replacement;
        }

        rawBytes[rawByteCount] = value;
        rawByteCount++;
    }

    void AppendDecodedValue(int value)
    {
        if (decodedValueCount == decodedValues.Length)
        {
            int newCapacity = decodedValues.Length * 2;
            int[] replacement = new int[newCapacity];
            global::System.Array.Copy(decodedValues, 0, replacement, 0, decodedValueCount);
            decodedValues = replacement;
        }

        decodedValues[decodedValueCount] = value;
        decodedValueCount++;
    }

    JsonParseException CreateDecoderFailure(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = CopyDiagnosticBytes();
        ResetDecoder();
        return new JsonParseException(diagnostic);
    }

    byte[] CopyDiagnosticBytes()
    {
        int length = rawByteCount < maximumDiagnosticExcerptBytes
            ? rawByteCount
            : maximumDiagnosticExcerptBytes;
        byte[] result = new byte[length];
        if (length > 0)
        {
            global::System.Buffer.BlockCopy(
                rawBytes,
                rawByteCount - length,
                result,
                0,
                length);
        }

        return result;
    }
}
