namespace svg.Tests;

public class SvgUriTests
{
    [Test]
    public void NormalizationAppliesCasePercentPathAndDefaultPortRules()
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue("HTTP://User@Example.COM:80/a/./b/../%63/%7b?q=%7e#F%72");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgUriError.None));
            Assert.That(result.NormalizedIri, Is.EqualTo("http://User@example.com/a/c/%7B?q=~#Fr"));
            Assert.That(result.NormalizedUri, Is.EqualTo("http://User@example.com/a/c/%7B?q=~#Fr"));
            Assert.That(result.ResourceIri, Is.EqualTo("http://User@example.com/a/c/%7B?q=~"));
            Assert.That(result.Fragment, Is.EqualTo("Fr"));
            Assert.That(result.HasFragment, Is.True);
            Assert.That(result.IsAbsolute, Is.True);
            Assert.That(result.CacheKey, Is.EqualTo("http://User@example.com/a/c/%7B?q=~"));
        });
    }

    [Test]
    public void UnicodeIrisUseNfcAndUtf8PercentEscaping()
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue("https://EXAMPLE.test/cafe\u0301/雪");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NormalizedIri, Is.EqualTo("https://example.test/café/雪"));
            Assert.That(result.NormalizedUri, Is.EqualTo("https://example.test/caf%C3%A9/%E9%9B%AA"));
            Assert.That(new SvgUri().EscapeIri("https://example.test/雪"), Is.EqualTo("https://example.test/%E9%9B%AA"));
        });
    }

    [TestCase("g:h", "g:h")]
    [TestCase("g", "http://a/b/c/g")]
    [TestCase("./g", "http://a/b/c/g")]
    [TestCase("g/", "http://a/b/c/g/")]
    [TestCase("/g", "http://a/g")]
    [TestCase("//g", "http://g/")]
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
    [TestCase("../../g", "http://a/g")]
    public void RelativeResolutionMatchesRfc3986Examples(string reference, string expected)
    {
        SvgUriResult result = new SvgUri().ResolveWithBaseUriAndReference("http://a/b/c/d;p?q", reference);

        Assert.That(result.Success, Is.True);
        Assert.That(result.NormalizedIri, Is.EqualTo(expected));
    }

    [Test]
    public void EncodedUnreservedDotsParticipateInPathNormalization()
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue("https://example.test/a/%2e/b/%2E%2e/c");
        SvgUriResult unresolved = new SvgUri().NormalizeWithValue("../café");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NormalizedIri, Is.EqualTo("https://example.test/a/c"));
            Assert.That(unresolved.Success, Is.True);
            Assert.That(unresolved.NormalizedIri, Is.EqualTo("../café"));
            Assert.That(unresolved.NormalizedUri, Is.EqualTo("../caf%C3%A9"));
        });
    }

    [Test]
    public void EmptyQueryAndFragmentDelimitersRemainSignificant()
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue("http://example.test?#");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NormalizedIri, Is.EqualTo("http://example.test/?#"));
            Assert.That(result.ResourceIri, Is.EqualTo("http://example.test/?"));
            Assert.That(result.HasFragment, Is.True);
            Assert.That(result.Fragment, Is.Empty);
            Assert.That(result.CacheKey, Is.EqualTo("http://example.test/?"));
        });
    }

    [Test]
    public void FileUrisDistinguishLocalAndRemoteAuthorities()
    {
        SvgUriResult local = new SvgUri().NormalizeWithValue("FILE://LOCALHOST/c:/Temp/../A%20B.svg#icon");
        SvgUriResult remote = new SvgUri().NormalizeWithValue("file://SERVER/share/image.svg");

        Assert.Multiple(() =>
        {
            Assert.That(local.Success, Is.True);
            Assert.That(local.NormalizedIri, Is.EqualTo("file:///C:/A%20B.svg#icon"));
            Assert.That(local.IsFile, Is.True);
            Assert.That(local.IsLocalFile, Is.True);
            Assert.That(local.CacheKey, Is.EqualTo("file:///C:/A%20B.svg"));
            Assert.That(remote.Success, Is.True);
            Assert.That(remote.NormalizedIri, Is.EqualTo("file://server/share/image.svg"));
            Assert.That(remote.IsLocalFile, Is.False);
        });
    }

    [TestCase("file:relative/path")]
    [TestCase("file://user@localhost/path")]
    [TestCase("file://localhost:80/path")]
    public void InvalidFileUriFormsAreRejected(string value)
    {
        SvgUriResult result = new SvgUri().NormalizeWithValue(value);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(SvgUriError.InvalidFileUri));
    }

    [Test]
    public void InvalidSyntaxPercentEscapesAndBoundsHaveStableFailures()
    {
        SvgUriResult percent = new SvgUri().NormalizeWithValue("https://example.test/a%2G");
        SvgUriResult space = new SvgUri().NormalizeWithValue("https://example.test/a b");
        SvgUriResult colon = new SvgUri().NormalizeWithValue("1bad:relative");
        SvgUriResult inputLimit = new SvgUri().NormalizeWithValueAndMaxCharacters("abcdef", 5);
        SvgUriResult outputLimit = new SvgUri().NormalizeWithValueAndMaxCharacters("é", 1);

        Assert.Multiple(() =>
        {
            Assert.That(percent.Error, Is.EqualTo(SvgUriError.InvalidPercentEscape));
            Assert.That(space.Error, Is.EqualTo(SvgUriError.InvalidSyntax));
            Assert.That(colon.Error, Is.EqualTo(SvgUriError.InvalidSyntax));
            Assert.That(inputLimit.Error, Is.EqualTo(SvgUriError.TooLong));
            Assert.That(outputLimit.Error, Is.EqualTo(SvgUriError.TooLong));
        });
    }

    [Test]
    public void FragmentDecodingAndCacheKeysUseResolvedResourceIdentity()
    {
        string keyOne = new SvgUri().GetCacheKey("HTTP://EXAMPLE.test:80/a/", "../b#one");
        string keyTwo = new SvgUri().GetCacheKey("http://example.test/", "b#two");

        Assert.Multiple(() =>
        {
            Assert.That(keyOne, Is.EqualTo("http://example.test/b"));
            Assert.That(keyTwo, Is.EqualTo(keyOne));
            Assert.That(new SvgUri().DecodeFragment("#a%20b%E2%9C%93"), Is.EqualTo("a b✓"));
            Assert.That(new SvgUri().DecodeFragment("#bad%2G"), Is.Empty);
        });
    }
}
