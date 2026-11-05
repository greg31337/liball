using System;

namespace http;

internal static class HttpDateCodec
{
    internal static HttpDateParseResult ParseHttpDate(byte[] input, int maximumSize)
    {
        return ParseHttpDateAt(input, maximumSize, DateTimeOffset.UtcNow);
    }

    internal static HttpDateParseResult ParseHttpDateAt(
        byte[] input,
        int maximumSize,
        DateTimeOffset referenceTime)
    {
        if (input == null)
        {
            return CreateMalformed(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimit();
        }

        if (input.Length == 29)
        {
            return ParseImfFixdate(input);
        }

        if (input.Length == 24)
        {
            return ParseAsctimeDate(input);
        }

        if (input.Length >= 30 && input.Length <= 33)
        {
            return ParseRfc850Date(input, referenceTime.ToUniversalTime());
        }

        return CreateMalformed(0);
    }

    internal static FieldValueWriteResult SerializeHttpDate(DateTimeOffset value, int maximumSize)
    {
        if (maximumSize < 29)
        {
            FieldValueWriteResult limit = new FieldValueWriteResult();
            limit.Status = HttpOperationStatus.Rejected;
            limit.RequiredSize = 29;
            limit.IsLimitExceeded = true;
            return limit;
        }

        DateTimeOffset utc = value.ToUniversalTime();
        byte[] output = new byte[29];
        string weekday = GetShortWeekday((int)utc.DayOfWeek);
        string month = GetMonthName(utc.Month);
        WriteAscii(output, 0, weekday);
        output[3] = 0x2c;
        output[4] = 0x20;
        WriteTwoDigits(output, 5, utc.Day);
        output[7] = 0x20;
        WriteAscii(output, 8, month);
        output[11] = 0x20;
        WriteFourDigits(output, 12, utc.Year);
        output[16] = 0x20;
        WriteTwoDigits(output, 17, utc.Hour);
        output[19] = 0x3a;
        WriteTwoDigits(output, 20, utc.Minute);
        output[22] = 0x3a;
        WriteTwoDigits(output, 23, utc.Second);
        output[25] = 0x20;
        output[26] = 0x47;
        output[27] = 0x4d;
        output[28] = 0x54;

        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Done;
        result.Bytes = output;
        result.Written = output.Length;
        result.RequiredSize = output.Length;
        return result;
    }

    internal static FieldValueWriteResult WriteHttpDate(
        DateTimeOffset value,
        Span<byte> output,
        int offset,
        int maximumSize)
    {
        FieldValueWriteResult serialized = SerializeHttpDate(value, maximumSize);
        if (serialized.Status != HttpOperationStatus.Done)
        {
            return serialized;
        }

        if (offset < 0)
        {
            FieldValueWriteResult malformed = new FieldValueWriteResult();
            malformed.Status = HttpOperationStatus.Rejected;
            malformed.IsMalformed = true;
            return malformed;
        }

        int requiredSize = int.MaxValue;
        if (serialized.Bytes.Length <= int.MaxValue - offset)
        {
            requiredSize = offset + serialized.Bytes.Length;
        }

        if (offset > output.Length || serialized.Bytes.Length > output.Length - offset)
        {
            FieldValueWriteResult insufficient = new FieldValueWriteResult();
            insufficient.Status = HttpOperationStatus.NeedOutput;
            insufficient.RequiredSize = requiredSize;
            return insufficient;
        }

        int index = 0;
        while (index < serialized.Bytes.Length)
        {
            output[offset + index] = serialized.Bytes[index];
            index = index + 1;
        }

        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Done;
        result.Written = serialized.Bytes.Length;
        result.RequiredSize = requiredSize;
        return result;
    }

    internal static int CompareAtSecondPrecision(DateTimeOffset left, DateTimeOffset right)
    {
        long leftSeconds = left.ToUniversalTime().Ticks / TimeSpan.TicksPerSecond;
        long rightSeconds = right.ToUniversalTime().Ticks / TimeSpan.TicksPerSecond;
        if (leftSeconds < rightSeconds)
        {
            return -1;
        }

        if (leftSeconds > rightSeconds)
        {
            return 1;
        }

        return 0;
    }

    internal static bool EqualsAtSecondPrecision(DateTimeOffset left, DateTimeOffset right)
    {
        return CompareAtSecondPrecision(left, right) == 0;
    }

    static HttpDateParseResult ParseImfFixdate(byte[] input)
    {
        int weekday = ReadShortWeekday(input, 0);
        if (weekday < 0 || input[3] != 0x2c || input[4] != 0x20 ||
            input[7] != 0x20 || input[11] != 0x20 || input[16] != 0x20 ||
            input[19] != 0x3a || input[22] != 0x3a || input[25] != 0x20 ||
            !MatchesAscii(input, 26, "GMT"))
        {
            return CreateMalformed(0);
        }

        int day = ReadFixedDigits(input, 5, 2);
        int month = ReadMonth(input, 8);
        int year = ReadFixedDigits(input, 12, 4);
        int hour = ReadFixedDigits(input, 17, 2);
        int minute = ReadFixedDigits(input, 20, 2);
        int second = ReadFixedDigits(input, 23, 2);
        return CreateDateResult(weekday, day, month, year, hour, minute, second, input.Length);
    }

    static HttpDateParseResult ParseRfc850Date(byte[] input, DateTimeOffset referenceTime)
    {
        int comma = 0;
        while (comma < input.Length && input[comma] != 0x2c)
        {
            comma = comma + 1;
        }

        int weekday = ReadLongWeekday(input, comma);
        if (weekday < 0 || comma + 24 != input.Length || input[comma + 1] != 0x20)
        {
            return CreateMalformed(0);
        }

        int start = comma + 2;
        if (input[start + 2] != 0x2d || input[start + 6] != 0x2d ||
            input[start + 9] != 0x20 || input[start + 12] != 0x3a ||
            input[start + 15] != 0x3a || input[start + 18] != 0x20 ||
            !MatchesAscii(input, start + 19, "GMT"))
        {
            return CreateMalformed(start);
        }

        int day = ReadFixedDigits(input, start, 2);
        int month = ReadMonth(input, start + 3);
        int shortYear = ReadFixedDigits(input, start + 7, 2);
        int hour = ReadFixedDigits(input, start + 10, 2);
        int minute = ReadFixedDigits(input, start + 13, 2);
        int second = ReadFixedDigits(input, start + 16, 2);
        if (shortYear < 0)
        {
            return CreateMalformed(start + 7);
        }

        int year = 2000 + shortYear;
        if (!AreValidDateParts(day, month, year, hour, minute, second))
        {
            return CreateMalformed(start);
        }

        DateTimeOffset candidate = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
        DateTimeOffset futureLimit = DateTimeOffset.MaxValue;
        if (referenceTime.Year <= 9949)
        {
            futureLimit = referenceTime.AddYears(50);
        }

        if (candidate > futureLimit)
        {
            year = year - 100;
        }

        return CreateDateResult(weekday, day, month, year, hour, minute, second, input.Length);
    }

    static HttpDateParseResult ParseAsctimeDate(byte[] input)
    {
        int weekday = ReadShortWeekday(input, 0);
        if (weekday < 0 || input[3] != 0x20 || input[7] != 0x20 ||
            input[10] != 0x20 || input[13] != 0x3a || input[16] != 0x3a ||
            input[19] != 0x20)
        {
            return CreateMalformed(0);
        }

        int month = ReadMonth(input, 4);
        int day;
        if (input[8] == 0x20)
        {
            day = ReadFixedDigits(input, 9, 1);
        }
        else
        {
            day = ReadFixedDigits(input, 8, 2);
        }

        int hour = ReadFixedDigits(input, 11, 2);
        int minute = ReadFixedDigits(input, 14, 2);
        int second = ReadFixedDigits(input, 17, 2);
        int year = ReadFixedDigits(input, 20, 4);
        return CreateDateResult(weekday, day, month, year, hour, minute, second, input.Length);
    }

    static HttpDateParseResult CreateDateResult(
        int weekday,
        int day,
        int month,
        int year,
        int hour,
        int minute,
        int second,
        int consumed)
    {
        if (!AreValidDateParts(day, month, year, hour, minute, second))
        {
            return CreateMalformed(0);
        }

        DateTimeOffset value = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
        if ((int)value.DayOfWeek != weekday)
        {
            return CreateMalformed(0);
        }

        HttpDateParseResult result = new HttpDateParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        result.Value = value;
        return result;
    }

    static bool AreValidDateParts(int day, int month, int year, int hour, int minute, int second)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12 ||
            day < 1 || hour < 0 || hour > 23 || minute < 0 || minute > 59 ||
            second < 0 || second > 59)
        {
            return false;
        }

