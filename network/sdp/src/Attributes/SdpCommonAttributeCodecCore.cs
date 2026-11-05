using System;
using System.Globalization;

namespace sdp;

internal static class SdpCommonAttributeCodecCore
{
    internal static string ReadCanonicalName(string name)
    {
        if (string.Equals(name, SdpAttributeNames.SendReceive, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.SendReceive;
        }
        if (string.Equals(name, SdpAttributeNames.SendOnly, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.SendOnly;
        }
        if (string.Equals(name, SdpAttributeNames.ReceiveOnly, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.ReceiveOnly;
        }
        if (string.Equals(name, SdpAttributeNames.Inactive, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Inactive;
        }
        if (string.Equals(name, SdpAttributeNames.Orientation, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Orientation;
        }
        if (string.Equals(name, SdpAttributeNames.ConferenceType, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.ConferenceType;
        }
        if (string.Equals(name, SdpAttributeNames.CharacterSet, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.CharacterSet;
        }
        if (string.Equals(name, SdpAttributeNames.SessionDescriptionLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.SessionDescriptionLanguage;
        }
        if (string.Equals(name, SdpAttributeNames.Language, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Language;
        }
        if (string.Equals(name, SdpAttributeNames.FrameRate, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.FrameRate;
        }
        if (string.Equals(name, SdpAttributeNames.Quality, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Quality;
        }
        if (string.Equals(name, SdpAttributeNames.Tool, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Tool;
        }
        if (string.Equals(name, SdpAttributeNames.Category, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Category;
        }
        if (string.Equals(name, SdpAttributeNames.Keywords, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Keywords;
        }
        if (string.Equals(name, SdpAttributeNames.Control, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Control;
        }
        if (string.Equals(name, SdpAttributeNames.Content, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Content;
        }
        if (string.Equals(name, SdpAttributeNames.Label, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.Label;
        }
        if (string.Equals(name, SdpAttributeNames.PacketTime, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.PacketTime;
        }
        if (string.Equals(name, SdpAttributeNames.MaximumPacketTime, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.MaximumPacketTime;
        }
        if (string.Equals(name, SdpAttributeNames.RtcpMux, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.RtcpMux;
        }
        if (string.Equals(name, SdpAttributeNames.RtcpMuxOnly, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.RtcpMuxOnly;
        }
        if (string.Equals(name, SdpAttributeNames.RtcpReducedSize, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.RtcpReducedSize;
        }
        if (string.Equals(name, SdpAttributeNames.BundleOnly, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.BundleOnly;
        }
        if (string.Equals(name, SdpAttributeNames.IceLite, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.IceLite;
        }
        if (string.Equals(name, SdpAttributeNames.IceMismatch, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.IceMismatch;
        }
        if (string.Equals(name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.EndOfCandidates;
        }
        if (string.Equals(name, SdpAttributeNames.ExtMapAllowMixed, StringComparison.OrdinalIgnoreCase))
        {
            return SdpAttributeNames.ExtMapAllowMixed;
        }
        return string.Empty;
    }

    internal static bool IsPropertyName(string canonicalName)
    {
        return ReadDirection(canonicalName) != SdpDirection.Unknown || canonicalName == SdpAttributeNames.RtcpMux || canonicalName == SdpAttributeNames.RtcpMuxOnly || canonicalName == SdpAttributeNames.RtcpReducedSize || canonicalName == SdpAttributeNames.BundleOnly || canonicalName == SdpAttributeNames.IceLite || canonicalName == SdpAttributeNames.IceMismatch || canonicalName == SdpAttributeNames.EndOfCandidates || canonicalName == SdpAttributeNames.ExtMapAllowMixed;
    }

    internal static SdpDirection ReadDirection(string canonicalName)
    {
        if (canonicalName == SdpAttributeNames.SendReceive)
        {
            return SdpDirection.SendReceive;
        }
        if (canonicalName == SdpAttributeNames.SendOnly)
        {
            return SdpDirection.SendOnly;
        }
        if (canonicalName == SdpAttributeNames.ReceiveOnly)
        {
            return SdpDirection.ReceiveOnly;
        }
        if (canonicalName == SdpAttributeNames.Inactive)
        {
            return SdpDirection.Inactive;
        }
        return SdpDirection.Unknown;
    }

    internal static string ReadDirectionName(SdpDirection direction)
    {
        if (direction == SdpDirection.SendReceive)
        {
            return SdpAttributeNames.SendReceive;
        }
        if (direction == SdpDirection.SendOnly)
        {
            return SdpAttributeNames.SendOnly;
        }
        if (direction == SdpDirection.ReceiveOnly)
        {
            return SdpAttributeNames.ReceiveOnly;
        }
        if (direction == SdpDirection.Inactive)
        {
            return SdpAttributeNames.Inactive;
        }
        return string.Empty;
    }

    internal static string ReadEncodingName(SdpCommonAttribute common)
    {
        string directionName = ReadDirectionName(common.Direction);
        if (directionName.Length > 0)
        {
            if (common.Name.Length == 0 || ReadCanonicalName(common.Name) == directionName)
            {
                return directionName;
            }
            return string.Empty;
        }
        return ReadCanonicalName(common.Name);
    }

    internal static string ReadEncodingValue(SdpCommonAttribute common, string canonicalName)
    {
        if (canonicalName == SdpAttributeNames.Quality && common.HasIntegerValue)
        {
            return common.IntegerValue.ToString(CultureInfo.InvariantCulture);
        }
        bool numericName = canonicalName == SdpAttributeNames.FrameRate || canonicalName == SdpAttributeNames.PacketTime || canonicalName == SdpAttributeNames.MaximumPacketTime;
        if (numericName && common.HasNumericValue)
        {
            if (double.IsNaN(common.NumericValue) || double.IsInfinity(common.NumericValue))
            {
                return string.Empty;
            }
            return common.NumericValue.ToString("0.###############################", CultureInfo.InvariantCulture);
        }
        return common.Value;
    }

    internal static string ReadValidationError(SdpCommonAttribute common, SdpAttributeKind kind)
    {
        if (IsPropertyName(common.Name))
        {
            if (kind != SdpAttributeKind.Property)
            {
                return "The SDP " + common.Name + " attribute must use property syntax without a colon-delimited value.";
            }
            return string.Empty;
        }
        if (kind != SdpAttributeKind.Value)
        {
            return "The SDP " + common.Name + " attribute requires a colon-delimited value.";
        }
        if (common.Value.Length == 0)
        {
            return "The SDP " + common.Name + " attribute value must not be empty.";
        }
        if (!IsByteStringValue(common.Value))
        {
            return "The SDP " + common.Name + " attribute value contains a prohibited NUL or line-ending character.";
        }
        if (common.Name == SdpAttributeNames.Orientation && !IsOrientationValue(common.Value))
        {
            return "The SDP orientation must be portrait, landscape, or seascape with the specified case.";
        }
        if (common.Name == SdpAttributeNames.ConferenceType && !IsConferenceTypeValue(common.Value))
        {
            return "The SDP conference type is not one of the values defined by RFC 8866.";
        }
        if (common.Name == SdpAttributeNames.CharacterSet && !IsTokenValue(common.Value))
        {
            return "The SDP character-set name must use token syntax.";
        }
        if ((common.Name == SdpAttributeNames.SessionDescriptionLanguage || common.Name == SdpAttributeNames.Language) && !IsLanguageTagValue(common.Value))
        {
            return "The SDP language value must be one non-empty language tag.";
        }
        if (common.Name == SdpAttributeNames.Category && !IsNonWhitespaceValue(common.Value))
        {
            return "The obsolete SDP category must be a non-whitespace string.";
        }
        if (common.Name == SdpAttributeNames.Label && !IsTokenValue(common.Value))
        {
            return "The SDP label value must use token syntax.";
        }
        if (common.Name == SdpAttributeNames.Content && !IsContentValue(common.Value))
        {
            return "The SDP content value must contain one or more comma-delimited tokens.";
        }
        if (common.Name == SdpAttributeNames.Quality && !IsQualityValue(common.Value))
        {
            return "The SDP quality value must be an integer from zero through ten without leading zeroes.";
        }
        bool positiveNumericName = common.Name == SdpAttributeNames.FrameRate || common.Name == SdpAttributeNames.PacketTime || common.Name == SdpAttributeNames.MaximumPacketTime;
        if (positiveNumericName && !IsPositiveIntegerOrRealValue(common.Value))
        {
            return "The SDP " + common.Name + " value must be a non-zero integer or real number in canonical decimal form.";
        }
        return string.Empty;
    }

    internal static bool IsOrientationValue(string value)
    {
        return value == "portrait" || value == "landscape" || value == "seascape";
    }

    internal static bool IsConferenceTypeValue(string value)
    {
        return value == "broadcast" || value == "meeting" || value == "moderated" || value == "test" || value == "H332";
    }

    internal static bool IsTokenValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!SdpCharacterClasses.IsTokenCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static bool IsLanguageTagValue(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
        {
            return false;
        }
        int index = 0;
        int subtagLength = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '-')
            {
                if (subtagLength == 0 || subtagLength > 8)
                {
                    return false;
                }
                subtagLength = 0;
            }
            else if (character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9')
            {
                subtagLength = subtagLength + 1;
            }
            else
            {
                return false;
            }
            index = index + 1;
        }
        return subtagLength > 0 && subtagLength <= 8;
    }

    internal static bool IsNonWhitespaceValue(string value)
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

    internal static bool IsByteStringValue(string value)
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
        return value.Length > 0;
    }

    internal static bool IsContentValue(string value)
    {
        int index = 0;
        int tokenLength = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ',')
            {
                if (tokenLength == 0)
                {
                    return false;
                }
                tokenLength = 0;
            }
            else if (SdpCharacterClasses.IsTokenCharacter(character))
            {
                tokenLength = tokenLength + 1;
            }
            else
            {
                return false;
            }
            index = index + 1;
        }
        return tokenLength > 0;
    }

    internal static bool IsQualityValue(string value)
    {
        if (!IsZeroBasedIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return number.Success && number.UnsignedValue <= 10;
    }

    internal static bool IsPositiveIntegerOrRealValue(string value)
    {
        int dotIndex = value.IndexOf('.');
        if (dotIndex < 0)
        {
            return IsPositiveIntegerValue(value);
        }
        if (value.IndexOf('.', dotIndex + 1) >= 0)
        {
            return false;
        }
        string integerPart = value.Substring(0, dotIndex);
        string fractionalPart = value.Substring(dotIndex + 1);
        if (!IsZeroBasedIntegerValue(integerPart) || fractionalPart.Length == 0 || fractionalPart[fractionalPart.Length - 1] == '0')
        {
            return false;
        }
        int index = 0;
        while (index < fractionalPart.Length)
        {
            char character = fractionalPart[index];
            if (character < '0' || character > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsPositiveIntegerValue(string value)
    {
        if (value.Length == 0 || value[0] < '1' || value[0] > '9')
        {
            return false;
        }
        int index = 1;
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

    internal static bool IsZeroBasedIntegerValue(string value)
    {
        if (value == "0")
        {
            return true;
        }
        return IsPositiveIntegerValue(value);
    }

    internal static void PopulateNumericProjection(SdpCommonAttribute common)
    {
        if (common.Name == SdpAttributeNames.Quality)
        {
            SdpNumberParseResult integer = SdpNumberParser.ParseCharacterUnsignedDecimal(common.Value.AsSpan());
            common.HasIntegerValue = true;
            common.IntegerValue = (int)integer.UnsignedValue;
            common.HasNumericValue = true;
            common.NumericValue = common.IntegerValue;
            return;
        }
        bool numericName = common.Name == SdpAttributeNames.FrameRate || common.Name == SdpAttributeNames.PacketTime || common.Name == SdpAttributeNames.MaximumPacketTime;
        if (!numericName)
        {
            return;
        }
        double numericValue = 0;
        double fractionalScale = 0.1;
        bool afterDot = false;
        int index = 0;
        while (index < common.Value.Length)
        {
            char character = common.Value[index];
            if (character == '.')
            {
                afterDot = true;
            }
            else if (afterDot)
            {
                numericValue = numericValue + (character - '0') * fractionalScale;
                fractionalScale = fractionalScale / 10;
            }
            else
            {
                numericValue = numericValue * 10 + character - '0';
            }
            index = index + 1;
        }
        if (!double.IsInfinity(numericValue) && numericValue > 0)
        {
            common.HasNumericValue = true;
            common.NumericValue = numericValue;
        }
    }
}
