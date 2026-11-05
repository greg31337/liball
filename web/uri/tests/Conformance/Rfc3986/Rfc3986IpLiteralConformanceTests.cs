namespace uri.Tests;

public class Rfc3986IpLiteralConformanceTests
{
    [TestCase("0.0.0.0", "00000000")]
    [TestCase("9.10.99.100", "090A6364")]
    [TestCase("199.200.249.250", "C7C8F9FA")]
    [TestCase("255.255.255.255", "FFFFFFFF")]
    public void Ipv4DecOctetAlternativesProduceExactAddressBytes(
        string host,
        string expectedHex)
    {
        UriReference reference = ParseReference("x://" + host + "/");

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv4), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(
                reference.Authority.AddressBytes,
                Is.EqualTo(Convert.FromHexString(expectedHex)),
                host);
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(99)]
    [TestCase(100)]
    [TestCase(199)]
    [TestCase(200)]
    [TestCase(249)]
    [TestCase(250)]
    [TestCase(255)]
    public void Ipv4BoundaryValuesAreAcceptedInEveryOctet(int value)
    {
        string octet = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string[] hosts = new string[]
        {
            octet + ".17.34.51",
            "17." + octet + ".34.51",
            "17.34." + octet + ".51",
            "17.34.51." + octet
        };

        int index = 0;
        while (index < hosts.Length)
        {
            UriReference reference = ParseReference("x://" + hosts[index] + "/");
            Assert.That(
                reference.Authority.HostKind,
                Is.EqualTo(UriHostKind.Ipv4),
                hosts[index]);
            index = index + 1;
        }
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1.2")]
    [TestCase("1.2.3")]
    [TestCase("1.2.3.")]
    [TestCase(".1.2.3")]
    [TestCase("1..2.3")]
    [TestCase("1.2.3.4.5")]
    [TestCase("01.2.3.4")]
    [TestCase("1.02.3.4")]
    [TestCase("256.0.0.1")]
    [TestCase("1.2.3.999")]
    [TestCase("+1.2.3.4")]
    [TestCase("1.2.-3.4")]
    [TestCase("0x7f.0.0.1")]
    [TestCase("1.2.3.4 ")]
    [TestCase("１.2.3.4")]
    public void StandaloneIpv4GrammarRejectsNonStrictForms(string text)
    {
        IpV4ParseResult result = IpV4Parser.Parse(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.AddressBytes, Is.Empty, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv4Address), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host), text);
        });
    }

    [TestCase("1:2:3:4:5:6:7:8", "00010002000300040005000600070008")]
    [TestCase("::1:2:3:4:5:6:7", "00000001000200030004000500060007")]
    [TestCase("1::2:3:4:5:6:7", "00010000000200030004000500060007")]
    [TestCase("1:2::3:4:5:6:7", "00010002000000030004000500060007")]
    [TestCase("1:2:3::4:5:6:7", "00010002000300000004000500060007")]
    [TestCase("1:2:3:4::5:6:7", "00010002000300040000000500060007")]
    [TestCase("1:2:3:4:5::6:7", "00010002000300040005000000060007")]
    [TestCase("1:2:3:4:5:6::7", "00010002000300040005000600000007")]
    [TestCase("1:2:3:4:5:6:7::", "00010002000300040005000600070000")]
    public void BracketedIpv6AcceptsEveryRfc3986GrammarAlternative(
        string host,
        string expectedHex)
    {
        AssertIpv6Host(host, expectedHex);
    }

    [TestCase("1:2:3:4:5:6:192.0.2.33", "000100020003000400050006C0000221")]
    [TestCase("::1:2:3:4:5:192.0.2.33", "000000010002000300040005C0000221")]
    [TestCase("1::2:3:4:5:192.0.2.33", "000100000002000300040005C0000221")]
    [TestCase("1:2::3:4:5:192.0.2.33", "000100020000000300040005C0000221")]
    [TestCase("1:2:3::4:5:192.0.2.33", "000100020003000000040005C0000221")]
    [TestCase("1:2:3:4::5:192.0.2.33", "000100020003000400000005C0000221")]
    [TestCase("1:2:3:4:5::192.0.2.33", "000100020003000400050000C0000221")]
    public void EmbeddedIpv4IsAcceptedInEveryIpv6Ls32Alternative(
        string host,
        string expectedHex)
    {
        AssertIpv6Host(host, expectedHex);
    }

    [TestCase("::ffff:192.0.2")]
    [TestCase("::ffff:192.0.2.1.5")]
    [TestCase("::ffff:192.0.02.1")]
    [TestCase("::ffff:256.0.0.1")]
    [TestCase("::ffff:0xC0.0.2.1")]
    [TestCase("::ffff:+192.0.2.1")]
    [TestCase("::ffff:192.0.2.1 ")]
    [TestCase("1:2:3:4:5:192.0.2.1")]
    [TestCase("1:2:3:4:5:6:7:192.0.2.1")]
    [TestCase("1:2:3:4:5:6:192.0.2.1:7")]
    public void EmbeddedIpv4RejectsInvalidOrNonFinalForms(string host)
    {
        AssertInvalidIpv6Host(host);
    }

    [TestCase("")]
    [TestCase(":")]
    [TestCase(":1:2:3:4:5:6:7")]
    [TestCase("1:2:3:4:5:6:7")]
    [TestCase("1:2:3:4:5:6:7:")]
    [TestCase("1:2:3:4:5:6:7:8:9")]
    [TestCase("1::2::3")]
    [TestCase(":::1")]
    [TestCase("1:::2")]
    [TestCase("1:2:3:4:5:6:7::8")]
    [TestCase("12345:2:3:4:5:6:7:8")]
    [TestCase("1:2:3:4:5:6:7:g")]
    [TestCase("1:2:3:4:5:6:7:+8")]
    [TestCase("1:2:3:4:5:6:7:８")]
    public void BracketedIpv6RejectsInvalidGroupAndCompressionForms(string host)
    {
        AssertInvalidIpv6Host(host);
    }

    [TestCase("2001:db8:0:0:1:0:0:1")]
    [TestCase("2001:0db8:0:0:1:0:0:1")]
    [TestCase("2001:db8::1:0:0:1")]
    [TestCase("2001:db8::0:1:0:0:1")]
    [TestCase("2001:0db8::1:0:0:1")]
    [TestCase("2001:db8:0:0:1::1")]
    [TestCase("2001:db8:0000:0:1::1")]
    [TestCase("2001:DB8:0:0:1::1")]
    public void Rfc5952IntroductionSpellingsAreAcceptedAndRemainLossless(string host)
    {
        const string expectedHex = "20010DB8000000000001000000000001";
        string text = "x://[" + host + "]:80/path";
        UriReference reference = ParseReference(text);
        IUriComposer composer = new UriComposer();

        string recomposed = composer.ComposeReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(
                reference.Authority.AddressBytes,
                Is.EqualTo(Convert.FromHexString(expectedHex)),
                host);
            Assert.That(recomposed, Is.EqualTo(text), host);
        });
    }

    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:0001")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:001")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:01")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:1")]
    public void Rfc5952LeadingZeroInputSpellingsAreAllAccepted(string host)
    {
        AssertIpv6Host(host, "20010DB8AAAABBBBCCCCDDDDEEEE0001");
    }

    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd::1")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:0:1")]
    [TestCase("2001:db8:0:0:0::1")]
    [TestCase("2001:db8:0:0::1")]
    [TestCase("2001:db8:0::1")]
    [TestCase("2001:db8::1")]
    [TestCase("2001:db8::aaaa:0:0:1")]
    [TestCase("2001:db8:0:0:aaaa::1")]
    [TestCase("2001:db8::1:1:1:1:1")]
    public void Rfc5952CompressionExamplesAreAcceptedAsLegalIpv6Input(string host)
    {
        UriReference reference = ParseReference("x://[" + host + "]/path");

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(reference.Authority.AddressBytes, Has.Length.EqualTo(16), host);
        });
    }

    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:aaaa")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:AAAA")]
    [TestCase("2001:db8:aaaa:bbbb:cccc:dddd:eeee:AaAa")]
    public void Rfc5952HexCaseExamplesAreAcceptedWithoutInputRewriting(string host)
    {
        string text = "x://[" + host + "]/";
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(reference.OriginalText, Is.EqualTo(text), host);
        });
    }

    [TestCase("v1.a")]
    [TestCase("V1.a")]
    [TestCase("vfF.A:b")]
    [TestCase("VABCDEF.x-y_z~!$&'()*+,;=:")]
    [TestCase("v0000000000000000..")]
    [TestCase("v0.:")]
    public void BracketedIpvFutureAcceptsVersionAndAddressCharacterBoundaries(string host)
    {
        string text = "x://[" + host + "]:9/path";
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.IpvFuture), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(reference.Authority.AddressBytes, Is.Empty, host);
            Assert.That(reference.Authority.Port, Is.EqualTo("9"), host);
        });
    }

    [TestCase("v")]
    [TestCase("v.")]
    [TestCase("v1")]
    [TestCase("v1.")]
    [TestCase("vG.address")]
    [TestCase("v1x.address")]
    [TestCase("v1.%41")]
    [TestCase("v1.a b")]
    [TestCase("v1.é")]
    public void BracketedIpvFutureRejectsMissingOrInvalidGrammarElements(string host)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(
            "x://[" + host + "]/",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, host);
            Assert.That(result.Diagnostic, Is.Not.Null, host);
            Assert.That(
                result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidIpvFutureAddress),
                host);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host), host);
        });
    }

    [TestCase("fe80::1234%eth0")]
    [TestCase("fe80::4321%7")]
    [TestCase("fe80::1%25eth0")]
    [TestCase("::%zone")]
    [TestCase("::ffff:192.0.2.1%25interface")]
    public void ZoneIdentifiersAreRejectedFromGenericUriIpLiterals(string host)
    {
        string text = "x://[" + host + "]/";
        int expectedOffset = text.IndexOf('%');
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, host);
            Assert.That(result.Diagnostic, Is.Not.Null, host);
            Assert.That(
                result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.ZoneIdentifierNotAllowed),
                host);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host), host);
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset), host);
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1), host);
            Assert.That(
                result.Diagnostic.Message,
                Is.EqualTo(DiagnosticFactory.ZoneIdentifierNotAllowedMessage),
                host);
        });
    }

    [Test]
    public void IpLiteralBracketsAndPortDelimiterArePreservedLosslessly()
    {
        const string text = "x://[2001:0DB8:0000::1]:080/path";
        UriReference reference = ParseReference(text);
        IUriComposer composer = new UriComposer();

        string recomposed = composer.ComposeReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.Host, Is.EqualTo("2001:0DB8:0000::1"));
            Assert.That(reference.Authority.Port, Is.EqualTo("080"));
            Assert.That(recomposed, Is.EqualTo(text));
        });
    }

    [Test]
    public void Ipv4AndIpvFutureDoNotBecomeIpLiteralsWithoutBrackets()
    {
        UriReference dotted = ParseReference("x://999.0.0.1/");
        UriReference future = ParseReference("x://v1.address/");

        Assert.Multiple(() =>
        {
            Assert.That(dotted.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(future.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
        });
    }

    [TestCase("x://[192.0.2.1]/")]
    [TestCase("x://[999.0.0.1]/")]
    public void BracketedDottedDecimalMustMatchIpv6RatherThanIpv4(string text)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv6Address), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host), text);
        });
    }

    private static UriReference ParseReference(string text)
    {
        IUriParser parser = new UriParser();
        return parser.ParseReference(text, new UriParseOptions());
    }

    private static void AssertIpv6Host(string host, string expectedHex)
    {
        UriReference reference = ParseReference("x://[" + host + "]/path");

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(
                reference.Authority.AddressBytes,
                Is.EqualTo(Convert.FromHexString(expectedHex)),
                host);
        });
    }

    private static void AssertInvalidIpv6Host(string host)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(
            "x://[" + host + "]/",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, host);
            Assert.That(result.Diagnostic, Is.Not.Null, host);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv6Address), host);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host), host);
        });
    }
}
