using System;

namespace image;

internal static class MetadataDateParserCore
{
    internal static MetadataDate Parse(string? text)
    {
        MetadataDate result = new MetadataDate();
        result.OriginalText = text == null ? "" : text;
        if (text == null)
        {
            return result;
        }

        string value = text.Trim();
        if (value.Length >= 2 && value[0] == '(' && value[value.Length - 1] == ')')
        {
            value = value.Substring(1, value.Length - 2).Trim();
        }

        if (value.Length == 0)
        {
            return result;
        }

        MetadataDateParts parts = new MetadataDateParts();
        bool parsed;
        if (value.Length > 2 && (value[0] == 'D' || value[0] == 'd') && value[1] == ':')
        {
            parsed = ParsePdfDate(parts, value.Substring(2));
        }
        else
        {
            parsed = ParseYearFirstDate(parts, value);
            if (!parsed)
            {
                parts = new MetadataDateParts();
                parsed = ParseSlashDate(parts, value);
            }
            if (!parsed)
            {
                parts = new MetadataDateParts();
                parsed = ParseEnglishDate(parts, value);
            }
        }

        if (!parsed || !ValidParts(parts))
        {
            return result;
        }

        result.IsValid = true;
        result.Precision = parts.Precision;
        result.Year = parts.Year;
        result.Month = parts.Month;
        result.Day = parts.Day;
        result.Hour = parts.Hour;
        result.Minute = parts.Minute;
        result.Second = parts.Second;
        result.FractionalSecondText = parts.FractionalSecondText;
        result.HasTimeZone = parts.HasTimeZone;
        result.TimeZoneOffsetMinutes = parts.TimeZoneOffsetMinutes;
        return result;
    }

    internal static bool ParseYearFirstDate(MetadataDateParts parts, string value)
    {
        if (value.Length < 4)
        {
            return false;
        }

        int year = DigitsValue(value, 0, 4);
        if (year < 0)
        {
            return false;
        }

        parts.Year = year;
        parts.Precision = MetadataDatePrecision.Year;
        if (value.Length == 4)
        {
            return true;
        }

        if (value[4] == '-')
        {
            if (value.Length < 7) return false;
            parts.Month = DigitsValue(value, 5, 2);
            if (parts.Month < 0) return false;
            parts.Precision = MetadataDatePrecision.Month;
            if (value.Length == 7) return true;
            if (value.Length < 10 || value[7] != '-') return false;
            parts.Day = DigitsValue(value, 8, 2);
            if (parts.Day < 0) return false;
            parts.Precision = MetadataDatePrecision.Day;
            if (value.Length == 10) return true;
            if (value[10] != 'T' && value[10] != ' ') return false;
            return ParseTimeText(parts, value.Substring(11), false);
        }

        if (value[4] == ':')
        {
            if (value.Length < 10 || value[7] != ':') return false;
            parts.Month = DigitsValue(value, 5, 2);
            parts.Day = DigitsValue(value, 8, 2);
            if (parts.Month < 0 || parts.Day < 0) return false;
            parts.Precision = MetadataDatePrecision.Day;
            if (value.Length == 10) return true;
            if (value[10] != ' ' && value[10] != 'T') return false;
            return ParseTimeText(parts, value.Substring(11), false);
        }

        if (DigitsValue(value, 4, 1) >= 0)
        {
            if (value.Length < 8) return false;
            parts.Month = DigitsValue(value, 4, 2);
            parts.Day = DigitsValue(value, 6, 2);
            if (parts.Month < 0 || parts.Day < 0) return false;
            parts.Precision = MetadataDatePrecision.Day;
            if (value.Length == 8) return true;
            if (value[8] == ' ' || value[8] == 'T')
            {
                return ParseTimeText(parts, value.Substring(9), true);
            }
            return ParseTimeText(parts, value.Substring(8), true);
        }

        return false;
    }

    internal static bool ParsePdfDate(MetadataDateParts parts, string value)
    {
        string dateText = value;
        string zoneText = "";
        int zoneIndex = FindZoneStart(value);
        if (zoneIndex >= 0)
        {
            dateText = value.Substring(0, zoneIndex);
            zoneText = value.Substring(zoneIndex);
        }
        else if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            dateText = value.Substring(0, value.Length - 1);
            zoneText = "Z";
        }

        if (dateText.Length < 4 || dateText.Length > 14 || (dateText.Length % 2) != 0)
        {
            return false;
        }

