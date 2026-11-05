using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpLosslessRecoveryTests
{
    [Test]
    public void LosslessMode_PreservesUnknownFieldsInSessionAndMediaScopes()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "x=session-extension\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "y=media-extension";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpMediaDescription media = description.MediaDescriptions.Count == 1 ? description.MediaDescriptions[0] : new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseUnknownField));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseUnknownField));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);

            Assert.That(description.RawLines, Has.Count.EqualTo(5));
            Assert.That(description.RawLines[3].FieldName, Is.EqualTo('x'));
            Assert.That(description.RawLines[3].Text, Is.EqualTo("x=session-extension"));
            Assert.That(description.RawLines[3].Value, Is.EqualTo("session-extension"));
            Assert.That(description.RawLines[3].IsMalformed, Is.False);
            Assert.That(description.RawLines[3].LineEnding, Is.EqualTo(SdpLineEnding.CrLf));

            Assert.That(media.RawLines, Has.Count.EqualTo(2));
            Assert.That(media.RawLines[1].FieldName, Is.EqualTo('y'));
            Assert.That(media.RawLines[1].Value, Is.EqualTo("media-extension"));
            Assert.That(media.RawLines[1].IsMalformed, Is.False);
            Assert.That(media.RawLines[1].LineEnding, Is.EqualTo(SdpLineEnding.None));
        });
    }

    [Test]
    public void LosslessMode_RetainsMalformedPrefixAndResumesAtFollowingFields()
    {
        ISdpParser parser = new SdpParser();
        string prefix = "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\n";
        string input = prefix + "BROKEN\r\nt=0 0\r\nm=audio 9 RTP/AVP 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpRawLine malformed = description.RawLines.Count > 3 ? description.RawLines[3] : new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[1].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('\0'));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(-1));
            Assert.That(result.Diagnostics[1].Location.CharacterOffset, Is.EqualTo(prefix.Length));
            Assert.That(result.Diagnostics[1].Location.LineNumber, Is.EqualTo(4));

            Assert.That(description.RawLines, Has.Count.EqualTo(5));
            Assert.That(malformed.Text, Is.EqualTo("BROKEN"));
            Assert.That(malformed.FieldName, Is.EqualTo('\0'));
            Assert.That(malformed.Value, Is.Empty);
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(description.Timings, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LosslessMode_MarksMalformedKnownCoreValueAndRetainsItsProjection()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "b=AS:not-a-number\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 RTP/AVP 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpRawLine bandwidthLine = description.RawLines.Count > 3 ? description.RawLines[3] : new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('b'));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.Zero);

            Assert.That(description.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(description.Bandwidths[0].RawValue, Is.EqualTo("not-a-number"));
            Assert.That(bandwidthLine.FieldName, Is.EqualTo('b'));
            Assert.That(bandwidthLine.Value, Is.EqualTo("AS:not-a-number"));
            Assert.That(bandwidthLine.IsMalformed, Is.True);
            Assert.That(description.Timings, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LosslessMode_RetainsMalformedMediaBoundaryInMediaScope()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=audio 9\r\n" +
            "a=sendrecv";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpMediaDescription media = result.Description?.MediaDescriptions.Count == 1 ? result.Description.MediaDescriptions[0] : new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[1].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('m'));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.Zero);

            Assert.That(media.RawLines, Has.Count.EqualTo(2));
            Assert.That(media.RawLines[0].Text, Is.EqualTo("m=audio 9"));
            Assert.That(media.RawLines[0].IsMalformed, Is.True);
            Assert.That(media.RawLines[1].FieldName, Is.EqualTo('a'));
            Assert.That(media.Attributes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PreserveMalformedLinesOutsideLosslessMode_DoesNotReportLosslessRecovery()
    {
        ISdpParser parser = new SdpParser();
        string input = "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\nBROKEN\r\nt=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { PreserveMalformedLines = true });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMalformedLine));
            Assert.That(description.RawLines, Has.Count.EqualTo(1));
            Assert.That(description.RawLines[0].Text, Is.EqualTo("BROKEN"));
            Assert.That(description.RawLines[0].IsMalformed, Is.True);
            Assert.That(description.Timings, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LosslessUtf8Recovery_PreservesInternationalTextAndExactOffsets()
    {
        ISdpParser parser = new SdpParser();
        string prefix = "v=0\r\no=- 1 1 IN IP4 host\r\ns=-\r\n";
        string input = prefix + "é broken\r\nt=0 0";
        byte[] bytes = Encoding.UTF8.GetBytes(input);

        SdpParseResult result = parser.ParseUtf8Bytes(bytes.AsSpan(), new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();
        SdpRawLine malformed = description.RawLines.Count > 3 ? description.RawLines[3] : new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[1].Location.CharacterOffset, Is.EqualTo(prefix.Length));
            Assert.That(result.Diagnostics[1].Location.ByteOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(prefix)));
            Assert.That(malformed.Text, Is.EqualTo("é broken"));
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(malformed.Location.CharacterOffset, Is.EqualTo(prefix.Length));
            Assert.That(malformed.Location.ByteOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(prefix)));
            Assert.That(description.Timings, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LosslessMode_MarksMalformedAttributeAndContinues()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "a=bad name:value\r\n" +
            "m=audio 9 RTP/AVP 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(result.Diagnostics[1].FieldName, Is.EqualTo('a'));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.Zero);
            Assert.That(description.Attributes, Has.Count.EqualTo(1));
            Assert.That(description.RawLines[4].IsMalformed, Is.True);
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(1));
        });
    }
}
