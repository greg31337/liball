using System;
using System.Globalization;

namespace iso20022;

internal static class PartialDateLexical
{
    internal static IsoPartialDateValue? ParsePartialDate(ReadOnlySpan<char> text)
    {
        TimezoneLexicalResult timezone = DateTimeLexicalHelpers.ParseTimezone(text);
        if (!timezone.Success)
        {
            return null;
        }

        ReadOnlySpan<char> content = text.Slice(0, timezone.ContentLength);
        IsoPartialDateValue? result = ParsePartialDateContent(content);
        if (result == null)
        {
            return null;
        }

        result.HasTimezone = timezone.HasTimezone;
        result.TimezoneOffsetMinutes = timezone.OffsetMinutes;
        result.IsUtc = timezone.IsUtc;
        result.IsNegativeTimezoneZero = timezone.IsNegativeZero;
        result.LexicalValue = text.ToString();
        return result;
    }

    internal static string? FormatPartialDate(IsoPartialDateValue value)
    {
        if (value == null)
        {
            return null;
        }

        string? content = FormatPartialDateContent(value);
        string? timezone = DateTimeLexicalHelpers.FormatTimezone(value.HasTimezone, value.TimezoneOffsetMinutes, value.IsUtc, value.IsNegativeTimezoneZero);
        if (content == null || timezone == null)
        {
            return null;
        }

        return content + timezone;
    }

    internal static IsoPartialDateValue? ParsePartialDateContent(ReadOnlySpan<char> content)
    {
        IsoPartialDateValue result = new IsoPartialDateValue();
        if (content.Length == 5 && content[0] == '-' && content[1] == '-' && content[2] == '-')
        {
            int day = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(3, 2));
            if (day < 1 || day > 31)
            {
                return null;
            }

            result.Kind = PartialDateKind.Day;
            result.Day = day;
            return result;
        }

        if (content.Length == 4 && content[0] == '-' && content[1] == '-')
        {
            int month = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(2, 2));
            if (month < 1 || month > 12)
            {
                return null;
            }

            result.Kind = PartialDateKind.Month;
            result.Month = month;
            return result;
        }

        if (content.Length == 7 && content[0] == '-' && content[1] == '-' && content[4] == '-')
        {
            int month = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(2, 2));
            int day = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(5, 2));
            if (!DateTimeLexicalHelpers.IsValidDate(2000, month, day))
            {
                return null;
            }

            result.Kind = PartialDateKind.MonthDay;
            result.Month = month;
            result.Day = day;
            return result;
        }

        int separatorIndex = FindYearMonthSeparator(content);
        if (separatorIndex >= 0)
        {
            ReadOnlySpan<char> yearText = content.Slice(0, separatorIndex);
            if (!DateTimeLexical.IsValidYearLexical(yearText))
            {
                return null;
            }

            IntegerLexicalResult yearResult = IntegerLexical.ParseInt32(yearText);
            int month = DateTimeLexicalHelpers.ParseTwoDigits(content.Slice(separatorIndex + 1, 2));
            if (!yearResult.Success || yearResult.Value == 0 || month < 1 || month > 12)
            {
                return null;
            }

            result.Kind = PartialDateKind.YearMonth;
            result.Year = yearResult.Value;
            result.Month = month;
            return result;
        }

        if (!DateTimeLexical.IsValidYearLexical(content))
        {
            return null;
        }

        IntegerLexicalResult yearOnlyResult = IntegerLexical.ParseInt32(content);
        if (!yearOnlyResult.Success || yearOnlyResult.Value == 0)
        {
            return null;
        }

        result.Kind = PartialDateKind.Year;
        result.Year = yearOnlyResult.Value;
        return result;
    }

    internal static string? FormatPartialDateContent(IsoPartialDateValue value)
    {
        switch (value.Kind)
        {
            case PartialDateKind.Year:
                return DateTimeLexicalHelpers.FormatYear(value.Year);
            case PartialDateKind.Month:
                if (value.Month < 1 || value.Month > 12)
                {
                    return null;
                }

                return "--" + value.Month.ToString("D2", CultureInfo.InvariantCulture);
            case PartialDateKind.Day:
                if (value.Day < 1 || value.Day > 31)
                {
                    return null;
                }

                return "---" + value.Day.ToString("D2", CultureInfo.InvariantCulture);
            case PartialDateKind.YearMonth:
                if (value.Month < 1 || value.Month > 12)
                {
                    return null;
                }

                string? year = DateTimeLexicalHelpers.FormatYear(value.Year);
                if (year == null)
                {
                    return null;
                }

                return year + "-" + value.Month.ToString("D2", CultureInfo.InvariantCulture);
            case PartialDateKind.MonthDay:
                if (!DateTimeLexicalHelpers.IsValidDate(2000, value.Month, value.Day))
                {
                    return null;
                }

                return "--" + value.Month.ToString("D2", CultureInfo.InvariantCulture) + "-" + value.Day.ToString("D2", CultureInfo.InvariantCulture);
            default:
                return null;
        }
    }

    internal static int FindYearMonthSeparator(ReadOnlySpan<char> content)
    {
        if (content.Length < 7)
        {
            return -1;
        }

        int separatorIndex = content.Length - 3;
        if (content[separatorIndex] != '-')
        {
            return -1;
        }

        return separatorIndex;
    }
}
