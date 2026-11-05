using System;
using System.Globalization;
using System.Text;

namespace time;

/// <summary>Represents a field-based calendar amount containing date and time units.</summary>
/// <remarks>Unlike <see cref="Duration"/>, a period is not an exact elapsed length until applied to a local value. Fields are preserved independently and may have mixed signs.</remarks>
public class Period : IPeriod
{
    long years;
    long months;
    long weeks;
    long days;
    long hours;
    long minutes;
    long seconds;
    long milliseconds;
    long microseconds;
    long ticks;

    internal Period(long years, long months, long weeks, long days, long hours, long minutes, long seconds, long milliseconds, long microseconds, long ticks)
    {
        this.years = years;
        this.months = months;
        this.weeks = weeks;
        this.days = days;
        this.hours = hours;
        this.minutes = minutes;
        this.seconds = seconds;
        this.milliseconds = milliseconds;
        this.microseconds = microseconds;
        this.ticks = ticks;
    }

    /// <summary>Creates a period whose fields are all zero.</summary><returns>An empty calendar amount.</returns>
    internal static Period Zero()
    {
        return Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period from independent calendar and clock fields.</summary>
    /// <param name="years">Years.</param><param name="months">Months.</param><param name="weeks">Weeks.</param><param name="days">Days.</param><param name="hours">Hours.</param>
    /// <param name="minutes">Minutes.</param><param name="seconds">Seconds.</param><param name="milliseconds">Milliseconds.</param><param name="microseconds">Microseconds.</param><param name="ticks">Additional 100-nanosecond ticks.</param>
    /// <returns>A period preserving every supplied field without normalization.</returns>
    internal static Period Create(long years, long months, long weeks, long days, long hours, long minutes, long seconds, long milliseconds, long microseconds, long ticks)
    {
        return new Period(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, ticks);
    }

    /// <summary>Creates a period containing only years.</summary><param name="years">The signed year count.</param><returns>The period.</returns>
    internal static Period FromYears(long years)
    {
        return Create(years, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only months.</summary><param name="months">The signed month count.</param><returns>The period.</returns>
    internal static Period FromMonths(long months)
    {
        return Create(0L, months, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only seven-day weeks.</summary><param name="weeks">The signed week count.</param><returns>The period.</returns>
    internal static Period FromWeeks(long weeks)
    {
        return Create(0L, 0L, weeks, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only calendar days.</summary><param name="days">The signed day count.</param><returns>The period.</returns>
    internal static Period FromDays(long days)
    {
        return Create(0L, 0L, 0L, days, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only hours.</summary><param name="hours">The signed hour count.</param><returns>The period.</returns>
    internal static Period FromHours(long hours)
    {
        return Create(0L, 0L, 0L, 0L, hours, 0L, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only minutes.</summary><param name="minutes">The signed minute count.</param><returns>The period.</returns>
    internal static Period FromMinutes(long minutes)
    {
        return Create(0L, 0L, 0L, 0L, 0L, minutes, 0L, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only seconds.</summary><param name="seconds">The signed second count.</param><returns>The period.</returns>
    internal static Period FromSeconds(long seconds)
    {
        return Create(0L, 0L, 0L, 0L, 0L, 0L, seconds, 0L, 0L, 0L);
    }

    /// <summary>Creates a period containing only milliseconds.</summary><param name="milliseconds">The signed millisecond count.</param><returns>The period.</returns>
    internal static Period FromMilliseconds(long milliseconds)
    {
        return Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, milliseconds, 0L, 0L);
    }

    /// <summary>Creates a period containing only microseconds.</summary><param name="microseconds">The signed microsecond count.</param><returns>The period.</returns>
    internal static Period FromMicroseconds(long microseconds)
    {
        return Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, microseconds, 0L);
    }

    /// <summary>Creates a period containing only 100-nanosecond ticks.</summary><param name="ticks">The signed tick count.</param><returns>The period.</returns>
    internal static Period FromTicks(long ticks)
    {
        return Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, ticks);
    }

    /// <summary>Gets the unnormalized years field.</summary><returns>The signed years.</returns>
    public long GetYears()
    {
        return years;
    }

    /// <summary>Gets the unnormalized months field.</summary><returns>The signed months.</returns>
    public long GetMonths()
    {
        return months;
    }

    /// <summary>Gets the unnormalized weeks field.</summary><returns>The signed weeks.</returns>
    public long GetWeeks()
    {
        return weeks;
    }

    /// <summary>Gets the unnormalized days field.</summary><returns>The signed days.</returns>
    public long GetDays()
    {
        return days;
    }

    /// <summary>Gets the unnormalized hours field.</summary><returns>The signed hours.</returns>
    public long GetHours()
    {
        return hours;
    }

    /// <summary>Gets the unnormalized minutes field.</summary><returns>The signed minutes.</returns>
    public long GetMinutes()
    {
        return minutes;
    }

    /// <summary>Gets the unnormalized seconds field.</summary><returns>The signed seconds.</returns>
    public long GetSeconds()
    {
        return seconds;
    }

    /// <summary>Gets the unnormalized milliseconds field.</summary><returns>The signed milliseconds.</returns>
    public long GetMilliseconds()
    {
        return milliseconds;
    }

    /// <summary>Gets the unnormalized microseconds field.</summary><returns>The signed microseconds.</returns>
    public long GetMicroseconds()
    {
        return microseconds;
    }

    /// <summary>Gets the unnormalized extra-ticks field.</summary><returns>The signed 100-nanosecond ticks.</returns>
    public long GetTicks()
    {
        return ticks;
    }

    /// <summary>Moves the weeks field into days using seven days per week.</summary><returns>A period with zero weeks and the checked combined day count.</returns><exception cref="OverflowException">The combined days cannot fit in a signed 64-bit value.</exception>
    public Period NormalizeWeeksToDays()
    {
        long normalizedDays = days + weeks * TimeConstants.DaysPerWeek;
        return Create(years, months, 0L, normalizedDays, hours, minutes, seconds, milliseconds, microseconds, ticks);
    }

    /// <summary>Adds corresponding fields without carrying between units.</summary><param name="other">The period to add.</param><returns>A period containing the checked field-by-field sums.</returns>
    public Period Add(Period other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return Create(
            years + other.GetYears(),
            months + other.GetMonths(),
            weeks + other.GetWeeks(),
            days + other.GetDays(),
            hours + other.GetHours(),
            minutes + other.GetMinutes(),
            seconds + other.GetSeconds(),
            milliseconds + other.GetMilliseconds(),
            microseconds + other.GetMicroseconds(),
            ticks + other.GetTicks());
    }

    /// <summary>Subtracts corresponding fields without borrowing between units.</summary><param name="other">The period to subtract.</param><returns>A period containing the checked field-by-field differences.</returns>
    public Period Subtract(Period other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return Create(
            years - other.GetYears(),
            months - other.GetMonths(),
            weeks - other.GetWeeks(),
            days - other.GetDays(),
            hours - other.GetHours(),
            minutes - other.GetMinutes(),
            seconds - other.GetSeconds(),
            milliseconds - other.GetMilliseconds(),
            microseconds - other.GetMicroseconds(),
            ticks - other.GetTicks());
    }

    /// <summary>Reverses the sign of every field.</summary><returns>A period with each field negated.</returns><exception cref="OverflowException">Any field contains the minimum signed 64-bit value.</exception>
    public Period Negate()
    {
        return Create(
            -years,
            -months,
            -weeks,
            -days,
            -hours,
            -minutes,
            -seconds,
            -milliseconds,
            -microseconds,
            -ticks);
    }

    /// <summary>Compares two periods whose fields have an anchor-independent exact length.</summary>
    /// <remarks>Only the hour, minute, second, millisecond, microsecond, and tick fields are exactly comparable. A nonzero year, month, week, or day field requires a calendar date or time zone to determine elapsed length.</remarks>
    /// <param name="other">The period to compare.</param>
    /// <returns>A negative value, zero, or a positive value when this exact clock-field amount is shorter than, equal to, or longer than the other value.</returns>
    /// <exception cref="InvalidOperationException">Either period contains a nonzero calendar field.</exception>
    /// <exception cref="OverflowException">Either exact clock-field amount cannot be represented as a signed 64-bit tick count.</exception>
    public int CompareTo(Period other)
    {
        TimeValidation.RequireNotNull(other, "other");
        RequireExactlyComparable(this);
        RequireExactlyComparable(other);

        long thisTicks = GetComparableTicks();
        long otherTicks = other.GetComparableTicks();
        return TimeComparison.CompareLong(thisTicks, otherTicks);
    }

    /// <summary>Tests field-by-field equality.</summary><param name="other">The period to compare, or null.</param><returns><c>true</c> only when every stored field matches.</returns>
    public bool EqualsValue(Period? other)
    {
        if (other == null)
        {
            return false;
        }

        return years == other.GetYears()
            && months == other.GetMonths()
            && weeks == other.GetWeeks()
            && days == other.GetDays()
            && hours == other.GetHours()
            && minutes == other.GetMinutes()
            && seconds == other.GetSeconds()
            && milliseconds == other.GetMilliseconds()
            && microseconds == other.GetMicroseconds()
            && ticks == other.GetTicks();
    }

    /// <summary>Formats this period in invariant ISO-style form.</summary><remarks>When all nonzero fields are negative, one leading minus sign is used. Mixed signs are written on their individual components and are accepted again by this library.</remarks><returns>Period text preserving supported fields and tick precision.</returns>
    public string ToIsoString()
    {
        if (IsZero())
        {
            return "P0D";
        }

        bool useLeadingNegative = AllNonZeroFieldsAreNegative();
        StringBuilder builder = new StringBuilder();
        if (useLeadingNegative)
        {
            builder.Append("-");
        }

        builder.Append("P");
        AppendComponent(builder, years, "Y", useLeadingNegative);
        AppendComponent(builder, months, "M", useLeadingNegative);
        AppendComponent(builder, weeks, "W", useLeadingNegative);
        AppendComponent(builder, days, "D", useLeadingNegative);

        if (HasTimeFields())
        {
            builder.Append("T");
            AppendComponent(builder, hours, "H", useLeadingNegative);
            AppendComponent(builder, minutes, "M", useLeadingNegative);
            AppendSecondComponent(builder, useLeadingNegative);
        }

        return builder.ToString();
    }

    /// <summary>Parses an invariant ISO-8601 calendar period.</summary><param name="text">The period text, optionally beginning with a sign.</param><returns>The parsed field-based period.</returns>
    /// <exception cref="FormatException">The text is malformed, repeats or reorders units, or uses unsupported precision.</exception>
    internal static Period Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length == 0)
        {
            throw new FormatException("Period text must not be empty.");
        }

        int index = 0;
        int leadingSign = 1;
        if (text[index] == '-')
        {
            leadingSign = -1;
            index = index + 1;
        }
        else if (text[index] == '+')
        {
            index = index + 1;
        }

        if (index >= text.Length || text[index] != 'P')
        {
            throw new FormatException("Period text must start with P.");
        }

        index = index + 1;
        bool inTime = false;
        bool hasComponent = false;
        int lastRank = 0;
        Period period = Period.Zero();

        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (inTime || index == text.Length - 1)
                {
                    throw new FormatException("Period time marker is invalid.");
                }

                inTime = true;
                index = index + 1;
            }
            else
            {
                int componentSign = leadingSign;
                if (text[index] == '-')
                {
                    componentSign = -1;
                    index = index + 1;
                }
                else if (text[index] == '+')
                {
                    componentSign = 1;
                    index = index + 1;
                }

                decimal number = ParseNumber(text, index);
                index = MovePastDigits(text, index);

                bool hasFraction = false;
                decimal fractionalTicks = 0m;
                if (index < text.Length && text[index] == '.')
                {
                    hasFraction = true;
                    index = index + 1;
                    fractionalTicks = ParseFractionalSecondTicks(text, index);
                    index = MovePastDigits(text, index);
                }

                if (index >= text.Length)
                {
                    throw new FormatException("Period component is missing a unit.");
                }

                char unit = text[index];
                index = index + 1;

                int rank = GetPeriodUnitRank(unit, inTime);
                if (rank <= lastRank)
                {
                    throw new FormatException("Period components must be in descending unit order.");
                }

                if (hasFraction && (unit != 'S' || !inTime))
                {
                    throw new FormatException("Only time seconds may contain a fractional part.");
                }

                period = period.Add(CreatePeriodComponent(number, fractionalTicks, unit, inTime, componentSign));
                hasComponent = true;
                lastRank = rank;
            }
        }

        if (!hasComponent)
        {
            throw new FormatException("Period text must contain at least one component.");
        }

        return period;
    }

    long GetComparableTicks()
    {
        return hours * TimeConstants.TicksPerHour
            + minutes * TimeConstants.TicksPerMinute
            + seconds * TimeConstants.TicksPerSecond
            + milliseconds * TimeConstants.TicksPerMillisecond
            + microseconds * TimeConstants.TicksPerMicrosecond
            + ticks;
    }

    static void RequireExactlyComparable(Period period)
    {
        if (period.GetYears() != 0L
            || period.GetMonths() != 0L
            || period.GetWeeks() != 0L
            || period.GetDays() != 0L)
        {
            throw new InvalidOperationException("Periods containing calendar fields cannot be compared without a calendar anchor and time zone.");
        }
    }

    bool IsZero()
    {
        return years == 0L
            && months == 0L
            && weeks == 0L
            && days == 0L
            && hours == 0L
            && minutes == 0L
            && seconds == 0L
            && milliseconds == 0L
            && microseconds == 0L
            && ticks == 0L;
    }

    bool HasTimeFields()
    {
        return hours != 0L
            || minutes != 0L
            || seconds != 0L
            || milliseconds != 0L
            || microseconds != 0L
            || ticks != 0L;
    }

    bool AllNonZeroFieldsAreNegative()
    {
        return IsNegativeOrZero(years)
            && IsNegativeOrZero(months)
            && IsNegativeOrZero(weeks)
            && IsNegativeOrZero(days)
            && IsNegativeOrZero(hours)
            && IsNegativeOrZero(minutes)
            && IsNegativeOrZero(seconds)
            && IsNegativeOrZero(milliseconds)
            && IsNegativeOrZero(microseconds)
            && IsNegativeOrZero(ticks);
    }

    static bool IsNegativeOrZero(long value)
    {
        return value <= 0L;
    }

    static void AppendComponent(StringBuilder builder, long value, string suffix, bool useAbsoluteValue)
    {
        if (value == 0L)
        {
            return;
        }

        builder.Append(GetComponentText(value, useAbsoluteValue));
        builder.Append(suffix);
    }

    void AppendSecondComponent(StringBuilder builder, bool useAbsoluteValue)
    {
        long totalSecondTicks = seconds * TimeConstants.TicksPerSecond
            + milliseconds * TimeConstants.TicksPerMillisecond
            + microseconds * TimeConstants.TicksPerMicrosecond
            + ticks;

        if (totalSecondTicks == 0L)
        {
            return;
        }

        bool negative = totalSecondTicks < 0L;
        ulong remaining = GetAbsoluteLong(totalSecondTicks);
        ulong wholeSeconds = remaining / (ulong)TimeConstants.TicksPerSecond;
        ulong fractionalTicks = remaining % (ulong)TimeConstants.TicksPerSecond;

        if (negative && !useAbsoluteValue)
        {
            builder.Append("-");
        }

        builder.Append(wholeSeconds.ToString(CultureInfo.InvariantCulture));
        if (fractionalTicks > 0UL)
        {
            string fraction = fractionalTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
            builder.Append(".");
            builder.Append(fraction);
        }

        builder.Append("S");
    }

    static string GetComponentText(long value, bool useAbsoluteValue)
    {
        if (useAbsoluteValue)
        {
            return GetAbsoluteLong(value).ToString(CultureInfo.InvariantCulture);
        }

        if (value < 0L)
        {
            return "-" + GetAbsoluteLong(value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    static ulong GetAbsoluteLong(long value)
    {
        if (value == long.MinValue)
        {
            return 9223372036854775808UL;
        }

        if (value < 0L)
        {
            return (ulong)(-value);
        }

        return (ulong)value;
    }

    static decimal ParseNumber(string text, int index)
    {
        if (index >= text.Length || text[index] < '0' || text[index] > '9')
        {
            throw new FormatException("Period component must start with a digit.");
        }

        decimal number = 0m;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            number = number * 10m + (text[index] - '0');
            if (number > 9223372036854775808m)
            {
                throw new OverflowException("Period component is too large.");
            }

            index = index + 1;
        }

        return number;
    }

    static int MovePastDigits(string text, int index)
    {
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            index = index + 1;
        }

        return index;
    }

    static decimal ParseFractionalSecondTicks(string text, int index)
    {
        if (index >= text.Length || text[index] < '0' || text[index] > '9')
        {
            throw new FormatException("Fractional seconds must contain at least one digit.");
        }

        decimal ticksValue = 0m;
        int digits = 0;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            if (digits == 7)
            {
                throw new FormatException("Fractional seconds support at most seven digits.");
            }

            ticksValue = ticksValue * 10m + (text[index] - '0');
            digits = digits + 1;
            index = index + 1;
        }

        while (digits < 7)
        {
            ticksValue = ticksValue * 10m;
            digits = digits + 1;
        }

        return ticksValue;
    }

    static int GetPeriodUnitRank(char unit, bool inTime)
    {
        if (unit == 'Y' && !inTime)
        {
            return 1;
        }

        if (unit == 'M' && !inTime)
        {
            return 2;
        }

        if (unit == 'W' && !inTime)
        {
            return 3;
        }

        if (unit == 'D' && !inTime)
        {
            return 4;
        }

        if (unit == 'H' && inTime)
        {
            return 5;
        }

        if (unit == 'M' && inTime)
        {
            return 6;
        }

        if (unit == 'S' && inTime)
        {
            return 7;
        }

        throw new FormatException("Period unit is not supported.");
    }

    static Period CreatePeriodComponent(decimal number, decimal fractionalTicks, char unit, bool inTime, int sign)
    {
        if (unit == 'Y' && !inTime)
        {
            return Period.FromYears(ApplySignToLong(number, sign));
        }

        if (unit == 'M' && !inTime)
        {
            return Period.FromMonths(ApplySignToLong(number, sign));
        }

        if (unit == 'W' && !inTime)
        {
            return Period.FromWeeks(ApplySignToLong(number, sign));
        }

        if (unit == 'D' && !inTime)
        {
            return Period.FromDays(ApplySignToLong(number, sign));
        }

        if (unit == 'H' && inTime)
        {
            return Period.FromHours(ApplySignToLong(number, sign));
        }

        if (unit == 'M' && inTime)
        {
            return Period.FromMinutes(ApplySignToLong(number, sign));
        }

        long signedFractionalTicks = ApplySignToLong(fractionalTicks, sign);
        long componentSeconds = ApplySignToLong(number, sign);
        long componentMilliseconds = signedFractionalTicks / TimeConstants.TicksPerMillisecond;
        long remainingTicks = signedFractionalTicks % TimeConstants.TicksPerMillisecond;
        long componentMicroseconds = remainingTicks / TimeConstants.TicksPerMicrosecond;
        long componentTicks = remainingTicks % TimeConstants.TicksPerMicrosecond;

        return Period.Create(0L, 0L, 0L, 0L, 0L, 0L, componentSeconds, componentMilliseconds, componentMicroseconds, componentTicks);
    }

    static long ApplySignToLong(decimal number, int sign)
    {
        if (sign < 0)
        {
            return (long)(-number);
        }

        if (number > long.MaxValue)
        {
            throw new OverflowException("Period component is outside the supported range.");
        }

        return (long)number;
    }
}
