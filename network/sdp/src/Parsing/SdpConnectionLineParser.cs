using System;

namespace sdp;

internal static class SdpConnectionLineParser
{
    internal static void ParseConnection(SdpParseContext context, SdpRawLine line)
    {
        int occurrenceIndex;
        if (context.Scope == SdpScope.Session)
        {
            occurrenceIndex = context.SessionConnectionCount;
            context.SessionConnectionCount = context.SessionConnectionCount + 1;
            if (occurrenceIndex > 0)
            {
                context.AddDiagnostic(SdpDiagnosticCodes.ParseDuplicateField, SdpValidationSeverity.Error, SdpScope.Session, 'c', -1, occurrenceIndex, "The session may contain only one connection field.", line.Location);
                return;
            }
        }
        else
        {
            if (context.CurrentMedia == null)
            {
                return;
            }
            occurrenceIndex = context.MediaConnectionCount;
            context.MediaConnectionCount = context.MediaConnectionCount + 1;
        }

        ReadOnlySpan<char> value = line.Text.AsSpan().Slice(2);
        SdpTokenReadResult[] tokens = new SdpTokenReadResult[3];
        int tokenIndex = 0;
        int offset = 0;
        int invalidOffset = 0;
        bool invalidCardinality = false;
        while (tokenIndex < tokens.Length && !invalidCardinality)
        {
            SdpTokenReadResult token = SdpTokenReader.ReadCharacterNonWhitespaceToken(value, offset, context.Options.MaximumTokenLength);
            bool utf8TokenLengthExceeded = false;
            if (context.InputIsUtf8 && !token.TokenLengthExceeded && token.Length > 0)
            {
                int utf8TokenLength = SdpLineReader.Utf8.GetByteCount(value.Slice(token.StartOffset, token.Length));
                utf8TokenLengthExceeded = utf8TokenLength > context.Options.MaximumTokenLength;
            }
            if (token.TokenLengthExceeded || utf8TokenLengthExceeded)
            {
                AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseTokenLengthLimitExceeded, SdpValidationSeverity.Fatal, "An SDP connection token exceeds the configured token-length limit.", token.StartOffset);
                context.Stop = true;
                return;
            }
            if (!token.Success)
            {
                invalidOffset = offset;
                invalidCardinality = true;
                continue;
            }

            tokens[tokenIndex] = token;
            if (tokenIndex < tokens.Length - 1)
            {
                int separatorOffset = token.NextOffset;
                if (separatorOffset >= value.Length || value[separatorOffset] != ' ')
                {
                    invalidOffset = separatorOffset;
                    invalidCardinality = true;
                }
                else if (separatorOffset + 1 >= value.Length || value[separatorOffset + 1] == ' ')
                {
                    invalidOffset = separatorOffset + 1;
                    invalidCardinality = true;
                }
                else
                {
                    offset = separatorOffset + 1;
                }
            }
            else if (token.NextOffset != value.Length)
            {
                invalidOffset = token.NextOffset;
                invalidCardinality = true;
            }
            tokenIndex = tokenIndex + 1;
        }

