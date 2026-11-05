using System;

namespace http;

internal static class Ascii
{
    internal static byte FoldByteToLower(byte value)
    {
        if (value >= 0x41 && value <= 0x5a)
        {
            return (byte)(value + 0x20);
        }

        return value;
    }

    internal static bool EqualsBytesIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (FoldByteToLower(left[index]) != FoldByteToLower(right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CompareBytesIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        int commonLength = left.Length;
        if (right.Length < commonLength)
        {
            commonLength = right.Length;
        }

        int index = 0;
        while (index < commonLength)
        {
            byte leftValue = FoldByteToLower(left[index]);
            byte rightValue = FoldByteToLower(right[index]);
            if (leftValue < rightValue)
            {
                return -1;
            }

            if (leftValue > rightValue)
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static bool EqualsStringsIgnoreCase(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            char leftValue = left[index];
            char rightValue = right[index];
            if (leftValue >= 'A' && leftValue <= 'Z')
            {
                leftValue = (char)(leftValue + 0x20);
            }

            if (rightValue >= 'A' && rightValue <= 'Z')
            {
                rightValue = (char)(rightValue + 0x20);
            }

            if (leftValue != rightValue)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static byte[] ToLowercaseBytes(ReadOnlySpan<byte> value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = FoldByteToLower(value[index]);
            index = index + 1;
        }

        return result;
    }

    internal static bool IsValid(ReadOnlySpan<byte> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int WriteString(string value, Span<byte> output, int offset)
    {
        if (offset < 0 || offset > output.Length || value.Length > output.Length - offset)
        {
            return -1;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return -1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < value.Length)
        {
            output[offset + index] = (byte)value[index];
            index = index + 1;
        }

        return value.Length;
    }

    internal static int WriteCarriageReturnLineFeed(Span<byte> output, int offset)
    {
        if (offset < 0 || offset > output.Length || output.Length - offset < 2)
        {
            return -1;
        }

        output[offset] = 0x0d;
        output[offset + 1] = 0x0a;
        return 2;
    }
}
