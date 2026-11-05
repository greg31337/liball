using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpTimingLineParserTests
{
    [Test]
    public void MultipleTimingBlocks_PreserveOrderAndAttachRepeatsToTheCurrentBlock()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0 25h\r\n" +
            "r=1h 10m 5m\r\n" +
            "t=3724394400 3730536000";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Timings, Has.Count.EqualTo(2));
            Assert.That(description.Timings[0].StartTime, Is.EqualTo("0"));
            Assert.That(description.Timings[0].StopTime, Is.EqualTo("0"));
            Assert.That(description.Timings[0].RawText, Is.EqualTo("0 0"));
            Assert.That(description.Timings[0].RepeatTimes, Has.Count.EqualTo(2));
            Assert.That(description.Timings[1].StartTime, Is.EqualTo("3724394400"));
            Assert.That(description.Timings[1].StopTime, Is.EqualTo("3730536000"));
            Assert.That(description.Timings[1].RepeatTimes, Is.Empty);
            Assert.That(description.Timings[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(description.Timings[1].Location.LineNumber, Is.EqualTo(5));
        });
    }

    [Test]
    public void Timing_PreservesNtpValuesBeyondMachineIntegerRanges()
    {
        ISdpParser parser = new SdpFragmentParser();
        string start = "1234567890123456789012345678901234567890";
        string stop = "9999999999999999999999999999999999999999";

        SdpParseResult result = parser.ParseText("v=0\r\nt=" + start + " " + stop, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Timings[0].StartTime, Is.EqualTo(start));
            Assert.That(result.Description?.Timings[0].StopTime, Is.EqualTo(stop));
        });
    }

    [TestCase("t=")]
    [TestCase("t=0")]
    [TestCase("t=0 0 extra")]
    [TestCase("t= 0 0")]
    [TestCase("t=0  0")]
    [TestCase("t=0 0 ")]
    public void Timing_RequiresExactlyTwoSingleSpaceSeparatedTokens(string timingLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\n" + timingLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('t'));
        });
    }

    [Test]
    public void InvalidNtpTokens_AreRetainedAndDiagnosedIndependently()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0123456789 123456789", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Timings[0].StartTime, Is.EqualTo("0123456789"));
            Assert.That(result.Description?.Timings[0].StopTime, Is.EqualTo("123456789"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[1].Location.ColumnNumber, Is.EqualTo(14));
        });
    }

    [Test]
    public void Repeat_ParsesTypedTimesAndOrderedOffsets()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\nr=7d 1h 0 25h", new SdpParseOptions());
        SdpRepeatTime repeat = result.Description?.Timings[0].RepeatTimes[0] ?? new SdpRepeatTime();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(repeat.RepeatInterval.Value, Is.EqualTo(7));
            Assert.That(repeat.RepeatInterval.Unit, Is.EqualTo("d"));
            Assert.That(repeat.RepeatInterval.RawValue, Is.EqualTo("7d"));
            Assert.That(repeat.ActiveDuration.Value, Is.EqualTo(1));
            Assert.That(repeat.ActiveDuration.Unit, Is.EqualTo("h"));
            Assert.That(repeat.Offsets, Has.Count.EqualTo(2));
            Assert.That(repeat.Offsets[0].Value, Is.Zero);
            Assert.That(repeat.Offsets[0].Unit, Is.Empty);
            Assert.That(repeat.Offsets[1].Value, Is.EqualTo(25));
            Assert.That(repeat.Offsets[1].Unit, Is.EqualTo("h"));
            Assert.That(repeat.RawText, Is.EqualTo("7d 1h 0 25h"));
            Assert.That(repeat.Location.LineNumber, Is.EqualTo(3));
        });
    }

    [TestCase("r=")]
    [TestCase("r=7d")]
    [TestCase("r=7d 1h")]
    [TestCase("r= 7d 1h 0")]
    [TestCase("r=7d  1h 0")]
    [TestCase("r=7d 1h 0 ")]
    public void Repeat_RequiresAtLeastThreeSingleSpaceSeparatedTokens(string repeatLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\n" + repeatLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings[0].RepeatTimes, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('r'));
        });
    }

    [Test]
    public void Repeat_RequiresPositiveIntervalAndActiveDuration()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\nr=0 0s 0", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings[0].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[1].Location.ColumnNumber, Is.EqualTo(5));
        });
    }

    [Test]
    public void InvalidRepeatComponents_RetainTheirRawTokensAndReportOverflowOrGrammar()
    {
        ISdpParser parser = new SdpFragmentParser();
        string overflow = "9223372036854775808";

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\nr=" + overflow + " 1H +1", new SdpParseOptions());
        SdpRepeatTime repeat = result.Description?.Timings[0].RepeatTimes[0] ?? new SdpRepeatTime();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(repeat.RepeatInterval.RawValue, Is.EqualTo(overflow));
            Assert.That(repeat.ActiveDuration.RawValue, Is.EqualTo("1H"));
            Assert.That(repeat.Offsets[0].RawValue, Is.EqualTo("+1"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
        });
    }

    [Test]
    public void TimeZoneAdjustment_RequiresAnAttachedRepeat()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nt=3724394400 3730536000\r\nz=3730928400 -1h 3749680800 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(description.TimeZoneAdjustments, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('z'));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
        });
    }

    [Test]
    public void TimeZoneAdjustment_AcceptsPairsAfterAttachedRepeats()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nt=0 0\r\nr=7d 1h 0\r\nz=3730928400 -1h";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.Timings[0].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void InvalidTimeZonePairValues_AreRetainedAndDiagnosed()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\nr=7d 1h 0\r\nz=0 +1h", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
            Assert.That(result.Description?.TimeZoneAdjustments[0].AdjustmentTime, Is.EqualTo("0"));
            Assert.That(result.Description?.TimeZoneAdjustments[0].Offset.RawValue, Is.EqualTo("+1h"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
        });
    }

    [TestCase("z=3730928400")]
    [TestCase("z=3730928400 -1h 3749680800")]
    [TestCase("z=3730928400  -1h")]
    public void TimeZoneAdjustment_RequiresSingleSpaceSeparatedPairs(string zoneLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0 0\r\nr=7d 1h 0\r\n" + zoneLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.TimeZoneAdjustments, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
        });
    }

    [TestCase("v=0\r\nr=7d 1h 0")]
    [TestCase("v=0\r\nt=0 0\r\ns=x\r\nr=7d 1h 0")]
    [TestCase("v=0\r\nt=0 0\r\nz=3730928400 0\r\nr=7d 1h 0")]
    public void RepeatOutsideItsImmediateTimingBlock_IsRejected(string input)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[^1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[^1].FieldName, Is.EqualTo('r'));
        });
    }

    [Test]
    public void ASecondZoneIsRejectedAndFollowingTimingStartsANewBlock()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "z=3730928400 0\r\n" +
            "z=3749680800 1h\r\n" +
            "t=3724394400 3730536000";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(2));
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('z'));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void TimingFieldsInMediaScope_AreRejected()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nm=audio 9 RTP/AVP 0\r\nt=0 0\r\nr=7d 1h 0\r\nz=3730928400 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Is.Empty);
            Assert.That(result.Description?.TimeZoneAdjustments, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
        });
    }

    [Test]
    public void TimingOccurrencePathsIncludeMalformedSourceOccurrences()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nt=0\r\nt=123456789 0", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TimingTokenLimit_StopsBeforeFollowingLines()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 9 };

        SdpParseResult result = parser.ParseText("v=0\r\nt=1234567890 0\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Is.Empty);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void Utf8TimingTokenLimit_IsMeasuredInBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 6 };

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes("v=0\r\nt=éééé 0"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Timings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }
}
