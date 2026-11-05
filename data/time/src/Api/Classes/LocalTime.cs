using System;
using System.Globalization;

namespace time;

/// <summary>Represents a wall-clock time without a date, offset, or time zone.</summary>
/// <remarks>Precision is one .NET tick, or 100 nanoseconds. Arithmetic returns a <see cref="LocalTimeRoll"/> so crossed calendar days are not lost.</remarks>
public class LocalTime : ILocalTime
{
    long ticksSinceMidnight;

    internal LocalTime(long ticksSinceMidnight)
    {
        TimeValidation.RequireTicksSinceMidnight(ticksSinceMidnight, "ticksSinceMidnight");

        this.ticksSinceMidnight = ticksSinceMidnight;
    }

    /// <summary>Gets 00:00:00.</summary>
    /// <returns>Midnight.</returns>
    internal static LocalTime Midnight()
    {
        return new LocalTime(TimeConstants.MinTicksSinceMidnight);
    }

    /// <summary>Gets 12:00:00.</summary>
    /// <returns>Noon.</returns>
    internal static LocalTime Noon()
    {
        return new LocalTime(TimeConstants.TicksPerHour * 12L);
    }

    /// <summary>Creates a local time from complete clock fields.</summary>
    /// <param name="hour">The hour from 0 through 23.</param>
    /// <param name="minute">The minute from 0 through 59.</param>
    /// <param name="second">The second from 0 through 59; leap seconds are not represented.</param>
    /// <param name="millisecond">The millisecond from 0 through 999.</param>
    /// <param name="microsecond">The additional microsecond from 0 through 999.</param>
    /// <param name="tickRemainder">The remaining 100-nanosecond ticks from 0 through 9.</param>
    /// <returns>The validated local time.</returns>
    internal static LocalTime Create(int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        TimeValidation.RequireTime(hour, minute, second, millisecond, microsecond, tickRemainder);

        long ticks =
            (long)hour * TimeConstants.TicksPerHour
            + (long)minute * TimeConstants.TicksPerMinute
            + (long)second * TimeConstants.TicksPerSecond
            + (long)millisecond * TimeConstants.TicksPerMillisecond
            + (long)microsecond * TimeConstants.TicksPerMicrosecond
            + tickRemainder;

        return new LocalTime(ticks);
    }

    /// <summary>Creates a whole-second local time.</summary>
    /// <param name="hour">The hour from 0 through 23.</param>
    /// <param name="minute">The minute from 0 through 59.</param>
    /// <param name="second">The second from 0 through 59.</param>
    /// <returns>The validated local time.</returns>
    internal static LocalTime Create(int hour, int minute, int second)
    {
        return Create(hour, minute, second, 0, 0, 0);
    }

    /// <summary>Creates a local time from a built-in time-only value.</summary>
    /// <param name="time">The value whose ticks since midnight are copied.</param>
    /// <returns>The equivalent local time.</returns>
    internal static LocalTime FromTimeOnly(TimeOnly time)
    {
        return new LocalTime(time.Ticks);
    }

    /// <summary>Creates a local time from 100-nanosecond ticks since midnight.</summary>
    /// <param name="ticksSinceMidnight">A value from zero through the final tick of the day.</param>
    /// <returns>The corresponding local time.</returns>
    internal static LocalTime FromTicksSinceMidnight(long ticksSinceMidnight)
    {
        return new LocalTime(ticksSinceMidnight);
    }

