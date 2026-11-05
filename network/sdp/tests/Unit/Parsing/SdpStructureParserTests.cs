namespace sdp.Tests.Unit.Parsing;

public class SdpStructureParserTests
{
    [Test]
    public void OrderedSessionAndMediaFields_AreAccepted()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 127.0.0.1\r\n" +
            "s=Example\r\n" +
            "i=Session information\r\n" +
            "u=https://example.test/\r\n" +
            "e=ops@example.test\r\n" +
            "p=+1 555 0100\r\n" +
            "c=IN IP4 224.2.17.12/127\r\n" +
            "b=AS:128\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "z=2882844526 -1h\r\n" +
            "a=recvonly\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "i=Audio stream\r\n" +
            "c=IN IP4 224.2.17.12/127\r\n" +
            "b=AS:64\r\n" +
            "a=sendrecv";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Timings[0].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
        });
    }

    [TestCase("o=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0", 'v')]
    [TestCase("v=0\r\ns=-\r\nt=0 0", 'o')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\nt=0 0", 's')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-", 't')]
    public void MissingRequiredSessionField_IsReportedAtCompletion(string input, char fieldName)
    {
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo(fieldName));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(input.Length));
        });
    }

    [TestCase(SdpParseMode.Strict, SdpValidationSeverity.Error, false)]
    [TestCase(SdpParseMode.Interoperable, SdpValidationSeverity.Warning, true)]
    [TestCase(SdpParseMode.Lossless, SdpValidationSeverity.Warning, true)]
    public void SessionFieldOrder_IsEnforcedAccordingToMode(SdpParseMode mode, SdpValidationSeverity severity, bool success)
    {
        ISdpParser parser = new SdpParser();
        string input = "v=0\r\ns=-\r\no=- 1 1 IN IP4 host\r\nt=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('o'));
            Assert.That(result.Description?.Origin?.SessionId, Is.EqualTo("1"));
        });
    }

    [TestCase(SdpParseMode.Strict, SdpValidationSeverity.Error, false)]
    [TestCase(SdpParseMode.Interoperable, SdpValidationSeverity.Warning, true)]
    [TestCase(SdpParseMode.Lossless, SdpValidationSeverity.Warning, true)]
    public void MediaFieldOrder_IsEnforcedAccordingToMode(SdpParseMode mode, SdpValidationSeverity severity, bool success)
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=sendrecv\r\n" +
            "c=IN IP4 192.0.2.1";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('c'));
            Assert.That(result.Description?.MediaDescriptions[0].Connections, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DuplicateSingularField_IsRejectedWithoutReplacingTheFirstValue()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=first 1 1 IN IP4 host\r\n" +
            "o=second 2 2 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Description?.Origin?.Username, Is.EqualTo("first"));
        });
    }

    [Test]
    public void SessionOnlyFieldInMediaScope_IsRejected()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "u=https://example.test/";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('u'));
            Assert.That(result.Description?.Uri, Is.Null);
        });
    }

    [TestCase(SdpParseMode.Strict, SdpValidationSeverity.Error, false)]
    [TestCase(SdpParseMode.Interoperable, SdpValidationSeverity.Warning, true)]
    [TestCase(SdpParseMode.Lossless, SdpValidationSeverity.Warning, true)]
    public void UnknownLowercaseField_IsDiagnosedAccordingToMode(SdpParseMode mode, SdpValidationSeverity severity, bool success)
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "x=extension\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseUnknownField));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('x'));
        });
    }

    [TestCase(SdpParseMode.Strict, 1)]
    [TestCase(SdpParseMode.Interoperable, 1)]
    [TestCase(SdpParseMode.Lossless, 2)]
    public void MalformedFieldPrefix_RemainsAnErrorInEveryMode(SdpParseMode mode, int diagnosticCount)
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "X=extension\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(diagnosticCount));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            if (mode == SdpParseMode.Lossless)
            {
                Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            }
        });
    }

    [Test]
    public void MediaCannotBeginBeforeTheRequiredSessionTiming()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "m=audio 9 RTP/AVP 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('m'));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('t'));
        });
    }

    [Test]
    public void MultipleTimingBlocksAndOneFollowingZoneField_AreAccepted()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "t=3724394400 3730536000\r\n" +
            "r=1h 10m 0\r\n" +
            "z=2882844526 -1h";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(2));
            Assert.That(result.Description?.Timings[0].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Timings[1].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
        });
    }

    [TestCase(SdpParseMode.Strict, SdpValidationSeverity.Error, false)]
    [TestCase(SdpParseMode.Interoperable, SdpValidationSeverity.Warning, true)]
    [TestCase(SdpParseMode.Lossless, SdpValidationSeverity.Warning, true)]
    public void SecondZoneInOneTimingBlock_IsDiagnosedAccordingToMode(SdpParseMode mode, SdpValidationSeverity severity, bool success)
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "z=2882844526 -1h\r\n" +
            "z=2898848070 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('z'));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
            Assert.That(result.Description?.TimeZoneAdjustments, Has.Count.EqualTo(1));
        });
    }
}
