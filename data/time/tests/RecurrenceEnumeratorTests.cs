using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceEnumeratorTests
{
    [Test]
    public void EnumerateLocal_FromSetReturnsSortedLocalOccurrences()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(CreateRule(count: 2));
        set.AddRDate(LocalDateTime.Create(2024, 1, 4, 9, 0, 0));

        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(set, 10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 2, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 4, 9, 0, 0);
    }

    [Test]
    public void EnumerateLocal_FromStartAndRuleBuildsSingleRuleLocalSchedule()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(2024, 1, 1, 9, 0, 0),
            CreateRule(interval: 2, count: 3),
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        AssertFields(result[0], 2024, 1, 1, 9, 0, 0);
        AssertFields(result[1], 2024, 1, 3, 9, 0, 0);
        AssertFields(result[2], 2024, 1, 5, 9, 0, 0);
    }

    [Test]
    public void PublicLocalEnumeration_ExpandsParsedRuleCandidatesWithinAnInterval()
    {
        ITimeFactory factory = TimeApi.Factory;
        LocalDateTime start = factory.CreateLocalDateTimeFromFields(2024, 1, 1, 9, 0, 20);
        RecurrenceRule rule = factory.ParseRecurrenceRule("FREQ=MINUTELY;COUNT=4;BYSECOND=10,40");

        LocalDateTime[] result = factory.EnumerateLocalRecurrenceRule(start, rule, 10);

        Assert.That(result.Length, Is.EqualTo(4));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T09:00:20"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-01-01T09:00:40"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("2024-01-01T09:01:10"));
        Assert.That(result[3].ToIsoString(), Is.EqualTo("2024-01-01T09:01:40"));
    }

    [Test]
    public void EnumerateLocal_CombinesParsedYearlyLeapDayFiltersWithInterval()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;INTERVAL=4;COUNT=2;BYMONTH=2;BYMONTHDAY=29;BYDAY=TU");

        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(2024, 2, 29, 9, 30, 0),
            rule,
            20);

        Assert.That(result.Length, Is.EqualTo(2));
        AssertFields(result[0], 2024, 2, 29, 9, 30, 0);
        AssertFields(result[1], 2028, 2, 29, 9, 30, 0);
    }

    [Test]
    public void PublicLocalEnumeration_AppliesInclusiveCallerEndToSetsAndRules()
    {
        ITimeFactory factory = TimeApi.Factory;
        LocalDateTime start = factory.CreateLocalDateTimeFromFields(2024, 1, 1, 9, 0, 0);
        LocalDateTime end = factory.CreateLocalDateTimeFromFields(2024, 1, 3, 9, 0, 0);
        RecurrenceRule rule = factory.ParseRecurrenceRule("FREQ=DAILY");
        RecurrenceSet set = factory.CreateRecurrenceSet(start).AddRule(rule);

        LocalDateTime[] setResult = factory.EnumerateLocalRecurrenceSetThrough(set, end, 20);
        LocalDateTime[] ruleResult = factory.EnumerateLocalRecurrenceRuleThrough(start, rule, end, 20);

        Assert.That(setResult.Length, Is.EqualTo(3));
        Assert.That(ruleResult.Length, Is.EqualTo(3));
        Assert.That(setResult[2].ToIsoString(), Is.EqualTo("2024-01-03T09:00:00"));
        Assert.That(ruleResult[2].ToIsoString(), Is.EqualTo("2024-01-03T09:00:00"));
    }

    [Test]
    public void FloatingEnumeration_PreservesUnresolvedWallClockFieldsAcrossDstDate()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(2024, 3, 9, 2, 30, 0),
            RecurrenceRule.Parse("FREQ=DAILY;COUNT=3"),
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-03-09T02:30:00"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-03-10T02:30:00"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("2024-03-11T02:30:00"));
    }

    [Test]
    public void FloatingWeeklyEnumeration_HandlesMinimumRangePartialWeek()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(1, 1, 1, 0, 0, 0),
            RecurrenceRule.Parse("FREQ=WEEKLY;COUNT=2;WKST=SU"),
            10);

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("0001-01-01T00:00:00"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("0001-01-08T00:00:00"));
    }

    [Test]
    public void FloatingWeeklyEnumeration_StopsAtMaximumRangeWithoutCreatingInvalidWeekdays()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(9999, 12, 31, 23, 59, 59),
            RecurrenceRule.Parse("FREQ=WEEKLY;COUNT=2;BYDAY=SU"),
            10);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("9999-12-31T23:59:59"));
    }

    [Test]
    public void Enumerator_MoveNextTraversesOccurrencesAndResetRestarts()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(CreateRule(count: 2));
        RecurrenceEnumerator enumerator = RecurrenceEnumerator.CreateLocal(set, 10);

        Assert.That(enumerator.GetCount(), Is.EqualTo(2));
        Assert.Throws<InvalidOperationException>(() => enumerator.GetCurrent());
        Assert.That(enumerator.MoveNext(), Is.True);
        AssertFields(enumerator.GetCurrent(), 2024, 1, 1, 9, 0, 0);
        Assert.That(enumerator.MoveNext(), Is.True);
        AssertFields(enumerator.GetCurrent(), 2024, 1, 2, 9, 0, 0);
        Assert.That(enumerator.MoveNext(), Is.False);
        Assert.Throws<InvalidOperationException>(() => enumerator.GetCurrent());

        enumerator.Reset();

        Assert.That(enumerator.MoveNext(), Is.True);
        AssertFields(enumerator.GetCurrent(), 2024, 1, 1, 9, 0, 0);
    }

    [Test]
    public void Enumerator_SnapshotsSetAndArrays()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        set.AddRule(CreateRule(count: 1));
        RecurrenceEnumerator enumerator = new RecurrenceEnumerator(set, 10);

        set.AddRDate(LocalDateTime.Create(2024, 1, 5, 9, 0, 0));
        LocalDateTime[] array = enumerator.ToArray();
        array[0] = LocalDateTime.Create(2024, 1, 6, 9, 0, 0);

        Assert.That(enumerator.GetCount(), Is.EqualTo(1));
        AssertFields(enumerator.ToArray()[0], 2024, 1, 1, 9, 0, 0);
    }

    [Test]
    public void EnumerateZoned_PreservesWallClockAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 3, 9, 12, 0, 0));
        set.AddRule(CreateRule(count: 3));

        ZonedDateTime[] result = RecurrenceEnumerator.EnumerateZoned(set, zone, ZoneResolver.Compatible(), 10);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-09T12:00:00"));
        Assert.That(result[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T12:00:00"));
        Assert.That(result[2].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-11T12:00:00"));
        Assert.That(result[0].GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(result[1].GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(result[0].GetInstant().ToIsoString(), Is.EqualTo("2024-03-09T17:00:00Z"));
        Assert.That(result[1].GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T16:00:00Z"));
    }

    [Test]
    public void EnumerateZoned_ShiftsInvalidLocalTimesWithCompatibleResolver()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime start = LocalDateTime.Create(2024, 3, 9, 2, 30, 0);
        RecurrenceRule rule = CreateRule(count: 2);

        ZonedDateTime[] result = RecurrenceEnumerator.EnumerateZoned(
            start,
            rule,
            zone,
            ZoneResolver.Compatible(),
            10);
        ZonedDateTime[] shifted = RecurrenceEnumerator.EnumerateZoned(
            start,
            rule,
            zone,
            ZoneResolver.ShiftForward(),
            10);

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-09T02:30:00"));
        Assert.That(result[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
        Assert.That(result[1].GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(result[1].GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.That(shifted[1].GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.Throws<InvalidOperationException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Strict(), 10));
        Assert.Throws<InvalidOperationException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Earlier(), 10));
        Assert.Throws<InvalidOperationException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Later(), 10));
    }

    [Test]
    public void EnumerateZoned_UsesResolverForAmbiguousLocalTimes()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime start = LocalDateTime.Create(2024, 11, 2, 1, 30, 0);
        RecurrenceRule rule = CreateRule(count: 2);

        ZonedDateTime[] earlier = RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Earlier(), 10);
        ZonedDateTime[] later = RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Later(), 10);
        ZonedDateTime[] compatible = RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Compatible(), 10);

        Assert.That(earlier[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T01:30:00"));
        Assert.That(later[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T01:30:00"));
        Assert.That(earlier[1].GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(later[1].GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(earlier[1].GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.That(later[1].GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.That(compatible[1].GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.Throws<InvalidOperationException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.Strict(), 10));
        Assert.Throws<InvalidOperationException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, ZoneResolver.ShiftForward(), 10));
    }

    [Test]
    public void EnumerateInstants_UsesExactElapsedDailyDurationAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant start = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 12, 0, 0), zone, ZoneResolver.Strict()).GetInstant();

        Instant[] result = RecurrenceEnumerator.EnumerateInstants(start, CreateRule(count: 2), 10);
        ZonedDateTime secondInZone = ZonedDateTime.CreateFromInstant(result[1], zone);

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-03-09T17:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-03-10T17:00:00Z"));
        Assert.That(secondInZone.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T13:00:00"));
    }

    [Test]
    public void EnumerateInstants_UsesCorrectedUtcExpansionBeforeUnixEpoch()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("1969-12-31T23:59:20Z"),
            RecurrenceRule.Parse("FREQ=MINUTELY;COUNT=4;BYSECOND=10,40"),
            10);

        Assert.That(result.Length, Is.EqualTo(4));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("1969-12-31T23:59:20Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("1969-12-31T23:59:40Z"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("1970-01-01T00:00:10Z"));
        Assert.That(result[3].ToIsoString(), Is.EqualTo("1970-01-01T00:00:40Z"));
    }

    [Test]
    public void EnumerateInstants_StopsCleanlyAtMaximumSupportedInstant()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("9999-12-31T23:59:59.9999999Z"),
            RecurrenceRule.Parse("FREQ=SECONDLY;COUNT=3"),
            10);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("9999-12-31T23:59:59.9999999Z"));
    }

    [Test]
    public void EnumerateInstants_HonorsCountAndInterval()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("2024-01-01T00:00:00Z"),
            CreateRule(frequency: RecurrenceFrequency.Hourly, interval: 6, count: 3),
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-01-01T06:00:00Z"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("2024-01-01T12:00:00Z"));
    }

    [Test]
    public void EnumerateInstants_StopsAtInclusiveUntil()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("2024-01-01T00:00:00Z"),
            CreateRule(until: Instant.Parse("2024-01-03T00:00:00Z")),
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-01-02T00:00:00Z"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("2024-01-03T00:00:00Z"));
    }

    [Test]
    public void PublicEnumerationPaths_RequireUntilTypeMatchingStartModel()
    {
        LocalDateTime localStart = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);
        Instant instantStart = Instant.Parse("2024-01-01T12:00:00Z");
        RecurrenceRule localRule = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240102T120000");
        RecurrenceRule utcRule = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240102T120000Z");
        TimeZoneRef zone = TimeZoneRef.Utc();
        IZoneResolver resolver = ZoneResolver.Strict();

        LocalDateTime[] local = TimeApi.Factory.EnumerateLocalRecurrenceRule(localStart, localRule, 5);
        ZonedDateTime[] zoned = TimeApi.Factory.EnumerateZonedRecurrenceRule(localStart, utcRule, zone, resolver, 5);
        Instant[] exact = TimeApi.Factory.EnumerateInstantRecurrence(instantStart, utcRule, 5);

        Assert.That(local.Length, Is.EqualTo(2));
        Assert.That(local[1].ToIsoString(), Is.EqualTo("2024-01-02T12:00:00"));
        Assert.That(zoned.Length, Is.EqualTo(2));
        Assert.That(zoned[1].GetInstant().ToIsoString(), Is.EqualTo("2024-01-02T12:00:00Z"));
        Assert.That(exact.Length, Is.EqualTo(2));
        Assert.That(exact[1].ToIsoString(), Is.EqualTo("2024-01-02T12:00:00Z"));
        Assert.That(Assert.Throws<ArgumentException>(() => TimeApi.Factory.EnumerateLocalRecurrenceRule(localStart, utcRule, 5))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentException>(() => TimeApi.Factory.EnumerateZonedRecurrenceRule(localStart, localRule, zone, resolver, 5))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentException>(() => TimeApi.Factory.EnumerateInstantRecurrence(instantStart, localRule, 5))!.ParamName, Is.EqualTo("rule"));
    }

    [Test]
    public void ZonedEnumeration_AppliesUtcUntilAfterZoneResolution()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);
        RecurrenceRule rule = TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240102T163000Z");
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());

        ZonedDateTime[] result = TimeApi.Factory.EnumerateZonedRecurrenceRule(start, rule, zone, ZoneResolver.Strict(), 5);

        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0].GetInstant().ToIsoString(), Is.EqualTo("2024-01-01T17:00:00Z"));
    }

    [Test]
    public void ZonedSetEnumeration_AppliesEachRulesUtcUntilBeforeMergingOtherSources()
    {
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);
        RecurrenceSet set = RecurrenceSet.Create(start);
        set.AddRule(TimeApi.Factory.ParseRecurrenceRule("FREQ=DAILY;UNTIL=20240103T090000Z"));
        set.AddRule(TimeApi.Factory.ParseRecurrenceRule("FREQ=WEEKLY;UNTIL=20240115T090000Z"));
        set.AddRDate(LocalDateTime.Create(2024, 1, 20, 9, 0, 0));
        set.AddExDate(LocalDateTime.Create(2024, 1, 8, 9, 0, 0));

        ZonedDateTime[] result = TimeApi.Factory.EnumerateZonedRecurrenceSet(
            set,
            TimeZoneRef.Utc(),
            ZoneResolver.Strict(),
            30);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[0].GetInstant().ToIsoString(), Is.EqualTo("2024-01-01T09:00:00Z"));
        Assert.That(result[1].GetInstant().ToIsoString(), Is.EqualTo("2024-01-02T09:00:00Z"));
        Assert.That(result[2].GetInstant().ToIsoString(), Is.EqualTo("2024-01-03T09:00:00Z"));
        Assert.That(result[3].GetInstant().ToIsoString(), Is.EqualTo("2024-01-15T09:00:00Z"));
        Assert.That(result[4].GetInstant().ToIsoString(), Is.EqualTo("2024-01-20T09:00:00Z"));
    }

    [Test]
    public void PublicZonedEnumeration_AppliesInclusiveCallerInstantEndToSetsAndRules()
    {
        ITimeFactory factory = TimeApi.Factory;
        LocalDateTime start = factory.CreateLocalDateTimeFromFields(2024, 1, 1, 9, 0, 0);
        Instant end = factory.ParseInstant("2024-01-03T09:00:00Z");
        RecurrenceRule rule = factory.ParseRecurrenceRule("FREQ=DAILY");
        RecurrenceSet set = factory.CreateRecurrenceSet(start).AddRule(rule);
        TimeZoneRef zone = factory.CreateUtcTimeZone();
        IZoneResolver resolver = factory.CreateStrictZoneResolver();

        ZonedDateTime[] setResult = factory.EnumerateZonedRecurrenceSetThrough(set, zone, resolver, end, 20);
        ZonedDateTime[] ruleResult = factory.EnumerateZonedRecurrenceRuleThrough(start, rule, zone, resolver, end, 20);

        Assert.That(setResult.Length, Is.EqualTo(3));
        Assert.That(ruleResult.Length, Is.EqualTo(3));
        Assert.That(setResult[2].GetInstant().ToIsoString(), Is.EqualTo("2024-01-03T09:00:00Z"));
        Assert.That(ruleResult[2].GetInstant().ToIsoString(), Is.EqualTo("2024-01-03T09:00:00Z"));
    }

    [Test]
    public void EnumerateInstants_CountsDtStartBeforeApplyingFiltersToLaterUtcCandidates()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("2024-01-01T00:00:00Z"),
            CreateRule(frequency: RecurrenceFrequency.Hourly, count: 2, byHour: new int[] { 2 }),
            5);

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-01-01T02:00:00Z"));
    }

    [Test]
    public void PublicInstantEnumeration_AppliesInclusiveCallerEndToInfiniteRule()
    {
        ITimeFactory factory = TimeApi.Factory;
        Instant start = factory.ParseInstant("2024-01-01T00:00:00Z");
        Instant end = factory.ParseInstant("2024-01-01T03:00:00Z");
        RecurrenceRule rule = factory.ParseRecurrenceRule("FREQ=HOURLY");

        Instant[] result = factory.EnumerateInstantRecurrenceThrough(start, rule, end, 20);
        Instant[] empty = factory.EnumerateInstantRecurrenceThrough(end, rule, start, 20);

        Assert.That(result.Length, Is.EqualTo(4));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(result[3].ToIsoString(), Is.EqualTo("2024-01-01T03:00:00Z"));
        Assert.That(empty, Is.Empty);
    }

    [Test]
    public void InstantEnumeration_AppliesSafetyLimitWhenStartDoesNotMatchFilter()
    {
        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            Instant.Parse("2024-01-01T00:00:00Z"),
            RecurrenceRule.Parse("FREQ=HOURLY;BYMINUTE=15,30"),
            2);

        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-01-01T00:15:00Z"));
    }

    [Test]
    public void ZonedSetEnumeration_SortsLimitsAndDeduplicatesResolvedInstants()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        RecurrenceSet orderedSet = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 10, 9, 0, 0));
        orderedSet.AddRDate(LocalDateTime.Create(2024, 1, 2, 9, 0, 0));
        orderedSet.AddRDate(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));

        ZonedDateTime[] ordered = RecurrenceEnumerator.EnumerateZoned(
            orderedSet,
            zone,
            ZoneResolver.Strict(),
            2);

        RecurrenceSet gapSet = RecurrenceSet.Create(LocalDateTime.Create(2024, 3, 10, 2, 30, 0));
        gapSet.AddRDate(LocalDateTime.Create(2024, 3, 10, 3, 30, 0));
        ZonedDateTime[] deduplicated = RecurrenceEnumerator.EnumerateZoned(
            gapSet,
            zone,
            ZoneResolver.ShiftForward(),
            5);

        Assert.That(ordered.Length, Is.EqualTo(2));
        Assert.That(ordered[0].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-01T09:00:00"));
        Assert.That(ordered[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-02T09:00:00"));
        Assert.That(deduplicated.Length, Is.EqualTo(1));
        Assert.That(deduplicated[0].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
    }

    [Test]
    public void EnumerateInstants_RejectsCalendarFrequenciesWithoutFixedElapsedDuration()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceEnumerator.EnumerateInstants(Instant.Parse("2024-01-01T00:00:00Z"), CreateRule(frequency: RecurrenceFrequency.Monthly), 1))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceEnumerator.EnumerateInstants(Instant.Parse("2024-01-01T00:00:00Z"), CreateRule(frequency: RecurrenceFrequency.Yearly), 1))!.ParamName, Is.EqualTo("rule"));
    }

    [Test]
    public void LocalEnumeration_ValidatesInputs()
    {
        RecurrenceSet set = RecurrenceSet.Create(LocalDateTime.Create(2024, 1, 1, 9, 0, 0));
        RecurrenceRule rule = CreateRule(count: 1);
        TimeZoneRef zone = TimeZoneRef.Utc();
        ZoneResolver resolver = ZoneResolver.Strict();
        LocalDateTime start = LocalDateTime.Create(2024, 1, 1, 9, 0, 0);

        Assert.That(Assert.Throws<ArgumentNullException>(() => new RecurrenceEnumerator(null!, 1))!.ParamName, Is.EqualTo("set"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => new RecurrenceEnumerator(set, 0))!.ParamName, Is.EqualTo("maximumCandidates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.CreateLocal(null!, 1))!.ParamName, Is.EqualTo("set"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateLocal(null!, 1))!.ParamName, Is.EqualTo("set"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateLocal(null!, rule, 1))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateLocal(start, null!, 1))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateLocalThrough(set, null!, 1))!.ParamName, Is.EqualTo("end"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateLocalRuleThrough(start, rule, null!, 1))!.ParamName, Is.EqualTo("end"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(null!, zone, resolver, 1))!.ParamName, Is.EqualTo("set"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(set, null!, resolver, 1))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(set, zone, null!, 1))!.ParamName, Is.EqualTo("resolver"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(null!, rule, zone, resolver, 1))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(start, null!, zone, resolver, 1))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, null!, resolver, 1))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZoned(start, rule, zone, null!, 1))!.ParamName, Is.EqualTo("resolver"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZonedThrough(set, zone, resolver, null!, 1))!.ParamName, Is.EqualTo("end"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateZonedRuleThrough(start, rule, zone, resolver, null!, 1))!.ParamName, Is.EqualTo("end"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.MapLocalToZoned(null!, zone, resolver))!.ParamName, Is.EqualTo("localOccurrences"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.MapLocalToZoned(new LocalDateTime[0], null!, resolver))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.MapLocalToZoned(new LocalDateTime[0], zone, null!))!.ParamName, Is.EqualTo("resolver"));
        Assert.That(Assert.Throws<ArgumentException>(() => RecurrenceEnumerator.MapLocalToZoned(new LocalDateTime[] { null! }, zone, resolver))!.ParamName, Is.EqualTo("localOccurrences"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateInstants(null!, rule, 1))!.ParamName, Is.EqualTo("start"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateInstants(Instant.Parse("2024-01-01T00:00:00Z"), null!, 1))!.ParamName, Is.EqualTo("rule"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceEnumerator.EnumerateInstants(Instant.Parse("2024-01-01T00:00:00Z"), rule, 0))!.ParamName, Is.EqualTo("maximumCandidates"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => RecurrenceEnumerator.EnumerateInstantsThrough(Instant.Parse("2024-01-01T00:00:00Z"), rule, null!, 1))!.ParamName, Is.EqualTo("end"));
    }

    static void AssertFields(LocalDateTime value, int year, int month, int day, int hour, int minute, int second)
    {
        Assert.That(value.GetYear(), Is.EqualTo(year));
        Assert.That(value.GetMonth(), Is.EqualTo(month));
        Assert.That(value.GetDay(), Is.EqualTo(day));
        Assert.That(value.GetHour(), Is.EqualTo(hour));
        Assert.That(value.GetMinute(), Is.EqualTo(minute));
        Assert.That(value.GetSecond(), Is.EqualTo(second));
    }

    static RecurrenceRule CreateRule(
        RecurrenceFrequency frequency = RecurrenceFrequency.Daily,
        int interval = 1,
        int count = 0,
        Instant? until = null,
        int[]? byHour = null)
    {
        return new RecurrenceRule(
            frequency,
            interval,
            count,
            until,
            null,
            TimeConstants.IsoMonday,
            new int[0],
            new int[0],
            byHour ?? new int[0],
            new RecurrenceDay[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }

    static TimeZoneInfo CreateTestDstZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            10);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            3);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-recurrence-enumerator",
            TimeSpan.FromHours(-5),
            "Test recurrence enumerator zone",
            "Test standard",
            "Test daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }
}
