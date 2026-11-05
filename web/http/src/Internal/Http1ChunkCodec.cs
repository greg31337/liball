namespace http;

internal class Http1ChunkLineResult
{
    internal HttpOperationStatus Status;
    internal long ChunkSize;
    internal int ErrorPosition;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1ChunkCodec
{
    internal static Http1ChunkLineResult ParseChunkLine(byte[] input)
    {
        Http1ChunkLineResult result = new Http1ChunkLineResult();
        if (input == null || input.Length == 0)
        {
            return Reject(
                result,
                0,
                "invalid-http1-chunk-size",
                "An HTTP/1 chunk size is missing.");
        }

        long size = 0;
        int position = 0;
        while (position < input.Length)
        {
            int digit = IntegerParser.GetDigit(input[position]);
            if (digit < 0)
            {
                break;
            }

            if (size > (long.MaxValue - digit) / 16)
            {
                return Reject(
                    result,
                    position,
                    "http1-chunk-size-overflow",
                    "The HTTP/1 chunk size exceeds the supported integer range.");
            }

            size = size * 16 + digit;
            position = position + 1;
        }

        if (position == 0)
        {
            return Reject(
                result,
                0,
                "invalid-http1-chunk-size",
                "An HTTP/1 chunk size must contain hexadecimal digits.");
        }

        if (position < input.Length && input[position] != 0x3b &&
            input[position] != 0x20 && input[position] != 0x09)
        {
            return Reject(
                result,
                position,
                "invalid-http1-chunk-size",
                "An HTTP/1 chunk size contains a non-hexadecimal character.");
        }

        if (!ParseExtensions(input, position))
        {
            return Reject(
                result,
                position,
                "invalid-http1-chunk-extension",
                "An HTTP/1 chunk extension is malformed.");
        }

        result.Status = HttpOperationStatus.Done;
        result.ChunkSize = size;
        return result;
    }

    static bool ParseExtensions(byte[] input, int start)
    {
        int position = start;
        while (position < input.Length)
        {
            position = SkipBadWhitespace(input, position);
            if (position == input.Length)
            {
                return false;
            }

            if (input[position] != 0x3b)
            {
                return false;
            }

            position = SkipBadWhitespace(input, position + 1);
            int nameStart = position;
            position = ReadTokenEnd(input, position);
            if (position == nameStart)
            {
                return false;
            }

            int equalsPosition = SkipBadWhitespace(input, position);
            if (equalsPosition < input.Length && input[equalsPosition] == 0x3d)
            {
                position = SkipBadWhitespace(input, equalsPosition + 1);
                if (position == input.Length)
                {
                    return false;
                }

                if (input[position] == 0x22)
                {
                    position = ReadQuotedStringEnd(input, position);
                    if (position < 0)
                    {
                        return false;
                    }
                }
                else
                {
                    int valueStart = position;
                    position = ReadTokenEnd(input, position);
                    if (position == valueStart)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    static int SkipBadWhitespace(byte[] input, int start)
    {
        int position = start;
        while (position < input.Length &&
            (input[position] == 0x20 || input[position] == 0x09))
        {
            position = position + 1;
        }

        return position;
    }

    static int ReadTokenEnd(byte[] input, int start)
    {
        int position = start;
        while (position < input.Length && HttpGrammar.IsTokenCharacter(input[position]))
        {
            position = position + 1;
        }

        return position;
    }

    static int ReadQuotedStringEnd(byte[] input, int start)
    {
        int position = start + 1;
        while (position < input.Length)
        {
            byte current = input[position];
            if (current == 0x22)
            {
                return position + 1;
            }

            if (current == 0x5c)
            {
                position = position + 1;
                if (position == input.Length || !IsQuotedPairCharacter(input[position]))
                {
                    return -1;
                }
            }
            else if (!IsQuotedText(current))
            {
                return -1;
            }

            position = position + 1;
        }

        return -1;
    }

    static bool IsQuotedText(byte value)
    {
        return value == 0x09 || value == 0x20 || value == 0x21 ||
            value >= 0x23 && value <= 0x5b ||
            value >= 0x5d && value <= 0x7e || value >= 0x80;
    }

    static bool IsQuotedPairCharacter(byte value)
    {
        return value == 0x09 || value == 0x20 ||
            value >= 0x21 && value <= 0x7e || value >= 0x80;
    }

    static Http1ChunkLineResult Reject(
        Http1ChunkLineResult result,
        int position,
        string code,
        string message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = position;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
