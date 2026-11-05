using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class PublicTimeFactoryTests
{
    [Test]
    public void TimeApiAndPublicConstructor_ProvideInterfaceFactory()
    {
        ITimeFactory shared = TimeApi.Factory;
        ITimeFactory constructed = new TimeFactory();

        Assert.That(shared, Is.TypeOf<TimeFactory>());
        Assert.That(constructed, Is.TypeOf<TimeFactory>());
        Assert.That(shared, Is.SameAs(TimeApi.Factory));
    }

    [Test]
    public void Factory_DispatchesEveryDurationInstantAndIntervalPath()
    {
        ITimeFactory factory = TimeApi.Factory;

        Assert.That(factory.CreateZeroDuration().GetTicks(), Is.EqualTo(0L));
        Assert.That(factory.CreateDurationFromTicks(1L).GetTicks(), Is.EqualTo(1L));
        Assert.That(factory.CreateDurationFromMicroseconds(1L).GetTicks(), Is.EqualTo(10L));
        Assert.That(factory.CreateDurationFromMilliseconds(1L).GetTicks(), Is.EqualTo(10000L));
        Assert.That(factory.CreateDurationFromSeconds(1L).GetTotalSeconds(), Is.EqualTo(1.0));
        Assert.That(factory.CreateDurationFromMinutes(1L).GetTotalMinutes(), Is.EqualTo(1.0));
        Assert.That(factory.CreateDurationFromHours(1L).GetTotalHours(), Is.EqualTo(1.0));
        Assert.That(factory.CreateDurationFromDays(1L).GetTotalDays(), Is.EqualTo(1.0));
        Assert.That(factory.CreateDurationFromTimeSpan(TimeSpan.FromSeconds(2)).GetTotalSeconds(), Is.EqualTo(2.0));
        Assert.That(factory.ParseDuration("PT3S").GetTotalSeconds(), Is.EqualTo(3.0));

        Assert.That(factory.CreateUnixEpoch().ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z"));
        Assert.That(factory.CreateCurrentInstant().GetUtcTicks(), Is.InRange(DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks));
        Assert.That(factory.CreateInstantFromUtcTicks(DateTime.UnixEpoch.Ticks).ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z"));
        Assert.That(factory.CreateInstantFromUnixSeconds(1L).ToIsoString(), Is.EqualTo("1970-01-01T00:00:01Z"));
        Assert.That(factory.CreateInstantFromUnixMilliseconds(1000L).ToIsoString(), Is.EqualTo("1970-01-01T00:00:01Z"));
        Assert.That(factory.CreateInstantFromUnixMicroseconds(1000000L).ToIsoString(), Is.EqualTo("1970-01-01T00:00:01Z"));
        Assert.That(factory.CreateInstantFromDateTimeOffset(DateTimeOffset.UnixEpoch).EqualsValue(factory.CreateUnixEpoch()), Is.True);
        Assert.That(factory.CreateInstantFromUtcDateTime(DateTime.UnixEpoch).EqualsValue(factory.CreateUnixEpoch()), Is.True);
        Assert.That(factory.ParseInstant("1970-01-01T00:00:02Z").GetUnixSeconds(), Is.EqualTo(2L));

        Instant start = factory.CreateUnixEpoch();
        Instant end = factory.CreateInstantFromUnixSeconds(60L);
        Assert.That(factory.CreateInterval(start, end).GetDuration().GetTotalSeconds(), Is.EqualTo(60.0));
        Assert.That(factory.CreateIntervalFromStartAndDuration(start, factory.CreateDurationFromMinutes(1L)).GetEnd().EqualsValue(end), Is.True);
        Assert.That(factory.ParseInterval("1970-01-01T00:00:00Z/PT1M").GetEnd().EqualsValue(end), Is.True);
    }

    [Test]
    public void Factory_DispatchesEveryLocalAndOffsetPath()
    {
        ITimeFactory factory = TimeApi.Factory;

        LocalDate date = factory.CreateLocalDate(2024, 2, 29);
        Assert.That(factory.CreateLocalDateFromDateOnly(new DateOnly(2024, 2, 29)).EqualsValue(date), Is.True);
        Assert.That(factory.CreateLocalDateFromDateTime(new DateTime(2024, 2, 29)).EqualsValue(date), Is.True);
        Assert.That(factory.CreateLocalDateFromDayNumber(date.GetDayNumber()).EqualsValue(date), Is.True);
        Assert.That(factory.ParseLocalDate("2024-02-29").EqualsValue(date), Is.True);
        Assert.That(factory.ParseIsoBasicLocalDate("20240229").EqualsValue(date), Is.True);

        LocalTime time = factory.CreateLocalTime(12, 34, 56);
        Assert.That(factory.CreatePreciseLocalTime(12, 34, 56, 123, 456, 7).ToIsoString(), Is.EqualTo("12:34:56.1234567"));
        Assert.That(factory.CreateMidnight().ToIsoString(), Is.EqualTo("00:00:00"));
        Assert.That(factory.CreateNoon().ToIsoString(), Is.EqualTo("12:00:00"));
        Assert.That(factory.CreateLocalTimeFromTimeOnly(new TimeOnly(12, 34, 56)).EqualsValue(time), Is.True);
        Assert.That(factory.CreateLocalTimeFromTicksSinceMidnight(time.GetTicksSinceMidnight()).EqualsValue(time), Is.True);
        Assert.That(factory.ParseLocalTime("12:34:56").EqualsValue(time), Is.True);
        Assert.That(factory.CreateLocalTimeRoll(time.GetTicksSinceMidnight(), -1).GetDayDelta(), Is.EqualTo(-1));

        LocalDateTime local = factory.CreateLocalDateTime(date, time);
        Assert.That(factory.CreateLocalDateTimeFromFields(2024, 2, 29, 12, 34, 56).EqualsValue(local), Is.True);
        Assert.That(factory.CreatePreciseLocalDateTime(2024, 2, 29, 12, 34, 56, 123, 456, 7).ToIsoString(), Is.EqualTo("2024-02-29T12:34:56.1234567"));
        Assert.That(factory.CreateLocalDateTimeFromDateTime(new DateTime(2024, 2, 29, 12, 34, 56)).EqualsValue(local), Is.True);
        Assert.That(factory.ParseLocalDateTime("2024-02-29T12:34:56").EqualsValue(local), Is.True);
        Assert.That(factory.ParseIsoBasicLocalDateTime("20240229T123456").EqualsValue(local), Is.True);

        Offset offset = factory.CreateOffsetFromComponents(5, 30, 0);
        Assert.That(factory.CreateZeroOffset().GetTotalSeconds(), Is.EqualTo(0));
        Assert.That(factory.CreateOffsetFromSeconds(19800).EqualsValue(offset), Is.True);
        Assert.That(factory.CreateOffsetFromMinutes(330).EqualsValue(offset), Is.True);
        Assert.That(factory.CreateOffsetFromHours(1).ToIsoString(), Is.EqualTo("+01:00"));
        Assert.That(factory.CreateOffsetFromTimeSpan(TimeSpan.FromMinutes(330)).EqualsValue(offset), Is.True);
        Assert.That(factory.ParseOffset("+05:30").EqualsValue(offset), Is.True);

        OffsetDateTime offsetDateTime = factory.CreateOffsetDateTime(local, offset);
        Assert.That(factory.CreateOffsetDateTimeFromFields(2024, 2, 29, 12, 34, 56, offset).EqualsValue(offsetDateTime), Is.True);
        Assert.That(factory.CreatePreciseOffsetDateTime(2024, 2, 29, 12, 34, 56, 123, 456, 7, offset).ToIsoString(), Is.EqualTo("2024-02-29T12:34:56.1234567+05:30"));
        Assert.That(factory.CreateOffsetDateTimeFromDateTimeOffset(new DateTimeOffset(2024, 2, 29, 12, 34, 56, TimeSpan.FromMinutes(330))).EqualsValue(offsetDateTime), Is.True);
        Assert.That(factory.CreateOffsetDateTimeFromInstant(offsetDateTime.GetInstant(), offset).EqualsValue(offsetDateTime), Is.True);
        Assert.That(factory.ParseOffsetDateTime("2024-02-29T12:34:56+05:30").EqualsValue(offsetDateTime), Is.True);
    }

    [Test]
    public void Factory_DispatchesEveryPeriodAndRecurrencePath()
    {
        ITimeFactory factory = TimeApi.Factory;

        Assert.That(factory.CreateZeroPeriod().ToIsoString(), Is.EqualTo("P0D"));
        Assert.That(factory.CreatePeriod(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 1L).GetYears(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromYears(1L).GetYears(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromMonths(1L).GetMonths(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromWeeks(1L).GetWeeks(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromDays(1L).GetDays(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromHours(1L).GetHours(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromMinutes(1L).GetMinutes(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromSeconds(1L).GetSeconds(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromMilliseconds(1L).GetMilliseconds(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromMicroseconds(1L).GetMicroseconds(), Is.EqualTo(1L));
        Assert.That(factory.CreatePeriodFromTicks(1L).GetTicks(), Is.EqualTo(1L));
        Assert.That(factory.ParsePeriod("P1Y2M3DT4H5M6S").GetMonths(), Is.EqualTo(2L));

        Assert.That(factory.CreateRecurrenceBuilder(RecurrenceFrequency.Hourly).Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Hourly));
        Assert.That(factory.CreateDailyRecurrenceBuilder().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.That(factory.CreateWeeklyRecurrenceBuilder().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Weekly));
        Assert.That(factory.CreateMonthlyRecurrenceBuilder().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Monthly));
        Assert.That(factory.CreateYearlyRecurrenceBuilder().Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Yearly));
        Assert.That(factory.CreateRecurrenceDay(1).HasOrdinal(), Is.False);
        Assert.That(factory.CreateOrdinalRecurrenceDay(5, -1).GetOrdinal(), Is.EqualTo(-1));
        Assert.That(factory.CreateRecurrenceRule(RecurrenceFrequency.Daily).ToRRuleString(), Is.EqualTo("FREQ=DAILY"));
        RecurrenceRule rule = factory.ParseRecurrenceRule("FREQ=DAILY;COUNT=2");
        LocalDateTime start = factory.ParseLocalDateTime("2024-01-01T09:00:00");
        RecurrenceSet set = factory.CreateRecurrenceSet(start).AddRule(rule);
        RecurrenceEnumerator enumerator = factory.CreateLocalRecurrenceEnumerator(set, 10);
        Assert.That(enumerator.ToArray().Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateLocalRecurrenceSet(set, 10).Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateLocalRecurrenceSetThrough(set, start, 10).Length, Is.EqualTo(1));
        Assert.That(factory.EnumerateLocalRecurrenceRule(start, rule, 10).Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateLocalRecurrenceRuleThrough(start, rule, start, 10).Length, Is.EqualTo(1));

        TimeZoneRef utc = factory.CreateUtcTimeZone();
        IZoneResolver resolver = factory.CreateStrictZoneResolver();
        Instant instantStart = factory.ParseInstant("2024-01-01T09:00:00Z");
        Instant instantEnd = factory.ParseInstant("2024-01-01T09:00:00Z");
        Assert.That(factory.EnumerateZonedRecurrenceSet(set, utc, resolver, 10).Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateZonedRecurrenceSetThrough(set, utc, resolver, instantEnd, 10).Length, Is.EqualTo(1));
        Assert.That(factory.EnumerateZonedRecurrenceRule(start, rule, utc, resolver, 10).Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateZonedRecurrenceRuleThrough(start, rule, utc, resolver, instantEnd, 10).Length, Is.EqualTo(1));
        Assert.That(factory.MapLocalOccurrencesToZoned(new LocalDateTime[] { start }, utc, resolver).Length, Is.EqualTo(1));
        Assert.That(factory.EnumerateInstantRecurrence(instantStart, rule, 10).Length, Is.EqualTo(2));
        Assert.That(factory.EnumerateInstantRecurrenceThrough(instantStart, rule, instantEnd, 10).Length, Is.EqualTo(1));
    }

    [Test]
    public void Factory_DispatchesEveryClockZoneWeekResolverAndZonedPath()
    {
        ITimeFactory factory = TimeApi.Factory;
        Instant instant = factory.ParseInstant("2024-01-01T12:00:00Z");
        TimeZoneRef utc = factory.CreateUtcTimeZone();

        Assert.That(factory.CreateSystemTimeClock().GetCurrentInstant(), Is.Not.Null);
        FixedTimeClock fixedClock = factory.CreateFixedTimeClock(instant);
        Assert.That(fixedClock.GetCurrentInstant(), Is.SameAs(instant));
        Assert.That(factory.CreateOffsetClock(fixedClock, factory.CreateDurationFromHours(1L)).GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-01-01T13:00:00Z"));
        Assert.That(factory.CreateZonedClock(fixedClock, utc).GetCurrentDate().ToIsoString(), Is.EqualTo("2024-01-01"));
        Assert.That(factory.CreateSystemZonedClock(utc).GetZone(), Is.SameAs(utc));
        Assert.That(factory.CreateFixedZonedClock(instant, utc).GetCurrentInstant(), Is.SameAs(instant));
        Assert.That(factory.CreateLocalTimeZone().GetId(), Is.EqualTo(TimeZoneInfo.Local.Id));
        Assert.That(factory.CreateTimeZoneFromSystemId(TimeZoneInfo.Utc.Id).GetId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
        Assert.That(factory.CreateFixedOffsetTimeZone(factory.CreateOffsetFromHours(2)).GetOffset(instant).GetTotalSeconds(), Is.EqualTo(7200));
        Assert.That(factory.GetAvailableSystemTimeZones(), Is.Not.Null);

        Assert.That(factory.CreateIsoWeekRules().GetFirstDayOfWeek(), Is.EqualTo(1));
        Assert.That(factory.CreateWeekRules(7, 1).GetFirstDayOfWeek(), Is.EqualTo(7));
        Assert.That(factory.CreateStrictZoneResolver(), Is.Not.Null);
        Assert.That(factory.CreateEarlierZoneResolver(), Is.Not.Null);
        Assert.That(factory.CreateLaterZoneResolver(), Is.Not.Null);
        Assert.That(factory.CreateCompatibleZoneResolver(), Is.Not.Null);
        Assert.That(factory.CreateShiftForwardZoneResolver(), Is.Not.Null);

        LocalDateTime local = factory.ParseLocalDateTime("2024-01-01T12:00:00");
        ZonedDateTime fromInstant = factory.CreateZonedDateTimeFromInstant(instant, utc);
        Assert.That(factory.CreateZonedDateTimeFromLocal(local, utc, factory.CreateStrictZoneResolver()).EqualsValue(fromInstant), Is.True);
        Assert.That(factory.ParseZonedDateTime("2024-01-01T12:00:00Z[UTC]").EqualsValue(fromInstant), Is.True);
    }

    [Test]
    public void Factory_PropagatesPublicFailureCategories()
    {
        ITimeFactory factory = TimeApi.Factory;

        Assert.Throws<ArgumentNullException>(() => factory.ParseDuration(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateLocalDate(2024, 13, 1));
        Assert.Throws<FormatException>(() => factory.ParseInstant("not-an-instant"));
        Assert.Throws<ArgumentException>(() => factory.CreateInstantFromUtcDateTime(DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Local)));
        Assert.Throws<OverflowException>(() => factory.CreateDurationFromDays(long.MaxValue));
        Assert.Throws<OverflowException>(() => factory.CreateInstantFromUnixSeconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => factory.CreateInstantFromUnixMilliseconds(long.MinValue));
        Assert.Throws<OverflowException>(() => factory.CreateInstantFromUnixMicroseconds(long.MaxValue));

        TimeZoneInfo ambiguousZoneInfo = CreateAmbiguousZone();
        TimeZoneRef ambiguousZone = new TimeZoneRef(ambiguousZoneInfo);
        LocalDateTime ambiguous = factory.CreateLocalDateTimeFromFields(2024, 11, 3, 1, 30, 0);
        Assert.Throws<InvalidOperationException>(() => factory.CreateZonedDateTimeFromLocal(ambiguous, ambiguousZone, factory.CreateStrictZoneResolver()));
    }

    [Test]
    public void PublicConstructors_ValidateAndCreateUsableObjects()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecurrenceBuilder((RecurrenceFrequency)99));
        Assert.That(new RecurrenceBuilder(RecurrenceFrequency.Daily).Build().GetFrequency(), Is.EqualTo(RecurrenceFrequency.Daily));
        Assert.Throws<ArgumentNullException>(() => new TimeZoneRef(null!));
        Assert.That(new TimeZoneRef(TimeZoneInfo.Utc).GetId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
        Assert.That(new TimeFactory().CreateUnixEpoch().GetUnixSeconds(), Is.EqualTo(0L));
    }

    static TimeZoneInfo CreateAmbiguousZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 10);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 3);
        TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "public-facade-ambiguous-zone",
            TimeSpan.FromHours(-5),
            "Public facade test zone",
            "Standard",
            "Daylight",
            new TimeZoneInfo.AdjustmentRule[] { adjustment });
    }
}
