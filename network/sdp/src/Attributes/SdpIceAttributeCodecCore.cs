using System;

namespace sdp;

internal static class SdpIceAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeUsernameFragment(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailIceDecode(attribute, result, "The SDP ice-ufrag attribute requires a colon-delimited value.");
        }
        if (!IsIceCredentialValue(attribute.Value, 4, 256))
        {
            return FailIceDecode(attribute, result, "The SDP ice-ufrag value must contain 4 through 256 ICE characters.");
        }

        SdpIceUsernameFragmentAttribute projection = new SdpIceUsernameFragmentAttribute();
        projection.SourceAttribute = attribute;
        projection.UsernameFragment = attribute.Value;
        result.IceUsernameFragmentAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeUsernameFragment(SdpIceUsernameFragmentAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsIceCredentialValue(projection.UsernameFragment, 4, 32))
        {
            result.Diagnostics.Add(CreateIceEncodeDiagnostic(SdpAttributeNames.IceUsernameFragment, "The SDP ice-ufrag value sent by an endpoint must contain 4 through 32 ICE characters."));
            return result;
        }

        result.Attribute = CreateIceValueAttribute(SdpAttributeNames.IceUsernameFragment, projection.UsernameFragment);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodePassword(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailIceDecode(attribute, result, "The SDP ice-pwd attribute requires a colon-delimited value.");
        }
        if (!IsIceCredentialValue(attribute.Value, 22, 256))
        {
            return FailIceDecode(attribute, result, "The SDP ice-pwd value must contain 22 through 256 ICE characters.");
        }

        SdpIcePasswordAttribute projection = new SdpIcePasswordAttribute();
        projection.SourceAttribute = attribute;
        projection.Password = attribute.Value;
        result.IcePasswordAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodePassword(SdpIcePasswordAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsIceCredentialValue(projection.Password, 22, 256))
        {
            result.Diagnostics.Add(CreateIceEncodeDiagnostic(SdpAttributeNames.IcePassword, "The SDP ice-pwd value must contain 22 through 256 ICE characters."));
            return result;
        }

        result.Attribute = CreateIceValueAttribute(SdpAttributeNames.IcePassword, projection.Password);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeOptions(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailIceDecode(attribute, result, "The SDP ice-options attribute requires a colon-delimited value.");
        }

        string[] tokens = attribute.Value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return FailIceDecode(attribute, result, "The SDP ice-options value must contain at least one ICE option token.");
        }

        SdpIceOptionsAttribute projection = new SdpIceOptionsAttribute();
        projection.SourceAttribute = attribute;
        int index = 0;
        while (index < tokens.Length)
        {
            if (!IsIceOptionValue(tokens[index]))
            {
                return FailIceDecode(attribute, result, "Every SDP ice-options entry must contain only ICE characters.");
            }
            projection.Options.Add(tokens[index]);
            index = index + 1;
        }

        result.IceOptionsAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeOptions(SdpIceOptionsAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Options.Count == 0)
        {
            result.Diagnostics.Add(CreateIceEncodeDiagnostic(SdpAttributeNames.IceOptions, "The SDP ice-options value must contain at least one ICE option token."));
            return result;
        }

        string value = string.Empty;
        int index = 0;
        while (index < projection.Options.Count)
        {
            string option = projection.Options[index];
            if (!IsIceOptionValue(option))
            {
                result.Diagnostics.Add(CreateIceEncodeDiagnostic(SdpAttributeNames.IceOptions, "Every SDP ice-options entry must contain only ICE characters."));
                return result;
            }
            if (index > 0)
            {
                value = value + " ";
            }
            value = value + option;
            index = index + 1;
        }

        result.Attribute = CreateIceValueAttribute(SdpAttributeNames.IceOptions, value);
        result.Success = true;
        return result;
    }

    internal static bool IsIceCredentialValue(string value, int minimumLength, int maximumLength)
    {
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!IsIceCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsIceOptionValue(string value)
    {
        return IsIceCredentialValue(value, 1, int.MaxValue);
    }

    internal static bool IsIceCharacter(char character)
    {
        return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '+' || character == '/';
    }

    internal static SdpAttribute CreateIceValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailIceDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateIceEncodeDiagnostic(string attributeName, string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail);
    }
}
