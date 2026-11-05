using System.Globalization;

namespace odf;

internal static class OdfLexicalFormatter
{
    internal static string FormatDate(OdfTemporalValue? value)
    {
        if (value == null || !value.IsValid || !value.HasDate || value.HasTime)
        {
            return PreservedTemporalValue(value);
        }
        OdfTemporalValue normalized = CopyTemporal(value);
        int offset = normalized.TimeZoneMinutes;
        if (normalized.HasTimeZone && offset > 720)
        {
            AddTemporalDays(normalized, -1);
            offset = offset - 1440;
        }
        else if (normalized.HasTimeZone && offset <= -720)
        {
            AddTemporalDays(normalized, 1);
            offset = offset + 1440;
        }

        string formatted = FormatDateFields(normalized);
        if (normalized.HasTimeZone)
        {
            formatted = formatted + FormatTimeZone(offset);
        }
        return formatted;
    }

    internal static string FormatTime(OdfTemporalValue? value)
    {
        if (value == null || !value.IsValid || value.HasDate || !value.HasTime)
        {
            return PreservedTemporalValue(value);
        }
        OdfTemporalValue normalized = CopyTemporal(value);
        NormalizeTemporalClock(normalized);
        if (normalized.HasTimeZone)
        {
            AdjustTemporalMinutes(normalized, -normalized.TimeZoneMinutes);
        }

        string formatted = FormatTimeFields(normalized);
        if (normalized.HasTimeZone)
        {
            formatted = formatted + "Z";
        }
        return formatted;
    }

    internal static string FormatDateTime(OdfTemporalValue? value)
    {
        if (value == null || !value.IsValid || !value.HasDate || !value.HasTime)
        {
            return PreservedTemporalValue(value);
        }
        OdfTemporalValue normalized = CopyTemporal(value);
        NormalizeTemporalClock(normalized);
        if (normalized.HasTimeZone)
        {
            AdjustTemporalMinutes(normalized, -normalized.TimeZoneMinutes);
        }

        string formatted = FormatDateFields(normalized) + "T" + FormatTimeFields(normalized);
        if (normalized.HasTimeZone)
        {
            formatted = formatted + "Z";
        }
        return formatted;
    }

    internal static string FormatDuration(OdfDurationValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }

