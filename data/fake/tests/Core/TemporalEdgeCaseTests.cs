using fake;

namespace fake.Tests;

[TestFixture]
public class TemporalEdgeCaseTests
{
    [Test]
    public void LeapDayRemainsCorrelatedAcrossRangesAgesAndAnniversaries()
    {
        FakeOptions options = FakeOptions.Create(6201);
        options.ReferenceTime = new DateTimeOffset(2024, 2, 29, 12, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();
        DateOnly leapDay = new DateOnly(2024, 2, 29);

        Assert.Multiple(() =>
        {
            Assert.That(date.DateOnly(leapDay, leapDay.AddDays(1)), Is.EqualTo(leapDay));
            Assert.That(date.DateOnlyFromMinimumAndMaximumAndBoundary(leapDay, leapDay, DateBoundary.Inclusive), Is.EqualTo(leapDay));
            Assert.That(date.Age(new DateOnly(2000, 2, 29)), Is.EqualTo(24));
            Assert.That(date.Anniversary(new DateOnly(2020, 2, 29)), Is.EqualTo(leapDay));
            Assert.That(date.AnniversaryFromOriginalDateAndYears(leapDay, 1), Is.EqualTo(new DateOnly(2025, 2, 28)));
        });

        int index = 0;
        while (index < 512)
        {
            DateOnly birthDate = date.BirthDateFromAge(24);
            Assert.That(date.Age(birthDate), Is.EqualTo(24));
            index++;
        }
    }

    [Test]
    public void EndOfMonthBoundariesStayExactAcrossCalendarOperations()
    {
        FakeOptions options = FakeOptions.Create(6202);
        options.ReferenceTime = new DateTimeOffset(2024, 1, 31, 8, 0, 0, TimeSpan.FromHours(-5));
        DateProvider date = Fake.Create(options).Date();
        DateOnly januaryEnd = new DateOnly(2024, 1, 31);
        FiscalPeriod februaryQuarter = date.FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(2024, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(date.DateOnly(januaryEnd, januaryEnd.AddDays(1)), Is.EqualTo(januaryEnd));
            Assert.That(date.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.FromDays(1), TimeSpan.FromDays(1)), Is.EqualTo(options.ReferenceTime.AddDays(1)));
            Assert.That(date.AnniversaryFromOriginalDateAndYears(new DateOnly(2020, 2, 29), 1), Is.EqualTo(new DateOnly(2021, 2, 28)));
            Assert.That(date.AnniversaryFromOriginalDateAndYears(new DateOnly(2023, 1, 31), 1), Is.EqualTo(new DateOnly(2024, 1, 31)));
            Assert.That(februaryQuarter.Start, Is.EqualTo(new DateOnly(2024, 2, 1)));
            Assert.That(februaryQuarter.End, Is.EqualTo(new DateOnly(2024, 4, 30)));
        });
    }

    [Test]
    public void NewYorkSpringTransitionExposesTheGapAndSkipsLocalHourTwo()
    {
        DateProvider date = Fake.Create(6203).Date();
        TimeZoneInfo zone = date.TimeZoneFromIanaId("America/New_York");
        DateTime invalidLocal = new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Unspecified);
        DateTimeOffset beforeInstant = new DateTimeOffset(2024, 3, 10, 6, 59, 59, TimeSpan.Zero);
        DateTimeOffset afterInstant = new DateTimeOffset(2024, 3, 10, 7, 0, 0, TimeSpan.Zero);
        DateTimeOffset beforeLocal = date.InTimeZone("America/New_York", beforeInstant);
        DateTimeOffset afterLocal = date.InTimeZone("America/New_York", afterInstant);

        Assert.Multiple(() =>
        {
            Assert.That(zone.IsInvalidTime(invalidLocal), Is.True);
            Assert.That(beforeLocal.ToString("yyyy-MM-dd HH:mm:ss zzz"), Is.EqualTo("2024-03-10 01:59:59 -05:00"));
            Assert.That(afterLocal.ToString("yyyy-MM-dd HH:mm:ss zzz"), Is.EqualTo("2024-03-10 03:00:00 -04:00"));
            Assert.That(beforeLocal.UtcTicks, Is.EqualTo(beforeInstant.UtcTicks));
            Assert.That(afterLocal.UtcTicks, Is.EqualTo(afterInstant.UtcTicks));
        });
    }

    [Test]
    public void NewYorkAutumnTransitionExposesTheOverlapAtBothOffsets()
    {
        DateProvider date = Fake.Create(6204).Date();
        TimeZoneInfo zone = date.TimeZoneFromIanaId("America/New_York");
        DateTime ambiguousLocal = new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);
        DateTimeOffset daylightInstant = new DateTimeOffset(2024, 11, 3, 5, 30, 0, TimeSpan.Zero);
        DateTimeOffset standardInstant = new DateTimeOffset(2024, 11, 3, 6, 30, 0, TimeSpan.Zero);
        DateTimeOffset daylightLocal = date.InTimeZone("America/New_York", daylightInstant);
        DateTimeOffset standardLocal = date.InTimeZone("America/New_York", standardInstant);

        Assert.Multiple(() =>
        {
            Assert.That(zone.IsAmbiguousTime(ambiguousLocal), Is.True);
            Assert.That(daylightLocal.DateTime, Is.EqualTo(ambiguousLocal));
            Assert.That(standardLocal.DateTime, Is.EqualTo(ambiguousLocal));
            Assert.That(daylightLocal.Offset, Is.EqualTo(TimeSpan.FromHours(-4)));
            Assert.That(standardLocal.Offset, Is.EqualTo(TimeSpan.FromHours(-5)));
            Assert.That(standardLocal.UtcTicks - daylightLocal.UtcTicks, Is.EqualTo(TimeSpan.TicksPerHour));
        });
    }

    [Test]
    public void OffsetExtremesFormatAndGenerateWithoutChangingInstants()
    {
        DateProvider date = Fake.Create(6205).Date();
        DateTimeOffset positive = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.FromHours(14));
        DateTimeOffset negative = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.FromHours(-14));

        Assert.Multiple(() =>
        {
            Assert.That(date.Rfc3339FromValue(positive), Is.EqualTo("2024-01-02T12:00:00+14:00"));
            Assert.That(date.Rfc3339FromValue(negative), Is.EqualTo("2024-01-02T12:00:00-14:00"));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Pacific/Kiritimati", positive), Is.EqualTo(TimeSpan.FromHours(14)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Pacific/Pago_Pago", positive), Is.EqualTo(TimeSpan.FromHours(-11)));
        });

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset generated = date.Between(positive, negative);
            Assert.That(generated.UtcTicks, Is.InRange(positive.UtcTicks, negative.UtcTicks - 1L));
            Assert.That(generated.Offset, Is.EqualTo(TimeSpan.Zero));
            index++;
        }
    }

    [Test]
    public void UnixEpochBoundariesUseFlooringAndOneTickIntervalsExactly()
    {
        DateProvider date = Fake.Create(6206).Date();
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch;
        DateTimeOffset before = epoch.AddTicks(-1L);
        DateTimeOffset after = epoch.AddTicks(1L);

        Assert.Multiple(() =>
        {
            Assert.That(date.Between(epoch, after), Is.EqualTo(epoch));
            Assert.That(date.Between(before, epoch), Is.EqualTo(before));
            Assert.That(date.UnixSecondsFromValue(before), Is.EqualTo(-1L));
            Assert.That(date.UnixMillisecondsFromValue(before), Is.EqualTo(-1L));
            Assert.That(date.UnixSecondsFromValue(epoch), Is.Zero);
            Assert.That(date.UnixMillisecondsFromValue(epoch), Is.Zero);
            Assert.That(date.Rfc3339FromValueAndPrecision(epoch, TemporalPrecision.Ticks), Is.EqualTo("1970-01-01T00:00:00.0000000Z"));
        });
    }

    [Test]
    public void RuntimeMinimumAndMaximumValuesRoundTripAtSupportedPrecision()
    {
        DateProvider date = Fake.Create(6207).Date();

        Assert.Multiple(() =>
        {
            Assert.That(date.DateTimeOffsetFromMinimumAndMaximumAndBoundary(DateTimeOffset.MinValue, DateTimeOffset.MinValue, DateBoundary.Inclusive), Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(date.DateTimeOffsetFromMinimumAndMaximumAndBoundary(DateTimeOffset.MaxValue, DateTimeOffset.MaxValue, DateBoundary.Inclusive), Is.EqualTo(DateTimeOffset.MaxValue));
            Assert.That(date.DateOnlyFromMinimumAndMaximumAndBoundary(DateOnly.MinValue, DateOnly.MinValue, DateBoundary.Inclusive), Is.EqualTo(DateOnly.MinValue));
            Assert.That(date.DateOnlyFromMinimumAndMaximumAndBoundary(DateOnly.MaxValue, DateOnly.MaxValue, DateBoundary.Inclusive), Is.EqualTo(DateOnly.MaxValue));
            Assert.That(date.TimeOnlyFromMinimumAndMaximumAndBoundary(TimeOnly.MinValue, TimeOnly.MinValue, DateBoundary.Inclusive), Is.EqualTo(TimeOnly.MinValue));
            Assert.That(date.TimeOnlyFromMinimumAndMaximumAndBoundary(TimeOnly.MaxValue, TimeOnly.MaxValue, DateBoundary.Inclusive), Is.EqualTo(TimeOnly.MaxValue));
            Assert.That(date.TimeSpanFromMinimumAndMaximumAndBoundary(TimeSpan.MinValue, TimeSpan.MinValue, DateBoundary.Inclusive), Is.EqualTo(TimeSpan.MinValue));
            Assert.That(date.TimeSpanFromMinimumAndMaximumAndBoundary(TimeSpan.MaxValue, TimeSpan.MaxValue, DateBoundary.Inclusive), Is.EqualTo(TimeSpan.MaxValue));
            Assert.That(date.Rfc3339FromValue(DateTimeOffset.MinValue), Is.EqualTo("0001-01-01T00:00:00Z"));
            Assert.That(date.Rfc3339FromValueAndPrecision(DateTimeOffset.MaxValue, TemporalPrecision.Ticks), Is.EqualTo("9999-12-31T23:59:59.9999999Z"));
            Assert.That(date.UnixSecondsFromValue(DateTimeOffset.MinValue), Is.EqualTo(-62135596800L));
            Assert.That(date.UnixSecondsFromValue(DateTimeOffset.MaxValue), Is.EqualTo(253402300799L));
            Assert.That(date.UnixMillisecondsFromValue(DateTimeOffset.MinValue), Is.EqualTo(-62135596800000L));
            Assert.That(date.UnixMillisecondsFromValue(DateTimeOffset.MaxValue), Is.EqualTo(253402300799999L));
        });
    }

    [Test]
    public void ReversedEmptyAndCategoryFreeRangesFailWithoutEntropy()
    {
        DateProvider actual = Fake.Create(6208).Date();
        DateProvider expected = Fake.Create(6208).Date();
        DateOnly monday = new DateOnly(2024, 1, 1);
        DateOnly saturday = monday.AddDays(5);
        DateTimeOffset instant = DateTimeOffset.UnixEpoch;

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Between(instant, instant)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BetweenFromMinimumAndMaximumAndBoundary(instant, instant.AddTicks(-1L), DateBoundary.Inclusive)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Betweens(instant, instant, 0)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DateOnly(monday, monday)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.OrderedInterval(instant, instant.AddTicks(-1L))).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BusinessDayFromMinimumAndMaximum(monday, monday)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.WeekendFromMinimumAndMaximum(saturday, saturday)).ParamName, Is.EqualTo("maximum"));
            Assert.That(() => actual.BusinessDayFromMinimumAndMaximum(saturday, saturday.AddDays(2)), Throws.TypeOf<FakeException>());
            Assert.That(() => actual.WeekendFromMinimumAndMaximum(monday, saturday), Throws.TypeOf<FakeException>());
        });

        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }
}
