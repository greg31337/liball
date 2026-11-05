# Universal Time Library Development Plan

## Goal

Build a universal C# library for instants, local dates, local times, local date-times, offsets, time zones, durations, calendar periods, intervals, and recurrence rules.

The implementation must use only built-in .NET APIs. It must not depend on external libraries such as Noda Time, dateutil, Quartz, or bundled time zone databases. Time zone data must come from `TimeZoneInfo` and the host operating system.

The source must follow `syntax.md`: file-scoped `namespace time;`, the permitted public API categories under `src/Api`, classes and interfaces instead of structs or records, method-based access instead of properties, no attributes, no LINQ, no local `var`, no generic type or method declarations, no `try/catch`, and no extension methods. Public behavioral classes implement contracts, named construction paths are exposed through `ITimeFactory`, and lower-level implementation types remain internal. Every public type and member has XML documentation using only the constructs allowed by `syntax.md`.

## Research Basis

Existing libraries and standards consistently separate exact timeline values from human calendar values:

- Noda Time distinguishes `Instant`, local types, `DateTimeZone`, `OffsetDateTime`, `ZonedDateTime`, `Duration`, `Period`, and `Interval`: https://www.nodatime.org/3.3.x/userguide/concepts
- Noda Time core type reference highlights exact instants, local values, zoned values, fixed durations, calendar periods, clocks, and half-open intervals: https://www.nodatime.org/3.3.x/userguide/core-types
- Java `java.time` separates `Instant`, `LocalDate`, `LocalTime`, `LocalDateTime`, `OffsetDateTime`, `ZonedDateTime`, `Duration`, and `Period`: https://docs.oracle.com/en/java/javase/26/docs/api/java.base/java/time/package-summary.html
- TC39 Temporal separates `Instant`, `PlainDate`, `PlainTime`, `PlainDateTime`, `ZonedDateTime`, and `Duration`, and makes DST-aware arithmetic explicit: https://tc39.es/proposal-temporal/docs/
- Temporal `ZonedDateTime` keeps date-time, offset, time zone, and calendar information together and supports conversion, DST-safe arithmetic, and transition inspection: https://tc39.es/proposal-temporal/docs/zoneddatetime.html
- .NET provides the allowed platform building blocks: `DateTimeOffset`, `TimeZoneInfo`, `TimeSpan`, `DateOnly`, `TimeOnly`, and `TimeProvider`: https://learn.microsoft.com/en-us/dotnet/standard/datetime/
- .NET `DateOnly` and `TimeOnly` show why date-only and time-only values should not be represented by overloaded `DateTime` or `TimeSpan` values: https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly
- RFC 5545 defines iCalendar recurrence concepts such as `FREQ`, `INTERVAL`, `COUNT`, `UNTIL`, `BYSECOND`, `BYMINUTE`, `BYHOUR`, `BYDAY`, `BYMONTHDAY`, `BYYEARDAY`, `BYWEEKNO`, `BYMONTH`, `BYSETPOS`, `WKST`, `EXDATE`, and recurrence expansion examples: https://www.rfc-editor.org/info/rfc5545/

## High Level Design

The library should be layered from low-level exact math to high-level scheduling:

1. Core constants and validation helpers.
2. Exact elapsed time with `Duration`.
3. Human calendar amounts with `Period`.
4. Local date, local time, and local date-time values.
5. Exact timeline values with `Instant`.
6. UTC offsets and offset date-times.
7. Time zone lookup, mapping, conversion, and ambiguity resolution.
8. Zoned date-times and DST-aware arithmetic.
9. Intervals and interval set operations.
10. ISO and RFC-style parsing and formatting.
11. Recurrence rules, recurrence sets, and occurrence enumeration.
12. Clock abstraction wrappers for testable "now".
13. Integration adapters to and from built-in .NET types.

