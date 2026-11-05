using System;
using System.Globalization;

namespace sdp;

internal static class SdpSctpAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeSctpPort(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP sctp-port attribute requires a colon-delimited value.");
        }
        SdpNumberParseResult port = ReadCanonicalPort(attribute.Value);
        if (!port.Success)
        {
            return FailNumberDecode(attribute, result, port, "The SDP sctp-port value must be a canonical decimal port from zero through 65535.");
        }
        SdpSctpPortAttribute projection = new SdpSctpPortAttribute();
        projection.SourceAttribute = attribute;
        projection.Port = port.Int32Value;
        result.SctpPortAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeMaximumMessageSize(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP max-message-size attribute requires a colon-delimited value.");
        }
        SdpNumberParseResult size = ReadCanonicalLong(attribute.Value);
        if (!size.Success)
        {
            return FailNumberDecode(attribute, result, size, "The SDP max-message-size value must be a canonical nonnegative decimal integer in the supported range.");
        }
        SdpMaximumMessageSizeAttribute projection = new SdpMaximumMessageSizeAttribute();
        projection.SourceAttribute = attribute;
        projection.MaximumMessageSize = size.SignedValue;
        result.MaximumMessageSizeAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeSctpMap(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The legacy SDP sctpmap attribute requires a colon-delimited value.");
        }
        string[] tokens = SdpRtcpAttributeCodecCore.ReadWhitespaceTokens(attribute.Value);
        if (tokens.Length != 2 && tokens.Length != 3)
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The legacy SDP sctpmap value requires a port, an application format, and an optional incoming-stream count.");
        }
        SdpNumberParseResult port = ReadCanonicalPort(tokens[0]);
        if (!port.Success)
        {
            return FailNumberDecode(attribute, result, port, "The legacy SDP sctpmap port must be a canonical decimal port from zero through 65535.");
        }
        if (!IsLegacyFormat(tokens[1]))
        {
            return FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The legacy SDP sctpmap application format must be a non-whitespace value.");
        }
        SdpNumberParseResult streams = new SdpNumberParseResult();
        if (tokens.Length == 3)
        {
            streams = ReadCanonicalStreamCount(tokens[2]);
            if (!streams.Success)
            {
                return FailNumberDecode(attribute, result, streams, "The legacy SDP sctpmap incoming-stream count must be a canonical decimal integer from one through 65535.");
            }
        }

        SdpSctpMapAttribute projection = new SdpSctpMapAttribute();
        projection.SourceAttribute = attribute;
        projection.Port = port.Int32Value;
        projection.Format = tokens[1];
        projection.HasMaximumStreams = tokens.Length == 3;
        if (projection.HasMaximumStreams)
        {
            projection.MaximumStreams = streams.Int32Value;
        }
        result.SctpMapAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSctpPort(SdpSctpPortAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Port < 0 || projection.Port > 65535)
        {
            result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.SctpPort, "The SDP sctp-port value must be from zero through 65535."));
            return result;
        }
        result.Attribute = CreateValueAttribute(SdpAttributeNames.SctpPort, projection.Port.ToString(CultureInfo.InvariantCulture));
        result.Success = true;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMaximumMessageSize(SdpMaximumMessageSizeAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.MaximumMessageSize < 0)
        {
            result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.MaximumMessageSize, "The SDP max-message-size value must be nonnegative."));
            return result;
        }
        result.Attribute = CreateValueAttribute(SdpAttributeNames.MaximumMessageSize, projection.MaximumMessageSize.ToString(CultureInfo.InvariantCulture));
        result.Success = true;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSctpMap(SdpSctpMapAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Port < 0 || projection.Port > 65535)
        {
            result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.SctpMap, "The legacy SDP sctpmap port must be from zero through 65535."));
            return result;
        }
        if (!IsLegacyFormat(projection.Format))
        {
            result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.SctpMap, "The legacy SDP sctpmap application format must be a non-whitespace value."));
            return result;
        }
        if (projection.HasMaximumStreams && (projection.MaximumStreams < 1 || projection.MaximumStreams > 65535))
        {
            result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.SctpMap, "The legacy SDP sctpmap incoming-stream count must be from one through 65535."));
            return result;
        }
        string value = projection.Port.ToString(CultureInfo.InvariantCulture) + " " + projection.Format;
        if (projection.HasMaximumStreams)
        {
            value = value + " " + projection.MaximumStreams.ToString(CultureInfo.InvariantCulture);
        }
        result.Attribute = CreateValueAttribute(SdpAttributeNames.SctpMap, value);
        result.Success = true;
        return result;
    }

    internal static SdpSctpDescription ResolveMediaSctp(SdpMediaDescription media)
    {
        SdpSctpDescription projection = new SdpSctpDescription();
        projection.MaximumMessageSize = SdpDataChannelTokens.DefaultMaximumMessageSize;
        if (media.Formats.Count > 0)
        {
            projection.Format = media.Formats[0];
        }
        int index = 0;
        while (index < media.Attributes.Count)
        {
            SdpAttribute attribute = media.Attributes[index];
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            if (decoded.Success && projection.PortAttribute == null && decoded.SctpPortAttribute != null)
            {
                projection.PortAttribute = CopySctpPort(decoded.SctpPortAttribute);
            }
            else if (decoded.Success && projection.MaximumMessageSizeAttribute == null && decoded.MaximumMessageSizeAttribute != null)
            {
                projection.MaximumMessageSizeAttribute = CopyMaximumMessageSize(decoded.MaximumMessageSizeAttribute);
            }
            else if (decoded.Success && projection.SctpMapAttribute == null && decoded.SctpMapAttribute != null)
            {
                projection.SctpMapAttribute = CopySctpMap(decoded.SctpMapAttribute);
            }
            index = index + 1;
        }
        if (projection.PortAttribute != null)
        {
            projection.Port = projection.PortAttribute.Port;
        }
        else if (projection.SctpMapAttribute != null)
        {
            projection.Port = projection.SctpMapAttribute.Port;
            projection.Format = projection.SctpMapAttribute.Format;
            projection.LegacySctpMap = true;
            projection.HasMaximumStreams = projection.SctpMapAttribute.HasMaximumStreams;
            projection.MaximumStreams = projection.SctpMapAttribute.MaximumStreams;
        }
        if (projection.MaximumMessageSizeAttribute != null)
        {
            projection.HasMaximumMessageSize = true;
            projection.MaximumMessageSize = projection.MaximumMessageSizeAttribute.MaximumMessageSize;
        }
        return projection;
    }

    internal static SdpSctpPortAttribute CopySctpPort(SdpSctpPortAttribute source)
    {
        SdpSctpPortAttribute result = new SdpSctpPortAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Port = source.Port;
        return result;
    }

    internal static SdpMaximumMessageSizeAttribute CopyMaximumMessageSize(SdpMaximumMessageSizeAttribute source)
    {
        SdpMaximumMessageSizeAttribute result = new SdpMaximumMessageSizeAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.MaximumMessageSize = source.MaximumMessageSize;
        return result;
    }

    internal static SdpSctpMapAttribute CopySctpMap(SdpSctpMapAttribute source)
    {
        SdpSctpMapAttribute result = new SdpSctpMapAttribute();
        result.SourceAttribute = source.SourceAttribute;
        result.Port = source.Port;
        result.Format = source.Format;
        result.HasMaximumStreams = source.HasMaximumStreams;
        result.MaximumStreams = source.MaximumStreams;
        return result;
    }

    internal static SdpNumberParseResult ReadCanonicalPort(string value)
    {
        SdpNumberParseResult result = SdpNumberParser.ParseCharacterPort(value.AsSpan());
        if (result.Success && !SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            SetInvalidNumber(result);
        }
        return result;
    }

    internal static SdpNumberParseResult ReadCanonicalLong(string value)
    {
        SdpNumberParseResult result = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        if (result.Success && result.UnsignedValue > 9223372036854775807UL)
        {
            SetOverflowNumber(result);
        }
        else if (result.Success && !SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            SetInvalidNumber(result);
        }
        else if (result.Success)
        {
            result.SignedValue = (long)result.UnsignedValue;
        }
        return result;
    }

    internal static SdpNumberParseResult ReadCanonicalStreamCount(string value)
    {
        SdpNumberParseResult result = SdpNumberParser.ParseCharacterPort(value.AsSpan());
        if (result.Success && (result.Int32Value == 0 || !SdpCommonAttributeCodecCore.IsPositiveIntegerValue(value)))
        {
            SetInvalidNumber(result);
        }
        return result;
    }

    internal static bool IsLegacyFormat(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsNonWhitespaceCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static void SetInvalidNumber(SdpNumberParseResult result)
    {
        result.Success = false;
        result.Invalid = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseInvalidNumber;
    }

    internal static void SetOverflowNumber(SdpNumberParseResult result)
    {
        result.Success = false;
        result.Overflow = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseNumberOverflow;
    }

    internal static SdpAttributeDecodeResult FailNumberDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, SdpNumberParseResult number, string detail)
    {
        string code = number.DiagnosticCode.Length == 0 ? SdpDiagnosticCodes.ValidationAttribute : number.DiagnosticCode;
        return FailDecode(attribute, result, code, detail);
    }

    internal static SdpAttributeDecodeResult FailDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
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
}
