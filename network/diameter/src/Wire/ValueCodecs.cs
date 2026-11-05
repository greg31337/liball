using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter;

internal static class ValueCodecs
{
    internal static DiameterValueResult EncodeInteger32(Span<byte> destination, int value)
    {
        DiameterValueResult result = ValidateDestination(destination, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        NetworkByteOrder.WriteInt32(destination, value);
        result.WrittenCount = 4;
        return result;
    }

    internal static DiameterValueResult DecodeInteger32(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        result.Integer32 = NetworkByteOrder.ReadInt32(source);
        result.ConsumedCount = 4;
        return result;
    }

    internal static DiameterValueResult EncodeEnumerated(Span<byte> destination, int value)
    {
        return EncodeInteger32(destination, value);
    }

    internal static DiameterValueResult DecodeEnumerated(ReadOnlySpan<byte> source)
    {
        return DecodeInteger32(source);
    }

    internal static DiameterValueResult EncodeInteger64(Span<byte> destination, long value)
    {
        DiameterValueResult result = ValidateDestination(destination, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        NetworkByteOrder.WriteInt64(destination, value);
        result.WrittenCount = 8;
        return result;
    }

    internal static DiameterValueResult DecodeInteger64(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        result.Integer64 = NetworkByteOrder.ReadInt64(source);
        result.ConsumedCount = 8;
        return result;
    }

    internal static DiameterValueResult EncodeUnsigned32(Span<byte> destination, uint value)
    {
        DiameterValueResult result = ValidateDestination(destination, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        NetworkByteOrder.WriteUInt32(destination, value);
        result.WrittenCount = 4;
        return result;
    }

    internal static DiameterValueResult DecodeUnsigned32(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        result.Unsigned32 = NetworkByteOrder.ReadUInt32(source);
        result.ConsumedCount = 4;
        return result;
    }

    internal static DiameterValueResult EncodeUnsigned64(Span<byte> destination, ulong value)
    {
        DiameterValueResult result = ValidateDestination(destination, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        NetworkByteOrder.WriteUInt64(destination, value);
        result.WrittenCount = 8;
        return result;
    }

    internal static DiameterValueResult DecodeUnsigned64(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        result.Unsigned64 = NetworkByteOrder.ReadUInt64(source);
        result.ConsumedCount = 8;
        return result;
    }

    internal static DiameterValueResult EncodeFloat32(Span<byte> destination, float value)
    {
        DiameterValueResult result = ValidateDestination(destination, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        int bits = BitConverter.SingleToInt32Bits(value);
        NetworkByteOrder.WriteInt32(destination, bits);
        result.WrittenCount = 4;
        return result;
    }

    internal static DiameterValueResult DecodeFloat32(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        int bits = NetworkByteOrder.ReadInt32(source);
        result.Float32 = BitConverter.Int32BitsToSingle(bits);
        result.ConsumedCount = 4;
        return result;
    }

    internal static DiameterValueResult EncodeFloat64(Span<byte> destination, double value)
    {
        DiameterValueResult result = ValidateDestination(destination, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        long bits = BitConverter.DoubleToInt64Bits(value);
        NetworkByteOrder.WriteInt64(destination, bits);
        result.WrittenCount = 8;
        return result;
    }

    internal static DiameterValueResult DecodeFloat64(ReadOnlySpan<byte> source)
    {
        DiameterValueResult result = ValidateSource(source, 8);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        long bits = NetworkByteOrder.ReadInt64(source);
        result.Float64 = BitConverter.Int64BitsToDouble(bits);
        result.ConsumedCount = 8;
        return result;
    }

    internal static DiameterValueResult EncodeTime(Span<byte> destination, DateTimeOffset value)
    {
        DateTimeOffset utcValue = value.ToUniversalTime();
        long unixSeconds = utcValue.ToUnixTimeSeconds();
        long ntpSeconds = unixSeconds + DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;
        if (ntpSeconds < 0L)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                4,
                0,
                "time-before-ntp-epoch",
                "A Diameter Time value cannot precede the NTP epoch.");
        }

        DiameterValueResult result = ValidateDestination(destination, 4);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        long foldedSeconds = ntpSeconds % DiameterProtocolConstants.NtpEraSeconds;
        NetworkByteOrder.WriteUInt32(destination, (uint)foldedSeconds);
        result.WrittenCount = 4;
        result.Time = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        return result;
    }

    internal static DiameterValueResult DecodeTime(
        ReadOnlySpan<byte> source,
        DateTimeOffset referenceTime)
    {
        if (source.Length < 4)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Incomplete,
                4,
                source.Length,
                "incomplete-time",
                "The source does not contain a complete Diameter Time value.");
        }

        if (source.Length > 4)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                4,
                4,
                "invalid-time-length",
                "A Diameter Time value must contain exactly four octets.");
        }

        long referenceUnixSeconds = referenceTime.ToUniversalTime().ToUnixTimeSeconds();
        long referenceNtpSeconds = referenceUnixSeconds +
            DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;
        if (referenceNtpSeconds < 0L)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                4,
                0,
                "reference-before-ntp-epoch",
                "The Time decoding reference cannot precede the NTP epoch.");
        }

        uint encodedSeconds = NetworkByteOrder.ReadUInt32(source);
        long era = referenceNtpSeconds / DiameterProtocolConstants.NtpEraSeconds;
        long candidateNtpSeconds = era * DiameterProtocolConstants.NtpEraSeconds +
            encodedSeconds;
        long difference = candidateNtpSeconds - referenceNtpSeconds;
        long halfEraSeconds = DiameterProtocolConstants.NtpEraSeconds / 2L;
        if (difference >= halfEraSeconds)
        {
            candidateNtpSeconds = candidateNtpSeconds - DiameterProtocolConstants.NtpEraSeconds;
        }
        else if (difference < -halfEraSeconds)
        {
            candidateNtpSeconds = candidateNtpSeconds + DiameterProtocolConstants.NtpEraSeconds;
        }

        if (candidateNtpSeconds < 0L)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                4,
                0,
                "time-before-ntp-epoch",
                "The decoded Diameter Time value precedes the NTP epoch.");
        }

        long unixSeconds = candidateNtpSeconds -
            DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;
        long minimumUnixSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();
        long maximumUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        if (unixSeconds < minimumUnixSeconds || unixSeconds > maximumUnixSeconds)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                4,
                0,
                "time-out-of-range",
                "The decoded Diameter Time value is outside the supported date range.");
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = 4;
        result.ConsumedCount = 4;
        result.Unsigned32 = encodedSeconds;
        result.Time = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        return result;
    }

    internal static DiameterValueResult EncodeOctetString(
        Span<byte> destination,
        ReadOnlySpan<byte> value)
    {
        DiameterValueResult result = ValidateDestination(destination, value.Length);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        value.CopyTo(destination);
        result.WrittenCount = value.Length;
        return result;
    }

    internal static DiameterValueResult DecodeOctetString(ReadOnlySpan<byte> source)
    {
        byte[] data = new byte[source.Length];
        source.CopyTo(data);

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = source.Length;
        result.ConsumedCount = source.Length;
        result.Data = data;
        return result;
    }

    internal static DiameterValueResult EncodeIpFilterRule(
        Span<byte> destination,
        string? value)
    {
        return EncodeAsciiFilterRule(destination, value, false);
    }

    internal static DiameterValueResult DecodeIpFilterRule(ReadOnlySpan<byte> source)
    {
        return DecodeAsciiFilterRule(source, false);
    }

    internal static DiameterValueResult EncodeQosFilterRule(
        Span<byte> destination,
        string? value)
    {
        return EncodeAsciiFilterRule(destination, value, true);
    }

    internal static DiameterValueResult DecodeQosFilterRule(ReadOnlySpan<byte> source)
    {
        return DecodeAsciiFilterRule(source, true);
    }

    internal static DiameterValueResult EncodeAsciiFilterRule(
        Span<byte> destination,
        string? value,
        bool qos)
    {
        DiameterValueResult validation;
        if (qos)
        {
            validation = FilterRuleCodec.ValidateQosFilterRule(value);
        }
        else
        {
            validation = FilterRuleCodec.ValidateIpFilterRule(value);
        }

        if (validation.Status != DiameterCodecStatus.Success)
        {
            return validation;
        }

        DiameterValueResult result = ValidateDestination(destination, validation.RequiredCount);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        int index = 0;
        while (index < validation.Text.Length)
        {
            destination[index] = (byte)validation.Text[index];
            index = index + 1;
        }

        result.WrittenCount = validation.RequiredCount;
        result.Text = validation.Text;
        return result;
    }

    internal static DiameterValueResult DecodeAsciiFilterRule(
        ReadOnlySpan<byte> source,
        bool qos)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] > 127)
            {
                string code = "invalid-ip-filter-rule";
                string message = "The IPFilterRule must contain only US-ASCII characters.";
                if (qos)
                {
                    code = "invalid-qos-filter-rule";
                    message = "The QoSFilterRule must contain only US-ASCII characters.";
                }

                return CreateValueFailure(
                    DiameterCodecStatus.Invalid,
                    source.Length,
                    index,
                    code,
                    message);
            }

            index = index + 1;
        }

        string value = Encoding.ASCII.GetString(source);
        DiameterValueResult result;
        if (qos)
        {
            result = FilterRuleCodec.ValidateQosFilterRule(value);
        }
        else
        {
            result = FilterRuleCodec.ValidateIpFilterRule(value);
        }

        if (result.Status == DiameterCodecStatus.Success)
        {
            result.ConsumedCount = source.Length;
        }

        return result;
    }

    internal static DiameterValueResult MeasureGrouped(
        List<DiameterAvp>? avps,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        return GroupedValueCodec.MeasureGroupedValue(avps, groupedTypes, limits);
    }

    internal static DiameterValueResult EncodeGrouped(
        Span<byte> destination,
        List<DiameterAvp>? avps,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        return GroupedValueCodec.EncodeGroupedValue(
            destination,
            avps,
            groupedTypes,
            limits);
    }

    internal static DiameterValueResult DecodeGrouped(
        ReadOnlySpan<byte> source,
        List<GroupedAvpType>? groupedTypes,
        DiameterLimitConfiguration? limits)
    {
        return GroupedValueCodec.DecodeGroupedValue(source, groupedTypes, limits);
    }

    internal static DiameterValueResult EncodeAddress(Span<byte> destination, IPAddress? address)
    {
        if (address == null)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "missing-address",
                "An IP address is required.");
        }

        int family = 0;
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            family = 1;
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            family = 2;
        }
        else
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "unsupported-address-family",
                "The IP address family cannot be represented by this codec.");
        }

        byte[] addressBytes = address.GetAddressBytes();
        int requiredCount = addressBytes.Length + 2;
        DiameterValueResult result = ValidateDestination(destination, requiredCount);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        destination[0] = 0;
        destination[1] = (byte)family;
        int index = 0;
        while (index < addressBytes.Length)
        {
            destination[index + 2] = addressBytes[index];
            index = index + 1;
        }

        result.WrittenCount = requiredCount;
        result.Address = address;
        return result;
    }

    internal static DiameterValueResult DecodeAddress(ReadOnlySpan<byte> source)
    {
        if (source.Length < 2)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Incomplete,
                2,
                source.Length,
                "incomplete-address-family",
                "The source does not contain an Address Family value.");
        }

        int family = (source[0] << 8) | source[1];
        int requiredCount = 0;
        if (family == 1)
        {
            requiredCount = 6;
        }
        else if (family == 2)
        {
            requiredCount = 18;
        }
        else
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                source.Length,
                0,
                "unsupported-address-family",
                "The Address Family value is not IPv4 or IPv6.");
        }

        if (source.Length < requiredCount)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Incomplete,
                requiredCount,
                source.Length,
                "incomplete-address",
                "The source does not contain the complete IP address.");
        }

        if (source.Length > requiredCount)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                requiredCount,
                requiredCount,
                "invalid-address-length",
                "The Address value contains trailing octets.");
        }

        byte[] addressBytes = new byte[requiredCount - 2];
        int index = 0;
        while (index < addressBytes.Length)
        {
            addressBytes[index] = source[index + 2];
            index = index + 1;
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        result.ConsumedCount = requiredCount;
        result.Address = new IPAddress(addressBytes);
        return result;
    }

    internal static DiameterValueResult NormalizeDiameterUri(DiameterUri? uri)
    {
        if (uri == null || uri.Host == null || uri.Scheme == null ||
            uri.Transport == null || uri.Protocol == null)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-diameter-uri",
                "A Diameter URI and host identity are required.");
        }

        string scheme = uri.Scheme.ToLowerInvariant();
        if (scheme != "aaa" && scheme != "aaas")
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-uri-scheme",
                "The Diameter URI scheme must be aaa or aaas.");
        }

        DiameterValueResult normalizedHost = NormalizeDiameterIdentity(uri.Host);
        if (normalizedHost.Status != DiameterCodecStatus.Success)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                6,
                "invalid-uri-host",
                "The Diameter URI host is not a valid FQDN.");
        }

        if (uri.Port < 1 || uri.Port > 65535)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-uri-port",
                "The Diameter URI port must be between 1 and 65535.");
        }

        string transport = uri.Transport.ToLowerInvariant();
        if (transport != "tcp" && transport != "sctp" && transport != "udp")
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-uri-transport",
                "The Diameter URI transport must be tcp, sctp, or udp.");
        }

        string protocol = uri.Protocol.ToLowerInvariant();
        if (protocol != "diameter" && protocol != "radius" && protocol != "tacacs+")
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-uri-protocol",
                "The Diameter URI protocol must be diameter, radius, or tacacs+.");
        }

        if (transport == "udp" && protocol == "diameter")
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-uri-transport-protocol",
                "UDP cannot carry the Diameter AAA protocol.");
        }

        int defaultPort = DiameterProtocolConstants.Port;
        if (scheme == "aaas")
        {
            defaultPort = DiameterProtocolConstants.SecurePort;
        }

        string text = scheme + "://" + normalizedHost.Text;
        if (uri.Port != defaultPort)
        {
            text = $"{text}:{uri.Port}";
        }

        if (transport != "tcp")
        {
            text = text + ";transport=" + transport;
        }

        if (protocol != "diameter")
        {
            text = text + ";protocol=" + protocol;
        }

        DiameterUri normalizedUri = new DiameterUri();
        normalizedUri.Scheme = scheme;
        normalizedUri.Host.Value = normalizedHost.Text;
        normalizedUri.Port = uri.Port;
        normalizedUri.Transport = transport;
        normalizedUri.Protocol = protocol;

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = text.Length;
        result.Text = text;
        result.Uri = normalizedUri;
        return result;
    }

    internal static DiameterValueResult EncodeDiameterUri(
        Span<byte> destination,
        DiameterUri? uri)
    {
        DiameterValueResult normalized = NormalizeDiameterUri(uri);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            return normalized;
        }

        DiameterValueResult result = EncodeUtf8String(destination, normalized.Text);
        if (result.Status == DiameterCodecStatus.Success)
        {
            result.Text = normalized.Text;
            result.Uri = normalized.Uri;
        }

        return result;
    }

    internal static DiameterValueResult DecodeDiameterUri(ReadOnlySpan<byte> source)
    {
        DiameterValueResult decoded = DecodeUtf8String(source);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return decoded;
        }

        DiameterValueResult result = ParseDiameterUri(decoded.Text);
        result.RequiredCount = source.Length;
        if (result.Status == DiameterCodecStatus.Success)
        {
            result.ConsumedCount = source.Length;
        }

        return result;
    }

    internal static DiameterValueResult ParseDiameterUri(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "invalid-diameter-uri",
                "A Diameter URI value is required.");
        }

        int schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                value.Length,
                0,
                "invalid-uri-scheme",
                "The Diameter URI does not contain a valid scheme separator.");
        }

        string scheme = value.Substring(0, schemeEnd).ToLowerInvariant();
        if (scheme != "aaa" && scheme != "aaas")
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                value.Length,
                0,
                "invalid-uri-scheme",
                "The Diameter URI scheme must be aaa or aaas.");
        }

        int authorityStart = schemeEnd + 3;
        int parameterStart = value.IndexOf(';', authorityStart);
        int authorityEnd = value.Length;
        if (parameterStart >= 0)
        {
            authorityEnd = parameterStart;
        }

        if (authorityEnd <= authorityStart)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                value.Length,
                authorityStart,
                "invalid-uri-host",
                "The Diameter URI host is missing.");
        }

        string authority = value.Substring(authorityStart, authorityEnd - authorityStart);
        int colonIndex = authority.IndexOf(':');
        string hostText = authority;
        int defaultPort = DiameterProtocolConstants.Port;
        if (scheme == "aaas")
        {
            defaultPort = DiameterProtocolConstants.SecurePort;
        }

        int port = defaultPort;
        if (colonIndex >= 0)
        {
            if (authority.IndexOf(':', colonIndex + 1) >= 0 || colonIndex == authority.Length - 1)
            {
                return CreateValueFailure(
                    DiameterCodecStatus.Invalid,
                    value.Length,
                    authorityStart + colonIndex,
                    "invalid-uri-port",
                    "The Diameter URI port is invalid.");
            }

            hostText = authority.Substring(0, colonIndex);
            string portText = authority.Substring(colonIndex + 1);
            port = 0;
            int portIndex = 0;
            while (portIndex < portText.Length)
            {
                char digitCharacter = portText[portIndex];
                if (digitCharacter < '0' || digitCharacter > '9')
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        value.Length,
                        authorityStart + colonIndex + 1 + portIndex,
                        "invalid-uri-port",
                        "The Diameter URI port contains a nondigit.");
                }

                int digit = digitCharacter - '0';
                if (port > (65535 - digit) / 10)
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        value.Length,
                        authorityStart + colonIndex + 1 + portIndex,
                        "invalid-uri-port",
                        "The Diameter URI port exceeds 65535.");
                }

                port = port * 10 + digit;
                portIndex = portIndex + 1;
            }

            if (port == 0)
            {
                return CreateValueFailure(
                    DiameterCodecStatus.Invalid,
                    value.Length,
                    authorityStart + colonIndex + 1,
                    "invalid-uri-port",
                    "The Diameter URI port cannot be zero.");
            }
        }

        DiameterIdentity host = new DiameterIdentity();
        host.Value = hostText;
        DiameterValueResult normalizedHost = NormalizeDiameterIdentity(host);
        if (normalizedHost.Status != DiameterCodecStatus.Success)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                value.Length,
                authorityStart,
                "invalid-uri-host",
                "The Diameter URI host is not a valid FQDN.");
        }

        string transport = "tcp";
        string protocol = "diameter";
        bool sawTransport = false;
        bool sawProtocol = false;
        while (parameterStart >= 0)
        {
            int parameterValueStart = parameterStart + 1;
            int nextParameter = value.IndexOf(';', parameterValueStart);
            int parameterEnd = value.Length;
            if (nextParameter >= 0)
            {
                parameterEnd = nextParameter;
            }

            string parameter = value.Substring(
                parameterValueStart,
                parameterEnd - parameterValueStart);
            if (parameter.StartsWith("transport=", StringComparison.OrdinalIgnoreCase) &&
                !sawTransport && !sawProtocol)
            {
                transport = parameter.Substring(10).ToLowerInvariant();
                sawTransport = true;
            }
            else if (parameter.StartsWith("protocol=", StringComparison.OrdinalIgnoreCase) &&
                !sawProtocol)
            {
                protocol = parameter.Substring(9).ToLowerInvariant();
                sawProtocol = true;
            }
            else
            {
                return CreateValueFailure(
                    DiameterCodecStatus.Invalid,
                    value.Length,
                    parameterStart,
                    "invalid-uri-parameter",
                    "The Diameter URI contains an unknown, duplicate, or out-of-order parameter.");
            }

            parameterStart = nextParameter;
        }

        DiameterUri uri = new DiameterUri();
        uri.Scheme = scheme;
        uri.Host.Value = normalizedHost.Text;
        uri.Port = port;
        uri.Transport = transport;
        uri.Protocol = protocol;
        return NormalizeDiameterUri(uri);
    }

    internal static DiameterValueResult EncodeUtf8String(Span<byte> destination, string? value)
    {
        if (value == null)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                "missing-utf8-string",
                "A UTF8String value is required.");
        }

        int invalidIndex = FindInvalidUtf16Index(value);
        if (invalidIndex >= 0)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                invalidIndex,
                "invalid-utf8-string",
                "The string contains a null code point or an invalid surrogate sequence.");
        }

        int requiredCount = Encoding.UTF8.GetByteCount(value);
        DiameterValueResult result = ValidateDestination(destination, requiredCount);
        if (result.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        result.WrittenCount = Encoding.UTF8.GetBytes(value, destination);
        return result;
    }

    internal static DiameterValueResult DecodeUtf8String(ReadOnlySpan<byte> source)
    {
        int invalidIndex = FindInvalidUtf8Index(source);
        if (invalidIndex >= 0)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                source.Length,
                invalidIndex,
                "invalid-utf8",
                "The source contains an invalid or prohibited UTF-8 sequence.");
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = source.Length;
        result.ConsumedCount = source.Length;
        result.Text = Encoding.UTF8.GetString(source);
        return result;
    }

    internal static DiameterValueResult NormalizeDiameterIdentity(DiameterIdentity? identity)
    {
        if (identity == null)
        {
            return NormalizeAsciiDomain(
                null,
                "invalid-diameter-identity",
                "A Diameter identity is required.");
        }

        return NormalizeAsciiDomain(
            identity.Value,
            "invalid-diameter-identity",
            "The Diameter identity is not a valid ASCII domain name.");
    }

    internal static DiameterValueResult NormalizeDiameterRealm(DiameterIdentity? realm)
    {
        if (realm == null)
        {
            return NormalizeAsciiDomain(
                null,
                "invalid-diameter-realm",
                "A Diameter realm is required.");
        }

        return NormalizeAsciiDomain(
            realm.Value,
            "invalid-diameter-realm",
            "The Diameter realm is not a valid ASCII domain name.");
    }

    internal static DiameterValueResult EncodeDiameterIdentity(
        Span<byte> destination,
        DiameterIdentity? identity)
    {
        DiameterValueResult normalized = NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            return normalized;
        }

        DiameterValueResult result = EncodeUtf8String(destination, normalized.Text);
        if (result.Status == DiameterCodecStatus.Success)
        {
            result.Text = normalized.Text;
            result.Identity = normalized.Identity;
        }

        return result;
    }

    internal static DiameterValueResult DecodeDiameterIdentity(ReadOnlySpan<byte> source)
    {
        DiameterValueResult decoded = DecodeUtf8String(source);
        if (decoded.Status != DiameterCodecStatus.Success)
        {
            return decoded;
        }

        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = decoded.Text;
        DiameterValueResult result = NormalizeDiameterIdentity(identity);
        result.RequiredCount = source.Length;
        if (result.Status == DiameterCodecStatus.Success)
        {
            result.ConsumedCount = source.Length;
        }

        return result;
    }

    internal static bool AreDiameterIdentitiesEqual(
        DiameterIdentity? left,
        DiameterIdentity? right)
    {
        DiameterValueResult normalizedLeft = NormalizeDiameterIdentity(left);
        DiameterValueResult normalizedRight = NormalizeDiameterIdentity(right);
        if (normalizedLeft.Status != DiameterCodecStatus.Success ||
            normalizedRight.Status != DiameterCodecStatus.Success)
        {
            return false;
        }

        return string.Equals(normalizedLeft.Text, normalizedRight.Text, StringComparison.Ordinal);
    }

    internal static bool AreDiameterRealmsEqual(
        DiameterIdentity? left,
        DiameterIdentity? right)
    {
        DiameterValueResult normalizedLeft = NormalizeDiameterRealm(left);
        DiameterValueResult normalizedRight = NormalizeDiameterRealm(right);
        if (normalizedLeft.Status != DiameterCodecStatus.Success ||
            normalizedRight.Status != DiameterCodecStatus.Success)
        {
            return false;
        }

        return string.Equals(normalizedLeft.Text, normalizedRight.Text, StringComparison.Ordinal);
    }

    internal static int FindInvalidUtf16Index(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character == 0)
            {
                return index;
            }

            if (character >= 55296 && character <= 56319)
            {
                if (value.Length - index < 2)
                {
                    return index;
                }

                int lowSurrogate = value[index + 1];
                if (lowSurrogate < 56320 || lowSurrogate > 57343)
                {
                    return index;
                }

                index = index + 2;
                continue;
            }

            if (character >= 56320 && character <= 57343)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindInvalidUtf8Index(ReadOnlySpan<byte> source)
    {
        int index = 0;
        while (index < source.Length)
        {
            byte first = source[index];
            if (first == 0)
            {
                return index;
            }

            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (source.Length - index < 2 || !IsUtf8Continuation(source[index + 1]))
                {
                    return index;
                }

                index = index + 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (source.Length - index < 3 || !IsUtf8Continuation(source[index + 2]))
                {
                    return index;
                }

                byte second = source[index + 1];
                bool validSecond = IsUtf8Continuation(second);
                if (first == 224)
                {
                    validSecond = second >= 160 && second <= 191;
                }
                else if (first == 237)
                {
                    validSecond = second >= 128 && second <= 159;
                }

                if (!validSecond)
                {
                    return index;
                }

                index = index + 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (source.Length - index < 4 ||
                    !IsUtf8Continuation(source[index + 2]) ||
                    !IsUtf8Continuation(source[index + 3]))
                {
                    return index;
                }

                byte second = source[index + 1];
                bool validSecond = IsUtf8Continuation(second);
                if (first == 240)
                {
                    validSecond = second >= 144 && second <= 191;
                }
                else if (first == 244)
                {
                    validSecond = second >= 128 && second <= 143;
                }

                if (!validSecond)
                {
                    return index;
                }

                index = index + 4;
                continue;
            }

            return index;
        }

        return -1;
    }

    internal static bool IsUtf8Continuation(byte value)
    {
        return value >= 128 && value <= 191;
    }

    internal static DiameterValueResult NormalizeAsciiDomain(
        string? value,
        string failureCode,
        string failureMessage)
    {
        if (value == null || value.Length == 0)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                0,
                failureCode,
                failureMessage);
        }

        string normalized = value.ToLowerInvariant();
        if (normalized[normalized.Length - 1] == '.')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        if (normalized.Length == 0 || normalized.Length > 253)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                normalized.Length,
                failureCode,
                failureMessage);
        }

        int labelStart = 0;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '.')
            {
                int labelLength = index - labelStart;
                if (labelLength == 0 || labelLength > 63 || normalized[index - 1] == '-')
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        0,
                        index,
                        failureCode,
                        failureMessage);
                }

                labelStart = index + 1;
            }
            else
            {
                bool letter = character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                if (!letter && !digit && character != '-')
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        0,
                        index,
                        failureCode,
                        failureMessage);
                }

                if (character == '-' && index == labelStart)
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        0,
                        index,
                        failureCode,
                        failureMessage);
                }

                if (index - labelStart + 1 > 63)
                {
                    return CreateValueFailure(
                        DiameterCodecStatus.Invalid,
                        0,
                        index,
                        failureCode,
                        failureMessage);
                }
            }

            index = index + 1;
        }

        if (labelStart == normalized.Length || normalized[normalized.Length - 1] == '-')
        {
            return CreateValueFailure(
                DiameterCodecStatus.Invalid,
                0,
                normalized.Length - 1,
                failureCode,
                failureMessage);
        }

        DiameterIdentity normalizedIdentity = new DiameterIdentity();
        normalizedIdentity.Value = normalized;
        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = normalized.Length;
        result.Text = normalized;
        result.Identity = normalizedIdentity;
        return result;
    }

    internal static DiameterValueResult ValidateDestination(Span<byte> destination, int requiredCount)
    {
        if (destination.Length < requiredCount)
        {
            return CreateValueFailure(
                DiameterCodecStatus.DestinationTooSmall,
                requiredCount,
                0,
                "destination-too-small",
                "The destination cannot hold the encoded Diameter value.");
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        return result;
    }

    internal static DiameterValueResult ValidateSource(ReadOnlySpan<byte> source, int requiredCount)
    {
        if (source.Length < requiredCount)
        {
            return CreateValueFailure(
                DiameterCodecStatus.Incomplete,
                requiredCount,
                source.Length,
                "incomplete-value",
                "The source does not contain the complete Diameter value.");
        }

        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = requiredCount;
        return result;
    }

    internal static DiameterValueResult CreateValueFailure(
        DiameterCodecStatus status,
        int requiredCount,
        int offset,
        string code,
        string message)
    {
        DiameterCodecIssue issue = new DiameterCodecIssue();
        if (status == DiameterCodecStatus.Incomplete)
        {
            issue.Severity = DiameterIssueSeverity.Information;
        }
        else
        {
            issue.Severity = DiameterIssueSeverity.Error;
        }

        issue.Status = status;
        issue.Offset = offset;
        issue.Code = code;
        issue.Message = message;

        DiameterValueResult result = new DiameterValueResult();
        result.Status = status;
        result.RequiredCount = requiredCount;
        result.Issues.Add(issue);
        return result;
    }
}
