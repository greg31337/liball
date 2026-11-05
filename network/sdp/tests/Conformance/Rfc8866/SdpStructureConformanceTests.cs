namespace sdp.Tests.Conformance.Rfc8866;

public class SdpStructureConformanceTests
{
    [TestCase("o=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0", 'v')]
    [TestCase("v=0\r\ns=-\r\nt=0 0", 'o')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\nt=0 0", 's')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-", 't')]
    public void EveryRequiredField_IsReportedWhenAbsent(string input, char fieldName)
    {
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMissingRequiredField));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo(fieldName));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
        });
    }

    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nu=relative\r\ni=late\r\nt=0 0", 'i')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\ne=a@example.test\r\nu=late\r\nt=0 0", 'u')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\np=+1 555 0100\r\ne=late@example.test\r\nt=0 0", 'e')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nc=IN IP4 host\r\np=+1 555 0100\r\nt=0 0", 'p')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nb=AS:1\r\nc=IN IP4 host\r\nt=0 0", 'c')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\nb=AS:1", 'b')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\na=recvonly\r\nt=0 0", 't')]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\na=recvonly\r\nk=prompt", 'k')]
    public void EverySessionFieldStage_IsRejectedWhenMovedBehindALaterStage(string input, char fieldName)
    {
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpDiagnostic order = FindDiagnostic(result, SdpDiagnosticCodes.ParseFieldOutOfOrder, fieldName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(order.Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(order.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(order.Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(order.FieldName, Is.EqualTo(fieldName));
        });
    }

    [TestCase("c=IN IP4 host\r\ni=late", 'i')]
    [TestCase("b=AS:1\r\nc=IN IP4 host", 'c')]
    [TestCase("k=prompt\r\nb=AS:1", 'b')]
    [TestCase("a=sendrecv\r\nk=prompt", 'k')]
    public void EveryMediaFieldStage_IsRejectedWhenMovedBehindALaterStage(string mediaFields, char fieldName)
    {
        ISdpParser parser = new SdpParser();
        string input = HeaderAndTiming() + "m=audio 9 RTP/AVP 0\r\n" + mediaFields;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpDiagnostic order = FindDiagnostic(result, SdpDiagnosticCodes.ParseFieldOutOfOrder, fieldName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(order.Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(order.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(order.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(order.MediaIndex, Is.Zero);
            Assert.That(order.FieldName, Is.EqualTo(fieldName));
        });
    }

    [Test]
    public void ANewMediaBoundary_ResetsMediaFieldOrdering()
    {
        ISdpParser parser = new SdpParser();
        string input =
            HeaderAndTiming() +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=sendrecv\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "i=Second media information\r\n" +
            "c=IN IP4 host.example\r\n" +
            "b=AS:100\r\n" +
            "a=recvonly";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(result.Description?.MediaDescriptions[1].Information, Is.EqualTo("Second media information"));
        });
    }

    [TestCase("v=0\r\nv=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0", 'v', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=first 1 1 IN IP4 host\r\no=second 2 2 IN IP4 host\r\ns=-\r\nt=0 0", 'o', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=first\r\ns=second\r\nt=0 0", 's', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\ni=first\r\ni=second\r\nt=0 0", 'i', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nu=first\r\nu=second\r\nt=0 0", 'u', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nc=IN IP4 first.example\r\nc=IN IP4 second.example\r\nt=0 0", 'c', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\nk=prompt\r\nk=prompt", 'k', SdpScope.Session, -1)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\nm=audio 9 RTP/AVP 0\r\ni=first\r\ni=second", 'i', SdpScope.Media, 0)]
    [TestCase("v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\nm=audio 9 RTP/AVP 0\r\nk=prompt\r\nk=prompt", 'k', SdpScope.Media, 0)]
    public void EverySingularFieldCardinality_IsEnforced(string input, char fieldName, SdpScope scope, int mediaIndex)
    {
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpDiagnostic duplicate = FindDiagnostic(result, SdpDiagnosticCodes.ParseDuplicateField, fieldName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(duplicate.Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(duplicate.Scope, Is.EqualTo(scope));
            Assert.That(duplicate.MediaIndex, Is.EqualTo(mediaIndex));
            Assert.That(duplicate.Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void EveryRepeatedFieldRetainsSourceOrder()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "e=first@example.test\r\n" +
            "e=second@example.test\r\n" +
            "p=+1 555 0100\r\n" +
            "p=+1 555 0101\r\n" +
            "b=AS:1\r\n" +
            "b=CT:2\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "r=1d 1h 0\r\n" +
            "a=x:first\r\n" +
            "a=x:second\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "c=IN IP4 first.example\r\n" +
            "c=IN IP4 second.example\r\n" +
            "b=AS:3\r\n" +
            "b=CT:4\r\n" +
            "a=x:third\r\n" +
            "a=x:fourth";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Emails, Is.EqualTo(new string[] { "first@example.test", "second@example.test" }));
            Assert.That(result.Description?.Phones, Is.EqualTo(new string[] { "+1 555 0100", "+1 555 0101" }));
            Assert.That(result.Description?.Bandwidths[1].Type, Is.EqualTo("CT"));
            Assert.That(result.Description?.Timings[0].RepeatTimes, Has.Count.EqualTo(2));
            Assert.That(result.Description?.Attributes[1].Value, Is.EqualTo("second"));
            Assert.That(result.Description?.MediaDescriptions[0].Connections[1].Address, Is.EqualTo("second.example"));
            Assert.That(result.Description?.MediaDescriptions[0].Bandwidths[1].Type, Is.EqualTo("CT"));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes[1].Value, Is.EqualTo("fourth"));
        });
    }

    [TestCase("v=0")]
    [TestCase("o=- 1 1 IN IP4 host")]
    [TestCase("s=late")]
    [TestCase("u=relative")]
    [TestCase("e=late@example.test")]
    [TestCase("p=+1 555 0100")]
    [TestCase("t=0 0")]
    [TestCase("r=7d 1h 0")]
    [TestCase("z=2882844526 -1h")]
    public void SessionOnlyFields_AreRejectedInMediaScope(string line)
    {
        ISdpParser parser = new SdpParser();
        string input = HeaderAndTiming() + "m=audio 9 RTP/AVP 0\r\n" + line;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo(line[0]));
        });
    }

    [TestCase("s=", 's')]
    [TestCase("i=", 'i')]
    [TestCase("e=", 'e')]
    [TestCase("p=", 'p')]
    [TestCase("c=", 'c')]
    [TestCase("b=", 'b')]
    public void EmptyRequiredByteStringOrTokenFields_AreRejectedBeforeTiming(string line, char fieldName)
    {
        ISdpParser parser = new SdpParser();
        string input;
        if (fieldName == 's')
        {
            input = "v=0\r\no=- 1 1 IN IP4 host\r\n" + line + "\r\nt=0 0";
        }
        else
        {
            input = "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\n" + line + "\r\nt=0 0";
        }

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo(fieldName));
        });
    }

    [TestCase("t=")]
    [TestCase("r=")]
    [TestCase("z=")]
    [TestCase("a=")]
    [TestCase("m=")]
    public void EmptyTimingAttributeAndMediaProductions_AreRejected(string line)
    {
        ISdpParser parser = new SdpParser();
        string input = HeaderAndTiming() + line;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void UnknownField_IsRejectedInStrictModeAndPreservedInLosslessMode()
    {
        ISdpParser parser = new SdpParser();
        string input = "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nx=future-field\r\nt=0 0";

        SdpParseResult strict = parser.ParseText(input, new SdpParseOptions());
        SdpParseResult lossless = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription strictDescription = strict.Description ?? new SdpSessionDescription();
        SdpSessionDescription losslessDescription = lossless.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(strict.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseUnknownField));
            Assert.That(strict.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(strictDescription.RawLines, Is.Empty);

            Assert.That(lossless.Success, Is.True);
            Assert.That(lossless.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(lossless.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseUnknownField));
            Assert.That(lossless.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(losslessDescription.RawLines, Has.Count.EqualTo(5));
            Assert.That(losslessDescription.RawLines[3].FieldName, Is.EqualTo('x'));
            Assert.That(losslessDescription.RawLines[3].Value, Is.EqualTo("future-field"));
            Assert.That(losslessDescription.RawLines[3].IsMalformed, Is.False);
        });
    }

    [Test]
    public void LosslessMalformedRecovery_RetainsEachLineAndContinuesToRequiredTiming()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "BROKEN\r\n" +
            "b=AS:not-a-number\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[3].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(description.RawLines, Has.Count.EqualTo(6));
            Assert.That(description.RawLines[3].Text, Is.EqualTo("BROKEN"));
            Assert.That(description.RawLines[3].IsMalformed, Is.True);
            Assert.That(description.RawLines[4].FieldName, Is.EqualTo('b'));
            Assert.That(description.RawLines[4].IsMalformed, Is.True);
            Assert.That(description.Bandwidths[0].RawValue, Is.EqualTo("not-a-number"));
            Assert.That(description.Timings, Has.Count.EqualTo(1));
        });
    }

    private static SdpDiagnostic FindDiagnostic(SdpParseResult result, string code, char fieldName)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            SdpDiagnostic diagnostic = result.Diagnostics[index];
            if (diagnostic.Code == code && diagnostic.FieldName == fieldName)
            {
                return diagnostic;
            }
            index = index + 1;
        }
        return new SdpDiagnostic();
    }

    private static string HeaderAndTiming()
    {
        return "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nt=0 0\r\n";
    }
}
