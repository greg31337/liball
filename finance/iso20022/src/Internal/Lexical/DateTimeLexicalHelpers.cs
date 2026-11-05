using System;
using System.Globalization;

namespace iso20022;

internal static class DateTimeLexicalHelpers
{
    internal static TimezoneLexicalResult ParseTimezone(ReadOnlySpan<char> text)
    {
        TimezoneLexicalResult result = new TimezoneLexicalResult();
        result.ContentLength = text.Length;
        if (text.Length > 0 && text[text.Length - 1] == 'Z')
        {
            result.Success = true;
            result.ContentLength = text.Length - 1;
            result.HasTimezone = true;
            result.IsUtc = true;
            return result;
        }

        if (text.Length >= 6)
        {
            int signIndex = text.Length - 6;
            char sign = text[signIndex];
            if ((sign == '+' || sign == '-') && text[signIndex + 3] == ':')
            {
                int hourTens = DecodeDigit(text[signIndex + 1]);
                int hourOnes = DecodeDigit(text[signIndex + 2]);
                int minuteTens = DecodeDigit(text[signIndex + 4]);
                int minuteOnes = DecodeDigit(text[signIndex + 5]);
                if (hourTens < 0 || hourOnes < 0 || minuteTens < 0 || minuteOnes < 0)
                {
                    return result;
                }

                int hours = hourTens * 10 + hourOnes;
                int minutes = minuteTens * 10 + minuteOnes;
                if (hours > 14 || minutes > 59 || (hours == 14 && minutes != 0))
                {
                    return result;
                }

                int offset = hours * 60 + minutes;
                if (sign == '-')
                {
                    offset = -offset;
                }

                result.Success = true;
                result.ContentLength = signIndex;
                result.HasTimezone = true;
                result.OffsetMinutes = offset;
                result.IsNegativeZero = sign == '-' && offset == 0;
                return result;
            }
        }

        result.Success = true;
        return result;
    }

    internal static string? FormatTimezone(bool hasTimezone, int offsetMinutes, bool isUtc, bool isNegativeZero)
    {
        if (!hasTimezone)
        {
            if (offsetMinutes != 0 || isUtc || isNegativeZero)
            {
                return null;
            }

            return string.Empty;
        }

        if (isUtc)
        {
            if (offsetMinutes != 0 || isNegativeZero)
            {
                return null;
            }

            return "Z";
        }

        if (isNegativeZero && offsetMinutes != 0)
        {
            return null;
        }

        if (offsetMinutes < -840 || offsetMinutes > 840)
        {
            return null;
        }

        int magnitude = offsetMinutes;
        if (magnitude < 0)
        {
            magnitude = -magnitude;
        }

        int hours = magnitude / 60;
        int minutes = magnitude % 60;
        if (hours == 14 && minutes != 0)
        {
            return null;
        }

        char sign = offsetMinutes < 0 || isNegativeZero ? '-' : '+';
        return sign + hours.ToString("D2", CultureInfo.InvariantCulture) + ":" + minutes.ToString("D2", CultureInfo.InvariantCulture);
    }

    internal static int DecodeDigit(char character)
    {
        if (character < '0' || character > '9')
        {
            return -1;
        }

        return character - '0';
    }

    internal static int ParseTwoDigits(ReadOnlySpan<char> text)
    {
        if (text.Length != 2)
        {
            return -1;
        }

        int first = DecodeDigit(text[0]);
        int second = DecodeDigit(text[1]);
        if (first < 0 || second < 0)
        {
            return -1;
        }

        return first * 10 + second;
    }

    internal static bool IsAsciiDigits(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (DecodeDigit(text[index]) < 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static string? FormatYear(int year)
    {
        if (year == 0)
        {
            return null;
        }

        return year.ToString("D4", CultureInfo.InvariantCulture);
    }

    internal static bool IsValidDate(int year, int month, int day)
    {
        if (year == 0 || month < 1 || month > 12 || day < 1)
        {
            return false;
        }

        int maximumDay = 31;
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            maximumDay = 30;
        }
        else if (month == 2)
        {
            maximumDay = IsLeapYear(year) ? 29 : 28;
        }

        return day <= maximumDay;
    }

    internal static bool IsLeapYear(int year)
    {
        if (year % 400 == 0)
        {
            return true;
        }

        if (year % 100 == 0)
        {
            return false;
        }

        return year % 4 == 0;
    }
}
