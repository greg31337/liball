using System;
using System.Globalization;

namespace sdp;

internal static class SdpCryptoAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeCrypto(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailCryptoDecode(attribute, result, "The SDP crypto attribute requires a colon-delimited value.");
        }

        string[] tokens = attribute.Value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3)
        {
            return FailCryptoDecode(attribute, result, "The SDP crypto attribute requires a tag, suite, and one or more key parameters.");
        }
        if (!IsCryptoTag(tokens[0]))
        {
            return FailCryptoDecode(attribute, result, "The SDP crypto tag must be a canonical decimal number containing one through nine digits.");
        }
        if (!IsCryptoName(tokens[1]))
        {
            return FailCryptoDecode(attribute, result, "The SDP crypto suite must contain only letters, digits, or underscores.");
        }

        string[] keyParameters = tokens[2].Split(';');
        if (keyParameters.Length == 0)
        {
            return FailCryptoDecode(attribute, result, "The SDP crypto attribute requires at least one valid key parameter.");
        }
        int keyIndex = 0;
        while (keyIndex < keyParameters.Length)
        {
            if (!IsKeyParameter(keyParameters[keyIndex]))
            {
                return FailCryptoDecode(attribute, result, "Every SDP crypto key parameter must contain a method and non-empty key information.");
            }
            keyIndex = keyIndex + 1;
        }

        int sessionIndex = 3;
        while (sessionIndex < tokens.Length)
        {
            if (!IsSessionParameter(tokens[sessionIndex]))
            {
                return FailCryptoDecode(attribute, result, "Every SDP crypto session parameter must contain only visible characters.");
            }
            sessionIndex = sessionIndex + 1;
        }

        SdpCryptoAttribute projection = new SdpCryptoAttribute();
        projection.SourceAttribute = attribute;
        projection.Tag = ReadCryptoTag(tokens[0]);
        projection.Suite = tokens[1];
        keyIndex = 0;
        while (keyIndex < keyParameters.Length)
        {
            projection.KeyParameters.Add(keyParameters[keyIndex]);
            keyIndex = keyIndex + 1;
        }
        sessionIndex = 3;
        while (sessionIndex < tokens.Length)
        {
            projection.SessionParameters.Add(tokens[sessionIndex]);
            sessionIndex = sessionIndex + 1;
        }
        result.CryptoAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeCrypto(SdpCryptoAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Tag < 0 || projection.Tag > 999999999)
        {
            result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("The SDP crypto tag must be from zero through 999999999."));
            return result;
        }
        if (!IsCryptoName(projection.Suite))
        {
            result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("The SDP crypto suite must contain only letters, digits, or underscores."));
            return result;
        }
        if (projection.KeyParameters == null || projection.KeyParameters.Count == 0)
        {
            result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("The SDP crypto attribute requires at least one valid key parameter."));
            return result;
        }

        string keyText = string.Empty;
        int keyIndex = 0;
        while (keyIndex < projection.KeyParameters.Count)
        {
            string keyParameter = projection.KeyParameters[keyIndex];
            if (keyParameter == null || !IsKeyParameter(keyParameter))
            {
                result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("Every SDP crypto key parameter must contain a method and non-empty key information."));
                return result;
            }
            if (keyIndex > 0)
            {
                keyText = keyText + ";";
            }
            keyText = keyText + keyParameter;
            keyIndex = keyIndex + 1;
        }

        string sessionText = string.Empty;
        if (projection.SessionParameters == null)
        {
            result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("The SDP crypto session-parameter collection must be available."));
            return result;
        }
        int sessionIndex = 0;
        while (sessionIndex < projection.SessionParameters.Count)
        {
            string sessionParameter = projection.SessionParameters[sessionIndex];
            if (sessionParameter == null || !IsSessionParameter(sessionParameter))
            {
                result.Diagnostics.Add(CreateCryptoEncodeDiagnostic("Every SDP crypto session parameter must contain only visible characters."));
                return result;
            }
            sessionText = sessionText + " " + sessionParameter;
            sessionIndex = sessionIndex + 1;
        }

        string value = projection.Tag.ToString(CultureInfo.InvariantCulture) + " " + projection.Suite + " " + keyText + sessionText;
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.Crypto;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.Crypto + ":" + value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static bool IsCryptoTag(string value)
    {
        if (value.Length == 0 || value.Length > 9 || value.Length > 1 && value[0] == '0')
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static int ReadCryptoTag(string value)
    {
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            result = result * 10 + value[index] - '0';
            index = index + 1;
        }
        return result;
    }

    internal static bool IsCryptoName(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool allowed = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '_';
            if (!allowed)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsKeyParameter(string value)
    {
        int colonIndex = value.IndexOf(':');
        if (colonIndex <= 0 || colonIndex == value.Length - 1)
        {
            return false;
        }
        string method = value.Substring(0, colonIndex);
        if (!IsCryptoName(method))
        {
            return false;
        }
        int index = colonIndex + 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '!' || character > '~' || character == ';')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsSessionParameter(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
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

    internal static SdpAttributeDecodeResult FailCryptoDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateCryptoEncodeDiagnostic(string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.Crypto, detail);
    }
}
