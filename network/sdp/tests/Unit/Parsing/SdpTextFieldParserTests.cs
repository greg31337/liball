using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpTextFieldParserTests
{
    [Test]
    public void TextFields_ArePreservedInSessionAndMediaSourceOrder()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "s=Résumé planning\r\n" +
            "i=Session information\r\n" +
            "u=https://example.test/session?q=a%20b#details\r\n" +
            "e=first@example.test (First Contact)\r\n" +
            "e=Second Contact <second@example.test>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "p=Support <+44 20 7946 0958>\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "i=Primary audio\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "i=Slides";

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes(input), new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpMediaDescription firstMedia = description.MediaDescriptions.Count > 0 ? description.MediaDescriptions[0] : new SdpMediaDescription();
        SdpMediaDescription secondMedia = description.MediaDescriptions.Count > 1 ? description.MediaDescriptions[1] : new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.HasSessionName, Is.True);
            Assert.That(description.SessionName, Is.EqualTo("Résumé planning"));
            Assert.That(description.Information, Is.EqualTo("Session information"));
            Assert.That(description.Uri, Is.EqualTo("https://example.test/session?q=a%20b#details"));
            Assert.That(description.Emails, Is.EqualTo(new string[] { "first@example.test (First Contact)", "Second Contact <second@example.test>" }));
            Assert.That(description.Phones, Is.EqualTo(new string[] { "+1 617 555-6011", "Support <+44 20 7946 0958>" }));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(firstMedia.Information, Is.EqualTo("Primary audio"));
            Assert.That(secondMedia.Information, Is.EqualTo("Slides"));
        });
    }

    [Test]
    public void SessionName_SingleSpaceIsContentButEmptyValueIsInvalid()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult singleSpace = parser.ParseText("v=0\r\ns= ", new SdpParseOptions());
        SdpParseResult empty = parser.ParseText("v=0\r\ns=", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(singleSpace.Success, Is.True);
            Assert.That(singleSpace.Description?.HasSessionName, Is.True);
            Assert.That(singleSpace.Description?.SessionName, Is.EqualTo(" "));
            Assert.That(singleSpace.Diagnostics, Is.Empty);
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Description?.HasSessionName, Is.True);
            Assert.That(empty.Description?.SessionName, Is.Empty);
            Assert.That(empty.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(empty.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(empty.Diagnostics[0].FieldName, Is.EqualTo('s'));
            Assert.That(empty.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(empty.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [TestCase(SdpParseMode.Strict, SdpValidationSeverity.Error, false, 1)]
    [TestCase(SdpParseMode.Interoperable, SdpValidationSeverity.Warning, true, 1)]
    [TestCase(SdpParseMode.Lossless, SdpValidationSeverity.Warning, true, 2)]
    public void EmptySessionName_IsAcceptedOnlyAsACompatibilityDeviation(SdpParseMode mode, SdpValidationSeverity severity, bool success, int diagnosticCount)
    {
        ISdpParser parser = new SdpParser();
        string input = "v=0\r\n" +
            "o=- 1 1 IN IP4 127.0.0.1\r\n" +
            "s=\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(diagnosticCount));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('s'));
            Assert.That(result.Description?.HasSessionName, Is.True);
            Assert.That(result.Description?.SessionName, Is.Empty);
            if (mode == SdpParseMode.Lossless)
            {
                Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
                Assert.That(result.Description?.RawLines[2].IsMalformed, Is.True);
            }
        });
    }

    [Test]
    public void DuplicateSessionName_PreservesTheFirstOccurrence()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\ns=first\r\ns=second", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.SessionName, Is.EqualTo("first"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('s'));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void InformationCardinality_IsIndependentForSessionAndEachMediaScope()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "i=session first\r\n" +
            "i=session duplicate\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "i=audio first\r\n" +
            "i=audio duplicate\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "i=video first";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(description.Information, Is.EqualTo("session first"));
            Assert.That(description.MediaDescriptions[0].Information, Is.EqualTo("audio first"));
            Assert.That(description.MediaDescriptions[1].Information, Is.EqualTo("video first"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void EmptyInformation_IsRetainedAndDiagnosedInEachScope()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\ni=\r\nm=audio 9 RTP/AVP 0\r\ni=", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Information, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions[0].Information, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);
        });
    }

    [Test]
    public void UriReference_IsPreservedAndItsSingularCardinalityIsEnforced()
    {
        ISdpParser parser = new SdpFragmentParser();
        string first = "https://user@example.test:8443/a/b?x=1&y=%E2%9C%93#part";

        SdpParseResult result = parser.ParseText("v=0\r\nu=" + first + "\r\nu=relative/path", new SdpParseOptions());
        SdpParseResult emptyReference = parser.ParseText("v=0\r\nu=", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Uri, Is.EqualTo(first));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(emptyReference.Success, Is.True);
            Assert.That(emptyReference.Description?.Uri, Is.EqualTo(string.Empty));
            Assert.That(emptyReference.Diagnostics, Is.Empty);
        });
    }

    [TestCase("u=https://example.test/a b", 25)]
    [TestCase("u=https://example.test/%ZZ", 24)]
    [TestCase("u=https://example.test/%A", 24)]
    public void UriReference_InvalidCharactersAndEscapesAreDiagnosed(string uriLine, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\n" + uriLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Uri, Is.EqualTo(uriLine.Substring(2)));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('u'));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void UriDiagnostic_ReportsUtf8LocationForNonAsciiCharacter()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nu=path/é rest";

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes(input), new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(12));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(12));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(8));
        });
    }

    [Test]
    public void RepeatedContacts_PreserveEmptyOccurrencesAndReportTheirPaths()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\ne=first@example.test\r\ne=\r\np=+1 555 0100\r\np=";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Emails, Is.EqualTo(new string[] { "first@example.test", string.Empty }));
            Assert.That(result.Description?.Phones, Is.EqualTo(new string[] { "+1 555 0100", string.Empty }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('e'));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('p'));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Location.LineNumber, Is.EqualTo(5));
        });
    }

    [Test]
    public void SessionOnlyTextFields_AreRejectedAfterMediaBegins()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nm=audio 9 RTP/AVP 0\r\ns=late\r\nu=https://example.test\r\ne=late@example.test\r\np=+1 555 0100\r\ni=media info";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.HasSessionName, Is.False);
            Assert.That(result.Description?.Uri, Is.Null);
            Assert.That(result.Description?.Emails, Is.Empty);
            Assert.That(result.Description?.Phones, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions[0].Information, Is.EqualTo("media info"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('s'));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('u'));
            Assert.That(result.Diagnostics[2].FieldName, Is.EqualTo('e'));
            Assert.That(result.Diagnostics[3].FieldName, Is.EqualTo('p'));
            Assert.That(result.Diagnostics[3].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[3].MediaIndex, Is.Zero);
        });
    }
}
