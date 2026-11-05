using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class ZonedDateTimeTests
{
    [Test]
    public void Constructor_RequiresInstantAndZone()
    {
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");
        TimeZoneRef zone = TimeZoneRef.Utc();

        ArgumentNullException instantException = Assert.Throws<ArgumentNullException>(() => new ZonedDateTime(null!, zone))!;
        ArgumentNullException zoneException = Assert.Throws<ArgumentNullException>(() => new ZonedDateTime(instant, null!))!;

        Assert.That(instantException.ParamName, Is.EqualTo("instant"));
        Assert.That(zoneException.ParamName, Is.EqualTo("zone"));
    }

    [Test]
    public void CreateFromInstant_RequiresInstantAndZone()
    {
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");
        TimeZoneRef zone = TimeZoneRef.Utc();

        ArgumentNullException instantException = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.CreateFromInstant(null!, zone))!;
        ArgumentNullException zoneException = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.CreateFromInstant(instant, null!))!;

        Assert.That(instantException.ParamName, Is.EqualTo("instant"));
        Assert.That(zoneException.ParamName, Is.EqualTo("zone"));
    }

    [Test]
    public void CreateFromInstant_ConvertsInstantToZoneLocalTimeAndOffset()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant instant = Instant.Parse("2024-06-15T16:00:00Z");
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(instant, zone);

        Assert.That(zonedDateTime.GetInstant(), Is.SameAs(instant));
        Assert.That(zonedDateTime.GetZone(), Is.SameAs(zone));
        Assert.That(zonedDateTime.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-06-15T12:00:00"));
        Assert.That(zonedDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
    }

    [Test]
    public void CreateFromLocal_RequiresLocalDateTimeZoneAndResolver()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        ZoneResolver resolver = ZoneResolver.Strict();

        ArgumentNullException localException = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.CreateFromLocal(null!, zone, resolver))!;
        ArgumentNullException zoneException = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.CreateFromLocal(localDateTime, null!, resolver))!;
        ArgumentNullException resolverException = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.CreateFromLocal(localDateTime, zone, null!))!;

        Assert.That(localException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(zoneException.ParamName, Is.EqualTo("zone"));
        Assert.That(resolverException.ParamName, Is.EqualTo("resolver"));
    }

    [Test]
    public void CreateFromLocal_ResolvesNormalLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 15, 12, 0, 0);
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromLocal(localDateTime, zone, ZoneResolver.Strict());

        Assert.That(zonedDateTime.GetZone(), Is.SameAs(zone));
        Assert.That(zonedDateTime.GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(zonedDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(zonedDateTime.GetInstant().ToIsoString(), Is.EqualTo("2024-01-15T17:00:00Z"));
    }

    [Test]
    public void CreateFromLocal_UsesResolverForAmbiguousLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        ZonedDateTime earlier = ZonedDateTime.CreateFromLocal(ambiguous, zone, ZoneResolver.Earlier());
        ZonedDateTime later = ZonedDateTime.CreateFromLocal(ambiguous, zone, ZoneResolver.Later());

        Assert.That(earlier.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(later.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(earlier.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(later.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.Throws<InvalidOperationException>(() => ZonedDateTime.CreateFromLocal(ambiguous, zone, ZoneResolver.Strict()));
    }

    [Test]
    public void CreateFromLocal_UsesResolverForInvalidLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);
        ZonedDateTime shifted = ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.ShiftForward());
        ZonedDateTime compatible = ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.Compatible());

        Assert.That(shifted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
        Assert.That(shifted.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(shifted.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.That(compatible.GetInstant().EqualsValue(shifted.GetInstant()), Is.True);
        Assert.Throws<InvalidOperationException>(() => ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.Strict()));
    }

    [Test]
    public void Accessors_ReturnZoneOffsetLocalDateAndTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-06-15T16:30:45Z"), zone);

        Assert.That(zonedDateTime.GetZone(), Is.SameAs(zone));
        Assert.That(zonedDateTime.GetZoneId(), Is.EqualTo("time-library-test-zoned-datetime"));
        Assert.That(zonedDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(zonedDateTime.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-06-15T12:30:45"));
        Assert.That(zonedDateTime.GetDate().ToIsoString(), Is.EqualTo("2024-06-15"));
        Assert.That(zonedDateTime.GetTime().ToIsoString(), Is.EqualTo("12:30:45"));
    }

    [Test]
    public void WithZone_PreservesInstantAndRecomputesLocalTime()
    {
        TimeZoneRef sourceZone = new TimeZoneRef(CreateTestDstZone());
        TimeZoneRef targetZone = TimeZoneRef.Utc();
        ZonedDateTime source = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-06-15T16:00:00Z"), sourceZone);
        ZonedDateTime converted = source.WithZone(targetZone);

        Assert.That(converted.GetZone(), Is.SameAs(targetZone));
        Assert.That(converted.GetInstant().EqualsValue(source.GetInstant()), Is.True);
        Assert.That(converted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-06-15T16:00:00"));
        Assert.That(converted.GetOffset().EqualsValue(Offset.Zero()), Is.True);
    }

    [Test]
    public void WithZone_RequiresTargetZone()
    {
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:00Z"), TimeZoneRef.Utc());

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => zonedDateTime.WithZone(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("targetZone"));
    }

    [Test]
    public void ToOffsetDateTime_ReturnsLocalTimeOffsetAndInstant()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-15T17:00:00Z"), zone);
        OffsetDateTime offsetDateTime = zonedDateTime.ToOffsetDateTime();

        Assert.That(offsetDateTime.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-15T12:00:00"));
        Assert.That(offsetDateTime.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(offsetDateTime.GetInstant().EqualsValue(zonedDateTime.GetInstant()), Is.True);
    }

    [Test]
    public void DurationArithmetic_MovesInstantExactlyAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime added = before.AddDuration(Duration.FromDays(1L));
        ZonedDateTime subtracted = added.SubtractDuration(Duration.FromDays(1L));

        Assert.That(added.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T17:00:00Z"));
        Assert.That(added.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T13:00:00"));
        Assert.That(added.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(subtracted.EqualsValue(before), Is.True);
    }

    [Test]
    public void PeriodArithmetic_UsesLocalCalendarSemanticsAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime added = before.AddPeriod(Period.FromDays(1L), ZoneResolver.Compatible());
        ZonedDateTime subtracted = added.SubtractPeriod(Period.FromDays(1L), ZoneResolver.Compatible());

        Assert.That(added.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T16:00:00Z"));
        Assert.That(added.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T12:00:00"));
        Assert.That(added.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(subtracted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-09T12:00:00"));
        Assert.That(subtracted.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
    }

    [Test]
    public void DurationAndPeriodArithmeticDifferAcrossFallBack()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 11, 2, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime durationAdded = before.AddDuration(Duration.FromDays(1L));
        ZonedDateTime periodAdded = before.AddPeriod(Period.FromDays(1L), ZoneResolver.Compatible());

        Assert.That(durationAdded.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T16:00:00Z"));
        Assert.That(durationAdded.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T11:00:00"));
        Assert.That(durationAdded.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(periodAdded.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T17:00:00Z"));
        Assert.That(periodAdded.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T12:00:00"));
        Assert.That(periodAdded.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
    }

    [Test]
    public void AddPeriod_GapPoliciesRejectOrShiftAsSpecified()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 2, 30, 0), zone, ZoneResolver.Strict());
        Period period = Period.FromDays(1L);
        ZonedDateTime compatible = before.AddPeriod(period, ZoneResolver.Compatible());
        ZonedDateTime shifted = before.AddPeriod(period, ZoneResolver.ShiftForward());

        Assert.That(compatible.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
        Assert.That(compatible.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(compatible.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.That(shifted.EqualsValue(compatible), Is.True);
        Assert.Throws<InvalidOperationException>(() => before.AddPeriod(period, ZoneResolver.Strict()));
        Assert.Throws<InvalidOperationException>(() => before.AddPeriod(period, ZoneResolver.Earlier()));
        Assert.Throws<InvalidOperationException>(() => before.AddPeriod(period, ZoneResolver.Later()));
    }

    [Test]
    public void AddPeriod_OverlapPoliciesSelectOrRejectAsSpecified()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 11, 2, 1, 30, 0), zone, ZoneResolver.Strict());
        Period period = Period.FromDays(1L);
        ZonedDateTime earlier = before.AddPeriod(period, ZoneResolver.Earlier());
        ZonedDateTime later = before.AddPeriod(period, ZoneResolver.Later());
        ZonedDateTime compatible = before.AddPeriod(period, ZoneResolver.Compatible());

        Assert.That(earlier.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T01:30:00"));
        Assert.That(earlier.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.That(later.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.That(compatible.EqualsValue(earlier), Is.True);
        Assert.Throws<InvalidOperationException>(() => before.AddPeriod(period, ZoneResolver.Strict()));
        Assert.Throws<InvalidOperationException>(() => before.AddPeriod(period, ZoneResolver.ShiftForward()));
    }

    [Test]
    public void AddPeriod_AllPoliciesResolveNormalTimeIdentically()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime before = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 1, 14, 12, 0, 0), zone, ZoneResolver.Strict());
        Period period = Period.FromDays(1L);
        IZoneResolver[] resolvers = new IZoneResolver[]
        {
            ZoneResolver.Strict(),
            ZoneResolver.Earlier(),
            ZoneResolver.Later(),
            ZoneResolver.Compatible(),
            ZoneResolver.ShiftForward()
        };

        foreach (IZoneResolver resolver in resolvers)
        {
            ZonedDateTime result = before.AddPeriod(period, resolver);

            Assert.That(result.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-15T12:00:00"));
            Assert.That(result.GetInstant().ToIsoString(), Is.EqualTo("2024-01-15T17:00:00Z"));
        }
    }

    [Test]
    public void SubtractPeriod_UsesResolverForGapAndOverlap()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Period period = Period.FromDays(1L);
        ZonedDateTime afterGap = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 11, 2, 30, 0), zone, ZoneResolver.Strict());
        ZonedDateTime afterOverlap = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 11, 4, 1, 30, 0), zone, ZoneResolver.Strict());
        ZonedDateTime shifted = afterGap.SubtractPeriod(period, ZoneResolver.ShiftForward());
        ZonedDateTime earlier = afterOverlap.SubtractPeriod(period, ZoneResolver.Earlier());
        ZonedDateTime later = afterOverlap.SubtractPeriod(period, ZoneResolver.Later());

        Assert.That(shifted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
        Assert.That(shifted.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.Throws<InvalidOperationException>(() => afterGap.SubtractPeriod(period, ZoneResolver.Strict()));
    }

    [Test]
    public void PeriodArithmetic_ConstrainsMonthEndBeforeResolving()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime january = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 1, 31, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime march = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 31, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime added = january.AddPeriod(Period.FromMonths(1L), ZoneResolver.Strict());
        ZonedDateTime subtracted = march.SubtractPeriod(Period.FromMonths(1L), ZoneResolver.Strict());

        Assert.That(added.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:00:00"));
        Assert.That(subtracted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:00:00"));
    }

    [Test]
    public void PeriodArithmetic_IsAvailableThroughZonedDateTimeContract()
    {
        IZonedDateTime source = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-15T17:00:00Z"), TimeZoneRef.Utc());
        ZonedDateTime added = source.AddPeriod(Period.FromDays(1L), ZoneResolver.Strict());
        IZonedDateTime addedContract = added;
        ZonedDateTime subtracted = addedContract.SubtractPeriod(Period.FromDays(1L), ZoneResolver.Strict());

        Assert.That(added.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-16T17:00:00"));
        Assert.That(subtracted.GetInstant().EqualsValue(source.GetInstant()), Is.True);
    }

    [Test]
    public void Arithmetic_RequiresDurationOrPeriod()
    {
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:00Z"), TimeZoneRef.Utc());

        ArgumentNullException addDurationException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.AddDuration(null!))!;
        ArgumentNullException subtractDurationException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.SubtractDuration(null!))!;
        ArgumentNullException addPeriodException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.AddPeriod(null!, ZoneResolver.Strict()))!;
        ArgumentNullException subtractPeriodException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.SubtractPeriod(null!, ZoneResolver.Strict()))!;
        ArgumentNullException addResolverException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.AddPeriod(Period.Zero(), null!))!;
        ArgumentNullException subtractResolverException = Assert.Throws<ArgumentNullException>(() => zonedDateTime.SubtractPeriod(Period.Zero(), null!))!;

        Assert.That(addDurationException.ParamName, Is.EqualTo("duration"));
        Assert.That(subtractDurationException.ParamName, Is.EqualTo("duration"));
        Assert.That(addPeriodException.ParamName, Is.EqualTo("period"));
        Assert.That(subtractPeriodException.ParamName, Is.EqualTo("period"));
        Assert.That(addResolverException.ParamName, Is.EqualTo("resolver"));
        Assert.That(subtractResolverException.ParamName, Is.EqualTo("resolver"));
    }

    [Test]
    public void StartOfDay_ReturnsMidnightForNormalDate()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime source = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 6, 15, 12, 30, 0), zone, ZoneResolver.Strict());
        ZonedDateTime start = source.StartOfDay();

        Assert.That(start.GetZone(), Is.SameAs(zone));
        Assert.That(start.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-06-15T00:00:00"));
        Assert.That(start.GetDate().ToIsoString(), Is.EqualTo("2024-06-15"));
        Assert.That(start.GetTime().EqualsValue(LocalTime.Midnight()), Is.True);
        Assert.That(start.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(start.GetInstant().ToIsoString(), Is.EqualTo("2024-06-15T04:00:00Z"));
    }

    [Test]
    public void StartOfDay_ShiftsForwardWhenMidnightIsSkipped()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateMidnightGapZone());
        LocalDateTime skippedMidnight = LocalDateTime.Create(2024, 3, 10, 0, 0, 0);
        ZonedDateTime source = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 10, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime start = source.StartOfDay();

        Assert.That(zone.IsInvalidLocal(skippedMidnight), Is.True);
        Assert.That(start.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T01:00:00"));
        Assert.That(start.GetDate().ToIsoString(), Is.EqualTo("2024-03-10"));
        Assert.That(start.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(start.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T05:00:00Z"));
    }

    [Test]
    public void Integration_ZoneConversionAndDurationArithmeticPreserveInstant()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime source = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 12, 0, 0), zone, ZoneResolver.Strict());
        ZonedDateTime utc = source.WithZone(TimeZoneRef.Utc());
        ZonedDateTime durationAdded = source.AddDuration(Duration.FromDays(1L));

        Assert.That(utc.GetInstant().EqualsValue(source.GetInstant()), Is.True);
        Assert.That(utc.GetLocalDateTime().EqualsValue(source.GetLocalDateTime()), Is.False);
        Assert.That(source.GetInstant().DurationUntil(durationAdded.GetInstant()).GetTicks(), Is.EqualTo(TimeConstants.TicksPerDay));
        Assert.That(durationAdded.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T13:00:00"));
    }

    [Test]
    public void Integration_PeriodArithmeticPreservesWallClockAcrossDstBoundaries()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime spring = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 8, 45, 0), zone, ZoneResolver.Strict());
        ZonedDateTime fall = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 11, 2, 8, 45, 0), zone, ZoneResolver.Strict());
        ZonedDateTime springAdded = spring.AddPeriod(Period.FromDays(1L), ZoneResolver.Compatible());
        ZonedDateTime fallAdded = fall.AddPeriod(Period.FromDays(1L), ZoneResolver.Compatible());

        Assert.That(springAdded.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T08:45:00"));
        Assert.That(springAdded.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(fallAdded.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-11-03T08:45:00"));
        Assert.That(fallAdded.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
    }

    [Test]
    public void Integration_DstGapResolutionUsesExplicitResolverModes()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 15, 0);
        ZonedDateTime shifted = ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.ShiftForward());
        ZonedDateTime compatible = ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.Compatible());

        Assert.That(zone.IsInvalidLocal(invalid), Is.True);
        Assert.That(shifted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:15:00"));
        Assert.That(shifted.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:15:00Z"));
        Assert.That(compatible.EqualsValue(shifted), Is.True);
        Assert.Throws<InvalidOperationException>(() => ZonedDateTime.CreateFromLocal(invalid, zone, ZoneResolver.Strict()));
    }

    [Test]
    public void Integration_DstRepeatResolutionKeepsWallClockAndSelectsDistinctInstants()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 15, 0);
        ZonedDateTime earlier = ZonedDateTime.CreateFromLocal(ambiguous, zone, ZoneResolver.Earlier());
        ZonedDateTime later = ZonedDateTime.CreateFromLocal(ambiguous, zone, ZoneResolver.Later());

        Assert.That(zone.IsAmbiguousLocal(ambiguous), Is.True);
        Assert.That(earlier.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(later.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:15:00Z"));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:15:00Z"));
        Assert.That(earlier.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(later.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
    }

    [Test]
    public void Integration_FormatAndParseRoundTripPreservesUtcZonedValue()
    {
        ZonedDateTime original = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-02-29T12:34:56.1234567Z"), TimeZoneRef.Utc());
        string text = original.ToIsoString();
        ZonedDateTime parsed = ZonedDateTime.Parse(text);

        Assert.That(text, Is.EqualTo("2024-02-29T12:34:56.1234567Z[" + TimeZoneInfo.Utc.Id + "]"));
        Assert.That(parsed.EqualsValue(original), Is.True);
        Assert.That(parsed.GetInstant().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56.1234567Z"));
        Assert.That(parsed.GetOffset().EqualsValue(Offset.Zero()), Is.True);
    }

    [Test]
    public void CompareInstant_OrdersByInstant()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();
        ZonedDateTime earlier = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:00Z"), zone);
        ZonedDateTime same = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:00Z"), zone);
        ZonedDateTime later = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:01Z"), zone);

        Assert.That(earlier.CompareInstant(later), Is.EqualTo(-1));
        Assert.That(earlier.CompareInstant(same), Is.EqualTo(0));
        Assert.That(later.CompareInstant(earlier), Is.EqualTo(1));
    }

    [Test]
    public void CompareInstant_RequiresOther()
    {
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-01-01T00:00:00Z"), TimeZoneRef.Utc());

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => zonedDateTime.CompareInstant(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_UsesInstantZoneAndOffsetDateTime()
    {
        Instant instant = Instant.Parse("2024-06-15T16:00:00Z");
        ZonedDateTime left = ZonedDateTime.CreateFromInstant(instant, new TimeZoneRef(CreateTestDstZone()));
        ZonedDateTime same = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-06-15T16:00:00Z"), new TimeZoneRef(CreateTestDstZone()));
        ZonedDateTime differentInstant = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-06-15T17:00:00Z"), new TimeZoneRef(CreateTestDstZone()));
        ZonedDateTime differentZone = ZonedDateTime.CreateFromInstant(instant, TimeZoneRef.Utc());

        Assert.That(left.EqualsValue(same), Is.True);
        Assert.That(left.EqualsValue(differentInstant), Is.False);
        Assert.That(left.EqualsValue(differentZone), Is.False);
        Assert.That(left.EqualsValue(null), Is.False);
    }

    [Test]
    public void ToIsoString_FormatsOffsetDateTimeWithBracketedZoneId()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        ZonedDateTime zonedDateTime = ZonedDateTime.CreateFromInstant(Instant.Parse("2024-06-15T16:00:00Z"), zone);

        Assert.That(zonedDateTime.ToIsoString(), Is.EqualTo("2024-06-15T12:00:00-04:00[time-library-test-zoned-datetime]"));
    }

    [Test]
    public void Parse_RoundTripsUtcZonedDateTime()
    {
        string text = "2024-02-29T12:34:56Z[" + TimeZoneInfo.Utc.Id + "]";
        ZonedDateTime parsed = ZonedDateTime.Parse(text);

        Assert.That(parsed.GetZoneId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
        Assert.That(parsed.GetInstant().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56Z"));
        Assert.That(parsed.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56"));
        Assert.That(parsed.GetOffset().EqualsValue(Offset.Zero()), Is.True);
        Assert.That(parsed.ToIsoString(), Is.EqualTo(text));
    }

    [Test]
    public void Parse_RequiresText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ZonedDateTime.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("2024-02-29T12:34:56Z")]
    [TestCase("2024-02-29T12:34:56Z[]")]
    [TestCase("[UTC]")]
    public void Parse_RejectsMalformedZoneIdText(string text)
    {
        Assert.Throws<FormatException>(() => ZonedDateTime.Parse(text));
    }

    [Test]
    public void Parse_RejectsOffsetDateTimeThatDoesNotMatchZone()
    {
        string text = "2024-02-29T13:34:56+01:00[" + TimeZoneInfo.Utc.Id + "]";

        Assert.Throws<FormatException>(() => ZonedDateTime.Parse(text));
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
            "time-library-test-zoned-datetime",
            TimeSpan.FromHours(-5),
            "Time Library Test Zoned DateTime",
            "TLT",
            "TLT Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    static TimeZoneInfo CreateMidnightGapZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 0, 0, 0),
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
            "time-library-test-midnight-gap",
            TimeSpan.FromHours(-5),
            "Time Library Test Midnight Gap",
            "TLT",
            "TLT Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }
}
