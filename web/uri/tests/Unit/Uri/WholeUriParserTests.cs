namespace uri.Tests;

public class WholeUriParserTests
{
    [TestCase("a:b", UriReferenceKind.Uri)]
    [TestCase("?query", UriReferenceKind.RelativeReference)]
    [TestCase("//host/path", UriReferenceKind.NetworkPathReference)]
    [TestCase("/path", UriReferenceKind.AbsolutePathReference)]
    [TestCase("path", UriReferenceKind.RelativePathReference)]
    [TestCase("", UriReferenceKind.EmptyReference)]
    [TestCase("#fragment", UriReferenceKind.SameDocumentReference)]
    public void UriReferenceUsesFirstMatchAndReturnsSpecificClassification(
        string text,
        UriReferenceKind expectedKind)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(expectedKind), text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
    }

    [TestCase("scheme:", "scheme", false, "", "", false, "", false, "")]
    [TestCase("urn:example:a:b", "urn", false, "", "example:a:b", false, "", false, "")]
    [TestCase("file:/a//b/", "file", false, "", "/a//b/", false, "", false, "")]
    [TestCase("x:?query", "x", false, "", "", true, "query", false, "")]
    [TestCase("x:#", "x", false, "", "", false, "", true, "")]
    [TestCase("x:///path?#", "x", true, "", "/path", true, "", true, "")]
    [TestCase("HTTP://Example.COM/a?b/c?d#e/f?g", "HTTP", true, "Example.COM", "/a", true, "b/c?d", true, "e/f?g")]
    public void UriParsingPreservesLosslessComponentTextAndDelimiterStates(
        string text,
        string scheme,
        bool hasAuthority,
        string host,
        string path,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        UriParseResult result = UriParserCore.ParseUri(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(result.Reference.HasScheme, Is.True);
            Assert.That(result.Reference.Scheme, Is.EqualTo(scheme));
            Assert.That(result.Reference.HasAuthority, Is.EqualTo(hasAuthority));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo(host));
            Assert.That(result.Reference.Path, Is.EqualTo(path));
            Assert.That(result.Reference.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(result.Reference.Query, Is.EqualTo(query));
            Assert.That(result.Reference.HasFragment, Is.EqualTo(hasFragment));
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment));
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
        });
    }

    [Test]
    public void AuthorityParsingPopulatesEveryHostKindAndSubcomponentState()
    {
        UriParseResult registered = UriParserCore.ParseReference(
            "x://user%40name@Example.COM:080/path".AsSpan());
        UriParseResult ipv4 = UriParserCore.ParseReference(
            "x://192.0.2.128/".AsSpan());
        UriParseResult ipv6 = UriParserCore.ParseReference(
            "x://[2001:db8::1]:443/".AsSpan());
        UriParseResult future = UriParserCore.ParseReference(
            "x://[Vf.a:b]:/".AsSpan());
        UriParseResult noAuthority = UriParserCore.ParseReference(
            "x:path".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(registered.Reference.Authority.HasUserInfo, Is.True);
            Assert.That(registered.Reference.Authority.UserInfo, Is.EqualTo("user%40name"));
            Assert.That(registered.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.RegisteredName));
            Assert.That(registered.Reference.Authority.Host, Is.EqualTo("Example.COM"));
            Assert.That(registered.Reference.Authority.AddressBytes, Is.Empty);
            Assert.That(registered.Reference.Authority.HasPort, Is.True);
            Assert.That(registered.Reference.Authority.Port, Is.EqualTo("080"));

            Assert.That(ipv4.Success, Is.True);
            Assert.That(ipv4.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv4));
            Assert.That(ipv4.Reference.Authority.AddressBytes, Is.EqualTo(new byte[] { 192, 0, 2, 128 }));

            Assert.That(ipv6.Success, Is.True);
            Assert.That(ipv6.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6));
            Assert.That(ipv6.Reference.Authority.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(ipv6.Reference.Authority.AddressBytes, Is.EqualTo(Convert.FromHexString("20010DB8000000000000000000000001")));
            Assert.That(ipv6.Reference.Authority.Port, Is.EqualTo("443"));

            Assert.That(future.Success, Is.True);
            Assert.That(future.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.IpvFuture));
            Assert.That(future.Reference.Authority.Host, Is.EqualTo("Vf.a:b"));
            Assert.That(future.Reference.Authority.AddressBytes, Is.Empty);
            Assert.That(future.Reference.Authority.HasPort, Is.True);
            Assert.That(future.Reference.Authority.Port, Is.Empty);

            Assert.That(noAuthority.Success, Is.True);
            Assert.That(noAuthority.Reference.HasAuthority, Is.False);
            Assert.That(noAuthority.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
        });
    }

    [TestCase("x://192.0.2.1/", UriHostKind.Ipv4)]
    [TestCase("x://255.255.255.255/", UriHostKind.Ipv4)]
    [TestCase("x://999.999.999.999/", UriHostKind.RegisteredName)]
    [TestCase("x://127.1/", UriHostKind.RegisteredName)]
    [TestCase("x://0177.0.0.1/", UriHostKind.RegisteredName)]
    [TestCase("x://0x7f.0.0.1/", UriHostKind.RegisteredName)]
    public void HostClassificationAppliesStrictIpv4FirstMatchThenRegisteredName(
        string text,
        UriHostKind expectedKind)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Authority.HostKind, Is.EqualTo(expectedKind), text);
        });
    }

    [TestCase("", UriReferenceKind.EmptyReference, "", false, "", false, "")]
    [TestCase("?", UriReferenceKind.RelativeReference, "", true, "", false, "")]
    [TestCase("#", UriReferenceKind.SameDocumentReference, "", false, "", true, "")]
    [TestCase("?#", UriReferenceKind.RelativeReference, "", true, "", true, "")]
    [TestCase("//", UriReferenceKind.NetworkPathReference, "", false, "", false, "")]
    [TestCase("///path", UriReferenceKind.NetworkPathReference, "/path", false, "", false, "")]
    [TestCase("/path", UriReferenceKind.AbsolutePathReference, "/path", false, "", false, "")]
    [TestCase("a/b:c", UriReferenceKind.RelativePathReference, "a/b:c", false, "", false, "")]
    [TestCase("a%3Ab", UriReferenceKind.RelativePathReference, "a%3Ab", false, "", false, "")]
    public void RelativeReferenceParsesEveryTopLevelForm(
        string text,
        UriReferenceKind kind,
        string path,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        UriParseResult result = UriParserCore.ParseRelativeReference(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Reference.HasScheme, Is.False);
            Assert.That(result.Reference.Path, Is.EqualTo(path));
            Assert.That(result.Reference.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(result.Reference.Query, Is.EqualTo(query));
            Assert.That(result.Reference.HasFragment, Is.EqualTo(hasFragment));
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment));
        });
    }

    [Test]
    public void AbsoluteUriRequiresSchemeAndForbidsFragmentAtItsDelimiter()
    {
        UriParseResult success = UriParserCore.ParseAbsoluteUri(
            "http://host/path?query".AsSpan());
        UriParseResult missingScheme = UriParserCore.ParseAbsoluteUri(
            "/path".AsSpan());
        UriParseResult fragment = UriParserCore.ParseAbsoluteUri(
            "http:path#%".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Reference.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            Assert.That(success.Reference.HasFragment, Is.False);
            AssertFailure(missingScheme, UriDiagnosticCode.InvalidScheme, UriComponent.Scheme, 0);
            AssertFailure(fragment, UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 9);
        });
    }

    [Test]
    public void SpanInputsConsumeOnlyTheProvidedSliceAndPreserveItsOriginalText()
    {
        string padded = "beforehttp://host/path?query#fragmentafter";
        int start = 6;
        string expected = "http://host/path?query#fragment";

        UriParseResult result = UriParserCore.ParseReference(
            padded.AsSpan(start, expected.Length));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(expected));
            Assert.That(result.Reference.Scheme, Is.EqualTo("http"));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("host"));
            Assert.That(result.Reference.Path, Is.EqualTo("/path"));
            Assert.That(result.Reference.Query, Is.EqualTo("query"));
            Assert.That(result.Reference.Fragment, Is.EqualTo("fragment"));
        });
    }

    [TestCase("uri", "1abc:path", UriDiagnosticCode.InvalidScheme, UriComponent.Scheme, 0)]
    [TestCase("relative", "a:b", UriDiagnosticCode.InvalidPath, UriComponent.Path, 1)]
    [TestCase("reference", "x://a@b@host", UriDiagnosticCode.InvalidUserInfo, UriComponent.UserInfo, 5)]
    [TestCase("reference", "x://ho[st", UriDiagnosticCode.InvalidHost, UriComponent.Host, 6)]
    [TestCase("reference", "x://host%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Host, 9)]
    [TestCase("reference", "x://[12345::]", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host, 9)]
    [TestCase("reference", "x://[v1.]", UriDiagnosticCode.InvalidIpvFutureAddress, UriComponent.Host, 8)]
    [TestCase("reference", "x://[fe80::1%25eth0]", UriDiagnosticCode.ZoneIdentifierNotAllowed, UriComponent.Host, 12)]
    [TestCase("reference", "x://host:x", UriDiagnosticCode.InvalidPort, UriComponent.Port, 9)]
    [TestCase("reference", "x:path%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path, 7)]
    [TestCase("reference", "x:path?bad[query", UriDiagnosticCode.InvalidQuery, UriComponent.Query, 10)]
    [TestCase("reference", "x:path#bad#fragment", UriDiagnosticCode.InvalidFragment, UriComponent.Fragment, 10)]
    public void ComponentFailuresPropagateWithWholeInputOffsets(
        string entryPoint,
        string text,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        UriParseResult result = Parse(entryPoint, text);

        AssertFailure(result, code, component, offset);
    }

    [Test]
    public void AuthorityFailuresUseSourceOrderBeforeThePort()
    {
        UriParseResult userInfo = UriParserCore.ParseReference(
            "x://bad@user@host:x".AsSpan());
        UriParseResult host = UriParserCore.ParseReference(
            "x://host%GG:x".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(userInfo.Diagnostic, Is.Not.Null);
            Assert.That(userInfo.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUserInfo));
            Assert.That(host.Diagnostic, Is.Not.Null);
            Assert.That(host.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(host.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
        });
    }

    private static UriParseResult Parse(string entryPoint, string text)
    {
        switch (entryPoint)
        {
            case "uri":
                return UriParserCore.ParseUri(text.AsSpan());
            case "absolute":
                return UriParserCore.ParseAbsoluteUri(text.AsSpan());
            case "relative":
                return UriParserCore.ParseRelativeReference(text.AsSpan());
            default:
                return UriParserCore.ParseReference(text.AsSpan());
        }
    }

    private static void AssertFailure(
        UriParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.Empty);
            Assert.That(result.Reference.HasScheme, Is.False);
            Assert.That(result.Reference.HasAuthority, Is.False);
            Assert.That(result.Reference.Path, Is.Empty);
            Assert.That(result.Reference.HasQuery, Is.False);
            Assert.That(result.Reference.HasFragment, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        });
    }
}
