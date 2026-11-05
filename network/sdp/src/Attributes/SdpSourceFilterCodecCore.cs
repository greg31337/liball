using System;

namespace sdp;

internal static class SdpSourceFilterCodecCore
{
    internal static SdpAttributeDecodeResult DecodeSourceFilter(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailSourceFilterDecode(attribute, result, "The SDP source-filter attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailSourceFilterDecode(attribute, result, "The SDP source-filter value must contain a filter mode, address specification, and source list.");
        }

        string[] tokens = ReadSourceFilterTokens(attribute.Value);
        if (tokens.Length < 5)
        {
            return FailSourceFilterDecode(attribute, result, "The SDP source-filter value requires a filter mode, network type, address type, destination, and at least one source.");
        }
        if (tokens[0] != "incl" && tokens[0] != "excl")
        {
            return FailSourceFilterDecode(attribute, result, "The SDP source-filter mode must be incl or excl with the specified case.");
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(tokens[1]) || tokens[2] != "*" && !SdpCommonAttributeCodecCore.IsTokenValue(tokens[2]))
        {
            return FailSourceFilterDecode(attribute, result, "The SDP source-filter network and address types must use token syntax.");
        }
        if (!IsValidFilterAddress(tokens[2], tokens[3], true))
        {
            return FailSourceFilterConnectionDecode(attribute, result, "The SDP source-filter destination does not match its declared address type.");
        }

        SdpSourceFilterAttribute projection = new SdpSourceFilterAttribute();
        projection.SourceAttribute = attribute;
        projection.FilterMode = tokens[0];
        projection.NetworkType = tokens[1];
        projection.AddressType = tokens[2];
        projection.DestinationAddress = tokens[3];
        projection.AddressKind = ReadFilterAddressKind(tokens[2], tokens[3]);
        int index = 4;
        while (index < tokens.Length)
        {
            if (!IsValidFilterAddress(tokens[2], tokens[index], false))
            {
                return FailSourceFilterConnectionDecode(attribute, result, "Every SDP source-filter source must be a unicast address matching the declared address type.");
            }
            projection.SourceAddresses.Add(tokens[index]);
            index = index + 1;
        }

        result.SourceFilterAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSourceFilter(SdpSourceFilterAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.FilterMode != "incl" && projection.FilterMode != "excl")
        {
            result.Diagnostics.Add(CreateSourceFilterEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP source-filter mode must be incl or excl with the specified case."));
            return result;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.NetworkType) || projection.AddressType != "*" && !SdpCommonAttributeCodecCore.IsTokenValue(projection.AddressType))
        {
            result.Diagnostics.Add(CreateSourceFilterEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP source-filter network and address types must use token syntax."));
            return result;
        }
        if (projection.SourceAddresses.Count == 0)
        {
            result.Diagnostics.Add(CreateSourceFilterEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP source-filter value requires at least one source address."));
            return result;
        }
        if (!IsValidFilterAddress(projection.AddressType, projection.DestinationAddress, true))
        {
            result.Diagnostics.Add(CreateSourceFilterEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "The SDP source-filter destination does not match its declared address type."));
            return result;
        }

        string value = projection.FilterMode + " " + projection.NetworkType + " " + projection.AddressType + " " + projection.DestinationAddress;
        int index = 0;
        while (index < projection.SourceAddresses.Count)
        {
            string source = projection.SourceAddresses[index];
            if (!IsValidFilterAddress(projection.AddressType, source, false))
            {
                result.Diagnostics.Add(CreateSourceFilterEncodeDiagnostic(SdpDiagnosticCodes.ValidationConnection, "Every SDP source-filter source must be a unicast address matching the declared address type."));
                return result;
            }
            value = value + " " + source;
            index = index + 1;
        }

        result.Attribute = CreateSourceFilterValueAttribute(value);
        result.Success = true;
        return result;
    }

    internal static bool IsValidFilterAddress(string addressType, string address, bool destination)
    {
        if (!SdpCommonAttributeCodecCore.IsNonWhitespaceValue(address))
        {
            return false;
        }
        if (address == "*")
        {
            return destination && addressType != "*";
        }
        if (address.IndexOf('/') >= 0)
        {
            return false;
        }
        if (addressType == SdpAddressTypes.Ip4)
        {
            if (SdpConnectionLineParser.IsValidIpv4Address(address))
            {
                int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(address);
                if (firstOctet >= 240)
                {
                    return false;
                }
                return destination || firstOctet < 224;
            }
            return SdpConnectionLineParser.IsValidFqdnAddress(address) && !SdpConnectionLineParser.LooksLikeIpv4Literal(address);
        }
        if (addressType == SdpAddressTypes.Ip6)
        {
            if (SdpConnectionLineParser.IsValidIpv6Address(address))
            {
                return destination || !SdpConnectionLineParser.IsIpv6MulticastAddress(address);
            }
            return SdpConnectionLineParser.IsValidFqdnAddress(address) && !SdpConnectionLineParser.LooksLikeIpv4Literal(address);
        }
        if (addressType == "*")
        {
            return SdpConnectionLineParser.IsValidFqdnAddress(address) && !SdpConnectionLineParser.LooksLikeIpv4Literal(address);
        }
        return true;
    }

    internal static SdpAddressKind ReadFilterAddressKind(string addressType, string address)
    {
        if (addressType == SdpAddressTypes.Ip4)
        {
            if (SdpConnectionLineParser.IsValidIpv4Address(address))
            {
                int firstOctet = SdpConnectionLineParser.ReadIpv4FirstOctet(address);
                return firstOctet >= 224 ? SdpAddressKind.Ipv4Multicast : SdpAddressKind.Ipv4;
            }
            if (address != "*")
            {
                return SdpAddressKind.HostName;
            }
        }
        if (addressType == SdpAddressTypes.Ip6)
        {
            if (SdpConnectionLineParser.IsValidIpv6Address(address))
            {
                return SdpConnectionLineParser.IsIpv6MulticastAddress(address) ? SdpAddressKind.Ipv6Multicast : SdpAddressKind.Ipv6;
            }
            if (address != "*")
            {
                return SdpAddressKind.HostName;
            }
        }
        if (addressType == "*" && address != "*")
        {
            return SdpAddressKind.HostName;
        }
        return SdpAddressKind.Unknown;
    }

    internal static string[] ReadSourceFilterTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static SdpAttribute CreateSourceFilterValueAttribute(string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.SourceFilter;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.SourceFilter + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailSourceFilterDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpAttributeDecodeResult FailSourceFilterConnectionDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationConnection, detail);
    }

    internal static SdpDiagnostic CreateSourceFilterEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.SourceFilter, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
