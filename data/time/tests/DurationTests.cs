using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class DurationTests
{
    [Test]
    public void Zero_ReturnsZeroTicks()
    {
        Duration duration = Duration.Zero();

        Assert.That(duration.GetTicks(), Is.EqualTo(0L));
    }

    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(-1L)]
    [TestCase(9223372036854775807L)]
    [TestCase(-9223372036854775808L)]
    public void FromTicks_StoresExactTickValue(long ticks)
    {
        Duration duration = Duration.FromTicks(ticks);

        Assert.That(duration.GetTicks(), Is.EqualTo(ticks));
    }

    [TestCase(3L, 30L)]
    [TestCase(-3L, -30L)]
    public void FromMicroseconds_MultipliesByTicksPerMicrosecond(long value, long expectedTicks)
    {
        Duration duration = Duration.FromMicroseconds(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(2L, 20000L)]
    [TestCase(-2L, -20000L)]
    public void FromMilliseconds_MultipliesByTicksPerMillisecond(long value, long expectedTicks)
    {
        Duration duration = Duration.FromMilliseconds(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(2L, 20000000L)]
    [TestCase(-2L, -20000000L)]
    public void FromSeconds_MultipliesByTicksPerSecond(long value, long expectedTicks)
    {
        Duration duration = Duration.FromSeconds(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(2L, 1200000000L)]
    [TestCase(-2L, -1200000000L)]
    public void FromMinutes_MultipliesByTicksPerMinute(long value, long expectedTicks)
    {
        Duration duration = Duration.FromMinutes(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(2L, 72000000000L)]
    [TestCase(-2L, -72000000000L)]
    public void FromHours_MultipliesByTicksPerHour(long value, long expectedTicks)
    {
        Duration duration = Duration.FromHours(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(2L, 1728000000000L)]
    [TestCase(-2L, -1728000000000L)]
    public void FromDays_MultipliesByTicksPerDay(long value, long expectedTicks)
    {
        Duration duration = Duration.FromDays(value);

        Assert.That(duration.GetTicks(), Is.EqualTo(expectedTicks));
    }

    [Test]
    public void FactoryMethods_UseCheckedMultiplication()
    {
        Assert.Throws<OverflowException>(() => Duration.FromMicroseconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => Duration.FromMilliseconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => Duration.FromSeconds(long.MaxValue));
        Assert.Throws<OverflowException>(() => Duration.FromMinutes(long.MaxValue));
        Assert.Throws<OverflowException>(() => Duration.FromHours(long.MaxValue));
        Assert.Throws<OverflowException>(() => Duration.FromDays(long.MaxValue));
    }

    [Test]
    public void Add_ReturnsSumOfTicks()
    {
        Duration duration = Duration.FromSeconds(3).Add(Duration.FromMilliseconds(250));

        Assert.That(duration.GetTicks(), Is.EqualTo(32500000L));
    }

    [Test]
    public void Add_RequiresOtherDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Duration.FromTicks(1).Add(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void Add_ThrowsOnOverflow()
    {
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MaxValue).Add(Duration.FromTicks(1)));
    }

    [Test]
    public void Subtract_ReturnsDifferenceOfTicks()
    {
        Duration duration = Duration.FromSeconds(3).Subtract(Duration.FromMilliseconds(250));

        Assert.That(duration.GetTicks(), Is.EqualTo(27500000L));
    }

    [Test]
    public void Subtract_RequiresOtherDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Duration.FromTicks(1).Subtract(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void Subtract_ThrowsOnOverflow()
    {
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MinValue).Subtract(Duration.FromTicks(1)));
    }

    [TestCase(5L, -5L)]
    [TestCase(-5L, 5L)]
    [TestCase(0L, 0L)]
    public void Negate_ReversesSign(long ticks, long expected)
    {
        Assert.That(Duration.FromTicks(ticks).Negate().GetTicks(), Is.EqualTo(expected));
    }

    [Test]
    public void Negate_ThrowsOnMinimumTickValue()
    {
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MinValue).Negate());
    }

    [TestCase(5L, 5L)]
    [TestCase(-5L, 5L)]
    [TestCase(0L, 0L)]
    public void Abs_ReturnsNonNegativeMagnitude(long ticks, long expected)
    {
        Assert.That(Duration.FromTicks(ticks).Abs().GetTicks(), Is.EqualTo(expected));
    }

    [Test]
    public void Abs_ThrowsOnMinimumTickValue()
    {
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MinValue).Abs());
    }

    [TestCase(-2L, 5L, -1)]
    [TestCase(5L, 5L, 0)]
    [TestCase(7L, 5L, 1)]
    public void CompareTo_ReturnsNormalizedTickOrdering(long left, long right, int expected)
    {
        Assert.That(Duration.FromTicks(left).CompareTo(Duration.FromTicks(right)), Is.EqualTo(expected));
    }

    [Test]
    public void CompareTo_RequiresOtherDuration()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Duration.FromTicks(1).CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesTicksAndHandlesNull()
    {
        Assert.That(Duration.FromTicks(10).EqualsValue(Duration.FromTicks(10)), Is.True);
        Assert.That(Duration.FromTicks(10).EqualsValue(Duration.FromTicks(11)), Is.False);
        Assert.That(Duration.FromTicks(10).EqualsValue(null), Is.False);
    }

    [Test]
    public void ToTimeSpan_ReturnsEquivalentTimeSpan()
    {
        TimeSpan timeSpan = Duration.FromMinutes(2).ToTimeSpan();

        Assert.That(timeSpan, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    [Test]
    public void FromTimeSpan_StoresTimeSpanTicks()
    {
        TimeSpan timeSpan = new TimeSpan(1, 2, 3, 4, 5);
        Duration duration = Duration.FromTimeSpan(timeSpan);

        Assert.That(duration.GetTicks(), Is.EqualTo(timeSpan.Ticks));
    }

    [TestCase(0L, "PT0S")]
    [TestCase(1L, "PT0.0000001S")]
    [TestCase(10L, "PT0.000001S")]
    [TestCase(10000000L, "PT1S")]
    [TestCase(650000000L, "PT1M5S")]
    [TestCase(54000000000L, "PT1H30M")]
    [TestCase(2592000000000L, "P3D")]
    [TestCase(936000000000L, "P1DT2H")]
    [TestCase(-15000000L, "-PT1.5S")]
    [TestCase(9223372036854775807L, "P10675199DT2H48M5.4775807S")]
    [TestCase(-9223372036854775808L, "-P10675199DT2H48M5.4775808S")]
    public void ToIsoString_FormatsFixedElapsedDuration(long ticks, string expected)
    {
        Assert.That(Duration.FromTicks(ticks).ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("PT0S", 0L)]
    [TestCase("+PT0S", 0L)]
    [TestCase("PT0.0000001S", 1L)]
    [TestCase("PT1S", 10000000L)]
    [TestCase("PT1M5S", 650000000L)]
    [TestCase("PT1H30M", 54000000000L)]
    [TestCase("P3D", 2592000000000L)]
    [TestCase("P1DT2H", 936000000000L)]
    [TestCase("P2W", 12096000000000L)]
    [TestCase("-PT1.5S", -15000000L)]
    [TestCase("P10675199DT2H48M5.4775807S", 9223372036854775807L)]
    [TestCase("-P10675199DT2H48M5.4775808S", -9223372036854775808L)]
    public void Parse_ReadsPracticalIsoDurationForms(string text, long expectedTicks)
    {
        Assert.That(Duration.Parse(text).GetTicks(), Is.EqualTo(expectedTicks));
    }

    [TestCase(0L)]
    [TestCase(1L)]
    [TestCase(-1L)]
    [TestCase(1728000000001L)]
    [TestCase(-1728000000001L)]
    [TestCase(9223372036854775807L)]
    [TestCase(-9223372036854775808L)]
    public void Parse_RoundTripsFormattedDurations(long ticks)
    {
        string text = Duration.FromTicks(ticks).ToIsoString();

        Assert.That(Duration.Parse(text).GetTicks(), Is.EqualTo(ticks));
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Duration.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("P")]
    [TestCase("PT")]
    [TestCase("1H")]
    [TestCase("P1Y")]
    [TestCase("P1M")]
    [TestCase("PT1D")]
    [TestCase("P1DT")]
    [TestCase("PT1.12345678S")]
    [TestCase("PT1.S")]
    [TestCase("PT1.xS")]
    [TestCase("PT1.5M")]
    [TestCase("PT1M1H")]
    [TestCase("P1W2D")]
    [TestCase("P1DT2W")]
    public void Parse_RejectsUnsupportedOrMalformedDurationText(string text)
    {
        Assert.Throws<FormatException>(() => Duration.Parse(text));
    }

    [TestCase("P10675199DT2H48M5.4775808S")]
    [TestCase("-P10675199DT2H48M5.4775809S")]
    [TestCase("PT9223372036854775808S")]
    [TestCase("PT9223372036854775809S")]
    public void Parse_ThrowsOverflowWhenTicksAreOutsideLongRange(string text)
    {
        Assert.Throws<OverflowException>(() => Duration.Parse(text));
    }

    [Test]
    public void Duration_EndToEndBehavior_CoversPositiveNegativeAndZeroValues()
    {
        Duration positive = Duration.FromHours(2).Add(Duration.FromMinutes(30));
        Duration negative = Duration.FromMinutes(-150);
        Duration zero = positive.Add(negative);

        Assert.That(positive.GetTicks(), Is.GreaterThan(0L));
        Assert.That(negative.GetTicks(), Is.LessThan(0L));
        Assert.That(zero.GetTicks(), Is.EqualTo(0L));
        Assert.That(positive.EqualsValue(negative.Negate()), Is.True);
        Assert.That(zero.ToIsoString(), Is.EqualTo("PT0S"));
        Assert.That(Duration.Parse(positive.ToIsoString()).EqualsValue(positive), Is.True);
        Assert.That(Duration.Parse(negative.ToIsoString()).EqualsValue(negative), Is.True);
    }

    [Test]
    public void TimeSpanInterop_RoundTripsRepresentativeValues()
    {
        TimeSpan[] values =
        [
            TimeSpan.Zero,
            TimeSpan.FromTicks(1),
            TimeSpan.FromTicks(-1),
            new TimeSpan(3, 4, 5, 6, 7),
            TimeSpan.MaxValue,
            TimeSpan.MinValue
        ];

        foreach (TimeSpan value in values)
        {
            Duration duration = Duration.FromTimeSpan(value);
            TimeSpan result = duration.ToTimeSpan();

            Assert.That(result, Is.EqualTo(value));
            Assert.That(Duration.Parse(duration.ToIsoString()).GetTicks(), Is.EqualTo(value.Ticks));
        }
    }

    [Test]
    public void FactoryMethods_UseCheckedMultiplicationForNegativeOverflow()
    {
        Assert.Throws<OverflowException>(() => Duration.FromMicroseconds(long.MinValue));
        Assert.Throws<OverflowException>(() => Duration.FromMilliseconds(long.MinValue));
        Assert.Throws<OverflowException>(() => Duration.FromSeconds(long.MinValue));
        Assert.Throws<OverflowException>(() => Duration.FromMinutes(long.MinValue));
        Assert.Throws<OverflowException>(() => Duration.FromHours(long.MinValue));
        Assert.Throws<OverflowException>(() => Duration.FromDays(long.MinValue));
    }

    [TestCase(" PT1S")]
    [TestCase("PT1S ")]
    [TestCase("pt1s")]
    [TestCase("P1DT2H3M4S5")]
    public void Parse_RejectsNonCanonicalOrTrailingText(string text)
    {
        Assert.Throws<FormatException>(() => Duration.Parse(text));
    }

    [TestCase(1296000000000L, 1.5)]
    [TestCase(-1296000000000L, -1.5)]
    [TestCase(0L, 0.0)]
    public void TotalConversions_PreserveSignedFractionalUnits(long ticks, double expectedDays)
    {
        Duration duration = Duration.FromTicks(ticks);

        Assert.That(duration.GetTotalTicks(), Is.EqualTo((double)ticks));
        Assert.That(duration.GetTotalMicroseconds(), Is.EqualTo(ticks / 10.0));
        Assert.That(duration.GetTotalMilliseconds(), Is.EqualTo(ticks / 10000.0));
        Assert.That(duration.GetTotalSeconds(), Is.EqualTo(ticks / 10000000.0));
        Assert.That(duration.GetTotalMinutes(), Is.EqualTo(ticks / 600000000.0));
        Assert.That(duration.GetTotalHours(), Is.EqualTo(ticks / 36000000000.0));
        Assert.That(duration.GetTotalDays(), Is.EqualTo(expectedDays));
    }

    [TestCase(9223372036854775807L)]
    [TestCase(-9223372036854775808L)]
    public void TotalConversions_HandleTickRangeEdges(long ticks)
    {
        Duration duration = Duration.FromTicks(ticks);

        Assert.That(duration.GetTotalTicks(), Is.EqualTo((double)ticks));
        Assert.That(duration.GetTotalMicroseconds(), Is.EqualTo(ticks / 10.0));
        Assert.That(duration.GetTotalMilliseconds(), Is.EqualTo(ticks / 10000.0));
        Assert.That(duration.GetTotalSeconds(), Is.EqualTo(ticks / 10000000.0));
        Assert.That(duration.GetTotalMinutes(), Is.EqualTo(ticks / 600000000.0));
        Assert.That(duration.GetTotalHours(), Is.EqualTo(ticks / 36000000000.0));
        Assert.That(duration.GetTotalDays(), Is.EqualTo(ticks / 864000000000.0));
    }

    [Test]
    public void TotalConversions_PreserveSubMicrosecondFractions()
    {
        Duration duration = Duration.FromTicks(1L);

        Assert.That(duration.GetTotalMicroseconds(), Is.EqualTo(0.1));
        Assert.That(duration.GetTotalMilliseconds(), Is.EqualTo(0.0001));
        Assert.That(duration.GetTotalSeconds(), Is.EqualTo(0.0000001));
    }

    [Test]
    public void MinAndMax_SelectByExactTickCount()
    {
        Duration shorter = Duration.FromTicks(-5L);
        Duration longer = Duration.FromTicks(7L);
        Duration equal = Duration.FromTicks(-5L);

        Assert.That(shorter.Min(longer).GetTicks(), Is.EqualTo(-5L));
        Assert.That(longer.Min(shorter).GetTicks(), Is.EqualTo(-5L));
        Assert.That(shorter.Max(longer).GetTicks(), Is.EqualTo(7L));
        Assert.That(longer.Max(shorter).GetTicks(), Is.EqualTo(7L));
        Assert.That(shorter.Min(equal).GetTicks(), Is.EqualTo(-5L));
        Assert.That(shorter.Max(equal).GetTicks(), Is.EqualTo(-5L));
        Assert.That(Duration.FromTicks(long.MinValue).Min(Duration.FromTicks(long.MaxValue)).GetTicks(), Is.EqualTo(long.MinValue));
        Assert.That(Duration.FromTicks(long.MinValue).Max(Duration.FromTicks(long.MaxValue)).GetTicks(), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void MinAndMax_RequireOtherDuration()
    {
        ArgumentNullException minException = Assert.Throws<ArgumentNullException>(() => Duration.FromTicks(1L).Min(null!))!;
        ArgumentNullException maxException = Assert.Throws<ArgumentNullException>(() => Duration.FromTicks(1L).Max(null!))!;

        Assert.That(minException.ParamName, Is.EqualTo("other"));
        Assert.That(maxException.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void RoundToFixedUnits_RoundsMidpointsAwayFromZero()
    {
        Assert.That(Duration.FromTicks(1L).RoundToTicks().GetTicks(), Is.EqualTo(1L));
        Assert.That(Duration.FromTicks(-1L).RoundToTicks().GetTicks(), Is.EqualTo(-1L));
        Assert.That(Duration.FromTicks(15L).RoundToMicroseconds().GetTicks(), Is.EqualTo(20L));
        Assert.That(Duration.FromTicks(-15L).RoundToMicroseconds().GetTicks(), Is.EqualTo(-20L));
        Assert.That(Duration.FromTicks(15000L).RoundToMilliseconds().GetTicks(), Is.EqualTo(20000L));
        Assert.That(Duration.FromTicks(-15000L).RoundToMilliseconds().GetTicks(), Is.EqualTo(-20000L));
        Assert.That(Duration.FromTicks(15000000L).RoundToSeconds().GetTicks(), Is.EqualTo(20000000L));
        Assert.That(Duration.FromTicks(-15000000L).RoundToSeconds().GetTicks(), Is.EqualTo(-20000000L));
        Assert.That(Duration.FromTicks(900000000L).RoundToMinutes().GetTicks(), Is.EqualTo(1200000000L));
        Assert.That(Duration.FromTicks(-900000000L).RoundToMinutes().GetTicks(), Is.EqualTo(-1200000000L));
        Assert.That(Duration.FromTicks(54000000000L).RoundToHours().GetTicks(), Is.EqualTo(72000000000L));
        Assert.That(Duration.FromTicks(-54000000000L).RoundToHours().GetTicks(), Is.EqualTo(-72000000000L));
        Assert.That(Duration.FromTicks(1296000000000L).RoundToDays().GetTicks(), Is.EqualTo(1728000000000L));
        Assert.That(Duration.FromTicks(-1296000000000L).RoundToDays().GetTicks(), Is.EqualTo(-1728000000000L));
    }

    [Test]
    public void RoundToFixedUnits_UsesNearestUnitOnEitherSideOfMidpoint()
    {
        Assert.That(Duration.FromTicks(14L).RoundToMicroseconds().GetTicks(), Is.EqualTo(10L));
        Assert.That(Duration.FromTicks(16L).RoundToMicroseconds().GetTicks(), Is.EqualTo(20L));
        Assert.That(Duration.FromTicks(-14L).RoundToMicroseconds().GetTicks(), Is.EqualTo(-10L));
        Assert.That(Duration.FromTicks(-16L).RoundToMicroseconds().GetTicks(), Is.EqualTo(-20L));
        Assert.That(Duration.Zero().RoundToDays().GetTicks(), Is.EqualTo(0L));
    }

    [Test]
    public void RoundToFixedUnits_ThrowsWhenNearestUnitIsOutsideTickRange()
    {
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MaxValue).RoundToMicroseconds());
        Assert.Throws<OverflowException>(() => Duration.FromTicks(long.MinValue).RoundToMicroseconds());

        Assert.That(Duration.FromTicks(long.MaxValue).RoundToTicks().GetTicks(), Is.EqualTo(long.MaxValue));
        Assert.That(Duration.FromTicks(long.MinValue).RoundToTicks().GetTicks(), Is.EqualTo(long.MinValue));
    }

    [Test]
    public void TruncateToFixedUnits_DiscardsPartialUnitsTowardZero()
    {
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToTicks().GetTicks(), Is.EqualTo(1234567890123L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToMicroseconds().GetTicks(), Is.EqualTo(1234567890120L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToMilliseconds().GetTicks(), Is.EqualTo(1234567890000L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToSeconds().GetTicks(), Is.EqualTo(1234560000000L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToMinutes().GetTicks(), Is.EqualTo(1234200000000L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToHours().GetTicks(), Is.EqualTo(1224000000000L));
        Assert.That(Duration.FromTicks(1234567890123L).TruncateToDays().GetTicks(), Is.EqualTo(864000000000L));

        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToMicroseconds().GetTicks(), Is.EqualTo(-1234567890120L));
        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToMilliseconds().GetTicks(), Is.EqualTo(-1234567890000L));
        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToSeconds().GetTicks(), Is.EqualTo(-1234560000000L));
        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToMinutes().GetTicks(), Is.EqualTo(-1234200000000L));
        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToHours().GetTicks(), Is.EqualTo(-1224000000000L));
        Assert.That(Duration.FromTicks(-1234567890123L).TruncateToDays().GetTicks(), Is.EqualTo(-864000000000L));
    }

    [TestCase(9223372036854775807L)]
    [TestCase(-9223372036854775808L)]
    public void TruncateToFixedUnits_HandlesTickRangeEdges(long ticks)
    {
        Duration duration = Duration.FromTicks(ticks);

        Assert.That(duration.TruncateToTicks().GetTicks(), Is.EqualTo(ticks));
        Assert.That(duration.TruncateToMicroseconds().GetTicks(), Is.EqualTo(ticks / 10L * 10L));
        Assert.That(duration.TruncateToMilliseconds().GetTicks(), Is.EqualTo(ticks / 10000L * 10000L));
        Assert.That(duration.TruncateToSeconds().GetTicks(), Is.EqualTo(ticks / 10000000L * 10000000L));
        Assert.That(duration.TruncateToMinutes().GetTicks(), Is.EqualTo(ticks / 600000000L * 600000000L));
        Assert.That(duration.TruncateToHours().GetTicks(), Is.EqualTo(ticks / 36000000000L * 36000000000L));
        Assert.That(duration.TruncateToDays().GetTicks(), Is.EqualTo(ticks / 864000000000L * 864000000000L));
    }

    [Test]
    public void DurationContract_DispatchesNewOperations()
    {
        IDuration duration = Duration.FromTicks(15000L);

        Assert.That(duration.GetTotalTicks(), Is.EqualTo(15000.0));
        Assert.That(duration.GetTotalMicroseconds(), Is.EqualTo(1500.0));
        Assert.That(duration.GetTotalMilliseconds(), Is.EqualTo(1.5));
        Assert.That(duration.GetTotalSeconds(), Is.EqualTo(0.0015));
        Assert.That(duration.GetTotalMinutes(), Is.EqualTo(0.000025));
        Assert.That(duration.GetTotalHours(), Is.EqualTo(15000.0 / 36000000000.0));
        Assert.That(duration.GetTotalDays(), Is.EqualTo(15000.0 / 864000000000.0));
        Assert.That(duration.Min(Duration.FromTicks(20000L)).GetTicks(), Is.EqualTo(15000L));
        Assert.That(duration.Max(Duration.FromTicks(20000L)).GetTicks(), Is.EqualTo(20000L));
        Assert.That(duration.RoundToTicks().GetTicks(), Is.EqualTo(15000L));
        Assert.That(duration.RoundToMicroseconds().GetTicks(), Is.EqualTo(15000L));
        Assert.That(duration.RoundToMilliseconds().GetTicks(), Is.EqualTo(20000L));
        Assert.That(duration.RoundToSeconds().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.RoundToMinutes().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.RoundToHours().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.RoundToDays().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.TruncateToTicks().GetTicks(), Is.EqualTo(15000L));
        Assert.That(duration.TruncateToMicroseconds().GetTicks(), Is.EqualTo(15000L));
        Assert.That(duration.TruncateToMilliseconds().GetTicks(), Is.EqualTo(10000L));
        Assert.That(duration.TruncateToSeconds().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.TruncateToMinutes().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.TruncateToHours().GetTicks(), Is.EqualTo(0L));
        Assert.That(duration.TruncateToDays().GetTicks(), Is.EqualTo(0L));
    }
}