Implementation should prefer simple immutable-style classes: validated factory methods construct values, operations return new instances, and callers read values through `Get...` methods. Raw constructors, constants, parser machinery, validation helpers, calendar algorithms, and recurrence expansion details remain internal unless consumers need them as part of the supported API.

## Necessary Features From Low Level To High

### Foundation

- Time unit constants: ticks per microsecond, millisecond, second, minute, hour, day, week.
- Calendar constants: months per year, days per week, ISO weekday numbering, leap-year rules.
- Range constants aligned to .NET-supported dates where possible: year 1 through year 9999 for local calendar values.
- Overflow-safe checked arithmetic helpers.
- Argument validation helpers that throw consistent exceptions.
- Text cursor/parser helpers for ISO-like formats without regex dependency.
- Comparison helpers returning negative, zero, or positive integers.
- Stable equality helpers with explicit methods instead of operators.

### Duration

- Fixed elapsed duration represented in .NET ticks, with optional helpers for whole days, hours, minutes, seconds, milliseconds, microseconds, and ticks.
- Positive, negative, and zero durations.
- Add, subtract, negate, abs, compare, min, max.
- Convert to and from `TimeSpan`.
- Total unit methods for ticks, milliseconds, seconds, minutes, hours, and days.
- ISO-8601 duration formatting for time-based values.
- Parsing for practical duration strings such as `PT1H30M`, `P3D`, and signed variants.
- Rounding/truncation to supported units.

### Period

- Calendar period fields: years, months, weeks, days, hours, minutes, seconds, milliseconds, microseconds, ticks.
- Positive, negative, and mixed-field periods.
- Normalize weeks into days when requested.
- Add, subtract, negate, compare by field tuple only when exact comparison is meaningful.
- ISO-8601 period formatting and parsing.
- Application to local date, local time, local date-time, and zoned date-time with explicit calendar arithmetic.
- End-of-month behavior: constrain to last valid day by default, with a reject option later if needed.

### Local Date

- `LocalDate` value with year, month, and day.
- Construction from fields, `DateOnly`, `DateTime`, day number, and ISO text.
- Validation of valid year/month/day and leap-day handling.
- Accessors for year, month, day, day-of-week, day-of-year, days-in-month, days-in-year.
- Add/subtract days, weeks, months, years, and periods.
- Start of month, end of month, start of year, end of year.
- Next/previous/same-or-next weekday.
- ISO week-year and week-of-year helpers.
- Comparisons and equality.
- Formatting to ISO date text.

### Local Time

- `LocalTime` value with hour, minute, second, millisecond, microsecond, and tick remainder.
- Construction from fields, `TimeOnly`, ticks since midnight, and ISO text.
- Validation of clock ranges.
- Accessors for clock fields and ticks since midnight.
- Add/subtract durations with day rollover result.
- Compare and equality.
- Formatting to ISO time text.

### Local Date-Time

- `LocalDateTime` value combining `LocalDate` and `LocalTime`.
- Construction from date and time, fields, `DateTime`, and ISO text.
- Accessors for date, time, and individual fields.
- Add/subtract durations and periods with explicit ordering.
- Compare and equality.
- Convert to `DateTime` with `DateTimeKind.Unspecified`.
- Formatting to ISO local date-time text.

### Instant

- Exact timeline value represented by UTC ticks, based on built-in `DateTimeOffset` range.
- Construction from Unix seconds, milliseconds, microseconds, ticks, `DateTimeOffset`, UTC `DateTime`, and ISO instant text.
- Accessors for Unix time and UTC ticks.
- Add/subtract durations.
- Difference between two instants as a duration.
- Compare and equality.
- Convert to `DateTimeOffset` and UTC `DateTime`.
- Formatting as RFC 3339/ISO instant text with `Z`.

### Offset

- UTC offset value in seconds or ticks.
- Construction from hours/minutes/seconds, `TimeSpan`, and text such as `Z`, `+05:30`, and `-08:00`.
- Validation of realistic offset range, initially from -18:00 through +18:00.
- Accessors for sign, hours, minutes, seconds, total seconds, and `TimeSpan`.
- Compare and equality.
- Formatting as `Z` or signed offset text.

