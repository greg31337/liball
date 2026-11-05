using System;
using System.Text;

namespace sdp;

internal static class SdpLineReader
{
    internal static readonly Encoding Utf8 = new UTF8Encoding(false, false);

    internal static SdpLineReadResult ReadCharacterLine(ReadOnlySpan<char> input, int startOffset, int lineNumber, int maximumLineLength, int maximumLineCount)
    {
        ValidateReadArguments(startOffset, input.Length, lineNumber, maximumLineLength, maximumLineCount);
        SdpLineReadResult result = new SdpLineReadResult();
        result.NextOffset = startOffset;
        if (startOffset == input.Length)
        {
            result.EndOfInput = true;
            return result;
        }
        if (lineNumber > maximumLineCount)
        {
            result.LineCountExceeded = true;
            return result;
        }

        int end = startOffset;
        while (end < input.Length && input[end] != '\r' && input[end] != '\n')
        {
            end = end + 1;
        }
        int contentLength = end - startOffset;
        int endingLength = 0;
        SdpLineEnding lineEnding = SdpLineEnding.None;
        if (end < input.Length)
        {
            if (input[end] == '\r' && end + 1 < input.Length && input[end + 1] == '\n')
            {
                lineEnding = SdpLineEnding.CrLf;
                endingLength = 2;
            }
            else if (input[end] == '\n')
            {
                lineEnding = SdpLineEnding.Lf;
                endingLength = 1;
            }
            else
            {
                lineEnding = SdpLineEnding.Cr;
                endingLength = 1;
            }
        }

        result.HasLine = true;
        result.CharacterLength = contentLength;
        result.CharactersConsumed = contentLength + endingLength;
        result.NextOffset = end + endingLength;
        result.EndOfInput = result.NextOffset == input.Length;
        result.Line.LineEnding = lineEnding;
        result.Line.Location.CharacterOffset = startOffset;
        result.Line.Location.LineNumber = lineNumber;
        result.Line.Location.ColumnNumber = 1;
        if (contentLength > maximumLineLength)
        {
            result.LineLengthExceeded = true;
            return result;
        }
        result.Line.Text = input.Slice(startOffset, contentLength).ToString();
        return result;
    }

    internal static SdpLineReadResult ReadUtf8Line(ReadOnlySpan<byte> input, int startByteOffset, int startCharacterOffset, int lineNumber, int maximumLineLength, int maximumLineCount)
    {
        ValidateReadArguments(startByteOffset, input.Length, lineNumber, maximumLineLength, maximumLineCount);
        if (startCharacterOffset < 0)
        {
            throw new ArgumentOutOfRangeException("startCharacterOffset");
        }
        SdpLineReadResult result = new SdpLineReadResult();
        result.NextOffset = startByteOffset;
        if (startByteOffset == input.Length)
        {
            result.EndOfInput = true;
            return result;
        }
        if (lineNumber > maximumLineCount)
        {
            result.LineCountExceeded = true;
            return result;
        }

        int end = startByteOffset;
        while (end < input.Length && input[end] != 13 && input[end] != 10)
        {
            end = end + 1;
        }
        int contentLength = end - startByteOffset;
        int endingLength = 0;
        SdpLineEnding lineEnding = SdpLineEnding.None;
        if (end < input.Length)
        {
            if (input[end] == 13 && end + 1 < input.Length && input[end + 1] == 10)
            {
                lineEnding = SdpLineEnding.CrLf;
                endingLength = 2;
            }
            else if (input[end] == 10)
            {
                lineEnding = SdpLineEnding.Lf;
                endingLength = 1;
            }
            else
            {
                lineEnding = SdpLineEnding.Cr;
                endingLength = 1;
            }
        }

        ReadOnlySpan<byte> content = input.Slice(startByteOffset, contentLength);
        int characterLength = Utf8.GetCharCount(content);
        result.HasLine = true;
        result.CharacterLength = characterLength;
        result.ByteLength = contentLength;
        result.CharactersConsumed = characterLength + endingLength;
        result.BytesConsumed = contentLength + endingLength;
        result.NextOffset = end + endingLength;
        result.EndOfInput = result.NextOffset == input.Length;
        result.Line.LineEnding = lineEnding;
        result.Line.Location.CharacterOffset = startCharacterOffset;
        result.Line.Location.ByteOffset = startByteOffset;
        result.Line.Location.LineNumber = lineNumber;
        result.Line.Location.ColumnNumber = 1;
        if (contentLength > maximumLineLength)
        {
            result.LineLengthExceeded = true;
            return result;
        }
        result.Line.Text = Utf8.GetString(content);
        return result;
    }

    internal static void ValidateReadArguments(int startOffset, int inputLength, int lineNumber, int maximumLineLength, int maximumLineCount)
    {
        if (startOffset < 0 || startOffset > inputLength)
        {
            throw new ArgumentOutOfRangeException("startOffset");
        }
        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }
        if (maximumLineLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLineLength");
        }
        if (maximumLineCount < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLineCount");
        }
    }
}
