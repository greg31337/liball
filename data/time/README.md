# time

Small date and time primitives for code that needs explicit choices about elapsed time, calendar time, offsets, zones, intervals, clocks, and recurrences.

The examples use the shared public factory:

```csharp
ITimeFactory timeFactory = TimeApi.Factory;
```

## Choosing Types

Use `Instant` for a point on the UTC timeline. It is the right value to store for events that already happened or for cross-system ordering.

```csharp
Instant receivedAt = timeFactory.ParseInstant("2024-02-29T12:34:56Z");
Instant retryAt = receivedAt.AddDuration(timeFactory.CreateDurationFromMinutes(15L));
```

Use `LocalDate` for a calendar date without a time or time zone, such as a birthday, invoice date, or business day.

```csharp
LocalDate invoiceDate = timeFactory.CreateLocalDate(2024, 2, 29);
LocalDate dueDate = invoiceDate.AddDays(30L);
```

Use `LocalTime` for a wall-clock time without a date or zone, such as a daily opening time.

```csharp
LocalTime opensAt = timeFactory.CreateLocalTime(9, 0, 0);
LocalTime closesAt = opensAt.AddDuration(timeFactory.CreateDurationFromHours(8L)).GetTime();
```

Use `LocalDateTime` for a floating local schedule value before choosing a time zone, such as "every day at 9 AM".

```csharp
LocalDateTime localStart = timeFactory.CreateLocalDateTimeFromFields(2024, 3, 9, 9, 0, 0);
LocalDateTime nextLocal = localStart.AddPeriod(timeFactory.CreatePeriodFromDays(1L));
```

Use `OffsetDateTime` when the offset is part of the data but no time-zone rules are needed.

```csharp
Offset offset = timeFactory.CreateOffsetFromHours(-5);
OffsetDateTime timestamp = timeFactory.CreateOffsetDateTime(localStart, offset);
Instant instant = timestamp.GetInstant();
```

Use `ZonedDateTime` when a named time zone and daylight-saving rules matter.

```csharp
TimeZoneRef zone = timeFactory.CreateTimeZoneFromSystemId("America/New_York");
ZonedDateTime meeting = timeFactory.CreateZonedDateTimeFromLocal(localStart, zone, timeFactory.CreateCompatibleZoneResolver());
ZonedDateTime nextMeeting = meeting.AddPeriod(timeFactory.CreatePeriodFromDays(1L), timeFactory.CreateCompatibleZoneResolver());
```

Use `Duration` for exact elapsed time. Adding one day as a duration means exactly 24 hours.

```csharp
Instant started = timeFactory.ParseInstant("2024-03-09T17:00:00Z");
Instant exactlyOneDayLater = started.AddDuration(timeFactory.CreateDurationFromDays(1L));
```

Use `Period` for calendar arithmetic. Adding one day as a period preserves local calendar intent through daylight-saving transitions when used with `ZonedDateTime`.

```csharp
Period oneCalendarDay = timeFactory.CreatePeriodFromDays(1L);
ZonedDateTime sameWallClockTomorrow = meeting.AddPeriod(oneCalendarDay, timeFactory.CreateCompatibleZoneResolver());
```

Use `Interval` for half-open ranges: the start is included and the end is excluded.

```csharp
Interval window = timeFactory.ParseInterval("2024-02-29T12:00:00Z/2024-02-29T13:00:00Z");
bool inside = window.ContainsInstant(timeFactory.ParseInstant("2024-02-29T12:30:00Z"));
```

## Recurrences

Use `RecurrenceRule` when parsing or formatting RRULE text directly.

```csharp
RecurrenceRule rule = timeFactory.ParseRecurrenceRule("FREQ=DAILY;COUNT=3");
string text = rule.ToRRuleString();
```

Use `RecurrenceBuilder` for common rules built in code.

```csharp
RecurrenceRule weekly = timeFactory
    .CreateWeeklyRecurrenceBuilder()
    .SetInterval(2)
    .SetCount(4)
    .AddByDay(timeFactory.CreateRecurrenceDay(1))
    .AddByHour(9)
    .Build();
```

Use `RecurrenceSet` when a schedule has one or more rules plus explicit `RDATE` or `EXDATE` values.

```csharp
RecurrenceSet set = timeFactory.CreateRecurrenceSet(timeFactory.CreateLocalDateTimeFromFields(2024, 1, 1, 9, 0, 0));
set.AddRule(rule);
set.AddRDate(timeFactory.CreateLocalDateTimeFromFields(2024, 1, 10, 9, 0, 0));
set.AddExDate(timeFactory.CreateLocalDateTimeFromFields(2024, 1, 2, 9, 0, 0));
LocalDateTime[] localOccurrences = set.ExpandBounded(10);
```

Use `ITimeFactory.EnumerateLocalRecurrenceSet` for floating schedules that have no zone yet.

```csharp
LocalDateTime[] floating = timeFactory.EnumerateLocalRecurrenceSet(set, 10);
```

Use `ITimeFactory.EnumerateZonedRecurrenceSet` when wall-clock intent matters through time-zone transitions.

```csharp
ZonedDateTime[] zoned = timeFactory.EnumerateZonedRecurrenceSet(set, zone, timeFactory.CreateCompatibleZoneResolver(), 10);
```

Use `ITimeFactory.EnumerateInstantRecurrence` for exact elapsed schedules on the UTC timeline.

```csharp
Instant[] exact = timeFactory.EnumerateInstantRecurrence(
    timeFactory.ParseInstant("2024-01-01T00:00:00Z"),
    timeFactory.ParseRecurrenceRule("FREQ=HOURLY;INTERVAL=6;COUNT=4"),
    10);
```

## Clocks

Use `SystemTimeClock` for production current time and `FixedTimeClock` for simple deterministic tests. `OffsetClock` shifts any clock by a fixed duration. Depend on `IClock` in application code so tests can supply a purpose-built clock. Wrap any zone-capable implementation with `ZonedClock` when code needs current zoned, local date, or local time values.

```csharp
IClock systemClock = timeFactory.CreateSystemTimeClock();
IClock fixedClock = timeFactory.CreateFixedTimeClock(timeFactory.ParseInstant("2024-02-29T12:00:00Z"));
OffsetClock shiftedClock = timeFactory.CreateOffsetClock(fixedClock, timeFactory.CreateDurationFromHours(2L));
ZonedClock clock = shiftedClock.InZone(timeFactory.CreateUtcTimeZone());
LocalDate today = clock.GetCurrentDate();
```

Use `IZoneResolver` when an application needs a custom policy for ambiguous or invalid local times. The resolver creation methods on `ITimeFactory` provide the common policies and remain the simplest choice for most code.

## Platform Time Zone Limitations

`TimeZoneRef` is a small wrapper around built-in .NET `TimeZoneInfo`. It does not ship its own time-zone database.

Time-zone IDs are platform dependent. Some systems expose IANA IDs such as `America/New_York`; others expose Windows IDs such as `Eastern Standard Time`. Use `ITimeFactory.GetAvailableSystemTimeZones()` or deployment-specific configuration when choosing IDs.

Historical time-zone data is limited to the rules provided by the operating system and .NET runtime. If your application needs audited historical civil-time behavior, compare results on each supported deployment platform.

Transition lookup uses bounded searches around the supplied instant. `GetNextTransition` and `GetPreviousTransition` are intended for nearby transition discovery, not exhaustive long-range time-zone database analysis.
