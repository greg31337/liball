namespace uri.Tests;

public class UriSchemeProfileNormalizationTests
{
    [Test]
    public void ProfileAndNormalizationOptionsHaveNoBehaviorEnabledByDefault()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        UriNormalizationOptions options = new UriNormalizationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(profile.Scheme, Is.Empty);
            Assert.That(profile.NormalizeEmptyAuthorityPath, Is.False);
            Assert.That(profile.RemoveEmptyPort, Is.False);
            Assert.That(profile.HasDefaultPort, Is.False);
            Assert.That(profile.DefaultPort, Is.Zero);
            Assert.That(profile.RemoveEmptyQuery, Is.False);
            Assert.That(profile.RemoveEmptyFragment, Is.False);
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.Profile, Is.Null);
        });

        options.Profile = profile;
        Assert.That(options.Profile, Is.SameAs(profile));
    }

    [Test]
    public void EnabledProfileRulesApplyAfterGenericSyntaxNormalization()
    {
        UriSchemeProfile profile = CreateHttpProfile();

        string normalized = NormalizeText("HTTP://EXAMPLE%2eCOM:080?#", profile);

        Assert.That(normalized, Is.EqualTo("http://example.com/"));
    }

    [TestCase("x://host", "x://host/")]
    [TestCase("x://host/a", "x://host/a")]
    [TestCase("x:path", "x:path")]
    [TestCase("x:", "x:")]
    public void EmptyAuthorityPathRuleAppliesOnlyToAnAuthorityWithAnEmptyPath(
        string input,
        string expected)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.NormalizeEmptyAuthorityPath = true;

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [TestCase("x://host:", "x://host")]
    [TestCase("x://host:0", "x://host:0")]
    [TestCase("x://host:80", "x://host:80")]
    public void EmptyPortRemovalIsSeparatelySelectable(
        string input,
        string expected)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.RemoveEmptyPort = true;

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [TestCase("x://host:80", "x://host")]
    [TestCase("x://host:080", "x://host")]
    [TestCase("x://host:00080", "x://host")]
    [TestCase("x://host:81", "x://host:81")]
    [TestCase("x://host:", "x://host:")]
    [TestCase("x://host:999999999999999999999999", "x://host:999999999999999999999999")]
    public void DeclaredDefaultPortUsesNumericPortEquivalenceWithoutOverflow(
        string input,
        string expected)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.HasDefaultPort = true;
        profile.DefaultPort = 80;

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [TestCase("x://host:0", "x://host")]
    [TestCase("x://host:000", "x://host")]
    [TestCase("x://host:1", "x://host:1")]
    public void ZeroCanBeDeclaredAsADefaultPort(string input, string expected)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.HasDefaultPort = true;
        profile.DefaultPort = 0;

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [TestCase("x:?", "x:")]
    [TestCase("x:#", "x:")]
    [TestCase("x:?#", "x:")]
    [TestCase("x:?value#", "x:?value")]
    [TestCase("x:?#value", "x:#value")]
    [TestCase("x:?value#value", "x:?value#value")]
    public void EmptyQueryAndFragmentCleanupNeverRemovesNonemptyValues(
        string input,
        string expected)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [Test]
    public void DisabledRulesPreserveEveryProfileSensitiveDistinction()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.DefaultPort = 80;

        string normalized = NormalizeText("X://EXAMPLE.COM:80?#", profile);

        Assert.That(normalized, Is.EqualTo("x://example.com:80?#"));
    }

    [TestCase("HTTPS://EXAMPLE.COM:080?#", "https://example.com:080?#")]
    [TestCase("//EXAMPLE.COM:080/a/./b?#", "//example.com:080/a/b?#")]
    public void ProfileRulesDoNotApplyWithoutAMatchingScheme(
        string input,
        string expected)
    {
        UriSchemeProfile profile = CreateHttpProfile();

        Assert.That(NormalizeText(input, profile), Is.EqualTo(expected));
    }

    [Test]
    public void ProfileSchemeComparisonUsesAsciiCaseInsensitivity()
    {
        UriSchemeProfile profile = CreateHttpProfile();
        profile.Scheme = "HtTp";

        string normalized = NormalizeText("HTTP://EXAMPLE.COM:80", profile);

        Assert.That(normalized, Is.EqualTo("http://example.com/"));
    }

    [Test]
    public void ProfileNormalizationReturnsADeepCloneWithoutMutatingInputs()
    {
        UriSchemeProfile profile = CreateHttpProfile();
        UriReference source = Parse("HTTP://[2001:0DB8:0:0:0:0:0:1]:080?#");
        byte[] originalAddress = (byte[])source.Authority.AddressBytes.Clone();

        UriReference normalized = UriNormalizerCore.NormalizeWithSchemeProfile(
            source,
            profile);

        Assert.Multiple(() =>
        {
            Assert.That(Compose(normalized), Is.EqualTo("http://[2001:db8::1]/"));
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(normalized.Authority, Is.Not.SameAs(source.Authority));
            Assert.That(normalized.Authority.AddressBytes, Is.Not.SameAs(source.Authority.AddressBytes));
            Assert.That(source.Authority.AddressBytes, Is.EqualTo(originalAddress));
            Assert.That(source.Scheme, Is.EqualTo("HTTP"));
            Assert.That(source.Authority.Host, Is.EqualTo("2001:0DB8:0:0:0:0:0:1"));
            Assert.That(source.Authority.Port, Is.EqualTo("080"));
            Assert.That(source.HasQuery, Is.True);
            Assert.That(source.HasFragment, Is.True);
            Assert.That(profile.Scheme, Is.EqualTo("http"));
            Assert.That(profile.DefaultPort, Is.EqualTo(80));
        });
    }

    [TestCase("HTTP://EXAMPLE.COM:080?#")]
    [TestCase("http://example.com")]
    [TestCase("http://example.com/path?value#value")]
    public void ProfileNormalizationIsIdempotent(string input)
    {
        UriSchemeProfile profile = CreateHttpProfile();
        string first = NormalizeText(input, profile);
        string second = NormalizeText(first, profile);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void ProfileNormalizationRejectsMissingInputs()
    {
        UriSchemeProfile profile = CreateHttpProfile();
        UriReference reference = Parse("http://example.com");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeWithSchemeProfile(null, profile));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeWithSchemeProfile(reference, null));
        });
    }

    private static UriSchemeProfile CreateHttpProfile()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "http";
        profile.NormalizeEmptyAuthorityPath = true;
        profile.RemoveEmptyPort = true;
        profile.HasDefaultPort = true;
        profile.DefaultPort = 80;
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;
        return profile;
    }

    private static string NormalizeText(string text, UriSchemeProfile profile)
    {
        UriReference normalized = UriNormalizerCore.NormalizeWithSchemeProfile(
            Parse(text),
            profile);
        return Compose(normalized);
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
