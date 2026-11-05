using xml;

namespace xml.Tests;

[TestFixture]
public class UriToolsTests
{
    [TestCase("other:h", "other:h")]
    [TestCase("g", "http://a/b/c/g")]
    [TestCase("./g", "http://a/b/c/g")]
    [TestCase("g/", "http://a/b/c/g/")]
    [TestCase("/g", "http://a/g")]
    [TestCase("//g", "http://g/")]
    [TestCase("?y", "http://a/b/c/d;p?y")]
    [TestCase("g?y", "http://a/b/c/g?y")]
    [TestCase("#s", "http://a/b/c/d;p?q#s")]
    [TestCase("g#s", "http://a/b/c/g#s")]
    [TestCase(";x", "http://a/b/c/;x")]
    [TestCase("", "http://a/b/c/d;p?q")]
    [TestCase(".", "http://a/b/c/")]
    [TestCase("..", "http://a/b/")]
    [TestCase("../g", "http://a/b/g")]
    [TestCase("../../g", "http://a/g")]
    [TestCase("../../../g", "http://a/g")]
    [TestCase("g/./h", "http://a/b/c/g/h")]
    [TestCase("g/../h", "http://a/b/c/h")]
    public void ResolvesRfc3986ReferenceExamples(string reference, string expected)
    {
        Uri baseUri = UriTools.CreateAbsolute("http://a/b/c/d;p?q");

        Uri resolved = UriTools.ResolveReference(baseUri, reference);

        Assert.That(resolved.AbsoluteUri, Is.EqualTo(expected));
    }

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
    [TestCase("g;x=1/../y", "http://a/b/c/y")]
    [TestCase("g?y/./x", "http://a/b/c/g?y/./x")]
    [TestCase("g#s/../x", "http://a/b/c/g#s/../x")]
    public void ResolvesRfc3986AbnormalReferenceExamples(string reference, string expected)
    {
        Uri baseUri = UriTools.CreateAbsolute("http://a/b/c/d;p?q");

        Uri resolved = UriTools.ResolveReference(baseUri, reference);

        Assert.That(resolved.AbsoluteUri, Is.EqualTo(expected));
    }

    [Test]
    public void ParsesAbsoluteAndRelativeIriReferences()
    {
        Uri absolute = UriTools.CreateAbsolute("https://example.test/café/");
        Uri relative = UriTools.CreateReference("目录/文件.xml");

        Assert.That(absolute.IsAbsoluteUri, Is.True);
        Assert.That(UriTools.IriToUriString(absolute.OriginalString), Is.EqualTo("https://example.test/caf%C3%A9/"));
        Assert.That(relative.IsAbsoluteUri, Is.False);
        Assert.That(UriTools.IriToUriString(relative.OriginalString), Is.EqualTo("%E7%9B%AE%E5%BD%95/%E6%96%87%E4%BB%B6.xml"));
    }

