using System;

namespace time;

/// <summary>Defines zone-view creation shared by concrete clock implementations.</summary>
public interface IClockZoneProvider
{
    /// <summary>Combines the clock with a zone for local-time queries.</summary>
    /// <param name="zone">The zone used to interpret current instants.</param>
    /// <returns>A zoned view of the clock.</returns>
    ZonedClock InZone(TimeZoneRef zone);
}

/// <summary>Defines the public instance operations provided by <see cref="Duration"/>.</summary>
public interface IDuration
{
    /// <summary>Invokes <see cref="Duration.GetTicks"/>.</summary>
    long GetTicks();

    /// <summary>Invokes <see cref="Duration.GetTotalTicks"/>.</summary>
    double GetTotalTicks();

    /// <summary>Invokes <see cref="Duration.GetTotalMicroseconds"/>.</summary>
    double GetTotalMicroseconds();

    /// <summary>Invokes <see cref="Duration.GetTotalMilliseconds"/>.</summary>
    double GetTotalMilliseconds();

    /// <summary>Invokes <see cref="Duration.GetTotalSeconds"/>.</summary>
    double GetTotalSeconds();

    /// <summary>Invokes <see cref="Duration.GetTotalMinutes"/>.</summary>
    double GetTotalMinutes();

    /// <summary>Invokes <see cref="Duration.GetTotalHours"/>.</summary>
    double GetTotalHours();

    /// <summary>Invokes <see cref="Duration.GetTotalDays"/>.</summary>
    double GetTotalDays();

    /// <summary>Invokes <see cref="Duration.Add"/>.</summary>
    Duration Add(Duration other);

    /// <summary>Invokes <see cref="Duration.Subtract"/>.</summary>
    Duration Subtract(Duration other);

    /// <summary>Invokes <see cref="Duration.Negate"/>.</summary>
    Duration Negate();

    /// <summary>Invokes <see cref="Duration.Abs"/>.</summary>
    Duration Abs();

    /// <summary>Invokes <see cref="Duration.CompareTo"/>.</summary>
    int CompareTo(Duration other);

    /// <summary>Invokes <see cref="Duration.EqualsValue"/>.</summary>
    bool EqualsValue(Duration? other);

    /// <summary>Invokes <see cref="Duration.Min"/>.</summary>
    Duration Min(Duration other);

    /// <summary>Invokes <see cref="Duration.Max"/>.</summary>
    Duration Max(Duration other);

    /// <summary>Invokes <see cref="Duration.RoundToTicks"/>.</summary>
    Duration RoundToTicks();

    /// <summary>Invokes <see cref="Duration.RoundToMicroseconds"/>.</summary>
    Duration RoundToMicroseconds();

    /// <summary>Invokes <see cref="Duration.RoundToMilliseconds"/>.</summary>
    Duration RoundToMilliseconds();

    /// <summary>Invokes <see cref="Duration.RoundToSeconds"/>.</summary>
    Duration RoundToSeconds();

    /// <summary>Invokes <see cref="Duration.RoundToMinutes"/>.</summary>
    Duration RoundToMinutes();

    /// <summary>Invokes <see cref="Duration.RoundToHours"/>.</summary>
    Duration RoundToHours();

    /// <summary>Invokes <see cref="Duration.RoundToDays"/>.</summary>
    Duration RoundToDays();

    /// <summary>Invokes <see cref="Duration.TruncateToTicks"/>.</summary>
    Duration TruncateToTicks();

    /// <summary>Invokes <see cref="Duration.TruncateToMicroseconds"/>.</summary>
    Duration TruncateToMicroseconds();

    /// <summary>Invokes <see cref="Duration.TruncateToMilliseconds"/>.</summary>
    Duration TruncateToMilliseconds();

    /// <summary>Invokes <see cref="Duration.TruncateToSeconds"/>.</summary>
    Duration TruncateToSeconds();

    /// <summary>Invokes <see cref="Duration.TruncateToMinutes"/>.</summary>
    Duration TruncateToMinutes();

    /// <summary>Invokes <see cref="Duration.TruncateToHours"/>.</summary>
    Duration TruncateToHours();

    /// <summary>Invokes <see cref="Duration.TruncateToDays"/>.</summary>
    Duration TruncateToDays();

    /// <summary>Invokes <see cref="Duration.ToTimeSpan"/>.</summary>
    TimeSpan ToTimeSpan();

    /// <summary>Invokes <see cref="Duration.ToIsoString"/>.</summary>
    string ToIsoString();

}

