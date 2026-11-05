namespace svg.Tests;

public class SvgTimingTests
{
    [Test]
    public void ClockValuesParseTimecountsUnitsAndSignedOffsets()
    {
        SvgClockParseResult seconds = new SvgTimingParser().ParseClock("3.5s", 32);
        SvgClockParseResult unitless = new SvgTimingParser().ParseClock("+ 3", 32);
        SvgClockParseResult milliseconds = new SvgTimingParser().ParseClock("250ms", 32);
        SvgClockParseResult minutes = new SvgTimingParser().ParseClock("-2min", 32);
        SvgClockParseResult hours = new SvgTimingParser().ParseClock("1.5h", 32);

        Assert.Multiple(() =>
        {
            Assert.That(seconds.Clock.Seconds, Is.EqualTo(3.5));
            Assert.That(unitless.Clock.Seconds, Is.EqualTo(3));
            Assert.That(milliseconds.Clock.Seconds, Is.EqualTo(0.25));
            Assert.That(minutes.Clock.Seconds, Is.EqualTo(-120));
            Assert.That(hours.Clock.Seconds, Is.EqualTo(5400));
            Assert.That(seconds.Clock.Source, Is.EqualTo("3.5s"));
        });
    }

    [Test]
    public void ClockValuesParseFullAndPartialClockForms()
    {
        SvgClockParseResult full = new SvgTimingParser().ParseClock("01:02:03.5", 32);
        SvgClockParseResult partial = new SvgTimingParser().ParseClock("02:30.25", 32);
        SvgClockParseResult negative = new SvgTimingParser().ParseClock("-00:00:05", 32);

        Assert.Multiple(() =>
        {
            Assert.That(full.Success, Is.True);
            Assert.That(full.Clock.Seconds, Is.EqualTo(3723.5));
            Assert.That(partial.Clock.Seconds, Is.EqualTo(150.25));
            Assert.That(negative.Clock.Seconds, Is.EqualTo(-5));
        });
    }

    [Test]
    public void ClockValuesRejectMalformedRangesExponentsAndBounds()
    {
        SvgClockParseResult shortMinute = new SvgTimingParser().ParseClock("1:02", 32);
        SvgClockParseResult badSeconds = new SvgTimingParser().ParseClock("00:60", 32);
        SvgClockParseResult exponent = new SvgTimingParser().ParseClock("1e2s", 32);
        SvgClockParseResult leadingPoint = new SvgTimingParser().ParseClock(".5s", 32);
        SvgClockParseResult extraColon = new SvgTimingParser().ParseClock("1:00:00:00", 32);
        SvgClockParseResult token = new SvgTimingParser().ParseClock("12345ms", 4);

        Assert.Multiple(() =>
        {
            Assert.That(shortMinute.Error, Is.EqualTo(SvgTimingParseError.InvalidClock));
            Assert.That(badSeconds.Error, Is.EqualTo(SvgTimingParseError.InvalidClock));
            Assert.That(exponent.Error, Is.EqualTo(SvgTimingParseError.InvalidClock));
            Assert.That(leadingPoint.Error, Is.EqualTo(SvgTimingParseError.InvalidClock));
            Assert.That(extraColon.Error, Is.EqualTo(SvgTimingParseError.InvalidClock));
            Assert.That(token.Error, Is.EqualTo(SvgTimingParseError.TokenTooLong));
        });
    }

    [Test]
    public void TimingSpecifiersParseOffsetsAndSynchronizationBases()
    {
        SvgTimingParseResult offset = new SvgTimingParser().ParseTiming("5s", 64);
        SvgTimingParseResult begin = new SvgTimingParser().ParseTiming("other.begin-250ms", 64);
        SvgTimingParseResult end = new SvgTimingParser().ParseTiming("node.end + 2s", 64);

        Assert.Multiple(() =>
        {
            Assert.That(offset.Timing.Kind, Is.EqualTo(SvgTimingKind.Offset));
            Assert.That(offset.Timing.OffsetSeconds, Is.EqualTo(5));
            Assert.That(begin.Timing.Kind, Is.EqualTo(SvgTimingKind.SyncBase));
            Assert.That(begin.Timing.BaseId, Is.EqualTo("other"));
            Assert.That(begin.Timing.Symbol, Is.EqualTo("begin"));
            Assert.That(begin.Timing.OffsetSeconds, Is.EqualTo(-0.25));
            Assert.That(end.Timing.BaseId, Is.EqualTo("node"));
            Assert.That(end.Timing.Symbol, Is.EqualTo("end"));
            Assert.That(end.Timing.OffsetSeconds, Is.EqualTo(2));
        });
    }

