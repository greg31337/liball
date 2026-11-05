using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class PeriodTests
{
    [Test]
    public void Zero_ReturnsEmptyPeriod()
    {
        AssertPeriod(Period.Zero(), 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L);
    }

    [Test]
    public void Create_StoresMixedFieldsExactly()
    {
        Period period = Period.Create(1L, -2L, 3L, -4L, 5L, -6L, 7L, -8L, 9L, -10L);

        AssertPeriod(period, 1L, -2L, 3L, -4L, 5L, -6L, 7L, -8L, 9L, -10L);
    }

    [TestCase("years", 4L)]
    [TestCase("months", 4L)]
    [TestCase("weeks", 4L)]
    [TestCase("days", 4L)]
    [TestCase("hours", 4L)]
    [TestCase("minutes", 4L)]
    [TestCase("seconds", 4L)]
    [TestCase("milliseconds", 4L)]
    [TestCase("microseconds", 4L)]
    [TestCase("ticks", 4L)]
    public void FactoryMethods_SetOnlyRequestedField(string field, long value)
    {
        Period period = CreateSingleFieldPeriod(field, value);

        Assert.That(period.GetYears(), Is.EqualTo(field == "years" ? value : 0L));
        Assert.That(period.GetMonths(), Is.EqualTo(field == "months" ? value : 0L));
        Assert.That(period.GetWeeks(), Is.EqualTo(field == "weeks" ? value : 0L));
        Assert.That(period.GetDays(), Is.EqualTo(field == "days" ? value : 0L));
        Assert.That(period.GetHours(), Is.EqualTo(field == "hours" ? value : 0L));
        Assert.That(period.GetMinutes(), Is.EqualTo(field == "minutes" ? value : 0L));
        Assert.That(period.GetSeconds(), Is.EqualTo(field == "seconds" ? value : 0L));
        Assert.That(period.GetMilliseconds(), Is.EqualTo(field == "milliseconds" ? value : 0L));
        Assert.That(period.GetMicroseconds(), Is.EqualTo(field == "microseconds" ? value : 0L));
        Assert.That(period.GetTicks(), Is.EqualTo(field == "ticks" ? value : 0L));
    }

    [Test]
    public void NormalizeWeeksToDays_MovesWeeksIntoDaysAndPreservesOtherFields()
    {
        Period period = Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L);
        Period normalized = period.NormalizeWeeksToDays();

        AssertPeriod(normalized, 1L, 2L, 0L, 25L, 5L, 6L, 7L, 8L, 9L, 10L);
    }

    [Test]
    public void NormalizeWeeksToDays_HandlesNegativeWeeks()
    {
        Period period = Period.Create(0L, 0L, -2L, 3L, 0L, 0L, 0L, 0L, 0L, 0L);
        Period normalized = period.NormalizeWeeksToDays();

        Assert.That(normalized.GetWeeks(), Is.EqualTo(0L));
        Assert.That(normalized.GetDays(), Is.EqualTo(-11L));
    }

    [Test]
    public void NormalizeWeeksToDays_ThrowsWhenWeekMultiplicationOverflows()
    {
        Assert.Throws<OverflowException>(() => Period.FromWeeks(long.MaxValue).NormalizeWeeksToDays());
    }

    [Test]
    public void NormalizeWeeksToDays_ThrowsWhenDayAdditionOverflows()
    {
        Period period = Period.Create(0L, 0L, 1L, long.MaxValue, 0L, 0L, 0L, 0L, 0L, 0L);

        Assert.Throws<OverflowException>(() => period.NormalizeWeeksToDays());
    }

    [Test]
    public void Add_AddsEachFieldIndependently()
    {
        Period left = Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L);
        Period right = Period.Create(10L, 9L, 8L, 7L, 6L, 5L, 4L, 3L, 2L, 1L);

        AssertPeriod(left.Add(right), 11L, 11L, 11L, 11L, 11L, 11L, 11L, 11L, 11L, 11L);
    }

    [Test]
    public void Add_RequiresOtherPeriod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Period.Zero().Add(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void Add_ThrowsOnFieldOverflow()
    {
        Assert.Throws<OverflowException>(() => Period.FromYears(long.MaxValue).Add(Period.FromYears(1L)));
    }

    [Test]
    public void Subtract_SubtractsEachFieldIndependently()
    {
        Period left = Period.Create(10L, 9L, 8L, 7L, 6L, 5L, 4L, 3L, 2L, 1L);
        Period right = Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L);

        AssertPeriod(left.Subtract(right), 9L, 7L, 5L, 3L, 1L, -1L, -3L, -5L, -7L, -9L);
    }

    [Test]
    public void Subtract_RequiresOtherPeriod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Period.Zero().Subtract(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void Subtract_ThrowsOnFieldOverflow()
    {
        Assert.Throws<OverflowException>(() => Period.FromYears(long.MinValue).Subtract(Period.FromYears(1L)));
    }

    [Test]
    public void Negate_ReversesEveryFieldSign()
    {
        Period period = Period.Create(1L, -2L, 3L, -4L, 5L, -6L, 7L, -8L, 9L, -10L);

        AssertPeriod(period.Negate(), -1L, 2L, -3L, 4L, -5L, 6L, -7L, 8L, -9L, 10L);
    }

    [Test]
    public void Negate_ThrowsOnMinimumFieldValue()
    {
        Assert.Throws<OverflowException>(() => Period.FromTicks(long.MinValue).Negate());
    }

    [Test]
    public void CompareTo_ReturnsZeroForEquivalentExactClockFields()
    {
        Assert.That(Period.Zero().CompareTo(Period.Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)), Is.EqualTo(0));
        Assert.That(Period.FromHours(1L).CompareTo(Period.FromMinutes(60L)), Is.EqualTo(0));
        Assert.That(Period.FromSeconds(1L).CompareTo(Period.FromMilliseconds(1000L)), Is.EqualTo(0));

        Period mixed = Period.Create(0L, 0L, 0L, 0L, 1L, -2L, 3L, -4L, 5L, -6L);
        Period equivalent = Period.FromTicks(34829960044L);

        Assert.That(mixed.CompareTo(equivalent), Is.EqualTo(0));
    }

    [Test]
    public void CompareTo_OrdersExactClockFieldsByTotalTicks()
    {
        Assert.That(Period.FromHours(2L).CompareTo(Period.FromMinutes(119L)), Is.EqualTo(1));
        Assert.That(Period.FromHours(-2L).CompareTo(Period.FromMinutes(-119L)), Is.EqualTo(-1));
        Assert.That(Period.FromTicks(long.MinValue).CompareTo(Period.FromTicks(long.MaxValue)), Is.EqualTo(-1));
        Assert.That(Period.FromTicks(long.MaxValue).CompareTo(Period.FromTicks(long.MinValue)), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_HandlesMixedSignClockFields()
    {
        Period negativeThirtyMinutes = Period.Create(0L, 0L, 0L, 0L, 1L, -90L, 0L, 0L, 0L, 0L);
        Period negativeTwentyNineMinutes = Period.FromMinutes(-29L);
        Period oneTick = Period.Create(0L, 0L, 0L, 0L, -1L, 60L, 0L, 0L, 0L, 1L);

        Assert.That(negativeThirtyMinutes.CompareTo(negativeTwentyNineMinutes), Is.EqualTo(-1));
        Assert.That(oneTick.CompareTo(Period.Zero()), Is.EqualTo(1));
    }

    [TestCase("years", "left")]
    [TestCase("months", "left")]
    [TestCase("weeks", "left")]
    [TestCase("days", "left")]
    [TestCase("years", "right")]
    [TestCase("months", "right")]
    [TestCase("weeks", "right")]
    [TestCase("days", "right")]
    public void CompareTo_RejectsCalendarDependentFields(string field, string side)
    {
        Period calendarPeriod = CreateSingleFieldPeriod(field, 1L);
        Period left = side == "left" ? calendarPeriod : Period.Zero();
        Period right = side == "right" ? calendarPeriod : Period.Zero();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => left.CompareTo(right))!;

        Assert.That(exception.Message, Does.Contain("calendar"));
    }

    [Test]
    public void CompareTo_RejectsEqualCalendarPeriodsBecauseTheyStillNeedAnAnchor()
    {
        Assert.Throws<InvalidOperationException>(() => Period.FromDays(1L).CompareTo(Period.FromDays(1L)));
        Assert.Throws<InvalidOperationException>(() => Period.FromMonths(1L).CompareTo(Period.FromDays(30L)));
        Assert.Throws<InvalidOperationException>(() => Period.FromDays(1L).CompareTo(Period.FromHours(24L)));
    }

    [Test]
    public void CompareTo_RequiresOtherPeriod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Period.Zero().CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void CompareTo_ThrowsWhenExactTickConversionOverflows()
    {
        Assert.Throws<OverflowException>(() => Period.FromHours(long.MaxValue).CompareTo(Period.Zero()));
        Assert.Throws<OverflowException>(() => Period.FromMinutes(long.MinValue).CompareTo(Period.Zero()));
        Assert.Throws<OverflowException>(() => Period.Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 1L, long.MaxValue).CompareTo(Period.Zero()));
        Assert.Throws<OverflowException>(() => Period.Zero().CompareTo(Period.FromSeconds(long.MaxValue)));
    }

    [Test]
    public void CompareTo_RejectsCalendarFieldsBeforeConvertingClockFields()
    {
        Period calendarPeriodWithHugeClockField = Period.Create(1L, 0L, 0L, 0L, long.MaxValue, 0L, 0L, 0L, 0L, 0L);

        Assert.Throws<InvalidOperationException>(() => calendarPeriodWithHugeClockField.CompareTo(Period.Zero()));
    }

    [Test]
    public void PeriodContract_DispatchesComparison()
    {
        IPeriod period = Period.FromMinutes(90L);

        Assert.That(period.CompareTo(Period.FromHours(1L)), Is.EqualTo(1));
    }

    [Test]
    public void EqualsValue_ComparesEveryFieldAndHandlesNull()
    {
        Period period = Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L);

        Assert.That(period.EqualsValue(Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 10L)), Is.True);
        Assert.That(period.EqualsValue(Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 11L)), Is.False);
        Assert.That(period.EqualsValue(null), Is.False);
    }

    [TestCase("zero", "P0D")]
    [TestCase("mixedPositive", "P1Y2M3W4DT5H6M7.0080091S")]
    [TestCase("allNegative", "-P1Y2M3W4DT5H6M7.0080091S")]
    [TestCase("mixedSigns", "P1Y-2M3DT-4M")]
    [TestCase("fractionOnly", "PT0.0010023S")]
    public void ToIsoString_FormatsCalendarAndTimeFields(string scenario, string expected)
    {
        Period period = CreatePeriodScenario(scenario);

        Assert.That(period.ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("P0D", "zero")]
    [TestCase("P1Y2M3W4DT5H6M7.0080091S", "mixedPositive")]
    [TestCase("-P1Y2M3W4DT5H6M7.0080091S", "allNegative")]
    [TestCase("P1Y-2M3DT-4M", "mixedSigns")]
    [TestCase("PT0.0010023S", "fractionOnly")]
    [TestCase("+P1Y", "oneYear")]
    public void Parse_ReadsCalendarAndTimeFields(string text, string scenario)
    {
        Period expected = CreatePeriodScenario(scenario);

        Assert.That(Period.Parse(text).EqualsValue(expected), Is.True);
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Period.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("P")]
    [TestCase("P1")]
    [TestCase("PT")]
    [TestCase("1Y")]
    [TestCase("P1D2Y")]
    [TestCase("PT1M1H")]
    [TestCase("P1.5Y")]
    [TestCase("P1DT2D")]
    [TestCase("PT1.12345678S")]
    [TestCase("PT1.S")]
    [TestCase("PT1.xS")]
    [TestCase("P1Y ")]
    public void Parse_RejectsUnsupportedOrMalformedText(string text)
    {
        Assert.Throws<FormatException>(() => Period.Parse(text));
    }

    [TestCase("P1Y2MT")]
    [TestCase("P1DT2H1D")]
    [TestCase("P1W2M")]
    [TestCase("PT1.5H")]
    [TestCase("PT1S2M")]
    [TestCase("P+Y")]
    public void Parse_RejectsComplexMalformedText(string text)
    {
        Assert.Throws<FormatException>(() => Period.Parse(text));
    }

    [Test]
    public void Parse_ThrowsOverflowWhenAComponentIsOutsideLongRange()
    {
        Assert.Throws<OverflowException>(() => Period.Parse("P9223372036854775808Y"));
        Assert.Throws<OverflowException>(() => Period.Parse("-P9223372036854775809Y"));
    }

    [Test]
    public void MixedFieldPeriod_RoundTripsThroughIsoText()
    {
        Period original = Period.Create(12L, -11L, 10L, -9L, 8L, -7L, 6L, 5L, 4L, 3L);
        string text = original.ToIsoString();
        Period parsed = Period.Parse(text);

        Assert.That(text, Is.EqualTo("P12Y-11M10W-9DT8H-7M6.0050043S"));
        Assert.That(parsed.EqualsValue(original), Is.True);
    }

    [Test]
    public void ToIsoString_FormatsMixedNegativeSecondsWithoutALeadingPeriodSign()
    {
        Period period = Period.Create(1L, 0L, 0L, 0L, 0L, 0L, -1L, 0L, 0L, 0L);

        Assert.That(period.ToIsoString(), Is.EqualTo("P1YT-1S"));
        Assert.That(Period.Parse(period.ToIsoString()).EqualsValue(period), Is.True);
    }

    [Test]
    public void ToIsoString_FormatsMinimumLongCalendarComponent()
    {
        Period period = Period.FromYears(long.MinValue);

        Assert.That(period.ToIsoString(), Is.EqualTo("-P9223372036854775808Y"));
        Assert.That(Period.Parse(period.ToIsoString()).EqualsValue(period), Is.True);
    }

    [Test]
    public void NegativeFractionalSeconds_ParseIntoNegativeSubsecondFields()
    {
        Period period = Period.Parse("PT-1.0020034S");

        AssertPeriod(period, 0L, 0L, 0L, 0L, 0L, 0L, -1L, -2L, -3L, -4L);
        Assert.That(period.ToIsoString(), Is.EqualTo("-PT1.0020034S"));
    }

    [Test]
    public void WeekNormalization_AfterParsingMixedWeeksAndDays_CombinesSignedFields()
    {
        Period period = Period.Parse("P2W-3D");
        Period normalized = period.NormalizeWeeksToDays();

        AssertPeriod(period, 0L, 0L, 2L, -3L, 0L, 0L, 0L, 0L, 0L, 0L);
        AssertPeriod(normalized, 0L, 0L, 0L, 11L, 0L, 0L, 0L, 0L, 0L, 0L);
        Assert.That(normalized.ToIsoString(), Is.EqualTo("P11D"));
    }

    [Test]
    public void AllSingleFields_ParseFormatRoundTrip()
    {
        Period[] periods =
        [
            Period.FromYears(3L),
            Period.FromMonths(-3L),
            Period.FromWeeks(3L),
            Period.FromDays(-3L),
            Period.FromHours(3L),
            Period.FromMinutes(-3L),
            Period.FromSeconds(3L),
            Period.FromMilliseconds(-3L),
            Period.FromMicroseconds(3L),
            Period.FromTicks(-3L)
        ];

        foreach (Period period in periods)
        {
            Period parsed = Period.Parse(period.ToIsoString());

            Assert.That(parsed.EqualsValue(period), Is.True);
        }
    }

    [Test]
    public void ParseFormatBehavior_PreservesLeadingNegativePeriodConvention()
    {
        Period period = Period.Parse("-P2Y3M4W5DT6H7M8.0090001S");

        AssertPeriod(period, -2L, -3L, -4L, -5L, -6L, -7L, -8L, -9L, 0L, -1L);
        Assert.That(period.ToIsoString(), Is.EqualTo("-P2Y3M4W5DT6H7M8.0090001S"));
    }

    private static Period CreateSingleFieldPeriod(string field, long value)
    {
        if (field == "years")
        {
            return Period.FromYears(value);
        }

        if (field == "months")
        {
            return Period.FromMonths(value);
        }

        if (field == "weeks")
        {
            return Period.FromWeeks(value);
        }

        if (field == "days")
        {
            return Period.FromDays(value);
        }

        if (field == "hours")
        {
            return Period.FromHours(value);
        }

        if (field == "minutes")
        {
            return Period.FromMinutes(value);
        }

        if (field == "seconds")
        {
            return Period.FromSeconds(value);
        }

        if (field == "milliseconds")
        {
            return Period.FromMilliseconds(value);
        }

        if (field == "microseconds")
        {
            return Period.FromMicroseconds(value);
        }

        return Period.FromTicks(value);
    }

    private static Period CreatePeriodScenario(string scenario)
    {
        if (scenario == "zero")
        {
            return Period.Zero();
        }

        if (scenario == "mixedPositive")
        {
            return Period.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, 1L);
        }

        if (scenario == "allNegative")
        {
            return Period.Create(-1L, -2L, -3L, -4L, -5L, -6L, -7L, -8L, -9L, -1L);
        }

        if (scenario == "mixedSigns")
        {
            return Period.Create(1L, -2L, 0L, 3L, 0L, -4L, 0L, 0L, 0L, 0L);
        }

        if (scenario == "fractionOnly")
        {
            return Period.Create(0L, 0L, 0L, 0L, 0L, 0L, 0L, 1L, 2L, 3L);
        }

        return Period.FromYears(1L);
    }

    private static void AssertPeriod(
        Period period,
        long years,
        long months,
        long weeks,
        long days,
        long hours,
        long minutes,
        long seconds,
        long milliseconds,
        long microseconds,
        long ticks)
    {
        Assert.That(period.GetYears(), Is.EqualTo(years));
        Assert.That(period.GetMonths(), Is.EqualTo(months));
        Assert.That(period.GetWeeks(), Is.EqualTo(weeks));
        Assert.That(period.GetDays(), Is.EqualTo(days));
        Assert.That(period.GetHours(), Is.EqualTo(hours));
        Assert.That(period.GetMinutes(), Is.EqualTo(minutes));
        Assert.That(period.GetSeconds(), Is.EqualTo(seconds));
        Assert.That(period.GetMilliseconds(), Is.EqualTo(milliseconds));
        Assert.That(period.GetMicroseconds(), Is.EqualTo(microseconds));
        Assert.That(period.GetTicks(), Is.EqualTo(ticks));
    }
}