/// <summary>Defines the public instance operations provided by <see cref="Instant"/>.</summary>
public interface IInstant
{
    /// <summary>Invokes <see cref="Instant.GetUtcTicks"/>.</summary>
    long GetUtcTicks();

    /// <summary>Invokes <see cref="Instant.GetUnixSeconds"/>.</summary>
    long GetUnixSeconds();

    /// <summary>Invokes <see cref="Instant.GetUnixMilliseconds"/>.</summary>
    long GetUnixMilliseconds();

    /// <summary>Invokes <see cref="Instant.GetUnixMicroseconds"/>.</summary>
    long GetUnixMicroseconds();

    /// <summary>Invokes <see cref="Instant.AddDuration"/>.</summary>
    Instant AddDuration(Duration duration);

    /// <summary>Invokes <see cref="Instant.SubtractDuration"/>.</summary>
    Instant SubtractDuration(Duration duration);

    /// <summary>Invokes <see cref="Instant.DurationUntil"/>.</summary>
    Duration DurationUntil(Instant other);

    /// <summary>Invokes <see cref="Instant.CompareTo"/>.</summary>
    int CompareTo(Instant other);

    /// <summary>Invokes <see cref="Instant.EqualsValue"/>.</summary>
    bool EqualsValue(Instant? other);

    /// <summary>Invokes <see cref="Instant.ToDateTimeOffset"/>.</summary>
    DateTimeOffset ToDateTimeOffset();

    /// <summary>Invokes <see cref="Instant.ToUtcDateTime"/>.</summary>
    DateTime ToUtcDateTime();

    /// <summary>Invokes <see cref="Instant.ToIsoString"/>.</summary>
    string ToIsoString();

}

/// <summary>Defines the public instance operations provided by <see cref="Interval"/>.</summary>
public interface IInterval
{
    /// <summary>Invokes <see cref="Interval.GetStart"/>.</summary>
    Instant GetStart();

    /// <summary>Invokes <see cref="Interval.GetEnd"/>.</summary>
    Instant GetEnd();

    /// <summary>Invokes <see cref="Interval.GetDuration"/>.</summary>
    Duration GetDuration();

    /// <summary>Invokes <see cref="Interval.IsEmpty"/>.</summary>
    bool IsEmpty();

    /// <summary>Invokes <see cref="Interval.ContainsInstant"/>.</summary>
    bool ContainsInstant(Instant instant);

    /// <summary>Invokes <see cref="Interval.ContainsInterval"/>.</summary>
    bool ContainsInterval(Interval interval);

    /// <summary>Invokes <see cref="Interval.Overlaps"/>.</summary>
    bool Overlaps(Interval interval);

    /// <summary>Invokes <see cref="Interval.Abuts"/>.</summary>
    bool Abuts(Interval interval);

    /// <summary>Invokes <see cref="Interval.Intersection"/>.</summary>
    Interval? Intersection(Interval interval);

    /// <summary>Invokes <see cref="Interval.UnionIfConnected"/>.</summary>
    Interval? UnionIfConnected(Interval interval);

    /// <summary>Invokes <see cref="Interval.ToIsoString"/>.</summary>
    string ToIsoString();

}

/// <summary>Defines the public instance operations provided by <see cref="LocalDate"/>.</summary>
public interface ILocalDate
{
    /// <summary>Invokes <see cref="LocalDate.GetYear"/>.</summary>
    int GetYear();

    /// <summary>Invokes <see cref="LocalDate.GetMonth"/>.</summary>
    int GetMonth();

    /// <summary>Invokes <see cref="LocalDate.GetDay"/>.</summary>
    int GetDay();

    /// <summary>Invokes <see cref="LocalDate.GetDayOfWeek"/>.</summary>
    int GetDayOfWeek();

    /// <summary>Invokes <see cref="LocalDate.GetDayOfYear"/>.</summary>
    int GetDayOfYear();

    /// <summary>Invokes <see cref="LocalDate.GetDayNumber"/>.</summary>
    int GetDayNumber();

    /// <summary>Invokes <see cref="LocalDate.GetDaysInMonth"/>.</summary>
    int GetDaysInMonth();

    /// <summary>Invokes <see cref="LocalDate.GetDaysInYear"/>.</summary>
    int GetDaysInYear();

    /// <summary>Invokes <see cref="LocalDate.IsLeapYear"/>.</summary>
    bool IsLeapYear();

    /// <summary>Invokes <see cref="LocalDate.CompareTo"/>.</summary>
    int CompareTo(LocalDate other);

    /// <summary>Invokes <see cref="LocalDate.EqualsValue"/>.</summary>
    bool EqualsValue(LocalDate? other);

