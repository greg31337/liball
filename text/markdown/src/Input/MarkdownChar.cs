using System;
using System.Globalization;

namespace markdown;

internal class MarkdownChar
{
    internal static bool IsSpace(char character)
    {
        return character == ' ';
    }

    internal static bool IsTab(char character)
    {
        return character == '\t';
    }

    internal static bool IsSpaceOrTab(char character)
    {
        return IsSpace(character) || IsTab(character);
    }

    internal static bool IsLineEnding(char character)
    {
        return character == '\n' || character == '\r';
    }

    internal static int GetLineEndingLength(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (index == text.Length)
        {
            return 0;
        }

        if (text[index] == '\n')
        {
            return 1;
        }

        if (text[index] == '\r')
        {
            if (index + 1 < text.Length && text[index + 1] == '\n')
            {
                return 2;
            }

            return 1;
        }

        return 0;
    }

    internal static bool IsWhitespace(char character)
    {
        if (character == '\t' || character == '\n' || character == '\f' || character == '\r')
        {
            return true;
        }

        return char.GetUnicodeCategory(character) == UnicodeCategory.SpaceSeparator;
    }

    internal static bool IsAsciiPunctuation(char character)
    {
        return character >= '!' && character <= '/'
            || character >= ':' && character <= '@'
            || character >= '[' && character <= '`'
            || character >= '{' && character <= '~';
    }

    internal static bool IsPunctuation(char character)
    {
        UnicodeCategory category = char.GetUnicodeCategory(character);
        return IsPunctuationCategory(category) || IsSymbolCategory(category);
    }

    internal static bool IsAsciiControl(char character)
    {
        return character <= '\u001f' || character == '\u007f';
    }

    internal static bool IsPunctuationCategory(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.DashPunctuation:
            case UnicodeCategory.OpenPunctuation:
            case UnicodeCategory.ClosePunctuation:
            case UnicodeCategory.InitialQuotePunctuation:
            case UnicodeCategory.FinalQuotePunctuation:
            case UnicodeCategory.OtherPunctuation:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsSymbolCategory(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.MathSymbol:
            case UnicodeCategory.CurrencySymbol:
            case UnicodeCategory.ModifierSymbol:
            case UnicodeCategory.OtherSymbol:
                return true;
            default:
                return false;
        }
    }
}