    [Test]
    public void EventRepeatAndAccessKeySpecifiersRetainTypedPayloads()
    {
        SvgTimingParseResult eventOnly = new SvgTimingParser().ParseTiming("click+1s", 64);
        SvgTimingParseResult eventBase = new SvgTimingParser().ParseTiming("button.mouseover-2s", 64);
        SvgTimingParseResult repeat = new SvgTimingParser().ParseTiming("anim.repeat(3)+0.5s", 64);
        SvgTimingParseResult access = new SvgTimingParser().ParseTiming("accessKey(😀)-1s", 64);

        Assert.Multiple(() =>
        {
            Assert.That(eventOnly.Timing.Kind, Is.EqualTo(SvgTimingKind.EventBase));
            Assert.That(eventOnly.Timing.BaseId, Is.Empty);
            Assert.That(eventOnly.Timing.Symbol, Is.EqualTo("click"));
            Assert.That(eventOnly.Timing.OffsetSeconds, Is.EqualTo(1));
            Assert.That(eventBase.Timing.BaseId, Is.EqualTo("button"));
            Assert.That(eventBase.Timing.Symbol, Is.EqualTo("mouseover"));
            Assert.That(repeat.Timing.Kind, Is.EqualTo(SvgTimingKind.Repeat));
            Assert.That(repeat.Timing.BaseId, Is.EqualTo("anim"));
            Assert.That(repeat.Timing.RepeatIteration, Is.EqualTo(3));
            Assert.That(repeat.Timing.OffsetSeconds, Is.EqualTo(0.5));
            Assert.That(access.Timing.Kind, Is.EqualTo(SvgTimingKind.AccessKey));
            Assert.That(access.Timing.AccessKey, Is.EqualTo("😀"));
            Assert.That(access.Timing.OffsetSeconds, Is.EqualTo(-1));
        });
    }

    [Test]
    public void WallclockAndIndefiniteValuesArePreserved()
    {
        string source = " wallclock(2026-08-18T12:30:00Z) ";
        SvgTimingParseResult wallclock = new SvgTimingParser().ParseTiming(source, 64);
        SvgTimingParseResult indefinite = new SvgTimingParser().ParseTiming("indefinite", 64);

        Assert.Multiple(() =>
        {
            Assert.That(wallclock.Success, Is.True);
            Assert.That(wallclock.Timing.Kind, Is.EqualTo(SvgTimingKind.Wallclock));
            Assert.That(wallclock.Timing.Wallclock, Is.EqualTo("2026-08-18T12:30:00Z"));
            Assert.That(wallclock.Timing.Source, Is.SameAs(source));
            Assert.That(wallclock.Timing.StartOffset, Is.EqualTo(1));
            Assert.That(wallclock.Timing.EndOffset, Is.EqualTo(source.Length - 1));
            Assert.That(indefinite.Timing.Kind, Is.EqualTo(SvgTimingKind.Indefinite));
        });
    }

    [Test]
    public void BeginAndEndListsPreserveSourceOffsetsAndValidPrefixes()
    {
        string input = " 0s; button.click+1s ; wallclock(2026-08-18T12:30:00Z); indefinite ";
        SvgTimingListParseResult begin = new SvgTimingParser().ParseBeginList(input, 8, 64);
        SvgTimingListParseResult end = new SvgTimingParser().ParseEndList("0s;bogus(;2s", 8, 64);
        SvgTimingListParseResult limit = new SvgTimingParser().ParseBeginList("0s;1s;2s", 2, 64);
        SvgTimingListParseResult trailing = new SvgTimingParser().ParseEndList("0s;", 4, 64);

        Assert.Multiple(() =>
        {
            Assert.That(begin.Success, Is.True);
            Assert.That(begin.Items, Has.Count.EqualTo(4));
            Assert.That(begin.Items[1].StartOffset, Is.EqualTo(input.IndexOf("button", StringComparison.Ordinal)));
            Assert.That(begin.Items[2].Source, Is.SameAs(input));
            Assert.That(end.Success, Is.False);
            Assert.That(end.Items, Has.Count.EqualTo(1));
            Assert.That(limit.Error, Is.EqualTo(SvgTimingParseError.ItemLimitExceeded));
            Assert.That(limit.Items, Has.Count.EqualTo(2));
            Assert.That(trailing.Error, Is.EqualTo(SvgTimingParseError.InvalidSeparator));
        });
    }

    [Test]
    public void MalformedTimingFunctionsAndIdentifiersReturnStableFailures()
    {
        SvgTimingParseResult repeat = new SvgTimingParser().ParseTiming("anim.repeat(0)", 64);
        SvgTimingParseResult access = new SvgTimingParser().ParseTiming("accessKey(ab)", 64);
        SvgTimingParseResult wallclock = new SvgTimingParser().ParseTiming("wallclock()", 64);
        SvgTimingParseResult identifier = new SvgTimingParser().ParseTiming("1bad", 64);

        Assert.Multiple(() =>
        {
            Assert.That(repeat.Error, Is.EqualTo(SvgTimingParseError.InvalidRepeat));
            Assert.That(access.Error, Is.EqualTo(SvgTimingParseError.InvalidAccessKey));
            Assert.That(wallclock.Error, Is.EqualTo(SvgTimingParseError.InvalidWallclock));
            Assert.That(identifier.Error, Is.EqualTo(SvgTimingParseError.InvalidIdentifier));
        });
    }