    /// <summary>Invokes <see cref="LocalDate.ToDateOnly"/>.</summary>
    DateOnly ToDateOnly();

    /// <summary>Invokes <see cref="LocalDate.ToIsoString"/>.</summary>
    string ToIsoString();

    /// <summary>Invokes <see cref="LocalDate.ToIsoBasicString"/>.</summary>
    string ToIsoBasicString();

    /// <summary>Invokes <see cref="LocalDate.AddDays"/>.</summary>
    LocalDate AddDays(long days);

    /// <summary>Invokes <see cref="LocalDate.SubtractDays"/>.</summary>
    LocalDate SubtractDays(long days);

    /// <summary>Invokes <see cref="LocalDate.AddWeeks"/>.</summary>
    LocalDate AddWeeks(long weeks);

    /// <summary>Invokes <see cref="LocalDate.SubtractWeeks"/>.</summary>
    LocalDate SubtractWeeks(long weeks);

    /// <summary>Invokes <see cref="LocalDate.AddMonths"/>.</summary>
    LocalDate AddMonths(long months);

    /// <summary>Invokes <see cref="LocalDate.SubtractMonths"/>.</summary>
    LocalDate SubtractMonths(long months);

    /// <summary>Invokes <see cref="LocalDate.AddYears"/>.</summary>
    LocalDate AddYears(long years);

    /// <summary>Invokes <see cref="LocalDate.SubtractYears"/>.</summary>
    LocalDate SubtractYears(long years);

    /// <summary>Invokes <see cref="LocalDate.AddPeriod"/>.</summary>
    LocalDate AddPeriod(Period period);

    /// <summary>Invokes <see cref="LocalDate.SubtractPeriod"/>.</summary>
    LocalDate SubtractPeriod(Period period);

    /// <summary>Invokes <see cref="LocalDate.StartOfMonth"/>.</summary>
    LocalDate StartOfMonth();

    /// <summary>Invokes <see cref="LocalDate.EndOfMonth"/>.</summary>
    LocalDate EndOfMonth();

    /// <summary>Invokes <see cref="LocalDate.StartOfYear"/>.</summary>
    LocalDate StartOfYear();

    /// <summary>Invokes <see cref="LocalDate.EndOfYear"/>.</summary>
    LocalDate EndOfYear();

    /// <summary>Invokes <see cref="LocalDate.Next"/>.</summary>
    LocalDate Next(int dayOfWeek);

    /// <summary>Invokes <see cref="LocalDate.Previous"/>.</summary>
    LocalDate Previous(int dayOfWeek);

    /// <summary>Invokes <see cref="LocalDate.SameOrNext"/>.</summary>
    LocalDate SameOrNext(int dayOfWeek);

    /// <summary>Invokes <see cref="LocalDate.SameOrPrevious"/>.</summary>
    LocalDate SameOrPrevious(int dayOfWeek);

}

/// <summary>Defines the public instance operations provided by <see cref="LocalDateTime"/>.</summary>
public interface ILocalDateTime
{
    /// <summary>Invokes <see cref="LocalDateTime.GetDate"/>.</summary>
    LocalDate GetDate();

    /// <summary>Invokes <see cref="LocalDateTime.GetTime"/>.</summary>
    LocalTime GetTime();

    /// <summary>Invokes <see cref="LocalDateTime.GetYear"/>.</summary>
    int GetYear();

    /// <summary>Invokes <see cref="LocalDateTime.GetMonth"/>.</summary>
    int GetMonth();

    /// <summary>Invokes <see cref="LocalDateTime.GetDay"/>.</summary>
    int GetDay();

    /// <summary>Invokes <see cref="LocalDateTime.GetHour"/>.</summary>
    int GetHour();

    /// <summary>Invokes <see cref="LocalDateTime.GetMinute"/>.</summary>
    int GetMinute();

    /// <summary>Invokes <see cref="LocalDateTime.GetSecond"/>.</summary>
    int GetSecond();

    /// <summary>Invokes <see cref="LocalDateTime.GetMillisecond"/>.</summary>
    int GetMillisecond();

    /// <summary>Invokes <see cref="LocalDateTime.GetMicrosecond"/>.</summary>
    int GetMicrosecond();

    /// <summary>Invokes <see cref="LocalDateTime.GetTickRemainder"/>.</summary>
    int GetTickRemainder();

    /// <summary>Invokes <see cref="LocalDateTime.CompareTo"/>.</summary>
    int CompareTo(LocalDateTime other);

    /// <summary>Invokes <see cref="LocalDateTime.EqualsValue"/>.</summary>
    bool EqualsValue(LocalDateTime? other);