        return day <= DateTime.DaysInMonth(year, month);
    }

    static int ReadFixedDigits(byte[] input, int start, int count)
    {
        if (start < 0 || count < 1 || start > input.Length || count > input.Length - start)
        {
            return -1;
        }

        int value = 0;
        int index = 0;
        while (index < count)
        {
            byte character = input[start + index];
            if (character < 0x30 || character > 0x39)
            {
                return -1;
            }

            value = value * 10 + character - 0x30;
            index = index + 1;
        }

        return value;
    }

    static int ReadMonth(byte[] input, int start)
    {
        if (MatchesAscii(input, start, "Jan"))
        {
            return 1;
        }

        if (MatchesAscii(input, start, "Feb"))
        {
            return 2;
        }

        if (MatchesAscii(input, start, "Mar"))
        {
            return 3;
        }

        if (MatchesAscii(input, start, "Apr"))
        {
            return 4;
        }

        if (MatchesAscii(input, start, "May"))
        {
            return 5;
        }

        if (MatchesAscii(input, start, "Jun"))
        {
            return 6;
        }

        if (MatchesAscii(input, start, "Jul"))
        {
            return 7;
        }

        if (MatchesAscii(input, start, "Aug"))
        {
            return 8;
        }

        if (MatchesAscii(input, start, "Sep"))
        {
            return 9;
        }

        if (MatchesAscii(input, start, "Oct"))
        {
            return 10;
        }

        if (MatchesAscii(input, start, "Nov"))
        {
            return 11;
        }

        if (MatchesAscii(input, start, "Dec"))
        {
            return 12;
        }

        return -1;
    }

    static int ReadShortWeekday(byte[] input, int start)
    {
        if (MatchesAscii(input, start, "Sun"))
        {
            return 0;
        }

        if (MatchesAscii(input, start, "Mon"))
        {
            return 1;
        }

        if (MatchesAscii(input, start, "Tue"))
        {
            return 2;
        }

        if (MatchesAscii(input, start, "Wed"))
        {
            return 3;
        }

        if (MatchesAscii(input, start, "Thu"))
        {
            return 4;
        }

        if (MatchesAscii(input, start, "Fri"))
        {
            return 5;
        }

        if (MatchesAscii(input, start, "Sat"))
        {
            return 6;
        }

        return -1;
    }

    static int ReadLongWeekday(byte[] input, int end)
    {
        if (MatchesAsciiRange(input, 0, end, "Sunday"))
        {
            return 0;
        }

        if (MatchesAsciiRange(input, 0, end, "Monday"))
        {
            return 1;
        }

        if (MatchesAsciiRange(input, 0, end, "Tuesday"))
        {
            return 2;
        }

        if (MatchesAsciiRange(input, 0, end, "Wednesday"))
        {
            return 3;
        }

        if (MatchesAsciiRange(input, 0, end, "Thursday"))
        {
            return 4;
        }

        if (MatchesAsciiRange(input, 0, end, "Friday"))
        {
            return 5;
        }

        if (MatchesAsciiRange(input, 0, end, "Saturday"))
        {
            return 6;
        }

        return -1;
    }

    static bool MatchesAscii(byte[] input, int start, string expected)
    {
        if (start < 0 || start > input.Length || expected.Length > input.Length - start)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (input[start + index] != (byte)expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool MatchesAsciiRange(byte[] input, int start, int end, string expected)
    {
        if (end - start != expected.Length)
        {
            return false;
        }

        return MatchesAscii(input, start, expected);
    }

    static string GetShortWeekday(int weekday)
    {
        if (weekday == 0)
        {
            return "Sun";
        }

        if (weekday == 1)
        {
            return "Mon";
        }

        if (weekday == 2)
        {
            return "Tue";
        }

        if (weekday == 3)
        {
            return "Wed";
        }

        if (weekday == 4)
        {
            return "Thu";
        }

        if (weekday == 5)
        {
            return "Fri";
        }

        return "Sat";
    }

    static string GetMonthName(int month)
    {
        if (month == 1)
        {
            return "Jan";
        }

        if (month == 2)
        {
            return "Feb";
        }

        if (month == 3)
        {
            return "Mar";
        }

        if (month == 4)
        {
            return "Apr";
        }

        if (month == 5)
        {
            return "May";
        }

        if (month == 6)
        {
            return "Jun";
        }

        if (month == 7)
        {
            return "Jul";
        }

        if (month == 8)
        {
            return "Aug";
        }

        if (month == 9)
        {
            return "Sep";
        }

        if (month == 10)
        {
            return "Oct";
        }

        if (month == 11)
        {
            return "Nov";
        }

        return "Dec";
    }

    static void WriteAscii(byte[] output, int start, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            output[start + index] = (byte)value[index];
            index = index + 1;
        }
    }

    static void WriteTwoDigits(byte[] output, int start, int value)
    {
        output[start] = (byte)(0x30 + value / 10);
        output[start + 1] = (byte)(0x30 + value % 10);
    }

    static void WriteFourDigits(byte[] output, int start, int value)
    {
        output[start] = (byte)(0x30 + value / 1000);
        output[start + 1] = (byte)(0x30 + value / 100 % 10);
        output[start + 2] = (byte)(0x30 + value / 10 % 10);
        output[start + 3] = (byte)(0x30 + value % 10);
    }

    static HttpDateParseResult CreateMalformed(int errorPosition)
    {
        HttpDateParseResult result = new HttpDateParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static HttpDateParseResult CreateLimit()
    {
        HttpDateParseResult result = new HttpDateParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }
}
