using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceExampleTests
{
    [Test]
    public void Rfc5545Style_DailyForTenOccurrences()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(1997, 9, 2, 9, 0, 0),
            RecurrenceRule.Parse("FREQ=DAILY;COUNT=10"),
            20);

        Assert.That(result.Length, Is.EqualTo(10));
        AssertFields(result[0], 1997, 9, 2, 9, 0, 0);
        AssertFields(result[1], 1997, 9, 3, 9, 0, 0);
        AssertFields(result[2], 1997, 9, 4, 9, 0, 0);
        AssertFields(result[3], 1997, 9, 5, 9, 0, 0);
        AssertFields(result[4], 1997, 9, 6, 9, 0, 0);
        AssertFields(result[5], 1997, 9, 7, 9, 0, 0);
        AssertFields(result[6], 1997, 9, 8, 9, 0, 0);
        AssertFields(result[7], 1997, 9, 9, 9, 0, 0);
        AssertFields(result[8], 1997, 9, 10, 9, 0, 0);
        AssertFields(result[9], 1997, 9, 11, 9, 0, 0);
    }

    [Test]
    public void Rfc5545Style_EveryOtherDayBounded()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(1997, 9, 2, 9, 0, 0),
            RecurrenceRule.Parse("FREQ=DAILY;INTERVAL=2"),
            5);

        Assert.That(result.Length, Is.EqualTo(5));
        AssertFields(result[0], 1997, 9, 2, 9, 0, 0);
        AssertFields(result[1], 1997, 9, 4, 9, 0, 0);
        AssertFields(result[2], 1997, 9, 6, 9, 0, 0);
        AssertFields(result[3], 1997, 9, 8, 9, 0, 0);
        AssertFields(result[4], 1997, 9, 10, 9, 0, 0);
    }

    [Test]
    public void Rfc5545Style_WeeklyForTenOccurrences()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(1997, 9, 2, 9, 0, 0),
            RecurrenceRule.Parse("FREQ=WEEKLY;COUNT=10"),
            20);

        Assert.That(result.Length, Is.EqualTo(10));
        AssertFields(result[0], 1997, 9, 2, 9, 0, 0);
        AssertFields(result[1], 1997, 9, 9, 9, 0, 0);
        AssertFields(result[2], 1997, 9, 16, 9, 0, 0);
        AssertFields(result[3], 1997, 9, 23, 9, 0, 0);
        AssertFields(result[4], 1997, 9, 30, 9, 0, 0);
        AssertFields(result[5], 1997, 10, 7, 9, 0, 0);
        AssertFields(result[6], 1997, 10, 14, 9, 0, 0);
        AssertFields(result[7], 1997, 10, 21, 9, 0, 0);
        AssertFields(result[8], 1997, 10, 28, 9, 0, 0);
        AssertFields(result[9], 1997, 11, 4, 9, 0, 0);
    }

    [Test]
    public void Rfc5545Style_EveryOtherWeekBounded()
    {
        LocalDateTime[] result = RecurrenceEnumerator.EnumerateLocal(
            LocalDateTime.Create(1997, 9, 2, 9, 0, 0),
            RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2"),
            5);

        Assert.That(result.Length, Is.EqualTo(5));
        AssertFields(result[0], 1997, 9, 2, 9, 0, 0);
        AssertFields(result[1], 1997, 9, 16, 9, 0, 0);
        AssertFields(result[2], 1997, 9, 30, 9, 0, 0);
        AssertFields(result[3], 1997, 10, 14, 9, 0, 0);
        AssertFields(result[4], 1997, 10, 28, 9, 0, 0);
    }

    [Test]
    public void DstSpecific_ZonedRecurrencePreservesWallClockAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());

        ZonedDateTime[] result = RecurrenceEnumerator.EnumerateZoned(
            LocalDateTime.Create(2024, 3, 9, 12, 0, 0),
            RecurrenceRule.Parse("FREQ=DAILY;COUNT=3"),
            zone,
            ZoneResolver.Compatible(),
            10);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-09T12:00:00"));
        Assert.That(result[1].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T12:00:00"));
        Assert.That(result[2].GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-11T12:00:00"));
        Assert.That(result[0].GetInstant().ToIsoString(), Is.EqualTo("2024-03-09T17:00:00Z"));
        Assert.That(result[1].GetInstant().ToIsoString(), Is.EqualTo("2024-03-10T16:00:00Z"));
        Assert.That(result[2].GetInstant().ToIsoString(), Is.EqualTo("2024-03-11T16:00:00Z"));
    }

    [Test]
    public void DstSpecific_InstantRecurrenceUsesExactElapsedTimeAcrossSpringForward()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant start = ZonedDateTime.CreateFromLocal(LocalDateTime.Create(2024, 3, 9, 12, 0, 0), zone, ZoneResolver.Strict()).GetInstant();

        Instant[] result = RecurrenceEnumerator.EnumerateInstants(
            start,
            RecurrenceRule.Parse("FREQ=DAILY;COUNT=3"),
            10);
        ZonedDateTime second = ZonedDateTime.CreateFromInstant(result[1], zone);
        ZonedDateTime third = ZonedDateTime.CreateFromInstant(result[2], zone);

        Assert.That(result.Length, Is.EqualTo(3));
        Assert.That(result[0].ToIsoString(), Is.EqualTo("2024-03-09T17:00:00Z"));
        Assert.That(result[1].ToIsoString(), Is.EqualTo("2024-03-10T17:00:00Z"));
        Assert.That(result[2].ToIsoString(), Is.EqualTo("2024-03-11T17:00:00Z"));
        Assert.That(second.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T13:00:00"));
        Assert.That(third.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-11T13:00:00"));
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
            "time-library-test-recurrence-examples",
            TimeSpan.FromHours(-5),
            "Test recurrence example zone",
            "Test standard",
            "Test daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }
}
