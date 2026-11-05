namespace cookies.Tests;

public class PublicSuffixIntegrationTests
{
    SnapshotPublicSuffixList Snapshot = null!;

    [OneTimeSetUp]
    public void LoadDownloadedSnapshot()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "specs",
            "public-suffix",
            "public_suffix_list.dat"));

        Assert.That(File.Exists(path), Is.True, path);
        Snapshot = SnapshotPublicSuffixList.Load(path);
    }

    [Test]
    public void DeterministicProviderImplementsWildcardAndExceptionRules()
    {
        SnapshotPublicSuffixList suffixes = new SnapshotPublicSuffixList(
            ["wild", "*.wild", "!city.wild"]);

        Assert.That(suffixes.GetPublicSuffix("tenant.wild"),
            Is.EqualTo("tenant.wild"));
        Assert.That(suffixes.IsPublicSuffix("tenant.wild"), Is.True);
        Assert.That(suffixes.GetRegistrableDomain("app.tenant.wild"),
            Is.EqualTo("app.tenant.wild"));
        Assert.That(suffixes.GetPublicSuffix("city.wild"),
            Is.EqualTo("wild"));
        Assert.That(suffixes.IsPublicSuffix("city.wild"), Is.False);
        Assert.That(suffixes.GetRegistrableDomain("shop.city.wild"),
            Is.EqualTo("city.wild"));
    }

    [Test]
    public void WildcardPublicSuffixRejectsSupercookiesWhileExceptionDomainPasses()
    {
        SnapshotPublicSuffixList suffixes = new SnapshotPublicSuffixList(
            ["wild", "*.wild", "!city.wild"]);
        CookieCanonicalHostResult wildcardOrigin = CanonicalOrigin(
            "https://app.tenant.wild/");
        CookieCanonicalHostResult exceptionOrigin = CanonicalOrigin(
            "https://shop.city.wild/");

        CookiePublicSuffixResult wildcard =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                NormalizeDomain(wildcardOrigin, "tenant.wild"),
                wildcardOrigin,
                suffixes,
                true);
        CookiePublicSuffixResult exception =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                NormalizeDomain(exceptionOrigin, "city.wild"),
                exceptionOrigin,
                suffixes,
                true);

        AssertFailure(wildcard, CookieErrorCode.PublicSuffix);
        AssertSuccess(exception, "city.wild", false, true);
    }

    [Test]
    public void DownloadedSnapshotRejectsKnownSupercookiesAndAllowsRegistrableDomains()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin(
            "https://shop.example.co.uk/");

        CookiePublicSuffixResult suffix =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                NormalizeDomain(origin, "co.uk"), origin, Snapshot, true);
        CookiePublicSuffixResult registrable =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                NormalizeDomain(origin, "example.co.uk"), origin, Snapshot, true);

        AssertFailure(suffix, CookieErrorCode.PublicSuffix);
        AssertSuccess(registrable, "example.co.uk", false, true);
    }

    [Test]
    public void SameHostPublicSuffixExceptionConvertsTheCookieToHostOnly()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin("https://com/");

        CookiePublicSuffixResult result =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                NormalizeDomain(origin, "com"), origin, Snapshot, true);

        AssertSuccess(result, "com", true, true);
    }

    [Test]
    public void DownloadedWildcardAndExceptionRulesDriveSiteComputation()
    {
        CookieSiteResult ordinary = CookieSiteCalculator.Compute(
            new Uri("https://www.example.co.uk/path"), Snapshot, true);
        CookieSiteResult wildcard = CookieSiteCalculator.Compute(
            new Uri("https://a.foo.kawasaki.jp/path"), Snapshot, true);
        CookieSiteResult exception = CookieSiteCalculator.Compute(
            new Uri("https://shop.city.kawasaki.jp/path"), Snapshot, true);

        AssertSite(ordinary, "https://example.co.uk", "example.co.uk", false);
        AssertSite(
            wildcard,
            "https://a.foo.kawasaki.jp",
            "a.foo.kawasaki.jp",
            false);
        AssertSite(
            exception,
            "https://city.kawasaki.jp",
            "city.kawasaki.jp",
            false);
    }

    [Test]
    public void MissingProviderModesFailClosedOrReturnExplicitWarnings()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin(
            "https://shop.example.test/");
        CookieDomainResult domain = NormalizeDomain(origin, "example.test");

        CookiePublicSuffixResult requiredSet =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                domain, origin, null, true);
        CookiePublicSuffixResult optionalSet =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                domain, origin, null, false);
        CookieSiteResult requiredSite = CookieSiteCalculator.Compute(
            new Uri("https://shop.example.test/"), null, true);
        CookieSiteResult optionalSite = CookieSiteCalculator.Compute(
            new Uri("https://shop.example.test/"), null, false);

        AssertFailure(
            requiredSet, CookieErrorCode.PublicSuffixListUnavailable);
        AssertSuccess(optionalSet, "example.test", false, true);
        AssertWarning(optionalSet.Issues);
        Assert.That(requiredSite.Success, Is.False);
        Assert.That(requiredSite.ErrorCode,
            Is.EqualTo(CookieErrorCode.PublicSuffixListUnavailable));
        AssertSite(
            optionalSite,
            "https://shop.example.test",
            "shop.example.test",
            true);
        AssertWarning(optionalSite.Issues);
    }

    [Test]
    public void LocalhostHostOnlyAndSiteOperationsDoNotRequireAProvider()
    {
        CookieCanonicalHostResult origin = CanonicalOrigin(
            "https://LOCALHOST:8443/path");
        SetCookie cookie = new SetCookie { HasDomain = false };
        CookieDomainResult domain = CookieDomainMatcher.NormalizeForOrigin(
            cookie, origin, CookieComplianceMode.Rfc6265Bis);

        CookiePublicSuffixResult setResult =
            CookiePublicSuffixPolicy.ValidateSetDomain(
                domain, origin, new ThrowingPublicSuffixList(), true);
        CookieSiteResult siteResult = CookieSiteCalculator.Compute(
            new Uri("https://LOCALHOST:8443/path"),
            new ThrowingPublicSuffixList(),
            true);

        AssertSuccess(setResult, "localhost", true, false);
        AssertSite(siteResult, "https://localhost", "localhost", true);
        Assert.That(setResult.Issues, Is.Empty);
        Assert.That(siteResult.Issues, Is.Empty);
    }

    [Test]
    public void ListChangesAreAppliedWhenStoredDomainsAreRevalidated()
    {
        SnapshotPublicSuffixList suffixes = new SnapshotPublicSuffixList(["test"]);
        StoredCookie cookie = new StoredCookie
        {
            Domain = "example.test",
            HostOnly = false
        };

        CookiePublicSuffixResult before =
            CookiePublicSuffixPolicy.ValidateStoredDomain(
                cookie, suffixes, true);
        suffixes.AddRule("example.test");
        CookiePublicSuffixResult after =
            CookiePublicSuffixPolicy.ValidateStoredDomain(
                cookie, suffixes, true);

        AssertSuccess(before, "example.test", false, false);
        AssertFailure(after, CookieErrorCode.PublicSuffix);
    }

    static CookieCanonicalHostResult CanonicalOrigin(string uriText)
    {
        return CookieHostCanonicalizer.Canonicalize(
            CookieUriHostExtractor.Extract(new Uri(uriText)));
    }

    static CookieDomainResult NormalizeDomain(
        CookieCanonicalHostResult origin,
        string domain)
    {
        SetCookie cookie = new SetCookie
        {
            HasDomain = true,
            Domain = domain
        };
        return CookieDomainMatcher.NormalizeForOrigin(
            cookie, origin, CookieComplianceMode.Rfc6265Bis);
    }

    static void AssertSuccess(
        CookiePublicSuffixResult result,
        string expectedDomain,
        bool hostOnly,
        bool supplied)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Domain, Is.EqualTo(expectedDomain));
        Assert.That(result.HostOnly, Is.EqualTo(hostOnly));
        Assert.That(result.DomainAttributeSupplied, Is.EqualTo(supplied));
    }

    static void AssertFailure(
        CookiePublicSuffixResult result,
        CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues.Single().ErrorCode, Is.EqualTo(expected));
    }

    static void AssertSite(
        CookieSiteResult result,
        string expectedSite,
        string expectedHost,
        bool fallback)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Site, Is.EqualTo(expectedSite));
        Assert.That(result.SiteHost, Is.EqualTo(expectedHost));
        Assert.That(result.UsedHostFallback, Is.EqualTo(fallback));
    }

    static void AssertWarning(CookieIssue[] issues)
    {
        Assert.That(issues, Has.Length.EqualTo(1));
        Assert.That(issues[0].ErrorCode,
            Is.EqualTo(CookieErrorCode.PublicSuffixListUnavailable));
        Assert.That(issues[0].Severity,
            Is.EqualTo(CookieParseSeverity.Warning));
    }
}

