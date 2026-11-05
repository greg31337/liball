using System.Globalization;

namespace sdp;

internal static class SdpExtMapAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeExtMap(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap value must not be empty or contain prohibited line-ending characters.");
        }

        int mapStart = SkipWhitespace(attribute.Value, 0);
        int mapEnd = ReadTokenEnd(attribute.Value, mapStart);
        int extensionStart = SkipWhitespace(attribute.Value, mapEnd);
        if (mapStart == mapEnd || extensionStart == mapEnd || extensionStart == attribute.Value.Length)
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap value requires a map entry followed by an extension URI.");
        }

        string mapEntry = attribute.Value.Substring(mapStart, mapEnd - mapStart);
        int slashIndex = mapEntry.IndexOf('/');
        if (slashIndex >= 0 && mapEntry.IndexOf('/', slashIndex + 1) >= 0)
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap map entry may contain at most one direction qualifier.");
        }
        string identifier = slashIndex < 0 ? mapEntry : mapEntry.Substring(0, slashIndex);
        if (!IsIdentifierText(identifier))
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap identifier must contain one through five decimal digits.");
        }

        SdpDirection direction = SdpDirection.SendReceive;
        if (slashIndex >= 0)
        {
            string directionToken = mapEntry.Substring(slashIndex + 1);
            direction = SdpCommonAttributeCodecCore.ReadDirection(directionToken);
            if (direction == SdpDirection.Unknown)
            {
                return FailExtMapDecode(attribute, result, "The SDP extmap direction must be sendonly, recvonly, sendrecv, or inactive.");
            }
        }

        int firstUriEnd = ReadTokenEnd(attribute.Value, extensionStart);
        string firstUri = attribute.Value.Substring(extensionStart, firstUriEnd - extensionStart);
        bool encrypted = firstUri == SdpHeaderExtensionUris.Encryption;
        string extensionUri = firstUri;
        int attributesStart = SkipWhitespace(attribute.Value, firstUriEnd);
        if (encrypted)
        {
            if (attributesStart == firstUriEnd || attributesStart == attribute.Value.Length)
            {
                return FailExtMapDecode(attribute, result, "The encrypted SDP extmap form requires an extension URI after the encryption marker.");
            }
            int extensionUriEnd = ReadTokenEnd(attribute.Value, attributesStart);
            extensionUri = attribute.Value.Substring(attributesStart, extensionUriEnd - attributesStart);
            attributesStart = SkipWhitespace(attribute.Value, extensionUriEnd);
        }
        if (!IsAbsoluteUri(extensionUri))
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap extension name must be an absolute non-whitespace URI.");
        }

        string extensionAttributes = TrimWhitespace(attribute.Value, attributesStart);
        if (extensionAttributes.Length > 0 && !SdpCommonAttributeCodecCore.IsByteStringValue(extensionAttributes))
        {
            return FailExtMapDecode(attribute, result, "The SDP extmap extension attributes contain a prohibited line-ending character.");
        }

        SdpExtMapAttribute projection = new SdpExtMapAttribute();
        projection.SourceAttribute = attribute;
        projection.Id = ReadIdentifier(identifier);
        projection.HasDirection = slashIndex >= 0;
        projection.Direction = direction;
        projection.Encrypted = encrypted;
        projection.Uri = extensionUri;
        projection.ExtensionAttributes = extensionAttributes;
        result.ExtMapAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeExtMap(SdpExtMapAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Id < 0 || projection.Id > 99999)
        {
            result.Diagnostics.Add(CreateExtMapEncodeDiagnostic("The SDP extmap identifier must fit the one-through-five-digit grammar."));
            return result;
        }
        string directionName = SdpCommonAttributeCodecCore.ReadDirectionName(projection.Direction);
        if (directionName.Length == 0)
        {
            result.Diagnostics.Add(CreateExtMapEncodeDiagnostic("The SDP extmap direction must be sendonly, recvonly, sendrecv, or inactive."));
            return result;
        }
        if (projection.Uri == null || !IsAbsoluteUri(projection.Uri))
        {
            result.Diagnostics.Add(CreateExtMapEncodeDiagnostic("The SDP extmap extension name must be an absolute non-whitespace URI."));
            return result;
        }
        if (projection.ExtensionAttributes == null || projection.ExtensionAttributes.Length > 0 && !IsSafeExtensionAttributes(projection.ExtensionAttributes))
        {
            result.Diagnostics.Add(CreateExtMapEncodeDiagnostic("The SDP extmap extension attributes must be an opaque byte string without outer whitespace or line endings."));
            return result;
        }

        string value = projection.Id.ToString(CultureInfo.InvariantCulture);
        if (projection.HasDirection || projection.Direction != SdpDirection.SendReceive)
        {
            value = value + "/" + directionName;
        }
        value = value + " ";
        if (projection.Encrypted)
        {
            value = value + SdpHeaderExtensionUris.Encryption + " ";
        }
        value = value + projection.Uri;
        if (projection.ExtensionAttributes.Length > 0)
        {
            value = value + " " + projection.ExtensionAttributes;
        }

        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.ExtMap;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.ExtMap + ":" + value;
        result.Attribute = attribute;
        result.Success = true;
        return result;
    }

    internal static bool IsIdentifierText(string value)
    {
        if (value.Length == 0 || value.Length > 5)
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

    internal static int ReadIdentifier(string value)
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

    internal static bool IsAbsoluteUri(string value)
    {
        int colonIndex = value.IndexOf(':');
        if (colonIndex <= 0 || !IsAsciiLetter(value[0]))
        {
            return false;
        }
        int index = 1;
        while (index < colonIndex)
        {
            char character = value[index];
            bool schemeCharacter = IsAsciiLetter(character) || character >= '0' && character <= '9' || character == '+' || character == '-' || character == '.';
            if (!schemeCharacter)
            {
                return false;
            }
            index = index + 1;
        }
        return SdpCommonAttributeCodecCore.IsNonWhitespaceValue(value);
    }

    internal static bool IsAsciiLetter(char character)
    {
        return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
    }

    internal static bool IsSafeExtensionAttributes(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value) || IsWhitespace(value[0]) || IsWhitespace(value[value.Length - 1]))
        {
            return false;
        }
        return true;
    }

    internal static int SkipWhitespace(string value, int index)
    {
        while (index < value.Length && IsWhitespace(value[index]))
        {
            index = index + 1;
        }
        return index;
    }

    internal static int ReadTokenEnd(string value, int index)
    {
        while (index < value.Length && !IsWhitespace(value[index]))
        {
            index = index + 1;
        }
        return index;
    }

    internal static string TrimWhitespace(string value, int start)
    {
        int end = value.Length;
        while (end > start && IsWhitespace(value[end - 1]))
        {
            end = end - 1;
        }
        if (start >= end)
        {
            return string.Empty;
        }
        return value.Substring(start, end - start);
    }

    internal static bool IsWhitespace(char character)
    {
        return character == ' ' || character == '\t';
    }

    internal static SdpAttributeDecodeResult FailExtMapDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateExtMapEncodeDiagnostic(string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.ExtMap, detail);
    }
}
