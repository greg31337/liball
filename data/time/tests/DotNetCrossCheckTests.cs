using System.Globalization;
using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class DotNetCrossCheckTests
{
    [Test]
    public void DateOnlyAndDateTime_CalendarValuesAndArithmeticMatch()
    {
        ITimeFactory factory = TimeApi.Factory;
        DateTime[] values = new DateTime[]
        {
            new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            new DateTime(1900, 2, 28, 23, 59, 59, DateTimeKind.Unspecified),
            new DateTime(2000, 2, 29, 12, 34, 56, DateTimeKind.Unspecified).AddTicks(7890L),
            new DateTime(2024, 12, 31, 6, 7, 8, DateTimeKind.Unspecified),
            new DateTime(9998, 12, 31, 23, 59, 59, DateTimeKind.Unspecified)
        };

        foreach (DateTime value in values)
        {
            DateOnly dateOnly = DateOnly.FromDateTime(value);
            LocalDate date = factory.CreateLocalDateFromDateOnly(dateOnly);
            LocalDateTime dateTime = factory.CreateLocalDateTimeFromDateTime(value);

            Assert.That(date.ToDateOnly(), Is.EqualTo(dateOnly));
            Assert.That(date.GetDayNumber(), Is.EqualTo(dateOnly.DayNumber));
            Assert.That(date.AddDays(1L).ToDateOnly(), Is.EqualTo(dateOnly.AddDays(1)));
            Assert.That(dateTime.ToDateTimeUnspecified(), Is.EqualTo(value));
            Assert.That(dateTime.AddDuration(factory.CreateDurationFromTimeSpan(TimeSpan.FromHours(25))).ToDateTimeUnspecified(), Is.EqualTo(value.AddHours(25)));
        }
    }

    [Test]
    public void TimeOnlyAndTimeSpan_ExactValuesAndRolloverMatch()
    {
        ITimeFactory factory = TimeApi.Factory;
        TimeOnly[] values = new TimeOnly[]
        {
            TimeOnly.MinValue,
            new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(7890L)),
            TimeOnly.MaxValue
        };
        TimeSpan[] deltas = new TimeSpan[]
        {
            TimeSpan.FromTicks(1L),
            TimeSpan.FromMinutes(90),
            TimeSpan.FromHours(-25),
            TimeSpan.FromDays(3).Add(TimeSpan.FromTicks(-1L))
        };

        foreach (TimeOnly value in values)
        {
            LocalTime time = factory.CreateLocalTimeFromTimeOnly(value);
            Assert.That(time.ToTimeOnly(), Is.EqualTo(value));

            foreach (TimeSpan delta in deltas)
            {
                int expectedDays;
                TimeOnly expected = value.Add(delta, out expectedDays);
                Duration duration = factory.CreateDurationFromTimeSpan(delta);
                LocalTimeRoll actual = time.AddDuration(duration);

                Assert.That(duration.ToTimeSpan(), Is.EqualTo(delta));
                Assert.That(actual.GetDayDelta(), Is.EqualTo(expectedDays));
                Assert.That(actual.GetTime().ToTimeOnly(), Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void DateTimeOffset_InstantAndOffsetDateTimeConversionsMatch()
    {
        ITimeFactory factory = TimeApi.Factory;
        DateTimeOffset[] values = new DateTimeOffset[]
        {
            DateTimeOffset.MinValue,
            new DateTimeOffset(1969, 12, 31, 23, 59, 59, TimeSpan.Zero).AddTicks(9999999L),
            new DateTimeOffset(2024, 2, 29, 23, 45, 1, TimeSpan.FromMinutes(345)).AddTicks(2345678L),
            DateTimeOffset.MaxValue
        };

        foreach (DateTimeOffset value in values)
        {
            Instant instant = factory.CreateInstantFromDateTimeOffset(value);
            OffsetDateTime offsetDateTime = factory.CreateOffsetDateTimeFromDateTimeOffset(value);

            Assert.That(instant.GetUtcTicks(), Is.EqualTo(value.UtcTicks));
            Assert.That(instant.ToDateTimeOffset(), Is.EqualTo(value.ToUniversalTime()));
            Assert.That(offsetDateTime.ToDateTimeOffset(), Is.EqualTo(value));
            Assert.That(offsetDateTime.GetInstant().GetUtcTicks(), Is.EqualTo(value.UtcTicks));
        }
    }

    [Test]
    public void TimeZoneInfo_OffsetsAndConversionsMatchAcrossAvailableZones()
    {
        Instant[] instants = new Instant[]
        {
            Instant.Parse("1900-01-01T00:00:00Z"),
            Instant.Parse("2024-01-15T12:00:00Z"),
            Instant.Parse("2024-07-15T12:00:00Z"),
            Instant.Parse("2099-12-31T23:59:59Z")
        };
        int checkedZones = 0;

        foreach (TimeZoneInfo zoneInfo in TimeZoneInfo.GetSystemTimeZones())
        {
            TimeZoneRef zone = new TimeZoneRef(zoneInfo);
            foreach (Instant instant in instants)
            {
                DateTimeOffset value = instant.ToDateTimeOffset();
                DateTimeOffset expected = TimeZoneInfo.ConvertTime(value, zoneInfo);
                OffsetDateTime actual = zone.ConvertInstant(instant);

                Assert.That(zone.GetOffset(instant).ToTimeSpan(), Is.EqualTo(zoneInfo.GetUtcOffset(value)));
                Assert.That(actual.GetLocalDateTime().ToDateTimeUnspecified(), Is.EqualTo(expected.DateTime));
                Assert.That(actual.GetOffset().ToTimeSpan(), Is.EqualTo(expected.Offset));
            }

            checkedZones = checkedZones + 1;
            if (checkedZones == 8)
            {
                break;
            }
        }

        Assert.That(checkedZones, Is.GreaterThan(0));
    }

    [Test]
    public void IsoWeek_MatchesAcrossRepresentativeYearBoundaries()
    {
        WeekRules rules = TimeApi.Factory.CreateIsoWeekRules();

        for (int year = 1900; year <= 2100; year = year + 5)
        {
            DateTime first = new DateTime(year, 1, 1);
            DateTime last = new DateTime(year, 12, 31);
            AssertIsoWeekMatches(rules, first);
            AssertIsoWeekMatches(rules, first.AddDays(3));
            AssertIsoWeekMatches(rules, last.AddDays(-3));
            AssertIsoWeekMatches(rules, last);
        }
    }

    [Test]
    public void TimeProviderSystem_AndSystemClockObserveTheSameUtcTimeline()
    {
        DateTimeOffset providerValue = TimeProvider.System.GetUtcNow();
        Instant clockValue = TimeApi.Factory.CreateSystemTimeClock().GetCurrentInstant();
        long tickDifference = clockValue.GetUtcTicks() - providerValue.UtcTicks;
        if (tickDifference < 0L)
        {
            tickDifference = -tickDifference;
        }

        Assert.That(tickDifference, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(5).Ticks));
        Assert.That(clockValue.ToDateTimeOffset().Offset, Is.EqualTo(TimeSpan.Zero));
    }

    static void AssertIsoWeekMatches(WeekRules rules, DateTime value)
    {
        LocalDate date = TimeApi.Factory.CreateLocalDateFromDateTime(value);

        Assert.That(rules.GetWeekYear(date), Is.EqualTo(ISOWeek.GetYear(value)));
        Assert.That(rules.GetWeekOfWeekYear(date), Is.EqualTo(ISOWeek.GetWeekOfYear(value)));
    }
}