### Offset Date-Time

- `OffsetDateTime` value combining `LocalDateTime` and `Offset`.
- Construction from local date-time and offset, `DateTimeOffset`, fields, and text.
- Conversion to and from `Instant`.
- Add/subtract exact durations by instant semantics.
- Add/subtract calendar periods by local semantics while preserving offset.
- Compare by instant and compare by local fields.
- Formatting as ISO offset date-time text.

### Time Zones

- Time zone lookup by system ID using `TimeZoneInfo`.
- Built-in UTC zone helper.
- Local system zone helper.
- Enumerate available system zones.
- Convert `Instant` to local date-time and offset in a zone.
- Map `LocalDateTime` to possible instants in a zone.
- Detect normal, ambiguous, and invalid local times.
- Resolve ambiguous local times with explicit modes: earlier, later, strict, and compatible.
- Resolve invalid local times with explicit modes: shift-forward, strict, and compatible.
- Get offset for instant.
- Get next and previous offset transition when the platform exposes enough adjustment-rule data.
- Format zone IDs and fixed offsets.
- Document that IANA versus Windows ID behavior depends on the OS and .NET runtime because no external tzdb is bundled.

### Zoned Date-Time

- `ZonedDateTime` value combining `Instant`, zone ID, offset, and local date-time.
- Construction from instant and zone.
- Construction from local date-time, zone, and resolver.
- Accessors for instant, zone ID, offset, local date-time, local date, and local time.
- Convert to another zone preserving instant.
- Convert to offset date-time.
- Add/subtract exact durations by timeline semantics.
- Add/subtract periods by calendar semantics with zone resolution.
- Start of day in zone, including skipped-midnight behavior.
- Compare by instant and equality by full identity.
- Formatting as ISO/RFC-style text with offset and bracketed zone ID.

### Calendar And Week Rules

- ISO Gregorian calendar as the default and only first-class calendar for the first version.
- Leap-year and days-in-month algorithms.
- Day-number conversion.
- ISO weekday values Monday through Sunday.
- ISO week-year calculation.
- Week rules with configurable first day of week and minimum days in first week.
- Culture-aware formatting can be deferred; invariant formatting is required first.

### Intervals

- Half-open `Interval` with start inclusive and end exclusive.
- Construction from two instants.
- Empty interval support.
- Contains instant.
- Contains interval.
- Overlaps, abuts, intersection, union when connected.
- Duration of interval.
- Formatting and parsing of start/end or start/duration forms.

### Recurrence

- `RecurrenceRule` with frequency, interval, count, until, week start, and BYxxx filters.
- Frequencies: secondly, minutely, hourly, daily, weekly, monthly, yearly.
- Range bounding with count and until.
- Infinite recurrence represented by lazy enumeration with caller-specified maximum count or end instant/date.
- BYSECOND, BYMINUTE, BYHOUR.
- BYDAY with optional ordinal for monthly/yearly rules.
- BYMONTHDAY with positive and negative month-day positions.
- BYYEARDAY with positive and negative year-day positions.
- BYWEEKNO for yearly rules.
- BYMONTH.
- BYSETPOS final positional filtering.
- WKST support for weekly and week-year calculations.
- DTSTART inclusion rules.
- EXDATE exclusions.
- RDATE inclusions.
- Recurrence set merge, de-duplicate, sort, and limit.
- Local recurrence expansion for floating/local schedules.
- Zoned recurrence expansion preserving wall-clock intent across DST.
- Instant recurrence expansion for exact elapsed schedules.
- RFC 5545-compatible RRULE parser and formatter for the supported subset.
- Clear unsupported-feature errors for full iCalendar constructs outside the library scope.

### Parsing And Formatting

