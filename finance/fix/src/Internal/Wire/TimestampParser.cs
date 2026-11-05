using System;
using System.Text;

namespace fix;

internal static class TimestampParser
{
    internal static TimestampParseResult ParseTimestamp(ReadOnlySpan<byte> source, FixFieldType fieldType, int maximumLength)
    {
        LimitCheckResult limit = WireLimit.CheckValue(source.Length, maximumLength);
        if (!limit.Allowed)
        {
            TimestampParseResult limited = new TimestampParseResult();
            limited.FieldType = fieldType;
            limited.ErrorCode = limit.ErrorCode;
            return limited;
        }
        if (fieldType == FixFieldType.MonthYear)
        {
            return ParseMonthYearValue(source);
        }
        if (fieldType == FixFieldType.DayOfMonth)
        {
            return ParseDayOfMonthValue(source);
        }
        if (fieldType == FixFieldType.LocalMktDate || fieldType == FixFieldType.UtcDateOnly)
        {
            return ParseDateOnlyValue(source, fieldType);
        }
        if (fieldType == FixFieldType.UtcTimeOnly)
        {
            return ParseUtcTimeOnlyValue(source);
        }
        if (fieldType == FixFieldType.UtcTimestamp)
        {
            return ParseUtcTimestampValue(source);
        }
        if (fieldType == FixFieldType.TzTimeOnly)
        {
            return ParseTzTimeOnlyValue(source);
        }
        if (fieldType == FixFieldType.TzTimestamp)
        {
            return ParseTzTimestampValue(source);
        }

        TimestampParseResult unsupported = new TimestampParseResult();
        unsupported.FieldType = fieldType;
        unsupported.ErrorCode = "unsupported-field-type";
        return unsupported;
    }

