using System;

namespace sdp;

internal static class SdpTokenReader
{
    internal static int SkipCharacterSpaces(ReadOnlySpan<char> input, int startOffset)
    {
        ValidateStartOffset(startOffset, input.Length);
        int nextOffset = startOffset;
        while (nextOffset < input.Length && input[nextOffset] == ' ')
        {
            nextOffset = nextOffset + 1;
        }
        return nextOffset;
    }

    internal static int SkipUtf8Spaces(ReadOnlySpan<byte> input, int startOffset)
    {
        ValidateStartOffset(startOffset, input.Length);
        int nextOffset = startOffset;
        while (nextOffset < input.Length && input[nextOffset] == 32)
        {
            nextOffset = nextOffset + 1;
        }
        return nextOffset;
    }

    internal static SdpTokenReadResult ReadCharacterNonWhitespaceToken(ReadOnlySpan<char> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && !char.IsWhiteSpace(input[endOffset]))
        {
            endOffset = endOffset + 1;
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult ReadUtf8NonWhitespaceToken(ReadOnlySpan<byte> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && !IsAsciiWhitespace(input[endOffset]))
        {
            endOffset = endOffset + 1;
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult ReadCharacterAttributeName(ReadOnlySpan<char> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && SdpCharacterClasses.IsTokenCharacter(input[endOffset]))
        {
            endOffset = endOffset + 1;
        }
        SdpTokenReadResult result = CreateTokenResult(startOffset, endOffset, maximumTokenLength);
        if (endOffset < input.Length && input[endOffset] != ':')
        {
            result.Success = false;
        }
        return result;
    }

    internal static SdpTokenReadResult ReadUtf8AttributeName(ReadOnlySpan<byte> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && SdpCharacterClasses.IsTokenByte(input[endOffset]))
        {
            endOffset = endOffset + 1;
        }
        SdpTokenReadResult result = CreateTokenResult(startOffset, endOffset, maximumTokenLength);
        if (endOffset < input.Length && input[endOffset] != 58)
        {
            result.Success = false;
        }
        return result;
    }

    internal static SdpTokenReadResult ReadCharacterDecimalToken(ReadOnlySpan<char> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && input[endOffset] >= '0' && input[endOffset] <= '9')
        {
            endOffset = endOffset + 1;
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult ReadUtf8DecimalToken(ReadOnlySpan<byte> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        while (endOffset < input.Length && input[endOffset] >= 48 && input[endOffset] <= 57)
        {
            endOffset = endOffset + 1;
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult ReadCharacterSignedDecimalToken(ReadOnlySpan<char> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        if (endOffset < input.Length && (input[endOffset] == '+' || input[endOffset] == '-'))
        {
            endOffset = endOffset + 1;
        }
        int digitOffset = endOffset;
        while (endOffset < input.Length && input[endOffset] >= '0' && input[endOffset] <= '9')
        {
            endOffset = endOffset + 1;
        }
        if (endOffset == digitOffset)
        {
            return CreateFailedResult(startOffset);
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult ReadUtf8SignedDecimalToken(ReadOnlySpan<byte> input, int startOffset, int maximumTokenLength)
    {
        ValidateReadArguments(startOffset, input.Length, maximumTokenLength);
        int endOffset = startOffset;
        if (endOffset < input.Length && (input[endOffset] == 43 || input[endOffset] == 45))
        {
            endOffset = endOffset + 1;
        }
        int digitOffset = endOffset;
        while (endOffset < input.Length && input[endOffset] >= 48 && input[endOffset] <= 57)
        {
            endOffset = endOffset + 1;
        }
        if (endOffset == digitOffset)
        {
            return CreateFailedResult(startOffset);
        }
        return CreateTokenResult(startOffset, endOffset, maximumTokenLength);
    }

    internal static SdpTokenReadResult CaptureCharacterRemainder(ReadOnlySpan<char> input, int startOffset)
    {
        ValidateStartOffset(startOffset, input.Length);
        SdpTokenReadResult result = CreateTokenResult(startOffset, input.Length, input.Length - startOffset);
        result.Success = true;
        return result;
    }

    internal static SdpTokenReadResult CaptureUtf8Remainder(ReadOnlySpan<byte> input, int startOffset)
    {
        ValidateStartOffset(startOffset, input.Length);
        SdpTokenReadResult result = CreateTokenResult(startOffset, input.Length, input.Length - startOffset);
        result.Success = true;
        return result;
    }

    internal static bool IsAsciiWhitespace(byte value)
    {
        return value == 9 || value == 10 || value == 11 || value == 12 || value == 13 || value == 32;
    }

    internal static SdpTokenReadResult CreateTokenResult(int startOffset, int endOffset, int maximumTokenLength)
    {
        SdpTokenReadResult result = new SdpTokenReadResult();
        result.StartOffset = startOffset;
        result.Length = endOffset - startOffset;
        result.NextOffset = endOffset;
        if (result.Length == 0)
        {
            return result;
        }
        if (result.Length > maximumTokenLength)
        {
            result.TokenLengthExceeded = true;
            return result;
        }
        result.Success = true;
        return result;
    }

    internal static SdpTokenReadResult CreateFailedResult(int startOffset)
    {
        SdpTokenReadResult result = new SdpTokenReadResult();
        result.StartOffset = startOffset;
        result.NextOffset = startOffset;
        return result;
    }

    internal static void ValidateReadArguments(int startOffset, int inputLength, int maximumTokenLength)
    {
        ValidateStartOffset(startOffset, inputLength);
        if (maximumTokenLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumTokenLength");
        }
    }

    internal static void ValidateStartOffset(int startOffset, int inputLength)
    {
        if (startOffset < 0 || startOffset > inputLength)
        {
            throw new ArgumentOutOfRangeException("startOffset");
        }
    }
}
