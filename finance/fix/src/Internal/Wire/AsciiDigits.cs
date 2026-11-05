using System;

namespace fix;

internal static class AsciiDigits
{
    internal static bool IsDigit(byte value)
    {
        return value >= 48 && value <= 57;
    }

    internal static AsciiIntegerResult ParseUnsignedInt(ReadOnlySpan<byte> source, int maximumValue)
    {
        AsciiIntegerResult result = new AsciiIntegerResult();
        if (maximumValue < 0)
        {
            result.ErrorCode = "invalid-limit";
            return result;
        }
        if (source.Length == 0)
        {
            result.Required = 1;
            result.ErrorIndex = 0;
            result.ErrorCode = "incomplete";
            return result;
        }

        int value = 0;
        for (int index = 0; index < source.Length; index++)
        {
            byte current = source[index];
            if (!IsDigit(current))
            {
                result.ErrorIndex = index;
                result.ErrorCode = "invalid-digit";
                return result;
            }
            int digit = current - 48;
            if (digit > maximumValue || value > (maximumValue - digit) / 10)
            {
                result.ErrorIndex = index;
                result.ErrorCode = "overflow";
                return result;
            }
            value = value * 10 + digit;
        }

        result.Success = true;
        result.Value = value;
        result.Count = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static AsciiIntegerResult WriteUnsignedInt(int value, Span<byte> destination)
    {
        AsciiIntegerResult result = new AsciiIntegerResult();
        if (value < 0)
        {
            result.ErrorCode = "negative-value";
            return result;
        }

        int required = 1;
        int remaining = value;
        while (remaining >= 10)
        {
            remaining = remaining / 10;
            required++;
        }
        result.Required = required;
        if (destination.Length < required)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }

        remaining = value;
        int index = required - 1;
        while (index >= 0)
        {
            destination[index] = (byte)(48 + remaining % 10);
            remaining = remaining / 10;
            index--;
        }
        result.Success = true;
        result.Value = value;
        result.Count = required;
        return result;
    }
}
