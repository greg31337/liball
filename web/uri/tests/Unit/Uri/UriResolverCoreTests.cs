namespace uri.Tests;

public class UriResolverCoreTests
{
    [TestCase("http://a", "g", "/g")]
    [TestCase("http://a/", "g", "/g")]
    [TestCase("http://a/b/c/d;p", "g", "/b/c/g")]
    [TestCase("http://a/b/c/", "g", "/b/c/g")]
    [TestCase("http://a//b", "g", "//g")]
    [TestCase("x:a/b/c", "g", "a/b/g")]
    [TestCase("x:a", "g", "g")]
    [TestCase("x:", "g", "g")]
    public void MergePathsUsesTheAuthorityAndRightmostSlashRules(
        string baseText,
        string referencePath,
        string expected)
    {
        UriReference baseReference = Parse(baseText);

        string merged = UriResolverCore.MergePaths(
            baseReference,
            referencePath);

        Assert.That(merged, Is.EqualTo(expected));
    }

    [TestCase("g:h", "g:h")]
    [TestCase("//g", "http://g")]
    [TestCase("/g", "http://a/g")]
    [TestCase("g", "http://a/b/c/g")]
    [TestCase("./g", "http://a/b/c/g")]
    [TestCase("g/../h", "http://a/b/c/h")]
    [TestCase("../../../g", "http://a/g")]
    [TestCase("?y", "http://a/b/c/d;p?y")]
    [TestCase("#s", "http://a/b/c/d;p?q#s")]
    [TestCase("g?y#s", "http://a/b/c/g?y#s")]
    public void StrictTransformationCoversEveryRfcBranch(
        string referenceText,
        string expected)
    {
        AssertResolution(
            "http://a/b/c/d;p?q",
            referenceText,
            expected);
    }

    [Test]
    public void ReferenceSchemeReplacesEveryBaseComponent()
    {
        AssertResolution(
            "http://base.example/a/b?base",
            "HTTPS://User@OTHER.example:443/x/../y?reference#fragment",
            "HTTPS://User@OTHER.example:443/y?reference#fragment");
    }

    [Test]
    public void SameSchemeReferenceRemainsAbsoluteInStrictMode()
    {
        AssertResolution(
            "http://a/b/c/d;p?q",
            "http:g",
            "http:g");
    }

    [Test]
    public void NetworkPathReferenceReplacesAuthorityPathAndQuery()
    {
        AssertResolution(
            "http://base.example/a/b?base",
            "//User@[2001:0DB8::1]:080/a/../b?#",
            "http://User@[2001:0DB8::1]:080/b?#");
    }

    [TestCase("", "http://a/b/c/d;p?q")]
    [TestCase("?", "http://a/b/c/d;p?")]
    [TestCase("?new", "http://a/b/c/d;p?new")]
    [TestCase("#", "http://a/b/c/d;p?q#")]
    [TestCase("#new", "http://a/b/c/d;p?q#new")]
    [TestCase("?#", "http://a/b/c/d;p?#")]
    public void EmptyReferencePathInheritsBasePathAndOnlyUndefinedQuery(
        string referenceText,
        string expected)
    {
        AssertResolution(
            "http://a/b/c/d;p?q",
            referenceText,
            expected);
    }

    [TestCase("/a/./b/../c", "http://a/a/c")]
    [TestCase("/../g", "http://a/g")]
    [TestCase("/a//b/../c", "http://a/a//c")]
    public void AbsoluteReferencePathReplacesBasePathBeforeDotRemoval(
        string referenceText,
        string expected)
    {
        AssertResolution(
            "http://a/base/path?query",
            referenceText,
            expected);
    }

    [TestCase("%2e/%2e%2e/g", "http://a/b/c/%2e/%2e%2e/g")]
    [TestCase("g?y/./x#s/../z", "http://a/b/c/g?y/./x#s/../z")]
    [TestCase("g%2fh/../i", "http://a/b/c/i")]
    public void ResolutionDoesNotDecodeEncodedDotsOrProcessQueryAndFragmentPaths(
        string referenceText,
        string expected)
    {
        AssertResolution(
            "http://a/b/c/d;p?q",
            referenceText,
            expected);
    }

    [TestCase("x://authority", "relative", "x://authority/relative")]
    [TestCase("x://authority", "a/../b", "x://authority/b")]
    [TestCase("x:base", "relative", "x:relative")]
    [TestCase("x:base/path", "relative", "x:base/relative")]
    public void RelativeMergeHandlesEmptyAuthorityAndRootlessBasePaths(
        string baseText,
        string referenceText,
        string expected)
    {
        AssertResolution(baseText, referenceText, expected);
    }

    [Test]
    public void TransformedTargetDeepCopiesInheritedAuthority()
    {
        UriReference baseReference = Parse(
            "http://user@[2001:db8::1]:80/a/b?base");
        UriReference reference = Parse("../c#fragment");
        string baseBefore = Compose(baseReference);
        string referenceBefore = Compose(reference);

        UriReference target = UriResolverCore.TransformStrictReference(
            baseReference,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(target), Is.EqualTo("http://user@[2001:db8::1]:80/c#fragment"));
            Assert.That(target.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(target.OriginalText, Is.Empty);
            Assert.That(target.Authority, Is.Not.SameAs(baseReference.Authority));
            Assert.That(target.Authority.AddressBytes, Is.Not.SameAs(baseReference.Authority.AddressBytes));
            Assert.That(target.Authority.AddressBytes, Is.EqualTo(baseReference.Authority.AddressBytes));
            Assert.That(Compose(baseReference), Is.EqualTo(baseBefore));
            Assert.That(Compose(reference), Is.EqualTo(referenceBefore));
        });

        target.Authority.AddressBytes[0] = 255;
        target.Authority.UserInfo = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(baseReference.Authority.AddressBytes[0], Is.EqualTo(32));
            Assert.That(baseReference.Authority.UserInfo, Is.EqualTo("user"));
        });
    }

    [Test]
    public void TransformedTargetDeepCopiesReplacementAuthority()
    {
        UriReference baseReference = Parse("http://base/a");
        UriReference reference = Parse("//other:81/b");

        UriReference target = UriResolverCore.TransformStrictReference(
            baseReference,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(target), Is.EqualTo("http://other:81/b"));
            Assert.That(target.Authority, Is.Not.SameAs(reference.Authority));
            Assert.That(target.Authority.AddressBytes, Is.Not.SameAs(reference.Authority.AddressBytes));
        });

        target.Authority.Host = "changed";
        Assert.That(reference.Authority.Host, Is.EqualTo("other"));
    }

    [Test]
    public void ResolutionCoreRejectsMissingTransformationInputs()
    {
        UriReference reference = Parse("x:a");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.MergePaths(null, "a"));
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.MergePaths(reference, null));
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.TransformStrictReference(null, reference));
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.TransformStrictReference(reference, null));
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.CloneAuthority(null));
        });
    }

    private static void AssertResolution(
        string baseText,
        string referenceText,
        string expected)
    {
        UriReference baseReference = Parse(baseText);
        UriReference reference = Parse(referenceText);

        UriReference target = UriResolverCore.TransformStrictReference(
            baseReference,
            reference);

        Assert.That(Compose(target), Is.EqualTo(expected), referenceText);
    }

    private static UriReference Parse(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }

    private static string Compose(UriReference reference)
    {
        UriCompositionResult result = UriComposerCore.RecomposeParsedReference(
            reference,
            new OperationLimits());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.Text;
    }
}
