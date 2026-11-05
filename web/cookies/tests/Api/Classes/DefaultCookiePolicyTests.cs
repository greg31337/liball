namespace cookies.Tests;

public class DefaultCookiePolicyTests
{
    static readonly DateTimeOffset ReceiptTime =
        new DateTimeOffset(2032, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Test]
    public void AcceptanceProducesCompleteCanonicalStoredCandidate()
    {
        CookieJarOptions options = Options();
        CookieAttribute extension = new CookieAttribute
        {
            Name = "Priority",
            HasValue = true,
            Value = "High"
        };
        SetCookie cookie = Cookie();
        cookie.HasMaxAge = true;
        cookie.MaxAgeSeconds = 60;
        cookie.HasDomain = true;
        cookie.Domain = "example.test";
        cookie.Secure = true;
        cookie.HttpOnly = true;
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Lax;
        cookie.Partitioned = true;
        cookie.IsValueQuoted = false;
        cookie.Extensions = [extension];
        CookieSetContext context = Context(options);

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(cookie, context);

        AssertAccepted(decision);
        StoredCookie normalized = decision.NormalizedCookie!;
        Assert.That(normalized.Name, Is.EqualTo("id"));
        Assert.That(normalized.Value, Is.EqualTo("value"));
        Assert.That(normalized.Domain, Is.EqualTo("example.test"));
        Assert.That(normalized.HostOnly, Is.False);
        Assert.That(normalized.Path, Is.EqualTo("/docs"));
        Assert.That(normalized.HasExpires, Is.True);
        Assert.That(normalized.Expires, Is.EqualTo(ReceiptTime.AddSeconds(60)));
        Assert.That(normalized.Persistent, Is.True);
        Assert.That(normalized.CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(normalized.LastAccessTime, Is.EqualTo(ReceiptTime));
        Assert.That(normalized.Secure, Is.True);
        Assert.That(normalized.HttpOnly, Is.True);
        Assert.That(normalized.HasSameSite, Is.True);
        Assert.That(normalized.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Lax));
        Assert.That(normalized.HasPartitionKey, Is.True);
        Assert.That(normalized.PartitionKey, Is.EqualTo("https://top.example"));
        Assert.That(normalized.Extensions, Is.Not.SameAs(cookie.Extensions));
        Assert.That(normalized.Extensions[0], Is.Not.SameAs(cookie.Extensions[0]));
        Assert.That(normalized.Extensions[0].Name, Is.EqualTo(extension.Name));
        Assert.That(normalized.Extensions[0].Value, Is.EqualTo(extension.Value));
        Assert.That(normalized.SequenceNumber, Is.Zero);
        Assert.That(decision.Issues, Is.Empty);
    }

    [Test]
    public void SessionAndDisabledPartitionAttributesNormalizeWithoutKeys()
    {
        CookieJarOptions options = Options();
        options.EnablePartitionedCookies = false;
        SetCookie cookie = Cookie();
        cookie.Partitioned = true;

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        AssertAccepted(decision);
        Assert.That(decision.NormalizedCookie!.HasExpires, Is.False);
        Assert.That(decision.NormalizedCookie.Persistent, Is.False);
        Assert.That(decision.NormalizedCookie.HasPartitionKey, Is.False);
        Assert.That(decision.NormalizedCookie.PartitionKey, Is.Empty);
    }

