using System;

namespace time;

/// <summary>Represents one exact point on the UTC timeline with 100-nanosecond precision.</summary>
/// <remarks>The supported range matches .NET UTC date-time ticks, from year 1 through year 9999.</remarks>
public class Instant : IInstant
{
    long utcTicks;

    internal Instant(long utcTicks)
    {
        RequireUtcTicks(utcTicks, "utcTicks");

        this.utcTicks = utcTicks;
    }

    /// <summary>Gets the Unix epoch at 1970-01-01T00:00:00Z.</summary>
    /// <returns>The Unix epoch instant.</returns>
    internal static Instant UnixEpoch()
    {
        return new Instant(TimeConstants.UnixEpochUtcTicks);
    }

    /// <summary>Reads the current system UTC time.</summary>
    /// <returns>The current instant at the time of the call.</returns>
    internal static Instant Now()
    {
        return FromDateTimeOffset(DateTimeOffset.UtcNow);
    }

    /// <summary>Creates an instant from .NET ticks since 0001-01-01T00:00:00Z.</summary>
    /// <param name="utcTicks">A tick count in the supported .NET UTC range.</param>
    /// <returns>The corresponding instant.</returns>
    internal static Instant FromUtcTicks(long utcTicks)
    {
        return new Instant(utcTicks);
    }

    /// <summary>Creates an instant from whole seconds relative to the Unix epoch.</summary>
    /// <param name="unixSeconds">Signed seconds before or after the epoch.</param>
    /// <returns>The corresponding instant.</returns>
    internal static Instant FromUnixSeconds(long unixSeconds)
    {
        return FromUnixUnits(unixSeconds, TimeConstants.TicksPerSecond);
    }

    /// <summary>Creates an instant from whole milliseconds relative to the Unix epoch.</summary>
    /// <param name="unixMilliseconds">Signed milliseconds before or after the epoch.</param>
    /// <returns>The corresponding instant.</returns>
    internal static Instant FromUnixMilliseconds(long unixMilliseconds)
    {
        return FromUnixUnits(unixMilliseconds, TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Creates an instant from whole microseconds relative to the Unix epoch.</summary>
    /// <param name="unixMicroseconds">Signed microseconds before or after the epoch.</param>
    /// <returns>The corresponding instant.</returns>
    internal static Instant FromUnixMicroseconds(long unixMicroseconds)
    {
        return FromUnixUnits(unixMicroseconds, TimeConstants.TicksPerMicrosecond);
    }

    static Instant FromUnixUnits(long units, long ticksPerUnit)
    {
        decimal ticks = TimeConstants.UnixEpochUtcTicks + (decimal)units * ticksPerUnit;
        if (ticks < long.MinValue || ticks > long.MaxValue)
        {
            throw new OverflowException("Unix time conversion exceeds the signed 64-bit tick range.");
        }

        return FromUtcTicks((long)ticks);
    }

    /// <summary>Creates an instant from a built-in offset date-time.</summary>
    /// <param name="dateTimeOffset">The value to convert to UTC.</param>
    /// <returns>The same point on the timeline.</returns>
    internal static Instant FromDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        return FromUtcTicks(dateTimeOffset.UtcDateTime.Ticks);
    }

    /// <summary>Creates an instant from a built-in UTC date-time.</summary>
    /// <param name="dateTime">A value whose kind must be UTC.</param>
    /// <returns>The represented instant.</returns>
    /// <exception cref="ArgumentException">The date-time kind is not UTC.</exception>
    internal static Instant FromUtcDateTime(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime kind must be UTC.", "dateTime");
        }

        return FromUtcTicks(dateTime.Ticks);
    }

    /// <summary>Parses an ISO instant ending in <c>Z</c>.</summary>
    /// <param name="text">Invariant text such as <c>2024-02-29T12:34:56Z</c>.</param>
    /// <returns>The parsed instant.</returns>
    /// <exception cref="FormatException">The input is not a supported UTC ISO date-time.</exception>
    internal static Instant Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        int offsetIndex = FindOffsetStart(text);
        if (offsetIndex < 0 || offsetIndex < 19)
        {
            throw new FormatException("Instant text must use ISO date-time text with a UTC offset.");
        }