- ISO local date: `YYYY-MM-DD`.
- ISO compact date for recurrence interop: `YYYYMMDD`.
- ISO local time with optional fractional seconds.
- ISO local date-time.
- ISO offset text: `Z`, `+HH:MM`, `-HH:MM`, optionally seconds.
- RFC 3339 instant and offset date-time text.
- Zoned date-time text: local date-time, offset, and bracketed zone ID.
- ISO duration and period text.
- Interval text.
- RRULE text.
- Invariant culture formatting by default.
- Parse methods that throw.
- Try-parse style methods can be added only if the simplified syntax allows an acceptable non-`out` result type; otherwise use parser result classes.

### Clocks And Testability

- System clock wrapper using `TimeProvider.System` or `DateTimeOffset.UtcNow`.
- Fixed clock for tests.
- Offset clock for tests.
- Zoned clock helper for "today", "now", and current local values in a zone.
- Depend on `IClock` for current-instant access and use concrete clock classes for the built-in implementations.

### Built-In .NET Interop

- Convert to and from `DateOnly`.
- Convert to and from `TimeOnly`.
- Convert to and from `DateTime` with explicit kind behavior.
- Convert to and from `DateTimeOffset`.
- Convert to and from `TimeSpan`.
- Convert to and from `TimeZoneInfo`.
- Avoid exposing external-library types.

### Error Handling

- Consistent `ArgumentException`, `ArgumentOutOfRangeException`, `FormatException`, and `InvalidOperationException` usage.
- No `try/catch` in source due to syntax policy.
- Parser methods should validate up front and throw deterministic messages.
- Resolver failures should identify invalid local time, ambiguous local time, missing time zone, or unsupported platform transition lookup.

### Testing

- Syntax conformance tests must continue to pass.
- Unit tests for every value class constructor/factory and validation path.
- Leap-year, month-end, day-of-week, day-of-year, and ISO week tests.
- Duration and period arithmetic tests.
- Instant conversion tests around Unix epoch and range edges.
- Offset parse/format tests.
- Time zone mapping tests for normal, ambiguous, and invalid local times using known `TimeZoneInfo` zones available on the test host.
- Zoned date-time DST arithmetic tests.
- Interval operation tests.
- RRULE parser/formatter tests.
- Recurrence expansion tests copied from RFC 5545 examples where feasible.
- Cross-check tests against built-in .NET APIs for supported behavior.


## Completion Tasks

