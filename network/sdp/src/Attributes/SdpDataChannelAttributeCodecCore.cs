using System;
using System.Collections.Generic;
using System.Globalization;

namespace sdp;

internal static class SdpDataChannelAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeMap(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDecode(attribute, result, "The SDP dcmap attribute requires a colon-delimited value.");
        }

        int spaceIndex = attribute.Value.IndexOf(' ');
        string channelText = spaceIndex < 0 ? attribute.Value : attribute.Value.Substring(0, spaceIndex);
        if (!IsChannelIdText(channelText))
        {
            return FailDecode(attribute, result, "The SDP dcmap stream identifier must contain from one through five decimal digits.");
        }

        SdpDataChannelAttribute projection = new SdpDataChannelAttribute();
        projection.SourceAttribute = attribute;
        projection.ChannelId = ReadChannelId(channelText);
        if (spaceIndex >= 0)
        {
            string parameterText = attribute.Value.Substring(spaceIndex + 1);
            if (parameterText.Length == 0 || !ReadParameters(parameterText, projection.Parameters))
            {
                return FailDecode(attribute, result, "The SDP dcmap parameters must be nonempty semicolon-delimited name or name-value entries with valid quoted strings.");
            }
        }

        result.DataChannelAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeSubprotocolAttribute(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailDecode(attribute, result, "The SDP dcsa attribute requires a colon-delimited value.");
        }

        int spaceIndex = attribute.Value.IndexOf(' ');
        if (spaceIndex < 0)
        {
            return FailDecode(attribute, result, "The SDP dcsa value requires a stream identifier followed by an embedded attribute.");
        }
        string channelText = attribute.Value.Substring(0, spaceIndex);
        if (!IsChannelIdText(channelText))
        {
            return FailDecode(attribute, result, "The SDP dcsa stream identifier must contain from one through five decimal digits.");
        }

        string embeddedText = attribute.Value.Substring(spaceIndex + 1);
        int embeddedOffset = 2 + SdpAttributeNames.DataChannelSubprotocol.Length + 1 + spaceIndex + 1;
        SdpAttribute? embedded = ReadEmbeddedAttribute(embeddedText, attribute, embeddedOffset);
        if (embedded == null)
        {
            return FailDecode(attribute, result, "The SDP dcsa value must contain a valid embedded SDP attribute after the stream identifier.");
        }

        SdpDataChannelSubprotocolAttribute projection = new SdpDataChannelSubprotocolAttribute();
        projection.SourceAttribute = attribute;
        projection.ChannelId = ReadChannelId(channelText);
        projection.EmbeddedAttribute = embedded;
        result.DataChannelSubprotocolAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMap(SdpDataChannelAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.ChannelId < 0 || projection.ChannelId > 99999)
        {
            return FailEncode(result, SdpAttributeNames.DataChannelMap, "The SDP dcmap stream identifier must contain no more than five decimal digits.");
        }

        string value = projection.ChannelId.ToString(CultureInfo.InvariantCulture);
        int index = 0;
        while (index < projection.Parameters.Count)
        {
            string encoded = EncodeParameter(projection.Parameters[index]);
            if (encoded.Length == 0)
            {
                return FailEncode(result, SdpAttributeNames.DataChannelMap, "The SDP dcmap parameters must have valid names and structurally valid values.");
            }
            value = value + (index == 0 ? " " : ";") + encoded;
            index = index + 1;
        }

        result.Attribute = CreateValueAttribute(SdpAttributeNames.DataChannelMap, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeSubprotocolAttribute(SdpDataChannelSubprotocolAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.ChannelId < 0 || projection.ChannelId > 99999)
        {
            return FailEncode(result, SdpAttributeNames.DataChannelSubprotocol, "The SDP dcsa stream identifier must contain no more than five decimal digits.");
        }
        string embeddedText = EncodeEmbeddedAttribute(projection.EmbeddedAttribute);
        if (embeddedText.Length == 0)
        {
            return FailEncode(result, SdpAttributeNames.DataChannelSubprotocol, "The SDP dcsa value must contain a valid embedded SDP attribute.");
        }

        string value = projection.ChannelId.ToString(CultureInfo.InvariantCulture) + " " + embeddedText;
        result.Attribute = CreateValueAttribute(SdpAttributeNames.DataChannelSubprotocol, value);
        result.Success = true;
        return result;
    }

    internal static bool ReadParameters(string text, List<SdpDataChannelParameter> parameters)
    {
        bool quoted = false;
        int start = 0;
        int index = 0;
        while (index <= text.Length)
        {
            bool atEnd = index == text.Length;
            char character = atEnd ? '\0' : text[index];
            if (atEnd || character == ';' && !quoted)
            {
                string rawText = text.Substring(start, index - start);
                SdpDataChannelParameter? parameter = ReadParameter(rawText);
                if (parameter == null)
                {
                    return false;
                }
                parameters.Add(parameter);
                start = index + 1;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            index = index + 1;
        }
        return !quoted;
    }

    internal static SdpDataChannelParameter? ReadParameter(string rawText)
    {
        if (rawText.Length == 0)
        {
            return null;
        }
        int equalsIndex = rawText.IndexOf('=');
        string name = equalsIndex < 0 ? rawText : rawText.Substring(0, equalsIndex);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
        {
            return null;
        }

        SdpDataChannelParameter parameter = new SdpDataChannelParameter();
        parameter.Name = name;
        parameter.HasValue = equalsIndex >= 0;
        parameter.RawText = rawText;
        if (parameter.HasValue)
        {
            parameter.Value = rawText.Substring(equalsIndex + 1);
        }

        if (IsQuotedParameterName(name))
        {
            if (!parameter.HasValue || !IsQuotedValue(parameter.Value))
            {
                return null;
            }
            parameter.Value = parameter.Value.Substring(1, parameter.Value.Length - 2);
        }
        else if (IsPlainParameterName(name) && (!parameter.HasValue || !IsPlainParameterValue(parameter.Value)))
        {
            return null;
        }
        else if (parameter.HasValue && !IsUnknownParameterValue(parameter.Value))
        {
            return null;
        }
        return parameter;
    }

    internal static string EncodeParameter(SdpDataChannelParameter parameter)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(parameter.Name))
        {
            return string.Empty;
        }
        if (IsQuotedParameterName(parameter.Name))
        {
            if (!parameter.HasValue || !IsQuotedContent(parameter.Value))
            {
                return string.Empty;
            }
            return ReadCanonicalParameterName(parameter.Name) + "=\"" + parameter.Value + "\"";
        }
        if (!parameter.HasValue)
        {
            if (IsPlainParameterName(parameter.Name))
            {
                return string.Empty;
            }
            return parameter.Name;
        }
        if (IsPlainParameterName(parameter.Name) && !IsPlainParameterValue(parameter.Value))
        {
            return string.Empty;
        }
        if (!IsPlainParameterName(parameter.Name) && !IsUnknownParameterValue(parameter.Value))
        {
            return string.Empty;
        }
        return ReadCanonicalParameterName(parameter.Name) + "=" + parameter.Value;
    }

    internal static bool IsQuotedValue(string value)
    {
        if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
        {
            return false;
        }
        return IsQuotedContent(value.Substring(1, value.Length - 2));
    }

    internal static bool IsQuotedContent(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2]))
                {
                    return false;
                }
                index = index + 3;
            }
            else
            {
                bool visible = character == '!' || character >= '#' && character <= '$' || character >= '&' && character <= '~';
                if (character != ' ' && !visible)
                {
                    return false;
                }
                index = index + 1;
            }
        }
        return true;
    }

    internal static bool IsPlainParameterValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character == '\r' || character == '\n' || character == ';' || character == '"' || character == ' ' || character == '\t')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static bool IsUnknownParameterValue(string value)
    {
        return IsPlainParameterValue(value) || IsQuotedValue(value);
    }

    internal static SdpAttribute? ReadEmbeddedAttribute(string text, SdpAttribute source, int embeddedOffset)
    {
        int colonIndex = text.IndexOf(':');
        string name = colonIndex < 0 ? text : text.Substring(0, colonIndex);
        if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
        {
            return null;
        }
        string value = colonIndex < 0 ? string.Empty : text.Substring(colonIndex + 1);
        if (!IsEmbeddedValue(value))
        {
            return null;
        }

        SdpAttribute embedded = new SdpAttribute();
        embedded.Kind = colonIndex < 0 ? SdpAttributeKind.Property : SdpAttributeKind.Value;
        embedded.Name = name;
        embedded.Value = value;
        embedded.RawText = text;
        embedded.Location = SdpDiagnosticBuilder.CopySourceLocation(source.Location);
        MoveEmbeddedLocation(embedded.Location, embeddedOffset);
        return embedded;
    }

    internal static void MoveEmbeddedLocation(SdpSourceLocation location, int offset)
    {
        if (location.CharacterOffset >= 0)
        {
            location.CharacterOffset = location.CharacterOffset + offset;
        }
        if (location.ByteOffset >= 0)
        {
            location.ByteOffset = location.ByteOffset + offset;
        }
        if (location.ColumnNumber > 0)
        {
            location.ColumnNumber = location.ColumnNumber + offset;
        }
    }

    internal static string EncodeEmbeddedAttribute(SdpAttribute attribute)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(attribute.Name))
        {
            return string.Empty;
        }
        if (attribute.Kind == SdpAttributeKind.Property)
        {
            return attribute.Name;
        }
        if (attribute.Kind != SdpAttributeKind.Value || !IsEmbeddedValue(attribute.Value))
        {
            return string.Empty;
        }
        return attribute.Name + ":" + attribute.Value;
    }

    internal static bool IsEmbeddedValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsByteStringCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsChannelIdText(string value)
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

    internal static int ReadChannelId(string value)
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

    internal static bool IsQuotedParameterName(string name)
    {
        return string.Equals(name, SdpDataChannelTokens.Subprotocol, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpDataChannelTokens.Label, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsPlainParameterName(string name)
    {
        return string.Equals(name, SdpDataChannelTokens.MaximumRetransmissions, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpDataChannelTokens.MaximumRetransmissionTime, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpDataChannelTokens.Ordered, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpDataChannelTokens.Priority, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ReadCanonicalParameterName(string name)
    {
        if (string.Equals(name, SdpDataChannelTokens.Subprotocol, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.Subprotocol;
        }
        if (string.Equals(name, SdpDataChannelTokens.Label, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.Label;
        }
        if (string.Equals(name, SdpDataChannelTokens.MaximumRetransmissions, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.MaximumRetransmissions;
        }
        if (string.Equals(name, SdpDataChannelTokens.MaximumRetransmissionTime, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.MaximumRetransmissionTime;
        }
        if (string.Equals(name, SdpDataChannelTokens.Ordered, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.Ordered;
        }
        if (string.Equals(name, SdpDataChannelTokens.Priority, StringComparison.OrdinalIgnoreCase))
        {
            return SdpDataChannelTokens.Priority;
        }
        return name;
    }

    internal static bool IsHexDigit(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F' || value >= 'a' && value <= 'f';
    }

    internal static SdpAttributeDecodeResult FailDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpAttributeEncodeResult FailEncode(SdpAttributeEncodeResult result, string attributeName, string detail)
    {
        result.Diagnostics.Add(SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail));
        return result;
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
