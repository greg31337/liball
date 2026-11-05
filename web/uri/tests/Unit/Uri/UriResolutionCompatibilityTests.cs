namespace uri.Tests;

public class UriResolutionCompatibilityTests
{
    [Test]
    public void ResolutionOptionsDefaultToStrictAndAllowExplicitCompatibility()
    {
        UriResolutionOptions first = new UriResolutionOptions();
        UriResolutionOptions second = new UriResolutionOptions();

        first.Mode = UriResolutionMode.Compatible;
        first.Limits.MaxInputCharacters = 12;

        Assert.Multiple(() =>
        {
            Assert.That(first.Mode, Is.EqualTo(UriResolutionMode.Compatible));
            Assert.That(second.Mode, Is.EqualTo(UriResolutionMode.Strict));
            Assert.That(first.Limits.MaxInputCharacters, Is.EqualTo(12));
            Assert.That(second.Limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
            Assert.That(
                Enum.GetValues<UriResolutionMode>(),
                Is.EqualTo(new UriResolutionMode[]
                {
                    UriResolutionMode.Strict,
                    UriResolutionMode.Compatible
                }));
        });
    }

    [TestCase("http:g", "http:g", "http://a/b/c/g")]
    [TestCase("HTTP:g", "HTTP:g", "http://a/b/c/g")]
    [TestCase("http:/g", "http:/g", "http://a/g")]
    [TestCase("http:?y", "http:?y", "http://a/b/c/d;p?y")]
    [TestCase("http:#s", "http:#s", "http://a/b/c/d;p?q#s")]
    [TestCase("http:", "http:", "http://a/b/c/d;p?q")]
    [TestCase("http:../../../g", "http:g", "http://a/g")]
    public void IdenticalSchemeReferencesDifferOnlyInCompatibleMode(
        string referenceText,
        string strictExpected,
        string compatibleExpected)
    {
        UriReference baseReference = Parse("http://a/b/c/d;p?q");
        UriReference reference = Parse(referenceText);

        UriReference strict = UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Strict);
        UriReference compatible = UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Compatible);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(strict), Is.EqualTo(strictExpected));
            Assert.That(Compose(compatible), Is.EqualTo(compatibleExpected));
        });
    }

    [TestCase("https:g")]
    [TestCase("httpx:g")]
    [TestCase("ftp:g")]
    public void DifferentReferenceSchemesRemainAbsoluteInBothModes(
        string referenceText)
    {
        UriReference baseReference = Parse("http://a/b/c/d;p?q");
        UriReference reference = Parse(referenceText);

        string strict = Compose(UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Strict));
        string compatible = Compose(UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Compatible));

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.EqualTo(referenceText));
            Assert.That(compatible, Is.EqualTo(referenceText));
        });
    }

    [TestCase("g", "http://a/b/c/g")]
    [TestCase("//g", "http://g")]
    [TestCase("/g", "http://a/g")]
    [TestCase("?y", "http://a/b/c/d;p?y")]
    [TestCase("#s", "http://a/b/c/d;p?q#s")]
    public void ReferencesWithoutSchemesResolveIdenticallyInBothModes(
        string referenceText,
        string expected)
    {
        UriReference baseReference = Parse("http://a/b/c/d;p?q");
        UriReference reference = Parse(referenceText);

        string strict = Compose(UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Strict));
        string compatible = Compose(UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Compatible));

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.EqualTo(expected));
            Assert.That(compatible, Is.EqualTo(expected));
        });
    }

    [Test]
    public void SchemeIdentityUsesAsciiCaseInsensitiveComparison()
    {
        UriReference baseReference = Parse("HtTp://a/b/c/d;p?q");
        UriReference reference = Parse("hTtP:g");

        string strict = Compose(UriResolverCore.TransformStrictReference(
            baseReference,
            reference));
        string compatible = Compose(UriResolverCore.TransformCompatibleReference(
            baseReference,
            reference));

        Assert.Multiple(() =>
        {
            Assert.That(strict, Is.EqualTo("hTtP:g"));
            Assert.That(compatible, Is.EqualTo("HtTp://a/b/c/g"));
        });
    }

    [Test]
    public void CompatibleAuthorityReferenceInheritsBaseSchemeSpelling()
    {
        UriReference baseReference = Parse("http://base.example/a/b?base");
        UriReference reference = Parse(
            "HTTP://user@[2001:db8::1]:80/x/../y?query#fragment");

        string strict = Compose(UriResolverCore.TransformStrictReference(
            baseReference,
            reference));
        string compatible = Compose(UriResolverCore.TransformCompatibleReference(
            baseReference,
            reference));

        Assert.Multiple(() =>
        {
            Assert.That(
                strict,
                Is.EqualTo("HTTP://user@[2001:db8::1]:80/y?query#fragment"));
            Assert.That(
                compatible,
                Is.EqualTo("http://user@[2001:db8::1]:80/y?query#fragment"));
        });
    }

    [Test]
    public void SchemeSuppressionUsesADeepCopyAndDoesNotMutateInputs()
    {
        UriReference baseReference = Parse("http://base.example/a/b?base");
        UriReference reference = Parse(
            "HTTP://user@[2001:db8::1]:80/x?query#fragment");
        string baseBefore = Compose(baseReference);
        string referenceBefore = Compose(reference);

        UriReference schemeLess = UriResolverCore.CloneWithoutScheme(reference);
        UriReference target = UriResolverCore.TransformCompatibleReference(
            baseReference,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(schemeLess.HasScheme, Is.False);
            Assert.That(schemeLess.Scheme, Is.Empty);
            Assert.That(schemeLess.Kind, Is.EqualTo(UriReferenceKind.NetworkPathReference));
            Assert.That(schemeLess.Authority, Is.Not.SameAs(reference.Authority));
            Assert.That(schemeLess.Authority.AddressBytes, Is.Not.SameAs(reference.Authority.AddressBytes));
            Assert.That(Compose(target), Is.EqualTo("http://user@[2001:db8::1]:80/x?query#fragment"));
            Assert.That(Compose(baseReference), Is.EqualTo(baseBefore));
            Assert.That(Compose(reference), Is.EqualTo(referenceBefore));
        });

        schemeLess.Authority.AddressBytes[0] = 255;
        target.Authority.UserInfo = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(reference.Authority.AddressBytes[0], Is.EqualTo(32));
            Assert.That(reference.Authority.UserInfo, Is.EqualTo("user"));
        });
    }

    [Test]
    public void ModeDispatcherRejectsUndefinedValuesWithoutChangingStrictBehavior()
    {
        UriReference baseReference = Parse("http://a/b/c/d;p?q");
        UriReference reference = Parse("http:g");

        string directStrict = Compose(UriResolverCore.TransformStrictReference(
            baseReference,
            reference));
        string selectedStrict = Compose(UriResolverCore.TransformReferenceWithMode(
            baseReference,
            reference,
            UriResolutionMode.Strict));

        Assert.Multiple(() =>
        {
            Assert.That(directStrict, Is.EqualTo("http:g"));
            Assert.That(selectedStrict, Is.EqualTo(directStrict));
            Assert.Throws<ArgumentException>(() =>
                UriResolverCore.TransformReferenceWithMode(
                    baseReference,
                    reference,
                    (UriResolutionMode)99));
        });
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
