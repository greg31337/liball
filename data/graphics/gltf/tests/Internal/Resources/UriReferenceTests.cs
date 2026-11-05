namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class UriReferenceTests
{
    [TestCase(null, GltfUriKind.Absent)]
    [TestCase("", GltfUriKind.Relative)]
    [TestCase("images/texture.png", GltfUriKind.Relative)]
    [TestCase("//cdn.example/model.bin", GltfUriKind.Relative)]
    [TestCase("file:///models/model.gltf", GltfUriKind.File)]
    [TestCase("FILE://localhost/models/model.gltf", GltfUriKind.File)]
    [TestCase("DATA:application/octet-stream,abc", GltfUriKind.Data)]
    [TestCase("https://example.com/model.gltf", GltfUriKind.Network)]
    [TestCase("FTP://example.com/model.gltf", GltfUriKind.Network)]
    [TestCase("ftps://example.com/model.gltf", GltfUriKind.Network)]
    [TestCase("ws://example.com/model.gltf", GltfUriKind.Network)]
    [TestCase("wss://example.com/model.gltf", GltfUriKind.Network)]
    [TestCase("urn:example:model", GltfUriKind.Absolute)]
    [TestCase("mailto:user@example.com", GltfUriKind.Absolute)]
    [TestCase("tel:+1-555-0100", GltfUriKind.Absolute)]
    [TestCase("news:comp.graphics", GltfUriKind.Absolute)]
    [TestCase("ldap://example.com/cn=model", GltfUriKind.Absolute)]
    [TestCase("asset:model/mesh", GltfUriKind.Custom)]
    public void ClassifyReference_UsesRfcSchemeSyntaxAndKnownResourceKinds(
        string? value,
        GltfUriKind expected)
    {
        Assert.That(new UriReference().ClassifyReference(value), Is.EqualTo(expected));
    }

    [Test]
    public void ParseReference_PreservesEscapedComponentsAndDelimiterPresence()
    {
        UriReferenceParts parts = new UriReference().ParseReference(
            "CuStOm://user@host:9/a%2Fb?x=%252F#f%2Fg");

        Assert.Multiple(() =>
        {
            Assert.That(parts.Scheme, Is.EqualTo("CuStOm"));
            Assert.That(parts.HasAuthority, Is.True);
            Assert.That(parts.Authority, Is.EqualTo("user@host:9"));
            Assert.That(parts.Path, Is.EqualTo("/a%2Fb"));
            Assert.That(parts.HasQuery, Is.True);
            Assert.That(parts.Query, Is.EqualTo("x=%252F"));
            Assert.That(parts.HasFragment, Is.True);
            Assert.That(parts.Fragment, Is.EqualTo("f%2Fg"));
        });

        UriReferenceParts emptyComponents = new UriReference().ParseReference("scheme://?#");
        Assert.Multiple(() =>
        {
            Assert.That(emptyComponents.HasAuthority, Is.True);
            Assert.That(emptyComponents.Authority, Is.Empty);
            Assert.That(emptyComponents.Path, Is.Empty);
            Assert.That(emptyComponents.HasQuery, Is.True);
            Assert.That(emptyComponents.Query, Is.Empty);
            Assert.That(emptyComponents.HasFragment, Is.True);
            Assert.That(emptyComponents.Fragment, Is.Empty);
        });
    }

    [TestCase("g:h", "g:h")]
    [TestCase("g", "http://a/b/c/g")]
    [TestCase("./g", "http://a/b/c/g")]
    [TestCase("g/", "http://a/b/c/g/")]
    [TestCase("/g", "http://a/g")]
    [TestCase("//g", "http://g")]
    [TestCase("?y", "http://a/b/c/d;p?y")]
    [TestCase("g?y", "http://a/b/c/g?y")]
    [TestCase("#s", "http://a/b/c/d;p?q#s")]
    [TestCase("g#s", "http://a/b/c/g#s")]
    [TestCase("g?y#s", "http://a/b/c/g?y#s")]
    [TestCase(";x", "http://a/b/c/;x")]
    [TestCase("g;x", "http://a/b/c/g;x")]
    [TestCase("g;x?y#s", "http://a/b/c/g;x?y#s")]
    [TestCase("", "http://a/b/c/d;p?q")]
    [TestCase(".", "http://a/b/c/")]
    [TestCase("./", "http://a/b/c/")]
    [TestCase("..", "http://a/b/")]
    [TestCase("../", "http://a/b/")]
    [TestCase("../g", "http://a/b/g")]
    [TestCase("../..", "http://a/")]
    [TestCase("../../", "http://a/")]
    [TestCase("../../g", "http://a/g")]
    public void ResolveReference_MatchesRfc3986NormalExamples(string reference, string expected)
    {
        Assert.That(
            new UriReference().ResolveReference("http://a/b/c/d;p?q", reference),
            Is.EqualTo(expected));
    }

    [TestCase("../../../g", "http://a/g")]
    [TestCase("../../../../g", "http://a/g")]
    [TestCase("/./g", "http://a/g")]
    [TestCase("/../g", "http://a/g")]
    [TestCase("g.", "http://a/b/c/g.")]
    [TestCase(".g", "http://a/b/c/.g")]
    [TestCase("g..", "http://a/b/c/g..")]
    [TestCase("..g", "http://a/b/c/..g")]
    [TestCase("./../g", "http://a/b/g")]
    [TestCase("./g/.", "http://a/b/c/g/")]
    [TestCase("g/./h", "http://a/b/c/g/h")]
    [TestCase("g/../h", "http://a/b/c/h")]
    [TestCase("g;x=1/./y", "http://a/b/c/g;x=1/y")]
    [TestCase("g;x=1/../y", "http://a/b/c/y")]
    [TestCase("g?y/./x", "http://a/b/c/g?y/./x")]
    [TestCase("g#s/../x", "http://a/b/c/g#s/../x")]
    [TestCase("http:g", "http:g")]
    public void ResolveReference_MatchesRfc3986AbnormalExamples(string reference, string expected)
    {
        Assert.That(
            new UriReference().ResolveReference("http://a/b/c/d;p?q", reference),
            Is.EqualTo(expected));
    }

    [Test]
    public void ResolveReference_PreservesQueryFragmentAndPercentEscapesWithoutDecoding()
    {
        UriReference resolver = new UriReference();

        Assert.Multiple(() =>
        {
            Assert.That(
                resolver.ResolveReference(
                    "https://example.com/a%2Fb/c%20d?old=%252F#old",
                    "../x%2Fy?value=%2525#part%2Ftwo"),
                Is.EqualTo("https://example.com/x%2Fy?value=%2525#part%2Ftwo"));
            Assert.That(
                resolver.ResolveReference("https://example.com/a/b/c", "%2E%2E/d"),
                Is.EqualTo("https://example.com/a/b/%2E%2E/d"));
            Assert.That(
                resolver.ResolveReference("https://example.com/a/b?old#fragment", "?#"),
                Is.EqualTo("https://example.com/a/b?#"));
            Assert.That(
                resolver.ResolveReference("https://example.com/a/b?old#fragment", "#new?value"),
                Is.EqualTo("https://example.com/a/b?old#new?value"));
        });
    }

    [Test]
    public void ResolveReference_MergesAgainstAnAuthorityWithAnEmptyPath()
    {
        Assert.That(
            new UriReference().ResolveReference("https://example.com", "model.bin"),
            Is.EqualTo("https://example.com/model.bin"));
    }

    [Test]
    public void ParseAndResolveReference_RejectInvalidSchemeAndRelativeBase()
    {
        UriReference resolver = new UriReference();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => resolver.ParseReference("1invalid:value"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("URI reference '1invalid:value' has an invalid scheme."));
            Assert.That(
                () => resolver.ResolveReference("models/model.gltf", "buffer.bin"),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.EqualTo("The base URI must be absolute and include a scheme."));
        });
    }
}
