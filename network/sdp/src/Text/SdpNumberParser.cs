using System;

namespace sdp;

internal static class SdpNumberParser
{
    internal static SdpNumberParseResult ParseCharacterSignedDecimal(ReadOnlySpan<char> input)
    {
        if (input.Length == 0)
        {
            return CreateInvalidResult();
        }
        bool negative = false;
        int offset = 0;
        if (input[0] == '+' || input[0] == '-')
        {
            negative = input[0] == '-';
            offset = 1;
        }
        if (offset == input.Length)
        {
            return CreateInvalidResult();
        }

        ulong limit = negative ? 9223372036854775808UL : 9223372036854775807UL;
        ulong magnitude = 0;
        while (offset < input.Length)
        {
            char character = input[offset];
            if (character < '0' || character > '9')
            {
                return CreateInvalidResult();
            }
            uint digit = (uint)(character - '0');
            if (magnitude > (limit - digit) / 10)
            {
                return CreateOverflowResult();
            }
            magnitude = magnitude * 10 + digit;
            offset = offset + 1;
        }

        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Success = true;
        result.UnsignedValue = magnitude;
        if (negative)
        {
            if (magnitude == 9223372036854775808UL)
            {
                result.SignedValue = long.MinValue;
            }
            else
            {
                result.SignedValue = 0 - (long)magnitude;
            }
        }
        else
        {
            result.SignedValue = (long)magnitude;
        }
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8SignedDecimal(ReadOnlySpan<byte> input)
    {
        if (input.Length == 0)
        {
            return CreateInvalidResult();
        }
        bool negative = false;
        int offset = 0;
        if (input[0] == 43 || input[0] == 45)
        {
            negative = input[0] == 45;
            offset = 1;
        }
        if (offset == input.Length)
        {
            return CreateInvalidResult();
        }

        ulong limit = negative ? 9223372036854775808UL : 9223372036854775807UL;
        ulong magnitude = 0;
        while (offset < input.Length)
        {
            byte character = input[offset];
            if (character < 48 || character > 57)
            {
                return CreateInvalidResult();
            }
            uint digit = (uint)(character - 48);
            if (magnitude > (limit - digit) / 10)
            {
                return CreateOverflowResult();
            }
            magnitude = magnitude * 10 + digit;
            offset = offset + 1;
        }

        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Success = true;
        result.UnsignedValue = magnitude;
        if (negative)
        {
            if (magnitude == 9223372036854775808UL)
            {
                result.SignedValue = long.MinValue;
            }
            else
            {
                result.SignedValue = 0 - (long)magnitude;
            }
        }
        else
        {
            result.SignedValue = (long)magnitude;
        }
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterUnsignedDecimal(ReadOnlySpan<char> input)
    {
        if (input.Length == 0)
        {
            return CreateInvalidResult();
        }
        ulong value = 0;
        int offset = 0;
        while (offset < input.Length)
        {
            char character = input[offset];
            if (character < '0' || character > '9')
            {
                return CreateInvalidResult();
            }
            uint digit = (uint)(character - '0');
            if (value > (ulong.MaxValue - digit) / 10)
            {
                return CreateOverflowResult();
            }
            value = value * 10 + digit;
            offset = offset + 1;
        }
        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Success = true;
        result.UnsignedValue = value;
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8UnsignedDecimal(ReadOnlySpan<byte> input)
    {
        if (input.Length == 0)
        {
            return CreateInvalidResult();
        }
        ulong value = 0;
        int offset = 0;
        while (offset < input.Length)
        {
            byte character = input[offset];
            if (character < 48 || character > 57)
            {
                return CreateInvalidResult();
            }
            uint digit = (uint)(character - 48);
            if (value > (ulong.MaxValue - digit) / 10)
            {
                return CreateOverflowResult();
            }
            value = value * 10 + digit;
            offset = offset + 1;
        }
        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Success = true;
        result.UnsignedValue = value;
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterPort(ReadOnlySpan<char> input)
    {
        SdpNumberParseResult result = ParseCharacterUnsignedDecimal(input);
        ApplyInt32Range(result, 65535);
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8Port(ReadOnlySpan<byte> input)
    {
        SdpNumberParseResult result = ParseUtf8UnsignedDecimal(input);
        ApplyInt32Range(result, 65535);
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterPayloadType(ReadOnlySpan<char> input)
    {
        SdpNumberParseResult result = ParseCharacterUnsignedDecimal(input);
        ApplyInt32Range(result, 127);
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8PayloadType(ReadOnlySpan<byte> input)
    {
        SdpNumberParseResult result = ParseUtf8UnsignedDecimal(input);
        ApplyInt32Range(result, 127);
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterSsrc(ReadOnlySpan<char> input)
    {
        SdpNumberParseResult result = ParseCharacterUnsignedDecimal(input);
        ApplyUInt32Range(result);
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8Ssrc(ReadOnlySpan<byte> input)
    {
        SdpNumberParseResult result = ParseUtf8UnsignedDecimal(input);
        ApplyUInt32Range(result);
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterPriority(ReadOnlySpan<char> input)
    {
        SdpNumberParseResult result = ParseCharacterUnsignedDecimal(input);
        ApplyUInt32Range(result);
        return result;
    }

    internal static SdpNumberParseResult ParseUtf8Priority(ReadOnlySpan<byte> input)
    {
        SdpNumberParseResult result = ParseUtf8UnsignedDecimal(input);
        ApplyUInt32Range(result);
        return result;
    }

    internal static SdpNumberParseResult ParseCharacterNtpValue(ReadOnlySpan<char> input)
    {
        return ParseCharacterUnsignedDecimal(input);
    }

    internal static SdpNumberParseResult ParseUtf8NtpValue(ReadOnlySpan<byte> input)
    {
        return ParseUtf8UnsignedDecimal(input);
    }

    internal static void ApplyInt32Range(SdpNumberParseResult result, int maximumValue)
    {
        if (!result.Success)
        {
            return;
        }
        if (result.UnsignedValue > (ulong)maximumValue)
        {
            SetOverflow(result);
            return;
        }
        result.Int32Value = (int)result.UnsignedValue;
    }

    internal static void ApplyUInt32Range(SdpNumberParseResult result)
    {
        if (!result.Success)
        {
            return;
        }
        if (result.UnsignedValue > uint.MaxValue)
        {
            SetOverflow(result);
            return;
        }
        result.UInt32Value = (uint)result.UnsignedValue;
    }

    internal static void SetOverflow(SdpNumberParseResult result)
    {
        result.Success = false;
        result.Overflow = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseNumberOverflow;
    }

    internal static SdpNumberParseResult CreateInvalidResult()
    {
        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Invalid = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseInvalidNumber;
        return result;
    }

    internal static SdpNumberParseResult CreateOverflowResult()
    {
        SdpNumberParseResult result = new SdpNumberParseResult();
        result.Overflow = true;
        result.DiagnosticCode = SdpDiagnosticCodes.ParseNumberOverflow;
        return result;
    }
}
