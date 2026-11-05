using System;
using System.Collections.Generic;
using System.Text;

namespace dns;

internal class EdnsOptionPayloadResult
{
    internal bool Success;
    internal byte[] Bytes = [];
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class TypedEdnsOptionCodec
{
    internal static DnsEdnsOptionResult DecodeOption(
        ReadOnlySpan<byte> source,
        DnsDecodeOptions options)
    {
        DnsEdnsOptionResult opaque =
            OpaqueEdnsOptionCodec.DecodeOption(source, options);
        if (!opaque.Success || opaque.Option == null ||
            !options.DecodeTypedRecordData)
        {
            return opaque;
        }

        DnsEdnsOption option = opaque.Option;
        int codeValue = DnsHeaderCodec.ResolveCode(
            option.CodeValue, (int)option.Code);
        if (!IsTypedCode(codeValue))
            return opaque;

        DnsDiagnostic? diagnostic = DecodeTypedPayload(option, codeValue);
        if (diagnostic != null)
        {
            if (options.FallBackToOpaqueRecordData)
                return opaque;
            DnsEdnsOptionResult failed = new DnsEdnsOptionResult();
            failed.Diagnostics.Add(diagnostic);
            return failed;
        }

        option.UseRawData = false;
        DnsEdnsOptionResult result = new DnsEdnsOptionResult();
        result.Success = true;
        result.BytesProcessed = opaque.BytesProcessed;
        result.Option = option;
        return result;
    }

    internal static DnsEdnsOptionResult EncodeOption(DnsEdnsOption option)
    {
        if (option == null)
        {
            return OpaqueEdnsOptionCodec.OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "ednsOption",
                "An EDNS option is required.",
                0);
        }

        if (option.UseRawData)
            return OpaqueEdnsOptionCodec.EncodeOption(option);

        int codeValue = DnsHeaderCodec.ResolveCode(
            option.CodeValue, (int)option.Code);
        if (codeValue < 0 || codeValue > 65535)
        {
            return OpaqueEdnsOptionCodec.OptionFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "ednsOption.code",
                "The EDNS option code must fit in sixteen bits.",
                0);
        }

        if (!IsTypedCode(codeValue))
        {
            return OpaqueEdnsOptionCodec.OptionFailure(
                DnsDiagnosticCode.InvalidEdns,
                "ednsOption.data",
                "This EDNS option requires opaque raw-data mode.",
                0);
        }

        EdnsOptionPayloadResult payload =
            EncodeTypedPayload(option, codeValue);
        if (!payload.Success)
        {
            DnsEdnsOptionResult failed = new DnsEdnsOptionResult();
            int diagnosticIndex = 0;
            while (diagnosticIndex < payload.Diagnostics.Count)
            {
                failed.Diagnostics.Add(payload.Diagnostics[diagnosticIndex]);
                diagnosticIndex = diagnosticIndex + 1;
            }

            return failed;
        }