        string localText = text.Substring(0, offsetIndex);
        LocalDateTime localDateTime = LocalDateTime.Parse(localText);
        int offsetSeconds = ParseOffsetSeconds(text, offsetIndex);
        long localTicks = (long)localDateTime.GetDate().GetDayNumber() * TimeConstants.TicksPerDay + localDateTime.GetTime().GetTicksSinceMidnight();
        decimal utcTicks = localTicks - (long)offsetSeconds * TimeConstants.TicksPerSecond;

        if (utcTicks < TimeConstants.MinUtcTicks || utcTicks > TimeConstants.MaxUtcTicks)
        {
            throw new ArgumentOutOfRangeException("text", "Instant text is outside the supported UTC range.");
        }

        return FromUtcTicks((long)utcTicks);
    }

    /// <summary>Gets .NET UTC ticks since 0001-01-01T00:00:00Z.</summary>
    /// <returns>The UTC tick count.</returns>
    public long GetUtcTicks()
    {
        return utcTicks;
    }

    /// <summary>Gets whole Unix seconds by rounding down to the containing second.</summary>
    /// <returns>The signed number of whole seconds relative to the Unix epoch.</returns>
    public long GetUnixSeconds()
    {
        return FloorDivide(utcTicks - TimeConstants.UnixEpochUtcTicks, TimeConstants.TicksPerSecond);
    }

    /// <summary>Gets whole Unix milliseconds by rounding down to the containing millisecond.</summary>
    /// <returns>The signed number of whole milliseconds relative to the Unix epoch.</returns>
    public long GetUnixMilliseconds()
    {
        return FloorDivide(utcTicks - TimeConstants.UnixEpochUtcTicks, TimeConstants.TicksPerMillisecond);
    }

    /// <summary>Gets whole Unix microseconds by rounding down to the containing microsecond.</summary>
    /// <returns>The signed number of whole microseconds relative to the Unix epoch.</returns>
    public long GetUnixMicroseconds()
    {
        return FloorDivide(utcTicks - TimeConstants.UnixEpochUtcTicks, TimeConstants.TicksPerMicrosecond);
    }

    /// <summary>Moves forward or backward by an exact elapsed duration.</summary>
    /// <param name="duration">The signed amount to add.</param>
    /// <returns>The resulting instant.</returns>
    public Instant AddDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return AddTickDelta(duration.GetTicks(), "duration");
    }

    /// <summary>Subtracts an exact elapsed duration.</summary>
    /// <param name="duration">The signed amount to subtract.</param>
    /// <returns>The resulting instant.</returns>
    public Instant SubtractDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return AddTickDelta(-((decimal)duration.GetTicks()), "duration");
    }

    /// <summary>Measures the exact elapsed time from this instant to another.</summary>
    /// <param name="other">The ending instant.</param>
    /// <returns>The other instant minus this instant.</returns>
    public Duration DurationUntil(Instant other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return Duration.FromTicks(other.GetUtcTicks() - utcTicks);
    }

    /// <summary>Orders two points on the UTC timeline.</summary>
    /// <param name="other">The instant to compare.</param>
    /// <returns>A negative value, zero, or a positive value when this instant is earlier than, equal to, or later than the other.</returns>
    public int CompareTo(Instant other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return TimeComparison.CompareLong(utcTicks, other.GetUtcTicks());
    }

    /// <summary>Tests whether two values identify the same UTC tick.</summary>
    /// <param name="other">The instant to compare, or null.</param>
    /// <returns><c>true</c> when the UTC ticks match; otherwise <c>false</c>.</returns>
    public bool EqualsValue(Instant? other)
    {
        if (other == null)
        {
            return false;
        }

        return utcTicks == other.GetUtcTicks();
    }

    /// <summary>Converts this instant to a zero-offset built-in date-time.</summary>
    /// <returns>A <see cref="DateTimeOffset"/> with offset zero.</returns>
    public DateTimeOffset ToDateTimeOffset()
    {
        return new DateTimeOffset(utcTicks, TimeSpan.Zero);
    }

    /// <summary>Converts this instant to a built-in UTC date-time.</summary>
    /// <returns>A <see cref="DateTime"/> whose kind is UTC.</returns>
    public DateTime ToUtcDateTime()
    {
        return new DateTime(utcTicks, DateTimeKind.Utc);
    }

    /// <summary>Formats this instant as invariant ISO text ending in <c>Z</c>.</summary>
    /// <returns>A UTC date-time string with fractional seconds only when needed.</returns>
    public string ToIsoString()
    {
        return LocalDateTime.FromDateTime(ToUtcDateTime()).ToIsoString() + "Z";
    }

    Instant AddTickDelta(decimal tickDelta, string parameterName)
    {
        decimal totalTicks = utcTicks + tickDelta;
        if (totalTicks < TimeConstants.MinUtcTicks || totalTicks > TimeConstants.MaxUtcTicks)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Instant arithmetic moved outside the supported UTC range.");
        }

        return new Instant((long)totalTicks);
    }

    static int FindOffsetStart(string text)
    {
        if (text.Length > 0 && text[text.Length - 1] == 'Z')
        {
            return text.Length - 1;
        }

        if (text.Length >= 9)
        {
            int secondOffsetIndex = text.Length - 9;
            if ((text[secondOffsetIndex] == '+' || text[secondOffsetIndex] == '-')
                && text[secondOffsetIndex + 3] == ':'
                && text[secondOffsetIndex + 6] == ':')
            {
                return secondOffsetIndex;
            }
        }

        if (text.Length >= 6)
        {
            int minuteOffsetIndex = text.Length - 6;
            if ((text[minuteOffsetIndex] == '+' || text[minuteOffsetIndex] == '-')
                && text[minuteOffsetIndex + 3] == ':')
            {
                return minuteOffsetIndex;
            }
        }

        return -1;
    }

    static int ParseOffsetSeconds(string text, int offsetIndex)
    {
        if (text[offsetIndex] == 'Z')
        {
            return 0;
        }

        int offsetLength = text.Length - offsetIndex;
        int offsetHour = ParseFixedDigits(text, offsetIndex + 1, 2);
        int offsetMinute = ParseFixedDigits(text, offsetIndex + 4, 2);
        int offsetSecond = 0;
        if (offsetLength == 9)
        {
            offsetSecond = ParseFixedDigits(text, offsetIndex + 7, 2);
        }

        if (offsetHour > TimeConstants.MaxHour)
        {
            throw new FormatException("UTC offset hour is outside the supported range.");
        }

        if (offsetMinute > TimeConstants.MaxMinute)
        {
            throw new FormatException("UTC offset minute is outside the supported range.");
        }

        if (offsetSecond > TimeConstants.MaxSecond)
        {
            throw new FormatException("UTC offset second is outside the supported range.");
        }

        int offsetSeconds = offsetHour * TimeConstants.SecondsPerMinute * TimeConstants.MinutesPerHour
            + offsetMinute * TimeConstants.SecondsPerMinute
            + offsetSecond;
        if (offsetSeconds > TimeConstants.MaxOffsetSeconds)
        {
            throw new FormatException("UTC offset is outside the supported range.");
        }

        if (text[offsetIndex] == '-')
        {
            offsetSeconds = -offsetSeconds;
        }

        return offsetSeconds;
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
                throw new FormatException("UTC offset components must contain only digits.");
            }

            value = value * 10 + (text[index] - '0');
            index = index + 1;
        }

        return value;
    }

    static long FloorDivide(long value, long divisor)
    {
        long quotient = value / divisor;
        long remainder = value % divisor;
        if (remainder < 0L)
        {
            quotient = quotient - 1L;
        }

        return quotient;
    }

    static void RequireUtcTicks(long utcTicks, string parameterName)
    {
        if (utcTicks < TimeConstants.MinUtcTicks || utcTicks > TimeConstants.MaxUtcTicks)
        {
            throw new ArgumentOutOfRangeException(parameterName, "UTC ticks must be in the supported DateTime range.");
        }
    }
}
