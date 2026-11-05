namespace cookies.Tests;

public class CookieEvictionAlgorithmTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2030, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void ExpiredCookiesAreRemovedBeforeGlobalQuotaCandidates()
    {
        StoredCookie oldest = Cookie("oldest", "example.test", 0, 0, true);
        StoredCookie next = Cookie("next", "other.test", 1, 2, true);
        StoredCookie newest = Cookie("newest", "third.test", 2, 3, true);
        StoredCookie expired = Cookie("expired", "fourth.test", 3, 1, true);
        expired.HasExpires = true;
        expired.Expires = EvaluationTime;

        CookieEvictionResult result = CookieEvictionAlgorithm.Plan(
            [oldest, next, newest, expired],
            Limits(10, 2),
            EvaluationTime,
            true);

        AssertSuccess(result);
        Assert.That(result.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "expired", "oldest" }));
        Assert.That(result.Evictions.Select(entry => entry.Reason),
            Is.EqualTo(new[]
            {
                CookieErrorCode.Expired,
                CookieErrorCode.TotalLimitExceeded
            }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[]
            {
                CookieErrorCode.Expired,
                CookieErrorCode.QuotaEviction
            }));
        Assert.That(result.RetainedCookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "next", "newest" }));
    }

    [Test]
    public void ModernDomainPressureEvictsNonSecureBeforeOlderSecureCookies()
    {
        StoredCookie secureOldest = Cookie("secure-oldest", "example.test", 0, 0, true);
        StoredCookie insecureOld = Cookie("insecure-old", "example.test", 1, 1, false);
        StoredCookie insecureNew = Cookie("insecure-new", "example.test", 2, 2, false);
        StoredCookie secureNewest = Cookie("secure-newest", "example.test", 3, 3, true);

        CookieEvictionResult result = CookieEvictionAlgorithm.Plan(
            [secureOldest, insecureOld, insecureNew, secureNewest],
            Limits(2, 10),
            EvaluationTime,
            true);

        AssertSuccess(result);
        Assert.That(result.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "insecure-old", "insecure-new" }));
        Assert.That(result.Evictions.All(entry =>
            entry.Reason == CookieErrorCode.PerDomainLimitExceeded), Is.True);
        Assert.That(result.RetainedCookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "secure-oldest", "secure-newest" }));
    }

    [Test]
    public void DomainFallbackUsesOldestAccessAndGroupsExactDomainFieldsOnly()
    {
        StoredCookie parent = Cookie("parent", "example.test", 0, 3, true);
        StoredCookie subdomainOld = Cookie("sub-old", "shop.example.test", 1, 0, true);
        StoredCookie subdomainNew = Cookie("sub-new", "shop.example.test", 2, 1, true);

        CookieEvictionResult result = CookieEvictionAlgorithm.Plan(
            [parent, subdomainNew, subdomainOld],
            Limits(1, 10),
            EvaluationTime,
            true);

        AssertSuccess(result);
        Assert.That(result.Evictions, Has.Length.EqualTo(1));
        Assert.That(result.Evictions[0].Cookie.Name, Is.EqualTo("sub-old"));
        Assert.That(result.Evictions[0].Reason,
            Is.EqualTo(CookieErrorCode.PerDomainLimitExceeded));
        Assert.That(result.RetainedCookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "parent", "sub-new" }));
    }

    [Test]
    public void GlobalPressureUsesStableSequenceForEqualAccessTimes()
    {
        StoredCookie laterSequence = Cookie("later", "one.test", 9, 0, true);
        StoredCookie earliestSequence = Cookie("earliest", "two.test", 3, 0, true);
        StoredCookie middleSequence = Cookie("middle", "three.test", 5, 0, true);

        CookieEvictionResult result = CookieEvictionAlgorithm.Plan(
            [laterSequence, earliestSequence, middleSequence],
            Limits(10, 2),
            EvaluationTime,
            true);
        earliestSequence.Value = "changed-input";

        AssertSuccess(result);
        Assert.That(result.Evictions, Has.Length.EqualTo(1));
        Assert.That(result.Evictions[0].Cookie.Name, Is.EqualTo("earliest"));
        Assert.That(result.Evictions[0].Cookie.Value, Is.EqualTo("value"));
        Assert.That(result.Evictions[0].Cookie, Is.Not.SameAs(earliestSequence));
        Assert.That(result.RetainedCookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "middle", "later" }));
    }

    [Test]
    public void SecurePreferenceCanBeDisabledForBaselineOldestFirstBehavior()
    {
        StoredCookie secureOld = Cookie("secure", "example.test", 0, 0, true);
        StoredCookie insecureNew = Cookie("insecure", "example.test", 1, 1, false);

        CookieEvictionResult modern = CookieEvictionAlgorithm.Plan(
            [secureOld, insecureNew],
            Limits(1, 10),
            EvaluationTime,
            true);
        CookieEvictionResult baseline = CookieEvictionAlgorithm.Plan(
            [secureOld, insecureNew],
            Limits(1, 10),
            EvaluationTime,
            false);

        Assert.That(modern.Evictions[0].Cookie.Name, Is.EqualTo("insecure"));
        Assert.That(baseline.Evictions[0].Cookie.Name, Is.EqualTo("secure"));
    }

    [Test]
    public void EqualQuotaBoundariesDoNotEvictAndZeroLimitsRemoveAll()
    {
        StoredCookie first = Cookie("first", "example.test", 0, 0, false);
        StoredCookie second = Cookie("second", "example.test", 1, 1, true);

        CookieEvictionResult equal = CookieEvictionAlgorithm.Plan(
            [first, second],
            Limits(2, 2),
            EvaluationTime,
            true);
        CookieEvictionResult zero = CookieEvictionAlgorithm.Plan(
            [first, second],
            Limits(0, 0),
            EvaluationTime,
            true);

        AssertSuccess(equal);
        Assert.That(equal.Evictions, Is.Empty);
        Assert.That(equal.RetainedCookies, Has.Length.EqualTo(2));
        AssertSuccess(zero);
        Assert.That(zero.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(zero.RetainedCookies, Is.Empty);
    }

    [Test]
    public void InvalidLimitsModelsAndDuplicateStateFailDeterministically()
    {
        CookieLimits invalidLimits = Limits(-1, 10);
        StoredCookie valid = Cookie("valid", "example.test", 0, 0, false);
        StoredCookie duplicateIdentity = Cookie("valid", "example.test", 1, 1, true);
        StoredCookie duplicateSequence = Cookie("other", "example.test", 0, 1, true);

        CookieEvictionResult configuration = CookieEvictionAlgorithm.Plan(
            [valid],
            invalidLimits,
            EvaluationTime,
            true);
        CookieEvictionResult invalidModel = CookieEvictionAlgorithm.Plan(
            [valid, null!],
            Limits(10, 10),
            EvaluationTime,
            true);
        CookieEvictionResult identity = CookieEvictionAlgorithm.Plan(
            [valid, duplicateIdentity],
            Limits(10, 10),
            EvaluationTime,
            true);
        CookieEvictionResult sequence = CookieEvictionAlgorithm.Plan(
            [valid, duplicateSequence],
            Limits(10, 10),
            EvaluationTime,
            true);

        AssertFailure(configuration, CookieErrorCode.InvalidConfiguration);
        AssertFailure(invalidModel, CookieErrorCode.InvalidAttribute);
        AssertFailure(identity, CookieErrorCode.DuplicateIdentity);
        AssertFailure(sequence, CookieErrorCode.InvalidAttribute);
    }

    static CookieLimits Limits(int maximumPerDomain, int maximumTotal)
    {
        return new CookieLimits
        {
            MaximumCookiesPerDomain = maximumPerDomain,
            MaximumCookies = maximumTotal
        };
    }

    static StoredCookie Cookie(
        string name,
        string domain,
        long sequenceNumber,
        int accessDayOffset,
        bool secure)
    {
        DateTimeOffset time = EvaluationTime.AddDays(-10 + accessDayOffset);
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = domain,
            Path = "/",
            CreationTime = time,
            LastAccessTime = time,
            Secure = secure,
            SequenceNumber = sequenceNumber
        };
    }

    static void AssertSuccess(CookieEvictionResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
    }

    static void AssertFailure(
        CookieEvictionResult result,
        CookieErrorCode errorCode)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.RetainedCookies, Is.Empty);
        Assert.That(result.Evictions, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
