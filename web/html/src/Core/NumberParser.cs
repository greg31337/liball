using System;
using System.Globalization;

namespace html;

internal class IntegerScanResult
{
    internal bool success;
    internal bool overflow;
    internal int value;
    internal int consumedLength;

    internal IntegerScanResult(bool success, bool overflow, int value, int consumedLength)
    {
        this.success = success;
        this.overflow = overflow;
        this.value = value;
        this.consumedLength = consumedLength;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool WasOverflow()
    {
        return this.overflow;
    }

    internal int GetValue()
    {
        return this.value;
    }

    internal int GetConsumedLength()
    {
        return this.consumedLength;
    }
}

internal class FloatingPointScanResult
{
    internal bool success;
    internal bool outOfRange;
    internal double value;
    internal int consumedLength;

    internal FloatingPointScanResult(bool success, bool outOfRange, double value, int consumedLength)
    {
        this.success = success;
        this.outOfRange = outOfRange;
        this.value = value;
        this.consumedLength = consumedLength;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool WasOutOfRange()
    {
        return this.outOfRange;
    }

    internal double GetValue()
    {
        return this.value;
    }

    internal int GetConsumedLength()
    {
        return this.consumedLength;
    }
}

internal class AnPlusBScanResult
{
    internal bool success;
    internal bool overflow;
    internal int step;
    internal int offset;
    internal int consumedLength;

    internal AnPlusBScanResult(bool success, bool overflow, int step, int offset, int consumedLength)
    {
        this.success = success;
        this.overflow = overflow;
        this.step = step;
        this.offset = offset;
        this.consumedLength = consumedLength;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool WasOverflow()
    {
        return this.overflow;
    }

    internal int GetStep()
    {
        return this.step;
    }

    internal int GetOffset()
    {
        return this.offset;
    }

    internal int GetConsumedLength()
    {
        return this.consumedLength;
    }
}

internal static class NumberParser
{
    internal static IntegerScanResult ScanDecimal(ReadOnlySpan<char> input, int start, bool allowLeadingPlus, bool allowLeadingMinus)
    {
        ValidateStart(input.Length, start);
        int index = start;
        bool negative = false;
        if (index < input.Length && input[index] == '+')
        {
            if (!allowLeadingPlus)
            {
                return new IntegerScanResult(false, false, 0, 0);
            }

            index = index + 1;
        }
        else if (index < input.Length && input[index] == '-')
        {
            if (!allowLeadingMinus)
            {
                return new IntegerScanResult(false, false, 0, 0);
            }

            negative = true;
            index = index + 1;
        }

        int digitsStart = index;
        long magnitude = 0;
        long maximum = negative ? 2147483648L : 2147483647L;
        bool overflow = false;
        while (index < input.Length && Ascii.IsDigit(input[index]))
        {
            int digit = input[index] - '0';
            if (!overflow)
            {
                if (magnitude > (maximum - digit) / 10)
                {
                    overflow = true;
                }
                else
                {
                    magnitude = magnitude * 10 + digit;
                }
            }

            index = index + 1;
        }

        if (index == digitsStart)
        {
            return new IntegerScanResult(false, false, 0, 0);
        }

        int consumed = index - start;
        if (overflow)
        {
            return new IntegerScanResult(false, true, 0, consumed);
        }

        long signed = negative ? -magnitude : magnitude;
        return new IntegerScanResult(true, false, (int)signed, consumed);
    }

    internal static IntegerScanResult ParseSignedInteger(ReadOnlySpan<char> input)
    {
        IntegerScanResult result = ScanDecimal(input, 0, false, true);
        if (!result.IsSuccess() || result.GetConsumedLength() != input.Length)
        {
            return new IntegerScanResult(false, result.WasOverflow(), 0, result.GetConsumedLength());
        }

        return result;
    }

    internal static IntegerScanResult ParseNonNegativeInteger(ReadOnlySpan<char> input)
    {
        IntegerScanResult result = ScanDecimal(input, 0, false, false);
        if (!result.IsSuccess() || result.GetConsumedLength() != input.Length)
        {
            return new IntegerScanResult(false, result.WasOverflow(), 0, result.GetConsumedLength());
        }

        return result;
    }

