using System;
using System.Globalization;

namespace iso20022;

internal static class DecimalLexical
{
    internal static IsoDecimalValue? ParseDecimal(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        int start = 0;
        bool negative = false;
        if (text[0] == '+' || text[0] == '-')
        {
            negative = text[0] == '-';
            start = 1;
        }

        if (start == text.Length)
        {
            return null;
        }

        int index = start;
        int decimalPointIndex = -1;
        int digitCount = 0;
        int fractionalDigitCount = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '.')
            {
                if (decimalPointIndex >= 0)
                {
                    return null;
                }

                decimalPointIndex = index;
            }
            else
            {
                if (character < '0' || character > '9')
                {
                    return null;
                }

                digitCount++;
                if (decimalPointIndex >= 0)
                {
                    fractionalDigitCount++;
                }
            }

            index++;
        }

        if (digitCount == 0)
        {
            return null;
        }

        int trailingFractionalZeroCount = 0;
        index = text.Length - 1;
        while (decimalPointIndex >= 0 && index > decimalPointIndex && text[index] == '0')
        {
            trailingFractionalZeroCount++;
            index--;
        }

        int effectiveScale = fractionalDigitCount - trailingFractionalZeroCount;
        if (effectiveScale > 28)
        {
            return null;
        }

        int coefficientEnd = text.Length - trailingFractionalZeroCount;
        decimal coefficient = decimal.Zero;
        index = start;
        while (index < coefficientEnd)
        {
            char character = text[index];
            if (character != '.')
            {
                int digit = character - '0';
                if (coefficient > (decimal.MaxValue - digit) / 10)
                {
                    return null;
                }

                coefficient = coefficient * 10 + digit;
            }

            index++;
        }

        decimal value = coefficient;
        index = 0;
        while (index < effectiveScale)
        {
            value = value / 10;
            index++;
        }

        if (negative && value != decimal.Zero)
        {
            value = -value;
        }

        IsoDecimalValue result = new IsoDecimalValue();
        result.Value = value;
        result.Scale = fractionalDigitCount;
        result.IsNegativeZero = negative && value == decimal.Zero;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatDecimal(IsoDecimalValue value)
    {
        if (value == null || value.Scale < 0 || value.Scale > 999)
        {
            return null;
        }

        if (value.IsNegativeZero && value.Value != decimal.Zero)
        {
            return null;
        }

        if (value.Scale <= 28 && decimal.Round(value.Value, value.Scale) != value.Value)
        {
            return null;
        }

        decimal magnitude = value.Value;
        if (magnitude < decimal.Zero)
        {
            magnitude = -magnitude;
        }

        string format = "F" + value.Scale.ToString(CultureInfo.InvariantCulture);
        string result = magnitude.ToString(format, CultureInfo.InvariantCulture);
        if (value.Value < decimal.Zero || value.IsNegativeZero)
        {
            return "-" + result;
        }

        return result;
    }
}
