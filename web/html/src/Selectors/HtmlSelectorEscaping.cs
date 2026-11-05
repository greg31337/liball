using System;
using System.Globalization;
using System.Text;

namespace html;

internal class HtmlSelectorCodePoint
{
    internal int value;
    internal int characterLength;

    internal HtmlSelectorCodePoint(int valueValue, int characterLengthValue)
    {
        this.value = valueValue;
        this.characterLength = characterLengthValue;
    }

    internal int GetValue()
    {
        return this.value;
    }

    internal int GetCharacterLength()
    {
        return this.characterLength;
    }
}

internal static class HtmlSelectorEscaping
{
    internal static string EscapeIdentifier(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder(value.Length);
        int offset = 0;
        int characterIndex = 0;
        while (offset < value.Length)
        {
            HtmlSelectorCodePoint character = GetCodePoint(value, offset);
            int characterLength = character.GetCharacterLength();
            int codePoint = character.GetValue();
            if (codePoint == 0 || codePoint == -1)
            {
                result.Append('\uFFFD');
            }
            else if (IsControl(codePoint)
                || characterIndex == 0 && IsDigit(codePoint)
                || characterIndex == 1 && IsDigit(codePoint)
                    && value[0] == '-')
            {
                AppendCodePointEscape(result, codePoint);
            }
            else if (characterIndex == 0 && codePoint == '-'
                && offset + characterLength == value.Length)
            {
                result.Append("\\-");
            }
            else if (IsIdentifierCodePoint(codePoint))
            {
                AppendCodePoint(result, codePoint);
            }
            else
            {
                result.Append('\\');
                AppendCodePoint(result, codePoint);
            }

            offset = offset + characterLength;
            characterIndex = characterIndex + 1;
        }

        return result.ToString();
    }

    internal static string EscapeString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder(value.Length + 2);
        result.Append('"');
        int offset = 0;
        while (offset < value.Length)
        {
            HtmlSelectorCodePoint character = GetCodePoint(value, offset);
            int characterLength = character.GetCharacterLength();
            int codePoint = character.GetValue();
            if (codePoint == 0 || codePoint == -1)
            {
                result.Append('\uFFFD');
            }
            else if (IsControl(codePoint))
            {
                AppendCodePointEscape(result, codePoint);
            }
            else if (codePoint == '"' || codePoint == '\\')
            {
                result.Append('\\');
                result.Append((char)codePoint);
            }
            else
            {
                AppendCodePoint(result, codePoint);
            }

            offset = offset + characterLength;
        }

        result.Append('"');
        return result.ToString();
    }

    internal static HtmlSelectorCodePoint GetCodePoint(string value,
        int offset)
    {
        char first = value[offset];
        if (char.IsHighSurrogate(first))
        {
            int next = offset + 1;
            if (next < value.Length && char.IsLowSurrogate(value[next]))
            {
                return new HtmlSelectorCodePoint(
                    char.ConvertToUtf32(first, value[next]), 2);
            }

            return new HtmlSelectorCodePoint(-1, 1);
        }

        return new HtmlSelectorCodePoint(
            char.IsLowSurrogate(first) ? -1 : first, 1);
    }

    internal static void AppendCodePointEscape(StringBuilder result, int codePoint)
    {
        result.Append('\\');
        result.Append(codePoint.ToString("x", CultureInfo.InvariantCulture));
        result.Append(' ');
    }

    internal static void AppendCodePoint(StringBuilder result, int codePoint)
    {
        if (codePoint <= char.MaxValue)
        {
            result.Append((char)codePoint);
        }
        else
        {
            result.Append(char.ConvertFromUtf32(codePoint));
        }
    }

    internal static bool IsControl(int codePoint)
    {
        return codePoint >= 1 && codePoint <= 31 || codePoint == 127;
    }

    internal static bool IsDigit(int codePoint)
    {
        return codePoint >= '0' && codePoint <= '9';
    }

    internal static bool IsIdentifierCodePoint(int codePoint)
    {
        return codePoint >= 128 || codePoint == '-' || codePoint == '_'
            || IsDigit(codePoint)
            || codePoint >= 'A' && codePoint <= 'Z'
            || codePoint >= 'a' && codePoint <= 'z';
    }
}
