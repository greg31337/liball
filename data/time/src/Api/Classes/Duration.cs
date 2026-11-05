using System;
using System.Globalization;

namespace time;

/// <summary>Represents an exact elapsed amount measured in 100-nanosecond ticks.</summary>
/// <remarks>A duration does not contain calendar units such as months or daylight-saving days. One duration day is always exactly 24 hours.</remarks>
public class Duration : IDuration
{
    long ticks;

    internal Duration(long ticks)
    {
        this.ticks = ticks;
    }

    /// <summary>Creates a duration with no elapsed time.</summary>
    /// <returns>A zero-tick duration.</returns>
    internal static Duration Zero()
    {
        return new Duration(0L);
    }

    /// <summary>Creates a duration from 100-nanosecond ticks.</summary>
    /// <param name="ticks">The signed tick count.</param>
    /// <returns>An exact duration with the supplied tick count.</returns>
    internal static Duration FromTicks(long ticks)
    {
        return new Duration(ticks);
    }

    /// <summary>Creates a duration from whole microseconds.</summary>
    /// <param name="microseconds">The signed microsecond count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromMicroseconds(long microseconds)
    {
        return new Duration(microseconds * TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Creates a duration from whole milliseconds.</summary>
    /// <param name="milliseconds">The signed millisecond count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromMilliseconds(long milliseconds)
    {
        return new Duration(milliseconds * TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Creates a duration from whole seconds.</summary>
    /// <param name="seconds">The signed second count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromSeconds(long seconds)
    {
        return new Duration(seconds * TimeConstants.TicksPerSecond);
    }

    /// <summary>Creates a duration from whole 60-second minutes.</summary>
    /// <param name="minutes">The signed minute count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromMinutes(long minutes)
    {
        return new Duration(minutes * TimeConstants.TicksPerMinute);
    }

    /// <summary>Creates a duration from whole 60-minute hours.</summary>
    /// <param name="hours">The signed hour count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromHours(long hours)
    {
        return new Duration(hours * TimeConstants.TicksPerHour);
    }

    /// <summary>Creates a duration from exact 24-hour days.</summary>
    /// <param name="days">The signed day count.</param>
    /// <returns>The equivalent exact duration.</returns>
    /// <exception cref="OverflowException">The value cannot be represented in ticks.</exception>
    internal static Duration FromDays(long days)
    {
        return new Duration(days * TimeConstants.TicksPerDay);
    }

    /// <summary>Gets the signed number of 100-nanosecond ticks.</summary>
    /// <returns>The exact tick count.</returns>
    public long GetTicks()
    {
        return ticks;
    }

    /// <summary>Gets the total number of ticks as a floating-point value.</summary>
    /// <returns>The signed total tick count.</returns>
    public double GetTotalTicks()
    {
        return ticks;
    }

    /// <summary>Gets the total number of microseconds, including any fractional microsecond.</summary>
    /// <returns>The signed total microseconds.</returns>
    public double GetTotalMicroseconds()
    {
        return ticks / (double)TimeConstants.TicksPerMicrosecond;
    }

    /// <summary>Gets the total number of milliseconds, including any fractional millisecond.</summary>
    /// <returns>The signed total milliseconds.</returns>
    public double GetTotalMilliseconds()
    {
        return ticks / (double)TimeConstants.TicksPerMillisecond;
    }

    /// <summary>Gets the total number of seconds, including any fractional second.</summary>
    /// <returns>The signed total seconds.</returns>
    public double GetTotalSeconds()
    {
        return ticks / (double)TimeConstants.TicksPerSecond;
    }

    /// <summary>Gets the total number of minutes, including any fractional minute.</summary>
    /// <returns>The signed total minutes.</returns>
    public double GetTotalMinutes()
    {
        return ticks / (double)TimeConstants.TicksPerMinute;
    }

    /// <summary>Gets the total number of hours, including any fractional hour.</summary>
    /// <returns>The signed total hours.</returns>
    public double GetTotalHours()
    {
        return ticks / (double)TimeConstants.TicksPerHour;
    }

    /// <summary>Gets the total number of exact 24-hour days, including any fractional day.</summary>
    /// <returns>The signed total days.</returns>
    public double GetTotalDays()
    {
        return ticks / (double)TimeConstants.TicksPerDay;
    }

    /// <summary>Adds two exact elapsed amounts.</summary>
    /// <param name="other">The duration to add.</param>
    /// <returns>A new duration containing the checked sum.</returns>
    /// <exception cref="OverflowException">The sum is outside the range of a signed 64-bit tick count.</exception>
    public Duration Add(Duration other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return new Duration(ticks + other.GetTicks());
    }

    /// <summary>Subtracts an exact elapsed amount.</summary>
    /// <param name="other">The duration to subtract.</param>
    /// <returns>A new duration containing the checked difference.</returns>
    /// <exception cref="OverflowException">The difference is outside the range of a signed 64-bit tick count.</exception>
    public Duration Subtract(Duration other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return new Duration(ticks - other.GetTicks());
    }

    /// <summary>Reverses the sign of this duration.</summary>
    /// <returns>A duration with the opposite sign.</returns>
    /// <exception cref="OverflowException">This duration contains the minimum signed tick value, which has no positive counterpart.</exception>
    public Duration Negate()
    {
        return new Duration(-ticks);
    }

    /// <summary>Gets the nonnegative magnitude of this duration.</summary>
    /// <returns>This duration when nonnegative; otherwise its negation.</returns>
    /// <exception cref="OverflowException">This duration contains the minimum signed tick value.</exception>
    public Duration Abs()
    {
        if (ticks < 0L)
        {
            return Negate();
        }

        return new Duration(ticks);
    }

    /// <summary>Orders this duration by its exact tick count.</summary>
    /// <param name="other">The duration to compare.</param>
    /// <returns>A negative value, zero, or a positive value when this duration is shorter than, equal to, or longer than the other value.</returns>
    public int CompareTo(Duration other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return TimeComparison.CompareLong(ticks, other.GetTicks());
    }

    /// <summary>Tests exact tick equality.</summary>
    /// <param name="other">The duration to compare, or null.</param>
    /// <returns><c>true</c> when both durations contain the same tick count; otherwise <c>false</c>.</returns>
    public bool EqualsValue(Duration? other)
    {
        if (other == null)
        {
            return false;
        }

        return ticks == other.GetTicks();
    }

    /// <summary>Selects the shorter of this duration and another duration.</summary>
    /// <param name="other">The duration to compare.</param>
    /// <returns>A duration containing the lesser tick count.</returns>
    public Duration Min(Duration other)
    {
        TimeValidation.RequireNotNull(other, "other");

        if (ticks <= other.GetTicks())
        {
            return new Duration(ticks);
        }

        return new Duration(other.GetTicks());
    }

    /// <summary>Selects the longer of this duration and another duration.</summary>
    /// <param name="other">The duration to compare.</param>
    /// <returns>A duration containing the greater tick count.</returns>
    public Duration Max(Duration other)
    {
        TimeValidation.RequireNotNull(other, "other");

        if (ticks >= other.GetTicks())
        {
            return new Duration(ticks);
        }

        return new Duration(other.GetTicks());
    }

    /// <summary>Rounds to the nearest tick.</summary>
    /// <returns>A duration with the same tick count.</returns>
    public Duration RoundToTicks()
    {
        return RoundToUnit(1L);
    }

    /// <summary>Rounds to the nearest microsecond, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of microseconds.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToMicroseconds()
    {
        return RoundToUnit(TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Rounds to the nearest millisecond, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of milliseconds.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToMilliseconds()
    {
        return RoundToUnit(TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Rounds to the nearest second, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of seconds.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToSeconds()
    {
        return RoundToUnit(TimeConstants.TicksPerSecond);
    }

    /// <summary>Rounds to the nearest minute, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of minutes.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToMinutes()
    {
        return RoundToUnit(TimeConstants.TicksPerMinute);
    }

    /// <summary>Rounds to the nearest hour, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of hours.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToHours()
    {
        return RoundToUnit(TimeConstants.TicksPerHour);
    }

    /// <summary>Rounds to the nearest exact 24-hour day, with midpoint values rounded away from zero.</summary>
    /// <returns>A duration containing a whole number of exact days.</returns>
    /// <exception cref="OverflowException">The rounded tick count is outside the signed 64-bit range.</exception>
    public Duration RoundToDays()
    {
        return RoundToUnit(TimeConstants.TicksPerDay);
    }

    /// <summary>Truncates to a whole number of ticks.</summary>
    /// <returns>A duration with the same tick count.</returns>
    public Duration TruncateToTicks()
    {
        return TruncateToUnit(1L);
    }

    /// <summary>Truncates toward zero to a whole number of microseconds.</summary>
    /// <returns>A duration containing only complete microseconds.</returns>
    public Duration TruncateToMicroseconds()
    {
        return TruncateToUnit(TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Truncates toward zero to a whole number of milliseconds.</summary>
    /// <returns>A duration containing only complete milliseconds.</returns>
    public Duration TruncateToMilliseconds()
    {
        return TruncateToUnit(TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Truncates toward zero to a whole number of seconds.</summary>
    /// <returns>A duration containing only complete seconds.</returns>
    public Duration TruncateToSeconds()
    {
        return TruncateToUnit(TimeConstants.TicksPerSecond);
    }

    /// <summary>Truncates toward zero to a whole number of minutes.</summary>
    /// <returns>A duration containing only complete minutes.</returns>
    public Duration TruncateToMinutes()
    {
        return TruncateToUnit(TimeConstants.TicksPerMinute);
    }

    /// <summary>Truncates toward zero to a whole number of hours.</summary>
    /// <returns>A duration containing only complete hours.</returns>
    public Duration TruncateToHours()
    {
        return TruncateToUnit(TimeConstants.TicksPerHour);
    }

    /// <summary>Truncates toward zero to a whole number of exact 24-hour days.</summary>
    /// <returns>A duration containing only complete exact days.</returns>
    public Duration TruncateToDays()
    {
        return TruncateToUnit(TimeConstants.TicksPerDay);
    }

    /// <summary>Converts this value to the built-in exact duration type.</summary>
    /// <returns>A <see cref="TimeSpan"/> with the same tick count.</returns>
    public TimeSpan ToTimeSpan()
    {
        return new TimeSpan(ticks);
    }

    /// <summary>Creates a duration from a built-in exact duration.</summary>
    /// <param name="timeSpan">The value whose ticks are copied.</param>
    /// <returns>A duration with the same tick count.</returns>
    internal static Duration FromTimeSpan(TimeSpan timeSpan)
    {
        return new Duration(timeSpan.Ticks);
    }

    /// <summary>Formats this duration in invariant ISO-8601 duration form.</summary>
    /// <returns>Text such as <c>PT1H30M</c>; fractional seconds preserve tick precision.</returns>
    public string ToIsoString()
    {
        bool negative = ticks < 0L;
        ulong remaining = GetAbsoluteTicks();

        if (remaining == 0UL)
        {
            return "PT0S";
        }

        ulong days = remaining / (ulong)TimeConstants.TicksPerDay;
        remaining = remaining % (ulong)TimeConstants.TicksPerDay;
        ulong hours = remaining / (ulong)TimeConstants.TicksPerHour;
        remaining = remaining % (ulong)TimeConstants.TicksPerHour;
        ulong minutes = remaining / (ulong)TimeConstants.TicksPerMinute;
        remaining = remaining % (ulong)TimeConstants.TicksPerMinute;
        ulong seconds = remaining / (ulong)TimeConstants.TicksPerSecond;
        ulong fractionalTicks = remaining % (ulong)TimeConstants.TicksPerSecond;

        string result = "";
        if (negative)
        {
            result = "-";
        }

        result = result + "P";
        if (days > 0UL)
        {
            result = result + days.ToString(CultureInfo.InvariantCulture) + "D";
        }

        if (hours > 0UL || minutes > 0UL || seconds > 0UL || fractionalTicks > 0UL || days == 0UL)
        {
            result = result + "T";
            if (hours > 0UL)
            {
                result = result + hours.ToString(CultureInfo.InvariantCulture) + "H";
            }

            if (minutes > 0UL)
            {
                result = result + minutes.ToString(CultureInfo.InvariantCulture) + "M";
            }

            if (seconds > 0UL || fractionalTicks > 0UL || result.EndsWith("T", StringComparison.Ordinal))
            {
                result = result + seconds.ToString(CultureInfo.InvariantCulture);
                if (fractionalTicks > 0UL)
                {
                    string fraction = fractionalTicks.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
                    result = result + "." + fraction;
                }

                result = result + "S";
            }
        }

        return result;
    }

    /// <summary>Parses an ISO-8601 fixed duration.</summary>
    /// <remarks>Years and months are rejected because they are calendar-dependent. Days are interpreted as exact 24-hour units.</remarks>
    /// <param name="text">ISO duration text, optionally beginning with a sign.</param>
    /// <returns>The parsed exact duration.</returns>
    /// <exception cref="FormatException">The text is empty, malformed, uses unsupported units, or exceeds tick precision.</exception>
    /// <exception cref="OverflowException">The parsed duration is outside the supported tick range.</exception>
    internal static Duration Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length == 0)
        {
            throw new FormatException("Duration text must not be empty.");
        }

        int index = 0;
        int sign = 1;
        if (text[index] == '-')
        {
            sign = -1;
            index = index + 1;
        }
        else if (text[index] == '+')
        {
            index = index + 1;
        }

        if (index >= text.Length || text[index] != 'P')
        {
            throw new FormatException("Duration text must start with P.");
        }

        index = index + 1;
        bool inTime = false;
        bool hasComponent = false;
        int lastRank = 0;
        decimal totalTicks = 0m;

        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (inTime || index == text.Length - 1)
                {
                    throw new FormatException("Duration time marker is invalid.");
                }

                inTime = true;
                index = index + 1;
            }
            else
            {
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
                    throw new FormatException("Duration component is missing a unit.");
                }

                char unit = text[index];
                index = index + 1;

                int rank = GetDurationUnitRank(unit, inTime);
                if (rank <= lastRank)
                {
                    throw new FormatException("Duration components must be in descending unit order.");
                }

                if (hasFraction && unit != 'S')
                {
                    throw new FormatException("Only seconds may contain a fractional part.");
                }

                totalTicks = totalTicks + GetDurationComponentTicks(number, fractionalTicks, unit, inTime, hasComponent, index == text.Length);
                hasComponent = true;
                lastRank = rank;
            }
        }

        if (!hasComponent)
        {
            throw new FormatException("Duration text must contain at least one component.");
        }

        return DurationFromParsedTicks(totalTicks, sign);
    }

    Duration RoundToUnit(long unitTicks)
    {
        long quotient = ticks / unitTicks;
        long remainder = ticks % unitTicks;
        long absoluteRemainder = remainder;
        if (absoluteRemainder < 0L)
        {
            absoluteRemainder = -absoluteRemainder;
        }

        if (absoluteRemainder >= unitTicks / 2L && remainder != 0L)
        {
            if (remainder > 0L)
            {
                quotient = quotient + 1L;
            }
            else
            {
                quotient = quotient - 1L;
            }
        }

        return new Duration(quotient * unitTicks);
    }

    Duration TruncateToUnit(long unitTicks)
    {
        return new Duration(ticks / unitTicks * unitTicks);
    }

    ulong GetAbsoluteTicks()
    {
        if (ticks == long.MinValue)
        {
            return 9223372036854775808UL;
        }

        if (ticks < 0L)
        {
            return (ulong)(-ticks);
        }

        return (ulong)ticks;
    }

    static decimal ParseNumber(string text, int index)
    {
        if (index >= text.Length || text[index] < '0' || text[index] > '9')
        {
            throw new FormatException("Duration component must start with a digit.");
        }

        decimal number = 0m;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            number = number * 10m + (text[index] - '0');
            if (number > 9223372036854775808m)
            {
                throw new OverflowException("Duration component is too large.");
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

    static int GetDurationUnitRank(char unit, bool inTime)
    {
        if (unit == 'W' && !inTime)
        {
            return 1;
        }

        if (unit == 'D' && !inTime)
        {
            return 2;
        }

        if (unit == 'H' && inTime)
        {
            return 3;
        }

        if (unit == 'M' && inTime)
        {
            return 4;
        }

        if (unit == 'S' && inTime)
        {
            return 5;
        }

        if (unit == 'Y' || unit == 'M')
        {
            throw new FormatException("Calendar duration units are not supported by Duration.");
        }

        throw new FormatException("Duration unit is not supported.");
    }

    static decimal GetDurationComponentTicks(decimal number, decimal fractionalTicks, char unit, bool inTime, bool hasComponent, bool isLastComponent)
    {
        if (unit == 'W' && !inTime)
        {
            if (hasComponent || !isLastComponent)
            {
                throw new FormatException("Week durations cannot be mixed with other components.");
            }

            return number * TimeConstants.TicksPerWeek;
        }

        if (unit == 'D' && !inTime)
        {
            return number * TimeConstants.TicksPerDay;
        }

        if (unit == 'H' && inTime)
        {
            return number * TimeConstants.TicksPerHour;
        }

        if (unit == 'M' && inTime)
        {
            return number * TimeConstants.TicksPerMinute;
        }

        return number * TimeConstants.TicksPerSecond + fractionalTicks;
    }

    static Duration DurationFromParsedTicks(decimal totalTicks, int sign)
    {
        if (sign < 0)
        {
            if (totalTicks > 9223372036854775808m)
            {
                throw new OverflowException("Duration is outside the supported tick range.");
            }

            return new Duration((long)(-totalTicks));
        }

        if (totalTicks > long.MaxValue)
        {
            throw new OverflowException("Duration is outside the supported tick range.");
        }

        return new Duration((long)totalTicks);
    }
}