        parts.Year = DigitsValue(dateText, 0, 4);
        if (parts.Year < 0) return false;
        parts.Precision = MetadataDatePrecision.Year;
        if (dateText.Length >= 6)
        {
            parts.Month = DigitsValue(dateText, 4, 2);
            parts.Precision = MetadataDatePrecision.Month;
        }
        if (dateText.Length >= 8)
        {
            parts.Day = DigitsValue(dateText, 6, 2);
            parts.Precision = MetadataDatePrecision.Day;
        }
        if (dateText.Length >= 10)
        {
            parts.Hour = DigitsValue(dateText, 8, 2);
            parts.Precision = MetadataDatePrecision.Hour;
        }
        if (dateText.Length >= 12)
        {
            parts.Minute = DigitsValue(dateText, 10, 2);
            parts.Precision = MetadataDatePrecision.Minute;
        }
        if (dateText.Length >= 14)
        {
            parts.Second = DigitsValue(dateText, 12, 2);
            parts.Precision = MetadataDatePrecision.Second;
        }
        if (parts.Month < 0 || parts.Day < 0 || parts.Hour < 0 || parts.Minute < 0 || parts.Second < 0)
        {
            return false;
        }

        if (zoneText.Length > 0 && !ParseTimeZone(parts, zoneText))
        {
            return false;
        }