    /// <summary>Invokes <see cref="LocalDateTime.ToDateTimeUnspecified"/>.</summary>
    DateTime ToDateTimeUnspecified();

    /// <summary>Invokes <see cref="LocalDateTime.ToIsoString"/>.</summary>
    string ToIsoString();

    /// <summary>Invokes <see cref="LocalDateTime.ToIsoBasicString"/>.</summary>
    string ToIsoBasicString();

    /// <summary>Invokes <see cref="LocalDateTime.AddDuration"/>.</summary>
    LocalDateTime AddDuration(Duration duration);

    /// <summary>Invokes <see cref="LocalDateTime.SubtractDuration"/>.</summary>
    LocalDateTime SubtractDuration(Duration duration);

    /// <summary>Invokes <see cref="LocalDateTime.AddPeriod"/>.</summary>
    LocalDateTime AddPeriod(Period period);

    /// <summary>Invokes <see cref="LocalDateTime.SubtractPeriod"/>.</summary>
    LocalDateTime SubtractPeriod(Period period);

}

/// <summary>Defines the public instance operations provided by <see cref="LocalTime"/>.</summary>
public interface ILocalTime
{
    /// <summary>Invokes <see cref="LocalTime.GetHour"/>.</summary>
    int GetHour();

    /// <summary>Invokes <see cref="LocalTime.GetMinute"/>.</summary>
    int GetMinute();

    /// <summary>Invokes <see cref="LocalTime.GetSecond"/>.</summary>
    int GetSecond();

    /// <summary>Invokes <see cref="LocalTime.GetMillisecond"/>.</summary>
    int GetMillisecond();

    /// <summary>Invokes <see cref="LocalTime.GetMicrosecond"/>.</summary>
    int GetMicrosecond();

    /// <summary>Invokes <see cref="LocalTime.GetTickRemainder"/>.</summary>
    int GetTickRemainder();

    /// <summary>Invokes <see cref="LocalTime.GetTicksSinceMidnight"/>.</summary>
    long GetTicksSinceMidnight();

    /// <summary>Invokes <see cref="LocalTime.CompareTo"/>.</summary>
    int CompareTo(LocalTime other);

    /// <summary>Invokes <see cref="LocalTime.EqualsValue"/>.</summary>
    bool EqualsValue(LocalTime? other);

    /// <summary>Invokes <see cref="LocalTime.ToTimeOnly"/>.</summary>
    TimeOnly ToTimeOnly();

    /// <summary>Invokes <see cref="LocalTime.ToIsoString"/>.</summary>
    string ToIsoString();

    /// <summary>Invokes <see cref="LocalTime.AddDuration"/>.</summary>
    LocalTimeRoll AddDuration(Duration duration);

    /// <summary>Invokes <see cref="LocalTime.SubtractDuration"/>.</summary>
    LocalTimeRoll SubtractDuration(Duration duration);

    /// <summary>Invokes <see cref="LocalTime.AddPeriod"/>.</summary>
    LocalTimeRoll AddPeriod(Period period);

    /// <summary>Invokes <see cref="LocalTime.SubtractPeriod"/>.</summary>
    LocalTimeRoll SubtractPeriod(Period period);

}

/// <summary>Defines the public instance operations provided by <see cref="LocalTimeRoll"/>.</summary>
public interface ILocalTimeRoll
{
    /// <summary>Invokes <see cref="LocalTimeRoll.GetTicksSinceMidnight"/>.</summary>
    long GetTicksSinceMidnight();

    /// <summary>Invokes <see cref="LocalTimeRoll.GetDayDelta"/>.</summary>
    int GetDayDelta();

    /// <summary>Invokes <see cref="LocalTimeRoll.GetTime"/>.</summary>
    LocalTime GetTime();

}

/// <summary>Defines the public instance operations provided by <see cref="Offset"/>.</summary>
public interface IOffset
{
    /// <summary>Invokes <see cref="Offset.GetSign"/>.</summary>
    int GetSign();

    /// <summary>Invokes <see cref="Offset.GetHours"/>.</summary>
    int GetHours();

    /// <summary>Invokes <see cref="Offset.GetMinutes"/>.</summary>
    int GetMinutes();

    /// <summary>Invokes <see cref="Offset.GetSeconds"/>.</summary>
    int GetSeconds();

    /// <summary>Invokes <see cref="Offset.GetTotalSeconds"/>.</summary>
    int GetTotalSeconds();

    /// <summary>Invokes <see cref="Offset.GetTotalMinutes"/>.</summary>
    int GetTotalMinutes();

    /// <summary>Invokes <see cref="Offset.ToTimeSpan"/>.</summary>
    TimeSpan ToTimeSpan();

