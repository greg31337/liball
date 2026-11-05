namespace cookies.Tests;

public class SecureAndHttpOnlyConformanceTests
{
    [TestCase("https://example.test/path", true)]
    [TestCase("wss://example.test/socket", true)]
    [TestCase("http://example.test/path", false)]
    [TestCase("ws://example.test/socket", false)]
    [TestCase("ftp://example.test/file", false)]
    public void DefaultPolicyClassifiesSecureSchemes(
        string uriText,
        bool expected)
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(false);

        Assert.That(policy.IsSecureOrigin(new Uri(uriText)), Is.EqualTo(expected));
    }

    [TestCase("http://localhost/path")]
    [TestCase("http://service.localhost:8080/path")]
    [TestCase("ws://127.0.0.1/socket")]
    [TestCase("http://127.255.255.254/path")]
    [TestCase("ws://[::1]:8080/socket")]
    public void LoopbackTrustIsControlledByTheExplicitPolicyOption(string uriText)
    {
        ISecureOriginPolicy disabled = new DefaultSecureOriginPolicy(false);
        ISecureOriginPolicy enabled = new DefaultSecureOriginPolicy(true);
        Uri uri = new Uri(uriText);

        Assert.That(disabled.IsSecureOrigin(uri), Is.False);
        Assert.That(enabled.IsSecureOrigin(uri), Is.True);
    }

    [Test]
    public void SecureCookiesCanBeSetAndReturnedOnlyOnTrustedOrigins()
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(false);
        SetCookie supplied = new SetCookie { Secure = true };
        StoredCookie stored = new StoredCookie { Secure = true };

        CookieSecurityAttributeResult secureSet =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied, SetContext("https://example.test/", CookieAccessKind.Http),
                policy, null);
        CookieSecurityAttributeResult insecureSet =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied, SetContext("http://example.test/", CookieAccessKind.Http),
                policy, null);
        CookieSecurityAttributeResult secureRetrieval =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored, RequestContext("https://example.test/", CookieAccessKind.Http),
                policy);
        CookieSecurityAttributeResult insecureRetrieval =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored, RequestContext("http://example.test/", CookieAccessKind.Http),
                policy);

        AssertAllowed(secureSet);
        AssertDenied(insecureSet, CookieErrorCode.InsecureSecureCookie);
        AssertAllowed(secureRetrieval);
        AssertDenied(insecureRetrieval, CookieErrorCode.InsecureSecureCookie);
    }

    [Test]
    public void LoopbackPolicyAlsoControlsSecureCookieSetAndRetrieval()
    {
        ISecureOriginPolicy disabled = new DefaultSecureOriginPolicy(false);
        ISecureOriginPolicy enabled = new DefaultSecureOriginPolicy(true);
        SetCookie supplied = new SetCookie { Secure = true };
        StoredCookie stored = new StoredCookie { Secure = true };
        CookieSetContext set = SetContext(
            "http://localhost:8080/", CookieAccessKind.Http);
        CookieRequestContext request = RequestContext(
            "http://[::1]/", CookieAccessKind.Http);

        AssertDenied(
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied, set, disabled, null),
            CookieErrorCode.InsecureSecureCookie);
        AssertAllowed(
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied, set, enabled, null));
        AssertDenied(
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored, request, disabled),
            CookieErrorCode.InsecureSecureCookie);
        AssertAllowed(
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored, request, enabled));
    }

    [Test]
    public void HttpOnlySettingAndRetrievalHonorHttpVersusNonHttpAccess()
    {
        SetCookie supplied = new SetCookie { HttpOnly = true };
        StoredCookie stored = new StoredCookie { HttpOnly = true };

        CookieSecurityAttributeResult httpSet =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied,
                SetContext("http://example.test/", CookieAccessKind.Http),
                null!,
                null);
        CookieSecurityAttributeResult nonHttpSet =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                supplied,
                SetContext("http://example.test/", CookieAccessKind.NonHttp),
                null!,
                null);
        CookieSecurityAttributeResult httpRetrieval =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored,
                RequestContext("http://example.test/", CookieAccessKind.Http),
                null!);
        CookieSecurityAttributeResult nonHttpRetrieval =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                stored,
                RequestContext("http://example.test/", CookieAccessKind.NonHttp),
                null!);

        AssertAllowed(httpSet);
        AssertDenied(nonHttpSet, CookieErrorCode.HttpOnlyApiViolation);
        AssertAllowed(httpRetrieval);
        AssertDenied(nonHttpRetrieval, CookieErrorCode.HttpOnlyApiViolation);
    }

    [Test]
    public void NonHttpAccessCannotReplaceAnExistingHttpOnlyCookie()
    {
        SetCookie replacement = new SetCookie();
        StoredCookie existing = new StoredCookie { HttpOnly = true };

        CookieSecurityAttributeResult nonHttp =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                replacement,
                SetContext("https://example.test/", CookieAccessKind.NonHttp),
                null!,
                existing);
        CookieSecurityAttributeResult http =
            CookieSecurityAttributeEvaluator.EvaluateSet(
                replacement,
                SetContext("https://example.test/", CookieAccessKind.Http),
                null!,
                existing);

        AssertDenied(nonHttp, CookieErrorCode.HttpOnlyApiViolation);
        Assert.That(nonHttp.ProtectedExistingHttpOnly, Is.True);
        AssertAllowed(http);
    }

    [Test]
    public void SecureFailurePrecedesHttpOnlyFailureDuringRetrieval()
    {
        StoredCookie cookie = new StoredCookie
        {
            Secure = true,
            HttpOnly = true
        };

        CookieSecurityAttributeResult insecure =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                cookie,
                RequestContext("http://example.test/", CookieAccessKind.NonHttp),
                new DefaultSecureOriginPolicy(false));
        CookieSecurityAttributeResult secure =
            CookieSecurityAttributeEvaluator.EvaluateRetrieval(
                cookie,
                RequestContext("https://example.test/", CookieAccessKind.NonHttp),
                new DefaultSecureOriginPolicy(false));

        AssertDenied(insecure, CookieErrorCode.InsecureSecureCookie);
        AssertDenied(secure, CookieErrorCode.HttpOnlyApiViolation);
    }

    [TestCase("session", "shop.example.test", "/login", true)]
    [TestCase("session", "shop.example.test", "/login/form", true)]
    [TestCase("session", "deep.shop.example.test", "/login/form", true)]
    [TestCase("Session", "shop.example.test", "/login/form", false)]
    [TestCase("session", "other.test", "/login/form", false)]
    [TestCase("session", "shop.example.test", "/", false)]
    [TestCase("session", "shop.example.test", "/logins", false)]
    public void InsecureOverlayProtectionUsesNameDomainAndPathBoundaries(
        string name,
        string domain,
        string path,
        bool expectedViolation)
    {
        StoredCookie existing = Cookie(
            "session", "example.test", "/login", true);
        StoredCookie candidate = Cookie(name, domain, path, false);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            new Uri("http://shop.example.test/login"),
            new DefaultSecureOriginPolicy(false),
            [existing],
            true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.EqualTo(!expectedViolation));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedViolation
            ? CookieErrorCode.SecureOverlayViolation
            : CookieErrorCode.None));
    }

    [Test]
    public void SecureOriginMayPerformAnOtherwiseOverlappingReplacement()
    {
        StoredCookie existing = Cookie(
            "session", "example.test", "/login", true);
        StoredCookie candidate = Cookie(
            "session", "shop.example.test", "/login/form", false);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            new Uri("https://shop.example.test/login/form"),
            new DefaultSecureOriginPolicy(false),
            [existing],
            true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.OriginWasEvaluated, Is.True);
        Assert.That(result.OriginIsSecure, Is.True);
        Assert.That(result.HasOverlappingSecureCookie, Is.True);
    }

    static CookieSetContext SetContext(
        string uriText,
        CookieAccessKind accessKind)
    {
        return new CookieSetContext
        {
            Origin = new Uri(uriText),
            AccessKind = accessKind
        };
    }

    static CookieRequestContext RequestContext(
        string uriText,
        CookieAccessKind accessKind)
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri(uriText),
            AccessKind = accessKind
        };
    }

    static StoredCookie Cookie(
        string name,
        string domain,
        string path,
        bool secure)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = domain,
            Path = path,
            Secure = secure
        };
    }

    static void AssertAllowed(CookieSecurityAttributeResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertDenied(
        CookieSecurityAttributeResult result,
        CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues.Single().ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity,
            Is.EqualTo(CookieParseSeverity.Error));
    }
}
