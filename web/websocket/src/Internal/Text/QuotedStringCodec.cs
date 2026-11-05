using System;

namespace websocket;

internal class QuotedStringReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int ConsumedCount;
    internal string Value = "";
}

internal class QuotedStringWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int ProducedCount;
}

internal static class QuotedStringCodec
{
    internal static QuotedStringReadResult Read(ReadOnlySpan<char> source, int offset)
    {
        QuotedStringReadResult result = new QuotedStringReadResult();
        if (offset < 0 || offset > source.Length)
        {
            return result;
        }

        if (offset == source.Length)
        {
            result.Status = WebSocketParseStatus.NeedInput;
            return result;
        }

        if (source[offset] != '"')
        {
            return result;
        }

        int decodedLength = MeasureDecodedLength(source, offset);
        if (decodedLength == -2)
        {
            result.Status = WebSocketParseStatus.NeedInput;
            return result;
        }

        if (decodedLength < 0)
        {
            return result;
        }

        char[] decoded = new char[decodedLength];
        int sourceIndex = offset + 1;
        int destinationIndex = 0;
        while (source[sourceIndex] != '"')
        {
            char character = source[sourceIndex];
            if (character == '\\')
            {
                sourceIndex = sourceIndex + 1;
                character = source[sourceIndex];
            }

            decoded[destinationIndex] = character;
            destinationIndex = destinationIndex + 1;
            sourceIndex = sourceIndex + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = sourceIndex - offset + 1;
        result.Value = new string(decoded);
        return result;
    }

    internal static int MeasureEncodedLength(ReadOnlySpan<char> value)
    {
        int encodedLength = 2;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsQuotedPairCharacter(character))
            {
                return -1;
            }

            int characterLength = 1;
            if (character == '"' || character == '\\')
            {
                characterLength = 2;
            }

            if (encodedLength > int.MaxValue - characterLength)
            {
                return -1;
            }

            encodedLength = encodedLength + characterLength;
            index = index + 1;
        }

        return encodedLength;
    }

    internal static QuotedStringWriteResult Write(
        ReadOnlySpan<char> value,
        Span<char> destination)
    {
        QuotedStringWriteResult result = new QuotedStringWriteResult();
        int encodedLength = MeasureEncodedLength(value);
        if (encodedLength < 0)
        {
            return result;
        }

        if (destination.Length < encodedLength)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        destination[destinationIndex] = '"';
        destinationIndex = destinationIndex + 1;
        while (sourceIndex < value.Length)
        {
            char character = value[sourceIndex];
            if (character == '"' || character == '\\')
            {
                destination[destinationIndex] = '\\';
                destinationIndex = destinationIndex + 1;
            }

            destination[destinationIndex] = character;
            destinationIndex = destinationIndex + 1;
            sourceIndex = sourceIndex + 1;
        }

        destination[destinationIndex] = '"';
        result.Status = WebSocketParseStatus.Complete;
        result.ProducedCount = encodedLength;
        return result;
    }

    static int MeasureDecodedLength(ReadOnlySpan<char> source, int offset)
    {
        int index = offset + 1;
        int decodedLength = 0;
        while (index < source.Length)
        {
            char character = source[index];
            if (character == '"')
            {
                return decodedLength;
            }

            if (character == '\\')
            {
                index = index + 1;
                if (index == source.Length)
                {
                    return -2;
                }

                if (!IsQuotedPairCharacter(source[index]))
                {
                    return -1;
                }
            }
            else if (!IsQuotedTextCharacter(character))
            {
                return -1;
            }

            decodedLength = decodedLength + 1;
            index = index + 1;
        }

        return -2;
    }

    static bool IsQuotedTextCharacter(char character)
    {
        return character == '\t' ||
            character == ' ' ||
            character == '!' ||
            (character >= '#' && character <= '[') ||
            (character >= ']' && character <= '~') ||
            (character >= (char)128 && character <= (char)255);
    }

    static bool IsQuotedPairCharacter(char character)
    {
        return character == '\t' ||
            character == ' ' ||
            (character >= '!' && character <= '~') ||
            (character >= (char)128 && character <= (char)255);
    }
}
