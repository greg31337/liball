namespace sip.Tests;

public class Rfc5118ConformanceTests
{
    static string[] ValidMessages =
    [
        "ipv6-good", "port-ambiguous", "port-unambiguous",
        "via-received-param-with-delim", "via-received-param-no-delim", "ipv6-in-sdp",
        "mult-ip-in-header", "mult-ip-in-sdp", "ipv4-mapped-ipv6",
        "ipv6-bug-abnf-3-colons", "ipv6-correct-abnf-2-colons"
    ];

    [TestCaseSource(nameof(ValidMessages))]
    public void AcceptsEveryValidAndRobustnessIpv6Message(string name)
    {
        SipParseResult result = Parse(name);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        Assert.That(result.Consumed, Is.EqualTo(result.RequiredBytes));
    }

    [Test]
    public void RejectsBareIpv6RequestUri()
    {
        SipParseResult result = Parse("ipv6-bad");

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorMessage, Does.Contain("brackets"));
    }

    [Test]
    public void KeepsAmbiguousIpv6PortInsideHostAndParsesUnambiguousPort()
    {
        SipUri ambiguous = Parse("port-ambiguous").Message!.RequestUri!;
        SipUri unambiguous = Parse("port-unambiguous").Message!.RequestUri!;

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Host, Is.EqualTo("2001:db8::10:5070"));
            Assert.That(ambiguous.Port, Is.EqualTo(-1));
            Assert.That(unambiguous.Host, Is.EqualTo("2001:db8::10"));
            Assert.That(unambiguous.Port, Is.EqualTo(5070));
        });
    }

    [Test]
    public void NormalizesBothIpv6ReceivedParameterForms()
    {
        SipMessage bracketed = Parse("via-received-param-with-delim").Message!;
        SipMessage unbracketed = Parse("via-received-param-no-delim").Message!;

        Assert.That(bracketed.ViaValues[0].Received, Is.EqualTo("2001:db8::9:255"));
        Assert.That(unbracketed.ViaValues[0].Received, Is.EqualTo("2001:db8::9:255"));

        SipWriteResult formatted = new SipMessageCodec().SerializeMessage(
            bracketed, new SipWriteOptions());
        string text = System.Text.Encoding.ASCII.GetString(formatted.Bytes);
        Assert.That(formatted.Success, Is.True, formatted.ErrorMessage);
        Assert.That(text, Does.Contain(";received=2001:db8::9:255;"));
        Assert.That(text, Does.Not.Contain("received=["));
    }

    [Test]
    public void NormalizesObsoleteTripleColonIpv6Reference()
    {
        SipUri obsolete = Parse("ipv6-bug-abnf-3-colons").Message!.RequestUri!;
        SipUri corrected = Parse("ipv6-correct-abnf-2-colons").Message!.RequestUri!;
        SipUriCodec codec = new SipUriCodec();
        SipWriteResult formatted = codec.FormatUri(obsolete, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(obsolete.Host, Is.EqualTo("2001:db8::192.0.2.1"));
            Assert.That(codec.CompareUris(obsolete, corrected), Is.True);
            Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
                Is.EqualTo("sip:user@[2001:db8::192.0.2.1]"));
        });
    }

    [TestCase("ipv6-in-sdp")]
    [TestCase("mult-ip-in-sdp")]
    [TestCase("ipv4-mapped-ipv6")]
    public void ParsesIpv6AndMixedAddressSdpBodies(string name)
    {
        SipMessage message = Parse(name).Message!;
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        options.AllowCompatibleWhitespace = true;
        SipParseResult sdp = new SipSdpCodec().ParseSessionDescription(message.Body, options);

        Assert.That(sdp.Status, Is.EqualTo(SipParseStatus.Complete), sdp.ErrorMessage);
        Assert.That(sdp.SessionDescription!.Origin, Does.Contain(" IN IP"));
    }

    [Test]
    public void PreservesMixedIpv4AndIpv6ViaRoutingOrder()
    {
        SipMessage message = Parse("mult-ip-in-header").Message!;

        Assert.That(message.ViaValues.Select(value => value.Host), Is.EqualTo(new string[]
        {
            "2001:db8::9:1", "192.0.2.1", "2001:db8::9:255"
        }));
        Assert.That(message.ViaValues[0].Port, Is.EqualTo(6050));
        Assert.That(message.ViaValues[2].Received, Is.EqualTo("192.0.2.200"));
    }

    static SipParseResult Parse(string name)
    {
        SipParseOptions options = new SipParseOptions();
        options.AllowHeaderFolding = true;
        options.AllowCompatibleWhitespace = true;
        return new SipMessageCodec().ParseDatagramMessage(
            RfcTortureFixture.Read5118(name), options);
    }
}