    [Test]
    public void CanonicalizesInternationalizedHostsAndPaths()
    {
        Uri value = UriTools.CreateAbsolute("https://münich.example/路径/%7eitem?q=résumé");

        string canonical = UriTools.CanonicalAbsoluteUri(value);

        Assert.That(canonical, Is.EqualTo("https://xn--mnich-kva.example/%E8%B7%AF%E5%BE%84/~item?q=r%C3%A9sum%C3%A9"));
        Assert.That(UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(canonical)), Is.EqualTo(canonical));
    }

    [Test]
    public void NormalizesPercentEscapesWithoutDecodingReservedCharacters()
    {
        string normalized = UriTools.NormalizePercentEncoding("/%7euser/%41%2f%3a?q=%62%20c");

        Assert.That(normalized, Is.EqualTo("/~user/A%2F%3A?q=b%20c"));
        Assert.Throws<UriFormatException>(() => UriTools.NormalizePercentEncoding("broken%2"));
        Assert.Throws<UriFormatException>(() => UriTools.CreateReference("broken%GG"));
    }

    [Test]
    public void RejectsCharactersThatRequireEscapingAndInvalidUnicode()
    {
        string unmatchedSurrogate = new string(['\ud800']);

        Assert.Throws<UriFormatException>(() => UriTools.CreateReference("two words"));
        Assert.Throws<UriFormatException>(() => UriTools.CreateReference("a\\b"));
        Assert.Throws<UriFormatException>(() => UriTools.CreateReference("a#b#c"));
        Assert.Throws<UriFormatException>(() => UriTools.IriToUriString(unmatchedSurrogate));
    }

    [Test]
    public void CanonicalizesResourceKeysAndRemovesOnlyFragments()
    {
        Uri value = UriTools.CreateAbsolute("https://EXAMPLE.com:443/a/%7euser/../item?q=%41%2f#view");

        Assert.That(UriTools.CanonicalAbsoluteUri(value), Is.EqualTo("https://example.com/a/item?q=A%2F#view"));
        Assert.That(UriTools.ResourceKey(value), Is.EqualTo("https://example.com/a/item?q=A%2F"));
    }

    [TestCase("file://localhost/c:/Temp/old/../data.xml#part", "file:///C:/Temp/data.xml#part")]
    [TestCase("file://SERVER/share/folder/../data.xml", "file://server/share/data.xml")]
    [TestCase("file:///tmp/a%20b/%7eitem", "file:///tmp/a%20b/~item")]
    public void NormalizesLocalDriveAndNetworkFileUris(string source, string expected)
    {
        Uri value = UriTools.CreateAbsolute(source);

        Assert.That(UriTools.NormalizeFileUri(value), Is.EqualTo(expected));
    }

    [Test]
    public void AppliesExplicitFragmentPolicies()
    {
        Uri value = UriTools.CreateAbsolute("https://example.test/data.xml#section");

        Assert.That(UriTools.ApplyFragmentPolicy(value, UriFragmentPolicy.Preserve).AbsoluteUri, Is.EqualTo("https://example.test/data.xml#section"));
        Assert.That(UriTools.ApplyFragmentPolicy(value, UriFragmentPolicy.Remove).AbsoluteUri, Is.EqualTo("https://example.test/data.xml"));
        Assert.Throws<UriFormatException>(() => UriTools.ApplyFragmentPolicy(value, UriFragmentPolicy.Reject));
        Assert.Throws<UriFormatException>(() => UriTools.ApplyFragmentPolicy(UriTools.CreateAbsolute("https://example.test/data.xml#"), UriFragmentPolicy.Reject));
        Assert.Throws<ArgumentOutOfRangeException>(() => UriTools.ApplyFragmentPolicy(value, (UriFragmentPolicy)3));
    }

    [Test]
    public void RejectsMissingAbsoluteUrisAndRelativeBases()
    {
        Assert.Throws<ArgumentException>(() => UriTools.CreateAbsolute(""));
        Assert.Throws<UriFormatException>(() => UriTools.CreateAbsolute("relative/path.xml"));
        Assert.Throws<ArgumentException>(() => UriTools.ResolveReference(UriTools.CreateReference("relative/"), "child.xml"));
        Assert.Throws<ArgumentException>(() => UriTools.ResourceKey(UriTools.CreateReference("relative.xml")));
    }

    [Test]
    public void BaseStackResolvesScopesAndRestoresParents()
    {
        UriBaseStack stack = new UriBaseStack(UriTools.CreateAbsolute("https://example.test/a/document.xml"));

        Assert.That(stack.PushBaseReference("../shared/").AbsoluteUri, Is.EqualTo("https://example.test/shared/"));
        Assert.That(stack.PushBaseReference("child.xml").AbsoluteUri, Is.EqualTo("https://example.test/shared/child.xml"));
        Assert.That(stack.BaseUriDepth(), Is.EqualTo(3));
        Assert.That(stack.PopBaseUri().AbsoluteUri, Is.EqualTo("https://example.test/shared/child.xml"));
        Assert.That(stack.CurrentBaseUri().AbsoluteUri, Is.EqualTo("https://example.test/shared/"));

        stack.ResetBaseUris();

        Assert.That(stack.BaseUriDepth(), Is.EqualTo(1));
        Assert.That(stack.CurrentBaseUri().AbsoluteUri, Is.EqualTo("https://example.test/a/document.xml"));
        Assert.Throws<InvalidOperationException>(() => stack.PopBaseUri());
        Assert.Throws<UriFormatException>(() => stack.PushBaseReference("child.xml#fragment"));
    }
}
