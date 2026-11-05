using System;

namespace fix;

internal static class TimestampFormatter
{
    internal static ScalarFormatResult FormatTimestamp(TimestampParseResult value, Span<byte> destination)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        if (!value.Success)
        {
            result.ErrorCode = "invalid-temporal-value";
            return result;
        }
        int required = GetFormattedLength(value);
        if (required < 0)
        {
            result.ErrorCode = "unsupported-field-type";
            return result;
        }
        result.Required = required;
        if (destination.Length < required)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }

        int index = 0;
        if (value.FieldType == FixFieldType.MonthYear)
        {
            WriteFourDigits(value.Year, destination, index);
            WriteTwoDigits(value.Month, destination, index + 4);
            index = 6;
            if (value.HasDay)
            {
                WriteTwoDigits(value.Day, destination, index);
                index += 2;
            }
            else if (value.HasWeek)
            {
                destination[index] = 119;
                destination[index + 1] = (byte)(48 + value.Week);
                index += 2;
            }
        }
        else if (value.FieldType == FixFieldType.DayOfMonth)
        {
            if (value.Day < 10)
            {
                destination[0] = (byte)(48 + value.Day);
                index = 1;
            }
            else
            {
                WriteTwoDigits(value.Day, destination, 0);
                index = 2;
            }
        }
        else if (value.FieldType == FixFieldType.LocalMktDate || value.FieldType == FixFieldType.UtcDateOnly)
        {
            index = WriteDate(value, destination, 0);
        }
        else if (value.FieldType == FixFieldType.UtcTimeOnly)
        {
            index = WriteClockWithSeconds(value, destination, 0);
            index = WriteFraction(value, destination, index);
        }
        else if (value.FieldType == FixFieldType.UtcTimestamp)
        {
            index = WriteDate(value, destination, 0);
            destination[index] = 45;
            index = WriteClockWithSeconds(value, destination, index + 1);
            index = WriteFraction(value, destination, index);
        }
        else if (value.FieldType == FixFieldType.TzTimeOnly)
        {
            WriteTwoDigits(value.Hour, destination, 0);
            destination[2] = 58;
            WriteTwoDigits(value.Minute, destination, 3);
            index = 5;
            if (value.HasSecond)
            {
                destination[index] = 58;
                WriteTwoDigits(value.Second, destination, index + 1);
                index += 3;
            }
            index = WriteTimezone(value, destination, index);
        }
        else if (value.FieldType == FixFieldType.TzTimestamp)
        {
            index = WriteDate(value, destination, 0);
            destination[index] = 45;
            index = WriteClockWithSeconds(value, destination, index + 1);
            index = WriteFraction(value, destination, index);
            index = WriteTimezone(value, destination, index);
        }

        result.Success = true;
        result.Written = index;
        return result;
    }

    internal static int GetFormattedLength(TimestampParseResult value)
    {
        if (value.FieldType == FixFieldType.MonthYear)
        {
            return value.HasDay || value.HasWeek ? 8 : 6;
        }
        if (value.FieldType == FixFieldType.DayOfMonth)
        {
            return value.Day < 10 ? 1 : 2;
        }
        if (value.FieldType == FixFieldType.LocalMktDate || value.FieldType == FixFieldType.UtcDateOnly)
        {
            return 8;
        }
        int fractionLength = value.HasFraction ? value.FractionalDigits + 1 : 0;
        if (value.FieldType == FixFieldType.UtcTimeOnly)
        {
            return 8 + fractionLength;
        }
        if (value.FieldType == FixFieldType.UtcTimestamp)
        {
            return 17 + fractionLength;
        }
        int timezoneLength = GetTimezoneLength(value);
        if (value.FieldType == FixFieldType.TzTimeOnly)
        {
            return 5 + (value.HasSecond ? 3 : 0) + timezoneLength;
        }
        if (value.FieldType == FixFieldType.TzTimestamp)
        {
            return 17 + fractionLength + timezoneLength;
        }
        return -1;
    }

    internal static int GetTimezoneLength(TimestampParseResult value)
    {
        if (!value.HasOffset)
        {
            return 0;
        }
        if (value.IsUtcZulu)
        {
            return 1;
        }
        return value.HasOffsetMinute ? 6 : 3;
    }

    internal static int WriteDate(TimestampParseResult value, Span<byte> destination, int start)
    {
        WriteFourDigits(value.Year, destination, start);
        WriteTwoDigits(value.Month, destination, start + 4);
        WriteTwoDigits(value.Day, destination, start + 6);
        return start + 8;
    }

    internal static int WriteClockWithSeconds(TimestampParseResult value, Span<byte> destination, int start)
    {
        WriteTwoDigits(value.Hour, destination, start);
        destination[start + 2] = 58;
        WriteTwoDigits(value.Minute, destination, start + 3);
        destination[start + 5] = 58;
        WriteTwoDigits(value.Second, destination, start + 6);
        return start + 8;
    }

    internal static int WriteFraction(TimestampParseResult value, Span<byte> destination, int start)
    {
        if (!value.HasFraction)
        {
            return start;
        }
        destination[start] = 46;
        for (int index = 0; index < value.Fraction.Length; index++)
        {
            destination[start + 1 + index] = (byte)value.Fraction[index];
        }
        return start + 1 + value.Fraction.Length;
    }

    internal static int WriteTimezone(TimestampParseResult value, Span<byte> destination, int start)
    {
        if (!value.HasOffset)
        {
            return start;
        }
        if (value.IsUtcZulu)
        {
            destination[start] = 90;
            return start + 1;
        }
        destination[start] = value.OffsetSign < 0 ? (byte)45 : (byte)43;
        WriteTwoDigits(value.OffsetHour, destination, start + 1);
        int end = start + 3;
        if (value.HasOffsetMinute)
        {
            destination[end] = 58;
            WriteTwoDigits(value.OffsetMinute, destination, end + 1);
            end += 3;
        }
        return end;
    }

    internal static void WriteTwoDigits(int value, Span<byte> destination, int start)
    {
        destination[start] = (byte)(48 + value / 10);
        destination[start + 1] = (byte)(48 + value % 10);
    }

    internal static void WriteFourDigits(int value, Span<byte> destination, int start)
    {
        destination[start] = (byte)(48 + value / 1000 % 10);
        destination[start + 1] = (byte)(48 + value / 100 % 10);
        destination[start + 2] = (byte)(48 + value / 10 % 10);
        destination[start + 3] = (byte)(48 + value % 10);
    }
}
