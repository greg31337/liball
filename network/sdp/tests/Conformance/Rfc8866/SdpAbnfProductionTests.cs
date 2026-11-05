namespace sdp.Tests.Conformance.Rfc8866;

public class SdpAbnfProductionTests
{
    [TestCase("- 0 00 IN IP4 0.0.0.0")]
    [TestCase("user/name 18446744073709551615 18446744073709551615 X-NET X-ADDR opaque:value")]
    [TestCase("usér 3724394400 3724394405 IN IP6 2001:db8::1")]
    public void OriginSubProductions_AcceptNonWhitespaceNamesDigitsTokensAndAddresses(string origin)
    {
        SdpParseResult result = ParseWithSessionField("o=" + origin, 'o');

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Origin?.RawText, Is.EqualTo(origin));
        });
    }

    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("../next?name=value#part")]
    [TestCase("https://user@example.test:8443/a%20b?q=%E2%9C%93")]
    public void UriReferenceProduction_AcceptsEmptyRelativeAndAbsoluteReferences(string uri)
    {
        SdpParseResult result = ParseWithSessionField("u=" + uri, 'u');

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Uri, Is.EqualTo(uri));
        });
    }

    [Test]
    public void TextEmailAndPhoneProductions_AcceptTheirRfcForms()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 host.example\r\n" +
            "s=Résumé planning\r\n" +
            "i=One byte or more of text\r\n" +
            "e=first@example.test (First Contact)\r\n" +
            "e=Second Contact <second@example.test>\r\n" +
            "e=third@example.test\r\n" +
            "p=+1 617 555-6011 (Main Office)\r\n" +
            "p=Support <+44 20 7946 0958>\r\n" +
            "p=1-212-555-0100\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Information, Is.EqualTo("One byte or more of text"));
            Assert.That(result.Description?.Emails, Has.Count.EqualTo(3));
            Assert.That(result.Description?.Phones, Has.Count.EqualTo(3));
        });
    }

    [TestCase("IN IP4 0.0.0.0", SdpAddressKind.Ipv4)]
    [TestCase("IN IP4 223.255.255.255", SdpAddressKind.Ipv4)]
    [TestCase("IN IP4 host.example", SdpAddressKind.HostName)]
    [TestCase("IN IP4 224.0.0.0/0", SdpAddressKind.Ipv4Multicast)]
    [TestCase("IN IP4 239.255.255.255/255/2147483647", SdpAddressKind.Ipv4Multicast)]
    [TestCase("IN IP6 1:2:3:4:5:6:7:8", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 ::1:2:3:4:5:6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1::2:3:4:5:6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1:2::3:4:5:6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1:2:3::4:5:6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1:2:3:4::5:6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1:2:3:4:5::6", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 1:2:3:4:5:6::", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 ::", SdpAddressKind.Ipv6)]
    [TestCase("IN IP6 ff15::101", SdpAddressKind.Ipv6Multicast)]
    [TestCase("IN IP6 ff15::101/2147483647", SdpAddressKind.Ipv6Multicast)]
    [TestCase("X-NET X-ADDR opaque:value/segment", SdpAddressKind.Unknown)]
    public void ConnectionAddressProductions_AcceptUnicastMulticastFqdnAndExtensions(string connection, SdpAddressKind kind)
    {
        SdpParseResult result = ParseWithSessionField("c=" + connection, 'c');

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Connection?.AddressKind, Is.EqualTo(kind));
        });
    }

    [TestCase("AS:0", "AS", 0)]
    [TestCase("CT:00128", "CT", 128)]
    [TestCase("X-BW:9223372036854775807", "X-BW", 9223372036854775807)]
    public void BandwidthProduction_AcceptsTokenAndDigitBoundaries(string bandwidth, string type, long value)
    {
        SdpParseResult result = ParseWithSessionField("b=" + bandwidth, 'b');

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Bandwidths[0].Type, Is.EqualTo(type));
            Assert.That(result.Description?.Bandwidths[0].Value, Is.EqualTo(value));
        });
    }

    [TestCase("0 0")]
    [TestCase("1000000000 9999999999")]
    [TestCase("184467440737095516160 184467440737095516161")]
    public void TimeProduction_AcceptsZeroAndUnboundedTenDigitNonzeroValues(string timing)
    {
        ISdpParser parser = new SdpParser();
        string input = Header() + "t=" + timing;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Timings[0].RawText, Is.EqualTo(timing));
        });
    }

    [TestCase("1 1 0")]
    [TestCase("7d 1h 0 25h")]
    [TestCase("3600s 60m 0s 1d")]
    [TestCase("1844674407370955161 1 0")]
    public void RepeatProduction_AcceptsPositiveIntervalsDurationsAndTypedOffsets(string repeat)
    {
        ISdpParser parser = new SdpParser();
        string input = Header() + "t=0 0\r\nr=" + repeat;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Timings[0].RepeatTimes[0].RawText, Is.EqualTo(repeat));
        });
    }

    [TestCase("1000000000 0")]
    [TestCase("2882844526 -1h 2898848070 0")]
    [TestCase("184467440737095516160 -1d")]
    public void ZoneProduction_AcceptsTimeAndSignedTypedTimePairs(string zone)
    {
        ISdpParser parser = new SdpParser();
        string input = Header() + "t=0 0\r\nr=7d 1h 0\r\nz=" + zone;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.TimeZoneAdjustments, Is.Not.Empty);
        });
    }

    [TestCase("prompt", "prompt", false)]
    [TestCase("clear:secret text", "clear", true)]
    [TestCase("base64:", "base64", true)]
    [TestCase("base64:YQ==", "base64", true)]
    [TestCase("uri:", "uri", true)]
    [TestCase("uri:https://example.test/key", "uri", true)]
    public void ObsoleteKeyProduction_AcceptsEveryDefinedMethod(string key, string method, bool hasValue)
    {
        SdpParseResult result = ParseAfterTiming("k=" + key, SdpParseMode.Interoperable);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.EncryptionKey?.Method, Is.EqualTo(method));
            Assert.That(result.Description?.EncryptionKey?.HasValue, Is.EqualTo(hasValue));
        });
    }

    [TestCase("recvonly", SdpAttributeKind.Property, "")]
    [TestCase("x-empty:", SdpAttributeKind.Value, "")]
    [TestCase("AZaz09!#$%&'*+-.^_`{|}~:opaque:value", SdpAttributeKind.Value, "opaque:value")]
    [TestCase("x-unicode:Résumé ✓", SdpAttributeKind.Value, "Résumé ✓")]
    public void AttributeProduction_AcceptsTokenNamesAndOpaqueValues(string attribute, SdpAttributeKind kind, string value)
    {
        SdpParseResult result = ParseAfterTiming("a=" + attribute, SdpParseMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Attributes[0].Kind, Is.EqualTo(kind));
            Assert.That(result.Description?.Attributes[0].Value, Is.EqualTo(value));
        });
    }

    [TestCase("audio 0 RTP/AVP 0")]
    [TestCase("video 65535/1 RTP/SAVPF 96 97")]
    [TestCase("application 9 UDP/DTLS/SCTP webrtc-datachannel")]
    [TestCase("x-media 1234 X-PROTO/One fmt.one * 42")]
    public void MediaProduction_AcceptsTokensPortsProtocolComponentsAndFormats(string media)
    {
        SdpParseResult result = ParseAfterTiming("m=" + media, SdpParseMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(result.Description?.MediaDescriptions[0].Formats, Is.Not.Empty);
        });
    }

    [TestCase("o=- x 1 IN IP4 host", 'o', SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("i=", 'i', SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("e=", 'e', SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("p=", 'p', SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("c=IN IP4", 'c', SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("b=AS:", 'b', SdpDiagnosticCodes.ParseInvalidNumber)]
    public void InvalidPreTimingProductions_AreRejected(string line, char fieldName, string diagnosticCode)
    {
        SdpParseResult result = ParseWithSessionField(line, fieldName);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(diagnosticCode));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo(fieldName));
        });
    }

    [TestCase("t=123456789 0", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("r=0 1h 0", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("r=7D 1h 0", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("z=2882844526 +1h", SdpDiagnosticCodes.ParseInvalidNumber)]
    public void InvalidTimingProductions_AreRejected(string line, string diagnosticCode)
    {
        ISdpParser parser = new SdpParser();
        string input = Header() + "t=0 0\r\nr=7d 1h 0\r\n" + line;

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[^1].Code, Is.EqualTo(diagnosticCode));
        });
    }

    [TestCase("a=", SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("m=audio 9 RTP//AVP 0", SdpDiagnosticCodes.ParseInvalidToken)]
    [TestCase("m=audio 9/0 RTP/AVP 0", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("m=audio 9 RTP/AVP", SdpDiagnosticCodes.ParseInvalidToken)]
    public void InvalidPostTimingProductions_AreRejected(string line, string diagnosticCode)
    {
        SdpParseResult result = ParseAfterTiming(line, SdpParseMode.Interoperable);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[^1].Code, Is.EqualTo(diagnosticCode));
        });
    }

    private static SdpParseResult ParseWithSessionField(string line, char fieldName)
    {
        ISdpParser parser = new SdpParser();
        string input;
        if (fieldName == 'o')
        {
            input = "v=0\r\n" + line + "\r\ns=-\r\nt=0 0";
        }
        else
        {
            input = Header() + line + "\r\nt=0 0";
        }
        return parser.ParseText(input, new SdpParseOptions());
    }

    private static SdpParseResult ParseAfterTiming(string line, SdpParseMode mode)
    {
        ISdpParser parser = new SdpParser();
        return parser.ParseText(Header() + "t=0 0\r\n" + line, new SdpParseOptions { Mode = mode });
    }

    private static string Header()
    {
        return "v=0\r\no=- 1 1 IN IP4 host.example\r\ns=-\r\n";
    }
}
