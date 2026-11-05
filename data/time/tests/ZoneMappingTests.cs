using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class ZoneMappingTests
{
    [Test]
    public void CreateNormal_StoresNormalMapping()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 12, 0, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant instant = Instant.Parse("2024-01-01T12:00:00Z");
        ZoneMapping mapping = ZoneMapping.CreateNormal(localDateTime, zone, instant);

        Assert.That(mapping.GetKind(), Is.EqualTo(ZoneMapping.KindNormal));
        Assert.That(mapping.GetLocalDateTime(), Is.SameAs(localDateTime));
        Assert.That(mapping.GetZone(), Is.SameAs(zone));
        Assert.That(mapping.GetEarlyInstant(), Is.SameAs(instant));
        Assert.That(mapping.GetLateInstant(), Is.SameAs(instant));
        Assert.That(mapping.GetGapDuration().EqualsValue(Duration.Zero()), Is.True);
    }

    [Test]
    public void CreateAmbiguous_StoresAmbiguousMapping()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant early = Instant.Parse("2024-11-03T05:30:00Z");
        Instant late = Instant.Parse("2024-11-03T06:30:00Z");
        ZoneMapping mapping = ZoneMapping.CreateAmbiguous(localDateTime, zone, early, late);

        Assert.That(mapping.GetKind(), Is.EqualTo(ZoneMapping.KindAmbiguous));
        Assert.That(mapping.GetLocalDateTime(), Is.SameAs(localDateTime));
        Assert.That(mapping.GetZone(), Is.SameAs(zone));
        Assert.That(mapping.GetEarlyInstant(), Is.SameAs(early));
        Assert.That(mapping.GetLateInstant(), Is.SameAs(late));
        Assert.That(mapping.GetGapDuration().EqualsValue(Duration.Zero()), Is.True);
    }

    [Test]
    public void CreateInvalid_StoresInvalidMapping()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant early = Instant.Parse("2024-03-10T06:59:59Z");
        Instant late = Instant.Parse("2024-03-10T07:00:00Z");
        Duration gap = Duration.FromHours(1L);
        ZoneMapping mapping = ZoneMapping.CreateInvalid(localDateTime, zone, early, late, gap);

        Assert.That(mapping.GetKind(), Is.EqualTo(ZoneMapping.KindInvalid));
        Assert.That(mapping.GetLocalDateTime(), Is.SameAs(localDateTime));
        Assert.That(mapping.GetZone(), Is.SameAs(zone));
        Assert.That(mapping.GetEarlyInstant(), Is.SameAs(early));
        Assert.That(mapping.GetLateInstant(), Is.SameAs(late));
        Assert.That(mapping.GetGapDuration(), Is.SameAs(gap));
    }

    [Test]
    public void Constructor_RejectsUnsupportedKind()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant instant = Instant.UnixEpoch();
        Duration gap = Duration.Zero();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ZoneMapping(0, localDateTime, zone, instant, instant, gap))!;

        Assert.That(exception.ParamName, Is.EqualTo("kind"));
    }

    [Test]
    public void Constructor_RequiresAllValues()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 1, 1, 0, 0, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant instant = Instant.UnixEpoch();
        Duration gap = Duration.Zero();

        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZoneMapping(ZoneMapping.KindNormal, null!, zone, instant, instant, gap))!.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZoneMapping(ZoneMapping.KindNormal, localDateTime, null!, instant, instant, gap))!.ParamName, Is.EqualTo("zone"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZoneMapping(ZoneMapping.KindNormal, localDateTime, zone, null!, instant, gap))!.ParamName, Is.EqualTo("earlyInstant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZoneMapping(ZoneMapping.KindNormal, localDateTime, zone, instant, null!, gap))!.ParamName, Is.EqualTo("lateInstant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => new ZoneMapping(ZoneMapping.KindNormal, localDateTime, zone, instant, instant, null!))!.ParamName, Is.EqualTo("gapDuration"));
    }

    [Test]
    public void CreateNormal_RequiresInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateNormal(LocalDateTime.Create(2024, 1, 1, 0, 0, 0), TimeZoneRef.Utc(), null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("instant"));
    }

    [Test]
    public void CreateAmbiguous_RequiresOrderedInstants()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant early = Instant.Parse("2024-11-03T05:30:00Z");
        Instant late = Instant.Parse("2024-11-03T06:30:00Z");

        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateAmbiguous(localDateTime, zone, null!, late))!.ParamName, Is.EqualTo("earlyInstant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateAmbiguous(localDateTime, zone, early, null!))!.ParamName, Is.EqualTo("lateInstant"));
        Assert.That(Assert.Throws<ArgumentException>(() => ZoneMapping.CreateAmbiguous(localDateTime, zone, late, early))!.ParamName, Is.EqualTo("earlyInstant"));
    }

    [Test]
    public void CreateInvalid_RequiresOrderedInstantsAndPositiveGap()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant early = Instant.Parse("2024-03-10T06:59:59Z");
        Instant late = Instant.Parse("2024-03-10T07:00:00Z");
        Duration gap = Duration.FromHours(1L);

        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, null!, late, gap))!.ParamName, Is.EqualTo("earlyInstant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, early, null!, gap))!.ParamName, Is.EqualTo("lateInstant"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, early, late, null!))!.ParamName, Is.EqualTo("gapDuration"));
        Assert.That(Assert.Throws<ArgumentException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, late, early, gap))!.ParamName, Is.EqualTo("earlyInstant"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, early, late, Duration.Zero()))!.ParamName, Is.EqualTo("gapDuration"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => ZoneMapping.CreateInvalid(localDateTime, zone, early, late, Duration.FromTicks(-1L)))!.ParamName, Is.EqualTo("gapDuration"));
    }
}
