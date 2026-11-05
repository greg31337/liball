using System;

namespace uri;

internal static class IpV4Parser
{
    internal static IpV4ParseResult Parse(ReadOnlySpan<char> text, int baseOffset)
    {
        byte[] addressBytes = new byte[4];
        int octetIndex = 0;
        int offset = 0;
        while (octetIndex < 4)
        {
            if (offset >= text.Length)
            {
                return CreateFailure(baseOffset, offset, 0);
            }

            int octetStart = offset;
            char first = text[offset];
            if (!Ascii.IsDigit(first))
            {
                return CreateFailure(baseOffset, offset, 1);
            }

            int value = first - '0';
            offset = offset + 1;
            if (first == '0')
            {
                if (offset < text.Length && Ascii.IsDigit(text[offset]))
                {
                    return CreateFailure(baseOffset, offset, 1);
                }
            }
            else
            {
                while (offset < text.Length && Ascii.IsDigit(text[offset]))
                {
                    if (offset - octetStart >= 3)
                    {
                        return CreateFailure(baseOffset, offset, 1);
                    }

                    value = value * 10 + (text[offset] - '0');
                    if (value > 255)
                    {
                        return CreateFailure(baseOffset, offset, 1);
                    }

                    offset = offset + 1;
                }
            }

            addressBytes[octetIndex] = (byte)value;
            if (octetIndex < 3)
            {
                if (offset >= text.Length)
                {
                    return CreateFailure(baseOffset, offset, 0);
                }

                if (text[offset] != '.')
                {
                    return CreateFailure(baseOffset, offset, 1);
                }

                offset = offset + 1;
            }
            else if (offset < text.Length)
            {
                return CreateFailure(baseOffset, offset, 1);
            }

            octetIndex = octetIndex + 1;
        }

        return new IpV4ParseResult(true, addressBytes, null);
    }

    internal static IpV4ParseResult CreateFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidIpv4Address(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return new IpV4ParseResult(false, new byte[0], diagnostic);
    }
}