    internal static IntegerScanResult ScanHexadecimal(ReadOnlySpan<char> input, int start)
    {
        ValidateStart(input.Length, start);
        int index = start;
        long value = 0;
        bool overflow = false;
        while (index < input.Length)
        {
            int digit = Ascii.GetHexValue(input[index]);
            if (digit < 0)
            {
                break;
            }

            if (!overflow)
            {
                if (value > (2147483647L - digit) / 16)
                {
                    overflow = true;
                }
                else
                {
                    value = value * 16 + digit;
                }
            }

            index = index + 1;
        }

        int consumed = index - start;
        if (consumed == 0)
        {
            return new IntegerScanResult(false, false, 0, 0);
        }

        if (overflow)
        {
            return new IntegerScanResult(false, true, 0, consumed);
        }

        return new IntegerScanResult(true, false, (int)value, consumed);
    }

    internal static IntegerScanResult ParseHexadecimal(ReadOnlySpan<char> input)
    {
        IntegerScanResult result = ScanHexadecimal(input, 0);
        if (!result.IsSuccess() || result.GetConsumedLength() != input.Length)
        {
            return new IntegerScanResult(false, result.WasOverflow(), 0, result.GetConsumedLength());
        }

        return result;
    }

    internal static FloatingPointScanResult ScanFloatingPoint(ReadOnlySpan<char> input, int start)
    {
        ValidateStart(input.Length, start);
        int index = start;
        if (index < input.Length && input[index] == '-')
        {
            index = index + 1;
        }

        int integerStart = index;
        while (index < input.Length && Ascii.IsDigit(input[index]))
        {
            index = index + 1;
        }

        bool hasIntegerDigits = index > integerStart;
        bool hasFractionDigits = false;
        if (index < input.Length && input[index] == '.')
        {
            int decimalPoint = index;
            index = index + 1;
            int fractionStart = index;
            while (index < input.Length && Ascii.IsDigit(input[index]))
            {
                index = index + 1;
            }

            if (index > fractionStart)
            {
                hasFractionDigits = true;
            }
            else
            {
                index = decimalPoint;
            }
        }

        if (!hasIntegerDigits && !hasFractionDigits)
        {
            return new FloatingPointScanResult(false, false, 0, 0);
        }

        if (index < input.Length && (input[index] == 'e' || input[index] == 'E'))
        {
            int exponentStart = index;
            index = index + 1;
            if (index < input.Length && (input[index] == '+' || input[index] == '-'))
            {
                index = index + 1;
            }

            int exponentDigits = index;
            while (index < input.Length && Ascii.IsDigit(input[index]))
            {
                index = index + 1;
            }

            if (index == exponentDigits)
            {
                index = exponentStart;
            }
        }

        int consumed = index - start;
        ReadOnlySpan<char> token = input.Slice(start, consumed);
        NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        double value = double.Parse(token, styles, CultureInfo.InvariantCulture);
        bool outOfRange = double.IsInfinity(value) || double.IsNaN(value);
        if (!outOfRange && value == 0 && ContainsNonzeroSignificandDigit(token))
        {
            outOfRange = true;
        }

        if (outOfRange)
        {
            return new FloatingPointScanResult(false, true, 0, consumed);
        }

        return new FloatingPointScanResult(true, false, value, consumed);
    }

    internal static FloatingPointScanResult ParseFloatingPoint(ReadOnlySpan<char> input)
    {
        FloatingPointScanResult result = ScanFloatingPoint(input, 0);
        if (!result.IsSuccess() || result.GetConsumedLength() != input.Length)
        {
            return new FloatingPointScanResult(false, result.WasOutOfRange(), 0, result.GetConsumedLength());
        }

        return result;
    }

    internal static FloatingPointScanResult ParseFloatingPointDescriptor(ReadOnlySpan<char> input)
    {
        FloatingPointScanResult result = ScanFloatingPoint(input, 0);
        int consumed = result.GetConsumedLength();
        if (!result.IsSuccess() || consumed + 1 != input.Length || input[consumed] != 'x' || result.GetValue() <= 0)
        {
            bool outOfRange = result.WasOutOfRange() || result.IsSuccess() && result.GetValue() <= 0;
            return new FloatingPointScanResult(false, outOfRange, 0, consumed);
        }

        return new FloatingPointScanResult(true, false, result.GetValue(), input.Length);
    }

