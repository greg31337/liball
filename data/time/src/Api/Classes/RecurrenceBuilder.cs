using System;

namespace time;

/// <summary>Builds common recurrence rules through validated, chainable operations.</summary>
/// <remarks>The builder is mutable. <see cref="Build"/> creates an independent immutable-style rule snapshot.</remarks>
public class RecurrenceBuilder : IRecurrenceBuilder
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

    /// <summary>Starts a builder for any supported frequency.</summary><param name="frequency">The base recurrence frequency.</param>
    public RecurrenceBuilder(RecurrenceFrequency frequency)
    {
        RequireFrequency(frequency);

        this.frequency = frequency;
        interval = 1;
        count = 0;
        until = null;
        localUntil = null;
        weekStart = TimeConstants.IsoMonday;
        bySecond = new int[0];
        byMinute = new int[0];
        byHour = new int[0];
        byDay = new RecurrenceDay[0];
        byMonthDay = new int[0];
        byYearDay = new int[0];
        byWeekNo = new int[0];
        byMonth = new int[0];
        bySetPos = new int[0];
    }

    /// <summary>Starts a daily rule.</summary><returns>A daily recurrence builder.</returns>
    internal static RecurrenceBuilder Daily()
    {
        return new RecurrenceBuilder(RecurrenceFrequency.Daily);
    }

    /// <summary>Starts a weekly rule.</summary><returns>A weekly recurrence builder.</returns>
    internal static RecurrenceBuilder Weekly()
    {
        return new RecurrenceBuilder(RecurrenceFrequency.Weekly);
    }

    /// <summary>Starts a monthly rule.</summary><returns>A monthly recurrence builder.</returns>
    internal static RecurrenceBuilder Monthly()
    {
        return new RecurrenceBuilder(RecurrenceFrequency.Monthly);
    }

    /// <summary>Starts a yearly rule.</summary><returns>A yearly recurrence builder.</returns>
    internal static RecurrenceBuilder Yearly()
    {
        return new RecurrenceBuilder(RecurrenceFrequency.Yearly);
    }

    /// <summary>Sets how many frequency units separate candidate periods.</summary><param name="interval">A positive interval; 1 means every unit.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder SetInterval(int interval)
    {
        if (interval <= 0)
        {
            throw new ArgumentOutOfRangeException("interval", "Recurrence interval must be positive.");
        }

        this.interval = interval;
        return this;
    }

    /// <summary>Limits the rule to a positive number of emitted occurrences.</summary><param name="count">The positive occurrence limit.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder SetCount(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException("count", "Recurrence count must be positive.");
        }

        this.count = count;
        until = null;
        localUntil = null;
        return this;
    }

    /// <summary>Sets an inclusive UTC upper bound.</summary><param name="until">The last permitted instant.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder SetUntil(Instant until)
    {
        TimeValidation.RequireNotNull(until, "until");

        this.until = until;
        localUntil = null;
        count = 0;
        return this;
    }

    /// <summary>Sets an inclusive floating-local upper bound.</summary><param name="until">The last permitted local date-time.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder SetLocalUntil(LocalDateTime until)
    {
        TimeValidation.RequireNotNull(until, "until");

        localUntil = until;
        this.until = null;
        count = 0;
        return this;
    }

    /// <summary>Sets the weekday that begins recurrence weeks.</summary><param name="weekStart">Monday as 1 through Sunday as 7.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder SetWeekStart(int weekStart)
    {
        TimeValidation.RequireIsoDayOfWeek(weekStart, "weekStart");

        this.weekStart = weekStart;
        return this;
    }

    /// <summary>Adds a BYSECOND filter.</summary><param name="second">A second from 0 through 59.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddBySecond(int second)
    {
        RequireRange(second, 0, 59, true, "second");
        bySecond = AppendUnique(bySecond, second);
        return this;
    }

    /// <summary>Adds a BYMINUTE filter.</summary><param name="minute">A minute from 0 through 59.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByMinute(int minute)
    {
        RequireRange(minute, 0, 59, true, "minute");
        byMinute = AppendUnique(byMinute, minute);
        return this;
    }

    /// <summary>Adds a BYHOUR filter.</summary><param name="hour">An hour from 0 through 23.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByHour(int hour)
    {
        RequireRange(hour, 0, 23, true, "hour");
        byHour = AppendUnique(byHour, hour);
        return this;
    }

    /// <summary>Adds a BYDAY weekday filter.</summary><param name="day">The weekday and optional ordinal to include.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByDay(RecurrenceDay day)
    {
        TimeValidation.RequireNotNull(day, "day");

        byDay = AppendUniqueDay(byDay, day);
        return this;
    }

    /// <summary>Adds a BYMONTHDAY filter.</summary><param name="monthDay">A nonzero day position from -31 through 31; negative values count backward from month end.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByMonthDay(int monthDay)
    {
        RequireRange(monthDay, -31, 31, false, "monthDay");
        byMonthDay = AppendUnique(byMonthDay, monthDay);
        return this;
    }

    /// <summary>Adds a BYYEARDAY filter.</summary><param name="yearDay">A nonzero day position from -366 through 366.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByYearDay(int yearDay)
    {
        RequireRange(yearDay, -366, 366, false, "yearDay");
        byYearDay = AppendUnique(byYearDay, yearDay);
        return this;
    }

    /// <summary>Adds a BYWEEKNO filter.</summary><param name="weekNumber">A nonzero week position from -53 through 53.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByWeekNo(int weekNumber)
    {
        RequireRange(weekNumber, -53, 53, false, "weekNumber");
        byWeekNo = AppendUnique(byWeekNo, weekNumber);
        return this;
    }

    /// <summary>Adds a BYMONTH filter.</summary><param name="month">A month from 1 through 12.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddByMonth(int month)
    {
        RequireRange(month, 1, 12, true, "month");
        byMonth = AppendUnique(byMonth, month);
        return this;
    }

    /// <summary>Adds a BYSETPOS filter.</summary><param name="position">A nonzero candidate-set position from -366 through 366.</param><returns>This builder for chaining.</returns>
    public RecurrenceBuilder AddBySetPos(int position)
    {
        RequireRange(position, -366, 366, false, "position");
        bySetPos = AppendUnique(bySetPos, position);
        return this;
    }

    /// <summary>Creates a recurrence rule from the current settings.</summary><returns>An independent validated rule.</returns>
    public RecurrenceRule Build()
    {
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
            throw new InvalidOperationException("BYSETPOS requires at least one other BY filter.");
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

    static void RequireFrequency(RecurrenceFrequency frequency)
    {
        int value = (int)frequency;
        if (value < (int)RecurrenceFrequency.Secondly || value > (int)RecurrenceFrequency.Yearly)
        {
            throw new ArgumentOutOfRangeException("frequency", "Recurrence frequency is not supported.");
        }
    }

    static void RequireRange(int value, int minimum, int maximum, bool allowZero, string parameterName)
    {
        if (value < minimum || value > maximum || (!allowZero && value == 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Recurrence filter value is outside the supported range.");
        }
    }

    static int[] AppendUnique(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return values;
            }

            index = index + 1;
        }

        int[] result = new int[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }

    static RecurrenceDay[] AppendUniqueDay(RecurrenceDay[] values, RecurrenceDay value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].EqualsValue(value))
            {
                return values;
            }

            index = index + 1;
        }

        RecurrenceDay[] result = new RecurrenceDay[values.Length + 1];
        Array.Copy(values, result, values.Length);
        result[values.Length] = value;
        return result;
    }
}