    /// <summary>Parses invariant ISO local-time text.</summary>
    /// <param name="text">Text in <c>HH:mm:ss</c> form with an optional fractional second.</param>
    /// <returns>The parsed local time.</returns>
    /// <exception cref="FormatException">The text is malformed or exceeds seven fractional digits.</exception>
    internal static LocalTime Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length != 8 && (text.Length < 10 || text.Length > 16))
        {
            throw new FormatException("Local time text must use HH:mm:ss with optional fractional seconds.");
        }

        if (text[2] != ':' || text[5] != ':')
        {
            throw new FormatException("Local time text must use HH:mm:ss with optional fractional seconds.");
        }

        int parsedHour = ParseFixedDigits(text, 0, 2);
        int parsedMinute = ParseFixedDigits(text, 3, 2);
        int parsedSecond = ParseFixedDigits(text, 6, 2);
        long fractionalTicks = 0L;

        if (text.Length > 8)
        {
            if (text[8] != '.')
            {
                throw new FormatException("Local time fractional seconds must start with a decimal point.");
            }

            fractionalTicks = ParseFractionalSecondTicks(text, 9);
        }

        if (parsedHour < TimeConstants.MinHour || parsedHour > TimeConstants.MaxHour)
        {
            throw new FormatException("Local time hour is outside the supported range.");
        }

        if (parsedMinute < TimeConstants.MinMinute || parsedMinute > TimeConstants.MaxMinute)
        {
            throw new FormatException("Local time minute is outside the supported range.");
        }

        if (parsedSecond < TimeConstants.MinSecond || parsedSecond > TimeConstants.MaxSecond)
        {
            throw new FormatException("Local time second is outside the supported range.");
        }

        long ticks = (long)parsedHour * TimeConstants.TicksPerHour
            + (long)parsedMinute * TimeConstants.TicksPerMinute
            + (long)parsedSecond * TimeConstants.TicksPerSecond
            + fractionalTicks;

        return new LocalTime(ticks);
    }

    /// <summary>Gets the hour.</summary><returns>A value from 0 through 23.</returns>
    public int GetHour()
    {
        return (int)(ticksSinceMidnight / TimeConstants.TicksPerHour);
    }

    /// <summary>Gets the minute.</summary><returns>A value from 0 through 59.</returns>
    public int GetMinute()
    {
        long remaining = ticksSinceMidnight % TimeConstants.TicksPerHour;
        return (int)(remaining / TimeConstants.TicksPerMinute);
    }

    /// <summary>Gets the second.</summary><returns>A value from 0 through 59.</returns>
    public int GetSecond()
    {
        long remaining = ticksSinceMidnight % TimeConstants.TicksPerMinute;
        return (int)(remaining / TimeConstants.TicksPerSecond);
    }

    /// <summary>Gets the millisecond component.</summary><returns>A value from 0 through 999.</returns>
    public int GetMillisecond()
    {
        long remaining = ticksSinceMidnight % TimeConstants.TicksPerSecond;
        return (int)(remaining / TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Gets the microsecond component following the millisecond.</summary><returns>A value from 0 through 999.</returns>
    public int GetMicrosecond()
    {
        long remaining = ticksSinceMidnight % TimeConstants.TicksPerMillisecond;
        return (int)(remaining / TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Gets the 100-nanosecond tick component following the microsecond.</summary><returns>A value from 0 through 9.</returns>
    public int GetTickRemainder()
    {
        return (int)(ticksSinceMidnight % TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Gets the complete time as 100-nanosecond ticks since midnight.</summary><returns>A value within one day.</returns>
    public long GetTicksSinceMidnight()
    {
        return ticksSinceMidnight;
    }

    /// <summary>Orders times by their position within a day.</summary>
    /// <param name="other">The local time to compare.</param>
    /// <returns>A negative value, zero, or a positive value when this time is earlier than, equal to, or later than the other.</returns>
    public int CompareTo(LocalTime other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return TimeComparison.CompareLong(ticksSinceMidnight, other.GetTicksSinceMidnight());
    }

    /// <summary>Tests equality of all time fields.</summary>
    /// <param name="other">The local time to compare, or null.</param>
    /// <returns><c>true</c> when the ticks since midnight match.</returns>
    public bool EqualsValue(LocalTime? other)
    {
        if (other == null)
        {
            return false;
        }

        return ticksSinceMidnight == other.GetTicksSinceMidnight();
    }

    /// <summary>Converts this value to the built-in time-only type.</summary><returns>A value with the same ticks since midnight.</returns>
    public TimeOnly ToTimeOnly()
    {
        return new TimeOnly(ticksSinceMidnight);
    }

    /// <summary>Formats this value as invariant ISO local-time text.</summary><returns>A clock string with fractional seconds only when needed.</returns>
    public string ToIsoString()
    {
        string text = GetHour().ToString("D2", CultureInfo.InvariantCulture)
            + ":"
            + GetMinute().ToString("D2", CultureInfo.InvariantCulture)
            + ":"
            + GetSecond().ToString("D2", CultureInfo.InvariantCulture);

        long fractionalTicks = ticksSinceMidnight % TimeConstants.TicksPerSecond;
        if (fractionalTicks > 0L)
        {
            string fraction = fractionalTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
            text = text + "." + fraction;
        }

        return text;
    }

    /// <summary>Adds an exact duration and records every crossed midnight.</summary>
    /// <param name="duration">The signed elapsed amount to add.</param>
    /// <returns>A normalized local time plus a signed day delta.</returns>
    public LocalTimeRoll AddDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return RollByTickDelta(duration.GetTicks());
    }

    /// <summary>Subtracts an exact duration and records every crossed midnight.</summary>
    /// <param name="duration">The signed elapsed amount to subtract.</param>
    /// <returns>A normalized local time plus a signed day delta.</returns>
    public LocalTimeRoll SubtractDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return RollByTickDelta(-((decimal)duration.GetTicks()));
    }

    /// <summary>Adds a period that does not require a calendar date and records every crossed midnight.</summary>
    /// <remarks>Weeks and days contribute calendar-day rollover, while hour through tick fields contribute time-of-day rollover. Year and month fields are not meaningful without a date.</remarks>
    /// <param name="period">The signed period to add.</param>
    /// <returns>A normalized local time plus the combined signed day delta.</returns>
    /// <exception cref="ArgumentException">The period contains a nonzero year or month field.</exception>
    /// <exception cref="OverflowException">The period or resulting day delta is outside the supported range.</exception>
    public LocalTimeRoll AddPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");
        RequireTimeOnlyPeriod(period);

        return RollByTickDelta(GetPeriodTickDelta(period));
    }

    /// <summary>Subtracts a period that does not require a calendar date and records every crossed midnight.</summary>
    /// <remarks>Weeks and days contribute calendar-day rollover, while hour through tick fields contribute time-of-day rollover. Year and month fields are not meaningful without a date.</remarks>
    /// <param name="period">The signed period to subtract.</param>
    /// <returns>A normalized local time plus the combined signed day delta.</returns>
    /// <exception cref="ArgumentException">The period contains a nonzero year or month field.</exception>
    /// <exception cref="OverflowException">The period or resulting day delta is outside the supported range.</exception>
    public LocalTimeRoll SubtractPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");
        RequireTimeOnlyPeriod(period);

        return RollByTickDelta(-GetPeriodTickDelta(period));
    }

    static decimal GetPeriodTickDelta(Period period)
    {
        decimal calendarDays = (decimal)period.GetWeeks() * TimeConstants.DaysPerWeek + period.GetDays();
        return calendarDays * TimeConstants.TicksPerDay
            + (decimal)period.GetHours() * TimeConstants.TicksPerHour
            + (decimal)period.GetMinutes() * TimeConstants.TicksPerMinute
            + (decimal)period.GetSeconds() * TimeConstants.TicksPerSecond
            + (decimal)period.GetMilliseconds() * TimeConstants.TicksPerMillisecond
            + (decimal)period.GetMicroseconds() * TimeConstants.TicksPerMicrosecond
            + period.GetTicks();
    }

    static void RequireTimeOnlyPeriod(Period period)
    {
        if (period.GetYears() != 0L || period.GetMonths() != 0L)
        {
            throw new ArgumentException("LocalTime period arithmetic does not support year or month fields.", "period");
        }
    }

    LocalTimeRoll RollByTickDelta(decimal tickDelta)
    {
        decimal totalTicks = ticksSinceMidnight + tickDelta;
        decimal ticksPerDay = TimeConstants.TicksPerDay;
        decimal dayDelta = decimal.Truncate(totalTicks / ticksPerDay);
        decimal ticksOfDay = totalTicks % ticksPerDay;

        if (ticksOfDay < 0m)
        {
            ticksOfDay = ticksOfDay + ticksPerDay;
            dayDelta = dayDelta - 1m;
        }

        if (dayDelta < int.MinValue || dayDelta > int.MaxValue)
        {
            throw new OverflowException("Day rollover is outside the supported integer range.");
        }

        return LocalTimeRoll.Create((long)ticksOfDay, (int)dayDelta);
    }

    static int ParseFixedDigits(string text, int start, int length)
    {
        int index = start;
        int end = start + length;
        int value = 0;

        while (index < end)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                throw new FormatException("Local time components must contain only digits.");
            }

            value = value * 10 + (text[index] - '0');
            index = index + 1;
        }

        return value;
    }

    static long ParseFractionalSecondTicks(string text, int start)
    {
        int index = start;
        int digits = 0;
        long ticks = 0L;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                throw new FormatException("Fractional seconds must contain only digits.");
            }

            ticks = ticks * 10L + (text[index] - '0');
            digits = digits + 1;
            index = index + 1;
        }

        while (digits < 7)
        {
            ticks = ticks * 10L;
            digits = digits + 1;
        }

        return ticks;
    }
}
