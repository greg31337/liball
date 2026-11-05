using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class PublicInterfaceDispatchTests
{
    [Test]
    public void DurationInstantAndIntervalInterfaces_DispatchEveryMember()
    {
        Duration concreteDuration = TimeApi.Factory.CreateDurationFromTicks(15000L);
        Duration otherDuration = TimeApi.Factory.CreateDurationFromTicks(5000L);
        IDuration duration = concreteDuration;
        _ = duration.GetTicks();
        _ = duration.GetTotalTicks();
        _ = duration.GetTotalMicroseconds();
        _ = duration.GetTotalMilliseconds();
        _ = duration.GetTotalSeconds();
        _ = duration.GetTotalMinutes();
        _ = duration.GetTotalHours();
        _ = duration.GetTotalDays();
        _ = duration.Add(otherDuration);
        _ = duration.Subtract(otherDuration);
        _ = duration.Negate();
        _ = duration.Abs();
        _ = duration.CompareTo(otherDuration);
        _ = duration.EqualsValue(otherDuration);
        _ = duration.Min(otherDuration);
        _ = duration.Max(otherDuration);
        _ = duration.RoundToTicks();
        _ = duration.RoundToMicroseconds();
        _ = duration.RoundToMilliseconds();
        _ = duration.RoundToSeconds();
        _ = duration.RoundToMinutes();
        _ = duration.RoundToHours();
        _ = duration.RoundToDays();
        _ = duration.TruncateToTicks();
        _ = duration.TruncateToMicroseconds();
        _ = duration.TruncateToMilliseconds();
        _ = duration.TruncateToSeconds();
        _ = duration.TruncateToMinutes();
        _ = duration.TruncateToHours();
        _ = duration.TruncateToDays();
        _ = duration.ToTimeSpan();
        Assert.That(duration.ToIsoString(), Is.EqualTo("PT0.0015S"));

        Instant concreteInstant = TimeApi.Factory.CreateUnixEpoch();
        Instant laterInstant = TimeApi.Factory.CreateInstantFromUnixSeconds(10L);
        IInstant instant = concreteInstant;
        _ = instant.GetUtcTicks();
        _ = instant.GetUnixSeconds();
        _ = instant.GetUnixMilliseconds();
        _ = instant.GetUnixMicroseconds();
        _ = instant.AddDuration(otherDuration);
        _ = instant.SubtractDuration(otherDuration);
        _ = instant.DurationUntil(laterInstant);
        _ = instant.CompareTo(laterInstant);
        _ = instant.EqualsValue(laterInstant);
        _ = instant.ToDateTimeOffset();
        _ = instant.ToUtcDateTime();
        Assert.That(instant.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z"));

        Interval concreteInterval = TimeApi.Factory.CreateInterval(concreteInstant, laterInstant);
        Interval inside = TimeApi.Factory.CreateInterval(
            TimeApi.Factory.CreateInstantFromUnixSeconds(2L),
            TimeApi.Factory.CreateInstantFromUnixSeconds(8L));
        IInterval interval = concreteInterval;
        _ = interval.GetStart();
        _ = interval.GetEnd();
        _ = interval.GetDuration();
        _ = interval.IsEmpty();
        _ = interval.ContainsInstant(concreteInstant);
        _ = interval.ContainsInterval(inside);
        _ = interval.Overlaps(inside);
        _ = interval.Abuts(inside);
        _ = interval.Intersection(inside);
        _ = interval.UnionIfConnected(inside);
        Assert.That(interval.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z/1970-01-01T00:00:10Z"));
    }

    [Test]
    public void LocalValueInterfaces_DispatchEveryMember()
    {
        LocalDate concreteDate = TimeApi.Factory.CreateLocalDate(2024, 6, 15);
        LocalDate otherDate = TimeApi.Factory.CreateLocalDate(2024, 6, 16);
        Period dayPeriod = TimeApi.Factory.CreatePeriodFromDays(1L);
        ILocalDate date = concreteDate;
        _ = date.GetYear();
        _ = date.GetMonth();
        _ = date.GetDay();
        _ = date.GetDayOfWeek();
        _ = date.GetDayOfYear();
        _ = date.GetDayNumber();
        _ = date.GetDaysInMonth();
        _ = date.GetDaysInYear();
        _ = date.IsLeapYear();
        _ = date.CompareTo(otherDate);
        _ = date.EqualsValue(otherDate);
        _ = date.ToDateOnly();
        _ = date.ToIsoString();
        _ = date.ToIsoBasicString();
        _ = date.AddDays(1L);
        _ = date.SubtractDays(1L);
        _ = date.AddWeeks(1L);
        _ = date.SubtractWeeks(1L);
        _ = date.AddMonths(1L);
        _ = date.SubtractMonths(1L);
        _ = date.AddYears(1L);
        _ = date.SubtractYears(1L);
        _ = date.AddPeriod(dayPeriod);
        _ = date.SubtractPeriod(dayPeriod);
        _ = date.StartOfMonth();
        _ = date.EndOfMonth();
        _ = date.StartOfYear();
        _ = date.EndOfYear();
        _ = date.Next(1);
        _ = date.Previous(1);
        _ = date.SameOrNext(1);
        Assert.That(date.SameOrPrevious(1), Is.Not.Null);

        LocalTime concreteTime = TimeApi.Factory.CreatePreciseLocalTime(12, 30, 45, 123, 456, 7);
        LocalTime otherTime = TimeApi.Factory.CreateLocalTime(13, 0, 0);
        ILocalTime time = concreteTime;
        _ = time.GetHour();
        _ = time.GetMinute();
        _ = time.GetSecond();
        _ = time.GetMillisecond();
        _ = time.GetMicrosecond();
        _ = time.GetTickRemainder();
        _ = time.GetTicksSinceMidnight();
        _ = time.CompareTo(otherTime);
        _ = time.EqualsValue(otherTime);
        _ = time.ToTimeOnly();
        _ = time.ToIsoString();
        _ = time.AddDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = time.SubtractDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = time.AddPeriod(TimeApi.Factory.CreatePeriodFromHours(1L));
        Assert.That(time.SubtractPeriod(TimeApi.Factory.CreatePeriodFromHours(1L)), Is.Not.Null);

        LocalDateTime concreteLocal = TimeApi.Factory.CreateLocalDateTime(concreteDate, concreteTime);
        LocalDateTime otherLocal = TimeApi.Factory.CreateLocalDateTime(otherDate, concreteTime);
        ILocalDateTime local = concreteLocal;
        _ = local.GetDate();
        _ = local.GetTime();
        _ = local.GetYear();
        _ = local.GetMonth();
        _ = local.GetDay();
        _ = local.GetHour();
        _ = local.GetMinute();
        _ = local.GetSecond();
        _ = local.GetMillisecond();
        _ = local.GetMicrosecond();
        _ = local.GetTickRemainder();
        _ = local.CompareTo(otherLocal);
        _ = local.EqualsValue(otherLocal);
        _ = local.ToDateTimeUnspecified();
        _ = local.ToIsoString();
        _ = local.ToIsoBasicString();
        _ = local.AddDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = local.SubtractDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = local.AddPeriod(dayPeriod);
        Assert.That(local.SubtractPeriod(dayPeriod), Is.Not.Null);

        ILocalTimeRoll roll = TimeApi.Factory.CreateLocalTimeRoll(concreteTime.GetTicksSinceMidnight(), 2);
        _ = roll.GetTicksSinceMidnight();
        _ = roll.GetDayDelta();
        Assert.That(roll.GetTime().EqualsValue(concreteTime), Is.True);
    }

    [Test]
    public void OffsetAndPeriodInterfaces_DispatchEveryMember()
    {
        Offset concreteOffset = TimeApi.Factory.CreateOffsetFromComponents(5, 30, 0);
        Offset otherOffset = TimeApi.Factory.CreateOffsetFromHours(1);
        IOffset offset = concreteOffset;
        _ = offset.GetSign();
        _ = offset.GetHours();
        _ = offset.GetMinutes();
        _ = offset.GetSeconds();
        _ = offset.GetTotalSeconds();
        _ = offset.GetTotalMinutes();
        _ = offset.ToTimeSpan();
        _ = offset.ToIsoString();
        _ = offset.CompareTo(otherOffset);
        Assert.That(offset.EqualsValue(concreteOffset), Is.True);

        LocalDateTime local = TimeApi.Factory.CreateLocalDateTimeFromFields(2024, 6, 15, 12, 0, 0);
        OffsetDateTime concreteOffsetDateTime = TimeApi.Factory.CreateOffsetDateTime(local, concreteOffset);
        OffsetDateTime otherOffsetDateTime = TimeApi.Factory.CreateOffsetDateTime(local.AddDuration(TimeApi.Factory.CreateDurationFromHours(1L)), concreteOffset);
        IOffsetDateTime offsetDateTime = concreteOffsetDateTime;
        _ = offsetDateTime.GetLocalDateTime();
        _ = offsetDateTime.GetOffset();
        _ = offsetDateTime.GetDate();
        _ = offsetDateTime.GetTime();
        _ = offsetDateTime.GetInstant();
        _ = offsetDateTime.AddDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = offsetDateTime.SubtractDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = offsetDateTime.AddPeriod(TimeApi.Factory.CreatePeriodFromDays(1L));
        _ = offsetDateTime.SubtractPeriod(TimeApi.Factory.CreatePeriodFromDays(1L));
        _ = offsetDateTime.CompareInstant(otherOffsetDateTime);
        _ = offsetDateTime.CompareLocal(otherOffsetDateTime);
        _ = offsetDateTime.EqualsValue(otherOffsetDateTime);
        _ = offsetDateTime.ToDateTimeOffset();
        Assert.That(offsetDateTime.ToIsoString(), Is.EqualTo("2024-06-15T12:00:00+05:30"));

        Period concretePeriod = TimeApi.Factory.CreatePeriodFromSeconds(2L);
        Period otherPeriod = TimeApi.Factory.CreatePeriodFromSeconds(1L);
        IPeriod period = concretePeriod;
        _ = period.GetYears();
        _ = period.GetMonths();
        _ = period.GetWeeks();
        _ = period.GetDays();
        _ = period.GetHours();
        _ = period.GetMinutes();
        _ = period.GetSeconds();
        _ = period.GetMilliseconds();
        _ = period.GetMicroseconds();
        _ = period.GetTicks();
        _ = period.NormalizeWeeksToDays();
        _ = period.Add(otherPeriod);
        _ = period.Subtract(otherPeriod);
        _ = period.Negate();
        _ = period.CompareTo(otherPeriod);
        _ = period.EqualsValue(otherPeriod);
        Assert.That(period.ToIsoString(), Is.Not.Empty);
    }

    [Test]
    public void RecurrenceInterfaces_DispatchEveryMember()
    {
        Instant until = TimeApi.Factory.ParseInstant("2024-12-31T00:00:00Z");
        LocalDateTime localUntil = TimeApi.Factory.ParseLocalDateTime("2024-12-31T00:00:00");
        RecurrenceDay monday = TimeApi.Factory.CreateRecurrenceDay(1);
        IRecurrenceBuilder builder = new RecurrenceBuilder(RecurrenceFrequency.Yearly);
        _ = builder.SetInterval(2);
        _ = builder.SetUntil(until);
        _ = builder.SetLocalUntil(localUntil);
        _ = builder.SetCount(3);
        _ = builder.SetWeekStart(7);
        _ = builder.AddBySecond(1);
        _ = builder.AddByMinute(2);
        _ = builder.AddByHour(3);
        _ = builder.AddByDay(monday);
        _ = builder.AddByMonthDay(4);
        _ = builder.AddByYearDay(5);
        _ = builder.AddByWeekNo(6);
        _ = builder.AddByMonth(7);
        _ = builder.AddBySetPos(1);
        RecurrenceRule concreteRule = builder.Build();

        IRecurrenceDay day = monday;
        _ = day.GetDayOfWeek();
        _ = day.GetOrdinal();
        _ = day.HasOrdinal();
        Assert.That(day.EqualsValue(monday), Is.True);

        IRecurrenceRule rule = concreteRule;
        _ = rule.GetFrequency();
        _ = rule.GetInterval();
        _ = rule.GetCount();
        _ = rule.GetUntil();
        _ = rule.GetLocalUntil();
        _ = rule.GetWeekStart();
        _ = rule.GetBySecond();
        _ = rule.GetByMinute();
        _ = rule.GetByHour();
        _ = rule.GetByDay();
        _ = rule.GetByMonthDay();
        _ = rule.GetByYearDay();
        _ = rule.GetByWeekNo();
        _ = rule.GetByMonth();
        _ = rule.GetBySetPos();
        Assert.That(rule.ToRRuleString(), Does.StartWith("FREQ=YEARLY"));

        LocalDateTime start = TimeApi.Factory.ParseLocalDateTime("2024-01-01T09:00:00");
        RecurrenceRule daily = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;COUNT=2");
        IRecurrenceSet set = TimeApi.Factory.CreateRecurrenceSet(start);
        _ = set.GetStart();
        _ = set.GetRules();
        _ = set.GetRDates();
        _ = set.GetExDates();
        _ = set.AddRule(daily);
        _ = set.AddRDate(start.AddPeriod(TimeApi.Factory.CreatePeriodFromHours(1L)));
        _ = set.AddExDate(start.AddPeriod(TimeApi.Factory.CreatePeriodFromDays(1L)));
        _ = set.ExpandBounded(10);
        Assert.That(set.ExpandThrough(start, 10).Length, Is.EqualTo(1));

        RecurrenceSet concreteSet = TimeApi.Factory.CreateRecurrenceSet(start).AddRule(daily);
        IRecurrenceEnumerator enumerator = TimeApi.Factory.CreateLocalRecurrenceEnumerator(concreteSet, 10);
        _ = enumerator.GetCount();
        Assert.That(enumerator.MoveNext(), Is.True);
        _ = enumerator.GetCurrent();
        enumerator.Reset();
        Assert.That(enumerator.ToArray().Length, Is.EqualTo(2));
    }

    [Test]
    public void ZoneClockAndZonedInterfaces_DispatchEveryMember()
    {
        TimeZoneRef concreteZone = new TimeZoneRef(TimeZoneInfo.Utc);
        Instant concreteInstant = TimeApi.Factory.ParseInstant("2024-06-15T12:00:00Z");
        LocalDateTime concreteLocal = TimeApi.Factory.ParseLocalDateTime("2024-06-15T12:00:00");
        ZoneResolver concreteResolver = TimeApi.Factory.CreateStrictZoneResolver();

        IZoneResolver resolver = concreteResolver;
        Assert.That(resolver.ResolveLocal(concreteZone, concreteLocal).GetInstant().EqualsValue(concreteInstant), Is.True);

        ITimeZoneRef zone = concreteZone;
        _ = zone.GetId();
        _ = zone.GetDisplayName();
        _ = zone.GetTimeZoneInfo();
        _ = zone.GetOffset(concreteInstant);
        _ = zone.ConvertInstant(concreteInstant);
        _ = zone.IsInvalidLocal(concreteLocal);
        _ = zone.IsAmbiguousLocal(concreteLocal);
        _ = zone.GetAmbiguousOffsets(concreteLocal);
        _ = zone.MapLocal(concreteLocal);
        _ = zone.ResolveLocal(concreteLocal, concreteResolver);
        _ = zone.GetNextTransition(concreteInstant);
        Assert.That(zone.GetPreviousTransition(concreteInstant), Is.Null);

        IWeekRules weekRules = TimeApi.Factory.CreateIsoWeekRules();
        LocalDate date = concreteLocal.GetDate();
        _ = weekRules.GetFirstDayOfWeek();
        _ = weekRules.GetMinimumDaysInFirstWeek();
        _ = weekRules.GetStartOfWeek(date);
        _ = weekRules.GetWeekYear(date);
        _ = weekRules.GetWeekOfWeekYear(date);
        Assert.That(weekRules.GetStartOfWeekYear(2024), Is.Not.Null);

        FixedTimeClock concreteClock = TimeApi.Factory.CreateFixedTimeClock(concreteInstant);
        IClock clock = concreteClock;
        Assert.That(clock.GetCurrentInstant(), Is.SameAs(concreteInstant));
        IClockZoneProvider zoneProvider = concreteClock;
        ZonedClock concreteZonedClock = zoneProvider.InZone(concreteZone);
        IZonedClock zonedClock = concreteZonedClock;
        _ = zonedClock.GetZone();
        _ = zonedClock.GetCurrentZonedDateTime();
        _ = zonedClock.GetCurrentLocalDateTime();
        _ = zonedClock.GetCurrentDate();
        Assert.That(zonedClock.GetCurrentTime().ToIsoString(), Is.EqualTo("12:00:00"));

        ZonedDateTime concreteZoned = TimeApi.Factory.CreateZonedDateTimeFromInstant(concreteInstant, concreteZone);
        IZonedDateTime zoned = concreteZoned;
        _ = zoned.GetInstant();
        _ = zoned.GetZone();
        _ = zoned.GetZoneId();
        _ = zoned.GetOffset();
        _ = zoned.GetLocalDateTime();
        _ = zoned.GetDate();
        _ = zoned.GetTime();
        _ = zoned.WithZone(concreteZone);
        _ = zoned.ToOffsetDateTime();
        _ = zoned.AddDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = zoned.SubtractDuration(TimeApi.Factory.CreateDurationFromHours(1L));
        _ = zoned.AddPeriod(TimeApi.Factory.CreatePeriodFromDays(1L), concreteResolver);
        _ = zoned.SubtractPeriod(TimeApi.Factory.CreatePeriodFromDays(1L), concreteResolver);
        _ = zoned.StartOfDay();
        _ = zoned.CompareInstant(concreteZoned);
        _ = zoned.EqualsValue(concreteZoned);
        Assert.That(zoned.ToIsoString(), Is.EqualTo("2024-06-15T12:00:00Z[UTC]"));
    }
}
