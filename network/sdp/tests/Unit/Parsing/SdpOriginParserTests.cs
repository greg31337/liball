using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpOriginParserTests
{
    [Test]
    public void Origin_PreservesEveryTokenAndUnsigned64BitIdentifierText()
    {
        ISdpParser parser = new SdpFragmentParser();
        string originText = "- 18446744073709551615 00018446744073709551615 IN IP4 192.0.2.1";
        string input = "v=0\r\no=" + originText + "\r\n";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpOrigin origin = result.Description?.Origin ?? new SdpOrigin();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(origin.Username, Is.EqualTo("-"));
            Assert.That(origin.SessionId, Is.EqualTo("18446744073709551615"));
            Assert.That(origin.SessionVersion, Is.EqualTo("00018446744073709551615"));
            Assert.That(origin.NetworkType, Is.EqualTo("IN"));
            Assert.That(origin.AddressType, Is.EqualTo("IP4"));
            Assert.That(origin.UnicastAddress, Is.EqualTo("192.0.2.1"));
            Assert.That(origin.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(origin.RawText, Is.EqualTo(originText));
            Assert.That(origin.Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(origin.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(origin.Location.LineNumber, Is.EqualTo(2));
            Assert.That(origin.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void Utf8Origin_PreservesInternationalAndExtensionValuesWithExactLocation()
    {
        ISdpParser parser = new SdpFragmentParser();
        string originText = "üser 0001 0002 X-NET X-ADDR opaque:value";
        byte[] input = Encoding.UTF8.GetBytes("\uFEFFv=0\r\no=" + originText);

        SdpParseResult result = parser.ParseUtf8Bytes(input.AsSpan(), new SdpParseOptions { AllowByteOrderMark = true });
        SdpOrigin origin = result.Description?.Origin ?? new SdpOrigin();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(origin.Username, Is.EqualTo("üser"));
            Assert.That(origin.SessionId, Is.EqualTo("0001"));
            Assert.That(origin.SessionVersion, Is.EqualTo("0002"));
            Assert.That(origin.NetworkType, Is.EqualTo("X-NET"));
            Assert.That(origin.AddressType, Is.EqualTo("X-ADDR"));
            Assert.That(origin.UnicastAddress, Is.EqualTo("opaque:value"));
            Assert.That(origin.AddressKind, Is.EqualTo(SdpAddressKind.Unknown));
            Assert.That(origin.RawText, Is.EqualTo(originText));
            Assert.That(origin.Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(origin.Location.ByteOffset, Is.EqualTo(3 + Encoding.UTF8.GetByteCount("v=0\r\n")));
            Assert.That(origin.Location.LineNumber, Is.EqualTo(2));
        });
    }

    [TestCase("IP4", "192.0.2.10", SdpAddressKind.Ipv4)]
    [TestCase("IP4", "origin.example", SdpAddressKind.HostName)]
    [TestCase("IP6", "2001:db8::1", SdpAddressKind.Ipv6)]
    [TestCase("IP6", "origin.example", SdpAddressKind.HostName)]
    [TestCase("X-ADDR", "origin.example", SdpAddressKind.Unknown)]
    public void Origin_ClassifiesRecognizedAddressFormsWithoutChangingText(string addressType, string address, SdpAddressKind expectedKind)
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\no=- 1 1 IN " + addressType + " " + address;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.Origin?.AddressType, Is.EqualTo(addressType));
            Assert.That(result.Description?.Origin?.UnicastAddress, Is.EqualTo(address));
            Assert.That(result.Description?.Origin?.AddressKind, Is.EqualTo(expectedKind));
        });
    }

    [Test]
    public void InvalidIdentifiers_AreRetainedAndDiagnosedAtUtf8TokenOffsets()
    {
        ISdpParser parser = new SdpFragmentParser();
        string text = "v=0\r\no=üser x y IN IP4 host";
        byte[] input = Encoding.UTF8.GetBytes(text);

        SdpParseResult result = parser.ParseUtf8Bytes(input.AsSpan(), new SdpParseOptions());
        SdpOrigin origin = result.Description?.Origin ?? new SdpOrigin();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(origin.SessionId, Is.EqualTo("x"));
            Assert.That(origin.SessionVersion, Is.EqualTo("y"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(12));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(13));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(8));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Diagnostics[1].Location.CharacterOffset, Is.EqualTo(14));
            Assert.That(result.Diagnostics[1].Location.ByteOffset, Is.EqualTo(15));
            Assert.That(result.Diagnostics[1].Location.ColumnNumber, Is.EqualTo(10));
        });
    }

    [Test]
    public void IdentifierBeyondUnsigned64BitRange_IsReportedAsOverflow()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\no=- 18446744073709551616 0 IN IP4 192.0.2.1", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin?.SessionId, Is.EqualTo("18446744073709551616"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('o'));
        });
    }

    [TestCase("o=")]
    [TestCase("o=- 1 1 IN IP4")]
    [TestCase("o=- 1 1 IN IP4 host extra")]
    [TestCase("o= - 1 1 IN IP4 host")]
    [TestCase("o=-  1 1 IN IP4 host")]
    [TestCase("o=- 1 1 IN IP4 host ")]
    public void Origin_RequiresExactlySixSingleSpaceSeparatedTokens(string originLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\n" + originLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('o'));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void Origin_ValidatesNetworkAndAddressTokenGrammarIndependently()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\no=- 1 1 I/N IP@ host", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin?.NetworkType, Is.EqualTo("I/N"));
            Assert.That(result.Description?.Origin?.AddressType, Is.EqualTo("IP@"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(10));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[1].Location.ColumnNumber, Is.EqualTo(15));
        });
    }

    [Test]
    public void OriginTokenLimit_StopsAtTheOverlongToken()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseText("v=0\r\no=user 12345 1 IN IP4 host\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin, Is.Null);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(8));
        });
    }

    [Test]
    public void DuplicateOrigin_IsDiagnosedWithoutReplacingTheFirstOrigin()
    {
        ISdpParser parser = new SdpFragmentParser();
        string first = "o=first 1 1 IN IP4 first.example";
        string second = "o=second 2 2 IN IP6 2001:db8::1";

        SdpParseResult result = parser.ParseText("v=0\r\n" + first + "\r\n" + second, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin?.Username, Is.EqualTo("first"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void OriginInMediaScope_IsRejectedWithoutReplacingTheSessionOrigin()
    {
        ISdpParser parser = new SdpFragmentParser();
        string first = "o=first 1 1 IN IP4 first.example";
        string mediaOrigin = "o=second 2 2 IN IP6 2001:db8::1";

        SdpParseResult result = parser.ParseText("v=0\r\n" + first + "\r\nm=audio 9 RTP/AVP 0\r\n" + mediaOrigin, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Origin?.Username, Is.EqualTo("first"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldInWrongScope));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(4));
        });
    }
}