    /// <summary>Invokes <see cref="Offset.ToIsoString"/>.</summary>
    string ToIsoString();

    /// <summary>Invokes <see cref="Offset.CompareTo"/>.</summary>
    int CompareTo(Offset other);

    /// <summary>Invokes <see cref="Offset.EqualsValue"/>.</summary>
    bool EqualsValue(Offset? other);

}

/// <summary>Defines the public instance operations provided by <see cref="OffsetDateTime"/>.</summary>
public interface IOffsetDateTime
{
    /// <summary>Invokes <see cref="OffsetDateTime.GetLocalDateTime"/>.</summary>
    LocalDateTime GetLocalDateTime();

    /// <summary>Invokes <see cref="OffsetDateTime.GetOffset"/>.</summary>
    Offset GetOffset();

    /// <summary>Invokes <see cref="OffsetDateTime.GetDate"/>.</summary>
    LocalDate GetDate();

    /// <summary>Invokes <see cref="OffsetDateTime.GetTime"/>.</summary>
    LocalTime GetTime();

    /// <summary>Invokes <see cref="OffsetDateTime.GetInstant"/>.</summary>
    Instant GetInstant();

    /// <summary>Invokes <see cref="OffsetDateTime.AddDuration"/>.</summary>
    OffsetDateTime AddDuration(Duration duration);

    /// <summary>Invokes <see cref="OffsetDateTime.SubtractDuration"/>.</summary>
    OffsetDateTime SubtractDuration(Duration duration);

    /// <summary>Invokes <see cref="OffsetDateTime.AddPeriod"/>.</summary>
    OffsetDateTime AddPeriod(Period period);

    /// <summary>Invokes <see cref="OffsetDateTime.SubtractPeriod"/>.</summary>
    OffsetDateTime SubtractPeriod(Period period);

    /// <summary>Invokes <see cref="OffsetDateTime.CompareInstant"/>.</summary>
    int CompareInstant(OffsetDateTime other);

    /// <summary>Invokes <see cref="OffsetDateTime.CompareLocal"/>.</summary>
    int CompareLocal(OffsetDateTime other);

    /// <summary>Invokes <see cref="OffsetDateTime.EqualsValue"/>.</summary>
    bool EqualsValue(OffsetDateTime? other);

    /// <summary>Invokes <see cref="OffsetDateTime.ToDateTimeOffset"/>.</summary>
    DateTimeOffset ToDateTimeOffset();

    /// <summary>Invokes <see cref="OffsetDateTime.ToIsoString"/>.</summary>
    string ToIsoString();

}

/// <summary>Defines the public instance operations provided by <see cref="Period"/>.</summary>
public interface IPeriod
{
    /// <summary>Invokes <see cref="Period.GetYears"/>.</summary>
    long GetYears();

    /// <summary>Invokes <see cref="Period.GetMonths"/>.</summary>
    long GetMonths();

    /// <summary>Invokes <see cref="Period.GetWeeks"/>.</summary>
    long GetWeeks();

    /// <summary>Invokes <see cref="Period.GetDays"/>.</summary>
    long GetDays();

    /// <summary>Invokes <see cref="Period.GetHours"/>.</summary>
    long GetHours();

    /// <summary>Invokes <see cref="Period.GetMinutes"/>.</summary>
    long GetMinutes();

    /// <summary>Invokes <see cref="Period.GetSeconds"/>.</summary>
    long GetSeconds();

    /// <summary>Invokes <see cref="Period.GetMilliseconds"/>.</summary>
    long GetMilliseconds();

    /// <summary>Invokes <see cref="Period.GetMicroseconds"/>.</summary>
    long GetMicroseconds();

    /// <summary>Invokes <see cref="Period.GetTicks"/>.</summary>
    long GetTicks();

    /// <summary>Invokes <see cref="Period.NormalizeWeeksToDays"/>.</summary>
    Period NormalizeWeeksToDays();

    /// <summary>Invokes <see cref="Period.Add"/>.</summary>
    Period Add(Period other);

    /// <summary>Invokes <see cref="Period.Subtract"/>.</summary>
    Period Subtract(Period other);

    /// <summary>Invokes <see cref="Period.Negate"/>.</summary>
    Period Negate();

    /// <summary>Invokes <see cref="Period.CompareTo"/>.</summary>
    int CompareTo(Period other);

    /// <summary>Invokes <see cref="Period.EqualsValue"/>.</summary>
    bool EqualsValue(Period? other);

    /// <summary>Invokes <see cref="Period.ToIsoString"/>.</summary>
    string ToIsoString();

}

