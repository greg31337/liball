using System;

namespace time;

/// <summary>Creates and parses time API objects whose named construction paths are implemented internally.</summary>
public interface ITimeFactory
{
    /// <summary>Creates a zero duration.</summary>
    Duration CreateZeroDuration();

    /// <summary>Creates a duration from ticks.</summary>
    Duration CreateDurationFromTicks(long ticks);

    /// <summary>Creates a duration from microseconds.</summary>
    Duration CreateDurationFromMicroseconds(long microseconds);

    /// <summary>Creates a duration from milliseconds.</summary>
    Duration CreateDurationFromMilliseconds(long milliseconds);

    /// <summary>Creates a duration from seconds.</summary>
    Duration CreateDurationFromSeconds(long seconds);

    /// <summary>Creates a duration from minutes.</summary>
    Duration CreateDurationFromMinutes(long minutes);

    /// <summary>Creates a duration from hours.</summary>
    Duration CreateDurationFromHours(long hours);

    /// <summary>Creates a duration from exact days.</summary>
    Duration CreateDurationFromDays(long days);

    /// <summary>Creates a duration from a built-in duration.</summary>
    Duration CreateDurationFromTimeSpan(TimeSpan timeSpan);

    /// <summary>Parses an ISO duration.</summary>
    Duration ParseDuration(string text);

    /// <summary>Creates the Unix epoch instant.</summary>
    Instant CreateUnixEpoch();

    /// <summary>Reads the current system instant.</summary>
    Instant CreateCurrentInstant();

    /// <summary>Creates an instant from UTC ticks.</summary>
    Instant CreateInstantFromUtcTicks(long utcTicks);

    /// <summary>Creates an instant from Unix seconds.</summary>
    Instant CreateInstantFromUnixSeconds(long unixSeconds);

    /// <summary>Creates an instant from Unix milliseconds.</summary>
    Instant CreateInstantFromUnixMilliseconds(long unixMilliseconds);

    /// <summary>Creates an instant from Unix microseconds.</summary>
    Instant CreateInstantFromUnixMicroseconds(long unixMicroseconds);

    /// <summary>Creates an instant from a built-in offset date-time.</summary>
    Instant CreateInstantFromDateTimeOffset(DateTimeOffset dateTimeOffset);

    /// <summary>Creates an instant from a UTC date-time.</summary>
    Instant CreateInstantFromUtcDateTime(DateTime dateTime);

    /// <summary>Parses an ISO instant.</summary>
    Instant ParseInstant(string text);

    /// <summary>Creates a half-open interval from its endpoints.</summary>
    Interval CreateInterval(Instant start, Instant end);

    /// <summary>Creates an interval from a start and duration.</summary>
    Interval CreateIntervalFromStartAndDuration(Instant start, Duration duration);

    /// <summary>Parses an ISO interval.</summary>
    Interval ParseInterval(string text);

    /// <summary>Creates a local date from calendar fields.</summary>
    LocalDate CreateLocalDate(int year, int month, int day);

    /// <summary>Creates a local date from a built-in date.</summary>
    LocalDate CreateLocalDateFromDateOnly(DateOnly date);

    /// <summary>Creates a local date from a date-time.</summary>
    LocalDate CreateLocalDateFromDateTime(DateTime dateTime);

    /// <summary>Creates a local date from a day number.</summary>
    LocalDate CreateLocalDateFromDayNumber(int dayNumber);

    /// <summary>Parses an ISO local date.</summary>
    LocalDate ParseLocalDate(string text);

    /// <summary>Parses an ISO basic local date.</summary>
    LocalDate ParseIsoBasicLocalDate(string text);

    /// <summary>Creates a local time from clock fields.</summary>
    LocalTime CreateLocalTime(int hour, int minute, int second);

    /// <summary>Creates a precision-preserving local time.</summary>
    LocalTime CreatePreciseLocalTime(int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder);

    /// <summary>Creates midnight.</summary>
    LocalTime CreateMidnight();

    /// <summary>Creates noon.</summary>
    LocalTime CreateNoon();

