using System;
using System.Globalization;

namespace sdp;

internal static class SdpTypedTimeParser
{
    internal static SdpTypedTimeParseResult ParseCharacterTypedTime(ReadOnlySpan<char> input)
    {
        return ParseCharacterCore(input, false);
    }

    internal static SdpTypedTimeParseResult ParseCharacterSignedTypedTime(ReadOnlySpan<char> input)
    {
        return ParseCharacterCore(input, true);
    }

    internal static SdpTypedTimeParseResult ParseUtf8TypedTime(ReadOnlySpan<byte> input)
    {
        return ParseUtf8Core(input, false);
    }

    internal static SdpTypedTimeParseResult ParseUtf8SignedTypedTime(ReadOnlySpan<byte> input)
    {
        return ParseUtf8Core(input, true);
    }

    internal static SdpTypedTimeFormatResult FormatCanonicalTypedTime(SdpTypedTime value)
    {
        SdpTypedTimeFormatResult result = new SdpTypedTimeFormatResult();
        if (value == null || value.Unit == null || !IsUnit(value.Unit))
        {
            result.DiagnosticCode = SdpDiagnosticCodes.WriteInvalidDescription;
            return result;
        }
        result.Success = true;
        result.Text = value.Value.ToString(CultureInfo.InvariantCulture) + value.Unit;
        return result;
    }

    internal static SdpTypedTimeParseResult ParseCharacterCore(ReadOnlySpan<char> input, bool allowNegative)
    {
        if (input.Length == 0 || input[0] == '+')
        {
            return CreateInvalidResult();
        }
        int numericLength = input.Length;
        string unit = string.Empty;
        char finalCharacter = input[input.Length - 1];
        if (IsUnitCharacter(finalCharacter))
        {
            numericLength = numericLength - 1;
            unit = finalCharacter.ToString();
        }
        if (numericLength == 0)
        {
            return CreateInvalidResult();
        }

        ReadOnlySpan<char> numericText = input.Slice(0, numericLength);
        SdpNumberParseResult numberResult;
        if (allowNegative)
        {
            numberResult = SdpNumberParser.ParseCharacterSignedDecimal(numericText);
        }
        else
        {
            numberResult = SdpNumberParser.ParseCharacterUnsignedDecimal(numericText);
        }
        if (!numberResult.Success)
        {
            return CreateNumberFailureResult(numberResult);
        }
        if (!allowNegative && numberResult.UnsignedValue > 9223372036854775807UL)
        {
            return CreateOverflowResult();
        }

        SdpTypedTimeParseResult result = new SdpTypedTimeParseResult();
        result.Success = true;
        result.Value.Value = allowNegative ? numberResult.SignedValue : (long)numberResult.UnsignedValue;
        result.Value.Unit = unit;
        result.Value.RawValue = input.ToString();
        return result;
    }

    internal static SdpTypedTimeParseResult ParseUtf8Core(ReadOnlySpan<byte> input, bool allowNegative)
    {
        if (input.Length == 0 || input[0] == 43)
        {
            return CreateInvalidResult();
        }
        int numericLength = input.Length;
        string unit = string.Empty;
        byte finalCharacter = input[input.Length - 1];
        if (IsUnitByte(finalCharacter))
        {
            numericLength = numericLength - 1;
            unit = ((char)finalCharacter).ToString();
        }
        if (numericLength == 0)
        {
            return CreateInvalidResult();
        }

        ReadOnlySpan<byte> numericText = input.Slice(0, numericLength);
        SdpNumberParseResult numberResult;
        if (allowNegative)
        {
            numberResult = SdpNumberParser.ParseUtf8SignedDecimal(numericText);
        }
        else
        {
            numberResult = SdpNumberParser.ParseUtf8UnsignedDecimal(numericText);
        }
        if (!numberResult.Success)
        {
            return CreateNumberFailureResult(numberResult);
        }
        if (!allowNegative && numberResult.UnsignedValue > 9223372036854775807UL)
        {
            return CreateOverflowResult();
        }

        SdpTypedTimeParseResult result = new SdpTypedTimeParseResult();
        result.Success = true;
        result.Value.Value = allowNegative ? numberResult.SignedValue : (long)numberResult.UnsignedValue;
        result.Value.Unit = unit;
        result.Value.RawValue = SdpUtf8Decoder.Utf8.GetString(input);
        return result;
    }

    internal static bool IsUnit(string unit)
    {
        return unit.Length == 0 || unit == "d" || unit == "h" || unit == "m" || unit == "s";
    }

    internal static bool IsUnitCharacter(char value)
    {
        return value == 'd' || value == 'h' || value == 'm' || value == 's';
    }

    internal static bool IsUnitByte(byte value)
    {
        return value == 100 || value == 104 || value == 109 || value == 115;
    }

    internal static SdpTypedTimeParseResult CreateNumberFailureResult(SdpNumberParseResult numberResult)
    {
        SdpTypedTimeParseResult result = new SdpTypedTimeParseResult();
        result.Invalid = numberResult.Invalid;
        result.Overflow = numberResult.Overflow;
        result.DiagnosticCode = numberResult.DiagnosticCode;
        return result;
    }

    internal static SdpTypedTimeParseResult CreateInvalidResult()
    {
        SdpTypedTimeParseResult result = new SdpTypedTimeParseResult();
        result.Invalid = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseInvalidNumber;
        return result;
    }

    internal static SdpTypedTimeParseResult CreateOverflowResult()
    {
        SdpTypedTimeParseResult result = new SdpTypedTimeParseResult();
        result.Overflow = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseNumberOverflow;
        return result;
    }
}
