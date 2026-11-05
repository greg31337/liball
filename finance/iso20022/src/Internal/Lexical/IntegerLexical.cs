using System;
using System.Globalization;

namespace iso20022;

internal static class IntegerLexical
{
    internal static IntegerLexicalResult ParseInt32(ReadOnlySpan<char> text)
    {
        IntegerLexicalResult result = new IntegerLexicalResult();
        if (text.Length == 0)
        {
            return result;
        }

        int index = 0;
        bool negative = false;
        if (text[0] == '+' || text[0] == '-')
        {
            negative = text[0] == '-';
            index = 1;
        }

        if (index == text.Length)
        {
            return result;
        }

        long limit = negative ? 2147483648L : 2147483647L;
        long value = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return result;
            }

            int digit = character - '0';
            if (value > (limit - digit) / 10)
            {
                return result;
            }

            value = value * 10 + digit;
            index++;
        }

        if (negative)
        {
            value = -value;
        }

        result.Success = true;
        result.Value = (int)value;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string FormatInt32(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static LongIntegerLexicalResult ParseInt64(ReadOnlySpan<char> text)
    {
        LongIntegerLexicalResult result = new LongIntegerLexicalResult();
        if (text.Length == 0)
        {
            return result;
        }

        int index = 0;
        bool negative = false;
        if (text[0] == '+' || text[0] == '-')
        {
            negative = text[0] == '-';
            index = 1;
        }

        if (index == text.Length)
        {
            return result;
        }

        long limit = negative ? long.MinValue : -long.MaxValue;
        long multiplicationLimit = limit / 10;
        long value = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return result;
            }

            int digit = character - '0';
            if (value < multiplicationLimit)
            {
                return result;
            }

            value = value * 10;
            if (value < limit + digit)
            {
                return result;
            }

            value = value - digit;
            index++;
        }

        if (!negative)
        {
            value = -value;
        }

        result.Success = true;
        result.Value = value;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string FormatInt64(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
