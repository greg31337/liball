namespace uri.Tests;

public class BuiltInUriSchemeProfileTests
{
    [TestCase("http", "http", 80)]
    [TestCase("https", "https", 443)]
    [TestCase("ws", "ws", 80)]
    [TestCase("wss", "wss", 443)]
    [TestCase("ftp", "ftp", 21)]
    public void BuiltInProfilesUseConstantSchemeAndDefaultPortData(
        string requestedScheme,
        string expectedScheme,
        int expectedPort)
    {
        UriSchemeProfile? profile = UriNormalizerCore.CreateBuiltInSchemeProfile(
            requestedScheme);

        Assert.That(profile, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(profile!.Scheme, Is.EqualTo(expectedScheme));
            Assert.That(profile.NormalizeEmptyAuthorityPath, Is.True);
            Assert.That(profile.RemoveEmptyPort, Is.True);
            Assert.That(profile.HasDefaultPort, Is.True);
            Assert.That(profile.DefaultPort, Is.EqualTo(expectedPort));
            Assert.That(profile.RemoveEmptyQuery, Is.False);
            Assert.That(profile.RemoveEmptyFragment, Is.False);
        });
    }

    [TestCase("HTTP", "http", 80)]
    [TestCase("Https", "https", 443)]
    [TestCase("WS", "ws", 80)]
    [TestCase("WsS", "wss", 443)]
    [TestCase("FTP", "ftp", 21)]
    public void BuiltInLookupUsesAsciiCaseInsensitiveSchemeNames(
        string requestedScheme,
        string expectedScheme,
        int expectedPort)
    {
        UriSchemeProfile? profile = UriNormalizerCore.CreateBuiltInSchemeProfile(
            requestedScheme);

        Assert.Multiple(() =>
        {
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile!.Scheme, Is.EqualTo(expectedScheme));
            Assert.That(profile.DefaultPort, Is.EqualTo(expectedPort));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("x")]
    [TestCase("httpx")]
    [TestCase("İttp")]
    public void UnsupportedSchemesDoNotProduceABuiltInProfile(string? scheme)
    {
        UriSchemeProfile? profile = UriNormalizerCore.CreateBuiltInSchemeProfile(scheme);

        Assert.That(profile, Is.Null);
    }

    [Test]
    public void EveryLookupReturnsAnIndependentProfileInstance()
    {
        UriSchemeProfile first = GetProfile("http");
        UriSchemeProfile second = GetProfile("http");
        first.Scheme = "changed";
        first.DefaultPort = 9999;
        first.NormalizeEmptyAuthorityPath = false;
        first.RemoveEmptyPort = false;
        first.RemoveEmptyQuery = true;
        first.RemoveEmptyFragment = true;

        UriSchemeProfile third = GetProfile("http");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(third, Is.Not.SameAs(first));
            Assert.That(third, Is.Not.SameAs(second));
            Assert.That(second.Scheme, Is.EqualTo(UriSchemeDefaults.HttpScheme));
            Assert.That(second.DefaultPort, Is.EqualTo(UriSchemeDefaults.HttpDefaultPort));
            Assert.That(second.NormalizeEmptyAuthorityPath, Is.True);
            Assert.That(second.RemoveEmptyPort, Is.True);
            Assert.That(second.RemoveEmptyQuery, Is.False);
            Assert.That(second.RemoveEmptyFragment, Is.False);
            Assert.That(third.Scheme, Is.EqualTo(UriSchemeDefaults.HttpScheme));
            Assert.That(third.DefaultPort, Is.EqualTo(UriSchemeDefaults.HttpDefaultPort));
        });
    }

    [TestCase("HTTP://EXAMPLE.COM:80", "http://example.com/")]
    [TestCase("http://example.com:080", "http://example.com/")]
    [TestCase("HTTPS://EXAMPLE.COM:443", "https://example.com/")]
    [TestCase("https://example.com:0443", "https://example.com/")]
    [TestCase("WS://EXAMPLE.COM:80", "ws://example.com/")]
    [TestCase("WSS://EXAMPLE.COM:443", "wss://example.com/")]
    [TestCase("FTP://EXAMPLE.COM:21", "ftp://example.com/")]
    [TestCase("http://example.com:", "http://example.com/")]
    public void BuiltInNormalizationRemovesEmptyAndDefaultPorts(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("http://example.com:81", "http://example.com:81/")]
    [TestCase("https://example.com:80", "https://example.com:80/")]
    [TestCase("ws://example.com:443", "ws://example.com:443/")]
    [TestCase("wss://example.com:80", "wss://example.com:80/")]
    [TestCase("ftp://example.com:20", "ftp://example.com:20/")]
    public void BuiltInNormalizationPreservesNondefaultPorts(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("http://example.com?#", "http://example.com/?#")]
    [TestCase("https://example.com?value#", "https://example.com/?value#")]
    [TestCase("ws://example.com#value", "ws://example.com/#value")]
    public void BuiltInsPreserveEmptyQueryAndFragmentDelimiters(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("X://EXAMPLE.COM:80?#", "x://example.com:80?#")]
    [TestCase("//EXAMPLE.COM:80/a/./b?#", "//example.com:80/a/b?#")]
    [TestCase("relative/./path", "relative/path")]
    public void UnsupportedOrAbsentSchemesReceiveOnlyGenericNormalization(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("HTTP://EXAMPLE.COM:080?#")]
    [TestCase("https://example.com:443/path")]
    [TestCase("WS://EXAMPLE.COM:")]
    [TestCase("ftp://example.com:20")]
    [TestCase("x://EXAMPLE.COM/a/./b")]
    public void BuiltInProfileNormalizationIsIdempotent(string input)
    {
        string first = NormalizeText(input);
        string second = NormalizeText(first);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void BuiltInNormalizationRejectsANullReference()
    {
        Assert.Throws<ArgumentException>(() =>
            UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(null));
    }

    private static UriSchemeProfile GetProfile(string scheme)
    {
        UriSchemeProfile? profile = UriNormalizerCore.CreateBuiltInSchemeProfile(scheme);
        Assert.That(profile, Is.Not.Null);
        return profile!;
    }

    private static string NormalizeText(string text)
    {
        UriReference source = Parse(text);
        UriReference normalized = UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(
            source);
        UriCompositionResult composed = UriComposerCore.RecomposeParsedReference(
            normalized,
            new OperationLimits());
        Assert.That(composed.Success, Is.True, composed.Diagnostic?.Message);
        return composed.Text;
    }

    private static UriReference Parse(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }
}
