using System;
using System.Globalization;

namespace iso20022;

internal static class DateTimeLexical
{
    internal static IsoDateValue? ParseDate(ReadOnlySpan<char> text)
    {
        TimezoneLexicalResult timezone = DateTimeLexicalHelpers.ParseTimezone(text);
        if (!timezone.Success)
        {
            return null;
        }

        ReadOnlySpan<char> content = text.Slice(0, timezone.ContentLength);
        IsoDateValue? result = ParseDateContent(content);
        if (result == null)
        {
            return null;
        }

        result.HasTimezone = timezone.HasTimezone;
        result.TimezoneOffsetMinutes = timezone.OffsetMinutes;
        result.IsUtc = timezone.IsUtc;
        result.IsNegativeTimezoneZero = timezone.IsNegativeZero;
        result.LexicalMode = DateTimeLexicalMode.Preserve;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatDate(IsoDateValue value)
    {
        if (value == null)
        {
            return null;
        }

        string? content = FormatDateContent(value);
        if (content == null)
        {
            return null;
        }

        string? timezone = DateTimeLexicalHelpers.FormatTimezone(value.HasTimezone, value.TimezoneOffsetMinutes, value.IsUtc, value.IsNegativeTimezoneZero);
        if (timezone == null)
        {
            return null;
        }

        return content + timezone;
    }

    internal static IsoTimeValue? ParseTime(ReadOnlySpan<char> text)
    {
        TimezoneLexicalResult timezone = DateTimeLexicalHelpers.ParseTimezone(text);
        if (!timezone.Success)
        {
            return null;
        }

        ReadOnlySpan<char> content = text.Slice(0, timezone.ContentLength);
        if (content.Length < 8 || content[2] != ':' || content[5] != ':')
        {
            return null;
        }

        int hour = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(0, 2));
        int minute = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(3, 2));
        int second = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(6, 2));
        if (hour < 0 || minute < 0 || second < 0 || hour > 24 || minute > 59 || second > 60)
        {
            return null;
        }

        string fractionalSecondDigits = string.Empty;
        if (content.Length > 8)
        {
            if (content[8] != '.' || content.Length == 9)
            {
                return null;
            }

            ReadOnlySpan<char> fractional = content.Slice(9, content.Length - 9);
            if (!DateTimeLexicalHelpers.IsAsciiDigits(fractional))
            {
                return null;
            }

            fractionalSecondDigits = fractional.ToString();
        }

        if (hour == 24 && (minute != 0 || second != 0 || HasNonzeroDigit(fractionalSecondDigits.AsSpan())))
        {
            return null;
        }

        IsoTimeValue result = new IsoTimeValue();
        result.Hour = hour;
        result.Minute = minute;
        result.Second = second;
        result.FractionalSecondDigits = fractionalSecondDigits;
        result.HasTimezone = timezone.HasTimezone;
        result.TimezoneOffsetMinutes = timezone.OffsetMinutes;
        result.IsUtc = timezone.IsUtc;
        result.IsNegativeTimezoneZero = timezone.IsNegativeZero;
        result.LexicalMode = DateTimeLexicalMode.Preserve;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatTime(IsoTimeValue value)
    {
        if (value == null || value.Hour < 0 || value.Hour > 24 || value.Minute < 0 || value.Minute > 59 || value.Second < 0 || value.Second > 60)
        {
            return null;
        }

        if (value.FractionalSecondDigits.Length > 0 && !DateTimeLexicalHelpers.IsAsciiDigits(value.FractionalSecondDigits.AsSpan()))
        {
            return null;
        }

        if (value.Hour == 24 && (value.Minute != 0 || value.Second != 0 || HasNonzeroDigit(value.FractionalSecondDigits.AsSpan())))
        {
            return null;
        }

        string? timezone = DateTimeLexicalHelpers.FormatTimezone(value.HasTimezone, value.TimezoneOffsetMinutes, value.IsUtc, value.IsNegativeTimezoneZero);
        if (timezone == null)
        {
            return null;
        }

        string result = value.Hour.ToString("D2", CultureInfo.InvariantCulture) + ":" + value.Minute.ToString("D2", CultureInfo.InvariantCulture) + ":" + value.Second.ToString("D2", CultureInfo.InvariantCulture);
        if (value.FractionalSecondDigits.Length > 0)
        {
            result = result + "." + value.FractionalSecondDigits;
        }

        return result + timezone;
    }

    internal static IsoDateTimeValue? ParseDateTime(ReadOnlySpan<char> text)
    {
        int separatorIndex = -1;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (separatorIndex >= 0)
                {
                    return null;
                }

                separatorIndex = index;
            }

            index++;
        }

        if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
        {
            return null;
        }

        IsoDateValue? date = ParseDate(text.Slice(0, separatorIndex));
        IsoTimeValue? time = ParseTime(text.Slice(separatorIndex + 1, text.Length - separatorIndex - 1));
        if (date == null || time == null || date.HasTimezone)
        {
            return null;
        }

        date.HasTimezone = time.HasTimezone;
        date.TimezoneOffsetMinutes = time.TimezoneOffsetMinutes;
        date.IsUtc = time.IsUtc;
        date.IsNegativeTimezoneZero = time.IsNegativeTimezoneZero;
        IsoDateTimeValue result = new IsoDateTimeValue();
        result.Date = date;
        result.Time = time;
        result.LexicalMode = DateTimeLexicalMode.Preserve;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatDateTime(IsoDateTimeValue value)
    {
        if (value == null || value.Date == null || value.Time == null)
        {
            return null;
        }

        if (value.Date.HasTimezone != value.Time.HasTimezone || value.Date.TimezoneOffsetMinutes != value.Time.TimezoneOffsetMinutes || value.Date.IsUtc != value.Time.IsUtc || value.Date.IsNegativeTimezoneZero != value.Time.IsNegativeTimezoneZero)
        {
            return null;
        }

        string? date = FormatDateContent(value.Date);
        string? time = FormatTime(value.Time);
        if (date == null || time == null)
        {
            return null;
        }

        return date + "T" + time;
    }

    internal static IsoDateValue? ParseDateContent(ReadOnlySpan<char> content)
    {
        if (content.Length < 10)
        {
            return null;
        }

        int monthSeparator = content.Length - 6;
        int daySeparator = content.Length - 3;
        if (content[monthSeparator] != '-' || content[daySeparator] != '-')
        {
            return null;
        }

        ReadOnlySpan<char> yearText = content.Slice(0, monthSeparator);
        if (!IsValidYearLexical(yearText))
        {
            return null;
        }

        IntegerLexicalResult yearResult = IntegerLexical.ParseInt32(yearText);
        int month = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(monthSeparator + 1, 2));
        int day = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(daySeparator + 1, 2));
        if (!yearResult.Success || !DateTimeLexicalHelpers.IsValidDate(yearResult.Value, month, day))
        {
            return null;
        }

        IsoDateValue result = new IsoDateValue();
        result.Year = yearResult.Value;
        result.Month = month;
        result.Day = day;
        result.LexicalMode = DateTimeLexicalMode.Preserve;
        result.LexicalValue = content.ToString();
        return result;
    }

    internal static string? FormatDateContent(IsoDateValue value)
    {
        if (!DateTimeLexicalHelpers.IsValidDate(value.Year, value.Month, value.Day))
        {
            return null;
        }

        string? year = DateTimeLexicalHelpers.FormatYear(value.Year);
        if (year == null)
        {
            return null;
        }

        return year + "-" + value.Month.ToString("D2", CultureInfo.InvariantCulture) + "-" + value.Day.ToString("D2", CultureInfo.InvariantCulture);
    }

    internal static bool IsValidYearLexical(ReadOnlySpan<char> text)
    {
        if (text.Length < 4 || text[0] == '+')
        {
            return false;
        }

        int digitStart = text[0] == '-' ? 1 : 0;
        int digitCount = text.Length - digitStart;
        if (digitCount < 4)
        {
            return false;
        }

        ReadOnlySpan<char> digits = text.Slice(digitStart, digitCount);
        if (!DateTimeLexicalHelpers.IsAsciiDigits(digits))
        {
            return false;
        }

        if (digitCount > 4 && digits[0] == '0')
        {
            return false;
        }

        return true;
    }

    internal static bool HasNonzeroDigit(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] != '0')
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
