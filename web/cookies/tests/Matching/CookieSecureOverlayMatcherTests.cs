namespace cookies.Tests;

public class CookieSecureOverlayMatcherTests
{
    [Test]
    public void DisabledProtectionAllowsExactOverlayWithoutCallingPolicy()
    {
        StoredCookie existing = Cookie("session", "example.test", "/account", true);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("session", "example.test", "/account", false),
            new Uri("http://example.test/account"),
            new ThrowingSecureOriginPolicy(),
            new[] { existing },
            false);

        AssertAllowed(result, false, false, false, -1, null);
    }

    [Test]
    public void SecureCandidateDoesNotOverlayASecureCookie()
    {
        StoredCookie existing = Cookie("session", "example.test", "/account", true);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("session", "example.test", "/account", true),
            new Uri("http://example.test/account"),
            new ThrowingSecureOriginPolicy(),
            new[] { existing },
            true);

        AssertAllowed(result, false, false, false, -1, null);
    }

    [Test]
    public void InsecureOriginCannotSetAnExactOverlay()
    {
        StoredCookie existing = Cookie("session", "example.test", "/account", true);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("session", "example.test", "/account", false),
            new Uri("http://example.test/account"),
            new DefaultSecureOriginPolicy(false),
            new[] { existing },
            true);

        AssertDenied(result, 0, existing);
    }

    [Test]
    public void SecureOriginMayReplaceAnOverlappingSecureCookieWithANonSecureCookie()
    {
        StoredCookie existing = Cookie("session", "example.test", "/account", true);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("session", "example.test", "/account", false),
            new Uri("https://example.test/account"),
            new DefaultSecureOriginPolicy(false),
            new[] { existing },
            true);

        AssertAllowed(result, true, true, true, 0, existing);
    }

    [Test]
    public void OverlayEvaluationHonorsReplacementSecureOriginPolicy()
    {
        Uri origin = new Uri("custom://device.example/account");
        StoredCookie existing = Cookie("session", "device.example", "/account", true);
        RecordingSecureOriginPolicy allowing = new RecordingSecureOriginPolicy(true);
        RecordingSecureOriginPolicy rejecting = new RecordingSecureOriginPolicy(false);
        StoredCookie candidate = Cookie("session", "device.example", "/account", false);

        CookieSecureOverlayResult allowed = CookieSecureOverlayMatcher.Evaluate(
            candidate, origin, allowing, new[] { existing }, true);
        CookieSecureOverlayResult denied = CookieSecureOverlayMatcher.Evaluate(
            candidate, origin, rejecting, new[] { existing }, true);

        AssertAllowed(allowed, true, true, true, 0, existing);
        AssertDenied(denied, 0, existing);
        Assert.That(allowing.Origins, Is.EqualTo(new[] { origin }));
        Assert.That(rejecting.Origins, Is.EqualTo(new[] { origin }));
    }

    [Test]
    public void IrrelevantCookiesDoNotRequireOrInvokeSecureOriginPolicy()
    {
        StoredCookie candidate = Cookie("session", "example.test", "/account", false);
        StoredCookie differentName = Cookie("other", "example.test", "/account", true);
        StoredCookie differentDomain = Cookie("session", "other.test", "/account", true);
        StoredCookie differentPath = Cookie("session", "example.test", "/other", true);
        StoredCookie nonSecure = Cookie("session", "example.test", "/account", false);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            null!,
            new ThrowingSecureOriginPolicy(),
            new[] { differentName, differentDomain, differentPath, nonSecure },
            true);

        AssertAllowed(result, false, false, false, -1, null);
    }

    [Test]
    public void SecureOverlayProtectionIsIsolatedByPartitionIdentity()
    {
        StoredCookie unpartitioned = Cookie("session", "example.test", "/", true);
        StoredCookie firstPartition = Cookie("session", "example.test", "/", true);
        firstPartition.HasPartitionKey = true;
        firstPartition.PartitionKey = "https://first.example";
        StoredCookie secondPartitionCandidate = Cookie("session", "example.test", "/", false);
        secondPartitionCandidate.HasPartitionKey = true;
        secondPartitionCandidate.PartitionKey = "https://second.example";

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            secondPartitionCandidate,
            null!,
            new ThrowingSecureOriginPolicy(),
            new[] { unpartitioned, firstPartition },
            true);

        AssertAllowed(result, false, false, false, -1, null);
    }

    [Test]
    public void SecureOverlayProtectionAppliesWithinTheSamePartition()
    {
        StoredCookie existing = Cookie("session", "example.test", "/", true);
        existing.HasPartitionKey = true;
        existing.PartitionKey = "https://top.example";
        StoredCookie candidate = Cookie("session", "example.test", "/", false);
        candidate.HasPartitionKey = true;
        candidate.PartitionKey = "https://top.example";

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            new Uri("http://example.test/"),
            new DefaultSecureOriginPolicy(false),
            new[] { existing },
            true);

        AssertDenied(result, 0, existing);
    }

    [TestCase("example.test", "example.test", true)]
    [TestCase("shop.example.test", "example.test", true)]
    [TestCase("example.test", "shop.example.test", true)]
    [TestCase("badexample.test", "example.test", false)]
    [TestCase("example.test", "badexample.test", false)]
    [TestCase("other.test", "example.test", false)]
    [TestCase("192.0.2.1", "192.0.2.1", true)]
    [TestCase("192.0.2.2", "192.0.2.1", false)]
    public void DomainOverlapIsSymmetricAndUsesDotBoundaries(
        string candidateDomain,
        string existingDomain,
        bool expected)
    {
        Assert.That(
            CookieSecureOverlayMatcher.DomainsOverlap(candidateDomain, existingDomain),
            Is.EqualTo(expected));
    }

    [TestCase("/login", true)]
    [TestCase("/login/", true)]
    [TestCase("/login/form", true)]
    [TestCase("/", false)]
    [TestCase("/logins", false)]
    [TestCase("/other", false)]
    public void NewPathMustPathMatchTheExistingSecurePath(
        string candidatePath,
        bool expectedViolation)
    {
        StoredCookie existing = Cookie("session", "example.test", "/login", true);
        StoredCookie candidate = Cookie("session", "example.test", candidatePath, false);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            new Uri("http://example.test/"),
            new DefaultSecureOriginPolicy(false),
            new[] { existing },
            true);

        Assert.That(result.Allowed, Is.EqualTo(!expectedViolation));
        Assert.That(
            result.ErrorCode,
            Is.EqualTo(expectedViolation ? CookieErrorCode.SecureOverlayViolation : CookieErrorCode.None));
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(expectedViolation));
    }

    [Test]
    public void CookieNamesAreComparedCaseSensitively()
    {
        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("Session", "example.test", "/", false),
            null!,
            new ThrowingSecureOriginPolicy(),
            new[] { Cookie("session", "example.test", "/", true) },
            true);

        AssertAllowed(result, false, false, false, -1, null);
    }

    [Test]
    public void FirstOverlappingSecureCookieIsReportedDeterministically()
    {
        StoredCookie unrelated = Cookie("other", "example.test", "/", true);
        StoredCookie first = Cookie("session", "example.test", "/", true);
        StoredCookie second = Cookie("session", "shop.example.test", "/account", true);

        CookieSecureOverlayResult result = CookieSecureOverlayMatcher.Evaluate(
            Cookie("session", "shop.example.test", "/account/page", false),
            new Uri("http://shop.example.test/account/page"),
            new DefaultSecureOriginPolicy(false),
            new[] { unrelated, first, second },
            true);

        AssertDenied(result, 1, first);
    }

    [Test]
    public void MissingThrowingOrInvalidOriginFailsOnlyWhenOverlapExists()
    {
        StoredCookie candidate = Cookie("session", "example.test", "/", false);
        StoredCookie existing = Cookie("session", "example.test", "/", true);
        Uri validOrigin = new Uri("http://example.test/");

        CookieSecureOverlayResult missingPolicy = CookieSecureOverlayMatcher.Evaluate(
            candidate, validOrigin, null!, new[] { existing }, true);
        CookieSecureOverlayResult throwingPolicy = CookieSecureOverlayMatcher.Evaluate(
            candidate, validOrigin, new ThrowingSecureOriginPolicy(), new[] { existing }, true);
        CookieSecureOverlayResult nullOrigin = CookieSecureOverlayMatcher.Evaluate(
            candidate, null!, new DefaultSecureOriginPolicy(false), new[] { existing }, true);
        CookieSecureOverlayResult relativeOrigin = CookieSecureOverlayMatcher.Evaluate(
            candidate,
            new Uri("relative", UriKind.Relative),
            new DefaultSecureOriginPolicy(false),
            new[] { existing },
            true);

        AssertFailure(missingPolicy, CookieErrorCode.InvalidConfiguration);
        AssertFailure(throwingPolicy, CookieErrorCode.InvalidConfiguration);
        AssertFailure(nullOrigin, CookieErrorCode.InvalidHost);
        AssertFailure(relativeOrigin, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void InvalidCandidateOrExistingGraphsReturnStableFailures()
    {
        StoredCookie invalidCandidate = Cookie("session", "example.test", "relative", false);
        StoredCookie validCandidate = Cookie("session", "example.test", "/", false);
        StoredCookie invalidExisting = Cookie("session", "example.test", "/", true);
        invalidExisting.Domain = null!;

        CookieSecureOverlayResult nullCandidate = CookieSecureOverlayMatcher.Evaluate(
            null!, null!, null!, [], true);
        CookieSecureOverlayResult badCandidate = CookieSecureOverlayMatcher.Evaluate(
            invalidCandidate, null!, null!, [], true);
        CookieSecureOverlayResult nullArray = CookieSecureOverlayMatcher.Evaluate(
            validCandidate, null!, null!, null!, true);
        CookieSecureOverlayResult nullEntry = CookieSecureOverlayMatcher.Evaluate(
            validCandidate, null!, null!, new StoredCookie[] { null! }, true);
        CookieSecureOverlayResult badEntry = CookieSecureOverlayMatcher.Evaluate(
            validCandidate, null!, null!, new[] { invalidExisting }, true);

        AssertFailure(nullCandidate, CookieErrorCode.InvalidAttribute);
        AssertFailure(badCandidate, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullArray, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullEntry, CookieErrorCode.InvalidAttribute);
        AssertFailure(badEntry, CookieErrorCode.InvalidAttribute);
    }

    static StoredCookie Cookie(string name, string domain, string path, bool secure)
    {
        return new StoredCookie
        {
            Name = name,
            Domain = domain,
            Path = path,
            Secure = secure
        };
    }

    static void AssertAllowed(
        CookieSecureOverlayResult result,
        bool originWasEvaluated,
        bool originIsSecure,
        bool hasOverlap,
        int overlapIndex,
        StoredCookie? overlap)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(originWasEvaluated));
        Assert.That(result.OriginIsSecure, Is.EqualTo(originIsSecure));
        Assert.That(result.HasOverlappingSecureCookie, Is.EqualTo(hasOverlap));
        Assert.That(result.OverlappingCookieIndex, Is.EqualTo(overlapIndex));
        Assert.That(result.OverlappingCookie, Is.SameAs(overlap));
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertDenied(
        CookieSecureOverlayResult result,
        int overlapIndex,
        StoredCookie overlap)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.SecureOverlayViolation));
        Assert.That(result.OriginWasEvaluated, Is.True);
        Assert.That(result.OriginIsSecure, Is.False);
        Assert.That(result.HasOverlappingSecureCookie, Is.True);
        Assert.That(result.OverlappingCookieIndex, Is.EqualTo(overlapIndex));
        Assert.That(result.OverlappingCookie, Is.SameAs(overlap));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.SecureOverlayViolation));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    static void AssertFailure(CookieSecureOverlayResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.OriginWasEvaluated, Is.False);
        Assert.That(result.OriginIsSecure, Is.False);
        Assert.That(result.HasOverlappingSecureCookie, Is.False);
        Assert.That(result.OverlappingCookieIndex, Is.EqualTo(-1));
        Assert.That(result.OverlappingCookie, Is.Null);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