    internal static TimestampParseResult ParseMonthYearValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.MonthYear;
        if (source.Length != 6 && source.Length != 8)
        {
            SetLengthError(result, source.Length, 6);
            return result;
        }
        if (!ParseYearMonth(source, 0, result))
        {
            return result;
        }
        if (source.Length == 8)
        {
            if (source[6] == 119 || source[6] == 87)
            {
                if (!AsciiDigits.IsDigit(source[7]))
                {
                    SetError(result, 7, "invalid-digit");
                    return result;
                }
                result.Week = source[7] - 48;
                if (result.Week < 1 || result.Week > 5)
                {
                    SetError(result, 7, "invalid-week");
                    return result;
                }
                result.HasWeek = true;
            }
            else
            {
                AsciiIntegerResult day = AsciiDigits.ParseUnsignedInt(source.Slice(6, 2), 31);
                if (!day.Success)
                {
                    CopyDigitError(result, day, 6);
                    return result;
                }
                result.Day = day.Value;
                if (!IsValidDate(result.Year, result.Month, result.Day))
                {
                    SetError(result, 6, "invalid-calendar-date");
                    return result;
                }
                result.HasDay = true;
            }
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseDayOfMonthValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.DayOfMonth;
        if (source.Length < 1 || source.Length > 2)
        {
            SetLengthError(result, source.Length, 1);
            return result;
        }
        AsciiIntegerResult day = AsciiDigits.ParseUnsignedInt(source, 31);
        if (!day.Success)
        {
            CopyDigitError(result, day, 0);
            return result;
        }
        if (day.Value < 1)
        {
            SetError(result, 0, "invalid-day-of-month");
            return result;
        }
        result.Day = day.Value;
        result.HasDay = true;
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseDateOnlyValue(ReadOnlySpan<byte> source, FixFieldType fieldType)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = fieldType;
        if (source.Length != 8)
        {
            SetLengthError(result, source.Length, 8);
            return result;
        }
        if (!ParseCalendarDate(source, 0, result))
        {
            return result;
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseUtcTimeOnlyValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.UtcTimeOnly;
        if (source.Length < 8)
        {
            SetLengthError(result, source.Length, 8);
            return result;
        }
        if (!ParseClockWithSeconds(source, 0, true, result))
        {
            return result;
        }
        if (!ParseFractionToEnd(source, 8, result))
        {
            return result;
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseUtcTimestampValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.UtcTimestamp;
        if (source.Length < 17)
        {
            SetLengthError(result, source.Length, 17);
            return result;
        }
        if (!ParseCalendarDate(source, 0, result))
        {
            return result;
        }
        if (source[8] != 45)
        {
            SetError(result, 8, "invalid-date-time-separator");
            return result;
        }
        if (!ParseClockWithSeconds(source, 9, true, result))
        {
            return result;
        }
        if (result.Second == 60 && !IsLeapSecondDate(result.Month, result.Day))
        {
            SetError(result, 15, "invalid-leap-second-date");
            return result;
        }
        if (!ParseFractionToEnd(source, 17, result))
        {
            return result;
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseTzTimeOnlyValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.TzTimeOnly;
        if (source.Length < 5)
        {
            SetLengthError(result, source.Length, 5);
            return result;
        }
        if (!ParseClockWithoutRequiredSeconds(source, result))
        {
            return result;
        }
        int suffixStart = result.HasSecond ? 8 : 5;
        if (!ParseTimezoneSuffix(source, suffixStart, result))
        {
            return result;
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static TimestampParseResult ParseTzTimestampValue(ReadOnlySpan<byte> source)
    {
        TimestampParseResult result = new TimestampParseResult();
        result.FieldType = FixFieldType.TzTimestamp;
        if (source.Length < 17)
        {
            SetLengthError(result, source.Length, 17);
            return result;
        }
        if (!ParseCalendarDate(source, 0, result))
        {
            return result;
        }
        if (source[8] != 45)
        {
            SetError(result, 8, "invalid-date-time-separator");
            return result;
        }
        if (!ParseClockWithSeconds(source, 9, false, result))
        {
            return result;
        }

        int suffixStart = 17;
        if (suffixStart < source.Length && source[suffixStart] == 46)
        {
            int fractionEnd = suffixStart + 1;
            while (fractionEnd < source.Length && AsciiDigits.IsDigit(source[fractionEnd]))
            {
                fractionEnd++;
            }
            if (!ParseFractionRange(source, suffixStart, fractionEnd, result))
            {
                return result;
            }
            suffixStart = fractionEnd;
        }
        if (!ParseTimezoneSuffix(source, suffixStart, result))
        {
            return result;
        }
        CompleteResult(result, source.Length);
        return result;
    }

    internal static bool ParseCalendarDate(ReadOnlySpan<byte> source, int start, TimestampParseResult result)
    {
        if (!ParseYearMonth(source, start, result))
        {
            return false;
        }
        AsciiIntegerResult day = AsciiDigits.ParseUnsignedInt(source.Slice(start + 6, 2), 31);
        if (!day.Success)
        {
            CopyDigitError(result, day, start + 6);
            return false;
        }
        result.Day = day.Value;
        result.HasDay = true;
        if (!IsValidDate(result.Year, result.Month, result.Day))
        {
            SetError(result, start + 6, "invalid-calendar-date");
            return false;
        }
        return true;
    }

    internal static bool ParseYearMonth(ReadOnlySpan<byte> source, int start, TimestampParseResult result)
    {
        AsciiIntegerResult year = AsciiDigits.ParseUnsignedInt(source.Slice(start, 4), 9999);
        if (!year.Success)
        {
            CopyDigitError(result, year, start);
            return false;
        }
        AsciiIntegerResult month = AsciiDigits.ParseUnsignedInt(source.Slice(start + 4, 2), 12);
        if (!month.Success)
        {
            CopyDigitError(result, month, start + 4);
            return false;
        }
        if (month.Value < 1)
        {
            SetError(result, start + 4, "invalid-month");
            return false;
        }
        result.Year = year.Value;
        result.Month = month.Value;
        return true;
    }

    internal static bool ParseClockWithSeconds(ReadOnlySpan<byte> source, int start, bool allowLeapSecond, TimestampParseResult result)
    {
        if (source[start + 2] != 58 || source[start + 5] != 58)
        {
            int separator = source[start + 2] != 58 ? start + 2 : start + 5;
            SetError(result, separator, "invalid-time-separator");
            return false;
        }
        AsciiIntegerResult hour = AsciiDigits.ParseUnsignedInt(source.Slice(start, 2), 23);
        AsciiIntegerResult minute = AsciiDigits.ParseUnsignedInt(source.Slice(start + 3, 2), 59);
        AsciiIntegerResult second = AsciiDigits.ParseUnsignedInt(source.Slice(start + 6, 2), allowLeapSecond ? 60 : 59);
        if (!hour.Success)
        {
            CopyDigitError(result, hour, start);
            return false;
        }
        if (!minute.Success)
        {
            CopyDigitError(result, minute, start + 3);
            return false;
        }
        if (!second.Success)
        {
            CopyDigitError(result, second, start + 6);
            return false;
        }
        if (second.Value == 60 && (hour.Value != 23 || minute.Value != 59))
        {
            SetError(result, start + 6, "invalid-leap-second-time");
            return false;
        }
        result.Hour = hour.Value;
        result.Minute = minute.Value;
        result.Second = second.Value;
        result.HasSecond = true;
        return true;
    }

    internal static bool ParseClockWithoutRequiredSeconds(ReadOnlySpan<byte> source, TimestampParseResult result)
    {
        if (source[2] != 58)
        {
            SetError(result, 2, "invalid-time-separator");
            return false;
        }
        AsciiIntegerResult hour = AsciiDigits.ParseUnsignedInt(source.Slice(0, 2), 23);
        AsciiIntegerResult minute = AsciiDigits.ParseUnsignedInt(source.Slice(3, 2), 59);
        if (!hour.Success)
        {
            CopyDigitError(result, hour, 0);
            return false;
        }
        if (!minute.Success)
        {
            CopyDigitError(result, minute, 3);
            return false;
        }
        result.Hour = hour.Value;
        result.Minute = minute.Value;
        if (source.Length > 5 && source[5] == 58)
        {
            if (source.Length < 8)
            {
                SetLengthError(result, source.Length, 8);
                return false;
            }
            AsciiIntegerResult second = AsciiDigits.ParseUnsignedInt(source.Slice(6, 2), 59);
            if (!second.Success)
            {
                CopyDigitError(result, second, 6);
                return false;
            }
            result.Second = second.Value;
            result.HasSecond = true;
        }
        return true;
    }

    internal static bool ParseFractionToEnd(ReadOnlySpan<byte> source, int start, TimestampParseResult result)
    {
        if (source.Length == start)
        {
            return true;
        }
        return ParseFractionRange(source, start, source.Length, result);
    }

    internal static bool ParseFractionRange(ReadOnlySpan<byte> source, int start, int end, TimestampParseResult result)
    {
        if (start >= source.Length || source[start] != 46)
        {
            SetError(result, start, "invalid-fraction-separator");
            return false;
        }
        int digits = end - start - 1;
        if (!IsSupportedPrecision(digits))
        {
            SetError(result, start + 1, "unsupported-precision");
            return false;
        }
        for (int index = start + 1; index < end; index++)
        {
            if (!AsciiDigits.IsDigit(source[index]))
            {
                SetError(result, index, "invalid-fraction-digit");
                return false;
            }
        }
        result.HasFraction = true;
        result.FractionalDigits = digits;
        result.Fraction = Encoding.ASCII.GetString(source.Slice(start + 1, digits));
        return true;
    }

    internal static bool ParseTimezoneSuffix(ReadOnlySpan<byte> source, int start, TimestampParseResult result)
    {
        if (start == source.Length)
        {
            return true;
        }
        if (source[start] == 90)
        {
            if (source.Length != start + 1)
            {
                SetError(result, start + 1, "invalid-timezone");
                return false;
            }
            result.HasOffset = true;
            result.IsUtcZulu = true;
            return true;
        }
        if (source[start] != 43 && source[start] != 45)
        {
            SetError(result, start, "invalid-timezone");
            return false;
        }
        int suffixLength = source.Length - start;
        if (suffixLength != 3 && suffixLength != 6)
        {
            SetLengthError(result, suffixLength, 3);
            result.ErrorIndex = start;
            return false;
        }
        AsciiIntegerResult hour = AsciiDigits.ParseUnsignedInt(source.Slice(start + 1, 2), 12);
        if (!hour.Success)
        {
            CopyDigitError(result, hour, start + 1);
            return false;
        }
        if (hour.Value < 1)
        {
            SetError(result, start + 1, "invalid-timezone-hour");
            return false;
        }
        int minuteValue = 0;
        if (suffixLength == 6)
        {
            if (source[start + 3] != 58)
            {
                SetError(result, start + 3, "invalid-timezone-separator");
                return false;
            }
            AsciiIntegerResult minute = AsciiDigits.ParseUnsignedInt(source.Slice(start + 4, 2), 59);
            if (!minute.Success)
            {
                CopyDigitError(result, minute, start + 4);
                return false;
            }
            minuteValue = minute.Value;
            result.HasOffsetMinute = true;
        }
        result.HasOffset = true;
        result.OffsetSign = source[start] == 43 ? 1 : -1;
        result.OffsetHour = hour.Value;
        result.OffsetMinute = minuteValue;
        return true;
    }

    internal static bool IsSupportedPrecision(int digits)
    {
        return digits == 3 || digits == 6 || digits == 9 || digits == 12;
    }

    internal static bool IsValidDate(int year, int month, int day)
    {
        if (month < 1 || month > 12 || day < 1)
        {
            return false;
        }
        return day <= DaysInMonth(year, month);
    }

    internal static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            return IsLeapYear(year) ? 29 : 28;
        }
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }
        return 31;
    }

    internal static bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }

    internal static bool IsLeapSecondDate(int month, int day)
    {
        return month == 6 && day == 30 || month == 12 && day == 31;
    }

    internal static void SetLengthError(TimestampParseResult result, int actual, int required)
    {
        result.Required = required;
        result.ErrorIndex = actual < required ? actual : required;
        result.ErrorCode = actual < required ? "incomplete" : "invalid-length";
    }

    internal static void CopyDigitError(TimestampParseResult result, AsciiIntegerResult parsed, int start)
    {
        result.ErrorIndex = start + parsed.ErrorIndex;
        result.ErrorCode = parsed.ErrorCode;
    }

    internal static void SetError(TimestampParseResult result, int index, string errorCode)
    {
        result.ErrorIndex = index;
        result.ErrorCode = errorCode;
    }

    internal static void CompleteResult(TimestampParseResult result, int length)
    {
        result.Success = true;
        result.Consumed = length;
        result.Required = length;
    }
}
