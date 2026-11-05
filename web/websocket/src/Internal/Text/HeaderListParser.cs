using System;

namespace websocket;

internal class HeaderTokenListResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal string[] Elements = Array.Empty<string>();
}

internal static class HeaderListParser
{
    internal static HeaderTokenListResult ParseTokenList(
        ReadOnlySpan<char> source,
        int maximumElementCount)
    {
        HeaderTokenListResult result = new HeaderTokenListResult();
        if (maximumElementCount < 0)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        int elementCount = CountTokenElements(source, maximumElementCount);
        if (elementCount == -2)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        if (elementCount < 0)
        {
            return result;
        }

        string[] elements = new string[elementCount];
        int sourceIndex = 0;
        int elementIndex = 0;
        while (sourceIndex < source.Length)
        {
            sourceIndex = HttpFieldValue.SkipOptionalWhitespace(source, sourceIndex);
            if (sourceIndex == source.Length)
            {
                break;
            }

            if (source[sourceIndex] == ',')
            {
                sourceIndex = sourceIndex + 1;
                continue;
            }

            int tokenLength = AsciiToken.ReadLength(source, sourceIndex);
            elements[elementIndex] = new string(source.Slice(sourceIndex, tokenLength));
            elementIndex = elementIndex + 1;
            sourceIndex = sourceIndex + tokenLength;
            sourceIndex = HttpFieldValue.SkipOptionalWhitespace(source, sourceIndex);
            if (sourceIndex < source.Length)
            {
                sourceIndex = sourceIndex + 1;
            }
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Elements = elements;
        return result;
    }

    static int CountTokenElements(ReadOnlySpan<char> source, int maximumElementCount)
    {
        int elementCount = 0;
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            sourceIndex = HttpFieldValue.SkipOptionalWhitespace(source, sourceIndex);
            if (sourceIndex == source.Length)
            {
                return elementCount;
            }

            if (source[sourceIndex] == ',')
            {
                sourceIndex = sourceIndex + 1;
                continue;
            }

            int tokenLength = AsciiToken.ReadLength(source, sourceIndex);
            if (tokenLength == 0)
            {
                return -1;
            }

            elementCount = elementCount + 1;
            if (elementCount > maximumElementCount)
            {
                return -2;
            }

            sourceIndex = sourceIndex + tokenLength;
            sourceIndex = HttpFieldValue.SkipOptionalWhitespace(source, sourceIndex);
            if (sourceIndex < source.Length)
            {
                if (source[sourceIndex] != ',')
                {
                    return -1;
                }

                sourceIndex = sourceIndex + 1;
            }
        }

        return elementCount;
    }
}
