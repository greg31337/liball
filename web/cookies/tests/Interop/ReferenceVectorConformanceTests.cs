namespace cookies.Tests;

public class ReferenceVectorConformanceTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2037, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [TestCase(CookieComplianceMode.Rfc6265, "/", "/")]
    [TestCase(CookieComplianceMode.Rfc6265, "/abc", "/")]
    [TestCase(CookieComplianceMode.Rfc6265, "/abc/", "/abc")]
    [TestCase(CookieComplianceMode.Rfc6265, "/abc/xyz", "/abc")]
    [TestCase(CookieComplianceMode.Rfc6265, "/abc/xyz/", "/abc/xyz")]
    [TestCase(CookieComplianceMode.Rfc6265, "/a/b/c.html", "/a/b")]
    public void GoDefaultPathVectorsUseTheRfc6265Profile(
        CookieComplianceMode profile,
        string requestPath,
        string expectedPath)
    {
        CookiePathResult result = CookiePathMatcher.ComputeDefaultPath(
            new Uri("https://example.test" + requestPath));

        Assert.That(profile, Is.EqualTo(CookieComplianceMode.Rfc6265));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Path, Is.EqualTo(expectedPath));
    }

    [TestCase(CookieComplianceMode.Compatible)]
    public void GoQuotedRequestCookieVectorUsesTheCompatibleProfile(
        CookieComplianceMode profile)
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = profile
        };

        CookieHeaderParseResult result = parser.TryParseCookieFields(
            ["Cookie-1=\"v$1\"; c2=\"v2\"; empty="], options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs.Select(pair => pair.Name),
            Is.EqualTo(new[] { "Cookie-1", "c2", "empty" }));
        Assert.That(result.Pairs.Select(pair => pair.Value),
            Is.EqualTo(new[] { "v$1", "v2", "" }));
        Assert.That(result.Pairs.Select(pair => pair.IsQuoted),
            Is.EqualTo(new[] { true, true, false }));
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    public void PythonNetscapeDuplicateNameVectorUsesTheRfc6265Profile(
        CookieComplianceMode profile)
    {
        CookieJarOptions options = Options(profile);
        ICookieJar jar = Jar(options);
        CookieSetContext root = SetContext(
            options, "http://www.acme.com/", "https://acme.com");
        CookieSetContext ammunition = SetContext(
            options, "http://www.acme.com/ammo", "https://acme.com");

        CookieSetResult general = jar.StoreSetCookieHeader(
            "PART_NUMBER=ROCKET_LAUNCHER_0001; Path=/", root);
        CookieSetResult specific = jar.StoreSetCookieHeader(
            "PART_NUMBER=RIDING_ROCKET_0023; Path=/ammo", ammunition);
        CookieSelectionResult selected = jar.CreateRequestCookieFields(
            RequestContext("http://www.acme.com/ammo", "https://acme.com"));

        Assert.That(general.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(specific.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(selected.Success, Is.True);
        Assert.That(selected.FieldValues, Is.EqualTo(new[]
        {
            "PART_NUMBER=RIDING_ROCKET_0023; PART_NUMBER=ROCKET_LAUNCHER_0001"
        }));
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    public void PythonInvalidExpiresVectorRemainsASessionCookie(
        CookieComplianceMode profile)
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = profile
        };

        SetCookieParseResult result = parser.TryParseSetCookie(
            "Customer=WILE_E_COYOTE; Expires=Foo Bar 12 33:22:11 2000", options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie, Is.Not.Null);
        Assert.That(result.Cookie!.HasExpires, Is.False);
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.InvalidDate));
    }

    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    public void ToughCookieLocalhostTrustVectorUsesTheRfc6265BisProfile(
        CookieComplianceMode profile)
    {
        CookieJarOptions trustedOptions = Options(profile);
        trustedOptions.TreatLoopbackAsSecure = true;
        ICookieJar trusted = Jar(trustedOptions);
        CookieJarOptions strictOptions = Options(profile);
        ICookieJar strict = Jar(strictOptions);

        CookieSetResult accepted = trusted.StoreSetCookieHeader(
            "sid=trusted; Secure; Path=/",
            SetContext(trustedOptions, "http://localhost/", "http://localhost"));
        CookieSetResult rejected = strict.StoreSetCookieHeader(
            "sid=strict; Secure; Path=/",
            SetContext(strictOptions, "http://localhost/", "http://localhost"));
        CookieSelectionResult selected = trusted.CreateRequestCookieFields(
            RequestContext("http://localhost/", "http://localhost"));

        Assert.That(accepted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(selected.FieldValues, Is.EqualTo(new[] { "sid=trusted" }));
        Assert.That(rejected.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(rejected.ErrorCode,
            Is.EqualTo(CookieErrorCode.InsecureSecureCookie));
    }

    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    public void CurlPrefixVectorsUseTheRfc6265BisProfile(
        CookieComplianceMode profile)
    {
        CookieJarOptions options = Options(profile);
        options.PrefixPolicy = CookiePrefixPolicy.ErrorReject;
        ICookieJar jar = Jar(options);
        CookieSetContext context = SetContext(
            options, "https://shop.example.test/account", "https://example.test");

        CookieSetResult secure = jar.StoreSetCookieHeader(
            "__Secure-id=valid; Secure", context);
        CookieSetResult missingSecure = jar.StoreSetCookieHeader(
            "__Secure-missing=invalid", context);
        CookieSetResult host = jar.StoreSetCookieHeader(
            "__Host-id=valid; Secure; Path=/", context);
        CookieSetResult hostWithDomain = jar.StoreSetCookieHeader(
            "__Host-domain=invalid; Secure; Path=/; Domain=example.test", context);
        CookieSetResult mixedCase = jar.StoreSetCookieHeader(
            "__SeCuRe-mixed=invalid", context);

        Assert.That(secure.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(host.Status, Is.EqualTo(CookieSetStatus.Accepted));
        AssertPrefixRejected(missingSecure);
        AssertPrefixRejected(hostWithDomain);
        AssertPrefixRejected(mixedCase);
    }

    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    public void WptPartitionIdentityVectorUsesTheRfc6265BisProfile(
        CookieComplianceMode profile)
    {
        CookieJarOptions options = Options(profile);
        options.EnablePartitionedCookies = true;
        ICookieJar jar = Jar(options);
        const string origin = "https://embedded.example.test/resource";

        CookieSetResult unpartitioned = jar.StoreSetCookieHeader(
            "identity=global; Secure; Path=/",
            SetContext(options, origin, "https://first.example"));
        CookieSetResult first = jar.StoreSetCookieHeader(
            "identity=first; Secure; Partitioned; Path=/",
            SetContext(options, origin, "https://first.example"));
        CookieSetResult second = jar.StoreSetCookieHeader(
            "identity=second; Secure; Partitioned; Path=/",
            SetContext(options, origin, "https://second.example"));

        CookieSelectionResult firstSite = jar.CreateRequestCookieFields(
            RequestContext(origin, "https://first.example"));
        CookieSelectionResult secondSite = jar.CreateRequestCookieFields(
            RequestContext(origin, "https://second.example"));
        CookieRequestContext missingSiteContext = RequestContext(origin, "");
        missingSiteContext.HasTopLevelSite = false;
        CookieSelectionResult missingSite = jar.CreateRequestCookieFields(
            missingSiteContext);

        Assert.That(unpartitioned.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(first.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(second.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(firstSite.Pairs.Select(pair => pair.Value),
            Is.EquivalentTo(new[] { "global", "first" }));
        Assert.That(secondSite.Pairs.Select(pair => pair.Value),
            Is.EquivalentTo(new[] { "global", "second" }));
        Assert.That(missingSite.Pairs.Select(pair => pair.Value),
            Is.EqualTo(new[] { "global" }));
    }

    static CookieJarOptions Options(CookieComplianceMode profile)
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = profile,
            RequirePublicSuffixList = true,
            PrefixPolicy = CookiePrefixPolicy.Disabled
        };
        options.ParseOptions.ComplianceMode = profile;
        options.SerializationOptions.ComplianceMode = profile;
        return options;
    }

    static ICookieJar Jar(CookieJarOptions options)
    {
        ReferenceVectorClock clock = new ReferenceVectorClock(EvaluationTime);
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            new ReferenceVectorPublicSuffixList(),
            new DefaultSecureOriginPolicy(options.TreatLoopbackAsSecure),
            clock);
        return new CookieJar(
            options,
            new CookieParser(),
            policy,
            new MemoryCookieStore(),
            new CookieSerializer(),
            clock);
    }

    static CookieSetContext SetContext(
        CookieJarOptions options,
        string origin,
        string topLevelSite)
    {
        return new CookieSetContext
        {
            Origin = new Uri(origin),
            Options = options,
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = topLevelSite
        };
    }

    static CookieRequestContext RequestContext(
        string requestUri,
        string topLevelSite)
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri(requestUri),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = topLevelSite
        };
    }

    static void AssertPrefixRejected(CookieSetResult result)
    {
        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
    }
}

public class ReferenceVectorClock : ICookieClock
{
    public DateTimeOffset Value;

    public ReferenceVectorClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class ReferenceVectorPublicSuffixList : IPublicSuffixList
{
    public string? GetPublicSuffix(string canonicalDomain)
    {
        int separator = canonicalDomain.LastIndexOf('.');
        if (separator < 0)
        {
            return canonicalDomain;
        }

        return canonicalDomain.Substring(separator + 1);
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        int separator = canonicalDomain.LastIndexOf('.');
        if (separator < 0)
        {
            return null;
        }

        int prior = canonicalDomain.LastIndexOf('.', separator - 1);
        return canonicalDomain.Substring(prior + 1);
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain.IndexOf('.') < 0;
    }
}
