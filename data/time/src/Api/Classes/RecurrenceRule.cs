using System;
using System.Globalization;

namespace time;

/// <summary>Represents the supported RFC 5545 recurrence-rule fields.</summary>
/// <remarks>Arrays returned by this type are copies. UTC and floating-local UNTIL values are represented separately and are mutually exclusive.</remarks>
public class RecurrenceRule : IRecurrenceRule
{
    RecurrenceFrequency frequency;
    int interval;
    int count;
    Instant? until;
    LocalDateTime? localUntil;
    int weekStart;
    int[] bySecond;
    int[] byMinute;
    int[] byHour;
    RecurrenceDay[] byDay;
    int[] byMonthDay;
    int[] byYearDay;
    int[] byWeekNo;
    int[] byMonth;
    int[] bySetPos;

    internal RecurrenceRule(
        RecurrenceFrequency frequency,
        int interval,
        int count,
        Instant? until,
        LocalDateTime? localUntil,
        int weekStart,
        int[] bySecond,
        int[] byMinute,
        int[] byHour,
        RecurrenceDay[] byDay,
        int[] byMonthDay,
        int[] byYearDay,
        int[] byWeekNo,
        int[] byMonth,
        int[] bySetPos)
    {
        RequireFrequency(frequency, "frequency");
        RequirePositive(interval, "interval");
        RequireNonNegative(count, "count");
        TimeValidation.RequireIsoDayOfWeek(weekStart, "weekStart");
        TimeValidation.RequireNotNull(bySecond, "bySecond");
        TimeValidation.RequireNotNull(byMinute, "byMinute");
        TimeValidation.RequireNotNull(byHour, "byHour");
        TimeValidation.RequireNotNull(byDay, "byDay");
        TimeValidation.RequireNotNull(byMonthDay, "byMonthDay");
        TimeValidation.RequireNotNull(byYearDay, "byYearDay");
        TimeValidation.RequireNotNull(byWeekNo, "byWeekNo");
        TimeValidation.RequireNotNull(byMonth, "byMonth");
        TimeValidation.RequireNotNull(bySetPos, "bySetPos");

        if (until != null && localUntil != null)
        {
            throw new ArgumentException("UTC and local until values must not both be set.", "localUntil");
        }

        if (count > 0 && (until != null || localUntil != null))
        {
            throw new ArgumentException("Count and until must not both be set.", "until");
        }

        if (until != null && until.GetUtcTicks() % TimeConstants.TicksPerSecond != 0L)
        {
            throw new ArgumentException("UTC until must have whole-second precision for RRULE formatting.", "until");
        }

        if (localUntil != null && localUntil.GetTime().GetTicksSinceMidnight() % TimeConstants.TicksPerSecond != 0L)
        {
            throw new ArgumentException("Local until must have whole-second precision for RRULE formatting.", "localUntil");
        }

        RequireRange(bySecond, 0, 59, true, "bySecond");
        RequireRange(byMinute, 0, 59, true, "byMinute");
        RequireRange(byHour, 0, 23, true, "byHour");
        RequireRecurrenceDays(byDay, "byDay");
        RequireRange(byMonthDay, -31, 31, false, "byMonthDay");
        RequireRange(byYearDay, -366, 366, false, "byYearDay");
        RequireRange(byWeekNo, -53, 53, false, "byWeekNo");
        RequireRange(byMonth, 1, 12, true, "byMonth");
        RequireRange(bySetPos, -366, 366, false, "bySetPos");
        RequireCompatibleFilters(frequency, byDay, byMonthDay, byYearDay, byWeekNo, bySetPos, bySecond, byMinute, byHour, byMonth);

        this.frequency = frequency;
        this.interval = interval;
        this.count = count;
        this.until = until;
        this.localUntil = localUntil;
        this.weekStart = weekStart;
        this.bySecond = CopyIntArray(bySecond);
        this.byMinute = CopyIntArray(byMinute);
        this.byHour = CopyIntArray(byHour);
        this.byDay = CopyDayArray(byDay);
        this.byMonthDay = CopyIntArray(byMonthDay);
        this.byYearDay = CopyIntArray(byYearDay);
        this.byWeekNo = CopyIntArray(byWeekNo);
        this.byMonth = CopyIntArray(byMonth);
        this.bySetPos = CopyIntArray(bySetPos);
    }

