using System;

namespace uri;

internal static class IpFutureParser
{
    internal static IpFutureParseResult Parse(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (AsciiCase.FoldToLower(text[0]) != 'v')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        int versionOffset = 1;
        int offset = versionOffset;
        while (offset < text.Length && Ascii.IsHexDigit(text[offset]))
        {
            offset = offset + 1;
        }

        int versionLength = offset - versionOffset;
        if (versionLength == 0)
        {
            int length = 1;
            if (offset >= text.Length)
            {
                length = 0;
            }

            return CreateFailure(baseOffset, offset, length);
        }

        if (offset >= text.Length)
        {
            return CreateFailure(baseOffset, offset, 0);
        }

        if (text[offset] != '.')
        {
            return CreateFailure(baseOffset, offset, 1);
        }

        int addressOffset = offset + 1;
        offset = addressOffset;
        if (offset >= text.Length)
        {
            return CreateFailure(baseOffset, offset, 0);
        }

        while (offset < text.Length)
        {
            if (!UriGrammar.IsIpFutureAddressCharacter(text[offset]))
            {
                return CreateFailure(baseOffset, offset, 1);
            }

            offset = offset + 1;
        }

        return new IpFutureParseResult(
            true,
            versionOffset,
            versionLength,
            addressOffset,
            text.Length - addressOffset,
            null);
    }

    internal static IpFutureParseResult CreateFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidIpvFutureAddress(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return new IpFutureParseResult(false, 0, 0, 0, 0, diagnostic);
    }
}
