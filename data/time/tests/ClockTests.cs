using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class ClockTests
{
    [Test]
    public void SystemTimeClock_ReturnsCurrentInstantInObservedRange()
    {
        SystemTimeClock clock = SystemTimeClock.Create();
        Instant before = Instant.Now();
        Instant current = clock.GetCurrentInstant();
        Instant after = Instant.Now();

        Assert.That(current.CompareTo(before), Is.GreaterThanOrEqualTo(0));
        Assert.That(current.CompareTo(after), Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void FixedTimeClock_ReturnsConfiguredInstant()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        FixedTimeClock clock = FixedTimeClock.Create(instant);

        Assert.That(clock.GetCurrentInstant().EqualsValue(instant), Is.True);
    }

    [Test]
    public void FixedTimeClock_RepeatedReadsStayDeterministicWithoutMocks()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        FixedTimeClock clock = new FixedTimeClock(instant);

        Assert.That(clock.GetCurrentInstant().EqualsValue(instant), Is.True);
        Assert.That(clock.GetCurrentInstant().EqualsValue(instant), Is.True);
        Assert.That(clock.GetCurrentInstant(), Is.SameAs(instant));
    }

    [Test]
    public void OffsetClock_AppliesPositiveAndNegativeDurationsToFixedClock()
    {
        FixedTimeClock source = FixedTimeClock.Create(Instant.Parse("2024-02-29T12:00:00Z"));
        OffsetClock positive = OffsetClock.Create(source, Duration.FromHours(3L));
        OffsetClock negative = TimeApi.Factory.CreateOffsetClock(source, Duration.FromMinutes(-90L));

        Assert.That(positive.GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-02-29T15:00:00Z"));
        Assert.That(negative.GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-02-29T10:30:00Z"));
    }

    [Test]
    public void OffsetClock_ReadsCustomClockOnEveryCall()
    {
        Instant first = Instant.Parse("2024-01-01T00:00:00Z");
        Instant second = Instant.Parse("2024-01-02T00:00:00Z");
        SequenceClock source = new SequenceClock(new Instant[] { first, second });
        IClock clock = OffsetClock.Create(source, Duration.FromSeconds(30L));

        Assert.That(clock.GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-01-01T00:00:30Z"));
        Assert.That(clock.GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-01-02T00:00:30Z"));
        Assert.That(source.GetReadCount(), Is.EqualTo(2));
    }

    [Test]
    public void OffsetClock_WithSystemClockReturnsShiftedInstantInObservedRange()
    {
        Duration offset = Duration.FromHours(2L);
        OffsetClock clock = OffsetClock.Create(SystemTimeClock.Create(), offset);
        Instant before = Instant.Now().AddDuration(offset);
        Instant current = clock.GetCurrentInstant();
        Instant after = Instant.Now().AddDuration(offset);

        Assert.That(current.CompareTo(before), Is.GreaterThanOrEqualTo(0));
        Assert.That(current.CompareTo(after), Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void OffsetClock_PropagatesInstantRangeOverflow()
    {
        OffsetClock aboveMaximum = OffsetClock.Create(
            FixedTimeClock.Create(Instant.FromUtcTicks(DateTime.MaxValue.Ticks)),
            Duration.FromTicks(1L));
        OffsetClock belowMinimum = OffsetClock.Create(
            FixedTimeClock.Create(Instant.FromUtcTicks(DateTime.MinValue.Ticks)),
            Duration.FromTicks(-1L));

        Assert.Throws<ArgumentOutOfRangeException>(() => aboveMaximum.GetCurrentInstant());
        Assert.Throws<ArgumentOutOfRangeException>(() => belowMinimum.GetCurrentInstant());
    }

    [Test]
    public void OffsetClock_InZoneUsesShiftedInstant()
    {
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");
        OffsetClock offsetClock = OffsetClock.Create(FixedTimeClock.Create(instant), Duration.FromMinutes(90L));
        TimeZoneRef zone = new TimeZoneRef(CreateFixedOffsetZone());
        ZonedClock zonedClock = offsetClock.InZone(zone);

        Assert.That(zonedClock.GetZone(), Is.SameAs(zone));
        Assert.That(zonedClock.GetCurrentInstant().ToIsoString(), Is.EqualTo("2024-01-01T01:30:00Z"));
        Assert.That(zonedClock.GetCurrentLocalDateTime().ToIsoString(), Is.EqualTo("2023-12-31T20:30:00"));
    }

    [Test]
    public void OffsetClock_ValidatesConstructionAndZoneInputs()
    {
        FixedTimeClock source = FixedTimeClock.Create(Instant.Parse("2024-01-01T00:00:00Z"));
        Duration offset = Duration.Zero();

        Assert.That(Assert.Throws<ArgumentNullException>(() => OffsetClock.Create(null!, offset))!.ParamName, Is.EqualTo("clock"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => OffsetClock.Create(source, null!))!.ParamName, Is.EqualTo("offset"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => TimeApi.Factory.CreateOffsetClock(null!, offset))!.ParamName, Is.EqualTo("clock"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => TimeApi.Factory.CreateOffsetClock(source, null!))!.ParamName, Is.EqualTo("offset"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => OffsetClock.Create(source, offset).InZone(null!))!.ParamName, Is.EqualTo("zone"));
    }

    [Test]
    public void ZonedClock_FromFixedReturnsCurrentZonedValues()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        TimeZoneRef zone = TimeZoneRef.Utc();
        ZonedClock clock = ZonedClock.FromFixed(instant, zone);

        Assert.That(clock.GetZone(), Is.SameAs(zone));
        Assert.That(clock.GetCurrentInstant().EqualsValue(instant), Is.True);
        Assert.That(clock.GetCurrentZonedDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56Z[" + TimeZoneInfo.Utc.Id + "]"));
        Assert.That(clock.GetCurrentLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56"));
        Assert.That(clock.GetCurrentDate().ToIsoString(), Is.EqualTo("2024-02-29"));
        Assert.That(clock.GetCurrentTime().ToIsoString(), Is.EqualTo("12:34:56"));
    }

    [Test]
    public void ZonedClock_WithFixedClockGivesDeterministicNonUtcValuesWithoutMocks()
    {
        Instant instant = Instant.Parse("2024-01-01T05:30:00Z");
        TimeZoneRef zone = new TimeZoneRef(CreateFixedOffsetZone());
        FixedTimeClock fixedClock = new FixedTimeClock(instant);
        ZonedClock clock = new ZonedClock(fixedClock, zone);

        Assert.That(clock.GetCurrentInstant().EqualsValue(instant), Is.True);
        Assert.That(clock.GetCurrentZonedDateTime().GetZone(), Is.SameAs(zone));
        Assert.That(clock.GetCurrentLocalDateTime().ToIsoString(), Is.EqualTo("2024-01-01T00:30:00"));
        Assert.That(clock.GetCurrentDate().ToIsoString(), Is.EqualTo("2024-01-01"));
        Assert.That(clock.GetCurrentTime().ToIsoString(), Is.EqualTo("00:30:00"));
    }

    [Test]
    public void ZonedClock_WithSystemClockReturnsInstantInObservedRangeWithoutMocks()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();
        ZonedClock clock = new ZonedClock(SystemTimeClock.Create(), zone);
        Instant before = Instant.Now();
        ZonedDateTime current = clock.GetCurrentZonedDateTime();
        Instant after = Instant.Now();

        Assert.That(current.GetZone(), Is.SameAs(zone));
        Assert.That(current.GetInstant().CompareTo(before), Is.GreaterThanOrEqualTo(0));
        Assert.That(current.GetInstant().CompareTo(after), Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void ClockTypes_AreConcreteClasses()
    {
        Assert.That(typeof(SystemTimeClock).IsClass, Is.True);
        Assert.That(typeof(SystemTimeClock).IsAbstract, Is.False);
        Assert.That(typeof(SystemTimeClock).IsInterface, Is.False);
        Assert.That(typeof(FixedTimeClock).IsClass, Is.True);
        Assert.That(typeof(FixedTimeClock).IsAbstract, Is.False);
        Assert.That(typeof(FixedTimeClock).IsInterface, Is.False);
        Assert.That(typeof(ZonedClock).IsClass, Is.True);
        Assert.That(typeof(ZonedClock).IsAbstract, Is.False);
        Assert.That(typeof(ZonedClock).IsInterface, Is.False);
    }

    [Test]
    public void ClockTypes_ImplementSharedClockInterface()
    {
        Assert.That(typeof(IClock).IsInterface, Is.True);
        Assert.That(typeof(IClock).IsAssignableFrom(typeof(SystemTimeClock)), Is.True);
        Assert.That(typeof(IClock).IsAssignableFrom(typeof(FixedTimeClock)), Is.True);
        Assert.That(typeof(IClock).IsAssignableFrom(typeof(ZonedClock)), Is.True);
    }

    [Test]
    public void ZonedClock_AcceptsApplicationClockImplementation()
    {
        Instant first = Instant.Parse("2024-01-01T23:59:59Z");
        Instant second = Instant.Parse("2024-01-02T00:00:00Z");
        SequenceClock source = new SequenceClock(new Instant[] { first, second });
        ZonedClock clock = ZonedClock.Create(source, TimeZoneRef.Utc());

        Assert.That(clock.GetCurrentDate().ToIsoString(), Is.EqualTo("2024-01-01"));
        Assert.That(clock.GetCurrentDate().ToIsoString(), Is.EqualTo("2024-01-02"));
        Assert.That(source.GetReadCount(), Is.EqualTo(2));
    }

    [Test]
    public void ClockInZoneFactoriesCreateZonedClocks()
    {
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");
        FixedTimeClock fixedClock = FixedTimeClock.Create(instant);
        SystemTimeClock systemClock = SystemTimeClock.Create();
        TimeZoneRef zone = TimeZoneRef.Utc();

        ZonedClock fixedZoned = fixedClock.InZone(zone);
        ZonedClock systemZoned = systemClock.InZone(zone);

        Assert.That(fixedZoned.GetZone(), Is.SameAs(zone));
        Assert.That(fixedZoned.GetCurrentInstant().EqualsValue(instant), Is.True);
        Assert.That(systemZoned.GetZone(), Is.SameAs(zone));
    }

    [Test]
    public void Clocks_ValidateInputs()
    {
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");
        FixedTimeClock fixedClock = FixedTimeClock.Create(instant);
        SystemTimeClock systemClock = SystemTimeClock.Create();
        TimeZoneRef zone = TimeZoneRef.Utc();

        Assert.That(Assert.Throws<ArgumentNullException>(() => FixedTimeClock.Create(null!))!.ParamName, Is.EqualTo("instant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => fixedClock.InZone(null!))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => systemClock.InZone(null!))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZonedClock((IClock)null!, zone))!.ParamName, Is.EqualTo("clock"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZonedClock(fixedClock, null!))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZonedClock.Create(null!, zone))!.ParamName, Is.EqualTo("clock"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZonedClock.Create(fixedClock, null!))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZonedClock.FromFixed(null!, zone))!.ParamName, Is.EqualTo("instant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZonedClock.FromFixed(instant, null!))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZonedClock.FromSystem(null!))!.ParamName, Is.EqualTo("zone"));
    }

    static TimeZoneInfo CreateFixedOffsetZone()
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-clock-fixed-offset",
            TimeSpan.FromHours(-5),
            "Test clock fixed offset zone",
            "Test standard");
    }

    class SequenceClock : IClock
    {
        Instant[] instants;
        int index;

        public SequenceClock(Instant[] instants)
        {
            this.instants = instants;
            index = 0;
        }

        public Instant GetCurrentInstant()
        {
            Instant current = instants[index];
            index = index + 1;
            return current;
        }

        public int GetReadCount()
        {
            return index;
        }
    }
}
