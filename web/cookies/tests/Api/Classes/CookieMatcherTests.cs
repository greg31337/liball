namespace cookies.Tests;

public class CookieMatcherTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Test]
    public void LowLevelMethodsUseCanonicalRfcAlgorithmsThroughContract()
    {
        ICookieMatcher matcher = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime));
        Uri uri = new Uri("https://Shop.BÜCHER.Example/account/page?q=1");

        Assert.That(
            matcher.CanonicalizeHost(uri),
            Is.EqualTo("shop.xn--bcher-kva.example"));
        Assert.That(matcher.DomainMatches("shop.example.test", "example.test"), Is.True);
        Assert.That(matcher.DomainMatches("badexample.test", "example.test"), Is.False);
        Assert.That(matcher.ComputeDefaultPath(uri), Is.EqualTo("/account"));
        Assert.That(matcher.PathMatches("/account/page", "/account"), Is.True);
        Assert.That(matcher.PathMatches("/accounts", "/account"), Is.False);
        Assert.That(
            matcher.ComputeSchemefulSite(new Uri("https://shop.example.test/path")),
            Is.EqualTo("https://example.test"));
    }

    [Test]
    public void ScalarMethodsThrowSafePolicyExceptionsForUnrepresentableFailures()
    {
        ICookieMatcher matcher = Matcher(
            Options(),
            new ThrowingPublicSuffixList(),
            new MatcherClock(EvaluationTime));

        CookiePolicyException host = Assert.Throws<CookiePolicyException>(
            () => matcher.CanonicalizeHost(new Uri("ftp://example.test/path")))!;
        CookiePolicyException path = Assert.Throws<CookiePolicyException>(
            () => matcher.ComputeDefaultPath(new Uri("relative", UriKind.Relative)))!;
        CookiePolicyException site = Assert.Throws<CookiePolicyException>(
            () => matcher.ComputeSchemefulSite(new Uri("https://shop.example.test/path")))!;
        CookiePolicyException expiry = Assert.Throws<CookiePolicyException>(
            () => matcher.IsCookieExpired(null!, EvaluationTime))!;

        Assert.That(host.Message, Is.EqualTo("Cookie matching failed with error code 11."));
        Assert.That(path.Message, Is.EqualTo("Cookie matching failed with error code 12."));
        Assert.That(site.Message, Is.EqualTo("Cookie matching failed with error code 16."));
        Assert.That(expiry.Message, Is.EqualTo("Cookie matching failed with error code 5."));
    }

    [Test]
    public void ExpiryMethodUsesInclusiveUtcBoundary()
    {
        ICookieMatcher matcher = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime));
        StoredCookie cookie = Cookie();
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime.ToOffset(TimeSpan.FromHours(5));

        Assert.That(matcher.IsCookieExpired(cookie, EvaluationTime.AddTicks(-1)), Is.False);
        Assert.That(matcher.IsCookieExpired(cookie, EvaluationTime), Is.True);
    }

    [Test]
    public void EligibleCookieProducesAnOrderedRequestPair()
    {
        MatcherClock clock = new MatcherClock(EvaluationTime);
        StoredCookie cookie = Cookie();
        cookie.IsValueQuoted = true;
        CookieMatcher matcher = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            clock);

        CookieMatchResult result = matcher.EvaluateCookieMatch(cookie, Context());

        AssertIncluded(result, cookie, []);
        Assert.That(result.Pair!.Name, Is.EqualTo("id"));
        Assert.That(result.Pair.Value, Is.EqualTo("value"));
        Assert.That(result.Pair.IsQuoted, Is.True);
        Assert.That(clock.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void ConstructorOptionsAndMatchResultsAreMutationIsolated()
    {
        CookieJarOptions options = Options();
        options.EnforceSameSite = false;
        StoredCookie cookie = Cookie();
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Strict;
        cookie.Extensions = [new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }];
        CookieRequestContext context = Context();
        context.SameSiteContext = CookieSameSiteContext.CrossSite;
        CookieMatcher matcher = Matcher(
            options,
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime));
        options.EnforceSameSite = true;

        CookieMatchResult result = matcher.EvaluateCookieMatch(cookie, context);
        cookie.Value = "changed-input";
        cookie.Extensions[0].Value = "changed-input";
        result.Cookie!.Value = "changed-output";
        result.Cookie.Extensions[0].Value = "changed-output";

        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(result.Cookie, Is.Not.SameAs(cookie));
        Assert.That(result.Cookie.Extensions, Is.Not.SameAs(cookie.Extensions));
        Assert.That(result.Cookie.Extensions[0], Is.Not.SameAs(cookie.Extensions[0]));
        Assert.That(cookie.Value, Is.EqualTo("changed-input"));
        Assert.That(cookie.Extensions[0].Value, Is.EqualTo("changed-input"));
    }

    [Test]
    public void OptionalMissingPslWarningSurvivesSuccessfulMatch()
    {
        CookieJarOptions options = Options();
        options.RequirePublicSuffixList = false;
        StoredCookie cookie = Cookie();

        CookieMatchResult result = Matcher(
            options,
            null,
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(
                cookie,
                Context());

        AssertIncluded(
            result,
            cookie,
            [CookieErrorCode.PublicSuffixListUnavailable]);
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
    }

    [Test]
    public void PartitionCheckIsTheFirstSelectionStep()
    {
        MatcherClock clock = new MatcherClock(EvaluationTime);
        StoredCookie cookie = Cookie();
        cookie.HasPartitionKey = true;
        cookie.PartitionKey = "https://first.example";
        cookie.Secure = true;
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime.AddDays(-1);
        CookieRequestContext context = Context();
        context.HasTopLevelSite = true;
        context.TopLevelSite = "https://second.example";

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            clock).EvaluateCookieMatch(cookie, context);

        AssertExcluded(result, cookie, CookieErrorCode.PartitionMismatch);
        Assert.That(clock.CallCount, Is.Zero);
    }

    [Test]
    public void DomainCheckPrecedesPslPathAndExpiry()
    {
        MatcherClock clock = new MatcherClock(EvaluationTime);
        StoredCookie cookie = Cookie();
        cookie.Domain = "other.test";
        cookie.Path = "/other";
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime.AddDays(-1);

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", ["other.test"]),
            clock).EvaluateCookieMatch(cookie, Context());

        AssertExcluded(result, cookie, CookieErrorCode.DomainMismatch);
        Assert.That(clock.CallCount, Is.Zero);
    }

    [Test]
    public void CurrentPslRevalidationPrecedesPathAndExpiry()
    {
        MatcherClock clock = new MatcherClock(EvaluationTime);
        StoredCookie cookie = Cookie();
        cookie.Path = "/other";
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime.AddDays(-1);

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", ["example.test"]),
            clock).EvaluateCookieMatch(cookie, Context());

        AssertExcluded(result, cookie, CookieErrorCode.PublicSuffix);
        Assert.That(clock.CallCount, Is.Zero);
    }

    [Test]
    public void PathCheckPrecedesExpiryAndPreservesEarlierWarning()
    {
        CookieJarOptions options = Options();
        options.RequirePublicSuffixList = false;
        MatcherClock clock = new MatcherClock(EvaluationTime);
        StoredCookie cookie = Cookie();
        cookie.Path = "/other";
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime.AddDays(-1);

        CookieMatchResult result = Matcher(options, null, clock).EvaluateCookieMatch(
            cookie,
            Context());

        AssertExcluded(result, cookie, CookieErrorCode.InvalidPath);
        Assert.That(
            result.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[]
            {
                CookieErrorCode.PublicSuffixListUnavailable,
                CookieErrorCode.InvalidPath
            }));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(result.Issues[1].Severity, Is.EqualTo(CookieParseSeverity.Error));
        Assert.That(clock.CallCount, Is.Zero);
    }

    [Test]
    public void ExpiryPrecedesSecureHttpOnlyAndSameSiteChecks()
    {
        StoredCookie cookie = Cookie();
        cookie.HasExpires = true;
        cookie.Expires = EvaluationTime;
        cookie.Secure = true;
        cookie.HttpOnly = true;
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Strict;
        CookieRequestContext context = Context();
        context.RequestUri = new Uri("http://shop.example.test/account/page");
        context.AccessKind = CookieAccessKind.NonHttp;
        context.SameSiteContext = CookieSameSiteContext.CrossSite;

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, context);

        AssertExcluded(result, cookie, CookieErrorCode.Expired);
    }

    [Test]
    public void SecureCheckPrecedesHttpOnlyAndSameSiteChecks()
    {
        StoredCookie cookie = Cookie();
        cookie.Secure = true;
        cookie.HttpOnly = true;
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Strict;
        CookieRequestContext context = Context();
        context.RequestUri = new Uri("http://shop.example.test/account/page");
        context.AccessKind = CookieAccessKind.NonHttp;
        context.SameSiteContext = CookieSameSiteContext.CrossSite;

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, context);

        AssertExcluded(result, cookie, CookieErrorCode.InsecureSecureCookie);
    }

    [Test]
    public void HttpOnlyCheckPrecedesSameSiteCheck()
    {
        StoredCookie cookie = Cookie();
        cookie.HttpOnly = true;
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Strict;
        CookieRequestContext context = Context();
        context.AccessKind = CookieAccessKind.NonHttp;
        context.SameSiteContext = CookieSameSiteContext.CrossSite;

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, context);

        AssertExcluded(result, cookie, CookieErrorCode.HttpOnlyApiViolation);
    }

    [Test]
    public void SameSiteIsTheFinalBuiltInSelectionCheck()
    {
        StoredCookie cookie = Cookie();
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Strict;
        CookieRequestContext context = Context();
        context.SameSiteContext = CookieSameSiteContext.CrossSite;

        CookieMatchResult result = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, context);

        AssertExcluded(result, cookie, CookieErrorCode.SameSiteViolation);
    }

    [Test]
    public void HostOnlyCookieRequiresExactRequestHost()
    {
        StoredCookie cookie = Cookie();
        cookie.HostOnly = true;

        CookieMatchResult subdomain = Matcher(
            Options(),
            new ThrowingPublicSuffixList(),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, Context());
        CookieRequestContext exactContext = Context();
        exactContext.RequestUri = new Uri("https://example.test/account/page");
        CookieMatchResult exact = Matcher(
            Options(),
            new ThrowingPublicSuffixList(),
            new MatcherClock(EvaluationTime)).EvaluateCookieMatch(cookie, exactContext);

        AssertExcluded(subdomain, cookie, CookieErrorCode.DomainMismatch);
        AssertIncluded(exact, cookie, []);
    }

    [Test]
    public void InvalidInputsDependenciesAndClockFailuresBecomeStableExclusions()
    {
        CookieMatcher matcher = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime));
        CookiePolicyException missingPolicy = Assert.Throws<CookiePolicyException>(() =>
            new CookieMatcher(
                Options(),
                null,
                null!,
                new MatcherClock(EvaluationTime)));
        CookieMatcher throwingClock = Matcher(
            Options(),
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(EvaluationTime) { Throw = true });
        CookieRequestContext missingUri = Context();
        missingUri.RequestUri = null;

        CookieMatchResult nullCookie = matcher.EvaluateCookieMatch(null!, Context());
        CookieMatchResult nullContext = matcher.EvaluateCookieMatch(Cookie(), null!);
        CookieMatchResult absentUri = matcher.EvaluateCookieMatch(Cookie(), missingUri);
        CookieMatchResult badClock = throwingClock.EvaluateCookieMatch(Cookie(), Context());

        AssertExcluded(nullCookie, null, CookieErrorCode.InvalidAttribute);
        AssertExcluded(nullContext, nullContext.Cookie, CookieErrorCode.InvalidAttribute);
        AssertExcluded(absentUri, absentUri.Cookie, CookieErrorCode.InvalidHost);
        Assert.That(missingPolicy.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
        AssertExcluded(badClock, badClock.Cookie, CookieErrorCode.InvalidConfiguration);
    }

    static CookieMatcher Matcher(
        CookieJarOptions options,
        IPublicSuffixList? publicSuffixList,
        ICookieClock clock)
    {
        return new CookieMatcher(
            options,
            publicSuffixList,
            new DefaultSecureOriginPolicy(false),
            clock);
    }

    static CookieJarOptions Options()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            EnforceSameSite = true,
            DefaultSameSiteMode = CookieSameSiteMode.Lax,
            RequireSecureForSameSiteNone = true,
            RequirePublicSuffixList = true,
            EnablePartitionedCookies = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static StoredCookie Cookie()
    {
        return new StoredCookie
        {
            Name = "id",
            Value = "value",
            Domain = "example.test",
            Path = "/account",
            HostOnly = false,
            CreationTime = EvaluationTime.AddMinutes(-10)
        };
    }

    static CookieRequestContext Context()
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri("https://shop.example.test/account/page"),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http
        };
    }

    static void AssertIncluded(
        CookieMatchResult result,
        StoredCookie cookie,
        CookieErrorCode[] issueCodes)
    {
        Assert.That(result.Cookie, Is.Not.SameAs(cookie));
        Assert.That(result.Cookie!.Name, Is.EqualTo(cookie.Name));
        Assert.That(result.Cookie.Value, Is.EqualTo(cookie.Value));
        Assert.That(result.Pair, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(
            result.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(issueCodes));
    }

    static void AssertExcluded(
        CookieMatchResult result,
        StoredCookie? cookie,
        CookieErrorCode errorCode)
    {
        if (cookie == null)
        {
            Assert.That(result.Cookie, Is.Null);
        }
        else
        {
            Assert.That(result.Cookie!.Name, Is.EqualTo(cookie.Name));
            Assert.That(result.Cookie.Value, Is.EqualTo(cookie.Value));
        }
        Assert.That(result.Pair, Is.Null);
        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Excluded));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues, Is.Not.Empty);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == errorCode), Is.True);
        Assert.That(result.Issues.Last().Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}

public class MatcherClock : ICookieClock
{
    public DateTimeOffset Value;
    public int CallCount;
    public bool Throw;

    public MatcherClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        CallCount = CallCount + 1;
        if (Throw)
        {
            throw new InvalidOperationException("clock failure");
        }

        return Value;
    }
}

public class MatcherPublicSuffixList : IPublicSuffixList
{
    public string? RegistrableDomain;
    public HashSet<string> PublicSuffixes;

    public MatcherPublicSuffixList(string? registrableDomain, string[] publicSuffixes)
    {
        RegistrableDomain = registrableDomain;
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
        return RegistrableDomain;
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return PublicSuffixes.Contains(canonicalDomain);
    }
}
