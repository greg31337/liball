using System;

namespace time;

/// <summary>Creates and parses the library's time, calendar, zone, clock, and recurrence objects.</summary>
public class TimeFactory : ITimeFactory
{
    /// <summary>Creates a reusable time API factory.</summary>
    public TimeFactory()
    {
    }

    /// <summary>Creates a zero duration.</summary>
    public Duration CreateZeroDuration()
    {
        return Duration.Zero();
    }

    /// <summary>Creates a duration from ticks.</summary>
    public Duration CreateDurationFromTicks(long ticks)
    {
        return Duration.FromTicks(ticks);
    }

    /// <summary>Creates a duration from microseconds.</summary>
    public Duration CreateDurationFromMicroseconds(long microseconds)
    {
        return Duration.FromMicroseconds(microseconds);
    }

    /// <summary>Creates a duration from milliseconds.</summary>
    public Duration CreateDurationFromMilliseconds(long milliseconds)
    {
        return Duration.FromMilliseconds(milliseconds);
    }

    /// <summary>Creates a duration from seconds.</summary>
    public Duration CreateDurationFromSeconds(long seconds)
    {
        return Duration.FromSeconds(seconds);
    }

    /// <summary>Creates a duration from minutes.</summary>
    public Duration CreateDurationFromMinutes(long minutes)
    {
        return Duration.FromMinutes(minutes);
    }

    /// <summary>Creates a duration from hours.</summary>
    public Duration CreateDurationFromHours(long hours)
    {
        return Duration.FromHours(hours);
    }

    /// <summary>Creates a duration from exact days.</summary>
    public Duration CreateDurationFromDays(long days)
    {
        return Duration.FromDays(days);
    }

    /// <summary>Creates a duration from a built-in duration.</summary>
    public Duration CreateDurationFromTimeSpan(TimeSpan timeSpan)
    {
        return Duration.FromTimeSpan(timeSpan);
    }

    /// <summary>Parses an ISO duration.</summary>
    public Duration ParseDuration(string text)
    {
        return Duration.Parse(text);
    }

    /// <summary>Creates the Unix epoch instant.</summary>
    public Instant CreateUnixEpoch()
    {
        return Instant.UnixEpoch();
    }

    /// <summary>Reads the current system instant.</summary>
    public Instant CreateCurrentInstant()
    {
        return Instant.Now();
    }

    /// <summary>Creates an instant from UTC ticks.</summary>
    public Instant CreateInstantFromUtcTicks(long utcTicks)
    {
        return Instant.FromUtcTicks(utcTicks);
    }

    /// <summary>Creates an instant from Unix seconds.</summary>
    public Instant CreateInstantFromUnixSeconds(long unixSeconds)
    {
        return Instant.FromUnixSeconds(unixSeconds);
    }

    /// <summary>Creates an instant from Unix milliseconds.</summary>
    public Instant CreateInstantFromUnixMilliseconds(long unixMilliseconds)
    {
        return Instant.FromUnixMilliseconds(unixMilliseconds);
    }

    /// <summary>Creates an instant from Unix microseconds.</summary>
    public Instant CreateInstantFromUnixMicroseconds(long unixMicroseconds)
    {
        return Instant.FromUnixMicroseconds(unixMicroseconds);
    }

