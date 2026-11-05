namespace cookies.Tests;

public class CookieSiteCalculatorTests
{
    [TestCase("https://www.example.test/path", "example.test", "https://example.test")]
    [TestCase("http://api.example.test:8080/path", "example.test", "http://example.test")]
    [TestCase("https://a.shop.example.co.test/path", "example.co.test", "https://example.co.test")]
    public void DnsSitesUseSchemeAndProviderRegistrableDomain(
        string uriText,
        string registrableDomain,
        string expected)
    {
        RecordingRegistrableDomainList suffixes = new RecordingRegistrableDomainList(registrableDomain);

        CookieSiteResult result = CookieSiteCalculator.Compute(new Uri(uriText), suffixes, true);

        AssertSuccess(result, expected, registrableDomain, false);
        Assert.That(suffixes.RequestedDomains, Is.EqualTo(new[]
        {
            new Uri(uriText).Host.ToLowerInvariant()
        }));
    }

    [Test]
    public void SchemeIsPartOfTheSiteWhileSiblingSubdomainsShareARegistrableSite()
    {
        RecordingRegistrableDomainList suffixes = new RecordingRegistrableDomainList("example.test");

        CookieSiteResult first = CookieSiteCalculator.Compute(
            new Uri("https://one.example.test/path"), suffixes, true);
        CookieSiteResult sibling = CookieSiteCalculator.Compute(
            new Uri("https://two.example.test/other"), suffixes, true);
        CookieSiteResult differentScheme = CookieSiteCalculator.Compute(
            new Uri("http://two.example.test/other"), suffixes, true);

        Assert.That(first.Site, Is.EqualTo(sibling.Site));
        Assert.That(differentScheme.Site, Is.Not.EqualTo(first.Site));
        Assert.That(differentScheme.Site, Is.EqualTo("http://example.test"));
    }

    [Test]
    public void UnicodeOriginsAreCanonicalizedBeforeLookupAndSiteConstruction()
    {
        RecordingRegistrableDomainList suffixes = new RecordingRegistrableDomainList(
            "xn--bcher-kva.example");

        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://shop.bücher.example/path"), suffixes, true);

        AssertSuccess(
            result,
            "https://xn--bcher-kva.example",
            "xn--bcher-kva.example",
            false);
        Assert.That(suffixes.RequestedDomains, Is.EqualTo(new[]
        {
            "shop.xn--bcher-kva.example"
        }));
    }

    [TestCase("https://192.0.2.10:8443/path", "https://192.0.2.10", "192.0.2.10")]
    [TestCase("http://[2001:DB8::ABCD]:8080/path", "http://[2001:db8::abcd]", "2001:db8::abcd")]
    public void IpSitesUseTheCanonicalLiteralWithoutCallingTheProvider(
        string uriText,
        string expectedSite,
        string expectedHost)
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri(uriText), new ThrowingPublicSuffixList(), true);

        AssertSuccess(result, expectedSite, expectedHost, true);
    }

    [TestCase("https://LOCALHOST:8443/path", "https://localhost", "localhost")]
    [TestCase("http://INTRANET/path", "http://intranet", "intranet")]
    public void SingleLabelSitesUseTheCanonicalHostWithoutCallingTheProvider(
        string uriText,
        string expectedSite,
        string expectedHost)
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri(uriText), new ThrowingPublicSuffixList(), true);

        AssertSuccess(result, expectedSite, expectedHost, true);
    }

    [Test]
    public void MissingRequiredProviderFailsClosedForMultiLabelDnsHosts()
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://www.example.test/path"), null, true);

        AssertFailure(result, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [Test]
    public void ExplicitlyDisabledProviderRequirementFallsBackToTheExactHostWithAWarning()
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://WWW.Example.Test:8443/path"), null, false);

        AssertSuccess(result, "https://www.example.test", "www.example.test", true);
        AssertWarning(result);
    }

    [Test]
    public void ProviderConfirmedAbsenceOfARegistrableDomainFallsBackToTheHost()
    {
        RecordingRegistrableDomainList suffixes = new RecordingRegistrableDomainList(null);

        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://public.suffix.test/path"), suffixes, true);

        AssertSuccess(result, "https://public.suffix.test", "public.suffix.test", true);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ProviderFailuresAreReportedAsUnavailable()
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://www.example.test/path"), new ThrowingPublicSuffixList(), false);

        AssertFailure(result, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [TestCase("")]
    [TestCase("EXAMPLE.TEST")]
    [TestCase("other.test")]
    [TestCase("deep.www.example.test")]
    [TestCase("192.0.2.1")]
    public void InvalidProviderResultsAreReportedAsUnavailable(string providerResult)
    {
        RecordingRegistrableDomainList suffixes = new RecordingRegistrableDomainList(providerResult);

        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri("https://www.example.test/path"), suffixes, true);

        AssertFailure(result, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [TestCase("ftp://example.test/path", CookieErrorCode.UnsupportedScheme)]
    [TestCase("file:///path", CookieErrorCode.InvalidHost)]
    public void InvalidOriginsPropagateStableFailures(string uriText, CookieErrorCode expected)
    {
        CookieSiteResult result = CookieSiteCalculator.Compute(
            new Uri(uriText), new ThrowingPublicSuffixList(), true);

        AssertFailure(result, expected);
    }

    [Test]
    public void NullAndRelativeOriginsAreRejectedBeforeProviderLookup()
    {
        CookieSiteResult nullResult = CookieSiteCalculator.Compute(
            null!, new ThrowingPublicSuffixList(), true);
        CookieSiteResult relativeResult = CookieSiteCalculator.Compute(
            new Uri("relative", UriKind.Relative), new ThrowingPublicSuffixList(), true);

        AssertFailure(nullResult, CookieErrorCode.InvalidHost);
        AssertFailure(relativeResult, CookieErrorCode.InvalidHost);
    }

    static void AssertSuccess(
        CookieSiteResult result,
        string expectedSite,
        string expectedHost,
        bool usedHostFallback)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Site, Is.EqualTo(expectedSite));
        Assert.That(result.SiteHost, Is.EqualTo(expectedHost));
        Assert.That(result.UsedHostFallback, Is.EqualTo(usedHostFallback));
    }

    static void AssertWarning(CookieSiteResult result)
    {
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.PublicSuffixListUnavailable));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(result.Issues[0].HasOffset, Is.False);
        Assert.That(result.Issues[0].SafeFragment, Is.Empty);
    }

    static void AssertFailure(CookieSiteResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Site, Is.Empty);
        Assert.That(result.SiteHost, Is.Empty);
        Assert.That(result.UsedHostFallback, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}

public class RecordingRegistrableDomainList : IPublicSuffixList
{
    public string? RegistrableDomain;
    public List<string> RequestedDomains = [];

    public RecordingRegistrableDomainList(string? registrableDomain)
    {
        RegistrableDomain = registrableDomain;
    }

    public string? GetPublicSuffix(string canonicalDomain)
    {
        return null;
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        RequestedDomains.Add(canonicalDomain);
        return RegistrableDomain;
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return false;
    }
}
