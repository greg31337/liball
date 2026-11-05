using System.Globalization;
using System.Numerics;
using System.Text;

namespace odf;

internal static class FormulaTemporalModel
{
    internal static readonly int[] MonthStarts =
        [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];

    internal static OdfFormulaResult CreateTemporal(
        string lexical,
        OdfSpreadsheetSettings settings)
    {
        if (lexical == null || settings == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfTemporalValue value;
        OdfValueKind kind;
        if (lexical.IndexOf('T') >= 0)
        {
            value = OdfLexicalParser.ParseDateTime(lexical);
            kind = OdfValueKind.DateTime;
        }
        else if (lexical.IndexOf(':') >= 0)
        {
            value = OdfLexicalParser.ParseTime(lexical);
            kind = OdfValueKind.Time;
        }
        else
        {
            value = OdfLexicalParser.ParseDate(lexical);
            kind = OdfValueKind.Date;
        }
        if (!value.IsValid)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double serial;
        if (kind == OdfValueKind.Time)
        {
            serial = NormalizeTime(TimeFraction(value)
                - TimeZoneFraction(value));
        }
        else
        {
            OdfTemporalValue nullDate = ParseNullDate(settings);
            if (!nullDate.IsValid)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            BigInteger days = DayNumber(value) - DayNumber(nullDate);
            double dayValue = (double)days;
            serial = dayValue
                + TimeFraction(value)
                - TimeZoneFraction(value)
                + TimeZoneFraction(nullDate);
        }
        if (!double.IsFinite(serial))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult result = FormulaValueModel.CreateNumber(serial);
        result.Kind = kind;
        result.StringValue = lexical;
        return result;
    }

    internal static OdfFormulaResult CreateDuration(string lexical)
    {
        OdfDurationValue value = OdfLexicalParser.ParseDuration(lexical);
        if (!value.IsValid || value.Years != 0 || value.Months != 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        BigInteger seconds = value.Days * 86400
            + value.Hours * 3600
            + value.Minutes * 60
            + value.Seconds;
        double serial = (double)seconds / 86400.0
            + FractionalSecond(value.FractionalSecond) / 86400.0;
        if (value.IsNegative)
        {
            serial = -serial;
        }
        if (!double.IsFinite(serial))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult result = FormulaValueModel.CreateNumber(serial);
        result.Kind = OdfValueKind.Duration;
        result.StringValue = lexical;
        return result;
    }

    internal static OdfFormulaResult TemporalFromSerial(
        double serial,
        OdfValueKind kind,
        OdfSpreadsheetSettings settings,
        int timeZoneMinutes,
        bool includeTimeZone)
    {
        if (settings == null
            || !double.IsFinite(serial)
            || (kind != OdfValueKind.Date
                && kind != OdfValueKind.DateTime
                && kind != OdfValueKind.Time)
            || timeZoneMinutes < -840
            || timeZoneMinutes > 840)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfTemporalValue nullDate = ParseNullDate(settings);
        if (!nullDate.IsValid)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double localSerial = serial;
        if (kind != OdfValueKind.Time)
        {
            localSerial = localSerial
                - TimeZoneFraction(nullDate)
                + (includeTimeZone ? timeZoneMinutes / 1440.0 : 0);
        }
        else if (includeTimeZone)
        {
            localSerial = localSerial + timeZoneMinutes / 1440.0;
        }
        double whole = global::System.Math.Floor(localSerial);
        double fraction = localSerial - whole;
        BigInteger day = DayNumber(nullDate) + new BigInteger(whole);
        if (kind == OdfValueKind.Time)
        {
            day = 0;
            fraction = NormalizeTime(localSerial);
        }
        OdfTemporalValue value = DateFromDayNumber(day);
        bool rolledOver = SetTimeFromFraction(value, fraction);
        if (rolledOver && kind == OdfValueKind.DateTime)
        {
            value = DateFromDayNumber(day + 1);
        }
        value.HasDate = kind != OdfValueKind.Time;
        value.HasTime = kind != OdfValueKind.Date;
        value.HasTimeZone = includeTimeZone;
        value.TimeZoneMinutes = timeZoneMinutes;
        string lexical = FormatTemporal(value, kind);
        OdfFormulaResult result = FormulaValueModel.CreateNumber(serial);
        result.Kind = kind;
        result.StringValue = lexical;
        return result;
    }

    internal static OdfFormulaResult DurationFromSerial(double serial)
    {
        if (!double.IsFinite(serial))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        bool negative = serial < 0;
        double absolute = global::System.Math.Abs(serial);
        double seconds = global::System.Math.Round(
            absolute * 86400.0,
            9,
            global::System.MidpointRounding.AwayFromZero);
        double days = global::System.Math.Floor(seconds / 86400.0);
        seconds = seconds - days * 86400.0;
        int hours = (int)global::System.Math.Floor(seconds / 3600.0);
        seconds = seconds - hours * 3600.0;
        int minutes = (int)global::System.Math.Floor(seconds / 60.0);
        seconds = seconds - minutes * 60.0;
        StringBuilder lexical = new StringBuilder();
        if (negative)
        {
            lexical.Append('-');
        }
        lexical.Append('P');
        if (days > 0)
        {
            lexical.Append(days.ToString("0", CultureInfo.InvariantCulture));
            lexical.Append('D');
        }
        lexical.Append('T');
        if (hours > 0)
        {
            lexical.Append(hours.ToString(CultureInfo.InvariantCulture));
            lexical.Append('H');
        }
        if (minutes > 0)
        {
            lexical.Append(minutes.ToString(CultureInfo.InvariantCulture));
            lexical.Append('M');
        }
        if (seconds > 0 || (days == 0 && hours == 0 && minutes == 0))
        {
            lexical.Append(FormatSecondNumber(seconds));
            lexical.Append('S');
        }
        OdfFormulaResult result = FormulaValueModel.CreateNumber(serial);
        result.Kind = OdfValueKind.Duration;
        result.StringValue = lexical.ToString();
        return result;
    }

    internal static bool IsLeapYear(BigInteger year)
    {
        BigInteger astronomical = AstronomicalYear(year);
        return Modulo(astronomical, 4) == 0
            && (Modulo(astronomical, 100) != 0
                || Modulo(astronomical, 400) == 0);
    }

    internal static BigInteger DaysBetweenDates(
        string first,
        string second)
    {
        OdfTemporalValue left = OdfLexicalParser.ParseDate(first);
        OdfTemporalValue right = OdfLexicalParser.ParseDate(second);
        if (!left.IsValid || !right.IsValid)
        {
            return 0;
        }
        return DayNumber(right) - DayNumber(left);
    }

    internal static OdfTemporalValue ParseNullDate(
        OdfSpreadsheetSettings settings)
    {
        string lexical = settings.NullDate.Length == 0
            ? "1899-12-30"
            : settings.NullDate;
        return OdfLexicalParser.ParseDate(lexical);
    }

    internal static BigInteger DayNumber(OdfTemporalValue value)
    {
        BigInteger astronomical = AstronomicalYear(value.Year);
        BigInteger preceding = astronomical - 1;
        BigInteger days = preceding * 365
            + FloorDivide(preceding, 4)
            - FloorDivide(preceding, 100)
            + FloorDivide(preceding, 400);
        days = days + MonthStarts[value.Month - 1] + value.Day - 1;
        if (value.Month > 2 && IsLeapYear(value.Year))
        {
            days = days + 1;
        }
        return days;
    }

    internal static OdfTemporalValue DateFromDayNumber(BigInteger day)
    {
        BigInteger year = FloorDivide(day * 400, 146097) + 1;
        while (day < DayNumberForAstronomicalYear(year))
        {
            year = year - 1;
        }
        while (day >= DayNumberForAstronomicalYear(year + 1))
        {
            year = year + 1;
        }
        BigInteger dayOfYearValue = day
            - DayNumberForAstronomicalYear(year);
        int dayOfYear = (int)dayOfYearValue;
        BigInteger xmlYear = year <= 0 ? year - 1 : year;
        int month = 1;
        while (month < 12
            && dayOfYear >= MonthStartForYear(xmlYear, month + 1))
        {
            month = month + 1;
        }
        OdfTemporalValue result = new OdfTemporalValue();
        result.Year = xmlYear;
        result.Month = month;
        result.Day = dayOfYear - MonthStartForYear(xmlYear, month) + 1;
        result.HasDate = true;
        result.IsValid = true;
        return result;
    }

    static BigInteger DayNumberForAstronomicalYear(BigInteger year)
    {
        BigInteger preceding = year - 1;
        return preceding * 365
            + FloorDivide(preceding, 4)
            - FloorDivide(preceding, 100)
            + FloorDivide(preceding, 400);
    }

    internal static int MonthStartForYear(BigInteger xmlYear, int month)
    {
        int result = MonthStarts[month - 1];
        if (month > 2 && IsLeapYear(xmlYear))
        {
            result = result + 1;
        }
        return result;
    }

    static BigInteger AstronomicalYear(BigInteger xmlYear)
    {
        return xmlYear < 0 ? xmlYear + 1 : xmlYear;
    }

    static BigInteger FloorDivide(BigInteger value, int divisor)
    {
        BigInteger quotient = value / divisor;
        BigInteger remainder = value % divisor;
        if (remainder < 0)
        {
            quotient = quotient - 1;
        }
        return quotient;
    }

    internal static BigInteger Modulo(BigInteger value, int divisor)
    {
        BigInteger result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    static double TimeFraction(OdfTemporalValue value)
    {
        double seconds = value.Hour * 3600.0
            + value.Minute * 60.0
            + value.Second
            + FractionalSecond(value.FractionalSecond);
        return seconds / 86400.0;
    }

    static double TimeZoneFraction(OdfTemporalValue value)
    {
        return value.HasTimeZone ? value.TimeZoneMinutes / 1440.0 : 0;
    }

    static double FractionalSecond(string digits)
    {
        if (digits.Length == 0)
        {
            return 0;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat("0." + digits);
        return parsed.IsValid ? parsed.FloatValue : 0;
    }

    static double NormalizeTime(double value)
    {
        double result = value - global::System.Math.Floor(value);
        return result == 1 ? 0 : result;
    }

    static bool SetTimeFromFraction(
        OdfTemporalValue value,
        double fraction)
    {
        double total = global::System.Math.Round(
            fraction * 86400.0,
            9,
            global::System.MidpointRounding.AwayFromZero);
        if (total >= 86400.0)
        {
            total = 0;
        }
        bool rolledOver = total == 0 && fraction > 0.5;
        value.Hour = (int)global::System.Math.Floor(total / 3600.0);
        total = total - value.Hour * 3600.0;
        value.Minute = (int)global::System.Math.Floor(total / 60.0);
        total = total - value.Minute * 60.0;
        value.Second = (int)global::System.Math.Floor(total);
        double fractionValue = total - value.Second;
        if (fractionValue > 0)
        {
            string text = fractionValue.ToString(
                "0.000000000",
                CultureInfo.InvariantCulture);
            value.FractionalSecond = TrimFraction(text.Substring(2));
        }
        return rolledOver;
    }

    static string FormatTemporal(OdfTemporalValue value, OdfValueKind kind)
    {
        StringBuilder result = new StringBuilder();
        if (kind != OdfValueKind.Time)
        {
            result.Append(FormatYear(value.Year));
            result.Append('-');
            result.Append(value.Month.ToString("00", CultureInfo.InvariantCulture));
            result.Append('-');
            result.Append(value.Day.ToString("00", CultureInfo.InvariantCulture));
        }
        if (kind == OdfValueKind.DateTime)
        {
            result.Append('T');
        }
        if (kind != OdfValueKind.Date)
        {
            result.Append(value.Hour.ToString("00", CultureInfo.InvariantCulture));
            result.Append(':');
            result.Append(value.Minute.ToString("00", CultureInfo.InvariantCulture));
            result.Append(':');
            result.Append(value.Second.ToString("00", CultureInfo.InvariantCulture));
            if (value.FractionalSecond.Length > 0)
            {
                result.Append('.');
                result.Append(value.FractionalSecond);
            }
        }
        if (value.HasTimeZone)
        {
            AppendTimeZone(result, value.TimeZoneMinutes);
        }
        return result.ToString();
    }

    static string FormatYear(BigInteger year)
    {
        bool negative = year < 0;
        BigInteger absolute = negative ? -year : year;
        string digits = absolute.ToString(CultureInfo.InvariantCulture);
        if (digits.Length < 4)
        {
            digits = digits.PadLeft(4, '0');
        }
        return negative ? "-" + digits : digits;
    }

    static void AppendTimeZone(StringBuilder result, int minutes)
    {
        if (minutes == 0)
        {
            result.Append('Z');
            return;
        }
        int absolute = global::System.Math.Abs(minutes);
        result.Append(minutes < 0 ? '-' : '+');
        result.Append((absolute / 60).ToString(
            "00",
            CultureInfo.InvariantCulture));
        result.Append(':');
        result.Append((absolute % 60).ToString(
            "00",
            CultureInfo.InvariantCulture));
    }

    static string TrimFraction(string value)
    {
        int end = value.Length;
        while (end > 0 && value[end - 1] == '0')
        {
            end = end - 1;
        }
        return value.Substring(0, end);
    }

    static string FormatSecondNumber(double value)
    {
        string result = value.ToString(
            "0.000000000",
            CultureInfo.InvariantCulture);
        int end = result.Length;
        while (end > 0 && result[end - 1] == '0')
        {
            end = end - 1;
        }
        if (end > 0 && result[end - 1] == '.')
        {
            end = end - 1;
        }
        return result.Substring(0, end);
    }
}
