using System;

namespace sdp.Tests.Unit.Operations;

public class SdpTimingOccurrenceTests
{
    [TestCase(2L, "", 2L)]
    [TestCase(2L, "s", 2L)]
    [TestCase(2L, "m", 120L)]
    [TestCase(-2L, "h", -7200L)]
    [TestCase(2L, "d", 172800L)]
    public void NormalizeTypedTime_ConvertsEverySupportedUnit(long value, string unit, long expectedSeconds)
    {
        ISdpTimingService service = new SdpTimingService();
        SdpTypedTime typedTime = new SdpTypedTime { Value = value, Unit = unit };

        SdpTypedTimeNormalizationResult result = service.NormalizeTypedTime(typedTime);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Seconds, Is.EqualTo(expectedSeconds));
            Assert.That(result.DiagnosticCode, Is.Empty);
            Assert.That(result.Detail, Is.Empty);
        });
    }

    [Test]
    public void NormalizeTypedTime_ReportsInvalidUnitsAndOverflow()
    {
        ISdpTimingService service = new SdpTimingService();

        SdpTypedTimeNormalizationResult invalid = service.NormalizeTypedTime(new SdpTypedTime { Value = 1, Unit = "H" });
        SdpTypedTimeNormalizationResult positiveOverflow = service.NormalizeTypedTime(new SdpTypedTime { Value = long.MaxValue, Unit = "m" });
        SdpTypedTimeNormalizationResult negativeOverflow = service.NormalizeTypedTime(new SdpTypedTime { Value = long.MinValue, Unit = "d" });

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ValidationTiming));
            Assert.That(invalid.Detail, Does.Contain("unit"));
            Assert.That(positiveOverflow.Success, Is.False);
            Assert.That(positiveOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.TimingUnrepresentable));
            Assert.That(negativeOverflow.Success, Is.False);
            Assert.That(negativeOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.TimingUnrepresentable));
        });
    }

    [Test]
    public void CalculateOccurrences_ExpandsRfcRepeatExampleChronologically()
    {
        SdpSessionDescription description = ParseTiming("t=3724394400 3730536000\r\nr=7d 1h 0 25h");
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset windowStart = Utc(2018, 1, 8, 9, 0, 0);
        DateTimeOffset windowStop = Utc(2018, 1, 17, 0, 0, 0);

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, windowStart, windowStop, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResultLimitReached, Is.False);
            Assert.That(result.Occurrences, Has.Count.EqualTo(4));
            Assert.That(result.Occurrences.Select(value => value.StartTime), Is.EqualTo(new[]
            {
                Utc(2018, 1, 8, 10, 0, 0),
                Utc(2018, 1, 9, 11, 0, 0),
                Utc(2018, 1, 15, 10, 0, 0),
                Utc(2018, 1, 16, 11, 0, 0)
            }));
            Assert.That(result.Occurrences.Select(value => value.StopTime - value.StartTime), Is.All.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(result.Occurrences.Select(value => value.OffsetIndex), Is.EqualTo(new[] { 0, 1, 0, 1 }));
        });
    }

    [Test]
    public void CalculateOccurrences_AppliesNonCumulativeZoneOffsetAtItsBaseTime()
    {
        SdpSessionDescription description = ParseTiming("t=2208988800 2209593600\r\nr=1d 1h 0\r\nz=2209161600 -1h");
        ISdpTimingService service = new SdpTimingService();

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, Utc(1970, 1, 1, 0, 0, 0), Utc(1970, 1, 4, 12, 0, 0), 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Occurrences.Select(value => value.StartTime), Is.EqualTo(new[]
            {
                Utc(1970, 1, 1, 0, 0, 0),
                Utc(1970, 1, 2, 0, 0, 0),
                Utc(1970, 1, 2, 23, 0, 0),
                Utc(1970, 1, 3, 23, 0, 0)
            }));
            Assert.That(result.Occurrences, Has.All.Matches<SdpTimingOccurrence>(value => value.StopTime - value.StartTime == TimeSpan.FromHours(1)));
        });
    }

    [Test]
    public void CalculateOccurrences_ClipsPermanentBoundedAndUnboundedPeriods()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Timings.Add(new SdpTiming { StartTime = "0", StopTime = "0" });
        description.Timings.Add(new SdpTiming { StartTime = "2209075200", StopTime = "2209082400" });
        description.Timings.Add(new SdpTiming { StartTime = "2209161600", StopTime = "0" });
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset windowStart = Utc(1970, 1, 1, 12, 0, 0);
        DateTimeOffset windowStop = Utc(1970, 1, 4, 0, 0, 0);

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, windowStart, windowStop, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Occurrences, Has.Count.EqualTo(3));
            Assert.That(result.Occurrences.Select(value => value.TimingIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Occurrences[0].StartTime, Is.EqualTo(windowStart));
            Assert.That(result.Occurrences[0].StopTime, Is.EqualTo(windowStop));
            Assert.That(result.Occurrences[1].StartTime, Is.EqualTo(Utc(1970, 1, 2, 0, 0, 0)));
            Assert.That(result.Occurrences[1].StopTime, Is.EqualTo(Utc(1970, 1, 2, 2, 0, 0)));
            Assert.That(result.Occurrences[2].StartTime, Is.EqualTo(Utc(1970, 1, 3, 0, 0, 0)));
            Assert.That(result.Occurrences[2].StopTime, Is.EqualTo(windowStop));
        });
    }

    [Test]
    public void CalculateOccurrences_MergesMultiplePeriodsAndClipsWindowEdgesChronologically()
    {
        SdpSessionDescription description = ParseTiming("t=2208988920 2208988980\r\nt=2208988800 2208989100\r\nr=1m 30s 0");
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset windowStart = Utc(1970, 1, 1, 0, 0, 15);
        DateTimeOffset windowStop = Utc(1970, 1, 1, 0, 2, 30);

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, windowStart, windowStop, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResultLimitReached, Is.False);
            Assert.That(result.Occurrences.Select(value => value.StartTime), Is.EqualTo(new[]
            {
                windowStart,
                Utc(1970, 1, 1, 0, 1, 0),
                Utc(1970, 1, 1, 0, 2, 0),
                Utc(1970, 1, 1, 0, 2, 0)
            }));
            Assert.That(result.Occurrences.Select(value => value.StopTime), Is.EqualTo(new[]
            {
                Utc(1970, 1, 1, 0, 0, 30),
                Utc(1970, 1, 1, 0, 1, 30),
                windowStop,
                windowStop
            }));
            Assert.That(result.Occurrences.Select(value => value.TimingIndex), Is.EqualTo(new[] { 1, 1, 0, 1 }));
        });
    }

    [Test]
    public void CalculateOccurrences_EnforcesResultLimitWithoutExpandingTheWindow()
    {
        SdpSessionDescription description = ParseTiming("t=2208988800 0\r\nr=1s 1s 0");
        ISdpTimingService service = new SdpTimingService();

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, Utc(1970, 1, 1, 0, 0, 0), Utc(9999, 1, 1, 0, 0, 0), 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResultLimitReached, Is.True);
            Assert.That(result.Occurrences, Has.Count.EqualTo(3));
            Assert.That(result.Occurrences.Select(value => value.StartTime), Is.EqualTo(new[]
            {
                Utc(1970, 1, 1, 0, 0, 0),
                Utc(1970, 1, 1, 0, 0, 1),
                Utc(1970, 1, 1, 0, 0, 2)
            }));
        });
    }

    [Test]
    public void CalculateOccurrences_ReportsInvalidRepeatDataWithoutPartialResults()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Timings.Add(new SdpTiming { StartTime = "2208988800", StopTime = "2208988860" });
        SdpTiming invalidTiming = new SdpTiming { StartTime = "2208988800", StopTime = "2208992400" };
        invalidTiming.RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 0 },
            ActiveDuration = new SdpTypedTime { Value = 1 },
            Offsets = { new SdpTypedTime { Value = 0 } }
        });
        description.Timings.Add(invalidTiming);
        ISdpTimingService service = new SdpTimingService();

        SdpTimingOccurrenceResult result = service.CalculateOccurrences(description, Utc(1970, 1, 1, 0, 0, 0), Utc(1970, 1, 2, 0, 0, 0), 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Occurrences, Is.Empty);
            Assert.That(result.ResultLimitReached, Is.False);
            Assert.That(result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ValidationTiming));
            Assert.That(result.Detail, Does.Contain("interval"));
        });
    }

    [Test]
    public void CalculateOccurrences_ValidatesWindowLimitAndNullArguments()
    {
        ISdpTimingService service = new SdpTimingService();
        SdpSessionDescription description = new SdpSessionDescription();
        DateTimeOffset start = Utc(1970, 1, 2, 0, 0, 0);
        DateTimeOffset stop = Utc(1970, 1, 1, 0, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CalculateOccurrences(null!, stop, start, 1), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.CalculateOccurrences(description, start, stop, 1), Throws.ArgumentException.With.Property("ParamName").EqualTo("windowStop"));
            Assert.That(() => service.CalculateOccurrences(description, stop, start, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("resultLimit"));
            Assert.That(() => service.NormalizeTypedTime(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("value"));
        });
    }

    private static SdpSessionDescription ParseTiming(string timingLines)
    {
        ISdpParser parser = new SdpParser();
        string input = "v=0\r\no=- 1 1 IN IP4 127.0.0.1\r\ns=-\r\n" + timingLines;
        SdpParseResult parsed = parser.ParseText(input, new SdpParseOptions());
        Assert.That(parsed.Success, Is.True);
        return parsed.Description ?? new SdpSessionDescription();
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }
}
