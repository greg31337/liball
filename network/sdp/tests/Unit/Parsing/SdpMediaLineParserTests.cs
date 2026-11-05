using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpMediaLineParserTests
{
    [Test]
    public void MediaLine_ParsesPortCountProtocolFormatsAndFollowingFields()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };
        string input =
            "v=0\r\n" +
            "m=audio 49170/2 RTP/AVP 0 96\r\n" +
            "i=Primary audio\r\n" +
            "c=IN IP4 192.0.2.10\r\n" +
            "b=AS:128\r\n" +
            "k=prompt\r\n" +
            "a=rtpmap:96 opus/48000/2";

        SdpParseResult result = parser.ParseText(input, options);
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(media.MediaType, Is.EqualTo("audio"));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Audio));
            Assert.That(media.Port, Is.EqualTo(49170));
            Assert.That(media.HasPortCount, Is.True);
            Assert.That(media.PortCount, Is.EqualTo(2));
            Assert.That(media.Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(media.Formats, Is.EqualTo(new string[] { "0", "96" }));
            Assert.That(media.Information, Is.EqualTo("Primary audio"));
            Assert.That(media.Connections, Has.Count.EqualTo(1));
            Assert.That(media.Connections[0].Address, Is.EqualTo("192.0.2.10"));
            Assert.That(media.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(media.Bandwidths[0].Value, Is.EqualTo(128));
            Assert.That(media.EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(media.Attributes, Has.Count.EqualTo(1));
            Assert.That(media.Attributes[0].RawText, Is.EqualTo("rtpmap:96 opus/48000/2"));
            Assert.That(media.Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(media.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(media.Location.LineNumber, Is.EqualTo(2));
            Assert.That(media.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void MultipleMediaLines_CreateIndependentSectionsAndPreserveZeroPort()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "m=audio 0 RTP/AVP 0\r\n" +
            "a=inactive\r\n" +
            "m=Video 65535 RTP/SAVPF 96 97\r\n" +
            "a=sendrecv";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(description.MediaDescriptions[0].HasPortCount, Is.False);
            Assert.That(description.MediaDescriptions[0].PortCount, Is.EqualTo(1));
            Assert.That(description.MediaDescriptions[0].Attributes[0].Name, Is.EqualTo("inactive"));
            Assert.That(description.MediaDescriptions[1].MediaType, Is.EqualTo("Video"));
            Assert.That(description.MediaDescriptions[1].MediaKind, Is.EqualTo(SdpMediaKind.Video));
            Assert.That(description.MediaDescriptions[1].Port, Is.EqualTo(65535));
            Assert.That(description.MediaDescriptions[1].Formats, Is.EqualTo(new string[] { "96", "97" }));
            Assert.That(description.MediaDescriptions[1].Attributes[0].Name, Is.EqualTo("sendrecv"));
        });
    }

    [TestCase("audio", SdpMediaKind.Audio)]
    [TestCase("AUDIO", SdpMediaKind.Audio)]
    [TestCase("video", SdpMediaKind.Video)]
    [TestCase("text", SdpMediaKind.Text)]
    [TestCase("application", SdpMediaKind.Application)]
    [TestCase("message", SdpMediaKind.Message)]
    [TestCase("image", SdpMediaKind.Unknown)]
    [TestCase("x-extension", SdpMediaKind.Unknown)]
    public void MediaKind_ClassifiesCommonNamesWithoutChangingSourceSpelling(string mediaType, SdpMediaKind expectedKind)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=" + mediaType + " 9 X 0", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.MediaDescriptions[0].MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Description?.MediaDescriptions[0].MediaKind, Is.EqualTo(expectedKind));
        });
    }

    [Test]
    public void ExtensionProtocolAndFormats_ArePreservedWithOrderAndDuplicates()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=x-custom 1234 X-PROTO/One fmt.one * 42 42", new SdpParseOptions());
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(media.MediaType, Is.EqualTo("x-custom"));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(media.Protocol, Is.EqualTo("X-PROTO/One"));
            Assert.That(media.Formats, Is.EqualTo(new string[] { "fmt.one", "*", "42", "42" }));
        });
    }

    [TestCase("00000", false, 1, 0)]
    [TestCase("65535", false, 1, 65535)]
    [TestCase("49170/1", true, 1, 49170)]
    [TestCase("9/2147483647", true, 2147483647, 9)]
    public void PortAndOptionalCount_AcceptSupportedBoundaries(string portText, bool expectedHasCount, int expectedCount, int expectedPort)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=audio " + portText + " RTP/AVP 0", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.MediaDescriptions[0].Port, Is.EqualTo(expectedPort));
            Assert.That(result.Description?.MediaDescriptions[0].HasPortCount, Is.EqualTo(expectedHasCount));
            Assert.That(result.Description?.MediaDescriptions[0].PortCount, Is.EqualTo(expectedCount));
        });
    }

    [TestCase("m=")]
    [TestCase("m=audio")]
    [TestCase("m=audio 9")]
    [TestCase("m=audio 9 RTP/AVP")]
    [TestCase("m= audio 9 RTP/AVP 0")]
    [TestCase("m=audio  9 RTP/AVP 0")]
    [TestCase("m=audio 9  RTP/AVP 0")]
    [TestCase("m=audio 9 RTP/AVP  0")]
    [TestCase("m=audio 9 RTP/AVP 0 ")]
    [TestCase("m=audio\u00A09 RTP/AVP 0")]
    public void MediaLine_RequiresFourOrMoreSingleSpaceSeparatedTokens(string mediaLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\n" + mediaLine + "\r\na=following", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].MediaType, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes[0].Name, Is.EqualTo("following"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('m'));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
        });
    }

    [TestCase("-1", SdpDiagnosticCodes.ParseInvalidNumber, false)]
    [TestCase("+1", SdpDiagnosticCodes.ParseInvalidNumber, false)]
    [TestCase("65536", SdpDiagnosticCodes.ParseNumberOverflow, false)]
    [TestCase("1/0", SdpDiagnosticCodes.ParseInvalidNumber, true)]
    [TestCase("1/01", SdpDiagnosticCodes.ParseInvalidNumber, true)]
    [TestCase("1/2147483648", SdpDiagnosticCodes.ParseNumberOverflow, true)]
    [TestCase("1/2/3", SdpDiagnosticCodes.ParseInvalidToken, true)]
    [TestCase("1/", SdpDiagnosticCodes.ParseInvalidNumber, true)]
    public void InvalidPortOrCount_IsDiagnosedWhileOtherMediaTokensAreRetained(string portText, string expectedCode, bool expectedHasCount)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=audio " + portText + " RTP/AVP 0 96", new SdpParseOptions());
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(media.MediaType, Is.EqualTo("audio"));
            Assert.That(media.HasPortCount, Is.EqualTo(expectedHasCount));
            Assert.That(media.Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(media.Formats, Is.EqualTo(new string[] { "0", "96" }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
        });
    }

    [Test]
    public void InvalidMediaProtocolAndFormatTokens_AreRetainedAndDiagnosedIndependently()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nm=bad/name 9 RTP//AVP fmt/one", new SdpParseOptions());
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(media.MediaType, Is.EqualTo("bad/name"));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(media.Port, Is.EqualTo(9));
            Assert.That(media.Protocol, Is.EqualTo("RTP//AVP"));
            Assert.That(media.Formats, Is.EqualTo(new string[] { "fmt/one" }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
        });
    }

    [Test]
    public void MalformedMediaLine_RemainsTheBoundaryForFollowingMediaFields()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { Mode = SdpParseMode.Interoperable };
        string input =
            "v=0\r\n" +
            "m=broken\r\n" +
            "i=Recovered section\r\n" +
            "c=IN IP4 192.0.2.20\r\n" +
            "b=AS:64\r\n" +
            "k=prompt\r\n" +
            "a=sendrecv";

        SdpParseResult result = parser.ParseText(input, options);
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(media.Information, Is.EqualTo("Recovered section"));
            Assert.That(media.Connections, Has.Count.EqualTo(1));
            Assert.That(media.Bandwidths, Has.Count.EqualTo(1));
            Assert.That(media.EncryptionKey?.Method, Is.EqualTo("prompt"));
            Assert.That(media.Attributes, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('m'));
        });
    }

    [TestCase("m=abcdef 9 X 0", 5, 3)]
    [TestCase("m=audio 123456 X 0", 5, 9)]
    [TestCase("m=audio 9 ABCDEF 0", 5, 11)]
    [TestCase("m=audio 9 X abcdef", 5, 13)]
    public void MediaTokenLimit_StopsBeforeFollowingLines(string mediaLine, int maximumTokenLength, int expectedColumn)
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = maximumTokenLength };

        SdpParseResult result = parser.ParseText("v=0\r\n" + mediaLine + "\r\na=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].MediaType, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions[0].Attributes, Is.Empty);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(expectedColumn));
        });
    }

    [Test]
    public void Utf8MediaTokenLimit_IsMeasuredInBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes("v=0\r\nm=ééé 9 X 0"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions[0].MediaType, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void MediaCountLimit_PreservesAlreadyParsedSectionsAndReportsTheNextIndex()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumMediaCount = 1 };

        SdpParseResult result = parser.ParseText("v=0\r\nm=audio 9 RTP/AVP 0\r\nm=video 9 RTP/AVP 96\r\na=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].MediaKind, Is.EqualTo(SdpMediaKind.Audio));
            Assert.That(result.LinesConsumed, Is.EqualTo(3));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseMediaCountLimitExceeded));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void Utf8MediaLocation_AccountsForEarlierInternationalText()
    {
        ISdpParser parser = new SdpFragmentParser();
        string prefix = "v=0\r\ns=Résumé\r\n";

        SdpParseResult result = parser.ParseUtf8Bytes(Encoding.UTF8.GetBytes(prefix + "m=audio 9 RTP/AVP 0"), new SdpParseOptions());
        SdpMediaDescription media = result.Description?.MediaDescriptions[0] ?? new SdpMediaDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(media.Location.CharacterOffset, Is.EqualTo(prefix.Length));
            Assert.That(media.Location.ByteOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(prefix)));
            Assert.That(media.Location.LineNumber, Is.EqualTo(3));
        });
    }
}
