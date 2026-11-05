using System;
using System.Globalization;

namespace sdp;

internal static class SdpRidAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeRid(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid value must not be empty or contain prohibited line-ending characters.");
        }

        string value = attribute.Value;
        int identifierEnd = value.IndexOf(' ');
        if (identifierEnd <= 0 || identifierEnd + 1 >= value.Length)
        {
            return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid value requires an identifier followed by a send or recv direction.");
        }
        string identifier = value.Substring(0, identifierEnd);
        if (!IsRidIdentifier(identifier))
        {
            return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid identifier must contain only letters, digits, hyphens, or underscores.");
        }

        int directionStart = identifierEnd + 1;
        int directionEnd = value.IndexOf(' ', directionStart);
        string directionText = directionEnd < 0 ? value.Substring(directionStart) : value.Substring(directionStart, directionEnd - directionStart);
        SdpDirection direction = ReadRidDirection(directionText);
        if (direction == SdpDirection.Unknown)
        {
            return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid direction must be send or recv.");
        }

        SdpRidAttribute projection = new SdpRidAttribute();
        projection.SourceAttribute = attribute;
        projection.Id = identifier;
        projection.Direction = direction;
        if (directionEnd >= 0)
        {
            int parametersStart = directionEnd + 1;
            if (parametersStart >= value.Length || !ParseRidParameters(value, parametersStart, projection))
            {
                return FailRidDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP rid parameter list must contain an optional leading pt list followed by valid semicolon-delimited restrictions.");
            }
        }

        result.RidAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static bool ParseRidParameters(string value, int start, SdpRidAttribute projection)
    {
        int segmentStart = start;
        int segmentIndex = 0;
        int index = start;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ';')
            {
                if (segmentStart == index)
                {
                    return false;
                }
                string segment = value.Substring(segmentStart, index - segmentStart);
                if (segmentIndex == 0 && segment.Length >= 3 && segment.Substring(0, 3) == "pt=")
                {
                    if (!ParseRidPayloads(segment.Substring(3), projection))
                    {
                        return false;
                    }
                }
                else if (!AddRidRestriction(segment, projection))
                {
                    return false;
                }
                segmentIndex = segmentIndex + 1;
                segmentStart = index + 1;
            }
            index = index + 1;
        }
        return segmentIndex > 0;
    }

    internal static bool ParseRidPayloads(string value, SdpRidAttribute projection)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int payloadStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ',')
            {
                if (payloadStart == index)
                {
                    return false;
                }
                string payloadText = value.Substring(payloadStart, index - payloadStart);
                if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(payloadText))
                {
                    return false;
                }
                SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(payloadText.AsSpan());
                if (!payload.Success)
                {
                    return false;
                }
                projection.PayloadTypes.Add(payload.Int32Value);
                payloadStart = index + 1;
            }
            index = index + 1;
        }
        return projection.PayloadTypes.Count > 0;
    }

    internal static bool AddRidRestriction(string text, SdpRidAttribute projection)
    {
        int equalsIndex = text.IndexOf('=');
        string name = equalsIndex < 0 ? text : text.Substring(0, equalsIndex);
        if (!IsRidRestrictionName(name) || name == "pt")
        {
            return false;
        }
        string restrictionValue = equalsIndex < 0 ? string.Empty : text.Substring(equalsIndex + 1);
        if (equalsIndex >= 0 && !IsRidRestrictionValue(restrictionValue))
        {
            return false;
        }

        SdpRidRestriction restriction = new SdpRidRestriction();
        restriction.Name = name;
        restriction.HasValue = equalsIndex >= 0;
        restriction.Value = restrictionValue;
        restriction.RawText = text;
        projection.Restrictions.Add(restriction);
        return true;
    }

    internal static SdpAttributeEncodeResult EncodeRid(SdpRidAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsRidIdentifier(projection.Id))
        {
            result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP rid identifier must contain only letters, digits, hyphens, or underscores."));
            return result;
        }
        string direction = ReadRidDirectionName(projection.Direction);
        if (direction.Length == 0)
        {
            result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP rid direction must be send or recv."));
            return result;
        }

        string value = projection.Id + " " + direction;
        if (projection.PayloadTypes.Count > 0)
        {
            string payloadList = string.Empty;
            int payloadIndex = 0;
            while (payloadIndex < projection.PayloadTypes.Count)
            {
                int payload = projection.PayloadTypes[payloadIndex];
                if (payload < 0 || payload > 127)
                {
                    result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationPayloadType, "Every SDP rid payload type must be from zero through 127."));
                    return result;
                }
                if (payloadIndex > 0)
                {
                    payloadList = payloadList + ",";
                }
                payloadList = payloadList + payload.ToString(CultureInfo.InvariantCulture);
                payloadIndex = payloadIndex + 1;
            }
            value = value + " pt=" + payloadList;
        }

        int restrictionIndex = 0;
        while (restrictionIndex < projection.Restrictions.Count)
        {
            SdpRidRestriction restriction = projection.Restrictions[restrictionIndex];
            if (restriction == null || !IsRidRestrictionName(restriction.Name) || restriction.Name == "pt")
            {
                result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "Every SDP rid restriction must have a valid non-pt parameter name."));
                return result;
            }
            if (!restriction.HasValue && restriction.Value.Length > 0)
            {
                result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "An SDP rid restriction value requires its equals delimiter to be enabled."));
                return result;
            }
            if (restriction.HasValue && !IsRidRestrictionValue(restriction.Value))
            {
                result.Diagnostics.Add(CreateRidEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "An SDP rid restriction value must contain printable ASCII text other than semicolons."));
                return result;
            }
            value = value + (projection.PayloadTypes.Count > 0 || restrictionIndex > 0 ? ";" : " ") + restriction.Name;
            if (restriction.HasValue)
            {
                value = value + "=" + restriction.Value;
            }
            restrictionIndex = restrictionIndex + 1;
        }

        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.Rid;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.Rid + ":" + value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static bool IsRidIdentifier(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!(character >= 'A' && character <= 'Z') && !(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9') && character != '-' && character != '_')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsRidRestrictionName(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!(character >= 'A' && character <= 'Z') && !(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9') && character != '-')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsRidRestrictionValue(string value)
    {
        if (value == null)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < ' ' || character > '~' || character == ';')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static SdpDirection ReadRidDirection(string value)
    {
        if (value == "send")
        {
            return SdpDirection.SendOnly;
        }
        if (value == "recv")
        {
            return SdpDirection.ReceiveOnly;
        }
        return SdpDirection.Unknown;
    }

    internal static string ReadRidDirectionName(SdpDirection direction)
    {
        if (direction == SdpDirection.SendOnly)
        {
            return "send";
        }
        if (direction == SdpDirection.ReceiveOnly)
        {
            return "recv";
        }
        return string.Empty;
    }

    internal static SdpAttributeDecodeResult FailRidDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateRidEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.Rid, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
