using System;

namespace time;

internal static class IsoParsing
{
    internal static int ReadFixedDigits(TextCursor cursor, int length)
    {
        TimeValidation.RequireNotNull(cursor, "cursor");

        return cursor.ReadFixedDigits(length);
    }

    internal static long ReadFractionalSecondTicks(TextCursor cursor, int maxDigits)
    {
        TimeValidation.RequireNotNull(cursor, "cursor");

        if (maxDigits <= 0 || maxDigits > 7)
        {
            throw new ArgumentOutOfRangeException("maxDigits", "Fractional second digit limit must be in range 1 through 7.");
        }

        long ticks = 0L;
        int digits = 0;
        while (!cursor.IsEnd() && TextCursor.IsDigit(cursor.Peek()))
        {
            if (digits >= maxDigits)
            {
                throw new FormatException("Fractional seconds contain too many digits.");
            }

            ticks = ticks * 10L + (cursor.Read() - '0');
            digits = digits + 1;
        }

        if (digits == 0)
        {
            throw new FormatException("Fractional seconds must contain at least one digit.");
        }

        while (digits < 7)
        {
            ticks = ticks * 10L;
            digits = digits + 1;
        }

        return ticks;
    }

    internal static int FindSingleSeparator(string text, char separator)
    {
        TimeValidation.RequireNotNull(text, "text");

        int found = -1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == separator)
            {
                if (found >= 0)
                {
                    throw new FormatException("Text must contain a single separator.");
                }

                found = index;
            }

            index = index + 1;
        }

        if (found <= 0 || found == text.Length - 1)
        {
            throw new FormatException("Text must contain non-empty components around the separator.");
        }

        return found;
    }

    internal static int FindOffsetStart(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length > 0 && text[text.Length - 1] == 'Z')
        {
            return text.Length - 1;
        }

        if (text.Length >= 9)
        {
            int secondOffsetIndex = text.Length - 9;
            if ((text[secondOffsetIndex] == '+' || text[secondOffsetIndex] == '-')
                && text[secondOffsetIndex + 3] == ':'
                && text[secondOffsetIndex + 6] == ':')
            {
                return secondOffsetIndex;
            }
        }

        if (text.Length >= 6)
        {
            int minuteOffsetIndex = text.Length - 6;
            if ((text[minuteOffsetIndex] == '+' || text[minuteOffsetIndex] == '-')
                && text[minuteOffsetIndex + 3] == ':')
            {
                return minuteOffsetIndex;
            }
        }

        return -1;
    }

    internal static bool StartsWithDurationDesignator(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length == 0)
        {
            return false;
        }

        if (text[0] == 'P')
        {
            return true;
        }

        return text.Length > 1 && (text[0] == '+' || text[0] == '-') && text[1] == 'P';
    }
}
