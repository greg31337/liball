using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class OffsetTests
{
    [Test]
    public void Zero_ReturnsUtcOffset()
    {
        Offset offset = Offset.Zero();

        Assert.That(offset.GetTotalSeconds(), Is.EqualTo(0));
        Assert.That(offset.GetSign(), Is.EqualTo(0));
        Assert.That(offset.GetHours(), Is.EqualTo(0));
        Assert.That(offset.GetMinutes(), Is.EqualTo(0));
        Assert.That(offset.GetSeconds(), Is.EqualTo(0));
    }

    [TestCase(-64800)]
    [TestCase(0)]
    [TestCase(64800)]
    public void Constructor_AllowsSupportedRangeEdges(int totalSeconds)
    {
        Offset offset = new Offset(totalSeconds);

        Assert.That(offset.GetTotalSeconds(), Is.EqualTo(totalSeconds));
    }

    [TestCase(-64801)]
    [TestCase(64801)]
    public void Constructor_RejectsValuesOutsideSupportedRange(int totalSeconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Offset(totalSeconds))!;

        Assert.That(exception.ParamName, Is.EqualTo("totalSeconds"));
    }

    [TestCase(0, 0, 0, 0, 0, 0)]
    [TestCase(1, 1, 0, 0, 1, 0)]
    [TestCase(-1, -1, 0, 0, 1, 0)]
    [TestCase(19800, 1, 5, 30, 0, 330)]
    [TestCase(-19845, -1, 5, 30, 45, -330)]
    [TestCase(64799, 1, 17, 59, 59, 1079)]
    [TestCase(-64799, -1, 17, 59, 59, -1079)]
    [TestCase(64800, 1, 18, 0, 0, 1080)]
    [TestCase(-64800, -1, 18, 0, 0, -1080)]
    public void FromSeconds_StoresOffsetAndProvidesComponents(int totalSeconds, int sign, int hours, int minutes, int seconds, int totalMinutes)
    {
        Offset offset = Offset.FromSeconds(totalSeconds);

        Assert.That(offset.GetTotalSeconds(), Is.EqualTo(totalSeconds));
        Assert.That(offset.GetSign(), Is.EqualTo(sign));
        Assert.That(offset.GetHours(), Is.EqualTo(hours));
        Assert.That(offset.GetMinutes(), Is.EqualTo(minutes));
        Assert.That(offset.GetSeconds(), Is.EqualTo(seconds));
        Assert.That(offset.GetTotalMinutes(), Is.EqualTo(totalMinutes));
    }

    [TestCase(-64801)]
    [TestCase(64801)]
    public void FromSeconds_RejectsOffsetsOutsideSupportedRange(int totalSeconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromSeconds(totalSeconds))!;

        Assert.That(exception.ParamName, Is.EqualTo("totalSeconds"));
    }

    [TestCase(1, 60)]
    [TestCase(-90, -5400)]
    [TestCase(1080, 64800)]
    [TestCase(-1080, -64800)]
    public void FromMinutes_ConvertsMinutesToSeconds(int minutes, int expectedSeconds)
    {
        Assert.That(Offset.FromMinutes(minutes).GetTotalSeconds(), Is.EqualTo(expectedSeconds));
    }

    [TestCase(1081)]
    [TestCase(-1081)]
    public void FromMinutes_RejectsOffsetsOutsideSupportedRange(int minutes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromMinutes(minutes));
    }

    [TestCase(1, 3600)]
    [TestCase(-5, -18000)]
    [TestCase(18, 64800)]
    [TestCase(-18, -64800)]
    public void FromHours_ConvertsHoursToSeconds(int hours, int expectedSeconds)
    {
        Assert.That(Offset.FromHours(hours).GetTotalSeconds(), Is.EqualTo(expectedSeconds));
    }

    [TestCase(19)]
    [TestCase(-19)]
    public void FromHours_RejectsOffsetsOutsideSupportedRange(int hours)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromHours(hours));
    }

    [TestCase(0, 0, 0, 0, "Z")]
    [TestCase(0, 0, 1, 1, "+00:00:01")]
    [TestCase(0, 0, -1, -1, "-00:00:01")]
    [TestCase(0, 30, 45, 1845, "+00:30:45")]
    [TestCase(0, -30, -45, -1845, "-00:30:45")]
    [TestCase(5, 30, 45, 19845, "+05:30:45")]
    [TestCase(-5, -30, -45, -19845, "-05:30:45")]
    [TestCase(5, 0, 45, 18045, "+05:00:45")]
    [TestCase(-5, 0, -45, -18045, "-05:00:45")]
    [TestCase(17, 59, 59, 64799, "+17:59:59")]
    [TestCase(-17, -59, -59, -64799, "-17:59:59")]
    [TestCase(18, 0, 0, 64800, "+18:00")]
    [TestCase(-18, 0, 0, -64800, "-18:00")]
    public void FromComponents_CombinesConsistentlySignedComponents(
        int hours,
        int minutes,
        int seconds,
        int expectedTotalSeconds,
        string expectedText)
    {
        Offset internalOffset = Offset.FromComponents(hours, minutes, seconds);
        Offset publicOffset = TimeApi.Factory.CreateOffsetFromComponents(hours, minutes, seconds);

        Assert.That(internalOffset.GetTotalSeconds(), Is.EqualTo(expectedTotalSeconds));
        Assert.That(internalOffset.ToIsoString(), Is.EqualTo(expectedText));
        Assert.That(publicOffset.EqualsValue(internalOffset), Is.True);
    }

    [Test]
    public void FromComponents_CoversEverySupportedSecondPrecisionOffset()
    {
        int totalSeconds = TimeConstants.MinOffsetSeconds;
        while (totalSeconds <= TimeConstants.MaxOffsetSeconds)
        {
            int sign = Math.Sign(totalSeconds);
            int absoluteSeconds = Math.Abs(totalSeconds);
            int hours = sign * (absoluteSeconds / 3600);
            int minutes = sign * (absoluteSeconds % 3600 / 60);
            int seconds = sign * (absoluteSeconds % 60);

            Offset offset = TimeApi.Factory.CreateOffsetFromComponents(hours, minutes, seconds);

            Assert.That(offset.GetTotalSeconds(), Is.EqualTo(totalSeconds));
            totalSeconds = totalSeconds + 1;
        }
    }

    [TestCase(19, 0, 0, "hours")]
    [TestCase(-19, 0, 0, "hours")]
    [TestCase(0, 60, 0, "minutes")]
    [TestCase(0, -60, 0, "minutes")]
    [TestCase(0, 0, 60, "seconds")]
    [TestCase(0, 0, -60, "seconds")]
    public void FromComponents_RejectsComponentsOutsideIndividualRanges(int hours, int minutes, int seconds, string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeApi.Factory.CreateOffsetFromComponents(hours, minutes, seconds))!;

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase(18, 1, 0)]
    [TestCase(18, 0, 1)]
    [TestCase(-18, -1, 0)]
    [TestCase(-18, 0, -1)]
    public void FromComponents_RejectsCombinedValuesOutsideEighteenHours(int hours, int minutes, int seconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeApi.Factory.CreateOffsetFromComponents(hours, minutes, seconds))!;

        Assert.That(exception.ParamName, Is.EqualTo("hours"));
    }

    [TestCase(1, -1, 0, "minutes")]
    [TestCase(-1, 1, 0, "minutes")]
    [TestCase(1, 0, -1, "seconds")]
    [TestCase(-1, 0, 1, "seconds")]
    [TestCase(0, 1, -1, "seconds")]
    [TestCase(0, -1, 1, "seconds")]
    [TestCase(1, -1, 1, "minutes")]
    public void FromComponents_RejectsMixedSignInputs(int hours, int minutes, int seconds, string parameterName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TimeApi.Factory.CreateOffsetFromComponents(hours, minutes, seconds))!;

        Assert.That(exception, Is.Not.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
    }

    [Test]
    public void FromFactories_ThrowWhenArithmeticOverflows()
    {
        Assert.Throws<OverflowException>(() => Offset.FromMinutes(int.MaxValue));
        Assert.Throws<OverflowException>(() => Offset.FromHours(int.MaxValue));
    }

    [Test]
    public void ToTimeSpan_ReturnsEquivalentTimeSpan()
    {
        Offset offset = Offset.FromSeconds(-19845);

        Assert.That(offset.ToTimeSpan(), Is.EqualTo(TimeSpan.FromSeconds(-19845)));
    }

    [Test]
    public void FromTimeSpan_StoresWholeSecondTimeSpan()
    {
        TimeSpan timeSpan = new TimeSpan(5, 30, 45);
        Offset offset = Offset.FromTimeSpan(timeSpan);

        Assert.That(offset.GetTotalSeconds(), Is.EqualTo(19845));
        Assert.That(offset.ToTimeSpan(), Is.EqualTo(timeSpan));
    }

    [TestCase(-64800)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(64800)]
    public void TimeSpanInterop_RoundTripsRepresentativeOffsets(int totalSeconds)
    {
        Offset offset = Offset.FromSeconds(totalSeconds);
        Offset parsed = Offset.FromTimeSpan(offset.ToTimeSpan());

        Assert.That(parsed.EqualsValue(offset), Is.True);
    }

    [Test]
    public void FromTimeSpan_RejectsSubsecondValues()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Offset.FromTimeSpan(TimeSpan.FromTicks(1L)))!;

        Assert.That(exception.ParamName, Is.EqualTo("timeSpan"));
    }

    [Test]
    public void FromTimeSpan_RejectsValuesOutsideSupportedRange()
    {
        ArgumentOutOfRangeException positiveException = Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromTimeSpan(TimeSpan.FromHours(18).Add(TimeSpan.FromSeconds(1))))!;
        ArgumentOutOfRangeException negativeException = Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromTimeSpan(TimeSpan.FromHours(-18).Subtract(TimeSpan.FromSeconds(1))))!;

        Assert.That(positiveException.ParamName, Is.EqualTo("totalSeconds"));
        Assert.That(negativeException.ParamName, Is.EqualTo("totalSeconds"));
    }

    [Test]
    public void FromTimeSpan_RejectsWholeSecondValuesOutsideIntegerRange()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Offset.FromTimeSpan(TimeSpan.FromDays(30000)))!;

        Assert.That(exception.ParamName, Is.EqualTo("timeSpan"));
    }

    [TestCase(0, "Z")]
    [TestCase(1, "+00:00:01")]
    [TestCase(-1, "-00:00:01")]
    [TestCase(19800, "+05:30")]
    [TestCase(-28800, "-08:00")]
    [TestCase(19845, "+05:30:45")]
    [TestCase(-19845, "-05:30:45")]
    [TestCase(64799, "+17:59:59")]
    [TestCase(-64799, "-17:59:59")]
    [TestCase(64800, "+18:00")]
    [TestCase(-64800, "-18:00")]
    public void ToIsoString_FormatsZeroMinuteAndSecondOffsets(int totalSeconds, string expected)
    {
        Assert.That(Offset.FromSeconds(totalSeconds).ToIsoString(), Is.EqualTo(expected));
    }

    [TestCase("Z", 0)]
    [TestCase("+00:00", 0)]
    [TestCase("-00:00", 0)]
    [TestCase("+00:00:01", 1)]
    [TestCase("-00:00:01", -1)]
    [TestCase("+05:30", 19800)]
    [TestCase("-08:00", -28800)]
    [TestCase("+05:30:45", 19845)]
    [TestCase("-05:30:45", -19845)]
    [TestCase("+17:59:59", 64799)]
    [TestCase("-17:59:59", -64799)]
    [TestCase("+18:00", 64800)]
    [TestCase("-18:00", -64800)]
    public void Parse_ReadsZeroMinuteAndSecondOffsets(string text, int expectedSeconds)
    {
        Assert.That(Offset.Parse(text).GetTotalSeconds(), Is.EqualTo(expectedSeconds));
    }

    [TestCase("Z")]
    [TestCase("+00:00")]
    [TestCase("-00:00")]
    [TestCase("+00:00:00")]
    [TestCase("-00:00:00")]
    public void Parse_NormalizesZeroOffsetFormsToCanonicalZ(string text)
    {
        Assert.That(Offset.Parse(text).ToIsoString(), Is.EqualTo("Z"));
    }

    [Test]
    public void Parse_RequiresNonNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Offset.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("z")]
    [TestCase("00:00")]
    [TestCase("x00:00")]
    [TestCase("+0:00")]
    [TestCase("+00")]
    [TestCase("+0000")]
    [TestCase("+00:0")]
    [TestCase("+00:00:0")]
    [TestCase("+00:00:00:")]
    [TestCase("+00-00")]
    [TestCase("+00:60")]
    [TestCase("+00:00:60")]
    [TestCase("+18:00:01")]
    [TestCase("-18:00:01")]
    [TestCase("+18:01")]
    [TestCase("-18:01")]
    [TestCase("+19:00")]
    [TestCase("-19:00")]
    [TestCase("+24:00")]
    [TestCase("+0A:00")]
    public void Parse_RejectsMalformedOrUnsupportedOffsetText(string text)
    {
        Assert.Throws<FormatException>(() => Offset.Parse(text));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(19800)]
    [TestCase(-19845)]
    [TestCase(64800)]
    [TestCase(-64800)]
    public void Parse_RoundTripsFormattedOffsets(int totalSeconds)
    {
        Offset offset = Offset.FromSeconds(totalSeconds);
        Offset parsed = Offset.Parse(offset.ToIsoString());

        Assert.That(parsed.EqualsValue(offset), Is.True);
    }

    [Test]
    public void CompareTo_OrdersByTotalSeconds()
    {
        Assert.That(Offset.FromHours(-1).CompareTo(Offset.Zero()), Is.EqualTo(-1));
        Assert.That(Offset.Zero().CompareTo(Offset.FromSeconds(0)), Is.EqualTo(0));
        Assert.That(Offset.FromHours(1).CompareTo(Offset.Zero()), Is.EqualTo(1));
    }

    [Test]
    public void CompareTo_OrdersSupportedRangeBoundaries()
    {
        Offset min = Offset.FromSeconds(TimeConstants.MinOffsetSeconds);
        Offset negativeOne = Offset.FromSeconds(-1);
        Offset zero = Offset.Zero();
        Offset positiveOne = Offset.FromSeconds(1);
        Offset max = Offset.FromSeconds(TimeConstants.MaxOffsetSeconds);

        Assert.That(min.CompareTo(negativeOne), Is.LessThan(0));
        Assert.That(negativeOne.CompareTo(zero), Is.LessThan(0));
        Assert.That(zero.CompareTo(positiveOne), Is.LessThan(0));
        Assert.That(positiveOne.CompareTo(max), Is.LessThan(0));
    }

    [Test]
    public void CompareTo_RequiresOtherOffset()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Offset.Zero().CompareTo(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("other"));
    }

    [Test]
    public void EqualsValue_ComparesTotalSecondsAndHandlesNull()
    {
        Assert.That(Offset.FromMinutes(90).EqualsValue(Offset.FromSeconds(5400)), Is.True);
        Assert.That(Offset.FromMinutes(90).EqualsValue(Offset.FromMinutes(91)), Is.False);
        Assert.That(Offset.Zero().EqualsValue(null), Is.False);
    }
}
