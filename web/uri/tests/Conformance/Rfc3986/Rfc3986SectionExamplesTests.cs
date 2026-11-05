namespace uri.Tests;

public class Rfc3986SectionExamplesTests
{
    [TestCase("ftp://ftp.is.co.za/rfc/rfc1808.txt", "ftp", true, "ftp.is.co.za", "/rfc/rfc1808.txt", false, "")]
    [TestCase("http://www.ietf.org/rfc/rfc2396.txt", "http", true, "www.ietf.org", "/rfc/rfc2396.txt", false, "")]
    [TestCase("ldap://[2001:db8::7]/c=GB?objectClass?one", "ldap", true, "2001:db8::7", "/c=GB", true, "objectClass?one")]
    [TestCase("mailto:John.Doe@example.com", "mailto", false, "", "John.Doe@example.com", false, "")]
    [TestCase("news:comp.infosystems.www.servers.unix", "news", false, "", "comp.infosystems.www.servers.unix", false, "")]
    [TestCase("tel:+1-816-555-1212", "tel", false, "", "+1-816-555-1212", false, "")]
    [TestCase("telnet://192.0.2.16:80/", "telnet", true, "192.0.2.16", "/", false, "")]
    [TestCase("urn:oasis:names:specification:docbook:dtd:xml:4.1.2", "urn", false, "", "oasis:names:specification:docbook:dtd:xml:4.1.2", false, "")]
    public void SectionOneUriExamplesMatchTheirGenericComponents(
        string text,
        string scheme,
        bool hasAuthority,
        string host,
        string path,
        bool hasQuery,
        string query)
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.Uri), text);
            Assert.That(reference.Scheme, Is.EqualTo(scheme), text);
            Assert.That(reference.HasAuthority, Is.EqualTo(hasAuthority), text);
            Assert.That(reference.Authority.Host, Is.EqualTo(host), text);
            Assert.That(reference.Path, Is.EqualTo(path), text);
            Assert.That(reference.HasQuery, Is.EqualTo(hasQuery), text);
            Assert.That(reference.Query, Is.EqualTo(query), text);
            Assert.That(reference.HasFragment, Is.False, text);
            Assert.That(reference.OriginalText, Is.EqualTo(text), text);
        });
    }

    [Test]
    public void SectionThreeHierarchicalExampleMatchesAllFiveComponents()
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(
            "foo://example.com:8042/over/there?name=ferret#nose",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Scheme, Is.EqualTo("foo"));
            Assert.That(reference.HasAuthority, Is.True);
            Assert.That(reference.Authority.Host, Is.EqualTo("example.com"));
            Assert.That(reference.Authority.HasPort, Is.True);
            Assert.That(reference.Authority.Port, Is.EqualTo("8042"));
            Assert.That(reference.Path, Is.EqualTo("/over/there"));
            Assert.That(reference.HasQuery, Is.True);
            Assert.That(reference.Query, Is.EqualTo("name=ferret"));
            Assert.That(reference.HasFragment, Is.True);
            Assert.That(reference.Fragment, Is.EqualTo("nose"));
        });
    }

    [Test]
    public void SectionThreeUrnExampleUsesAColonBearingRootlessPath()
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(
            "urn:example:animal:ferret:nose",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.HasScheme, Is.True);
            Assert.That(reference.Scheme, Is.EqualTo("urn"));
            Assert.That(reference.HasAuthority, Is.False);
            Assert.That(reference.Path, Is.EqualTo("example:animal:ferret:nose"));
        });
    }

    [Test]
    public void SectionThreePathExamplesDistinguishRootlessAndEmptyPaths()
    {
        IUriParser parser = new UriParser();

        UriReference mail = parser.ParseReference(
            "mailto:fred@example.com",
            new UriParseOptions());
        UriReference information = parser.ParseReference(
            "foo://info.example.com?fred",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(mail.HasAuthority, Is.False);
            Assert.That(mail.Path, Is.EqualTo("fred@example.com"));
            Assert.That(information.HasAuthority, Is.True);
            Assert.That(information.Authority.Host, Is.EqualTo("info.example.com"));
            Assert.That(information.Path, Is.Empty);
            Assert.That(information.HasQuery, Is.True);
            Assert.That(information.Query, Is.EqualTo("fred"));
        });
    }

    [Test]
    public void SectionFourColonExampleRequiresDotSegmentForRelativeForm()
    {
        IUriParser parser = new UriParser();

        UriParseResult guarded = parser.TryParseRelativeReference(
            "./this:that",
            new UriParseOptions());
        UriParseResult ambiguousRelative = parser.TryParseRelativeReference(
            "this:that",
            new UriParseOptions());
        UriParseResult ambiguousReference = parser.TryParseReference(
            "this:that",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(guarded.Success, Is.True);
            Assert.That(guarded.Reference.Path, Is.EqualTo("./this:that"));
            Assert.That(ambiguousRelative.Success, Is.False);
            Assert.That(ambiguousRelative.Diagnostic, Is.Not.Null);
            Assert.That(ambiguousRelative.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(ambiguousReference.Success, Is.True);
            Assert.That(ambiguousReference.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(ambiguousReference.Reference.Scheme, Is.EqualTo("this"));
        });
    }

    [TestCase("", UriReferenceKind.EmptyReference, false, "")]
    [TestCase("#", UriReferenceKind.SameDocumentReference, true, "")]
    [TestCase("#fragment", UriReferenceKind.SameDocumentReference, true, "fragment")]
    public void SectionFourSameDocumentExamplesPreserveFragmentPresence(
        string text,
        UriReferenceKind kind,
        bool hasFragment,
        string fragment)
    {
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReference(text, new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(kind), text);
            Assert.That(result.Reference.HasFragment, Is.EqualTo(hasFragment), text);
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment), text);
        });
    }

    [Test]
    public void AppendixBExampleMatchesTheSpecifiedSubexpressions()
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(
            "http://www.ics.uci.edu/pub/ietf/uri/#Related",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Scheme, Is.EqualTo("http"));
            Assert.That(reference.Authority.Host, Is.EqualTo("www.ics.uci.edu"));
            Assert.That(reference.Path, Is.EqualTo("/pub/ietf/uri/"));
            Assert.That(reference.HasQuery, Is.False);
            Assert.That(reference.Query, Is.Empty);
            Assert.That(reference.HasFragment, Is.True);
            Assert.That(reference.Fragment, Is.EqualTo("Related"));
        });
    }
}
