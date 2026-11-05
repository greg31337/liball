namespace uri.Tests;

public class Rfc3986Section54ResolutionTests
{
    private const string BaseUri = "http://a/b/c/d;p?q";

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
    public void SectionFiveFourOneNormalVectorsResolveInBothModes(
        string reference,
        string expected)
    {
        AssertBothModes(reference, expected, expected);
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
    [TestCase("g?y/../x", "http://a/b/c/g?y/../x")]
    [TestCase("g#s/./x", "http://a/b/c/g#s/./x")]
    [TestCase("g#s/../x", "http://a/b/c/g#s/../x")]
    public void SectionFiveFourTwoAbnormalVectorsResolveInBothModes(
        string reference,
        string expected)
    {
        AssertBothModes(reference, expected, expected);
    }

    [Test]
    public void SectionFiveFourTwoSameSchemeLoopholeIsModeSpecific()
    {
        AssertBothModes(
            "http:g",
            "http:g",
            "http://a/b/c/g");
    }

    private static void AssertBothModes(
        string reference,
        string strictExpected,
        string compatibleExpected)
    {
        IUriResolver resolver = new UriResolver();
        UriResolutionOptions strictOptions = new UriResolutionOptions();
        UriResolutionOptions compatibleOptions = new UriResolutionOptions();
        compatibleOptions.Mode = UriResolutionMode.Compatible;

        UriTextResult strict = resolver.TryResolveReference(
            BaseUri,
            reference,
            strictOptions);
        UriTextResult compatible = resolver.TryResolveReference(
            BaseUri,
            reference,
            compatibleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.True, strict.Diagnostic?.Message);
            Assert.That(strict.Text, Is.EqualTo(strictExpected), "strict " + reference);
            Assert.That(strict.Length, Is.EqualTo(strictExpected.Length));
            Assert.That(strict.Diagnostic, Is.Null);
            Assert.That(compatible.Success, Is.True, compatible.Diagnostic?.Message);
            Assert.That(compatible.Text, Is.EqualTo(compatibleExpected), "compatible " + reference);
            Assert.That(compatible.Length, Is.EqualTo(compatibleExpected.Length));
            Assert.That(compatible.Diagnostic, Is.Null);
        });
    }
}
