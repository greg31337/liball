namespace cookies.Tests;

public class CookiePublicSuffixPolicyTests
{
    [Test]
    public void SetTimeDomainCookiePassesWhenItsDomainIsNotAPublicSuffix()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList(["test"]);
        CookieCanonicalHostResult origin = CanonicalOrigin("https://shop.example.test/");
        CookieDomainResult domain = NormalizeDomain(origin, "example.test");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            domain,
            origin,
            suffixes,
            true);

        AssertSuccess(result, "example.test", false, true, false);
        Assert.That(result.Issues, Is.Empty);
        Assert.That(suffixes.CheckedDomains, Is.EqualTo(new string[] { "example.test" }));
    }

    [Test]
    public void PublicSuffixDomainCookieIsRejected()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList(["co.test"]);
        CookieCanonicalHostResult origin = CanonicalOrigin("https://shop.co.test/");
        CookieDomainResult domain = NormalizeDomain(origin, "co.test");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            domain,
            origin,
            suffixes,
            true);

        AssertFailure(result, CookieErrorCode.PublicSuffix);
    }

    [Test]
    public void SameHostPublicSuffixExceptionBecomesHostOnly()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList(["co.test"]);
        CookieCanonicalHostResult origin = CanonicalOrigin("https://co.test/");
        CookieDomainResult domain = NormalizeDomain(origin, "co.test");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            domain,
            origin,
            suffixes,
            true);

        AssertSuccess(result, "co.test", true, true, false);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ProviderWildcardAndExceptionOutcomesAreHonored()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList(["tenant.wild"]);
        CookieCanonicalHostResult wildcardOrigin = CanonicalOrigin("https://app.tenant.wild/");
        CookieCanonicalHostResult exceptionOrigin = CanonicalOrigin("https://app.city.wild/");

        CookiePublicSuffixResult wildcard = CookiePublicSuffixPolicy.ValidateSetDomain(
            NormalizeDomain(wildcardOrigin, "tenant.wild"),
            wildcardOrigin,
            suffixes,
            true);
        CookiePublicSuffixResult exception = CookiePublicSuffixPolicy.ValidateSetDomain(
            NormalizeDomain(exceptionOrigin, "city.wild"),
            exceptionOrigin,
            suffixes,
            true);

        AssertFailure(wildcard, CookieErrorCode.PublicSuffix);
        AssertSuccess(exception, "city.wild", false, true, false);
    }

    [Test]
    public void RequiredProviderFailsClosedForDomainCookies()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin("https://shop.example.test/");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            NormalizeDomain(origin, "example.test"),
            origin,
            null,
            true);

        AssertFailure(result, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [Test]
    public void ExplicitlyDisabledProtectionReturnsAWarningForDomainCookies()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin("https://shop.example.test/");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            NormalizeDomain(origin, "example.test"),
            origin,
            null,
            false);

        AssertSuccess(result, "example.test", false, true, false);
        AssertWarning(result);
    }

    [Test]
    public void HostOnlyAndIpCookiesDoNotRequireOrCallAPublicSuffixList()
    {
        ThrowingPublicSuffixList suffixes = new ThrowingPublicSuffixList();
        CookieCanonicalHostResult dnsOrigin = CanonicalOrigin("https://example.test/");
        CookieCanonicalHostResult ipOrigin = CanonicalOrigin("https://192.0.2.1/");
        SetCookie absentDomain = new SetCookie();
        CookieDomainResult hostOnly = CookieDomainMatcher.NormalizeForOrigin(
            absentDomain,
            dnsOrigin,
            CookieComplianceMode.Rfc6265);
        CookieDomainResult ip = NormalizeDomain(ipOrigin, "192.0.2.1");

        CookiePublicSuffixResult hostOnlyResult = CookiePublicSuffixPolicy.ValidateSetDomain(
            hostOnly,
            dnsOrigin,
            suffixes,
            true);
        CookiePublicSuffixResult ipResult = CookiePublicSuffixPolicy.ValidateSetDomain(
            ip,
            ipOrigin,
            suffixes,
            true);

        AssertSuccess(hostOnlyResult, "example.test", true, false, false);
        AssertSuccess(ipResult, "192.0.2.1", true, true, true);
        Assert.That(hostOnlyResult.Issues, Is.Empty);
        Assert.That(ipResult.Issues, Is.Empty);
    }

    [Test]
    public void ProviderFailureIsReportedAsUnavailable()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin("https://shop.example.test/");

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateSetDomain(
            NormalizeDomain(origin, "example.test"),
            origin,
            new ThrowingPublicSuffixList(),
            false);

        AssertFailure(result, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [Test]
    public void RetrievalRevalidatesAgainstTheProvidersCurrentState()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList([]);
        StoredCookie cookie = new StoredCookie
        {
            Domain = "example.test",
            HostOnly = false
        };

        CookiePublicSuffixResult before = CookiePublicSuffixPolicy.ValidateStoredDomain(
            cookie,
            suffixes,
            true);
        suffixes.PublicSuffixes.Add("example.test");
        CookiePublicSuffixResult after = CookiePublicSuffixPolicy.ValidateStoredDomain(
            cookie,
            suffixes,
            true);

        AssertSuccess(before, "example.test", false, false, false);
        AssertFailure(after, CookieErrorCode.PublicSuffix);
        Assert.That(suffixes.CheckedDomains, Is.EqualTo(
            new string[] { "example.test", "example.test" }));
    }

    [Test]
    public void RetrievalKeepsHostOnlyPublicSuffixCookiesFromTheSameHostException()
    {
        MutablePublicSuffixList suffixes = new MutablePublicSuffixList(["co.test"]);
        StoredCookie cookie = new StoredCookie
        {
            Domain = "co.test",
            HostOnly = true
        };

        CookiePublicSuffixResult result = CookiePublicSuffixPolicy.ValidateStoredDomain(
            cookie,
            suffixes,
            true);

        AssertSuccess(result, "co.test", true, false, false);
        Assert.That(suffixes.CheckedDomains, Is.Empty);
    }

    [Test]
    public void RetrievalProviderAvailabilityModesMatchSetTimeBehavior()
    {
        StoredCookie cookie = new StoredCookie
        {
            Domain = "example.test",
            HostOnly = false
        };

        CookiePublicSuffixResult required = CookiePublicSuffixPolicy.ValidateStoredDomain(
            cookie,
            null,
            true);
        CookiePublicSuffixResult disabled = CookiePublicSuffixPolicy.ValidateStoredDomain(
            cookie,
            null,
            false);

        AssertFailure(required, CookieErrorCode.PublicSuffixListUnavailable);
        AssertSuccess(disabled, "example.test", false, false, false);
        AssertWarning(disabled);
    }

    [Test]
    public void RetrievalRejectsMalformedOrNonHostOnlyIpStorageState()
    {
        StoredCookie malformed = new StoredCookie
        {
            Domain = "EXAMPLE.TEST",
            HostOnly = false
        };
        StoredCookie ipDomainCookie = new StoredCookie
        {
            Domain = "192.0.2.1",
            HostOnly = false
        };
        StoredCookie ipHostCookie = new StoredCookie
        {
            Domain = "192.0.2.1",
            HostOnly = true
        };

        CookiePublicSuffixResult malformedResult = CookiePublicSuffixPolicy.ValidateStoredDomain(
            malformed,
            null,
            false);
        CookiePublicSuffixResult ipDomainResult = CookiePublicSuffixPolicy.ValidateStoredDomain(
            ipDomainCookie,
            null,
            false);
        CookiePublicSuffixResult ipHostResult = CookiePublicSuffixPolicy.ValidateStoredDomain(
            ipHostCookie,
            null,
            true);

        AssertFailure(malformedResult, CookieErrorCode.InvalidHost);
        AssertFailure(ipDomainResult, CookieErrorCode.InvalidHost);
        AssertSuccess(ipHostResult, "192.0.2.1", true, false, true);
    }

    [Test]
    public void InvalidSetDomainGraphsAreRejectedBeforeProviderCalls()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin("https://example.test/");
        CookieDomainResult contradictory = new CookieDomainResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Domain = "example.test",
            HostOnly = true,
            DomainAttributeSupplied = true
        };
        CookieDomainResult wrongOrigin = new CookieDomainResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Domain = "other.test",
            HostOnly = false,
            DomainAttributeSupplied = true
        };

        CookiePublicSuffixResult nullResult = CookiePublicSuffixPolicy.ValidateSetDomain(
            null!,
            origin,
            new ThrowingPublicSuffixList(),
            true);
        CookiePublicSuffixResult contradictoryResult = CookiePublicSuffixPolicy.ValidateSetDomain(
            contradictory,
            origin,
            new ThrowingPublicSuffixList(),
            true);
        CookiePublicSuffixResult wrongOriginResult = CookiePublicSuffixPolicy.ValidateSetDomain(
            wrongOrigin,
            origin,
            new ThrowingPublicSuffixList(),
            true);

        AssertFailure(nullResult, CookieErrorCode.InvalidHost);
        AssertFailure(contradictoryResult, CookieErrorCode.InvalidHost);
        AssertFailure(wrongOriginResult, CookieErrorCode.DomainMismatch);
    }

    static CookieDomainResult NormalizeDomain(CookieCanonicalHostResult origin, string domain)
    {
        SetCookie cookie = new SetCookie
        {
            HasDomain = true,
            Domain = domain
        };
        return CookieDomainMatcher.NormalizeForOrigin(
            cookie,
            origin,
            CookieComplianceMode.Rfc6265Bis);
    }

    static CookieCanonicalHostResult CanonicalOrigin(string uriText)
    {
        return CookieHostCanonicalizer.Canonicalize(
            CookieUriHostExtractor.Extract(new Uri(uriText)));
    }

    static void AssertSuccess(
        CookiePublicSuffixResult result,
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

    static void AssertWarning(CookiePublicSuffixResult result)
    {
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.PublicSuffixListUnavailable));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(result.Issues[0].HasOffset, Is.False);
        Assert.That(result.Issues[0].SafeFragment, Is.Empty);
    }

    static void AssertFailure(CookiePublicSuffixResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Domain, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}

public class MutablePublicSuffixList : IPublicSuffixList
{
    public HashSet<string> PublicSuffixes;
    public List<string> CheckedDomains = [];

    public MutablePublicSuffixList(string[] publicSuffixes)
    {
        PublicSuffixes = new HashSet<string>(publicSuffixes, StringComparer.Ordinal);
    }

    public string? GetPublicSuffix(string canonicalDomain)
    {
        if (PublicSuffixes.Contains(canonicalDomain))
        {
            return canonicalDomain;
        }

        return null;
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        return null;
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        CheckedDomains.Add(canonicalDomain);
        return PublicSuffixes.Contains(canonicalDomain);
    }
}

public class ThrowingPublicSuffixList : IPublicSuffixList
{
    public string? GetPublicSuffix(string canonicalDomain)
    {
        throw new InvalidOperationException("provider failure");
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        throw new InvalidOperationException("provider failure");
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        throw new InvalidOperationException("provider failure");
    }
}