    /// <summary>Creates an unbounded rule with interval one and no BY filters.</summary><param name="frequency">The base frequency.</param><returns>A minimal recurrence rule.</returns>
    internal static RecurrenceRule Create(RecurrenceFrequency frequency)
    {
        return new RecurrenceRule(
            frequency,
            1,
            0,
            null,
            null,
            TimeConstants.IsoMonday,
            new int[0],
            new int[0],
            new int[0],
            new RecurrenceDay[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }

    /// <summary>Parses the supported RFC 5545 RRULE value syntax.</summary>
    /// <remarks>The optional <c>RRULE:</c> prefix is accepted. Unsupported or duplicate parts are rejected instead of ignored.</remarks><param name="text">The rule text.</param><returns>The validated recurrence rule.</returns><exception cref="FormatException">The text is malformed or contains an unsupported, duplicate, or invalid rule part.</exception>
    internal static RecurrenceRule Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        string ruleText = text;
        if (ruleText.StartsWith("RRULE:", StringComparison.Ordinal))
        {
            ruleText = ruleText.Substring(6);
        }

        if (ruleText.IndexOf(':') >= 0 || ruleText.IndexOf('\r') >= 0 || ruleText.IndexOf('\n') >= 0)
        {
            throw new FormatException("Only a single RRULE value or RRULE content line is supported.");
        }

        RecurrenceFrequency frequency = RecurrenceFrequency.Daily;
        bool hasFrequency = false;
        int interval = 1;
        int count = 0;
        Instant? until = null;
        LocalDateTime? localUntil = null;
        int weekStart = TimeConstants.IsoMonday;
        int[] bySecond = new int[0];
        int[] byMinute = new int[0];
        int[] byHour = new int[0];
        RecurrenceDay[] byDay = new RecurrenceDay[0];
        int[] byMonthDay = new int[0];
        int[] byYearDay = new int[0];
        int[] byWeekNo = new int[0];
        int[] byMonth = new int[0];
        int[] bySetPos = new int[0];

        string[] parts = ruleText.Split(';');
        string[] seenKeys = new string[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            int separator = IsoParsing.FindSingleSeparator(parts[index], '=');
            string key = parts[index].Substring(0, separator);
            string value = parts[index].Substring(separator + 1);
            RequireUniquePart(key, seenKeys, index);
            seenKeys[index] = key;

            if (key == "FREQ")
            {
                frequency = ParseFrequency(value);
                hasFrequency = true;
            }
            else if (key == "INTERVAL")
            {
                interval = ParsePositiveInteger(value, "INTERVAL");
            }
            else if (key == "COUNT")
            {
                count = ParsePositiveInteger(value, "COUNT");
            }
            else if (key == "UNTIL")
            {
                if (value.EndsWith("Z", StringComparison.Ordinal))
                {
                    until = ParseUtcUntil(value);
                    localUntil = null;
                }
                else
                {
                    localUntil = ParseLocalUntil(value);
                    until = null;
                }
            }
            else if (key == "WKST")
            {
                weekStart = ParseWeekday(value);
            }
            else if (key == "BYSECOND")
            {
                bySecond = ParseIntegerList(value);
            }
            else if (key == "BYMINUTE")
            {
                byMinute = ParseIntegerList(value);
            }
            else if (key == "BYHOUR")
            {
                byHour = ParseIntegerList(value);
            }
            else if (key == "BYDAY")
            {
                byDay = ParseDayList(value);
            }
            else if (key == "BYMONTHDAY")
            {
                byMonthDay = ParseIntegerList(value);
            }
            else if (key == "BYYEARDAY")
            {
                byYearDay = ParseIntegerList(value);
            }
            else if (key == "BYWEEKNO")
            {
                byWeekNo = ParseIntegerList(value);
            }
            else if (key == "BYMONTH")
            {
                byMonth = ParseIntegerList(value);
            }
            else if (key == "BYSETPOS")
            {
                bySetPos = ParseIntegerList(value);
            }
            else
            {
                throw new FormatException("Unsupported recurrence rule part.");
            }

            index = index + 1;
        }

        if (!hasFrequency)
        {
            throw new FormatException("Recurrence rule must include FREQ.");
        }

        return new RecurrenceRule(
            frequency,
            interval,
            count,
            until,
            localUntil,
            weekStart,
            bySecond,
            byMinute,
            byHour,
            byDay,
            byMonthDay,
            byYearDay,
            byWeekNo,
            byMonth,
            bySetPos);
    }

    /// <summary>Gets the required base frequency.</summary><returns>The FREQ value.</returns>
    public RecurrenceFrequency GetFrequency()
    {
        return frequency;
    }

    /// <summary>Gets the positive number of frequency units between candidate periods.</summary><returns>The INTERVAL value, normally 1.</returns>
    public int GetInterval()
    {
        return interval;
    }

    /// <summary>Gets the optional occurrence limit.</summary><returns>The COUNT value, or zero when absent.</returns>
    public int GetCount()
    {
        return count;
    }

    /// <summary>Gets the optional inclusive UTC upper bound.</summary><returns>The UNTIL instant, or null when absent.</returns>
    public Instant? GetUntil()
    {
        return until;
    }

    /// <summary>Gets the optional inclusive floating-local upper bound.</summary><returns>The local UNTIL value, or null when absent.</returns>
    public LocalDateTime? GetLocalUntil()
    {
        return localUntil;
    }

    /// <summary>Gets the ISO weekday that begins a recurrence week.</summary><returns>The WKST value, Monday as 1 through Sunday as 7.</returns>
    public int GetWeekStart()
    {
        return weekStart;
    }

    /// <summary>Gets a copy of allowed second numbers.</summary><returns>BYSECOND values from 0 through 59.</returns>
    public int[] GetBySecond()
    {
        return CopyIntArray(bySecond);
    }

    /// <summary>Gets a copy of allowed minute numbers.</summary><returns>BYMINUTE values from 0 through 59.</returns>
    public int[] GetByMinute()
    {
        return CopyIntArray(byMinute);
    }

    /// <summary>Gets a copy of allowed hour numbers.</summary><returns>BYHOUR values from 0 through 23.</returns>
    public int[] GetByHour()
    {
        return CopyIntArray(byHour);
    }

    /// <summary>Gets a copy of weekday and ordinal filters.</summary><returns>The BYDAY values.</returns>
    public RecurrenceDay[] GetByDay()
    {
        return CopyDayArray(byDay);
    }

    /// <summary>Gets a copy of month-day positions.</summary><returns>Nonzero BYMONTHDAY values from -31 through 31.</returns>
    public int[] GetByMonthDay()
    {
        return CopyIntArray(byMonthDay);
    }

    /// <summary>Gets a copy of year-day positions.</summary><returns>Nonzero BYYEARDAY values from -366 through 366.</returns>
    public int[] GetByYearDay()
    {
        return CopyIntArray(byYearDay);
    }

    /// <summary>Gets a copy of week-number positions.</summary><returns>Nonzero BYWEEKNO values from -53 through 53.</returns>
    public int[] GetByWeekNo()
    {
        return CopyIntArray(byWeekNo);
    }

    /// <summary>Gets a copy of allowed month numbers.</summary><returns>BYMONTH values from 1 through 12.</returns>
    public int[] GetByMonth()
    {
        return CopyIntArray(byMonth);
    }

    /// <summary>Gets a copy of final candidate-set positions.</summary><returns>Nonzero BYSETPOS values from -366 through 366.</returns>
    public int[] GetBySetPos()
    {
        return CopyIntArray(bySetPos);
    }

    /// <summary>Formats this rule as a canonical RFC 5545 RRULE value without the property prefix.</summary><returns>Invariant semicolon-separated rule text.</returns>
    public string ToRRuleString()
    {
        string text = "FREQ=" + FormatFrequency(frequency);

        if (interval != 1)
        {
            text = text + ";INTERVAL=" + interval.ToString(CultureInfo.InvariantCulture);
        }

        if (count > 0)
        {
            text = text + ";COUNT=" + count.ToString(CultureInfo.InvariantCulture);
        }

        if (until != null)
        {
            text = text + ";UNTIL=" + FormatUtcUntil(until);
        }

        if (localUntil != null)
        {
            text = text + ";UNTIL=" + localUntil.ToIsoBasicString();
        }

        if (weekStart != TimeConstants.IsoMonday)
        {
            text = text + ";WKST=" + FormatWeekday(weekStart);
        }

        text = AppendIntList(text, "BYSECOND", bySecond);
        text = AppendIntList(text, "BYMINUTE", byMinute);
        text = AppendIntList(text, "BYHOUR", byHour);
        text = AppendDayList(text, byDay);
        text = AppendIntList(text, "BYMONTHDAY", byMonthDay);
        text = AppendIntList(text, "BYYEARDAY", byYearDay);
        text = AppendIntList(text, "BYWEEKNO", byWeekNo);
        text = AppendIntList(text, "BYMONTH", byMonth);
        text = AppendIntList(text, "BYSETPOS", bySetPos);

        return text;
    }

    internal void RequireFloatingStart()
    {
        if (until != null)
        {
            throw new ArgumentException("A floating recurrence requires a local UNTIL value.", "rule");
        }
    }

    internal void RequireUtcStart()
    {
        if (localUntil != null)
        {
            throw new ArgumentException("A zoned or instant recurrence requires a UTC UNTIL value.", "rule");
        }
    }

    static void RequireFrequency(RecurrenceFrequency frequency, string parameterName)
    {
        int value = (int)frequency;
        if (value < (int)RecurrenceFrequency.Secondly || value > (int)RecurrenceFrequency.Yearly)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Recurrence frequency is not supported.");
        }
    }

