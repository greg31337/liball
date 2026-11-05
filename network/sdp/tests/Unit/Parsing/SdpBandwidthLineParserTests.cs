using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpBandwidthLineParserTests
{
    [Test]
    public void SessionBandwidths_PreserveRegisteredAndExtensionValuesInOrder()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "b=CT:0\r\n" +
            "b=AS:128\r\n" +
            "b=RS:64\r\n" +
            "b=RR:32\r\n" +
            "b=TIAS:9223372036854775807\r\n" +
            "b=X-CUSTOM:00042";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        System.Collections.Generic.List<SdpBandwidth> bandwidths = result.Description?.Bandwidths ?? new System.Collections.Generic.List<SdpBandwidth>();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(bandwidths, Has.Count.EqualTo(6));
            Assert.That(bandwidths[0].Type, Is.EqualTo(SdpBandwidthTypes.ConferenceTotal));
            Assert.That(bandwidths[0].Value, Is.Zero);
            Assert.That(bandwidths[0].RawValue, Is.EqualTo("0"));
            Assert.That(bandwidths[1].Type, Is.EqualTo(SdpBandwidthTypes.ApplicationSpecific));
            Assert.That(bandwidths[1].Value, Is.EqualTo(128));
            Assert.That(bandwidths[2].Type, Is.EqualTo(SdpBandwidthTypes.RtcpSender));
            Assert.That(bandwidths[3].Type, Is.EqualTo(SdpBandwidthTypes.RtcpReceiver));
            Assert.That(bandwidths[4].Type, Is.EqualTo(SdpBandwidthTypes.TransportIndependentApplicationSpecific));
            Assert.That(bandwidths[4].Value, Is.EqualTo(long.MaxValue));
            Assert.That(bandwidths[5].Type, Is.EqualTo("X-CUSTOM"));
            Assert.That(bandwidths[5].Value, Is.EqualTo(42));
            Assert.That(bandwidths[5].RawValue, Is.EqualTo("00042"));
            Assert.That(bandwidths[5].RawText, Is.EqualTo("X-CUSTOM:00042"));
            Assert.That(bandwidths[0].Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(bandwidths[0].Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(bandwidths[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(bandwidths[0].Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void MediaBandwidths_AreRepeatedAndResetOccurrencePathsPerMedia()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "b=CT:1000\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "b=AS:128\r\n" +
            "b=TIAS:64000\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "b=AS:2048";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(description.Bandwidths[0].Value, Is.EqualTo(1000));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Bandwidths, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Bandwidths[0].Value, Is.EqualTo(128));
            Assert.That(description.MediaDescriptions[0].Bandwidths[1].Value, Is.EqualTo(64000));
            Assert.That(description.MediaDescriptions[1].Bandwidths, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[1].Bandwidths[0].Value, Is.EqualTo(2048));
        });
    }

    [TestCase("")]
    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1x")]
    public void InvalidBandwidthNumber_IsRetainedWithoutAConvertedValue(string rawValue)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=AS:" + rawValue, new SdpParseOptions());
        SdpBandwidth bandwidth = result.Description?.Bandwidths.Count == 1 ? result.Description.Bandwidths[0] : new SdpBandwidth();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(bandwidth.Type, Is.EqualTo("AS"));
            Assert.That(bandwidth.Value, Is.Zero);
            Assert.That(bandwidth.RawValue, Is.EqualTo(rawValue));
            Assert.That(bandwidth.RawText, Is.EqualTo("AS:" + rawValue));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('b'));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(6));
        });
    }

    [TestCase("9223372036854775808")]
    [TestCase("18446744073709551615")]
    [TestCase("18446744073709551616")]
    public void BandwidthBeyondSigned64BitRange_IsRetainedAndReportedAsOverflow(string rawValue)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=AS:" + rawValue, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths[0].RawValue, Is.EqualTo(rawValue));
            Assert.That(result.Description?.Bandwidths[0].Value, Is.Zero);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(6));
        });
    }

    [TestCase(":1", "", 3)]
    [TestCase("A/S:1", "A/S", 4)]
    [TestCase("TYPE@:1", "TYPE@", 7)]
    public void InvalidBandwidthType_IsRetainedAndDiagnosedAtTheInvalidCharacter(string bandwidthText, string expectedType, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=" + bandwidthText, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Bandwidths[0].Type, Is.EqualTo(expectedType));
            Assert.That(result.Description?.Bandwidths[0].Value, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void MissingColon_IsRejectedWithoutCreatingAProjectedBandwidth()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=AS128", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(8));
        });
    }

    [Test]
    public void AdditionalColon_IsRetainedAsPartOfTheInvalidRawValue()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=AS:1:2", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths[0].RawValue, Is.EqualTo("1:2"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
        });
    }

    [Test]
    public void MalformedBandwidthStillAdvancesFollowingOccurrencePaths()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nb=missing-colon\r\nb=AS:x", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Description?.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Bandwidths[0].RawValue, Is.EqualTo("x"));
        });
    }

    [TestCase("b=ABCDE:1", 3)]
    [TestCase("b=AS:12345", 6)]
    [TestCase("b=ABCDE", 3)]
    public void BandwidthComponentLimit_StopsBeforeFollowingLines(string bandwidthLine, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseText("v=0\r\n" + bandwidthLine + "\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths, Is.Empty);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void Utf8BandwidthComponentLimit_IsMeasuredInBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes("v=0\r\nb=ééé:1"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Bandwidths, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void MediaBandwidthDiagnosticCarriesMediaScopeAndOccurrence()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=audio 9 RTP/AVP 0\r\nb=AS:10\r\nb=TIAS:x", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions[0].Bandwidths, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(4));
        });
    }
}
