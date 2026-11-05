using System;
using System.Globalization;

namespace tiff;

internal static class TiffDateTimeCodec
{
    internal static TiffDateTimeParseResult Parse(string text)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        string actualText = text == null ? "" : text;
        DateTime value = default(DateTime);
        if (actualText.Length != 19 || actualText[4] != ':' || actualText[7] != ':' || actualText[10] != ' ' || actualText[13] != ':' || actualText[16] != ':')
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "TIFF date/time text must use YYYY:MM:DD HH:MM:SS form.");
            return TiffDateTimeParseResult.Create(value, actualText, diagnostics);
        }

        if (!HasDigitsAtExpectedPositions(actualText))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "TIFF date/time components must contain decimal digits.");
            return TiffDateTimeParseResult.Create(value, actualText, diagnostics);
        }

        int year = ReadFourDigits(actualText, 0);
        int month = ReadTwoDigits(actualText, 5);
        int day = ReadTwoDigits(actualText, 8);
        int hour = ReadTwoDigits(actualText, 11);
        int minute = ReadTwoDigits(actualText, 14);
        int second = ReadTwoDigits(actualText, 17);
        if (year < 1 || month < 1 || month > 12 || hour > 23 || minute > 59 || second > 59)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "TIFF date/time text contains a component outside its valid range.");
            return TiffDateTimeParseResult.Create(value, actualText, diagnostics);
        }

        int daysInMonth = DateTime.DaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "TIFF date/time text contains an invalid calendar date.");
            return TiffDateTimeParseResult.Create(value, actualText, diagnostics);
        }

        value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        return TiffDateTimeParseResult.Create(value, actualText, diagnostics);
    }

    internal static string Format(DateTime value)
    {
        return value.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    internal static bool HasDigitsAtExpectedPositions(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (index != 4 && index != 7 && index != 10 && index != 13 && index != 16 && !IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static int ReadTwoDigits(string text, int offset)
    {
        return (text[offset] - '0') * 10 + text[offset + 1] - '0';
    }

    internal static int ReadFourDigits(string text, int offset)
    {
        return (text[offset] - '0') * 1000 + (text[offset + 1] - '0') * 100 + (text[offset + 2] - '0') * 10 + text[offset + 3] - '0';
    }
}