public class SnapshotPublicSuffixList : IPublicSuffixList
{
    public HashSet<string> ExactRules = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> WildcardRules = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> ExceptionRules = new HashSet<string>(StringComparer.Ordinal);

    public SnapshotPublicSuffixList(string[] rules)
    {
        foreach (string rule in rules)
        {
            AddRule(rule);
        }
    }

    public static SnapshotPublicSuffixList Load(string path)
    {
        return new SnapshotPublicSuffixList(File.ReadAllLines(path));
    }

    public void AddRule(string suppliedRule)
    {
        string rule = suppliedRule.Trim().TrimStart('\uFEFF');
        int comment = rule.IndexOf("//", StringComparison.Ordinal);
        if (comment >= 0)
        {
            rule = rule.Substring(0, comment).Trim();
        }

        if (rule.Length == 0)
        {
            return;
        }

        bool exception = rule[0] == '!';
        bool wildcard = rule.StartsWith("*.", StringComparison.Ordinal);
        if (exception)
        {
            rule = rule.Substring(1);
        }
        else if (wildcard)
        {
            rule = rule.Substring(2);
        }

        global::System.Globalization.IdnMapping mapping =
            new global::System.Globalization.IdnMapping
            {
                AllowUnassigned = false,
                UseStd3AsciiRules = true
            };
        string canonical = mapping.GetAscii(rule).ToLowerInvariant();
        if (exception)
        {
            ExceptionRules.Add(canonical);
        }
        else if (wildcard)
        {
            WildcardRules.Add(canonical);
        }
        else
        {
            ExactRules.Add(canonical);
        }
    }

