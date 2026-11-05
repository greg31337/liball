using System;

namespace time;

/// <summary>Pairs a local date-time with a fixed UTC offset.</summary>
/// <remarks>The pair identifies an instant but carries no time-zone rules. Period arithmetic preserves the offset and changes local fields; duration arithmetic moves on the timeline.</remarks>
public class OffsetDateTime : IOffsetDateTime
{
    const int MinDateTimeOffsetOffsetSeconds = -50400;
    const int MaxDateTimeOffsetOffsetSeconds = 50400;

    LocalDateTime localDateTime;
    Offset offset;

    internal OffsetDateTime(LocalDateTime localDateTime, Offset offset)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");
        TimeValidation.RequireNotNull(offset, "offset");

        this.localDateTime = localDateTime;
        this.offset = offset;
    }

    /// <summary>Combines a local date-time and fixed offset.</summary><param name="localDateTime">The local fields.</param><param name="offset">The displacement from UTC.</param><returns>The validated offset date-time.</returns>
    internal static OffsetDateTime Create(LocalDateTime localDateTime, Offset offset)
    {
        return new OffsetDateTime(localDateTime, offset);
    }

    /// <summary>Creates a whole-second offset date-time.</summary><param name="year">The year.</param><param name="month">The month.</param><param name="day">The day.</param><param name="hour">The hour.</param><param name="minute">The minute.</param><param name="second">The second.</param><param name="offset">The fixed UTC offset.</param><returns>The validated value.</returns>
    internal static OffsetDateTime Create(int year, int month, int day, int hour, int minute, int second, Offset offset)
    {
        return new OffsetDateTime(
            LocalDateTime.Create(year, month, day, hour, minute, second),
            offset);
    }

    /// <summary>Creates a tick-precision offset date-time from fields.</summary>
    /// <param name="year">The year.</param><param name="month">The month.</param><param name="day">The day.</param><param name="hour">The hour.</param><param name="minute">The minute.</param><param name="second">The second.</param>
    /// <param name="millisecond">The millisecond.</param><param name="microsecond">The following microsecond.</param><param name="tickRemainder">The following 100-nanosecond tick.</param><param name="offset">The fixed UTC offset.</param><returns>The validated value.</returns>
    internal static OffsetDateTime Create(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder, Offset offset)
    {
        return new OffsetDateTime(
            LocalDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder),
            offset);
    }

    /// <summary>Copies a built-in offset date-time.</summary><param name="dateTimeOffset">The value to copy.</param><returns>An equivalent value with the same local fields, offset, and instant.</returns>
    internal static OffsetDateTime FromDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        return new OffsetDateTime(
            LocalDateTime.FromDateTime(dateTimeOffset.DateTime),
            Offset.FromTimeSpan(dateTimeOffset.Offset));
    }

    /// <summary>Views an instant through a fixed offset.</summary><param name="instant">The exact timeline point.</param><param name="offset">The offset used to compute local fields.</param><returns>The corresponding offset date-time.</returns>
    internal static OffsetDateTime FromInstant(Instant instant, Offset offset)
    {
        TimeValidation.RequireNotNull(instant, "instant");
        TimeValidation.RequireNotNull(offset, "offset");

        decimal localTicks = instant.GetUtcTicks() + (long)offset.GetTotalSeconds() * TimeConstants.TicksPerSecond;
        if (localTicks < 0m || localTicks > GetMaxLocalTicks())
        {
            throw new ArgumentOutOfRangeException("instant", "Instant and offset map outside the supported local date-time range.");
        }

        return new OffsetDateTime(LocalDateTimeFromTicks(localTicks), offset);
    }

    /// <summary>Parses invariant ISO date-time text with a required offset.</summary><param name="text">Text such as <c>2024-02-29T12:34:56-05:00</c>.</param><returns>The parsed value.</returns><exception cref="FormatException">The text lacks a valid date, time, or offset.</exception>
    internal static OffsetDateTime Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        int offsetIndex = FindOffsetStart(text);
        if (offsetIndex < 0 || offsetIndex < 19)
        {
            throw new FormatException("Offset date-time text must use ISO local date-time text with an offset.");
        }

        return new OffsetDateTime(
            LocalDateTime.Parse(text.Substring(0, offsetIndex)),
            Offset.Parse(text.Substring(offsetIndex)));
    }

    /// <summary>Gets the stored local date and time.</summary><returns>The local fields without offset semantics.</returns>
    public LocalDateTime GetLocalDateTime()
    {
        return localDateTime;
    }

    /// <summary>Gets the fixed offset from UTC.</summary><returns>The stored offset.</returns>
    public Offset GetOffset()
    {
        return offset;
    }

    /// <summary>Gets the local date portion.</summary><returns>The local date.</returns>
    public LocalDate GetDate()
    {
        return localDateTime.GetDate();
    }

    /// <summary>Gets the local time portion.</summary><returns>The local time.</returns>
    public LocalTime GetTime()
    {
        return localDateTime.GetTime();
    }

    /// <summary>Converts the local fields and offset to an exact UTC point.</summary><returns>The represented instant.</returns>
    public Instant GetInstant()
    {
        decimal utcTicks = GetLocalTicks() - (long)offset.GetTotalSeconds() * TimeConstants.TicksPerSecond;
        if (utcTicks < TimeConstants.MinUtcTicks || utcTicks > TimeConstants.MaxUtcTicks)
        {
            throw new InvalidOperationException("Offset date-time maps outside the supported instant range.");
        }

        return Instant.FromUtcTicks((long)utcTicks);
    }

    /// <summary>Adds exact elapsed time on the UTC timeline while preserving the offset.</summary><param name="duration">The duration to add.</param><returns>A value at the resulting instant with the same offset.</returns>
    public OffsetDateTime AddDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return FromInstant(GetInstant().AddDuration(duration), offset);
    }

    /// <summary>Subtracts exact elapsed time on the UTC timeline while preserving the offset.</summary><param name="duration">The duration to subtract.</param><returns>A value at the resulting instant with the same offset.</returns>
    public OffsetDateTime SubtractDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return FromInstant(GetInstant().SubtractDuration(duration), offset);
    }

    /// <summary>Adds calendar fields to the local date-time while preserving the offset.</summary><param name="period">The period to add.</param><returns>The local-arithmetic result.</returns>
    public OffsetDateTime AddPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");

        return new OffsetDateTime(localDateTime.AddPeriod(period), offset);
    }

    /// <summary>Subtracts calendar fields from the local date-time while preserving the offset.</summary><param name="period">The period to subtract.</param><returns>The local-arithmetic result.</returns>
    public OffsetDateTime SubtractPeriod(Period period)
    {
        TimeValidation.RequireNotNull(period, "period");

        return new OffsetDateTime(localDateTime.SubtractPeriod(period), offset);
    }

    /// <summary>Orders values by represented UTC instant.</summary><param name="other">The value to compare.</param><returns>A comparison result based on timeline position, ignoring different local representations.</returns>
    public int CompareInstant(OffsetDateTime other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return GetInstant().CompareTo(other.GetInstant());
    }

    /// <summary>Orders values by local fields and then offset.</summary><param name="other">The value to compare.</param><returns>A comparison result based on the stored representation.</returns>
    public int CompareLocal(OffsetDateTime other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return localDateTime.CompareTo(other.GetLocalDateTime());
    }

    /// <summary>Tests equality of both local fields and offset.</summary><param name="other">The value to compare, or null.</param><returns><c>true</c> when the complete representation matches.</returns>
    public bool EqualsValue(OffsetDateTime? other)
    {
        if (other == null)
        {
            return false;
        }

        return localDateTime.EqualsValue(other.GetLocalDateTime())
            && offset.EqualsValue(other.GetOffset());
    }

    /// <summary>Converts this value to the built-in offset date-time type.</summary><remarks><see cref="DateTimeOffset"/> accepts only whole-minute offsets from -14:00 through +14:00, which is narrower than this library's offset range.</remarks><returns>An equivalent built-in value.</returns><exception cref="InvalidOperationException">The offset has seconds, is outside the built-in range, or maps outside the supported UTC instant range.</exception>
    public DateTimeOffset ToDateTimeOffset()
    {
        if (offset.GetSeconds() != 0)
        {
            throw new InvalidOperationException("DateTimeOffset supports only whole-minute offsets.");
        }

        if (offset.GetTotalSeconds() < MinDateTimeOffsetOffsetSeconds || offset.GetTotalSeconds() > MaxDateTimeOffsetOffsetSeconds)
        {
            throw new InvalidOperationException("DateTimeOffset supports offsets in range -14:00 through +14:00.");
        }

        GetInstant();

        return new DateTimeOffset(localDateTime.ToDateTimeUnspecified(), offset.ToTimeSpan());
    }

    /// <summary>Formats the local fields followed by the fixed offset.</summary><returns>Invariant ISO offset date-time text.</returns>
    public string ToIsoString()
    {
        return localDateTime.ToIsoString() + offset.ToIsoString();
    }

    decimal GetLocalTicks()
    {
        return (long)localDateTime.GetDate().GetDayNumber() * TimeConstants.TicksPerDay + localDateTime.GetTime().GetTicksSinceMidnight();
    }

    static decimal GetMaxLocalTicks()
    {
        return (long)TimeConstants.MaxDayNumber * TimeConstants.TicksPerDay + TimeConstants.MaxTicksSinceMidnight;
    }

    static LocalDateTime LocalDateTimeFromTicks(decimal ticks)
    {
        decimal dayNumber = decimal.Truncate(ticks / TimeConstants.TicksPerDay);
        decimal ticksOfDay = ticks % TimeConstants.TicksPerDay;

        return LocalDateTime.Create(
            LocalDate.FromDayNumber((int)dayNumber),
            LocalTime.FromTicksSinceMidnight((long)ticksOfDay));
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
}