        if (invalidCardinality || tokenIndex != tokens.Length)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP connection field must contain exactly three tokens separated by single spaces.", invalidOffset);
            return;
        }

        SdpConnectionData connection = new SdpConnectionData();
        connection.NetworkType = value.Slice(tokens[0].StartOffset, tokens[0].Length).ToString();
        connection.AddressType = value.Slice(tokens[1].StartOffset, tokens[1].Length).ToString();
        connection.RawAddress = value.Slice(tokens[2].StartOffset, tokens[2].Length).ToString();
        connection.Address = connection.RawAddress;
        connection.RawText = value.ToString();
        connection.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        if (context.Scope == SdpScope.Session)
        {
            context.Description.Connection = connection;
        }
        else if (context.CurrentMedia != null)
        {
            context.CurrentMedia.Connections.Add(connection);
        }

        ValidateConnectionToken(context, line, occurrenceIndex, connection.NetworkType, tokens[0], "network type");
        ValidateConnectionToken(context, line, occurrenceIndex, connection.AddressType, tokens[1], "address type");
        ParseConnectionAddress(context, line, occurrenceIndex, connection, tokens[2]);
    }

    internal static void ValidateConnectionToken(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string value, SdpTokenReadResult token, string componentName)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(value[index]))
            {
                AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "The SDP connection " + componentName + " contains a character outside the token grammar.", token.StartOffset + index);
                return;
            }
            index = index + 1;
        }
    }

    internal static void ParseConnectionAddress(SdpParseContext context, SdpRawLine line, int occurrenceIndex, SdpConnectionData connection, SdpTokenReadResult addressToken)
    {
        if (connection.AddressType == SdpAddressTypes.Ip4)
        {
            ParseIpv4ConnectionAddress(context, line, occurrenceIndex, connection, addressToken);
            return;
        }
        if (connection.AddressType == SdpAddressTypes.Ip6)
        {
            ParseIpv6ConnectionAddress(context, line, occurrenceIndex, connection, addressToken);
        }
    }

    internal static void ParseIpv4ConnectionAddress(SdpParseContext context, SdpRawLine line, int occurrenceIndex, SdpConnectionData connection, SdpTokenReadResult addressToken)
    {
        string rawAddress = connection.RawAddress;
        int firstSlash = rawAddress.IndexOf('/');
        int secondSlash = firstSlash >= 0 ? rawAddress.IndexOf('/', firstSlash + 1) : -1;
        int thirdSlash = secondSlash >= 0 ? rawAddress.IndexOf('/', secondSlash + 1) : -1;
        string address = firstSlash >= 0 ? rawAddress.Substring(0, firstSlash) : rawAddress;
        connection.Address = address;
        if (thirdSlash >= 0)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "An IPv4 connection address contains too many multicast suffixes.", addressToken.StartOffset + thirdSlash);
            return;
        }

        bool validIpv4 = IsValidIpv4Address(address);
        if (!validIpv4)
        {
            if (firstSlash < 0 && IsValidFqdnAddress(address) && !LooksLikeIpv4Literal(address))
            {
                connection.AddressKind = SdpAddressKind.HostName;
                return;
            }
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "The IP4 connection address is neither a valid IPv4 address nor a valid host name.", addressToken.StartOffset);
            return;
        }

        int firstOctet = ReadIpv4FirstOctet(address);
        bool multicast = firstOctet >= 224 && firstOctet <= 239;
        if (multicast)
        {
            connection.AddressKind = SdpAddressKind.Ipv4Multicast;
            if (firstSlash < 0)
            {
                AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "An IPv4 multicast connection address requires a time-to-live suffix.", addressToken.StartOffset + address.Length);
                return;
            }

            int timeToLiveEnd = secondSlash >= 0 ? secondSlash : rawAddress.Length;
            string timeToLive = rawAddress.Substring(firstSlash + 1, timeToLiveEnd - firstSlash - 1);
            ParseIpv4TimeToLive(context, line, occurrenceIndex, connection, timeToLive, addressToken.StartOffset + firstSlash + 1);
            if (secondSlash >= 0)
            {
                string addressCount = rawAddress.Substring(secondSlash + 1);
                ParseMulticastAddressCount(context, line, occurrenceIndex, connection, addressCount, addressToken.StartOffset + secondSlash + 1);
            }
            return;
        }

        if (firstOctet >= 240)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "The IPv4 connection address is outside the unicast and multicast ranges permitted by SDP.", addressToken.StartOffset);
            return;
        }
        connection.AddressKind = SdpAddressKind.Ipv4;
        if (firstSlash >= 0)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "An IPv4 unicast connection address must not contain multicast suffixes.", addressToken.StartOffset + firstSlash);
        }
    }

    internal static void ParseIpv6ConnectionAddress(SdpParseContext context, SdpRawLine line, int occurrenceIndex, SdpConnectionData connection, SdpTokenReadResult addressToken)
    {
        string rawAddress = connection.RawAddress;
        int firstSlash = rawAddress.IndexOf('/');
        int secondSlash = firstSlash >= 0 ? rawAddress.IndexOf('/', firstSlash + 1) : -1;
        string address = firstSlash >= 0 ? rawAddress.Substring(0, firstSlash) : rawAddress;
        connection.Address = address;
        if (secondSlash >= 0)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseInvalidToken, SdpValidationSeverity.Error, "An IPv6 connection address may contain only one address-count suffix.", addressToken.StartOffset + secondSlash);
            return;
        }

        bool validIpv6 = IsValidIpv6Address(address);
        if (!validIpv6)
        {
            if (firstSlash < 0 && IsValidFqdnAddress(address) && !LooksLikeIpv4Literal(address))
            {
                connection.AddressKind = SdpAddressKind.HostName;
                return;
            }
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "The IP6 connection address is neither a valid IPv6 address nor a valid host name.", addressToken.StartOffset);
            return;
        }

        bool multicast = IsIpv6MulticastAddress(address);
        connection.AddressKind = multicast ? SdpAddressKind.Ipv6Multicast : SdpAddressKind.Ipv6;
        if (firstSlash >= 0)
        {
            if (!multicast)
            {
                AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "An IPv6 unicast connection address must not contain an address-count suffix.", addressToken.StartOffset + firstSlash);
                return;
            }
            string addressCount = rawAddress.Substring(firstSlash + 1);
            ParseMulticastAddressCount(context, line, occurrenceIndex, connection, addressCount, addressToken.StartOffset + firstSlash + 1);
        }
    }

    internal static void ParseIpv4TimeToLive(SdpParseContext context, SdpRawLine line, int occurrenceIndex, SdpConnectionData connection, string text, int relativeOffset)
    {
        connection.HasTimeToLive = true;
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(text.AsSpan());
        if (!number.Success)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, number.DiagnosticCode, SdpValidationSeverity.Error, "The IPv4 multicast time-to-live must be a decimal integer.", relativeOffset);
            return;
        }
        if (text.Length > 1 && text[0] == '0' || number.UnsignedValue > 255)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "The IPv4 multicast time-to-live must use canonical decimal form in the range zero through 255.", relativeOffset);
            return;
        }
        connection.TimeToLive = (int)number.UnsignedValue;
    }

    internal static void ParseMulticastAddressCount(SdpParseContext context, SdpRawLine line, int occurrenceIndex, SdpConnectionData connection, string text, int relativeOffset)
    {
        connection.HasAddressCount = true;
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(text.AsSpan());
        if (!number.Success)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, number.DiagnosticCode, SdpValidationSeverity.Error, "The multicast address count must be a positive decimal integer.", relativeOffset);
            return;
        }
        if (number.UnsignedValue > int.MaxValue)
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ParseNumberOverflow, SdpValidationSeverity.Error, "The multicast address count exceeds the supported integer representation.", relativeOffset);
            return;
        }
        if (number.UnsignedValue == 0 || text.Length > 1 && text[0] == '0')
        {
            AddConnectionDiagnostic(context, line, occurrenceIndex, SdpDiagnosticCodes.ValidationConnection, SdpValidationSeverity.Error, "The multicast address count must be a positive canonical decimal integer.", relativeOffset);
            return;
        }
        connection.AddressCount = (int)number.UnsignedValue;
    }

    internal static bool IsValidIpv4Address(string address)
    {
        int segmentCount = 0;
        int segmentStart = 0;
        int segmentValue = 0;
        int index = 0;
        while (index <= address.Length)
        {
            if (index == address.Length || address[index] == '.')
            {
                int segmentLength = index - segmentStart;
                if (segmentLength == 0 || segmentLength > 3 || segmentLength > 1 && address[segmentStart] == '0' || segmentValue > 255)
                {
                    return false;
                }
                segmentCount = segmentCount + 1;
                segmentStart = index + 1;
                segmentValue = 0;
            }
            else
            {
                char character = address[index];
                if (character < '0' || character > '9')
                {
                    return false;
                }
                if (index - segmentStart >= 3)
                {
                    return false;
                }
                segmentValue = segmentValue * 10 + character - '0';
            }
            index = index + 1;
        }
        return segmentCount == 4;
    }

    internal static int ReadIpv4FirstOctet(string address)
    {
        int result = 0;
        int index = 0;
        while (index < address.Length && address[index] != '.')
        {
            result = result * 10 + address[index] - '0';
            index = index + 1;
        }
        return result;
    }

    internal static bool LooksLikeIpv4Literal(string address)
    {
        bool hasDot = false;
        int index = 0;
        while (index < address.Length)
        {
            char character = address[index];
            if (character == '.')
            {
                hasDot = true;
            }
            else if (character < '0' || character > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return hasDot;
    }

    internal static bool IsValidFqdnAddress(string address)
    {
        if (address.Length < 4)
        {
            return false;
        }
        int index = 0;
        while (index < address.Length)
        {
            char character = address[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-' && character != '.')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsValidIpv6Address(string address)
    {
        if (address.Length == 0)
        {
            return false;
        }
        int compressionOffset = address.IndexOf("::", StringComparison.Ordinal);
        if (compressionOffset < 0)
        {
            return CountIpv6Words(address, true) == 8;
        }
        if (address.IndexOf("::", compressionOffset + 2, StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        string left = address.Substring(0, compressionOffset);
        string right = address.Substring(compressionOffset + 2);
        int leftCount = CountIpv6Words(left, right.Length == 0);
        int rightCount = CountIpv6Words(right, true);
        return leftCount >= 0 && rightCount >= 0 && leftCount + rightCount < 8;
    }

    internal static int CountIpv6Words(string text, bool allowIpv4AtEnd)
    {
        if (text.Length == 0)
        {
            return 0;
        }
        string[] parts = text.Split(':');
        int wordCount = 0;
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index];
            if (part.Length == 0)
            {
                return -1;
            }
            if (part.IndexOf('.') >= 0)
            {
                if (!allowIpv4AtEnd || index != parts.Length - 1 || !IsValidIpv4Address(part))
                {
                    return -1;
                }
                wordCount = wordCount + 2;
            }
            else
            {
                if (part.Length > 4)
                {
                    return -1;
                }
                int characterIndex = 0;
                while (characterIndex < part.Length)
                {
                    if (!IsHexDigit(part[characterIndex]))
                    {
                        return -1;
                    }
                    characterIndex = characterIndex + 1;
                }
                wordCount = wordCount + 1;
            }
            index = index + 1;
        }
        return wordCount;
    }

    internal static bool IsHexDigit(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    internal static bool IsIpv6MulticastAddress(string address)
    {
        return address.Length >= 2 && (address[0] == 'F' || address[0] == 'f') && (address[1] == 'F' || address[1] == 'f');
    }

    internal static void AddConnectionDiagnostic(SdpParseContext context, SdpRawLine line, int occurrenceIndex, string code, SdpValidationSeverity severity, string detail, int relativeOffset)
    {
        SdpSourceLocation location = CreateConnectionValueLocation(line, relativeOffset);
        context.AddDiagnostic(code, severity, context.Scope, 'c', context.MediaIndex, occurrenceIndex, detail, location);
    }

    internal static SdpSourceLocation CreateConnectionValueLocation(SdpRawLine line, int relativeOffset)
    {
        SdpSourceLocation result = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        result.CharacterOffset = result.CharacterOffset + 2 + relativeOffset;
        if (result.ByteOffset >= 0)
        {
            int byteLength = SdpLineReader.Utf8.GetByteCount(line.Text.AsSpan(0, 2 + relativeOffset));
            result.ByteOffset = result.ByteOffset + byteLength;
        }
        result.ColumnNumber = 3 + relativeOffset;
        return result;
    }
}