    [Test]
    public void ParsedInvalidSameSiteDefaultRemainsObservableAndAcceptable()
    {
        CookieJarOptions options = Options();
        SetCookie cookie = Cookie();
        cookie.HasSameSite = true;
        cookie.SameSiteMode = CookieSameSiteMode.Default;

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        AssertAccepted(decision);
        Assert.That(decision.NormalizedCookie!.HasSameSite, Is.True);
        Assert.That(decision.NormalizedCookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Default));
    }

    [Test]
    public void MandatorySyntaxFloorRunsBeforeApplicationDomainPolicy()
    {
        CookieJarOptions options = Options();
        options.AllowedDomains = ["example.test"];
        SetCookie cookie = Cookie();
        cookie.Value = "safe\r\nInjected: yes";

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        AssertRejected(decision, CookieErrorCode.HeaderInjection);
    }

    [Test]
    public void AcceptancePipelineReturnsStableStageReasons()
    {
        CookieJarOptions options = Options();
        DefaultCookiePolicy policy = Policy(options);

        SetCookie invalidPath = Cookie();
        invalidPath.HasPath = true;
        invalidPath.Path = "relative";
        SetCookie wrongDomain = Cookie();
        wrongDomain.HasDomain = true;
        wrongDomain.Domain = "other.test";
        SetCookie publicSuffix = Cookie();
        publicSuffix.HasDomain = true;
        publicSuffix.Domain = "test";
        SetCookie insecure = Cookie();
        insecure.Secure = true;
        SetCookie crossSite = Cookie();
        crossSite.HasSameSite = true;
        crossSite.SameSiteMode = CookieSameSiteMode.Strict;
        SetCookie badPrefix = Cookie();
        badPrefix.Name = "__Secure-id";
        SetCookie missingPartition = Cookie();
        missingPartition.Partitioned = true;
        missingPartition.Secure = true;

        CookieSetContext unsupportedContext = Context(options);
        unsupportedContext.Origin = new Uri("ftp://shop.example.test/docs/page");
        CookieSetContext insecureContext = Context(options);
        insecureContext.Origin = new Uri("http://shop.example.test/docs/page");
        CookieSetContext crossSiteContext = Context(options);
        crossSiteContext.SameSiteContext = CookieSameSiteContext.CrossSite;
        CookieSetContext noPartitionContext = Context(options);
        noPartitionContext.HasTopLevelSite = false;
        noPartitionContext.TopLevelSite = "";
        DefaultCookiePolicy suffixPolicy = Policy(
            options,
            new MatcherPublicSuffixList("shop.test", ["test"]));

        AssertRejected(
            policy.EvaluateCookieAcceptance(invalidPath, Context(options)),
            CookieErrorCode.InvalidPath);
        AssertRejected(
            policy.EvaluateCookieAcceptance(Cookie(), unsupportedContext),
            CookieErrorCode.UnsupportedScheme);
        AssertRejected(
            policy.EvaluateCookieAcceptance(wrongDomain, Context(options)),
            CookieErrorCode.DomainMismatch);
        AssertRejected(
            suffixPolicy.EvaluateCookieAcceptance(
                publicSuffix,
                ContextWithOrigin(options, "https://shop.test/docs/page")),
            CookieErrorCode.PublicSuffix);
        AssertRejected(
            policy.EvaluateCookieAcceptance(insecure, insecureContext),
            CookieErrorCode.InsecureSecureCookie);
        AssertRejected(
            policy.EvaluateCookieAcceptance(crossSite, crossSiteContext),
            CookieErrorCode.SameSiteViolation);
        AssertRejected(
            policy.EvaluateCookieAcceptance(badPrefix, Context(options)),
            CookieErrorCode.PrefixViolation);
        AssertRejected(
            policy.EvaluateCookieAcceptance(missingPartition, noPartitionContext),
            CookieErrorCode.MissingPartitionContext);
    }

    [Test]
    public void SecurityRequirementsPrecedeApplicationBlockRules()
    {
        CookieJarOptions options = Options();
        options.BlockedDomains = ["example.test"];
        SetCookie cookie = Cookie();
        cookie.Secure = true;
        CookieSetContext context = ContextWithOrigin(
            options,
            "http://shop.example.test/docs/page");

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(cookie, context);

        AssertRejected(decision, CookieErrorCode.InsecureSecureCookie);
    }

    [Test]
    public void NonHttpAcceptanceCannotReplaceExistingHttpOnlyCookie()
    {
        CookieJarOptions options = Options();
        CookieSetContext context = Context(options);
        context.AccessKind = CookieAccessKind.NonHttp;
        context.ExistingCookie = new StoredCookie { HttpOnly = true };

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            Cookie(),
            context);

        AssertRejected(decision, CookieErrorCode.HttpOnlyApiViolation);
    }

    [Test]
    public void InsecureAcceptanceCannotOverlayExistingSecureCookie()
    {
        CookieJarOptions options = Options();
        options.ProtectSecureCookieOverlay = true;
        CookieSetContext context = ContextWithOrigin(
            options,
            "http://shop.example.test/docs/page");
        context.ExistingCookies =
        [
            new StoredCookie
            {
                Name = "id",
                Domain = "shop.example.test",
                Path = "/docs",
                Secure = true
            }
        ];

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            Cookie(),
            context);

        AssertRejected(decision, CookieErrorCode.SecureOverlayViolation);
    }

    [Test]
    public void DomainAllowListIncludesSubdomainsAndRejectsOutsideDomains()
    {
        CookieJarOptions allowedOptions = Options();
        allowedOptions.AllowedDomains = ["example.test"];
        CookieJarOptions rejectedOptions = Options();
        rejectedOptions.AllowedDomains = ["other.test"];

        CookieSetDecision allowed = Policy(allowedOptions).EvaluateCookieAcceptance(
            Cookie(),
            Context(allowedOptions));
        CookieSetDecision rejected = Policy(rejectedOptions).EvaluateCookieAcceptance(
            Cookie(),
            Context(rejectedOptions));

        AssertAccepted(allowed);
        Assert.That(allowed.NormalizedCookie!.Domain, Is.EqualTo("shop.example.test"));
        AssertRejected(rejected, CookieErrorCode.PolicyRejection);
    }

    [Test]
    public void BlockListOverridesAllowListForDomainsAndSubdomains()
    {
        CookieJarOptions options = Options();
        options.AllowedDomains = ["example.test"];
        options.BlockedDomains = ["example.test"];

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            Cookie(),
            Context(options));

        AssertRejected(decision, CookieErrorCode.PolicyRejection);
    }

    [Test]
    public void InvalidApplicationDomainConfigurationFailsDeterministically()
    {
        CookieJarOptions uppercase = Options();
        uppercase.AllowedDomains = ["EXAMPLE.TEST"];
        CookieJarOptions empty = Options();
        empty.BlockedDomains = [""];
        CookieJarOptions absent = Options();
        absent.AllowedDomains = null!;

        CookiePolicyException uppercaseFailure = Assert.Throws<CookiePolicyException>(
            () => Policy(uppercase));
        CookiePolicyException emptyFailure = Assert.Throws<CookiePolicyException>(
            () => Policy(empty));
        CookiePolicyException absentFailure = Assert.Throws<CookiePolicyException>(
            () => Policy(absent));

        Assert.That(
            new[]
            {
                uppercaseFailure.Message,
                emptyFailure.Message,
                absentFailure.Message
            },
            Is.All.EqualTo("Cookie service configuration failed with error code 43."));
    }

    [Test]
    public void DefensiveCopyDoesNotRepairInvalidMutableInput()
    {
        CookieJarOptions options = Options();
        SetCookie cookie = Cookie();
        cookie.Extensions = null!;

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        AssertRejected(decision, CookieErrorCode.InvalidAttribute);
    }

    [Test]
    public void OptionalPslWarningIsPreservedWhenApplicationPolicyRejects()
    {
        CookieJarOptions options = Options();
        options.RequirePublicSuffixList = false;
        options.BlockedDomains = ["example.test"];
        SetCookie cookie = Cookie();
        cookie.HasDomain = true;
        cookie.Domain = "example.test";

        CookieSetDecision decision = Policy(options, null).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        AssertRejected(decision, CookieErrorCode.PolicyRejection);
        Assert.That(
            decision.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[]
            {
                CookieErrorCode.PublicSuffixListUnavailable,
                CookieErrorCode.PolicyRejection
            }));
        Assert.That(decision.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(decision.Issues[1].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    [Test]
    public void SilentPrefixPolicyProducesIgnoredDecisionWithoutCandidate()
    {
        CookieJarOptions options = Options();
        options.PrefixPolicy = CookiePrefixPolicy.SilentReject;
        SetCookie cookie = Cookie();
        cookie.Name = "__Secure-id";

        CookieSetDecision decision = Policy(options).EvaluateCookieAcceptance(
            cookie,
            Context(options));

        Assert.That(decision.Status, Is.EqualTo(CookieSetStatus.Ignored));
        Assert.That(decision.ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
        Assert.That(decision.NormalizedCookie, Is.Null);
        Assert.That(decision.Issues, Is.Empty);
    }

    [Test]
    public void ReturnSafetyFloorRejectsHeaderInjectionBeforeApplicationPolicy()
    {
        CookieJarOptions options = Options();
        options.AllowedDomains = ["example.test"];
        StoredCookie cookie = StoredHostCookie();
        cookie.Value = "safe\r\nInjected: yes";

        CookieMatchResult result = Policy(options).EvaluateCookieReturn(
            cookie,
            RequestContext());

        AssertExcluded(result, CookieErrorCode.HeaderInjection);
    }

    [Test]
    public void ClockAndExternalPolicyFailuresProduceConfigurationFailures()
    {
        CookieJarOptions options = Options();
        MatcherClock throwingClock = new MatcherClock(ReceiptTime) { Throw = true };
        CookiePolicyException missingSecurePolicy =
            Assert.Throws<CookiePolicyException>(() =>
                new DefaultCookiePolicy(
                    options,
                    new MatcherPublicSuffixList("example.test", []),
                    null!,
                    new MatcherClock(ReceiptTime)));
        CookieSetDecision clock = Policy(
            options,
            new MatcherPublicSuffixList("example.test", []),
            throwingClock).EvaluateCookieAcceptance(Cookie(), Context(options));
        SetCookie domainCookie = Cookie();
        domainCookie.HasDomain = true;
        domainCookie.Domain = "example.test";
        CookieSetDecision suffix = Policy(
            options,
            new ThrowingPublicSuffixList()).EvaluateCookieAcceptance(
                domainCookie,
                Context(options));

        AssertRejected(clock, CookieErrorCode.InvalidConfiguration);
        Assert.That(missingSecurePolicy.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
        AssertRejected(suffix, CookieErrorCode.PublicSuffixListUnavailable);
    }

    [Test]
    public void ReturnPolicyAppliesMandatoryMatchingBeforeApplicationBlock()
    {
        CookieJarOptions options = Options();
        options.BlockedDomains = ["example.test"];
        DefaultCookiePolicy policy = Policy(options);
        StoredCookie eligible = StoredHostCookie();
        StoredCookie expired = StoredHostCookie();
        expired.HasExpires = true;
        expired.Expires = ReceiptTime;
        CookieRequestContext context = RequestContext();

        CookieMatchResult blocked = policy.EvaluateCookieReturn(eligible, context);
        CookieMatchResult mandatory = policy.EvaluateCookieReturn(expired, context);

        AssertExcluded(blocked, CookieErrorCode.PolicyRejection);
        AssertExcluded(mandatory, CookieErrorCode.Expired);
    }

    [Test]
    public void ReturnPolicyAllowsConfiguredParentDomain()
    {
        CookieJarOptions options = Options();
        options.AllowedDomains = ["example.test"];
        StoredCookie cookie = StoredHostCookie();

        CookieMatchResult result = Policy(options).EvaluateCookieReturn(
            cookie,
            RequestContext());

        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookie, Is.Not.SameAs(cookie));
        Assert.That(result.Cookie!.Name, Is.EqualTo(cookie.Name));
        Assert.That(result.Pair, Is.Not.Null);
    }

    [Test]
    public void ConstructorOptionsAndAcceptanceResultsAreMutationIsolated()
    {
        CookieJarOptions options = Options();
        options.AllowedDomains = ["example.test"];
        DefaultCookiePolicy policy = Policy(options);
        options.AllowedDomains[0] = "other.test";
        options.BlockedDomains = ["example.test"];
        SetCookie input = Cookie();
        input.Extensions = [new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }];

        CookieSetDecision decision = policy.EvaluateCookieAcceptance(
            input,
            Context(Options()));
        input.Value = "changed-input";
        input.Extensions[0].Value = "changed-input";
        decision.NormalizedCookie!.Value = "changed-output";
        decision.NormalizedCookie.Extensions[0].Value = "changed-output";
        CookieMatchResult returned = policy.EvaluateCookieReturn(
            StoredHostCookie(),
            RequestContext());

        AssertAccepted(decision);
        Assert.That(decision.NormalizedCookie.Extensions, Is.Not.SameAs(input.Extensions));
        Assert.That(decision.NormalizedCookie.Extensions[0], Is.Not.SameAs(input.Extensions[0]));
        Assert.That(input.Value, Is.EqualTo("changed-input"));
        Assert.That(input.Extensions[0].Value, Is.EqualTo("changed-input"));
        Assert.That(returned.Status, Is.EqualTo(CookieMatchStatus.Included));
    }

    static DefaultCookiePolicy Policy(CookieJarOptions options)
    {
        return Policy(
            options,
            new MatcherPublicSuffixList("example.test", []),
            new MatcherClock(ReceiptTime));
    }

    static DefaultCookiePolicy Policy(
        CookieJarOptions options,
        IPublicSuffixList? suffixList)
    {
        return Policy(options, suffixList, new MatcherClock(ReceiptTime));
    }

    static DefaultCookiePolicy Policy(
        CookieJarOptions options,
        IPublicSuffixList? suffixList,
        ICookieClock clock)
    {
        return new DefaultCookiePolicy(
            options,
            suffixList,
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
            PrefixPolicy = CookiePrefixPolicy.ErrorReject,
            RequirePublicSuffixList = true,
            EnablePartitionedCookies = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static SetCookie Cookie()
    {
        return new SetCookie
        {
            Name = "id",
            Value = "value"
        };
    }

    static CookieSetContext Context(CookieJarOptions options)
    {
        return ContextWithOrigin(options, "https://shop.example.test/docs/page");
    }

    static CookieSetContext ContextWithOrigin(
        CookieJarOptions options,
        string origin)
    {
        return new CookieSetContext
        {
            Origin = new Uri(origin),
            Options = options,
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }

    static StoredCookie StoredHostCookie()
    {
        return new StoredCookie
        {
            Name = "id",
            Value = "value",
            Domain = "shop.example.test",
            Path = "/docs",
            HostOnly = true,
            CreationTime = ReceiptTime.AddMinutes(-5)
        };
    }

    static CookieRequestContext RequestContext()
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri("https://shop.example.test/docs/page"),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http
        };
    }

    static void AssertAccepted(CookieSetDecision decision)
    {
        Assert.That(decision.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(decision.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(decision.NormalizedCookie, Is.Not.Null);
    }

    static void AssertRejected(
        CookieSetDecision decision,
        CookieErrorCode errorCode)
    {
        Assert.That(decision.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(decision.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(decision.NormalizedCookie, Is.Null);
        Assert.That(decision.Issues, Is.Not.Empty);
        Assert.That(decision.Issues.Any(issue => issue.ErrorCode == errorCode), Is.True);
        Assert.That(decision.Issues.Last().Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    static void AssertExcluded(CookieMatchResult result, CookieErrorCode errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Excluded));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Pair, Is.Null);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == errorCode), Is.True);
    }
}