    /// <summary>Creates a local time from a built-in time.</summary>
    LocalTime CreateLocalTimeFromTimeOnly(TimeOnly time);

    /// <summary>Creates a local time from ticks since midnight.</summary>
    LocalTime CreateLocalTimeFromTicksSinceMidnight(long ticksSinceMidnight);

    /// <summary>Parses an ISO local time.</summary>
    LocalTime ParseLocalTime(string text);

    /// <summary>Creates a local-time rollover result.</summary>
    LocalTimeRoll CreateLocalTimeRoll(long ticksSinceMidnight, int dayDelta);

    /// <summary>Creates a local date-time from date and time objects.</summary>
    LocalDateTime CreateLocalDateTime(LocalDate date, LocalTime time);

    /// <summary>Creates a local date-time from common fields.</summary>
    LocalDateTime CreateLocalDateTimeFromFields(int year, int month, int day, int hour, int minute, int second);

    /// <summary>Creates a precision-preserving local date-time.</summary>
    LocalDateTime CreatePreciseLocalDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder);

    /// <summary>Creates a local date-time from a built-in date-time.</summary>
    LocalDateTime CreateLocalDateTimeFromDateTime(DateTime dateTime);

    /// <summary>Parses an ISO local date-time.</summary>
    LocalDateTime ParseLocalDateTime(string text);

    /// <summary>Parses an ISO basic local date-time.</summary>
    LocalDateTime ParseIsoBasicLocalDateTime(string text);

    /// <summary>Creates a zero offset.</summary>
    Offset CreateZeroOffset();

    /// <summary>Creates an offset from seconds.</summary>
    Offset CreateOffsetFromSeconds(int seconds);

    /// <summary>Creates an offset from minutes.</summary>
    Offset CreateOffsetFromMinutes(int minutes);

    /// <summary>Creates an offset from hours.</summary>
    Offset CreateOffsetFromHours(int hours);

    /// <summary>Creates an offset from signed hour, minute, and second components.</summary>
    /// <remarks>Zero components are neutral. All nonzero components must have the same sign, and the combined offset must be in the range -18:00 through +18:00.</remarks>
    Offset CreateOffsetFromComponents(int hours, int minutes, int seconds);

    /// <summary>Creates an offset from a built-in duration.</summary>
    Offset CreateOffsetFromTimeSpan(TimeSpan timeSpan);

    /// <summary>Parses an ISO offset.</summary>
    Offset ParseOffset(string text);

    /// <summary>Creates an offset date-time from local fields and an offset.</summary>
    OffsetDateTime CreateOffsetDateTime(LocalDateTime localDateTime, Offset offset);

    /// <summary>Creates an offset date-time from common fields.</summary>
    OffsetDateTime CreateOffsetDateTimeFromFields(int year, int month, int day, int hour, int minute, int second, Offset offset);

    /// <summary>Creates a precision-preserving offset date-time.</summary>
    OffsetDateTime CreatePreciseOffsetDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder, Offset offset);

    /// <summary>Creates an offset date-time from a built-in value.</summary>
    OffsetDateTime CreateOffsetDateTimeFromDateTimeOffset(DateTimeOffset dateTimeOffset);

    /// <summary>Creates an offset date-time from an instant and offset.</summary>
    OffsetDateTime CreateOffsetDateTimeFromInstant(Instant instant, Offset offset);

    /// <summary>Parses an ISO offset date-time.</summary>
    OffsetDateTime ParseOffsetDateTime(string text);

    /// <summary>Creates a zero calendar period.</summary>
    Period CreateZeroPeriod();

    /// <summary>Creates a calendar period from every supported field.</summary>
    Period CreatePeriod(long years, long months, long weeks, long days, long hours, long minutes, long seconds, long milliseconds, long microseconds, long ticks);

    /// <summary>Creates a period from years.</summary>
    Period CreatePeriodFromYears(long years);

    /// <summary>Creates a period from months.</summary>
    Period CreatePeriodFromMonths(long months);

    /// <summary>Creates a period from weeks.</summary>
    Period CreatePeriodFromWeeks(long weeks);

    /// <summary>Creates a period from days.</summary>
    Period CreatePeriodFromDays(long days);

    /// <summary>Creates a period from hours.</summary>
    Period CreatePeriodFromHours(long hours);

    /// <summary>Creates a period from minutes.</summary>
    Period CreatePeriodFromMinutes(long minutes);

    /// <summary>Creates a period from seconds.</summary>
    Period CreatePeriodFromSeconds(long seconds);

    /// <summary>Creates a period from milliseconds.</summary>
    Period CreatePeriodFromMilliseconds(long milliseconds);

    /// <summary>Creates a period from microseconds.</summary>
    Period CreatePeriodFromMicroseconds(long microseconds);

    /// <summary>Creates a period from ticks.</summary>
    Period CreatePeriodFromTicks(long ticks);

    /// <summary>Parses an ISO period.</summary>
    Period ParsePeriod(string text);

    /// <summary>Creates a recurrence builder for a frequency.</summary>
    RecurrenceBuilder CreateRecurrenceBuilder(RecurrenceFrequency frequency);

    /// <summary>Creates a daily recurrence builder.</summary>
    RecurrenceBuilder CreateDailyRecurrenceBuilder();

    /// <summary>Creates a weekly recurrence builder.</summary>
    RecurrenceBuilder CreateWeeklyRecurrenceBuilder();

    /// <summary>Creates a monthly recurrence builder.</summary>
    RecurrenceBuilder CreateMonthlyRecurrenceBuilder();

    /// <summary>Creates a yearly recurrence builder.</summary>
    RecurrenceBuilder CreateYearlyRecurrenceBuilder();

    /// <summary>Creates a recurrence day without an ordinal.</summary>
    RecurrenceDay CreateRecurrenceDay(int dayOfWeek);

    /// <summary>Creates an ordinal recurrence day.</summary>
    RecurrenceDay CreateOrdinalRecurrenceDay(int dayOfWeek, int ordinal);

    /// <summary>Creates an empty recurrence rule for a frequency.</summary>
    RecurrenceRule CreateRecurrenceRule(RecurrenceFrequency frequency);

    /// <summary>Parses an RFC-style recurrence rule.</summary>
    RecurrenceRule ParseRecurrenceRule(string text);

    /// <summary>Creates a recurrence set.</summary>
    RecurrenceSet CreateRecurrenceSet(LocalDateTime start);

    /// <summary>Creates a bounded local recurrence enumerator.</summary>
    RecurrenceEnumerator CreateLocalRecurrenceEnumerator(RecurrenceSet set, int maximumCandidates);

    /// <summary>Enumerates a local recurrence set.</summary>
    LocalDateTime[] EnumerateLocalRecurrenceSet(RecurrenceSet set, int maximumCandidates);

    /// <summary>Enumerates a local recurrence set through an inclusive local end value.</summary>
    LocalDateTime[] EnumerateLocalRecurrenceSetThrough(RecurrenceSet set, LocalDateTime end, int maximumCandidates);

    /// <summary>Enumerates a local recurrence rule.</summary>
    LocalDateTime[] EnumerateLocalRecurrenceRule(LocalDateTime start, RecurrenceRule rule, int maximumCandidates);

    /// <summary>Enumerates a local recurrence rule through an inclusive local end value.</summary>
    LocalDateTime[] EnumerateLocalRecurrenceRuleThrough(LocalDateTime start, RecurrenceRule rule, LocalDateTime end, int maximumCandidates);

    /// <summary>Enumerates a zoned recurrence set.</summary>
    ZonedDateTime[] EnumerateZonedRecurrenceSet(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates);

    /// <summary>Enumerates a zoned recurrence set through an inclusive instant end value.</summary>
    ZonedDateTime[] EnumerateZonedRecurrenceSetThrough(RecurrenceSet set, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates);

    /// <summary>Enumerates a zoned recurrence rule.</summary>
    ZonedDateTime[] EnumerateZonedRecurrenceRule(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, int maximumCandidates);

    /// <summary>Enumerates a zoned recurrence rule through an inclusive instant end value.</summary>
    ZonedDateTime[] EnumerateZonedRecurrenceRuleThrough(LocalDateTime start, RecurrenceRule rule, TimeZoneRef zone, IZoneResolver resolver, Instant end, int maximumCandidates);

    /// <summary>Maps local occurrences into a zone.</summary>
    ZonedDateTime[] MapLocalOccurrencesToZoned(LocalDateTime[] localOccurrences, TimeZoneRef zone, IZoneResolver resolver);

    /// <summary>Enumerates an exact instant recurrence.</summary>
    Instant[] EnumerateInstantRecurrence(Instant start, RecurrenceRule rule, int maximumCandidates);

    /// <summary>Enumerates an exact instant recurrence through an inclusive instant end value.</summary>
    Instant[] EnumerateInstantRecurrenceThrough(Instant start, RecurrenceRule rule, Instant end, int maximumCandidates);

    /// <summary>Creates the system clock.</summary>
    SystemTimeClock CreateSystemTimeClock();

    /// <summary>Creates a fixed clock.</summary>
    FixedTimeClock CreateFixedTimeClock(Instant instant);

    /// <summary>Creates a clock shifted from a base clock by a fixed duration.</summary>
    OffsetClock CreateOffsetClock(IClock clock, Duration offset);

    /// <summary>Creates a zoned view over a clock.</summary>
    ZonedClock CreateZonedClock(IClock clock, TimeZoneRef zone);

    /// <summary>Creates a zoned system clock.</summary>
    ZonedClock CreateSystemZonedClock(TimeZoneRef zone);

    /// <summary>Creates a fixed zoned clock.</summary>
    ZonedClock CreateFixedZonedClock(Instant instant, TimeZoneRef zone);

    /// <summary>Gets the UTC time zone.</summary>
    TimeZoneRef CreateUtcTimeZone();

    /// <summary>Gets the local system time zone.</summary>
    TimeZoneRef CreateLocalTimeZone();

    /// <summary>Gets a system time zone or canonical fixed-offset zone by ID.</summary>
    TimeZoneRef CreateTimeZoneFromSystemId(string id);

    /// <summary>Creates a fixed-offset time zone backed by <see cref="TimeZoneInfo"/>.</summary>
    /// <remarks>The offset must use whole-minute precision and be in the range -14:00 through +14:00.</remarks>
    TimeZoneRef CreateFixedOffsetTimeZone(Offset offset);

    /// <summary>Gets all available system time zones.</summary>
    TimeZoneRef[] GetAvailableSystemTimeZones();

    /// <summary>Creates ISO week rules.</summary>
    WeekRules CreateIsoWeekRules();

    /// <summary>Creates custom week rules.</summary>
    WeekRules CreateWeekRules(int firstDayOfWeek, int minimumDaysInFirstWeek);

    /// <summary>Creates a strict zone resolver.</summary>
    ZoneResolver CreateStrictZoneResolver();

    /// <summary>Creates an earlier-instant zone resolver.</summary>
    ZoneResolver CreateEarlierZoneResolver();

    /// <summary>Creates a later-instant zone resolver.</summary>
    ZoneResolver CreateLaterZoneResolver();

    /// <summary>Creates a compatible zone resolver.</summary>
    ZoneResolver CreateCompatibleZoneResolver();

    /// <summary>Creates a gap-shifting zone resolver.</summary>
    ZoneResolver CreateShiftForwardZoneResolver();

    /// <summary>Creates a zoned date-time from an instant.</summary>
    ZonedDateTime CreateZonedDateTimeFromInstant(Instant instant, TimeZoneRef zone);

    /// <summary>Creates a zoned date-time from local fields.</summary>
    ZonedDateTime CreateZonedDateTimeFromLocal(LocalDateTime localDateTime, TimeZoneRef zone, IZoneResolver resolver);

    /// <summary>Parses an ISO zoned date-time.</summary>
    ZonedDateTime ParseZonedDateTime(string text);
}
