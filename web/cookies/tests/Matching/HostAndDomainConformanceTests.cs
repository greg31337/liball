namespace cookies.Tests;

public class HostAndDomainConformanceTests
{
    [TestCase("bücher.example", "xn--bcher-kva.example")]
    [TestCase("例え.テスト", "xn--r8jz45g.xn--zckzah")]
    [TestCase("παράδειγμα.δοκιμή", "xn--hxajbheg2az3al.xn--jxalpdlp")]
    public void UnicodeAndALabelFormsCanonicalizeToTheSameAsciiHost(
        string unicodeHost,
        string asciiHost)
    {
        CookieCanonicalHostResult unicode = Canonicalize(
            new Uri("https://" + unicodeHost + ":8443/path"));
        CookieCanonicalHostResult alabel = Canonicalize(
            new Uri("https://" + asciiHost.ToUpperInvariant() + "/path"));

        AssertDnsHost(unicode, asciiHost);
        AssertDnsHost(alabel, asciiHost);
    }

    [TestCase("-leading.example", CookieErrorCode.InvalidHost)]
    [TestCase("trailing-.example", CookieErrorCode.InvalidHost)]
    [TestCase("empty..example", CookieErrorCode.InvalidHost)]
    [TestCase("bad_name.example", CookieErrorCode.InvalidHost)]
    [TestCase("xn--a.example", CookieErrorCode.InvalidIdna)]
    [TestCase("ab--reserved.example", CookieErrorCode.InvalidIdna)]
    public void InvalidDnsLabelsAreRejectedWithStableErrors(
        string host,
        CookieErrorCode expectedError)
    {
        CookieCanonicalHostResult result = CookieHostCanonicalizer.Canonicalize(
            DnsHost(host));

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
        Assert.That(result.Host, Is.Empty);
    }

    [TestCase("https://EXAMPLE.TEST.:9443/path", "example.test", true, false, false)]
    [TestCase("http://192.0.2.1:8080/path", "192.0.2.1", false, true, false)]
    [TestCase("https://[2001:DB8::1]:8443/path", "2001:db8::1", false, false, true)]
    public void PortsAndDnsRootDotsDoNotChangeCanonicalHostIdentity(
        string uriText,
        string expectedHost,
        bool isDns,
        bool isIpv4,
        bool isIpv6)
    {
        CookieCanonicalHostResult result = Canonicalize(new Uri(uriText));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Host, Is.EqualTo(expectedHost));
        Assert.That(result.IsDnsName, Is.EqualTo(isDns));
        Assert.That(result.IsIpv4, Is.EqualTo(isIpv4));
        Assert.That(result.IsIpv6, Is.EqualTo(isIpv6));
    }

    [Test]
    public void DomainMatchingUsesExactAndWholeLabelSuffixBoundaries()
    {
        Assert.That(CookieDomainMatcher.DomainMatches(
            "example.test", "example.test", false), Is.True);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "shop.example.test", "example.test", false), Is.True);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "deep.shop.example.test", "example.test", false), Is.True);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "notexample.test", "example.test", false), Is.False);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "example.test.invalid", "example.test", false), Is.False);
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void HostOnlyCookiesRemainExactMatchOnlyInEveryProfile(
        CookieComplianceMode mode)
    {
        SetCookie cookie = new SetCookie { HasDomain = false };
        CookieCanonicalHostResult origin = Canonicalize(
            new Uri("https://Shop.Example.Test:8443/path"));

        CookieDomainResult result = CookieDomainMatcher.NormalizeForOrigin(
            cookie, origin, mode);

        AssertDomain(result, "shop.example.test", true, false, false);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "shop.example.test", result.Domain, result.HostOnly), Is.True);
        Assert.That(CookieDomainMatcher.DomainMatches(
            "child.shop.example.test", result.Domain, result.HostOnly), Is.False);
    }

    [Test]
    public void EquivalentIpv6SpellingsIdentifyTheSameCanonicalAddress()
    {
        CookieDomainResult result = Normalize(
            "https://[2001:db8::1]/path",
            "2001:0DB8:0:0:0:0:0:1",
            CookieComplianceMode.Rfc6265Bis);

        AssertDomain(result, "2001:db8::1", true, true, true);
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void EveryProfileUsesTheSameCanonicalDomainAndIpSafetyRules(
        CookieComplianceMode mode)
    {
        CookieDomainResult unicodeParent = Normalize(
            "https://shop.bücher.example/path",
            ".BÜCHER.example",
            mode);
        CookieDomainResult exactIpv4 = Normalize(
            "https://192.0.2.1:8443/path",
            ".192.0.2.1",
            mode);
        CookieDomainResult mismatchedIpv4 = Normalize(
            "https://192.0.2.1/path",
            "192.0.2.2",
            mode);

        AssertDomain(
            unicodeParent, "xn--bcher-kva.example", false, true, false);
        AssertDomain(exactIpv4, "192.0.2.1", true, true, true);
        Assert.That(mismatchedIpv4.Success, Is.False);
        Assert.That(mismatchedIpv4.ErrorCode,
            Is.EqualTo(CookieErrorCode.DomainMismatch));
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void EveryProfileRejectsMalformedDomainAttributes(
        CookieComplianceMode mode)
    {
        string[] malformed =
        [
            "",
            ".",
            "..example.test",
            "example.test.",
            ".example.test.",
            "bad_name.example",
            "[2001:db8::1]"
        ];

        foreach (string domain in malformed)
        {
            CookieDomainResult result = Normalize(
                "https://example.test/path", domain, mode);

            Assert.That(result.Success, Is.False, domain);
            Assert.That(result.ErrorCode,
                Is.EqualTo(CookieErrorCode.InvalidAttribute), domain);
        }
    }

    static CookieCanonicalHostResult Canonicalize(Uri uri)
    {
        return CookieHostCanonicalizer.Canonicalize(
            CookieUriHostExtractor.Extract(uri));
    }

    static CookieUriHostResult DnsHost(string host)
    {
        return new CookieUriHostResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Host = host,
            IsDnsName = true
        };
    }

    static CookieDomainResult Normalize(
        string originUri,
        string domain,
        CookieComplianceMode mode)
    {
        SetCookie cookie = new SetCookie
        {
            HasDomain = true,
            Domain = domain
        };
        return CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            Canonicalize(new Uri(originUri)),
            mode);
    }

    static void AssertDnsHost(
        CookieCanonicalHostResult result,
        string expectedHost)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Host, Is.EqualTo(expectedHost));
        Assert.That(result.IsDnsName, Is.True);
        Assert.That(result.IsIpv4, Is.False);
        Assert.That(result.IsIpv6, Is.False);
    }

    static void AssertDomain(
        CookieDomainResult result,
        string expectedDomain,
        bool hostOnly,
        bool supplied,
        bool isIpAddress)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Domain, Is.EqualTo(expectedDomain));
        Assert.That(result.HostOnly, Is.EqualTo(hostOnly));
        Assert.That(result.DomainAttributeSupplied, Is.EqualTo(supplied));
        Assert.That(result.IsIpAddress, Is.EqualTo(isIpAddress));
    }
}
