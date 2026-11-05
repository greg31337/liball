namespace uri.Tests;

public class IriComponentScannerTests
{
    [Test]
    public void AuthorityAcceptsInternationalizedUserInfoAndRegisteredName()
    {
        AuthorityValueParseResult result = IriAuthorityParser.Parse(
            "usér:密碼@例え.テスト:443".AsSpan(),
            10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Authority.HasUserInfo, Is.True);
            Assert.That(result.Authority.UserInfo, Is.EqualTo("usér:密碼"));
            Assert.That(result.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(result.Authority.Host, Is.EqualTo("例え.テスト"));
            Assert.That(result.Authority.HasPort, Is.True);
            Assert.That(result.Authority.Port, Is.EqualTo("443"));
        });
    }

    [Test]
    public void AuthorityPreservesStrictUriIpLiteralAndIpv4Parsing()
    {
        AuthorityValueParseResult ipv4 = IriAuthorityParser.Parse(
            "192.0.2.128".AsSpan(),
            0);
        AuthorityValueParseResult ipv6 = IriAuthorityParser.Parse(
            "[2001:db8::1]:".AsSpan(),
            0);
        AuthorityValueParseResult future = IriAuthorityParser.Parse(
            "[vF.a:b]".AsSpan(),
            0);
        AuthorityValueParseResult registered = IriAuthorityParser.Parse(
            "999.999.999.999".AsSpan(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Success, Is.True);
            Assert.That(ipv4.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv4));
            Assert.That(ipv4.Authority.AddressBytes, Is.EqualTo(new byte[] { 192, 0, 2, 128 }));
            Assert.That(ipv6.Success, Is.True);
            Assert.That(ipv6.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6));
            Assert.That(ipv6.Authority.HasPort, Is.True);
            Assert.That(ipv6.Authority.Port, Is.Empty);
            Assert.That(future.Success, Is.True);
            Assert.That(future.Authority.HostKind, Is.EqualTo(UriHostKind.IpvFuture));
            Assert.That(registered.Success, Is.True);
            Assert.That(registered.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
        });
    }

    [TestCase("name%20value@host")]
    [TestCase("name@host%2Dname")]
    public void AuthorityAcceptsCompletePercentTriplets(string text)
    {
        Assert.That(IriAuthorityParser.Parse(text.AsSpan(), 0).Success, Is.True);
    }

    [TestCase("user\uE000@host", UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo, 14)]
    [TestCase("user@host\uE000", UriDiagnosticCode.InvalidHost, UriComponent.Host, 19)]
    [TestCase("user@host:port", UriDiagnosticCode.InvalidPort, UriComponent.Port, 20)]
    [TestCase("user@host%G0", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Host, 20)]
    public void AuthorityRejectsCharactersOutsideIriProductions(
        string text,
        UriDiagnosticCode code,
        UriComponent component,
        int expectedOffset)
    {
        AuthorityValueParseResult result = IriAuthorityParser.Parse(text.AsSpan(), 10);

        AssertFailure(result.Diagnostic, code, component, expectedOffset);
    }

    [Test]
    public void AuthorityRejectsMalformedUnicodeAndUnicodeInsideIpLiteral()
    {
        AuthorityValueParseResult malformed = IriAuthorityParser.Parse(
            "host\uD800".AsSpan(),
            30);
        AuthorityValueParseResult literal = IriAuthorityParser.Parse(
            "[例え]".AsSpan(),
            40);

        Assert.Multiple(() =>
        {
            AssertFailure(
                malformed.Diagnostic,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.Host,
                34);
            AssertFailure(
                literal.Diagnostic,
                UriDiagnosticCode.InvalidIpv6Address,
                UriComponent.Host,
                41);
        });
    }

    [Test]
    public void EveryInternationalizedPathFormAcceptsUcsCharacters()
    {
        PathParseResult abEmpty = IriPathParser.ParseAbEmpty("/α//𐀀/".AsSpan(), 0);
        PathParseResult absolute = IriPathParser.ParseAbsolute("/例え/道".AsSpan(), 0);
        PathParseResult noScheme = IriPathParser.ParseNoScheme("例え/道:先".AsSpan(), 0);
        PathParseResult rootless = IriPathParser.ParseRootless("例え:道/先".AsSpan(), 0);
        PathParseResult empty = IriPathParser.ParseEmpty("".AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(abEmpty.Success, Is.True);
            Assert.That(abEmpty.SegmentCount, Is.EqualTo(4));
            Assert.That(absolute.Success, Is.True);
            Assert.That(absolute.SegmentCount, Is.EqualTo(2));
            Assert.That(noScheme.Success, Is.True);
            Assert.That(noScheme.SegmentCount, Is.EqualTo(2));
            Assert.That(rootless.Success, Is.True);
            Assert.That(rootless.SegmentCount, Is.EqualTo(2));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.SegmentCount, Is.Zero);
        });
    }

    [Test]
    public void PathRetainsUriFormRulesAndPercentTripletSemantics()
    {
        PathParseResult encodedColon = IriPathParser.ParseNoScheme("α%3Aβ".AsSpan(), 0);
        PathParseResult encodedSlash = IriPathParser.ParseRootless("α%2Fβ".AsSpan(), 0);
        PathParseResult literalColon = IriPathParser.ParseNoScheme("α:β".AsSpan(), 5);
        PathParseResult doubleSlash = IriPathParser.ParseAbsolute("//α".AsSpan(), 5);

        Assert.Multiple(() =>
        {
            Assert.That(encodedColon.Success, Is.True);
            Assert.That(encodedSlash.Success, Is.True);
            Assert.That(encodedSlash.SegmentCount, Is.EqualTo(1));
            AssertFailure(literalColon.Diagnostic, UriDiagnosticCode.InvalidPath, UriComponent.Path, 6);
            AssertFailure(doubleSlash.Diagnostic, UriDiagnosticCode.InvalidPath, UriComponent.Path, 6);
        });
    }

    [Test]
    public void PathRejectsPrivateCharactersMalformedUnicodeAndBadTriplets()
    {
        PathParseResult privateCharacter = IriPathParser.ParseRootless("a\uE000".AsSpan(), 10);
        PathParseResult malformed = IriPathParser.ParseRootless("a\uD800".AsSpan(), 20);
        PathParseResult percent = IriPathParser.ParseRootless("a%G0".AsSpan(), 30);

        Assert.Multiple(() =>
        {
            AssertFailure(privateCharacter.Diagnostic, UriDiagnosticCode.InvalidPath, UriComponent.Path, 11);
            AssertFailure(malformed.Diagnostic, UriDiagnosticCode.InvalidUnicode, UriComponent.Path, 21);
            AssertFailure(percent.Diagnostic, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 32);
        });
    }

    [Test]
    public void QueryAcceptsIprivateWhileFragmentAcceptsOnlyUcschar()
    {
        string text = "?α/β?\uE000#片/段?";
        QueryFragmentScanResult query = IriQueryFragmentParser.ScanQuery(
            text.AsSpan(),
            0,
            50);
        QueryFragmentScanResult fragment = IriQueryFragmentParser.ScanFragment(
            text.AsSpan(),
            query.NextOffset,
            50);
        QueryFragmentScanResult privateFragment = IriQueryFragmentParser.ScanFragment(
            "#片\uE000".AsSpan(),
            0,
            70);

        Assert.Multiple(() =>
        {
            Assert.That(query.Success, Is.True);
            Assert.That(query.ValueLength, Is.EqualTo(5));
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.ValueLength, Is.EqualTo(4));
            AssertFailure(
                privateFragment.Diagnostic,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                72);
        });
    }

    [Test]
    public void QueryAndFragmentReportUnicodeAndPercentFailuresPrecisely()
    {
        QueryFragmentScanResult query = IriQueryFragmentParser.ScanQuery(
            "?α\uD800".AsSpan(),
            0,
            100);
        QueryFragmentScanResult fragment = IriQueryFragmentParser.ScanFragment(
            "#α%G0".AsSpan(),
            0,
            200);

        Assert.Multiple(() =>
        {
            AssertFailure(query.Diagnostic, UriDiagnosticCode.InvalidUnicode, UriComponent.Query, 102);
            AssertFailure(
                fragment.Diagnostic,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Fragment,
                203);
        });
    }

    [Test]
    public void MissingOptionalDelimitersAndInvalidOffsetsAreHandled()
    {
        QueryFragmentScanResult query = IriQueryFragmentParser.ScanQuery(
            "α".AsSpan(),
            0,
            0);
        QueryFragmentScanResult fragment = IriQueryFragmentParser.ScanFragment(
            "α".AsSpan(),
            0,
            0);
        QueryFragmentScanResult invalid = IriQueryFragmentParser.ScanQuery(
            "".AsSpan(),
            -1,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(query.Success, Is.True);
            Assert.That(query.Present, Is.False);
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.Present, Is.False);
            AssertFailure(invalid.Diagnostic, UriDiagnosticCode.InvalidInput, UriComponent.Input, 0);
        });
    }

    private static void AssertFailure(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        });
    }
}
