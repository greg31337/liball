namespace cookies.Tests;

public class CookieUriHostExtractorTests
{
    [TestCase("https://example.test/path", "example.test")]
    [TestCase("http://Example.TEST:8080/path", "example.test")]
    [TestCase("HTTPS://LOCALHOST:443/path", "localhost")]
    [TestCase("https://user:password@sub.example.test:8443/path?query=yes", "sub.example.test")]
    public void ExtractorReturnsDnsHostsWithoutAuthorityDetails(string uriText, string expected)
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(new Uri(uriText));

        AssertSuccess(result, expected);
        Assert.That(result.IsDnsName, Is.True);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.False);
    }

    [TestCase("https://example.test./", "example.test")]
    [TestCase("https://Sub.Example.TEST.:443/path", "sub.example.test")]
    public void ExtractorRemovesOneTerminalDnsRootDot(string uriText, string expected)
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(new Uri(uriText));

        AssertSuccess(result, expected);
        Assert.That(result.IsDnsName, Is.True);
    }

    [TestCase("http://192.0.2.1/", "192.0.2.1")]
    [TestCase("https://127.0.0.1:9443/path", "127.0.0.1")]
    public void ExtractorReturnsIpv4WithoutPorts(string uriText, string expected)
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(new Uri(uriText));

        AssertSuccess(result, expected);
        Assert.That(result.IsDnsName, Is.False);
        Assert.That(result.IsIpv4, Is.True);
        Assert.That(result.IsIpv6, Is.False);
    }

    [TestCase("http://[2001:db8::1]/", "[2001:db8::1]")]
    [TestCase("https://[2001:db8::1]:8443/path", "[2001:db8::1]")]
    [TestCase("https://[::1]/", "[::1]")]
    public void ExtractorReturnsBracketedIpv6WithoutPorts(string uriText, string uriHost)
    {
        Uri uri = new Uri(uriText);

        CookieUriHostResult result = CookieUriHostExtractor.Extract(uri);

        Assert.That(uri.Host, Is.EqualTo(uriHost));
        AssertSuccess(result, uriHost.Substring(1, uriHost.Length - 2));
        Assert.That(result.IsDnsName, Is.False);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.True);
    }

    [Test]
    public void ExtractorRetainsUnicodeDnsHostForTheIdnaStage()
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(
            new Uri("https://bücher.example/path"));

        AssertSuccess(result, "bücher.example");
        Assert.That(result.IsDnsName, Is.True);
    }

    [TestCase("file:///tmp/cookies")]
    [TestCase("urn:example:cookie")]
    public void AbsoluteUrisWithoutHostsAreRejected(string uriText)
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(new Uri(uriText));

        AssertFailure(result);
    }

    [TestCase("mailto:user@example.test")]
    [TestCase("custom://example.test/path")]
    [TestCase("ftp://example.test/file")]
    public void UnsupportedSchemesAreRejectedEvenWhenSystemUriExposesAHost(string uriText)
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(new Uri(uriText));

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.UnsupportedScheme));
        Assert.That(result.Host, Is.Empty);
    }

    [Test]
    public void RelativeUrisAreRejected()
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(
            new Uri("relative/path", UriKind.Relative));

        AssertFailure(result);
    }

    [Test]
    public void NullUrisAreRejected()
    {
        CookieUriHostResult result = CookieUriHostExtractor.Extract(null!);

        AssertFailure(result);
    }

    static void AssertSuccess(CookieUriHostResult result, string expectedHost)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Host, Is.EqualTo(expectedHost));
    }

    static void AssertFailure(CookieUriHostResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidHost));
        Assert.That(result.Host, Is.Empty);
        Assert.That(result.IsDnsName, Is.False);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.False);
    }
}
