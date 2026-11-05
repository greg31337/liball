using System;

namespace currencycodes;

internal static class AsciiCodeText
{
    internal static string TrimWhitespace(string text)
    {
        int start = 0;
        int end = text.Length - 1;
        while (start <= end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end >= start && char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        if (start == 0 && end == text.Length - 1)
        {
            return text;
        }

        if (start > end)
        {
            return string.Empty;
        }

        return text.Substring(start, end - start + 1);
    }

    internal static bool IsAsciiLetterText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        ReadOnlySpan<char> characters = text.AsSpan();
        for (int index = 0; index < characters.Length; index++)
        {
            char character = characters[index];
            bool uppercase = character >= 'A' && character <= 'Z';
            bool lowercase = character >= 'a' && character <= 'z';
            if (!uppercase && !lowercase)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsAsciiDigitText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        ReadOnlySpan<char> characters = text.AsSpan();
        for (int index = 0; index < characters.Length; index++)
        {
            char character = characters[index];
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static string UppercaseAscii(string text)
    {
        bool changed = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character >= 'a' && character <= 'z')
            {
                changed = true;
                break;
            }
        }

        if (!changed)
        {
            return text;
        }

        char[] characters = new char[text.Length];
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            characters[index] = character >= 'a' && character <= 'z'
                ? (char)(character - ('a' - 'A'))
                : character;
        }

        return new string(characters);
    }

    internal static string PadNumericCode(string text)
    {
        int width = CurrencyCodeConstants.CurrencyNumericCodeLength;
        if (text.Length >= width)
        {
            return text;
        }

        char[] characters = new char[width];
        int padding = width - text.Length;
        for (int index = 0; index < padding; index++)
        {
            characters[index] = '0';
        }

        for (int index = 0; index < text.Length; index++)
        {
            characters[padding + index] = text[index];
        }

        return new string(characters);
    }
}
