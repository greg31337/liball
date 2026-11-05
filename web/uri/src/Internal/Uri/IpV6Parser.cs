using System;

namespace uri;

internal static class IpV6Parser
{
    internal static IpV6ParseResult Parse(ReadOnlySpan<char> text, int baseOffset)
    {
        int zoneOffset = FindZoneMarker(text);
        if (zoneOffset >= 0)
        {
            IpV6ParseResult address = ParseAddress(
                text.Slice(0, zoneOffset),
                baseOffset);
            if (address.Success)
            {
                return CreateZoneIdentifierFailure(baseOffset, zoneOffset);
            }
        }

        return ParseAddress(text, baseOffset);
    }

    internal static IpV6ParseResult ParseAddress(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        byte[] addressBytes = new byte[16];
        int groupCount = 0;
        int compressionGroup = -1;
        int compressionOffset = -1;
        int offset = 0;
        if (text[0] == ':')
        {
            if (text.Length < 2 || text[1] != ':')
            {
                return CreateFailure(baseOffset, 0, 1);
            }

            compressionGroup = 0;
            compressionOffset = 0;
            offset = 2;
        }

        while (offset < text.Length)
        {
            if (groupCount >= 8)
            {
                return CreateFailure(baseOffset, offset, 1);
            }

            if (HasDotBeforeColon(text, offset))
            {
                if (groupCount > 6)
                {
                    return CreateFailure(baseOffset, offset, 1);
                }

                IpV4ParseResult ipv4 = IpV4Parser.Parse(
                    text.Slice(offset),
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset));
                if (!ipv4.Success)
                {
                    return CreateIpv4Failure(ipv4, baseOffset, offset);
                }

                WriteIpv4Address(addressBytes, groupCount, ipv4.AddressBytes);
                groupCount = groupCount + 2;
                offset = text.Length;
                continue;
            }

            int digitCount = 0;
            int value = 0;
            while (offset < text.Length && Ascii.IsHexDigit(text[offset]))
            {
                if (digitCount >= 4)
                {
                    return CreateFailure(baseOffset, offset, 1);
                }

                value = value * 16 + Ascii.GetHexValue(text[offset]);
                digitCount = digitCount + 1;
                offset = offset + 1;
            }

            if (digitCount == 0)
            {
                int length = 1;
                if (offset >= text.Length)
                {
                    length = 0;
                }

                return CreateFailure(baseOffset, offset, length);
            }

            WriteHexGroup(addressBytes, groupCount, value);
            groupCount = groupCount + 1;
            if (offset >= text.Length)
            {
                continue;
            }

            if (text[offset] != ':')
            {
                return CreateFailure(baseOffset, offset, 1);
            }

            if (offset + 1 < text.Length && text[offset + 1] == ':')
            {
                if (compressionGroup >= 0)
                {
                    return CreateFailure(baseOffset, offset, 2);
                }

                compressionGroup = groupCount;
                compressionOffset = offset;
                offset = offset + 2;
            }
            else
            {
                offset = offset + 1;
                if (offset >= text.Length)
                {
                    return CreateFailure(baseOffset, offset, 0);
                }
            }
        }

        if (compressionGroup < 0)
        {
            if (groupCount != 8)
            {
                return CreateFailure(baseOffset, text.Length, 0);
            }

            return new IpV6ParseResult(true, addressBytes, null);
        }

        if (groupCount >= 8)
        {
            return CreateFailure(baseOffset, compressionOffset, 2);
        }

        ExpandCompression(addressBytes, compressionGroup, groupCount);
        return new IpV6ParseResult(true, addressBytes, null);
    }

    internal static int FindZoneMarker(ReadOnlySpan<char> text)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    internal static bool HasDotBeforeColon(ReadOnlySpan<char> text, int startOffset)
    {
        int offset = startOffset;
        while (offset < text.Length && text[offset] != ':')
        {
            if (text[offset] == '.')
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static void WriteHexGroup(byte[] addressBytes, int groupIndex, int value)
    {
        int byteOffset = groupIndex * 2;
        addressBytes[byteOffset] = (byte)(value / 256);
        addressBytes[byteOffset + 1] = (byte)(value % 256);
    }

    internal static void WriteIpv4Address(
        byte[] addressBytes,
        int groupIndex,
        byte[] ipv4Bytes)
    {
        int byteOffset = groupIndex * 2;
        addressBytes[byteOffset] = ipv4Bytes[0];
        addressBytes[byteOffset + 1] = ipv4Bytes[1];
        addressBytes[byteOffset + 2] = ipv4Bytes[2];
        addressBytes[byteOffset + 3] = ipv4Bytes[3];
    }

    internal static void ExpandCompression(
        byte[] addressBytes,
        int compressionGroup,
        int groupCount)
    {
        int suffixGroupCount = groupCount - compressionGroup;
        int suffixIndex = suffixGroupCount - 1;
        while (suffixIndex >= 0)
        {
            int sourceGroup = compressionGroup + suffixIndex;
            int destinationGroup = 8 - suffixGroupCount + suffixIndex;
            addressBytes[destinationGroup * 2] = addressBytes[sourceGroup * 2];
            addressBytes[destinationGroup * 2 + 1] = addressBytes[sourceGroup * 2 + 1];
            suffixIndex = suffixIndex - 1;
        }

        int zeroStart = compressionGroup * 2;
        int zeroEnd = (8 - suffixGroupCount) * 2;
        int byteIndex = zeroStart;
        while (byteIndex < zeroEnd)
        {
            addressBytes[byteIndex] = 0;
            byteIndex = byteIndex + 1;
        }
    }

    internal static IpV6ParseResult CreateIpv4Failure(
        IpV4ParseResult ipv4,
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic? diagnostic = ipv4.Diagnostic;
        if (diagnostic != null)
        {
            UriDiagnostic mapped = DiagnosticFactory.CreateInvalidIpv6Address(
                diagnostic.Range.Offset,
                diagnostic.Range.Length);
            return new IpV6ParseResult(false, new byte[0], mapped);
        }

        return CreateFailure(baseOffset, relativeOffset, 1);
    }

    internal static IpV6ParseResult CreateFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidIpv6Address(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return new IpV6ParseResult(false, new byte[0], diagnostic);
    }

    internal static IpV6ParseResult CreateZoneIdentifierFailure(
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateZoneIdentifierNotAllowed(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            1);
        return new IpV6ParseResult(false, new byte[0], diagnostic);
    }
}
