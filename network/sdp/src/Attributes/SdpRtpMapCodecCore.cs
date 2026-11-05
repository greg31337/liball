using System;
using System.Globalization;

namespace sdp;

internal static class SdpRtpMapCodecCore
{
    internal static SdpAttributeDecodeResult DecodeRtpMap(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap attribute requires a colon-delimited value.");
        }

        string value = attribute.Value;
        int spaceIndex = value.IndexOf(' ');
        if (spaceIndex <= 0 || spaceIndex + 1 >= value.Length || value.IndexOf(' ', spaceIndex + 1) >= 0)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap value requires one payload token, one space, and one encoding description.");
        }
        string payloadText = value.Substring(0, spaceIndex);
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(payloadText))
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP rtpmap payload type must be a canonical integer from zero through 127.");
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(payloadText.AsSpan());
        if (!payload.Success)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP rtpmap payload type must be a canonical integer from zero through 127.");
        }

        string encodingText = value.Substring(spaceIndex + 1);
        int firstSlash = encodingText.IndexOf('/');
        if (firstSlash <= 0 || firstSlash + 1 >= encodingText.Length)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding description requires an encoding name and clock rate.");
        }
        int secondSlash = encodingText.IndexOf('/', firstSlash + 1);
        if (secondSlash >= 0 && secondSlash + 1 >= encodingText.Length || secondSlash >= 0 && encodingText.IndexOf('/', secondSlash + 1) >= 0)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding description contains an invalid encoding-parameter form.");
        }

        string encodingName = encodingText.Substring(0, firstSlash);
        int clockEnd = secondSlash < 0 ? encodingText.Length : secondSlash;
        string clockText = encodingText.Substring(firstSlash + 1, clockEnd - firstSlash - 1);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(encodingName) || !SdpCommonAttributeCodecCore.IsPositiveIntegerValue(clockText))
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding name must be a token and its clock rate must be a positive canonical integer.");
        }
        SdpNumberParseResult clockRate = SdpNumberParser.ParseCharacterUnsignedDecimal(clockText.AsSpan());
        if (!clockRate.Success || clockRate.UnsignedValue > int.MaxValue)
        {
            return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap clock rate exceeds the supported positive integer range.");
        }

        SdpRtpMapAttribute projection = new SdpRtpMapAttribute();
        projection.SourceAttribute = attribute;
        projection.PayloadType = payload.Int32Value;
        projection.EncodingName = encodingName;
        projection.ClockRate = (int)clockRate.UnsignedValue;
        if (secondSlash >= 0)
        {
            string parameters = encodingText.Substring(secondSlash + 1);
            if (!SdpCommonAttributeCodecCore.IsTokenValue(parameters))
            {
                return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding parameter must use non-empty token syntax.");
            }
            projection.HasEncodingParameters = true;
            projection.EncodingParameters = parameters;
            if (IsDecimalParameter(parameters))
            {
                if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(parameters))
                {
                    return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "A numeric SDP rtpmap channel count must be a positive canonical integer.");
                }
                SdpNumberParseResult channelCount = SdpNumberParser.ParseCharacterUnsignedDecimal(parameters.AsSpan());
                if (!channelCount.Success || channelCount.UnsignedValue > int.MaxValue)
                {
                    return FailRtpMapDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap channel count exceeds the supported positive integer range.");
                }
                projection.HasChannelCount = true;
                projection.ChannelCount = (int)channelCount.UnsignedValue;
            }
        }

        result.Success = true;
        result.RtpMapAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeRtpMap(SdpRtpMapAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.PayloadType < 0 || projection.PayloadType > 127)
        {
            result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationPayloadType, "The SDP rtpmap payload type must be from zero through 127."));
            return result;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.EncodingName) || projection.ClockRate <= 0)
        {
            result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding name must be a token and its clock rate must be positive."));
            return result;
        }
        if (projection.HasChannelCount && !projection.HasEncodingParameters)
        {
            result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "An SDP rtpmap channel count requires encoding parameters to be enabled."));
            return result;
        }

        string parameters = projection.EncodingParameters;
        if (projection.HasEncodingParameters && projection.HasChannelCount)
        {
            if (projection.ChannelCount <= 0)
            {
                result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap channel count must be positive."));
                return result;
            }
            parameters = projection.ChannelCount.ToString(CultureInfo.InvariantCulture);
        }
        else if (projection.HasEncodingParameters)
        {
            if (!SdpCommonAttributeCodecCore.IsTokenValue(parameters))
            {
                result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP rtpmap encoding parameter must use non-empty token syntax."));
                return result;
            }
            if (IsDecimalParameter(parameters) && !SdpCommonAttributeCodecCore.IsPositiveIntegerValue(parameters))
            {
                result.Diagnostics.Add(CreateRtpMapEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "A numeric SDP rtpmap channel count must be a positive canonical integer."));
                return result;
            }
        }

        string value = projection.PayloadType.ToString(CultureInfo.InvariantCulture) + " " + projection.EncodingName + "/" + projection.ClockRate.ToString(CultureInfo.InvariantCulture);
        if (projection.HasEncodingParameters)
        {
            value = value + "/" + parameters;
        }
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.RtpMap;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.RtpMap + ":" + value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static bool IsDecimalParameter(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static SdpAttributeDecodeResult FailRtpMapDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateRtpMapEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.RtpMap, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