/// <summary>Defines the public instance operations provided by <see cref="RecurrenceBuilder"/>.</summary>
public interface IRecurrenceBuilder
{
    /// <summary>Invokes <see cref="RecurrenceBuilder.SetInterval"/>.</summary>
    RecurrenceBuilder SetInterval(int interval);

    /// <summary>Invokes <see cref="RecurrenceBuilder.SetCount"/>.</summary>
    RecurrenceBuilder SetCount(int count);

    /// <summary>Invokes <see cref="RecurrenceBuilder.SetUntil"/>.</summary>
    RecurrenceBuilder SetUntil(Instant until);

    /// <summary>Invokes <see cref="RecurrenceBuilder.SetLocalUntil"/>.</summary>
    RecurrenceBuilder SetLocalUntil(LocalDateTime until);

    /// <summary>Invokes <see cref="RecurrenceBuilder.SetWeekStart"/>.</summary>
    RecurrenceBuilder SetWeekStart(int weekStart);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddBySecond"/>.</summary>
    RecurrenceBuilder AddBySecond(int second);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByMinute"/>.</summary>
    RecurrenceBuilder AddByMinute(int minute);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByHour"/>.</summary>
    RecurrenceBuilder AddByHour(int hour);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByDay"/>.</summary>
    RecurrenceBuilder AddByDay(RecurrenceDay day);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByMonthDay"/>.</summary>
    RecurrenceBuilder AddByMonthDay(int monthDay);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByYearDay"/>.</summary>
    RecurrenceBuilder AddByYearDay(int yearDay);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByWeekNo"/>.</summary>
    RecurrenceBuilder AddByWeekNo(int weekNumber);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddByMonth"/>.</summary>
    RecurrenceBuilder AddByMonth(int month);

    /// <summary>Invokes <see cref="RecurrenceBuilder.AddBySetPos"/>.</summary>
    RecurrenceBuilder AddBySetPos(int position);

    /// <summary>Invokes <see cref="RecurrenceBuilder.Build"/>.</summary>
    RecurrenceRule Build();

}

/// <summary>Defines the public instance operations provided by <see cref="RecurrenceDay"/>.</summary>
public interface IRecurrenceDay
{
    /// <summary>Invokes <see cref="RecurrenceDay.GetDayOfWeek"/>.</summary>
    int GetDayOfWeek();

    /// <summary>Invokes <see cref="RecurrenceDay.GetOrdinal"/>.</summary>
    int GetOrdinal();

    /// <summary>Invokes <see cref="RecurrenceDay.HasOrdinal"/>.</summary>
    bool HasOrdinal();

    /// <summary>Invokes <see cref="RecurrenceDay.EqualsValue"/>.</summary>
    bool EqualsValue(RecurrenceDay? other);

}

/// <summary>Defines the public instance operations provided by <see cref="RecurrenceEnumerator"/>.</summary>
public interface IRecurrenceEnumerator
{
    /// <summary>Invokes <see cref="RecurrenceEnumerator.GetCount"/>.</summary>
    int GetCount();

    /// <summary>Invokes <see cref="RecurrenceEnumerator.MoveNext"/>.</summary>
    bool MoveNext();

    /// <summary>Invokes <see cref="RecurrenceEnumerator.GetCurrent"/>.</summary>
    LocalDateTime GetCurrent();

    /// <summary>Invokes <see cref="RecurrenceEnumerator.Reset"/>.</summary>
    void Reset();

    /// <summary>Invokes <see cref="RecurrenceEnumerator.ToArray"/>.</summary>
    LocalDateTime[] ToArray();

}

/// <summary>Defines the public instance operations provided by <see cref="RecurrenceRule"/>.</summary>
public interface IRecurrenceRule
{
    /// <summary>Invokes <see cref="RecurrenceRule.GetFrequency"/>.</summary>
    RecurrenceFrequency GetFrequency();

    /// <summary>Invokes <see cref="RecurrenceRule.GetInterval"/>.</summary>
    int GetInterval();

    /// <summary>Invokes <see cref="RecurrenceRule.GetCount"/>.</summary>
    int GetCount();

    /// <summary>Invokes <see cref="RecurrenceRule.GetUntil"/>.</summary>
    Instant? GetUntil();

    /// <summary>Invokes <see cref="RecurrenceRule.GetLocalUntil"/>.</summary>
    LocalDateTime? GetLocalUntil();

    /// <summary>Invokes <see cref="RecurrenceRule.GetWeekStart"/>.</summary>
    int GetWeekStart();