    static void RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be positive.");
        }
    }

    static void RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must not be negative.");
        }
    }

    static void RequireRange(int[] values, int min, int max, bool allowZero, string parameterName)
    {
        int index = 0;
        while (index < values.Length)
        {
            int value = values[index];
            if (value < min || value > max || (!allowZero && value == 0))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Recurrence rule list value is outside the supported range.");
            }

            index = index + 1;
        }
    }

    static void RequireRecurrenceDays(RecurrenceDay[] values, string parameterName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Recurrence day values must not be null.", parameterName);
            }

            index = index + 1;
        }
    }

    static void RequireCompatibleFilters(
        RecurrenceFrequency frequency,
        RecurrenceDay[] byDay,
        int[] byMonthDay,
        int[] byYearDay,
        int[] byWeekNo,
        int[] bySetPos,
        int[] bySecond,
        int[] byMinute,
        int[] byHour,
        int[] byMonth)
    {
        if (byWeekNo.Length > 0 && frequency != RecurrenceFrequency.Yearly)
        {
            throw new ArgumentException("BYWEEKNO is supported only with YEARLY frequency.", "byWeekNo");
        }

        if (byYearDay.Length > 0
            && (frequency == RecurrenceFrequency.Daily
                || frequency == RecurrenceFrequency.Weekly
                || frequency == RecurrenceFrequency.Monthly))
        {
            throw new ArgumentException("BYYEARDAY is not supported with DAILY, WEEKLY, or MONTHLY frequency.", "byYearDay");
        }

        if (byMonthDay.Length > 0 && frequency == RecurrenceFrequency.Weekly)
        {
            throw new ArgumentException("BYMONTHDAY is not supported with WEEKLY frequency.", "byMonthDay");
        }

        int dayIndex = 0;
        while (dayIndex < byDay.Length)
        {
            if (byDay[dayIndex].HasOrdinal()
                && frequency != RecurrenceFrequency.Monthly
                && frequency != RecurrenceFrequency.Yearly)
            {
                throw new ArgumentException("Ordinal BYDAY is supported only with MONTHLY or YEARLY frequency.", "byDay");
            }

            if (byDay[dayIndex].HasOrdinal()
                && frequency == RecurrenceFrequency.Yearly
                && byWeekNo.Length > 0)
            {
                throw new ArgumentException("Ordinal BYDAY must not be combined with BYWEEKNO in a YEARLY rule.", "byDay");
            }

            dayIndex = dayIndex + 1;
        }

        if (bySetPos.Length > 0
            && bySecond.Length == 0
            && byMinute.Length == 0
            && byHour.Length == 0
            && byDay.Length == 0
            && byMonthDay.Length == 0
            && byYearDay.Length == 0
            && byWeekNo.Length == 0
            && byMonth.Length == 0)
        {
            throw new ArgumentException("BYSETPOS requires at least one other BY filter.", "bySetPos");
        }
    }

    static int[] CopyIntArray(int[] values)
    {
        int[] copy = new int[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    static RecurrenceDay[] CopyDayArray(RecurrenceDay[] values)
    {
        RecurrenceDay[] copy = new RecurrenceDay[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    static RecurrenceFrequency ParseFrequency(string text)
    {
        if (text == "SECONDLY")
        {
            return RecurrenceFrequency.Secondly;
        }

        if (text == "MINUTELY")
        {
            return RecurrenceFrequency.Minutely;
        }

        if (text == "HOURLY")
        {
            return RecurrenceFrequency.Hourly;
        }

        if (text == "DAILY")
        {
            return RecurrenceFrequency.Daily;
        }

        if (text == "WEEKLY")
        {
            return RecurrenceFrequency.Weekly;
        }

        if (text == "MONTHLY")
        {
            return RecurrenceFrequency.Monthly;
        }

        if (text == "YEARLY")
        {
            return RecurrenceFrequency.Yearly;
        }

        throw new FormatException("Recurrence frequency is not supported.");
    }

    static string FormatFrequency(RecurrenceFrequency frequency)
    {
        if (frequency == RecurrenceFrequency.Secondly)
        {
            return "SECONDLY";
        }

        if (frequency == RecurrenceFrequency.Minutely)
        {
            return "MINUTELY";
        }

        if (frequency == RecurrenceFrequency.Hourly)
        {
            return "HOURLY";
        }

        if (frequency == RecurrenceFrequency.Daily)
        {
            return "DAILY";
        }

        if (frequency == RecurrenceFrequency.Weekly)
        {
            return "WEEKLY";
        }

        if (frequency == RecurrenceFrequency.Monthly)
        {
            return "MONTHLY";
        }

        return "YEARLY";
    }

    static int ParsePositiveInteger(string text, string name)
    {
        int value = ParseUnsignedInteger(text);
        if (value <= 0)
        {
            throw new FormatException(name + " must be positive.");
        }

        return value;
    }

    static int ParseInteger(string text)
    {
        int sign = 1;
        int start = 0;
        if (text.Length > 0 && text[0] == '-')
        {
            sign = -1;
            start = 1;
        }
        else if (text.Length > 0 && text[0] == '+')
        {
            start = 1;
        }

        return sign * ParseUnsignedInteger(text.Substring(start));
    }

    static int ParseUnsignedInteger(string text)
    {
        if (text.Length == 0)
        {
            throw new FormatException("Expected at least one digit.");
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (!TextCursor.IsDigit(character))
            {
                throw new FormatException("Expected only decimal digits.");
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                throw new FormatException("Recurrence integer is too large.");
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        return value;
    }

    static int[] ParseIntegerList(string text)
    {
        string[] parts = text.Split(',');
        int[] values = new int[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            values[index] = ParseInteger(parts[index]);
            index = index + 1;
        }

        return values;
    }

    static RecurrenceDay[] ParseDayList(string text)
    {
        string[] parts = text.Split(',');
        RecurrenceDay[] values = new RecurrenceDay[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            values[index] = ParseDay(parts[index]);
            index = index + 1;
        }

        return values;
    }

    static RecurrenceDay ParseDay(string text)
    {
        if (text.Length < 2)
        {
            throw new FormatException("Recurrence day token is too short.");
        }

        int dayStart = text.Length - 2;
        string dayText = text.Substring(dayStart);
        int ordinal = RecurrenceDay.NoOrdinal;
        if (dayStart > 0)
        {
            ordinal = ParseInteger(text.Substring(0, dayStart));
        }

        return RecurrenceDay.Create(ParseWeekday(dayText), ordinal);
    }

    static int ParseWeekday(string text)
    {
        if (text == "MO")
        {
            return TimeConstants.IsoMonday;
        }

        if (text == "TU")
        {
            return TimeConstants.IsoTuesday;
        }

        if (text == "WE")
        {
            return TimeConstants.IsoWednesday;
        }

        if (text == "TH")
        {
            return TimeConstants.IsoThursday;
        }

        if (text == "FR")
        {
            return TimeConstants.IsoFriday;
        }

        if (text == "SA")
        {
            return TimeConstants.IsoSaturday;
        }

        if (text == "SU")
        {
            return TimeConstants.IsoSunday;
        }

        throw new FormatException("Weekday token is not supported.");
    }

    static string FormatWeekday(int dayOfWeek)
    {
        if (dayOfWeek == TimeConstants.IsoMonday)
        {
            return "MO";
        }

        if (dayOfWeek == TimeConstants.IsoTuesday)
        {
            return "TU";
        }

        if (dayOfWeek == TimeConstants.IsoWednesday)
        {
            return "WE";
        }

        if (dayOfWeek == TimeConstants.IsoThursday)
        {
            return "TH";
        }

        if (dayOfWeek == TimeConstants.IsoFriday)
        {
            return "FR";
        }

        if (dayOfWeek == TimeConstants.IsoSaturday)
        {
            return "SA";
        }

        return "SU";
    }

    static Instant ParseUtcUntil(string text)
    {
        if (text.Length != 16 || text[8] != 'T' || text[15] != 'Z')
        {
            throw new FormatException("UTC UNTIL must use YYYYMMDDTHHMMSSZ format.");
        }

        LocalDateTime value = ParseLocalUntil(text.Substring(0, 15));

        return Instant.FromUtcDateTime(new DateTime(
            value.GetYear(),
            value.GetMonth(),
            value.GetDay(),
            value.GetHour(),
            value.GetMinute(),
            value.GetSecond(),
            DateTimeKind.Utc));
    }

    static LocalDateTime ParseLocalUntil(string text)
    {
        if (text.Length != 15 || text[8] != 'T')
        {
            throw new FormatException("Local UNTIL must use YYYYMMDDTHHMMSS format.");
        }

        return LocalDateTime.ParseIsoBasic(text);
    }

    static void RequireUniquePart(string key, string[] seenKeys, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (seenKeys[index] == key)
            {
                throw new FormatException("Recurrence rule part must not occur more than once.");
            }

            index = index + 1;
        }
    }

    static string FormatUtcUntil(Instant instant)
    {
        DateTime dateTime = instant.ToUtcDateTime();
        return IsoFormatting.FormatFixedDigits(dateTime.Year, 4)
            + IsoFormatting.FormatFixedDigits(dateTime.Month, 2)
            + IsoFormatting.FormatFixedDigits(dateTime.Day, 2)
            + "T"
            + IsoFormatting.FormatFixedDigits(dateTime.Hour, 2)
            + IsoFormatting.FormatFixedDigits(dateTime.Minute, 2)
            + IsoFormatting.FormatFixedDigits(dateTime.Second, 2)
            + "Z";
    }

    static string AppendIntList(string text, string name, int[] values)
    {
        if (values.Length == 0)
        {
            return text;
        }

        return text + ";" + name + "=" + FormatIntList(values);
    }

    static string FormatIntList(int[] values)
    {
        string text = values[0].ToString(CultureInfo.InvariantCulture);
        int index = 1;
        while (index < values.Length)
        {
            text = text + "," + values[index].ToString(CultureInfo.InvariantCulture);
            index = index + 1;
        }

        return text;
    }

    static string AppendDayList(string text, RecurrenceDay[] values)
    {
        if (values.Length == 0)
        {
            return text;
        }

        string result = text + ";BYDAY=" + FormatDay(values[0]);
        int index = 1;
        while (index < values.Length)
        {
            result = result + "," + FormatDay(values[index]);
            index = index + 1;
        }

        return result;
    }

    static string FormatDay(RecurrenceDay day)
    {
        string text = "";
        if (day.HasOrdinal())
        {
            text = day.GetOrdinal().ToString(CultureInfo.InvariantCulture);
        }

        return text + FormatWeekday(day.GetDayOfWeek());
    }
}
