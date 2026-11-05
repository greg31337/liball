using System;

namespace iso20022;

internal static class XmlNameLexical
{
    internal static bool IsXmlName(ReadOnlySpan<char> text)
    {
        return IsNameWithColonPolicy(text, true);
    }

    internal static bool IsNcName(ReadOnlySpan<char> text)
    {
        return IsNameWithColonPolicy(text, false);
    }

    internal static bool IsNameWithColonPolicy(ReadOnlySpan<char> text, bool allowColon)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        int codePoint = ReadCodePoint(text, index);
        if (codePoint < 0 || !IsNameStartCodePoint(codePoint, allowColon))
        {
            return false;
        }

        index = index + CodePointLength(codePoint);
        while (index < text.Length)
        {
            codePoint = ReadCodePoint(text, index);
            if (codePoint < 0 || !IsNameCodePoint(codePoint, allowColon))
            {
                return false;
            }

            index = index + CodePointLength(codePoint);
        }

        return true;
    }

    internal static int ReadCodePoint(ReadOnlySpan<char> text, int index)
    {
        char first = text[index];
        if (first >= 55296 && first <= 56319)
        {
            if (index + 1 >= text.Length)
            {
                return -1;
            }

            char second = text[index + 1];
            if (second < 56320 || second > 57343)
            {
                return -1;
            }

            return 65536 + (first - 55296) * 1024 + second - 56320;
        }

        if (first >= 56320 && first <= 57343)
        {
            return -1;
        }

        return first;
    }

    internal static int CodePointLength(int codePoint)
    {
        return codePoint >= 65536 ? 2 : 1;
    }

    internal static bool IsNameStartCodePoint(int codePoint, bool allowColon)
    {
        if (allowColon && codePoint == 58)
        {
            return true;
        }

        if (codePoint == 95 || (codePoint >= 65 && codePoint <= 90) || (codePoint >= 97 && codePoint <= 122))
        {
            return true;
        }

        return (codePoint >= 192 && codePoint <= 214)
            || (codePoint >= 216 && codePoint <= 246)
            || (codePoint >= 248 && codePoint <= 767)
            || (codePoint >= 880 && codePoint <= 893)
            || (codePoint >= 895 && codePoint <= 8191)
            || (codePoint >= 8204 && codePoint <= 8205)
            || (codePoint >= 8304 && codePoint <= 8591)
            || (codePoint >= 11264 && codePoint <= 12271)
            || (codePoint >= 12289 && codePoint <= 55295)
            || (codePoint >= 63744 && codePoint <= 64975)
            || (codePoint >= 65008 && codePoint <= 65533)
            || (codePoint >= 65536 && codePoint <= 983039);
    }

    internal static bool IsNameCodePoint(int codePoint, bool allowColon)
    {
        if (IsNameStartCodePoint(codePoint, allowColon))
        {
            return true;
        }

        return codePoint == 45
            || codePoint == 46
            || (codePoint >= 48 && codePoint <= 57)
            || codePoint == 183
            || (codePoint >= 768 && codePoint <= 879)
            || (codePoint >= 8255 && codePoint <= 8256);
    }
}
