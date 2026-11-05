using System;

namespace avif;

internal static class Utf8Utility
{
    internal static bool IsValid(ReadOnlySpan<byte> source)
    {
        int index = 0;
        while (index < source.Length)
        {
            byte first = source[index];
            if (first <= 127)
            {
                index++;
                continue;
            }

            if (first >= 194 && first <= 223)
            {
                if (index > source.Length - 2 || !IsContinuation(source[index + 1]))
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index > source.Length - 3)
                {
                    return false;
                }

                byte second = source[index + 1];
                byte third = source[index + 2];
                if (!IsContinuation(third))
                {
                    return false;
                }

                if (first == 224)
                {
                    if (second < 160 || second > 191)
                    {
                        return false;
                    }
                }
                else if (first == 237)
                {
                    if (second < 128 || second > 159)
                    {
                        return false;
                    }
                }
                else if (!IsContinuation(second))
                {
                    return false;
                }

                index += 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index > source.Length - 4)
                {
                    return false;
                }

                byte second = source[index + 1];
                if (!IsContinuation(source[index + 2]) || !IsContinuation(source[index + 3]))
                {
                    return false;
                }

                if (first == 240)
                {
                    if (second < 144 || second > 191)
                    {
                        return false;
                    }
                }
                else if (first == 244)
                {
                    if (second < 128 || second > 143)
                    {
                        return false;
                    }
                }
                else if (!IsContinuation(second))
                {
                    return false;
                }

                index += 4;
                continue;
            }

            return false;
        }

        return true;
    }

    internal static bool IsValidUtf16(string value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index == value.Length - 1 || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }
}
