namespace cookies.Tests;

public class CookieSecurityAttributeEvaluatorTests
{
    [Test]
    public void SecureCookieCanBeSetOnlyFromAnOriginTrustedByThePolicy()
    {
        SetCookie cookie = new SetCookie { Secure = true };
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(false);

        CookieSecurityAttributeResult secure = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            SetContext("https://example.test/path", CookieAccessKind.Http),
            policy,
            null);
        CookieSecurityAttributeResult insecure = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            SetContext("http://example.test/path", CookieAccessKind.Http),
            policy,
            null);

        AssertAllowed(secure, true, true);
        AssertDenied(insecure, CookieErrorCode.InsecureSecureCookie, true, false, false);
    }

    [Test]
    public void SecureCookieSetHonorsReplacementPolicyDecisionsForCustomSchemes()
    {
        Uri customOrigin = new Uri("custom://device.example/path");
        RecordingSecureOriginPolicy allowing = new RecordingSecureOriginPolicy(true);
        RecordingSecureOriginPolicy rejecting = new RecordingSecureOriginPolicy(false);
        SetCookie cookie = new SetCookie { Secure = true };

        CookieSecurityAttributeResult allowed = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            new CookieSetContext { Origin = customOrigin, AccessKind = CookieAccessKind.Http },
            allowing,
            null);
        CookieSecurityAttributeResult denied = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            new CookieSetContext { Origin = customOrigin, AccessKind = CookieAccessKind.Http },
            rejecting,
            null);

        AssertAllowed(allowed, true, true);
        AssertDenied(denied, CookieErrorCode.InsecureSecureCookie, true, false, false);
        Assert.That(allowing.Origins, Is.EqualTo(new[] { customOrigin }));
        Assert.That(rejecting.Origins, Is.EqualTo(new[] { customOrigin }));
    }

    [Test]
    public void NonSecureCookieDoesNotRequireOrInvokeASecureOriginPolicy()
    {
        CookieSetContext context = new CookieSetContext
        {
            Origin = null,
            AccessKind = CookieAccessKind.Http
        };

        CookieSecurityAttributeResult result = CookieSecurityAttributeEvaluator.EvaluateSet(
            new SetCookie(), context, new ThrowingSecureOriginPolicy(), null);

        AssertAllowed(result, false, false);
    }

    [Test]
    public void MissingThrowingOrInvalidOriginFailsSecureSetDeterministically()
    {
        SetCookie cookie = new SetCookie { Secure = true };
        CookieSetContext validContext = SetContext(
            "https://example.test/path", CookieAccessKind.Http);
        CookieSetContext nullOrigin = new CookieSetContext
        {
            Origin = null,
            AccessKind = CookieAccessKind.Http
        };
        CookieSetContext relativeOrigin = new CookieSetContext
        {
            Origin = new Uri("relative", UriKind.Relative),
            AccessKind = CookieAccessKind.Http
        };

        CookieSecurityAttributeResult missing = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie, validContext, null!, null);
        CookieSecurityAttributeResult throwing = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie, validContext, new ThrowingSecureOriginPolicy(), null);
        CookieSecurityAttributeResult absent = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie, nullOrigin, new DefaultSecureOriginPolicy(false), null);
        CookieSecurityAttributeResult relative = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie, relativeOrigin, new DefaultSecureOriginPolicy(false), null);

        AssertFailure(missing, CookieErrorCode.InvalidConfiguration);
        AssertFailure(throwing, CookieErrorCode.InvalidConfiguration);
        AssertFailure(absent, CookieErrorCode.InvalidHost);
        AssertFailure(relative, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void HttpOnlyCookieCanBeSetByHttpButNotByNonHttpApi()
    {
        SetCookie cookie = new SetCookie { HttpOnly = true };

        CookieSecurityAttributeResult http = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            SetContext("http://example.test/path", CookieAccessKind.Http),
            null!,
            null);
        CookieSecurityAttributeResult nonHttp = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            SetContext("http://example.test/path", CookieAccessKind.NonHttp),
            null!,
            null);

        AssertAllowed(http, false, false);
        AssertDenied(nonHttp, CookieErrorCode.HttpOnlyApiViolation, false, false, false);
    }

    [Test]
    public void NonHttpApiCannotReplaceAnExistingHttpOnlyCookie()
    {
        SetCookie replacement = new SetCookie { HttpOnly = false };
        StoredCookie existingHttpOnly = new StoredCookie { HttpOnly = true };
        CookieSetContext nonHttpContext = SetContext(
            "https://example.test/path", CookieAccessKind.NonHttp);
        CookieSetContext httpContext = SetContext(
            "https://example.test/path", CookieAccessKind.Http);

        CookieSecurityAttributeResult nonHttp = CookieSecurityAttributeEvaluator.EvaluateSet(
            replacement, nonHttpContext, null!, existingHttpOnly);
        CookieSecurityAttributeResult http = CookieSecurityAttributeEvaluator.EvaluateSet(
            replacement, httpContext, null!, existingHttpOnly);
        CookieSecurityAttributeResult noExisting = CookieSecurityAttributeEvaluator.EvaluateSet(
            replacement, nonHttpContext, null!, null);
        CookieSecurityAttributeResult ordinaryExisting = CookieSecurityAttributeEvaluator.EvaluateSet(
            replacement, nonHttpContext, null!, new StoredCookie { HttpOnly = false });

        AssertDenied(nonHttp, CookieErrorCode.HttpOnlyApiViolation, false, false, true);
        AssertAllowed(http, false, false);
        AssertAllowed(noExisting, false, false);
        AssertAllowed(ordinaryExisting, false, false);
    }

    [Test]
    public void SecureRulePrecedesHttpOnlySetRule()
    {
        SetCookie cookie = new SetCookie { Secure = true, HttpOnly = true };

        CookieSecurityAttributeResult result = CookieSecurityAttributeEvaluator.EvaluateSet(
            cookie,
            SetContext("http://example.test/path", CookieAccessKind.NonHttp),
            new DefaultSecureOriginPolicy(false),
            null);

        AssertDenied(result, CookieErrorCode.InsecureSecureCookie, true, false, false);
    }

    [Test]
    public void SecureCookieIsReturnedOnlyToAnOriginTrustedByThePolicy()
    {
        StoredCookie cookie = new StoredCookie { Secure = true };
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(false);

        CookieSecurityAttributeResult secure = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext("https://example.test/path", CookieAccessKind.Http),
            policy);
        CookieSecurityAttributeResult insecure = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext("http://example.test/path", CookieAccessKind.Http),
            policy);

        AssertAllowed(secure, true, true);
        AssertDenied(insecure, CookieErrorCode.InsecureSecureCookie, true, false, false);
    }

    [Test]
    public void NonSecureCookieRetrievalDoesNotRequireAnOriginOrPolicy()
    {
        CookieRequestContext context = new CookieRequestContext
        {
            RequestUri = null,
            AccessKind = CookieAccessKind.Http
        };

        CookieSecurityAttributeResult result = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            new StoredCookie(), context, new ThrowingSecureOriginPolicy());

        AssertAllowed(result, false, false);
    }

    [Test]
    public void HttpOnlyCookieIsExcludedFromNonHttpRetrieval()
    {
        StoredCookie httpOnly = new StoredCookie { HttpOnly = true };
        StoredCookie ordinary = new StoredCookie { HttpOnly = false };
        CookieRequestContext http = RequestContext(
            "http://example.test/path", CookieAccessKind.Http);
        CookieRequestContext nonHttp = RequestContext(
            "http://example.test/path", CookieAccessKind.NonHttp);

        CookieSecurityAttributeResult httpResult = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            httpOnly, http, null!);
        CookieSecurityAttributeResult nonHttpResult = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            httpOnly, nonHttp, null!);
        CookieSecurityAttributeResult ordinaryResult = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            ordinary, nonHttp, null!);

        AssertAllowed(httpResult, false, false);
        AssertDenied(nonHttpResult, CookieErrorCode.HttpOnlyApiViolation, false, false, false);
        AssertAllowed(ordinaryResult, false, false);
    }

    [Test]
    public void SecureRulePrecedesHttpOnlyRetrievalRule()
    {
        StoredCookie cookie = new StoredCookie { Secure = true, HttpOnly = true };

        CookieSecurityAttributeResult result = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext("http://example.test/path", CookieAccessKind.NonHttp),
            new DefaultSecureOriginPolicy(false));

        AssertDenied(result, CookieErrorCode.InsecureSecureCookie, true, false, false);
    }

    [Test]
    public void InvalidRetrievalInputsReturnStableFailures()
    {
        CookieRequestContext invalidAccess = RequestContext(
            "https://example.test/path", CookieAccessKind.Http);
        invalidAccess.AccessKind = (CookieAccessKind)99;
        StoredCookie secureCookie = new StoredCookie { Secure = true };
        CookieRequestContext nullRequest = new CookieRequestContext
        {
            RequestUri = null,
            AccessKind = CookieAccessKind.Http
        };

        CookieSecurityAttributeResult nullCookie = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            null!, RequestContext("https://example.test/path", CookieAccessKind.Http), null!);
        CookieSecurityAttributeResult badAccess = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            new StoredCookie(), invalidAccess, null!);
        CookieSecurityAttributeResult absentRequest = CookieSecurityAttributeEvaluator.EvaluateRetrieval(
            secureCookie, nullRequest, new DefaultSecureOriginPolicy(false));

        AssertFailure(nullCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(badAccess, CookieErrorCode.InvalidConfiguration);
        AssertFailure(absentRequest, CookieErrorCode.InvalidHost);
    }

    static CookieSetContext SetContext(string uriText, CookieAccessKind accessKind)
    {
        return new CookieSetContext
        {
            Origin = new Uri(uriText),
            AccessKind = accessKind
        };
    }

    static CookieRequestContext RequestContext(string uriText, CookieAccessKind accessKind)
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri(uriText),
            AccessKind = accessKind
        };
    }

    static void AssertAllowed(
        CookieSecurityAttributeResult result,
        bool originWasEvaluated,
        bool originIsSecure)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(originWasEvaluated));
        Assert.That(result.OriginIsSecure, Is.EqualTo(originIsSecure));
        Assert.That(result.ProtectedExistingHttpOnly, Is.False);
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertDenied(
        CookieSecurityAttributeResult result,
        CookieErrorCode expected,
        bool originWasEvaluated,
        bool originIsSecure,
        bool protectedExistingHttpOnly)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(originWasEvaluated));
        Assert.That(result.OriginIsSecure, Is.EqualTo(originIsSecure));
        Assert.That(result.ProtectedExistingHttpOnly, Is.EqualTo(protectedExistingHttpOnly));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    static void AssertFailure(CookieSecurityAttributeResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.OriginWasEvaluated, Is.False);
        Assert.That(result.OriginIsSecure, Is.False);
        Assert.That(result.ProtectedExistingHttpOnly, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