    /// <summary>Invokes <see cref="RecurrenceRule.GetBySecond"/>.</summary>
    int[] GetBySecond();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByMinute"/>.</summary>
    int[] GetByMinute();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByHour"/>.</summary>
    int[] GetByHour();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByDay"/>.</summary>
    RecurrenceDay[] GetByDay();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByMonthDay"/>.</summary>
    int[] GetByMonthDay();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByYearDay"/>.</summary>
    int[] GetByYearDay();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByWeekNo"/>.</summary>
    int[] GetByWeekNo();

    /// <summary>Invokes <see cref="RecurrenceRule.GetByMonth"/>.</summary>
    int[] GetByMonth();

    /// <summary>Invokes <see cref="RecurrenceRule.GetBySetPos"/>.</summary>
    int[] GetBySetPos();

    /// <summary>Invokes <see cref="RecurrenceRule.ToRRuleString"/>.</summary>
    string ToRRuleString();

}

/// <summary>Defines the public instance operations provided by <see cref="RecurrenceSet"/>.</summary>
public interface IRecurrenceSet
{
    /// <summary>Invokes <see cref="RecurrenceSet.GetStart"/>.</summary>
    LocalDateTime GetStart();

    /// <summary>Invokes <see cref="RecurrenceSet.GetRules"/>.</summary>
    RecurrenceRule[] GetRules();

    /// <summary>Invokes <see cref="RecurrenceSet.GetRDates"/>.</summary>
    LocalDateTime[] GetRDates();

    /// <summary>Invokes <see cref="RecurrenceSet.GetExDates"/>.</summary>
    LocalDateTime[] GetExDates();

    /// <summary>Invokes <see cref="RecurrenceSet.AddRule"/>.</summary>
    RecurrenceSet AddRule(RecurrenceRule rule);

    /// <summary>Invokes <see cref="RecurrenceSet.AddRDate"/>.</summary>
    RecurrenceSet AddRDate(LocalDateTime date);

    /// <summary>Invokes <see cref="RecurrenceSet.AddExDate"/>.</summary>
    RecurrenceSet AddExDate(LocalDateTime date);

    /// <summary>Invokes <see cref="RecurrenceSet.ExpandBounded"/>.</summary>
    LocalDateTime[] ExpandBounded(int maximumCandidates);

    /// <summary>Invokes <see cref="RecurrenceSet.ExpandThrough"/>.</summary>
    LocalDateTime[] ExpandThrough(LocalDateTime end, int maximumCandidates);

}

/// <summary>Defines the public instance operations provided by <see cref="TimeZoneRef"/>.</summary>
public interface ITimeZoneRef
{
    /// <summary>Invokes <see cref="TimeZoneRef.GetId"/>.</summary>
    string GetId();

    /// <summary>Invokes <see cref="TimeZoneRef.GetDisplayName"/>.</summary>
    string GetDisplayName();

    /// <summary>Invokes <see cref="TimeZoneRef.GetTimeZoneInfo"/>.</summary>
    TimeZoneInfo GetTimeZoneInfo();

    /// <summary>Invokes <see cref="TimeZoneRef.GetOffset"/>.</summary>
    Offset GetOffset(Instant instant);

    /// <summary>Invokes <see cref="TimeZoneRef.ConvertInstant"/>.</summary>
    OffsetDateTime ConvertInstant(Instant instant);

    /// <summary>Invokes <see cref="TimeZoneRef.IsInvalidLocal"/>.</summary>
    bool IsInvalidLocal(LocalDateTime localDateTime);

    /// <summary>Invokes <see cref="TimeZoneRef.IsAmbiguousLocal"/>.</summary>
    bool IsAmbiguousLocal(LocalDateTime localDateTime);

    /// <summary>Invokes <see cref="TimeZoneRef.GetAmbiguousOffsets"/>.</summary>
    Offset[] GetAmbiguousOffsets(LocalDateTime localDateTime);

    /// <summary>Invokes <see cref="TimeZoneRef.MapLocal"/>.</summary>
    OffsetDateTime[] MapLocal(LocalDateTime localDateTime);

    /// <summary>Invokes <see cref="TimeZoneRef.ResolveLocal"/>.</summary>
    OffsetDateTime ResolveLocal(LocalDateTime localDateTime, IZoneResolver resolver);

    /// <summary>Invokes <see cref="TimeZoneRef.GetNextTransition"/>.</summary>
    Instant? GetNextTransition(Instant instant);

    /// <summary>Invokes <see cref="TimeZoneRef.GetPreviousTransition"/>.</summary>
    Instant? GetPreviousTransition(Instant instant);

}