        return true;
    }

    internal static bool ParseSlashDate(MetadataDateParts parts, string value)
    {
        int firstSlash = value.IndexOf('/');
        if (firstSlash < 1) return false;
        int secondSlash = value.IndexOf('/', firstSlash + 1);
        if (secondSlash <= firstSlash + 1) return false;
        int dateEnd = value.IndexOf(' ', secondSlash + 1);
        if (dateEnd < 0) dateEnd = value.Length;

        parts.Month = DigitsValue(value, 0, firstSlash);
        parts.Day = DigitsValue(value, firstSlash + 1, secondSlash - firstSlash - 1);
        parts.Year = DigitsValue(value, secondSlash + 1, dateEnd - secondSlash - 1);
        if (parts.Month < 0 || parts.Day < 0 || parts.Year < 0 || dateEnd - secondSlash - 1 != 4)
        {
            return false;
        }

        parts.Precision = MetadataDatePrecision.Day;
        if (dateEnd == value.Length)
        {
            return true;
        }

        return ParseTimeText(parts, value.Substring(dateEnd + 1), false);
    }

    internal static bool ParseEnglishDate(MetadataDateParts parts, string value)
    {
        string normalized = value.Replace(",", " ");
        string[] tokens = normalized.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3)
        {
            return false;
        }

        int start = IsWeekday(tokens[0]) ? 1 : 0;
        if (tokens.Length - start < 3)
        {
            return false;
        }

        int monthIndex = -1;
        int candidate = start;
        while (candidate < tokens.Length && candidate <= start + 1)
        {
            if (MonthNumber(tokens[candidate]) > 0)
            {
                monthIndex = candidate;
                break;
            }
            candidate = candidate + 1;
        }
        if (monthIndex < 0)
        {
            return false;
        }

        int dayIndex = monthIndex == start ? monthIndex + 1 : start;
        int yearIndex = -1;
        candidate = start;
        while (candidate < tokens.Length)
        {
            if (candidate != monthIndex && candidate != dayIndex && tokens[candidate].Length == 4 && DigitsValue(tokens[candidate], 0, 4) > 0)
            {
                yearIndex = candidate;
                break;
            }
            candidate = candidate + 1;
        }
        if (yearIndex < 0)
        {
            return false;
        }

        parts.Month = MonthNumber(tokens[monthIndex]);
        parts.Day = DigitsValue(tokens[dayIndex], 0, tokens[dayIndex].Length);
        parts.Year = DigitsValue(tokens[yearIndex], 0, 4);
        if (parts.Day < 0)
        {
            return false;
        }
        parts.Precision = MetadataDatePrecision.Day;

        int timeIndex = -1;
        candidate = start;
        while (candidate < tokens.Length)
        {
            if (tokens[candidate].IndexOf(':') >= 0)
            {
                timeIndex = candidate;
                break;
            }
            candidate = candidate + 1;
        }
        if (timeIndex < 0)
        {
            return true;
        }

        string timeText = tokens[timeIndex];
        if (timeIndex + 1 < tokens.Length && IsMeridiem(tokens[timeIndex + 1]))
        {
            timeText = timeText + " " + tokens[timeIndex + 1];
            if (timeIndex + 2 < tokens.Length && IsTimeZoneToken(tokens[timeIndex + 2]))
            {
                timeText = timeText + " " + tokens[timeIndex + 2];
            }
        }
        else if (timeIndex + 1 < tokens.Length && IsTimeZoneToken(tokens[timeIndex + 1]))
        {
            timeText = timeText + " " + tokens[timeIndex + 1];
        }

        return ParseTimeText(parts, timeText, false);
    }

    internal static bool ParseTimeText(MetadataDateParts parts, string value, bool compactAllowed)
    {
        string timeText = value.Trim();
        if (timeText.Length == 0)
        {
            return false;
        }

        string zoneText = "";
        if (timeText.EndsWith(" GMT", StringComparison.OrdinalIgnoreCase) || timeText.EndsWith(" UTC", StringComparison.OrdinalIgnoreCase))
        {
            zoneText = timeText.Substring(timeText.Length - 3);
            timeText = timeText.Substring(0, timeText.Length - 4).Trim();
        }
        else if (timeText.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            zoneText = "Z";
            timeText = timeText.Substring(0, timeText.Length - 1).Trim();
        }
        else
        {
            int zoneIndex = FindZoneStart(timeText);
            if (zoneIndex >= 0)
            {
                zoneText = timeText.Substring(zoneIndex).Trim();
                timeText = timeText.Substring(0, zoneIndex).Trim();
            }
        }

        bool afternoon = false;
        bool meridiem = false;
        if (timeText.EndsWith(" AM", StringComparison.OrdinalIgnoreCase) || timeText.EndsWith(" PM", StringComparison.OrdinalIgnoreCase))
        {
            afternoon = timeText.EndsWith(" PM", StringComparison.OrdinalIgnoreCase);
            meridiem = true;
            timeText = timeText.Substring(0, timeText.Length - 3).Trim();
        }

        int index;
        int firstColon = timeText.IndexOf(':');
        if (timeText.Length >= 4 && (firstColon == 1 || firstColon == 2))
        {
            parts.Hour = DigitsValue(timeText, 0, firstColon);
            parts.Minute = DigitsValue(timeText, firstColon + 1, 2);
            if (parts.Hour < 0 || parts.Minute < 0) return false;
            parts.Precision = MetadataDatePrecision.Minute;
            index = firstColon + 3;
            if (index < timeText.Length)
            {
                if (timeText[index] != ':' || index + 3 > timeText.Length) return false;
                parts.Second = DigitsValue(timeText, index + 1, 2);
                if (parts.Second < 0) return false;
                parts.Precision = MetadataDatePrecision.Second;
                index = index + 3;
            }
        }
        else if (compactAllowed && (timeText.Length == 4 || timeText.Length == 6))
        {
            parts.Hour = DigitsValue(timeText, 0, 2);
            parts.Minute = DigitsValue(timeText, 2, 2);
            if (parts.Hour < 0 || parts.Minute < 0) return false;
            parts.Precision = MetadataDatePrecision.Minute;
            index = 4;
            if (timeText.Length == 6)
            {
                parts.Second = DigitsValue(timeText, 4, 2);
                if (parts.Second < 0) return false;
                parts.Precision = MetadataDatePrecision.Second;
                index = 6;
            }
        }
        else
        {
            return false;
        }

        if (index < timeText.Length)
        {
            if (timeText[index] != '.' && timeText[index] != ',') return false;
            string fraction = timeText.Substring(index + 1);
            if (fraction.Length == 0 || DigitsValue(fraction, 0, fraction.Length) < 0) return false;
            parts.FractionalSecondText = fraction;
            parts.Precision = MetadataDatePrecision.FractionalSecond;
            index = timeText.Length;
        }
        if (index != timeText.Length)
        {
            return false;
        }

        if (meridiem)
        {
            if (parts.Hour < 1 || parts.Hour > 12) return false;
            if (afternoon && parts.Hour != 12) parts.Hour = parts.Hour + 12;
            if (!afternoon && parts.Hour == 12) parts.Hour = 0;
        }

        if (zoneText.Length > 0 && !ParseTimeZone(parts, zoneText))
        {
            return false;
        }
        return true;
    }

    internal static bool ParseTimeZone(MetadataDateParts parts, string value)
    {
        string zone = value.Trim();
        if (zone == "Z" || zone == "z" || string.Equals(zone, "GMT", StringComparison.OrdinalIgnoreCase) || string.Equals(zone, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            parts.HasTimeZone = true;
            parts.TimeZoneOffsetMinutes = 0;
            return true;
        }
        if (zone.Length < 3 || (zone[0] != '+' && zone[0] != '-'))
        {
            return false;
        }

        int sign = zone[0] == '-' ? -1 : 1;
        string digits = zone.Substring(1).Replace(":", "").Replace("'", "");
        if (digits.Length != 2 && digits.Length != 4)
        {
            return false;
        }
        int hour = DigitsValue(digits, 0, 2);
        int minute = digits.Length == 4 ? DigitsValue(digits, 2, 2) : 0;
        if (hour < 0 || minute < 0 || hour > 14 || minute > 59 || (hour == 14 && minute != 0))
        {
            return false;
        }

        parts.HasTimeZone = true;
        parts.TimeZoneOffsetMinutes = sign * ((hour * 60) + minute);
        return true;
    }

    internal static int FindZoneStart(string value)
    {
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] == '+' || value[index] == '-')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool ValidParts(MetadataDateParts parts)
    {
        int precision = (int)parts.Precision;
        if (parts.Year < 1 || parts.Year > 9999) return false;
        if (precision >= (int)MetadataDatePrecision.Month && (parts.Month < 1 || parts.Month > 12)) return false;
        if (precision >= (int)MetadataDatePrecision.Day && (parts.Day < 1 || parts.Day > DaysInMonth(parts.Year, parts.Month))) return false;
        if (precision >= (int)MetadataDatePrecision.Hour && (parts.Hour < 0 || parts.Hour > 23)) return false;
        if (precision >= (int)MetadataDatePrecision.Minute && (parts.Minute < 0 || parts.Minute > 59)) return false;
        if (precision >= (int)MetadataDatePrecision.Second && (parts.Second < 0 || parts.Second > 60)) return false;
        if (parts.HasTimeZone && precision < (int)MetadataDatePrecision.Hour) return false;
        return true;
    }

    internal static int DaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool leap = (year % 4) == 0 && ((year % 100) != 0 || (year % 400) == 0);
            return leap ? 29 : 28;
        }
        if (month == 4 || month == 6 || month == 9 || month == 11) return 30;
        return 31;
    }

    internal static int DigitsValue(string value, int start, int count)
    {
        if (value == null || start < 0 || count <= 0 || start > value.Length - count)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < count)
        {
            char character = value[start + index];
            if (character < '0' || character > '9') return -1;
            result = (result * 10) + (character - '0');
            index = index + 1;
        }
        return result;
    }

    internal static int MonthNumber(string value)
    {
        string month = value == null ? "" : value.ToLowerInvariant();
        if (month == "jan" || month == "january") return 1;
        if (month == "feb" || month == "february") return 2;
        if (month == "mar" || month == "march") return 3;
        if (month == "apr" || month == "april") return 4;
        if (month == "may") return 5;
        if (month == "jun" || month == "june") return 6;
        if (month == "jul" || month == "july") return 7;
        if (month == "aug" || month == "august") return 8;
        if (month == "sep" || month == "sept" || month == "september") return 9;
        if (month == "oct" || month == "october") return 10;
        if (month == "nov" || month == "november") return 11;
        if (month == "dec" || month == "december") return 12;
        return 0;
    }

    internal static bool IsWeekday(string value)
    {
        string day = value == null ? "" : value.ToLowerInvariant();
        return day == "mon" || day == "monday" || day == "tue" || day == "tues" || day == "tuesday" || day == "wed" || day == "wednesday" || day == "thu" || day == "thur" || day == "thurs" || day == "thursday" || day == "fri" || day == "friday" || day == "sat" || day == "saturday" || day == "sun" || day == "sunday";
    }

    internal static bool IsMeridiem(string value)
    {
        return string.Equals(value, "AM", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "PM", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsTimeZoneToken(string value)
    {
        if (string.Equals(value, "GMT", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "UTC", StringComparison.OrdinalIgnoreCase) || value == "Z" || value == "z") return true;
        return value != null && value.Length > 0 && (value[0] == '+' || value[0] == '-');
    }
}

internal class MetadataDateParts
{
    internal MetadataDatePrecision Precision;
    internal int Year;
    internal int Month;
    internal int Day;
    internal int Hour;
    internal int Minute;
    internal int Second;
    internal string FractionalSecondText;
    internal bool HasTimeZone;
    internal int TimeZoneOffsetMinutes;

    internal MetadataDateParts()
    {
        Precision = MetadataDatePrecision.Unknown;
        Year = 0;
        Month = 0;
        Day = 0;
        Hour = 0;
        Minute = 0;
        Second = 0;
        FractionalSecondText = "";
        HasTimeZone = false;
        TimeZoneOffsetMinutes = 0;
    }
}
