namespace json;

internal class JsonStringScanner
{
    byte[] rawBytes = new byte[32];
    Utf8Decoder utf8Decoder = new Utf8Decoder();
    int maximumDiagnosticExcerptBytes =
        JsonDefaults.MaximumDiagnosticExcerptBytes;
    int rawByteCount;
    int escapeByteCount;
    bool started;
    bool complete;

    internal bool PushByte(byte value)
    {
        if (complete)
        {
            throw new JsonParseException(CreateStringDiagnostic(JsonDiagnosticCode.InvalidStringCharacter, "The JSON string is already complete."));
        }

        AppendRawByte(value);
        if (!started)
        {
            if (value != 0x22)
            {
                JsonDiagnostic diagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidStringCharacter, "A JSON string must begin with a quotation mark.");
                ResetScanner();
                throw new JsonParseException(diagnostic);
            }

            started = true;
            return false;
        }

        if (utf8Decoder.HasPendingBytes())
        {
            utf8Decoder.PushByte(value);
            return false;
        }

        if (escapeByteCount > 0)
        {
            ConsumeEscapeByte(value);
            return false;
        }

        if (value == 0x22)
        {
            complete = true;
            return true;
        }

        if (value == 0x5C)
        {
            escapeByteCount = 1;
            return false;
        }

        if (value < 0x20)
        {
            JsonDiagnostic diagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidStringCharacter, "A JSON string cannot contain an unescaped control character.");
            ResetScanner();
            throw new JsonParseException(diagnostic);
        }

        if (value >= 0x80)
        {
            utf8Decoder.PushByte(value);
        }

        return false;
    }

    internal void SetMaximumDiagnosticExcerptBytes(int maximumBytes)
    {
        maximumDiagnosticExcerptBytes = maximumBytes;
    }

    internal void CompleteString()
    {
        if (!started || complete)
        {
            return;
        }

        if (utf8Decoder.HasPendingBytes())
        {
            utf8Decoder.CompleteDecoder();
        }

        if (escapeByteCount == 1)
        {
            JsonDiagnostic escapeDiagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidEscape, "The JSON escape is incomplete.");
            ResetScanner();
            throw new JsonParseException(escapeDiagnostic);
        }

        if (escapeByteCount > 1)
        {
            JsonDiagnostic unicodeDiagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidUnicodeEscape, "The JSON Unicode escape requires four hexadecimal digits.");
            ResetScanner();
            throw new JsonParseException(unicodeDiagnostic);
        }

        JsonDiagnostic diagnostic = CreateStringDiagnostic(JsonDiagnosticCode.UnexpectedEndOfInput, "The JSON string is missing its closing quotation mark.");
        ResetScanner();
        throw new JsonParseException(diagnostic);
    }

    internal bool HasStarted()
    {
        return started;
    }

    internal bool HasPendingBytes()
    {
        return started && !complete;
    }

    internal bool HasPendingEscapeBytes()
    {
        return escapeByteCount > 0;
    }

    internal bool HasPendingUtf8Bytes()
    {
        return utf8Decoder.HasPendingBytes();
    }

    internal bool IsComplete()
    {
        return complete;
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

    internal byte[] CopyRawContentBytes()
    {
        if (!started)
        {
            return new byte[0];
        }

        int contentLength = rawByteCount - 1;
        if (complete)
        {
            contentLength--;
        }

        byte[] result = new byte[contentLength];
        if (contentLength > 0)
        {
            global::System.Buffer.BlockCopy(rawBytes, 1, result, 0, contentLength);
        }

        return result;
    }

    internal void ResetScanner()
    {
        rawByteCount = 0;
        escapeByteCount = 0;
        started = false;
        complete = false;
        utf8Decoder.ResetDecoder();
    }

    void ConsumeEscapeByte(byte value)
    {
        if (escapeByteCount == 1)
        {
            if (value == 0x75)
            {
                escapeByteCount = 5;
                return;
            }

            if (value == 0x22 || value == 0x5C || value == 0x2F || value == 0x62 ||
                value == 0x66 || value == 0x6E || value == 0x72 || value == 0x74)
            {
                escapeByteCount = 0;
                return;
            }

            JsonDiagnostic diagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidEscape, "The JSON escape name is invalid.");
            ResetScanner();
            throw new JsonParseException(diagnostic);
        }

        if (!IsHexadecimalByte(value))
        {
            JsonDiagnostic diagnostic = CreateStringDiagnostic(JsonDiagnosticCode.InvalidUnicodeEscape, "A JSON Unicode escape contains a non-hexadecimal byte.");
            ResetScanner();
            throw new JsonParseException(diagnostic);
        }

        escapeByteCount--;
        if (escapeByteCount == 1)
        {
            escapeByteCount = 0;
        }
    }

    bool IsHexadecimalByte(byte value)
    {
        if (value >= 0x30 && value <= 0x39)
        {
            return true;
        }

        if (value >= 0x41 && value <= 0x46)
        {
            return true;
        }

        return value >= 0x61 && value <= 0x66;
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

    JsonDiagnostic CreateStringDiagnostic(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = CopyDiagnosticBytes();
        return diagnostic;
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
