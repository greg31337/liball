using System;

namespace sdp;

internal static class SdpDtlsAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeSetup(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDtlsDecode(attribute, result, "The SDP setup attribute requires a colon-delimited value.");
        }
        string token = TrimDtlsWhitespace(attribute.Value);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(token))
        {
            return FailDtlsDecode(attribute, result, "The SDP setup role must use non-empty token syntax.");
        }

        SdpSetupAttribute projection = new SdpSetupAttribute();
        projection.SourceAttribute = attribute;
        projection.Role = ReadSetupRole(token);
        projection.RoleToken = token;
        result.SetupAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSetup(SdpSetupAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string token = ReadSetupEncodingToken(projection);
        if (token.Length == 0)
        {
            result.Diagnostics.Add(CreateDtlsEncodeDiagnostic(SdpAttributeNames.Setup, "The SDP setup role kind and token are invalid or inconsistent."));
            return result;
        }
        result.Attribute = CreateDtlsValueAttribute(SdpAttributeNames.Setup, token);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeConnectionRole(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDtlsDecode(attribute, result, "The SDP connection attribute requires a colon-delimited value.");
        }
        string token = TrimDtlsWhitespace(attribute.Value);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(token))
        {
            return FailDtlsDecode(attribute, result, "The SDP connection role must use non-empty token syntax.");
        }

        SdpConnectionAttribute projection = new SdpConnectionAttribute();
        projection.SourceAttribute = attribute;
        projection.ConnectionRole = token;
        result.ConnectionAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeConnectionRole(SdpConnectionAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string token = ReadConnectionEncodingToken(projection.ConnectionRole);
        if (token.Length == 0)
        {
            result.Diagnostics.Add(CreateDtlsEncodeDiagnostic(SdpAttributeNames.Connection, "The SDP connection role must use non-empty token syntax."));
            return result;
        }
        result.Attribute = CreateDtlsValueAttribute(SdpAttributeNames.Connection, token);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeTlsId(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDtlsDecode(attribute, result, "The SDP tls-id attribute requires a colon-delimited value.");
        }
        string id = TrimDtlsWhitespace(attribute.Value);
        if (!IsTlsIdValue(id))
        {
            return FailDtlsDecode(attribute, result, "The SDP tls-id value must contain 20 through 255 permitted identifier characters.");
        }

        SdpTlsIdAttribute projection = new SdpTlsIdAttribute();
        projection.SourceAttribute = attribute;
        projection.Id = id;
        result.TlsIdAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeTlsId(SdpTlsIdAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsTlsIdValue(projection.Id))
        {
            result.Diagnostics.Add(CreateDtlsEncodeDiagnostic(SdpAttributeNames.TlsId, "The SDP tls-id value must contain 20 through 255 permitted identifier characters."));
            return result;
        }
        result.Attribute = CreateDtlsValueAttribute(SdpAttributeNames.TlsId, projection.Id);
        result.Success = true;
        return result;
    }

    internal static SdpDtlsSetupRole ReadSetupRole(string token)
    {
        if (string.Equals(token, "active", StringComparison.OrdinalIgnoreCase))
        {
            return SdpDtlsSetupRole.Active;
        }
        if (string.Equals(token, "passive", StringComparison.OrdinalIgnoreCase))
        {
            return SdpDtlsSetupRole.Passive;
        }
        if (string.Equals(token, "actpass", StringComparison.OrdinalIgnoreCase))
        {
            return SdpDtlsSetupRole.ActPass;
        }
        if (string.Equals(token, "holdconn", StringComparison.OrdinalIgnoreCase))
        {
            return SdpDtlsSetupRole.HoldConnection;
        }
        return SdpDtlsSetupRole.Unknown;
    }

    internal static string ReadSetupEncodingToken(SdpSetupAttribute projection)
    {
        string expected = string.Empty;
        if (projection.Role == SdpDtlsSetupRole.Active)
        {
            expected = "active";
        }
        else if (projection.Role == SdpDtlsSetupRole.Passive)
        {
            expected = "passive";
        }
        else if (projection.Role == SdpDtlsSetupRole.ActPass)
        {
            expected = "actpass";
        }
        else if (projection.Role == SdpDtlsSetupRole.HoldConnection)
        {
            expected = "holdconn";
        }
        if (expected.Length > 0)
        {
            if (projection.RoleToken.Length == 0 || string.Equals(projection.RoleToken, expected, StringComparison.OrdinalIgnoreCase))
            {
                return expected;
            }
            return string.Empty;
        }
        if (!SdpCommonAttributeCodecCore.IsTokenValue(projection.RoleToken) || ReadSetupRole(projection.RoleToken) != SdpDtlsSetupRole.Unknown)
        {
            return string.Empty;
        }
        return projection.RoleToken;
    }

    internal static string ReadConnectionEncodingToken(string token)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(token))
        {
            return string.Empty;
        }
        if (string.Equals(token, "new", StringComparison.OrdinalIgnoreCase))
        {
            return "new";
        }
        if (string.Equals(token, "existing", StringComparison.OrdinalIgnoreCase))
        {
            return "existing";
        }
        return token;
    }

    internal static bool IsTlsIdValue(string value)
    {
        if (value.Length < 20 || value.Length > 255)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool allowed = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '+' || character == '/' || character == '-' || character == '_';
            if (!allowed)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string TrimDtlsWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t'))
        {
            start = start + 1;
        }
        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t'))
        {
            end = end - 1;
        }
        return value.Substring(start, end - start);
    }

    internal static SdpAttribute CreateDtlsValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailDtlsDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateDtlsEncodeDiagnostic(string name, string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(name, detail);
    }
}