    internal static AnPlusBScanResult ParseAnPlusB(ReadOnlySpan<char> input)
    {
        int start = 0;
        int end = input.Length;
        while (start < end && Ascii.IsHtmlWhitespace(input[start]))
        {
            start = start + 1;
        }

        while (end > start && Ascii.IsHtmlWhitespace(input[end - 1]))
        {
            end = end - 1;
        }

        if (start == end)
        {
            return new AnPlusBScanResult(false, false, 0, 0, 0);
        }

        ReadOnlySpan<char> trimmed = input.Slice(start, end - start);
        if (Ascii.EqualsIgnoreCase(trimmed, "odd".AsSpan()))
        {
            return new AnPlusBScanResult(true, false, 2, 1, input.Length);
        }

        if (Ascii.EqualsIgnoreCase(trimmed, "even".AsSpan()))
        {
            return new AnPlusBScanResult(true, false, 2, 0, input.Length);
        }

        int index = start;
        bool negative = false;
        if (input[index] == '+' || input[index] == '-')
        {
            negative = input[index] == '-';
            index = index + 1;
        }

        int digitsStart = index;
        IntegerScanResult coefficientDigits = ScanDigits(input, index, end, negative);
        if (coefficientDigits.GetConsumedLength() > 0)
        {
            index = index + coefficientDigits.GetConsumedLength();
        }

        bool hasDigits = index > digitsStart;
        if (index < end && (input[index] == 'n' || input[index] == 'N'))
        {
            int step;
            if (hasDigits)
            {
                if (coefficientDigits.WasOverflow())
                {
                    return new AnPlusBScanResult(false, true, 0, 0, index - start);
                }

                step = coefficientDigits.GetValue();
            }
            else
            {
                step = negative ? -1 : 1;
            }

            index = index + 1;
            while (index < end && Ascii.IsHtmlWhitespace(input[index]))
            {
                index = index + 1;
            }

            if (index == end)
            {
                return new AnPlusBScanResult(true, false, step, 0, input.Length);
            }

            if (input[index] != '+' && input[index] != '-')
            {
                return new AnPlusBScanResult(false, false, 0, 0, index - start);
            }

            bool offsetNegative = input[index] == '-';
            index = index + 1;
            while (index < end && Ascii.IsHtmlWhitespace(input[index]))
            {
                index = index + 1;
            }

            IntegerScanResult offsetDigits = ScanDigits(input, index, end, offsetNegative);
            if (offsetDigits.GetConsumedLength() == 0)
            {
                return new AnPlusBScanResult(false, false, 0, 0, index - start);
            }

            index = index + offsetDigits.GetConsumedLength();
            if (index != end)
            {
                return new AnPlusBScanResult(false, offsetDigits.WasOverflow(), 0, 0, index - start);
            }

            if (offsetDigits.WasOverflow())
            {
                return new AnPlusBScanResult(false, true, 0, 0, input.Length);
            }

            return new AnPlusBScanResult(true, false, step, offsetDigits.GetValue(), input.Length);
        }

        if (!hasDigits || index != end)
        {
            return new AnPlusBScanResult(false, coefficientDigits.WasOverflow(), 0, 0, index - start);
        }

        if (coefficientDigits.WasOverflow())
        {
            return new AnPlusBScanResult(false, true, 0, 0, input.Length);
        }

        return new AnPlusBScanResult(true, false, 0, coefficientDigits.GetValue(), input.Length);
    }

    internal static IntegerScanResult ScanDigits(ReadOnlySpan<char> input, int start, int end, bool negative)
    {
        int index = start;
        long magnitude = 0;
        long maximum = negative ? 2147483648L : 2147483647L;
        bool overflow = false;
        while (index < end && Ascii.IsDigit(input[index]))
        {
            int digit = input[index] - '0';
            if (!overflow)
            {
                if (magnitude > (maximum - digit) / 10)
                {
                    overflow = true;
                }
                else
                {
                    magnitude = magnitude * 10 + digit;
                }
            }

            index = index + 1;
        }

        int consumed = index - start;
        if (consumed == 0)
        {
            return new IntegerScanResult(false, false, 0, 0);
        }

        if (overflow)
        {
            return new IntegerScanResult(false, true, 0, consumed);
        }

        long signed = negative ? -magnitude : magnitude;
        return new IntegerScanResult(true, false, (int)signed, consumed);
    }

    internal static bool ContainsNonzeroSignificandDigit(ReadOnlySpan<char> input)
    {
        int index = 0;
        while (index < input.Length)
        {
            if (input[index] == 'e' || input[index] == 'E')
            {
                return false;
            }

            if (input[index] >= '1' && input[index] <= '9')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateStart(int length, int start)
    {
        if (start < 0 || start > length)
        {
            throw new ArgumentOutOfRangeException("start", start, "The numeric scan start is outside the input.");
        }
    }
}
