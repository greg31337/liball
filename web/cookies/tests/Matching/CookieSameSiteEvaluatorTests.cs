namespace cookies.Tests;

public class CookieSameSiteEvaluatorTests
{
    static readonly DateTimeOffset EvaluationTime = new DateTimeOffset(
        2025, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Test]
    public void DisabledEnforcementAllowsCrossSiteStrictCreationAndRetrieval()
    {
        CookieJarOptions options = ModernOptions();
        options.EnforceSameSite = false;
        SetCookie setCookie = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict
        };
        StoredCookie storedCookie = Stored(CookieSameSiteMode.Strict, true);

        CookieSameSiteResult creation = CookieSameSiteEvaluator.EvaluateCreation(
            setCookie, SetContext(options, CookieSameSiteContext.CrossSite, false));
        CookieSameSiteResult retrieval = CookieSameSiteEvaluator.EvaluateRetrieval(
            storedCookie, RequestContext("POST", false), options, EvaluationTime);

        AssertAllowed(creation, CookieSameSiteMode.Strict, false, false);
        AssertAllowed(retrieval, CookieSameSiteMode.Strict, false, false);
    }

    [Test]
    public void StrictCreationAllowsTopLevelNavigationButRejectsCrossSiteSubresources()
    {
        CookieJarOptions options = ModernOptions();
        SetCookie cookie = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict
        };

        CookieSameSiteResult sameSite = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.SameSite, false));
        CookieSameSiteResult crossSiteTopLevel = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.CrossSite, true));
        CookieSameSiteResult crossSiteSubresource = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.CrossSite, false));

        AssertAllowed(sameSite, CookieSameSiteMode.Strict, false, false);
        AssertAllowed(crossSiteTopLevel, CookieSameSiteMode.Strict, false, false);
        AssertDenied(crossSiteSubresource, CookieSameSiteMode.Strict, false);
    }

    [Test]
    public void LaxCreationAllowsOnlySameSiteOrCrossSiteTopLevelHttpContexts()
    {
        CookieJarOptions options = ModernOptions();
        SetCookie cookie = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Lax
        };

        CookieSameSiteResult sameSite = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.SameSite, false));
        CookieSameSiteResult topLevel = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.CrossSite, true));
        CookieSameSiteResult subresource = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(options, CookieSameSiteContext.CrossSite, false));
        CookieSetContext nonHttpContext = SetContext(
            options, CookieSameSiteContext.CrossSite, true);
        nonHttpContext.AccessKind = CookieAccessKind.NonHttp;
        CookieSameSiteResult nonHttp = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, nonHttpContext);

        Assert.That(sameSite.Allowed, Is.True);
        Assert.That(topLevel.Allowed, Is.True);
        Assert.That(subresource.Allowed, Is.False);
        Assert.That(nonHttp.Allowed, Is.False);
    }

    [Test]
    public void ExplicitNoneRequiresSecureOnlyWhenConfiguredInAModernProfile()
    {
        SetCookie insecureNone = new SetCookie
        {
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.None,
            Secure = false
        };
        CookieJarOptions modern = ModernOptions();
        modern.EnforceSameSite = false;
        modern.RequireSecureForSameSiteNone = true;
        CookieJarOptions baseline = ModernOptions();
        baseline.ComplianceMode = CookieComplianceMode.Rfc6265;
        baseline.EnforceSameSite = true;
        baseline.RequireSecureForSameSiteNone = true;

        CookieSameSiteResult modernResult = CookieSameSiteEvaluator.EvaluateCreation(
            insecureNone, SetContext(modern, CookieSameSiteContext.SameSite, false));
        CookieSameSiteResult baselineResult = CookieSameSiteEvaluator.EvaluateCreation(
            insecureNone, SetContext(baseline, CookieSameSiteContext.CrossSite, false));
        insecureNone.Secure = true;
        CookieSameSiteResult secureModern = CookieSameSiteEvaluator.EvaluateCreation(
            insecureNone, SetContext(modern, CookieSameSiteContext.CrossSite, false));

        AssertDenied(modernResult, CookieSameSiteMode.None, false);
        AssertAllowed(baselineResult, CookieSameSiteMode.None, false, false);
        AssertAllowed(secureModern, CookieSameSiteMode.None, false, false);
    }

    [Test]
    public void ModernDefaultUsesLaxCreationWhileBaselineDefaultRemainsUnrestricted()
    {
        SetCookie cookie = new SetCookie();
        CookieJarOptions modern = ModernOptions();
        CookieJarOptions baseline = ModernOptions();
        baseline.ComplianceMode = CookieComplianceMode.Rfc6265;

        CookieSameSiteResult modernSubresource = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(modern, CookieSameSiteContext.CrossSite, false));
        CookieSameSiteResult modernTopLevel = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(modern, CookieSameSiteContext.CrossSite, true));
        CookieSameSiteResult baselineSubresource = CookieSameSiteEvaluator.EvaluateCreation(
            cookie, SetContext(baseline, CookieSameSiteContext.CrossSite, false));

        AssertDenied(modernSubresource, CookieSameSiteMode.Default, true);
        AssertAllowed(modernTopLevel, CookieSameSiteMode.Default, true, false);
        AssertAllowed(baselineSubresource, CookieSameSiteMode.None, true, false);
    }

    [Test]
    public void ConfiguredDefaultModeOverridesProfileDefault()
    {
        CookieJarOptions options = ModernOptions();
        options.DefaultSameSiteMode = CookieSameSiteMode.Strict;

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateCreation(
            new SetCookie(),
            SetContext(options, CookieSameSiteContext.CrossSite, false));

        AssertDenied(result, CookieSameSiteMode.Strict, true);
    }

    [TestCase(CookieSameSiteMode.Strict)]
    [TestCase(CookieSameSiteMode.Lax)]
    [TestCase(CookieSameSiteMode.None)]
    [TestCase(CookieSameSiteMode.Default)]
    public void SameSiteRetrievalAllowsEveryMode(CookieSameSiteMode mode)
    {
        CookieJarOptions options = ModernOptions();
        StoredCookie cookie = Stored(mode, mode != CookieSameSiteMode.Default);
        CookieRequestContext context = RequestContext("POST", false);
        context.SameSiteContext = CookieSameSiteContext.SameSite;

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, context, options, EvaluationTime);

        Assert.That(result.Allowed, Is.True);
    }

    [Test]
    public void CrossSiteStrictRetrievalIsDeniedForTopLevelSafeNavigation()
    {
        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Strict, true),
            RequestContext("GET", true),
            ModernOptions(),
            EvaluationTime);

        AssertDenied(result, CookieSameSiteMode.Strict, false);
    }

    [TestCase("GET")]
    [TestCase("HEAD")]
    [TestCase("OPTIONS")]
    [TestCase("TRACE")]
    public void LaxRetrievalAllowsCrossSiteTopLevelSafeMethods(string method)
    {
        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true),
            RequestContext(method, true),
            ModernOptions(),
            EvaluationTime);

        AssertAllowed(result, CookieSameSiteMode.Lax, false, false);
    }

    [TestCase("POST", true)]
    [TestCase("get", true)]
    [TestCase("GET", false)]
    [TestCase("DELETE", false)]
    public void LaxRetrievalRejectsUnsafeOrNonTopLevelCrossSiteRequests(
        string method,
        bool topLevel)
    {
        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true),
            RequestContext(method, topLevel),
            ModernOptions(),
            EvaluationTime);

        AssertDenied(result, CookieSameSiteMode.Lax, false);
    }

    [Test]
    public void LaxTopLevelExceptionIsNotAppliedToNonHttpAccess()
    {
        CookieRequestContext context = RequestContext("GET", true);
        context.AccessKind = CookieAccessKind.NonHttp;

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true), context, ModernOptions(), EvaluationTime);

        AssertDenied(result, CookieSameSiteMode.Lax, false);
    }

    [Test]
    public void RedirectChainForcesCrossSiteButUsesTheCurrentHopMethodForLax()
    {
        CookieRequestContext context = RequestContext("GET", true);
        context.SameSiteContext = CookieSameSiteContext.SameSite;
        context.RedirectChainIsCrossSite = true;
        CookieJarOptions options = ModernOptions();

        CookieSameSiteResult strict = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Strict, true), context, options, EvaluationTime);
        CookieSameSiteResult lax = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true), context, options, EvaluationTime);

        Assert.That(strict.Allowed, Is.False);
        Assert.That(lax.Allowed, Is.True);
    }

    [Test]
    public void UserTriggeredReloadRetainsRelationWhileProgrammaticTopLevelReloadIsSameSite()
    {
        StoredCookie strictCookie = Stored(CookieSameSiteMode.Strict, true);
        CookieJarOptions options = ModernOptions();
        CookieRequestContext userReload = RequestContext("GET", true);
        userReload.IsReload = true;
        userReload.HasUserActivation = true;
        CookieRequestContext programmaticReload = RequestContext("POST", true);
        programmaticReload.IsReload = true;
        programmaticReload.HasUserActivation = false;

        CookieSameSiteResult userResult = CookieSameSiteEvaluator.EvaluateRetrieval(
            strictCookie, userReload, options, EvaluationTime);
        CookieSameSiteResult programmaticResult = CookieSameSiteEvaluator.EvaluateRetrieval(
            strictCookie, programmaticReload, options, EvaluationTime);

        Assert.That(userResult.Allowed, Is.False);
        Assert.That(programmaticResult.Allowed, Is.True);
    }

    [Test]
    public void LaxAllowingUnsafeAppliesOnlyToRecentDefaultCookiesAtInclusiveBoundary()
    {
        CookieJarOptions options = ModernOptions();
        options.EnableLaxAllowingUnsafe = true;
        options.LaxAllowingUnsafeGracePeriod = TimeSpan.FromMinutes(2);
        StoredCookie cookie = Stored(CookieSameSiteMode.Default, false);
        cookie.CreationTime = EvaluationTime.AddMinutes(-2);

        CookieSameSiteResult boundary = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, RequestContext("POST", true), options, EvaluationTime);
        CookieSameSiteResult after = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, RequestContext("POST", true), options, EvaluationTime.AddTicks(1));

        AssertAllowed(boundary, CookieSameSiteMode.Default, true, true);
        AssertDenied(after, CookieSameSiteMode.Default, true);
    }

    [Test]
    public void LaxAllowingUnsafeDoesNotApplyToExplicitDefaultOrLaxCookies()
    {
        CookieJarOptions options = ModernOptions();
        options.EnableLaxAllowingUnsafe = true;
        StoredCookie explicitDefault = Stored(CookieSameSiteMode.Default, true);
        StoredCookie explicitLax = Stored(CookieSameSiteMode.Lax, true);
        explicitDefault.CreationTime = EvaluationTime;
        explicitLax.CreationTime = EvaluationTime;

        CookieSameSiteResult defaultResult = CookieSameSiteEvaluator.EvaluateRetrieval(
            explicitDefault, RequestContext("POST", true), options, EvaluationTime);
        CookieSameSiteResult laxResult = CookieSameSiteEvaluator.EvaluateRetrieval(
            explicitLax, RequestContext("POST", true), options, EvaluationTime);

        Assert.That(defaultResult.Allowed, Is.False);
        Assert.That(laxResult.Allowed, Is.False);
    }

    [Test]
    public void FutureCreationTimeDoesNotUnderflowGraceAge()
    {
        CookieJarOptions options = ModernOptions();
        options.EnableLaxAllowingUnsafe = true;
        StoredCookie cookie = Stored(CookieSameSiteMode.Default, false);
        cookie.CreationTime = EvaluationTime.AddDays(1);

        CookieSameSiteResult result = CookieSameSiteEvaluator.EvaluateRetrieval(
            cookie, RequestContext("POST", true), options, EvaluationTime);

        AssertAllowed(result, CookieSameSiteMode.Default, true, true);
    }

    [Test]
    public void InvalidInputsAndOptionsReturnStableFailures()
    {
        CookieJarOptions invalidMode = ModernOptions();
        invalidMode.ComplianceMode = (CookieComplianceMode)99;
        CookieJarOptions negativeGrace = ModernOptions();
        negativeGrace.LaxAllowingUnsafeGracePeriod = TimeSpan.FromTicks(-1);
        CookieRequestContext invalidContext = RequestContext("GET", true);
        invalidContext.SameSiteContext = (CookieSameSiteContext)99;
        StoredCookie invalidCookie = Stored((CookieSameSiteMode)99, true);

        CookieSameSiteResult nullCookie = CookieSameSiteEvaluator.EvaluateRetrieval(
            null!, RequestContext("GET", true), ModernOptions(), EvaluationTime);
        CookieSameSiteResult badMode = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true),
            RequestContext("GET", true),
            invalidMode,
            EvaluationTime);
        CookieSameSiteResult badGrace = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true),
            RequestContext("GET", true),
            negativeGrace,
            EvaluationTime);
        CookieSameSiteResult badContext = CookieSameSiteEvaluator.EvaluateRetrieval(
            Stored(CookieSameSiteMode.Lax, true), invalidContext, ModernOptions(), EvaluationTime);
        CookieSameSiteResult badCookie = CookieSameSiteEvaluator.EvaluateRetrieval(
            invalidCookie, RequestContext("GET", true), ModernOptions(), EvaluationTime);

        AssertFailure(nullCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(badMode, CookieErrorCode.InvalidConfiguration);
        AssertFailure(badGrace, CookieErrorCode.InvalidConfiguration);
        AssertFailure(badContext, CookieErrorCode.InvalidConfiguration);
        AssertFailure(badCookie, CookieErrorCode.InvalidConfiguration);
    }

    static CookieJarOptions ModernOptions()
    {
        return new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            EnforceSameSite = true
        };
    }

    static CookieSetContext SetContext(
        CookieJarOptions options,
        CookieSameSiteContext sameSiteContext,
        bool topLevel)
    {
        return new CookieSetContext
        {
            Options = options,
            SameSiteContext = sameSiteContext,
            AccessKind = CookieAccessKind.Http,
            IsTopLevelNavigation = topLevel
        };
    }

    static CookieRequestContext RequestContext(string method, bool topLevel)
    {
        return new CookieRequestContext
        {
            RequestMethod = method,
            SameSiteContext = CookieSameSiteContext.CrossSite,
            AccessKind = CookieAccessKind.Http,
            IsTopLevelNavigation = topLevel
        };
    }

    static StoredCookie Stored(CookieSameSiteMode mode, bool hasSameSite)
    {
        return new StoredCookie
        {
            HasSameSite = hasSameSite,
            SameSiteMode = mode,
            CreationTime = EvaluationTime
        };
    }

    static void AssertAllowed(
        CookieSameSiteResult result,
        CookieSameSiteMode effectiveMode,
        bool usedDefaultMode,
        bool usedLaxAllowingUnsafe)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.EffectiveMode, Is.EqualTo(effectiveMode));
        Assert.That(result.UsedDefaultMode, Is.EqualTo(usedDefaultMode));
        Assert.That(result.UsedLaxAllowingUnsafe, Is.EqualTo(usedLaxAllowingUnsafe));
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertDenied(
        CookieSameSiteResult result,
        CookieSameSiteMode effectiveMode,
        bool usedDefaultMode)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.SameSiteViolation));
        Assert.That(result.EffectiveMode, Is.EqualTo(effectiveMode));
        Assert.That(result.UsedDefaultMode, Is.EqualTo(usedDefaultMode));
        Assert.That(result.UsedLaxAllowingUnsafe, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.SameSiteViolation));
    }

    static void AssertFailure(CookieSameSiteResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
