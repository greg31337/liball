using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class TimeConstantsTests
{
    [Test]
    public void TickConstants_MatchTimeSpanUnits()
    {
        Assert.That(TimeConstants.TicksPerMicrosecond, Is.EqualTo(TimeSpan.TicksPerMicrosecond));
        Assert.That(TimeConstants.TicksPerMillisecond, Is.EqualTo(TimeSpan.TicksPerMillisecond));
        Assert.That(TimeConstants.TicksPerSecond, Is.EqualTo(TimeSpan.TicksPerSecond));
        Assert.That(TimeConstants.TicksPerMinute, Is.EqualTo(TimeSpan.TicksPerMinute));
        Assert.That(TimeConstants.TicksPerHour, Is.EqualTo(TimeSpan.TicksPerHour));
        Assert.That(TimeConstants.TicksPerDay, Is.EqualTo(TimeSpan.TicksPerDay));
        Assert.That(TimeConstants.TicksPerWeek, Is.EqualTo(TimeSpan.TicksPerDay * TimeConstants.DaysPerWeek));
        Assert.That(TimeConstants.MinTicksSinceMidnight, Is.EqualTo(0L));
        Assert.That(TimeConstants.MaxTicksSinceMidnight, Is.EqualTo(TimeSpan.TicksPerDay - 1L));
        Assert.That(DateTime.MinValue.Ticks, Is.EqualTo(TimeConstants.MinUtcTicks));
        Assert.That(DateTime.MaxValue.Ticks, Is.EqualTo(TimeConstants.MaxUtcTicks));
        Assert.That(DateTime.UnixEpoch.Ticks, Is.EqualTo(TimeConstants.UnixEpochUtcTicks));
    }

    [Test]
    public void UnitConstants_FormExpectedLargerUnits()
    {
        Assert.That(TimeConstants.TicksPerMillisecond, Is.EqualTo(TimeConstants.TicksPerMicrosecond * TimeConstants.MicrosecondsPerMillisecond));
        Assert.That(TimeConstants.TicksPerSecond, Is.EqualTo(TimeConstants.TicksPerMillisecond * TimeConstants.MillisecondsPerSecond));
        Assert.That(TimeConstants.TicksPerMinute, Is.EqualTo(TimeConstants.TicksPerSecond * TimeConstants.SecondsPerMinute));
        Assert.That(TimeConstants.TicksPerHour, Is.EqualTo(TimeConstants.TicksPerMinute * TimeConstants.MinutesPerHour));
        Assert.That(TimeConstants.TicksPerDay, Is.EqualTo(TimeConstants.TicksPerHour * TimeConstants.HoursPerDay));
        Assert.That(TimeConstants.TicksPerWeek, Is.EqualTo(TimeConstants.TicksPerDay * TimeConstants.DaysPerWeek));
    }

    [Test]
    public void CalendarConstants_MatchIsoGregorianCalendarBasics()
    {
        Assert.That(TimeConstants.MonthsPerYear, Is.EqualTo(12));
        Assert.That(TimeConstants.DaysPerWeek, Is.EqualTo(7));
        Assert.That(TimeConstants.DaysPerCommonYear, Is.EqualTo(365));
        Assert.That(TimeConstants.DaysPerLeapYear, Is.EqualTo(366));
        Assert.That(TimeConstants.IsoMonday, Is.EqualTo((int)DayOfWeek.Monday));
        Assert.That(TimeConstants.IsoTuesday, Is.EqualTo((int)DayOfWeek.Tuesday));
        Assert.That(TimeConstants.IsoWednesday, Is.EqualTo((int)DayOfWeek.Wednesday));
        Assert.That(TimeConstants.IsoThursday, Is.EqualTo((int)DayOfWeek.Thursday));
        Assert.That(TimeConstants.IsoFriday, Is.EqualTo((int)DayOfWeek.Friday));
        Assert.That(TimeConstants.IsoSaturday, Is.EqualTo((int)DayOfWeek.Saturday));
        Assert.That(TimeConstants.IsoSunday, Is.EqualTo(7));
    }

    [Test]
    public void RangeConstants_MatchDateOnlySupportedRange()
    {
        Assert.That(DateOnly.MinValue.Year, Is.EqualTo(TimeConstants.MinYear));
        Assert.That(DateOnly.MaxValue.Year, Is.EqualTo(TimeConstants.MaxYear));
        Assert.That(TimeConstants.MinMonth, Is.EqualTo(1));
        Assert.That(TimeConstants.MaxMonth, Is.EqualTo(TimeConstants.MonthsPerYear));
        Assert.That(TimeConstants.MinDay, Is.EqualTo(1));
        Assert.That(TimeConstants.MaxDay, Is.EqualTo(31));
        Assert.That(DateOnly.MinValue.DayNumber, Is.EqualTo(TimeConstants.MinDayNumber));
        Assert.That(DateOnly.MaxValue.DayNumber, Is.EqualTo(TimeConstants.MaxDayNumber));
    }
}