    [Test]
    public void KeyTimesAndKeyPointsParseBoundedMonotonicUnitValues()
    {
        SvgKeyValueListParseResult times = new SvgAnimationValueParser().ParseKeyTimes("0; .25; 0.5; 1", 8, 16);
        SvgKeyValueListParseResult points = new SvgAnimationValueParser().ParseKeyPoints("0;0.8;0.4;1", 8, 16);
        SvgKeyValueListParseResult decreasing = new SvgAnimationValueParser().ParseKeyTimes("0;.8;.7;1", 8, 16);
        SvgKeyValueListParseResult range = new SvgAnimationValueParser().ParseKeyPoints("0;1.1", 8, 16);
        SvgKeyValueListParseResult limit = new SvgAnimationValueParser().ParseKeyTimes("0;.5;1", 2, 16);
        SvgKeyValueListParseResult trailing = new SvgAnimationValueParser().ParseKeyTimes("0;1;", 4, 16);

        Assert.Multiple(() =>
        {
            Assert.That(times.Success, Is.True);
            Assert.That(times.Values, Is.EqualTo(new[] { 0.0, 0.25, 0.5, 1.0 }));
            Assert.That(points.Success, Is.True);
            Assert.That(points.Values, Is.EqualTo(new[] { 0.0, 0.8, 0.4, 1.0 }));
            Assert.That(decreasing.Error, Is.EqualTo(SvgTimingParseError.NonMonotonic));
            Assert.That(decreasing.Values, Is.EqualTo(new[] { 0.0, 0.8 }));
            Assert.That(range.Error, Is.EqualTo(SvgTimingParseError.InvalidKeyValue));
            Assert.That(limit.Error, Is.EqualTo(SvgTimingParseError.ItemLimitExceeded));
            Assert.That(trailing.Error, Is.EqualTo(SvgTimingParseError.InvalidSeparator));
        });
    }

    [Test]
    public void KeySplinesParseFourControlValuesPerSegment()
    {
        SvgKeySplineListParseResult valid = new SvgAnimationValueParser().ParseKeySplines("0 0 .5 1; .25,.1,.75,.9", 4, 16);
        SvgKeySplineListParseResult count = new SvgAnimationValueParser().ParseKeySplines("0 0 1", 4, 16);
        SvgKeySplineListParseResult range = new SvgAnimationValueParser().ParseKeySplines("0 0 1 1.2", 4, 16);
        SvgKeySplineListParseResult limit = new SvgAnimationValueParser().ParseKeySplines("0 0 1 1;0 0 1 1", 1, 16);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Splines, Has.Count.EqualTo(2));
            Assert.That(valid.Splines[0].X2, Is.EqualTo(0.5));
            Assert.That(valid.Splines[1].Y2, Is.EqualTo(0.9));
            Assert.That(count.Error, Is.EqualTo(SvgTimingParseError.InvalidKeyValue));
            Assert.That(range.Error, Is.EqualTo(SvgTimingParseError.InvalidKeyValue));
            Assert.That(limit.Error, Is.EqualTo(SvgTimingParseError.ItemLimitExceeded));
        });
    }

    [Test]
    public void AnimationKeywordParsersReturnEveryTypedEnumValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That((SvgAnimationRestart)new SvgAnimationKeywordParser().ParseRestart(" whenNotActive ").Value, Is.EqualTo(SvgAnimationRestart.WhenNotActive));
            Assert.That((SvgAnimationFill)new SvgAnimationKeywordParser().ParseFill("freeze").Value, Is.EqualTo(SvgAnimationFill.Freeze));
            Assert.That((SvgAnimationAdditive)new SvgAnimationKeywordParser().ParseAdditive("sum").Value, Is.EqualTo(SvgAnimationAdditive.Sum));
            Assert.That((SvgAnimationAccumulate)new SvgAnimationKeywordParser().ParseAccumulate("none").Value, Is.EqualTo(SvgAnimationAccumulate.None));
            Assert.That((SvgAnimationCalcMode)new SvgAnimationKeywordParser().ParseCalcMode("paced").Value, Is.EqualTo(SvgAnimationCalcMode.Paced));
            Assert.That((SvgAnimationAttributeType)new SvgAnimationKeywordParser().ParseAttributeType("CSS").Value, Is.EqualTo(SvgAnimationAttributeType.Css));
            Assert.That(new SvgAnimationKeywordParser().ParseRestart("Always").Error, Is.EqualTo(SvgTimingParseError.InvalidSpecifier));
            Assert.That(new SvgAnimationKeywordParser().ParseAttributeType("css").Error, Is.EqualTo(SvgTimingParseError.InvalidSpecifier));
            Assert.That(new SvgAnimationKeywordParser().ParseFill(null).Error, Is.EqualTo(SvgTimingParseError.InvalidArgument));
        });
    }
}
