namespace sip.Tests;

public class SipViaCodecTests
{
    [Test]
    public void ParsesTypedViaWithKnownAndUnknownParameters()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "Via: sip / 2.0 / udp Client.Example.COM:5060;Branch=z9hG4bK-1;received=192.0.2.1"
            + ";rport=5080;maddr=Proxy.Example.com;ttl=005;alias;Foo=\"bar baz\"\r\n";

        SipHeaderParseResult result = Parse(codec, text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ViaValues, Has.Length.EqualTo(1));
        SipViaValue via = result.ViaValues[0];
        Assert.Multiple(() =>
        {
            Assert.That(via.ProtocolName, Is.EqualTo("sip"));
            Assert.That(via.ProtocolVersion, Is.EqualTo("2.0"));
            Assert.That(via.Transport, Is.EqualTo("udp"));
            Assert.That(via.Host, Is.EqualTo("Client.Example.COM"));
            Assert.That(via.Port, Is.EqualTo(5060));
            Assert.That(via.Branch, Is.EqualTo("z9hG4bK-1"));
            Assert.That(via.Received, Is.EqualTo("192.0.2.1"));
            Assert.That(via.HasRPort, Is.True);
            Assert.That(via.RPort, Is.EqualTo(5080));
            Assert.That(via.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "Branch", "received", "rport", "maddr", "ttl", "alias", "Foo" }));
            Assert.That(via.Parameters[4].Value, Is.EqualTo("005"));
            Assert.That(via.Parameters[5].HasValue, Is.False);
            Assert.That(via.Parameters[6].Value, Is.EqualTo("bar baz"));
        });
    }

    [Test]
    public void ParsesCompactMultiValueViaWithIpv6Forms()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        string text = "v: SIP/2.0/TCP first.example.com;branch=z9hG4bK-a, "
            + "SIP/2.0/TLS [2001:DB8::1]:5071;received=2001:db8::2;maddr=[FF02::1];rport\r\n";

        SipHeaderParseResult result = Parse(codec, text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ViaValues, Has.Length.EqualTo(2));
        SipViaValue second = result.ViaValues[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.Transport, Is.EqualTo("TLS"));
            Assert.That(second.Host, Is.EqualTo("2001:DB8::1"));
            Assert.That(second.Port, Is.EqualTo(5071));
            Assert.That(second.Received, Is.EqualTo("2001:db8::2"));
            Assert.That(second.HasRPort, Is.True);
            Assert.That(second.RPort, Is.EqualTo(-1));
            Assert.That(second.Parameters.Single(parameter =>
                parameter.Name!.Equals("maddr", System.StringComparison.OrdinalIgnoreCase)).Value,
                Is.EqualTo("[FF02::1]"));
        });
    }

    [TestCase("Via: SIP/2.0 client.example.com")]
    [TestCase("Via: SIP/2.0/UDPclient.example.com")]
    [TestCase("Via: SIP/2.0/UDP client.example.com:abc")]
    [TestCase("Via: SIP/2.0/UDP bad_host")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;branch=\"z9hG4bK-a\"")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;received=host.example.com")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;rport=")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;rport=65536")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;rport=\"5080\"")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;maddr=bad_host")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;ttl=256")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;ttl=0005")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;ttl=\"5\"")]
    [TestCase("Via: SIP/2.0/TCP client.example.com;alias=on")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;branch=one;Branch=two")]
    [TestCase("Via: SIP/2.0/UDP client.example.com;unknown=/")]
    public void RejectsMalformedViaValues(string text)
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult result = Parse(codec, text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ViaValues, Is.Empty);
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    [Test]
    public void ViaParsingHonorsListAndParameterLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions listOptions = new SipParseOptions();
        listOptions.MaxListValueCount = 1;
        SipParseOptions parameterOptions = new SipParseOptions();
        parameterOptions.MaxParameterCount = 1;
        SipParseOptions invalidOptions = new SipParseOptions();
        invalidOptions.MaxListValueCount = -1;

        SipHeaderParseResult tooManyValues = Parse(codec,
            "Via: SIP/2.0/UDP one.example.com, SIP/2.0/TCP two.example.com", listOptions);
        SipHeaderParseResult tooManyParameters = Parse(codec,
            "Via: SIP/2.0/UDP one.example.com;branch=one;received=192.0.2.1", parameterOptions);

        Assert.That(tooManyValues.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyValues.ErrorMessage, Does.Contain("item count"));
        Assert.That(tooManyParameters.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyParameters.ErrorMessage, Does.Contain("parameter count"));
        Assert.That(Parse(codec, "Via: SIP/2.0/UDP one.example.com", invalidOptions).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void CanonicalViaFormattingRoundTripsTypedValues()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Via: sip/2.0/udp Client.Example.COM:5060;Branch=z9hG4bK-1;received=2001:DB8::2"
                + ";rport=05080;maddr=[FF02::1];ttl=005;alias;Foo=\"bar baz\"", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());
        string text = System.Text.Encoding.UTF8.GetString(formatted.Bytes);
        SipHeaderParseResult reparsed = Parse(codec, text, new SipParseOptions());

        Assert.That(formatted.Success, Is.True);
        Assert.That(text, Is.EqualTo("Via: SIP/2.0/UDP client.example.com:5060;branch=z9hG4bK-1"
            + ";received=2001:db8::2;rport=5080;maddr=[ff02::1];ttl=5;alias;foo=\"bar baz\"\r\n"));
        Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(reparsed.ViaValues[0].RPort, Is.EqualTo(5080));
        Assert.That(reparsed.ViaValues[0].Parameters.Last().Value, Is.EqualTo("bar baz"));
    }

    [Test]
    public void TypedFieldsMissingFromParameterArrayAreFormatted()
    {
        SipViaValue via = new SipViaValue();
        via.ProtocolName = "SIP";
        via.ProtocolVersion = "2.0";
        via.Transport = "TCP";
        via.Host = "client.example.com";
        via.Branch = "z9hG4bK-manual";
        via.Received = "192.0.2.10";
        via.HasRPort = true;
        SipUriParameter staleBranch = new SipUriParameter();
        staleBranch.Name = "Branch";
        staleBranch.Value = "z9hG4bK-old";
        staleBranch.HasValue = true;
        via.Parameters = new SipUriParameter[] { staleBranch };
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult result = SipViaCodec.FormatViaHeaderValues(new SipViaValue[] { via }, options);

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
            "v: SIP/2.0/TCP client.example.com;branch=z9hG4bK-manual;received=192.0.2.10;rport\r\n"));
    }

    [Test]
    public void ViaFormattingRejectsInvalidModelsAndHonorsOutputLimit()
    {
        SipViaValue invalidHost = CreateBasicVia();
        invalidHost.Host = "bad_host";
        SipViaValue inconsistentRPort = CreateBasicVia();
        inconsistentRPort.RPort = 5080;
        SipViaValue valid = CreateBasicVia();
        SipWriteOptions limited = new SipWriteOptions();
        limited.MaxHeaderLineBytes = 10;

        Assert.That(SipViaCodec.FormatViaHeaderValues(new SipViaValue[] { invalidHost },
            new SipWriteOptions()).Success, Is.False);
        Assert.That(SipViaCodec.FormatViaHeaderValues(new SipViaValue[] { inconsistentRPort },
            new SipWriteOptions()).Success, Is.False);
        SipWriteResult tooLong = SipViaCodec.FormatViaHeaderValues(new SipViaValue[] { valid }, limited);
        Assert.That(tooLong.Success, Is.False);
        Assert.That(tooLong.RequiredBytes, Is.GreaterThan(10));
    }

    [Test]
    public void NonViaHeadersDoNotProduceTypedViaValues()
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult result = Parse(codec, "X-Via: SIP/2.0/UDP client.example.com", new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ViaValues, Is.Empty);
    }

    [Test]
    public void ViaParameterOriginalTextHonorsPreservationOption()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions options = new SipParseOptions();
        options.PreserveOriginal = false;

        SipHeaderParseResult result = Parse(codec,
            "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-a;foo=bar", options);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Header!.OriginalBytes, Is.Empty);
        Assert.That(result.ViaValues[0].Parameters.Select(parameter => parameter.OriginalText),
            Is.All.Null);
    }

    static SipHeaderParseResult Parse(SipHeaderCodec codec, string text, SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }

    static SipViaValue CreateBasicVia()
    {
        SipViaValue via = new SipViaValue();
        via.ProtocolName = "SIP";
        via.ProtocolVersion = "2.0";
        via.Transport = "UDP";
        via.Host = "client.example.com";
        return via;
    }
}