/// <summary>Defines the public instance operations provided by <see cref="WeekRules"/>.</summary>
public interface IWeekRules
{
    /// <summary>Invokes <see cref="WeekRules.GetFirstDayOfWeek"/>.</summary>
    int GetFirstDayOfWeek();

    /// <summary>Invokes <see cref="WeekRules.GetMinimumDaysInFirstWeek"/>.</summary>
    int GetMinimumDaysInFirstWeek();

    /// <summary>Invokes <see cref="WeekRules.GetStartOfWeek"/>.</summary>
    LocalDate GetStartOfWeek(LocalDate date);

    /// <summary>Invokes <see cref="WeekRules.GetWeekYear"/>.</summary>
    int GetWeekYear(LocalDate date);

    /// <summary>Invokes <see cref="WeekRules.GetWeekOfWeekYear"/>.</summary>
    int GetWeekOfWeekYear(LocalDate date);

    /// <summary>Invokes <see cref="WeekRules.GetStartOfWeekYear"/>.</summary>
    LocalDate GetStartOfWeekYear(int weekYear);

}

/// <summary>Defines the public instance operations provided by <see cref="ZonedClock"/>.</summary>
public interface IZonedClock
{
    /// <summary>Invokes <see cref="ZonedClock.GetZone"/>.</summary>
    TimeZoneRef GetZone();

    /// <summary>Invokes <see cref="ZonedClock.GetCurrentZonedDateTime"/>.</summary>
    ZonedDateTime GetCurrentZonedDateTime();

    /// <summary>Invokes <see cref="ZonedClock.GetCurrentLocalDateTime"/>.</summary>
    LocalDateTime GetCurrentLocalDateTime();

    /// <summary>Invokes <see cref="ZonedClock.GetCurrentDate"/>.</summary>
    LocalDate GetCurrentDate();

    /// <summary>Invokes <see cref="ZonedClock.GetCurrentTime"/>.</summary>
    LocalTime GetCurrentTime();

}

/// <summary>Defines the public instance operations provided by <see cref="ZonedDateTime"/>.</summary>
public interface IZonedDateTime
{
    /// <summary>Invokes <see cref="ZonedDateTime.GetInstant"/>.</summary>
    Instant GetInstant();

    /// <summary>Invokes <see cref="ZonedDateTime.GetZone"/>.</summary>
    TimeZoneRef GetZone();

    /// <summary>Invokes <see cref="ZonedDateTime.GetZoneId"/>.</summary>
    string GetZoneId();

    /// <summary>Invokes <see cref="ZonedDateTime.GetOffset"/>.</summary>
    Offset GetOffset();

    /// <summary>Invokes <see cref="ZonedDateTime.GetLocalDateTime"/>.</summary>
    LocalDateTime GetLocalDateTime();

    /// <summary>Invokes <see cref="ZonedDateTime.GetDate"/>.</summary>
    LocalDate GetDate();

    /// <summary>Invokes <see cref="ZonedDateTime.GetTime"/>.</summary>
    LocalTime GetTime();

    /// <summary>Invokes <see cref="ZonedDateTime.WithZone"/>.</summary>
    ZonedDateTime WithZone(TimeZoneRef targetZone);

    /// <summary>Invokes <see cref="ZonedDateTime.ToOffsetDateTime"/>.</summary>
    OffsetDateTime ToOffsetDateTime();

    /// <summary>Invokes <see cref="ZonedDateTime.AddDuration"/>.</summary>
    ZonedDateTime AddDuration(Duration duration);

    /// <summary>Invokes <see cref="ZonedDateTime.SubtractDuration"/>.</summary>
    ZonedDateTime SubtractDuration(Duration duration);

    /// <summary>Invokes <see cref="ZonedDateTime.AddPeriod"/>.</summary>
    ZonedDateTime AddPeriod(Period period, IZoneResolver resolver);

    /// <summary>Invokes <see cref="ZonedDateTime.SubtractPeriod"/>.</summary>
    ZonedDateTime SubtractPeriod(Period period, IZoneResolver resolver);

    /// <summary>Invokes <see cref="ZonedDateTime.StartOfDay"/>.</summary>
    ZonedDateTime StartOfDay();

    /// <summary>Invokes <see cref="ZonedDateTime.CompareInstant"/>.</summary>
    int CompareInstant(ZonedDateTime other);

    /// <summary>Invokes <see cref="ZonedDateTime.EqualsValue"/>.</summary>
    bool EqualsValue(ZonedDateTime? other);

    /// <summary>Invokes <see cref="ZonedDateTime.ToIsoString"/>.</summary>
    string ToIsoString();

}
