using System;
using System.Globalization;

namespace html;

internal class HtmlWordWrapper
{
    internal int wrapColumn;
    internal int column;
    internal char pendingHighSurrogate;

    internal HtmlWordWrapper(int wrapColumnValue)
    {
        if (wrapColumnValue < 0)
        {
            throw new ArgumentOutOfRangeException("wrapColumnValue",
                wrapColumnValue,
                "The text wrap column cannot be negative.");
        }

        this.wrapColumn = wrapColumnValue;
        this.column = 0;
        this.pendingHighSurrogate = '\0';
    }

    internal bool ShouldWrap(string word, bool separator)
    {
        if (word == null)
        {
            throw new ArgumentNullException("word");
        }

        this.CompletePendingSurrogate();
        if (this.wrapColumn == 0 || this.column == 0)
        {
            return false;
        }

        int required = GetWidth(word);
        if (separator)
        {
            required = required + 1;
        }

        return this.column + required > this.wrapColumn;
    }

    internal void Observe(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.CompletePendingSurrogate();
        TextElementEnumerator elements =
            StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (ContainsLineBreak(element))
            {
                this.column = GetWidthAfterLastLineBreak(element);
            }
            else if (element.Length == 1 && element[0] == '\t')
            {
                this.column = this.column + 8 - this.column % 8;
            }
            else
            {
                this.column = this.column + GetElementWidth(element);
            }
        }
    }

    internal void ObserveCharacter(char value)
    {
        if (value == '\r' || value == '\n')
        {
            this.column = 0;
            this.pendingHighSurrogate = '\0';
            return;
        }

        if (value == '\t')
        {
            this.column = this.column + 8 - this.column % 8;
            this.pendingHighSurrogate = '\0';
            return;
        }

        if (this.pendingHighSurrogate != '\0')
        {
            if (char.IsLowSurrogate(value))
            {
                int scalar = char.ConvertToUtf32(
                    this.pendingHighSurrogate, value);
                this.column = this.column
                    + (IsWideScalar(scalar) ? 2 : 1);
                this.pendingHighSurrogate = '\0';
                return;
            }

            this.CompletePendingSurrogate();
        }

        if (char.IsHighSurrogate(value))
        {
            this.pendingHighSurrogate = value;
            return;
        }

        UnicodeCategory category = char.GetUnicodeCategory(value);
        if (!IsZeroWidthCategory(category))
        {
            this.column = this.column
                + (IsWideScalar(value) ? 2 : 1);
        }
    }

    internal static int GetWidth(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int width = 0;
        TextElementEnumerator elements =
            StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            width = width + GetElementWidth(element);
        }

        return width;
    }

    internal static int GetElementWidth(string element)
    {
        int width = 0;
        bool emojiPresentation = false;
        int index = 0;
        while (index < element.Length)
        {
            int scalar = element[index];
            int scalarLength = 1;
            if (char.IsHighSurrogate(element[index])
                && index + 1 < element.Length
                && char.IsLowSurrogate(element[index + 1]))
            {
                scalar = char.ConvertToUtf32(element[index],
                    element[index + 1]);
                scalarLength = 2;
            }

            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(element, index);
            int scalarWidth = IsZeroWidthCategory(category)
                ? 0 : IsWideScalar(scalar) ? 2 : 1;
            if (scalarWidth > width)
            {
                width = scalarWidth;
            }

            if (scalar == 0xFE0F || scalar == 0x20E3
                || scalar >= 0x1F1E6 && scalar <= 0x1F1FF)
            {
                emojiPresentation = true;
            }

            index = index + scalarLength;
        }

        return emojiPresentation && width < 2 ? 2 : width;
    }

    internal static bool IsZeroWidthCategory(UnicodeCategory category)
    {
        return category == UnicodeCategory.NonSpacingMark
            || category == UnicodeCategory.SpacingCombiningMark
            || category == UnicodeCategory.EnclosingMark
            || category == UnicodeCategory.Format
            || category == UnicodeCategory.Control;
    }

    internal static bool IsWideScalar(int scalar)
    {
        return scalar >= 0x1100 && scalar <= 0x115F
            || scalar == 0x2329 || scalar == 0x232A
            || scalar >= 0x2E80 && scalar <= 0xA4CF
            && scalar != 0x303F
            || scalar >= 0xAC00 && scalar <= 0xD7A3
            || scalar >= 0xF900 && scalar <= 0xFAFF
            || scalar >= 0xFE10 && scalar <= 0xFE19
            || scalar >= 0xFE30 && scalar <= 0xFE6F
            || scalar >= 0xFF00 && scalar <= 0xFF60
            || scalar >= 0xFFE0 && scalar <= 0xFFE6
            || scalar >= 0x1F300 && scalar <= 0x1FAFF
            || scalar >= 0x20000 && scalar <= 0x3FFFD;
    }

    internal static bool ContainsLineBreak(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' || value[index] == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int GetWidthAfterLastLineBreak(string value)
    {
        int index = value.Length - 1;
        while (index >= 0)
        {
            if (value[index] == '\r' || value[index] == '\n')
            {
                return GetWidth(value.Substring(index + 1));
            }

            index = index - 1;
        }

        return GetWidth(value);
    }

    internal void CompletePendingSurrogate()
    {
        if (this.pendingHighSurrogate != '\0')
        {
            this.column = this.column + 1;
            this.pendingHighSurrogate = '\0';
        }
    }
}
