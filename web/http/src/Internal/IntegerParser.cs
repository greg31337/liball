using System;

namespace http;

internal static class IntegerParser
{
    internal static IntegerParseResult ParseDecimal(ReadOnlySpan<byte> input)
    {
        return ParseDigits(input, 10);
    }

    internal static IntegerParseResult ParseHexadecimal(ReadOnlySpan<byte> input)
    {
        return ParseDigits(input, 16);
    }

    internal static IntegerParseResult ParseDigits(ReadOnlySpan<byte> input, int radix)
    {
        IntegerParseResult result = new IntegerParseResult();
        if (input.Length == 0)
        {
            return result;
        }

        long value = 0;
        int index = 0;
        while (index < input.Length)
        {
            int digit = GetDigit(input[index]);
            if (digit < 0 || digit >= radix)
            {
                result.Consumed = index;
                return result;
            }

            if (value > (long.MaxValue - digit) / radix)
            {
                result.Overflow = true;
                result.Consumed = index;
                return result;
            }

            value = value * radix + digit;
            index = index + 1;
        }

        result.IsValid = true;
        result.Value = value;
        result.Consumed = input.Length;
        return result;
    }

    internal static int GetDigit(byte value)
    {
        if (value >= 0x30 && value <= 0x39)
        {
            return value - 0x30;
        }

        if (value >= 0x41 && value <= 0x46)
        {
            return value - 0x41 + 10;
        }

        if (value >= 0x61 && value <= 0x66)
        {
            return value - 0x61 + 10;
        }

        return -1;
    }
}
