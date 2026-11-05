namespace uri.Tests;

public class Rfc3986SchemeProfileTests
{
    [TestCase("http://example.com")]
    [TestCase("http://example.com:")]
    [TestCase("http://example.com:80")]
    [TestCase("https://example.com:443")]
    [TestCase("ws://example.com:80")]
    [TestCase("wss://example.com:443")]
    [TestCase("ftp://example.com:21")]
    [TestCase("http://")]
    public void GenericNormalizationPreservesEveryProfileSensitiveForm(string input)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        string actual = normalizer.NormalizeSyntax(
            input,
            new UriNormalizationOptions());

        Assert.That(actual, Is.EqualTo(input));
    }

    [TestCase("HTTP://EXAMPLE.COM", "http://example.com/")]
    [TestCase("http://example.com:", "http://example.com/")]
    [TestCase("http://example.com:80", "http://example.com/")]
    [TestCase("http://example.com:080", "http://example.com/")]
    [TestCase("HTTPS://EXAMPLE.COM", "https://example.com/")]
    [TestCase("https://example.com:", "https://example.com/")]
    [TestCase("https://example.com:443", "https://example.com/")]
    [TestCase("https://example.com:0443", "https://example.com/")]
    [TestCase("WS://EXAMPLE.COM", "ws://example.com/")]
    [TestCase("ws://example.com:", "ws://example.com/")]
    [TestCase("ws://example.com:80", "ws://example.com/")]
    [TestCase("WSS://EXAMPLE.COM", "wss://example.com/")]
    [TestCase("wss://example.com:", "wss://example.com/")]
    [TestCase("wss://example.com:443", "wss://example.com/")]
    [TestCase("FTP://EXAMPLE.COM", "ftp://example.com/")]
    [TestCase("ftp://example.com:", "ftp://example.com/")]
    [TestCase("ftp://example.com:21", "ftp://example.com/")]
    public void BuiltInProfilesNormalizeEmptyPathsAndEmptyOrDefaultPorts(
        string input,
        string expected)
    {
        AssertBuiltInNormalization(input, expected);
    }

    [TestCase("http://example.com:81", "http://example.com:81/")]
    [TestCase("https://example.com:80", "https://example.com:80/")]
    [TestCase("ws://example.com:443", "ws://example.com:443/")]
    [TestCase("wss://example.com:80", "wss://example.com:80/")]
    [TestCase("ftp://example.com:20", "ftp://example.com:20/")]
    [TestCase("http://example.com:999999999999999999999", "http://example.com:999999999999999999999/")]
    public void BuiltInProfilesPreserveNondefaultPorts(
        string input,
        string expected)
    {
        AssertBuiltInNormalization(input, expected);
    }

    [TestCase("http://", "http:///")]
    [TestCase("http://user@", "http://user@/")]
    [TestCase("http://:80", "http:///")]
    [TestCase("http://example.com?query", "http://example.com/?query")]
    [TestCase("http://example.com#fragment", "http://example.com/#fragment")]
    [TestCase("http://example.com/path", "http://example.com/path")]
    public void EmptyAuthorityPathRuleDependsOnPathNotHostContents(
        string input,
        string expected)
    {
        AssertBuiltInNormalization(input, expected);
    }

    [Test]
    public void ExplicitProfileRulesAreIndependentlySelectable()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions pathOptions = CreateOptions("x");
        pathOptions.Profile!.NormalizeEmptyAuthorityPath = true;
        UriNormalizationOptions emptyPortOptions = CreateOptions("x");
        emptyPortOptions.Profile!.RemoveEmptyPort = true;
        UriNormalizationOptions defaultPortOptions = CreateOptions("x");
        defaultPortOptions.Profile!.HasDefaultPort = true;
        defaultPortOptions.Profile.DefaultPort = 80;

        string pathOnly = normalizer.NormalizeWithProfile("x://host:80", pathOptions);
        string emptyPortOnly = normalizer.NormalizeWithProfile("x://host:", emptyPortOptions);
        string defaultPortOnly = normalizer.NormalizeWithProfile("x://host:080", defaultPortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pathOnly, Is.EqualTo("x://host:80/"));
            Assert.That(emptyPortOnly, Is.EqualTo("x://host"));
            Assert.That(defaultPortOnly, Is.EqualTo("x://host"));
        });
    }

    [TestCase("x://host:0", "x://host")]
    [TestCase("x://host:000", "x://host")]
    [TestCase("x://host:1", "x://host:1")]
    [TestCase("x://host:", "x://host:")]
    public void ExplicitZeroDefaultPortUsesNumericEquivalence(
        string input,
        string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions options = CreateOptions("x");
        options.Profile!.HasDefaultPort = true;
        options.Profile.DefaultPort = 0;

        string actual = normalizer.NormalizeWithProfile(input, options);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("http://host", "http://host/", UriComponent.Path)]
    [TestCase("http://host:", "http://host/", UriComponent.Port)]
    [TestCase("http://host:80", "http://host/", UriComponent.Port)]
    [TestCase("https://host:0443", "https://host/", UriComponent.Port)]
    [TestCase("ftp://host:21", "ftp://host/", UriComponent.Port)]
    public void ProfileComparisonAddsOnlyTheDeclaredEquivalences(
        string left,
        string right,
        UriComponent syntaxDifference)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult syntax = comparer.CompareSyntax(
            left,
            right,
            new UriComparisonOptions());
        UriComparisonResult profile = comparer.CompareWithProfile(
            left,
            right,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertDifference(syntax, UriComparisonLevel.Syntax, syntaxDifference);
            AssertEquality(profile, UriComparisonLevel.SchemeProfile);
        });
    }

    [TestCase("x://host", "x://host/", UriComponent.Path)]
    [TestCase("//host", "//host/", UriComponent.Path)]
    [TestCase("https://host:80", "https://host/", UriComponent.Port)]
    [TestCase("http://host:443", "http://host/", UriComponent.Port)]
    [TestCase("http:path", "http:/path", UriComponent.Path)]
    public void BuiltInProfileComparisonDoesNotInventUnselectedEquivalence(
        string left,
        string right,
        UriComponent difference)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareWithProfile(
            left,
            right,
            new UriComparisonOptions());

        AssertDifference(result, UriComparisonLevel.SchemeProfile, difference);
    }

    [TestCase("https://host:80", "https://host:80")]
    [TestCase("//host:80", "//host:80")]
    [TestCase("x://host:80", "x://host:80")]
    public void ExplicitProfilesRequireTheirMatchingScheme(
        string input,
        string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions options = CreateOptions("http");
        options.Profile!.NormalizeEmptyAuthorityPath = true;
        options.Profile.RemoveEmptyPort = true;
        options.Profile.HasDefaultPort = true;
        options.Profile.DefaultPort = 80;

        string actual = normalizer.NormalizeWithProfile(input, options);

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static UriNormalizationOptions CreateOptions(string scheme)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = scheme;
        UriNormalizationOptions options = new UriNormalizationOptions();
        options.Profile = profile;
        return options;
    }

    private static void AssertBuiltInNormalization(string input, string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        string actual = normalizer.NormalizeWithProfile(
            input,
            new UriNormalizationOptions());

        Assert.That(actual, Is.EqualTo(expected), input);
    }

    private static void AssertEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.True);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(component));
        Assert.That(result.Diagnostic, Is.Null);
    }
}
