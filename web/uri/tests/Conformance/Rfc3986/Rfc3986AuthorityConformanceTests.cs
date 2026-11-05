namespace uri.Tests;

public class Rfc3986AuthorityConformanceTests
{
    [TestCase("x://a@b@host/", 5)]
    [TestCase("//first@second@host/path", 7)]
    [TestCase("x://user@@host/", 8)]
    public void MultipleRawAtSignsUseTheFinalDelimiterThenRejectEarlierAtSigns(
        string text,
        int expectedOffset)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUserInfo), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.UserInfo), text);
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset), text);
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1), text);
        });
    }

    [TestCase("x://%40@host/", "%40", "host")]
    [TestCase("x://first%40second@host/", "first%40second", "host")]
    [TestCase("x://host%40name/", "", "host%40name")]
    [TestCase("x://host@/", "host", "")]
    public void EncodedAtSignsRemainDataAndRawFinalAtSignsRemainDelimiters(
        string text,
        string userInfo,
        string host)
    {
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HasUserInfo, Is.EqualTo(text.Contains('@')), text);
            Assert.That(reference.Authority.UserInfo, Is.EqualTo(userInfo), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
        });
    }

    [Test]
    public void EncodedAuthorityDelimitersDoNotSplitTheAuthority()
    {
        const string text =
            "x://name%40part@host%3Avalue%2Fpart%3Fq%23f:007/path?query#fragment";

        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.UserInfo, Is.EqualTo("name%40part"));
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(reference.Authority.Host, Is.EqualTo("host%3Avalue%2Fpart%3Fq%23f"));
            Assert.That(reference.Authority.HasPort, Is.True);
            Assert.That(reference.Authority.Port, Is.EqualTo("007"));
            Assert.That(reference.Path, Is.EqualTo("/path"));
            Assert.That(reference.Query, Is.EqualTo("query"));
            Assert.That(reference.Fragment, Is.EqualTo("fragment"));
        });
    }

    [TestCase("x://%5Bnot-ip%5D/", "%5Bnot-ip%5D")]
    [TestCase("x://host%3A80/", "host%3A80")]
    [TestCase("x://host%2Fpath/", "host%2Fpath")]
    [TestCase("x://host%3Fquery/", "host%3Fquery")]
    [TestCase("x://host%23fragment/", "host%23fragment")]
    public void EncodedStructuralCharactersRemainRegisteredNameData(
        string text,
        string host)
    {
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Path, Is.EqualTo("/"), text);
        });
    }

    [TestCase("x://", false, "", "", false, "")]
    [TestCase("x://@", true, "", "", false, "")]
    [TestCase("x://:", false, "", "", true, "")]
    [TestCase("x://@:", true, "", "", true, "")]
    [TestCase("x://user@", true, "user", "", false, "")]
    [TestCase("x://host:", false, "", "host", true, "")]
    [TestCase("x://user@host:", true, "user", "host", true, "")]
    public void EmptyAuthoritySubcomponentsRemainDistinctFromAbsentSubcomponents(
        string text,
        bool hasUserInfo,
        string userInfo,
        string host,
        bool hasPort,
        string port)
    {
        UriReference reference = ParseReference(text);
        IUriComposer composer = new UriComposer();

        string recomposed = composer.ComposeReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.HasAuthority, Is.True, text);
            Assert.That(reference.Authority.HasUserInfo, Is.EqualTo(hasUserInfo), text);
            Assert.That(reference.Authority.UserInfo, Is.EqualTo(userInfo), text);
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Authority.HasPort, Is.EqualTo(hasPort), text);
            Assert.That(reference.Authority.Port, Is.EqualTo(port), text);
            Assert.That(recomposed, Is.EqualTo(text), text);
        });
    }

    [Test]
    public void MissingAuthorityUsesAbsentHostRatherThanPresentEmptyHost()
    {
        UriReference noAuthority = ParseReference("x:path");
        UriReference emptyAuthority = ParseReference("x://");

        Assert.Multiple(() =>
        {
            Assert.That(noAuthority.HasAuthority, Is.False);
            Assert.That(noAuthority.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(emptyAuthority.HasAuthority, Is.True);
            Assert.That(emptyAuthority.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
        });
    }

    [TestCase("x://Example.COM/", UriHostKind.RegisteredName, "Example.COM")]
    [TestCase("x://192.0.2.128/", UriHostKind.Ipv4, "192.0.2.128")]
    [TestCase("x://[2001:0DB8::1]/", UriHostKind.Ipv6, "2001:0DB8::1")]
    [TestCase("x://[Vf.A:B]/", UriHostKind.IpvFuture, "Vf.A:B")]
    public void AuthorityPopulatesEveryPresentHostKindWithoutChangingSpelling(
        string text,
        UriHostKind kind,
        string host)
    {
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(kind), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
        });

        if (kind == UriHostKind.Ipv4)
        {
            Assert.That(
                reference.Authority.AddressBytes,
                Is.EqualTo(new byte[] { 192, 0, 2, 128 }));
        }
        else if (kind == UriHostKind.Ipv6)
        {
            Assert.That(
                reference.Authority.AddressBytes,
                Is.EqualTo(Convert.FromHexString("20010DB8000000000000000000000001")));
        }
        else
        {
            Assert.That(reference.Authority.AddressBytes, Is.Empty);
        }
    }

    [TestCase("0.0.0.0")]
    [TestCase("1.2.3.4")]
    [TestCase("192.168.0.1")]
    [TestCase("255.255.255.255")]
    public void StrictFourOctetDecimalHostsWinOverRegisteredName(string host)
    {
        UriReference reference = ParseReference("x://" + host + "/");

        Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv4), host);
    }

    [TestCase("256.0.0.1")]
    [TestCase("999.999.999.999")]
    [TestCase("01.2.3.4")]
    [TestCase("192.168.001.1")]
    [TestCase("127.1")]
    [TestCase("1.2.3.4.5")]
    [TestCase("1.2.3.4x")]
    [TestCase("0x7f.0.0.1")]
    public void DottedNamesOutsideStrictIpv4RemainRegisteredNames(string host)
    {
        UriReference reference = ParseReference("x://" + host + "/");

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName), host);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), host);
            Assert.That(reference.Authority.AddressBytes, Is.Empty, host);
        });
    }

    [TestCase("x://[::1", UriDiagnosticCode.InvalidHost, UriComponent.Host, 8, 0)]
    [TestCase("x://[[::1]/", UriDiagnosticCode.InvalidHost, UriComponent.Host, 5, 1)]
    [TestCase("x://[::1]]/", UriDiagnosticCode.InvalidHost, UriComponent.Host, 9, 1)]
    [TestCase("x://[::1]80/", UriDiagnosticCode.InvalidHost, UriComponent.Host, 9, 1)]
    [TestCase("x://[]/", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host, 5, 0)]
    [TestCase("x://::1/", UriDiagnosticCode.InvalidPort, UriComponent.Port, 5, 1)]
    [TestCase("x://[v1.a:b]x/", UriDiagnosticCode.InvalidHost, UriComponent.Host, 12, 1)]
    [TestCase("x://u[ser@host/", UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo, 5, 1)]
    public void InvalidBracketCombinationsReturnStableAuthorityDiagnostics(
        string text,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length)
    {
        AssertAuthorityFailure(text, code, component, offset, length);
    }

    [TestCase("x://host:x/", 9)]
    [TestCase("x://host:12x/", 11)]
    [TestCase("x://host:-1/", 9)]
    [TestCase("x://host:+80/", 9)]
    [TestCase("x://host:80:90/", 11)]
    [TestCase("x://[::1]:x/", 10)]
    [TestCase("x://[::1]:80:90/", 12)]
    [TestCase("x://host:８０/", 9)]
    public void InvalidPortsRejectTheFirstNonAsciiDigit(string text, int offset)
    {
        AssertAuthorityFailure(
            text,
            UriDiagnosticCode.InvalidPort,
            UriComponent.Port,
            offset,
            1);
    }

    [TestCase("x://host:")]
    [TestCase("x://host:0")]
    [TestCase("x://host:00080")]
    [TestCase("x://host:65536")]
    [TestCase("x://host:999999999999999999999999999999")]
    [TestCase("x://[::1]:443")]
    public void PortGrammarAcceptsEmptyAndAnyLengthOfAsciiDigits(string text)
    {
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.HasPort, Is.True, text);
            Assert.That(reference.Authority.Port, Is.EqualTo(text.Substring(text.LastIndexOf(':') + 1)), text);
        });
    }

    [TestCase("x://host/path", "host", "/path", false, false)]
    [TestCase("x://host?query", "host", "", true, false)]
    [TestCase("x://host#fragment", "host", "", false, true)]
    [TestCase("//host/path?query#fragment", "host", "/path", true, true)]
    public void RawPathQueryAndFragmentDelimitersTerminateAuthority(
        string text,
        string host,
        string path,
        bool hasQuery,
        bool hasFragment)
    {
        UriReference reference = ParseReference(text);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Path, Is.EqualTo(path), text);
            Assert.That(reference.HasQuery, Is.EqualTo(hasQuery), text);
            Assert.That(reference.HasFragment, Is.EqualTo(hasFragment), text);
        });
    }

    private static UriReference ParseReference(string text)
    {
        IUriParser parser = new UriParser();
        return parser.ParseReference(text, new UriParseOptions());
    }

    private static void AssertAuthorityFailure(
        string text,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Reference.HasAuthority, Is.False, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri), text);
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code), text);
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component), text);
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset), text);
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length), text);
        });
    }
}
