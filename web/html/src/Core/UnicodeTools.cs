using System;
using System.Text;

namespace html;

internal static class UnicodeTools
{
    internal const int ReplacementCharacter = 65533;
    internal const int MaximumScalar = 1114111;

    internal static bool IsScalarValue(int value)
    {
        return value >= 0 && value <= MaximumScalar && !IsSurrogateCodePoint(value);
    }

    internal static bool IsSurrogateCodePoint(int value)
    {
        return value >= 55296 && value <= 57343;
    }

    internal static bool IsHighSurrogate(char value)
    {
        return value >= '\uD800' && value <= '\uDBFF';
    }

    internal static bool IsLowSurrogate(char value)
    {
        return value >= '\uDC00' && value <= '\uDFFF';
    }

    internal static int GetScalarOrReplacement(int value)
    {
        return IsScalarValue(value) ? value : ReplacementCharacter;
    }

    internal static int GetHtmlScalarOrReplacement(int value)
    {
        return value == 0 || !IsScalarValue(value) ? ReplacementCharacter : value;
    }

    internal static int GetUtf16Length(int scalar)
    {
        ValidateScalar(scalar);
        return scalar <= 65535 ? 1 : 2;
    }

    internal static int CombineSurrogates(char high, char low)
    {
        if (!IsHighSurrogate(high))
        {
            throw new ArgumentOutOfRangeException("high", (int)high, "The first character must be a high surrogate.");
        }

        if (!IsLowSurrogate(low))
        {
            throw new ArgumentOutOfRangeException("low", (int)low, "The second character must be a low surrogate.");
        }

        return 65536 + ((high - 55296) << 10) + (low - 56320);
    }

    internal static char GetHighSurrogate(int scalar)
    {
        ValidateSupplementaryScalar(scalar);
        int value = scalar - 65536;
        return (char)(55296 + (value >> 10));
    }

    internal static char GetLowSurrogate(int scalar)
    {
        ValidateSupplementaryScalar(scalar);
        int value = scalar - 65536;
        return (char)(56320 + (value & 1023));
    }

    internal static string ScalarToString(int value)
    {
        int scalar = GetScalarOrReplacement(value);
        return char.ConvertFromUtf32(scalar);
    }

    internal static bool IsNoncharacter(int value)
    {
        if (!IsScalarValue(value))
        {
            return false;
        }

        return value >= 64976 && value <= 65007
            || value >= 65534 && (value & 65535) >= 65534;
    }

    internal static bool IsControl(int value)
    {
        return value >= 0 && value <= 31 || value >= 127 && value <= 159;
    }

    internal static bool IsUnicodeWhitespace(int value)
    {
        if (value >= 9 && value <= 13)
        {
            return true;
        }

        if (value == 32 || value == 133 || value == 160 || value == 5760)
        {
            return true;
        }

        if (value >= 8192 && value <= 8202)
        {
            return true;
        }

        return value == 8232 || value == 8233 || value == 8239 || value == 8287 || value == 12288;
    }

    internal static bool IsHtmlWhitespace(char value)
    {
        return Ascii.IsHtmlWhitespace(value);
    }

    internal static void ValidateScalar(int scalar)
    {
        if (!IsScalarValue(scalar))
        {
            throw new ArgumentOutOfRangeException("scalar", scalar, "The value must be a Unicode scalar.");
        }
    }

    internal static void ValidateSupplementaryScalar(int scalar)
    {
        if (!IsScalarValue(scalar) || scalar <= 65535)
        {
            throw new ArgumentOutOfRangeException("scalar", scalar, "The value must be a supplementary Unicode scalar.");
        }
    }
}

internal static class HtmlNewlines
{
    internal static bool IsNewline(char value)
    {
        return value == '\r' || value == '\n';
    }

    internal static int GetSequenceLength(ReadOnlySpan<char> input, int index)
    {
        if (index < 0 || index > input.Length)
        {
            throw new ArgumentOutOfRangeException("index", index, "The newline index is outside the input.");
        }

        if (index == input.Length)
        {
            return 0;
        }

        char value = input[index];
        if (value == '\n')
        {
            return 1;
        }

        if (value != '\r')
        {
            return 0;
        }

        if (index + 1 < input.Length && input[index + 1] == '\n')
        {
            return 2;
        }

        return 1;
    }

    internal static char GetNormalizedCharacter(char value)
    {
        return value == '\r' ? '\n' : value;
    }

    internal static int GetNormalizedLength(ReadOnlySpan<char> input)
    {
        int length = 0;
        int index = 0;
        while (index < input.Length)
        {
            int newlineLength = GetSequenceLength(input, index);
            if (newlineLength == 2)
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            length = length + 1;
        }

        return length;
    }

    internal static string Normalize(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        int firstCarriageReturn = input.IndexOf('\r');
        if (firstCarriageReturn < 0)
        {
            return input;
        }

        StringBuilder builder = new StringBuilder(input.Length);
        int index = 0;
        while (index < input.Length)
        {
            char value = input[index];
            if (value == '\r')
            {
                builder.Append('\n');
                if (index + 1 < input.Length && input[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }
            }
            else
            {
                builder.Append(value);
                index = index + 1;
            }
        }

        return builder.ToString();
    }
}
