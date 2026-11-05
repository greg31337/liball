namespace cookies.Tests;

public class SameSiteConformanceTests
{
    static readonly DateTimeOffset EvaluationTime = new DateTimeOffset(
        2030, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.SameSite, "POST", false, true)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.SameSite, "POST", false, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.SameSite, "POST", false, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.SameSite, "POST", false, true)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, "GET", true, false)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, "GET", false, false)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, "POST", true, false)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, "POST", false, false)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, "GET", true, true)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, "GET", false, false)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, "POST", true, false)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, "POST", false, false)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, "GET", true, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, "GET", false, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, "POST", true, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, "POST", false, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, "GET", true, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, "GET", false, false)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, "POST", true, false)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, "POST", false, false)]
    public void RetrievalMatrixCoversEveryModeRelationMethodAndNavigationType(
        CookieSameSiteMode mode,
        bool hasSameSite,
        CookieSameSiteContext relation,
        string method,
        bool topLevel,
        bool expectedAllowed)
    {
        StoredCookie cookie = Stored(mode, hasSameSite, EvaluationTime);
        CookieRequestContext context = RequestContext(
            relation, method, topLevel);

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, context, ModernOptions(), EvaluationTime);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedAllowed
            ? CookieErrorCode.None
            : CookieErrorCode.SameSiteViolation));
    }

    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.SameSite, false, true)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.SameSite, false, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.SameSite, false, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.SameSite, false, true)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, true, true)]
    [TestCase(CookieSameSiteMode.Strict, true, CookieSameSiteContext.CrossSite, false, false)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, true, true)]
    [TestCase(CookieSameSiteMode.Lax, true, CookieSameSiteContext.CrossSite, false, false)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, true, true)]
    [TestCase(CookieSameSiteMode.None, true, CookieSameSiteContext.CrossSite, false, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, true, true)]
    [TestCase(CookieSameSiteMode.Default, false, CookieSameSiteContext.CrossSite, false, false)]
    public void CreationMatrixCoversSameSiteTopLevelAndSubresourceRestrictions(
        CookieSameSiteMode mode,
        bool hasSameSite,
        CookieSameSiteContext relation,
        bool topLevel,
        bool expectedAllowed)
    {
        SetCookie cookie = new SetCookie
        {
            HasSameSite = hasSameSite,
            SameSiteMode = mode,
            Secure = true
        };
        CookieSetContext context = new CookieSetContext
        {
            Options = ModernOptions(),
            SameSiteContext = relation,
            AccessKind = CookieAccessKind.Http,
            IsTopLevelNavigation = topLevel
        };

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, context);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
    }

    [Test]
    public void RedirectChainForcesCrossSiteButLaxUsesTheCurrentHopMethod()
    {
        CookieJarOptions options = ModernOptions();
        CookieRequestContext safeHop = RequestContext(
            CookieSameSiteContext.SameSite, "GET", true);
        safeHop.RedirectChainIsCrossSite = true;
        CookieRequestContext unsafeHop = RequestContext(
            CookieSameSiteContext.SameSite, "POST", true);
        unsafeHop.RedirectChainIsCrossSite = true;

        CookieSameSiteResult strict = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Strict, true, EvaluationTime),
            safeHop,
            options,
            EvaluationTime);
        CookieSameSiteResult laxSafe = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true, EvaluationTime),
            safeHop,
            options,
            EvaluationTime);
        CookieSameSiteResult laxUnsafe = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true, EvaluationTime),
            unsafeHop,
            options,
            EvaluationTime);

        Assert.That(strict.Allowed, Is.False);
        Assert.That(laxSafe.Allowed, Is.True);
        Assert.That(laxUnsafe.Allowed, Is.False);
    }

    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void ReloadAndActivationDetermineWhetherCrossSiteRelationIsRetained(
        bool isReload,
        bool hasUserActivation,
        bool expectedAllowed)
    {
        CookieRequestContext context = RequestContext(
            CookieSameSiteContext.CrossSite, "POST", true);
        context.IsReload = isReload;
        context.HasUserActivation = hasUserActivation;

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Strict, true, EvaluationTime),
            context,
            ModernOptions(),
            EvaluationTime);

        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
    }

    [Test]
    public void LaxAllowingUnsafeUsesInclusiveAgeBoundariesOnlyForImplicitDefaults()
    {
        CookieJarOptions options = ModernOptions();
        options.EnableLaxAllowingUnsafe = true;
        options.LaxAllowingUnsafeGracePeriod = TimeSpan.FromMinutes(2);
        CookieRequestContext context = RequestContext(
            CookieSameSiteContext.CrossSite, "POST", true);
        StoredCookie future = Stored(
            CookieSameSiteMode.Default, false, EvaluationTime.AddTicks(1));
        StoredCookie newCookie = Stored(
            CookieSameSiteMode.Default, false, EvaluationTime);
        StoredCookie boundary = Stored(
            CookieSameSiteMode.Default, false, EvaluationTime.AddMinutes(-2));
        StoredCookie old = Stored(
            CookieSameSiteMode.Default,
            false,
            EvaluationTime.AddMinutes(-2).AddTicks(-1));
        StoredCookie explicitDefault = Stored(
            CookieSameSiteMode.Default, true, EvaluationTime);

        CookieSameSiteResult futureResult = Evaluate(
            future, context, options);
        CookieSameSiteResult newResult = Evaluate(
            newCookie, context, options);
        CookieSameSiteResult boundaryResult = Evaluate(
            boundary, context, options);
        CookieSameSiteResult oldResult = Evaluate(old, context, options);
        CookieSameSiteResult explicitResult = Evaluate(
            explicitDefault, context, options);

        AssertGraceAllowed(futureResult);
        AssertGraceAllowed(newResult);
        AssertGraceAllowed(boundaryResult);
        Assert.That(oldResult.Allowed, Is.False);
        Assert.That(explicitResult.Allowed, Is.False);
    }

    [Test]
    public void SchemeChangesProduceDifferentSchemefulSites()
    {
        IPublicSuffixList suffixes = new SameSitePublicSuffixList();

        CookieSiteResult secure = CookieSiteCalculator.Compute(
            new Uri("https://shop.example.test/path"), suffixes, true);
        CookieSiteResult insecure = CookieSiteCalculator.Compute(
            new Uri("http://shop.example.test/path"), suffixes, true);
        CookieSiteResult sibling = CookieSiteCalculator.Compute(
            new Uri("https://api.example.test/path"), suffixes, true);

        Assert.That(secure.Site, Is.EqualTo("https://example.test"));
        Assert.That(insecure.Site, Is.EqualTo("http://example.test"));
        Assert.That(sibling.Site, Is.EqualTo(secure.Site));
        Assert.That(insecure.Site, Is.Not.EqualTo(secure.Site));
    }

    [TestCase(CookieComplianceMode.Rfc6265, true)]
    [TestCase(CookieComplianceMode.Rfc6265Bis, false)]
    [TestCase(CookieComplianceMode.Compatible, false)]
    public void ExplicitNoneWithoutSecureUsesProfileSpecificCreationRules(
        CookieComplianceMode mode,
        bool expectedAllowed)
    {
        CookieJarOptions options = ModernOptions();
        options.ComplianceMode = mode;
        options.RequireSecureForSameSiteNone = true;
        SetCookie cookie = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.None,
            Secure = false
        };
        CookieSetContext context = new CookieSetContext
        {
            Options = options,
            SameSiteContext = CookieSameSiteContext.CrossSite,
            AccessKind = CookieAccessKind.Http
        };

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, context);

        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedAllowed
            ? CookieErrorCode.None
            : CookieErrorCode.SameSiteViolation));
    }

    [Test]
    public void SecureNoneOrDisabledRequirementAllowsModernCreation()
    {
        CookieJarOptions required = ModernOptions();
        required.RequireSecureForSameSiteNone = true;
        CookieJarOptions disabled = ModernOptions();
        disabled.RequireSecureForSameSiteNone = false;
        SetCookie secure = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.None,
            Secure = true
        };
        SetCookie insecure = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.None,
            Secure = false
        };

        CookieSameSiteResult secureResult = CookieSameSiteEvaluator.EvaluateCreation(
            secure, SetContext(required));
        CookieSameSiteResult disabledResult = CookieSameSiteEvaluator.EvaluateCreation(
            insecure, SetContext(disabled));

        Assert.That(secureResult.Allowed, Is.True);
        Assert.That(disabledResult.Allowed, Is.True);
    }

    static CookieSameSiteResult Evaluate(
        StoredCookie cookie,
        CookieRequestContext context,
        CookieJarOptions options)
    {
        return CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, context, options, EvaluationTime);
    }

    static CookieJarOptions ModernOptions()
    {
        return new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            EnforceSameSite = true
        };
    }

    static CookieRequestContext RequestContext(
        CookieSameSiteContext relation,
        string method,
        bool topLevel)
    {
        return new CookieRequestContext
        {
            RequestMethod = method,
            SameSiteContext = relation,
            AccessKind = CookieAccessKind.Http,
            IsTopLevelNavigation = topLevel
        };
    }

    static CookieSetContext SetContext(CookieJarOptions options)
    {
        return new CookieSetContext
        {
            Options = options,
            SameSiteContext = CookieSameSiteContext.CrossSite,
            AccessKind = CookieAccessKind.Http
        };
    }

    static StoredCookie Stored(
        CookieSameSiteMode mode,
        bool hasSameSite,
        DateTimeOffset creationTime)
    {
        return new StoredCookie
        {
            HasSameSite = hasSameSite,
            SameSiteMode = mode,
            Secure = true,
            CreationTime = creationTime
        };
    }

    static void AssertGraceAllowed(CookieSameSiteResult result)
    {
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.UsedDefaultMode, Is.True);
        Assert.That(result.UsedLaxAllowingUnsafe, Is.True);
    }
}

public class SameSitePublicSuffixList : IPublicSuffixList
{
    public string? GetPublicSuffix(string canonicalDomain)
    {
        return "test";
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        return "example.test";
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain == "test";
    }
}