        byte[] bytes = new byte[4 + payload.Bytes.Length];
        DnsHeaderCodec.WriteUnsigned16(bytes, 0, codeValue);
        DnsHeaderCodec.WriteUnsigned16(bytes, 2, payload.Bytes.Length);
        Array.Copy(payload.Bytes, 0, bytes, 4, payload.Bytes.Length);
        DnsEdnsOptionResult result = new DnsEdnsOptionResult();
        result.Success = true;
        result.BytesProcessed = bytes.Length;
        result.Option = option;
        result.Bytes = bytes;
        return result;
    }

    static bool IsTypedCode(int codeValue)
    {
        return codeValue == 2 || codeValue == 3 || codeValue == 5 ||
            codeValue == 6 || codeValue == 7 || codeValue == 8 ||
            codeValue == 9 || codeValue == 10 ||
            codeValue == 11 || codeValue == 12 || codeValue == 13 ||
            codeValue == 14 || codeValue == 15 || codeValue == 18 ||
            codeValue == 19;
    }

    static DnsDiagnostic? DecodeTypedPayload(
        DnsEdnsOption option,
        int codeValue)
    {
        switch (codeValue)
        {
            case 2: return DecodeUpdateLease(option);
            case 3: return DecodeNameServerIdentifier(option);
            case 5: return DecodeDnssecAlgorithms(option);
            case 6: return DecodeDigestAlgorithms(option);
            case 7: return DecodeNsec3Algorithms(option);
            case 8: return DecodeClientSubnet(option);
            case 9: return DecodeExpire(option);
            case 10: return DecodeCookie(option);
            case 11: return DecodeKeepalive(option);
            case 12: return DecodePadding(option);
            case 13: return DecodeChain(option);
            case 14: return DecodeKeyTags(option);
            case 15: return DecodeExtendedError(option);
            case 18: return DecodeReportChannel(option);
            case 19: return DecodeZoneVersion(option);
            default:
                return CreateDiagnostic(
                    "ednsOption.code",
                    "No typed EDNS option codec is available for this code.",
                    0);
        }
    }

    static EdnsOptionPayloadResult EncodeTypedPayload(
        DnsEdnsOption option,
        int codeValue)
    {
        switch (codeValue)
        {
            case 2: return EncodeUpdateLease(option);
            case 3: return EncodeNameServerIdentifier(option);
            case 5: return EncodeDnssecAlgorithms(option);
            case 6: return EncodeDigestAlgorithms(option);
            case 7: return EncodeNsec3Algorithms(option);
            case 8: return EncodeClientSubnet(option);
            case 9: return EncodeExpire(option);
            case 10: return EncodeCookie(option);
            case 11: return EncodeKeepalive(option);
            case 12: return EncodePadding(option);
            case 13: return EncodeChain(option);
            case 14: return EncodeKeyTags(option);
            case 15: return EncodeExtendedError(option);
            case 18: return EncodeReportChannel(option);
            case 19: return EncodeZoneVersion(option);
            default:
                return PayloadFailure(
                    "ednsOption.code",
                    "No typed EDNS option codec is available for this code.",
                    0);
        }
    }

    static DnsDiagnostic? DecodeUpdateLease(DnsEdnsOption option)
    {
        if (option.RawData.Length != 4 && option.RawData.Length != 8)
        {
            return CreateDiagnostic(
                "ednsOption.updateLease.length",
                "An Update Lease option must contain four or eight octets.",
                2);
        }

        option.HasUpdateLease = true;
        option.UpdateLeaseSeconds = ReadUnsigned32(option.RawData, 0);
        if (option.RawData.Length == 8)
        {
            option.HasUpdateKeyLease = true;
            option.UpdateKeyLeaseSeconds = ReadUnsigned32(option.RawData, 4);
        }

        return null;
    }

    static DnsDiagnostic? DecodeNameServerIdentifier(DnsEdnsOption option)
    {
        option.NameServerIdentifier = CopyBytes(option.RawData);
        return null;
    }

    static DnsDiagnostic? DecodeDnssecAlgorithms(DnsEdnsOption option)
    {
        CopyOctetsToIntegers(
            option.RawData, option.DnssecAlgorithmValues);
        return null;
    }

    static DnsDiagnostic? DecodeDigestAlgorithms(DnsEdnsOption option)
    {
        CopyOctetsToIntegers(
            option.RawData, option.DigestAlgorithmValues);
        return null;
    }

    static DnsDiagnostic? DecodeNsec3Algorithms(DnsEdnsOption option)
    {
        CopyOctetsToIntegers(
            option.RawData, option.Nsec3HashAlgorithmValues);
        return null;
    }

    static DnsDiagnostic? DecodeClientSubnet(DnsEdnsOption option)
    {
        if (option.RawData.Length < 4)
        {
            return CreateDiagnostic(
                "ednsOption.clientSubnet.length",
                "A Client Subnet option requires a four-octet header.",
                2);
        }

        int family = ReadUnsigned16(option.RawData, 0);
        int addressBits = ClientSubnetAddressBits(family);
        if (addressBits == 0)
        {
            return CreateDiagnostic(
                "ednsOption.clientSubnet.addressFamily",
                "A Client Subnet address family must be IPv4 or IPv6.",
                4);
        }

        int sourcePrefixLength = option.RawData[2];
        int scopePrefixLength = option.RawData[3];
        if (sourcePrefixLength > addressBits)
        {
            return CreateDiagnostic(
                "ednsOption.clientSubnet.sourcePrefixLength",
                "The Client Subnet source prefix exceeds its address family.",
                6);
        }

        if (scopePrefixLength > addressBits)
        {
            return CreateDiagnostic(
                "ednsOption.clientSubnet.scopePrefixLength",
                "The Client Subnet scope prefix exceeds its address family.",
                7);
        }

        int addressLength = (sourcePrefixLength + 7) / 8;
        if (option.RawData.Length != 4 + addressLength)
        {
            return CreateDiagnostic(
                "ednsOption.clientSubnet.address",
                "The Client Subnet address must use the exact minimal prefix length.",
                8);
        }

        if (addressLength > 0 && sourcePrefixLength % 8 != 0)
        {
            int significantBits = sourcePrefixLength % 8;
            int hostMask = (1 << (8 - significantBits)) - 1;
            if ((option.RawData[option.RawData.Length - 1] & hostMask) != 0)
            {
                return CreateDiagnostic(
                    "ednsOption.clientSubnet.address",
                    "Client Subnet address bits beyond the source prefix must be zero.",
                    7 + addressLength);
            }
        }

        DnsEdnsClientSubnet subnet = new DnsEdnsClientSubnet();
        subnet.AddressFamily = family;
        subnet.SourcePrefixLength = sourcePrefixLength;
        subnet.ScopePrefixLength = scopePrefixLength;
        subnet.Address = new byte[addressLength];
        Array.Copy(option.RawData, 4, subnet.Address, 0, addressLength);
        option.ClientSubnet = subnet;
        return null;
    }

    static DnsDiagnostic? DecodeExpire(DnsEdnsOption option)
    {
        if (option.RawData.Length == 0)
            return null;
        if (option.RawData.Length != 4)
        {
            return CreateDiagnostic(
                "ednsOption.expire.length",
                "An EXPIRE option must contain zero or four octets.",
                2);
        }

        option.HasExpire = true;
        option.ExpireSeconds = ReadUnsigned32(option.RawData, 0);
        return null;
    }

    static DnsDiagnostic? DecodeCookie(DnsEdnsOption option)
    {
        int length = option.RawData.Length;
        if (length != 8 && (length < 16 || length > 40))
        {
            return CreateDiagnostic(
                "ednsOption.cookie.length",
                "A DNS Cookie must contain eight octets or between sixteen and forty octets.",
                2);
        }

        DnsEdnsCookie cookie = new DnsEdnsCookie();
        cookie.ClientCookie = new byte[8];
        Array.Copy(option.RawData, 0, cookie.ClientCookie, 0, 8);
        cookie.ServerCookie = new byte[length - 8];
        Array.Copy(
            option.RawData, 8, cookie.ServerCookie, 0, length - 8);
        option.Cookie = cookie;
        return null;
    }

    static DnsDiagnostic? DecodeKeepalive(DnsEdnsOption option)
    {
        if (option.RawData.Length != 0 && option.RawData.Length != 2)
        {
            return CreateDiagnostic(
                "ednsOption.keepalive.length",
                "A TCP Keepalive option must contain zero or two octets.",
                2);
        }

        option.Keepalive = new DnsEdnsKeepalive();
        if (option.RawData.Length == 2)
        {
            option.Keepalive.HasTimeout = true;
            option.Keepalive.TimeoutUnits =
                ReadUnsigned16(option.RawData, 0);
        }

        return null;
    }

    static DnsDiagnostic? DecodePadding(DnsEdnsOption option)
    {
        option.Padding = CopyBytes(option.RawData);
        return null;
    }

    static DnsDiagnostic? DecodeChain(DnsEdnsOption option)
    {
        if (option.RawData.Length == 0)
            return null;

        List<byte[]> labels = new List<byte[]>();
        int offset = 0;
        while (offset < option.RawData.Length)
        {
            int labelLength = option.RawData[offset];
            if ((labelLength & 192) != 0)
            {
                return CreateDiagnostic(
                    "ednsOption.chain.name",
                    "A CHAIN name cannot use DNS name compression.",
                    4 + offset);
            }

            if (labelLength == 0)
            {
                if (offset != option.RawData.Length - 1)
                {
                    return CreateDiagnostic(
                        "ednsOption.chain.length",
                        "Trailing octets follow the CHAIN name.",
                        4 + offset + 1);
                }

                RawDnsNameResult built =
                    RawDnsName.CreateAbsoluteName(labels);
                if (!built.Success || built.Name == null)
                {
                    return CreateDiagnostic(
                        "ednsOption.chain.name",
                        "The CHAIN option contains an invalid DNS name.",
                        4 + offset);
                }

                option.ChainName = built.Name;
                return null;
            }

            if (labelLength > 63 ||
                labelLength > option.RawData.Length - offset - 1)
            {
                return CreateDiagnostic(
                    "ednsOption.chain.name",
                    "The CHAIN option contains a truncated or invalid DNS label.",
                    4 + offset);
            }

            byte[] label = new byte[labelLength];
            Array.Copy(option.RawData, offset + 1, label, 0, labelLength);
            labels.Add(label);
            offset = offset + labelLength + 1;
        }

        return CreateDiagnostic(
            "ednsOption.chain.name",
            "A non-empty CHAIN name requires a terminating root label.",
            4 + option.RawData.Length);
    }

    static DnsDiagnostic? DecodeKeyTags(DnsEdnsOption option)
    {
        if (option.RawData.Length == 0 || option.RawData.Length % 2 != 0)
        {
            return CreateDiagnostic(
                "ednsOption.keyTag.length",
                "A Key Tag option requires one or more complete 16-bit values.",
                2);
        }

        int offset = 0;
        while (offset < option.RawData.Length)
        {
            option.KeyTagValues.Add(ReadUnsigned16(option.RawData, offset));
            offset = offset + 2;
        }

        return null;
    }

    static DnsDiagnostic? DecodeExtendedError(DnsEdnsOption option)
    {
        if (option.RawData.Length < 2)
        {
            return CreateDiagnostic(
                "ednsOption.extendedError.length",
                "An Extended DNS Error requires a two-octet information code.",
                2);
        }

        byte[] textBytes = new byte[option.RawData.Length - 2];
        Array.Copy(option.RawData, 2, textBytes, 0, textBytes.Length);
        if (!Utf8BytesAreValid(textBytes))
        {
            return CreateDiagnostic(
                "ednsOption.extendedError.extraText",
                "Extended DNS Error text must contain valid UTF-8.",
                6);
        }

        int code = ReadUnsigned16(option.RawData, 0);
        DnsEdnsExtendedError error = new DnsEdnsExtendedError();
        error.Code = (DnsExtendedErrorCode)code;
        error.CodeValue = code;
        if (textBytes.Length > 0)
            error.ExtraText = Encoding.UTF8.GetString(textBytes);
        option.ExtendedError = error;
        return null;
    }

    static DnsDiagnostic? DecodeReportChannel(DnsEdnsOption option)
    {
        DnsDecodeResult decoded =
            SharedRecordDataCodec.DecodeUncompressedName(
                option.RawData,
                0,
                option.RawData.Length,
                "ednsOption.reportChannel.agentDomain");
        if (!decoded.Success || decoded.Name == null)
        {
            if (decoded.Diagnostics.Count > 0)
            {
                DnsDiagnostic source = decoded.Diagnostics[0];
                int offset = 4;
                if (source.HasOffset) offset = offset + source.Offset;
                return CreateDiagnostic(
                    "ednsOption.reportChannel.agentDomain",
                    source.Reason,
                    offset);
            }

            return CreateDiagnostic(
                "ednsOption.reportChannel.agentDomain",
                "A Report Channel requires a complete agent domain.",
                4);
        }

        if (decoded.Name.Labels.Count == 0)
        {
            return CreateDiagnostic(
                "ednsOption.reportChannel.agentDomain",
                "A Report Channel agent domain cannot be the DNS root.",
                4);
        }

        option.ReportChannel = decoded.Name;
        return null;
    }

    static DnsDiagnostic? DecodeZoneVersion(DnsEdnsOption option)
    {
        DnsEdnsZoneVersion version = new DnsEdnsZoneVersion();
        if (option.RawData.Length == 0)
        {
            option.ZoneVersion = version;
            return null;
        }

        if (option.RawData.Length < 2)
        {
            return CreateDiagnostic(
                "ednsOption.zoneVersion.length",
                "A Zone Version response requires label-count and type octets.",
                2);
        }

        version.HasVersion = true;
        version.LabelCount = option.RawData[0];
        version.Type = option.RawData[1];
        version.Version = new byte[option.RawData.Length - 2];
        Array.Copy(option.RawData, 2, version.Version, 0, version.Version.Length);
        if (version.Type == 0 && version.Version.Length != 4)
        {
            return CreateDiagnostic(
                "ednsOption.zoneVersion.version",
                "A SOA-SERIAL Zone Version requires exactly four version octets.",
                6);
        }

        option.ZoneVersion = version;
        return null;
    }

    static EdnsOptionPayloadResult EncodeUpdateLease(DnsEdnsOption option)
    {
        if (!option.HasUpdateLease)
        {
            return PayloadFailure(
                "ednsOption.updateLease",
                "A typed Update Lease value is required.",
                4);
        }

        int length = option.HasUpdateKeyLease ? 8 : 4;
        byte[] bytes = new byte[length];
        WriteUnsigned32(bytes, 0, option.UpdateLeaseSeconds);
        if (option.HasUpdateKeyLease)
            WriteUnsigned32(bytes, 4, option.UpdateKeyLeaseSeconds);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeNameServerIdentifier(
        DnsEdnsOption option)
    {
        return EncodeByteArray(
            option.NameServerIdentifier,
            "ednsOption.nameServerIdentifier",
            "Name Server Identifier data is required.");
    }

    static EdnsOptionPayloadResult EncodeDnssecAlgorithms(
        DnsEdnsOption option)
    {
        return EncodeIntegerOctets(
            option.DnssecAlgorithmValues,
            "ednsOption.dnssecAlgorithms");
    }

    static EdnsOptionPayloadResult EncodeDigestAlgorithms(
        DnsEdnsOption option)
    {
        return EncodeIntegerOctets(
            option.DigestAlgorithmValues,
            "ednsOption.digestAlgorithms");
    }

    static EdnsOptionPayloadResult EncodeNsec3Algorithms(
        DnsEdnsOption option)
    {
        return EncodeIntegerOctets(
            option.Nsec3HashAlgorithmValues,
            "ednsOption.nsec3HashAlgorithms");
    }

    static EdnsOptionPayloadResult EncodeClientSubnet(DnsEdnsOption option)
    {
        if (option.ClientSubnet == null ||
            option.ClientSubnet.Address == null)
        {
            return PayloadFailure(
                "ednsOption.clientSubnet",
                "Typed Client Subnet values and address bytes are required.",
                4);
        }

        DnsEdnsClientSubnet subnet = option.ClientSubnet;
        int addressBits = ClientSubnetAddressBits(subnet.AddressFamily);
        if (addressBits == 0)
        {
            return PayloadFailure(
                "ednsOption.clientSubnet.addressFamily",
                "A Client Subnet address family must be IPv4 or IPv6.",
                4);
        }

        if (subnet.SourcePrefixLength < 0 ||
            subnet.SourcePrefixLength > addressBits)
        {
            return PayloadFailure(
                "ednsOption.clientSubnet.sourcePrefixLength",
                "The Client Subnet source prefix exceeds its address family.",
                6);
        }

        if (subnet.ScopePrefixLength < 0 ||
            subnet.ScopePrefixLength > addressBits)
        {
            return PayloadFailure(
                "ednsOption.clientSubnet.scopePrefixLength",
                "The Client Subnet scope prefix exceeds its address family.",
                7);
        }

        int addressLength = (subnet.SourcePrefixLength + 7) / 8;
        int completeAddressLength = addressBits / 8;
        if (subnet.Address.Length != addressLength &&
            subnet.Address.Length != completeAddressLength)
        {
            return PayloadFailure(
                "ednsOption.clientSubnet.address",
                "The Client Subnet address must contain its minimal prefix or a complete address.",
                8);
        }

        byte[] bytes = new byte[4 + addressLength];
        WriteUnsigned16(bytes, 0, subnet.AddressFamily);
        bytes[2] = (byte)subnet.SourcePrefixLength;
        bytes[3] = (byte)subnet.ScopePrefixLength;
        Array.Copy(subnet.Address, 0, bytes, 4, addressLength);
        if (addressLength > 0 && subnet.SourcePrefixLength % 8 != 0)
        {
            int significantBits = subnet.SourcePrefixLength % 8;
            int prefixMask = 255 << (8 - significantBits);
            bytes[bytes.Length - 1] =
                (byte)(bytes[bytes.Length - 1] & prefixMask);
        }

        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeExpire(DnsEdnsOption option)
    {
        if (!option.HasExpire)
            return PayloadSuccess([]);
        byte[] bytes = new byte[4];
        WriteUnsigned32(bytes, 0, option.ExpireSeconds);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeCookie(DnsEdnsOption option)
    {
        if (option.Cookie == null ||
            option.Cookie.ClientCookie == null ||
            option.Cookie.ServerCookie == null)
        {
            return PayloadFailure(
                "ednsOption.cookie",
                "Typed Client Cookie and Server Cookie byte arrays are required.",
                4);
        }

        DnsEdnsCookie cookie = option.Cookie;
        if (cookie.ClientCookie.Length != 8)
        {
            return PayloadFailure(
                "ednsOption.cookie.clientCookie",
                "A Client Cookie must contain exactly eight octets.",
                4);
        }

        int serverLength = cookie.ServerCookie.Length;
        if (serverLength != 0 &&
            (serverLength < 8 || serverLength > 32))
        {
            return PayloadFailure(
                "ednsOption.cookie.serverCookie",
                "A Server Cookie must be absent or contain between eight and thirty-two octets.",
                12);
        }

        byte[] bytes = new byte[8 + serverLength];
        Array.Copy(cookie.ClientCookie, 0, bytes, 0, 8);
        Array.Copy(cookie.ServerCookie, 0, bytes, 8, serverLength);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeKeepalive(DnsEdnsOption option)
    {
        if (option.Keepalive == null || !option.Keepalive.HasTimeout)
            return PayloadSuccess([]);
        if (option.Keepalive.TimeoutUnits < 0 ||
            option.Keepalive.TimeoutUnits > 65535)
        {
            return PayloadFailure(
                "ednsOption.keepalive.timeout",
                "The TCP Keepalive timeout must fit in sixteen bits.",
                4);
        }

        byte[] bytes = new byte[2];
        WriteUnsigned16(bytes, 0, option.Keepalive.TimeoutUnits);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodePadding(DnsEdnsOption option)
    {
        return EncodeByteArray(
            option.Padding,
            "ednsOption.padding",
            "EDNS padding data is required.");
    }

    static EdnsOptionPayloadResult EncodeChain(DnsEdnsOption option)
    {
        if (option.ChainName == null)
            return PayloadSuccess([]);
        RawDnsNameResult validation = RawDnsName.ValidateName(option.ChainName);
        if (!validation.Success || validation.Name == null ||
            !validation.Name.IsAbsolute)
        {
            return PayloadFailure(
                "ednsOption.chain.name",
                "A CHAIN trust point must be a valid absolute DNS name.",
                4);
        }

        DnsName name = validation.Name;
        byte[] bytes = new byte[validation.ExpandedWireLength];
        int offset = 0;
        int labelIndex = 0;
        while (labelIndex < name.Labels.Count)
        {
            byte[] label = name.Labels[labelIndex];
            bytes[offset] = (byte)label.Length;
            Array.Copy(label, 0, bytes, offset + 1, label.Length);
            offset = offset + label.Length + 1;
            labelIndex = labelIndex + 1;
        }

        bytes[offset] = 0;
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeKeyTags(DnsEdnsOption option)
    {
        if (option.KeyTagValues == null ||
            option.KeyTagValues.Count == 0 ||
            option.KeyTagValues.Count > 32767)
        {
            return PayloadFailure(
                "ednsOption.keyTags",
                "A Key Tag option requires between one and 32767 values.",
                4);
        }

        byte[] bytes = new byte[option.KeyTagValues.Count * 2];
        int index = 0;
        while (index < option.KeyTagValues.Count)
        {
            int value = option.KeyTagValues[index];
            if (value < 0 || value > 65535)
            {
                return PayloadFailure(
                    "ednsOption.keyTags",
                    "Every Key Tag value must fit in sixteen bits.",
                    4 + index * 2);
            }

            WriteUnsigned16(bytes, index * 2, value);
            index = index + 1;
        }

        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeExtendedError(
        DnsEdnsOption option)
    {
        if (option.ExtendedError == null)
        {
            return PayloadFailure(
                "ednsOption.extendedError",
                "Typed Extended DNS Error values are required.",
                4);
        }

        DnsEdnsExtendedError error = option.ExtendedError;
        int code = DnsHeaderCodec.ResolveCode(
            error.CodeValue, (int)error.Code);
        if (code < 0 || code > 65535)
        {
            return PayloadFailure(
                "ednsOption.extendedError.code",
                "The Extended DNS Error code must fit in sixteen bits.",
                4);
        }

        byte[] textBytes = [];
        if (error.ExtraText != null)
        {
            if (error.ExtraText.Length > 65533 ||
                !Utf16StringIsValid(error.ExtraText))
            {
                return PayloadFailure(
                    "ednsOption.extendedError.extraText",
                    "Extended DNS Error text must be valid Unicode and fit in the option.",
                    6);
            }

            textBytes = Encoding.UTF8.GetBytes(error.ExtraText);
            if (textBytes.Length > 65533)
            {
                return PayloadFailure(
                    "ednsOption.extendedError.extraText",
                    "Extended DNS Error text must fit in the option length field.",
                    6);
            }
        }

        byte[] bytes = new byte[2 + textBytes.Length];
        WriteUnsigned16(bytes, 0, code);
        Array.Copy(textBytes, 0, bytes, 2, textBytes.Length);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeReportChannel(
        DnsEdnsOption option)
    {
        if (option.ReportChannel == null)
        {
            return PayloadFailure(
                "ednsOption.reportChannel.agentDomain",
                "A Report Channel agent domain is required.",
                4);
        }

        RawDnsNameResult validation =
            RawDnsName.ValidateName(option.ReportChannel);
        if (!validation.Success || validation.Name == null ||
            !validation.Name.IsAbsolute || validation.Name.Labels.Count == 0)
        {
            return PayloadFailure(
                "ednsOption.reportChannel.agentDomain",
                "The Report Channel agent domain must be a non-root absolute DNS name.",
                4);
        }

        DnsName name = validation.Name;
        byte[] bytes = new byte[validation.ExpandedWireLength];
        int offset = 0;
        int labelIndex = 0;
        while (labelIndex < name.Labels.Count)
        {
            byte[] label = name.Labels[labelIndex];
            bytes[offset] = (byte)label.Length;
            Array.Copy(label, 0, bytes, offset + 1, label.Length);
            offset = offset + label.Length + 1;
            labelIndex = labelIndex + 1;
        }

        bytes[offset] = 0;
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeZoneVersion(DnsEdnsOption option)
    {
        if (option.ZoneVersion == null)
        {
            return PayloadFailure(
                "ednsOption.zoneVersion",
                "Typed Zone Version values are required.",
                4);
        }

        DnsEdnsZoneVersion version = option.ZoneVersion;
        if (!version.HasVersion)
            return PayloadSuccess([]);
        if (version.LabelCount < 0 || version.LabelCount > 255)
        {
            return PayloadFailure(
                "ednsOption.zoneVersion.labelCount",
                "The Zone Version label count must fit in one octet.",
                4);
        }

        if (version.Type < 0 || version.Type > 255)
        {
            return PayloadFailure(
                "ednsOption.zoneVersion.type",
                "The Zone Version type must fit in one octet.",
                5);
        }

        if (version.Version == null || version.Version.Length > 65533)
        {
            return PayloadFailure(
                "ednsOption.zoneVersion.version",
                "Zone Version data is required and must fit in the option.",
                6);
        }

        if (version.Type == 0 && version.Version.Length != 4)
        {
            return PayloadFailure(
                "ednsOption.zoneVersion.version",
                "A SOA-SERIAL Zone Version requires exactly four version octets.",
                6);
        }

        byte[] bytes = new byte[2 + version.Version.Length];
        bytes[0] = (byte)version.LabelCount;
        bytes[1] = (byte)version.Type;
        Array.Copy(version.Version, 0, bytes, 2, version.Version.Length);
        return PayloadSuccess(bytes);
    }

    static EdnsOptionPayloadResult EncodeByteArray(
        byte[] source,
        string field,
        string missingReason)
    {
        if (source == null)
            return PayloadFailure(field, missingReason, 4);
        if (source.Length > 65535)
        {
            return PayloadFailure(
                field,
                "The EDNS option payload must fit in sixteen bits.",
                4);
        }

        return PayloadSuccess(CopyBytes(source));
    }

    static EdnsOptionPayloadResult EncodeIntegerOctets(
        List<int> values,
        string field)
    {
        if (values == null || values.Count > 65535)
        {
            return PayloadFailure(
                field,
                "The algorithm list must fit in the EDNS option length field.",
                4);
        }

        byte[] bytes = new byte[values.Count];
        int index = 0;
        while (index < values.Count)
        {
            int value = values[index];
            if (value < 0 || value > 255)
            {
                return PayloadFailure(
                    field,
                    "Every EDNS algorithm value must fit in one octet.",
                    4 + index);
            }

            bytes[index] = (byte)value;
            index = index + 1;
        }

        return PayloadSuccess(bytes);
    }

    static void CopyOctetsToIntegers(byte[] source, List<int> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static bool Utf8BytesAreValid(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int first = value[index];
            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (!ContinuationByteIsValid(value, index + 1)) return false;
                index = index + 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index + 2 >= value.Length) return false;
                int second = value[index + 1];
                if ((first == 224 && (second < 160 || second > 191)) ||
                    (first == 237 && (second < 128 || second > 159)) ||
                    (first != 224 && first != 237 &&
                        (second < 128 || second > 191)) ||
                    !ContinuationByteIsValid(value, index + 2))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index + 3 >= value.Length) return false;
                int second = value[index + 1];
                if ((first == 240 && (second < 144 || second > 191)) ||
                    (first == 244 && (second < 128 || second > 143)) ||
                    (first != 240 && first != 244 &&
                        (second < 128 || second > 191)) ||
                    !ContinuationByteIsValid(value, index + 2) ||
                    !ContinuationByteIsValid(value, index + 3))
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            return false;
        }

        return true;
    }

    static bool ContinuationByteIsValid(byte[] value, int index)
    {
        return index < value.Length &&
            value[index] >= 128 && value[index] <= 191;
    }

    static bool Utf16StringIsValid(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 ||
                    value[index + 1] > 57343)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (character >= 56320 && character <= 57343)
                return false;
            index = index + 1;
        }

        return true;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, 0, result, 0, source.Length);
        return result;
    }

    static int ReadUnsigned16(byte[] source, int offset)
    {
        return (source[offset] << 8) | source[offset + 1];
    }

    static int ClientSubnetAddressBits(int family)
    {
        if (family == 1)
            return 32;
        if (family == 2)
            return 128;
        return 0;
    }

    static uint ReadUnsigned32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) |
            ((uint)source[offset + 1] << 16) |
            ((uint)source[offset + 2] << 8) |
            source[offset + 3];
    }

    static void WriteUnsigned16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)((value >> 8) & 255);
        destination[offset + 1] = (byte)(value & 255);
    }

    static void WriteUnsigned32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)((value >> 24) & 255);
        destination[offset + 1] = (byte)((value >> 16) & 255);
        destination[offset + 2] = (byte)((value >> 8) & 255);
        destination[offset + 3] = (byte)(value & 255);
    }

    static EdnsOptionPayloadResult PayloadSuccess(byte[] bytes)
    {
        EdnsOptionPayloadResult result = new EdnsOptionPayloadResult();
        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    static EdnsOptionPayloadResult PayloadFailure(
        string field,
        string reason,
        int offset)
    {
        EdnsOptionPayloadResult result = new EdnsOptionPayloadResult();
        result.Diagnostics.Add(CreateDiagnostic(field, reason, offset));
        return result;
    }

    static DnsDiagnostic CreateDiagnostic(
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidEdns;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        return diagnostic;
    }
}
