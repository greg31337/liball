using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class LocalTimeTests
{
    [Test]
    public void Create_StoresValidatedClockFields()
    {
        LocalTime time = LocalTime.Create(23, 59, 58, 987, 654, 3);

        Assert.That(time.GetHour(), Is.EqualTo(23));
        Assert.That(time.GetMinute(), Is.EqualTo(59));
        Assert.That(time.GetSecond(), Is.EqualTo(58));
        Assert.That(time.GetMillisecond(), Is.EqualTo(987));
        Assert.That(time.GetMicrosecond(), Is.EqualTo(654));
        Assert.That(time.GetTickRemainder(), Is.EqualTo(3));
    }

    [Test]
    public void Create_WithHourMinuteSecond_DefaultsFractionalFieldsToZero()
    {
        LocalTime time = LocalTime.Create(1, 2, 3);

        Assert.That(time.GetHour(), Is.EqualTo(1));
        Assert.That(time.GetMinute(), Is.EqualTo(2));
        Assert.That(time.GetSecond(), Is.EqualTo(3));
        Assert.That(time.GetMillisecond(), Is.EqualTo(0));
        Assert.That(time.GetMicrosecond(), Is.EqualTo(0));
        Assert.That(time.GetTickRemainder(), Is.EqualTo(0));
    }

    [TestCase(-1, 0, 0, 0, 0, 0, "hour")]
    [TestCase(24, 0, 0, 0, 0, 0, "hour")]
    [TestCase(0, 60, 0, 0, 0, 0, "minute")]
    [TestCase(0, 0, 60, 0, 0, 0, "second")]
    [TestCase(0, 0, 0, 1000, 0, 0, "millisecond")]
    [TestCase(0, 0, 0, 0, 1000, 0, "microsecond")]
    [TestCase(0, 0, 0, 0, 0, 10, "tickRemainder")]
    public void Create_RejectsInvalidClockFields(
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder,
        string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => LocalTime.Create(hour, minute, second, millisecond, microsecond, tickRemainder))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase(0L, 0, 0, 0, 0, 0, 0)]
    [TestCase(432000000000L, 12, 0, 0, 0, 0, 0)]
    [TestCase(863999999999L, 23, 59, 59, 999, 999, 9)]
    public void FromTicksSinceMidnight_DecomposesClockFields(long ticks, int hour, int minute, int second, int millisecond, int microsecond, int tickRemainder)
    {
        LocalTime time = LocalTime.FromTicksSinceMidnight(ticks);

        Assert.That(time.GetHour(), Is.EqualTo(hour));
        Assert.That(time.GetMinute(), Is.EqualTo(minute));
        Assert.That(time.GetSecond(), Is.EqualTo(second));
        Assert.That(time.GetMillisecond(), Is.EqualTo(millisecond));
        Assert.That(time.GetMicrosecond(), Is.EqualTo(microsecond));
        Assert.That(time.GetTickRemainder(), Is.EqualTo(tickRemainder));
        Assert.That(time.GetTicksSinceMidnight(), Is.EqualTo(ticks));
    }

    [TestCase(-1L)]
    [TestCase(864000000000L)]
    public void FromTicksSinceMidnight_RejectsTicksOutsideSingleDay(long ticks)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => LocalTime.FromTicksSinceMidnight(ticks))!;

        Assert.That(exception.ParamName, Is.EqualTo("ticksSinceMidnight"));
    }

    [Test]
    public void MidnightAndNoon_ReturnExpectedTimes()
    {
        Assert.That(LocalTime.Midnight().GetTicksSinceMidnight(), Is.EqualTo(0L));
        Assert.That(LocalTime.Noon().GetTicksSinceMidnight(), Is.EqualTo(432000000000L));
    }

    [Test]
    public void FromTimeOnly_CopiesTicks()
    {
        TimeOnly timeOnly = new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(7890));
        LocalTime time = LocalTime.FromTimeOnly(timeOnly);

        Assert.That(time.GetTicksSinceMidnight(), Is.EqualTo(timeOnly.Ticks));
    }

    [Test]
    public void ToTimeOnly_ReturnsEquivalentTimeOnly()
    {
        LocalTime time = LocalTime.Create(12, 34, 56, 789, 123, 4);
        TimeOnly timeOnly = time.ToTimeOnly();

        Assert.That(timeOnly.Ticks, Is.EqualTo(time.GetTicksSinceMidnight()));
    }

    [TestCase(0, 0, 0, 0, 0, 0, "00:00:00")]
    [TestCase(12, 34, 56, 0, 0, 0, "12:34:56")]
    [TestCase(12, 34, 56, 789, 0, 0, "12:34:56.789")]
    [TestCase(12, 34, 56, 789, 123, 0, "12:34:56.789123")]
    [TestCase(23, 59, 59, 999, 999, 9, "23:59:59.9999999")]
    public void ToIsoString_FormatsTimeWithOptionalFraction(
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder,
        string expected)
    {
        LocalTime time = LocalTime.Create(hour, minute, second, millisecond, microsecond, tickRemainder);

        Assert.That(time.ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("00:00:00", 0, 0, 0, 0, 0, 0)]
    [TestCase("12:34:56", 12, 34, 56, 0, 0, 0)]
    [TestCase("12:34:56.7", 12, 34, 56, 700, 0, 0)]
    [TestCase("12:34:56.7891234", 12, 34, 56, 789, 123, 4)]
    [TestCase("23:59:59.9999999", 23, 59, 59, 999, 999, 9)]
    public void Parse_ReadsIsoTimeText(
        string text,
        int hour,
        int minute,
        int second,
        int millisecond,
        int microsecond,
        int tickRemainder)
    {
        LocalTime time = LocalTime.Parse(text);

        Assert.That(time.GetHour(), Is.EqualTo(hour));
        Assert.That(time.GetMinute(), Is.EqualTo(minute));
        Assert.That(time.GetSecond(), Is.EqualTo(second));
        Assert.That(time.GetMillisecond(), Is.EqualTo(millisecond));
        Assert.That(time.GetMicrosecond(), Is.EqualTo(microsecond));
        Assert.That(time.GetTickRemainder(), Is.EqualTo(tickRemainder));
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalTime.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("1:02:03")]
    [TestCase("01:02")]
    [TestCase("01:02:03.")]
    [TestCase("01:02:03X1")]
    [TestCase("01-02-03")]
    [TestCase("aa:02:03")]
    [TestCase("01:bb:03")]
    [TestCase("01:02:cc")]
    [TestCase("24:00:00")]
    [TestCase("23:60:00")]
    [TestCase("23:59:60")]
    [TestCase("23:59:59.12345678")]
    [TestCase("23:59:59.123x")]
    public void Parse_RejectsMalformedOrInvalidIsoTimeText(string text)
    {
        Assert.Throws<FormatException>(() => LocalTime.Parse(text));
    }

    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(452967891234L)]
    [TestCase(863999999999L)]
    public void Parse_RoundTripsFormattedTimes(long ticksSinceMidnight)
    {
        LocalTime original = LocalTime.FromTicksSinceMidnight(ticksSinceMidnight);
        LocalTime parsed = LocalTime.Parse(original.ToIsoString());

        Assert.That(parsed.EqualsValue(original), Is.True);
    }

    [Test]
    public void CompareTo_OrdersByTicksSinceMidnight()
    {
        Assert.That(LocalTime.Create(1, 0, 0).CompareTo(LocalTime.Create(2, 0, 0)), Is.EqualTo(-1));
        Assert.That(LocalTime.Create(1, 0, 0).CompareTo(LocalTime.Create(1, 0, 0)), Is.EqualTo(0));
        Assert.That(LocalTime.Create(2, 0, 0).CompareTo(LocalTime.Create(1, 0, 0)), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_RequiresOtherTime()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalTime.Midnight().CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesTicksAndHandlesNull()
    {
        LocalTime time = LocalTime.Create(1, 2, 3, 4, 5, 6);

        Assert.That(time.EqualsValue(LocalTime.FromTicksSinceMidnight(time.GetTicksSinceMidnight())), Is.True);
        Assert.That(time.EqualsValue(LocalTime.Create(1, 2, 3, 4, 5, 7)), Is.False);
        Assert.That(time.EqualsValue(null), Is.False);
    }

    [Test]
    public void AddDuration_ReturnsSameDayRollWhenResultStaysInDay()
    {
        LocalTimeRoll roll = LocalTime.Create(10, 0, 0).AddDuration(Duration.FromMinutes(90));

        AssertTimeRoll(roll, 11, 30, 0, 0);
    }

    [Test]
    public void AddDuration_RollsForwardAcrossMidnight()
    {
        LocalTimeRoll roll = LocalTime.Create(23, 30, 0).AddDuration(Duration.FromHours(2));

        AssertTimeRoll(roll, 1, 30, 0, 1);
    }

    [Test]
    public void AddDuration_RollsBackwardAcrossMidnight()
    {
        LocalTimeRoll roll = LocalTime.Create(0, 30, 0).AddDuration(Duration.FromHours(-1));

        AssertTimeRoll(roll, 23, 30, 0, -1);
    }

    [Test]
    public void AddDuration_HandlesMultipleDayRolls()
    {
        Duration duration = Duration.FromDays(3).Add(Duration.FromHours(1));
        LocalTimeRoll roll = LocalTime.Noon().AddDuration(duration);

        AssertTimeRoll(roll, 13, 0, 0, 3);
    }

    [Test]
    public void AddDuration_RequiresDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalTime.Midnight().AddDuration(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void SubtractDuration_ReturnsRolledTime()
    {
        LocalTimeRoll roll = LocalTime.Create(0, 30, 0).SubtractDuration(Duration.FromHours(1));

        AssertTimeRoll(roll, 23, 30, 0, -1);
    }

    [Test]
    public void SubtractDuration_HandlesMinimumDurationWithoutNegationOverflow()
    {
        LocalTimeRoll roll = LocalTime.Midnight().SubtractDuration(Duration.FromTicks(long.MinValue));

        Assert.That(roll.GetDayDelta(), Is.EqualTo(10675199));
        Assert.That(roll.GetTime().GetHour(), Is.EqualTo(2));
        Assert.That(roll.GetTime().GetMinute(), Is.EqualTo(48));
        Assert.That(roll.GetTime().GetSecond(), Is.EqualTo(5));
        Assert.That(roll.GetTime().GetMillisecond(), Is.EqualTo(477));
        Assert.That(roll.GetTime().GetMicrosecond(), Is.EqualTo(580));
        Assert.That(roll.GetTime().GetTickRemainder(), Is.EqualTo(8));
    }

    [Test]
    public void SubtractDuration_RequiresDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => LocalTime.Midnight().SubtractDuration(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void AddPeriod_IncludesPositiveWeekDayAndSubDayFields()
    {
        Period period = Period.Create(0L, 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L);

        LocalTimeRoll roll = LocalTime.Create(10, 0, 0).AddPeriod(period);

        Assert.That(roll.GetDayDelta(), Is.EqualTo(9));
        Assert.That(roll.GetTime().GetHour(), Is.EqualTo(13));
        Assert.That(roll.GetTime().GetMinute(), Is.EqualTo(4));
        Assert.That(roll.GetTime().GetSecond(), Is.EqualTo(5));
        Assert.That(roll.GetTime().GetMillisecond(), Is.EqualTo(6));
        Assert.That(roll.GetTime().GetMicrosecond(), Is.EqualTo(7));
        Assert.That(roll.GetTime().GetTickRemainder(), Is.EqualTo(8));
    }

    [Test]
    public void AddPeriod_IncludesNegativeWeekDayAndSubDayFields()
    {
        Period period = Period.Create(0L, 0L, -1L, -2L, -3L, -4L, -5L, -6L, -7L, -8L);

        LocalTimeRoll roll = LocalTime.Create(10, 0, 0).AddPeriod(period);

        Assert.That(roll.GetDayDelta(), Is.EqualTo(-9));
        Assert.That(roll.GetTime().GetHour(), Is.EqualTo(6));
        Assert.That(roll.GetTime().GetMinute(), Is.EqualTo(55));
        Assert.That(roll.GetTime().GetSecond(), Is.EqualTo(54));
        Assert.That(roll.GetTime().GetMillisecond(), Is.EqualTo(993));
        Assert.That(roll.GetTime().GetMicrosecond(), Is.EqualTo(992));
        Assert.That(roll.GetTime().GetTickRemainder(), Is.EqualTo(2));
    }

    [Test]
    public void AddPeriod_CombinesMixedFieldsBeforeNormalizingRollover()
    {
        Period period = Period.Create(0L, 0L, 1L, -6L, -25L, 30L, 0L, 0L, 0L, 0L);

        LocalTimeRoll roll = LocalTime.Create(0, 15, 0).AddPeriod(period);

        AssertTimeRoll(roll, 23, 45, 0, -1);
    }

    [Test]
    public void AddPeriod_HandlesMultipleCalendarAndClockDays()
    {
        Period period = Period.Create(0L, 0L, 3L, 2L, 49L, 0L, 0L, 0L, 0L, 0L);

        LocalTimeRoll roll = LocalTime.Noon().AddPeriod(period);

        AssertTimeRoll(roll, 13, 0, 0, 25);
    }

    [Test]
    public void SubtractPeriod_SubtractsCalendarAndClockFields()
    {
        Period period = Period.Create(0L, 0L, 1L, 1L, 3L, 0L, 0L, 0L, 0L, 0L);

        LocalTimeRoll roll = LocalTime.Create(2, 0, 0).SubtractPeriod(period);

        AssertTimeRoll(roll, 23, 0, 0, -9);
    }

    [Test]
    public void SubtractPeriod_HandlesMinimumTickFieldWithoutNegationOverflow()
    {
        LocalTimeRoll roll = LocalTime.Midnight().SubtractPeriod(Period.FromTicks(long.MinValue));

        Assert.That(roll.GetDayDelta(), Is.EqualTo(10675199));
        Assert.That(roll.GetTime().GetHour(), Is.EqualTo(2));
        Assert.That(roll.GetTime().GetMinute(), Is.EqualTo(48));
        Assert.That(roll.GetTime().GetSecond(), Is.EqualTo(5));
        Assert.That(roll.GetTime().GetMillisecond(), Is.EqualTo(477));
        Assert.That(roll.GetTime().GetMicrosecond(), Is.EqualTo(580));
        Assert.That(roll.GetTime().GetTickRemainder(), Is.EqualTo(8));
    }

    [Test]
    public void PeriodArithmetic_RequiresPeriod()
    {
        ArgumentNullException addException = Assert.Throws<ArgumentNullException>(() => LocalTime.Midnight().AddPeriod(null!))!;
        ArgumentNullException subtractException = Assert.Throws<ArgumentNullException>(() => LocalTime.Midnight().SubtractPeriod(null!))!;

        Assert.That(addException.ParamName, Is.EqualTo("period"));
        Assert.That(subtractException.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void PeriodArithmetic_RejectsYearAndMonthFields()
    {
        ArgumentException addYears = Assert.Throws<ArgumentException>(() => LocalTime.Midnight().AddPeriod(Period.FromYears(1L)))!;
        ArgumentException addMonths = Assert.Throws<ArgumentException>(() => LocalTime.Midnight().AddPeriod(Period.FromMonths(-1L)))!;
        ArgumentException subtractYears = Assert.Throws<ArgumentException>(() => LocalTime.Midnight().SubtractPeriod(Period.Create(long.MinValue, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)))!;
        ArgumentException subtractMonths = Assert.Throws<ArgumentException>(() => LocalTime.Midnight().SubtractPeriod(Period.Create(0L, long.MinValue, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)))!;

        Assert.That(addYears.ParamName, Is.EqualTo("period"));
        Assert.That(addMonths.ParamName, Is.EqualTo("period"));
        Assert.That(subtractYears.ParamName, Is.EqualTo("period"));
        Assert.That(subtractMonths.ParamName, Is.EqualTo("period"));
    }

    [Test]
    public void AddPeriod_AllowsDayDeltaIntegerRangeEdges()
    {
        LocalTimeRoll maximum = LocalTime.Midnight().AddPeriod(Period.FromDays(int.MaxValue));
        LocalTimeRoll minimum = LocalTime.Midnight().AddPeriod(Period.FromDays(int.MinValue));

        Assert.That(maximum.GetDayDelta(), Is.EqualTo(int.MaxValue));
        Assert.That(maximum.GetTime().EqualsValue(LocalTime.Midnight()), Is.True);
        Assert.That(minimum.GetDayDelta(), Is.EqualTo(int.MinValue));
        Assert.That(minimum.GetTime().EqualsValue(LocalTime.Midnight()), Is.True);
    }

    [Test]
    public void PeriodArithmetic_ThrowsWhenDayDeltaExceedsIntegerRange()
    {
        Period aboveMaximum = Period.Create(0L, 0L, 0L, int.MaxValue, 0L, 0L, 0L, 0L, 0L, 1L);
        Period belowMinimum = Period.Create(0L, 0L, 0L, int.MinValue, 0L, 0L, 0L, 0L, 0L, -1L);

        Assert.Throws<OverflowException>(() => LocalTime.FromTicksSinceMidnight(TimeConstants.MaxTicksSinceMidnight).AddPeriod(aboveMaximum));
        Assert.Throws<OverflowException>(() => LocalTime.Midnight().AddPeriod(belowMinimum));
        Assert.Throws<OverflowException>(() => LocalTime.Midnight().SubtractPeriod(Period.FromDays(int.MinValue)));
    }

    [Test]
    public void AddPeriod_ThrowsWhenPeriodTickCalculationExceedsDecimalRange()
    {
        Assert.Throws<OverflowException>(() => LocalTime.Midnight().AddPeriod(Period.FromWeeks(long.MaxValue)));
        Assert.Throws<OverflowException>(() => LocalTime.Midnight().AddPeriod(Period.FromHours(long.MinValue)));
    }

    [Test]
    public void LocalTimeContract_DispatchesPeriodArithmetic()
    {
        ILocalTime time = LocalTime.Create(23, 0, 0);

        LocalTimeRoll added = time.AddPeriod(Period.FromHours(2L));
        LocalTimeRoll subtracted = time.SubtractPeriod(Period.FromDays(1L));

        AssertTimeRoll(added, 1, 0, 0, 1);
        AssertTimeRoll(subtracted, 23, 0, 0, -1);
    }

    [Test]
    public void LocalTime_MatchesTimeOnlyAcrossRepresentativeTicks()
    {
        long[] ticks =
        [
            TimeConstants.MinTicksSinceMidnight,
            1L,
            TimeConstants.TicksPerSecond,
            452967891234L,
            TimeConstants.MaxTicksSinceMidnight
        ];

        foreach (long tick in ticks)
        {
            TimeOnly timeOnly = new TimeOnly(tick);
            LocalTime time = LocalTime.FromTimeOnly(timeOnly);

            AssertTimeMatchesTimeOnly(time, timeOnly);
            Assert.That(LocalTime.Parse(time.ToIsoString()).EqualsValue(time), Is.True);
        }
    }

    [Test]
    public void FractionalSecondParsing_HandlesEverySupportedPrecisionWidth()
    {
        Assert.That(LocalTime.Parse("00:00:00.1").GetTicksSinceMidnight(), Is.EqualTo(1000000L));
        Assert.That(LocalTime.Parse("00:00:00.12").GetTicksSinceMidnight(), Is.EqualTo(1200000L));
        Assert.That(LocalTime.Parse("00:00:00.123").GetTicksSinceMidnight(), Is.EqualTo(1230000L));
        Assert.That(LocalTime.Parse("00:00:00.1234").GetTicksSinceMidnight(), Is.EqualTo(1234000L));
        Assert.That(LocalTime.Parse("00:00:00.12345").GetTicksSinceMidnight(), Is.EqualTo(1234500L));
        Assert.That(LocalTime.Parse("00:00:00.123456").GetTicksSinceMidnight(), Is.EqualTo(1234560L));
        Assert.That(LocalTime.Parse("00:00:00.1234567").GetTicksSinceMidnight(), Is.EqualTo(1234567L));
    }

    [Test]
    public void RolloverAtMidnightAndMaxTime_UsesAdjacentDayDelta()
    {
        LocalTimeRoll forward = LocalTime.FromTicksSinceMidnight(TimeConstants.MaxTicksSinceMidnight).AddDuration(Duration.FromTicks(1L));
        LocalTimeRoll backward = LocalTime.Midnight().SubtractDuration(Duration.FromTicks(1L));

        Assert.That(forward.GetDayDelta(), Is.EqualTo(1));
        Assert.That(forward.GetTime().EqualsValue(LocalTime.Midnight()), Is.True);
        Assert.That(backward.GetDayDelta(), Is.EqualTo(-1));
        Assert.That(backward.GetTime().GetTicksSinceMidnight(), Is.EqualTo(TimeConstants.MaxTicksSinceMidnight));
    }

    [Test]
    public void DurationArithmetic_MatchesTimeOnlyAddForRepresentativeDurations()
    {
        LocalTime start = LocalTime.Create(22, 15, 30, 250, 0, 0);
        TimeOnly timeOnly = start.ToTimeOnly();
        Duration[] durations =
        [
            Duration.FromTicks(1L),
            Duration.FromMinutes(90L),
            Duration.FromHours(-24L),
            Duration.FromDays(2L).Add(Duration.FromSeconds(45L))
        ];

        foreach (Duration duration in durations)
        {
            int wrappedDays;
            TimeOnly expected = timeOnly.Add(duration.ToTimeSpan(), out wrappedDays);
            LocalTimeRoll actual = start.AddDuration(duration);

            Assert.That(actual.GetDayDelta(), Is.EqualTo(wrappedDays));
            AssertTimeMatchesTimeOnly(actual.GetTime(), expected);
        }
    }

    [Test]
    public void MaxTimeFormatsParsesAndConvertsThroughTimeOnly()
    {
        LocalTime max = LocalTime.FromTicksSinceMidnight(TimeConstants.MaxTicksSinceMidnight);

        Assert.That(max.ToIsoString(), Is.EqualTo("23:59:59.9999999"));
        Assert.That(LocalTime.Parse(max.ToIsoString()).EqualsValue(max), Is.True);
        Assert.That(LocalTime.FromTimeOnly(max.ToTimeOnly()).EqualsValue(max), Is.True);
    }

    private static void AssertTimeRoll(LocalTimeRoll roll, int hour, int minute, int second, int dayDelta)
    {
        LocalTime time = roll.GetTime();

        Assert.That(time.GetHour(), Is.EqualTo(hour));
        Assert.That(time.GetMinute(), Is.EqualTo(minute));
        Assert.That(time.GetSecond(), Is.EqualTo(second));
        Assert.That(roll.GetDayDelta(), Is.EqualTo(dayDelta));
    }

    private static void AssertTimeMatchesTimeOnly(LocalTime time, TimeOnly timeOnly)
    {
        Assert.That(time.GetHour(), Is.EqualTo(timeOnly.Hour));
        Assert.That(time.GetMinute(), Is.EqualTo(timeOnly.Minute));
        Assert.That(time.GetSecond(), Is.EqualTo(timeOnly.Second));
        Assert.That(time.GetMillisecond(), Is.EqualTo(timeOnly.Millisecond));
        Assert.That(time.GetTicksSinceMidnight(), Is.EqualTo(timeOnly.Ticks));
        Assert.That(time.ToTimeOnly(), Is.EqualTo(timeOnly));
    }
}
