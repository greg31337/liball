using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class ZoneResolverTests
{
    [TestCase(ZoneResolver.ModeStrict)]
    [TestCase(ZoneResolver.ModeEarlier)]
    [TestCase(ZoneResolver.ModeLater)]
    [TestCase(ZoneResolver.ModeCompatible)]
    [TestCase(ZoneResolver.ModeShiftForward)]
    public void Constructor_AllowsSupportedModes(int mode)
    {
        ZoneResolver resolver = new ZoneResolver(mode);

        Assert.That(resolver.GetMode(), Is.EqualTo(mode));
    }

    [Test]
    public void Factories_ReturnExpectedModes()
    {
        Assert.That(ZoneResolver.Strict().GetMode(), Is.EqualTo(ZoneResolver.ModeStrict));
        Assert.That(ZoneResolver.Earlier().GetMode(), Is.EqualTo(ZoneResolver.ModeEarlier));
        Assert.That(ZoneResolver.Later().GetMode(), Is.EqualTo(ZoneResolver.ModeLater));
        Assert.That(ZoneResolver.Compatible().GetMode(), Is.EqualTo(ZoneResolver.ModeCompatible));
        Assert.That(ZoneResolver.ShiftForward().GetMode(), Is.EqualTo(ZoneResolver.ModeShiftForward));
    }

    [Test]
    public void Constructor_RejectsUnsupportedMode()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneResolver(0))!;

        Assert.That(exception.ParamName, Is.EqualTo("mode"));
    }

    [Test]
    public void Resolve_RequiresMapping()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ZoneResolver.Strict().Resolve(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("mapping"));
    }

    [Test]
    public void Strict_ResolvesNormalMapping()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant instant = Instant.Parse("2024-01-01T12:00:00Z");
        ZoneMapping mapping = ZoneMapping.CreateNormal(localDateTime, zone, instant);
        OffsetDateTime resolved = ZoneResolver.Strict().Resolve(mapping);

        Assert.That(resolved.GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(resolved.GetOffset().EqualsValue(Offset.Zero()), Is.True);
        Assert.That(resolved.GetInstant().EqualsValue(instant), Is.True);
    }

    [Test]
    public void ResolveLocal_RequiresLocalDateTimeAndResolver()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);

        ArgumentNullException localException = Assert.Throws<ArgumentNullException>(() => zone.ResolveLocal(null!, ZoneResolver.Strict()))!;
        ArgumentNullException resolverException = Assert.Throws<ArgumentNullException>(() => zone.ResolveLocal(localDateTime, null!))!;

        Assert.That(localException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(resolverException.ParamName, Is.EqualTo("resolver"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneResolver.Strict().ResolveLocal(null!, localDateTime))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneResolver.Strict().ResolveLocal(zone, null!))!.ParamName, Is.EqualTo("localDateTime"));
    }

    [Test]
    public void ResolverTypes_ExposeSharedStrategyInterface()
    {
        Assert.That(typeof(IZoneResolver).IsInterface, Is.True);
        Assert.That(typeof(IZoneResolver).IsAssignableFrom(typeof(ZoneResolver)), Is.True);
    }

    [Test]
    public void ResolveLocal_AcceptsApplicationResolutionPolicy()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        LatestMappingResolver resolver = new LatestMappingResolver();

        OffsetDateTime resolved = zone.ResolveLocal(ambiguous, resolver);

        Assert.That(resolved.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.That(resolver.GetCallCount(), Is.EqualTo(1));
        Assert.That(resolver.GetLastZone(), Is.SameAs(zone));
        Assert.That(resolver.GetLastLocalDateTime(), Is.SameAs(ambiguous));
    }

    [Test]
    public void Strict_RejectsAmbiguousAndInvalidLocalTimes()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);

        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(ambiguous, ZoneResolver.Strict()));
        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(invalid, ZoneResolver.Strict()));
    }

    [Test]
    public void EarlierLaterAndCompatible_ResolveAmbiguousLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        OffsetDateTime earlier = zone.ResolveLocal(ambiguous, ZoneResolver.Earlier());
        OffsetDateTime later = zone.ResolveLocal(ambiguous, ZoneResolver.Later());
        OffsetDateTime compatible = zone.ResolveLocal(ambiguous, ZoneResolver.Compatible());

        Assert.That(earlier.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(later.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(earlier.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(later.GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T05:30:00Z"));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-03T06:30:00Z"));
        Assert.That(compatible.EqualsValue(earlier), Is.True);
    }

    [Test]
    public void ShiftForward_RejectsAmbiguousLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);

        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(ambiguous, ZoneResolver.ShiftForward()));
    }

    [Test]
    public void ShiftForwardAndCompatible_ResolveInvalidLocalTimeByGapDuration()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);
        OffsetDateTime shifted = zone.ResolveLocal(invalid, ZoneResolver.ShiftForward());
        OffsetDateTime compatible = zone.ResolveLocal(invalid, ZoneResolver.Compatible());

        Assert.That(shifted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:30:00"));
        Assert.That(shifted.GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(shifted.GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T07:30:00Z"));
        Assert.That(compatible.EqualsValue(shifted), Is.True);
    }

    [Test]
    public void EarlierAndLater_RejectInvalidLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);

        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(invalid, ZoneResolver.Earlier()));
        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(invalid, ZoneResolver.Later()));
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
            "time-library-test-dst-resolver",
            TimeSpan.FromHours(-5),
            "Time Library Test DST Resolver",
            "TLT",
            "TLT Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    class LatestMappingResolver : IZoneResolver
    {
        int callCount;
        TimeZoneRef? lastZone;
        LocalDateTime? lastLocalDateTime;

        public OffsetDateTime ResolveLocal(TimeZoneRef zone, LocalDateTime localDateTime)
        {
            callCount = callCount + 1;
            lastZone = zone;
            lastLocalDateTime = localDateTime;
            OffsetDateTime[] mappings = zone.MapLocal(localDateTime);
            OffsetDateTime latest = mappings[0];
            int index = 1;
            while (index < mappings.Length)
            {
                if (mappings[index].GetInstant().CompareTo(latest.GetInstant()) > 0)
                {
                    latest = mappings[index];
                }

                index = index + 1;
            }

            return latest;
        }

        public int GetCallCount()
        {
            return callCount;
        }

        public TimeZoneRef? GetLastZone()
        {
            return lastZone;
        }

        public LocalDateTime? GetLastLocalDateTime()
        {
            return lastLocalDateTime;
        }
    }
}
