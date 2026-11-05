namespace json;

internal class Utf8Decoder
{
    internal const int NeedMoreData = -1;

    byte[] pendingBytes = new byte[4];
    int pendingByteCount;
    int expectedByteCount;
    int scalar;
    int minimumScalar;

    internal int PushByte(byte value)
    {
        if (expectedByteCount == 0)
        {
            if (value <= 0x7F)
            {
                return value;
            }

            pendingBytes[0] = value;
            pendingByteCount = 1;
            if (value >= 0xC2 && value <= 0xDF)
            {
                expectedByteCount = 2;
                scalar = value & 0x1F;
                minimumScalar = 0x80;
                return NeedMoreData;
            }

            if (value >= 0xE0 && value <= 0xEF)
            {
                expectedByteCount = 3;
                scalar = value & 0x0F;
                minimumScalar = 0x800;
                return NeedMoreData;
            }

            if (value >= 0xF0 && value <= 0xF4)
            {
                expectedByteCount = 4;
                scalar = value & 0x07;
                minimumScalar = 0x10000;
                return NeedMoreData;
            }

            ResetDecoder();
            throw new JsonParseException(CreateUtf8Diagnostic("The byte is not a valid UTF-8 leading byte."));
        }

        if (value < 0x80 || value > 0xBF)
        {
            ResetDecoder();
            throw new JsonParseException(CreateUtf8Diagnostic("A UTF-8 continuation byte was required."));
        }

        pendingBytes[pendingByteCount] = value;
        pendingByteCount++;
        scalar = (scalar << 6) | (value & 0x3F);
        if (pendingByteCount < expectedByteCount)
        {
            return NeedMoreData;
        }

        int decodedScalar = scalar;
        int decodedMinimum = minimumScalar;
        ResetDecoder();
        if (decodedScalar < decodedMinimum)
        {
            throw new JsonParseException(CreateUtf8Diagnostic("The UTF-8 sequence is overlong."));
        }

        if (decodedScalar >= 0xD800 && decodedScalar <= 0xDFFF)
        {
            throw new JsonParseException(CreateUtf8Diagnostic("UTF-8 cannot encode a surrogate code point."));
        }

        if (decodedScalar > 0x10FFFF)
        {
            throw new JsonParseException(CreateUtf8Diagnostic("The UTF-8 sequence is above U+10FFFF."));
        }

        return decodedScalar;
    }

    internal void CompleteDecoder()
    {
        if (pendingByteCount != 0)
        {
            ResetDecoder();
            throw new JsonParseException(CreateUtf8Diagnostic("The UTF-8 sequence is truncated."));
        }
    }

    internal bool HasPendingBytes()
    {
        return pendingByteCount != 0;
    }

    internal int GetPendingByteCount()
    {
        return pendingByteCount;
    }

    internal byte[] CopyPendingBytes()
    {
        byte[] result = new byte[pendingByteCount];
        if (pendingByteCount > 0)
        {
            global::System.Buffer.BlockCopy(pendingBytes, 0, result, 0, pendingByteCount);
        }

        return result;
    }

    internal void ResetDecoder()
    {
        pendingByteCount = 0;
        expectedByteCount = 0;
        scalar = 0;
        minimumScalar = 0;
    }

    internal static JsonDiagnostic CreateUtf8Diagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidUtf8;
        diagnostic.Message = message;
        return diagnostic;
    }
}