- [DONE] T001: Complete the public Duration contract with total-tick, total-microsecond, total-millisecond, total-second, total-minute, total-hour, and total-day conversions, minimum and maximum selection, and explicit rounding and truncation to each supported fixed unit; update the implementation contract and cover signs, ties, overflow, and range edges.
- [DONE] T002: Complete the public Period contract with comparison for exactly comparable field combinations and explicit rejection of calendar-dependent comparisons, and cover equal, ordered, incomparable, mixed-sign, and overflow cases.
- [DONE] T003: Add public LocalTime period addition and subtraction that return LocalTimeRoll, include week/day and sub-day fields in the rollover, reject year/month fields that require a date, update the implementation contract, and cover positive, negative, mixed-field, multi-day, and overflow behavior.
- [DONE] T004: Add compact ISO basic date and date-time formatting and parsing entry points needed by recurrence interchange, while preserving the existing extended invariant forms, and cover valid forms, malformed text, leap dates, fractional precision, and supported range boundaries.
- [DONE] T005: Add a public offset factory path from signed hour, minute, and second components, define consistent sign and component validation, and cover the full -18:00 through +18:00 range including second-precision offsets and invalid mixed-sign inputs.
- [DONE] T006: Add a public fixed-offset time-zone construction path backed only by TimeZoneInfo, expose stable fixed-zone identity and formatting behavior, and cover UTC, positive and negative offsets, second-precision limitations imposed by TimeZoneInfo, conversion, mapping, and transition-free behavior.
- [DONE] T007: Add zoned period addition and subtraction operations that accept an explicit IZoneResolver instead of always using compatible resolution, update IZonedDateTime, and cover normal times, gaps, overlaps, month-end constraining, and strict, earlier, later, compatible, and shift-forward policies.
- [DONE] T008: Add an offset clock implementing IClock and zone-view creation, with factory construction from a base IClock and Duration, and cover positive and negative offsets, overflow, custom clocks, fixed clocks, system clocks, and zoned views.
- [DONE] T009: Complete RecurrenceBuilder so callers can construct every supported frequency and every BYSECOND, BYMINUTE, BYHOUR, BYDAY, BYMONTHDAY, BYYEARDAY, BYWEEKNO, BYMONTH, and BYSETPOS filter without parsing text; update its contract and cover chaining, snapshots, duplicates, ranges, and invalid combinations.
- [DONE] T010: Complete RecurrenceRule validation and representation for RFC 5545 frequency/filter compatibility, COUNT versus UNTIL exclusivity, ordinal BYDAY restrictions, and local versus UTC UNTIL values appropriate to floating, zoned, and instant recurrence starts; update parser, formatter, builder, accessors, and factory paths and cover every accepted and rejected combination.
- [DONE] T011: Replace recurrence filtering over one stepped DTSTART-shaped candidate with RFC 5545 expand/limit candidate generation for each frequency, apply BYxxx rules in the required order, apply BYSETPOS within each frequency interval, and cover secondly through yearly rules including multiple occurrences per interval, negative positions, leap years, week boundaries, and month-end omissions.
- [DONE] T012: Complete recurrence-set semantics for DTSTART, multiple RRULE values, RDATE, EXDATE, de-duplication, ordering, COUNT, inclusive UNTIL, caller-specified local end bounds, caller-specified instant end bounds, and safety limits; cover empty sets, overlapping sources, exclusions of DTSTART, and bounded infinite rules.
- [DONE] T013: Complete floating, zoned, and exact-timeline recurrence enumeration on top of the corrected recurrence engine, preserving wall-clock intent through gaps and overlaps for zoned schedules and fixed elapsed steps for instant schedules, and cover all resolver modes, DST transitions, pre-epoch instants, and supported range edges.
- [DONE] T014: Make recurrence parsing and formatting explicitly reject unsupported iCalendar properties and value forms with deterministic FormatException or ArgumentException failures, preserve every supported RRULE part on round trip, and cover the normative and illustrative recurrence examples in RFC 5545.
- [DONE] T015: Add public-facade tests that exercise TimeApi.Factory, every ITimeFactory method, every public constructor, and every public interface dispatch path so consumers are tested without relying on internal factory methods; include success, null, range, format, invalid-operation, and overflow behavior.
- [DONE] T016: Complete behavioral coverage for the public duration, period, local date, local time, local date-time, instant, offset, offset date-time, week-rule, interval, and clock surfaces, including all untested branches, .NET interop, round trips, arithmetic boundaries, and immutable-style result independence.
- [DONE] T017: Complete behavioral coverage for the public time-zone, resolver, and zoned date-time surfaces using dynamically discovered TimeZoneInfo zones or test-created custom zones, including normal mappings, non-hour gaps and overlaps, ambiguous-offset ordering, transition searches, fixed zones, platform ID behavior, and range edges.
- [DONE] T018: Complete behavioral coverage for the public recurrence rule, builder, day, set, enumerator, and factory surfaces, including all frequencies, every BYxxx expansion interaction, DTSTART/RDATE/EXDATE semantics, COUNT and UNTIL bounds, stateful enumeration, defensive array copies, RFC 5545 examples, and DST-aware zoned schedules.
- [DONE] T019: Add cross-check tests against DateOnly, TimeOnly, DateTime, DateTimeOffset, TimeSpan, TimeZoneInfo, ISOWeek, and TimeProvider for all overlapping public behavior, and close every remaining reachable line and branch coverage gap under src/Api without adding syntax-policy or source-inspection tests.
