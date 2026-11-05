using System;
using System.Globalization;

namespace sdp;

internal static class SdpFormatParametersCodecCore
{
    internal static SdpAttributeDecodeResult DecodeFormatParameters(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP fmtp attribute requires a colon-delimited value.");
        }

        string value = attribute.Value;
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP fmtp value contains a prohibited NUL or line-ending character.");
        }
        int valueStart = SkipOptionalWhitespace(value, 0);
        int payloadEnd = valueStart;
        while (payloadEnd < value.Length && value[payloadEnd] != ' ' && value[payloadEnd] != '\t')
        {
            payloadEnd = payloadEnd + 1;
        }
        if (payloadEnd == valueStart || payloadEnd == value.Length)
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP fmtp value requires a payload token followed by format-specific parameters.");
        }
        string payloadText = value.Substring(valueStart, payloadEnd - valueStart);
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(payloadText))
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP fmtp payload type must be a canonical integer from zero through 127.");
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(payloadText.AsSpan());
        if (!payload.Success)
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP fmtp payload type must be a canonical integer from zero through 127.");
        }

        int parametersStart = SkipOptionalWhitespace(value, payloadEnd);
        if (parametersStart >= value.Length)
        {
            return FailFormatParametersDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP fmtp value must contain non-empty format-specific parameters.");
        }
        SdpFormatParametersAttribute projection = new SdpFormatParametersAttribute();
        projection.SourceAttribute = attribute;
        projection.PayloadType = payload.Int32Value;
        projection.RawParameters = value.Substring(parametersStart);
        ParseParameterList(projection);
        result.Success = true;
        result.FormatParametersAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static bool ParseParameterList(SdpFormatParametersAttribute projection)
    {
        string raw = projection.RawParameters;
        int segmentStart = 0;
        int index = 0;
        bool quoted = false;
        bool escaped = false;
        while (index <= raw.Length)
        {
            bool atEnd = index == raw.Length;
            char character = atEnd ? '\0' : raw[index];
            if (atEnd || character == ';' && !quoted)
            {
                if (!AddParsedParameter(projection, segmentStart, index))
                {
                    projection.Parameters.Clear();
                    return false;
                }
                segmentStart = index + 1;
            }
            else if (quoted && escaped)
            {
                escaped = false;
            }
            else if (quoted && character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            index = index + 1;
        }
        if (quoted)
        {
            projection.Parameters.Clear();
            return false;
        }
        return projection.Parameters.Count > 0;
    }

    internal static bool AddParsedParameter(SdpFormatParametersAttribute projection, int start, int end)
    {
        string raw = projection.RawParameters;
        int trimmedStart = SkipOptionalWhitespace(raw, start);
        int trimmedEnd = TrimOptionalWhitespaceEnd(raw, trimmedStart, end);
        if (trimmedStart >= trimmedEnd)
        {
            return false;
        }
        int equalsIndex = trimmedStart;
        while (equalsIndex < trimmedEnd && raw[equalsIndex] != '=')
        {
            equalsIndex = equalsIndex + 1;
        }
        int nameEnd = equalsIndex < trimmedEnd ? equalsIndex : trimmedEnd;
        nameEnd = TrimOptionalWhitespaceEnd(raw, trimmedStart, nameEnd);
        string name = raw.Substring(trimmedStart, nameEnd - trimmedStart);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
        {
            return false;
        }

        SdpFormatParameter parameter = new SdpFormatParameter();
        parameter.Name = name;
        parameter.RawText = raw.Substring(trimmedStart, trimmedEnd - trimmedStart);
        if (equalsIndex < trimmedEnd)
        {
            int parameterValueStart = SkipOptionalWhitespace(raw, equalsIndex + 1);
            if (parameterValueStart > trimmedEnd)
            {
                parameterValueStart = trimmedEnd;
            }
            int parameterValueEnd = TrimOptionalWhitespaceEnd(raw, parameterValueStart, trimmedEnd);
            parameter.HasValue = true;
            parameter.Value = raw.Substring(parameterValueStart, parameterValueEnd - parameterValueStart);
        }
        projection.Parameters.Add(parameter);
        return true;
    }

    internal static int SkipOptionalWhitespace(string value, int start)
    {
        int index = start;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t'))
        {
            index = index + 1;
        }
        return index;
    }

    internal static int TrimOptionalWhitespaceEnd(string value, int start, int end)
    {
        int index = end;
        while (index > start && (value[index - 1] == ' ' || value[index - 1] == '\t'))
        {
            index = index - 1;
        }
        return index;
    }

    internal static SdpAttributeEncodeResult EncodeFormatParameters(SdpFormatParametersAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.PayloadType < 0 || projection.PayloadType > 127)
        {
            result.Diagnostics.Add(CreateFormatParametersEncodeDiagnostic(SdpDiagnosticCodes.ValidationPayloadType, "The SDP fmtp payload type must be from zero through 127."));
            return result;
        }

        string parameters = projection.RawParameters;
        if (projection.Parameters.Count > 0)
        {
            parameters = string.Empty;
            int index = 0;
            while (index < projection.Parameters.Count)
            {
                SdpFormatParameter parameter = projection.Parameters[index];
                if (!SdpCommonAttributeCodecCore.IsTokenValue(parameter.Name))
                {
                    result.Diagnostics.Add(CreateFormatParametersEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "Every parsed SDP fmtp parameter must have a token name."));
                    return result;
                }
                if (parameter.HasValue && !IsSafeEncodedParameterValue(parameter.Value))
                {
                    result.Diagnostics.Add(CreateFormatParametersEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "An SDP fmtp parameter value contains unsafe delimiter or quotation syntax."));
                    return result;
                }
                if (index > 0)
                {
                    parameters = parameters + ";";
                }
                parameters = parameters + parameter.Name;
                if (parameter.HasValue)
                {
                    parameters = parameters + "=" + parameter.Value;
                }
                index = index + 1;
            }
        }
        else if (!SdpCommonAttributeCodecCore.IsByteStringValue(parameters))
        {
            result.Diagnostics.Add(CreateFormatParametersEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "An opaque SDP fmtp value must contain non-empty format-specific parameter data."));
            return result;
        }

        string value = projection.PayloadType.ToString(CultureInfo.InvariantCulture) + " " + parameters;
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.FormatParameters;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.FormatParameters + ":" + value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static bool IsSafeEncodedParameterValue(string value)
    {
        bool quoted = false;
        bool escaped = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!SdpCharacterClasses.IsByteStringCharacter(character))
            {
                return false;
            }
            if (quoted && escaped)
            {
                escaped = false;
            }
            else if (quoted && character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ';' && !quoted)
            {
                return false;
            }
            index = index + 1;
        }
        return !quoted;
    }

    internal static SdpAttributeDecodeResult FailFormatParametersDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateFormatParametersEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.FormatParameters, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
