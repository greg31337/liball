using System;
using System.Globalization;

namespace sdp;

internal static class SdpIceCandidateCodecCore
{
    internal static SdpAttributeDecodeResult DecodeCandidate(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate value must contain the required candidate fields.");
        }

        string[] tokens = ReadCandidateTokens(attribute.Value);
        if (tokens.Length < 8 || (tokens.Length - 8) % 2 != 0)
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate value requires eight base fields followed by complete extension name/value pairs.");
        }
        if (!IsFoundationValue(tokens[0]))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate foundation must contain 1 through 32 ICE characters.");
        }

        SdpNumberParseResult component = SdpNumberParser.ParseCharacterUnsignedDecimal(tokens[1].AsSpan());
        if (!component.Success || tokens[1].Length > 3 || component.UnsignedValue > 999)
        {
            string code = component.Success ? SdpDiagnosticCodes.ValidationAttribute : component.DiagnosticCode;
            return FailCandidateDecode(attribute, result, code, "The SDP candidate component identifier must contain one through three decimal digits.");
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[2]))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate transport must use token syntax.");
        }

        SdpNumberParseResult priority = SdpNumberParser.ParseCharacterPriority(tokens[3].AsSpan());
        if (!priority.Success || tokens[3].Length > 10)
        {
            string code = priority.Success ? SdpDiagnosticCodes.ValidationAttribute : priority.DiagnosticCode;
            return FailCandidateDecode(attribute, result, code, "The SDP candidate priority must contain one through ten decimal digits in the unsigned 32-bit range.");
        }
        if (!IsCandidateAddressValue(tokens[4]))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationConnection, "The SDP candidate connection address must be a non-whitespace value.");
        }

        SdpNumberParseResult port = SdpNumberParser.ParseCharacterPort(tokens[5].AsSpan());
        if (!port.Success || tokens[5].Length > 5)
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationPort, "The SDP candidate port must contain one through five decimal digits from zero through 65535.");
        }
        if (!string.Equals(tokens[6], SdpIceTokens.CandidateType, StringComparison.OrdinalIgnoreCase))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate type field must begin with the typ token.");
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[7]))
        {
            return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate type must use token syntax.");
        }

        SdpIceCandidate projection = new SdpIceCandidate();
        projection.SourceAttribute = attribute;
        projection.Foundation = tokens[0];
        projection.Component = (int)component.UnsignedValue;
        projection.TransportToken = tokens[2];
        projection.TransportProtocol = ReadTransportProtocol(tokens[2]);
        projection.Priority = priority.UInt32Value;
        projection.Address = tokens[4];
        projection.AddressKind = ReadCandidateAddressKind(tokens[4]);
        projection.Port = port.Int32Value;
        projection.CandidateTypeToken = tokens[7];
        projection.CandidateType = ReadCandidateType(tokens[7]);

        int index = 8;
        while (index < tokens.Length)
        {
            string name = tokens[index];
            string value = tokens[index + 1];
            if (!SdpCommonAttributeCodecCore.IsTokenValue(name) || !IsExtensionValue(value))
            {
                return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "Every SDP candidate extension must use a token name and visible-character value.");
            }
            if (string.Equals(name, SdpIceTokens.RelatedAddress, StringComparison.OrdinalIgnoreCase))
            {
                if (projection.HasRelatedAddress || !IsCandidateAddressValue(value))
                {
                    return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationConnection, "The SDP candidate may contain one valid related-address extension.");
                }
                projection.HasRelatedAddress = true;
                projection.RelatedAddress = value;
            }
            else if (string.Equals(name, SdpIceTokens.RelatedPort, StringComparison.OrdinalIgnoreCase))
            {
                if (projection.HasRelatedPort)
                {
                    return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationPort, "The SDP candidate may contain only one related-port extension.");
                }
                SdpNumberParseResult relatedPort = SdpNumberParser.ParseCharacterPort(value.AsSpan());
                if (!relatedPort.Success || value.Length > 5)
                {
                    return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationPort, "The SDP candidate related port must contain one through five decimal digits from zero through 65535.");
                }
                projection.HasRelatedPort = true;
                projection.RelatedPort = relatedPort.Int32Value;
            }
            else if (string.Equals(name, SdpIceTokens.TcpType, StringComparison.OrdinalIgnoreCase))
            {
                if (projection.TcpTypeToken.Length > 0 || !SdpCommonAttributeCodecCore.IsTokenValue(value))
                {
                    return FailCandidateDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate may contain one token-valued TCP type extension.");
                }
                projection.TcpTypeToken = value;
                projection.TcpType = ReadTcpType(value);
            }
            else
            {
                SdpIceCandidateExtension extension = new SdpIceCandidateExtension();
                extension.Name = name;
                extension.Value = value;
                projection.Extensions.Add(extension);
            }
            index = index + 2;
        }

        result.IceCandidate = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeCandidate(SdpIceCandidate projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsFoundationValue(projection.Foundation))
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate foundation must contain 1 through 32 ICE characters."));
            return result;
        }
        if (projection.Component < 0 || projection.Component > 999)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate component identifier must contain one through three decimal digits."));
            return result;
        }

        string transport = ReadTransportEncodingToken(projection);
        if (transport.Length == 0)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate transport kind and token are invalid or inconsistent."));
            return result;
        }
        if (!IsCandidateAddressValue(projection.Address))
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "The SDP candidate connection address must be a non-whitespace value."));
            return result;
        }
        if (projection.Port < 0 || projection.Port > 65535)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationPort, "The SDP candidate port must be from zero through 65535."));
            return result;
        }

        string candidateType = ReadCandidateTypeEncodingToken(projection);
        if (candidateType.Length == 0)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate type kind and token are invalid or inconsistent."));
            return result;
        }

        string value = projection.Foundation + " " + projection.Component.ToString(CultureInfo.InvariantCulture) + " " + transport + " " + projection.Priority.ToString(CultureInfo.InvariantCulture) + " " + projection.Address + " " + projection.Port.ToString(CultureInfo.InvariantCulture) + " " + SdpIceTokens.CandidateType + " " + candidateType;
        if (projection.HasRelatedAddress)
        {
            if (!IsCandidateAddressValue(projection.RelatedAddress))
            {
                result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "The SDP candidate related address must be a non-whitespace value."));
                return result;
            }
            value = value + " " + SdpIceTokens.RelatedAddress + " " + projection.RelatedAddress;
        }
        else if (projection.RelatedAddress.Length > 0)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "SDP candidate related-address data requires HasRelatedAddress to be enabled."));
            return result;
        }
        if (projection.HasRelatedPort)
        {
            if (projection.RelatedPort < 0 || projection.RelatedPort > 65535)
            {
                result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationPort, "The SDP candidate related port must be from zero through 65535."));
                return result;
            }
            value = value + " " + SdpIceTokens.RelatedPort + " " + projection.RelatedPort.ToString(CultureInfo.InvariantCulture);
        }
        else if (projection.RelatedPort != 0)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationPort, "SDP candidate related-port data requires HasRelatedPort to be enabled."));
            return result;
        }

        string tcpType = ReadTcpTypeEncodingToken(projection);
        if (tcpType.Length > 0)
        {
            value = value + " " + SdpIceTokens.TcpType + " " + tcpType;
        }
        else if (projection.TcpType != SdpIceTcpType.Unknown || projection.TcpTypeToken.Length > 0)
        {
            result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP candidate TCP type kind and token are invalid or inconsistent."));
            return result;
        }

        int index = 0;
        while (index < projection.Extensions.Count)
        {
            SdpIceCandidateExtension extension = projection.Extensions[index];
            if (!SdpCommonAttributeCodecCore.IsTokenValue(extension.Name) || IsKnownExtensionName(extension.Name) || !IsExtensionValue(extension.Value))
            {
                result.Diagnostics.Add(CreateCandidateEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "Every unknown SDP candidate extension must use a non-reserved token name and visible-character value."));
                return result;
            }
            value = value + " " + extension.Name + " " + extension.Value;
            index = index + 1;
        }

        result.Attribute = CreateCandidateValueAttribute(value);
        result.Success = true;
        return result;
    }

    internal static string[] ReadCandidateTokens(string value)
    {
        string[] tokens = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 9 && (tokens.Length - 8) % 2 != 0 && EndsWithCandidateWhitespace(value))
        {
            string[] extended = new string[tokens.Length + 1];
            int index = 0;
            while (index < tokens.Length)
            {
                extended[index] = tokens[index];
                index = index + 1;
            }
            extended[tokens.Length] = string.Empty;
            return extended;
        }
        return tokens;
    }

    internal static bool EndsWithCandidateWhitespace(string value)
    {
        return value.Length > 0 && (value[value.Length - 1] == ' ' || value[value.Length - 1] == '\t');
    }

    internal static bool IsFoundationValue(string value)
    {
        return SdpIceAttributeCodecCore.IsIceCredentialValue(value, 1, 32);
    }

    internal static bool IsCandidateAddressValue(string value)
    {
        return SdpCommonAttributeCodecCore.IsNonWhitespaceValue(value);
    }

    internal static bool IsExtensionValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '!' || value[index] > '~')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static SdpIceTransportProtocol ReadTransportProtocol(string token)
    {
        if (string.Equals(token, SdpIceTokens.UdpTransport, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceTransportProtocol.Udp;
        }
        if (string.Equals(token, SdpIceTokens.TcpTransport, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceTransportProtocol.Tcp;
        }
        return SdpIceTransportProtocol.Unknown;
    }

    internal static SdpIceCandidateType ReadCandidateType(string token)
    {
        if (string.Equals(token, SdpIceTokens.HostCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceCandidateType.Host;
        }
        if (string.Equals(token, SdpIceTokens.ServerReflexiveCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceCandidateType.ServerReflexive;
        }
        if (string.Equals(token, SdpIceTokens.PeerReflexiveCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceCandidateType.PeerReflexive;
        }
        if (string.Equals(token, SdpIceTokens.RelayedCandidate, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceCandidateType.Relayed;
        }
        return SdpIceCandidateType.Unknown;
    }

    internal static SdpIceTcpType ReadTcpType(string token)
    {
        if (string.Equals(token, SdpIceTokens.ActiveTcp, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceTcpType.Active;
        }
        if (string.Equals(token, SdpIceTokens.PassiveTcp, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceTcpType.Passive;
        }
        if (string.Equals(token, SdpIceTokens.SimultaneousOpenTcp, StringComparison.OrdinalIgnoreCase))
        {
            return SdpIceTcpType.SimultaneousOpen;
        }
        return SdpIceTcpType.Unknown;
    }

    internal static string ReadTransportEncodingToken(SdpIceCandidate projection)
    {
        string expected = string.Empty;
        if (projection.TransportProtocol == SdpIceTransportProtocol.Udp)
        {
            expected = SdpIceTokens.UdpTransport;
        }
        else if (projection.TransportProtocol == SdpIceTransportProtocol.Tcp)
        {
            expected = SdpIceTokens.TcpTransport;
        }
        if (expected.Length > 0)
        {
            if (projection.TransportToken.Length == 0 || string.Equals(projection.TransportToken, expected, StringComparison.OrdinalIgnoreCase))
            {
                return expected;
            }
            return string.Empty;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.TransportToken) || ReadTransportProtocol(projection.TransportToken) != SdpIceTransportProtocol.Unknown)
        {
            return string.Empty;
        }
        return projection.TransportToken;
    }

    internal static string ReadCandidateTypeEncodingToken(SdpIceCandidate projection)
    {
        string expected = string.Empty;
        if (projection.CandidateType == SdpIceCandidateType.Host)
        {
            expected = SdpIceTokens.HostCandidate;
        }
        else if (projection.CandidateType == SdpIceCandidateType.ServerReflexive)
        {
            expected = SdpIceTokens.ServerReflexiveCandidate;
        }
        else if (projection.CandidateType == SdpIceCandidateType.PeerReflexive)
        {
            expected = SdpIceTokens.PeerReflexiveCandidate;
        }
        else if (projection.CandidateType == SdpIceCandidateType.Relayed)
        {
            expected = SdpIceTokens.RelayedCandidate;
        }
        if (expected.Length > 0)
        {
            if (projection.CandidateTypeToken.Length == 0 || string.Equals(projection.CandidateTypeToken, expected, StringComparison.OrdinalIgnoreCase))
            {
                return expected;
            }
            return string.Empty;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.CandidateTypeToken) || ReadCandidateType(projection.CandidateTypeToken) != SdpIceCandidateType.Unknown)
        {
            return string.Empty;
        }
        return projection.CandidateTypeToken;
    }

    internal static string ReadTcpTypeEncodingToken(SdpIceCandidate projection)
    {
        string expected = string.Empty;
        if (projection.TcpType == SdpIceTcpType.Active)
        {
            expected = SdpIceTokens.ActiveTcp;
        }
        else if (projection.TcpType == SdpIceTcpType.Passive)
        {
            expected = SdpIceTokens.PassiveTcp;
        }
        else if (projection.TcpType == SdpIceTcpType.SimultaneousOpen)
        {
            expected = SdpIceTokens.SimultaneousOpenTcp;
        }
        if (expected.Length > 0)
        {
            if (projection.TcpTypeToken.Length == 0 || string.Equals(projection.TcpTypeToken, expected, StringComparison.OrdinalIgnoreCase))
            {
                return expected;
            }
            return string.Empty;
        }
        if (projection.TcpTypeToken.Length == 0)
        {
            return string.Empty;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.TcpTypeToken) || ReadTcpType(projection.TcpTypeToken) != SdpIceTcpType.Unknown)
        {
            return string.Empty;
        }
        return projection.TcpTypeToken;
    }

    internal static SdpAddressKind ReadCandidateAddressKind(string address)
    {
        if (SdpConnectionLineParser.IsValidIpv4Address(address))
        {
            int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(address);
            return firstOctet >= 224 ? SdpAddressKind.Ipv4Multicast : SdpAddressKind.Ipv4;
        }
        if (SdpConnectionLineParser.IsValidIpv6Address(address))
        {
            return SdpConnectionLineParser.IsIpv6MulticastAddress(address) ? SdpAddressKind.Ipv6Multicast : SdpAddressKind.Ipv6;
        }
        if (SdpConnectionLineParser.IsValidFqdnAddress(address) && !SdpConnectionLineParser.LooksLikeIpv4Literal(address))
        {
            return SdpAddressKind.HostName;
        }
        return SdpAddressKind.Unknown;
    }

    internal static bool IsKnownExtensionName(string name)
    {
        return string.Equals(name, SdpIceTokens.RelatedAddress, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpIceTokens.RelatedPort, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpIceTokens.TcpType, StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpAttribute CreateCandidateValueAttribute(string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.Candidate;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.Candidate + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailCandidateDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateCandidateEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.Candidate, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