    public string? GetPublicSuffix(string canonicalDomain)
    {
        string[] labels = canonicalDomain.Split('.');
        int exceptionStart = -1;
        int bestStart = labels.Length - 1;
        int index = 0;
        while (index < labels.Length)
        {
            string suffix = string.Join(
                ".", labels, index, labels.Length - index);
            if (ExceptionRules.Contains(suffix))
            {
                exceptionStart = index;
                break;
            }

            if (ExactRules.Contains(suffix) && index < bestStart)
            {
                bestStart = index;
            }

            if (index + 1 < labels.Length)
            {
                string wildcardBase = string.Join(
                    ".", labels, index + 1, labels.Length - index - 1);
                if (WildcardRules.Contains(wildcardBase) && index < bestStart)
                {
                    bestStart = index;
                }
            }

            index = index + 1;
        }

        if (exceptionStart >= 0)
        {
            return string.Join(
                ".",
                labels,
                exceptionStart + 1,
                labels.Length - exceptionStart - 1);
        }

        return string.Join(
            ".", labels, bestStart, labels.Length - bestStart);
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        string publicSuffix = GetPublicSuffix(canonicalDomain)!;
        string[] domainLabels = canonicalDomain.Split('.');
        string[] suffixLabels = publicSuffix.Split('.');
        if (domainLabels.Length <= suffixLabels.Length)
        {
            return null;
        }

        int start = domainLabels.Length - suffixLabels.Length - 1;
        return string.Join(
            ".", domainLabels, start, domainLabels.Length - start);
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return string.Equals(
            GetPublicSuffix(canonicalDomain),
            canonicalDomain,
            StringComparison.Ordinal);
    }
}