        string fraction = TrimFraction(value.FractionalSecond);
        bool hasDate = value.Years != 0 || value.Months != 0 || value.Days != 0;
        bool hasTime = value.Hours != 0 || value.Minutes != 0 || value.Seconds != 0 || fraction.Length != 0;
        bool hasValue = hasDate || hasTime;
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        if (value.IsNegative && hasValue)
        {
            builder.Append('-');
        }
        builder.Append('P');
        if (value.Years != 0)
        {
            builder.Append(value.Years.ToString(CultureInfo.InvariantCulture));
            builder.Append('Y');
        }
        if (value.Months != 0)
        {
            builder.Append(value.Months.ToString(CultureInfo.InvariantCulture));
            builder.Append('M');
        }
        if (value.Days != 0)
        {
            builder.Append(value.Days.ToString(CultureInfo.InvariantCulture));
            builder.Append('D');
        }
        if (hasTime)
        {
            builder.Append('T');
            if (value.Hours != 0)
            {
                builder.Append(value.Hours.ToString(CultureInfo.InvariantCulture));
                builder.Append('H');
            }
            if (value.Minutes != 0)
            {
                builder.Append(value.Minutes.ToString(CultureInfo.InvariantCulture));
                builder.Append('M');
            }
            if (value.Seconds != 0 || fraction.Length != 0)
            {
                builder.Append(value.Seconds.ToString(CultureInfo.InvariantCulture));
                if (fraction.Length != 0)
                {
                    builder.Append('.');
                    builder.Append(fraction);
                }
                builder.Append('S');
            }
        }
        if (!hasValue)
        {
            builder.Append("T0S");
        }
        return builder.ToString();
    }

    internal static string FormatMeasurement(OdfUnitValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedUnitValue(value);
        }
        string suffix = UnitSuffix(value.UnitKind);
        if (suffix.Length == 0 && value.UnitKind != OdfUnitKind.Unitless)
        {
            return value.LexicalValue;
        }
        return value.NumericValue.ToString("R", CultureInfo.InvariantCulture) + suffix;
    }

    internal static string FormatAngle(OdfUnitValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedUnitValue(value);
        }
        if (value.UnitKind != OdfUnitKind.Unitless &&
            value.UnitKind != OdfUnitKind.Degree &&
            value.UnitKind != OdfUnitKind.Radian &&
            value.UnitKind != OdfUnitKind.Grad)
        {
            return value.LexicalValue;
        }
        return value.NumericValue.ToString("R", CultureInfo.InvariantCulture) + UnitSuffix(value.UnitKind);
    }

    internal static string FormatPoint(OdfPointValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        return value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatVector(OdfVectorValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        return "(" +
            value.X.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Y.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Z.ToString("R", CultureInfo.InvariantCulture) + ")";
    }

    internal static string FormatViewBox(OdfViewBoxValue? value)
    {
        if (value == null)
        {
            return "";
        }
        if (!value.IsValid)
        {
            return value.LexicalValue;
        }
        return value.Left.ToString(CultureInfo.InvariantCulture) + " " +
            value.Top.ToString(CultureInfo.InvariantCulture) + " " +
            value.Width.ToString(CultureInfo.InvariantCulture) + " " +
            value.Height.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatBoolean(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        return value.BooleanValue ? "true" : "false";
    }

    internal static string FormatInteger(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDecimal(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        return value.DecimalValue.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatFloat(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        if (double.IsPositiveInfinity(value.FloatValue))
        {
            return "INF";
        }
        if (double.IsNegativeInfinity(value.FloatValue))
        {
            return "-INF";
        }
        if (double.IsNaN(value.FloatValue))
        {
            return "NaN";
        }
        return value.FloatValue.ToString("R", CultureInfo.InvariantCulture);
    }

    internal static string FormatPercentage(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        return value.DecimalValue.ToString(CultureInfo.InvariantCulture) + "%";
    }

    internal static string FormatBase64(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        return global::System.Convert.ToBase64String(value.BinaryValue);
    }

    internal static string FormatColor(OdfLexicalValue? value)
    {
        if (value == null || !value.IsValid)
        {
            return PreservedValue(value);
        }
        uint color = value.ColorValue & 0xFFFFFF;
        return "#" + color.ToString("X6", CultureInfo.InvariantCulture);
    }

    static string PreservedValue(OdfLexicalValue? value)
    {
        if (value == null)
        {
            return "";
        }
        return value.LexicalValue;
    }

    static string PreservedUnitValue(OdfUnitValue? value)
    {
        if (value == null)
        {
            return "";
        }
        return value.LexicalValue;
    }

    static string PreservedTemporalValue(OdfTemporalValue? value)
    {
        if (value == null)
        {
            return "";
        }
        return value.LexicalValue;
    }

    static OdfTemporalValue CopyTemporal(OdfTemporalValue value)
    {
        OdfTemporalValue result = new OdfTemporalValue();
        result.LexicalValue = value.LexicalValue;
        result.Year = value.Year;
        result.Month = value.Month;
        result.Day = value.Day;
        result.Hour = value.Hour;
        result.Minute = value.Minute;
        result.Second = value.Second;
        result.FractionalSecond = value.FractionalSecond;
        result.TimeZoneMinutes = value.TimeZoneMinutes;
        result.HasDate = value.HasDate;
        result.HasTime = value.HasTime;
        result.HasTimeZone = value.HasTimeZone;
        result.IsValid = value.IsValid;
        return result;
    }

    static void NormalizeTemporalClock(OdfTemporalValue value)
    {
        if (value.Hour == 24)
        {
            value.Hour = 0;
            AddTemporalDays(value, 1);
        }
        if (value.Second == 60)
        {
            value.Second = 0;
            AdjustTemporalMinutes(value, 1);
        }
    }

    static void AdjustTemporalMinutes(OdfTemporalValue value, int minutes)
    {
        int total = value.Hour * 60 + value.Minute + minutes;
        int dayChange = 0;
        while (total < 0)
        {
            total = total + 1440;
            dayChange = dayChange - 1;
        }
        while (total >= 1440)
        {
            total = total - 1440;
            dayChange = dayChange + 1;
        }
        value.Hour = total / 60;
        value.Minute = total % 60;
        AddTemporalDays(value, dayChange);
    }

    static void AddTemporalDays(OdfTemporalValue value, int days)
    {
        if (!value.HasDate)
        {
            return;
        }
        while (days > 0)
        {
            value.Day = value.Day + 1;
            if (value.Day > TemporalDaysInMonth(value.Year, value.Month))
            {
                value.Day = 1;
                value.Month = value.Month + 1;
                if (value.Month > 12)
                {
                    value.Month = 1;
                    value.Year = value.Year == -1 ? 1 : value.Year + 1;
                }
            }
            days = days - 1;
        }
        while (days < 0)
        {
            value.Day = value.Day - 1;
            if (value.Day < 1)
            {
                value.Month = value.Month - 1;
                if (value.Month < 1)
                {
                    value.Month = 12;
                    value.Year = value.Year == 1 ? -1 : value.Year - 1;
                }
                value.Day = TemporalDaysInMonth(value.Year, value.Month);
            }
            days = days + 1;
        }
    }

    static string FormatDateFields(OdfTemporalValue value)
    {
        global::System.Numerics.BigInteger absoluteYear = global::System.Numerics.BigInteger.Abs(value.Year);
        string year = absoluteYear.ToString(CultureInfo.InvariantCulture);
        while (year.Length < 4)
        {
            year = "0" + year;
        }
        if (value.Year < 0)
        {
            year = "-" + year;
        }
        return year + "-" + TwoDigitText(value.Month) + "-" + TwoDigitText(value.Day);
    }

    static string FormatTimeFields(OdfTemporalValue value)
    {
        string fraction = TrimFraction(value.FractionalSecond);
        string result = TwoDigitText(value.Hour) + ":" +
            TwoDigitText(value.Minute) + ":" +
            TwoDigitText(value.Second);
        if (fraction.Length != 0)
        {
            result = result + "." + fraction;
        }
        return result;
    }

    static string FormatTimeZone(int minutes)
    {
        if (minutes == 0)
        {
            return "Z";
        }
        char sign = minutes < 0 ? '-' : '+';
        int magnitude = minutes < 0 ? -minutes : minutes;
        return sign + TwoDigitText(magnitude / 60) + ":" + TwoDigitText(magnitude % 60);
    }

    static string TwoDigitText(int value)
    {
        if (value < 10)
        {
            return "0" + value.ToString(CultureInfo.InvariantCulture);
        }
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static string TrimFraction(string value)
    {
        int length = value.Length;
        while (length > 0 && value[length - 1] == '0')
        {
            length = length - 1;
        }
        return value.Substring(0, length);
    }

    static int TemporalDaysInMonth(global::System.Numerics.BigInteger year, int month)
    {
        if (month == 2)
        {
            return IsTemporalLeapYear(year) ? 29 : 28;
        }
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }
        return 31;
    }

    static bool IsTemporalLeapYear(global::System.Numerics.BigInteger year)
    {
        global::System.Numerics.BigInteger calendarYear = year < 0 ? year + 1 : year;
        return calendarYear % 400 == 0 || (calendarYear % 4 == 0 && calendarYear % 100 != 0);
    }

    static string UnitSuffix(OdfUnitKind unit)
    {
        if (unit == OdfUnitKind.Millimetre)
        {
            return "mm";
        }
        if (unit == OdfUnitKind.Centimetre)
        {
            return "cm";
        }
        if (unit == OdfUnitKind.Inch)
        {
            return "in";
        }
        if (unit == OdfUnitKind.Point)
        {
            return "pt";
        }
        if (unit == OdfUnitKind.Pica)
        {
            return "pc";
        }
        if (unit == OdfUnitKind.Pixel)
        {
            return "px";
        }
        if (unit == OdfUnitKind.Em)
        {
            return "em";
        }
        if (unit == OdfUnitKind.Ex)
        {
            return "ex";
        }
        if (unit == OdfUnitKind.Percentage)
        {
            return "%";
        }
        if (unit == OdfUnitKind.Degree)
        {
            return "deg";
        }
        if (unit == OdfUnitKind.Radian)
        {
            return "rad";
        }
        if (unit == OdfUnitKind.Grad)
        {
            return "grad";
        }
        return "";
    }
}
