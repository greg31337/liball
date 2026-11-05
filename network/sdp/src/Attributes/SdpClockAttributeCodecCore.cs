using System;
using System.Globalization;

namespace sdp;

internal static class SdpClockAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeClockReference(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailClockDecode(attribute, result, "The SDP ts-refclk attribute requires a colon-delimited value.");
        }
        string value = TrimClockWhitespace(attribute.Value);
        string validationError = ReadClockReferenceTextError(value);
        if (validationError.Length > 0)
        {
            return FailClockDecode(attribute, result, validationError);
        }

        SdpClockReferenceAttribute projection = new SdpClockReferenceAttribute();
        projection.SourceAttribute = attribute;
        if (value == "private:traceable")
        {
            projection.SourceKind = SdpClockSourceKind.Private;
            projection.SourceToken = "private";
            projection.Value = "traceable";
        }
        else
        {
            int equalsIndex = value.IndexOf('=');
            projection.SourceToken = equalsIndex >= 0 ? value.Substring(0, equalsIndex) : value;
            projection.Value = equalsIndex >= 0 ? value.Substring(equalsIndex + 1) : string.Empty;
            projection.SourceKind = ReadClockSourceKind(projection.SourceToken);
        }
        result.ClockReferenceAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeClockReference(SdpClockReferenceAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        string sourceToken = ReadClockSourceToken(projection);
        if (sourceToken.Length == 0)
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.TimestampReferenceClock, "The timestamp reference-clock kind and source token are inconsistent."));
            return result;
        }

        string value = sourceToken;
        if (projection.SourceKind == SdpClockSourceKind.Private && projection.Value == "traceable")
        {
            value = sourceToken + ":traceable";
        }
        else if (projection.Value.Length > 0)
        {
            value = sourceToken + "=" + projection.Value;
        }
        string validationError = ReadClockReferenceTextError(value);
        if (validationError.Length > 0)
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.TimestampReferenceClock, validationError));
            return result;
        }

        result.Attribute = CreateClockValueAttribute(SdpAttributeNames.TimestampReferenceClock, value);
        result.Success = true;
        return result;
    }

    internal static SdpAttributeDecodeResult DecodeMediaClock(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailClockDecode(attribute, result, "The SDP mediaclk attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailClockDecode(attribute, result, "The SDP mediaclk value must contain a media-clock source.");
        }
        string[] tokens = ReadClockTokens(attribute.Value);
        int clockIndex = 0;
        string id = string.Empty;
        if (tokens.Length > 0 && tokens[0].StartsWith("id=", StringComparison.Ordinal))
        {
            id = tokens[0].Substring(3);
            if (!IsValidMediaClockId(id))
            {
                return FailClockDecode(attribute, result, "The SDP mediaclk identifier must contain a non-empty base64 clock tag, optionally prefixed by src:.");
            }
            clockIndex = 1;
        }
        if (clockIndex >= tokens.Length || tokens.Length - clockIndex > 2)
        {
            return FailClockDecode(attribute, result, "The SDP mediaclk value requires one clock source and at most one rational rate.");
        }

        string clockText = tokens[clockIndex];
        int equalsIndex = clockText.IndexOf('=');
        string clockName = equalsIndex >= 0 ? clockText.Substring(0, equalsIndex) : clockText;
        string clockValue = equalsIndex >= 0 ? clockText.Substring(equalsIndex + 1) : string.Empty;
        string clockError = ReadMediaClockSourceError(clockName, equalsIndex >= 0, clockValue);
        if (clockError.Length > 0)
        {
            return FailClockDecode(attribute, result, clockError);
        }

        SdpMediaClockAttribute projection = new SdpMediaClockAttribute();
        projection.SourceAttribute = attribute;
        projection.Id = id;
        projection.ClockName = clockName;
        projection.ClockValue = clockValue;
        if (tokens.Length - clockIndex == 2)
        {
            if (clockName != "direct")
            {
                return FailClockDecode(attribute, result, "An SDP mediaclk rational rate is permitted only for the direct clock source.");
            }
            string rateError = PopulateMediaClockRate(projection, tokens[clockIndex + 1]);
            if (rateError.Length > 0)
            {
                return FailClockDecode(attribute, result, rateError);
            }
        }

        result.MediaClockAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeMediaClock(SdpMediaClockAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (projection.Id.Length > 0 && !IsValidMediaClockId(projection.Id))
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.MediaClock, "The SDP mediaclk identifier must contain a valid base64 clock tag, optionally prefixed by src:."));
            return result;
        }
        string clockError = ReadMediaClockSourceError(projection.ClockName, projection.ClockValue.Length > 0, projection.ClockValue);
        if (clockError.Length > 0)
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.MediaClock, clockError));
            return result;
        }
        if (!projection.HasRate && (projection.RateNumerator != 0 || projection.RateDenominator != 1))
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.MediaClock, "SDP mediaclk rational-rate values require HasRate to be enabled."));
            return result;
        }
        if (projection.HasRate && (projection.ClockName != "direct" || projection.RateNumerator <= 0 || projection.RateDenominator <= 0))
        {
            result.Diagnostics.Add(CreateClockEncodeDiagnostic(SdpAttributeNames.MediaClock, "An SDP mediaclk rational rate requires a direct clock and positive numerator and denominator."));
            return result;
        }

        string value = string.Empty;
        if (projection.Id.Length > 0)
        {
            value = "id=" + projection.Id + " ";
        }
        value = value + projection.ClockName;
        if (projection.ClockValue.Length > 0)
        {
            value = value + "=" + projection.ClockValue;
        }
        if (projection.HasRate)
        {
            value = value + " rate=" + projection.RateNumerator.ToString(CultureInfo.InvariantCulture) + "/" + projection.RateDenominator.ToString(CultureInfo.InvariantCulture);
        }
        result.Attribute = CreateClockValueAttribute(SdpAttributeNames.MediaClock, value);
        result.Success = true;
        return result;
    }

    internal static string ReadClockReferenceTextError(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return "The SDP ts-refclk value must contain a clock source without prohibited line-ending characters.";
        }
        if (value == "private:traceable")
        {
            return string.Empty;
        }
        int equalsIndex = value.IndexOf('=');
        string sourceToken = equalsIndex >= 0 ? value.Substring(0, equalsIndex) : value;
        string sourceValue = equalsIndex >= 0 ? value.Substring(equalsIndex + 1) : string.Empty;
        if (!SdpCommonAttributeCodecCore.IsTokenValue(sourceToken))
        {
            return "The SDP ts-refclk source name must use token syntax.";
        }
        SdpClockSourceKind sourceKind = ReadClockSourceKind(sourceToken);
        if (sourceKind == SdpClockSourceKind.Ntp || sourceKind == SdpClockSourceKind.Ptp)
        {
            if (equalsIndex < 0 || !SdpCommonAttributeCodecCore.IsNonWhitespaceValue(sourceValue))
            {
                return "The SDP ts-refclk NTP and PTP sources require non-whitespace source data.";
            }
        }
        else if (sourceKind != SdpClockSourceKind.Unknown && sourceValue.Length > 0 || sourceKind != SdpClockSourceKind.Unknown && equalsIndex >= 0)
        {
            return "This registered SDP ts-refclk source does not permit equals-delimited source data.";
        }
        else if (sourceKind == SdpClockSourceKind.Unknown && equalsIndex >= 0 && !SdpCommonAttributeCodecCore.IsByteStringValue(sourceValue))
        {
            return "An extension SDP ts-refclk source value must not be empty or contain prohibited line-ending characters.";
        }
        return string.Empty;
    }

    internal static SdpClockSourceKind ReadClockSourceKind(string token)
    {
        if (token == "ntp")
        {
            return SdpClockSourceKind.Ntp;
        }
        if (token == "ptp")
        {
            return SdpClockSourceKind.Ptp;
        }
        if (token == "gps")
        {
            return SdpClockSourceKind.Gps;
        }
        if (token == "gal")
        {
            return SdpClockSourceKind.Galileo;
        }
        if (token == "glonass")
        {
            return SdpClockSourceKind.Glonass;
        }
        if (token == "local")
        {
            return SdpClockSourceKind.Local;
        }
        if (token == "private")
        {
            return SdpClockSourceKind.Private;
        }
        return SdpClockSourceKind.Unknown;
    }

    internal static string ReadClockSourceToken(SdpClockReferenceAttribute projection)
    {
        string expected = string.Empty;
        if (projection.SourceKind == SdpClockSourceKind.Ntp)
        {
            expected = "ntp";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Ptp)
        {
            expected = "ptp";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Gps)
        {
            expected = "gps";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Galileo)
        {
            expected = "gal";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Glonass)
        {
            expected = "glonass";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Local)
        {
            expected = "local";
        }
        else if (projection.SourceKind == SdpClockSourceKind.Private)
        {
            expected = "private";
        }
        if (expected.Length > 0)
        {
            if (projection.SourceToken.Length == 0 || projection.SourceToken == expected)
            {
                return expected;
            }
            return string.Empty;
        }
        return SdpCommonAttributeCodecCore.IsTokenValue(projection.SourceToken) ? projection.SourceToken : string.Empty;
    }

    internal static string ReadMediaClockSourceError(string name, bool hasValue, string value)
    {
        if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
        {
            return "The SDP mediaclk source name must use token syntax.";
        }
        if (hasValue && !SdpCommonAttributeCodecCore.IsNonWhitespaceValue(value))
        {
            return "The SDP mediaclk source value must be non-whitespace text.";
        }
        if (name == "sender" && hasValue)
        {
            return "The SDP mediaclk sender source does not permit a value.";
        }
        if (name == "direct" && hasValue && !IsClockDecimal(value))
        {
            return "The SDP mediaclk direct offset must contain decimal digits.";
        }
        if (name == "IEEE1722" && (!hasValue || !IsClockEui64(value)))
        {
            return "The SDP mediaclk IEEE1722 source requires an eight-octet hyphen-delimited stream identifier.";
        }
        return string.Empty;
    }

    internal static string PopulateMediaClockRate(SdpMediaClockAttribute projection, string rateText)
    {
        if (!rateText.StartsWith("rate=", StringComparison.Ordinal))
        {
            return "The SDP mediaclk rational-rate parameter must begin with rate=.";
        }
        string ratio = rateText.Substring(5);
        int slashIndex = ratio.IndexOf('/');
        if (slashIndex <= 0 || slashIndex + 1 >= ratio.Length || ratio.IndexOf('/', slashIndex + 1) >= 0)
        {
            return "The SDP mediaclk rational rate requires one numerator and denominator.";
        }
        string numeratorText = ratio.Substring(0, slashIndex);
        string denominatorText = ratio.Substring(slashIndex + 1);
        if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(numeratorText) || !SdpCommonAttributeCodecCore.IsPositiveIntegerValue(denominatorText))
        {
            return "The SDP mediaclk rational rate requires positive canonical integers.";
        }
        SdpNumberParseResult numerator = SdpNumberParser.ParseCharacterUnsignedDecimal(numeratorText.AsSpan());
        SdpNumberParseResult denominator = SdpNumberParser.ParseCharacterUnsignedDecimal(denominatorText.AsSpan());
        if (!numerator.Success || !denominator.Success || numerator.UnsignedValue > long.MaxValue || denominator.UnsignedValue > long.MaxValue)
        {
            return "The SDP mediaclk rational rate exceeds the supported positive integer range.";
        }
        projection.HasRate = true;
        projection.RateNumerator = (long)numerator.UnsignedValue;
        projection.RateDenominator = (long)denominator.UnsignedValue;
        return string.Empty;
    }

    internal static bool IsValidMediaClockId(string value)
    {
        string tag = value;
        if (tag.StartsWith("src:", StringComparison.Ordinal))
        {
            tag = tag.Substring(4);
        }
        if (tag.Length == 0)
        {
            return false;
        }
        int index = 0;
        bool padding = false;
        int paddingCount = 0;
        while (index < tag.Length)
        {
            char character = tag[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (character == '=')
            {
                padding = true;
                paddingCount = paddingCount + 1;
                if (paddingCount > 2)
                {
                    return false;
                }
            }
            else if (padding || !alphaNumeric && character != '+' && character != '/')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsClockDecimal(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    internal static bool IsClockEui64(string value)
    {
        if (value.Length != 23)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (index % 3 == 2)
            {
                if (value[index] != '-')
                {
                    return false;
                }
            }
            else if (!SdpConnectionLineParser.IsHexDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static string[] ReadClockTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static string TrimClockWhitespace(string value)
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

    internal static SdpAttribute CreateClockValueAttribute(string name, string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = name;
        attribute.Value = value;
        attribute.RawText = name + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailClockDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, detail);
    }

    internal static SdpDiagnostic CreateClockEncodeDiagnostic(string attributeName, string detail)
    {
        return SdpAttributeEncoderCore.CreateEncodeDiagnostic(attributeName, detail);
    }
}
