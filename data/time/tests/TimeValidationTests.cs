using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class TimeValidationTests
{
    [Test]
    public void RequireNotNull_AllowsNonNullValues()
    {
        TimeValidation.RequireNotNull("value", "sample");
    }

    [Test]
    public void RequireNotNull_ThrowsArgumentNullExceptionForNull()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TimeValidation.RequireNotNull(null, "sample"))!;

        Assert.That(exception.ParamName, Is.EqualTo("sample"));
    }

    [TestCase(1)]
    [TestCase(2024)]
    [TestCase(9999)]
    public void RequireYear_AllowsSupportedYears(int year)
    {
        TimeValidation.RequireYear(year, "year");
    }

    [TestCase(0)]
    [TestCase(10000)]
    public void RequireYear_RejectsUnsupportedYears(int year)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireYear(year, "year"))!;

        Assert.That(exception.ParamName, Is.EqualTo("year"));
    }

    [TestCase(1)]
    [TestCase(12)]
    public void RequireMonth_AllowsSupportedMonths(int month)
    {
        TimeValidation.RequireMonth(month, "month");
    }

    [TestCase(0)]
    [TestCase(13)]
    public void RequireMonth_RejectsUnsupportedMonths(int month)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMonth(month, "month"))!;

        Assert.That(exception.ParamName, Is.EqualTo("month"));
    }

    [TestCase(2024, 2, 29)]
    [TestCase(2023, 2, 28)]
    [TestCase(9999, 12, 31)]
    public void RequireDay_AllowsValidCalendarDays(int year, int month, int day)
    {
        TimeValidation.RequireDay(year, month, day, "day");
    }

    [TestCase(2023, 2, 29)]
    [TestCase(2024, 4, 31)]
    [TestCase(1, 1, 0)]
    public void RequireDay_RejectsInvalidCalendarDays(int year, int month, int day)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireDay(year, month, day, "day"))!;

        Assert.That(exception.ParamName, Is.EqualTo("day"));
    }

    [Test]
    public void RequireDay_RejectsInvalidYearBeforeCalculatingMonthLength()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireDay(10000, 1, 1, "day"))!;

        Assert.That(exception.ParamName, Is.EqualTo("year"));
    }

    [Test]
    public void RequireDay_RejectsInvalidMonthBeforeCalculatingMonthLength()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireDay(2024, 13, 1, "day"))!;

        Assert.That(exception.ParamName, Is.EqualTo("month"));
    }

    [TestCase(0)]
    [TestCase(3652058)]
    public void RequireDayNumber_AllowsSupportedLocalDateRange(int dayNumber)
    {
        TimeValidation.RequireDayNumber(dayNumber, "dayNumber");
    }

    [TestCase(-1)]
    [TestCase(3652059)]
    public void RequireDayNumber_RejectsValuesOutsideLocalDateRange(int dayNumber)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireDayNumber(dayNumber, "dayNumber"))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayNumber"));
    }

    [TestCase(0, 0, 0, 0, 0, 0)]
    [TestCase(23, 59, 59, 999, 999, 9)]
    public void RequireTime_AllowsSupportedClockFields(int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        TimeValidation.RequireTime(hour, minute, second, millisecond, microsecond, tickRemainder);
    }

    [TestCase(-1, 0, 0, 0, 0, 0, "hour")]
    [TestCase(24, 0, 0, 0, 0, 0, "hour")]
    [TestCase(0, -1, 0, 0, 0, 0, "minute")]
    [TestCase(0, 60, 0, 0, 0, 0, "minute")]
    [TestCase(0, 0, -1, 0, 0, 0, "second")]
    [TestCase(0, 0, 60, 0, 0, 0, "second")]
    [TestCase(0, 0, 0, -1, 0, 0, "millisecond")]
    [TestCase(0, 0, 0, 1000, 0, 0, "millisecond")]
    [TestCase(0, 0, 0, 0, -1, 0, "microsecond")]
    [TestCase(0, 0, 0, 0, 1000, 0, "microsecond")]
    [TestCase(0, 0, 0, 0, 0, -1, "tickRemainder")]
    [TestCase(0, 0, 0, 0, 0, 10, "tickRemainder")]
    public void RequireTime_RejectsInvalidClockFields(
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder,
        string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireTime(hour, minute, second, millisecond, microsecond, tickRemainder))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [Test]
    public void IndividualClockFieldValidators_AllowSupportedRanges()
    {
        TimeValidation.RequireHour(0, "hour");
        TimeValidation.RequireHour(23, "hour");
        TimeValidation.RequireMinute(0, "minute");
        TimeValidation.RequireMinute(59, "minute");
        TimeValidation.RequireSecond(0, "second");
        TimeValidation.RequireSecond(59, "second");
        TimeValidation.RequireMillisecond(0, "millisecond");
        TimeValidation.RequireMillisecond(999, "millisecond");
        TimeValidation.RequireMicrosecond(0, "microsecond");
        TimeValidation.RequireMicrosecond(999, "microsecond");
        TimeValidation.RequireTickRemainder(0, "tickRemainder");
        TimeValidation.RequireTickRemainder(9, "tickRemainder");
    }

    [Test]
    public void IndividualClockFieldValidators_RejectValuesOutsideSupportedRanges()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireHour(-1, "hour"))!.ParamName, Is.EqualTo("hour"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireHour(24, "hour"))!.ParamName, Is.EqualTo("hour"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMinute(-1, "minute"))!.ParamName, Is.EqualTo("minute"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMinute(60, "minute"))!.ParamName, Is.EqualTo("minute"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireSecond(-1, "second"))!.ParamName, Is.EqualTo("second"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireSecond(60, "second"))!.ParamName, Is.EqualTo("second"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMillisecond(-1, "millisecond"))!.ParamName, Is.EqualTo("millisecond"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMillisecond(1000, "millisecond"))!.ParamName, Is.EqualTo("millisecond"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMicrosecond(-1, "microsecond"))!.ParamName, Is.EqualTo("microsecond"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireMicrosecond(1000, "microsecond"))!.ParamName, Is.EqualTo("microsecond"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireTickRemainder(-1, "tickRemainder"))!.ParamName, Is.EqualTo("tickRemainder"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireTickRemainder(10, "tickRemainder"))!.ParamName, Is.EqualTo("tickRemainder"));
    }

    [TestCase(0L)]
    [TestCase(863999999999L)]
    public void RequireTicksSinceMidnight_AllowsTicksWithinSingleDay(long ticksSinceMidnight)
    {
        TimeValidation.RequireTicksSinceMidnight(ticksSinceMidnight, "ticksSinceMidnight");
    }

    [TestCase(-1L)]
    [TestCase(864000000000L)]
    public void RequireTicksSinceMidnight_RejectsTicksOutsideSingleDay(long ticksSinceMidnight)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireTicksSinceMidnight(ticksSinceMidnight, "ticksSinceMidnight"))!;

        Assert.That(exception.ParamName, Is.EqualTo("ticksSinceMidnight"));
    }

    [TestCase(-64800)]
    [TestCase(0)]
    [TestCase(64800)]
    public void RequireOffsetSeconds_AllowsRealisticOffsetRange(int offsetSeconds)
    {
        TimeValidation.RequireOffsetSeconds(offsetSeconds, "offsetSeconds");
    }

    [TestCase(-64801)]
    [TestCase(64801)]
    public void RequireOffsetSeconds_RejectsValuesOutsideRealisticOffsetRange(int offsetSeconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireOffsetSeconds(offsetSeconds, "offsetSeconds"))!;

        Assert.That(exception.ParamName, Is.EqualTo("offsetSeconds"));
    }

    [TestCase(1)]
    [TestCase(7)]
    public void RequireIsoDayOfWeek_AllowsIsoWeekdayRange(int dayOfWeek)
    {
        TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek");
    }

    [TestCase(0)]
    [TestCase(8)]
    public void RequireIsoDayOfWeek_RejectsValuesOutsideIsoWeekdayRange(int dayOfWeek)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeValidation.RequireIsoDayOfWeek(dayOfWeek, "dayOfWeek"))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayOfWeek"));
    }
}