    /// <summary>Creates an instant from a built-in offset date-time.</summary>
    public Instant CreateInstantFromDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        return Instant.FromDateTimeOffset(dateTimeOffset);
    }

    /// <summary>Creates an instant from a UTC date-time.</summary>
    public Instant CreateInstantFromUtcDateTime(DateTime dateTime)
    {
        return Instant.FromUtcDateTime(dateTime);
    }

    /// <summary>Parses an ISO instant.</summary>
    public Instant ParseInstant(string text)
    {
        return Instant.Parse(text);
    }

    /// <summary>Creates a half-open interval from its endpoints.</summary>
    public Interval CreateInterval(Instant start, Instant end)
    {
        return Interval.Create(start, end);
    }

    /// <summary>Creates an interval from a start and duration.</summary>
    public Interval CreateIntervalFromStartAndDuration(Instant start, Duration duration)
    {
        return Interval.FromStartAndDuration(start, duration);
    }

    /// <summary>Parses an ISO interval.</summary>
    public Interval ParseInterval(string text)
    {
        return Interval.Parse(text);
    }

    /// <summary>Creates a local date from calendar fields.</summary>
    public LocalDate CreateLocalDate(int year, int month, int day)
    {
        return LocalDate.Create(year, month, day);
    }

    /// <summary>Creates a local date from a built-in date.</summary>
    public LocalDate CreateLocalDateFromDateOnly(DateOnly date)
    {
        return LocalDate.FromDateOnly(date);
    }

    /// <summary>Creates a local date from a date-time.</summary>
    public LocalDate CreateLocalDateFromDateTime(DateTime dateTime)
    {
        return LocalDate.FromDateTime(dateTime);
    }

    /// <summary>Creates a local date from a day number.</summary>
    public LocalDate CreateLocalDateFromDayNumber(int dayNumber)
    {
        return LocalDate.FromDayNumber(dayNumber);
    }

    /// <summary>Parses an ISO local date.</summary>
    public LocalDate ParseLocalDate(string text)
    {
        return LocalDate.Parse(text);
    }

    /// <summary>Parses an ISO basic local date.</summary>
    public LocalDate ParseIsoBasicLocalDate(string text)
    {
        return LocalDate.ParseIsoBasic(text);
    }

    /// <summary>Creates a local time from clock fields.</summary>
    public LocalTime CreateLocalTime(int hour, int minute, int second)
    {
        return LocalTime.Create(hour, minute, second);
    }

    /// <summary>Creates a precision-preserving local time.</summary>
    public LocalTime CreatePreciseLocalTime(int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        return LocalTime.Create(hour, minute, second, millisecond, microsecond, tickRemainder);
    }

    /// <summary>Creates midnight.</summary>
    public LocalTime CreateMidnight()
    {
        return LocalTime.Midnight();
    }

    /// <summary>Creates noon.</summary>
    public LocalTime CreateNoon()
    {
        return LocalTime.Noon();
    }

    /// <summary>Creates a local time from a built-in time.</summary>
    public LocalTime CreateLocalTimeFromTimeOnly(TimeOnly time)
    {
        return LocalTime.FromTimeOnly(time);
    }

    /// <summary>Creates a local time from ticks since midnight.</summary>
    public LocalTime CreateLocalTimeFromTicksSinceMidnight(long ticksSinceMidnight)
    {
        return LocalTime.FromTicksSinceMidnight(ticksSinceMidnight);
    }

    /// <summary>Parses an ISO local time.</summary>
    public LocalTime ParseLocalTime(string text)
    {
        return LocalTime.Parse(text);
    }

    /// <summary>Creates a local-time rollover result.</summary>
    public LocalTimeRoll CreateLocalTimeRoll(long ticksSinceMidnight, int dayDelta)
    {
        return LocalTimeRoll.Create(ticksSinceMidnight, dayDelta);
    }

    /// <summary>Creates a local date-time from date and time objects.</summary>
    public LocalDateTime CreateLocalDateTime(LocalDate date, LocalTime time)
    {
        return LocalDateTime.Create(date, time);
    }

    /// <summary>Creates a local date-time from common fields.</summary>
    public LocalDateTime CreateLocalDateTimeFromFields(int year, int month, int day, int hour, int minute, int second)
    {
        return LocalDateTime.Create(year, month, day, hour, minute, second);
    }

    /// <summary>Creates a precision-preserving local date-time.</summary>
    public LocalDateTime CreatePreciseLocalDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        return LocalDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder);
    }

    /// <summary>Creates a local date-time from a built-in date-time.</summary>
    public LocalDateTime CreateLocalDateTimeFromDateTime(DateTime dateTime)
    {
        return LocalDateTime.FromDateTime(dateTime);
    }

    /// <summary>Parses an ISO local date-time.</summary>
    public LocalDateTime ParseLocalDateTime(string text)
    {
        return LocalDateTime.Parse(text);
    }

    /// <summary>Parses an ISO basic local date-time.</summary>
    public LocalDateTime ParseIsoBasicLocalDateTime(string text)
    {
        return LocalDateTime.ParseIsoBasic(text);
    }

    /// <summary>Creates a zero offset.</summary>
    public Offset CreateZeroOffset()
    {
        return Offset.Zero();
    }

    /// <summary>Creates an offset from seconds.</summary>
    public Offset CreateOffsetFromSeconds(int seconds)
    {
        return Offset.FromSeconds(seconds);
    }

    /// <summary>Creates an offset from minutes.</summary>
    public Offset CreateOffsetFromMinutes(int minutes)
    {
        return Offset.FromMinutes(minutes);
    }

    /// <summary>Creates an offset from hours.</summary>
    public Offset CreateOffsetFromHours(int hours)
    {
        return Offset.FromHours(hours);
    }

    /// <summary>Creates an offset from signed hour, minute, and second components.</summary>
    /// <remarks>Zero components are neutral. All nonzero components must have the same sign, and the combined offset must be in the range -18:00 through +18:00.</remarks>
    public Offset CreateOffsetFromComponents(int hours, int minutes, int seconds)
    {
        return Offset.FromComponents(hours, minutes, seconds);
    }

    /// <summary>Creates an offset from a built-in duration.</summary>
    public Offset CreateOffsetFromTimeSpan(TimeSpan timeSpan)
    {
        return Offset.FromTimeSpan(timeSpan);
    }

    /// <summary>Parses an ISO offset.</summary>
    public Offset ParseOffset(string text)
    {
        return Offset.Parse(text);
    }

    /// <summary>Creates an offset date-time from local fields and an offset.</summary>
    public OffsetDateTime CreateOffsetDateTime(LocalDateTime localDateTime, Offset offset)
    {
        return OffsetDateTime.Create(localDateTime, offset);
    }

    /// <summary>Creates an offset date-time from common fields.</summary>
    public OffsetDateTime CreateOffsetDateTimeFromFields(int year, int month, int day, int hour, int minute, int second, Offset offset)
    {
        return OffsetDateTime.Create(year, month, day, hour, minute, second, offset);
    }

    /// <summary>Creates a precision-preserving offset date-time.</summary>
    public OffsetDateTime CreatePreciseOffsetDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder, Offset offset)
    {
        return OffsetDateTime.Create(year, month, day, hour, minute, second, millisecond, microsecond, tickRemainder, offset);
    }

    /// <summary>Creates an offset date-time from a built-in value.</summary>
    public OffsetDateTime CreateOffsetDateTimeFromDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        return OffsetDateTime.FromDateTimeOffset(dateTimeOffset);
    }

    /// <summary>Creates an offset date-time from an instant and offset.</summary>
    public OffsetDateTime CreateOffsetDateTimeFromInstant(Instant instant, Offset offset)
    {
        return OffsetDateTime.FromInstant(instant, offset);
    }

    /// <summary>Parses an ISO offset date-time.</summary>
    public OffsetDateTime ParseOffsetDateTime(string text)
    {
        return OffsetDateTime.Parse(text);
    }

    /// <summary>Creates a zero calendar period.</summary>
    public Period CreateZeroPeriod()
    {
        return Period.Zero();
    }

    /// <summary>Creates a calendar period from every supported field.</summary>
    public Period CreatePeriod(long years, long months, long weeks, long days, long hours, long minutes, long seconds, long milliseconds, long microseconds, long ticks)
    {
        return Period.Create(years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, ticks);
    }

    /// <summary>Creates a period from years.</summary>
    public Period CreatePeriodFromYears(long years)
    {
        return Period.FromYears(years);
    }

    /// <summary>Creates a period from months.</summary>
    public Period CreatePeriodFromMonths(long months)
    {
        return Period.FromMonths(months);
    }

    /// <summary>Creates a period from weeks.</summary>
    public Period CreatePeriodFromWeeks(long weeks)
    {
        return Period.FromWeeks(weeks);
    }

    /// <summary>Creates a period from days.</summary>
    public Period CreatePeriodFromDays(long days)
    {
        return Period.FromDays(days);
    }

    /// <summary>Creates a period from hours.</summary>
    public Period CreatePeriodFromHours(long hours)
    {
        return Period.FromHours(hours);
    }

    /// <summary>Creates a period from minutes.</summary>
    public Period CreatePeriodFromMinutes(long minutes)
    {
        return Period.FromMinutes(minutes);
    }

    /// <summary>Creates a period from seconds.</summary>
    public Period CreatePeriodFromSeconds(long seconds)
    {
        return Period.FromSeconds(seconds);
    }

    /// <summary>Creates a period from milliseconds.</summary>
    public Period CreatePeriodFromMilliseconds(long milliseconds)
    {
        return Period.FromMilliseconds(milliseconds);
    }

    /// <summary>Creates a period from microseconds.</summary>
    public Period CreatePeriodFromMicroseconds(long microseconds)
    {
        return Period.FromMicroseconds(microseconds);
    }

    /// <summary>Creates a period from ticks.</summary>
    public Period CreatePeriodFromTicks(long ticks)
    {
        return Period.FromTicks(ticks);
    }

    /// <summary>Parses an ISO period.</summary>
    public Period ParsePeriod(string text)
    {
        return Period.Parse(text);
    }

    /// <summary>Creates a recurrence builder for a frequency.</summary>
    public RecurrenceBuilder CreateRecurrenceBuilder(RecurrenceFrequency frequency)
    {
        return new RecurrenceBuilder(frequency);
    }

    /// <summary>Creates a daily recurrence builder.</summary>
    public RecurrenceBuilder CreateDailyRecurrenceBuilder()
    {
        return RecurrenceBuilder.Daily();
    }

    /// <summary>Creates a weekly recurrence builder.</summary>
    public RecurrenceBuilder CreateWeeklyRecurrenceBuilder()
    {
        return RecurrenceBuilder.Weekly();
    }

    /// <summary>Creates a monthly recurrence builder.</summary>
    public RecurrenceBuilder CreateMonthlyRecurrenceBuilder()
    {
        return RecurrenceBuilder.Monthly();
    }

    /// <summary>Creates a yearly recurrence builder.</summary>
    public RecurrenceBuilder CreateYearlyRecurrenceBuilder()
    {
        return RecurrenceBuilder.Yearly();
    }

    /// <summary>Creates a recurrence day without an ordinal.</summary>
    public RecurrenceDay CreateRecurrenceDay(int dayOfWeek)
    {
        return RecurrenceDay.Create(dayOfWeek);
    }

    /// <summary>Creates an ordinal recurrence day.</summary>
    public RecurrenceDay CreateOrdinalRecurrenceDay(int dayOfWeek, int ordinal)
    {
        return RecurrenceDay.Create(dayOfWeek, ordinal);
    }

    /// <summary>Creates an empty recurrence rule for a frequency.</summary>
    public RecurrenceRule CreateRecurrenceRule(RecurrenceFrequency frequency)
    {
        return RecurrenceRule.Create(frequency);
    }

    /// <summary>Parses an RFC-style recurrence rule.</summary>
    public RecurrenceRule ParseRecurrenceRule(string text)
    {
        return RecurrenceRule.Parse(text);
    }

    /// <summary>Creates a recurrence set.</summary>
    public RecurrenceSet CreateRecurrenceSet(LocalDateTime start)
    {
        return RecurrenceSet.Create(start);
    }

    /// <summary>Creates a bounded local recurrence enumerator.</summary>
    public RecurrenceEnumerator CreateLocalRecurrenceEnumerator(RecurrenceSet set, int maximumCandidates)
    {
        return RecurrenceEnumerator.CreateLocal(set, maximumCandidates);
    }

    /// <summary>Enumerates a local recurrence set.</summary>
    public LocalDateTime[] EnumerateLocalRecurrenceSet(RecurrenceSet set, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateLocal(set, maximumCandidates);
    }

    /// <summary>Enumerates a local recurrence set through an inclusive local end value.</summary>
    public LocalDateTime[] EnumerateLocalRecurrenceSetThrough(RecurrenceSet set, LocalDateTime end, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateLocalThrough(set, end, maximumCandidates);
    }

    /// <summary>Enumerates a local recurrence rule.</summary>
    public LocalDateTime[] EnumerateLocalRecurrenceRule(LocalDateTime start, RecurrenceRule rule, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateLocal(start, rule, maximumCandidates);
    }

    /// <summary>Enumerates a local recurrence rule through an inclusive local end value.</summary>
    public LocalDateTime[] EnumerateLocalRecurrenceRuleThrough(LocalDateTime start, RecurrenceRule rule, LocalDateTime end, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateLocalRuleThrough(start, rule, end, maximumCandidates);
    }

    /// <summary>Enumerates a zoned recurrence set.</summary>
    public ZonedDateTime[] EnumerateZonedRecurrenceSet(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateZoned(set, zone, resolver, maximumCandidates);
    }

    /// <summary>Enumerates a zoned recurrence set through an inclusive instant end value.</summary>
    public ZonedDateTime[] EnumerateZonedRecurrenceSetThrough(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateZonedThrough(set, zone, resolver, end, maximumCandidates);
    }

    /// <summary>Enumerates a zoned recurrence rule.</summary>
    public ZonedDateTime[] EnumerateZonedRecurrenceRule(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateZoned(start, rule, zone, resolver, maximumCandidates);
    }

    /// <summary>Enumerates a zoned recurrence rule through an inclusive instant end value.</summary>
    public ZonedDateTime[] EnumerateZonedRecurrenceRuleThrough(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateZonedRuleThrough(start, rule, zone, resolver, end, maximumCandidates);
    }

    /// <summary>Maps local occurrences into a zone.</summary>
    public ZonedDateTime[] MapLocalOccurrencesToZoned(LocalDateTime[] localOccurrences, TimeZoneRef zone, IZoneResolver resolver)
    {
        return RecurrenceEnumerator.MapLocalToZoned(localOccurrences, zone, resolver);
    }

    /// <summary>Enumerates an exact instant recurrence.</summary>
    public Instant[] EnumerateInstantRecurrence(Instant start, RecurrenceRule rule, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateInstants(start, rule, maximumCandidates);
    }

    /// <summary>Enumerates an exact instant recurrence through an inclusive instant end value.</summary>
    public Instant[] EnumerateInstantRecurrenceThrough(Instant start, RecurrenceRule rule, Instant end, int maximumCandidates)
    {
        return RecurrenceEnumerator.EnumerateInstantsThrough(start, rule, end, maximumCandidates);
    }

    /// <summary>Creates the system clock.</summary>
    public SystemTimeClock CreateSystemTimeClock()
    {
        return SystemTimeClock.Create();
    }

    /// <summary>Creates a fixed clock.</summary>
    public FixedTimeClock CreateFixedTimeClock(Instant instant)
    {
        return FixedTimeClock.Create(instant);
    }

    /// <summary>Creates a clock shifted from a base clock by a fixed duration.</summary>
    public OffsetClock CreateOffsetClock(IClock clock, Duration offset)
    {
        return OffsetClock.Create(clock, offset);
    }

    /// <summary>Creates a zoned view over a clock.</summary>
    public ZonedClock CreateZonedClock(IClock clock, TimeZoneRef zone)
    {
        return ZonedClock.Create(clock, zone);
    }

    /// <summary>Creates a zoned system clock.</summary>
    public ZonedClock CreateSystemZonedClock(TimeZoneRef zone)
    {
        return ZonedClock.FromSystem(zone);
    }

    /// <summary>Creates a fixed zoned clock.</summary>
    public ZonedClock CreateFixedZonedClock(Instant instant, TimeZoneRef zone)
    {
        return ZonedClock.FromFixed(instant, zone);
    }

    /// <summary>Gets the UTC time zone.</summary>
    public TimeZoneRef CreateUtcTimeZone()
    {
        return TimeZoneRef.Utc();
    }

    /// <summary>Gets the local system time zone.</summary>
    public TimeZoneRef CreateLocalTimeZone()
    {
        return TimeZoneRef.Local();
    }

    /// <summary>Gets a system time zone or canonical fixed-offset zone by ID.</summary>
    public TimeZoneRef CreateTimeZoneFromSystemId(string id)
    {
        return TimeZoneRef.FromSystemId(id);
    }

    /// <summary>Creates a fixed-offset time zone backed by <see cref="TimeZoneInfo"/>.</summary>
    /// <remarks>The offset must use whole-minute precision and be in the range -14:00 through +14:00.</remarks>
    public TimeZoneRef CreateFixedOffsetTimeZone(Offset offset)
    {
        return TimeZoneRef.FromFixedOffset(offset);
    }

    /// <summary>Gets all available system time zones.</summary>
    public TimeZoneRef[] GetAvailableSystemTimeZones()
    {
        return TimeZoneRef.GetAvailableSystemZones();
    }

    /// <summary>Creates ISO week rules.</summary>
    public WeekRules CreateIsoWeekRules()
    {
        return WeekRules.Iso();
    }

    /// <summary>Creates custom week rules.</summary>
    public WeekRules CreateWeekRules(int firstDayOfWeek, int minimumDaysInFirstWeek)
    {
        return WeekRules.Create(firstDayOfWeek, minimumDaysInFirstWeek);
    }

    /// <summary>Creates a strict zone resolver.</summary>
    public ZoneResolver CreateStrictZoneResolver()
    {
        return ZoneResolver.Strict();
    }

    /// <summary>Creates an earlier-instant zone resolver.</summary>
    public ZoneResolver CreateEarlierZoneResolver()
    {
        return ZoneResolver.Earlier();
    }

    /// <summary>Creates a later-instant zone resolver.</summary>
    public ZoneResolver CreateLaterZoneResolver()
    {
        return ZoneResolver.Later();
    }

    /// <summary>Creates a compatible zone resolver.</summary>
    public ZoneResolver CreateCompatibleZoneResolver()
    {
        return ZoneResolver.Compatible();
    }

    /// <summary>Creates a gap-shifting zone resolver.</summary>
    public ZoneResolver CreateShiftForwardZoneResolver()
    {
        return ZoneResolver.ShiftForward();
    }

    /// <summary>Creates a zoned date-time from an instant.</summary>
    public ZonedDateTime CreateZonedDateTimeFromInstant(Instant instant, TimeZoneRef zone)
    {
        return ZonedDateTime.CreateFromInstant(instant, zone);
    }

    /// <summary>Creates a zoned date-time from local fields.</summary>
    public ZonedDateTime CreateZonedDateTimeFromLocal(LocalDateTime localDateTime, TimeZoneRef zone, IZoneResolver resolver)
    {
        return ZonedDateTime.CreateFromLocal(localDateTime, zone, resolver);
    }

    /// <summary>Parses an ISO zoned date-time.</summary>
    public ZonedDateTime ParseZonedDateTime(string text)
    {
        return ZonedDateTime.Parse(text);
    }
}
