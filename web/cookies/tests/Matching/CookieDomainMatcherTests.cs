namespace cookies.Tests;

public class CookieDomainMatcherTests
{
    [TestCase("example.test", "example.test", false, true)]
    [TestCase("EXAMPLE.TEST", "example.test", false, true)]
    [TestCase("sub.example.test", "example.test", false, true)]
    [TestCase("deep.sub.example.test", "EXAMPLE.TEST", false, true)]
    [TestCase("notexample.test", "example.test", false, false)]
    [TestCase("example.test.invalid", "example.test", false, false)]
    [TestCase("short.test", "long.short.test", false, false)]
    public void DomainMatchingUsesExactOrDotBoundarySuffixRules(
        string requestHost,
        string cookieDomain,
        bool hostOnly,
        bool expected)
    {
        bool result = CookieDomainMatcher.DomainMatches(requestHost, cookieDomain, hostOnly);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void HostOnlyCookiesMatchOnlyTheExactCanonicalHost()
    {
        Assert.That(
            CookieDomainMatcher.DomainMatches("example.test", "example.test", true),
            Is.True);
        Assert.That(
            CookieDomainMatcher.DomainMatches("sub.example.test", "example.test", true),
            Is.False);
    }

    [TestCase("192.0.2.1", "192.0.2.1", false, true)]
    [TestCase("192.0.2.1", "0.2.1", false, false)]
    [TestCase("sub.192.0.2.1", "192.0.2.1", false, false)]
    [TestCase("2001:db8::1", "2001:DB8::1", false, true)]
    [TestCase("2001:db8::1", "db8::1", false, false)]
    public void IpAddressesNeverUseSuffixDomainMatching(
        string requestHost,
        string cookieDomain,
        bool hostOnly,
        bool expected)
    {
        bool result = CookieDomainMatcher.DomainMatches(requestHost, cookieDomain, hostOnly);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NullAndEmptyMatchInputsReturnFalse()
    {
        Assert.That(CookieDomainMatcher.DomainMatches(null!, "example.test", false), Is.False);
        Assert.That(CookieDomainMatcher.DomainMatches("example.test", null!, false), Is.False);
        Assert.That(CookieDomainMatcher.DomainMatches("", "example.test", false), Is.False);
        Assert.That(CookieDomainMatcher.DomainMatches("example.test", "", false), Is.False);
    }

    [Test]
    public void AbsentDomainCreatesAHostOnlyCookie()
    {
        SetCookie cookie = new SetCookie
        {
            HasDomain = false,
            Domain = "ignored.example"
        };
        CookieCanonicalHostResult origin = CanonicalOrigin("https://Sub.Example.Test/path");

        CookieDomainResult result = CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            origin,
            CookieComplianceMode.Rfc6265);

        AssertSuccess(result, "sub.example.test", true, false, false);
    }

    [TestCase("example.test", "example.test")]
    [TestCase("EXAMPLE.TEST", "example.test")]
    [TestCase(".example.test", "example.test")]
    public void ExactOrSingleLeadingDotDomainCreatesADomainCookie(string attribute, string expected)
    {
        CookieDomainResult result = Normalize(
            "https://example.test/path",
            attribute,
            CookieComplianceMode.Rfc6265Bis);

        AssertSuccess(result, expected, false, true, false);
    }

    [Test]
    public void ParentDomainMatchesOnlyAtALabelBoundary()
    {
        CookieDomainResult parent = Normalize(
            "https://shop.example.test/path",
            ".example.test",
            CookieComplianceMode.Compatible);
        CookieDomainResult partial = Normalize(
            "https://notexample.test/path",
            "example.test",
            CookieComplianceMode.Compatible);

        AssertSuccess(parent, "example.test", false, true, false);
        AssertFailure(partial, CookieErrorCode.DomainMismatch);
    }

    [TestCase("")]
    [TestCase(".")]
    [TestCase("..example.test")]
    [TestCase("example.test.")]
    [TestCase(".example.test.")]
    [TestCase("bad_name.example")]
    [TestCase("[2001:db8::1]")]
    public void MalformedDomainAttributesAreRejected(string attribute)
    {
        CookieDomainResult result = Normalize(
            "https://example.test/path",
            attribute,
            CookieComplianceMode.Rfc6265);

        AssertFailure(result, CookieErrorCode.InvalidAttribute);
    }

    [Test]
    public void InvalidIdnaDomainUsesTheIdnaErrorCode()
    {
        CookieDomainResult result = Normalize(
            "https://example.test/path",
            "xn--a",
            CookieComplianceMode.Rfc6265Bis);

        AssertFailure(result, CookieErrorCode.InvalidIdna);
    }

    [Test]
    public void UnicodeDomainIsCanonicalizedBeforeOriginMatching()
    {
        CookieDomainResult result = Normalize(
            "https://shop.bücher.example/path",
            "BÜCHER.example",
            CookieComplianceMode.Compatible);

        AssertSuccess(result, "xn--bcher-kva.example", false, true, false);
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void ExactIpv4DomainIsAcceptedAsHostOnlyWhilePreservingAttributePresence(
        CookieComplianceMode mode)
    {
        CookieDomainResult result = Normalize("https://192.0.2.1/path", ".192.0.2.1", mode);

        AssertSuccess(result, "192.0.2.1", true, true, true);
    }

    [Test]
    public void Ipv4DomainMustBeTheExactOriginAddress()
    {
        CookieDomainResult differentIp = Normalize(
            "https://192.0.2.1/path",
            "192.0.2.2",
            CookieComplianceMode.Rfc6265);
        CookieDomainResult dnsSuffix = Normalize(
            "https://192.0.2.1/path",
            "0.2.1",
            CookieComplianceMode.Rfc6265);

        AssertFailure(differentIp, CookieErrorCode.DomainMismatch);
        AssertFailure(dnsSuffix, CookieErrorCode.DomainMismatch);
    }

    [Test]
    public void ExactIpv6DomainIsAcceptedOnlyWithoutUriBrackets()
    {
        CookieDomainResult exact = Normalize(
            "https://[2001:db8::1]/path",
            "2001:DB8::1",
            CookieComplianceMode.Rfc6265Bis);
        CookieDomainResult different = Normalize(
            "https://[2001:db8::1]/path",
            "2001:db8::2",
            CookieComplianceMode.Rfc6265Bis);

        AssertSuccess(exact, "2001:db8::1", true, true, true);
        AssertFailure(different, CookieErrorCode.DomainMismatch);
    }

    [Test]
    public void DnsOriginsRejectIpDomainAttributes()
    {
        CookieDomainResult result = Normalize(
            "https://example.test/path",
            "192.0.2.1",
            CookieComplianceMode.Rfc6265);

        AssertFailure(result, CookieErrorCode.DomainMismatch);
    }

    [Test]
    public void InvalidOriginAndComplianceModeFailDeterministically()
    {
        SetCookie cookie = new SetCookie { HasDomain = true, Domain = "example.test" };
        CookieCanonicalHostResult invalidOrigin = new CookieCanonicalHostResult
        {
            Success = true,
            Host = "example.test"
        };
        CookieCanonicalHostResult validOrigin = CanonicalOrigin("https://example.test/");
        CookieCanonicalHostResult noncanonicalOrigin = new CookieCanonicalHostResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Host = "EXAMPLE.TEST",
            IsDnsName = true
        };

        CookieDomainResult invalidHost = CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            invalidOrigin,
            CookieComplianceMode.Rfc6265);
        CookieDomainResult invalidMode = CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            validOrigin,
            (CookieComplianceMode)99);
        CookieDomainResult noncanonical = CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            noncanonicalOrigin,
            CookieComplianceMode.Rfc6265);
        CookieDomainResult nullCookie = CookieDomainMatcher.NormalizeForOrigin(
            null!,
            validOrigin,
            CookieComplianceMode.Rfc6265);

        AssertFailure(invalidHost, CookieErrorCode.InvalidHost);
        AssertFailure(invalidMode, CookieErrorCode.InvalidConfiguration);
        AssertFailure(noncanonical, CookieErrorCode.InvalidHost);
        AssertFailure(nullCookie, CookieErrorCode.InvalidAttribute);
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
        return CookieDomainMatcher.NormalizeForOrigin(cookie, CanonicalOrigin(originUri), mode);
    }

    static CookieCanonicalHostResult CanonicalOrigin(string uriText)
    {
        return CookieHostCanonicalizer.Canonicalize(
            CookieUriHostExtractor.Extract(new Uri(uriText)));
    }

    static void AssertSuccess(
        CookieDomainResult result,
        string domain,
        bool hostOnly,
        bool domainAttributeSupplied,
        bool isIpAddress)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Domain, Is.EqualTo(domain));
        Assert.That(result.HostOnly, Is.EqualTo(hostOnly));
        Assert.That(result.DomainAttributeSupplied, Is.EqualTo(domainAttributeSupplied));
        Assert.That(result.IsIpAddress, Is.EqualTo(isIpAddress));
    }

    static void AssertFailure(CookieDomainResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Domain, Is.Empty);
        Assert.That(result.HostOnly, Is.False);
        Assert.That(result.DomainAttributeSupplied, Is.False);
        Assert.That(result.IsIpAddress, Is.False);
    }
}
