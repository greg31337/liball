namespace uri.Tests;

public class Rfc3986AppendixAGrammarTests
{
    [TestCase("x://host/a//b", true, "host", "/a//b")]
    [TestCase("x:/a/b", false, "", "/a/b")]
    [TestCase("x:a:b/@c", false, "", "a:b/@c")]
    [TestCase("x:", false, "", "")]
    public void UriAcceptsEveryHierPartAlternative(
        string text,
        bool hasAuthority,
        string host,
        string path)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.Uri), text);
            Assert.That(reference.HasScheme, Is.True, text);
            Assert.That(reference.HasAuthority, Is.EqualTo(hasAuthority), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Path, Is.EqualTo(path), text);
        });
    }

    [TestCase("x://host/a//b?query")]
    [TestCase("x:/a/b?query")]
    [TestCase("x:a:b?query")]
    [TestCase("x:?query")]
    public void AbsoluteUriAcceptsEveryHierPartAlternativeWithoutFragment(string text)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseAbsoluteUri(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri), text);
            Assert.That(result.Reference.HasQuery, Is.True, text);
            Assert.That(result.Reference.Query, Is.EqualTo("query"), text);
            Assert.That(result.Reference.HasFragment, Is.False, text);
        });
    }

    [TestCase("x:#fragment")]
    [TestCase("x://host/#")]
    public void AbsoluteUriRejectsTheFragmentProduction(string text)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseAbsoluteUri(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidFragment), text);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Fragment), text);
        });
    }

    [TestCase("//host/a//b", UriReferenceKind.NetworkPathReference, true, "/a//b")]
    [TestCase("/a/b", UriReferenceKind.AbsolutePathReference, false, "/a/b")]
    [TestCase("a/b:c", UriReferenceKind.RelativePathReference, false, "a/b:c")]
    [TestCase("", UriReferenceKind.EmptyReference, false, "")]
    [TestCase("?query", UriReferenceKind.RelativeReference, false, "")]
    [TestCase("#fragment", UriReferenceKind.SameDocumentReference, false, "")]
    public void RelativeRefAcceptsEveryRelativePartAlternative(
        string text,
        UriReferenceKind kind,
        bool hasAuthority,
        string path)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseRelativeReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Reference.HasScheme, Is.False, text);
            Assert.That(result.Reference.HasAuthority, Is.EqualTo(hasAuthority), text);
            Assert.That(result.Reference.Path, Is.EqualTo(path), text);
        });
    }

    [TestCase("a", true)]
    [TestCase("A1+.-", true)]
    [TestCase("1a", false)]
    [TestCase("a_", false)]
    [TestCase("", false)]
    public void SchemeMatchesAlphaThenAllowedContinuationCharacters(
        string scheme,
        bool valid)
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = scheme;
        IUriValidator validator = new UriValidator();

        UriValidationResult result = validator.ValidateComponents(
            reference,
            new UriValidationOptions());

        Assert.That(result.Success, Is.EqualTo(valid), scheme);
        if (!valid)
        {
            Assert.That(result.Diagnostic, Is.Not.Null, scheme);
            Assert.That(result.Diagnostic!.Component, Is.EqualTo(UriComponent.Scheme), scheme);
        }
    }

    [TestCase("x://host", false, "", "host", false, "")]
    [TestCase("x://@host", true, "", "host", false, "")]
    [TestCase("x://user:password@host", true, "user:password", "host", false, "")]
    [TestCase("x://host:", false, "", "host", true, "")]
    [TestCase("x://host:080", false, "", "host", true, "080")]
    [TestCase("x://@:", true, "", "", true, "")]
    public void AuthorityPreservesOptionalAndPresentEmptySubcomponents(
        string text,
        bool hasUserInfo,
        string userInfo,
        string host,
        bool hasPort,
        string port)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.HasAuthority, Is.True, text);
            Assert.That(reference.Authority.HasUserInfo, Is.EqualTo(hasUserInfo), text);
            Assert.That(reference.Authority.UserInfo, Is.EqualTo(userInfo), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Authority.HasPort, Is.EqualTo(hasPort), text);
            Assert.That(reference.Authority.Port, Is.EqualTo(port), text);
        });
    }

    [TestCase("x://[2001:db8::1]/", UriHostKind.Ipv6)]
    [TestCase("x://[v1.a:b]/", UriHostKind.IpvFuture)]
    [TestCase("x://192.0.2.1/", UriHostKind.Ipv4)]
    [TestCase("x://example!$&'()*+,;=%41/", UriHostKind.RegisteredName)]
    [TestCase("x:///", UriHostKind.RegisteredName)]
    public void HostAcceptsEveryAppendixAAlternative(string text, UriHostKind kind)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.That(reference.Authority.HostKind, Is.EqualTo(kind), text);
    }

    [TestCase("x://host/a//b/", "/a//b/")]
    [TestCase("x:/a//b/", "/a//b/")]
    [TestCase("a//b/", "a//b/")]
    [TestCase("x:a//b/", "a//b/")]
    [TestCase("", "")]
    public void PathProductionsPreserveSegmentsAndEmptySegments(string text, string path)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.That(reference.Path, Is.EqualTo(path), text);
    }

    [Test]
    public void PcharQueryAndFragmentAcceptTheirCompleteCharacterBoundaries()
    {
        const string pchar = "AZaz09-._~!$&'()*+,;=:@%41";
        string text = "x:/" + pchar + "?" + pchar + "/?" + "#" + pchar + "/?";
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());
        string recomposed = composer.ComposeReference(reference, new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Path, Is.EqualTo("/" + pchar));
            Assert.That(reference.Query, Is.EqualTo(pchar + "/?"));
            Assert.That(reference.Fragment, Is.EqualTo(pchar + "/?"));
            Assert.That(recomposed, Is.EqualTo(text));
        });
    }

    [TestCase("x://host/a?b/c?d#e/f?g", "/a", "b/c?d", "e/f?g")]
    [TestCase("x://host?#", "", "", "")]
    [TestCase("x:/a%2Fb?c%23d#e%3Ff", "/a%2Fb", "c%23d", "e%3Ff")]
    public void DelimitersTerminateOnlyTheirApplicableComponents(
        string text,
        string path,
        string query,
        string fragment)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Path, Is.EqualTo(path), text);
            Assert.That(reference.HasQuery, Is.True, text);
            Assert.That(reference.Query, Is.EqualTo(query), text);
            Assert.That(reference.HasFragment, Is.True, text);
            Assert.That(reference.Fragment, Is.EqualTo(fragment), text);
        });
    }

    [TestCase("1x:path", UriComponent.Path)]
    [TestCase("x://user[name@host/", UriComponent.UserInfo)]
    [TestCase("x://exa[mple/", UriComponent.Host)]
    [TestCase("x://host:abc/", UriComponent.Port)]
    [TestCase("x:/bad path", UriComponent.Path)]
    [TestCase("x:/a?bad space", UriComponent.Query)]
    [TestCase("x:/a#bad space", UriComponent.Fragment)]
    [TestCase("x:/a%GG", UriComponent.Path)]
    public void InvalidGrammarCharactersFailInTheirOwningComponent(
        string text,
        UriComponent component)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(result.Diagnostic!.Component, Is.EqualTo(component), text);
        });
    }

    [Test]
    public void ComponentValidationEnforcesAuthorityPathAndDoubleSlashBoundaries()
    {
        IUriParser parser = new UriParser();
        IUriValidator validator = new UriValidator();
        UriReference authorityRootless = parser.ParseReference(
            "x://host/path",
            new UriParseOptions());
        authorityRootless.Path = "rootless";
        UriReference noAuthorityDoubleSlash = parser.ParseReference(
            "/path",
            new UriParseOptions());
        noAuthorityDoubleSlash.Path = "//path";

        UriValidationResult authorityResult = validator.ValidateComponents(
            authorityRootless,
            new UriValidationOptions());
        UriValidationResult noAuthorityResult = validator.ValidateComponents(
            noAuthorityDoubleSlash,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(authorityResult.Success, Is.False);
            Assert.That(authorityResult.Diagnostic, Is.Not.Null);
            Assert.That(authorityResult.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(noAuthorityResult.Success, Is.False);
            Assert.That(noAuthorityResult.Diagnostic, Is.Not.Null);
            Assert.That(noAuthorityResult.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
        });
    }

    [Test]
    public void UriReferenceFirstMatchDistinguishesEveryReferenceKind()
    {
        IUriParser parser = new UriParser();
        string[] texts = new string[]
        {
            "x:path",
            "?query",
            "//host/path",
            "/path",
            "path",
            "",
            "#fragment"
        };
        UriReferenceKind[] kinds = new UriReferenceKind[]
        {
            UriReferenceKind.Uri,
            UriReferenceKind.RelativeReference,
            UriReferenceKind.NetworkPathReference,
            UriReferenceKind.AbsolutePathReference,
            UriReferenceKind.RelativePathReference,
            UriReferenceKind.EmptyReference,
            UriReferenceKind.SameDocumentReference
        };

        int index = 0;
        while (index < texts.Length)
        {
            UriParseResult result = parser.TryParseReference(
                texts[index],
                new UriParseOptions());
            Assert.That(result.Success, Is.True, texts[index]);
            Assert.That(result.Reference.Kind, Is.EqualTo(kinds[index]), texts[index]);
            index = index + 1;
        }
    }
}
