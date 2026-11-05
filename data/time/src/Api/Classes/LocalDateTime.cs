using System;

namespace time;

/// <summary>Combines a local Gregorian date and wall-clock time without an offset or time zone.</summary>
/// <remarks>This is a floating civil value. It does not identify an instant until mapped through a <see cref="TimeZoneRef"/> or paired with an <see cref="Offset"/>.</remarks>
public class LocalDateTime : ILocalDateTime
{
    LocalDate date;
    LocalTime time;

    internal LocalDateTime(LocalDate date, LocalTime time)
    {
        TimeValidation.RequireNotNull(date, "date");
        TimeValidation.RequireNotNull(time, "time");

        this.date = date;
        this.time = time;
    }

    /// <summary>Combines an existing date and time.</summary><param name="date">The local date.</param><param name="time">The local time.</param><returns>The combined local date-time.</returns>
    internal static LocalDateTime Create(LocalDate date, LocalTime time)
    {
        return new LocalDateTime(date, time);
    }

    /// <summary>Creates a local date-time from complete calendar and clock fields.</summary>
    /// <param name="year">The year from 1 through 9999.</param><param name="month">The month from 1 through 12.</param><param name="day">The valid day of month.</param>
    /// <param name="hour">The hour from 0 through 23.</param><param name="minute">The minute from 0 through 59.</param><param name="second">The second from 0 through 59.</param>
    /// <param name="millisecond">The millisecond from 0 through 999.</param><param name="microsecond">The following microsecond from 0 through 999.</param><param name="tickRemainder">The following tick from 0 through 9.</param>
    /// <returns>The validated local date-time.</returns>
    internal static LocalDateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        return new LocalDateTime(
            LocalDate.Create(year, month, day),
            LocalTime.Create(hour, minute, second, millisecond, microsecond, tickRemainder));
    }

    /// <summary>Creates a whole-second local date-time.</summary>
    /// <param name="year">The year.</param><param name="month">The month.</param><param name="day">The day.</param><param name="hour">The hour.</param><param name="minute">The minute.</param><param name="second">The second.</param>
    /// <returns>The validated local date-time.</returns>
    internal static LocalDateTime Create(int year, int month, int day, int hour, int minute, int second)
    {
        return new LocalDateTime(
            LocalDate.Create(year, month, day),
            LocalTime.Create(hour, minute, second));
    }

    /// <summary>Copies the calendar and clock fields of a built-in date-time.</summary><remarks>The kind is ignored and no time-zone conversion occurs.</remarks><param name="dateTime">The value to copy.</param><returns>The equivalent floating local date-time.</returns>
    internal static LocalDateTime FromDateTime(DateTime dateTime)
    {
        return new LocalDateTime(
            LocalDate.FromDateTime(dateTime),
            LocalTime.FromTicksSinceMidnight(dateTime.TimeOfDay.Ticks));
    }

    /// <summary>Parses invariant ISO local date-time text.</summary><param name="text">A date and time separated by <c>T</c>.</param><returns>The parsed local value.</returns><exception cref="FormatException">The text is not supported ISO local date-time text.</exception>
    internal static LocalDateTime Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length < 19)
        {
            throw new FormatException("Local date-time text must use ISO local date-time format.");
        }

        if (text[10] != 'T')
        {
            throw new FormatException("Local date-time date and time must be separated by T.");
        }

        string dateText = text.Substring(0, 10);
        string timeText = text.Substring(11);

        return new LocalDateTime(
            LocalDate.Parse(dateText),
            LocalTime.Parse(timeText));
    }

    /// <summary>Parses invariant ISO basic local date-time text.</summary><param name="text">Text in <c>yyyyMMddTHHmmss</c> form with optional fractional seconds.</param><returns>The parsed local value.</returns><exception cref="FormatException">The text is not supported ISO basic local date-time text.</exception>
    internal static LocalDateTime ParseIsoBasic(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length != 15 && (text.Length < 17 || text.Length > 23))
        {
            throw new FormatException("Local date-time text must use ISO basic local date-time format.");
        }

        if (text[8] != 'T')
        {
            throw new FormatException("Local date-time date and time must be separated by T.");
        }

        if (text.Length > 15 && text[15] != '.')
        {
            throw new FormatException("Local date-time fractional seconds must start with a decimal point.");
        }

        string timeText = text.Substring(9, 2)
            + ":"
            + text.Substring(11, 2)
            + ":"
            + text.Substring(13);

        return new LocalDateTime(
            LocalDate.ParseIsoBasic(text.Substring(0, 8)),
            LocalTime.Parse(timeText));
    }

    /// <summary>Gets the date portion.</summary><returns>The local date.</returns>
    public LocalDate GetDate()
    {
        return date;
    }

    /// <summary>Gets the time portion.</summary><returns>The local time.</returns>
    public LocalTime GetTime()
    {
        return time;
    }

    /// <summary>Gets the year.</summary><returns>The Gregorian year.</returns>
    public int GetYear()
    {
        return date.GetYear();
    }

    /// <summary>Gets the month.</summary><returns>The month from 1 through 12.</returns>
    public int GetMonth()
    {
        return date.GetMonth();
    }

    /// <summary>Gets the day.</summary><returns>The day of month.</returns>
    public int GetDay()
    {
        return date.GetDay();
    }

    /// <summary>Gets the hour.</summary><returns>The hour from 0 through 23.</returns>
    public int GetHour()
    {
        return time.GetHour();
    }

    /// <summary>Gets the minute.</summary><returns>The minute from 0 through 59.</returns>
    public int GetMinute()
    {
        return time.GetMinute();
    }

    /// <summary>Gets the second.</summary><returns>The second from 0 through 59.</returns>
    public int GetSecond()
    {
        return time.GetSecond();
    }

    /// <summary>Gets the millisecond component.</summary><returns>A value from 0 through 999.</returns>
    public int GetMillisecond()
    {
        return time.GetMillisecond();
    }

    /// <summary>Gets the following microsecond component.</summary><returns>A value from 0 through 999.</returns>
    public int GetMicrosecond()
    {
        return time.GetMicrosecond();
    }

    /// <summary>Gets the following 100-nanosecond tick component.</summary><returns>A value from 0 through 9.</returns>
    public int GetTickRemainder()
    {
        return time.GetTickRemainder();
    }

    /// <summary>Orders values by local calendar and clock fields.</summary><param name="other">The local value to compare.</param><returns>A negative value, zero, or a positive value according to local field order.</returns>
    public int CompareTo(LocalDateTime other)
    {
        TimeValidation.RequireNotNull(other, "other");

        int dateComparison = date.CompareTo(other.GetDate());
        if (dateComparison != 0)
        {
            return dateComparison;
        }

        return time.CompareTo(other.GetTime());
    }

    /// <summary>Tests equality of the complete local date and time.</summary><param name="other">The value to compare, or null.</param><returns><c>true</c> when every local field matches.</returns>
    public bool EqualsValue(LocalDateTime? other)
    {
        if (other == null)
        {
            return false;
        }

        return date.EqualsValue(other.GetDate())
            && time.EqualsValue(other.GetTime());
    }

    /// <summary>Converts this value to a built-in date-time without assigning zone semantics.</summary><returns>A <see cref="DateTime"/> whose kind is unspecified.</returns>
    public DateTime ToDateTimeUnspecified()
    {
        long ticks = (long)date.GetDayNumber() * TimeConstants.TicksPerDay + time.GetTicksSinceMidnight();
        return new DateTime(ticks, DateTimeKind.Unspecified);
    }

    /// <summary>Formats this value as invariant ISO local date-time text.</summary><returns>A date and time joined with <c>T</c>.</returns>
    public string ToIsoString()
    {
        return date.ToIsoString() + "T" + time.ToIsoString();
    }

    /// <summary>Formats this value as invariant ISO basic local date-time text.</summary><returns>Text in <c>yyyyMMddTHHmmss</c> form with fractional seconds only when needed.</returns>
    public string ToIsoBasicString()
    {
        return date.ToIsoBasicString() + "T" + time.ToIsoString().Replace(":", "", StringComparison.Ordinal);
    }

    /// <summary>Adds exact ticks to the local fields, carrying across calendar days.</summary><param name="duration">The signed elapsed amount.</param><returns>The resulting floating local date-time.</returns>
    public LocalDateTime AddDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return AddTickDelta(duration.GetTicks(), "duration");
    }

    /// <summary>Subtracts exact ticks from the local fields, carrying across calendar days.</summary><param name="duration">The signed elapsed amount.</param><returns>The resulting floating local date-time.</returns>
    public LocalDateTime SubtractDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return AddTickDelta(-((decimal)duration.GetTicks()), "duration");
    }

    /// <summary>Adds a calendar period in local field order.</summary><remarks>Years and months are applied to the date before weeks, days, and the exact time fields.</remarks><param name="period">The period to add.</param><returns>The resulting local date-time.</returns>
    public LocalDateTime AddPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");

        Period datePeriod = Period.Create(
            period.GetYears(),
            period.GetMonths(),
            period.GetWeeks(),
            period.GetDays(),
            0L,
            0L,
            0L,
            0L,
            0L,
            0L);
        LocalDate resultDate = date.AddPeriod(datePeriod);
        long timeTicks = GetPeriodTimeTicks(period);
        LocalTimeRoll roll = time.AddDuration(Duration.FromTicks(timeTicks));
        resultDate = resultDate.AddDays(roll.GetDayDelta());

        return new LocalDateTime(resultDate, roll.GetTime());
    }

    /// <summary>Subtracts a calendar period in local field order.</summary><param name="period">The period to subtract.</param><returns>The resulting local date-time.</returns>
    public LocalDateTime SubtractPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");

        return AddPeriod(period.Negate());
    }

    LocalDateTime AddTickDelta(decimal tickDelta, string parameterName)
    {
        decimal currentTicks = (long)date.GetDayNumber() * TimeConstants.TicksPerDay + time.GetTicksSinceMidnight();
        decimal totalTicks = currentTicks + tickDelta;
        decimal maxTicks = (long)TimeConstants.MaxDayNumber * TimeConstants.TicksPerDay + TimeConstants.MaxTicksSinceMidnight;

        if (totalTicks < 0m || totalTicks > maxTicks)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Date-time arithmetic moved outside the supported local date-time range.");
        }

        decimal dayNumber = decimal.Truncate(totalTicks / TimeConstants.TicksPerDay);
        decimal ticksOfDay = totalTicks % TimeConstants.TicksPerDay;

        return new LocalDateTime(
            LocalDate.FromDayNumber((int)dayNumber),
            LocalTime.FromTicksSinceMidnight((long)ticksOfDay));
    }

    static long GetPeriodTimeTicks(Period period)
    {
        return period.GetHours() * TimeConstants.TicksPerHour
            + period.GetMinutes() * TimeConstants.TicksPerMinute
            + period.GetSeconds() * TimeConstants.TicksPerSecond
            + period.GetMilliseconds() * TimeConstants.TicksPerMillisecond
            + period.GetMicroseconds() * TimeConstants.TicksPerMicrosecond
            + period.GetTicks();
    }
}
