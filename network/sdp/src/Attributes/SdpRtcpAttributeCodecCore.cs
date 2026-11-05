using System;
using System.Globalization;

namespace sdp;

internal static class SdpRtcpAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeRtcp(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp value must contain a port and no prohibited line-ending characters.");
        }

        string[] tokens = ReadWhitespaceTokens(attribute.Value);
        if (tokens.Length != 1 && tokens.Length != 4)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp value requires a port followed by either no address or a complete network, address-type, and address triple.");
        }
        SdpNumberParseResult port = SdpNumberParser.ParseCharacterPort(tokens[0].AsSpan());
        if (!port.Success)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationPort, "The SDP rtcp port must be a decimal integer from zero through 65535.");
        }

        SdpRtcpAttribute projection = new SdpRtcpAttribute();
        projection.SourceAttribute = attribute;
        projection.Port = port.Int32Value;
        if (tokens.Length == 4)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[1]) || !SdpCommonAttributeCodecCore.IsTokenValue(tokens[2]))
            {
                return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp network and address types must use token syntax.");
            }
            string addressError = ReadAddressValidationError(tokens[2], tokens[3]);
            if (addressError.Length > 0)
            {
                return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationConnection, addressError);
            }
            projection.HasNetworkAddress = true;
            projection.NetworkType = tokens[1];
            projection.AddressType = tokens[2];
            projection.Address = tokens[3];
            projection.AddressKind = ReadAddressKind(tokens[2], tokens[3]);
        }

        result.RtcpAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeRtcp(SdpRtcpAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Port < 0 || projection.Port > 65535)
        {
            result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.Rtcp, SdpDiagnosticCodes.ValidationPort, "The SDP rtcp port must be from zero through 65535."));
            return result;
        }

        string value = projection.Port.ToString(CultureInfo.InvariantCulture);
        if (projection.HasNetworkAddress)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.NetworkType) || !SdpCommonAttributeCodecCore.IsTokenValue(projection.AddressType))
            {
                result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.Rtcp, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp network and address types must use token syntax."));
                return result;
            }
            string addressError = ReadAddressValidationError(projection.AddressType, projection.Address);
            if (addressError.Length > 0)
            {
                result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.Rtcp, SdpDiagnosticCodes.ValidationConnection, addressError));
                return result;
            }
            value = value + " " + projection.NetworkType + " " + projection.AddressType + " " + projection.Address;
        }
        else if (projection.NetworkType.Length > 0 || projection.AddressType.Length > 0 || projection.Address.Length > 0 || projection.AddressKind != SdpAddressKind.Unknown)
        {
            result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.Rtcp, SdpDiagnosticCodes.ValidationAttribute, "SDP rtcp address data requires HasNetworkAddress to be enabled."));
            return result;
        }

        result.Attribute = CreateValueAttribute(SdpAttributeNames.Rtcp, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeRtcpFeedback(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-fb attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-fb value must not be empty or contain prohibited line-ending characters.");
        }

        string value = attribute.Value;
        int valueStart = SkipWhitespace(value, 0);
        int valueEnd = TrimWhitespaceEnd(value, valueStart, value.Length);
        int payloadEnd = ReadTokenEnd(value, valueStart, valueEnd);
        int typeStart = SkipWhitespace(value, payloadEnd);
        int typeEnd = ReadTokenEnd(value, typeStart, valueEnd);
        if (payloadEnd == valueStart || typeStart == payloadEnd || typeEnd == typeStart)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-fb value requires a payload selector and feedback type.");
        }

        string payloadType = value.Substring(valueStart, payloadEnd - valueStart);
        if (!IsValidFeedbackPayload(payloadType))
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP rtcp-fb payload selector must be an asterisk or a canonical integer from zero through 127.");
        }
        string feedbackType = value.Substring(typeStart, typeEnd - typeStart);
        if (!IsFeedbackIdentifier(feedbackType))
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-fb feedback type must contain only letters, digits, hyphens, or underscores.");
        }

        int parameterStart = SkipWhitespace(value, typeEnd);
        bool hasParameter = parameterStart < valueEnd;
        string parameter = hasParameter ? value.Substring(parameterStart, valueEnd - parameterStart) : string.Empty;
        string parameterError = ReadFeedbackParameterError(feedbackType, hasParameter, parameter);
        if (parameterError.Length > 0)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, parameterError);
        }

        SdpRtcpFeedbackAttribute projection = new SdpRtcpFeedbackAttribute();
        projection.SourceAttribute = attribute;
        projection.PayloadType = payloadType;
        projection.Type = feedbackType;
        projection.HasParameter = hasParameter;
        projection.Parameter = parameter;
        result.RtcpFeedbackAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeRtcpFeedback(SdpRtcpFeedbackAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsValidFeedbackPayload(projection.PayloadType))
        {
            result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.RtcpFeedback, SdpDiagnosticCodes.ValidationPayloadType, "The SDP rtcp-fb payload selector must be an asterisk or an integer from zero through 127."));
            return result;
        }
        if (!IsFeedbackIdentifier(projection.Type))
        {
            result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.RtcpFeedback, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-fb feedback type must contain only letters, digits, hyphens, or underscores."));
            return result;
        }
        string parameterError = ReadFeedbackParameterError(projection.Type, projection.HasParameter, projection.Parameter);
        if (parameterError.Length > 0)
        {
            result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.RtcpFeedback, SdpDiagnosticCodes.ValidationAttribute, parameterError));
            return result;
        }

        string value = projection.PayloadType + " " + projection.Type;
        if (projection.HasParameter)
        {
            value = value + " " + projection.Parameter;
        }
        result.Attribute = CreateValueAttribute(SdpAttributeNames.RtcpFeedback, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeRtcpExtendedReport(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-xr attribute requires colon-delimited value syntax, including when its value is empty.");
        }
        if (!IsSafeExtendedReportValue(attribute.Value))
        {
            return FailRtcpDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-xr value must contain whitespace-delimited non-whitespace report block tokens.");
        }

        SdpRtcpExtendedReportAttribute projection = new SdpRtcpExtendedReportAttribute();
        projection.SourceAttribute = attribute;
        projection.RawValue = attribute.Value;
        string[] blocks = ReadWhitespaceTokens(attribute.Value);
        int index = 0;
        while (index < blocks.Length)
        {
            projection.Blocks.Add(blocks[index]);
            index = index + 1;
        }
        result.RtcpExtendedReportAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeRtcpExtendedReport(SdpRtcpExtendedReportAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string value = string.Empty;
        if (projection.Blocks.Count > 0)
        {
            int index = 0;
            while (index < projection.Blocks.Count)
            {
                string block = projection.Blocks[index];
                if (!SdpCommonAttributeCodecCore.IsNonWhitespaceValue(block))
                {
                    result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.RtcpExtendedReports, SdpDiagnosticCodes.ValidationAttribute, "Every SDP rtcp-xr report block must be a non-whitespace token."));
                    return result;
                }
                if (index > 0)
                {
                    value = value + " ";
                }
                value = value + block;
                index = index + 1;
            }
        }
        else
        {
            if (!IsSafeExtendedReportValue(projection.RawValue))
            {
                result.Diagnostics.Add(CreateRtcpEncodeDiagnostic(SdpAttributeNames.RtcpExtendedReports, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtcp-xr value must contain whitespace-delimited non-whitespace report block tokens."));
                return result;
            }
            string[] rawBlocks = ReadWhitespaceTokens(projection.RawValue);
            int index = 0;
            while (index < rawBlocks.Length)
            {
                if (index > 0)
                {
                    value = value + " ";
                }
                value = value + rawBlocks[index];
                index = index + 1;
            }
        }

        result.Attribute = CreateValueAttribute(SdpAttributeNames.RtcpExtendedReports, value);
        result.Success = true;
        return result;
    }

    internal static string[] ReadWhitespaceTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static int SkipWhitespace(string value, int start)
    {
        int index = start;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t'))
        {
            index = index + 1;
        }
        return index;
    }

    internal static int TrimWhitespaceEnd(string value, int start, int end)
    {
        int index = end;
        while (index > start && (value[index - 1] == ' ' || value[index - 1] == '\t'))
        {
            index = index - 1;
        }
        return index;
    }

    internal static int ReadTokenEnd(string value, int start, int end)
    {
        int index = start;
        while (index < end && value[index] != ' ' && value[index] != '\t')
        {
            index = index + 1;
        }
        return index;
    }

    internal static bool IsValidFeedbackPayload(string value)
    {
        if (value == "*")
        {
            return true;
        }
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan());
        return payload.Success;
    }

    internal static bool IsFeedbackIdentifier(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-' && character != '_')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static string ReadFeedbackParameterError(string feedbackType, bool hasParameter, string parameter)
    {
        if (hasParameter)
        {
            if (!SdpCommonAttributeCodecCore.IsByteStringValue(parameter))
            {
                return "The SDP rtcp-fb parameter must not be empty or contain prohibited line-ending characters.";
            }
            int firstTokenEnd = ReadTokenEnd(parameter, 0, parameter.Length);
            string firstToken = parameter.Substring(0, firstTokenEnd);
            if (!SdpCommonAttributeCodecCore.IsTokenValue(firstToken))
            {
                return "The SDP rtcp-fb parameter must begin with a token.";
            }
        }
        if (feedbackType == SdpRtcpFeedbackTypes.MinimumInterval)
        {
            if (!hasParameter || !IsDecimalText(parameter))
            {
                return "The SDP rtcp-fb trr-int feedback type requires a decimal interval parameter.";
            }
        }
        return string.Empty;
    }

    internal static bool IsDecimalText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static bool IsSafeExtendedReportValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character == '\r' || character == '\n')
            {
                return false;
            }
            if (character != ' ' && character != '\t' && !SdpCharacterClasses.IsNonWhitespaceCharacter(character))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string ReadAddressValidationError(string addressType, string address)
    {
        if (!SdpCommonAttributeCodecCore.IsNonWhitespaceValue(address))
        {
            return "The SDP rtcp connection address must be a non-whitespace value.";
        }
        if (addressType == SdpAddressTypes.Ip4)
        {
            return ReadIpv4AddressValidationError(address);
        }
        if (addressType == SdpAddressTypes.Ip6)
        {
            return ReadIpv6AddressValidationError(address);
        }
        return string.Empty;
    }

    internal static string ReadIpv4AddressValidationError(string address)
    {
        int firstSlash = address.IndexOf('/');
        int secondSlash = firstSlash >= 0 ? address.IndexOf('/', firstSlash + 1) : -1;
        int thirdSlash = secondSlash >= 0 ? address.IndexOf('/', secondSlash + 1) : -1;
        if (thirdSlash >= 0)
        {
            return "The SDP rtcp IPv4 address contains too many multicast suffixes.";
        }
        string host = firstSlash >= 0 ? address.Substring(0, firstSlash) : address;
        if (!SdpConnectionLineParser.IsValidIpv4Address(host))
        {
            if (firstSlash < 0 && SdpConnectionLineParser.IsValidFqdnAddress(host) && !SdpConnectionLineParser.LooksLikeIpv4Literal(host))
            {
                return string.Empty;
            }
            return "The SDP rtcp IP4 address is neither a valid IPv4 address nor a valid host name.";
        }
        int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(host);
        if (firstOctet >= 240)
        {
            return "The SDP rtcp IPv4 address is outside the unicast and multicast ranges permitted by SDP.";
        }
        bool multicast = firstOctet >= 224;
        if (!multicast && firstSlash >= 0)
        {
            return "An SDP rtcp IPv4 unicast address must not contain multicast suffixes.";
        }
        if (multicast && firstSlash < 0)
        {
            return "An SDP rtcp IPv4 multicast address requires a time-to-live suffix.";
        }
        if (multicast)
        {
            int ttlEnd = secondSlash >= 0 ? secondSlash : address.Length;
            string ttl = address.Substring(firstSlash + 1, ttlEnd - firstSlash - 1);
            SdpNumberParseResult ttlNumber = SdpNumberParser.ParseCharacterUnsignedDecimal(ttl.AsSpan());
            if (!ttlNumber.Success || ttlNumber.UnsignedValue > 255 || ttl.Length > 1 && ttl[0] == '0')
            {
                return "The SDP rtcp IPv4 multicast time-to-live must be a canonical integer from zero through 255.";
            }
            if (secondSlash >= 0)
            {
                string count = address.Substring(secondSlash + 1);
                if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(count))
                {
                    return "The SDP rtcp multicast address count must be a positive canonical integer.";
                }
                SdpNumberParseResult countNumber = SdpNumberParser.ParseCharacterUnsignedDecimal(count.AsSpan());
                if (!countNumber.Success || countNumber.UnsignedValue > int.MaxValue)
                {
                    return "The SDP rtcp multicast address count exceeds the supported integer range.";
                }
            }
        }
        return string.Empty;
    }

    internal static string ReadIpv6AddressValidationError(string address)
    {
        int firstSlash = address.IndexOf('/');
        int secondSlash = firstSlash >= 0 ? address.IndexOf('/', firstSlash + 1) : -1;
        if (secondSlash >= 0)
        {
            return "The SDP rtcp IPv6 address may contain only one address-count suffix.";
        }
        string host = firstSlash >= 0 ? address.Substring(0, firstSlash) : address;
        if (!SdpConnectionLineParser.IsValidIpv6Address(host))
        {
            if (firstSlash < 0 && SdpConnectionLineParser.IsValidFqdnAddress(host) && !SdpConnectionLineParser.LooksLikeIpv4Literal(host))
            {
                return string.Empty;
            }
            return "The SDP rtcp IP6 address is neither a valid IPv6 address nor a valid host name.";
        }
        bool multicast = SdpConnectionLineParser.IsIpv6MulticastAddress(host);
        if (!multicast && firstSlash >= 0)
        {
            return "An SDP rtcp IPv6 unicast address must not contain an address-count suffix.";
        }
        if (firstSlash >= 0)
        {
            string count = address.Substring(firstSlash + 1);
            if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(count))
            {
                return "The SDP rtcp multicast address count must be a positive canonical integer.";
            }
            SdpNumberParseResult countNumber = SdpNumberParser.ParseCharacterUnsignedDecimal(count.AsSpan());
            if (!countNumber.Success || countNumber.UnsignedValue > int.MaxValue)
            {
                return "The SDP rtcp multicast address count exceeds the supported integer range.";
            }
        }
        return string.Empty;
    }

    internal static SdpAddressKind ReadAddressKind(string addressType, string address)
    {
        int slash = address.IndexOf('/');
        string host = slash >= 0 ? address.Substring(0, slash) : address;
        if (addressType == SdpAddressTypes.Ip4)
        {
            if (SdpConnectionLineParser.IsValidIpv4Address(host))
            {
                int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(host);
                return firstOctet >= 224 ? SdpAddressKind.Ipv4Multicast : SdpAddressKind.Ipv4;
            }
            return SdpAddressKind.HostName;
        }
        if (addressType == SdpAddressTypes.Ip6)
        {
            if (SdpConnectionLineParser.IsValidIpv6Address(host))
            {
                return SdpConnectionLineParser.IsIpv6MulticastAddress(host) ? SdpAddressKind.Ipv6Multicast : SdpAddressKind.Ipv6;
            }
            return SdpAddressKind.HostName;
        }
        return SdpAddressKind.Unknown;
    }

    internal static SdpAttribute CreateValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailRtcpDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateRtcpEncodeDiagnostic(string attributeName, string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
