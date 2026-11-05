namespace cookies.Tests;

public class CookieLimitAndEvictionConformanceTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2037, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [Test]
    public void InputLimitAcceptsBelowAndEqualUtf8SizesAndRejectsAbove()
    {
        CookieParseOptions options = ParseOptions();
        options.Limits.MaximumInputLineOctets = 4;

        SetCookieParseResult below = SetCookieParserCore.Parse(
            "a=x".AsSpan(), options);
        SetCookieParseResult equal = SetCookieParserCore.Parse(
            "é=x".AsSpan(), options);
        SetCookieParseResult above = SetCookieParserCore.Parse(
            "é=xy".AsSpan(), options);

        Assert.That(below.Success, Is.True);
        Assert.That(equal.Success, Is.True);
        Assert.That(above.Success, Is.False);
        Assert.That(above.ErrorCode, Is.EqualTo(CookieErrorCode.InputLimitExceeded));
    }

    [Test]
    public void AttributeLimitAcceptsBelowAndEqualUtf8SizesAndIgnoresAbove()
    {
        CookieParseOptions options = ParseOptions();
        options.Limits.MaximumAttributeValueOctets = 3;

        SetCookieParseResult below = SetCookieParserCore.Parse(
            "id=1; Path=/a".AsSpan(), options);
        SetCookieParseResult equal = SetCookieParserCore.Parse(
            "id=1; Path=/é".AsSpan(), options);
        SetCookieParseResult above = SetCookieParserCore.Parse(
            "id=1; Path=/éa".AsSpan(), options);

        Assert.That(below.Cookie!.HasPath, Is.True);
        Assert.That(below.Cookie.Path, Is.EqualTo("/a"));
        Assert.That(equal.Cookie!.HasPath, Is.True);
        Assert.That(equal.Cookie.Path, Is.EqualTo("/é"));
        Assert.That(above.Success, Is.True);
        Assert.That(above.Cookie!.HasPath, Is.False);
        Assert.That(above.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.AttributeLimitExceeded));
    }

    [Test]
    public void PerDomainLimitEvictsOnlyWhenTheCountIsAboveTheBoundary()
    {
        StoredCookie first = Cookie(
            "first", "example.test", 0, EvaluationTime.AddHours(-3), false);
        StoredCookie second = Cookie(
            "second", "example.test", 1, EvaluationTime.AddHours(-2), false);
        StoredCookie third = Cookie(
            "third", "example.test", 2, EvaluationTime.AddHours(-1), false);
        CookieLimits limits = Limits(2, 10);

        CookieEvictionResult below = CookieEvictionAlgorithm.Plan(
            [first], limits, EvaluationTime, false);
        CookieEvictionResult equal = CookieEvictionAlgorithm.Plan(
            [first, second], limits, EvaluationTime, false);
        CookieEvictionResult above = CookieEvictionAlgorithm.Plan(
            [third, first, second], limits, EvaluationTime, false);

        Assert.That(below.Evictions, Is.Empty);
        Assert.That(equal.Evictions, Is.Empty);
        Assert.That(above.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(above.Evictions.Single().Reason,
            Is.EqualTo(CookieErrorCode.PerDomainLimitExceeded));
    }

    [Test]
    public void TotalLimitEvictsOnlyWhenTheCountIsAboveTheBoundary()
    {
        StoredCookie first = Cookie(
            "first", "one.test", 0, EvaluationTime.AddHours(-3), false);
        StoredCookie second = Cookie(
            "second", "two.test", 1, EvaluationTime.AddHours(-2), false);
        StoredCookie third = Cookie(
            "third", "three.test", 2, EvaluationTime.AddHours(-1), false);
        CookieLimits limits = Limits(10, 2);

        CookieEvictionResult below = CookieEvictionAlgorithm.Plan(
            [first], limits, EvaluationTime, false);
        CookieEvictionResult equal = CookieEvictionAlgorithm.Plan(
            [first, second], limits, EvaluationTime, false);
        CookieEvictionResult above = CookieEvictionAlgorithm.Plan(
            [third, first, second], limits, EvaluationTime, false);

        Assert.That(below.Evictions, Is.Empty);
        Assert.That(equal.Evictions, Is.Empty);
        Assert.That(above.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(above.Evictions.Single().Reason,
            Is.EqualTo(CookieErrorCode.TotalLimitExceeded));
    }

    [Test]
    public void LifetimeLimitLeavesBelowAndEqualAgesAndCapsAbove()
    {
        TimeSpan limit = TimeSpan.FromSeconds(10);
        CookieExpiryResult below = CookieExpiryEvaluator.Normalize(
            MaxAge(9),
            EvaluationTime,
            CookieComplianceMode.Rfc6265Bis,
            limit);
        CookieExpiryResult equal = CookieExpiryEvaluator.Normalize(
            MaxAge(10),
            EvaluationTime,
            CookieComplianceMode.Rfc6265Bis,
            limit);
        CookieExpiryResult above = CookieExpiryEvaluator.Normalize(
            MaxAge(11),
            EvaluationTime,
            CookieComplianceMode.Rfc6265Bis,
            limit);

        Assert.That(below.Expires, Is.EqualTo(EvaluationTime.AddSeconds(9)));
        Assert.That(below.WasCapped, Is.False);
        Assert.That(equal.Expires, Is.EqualTo(EvaluationTime.AddSeconds(10)));
        Assert.That(equal.WasCapped, Is.False);
        Assert.That(above.Expires, Is.EqualTo(EvaluationTime.AddSeconds(10)));
        Assert.That(above.WasCapped, Is.True);
        Assert.That(above.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[] { CookieErrorCode.LifetimeLimitExceeded }));
    }

    [Test]
    public void HeaderLimitKeepsBelowAndEqualFieldsAndSplitsAbove()
    {
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult below = CookieRequestSerializerCore.SerializeFields(
            [Pair("a", "1"), Pair("b", "2")],
            options);
        CookieSelectionResult equal = CookieRequestSerializerCore.SerializeFields(
            [Pair("a", "1"), Pair("bb", "22")],
            options);
        CookieSelectionResult above = CookieRequestSerializerCore.SerializeFields(
            [Pair("a", "1"), Pair("bbb", "333")],
            options);

        Assert.That(below.FieldValues, Is.EqualTo(new[] { "a=1; b=2" }));
        Assert.That(equal.FieldValues, Is.EqualTo(new[] { "a=1; bb=22" }));
        Assert.That(above.Success, Is.True);
        Assert.That(above.FieldValues,
            Is.EqualTo(new[] { "a=1", "bbb=333" }));
    }

    [Test]
    public void EvictionRunsExpiredThenNonSecureDomainThenDomainThenGlobalPhases()
    {
        StoredCookie expired = Cookie(
            "expired", "expired.test", 0, EvaluationTime, true);
        expired.HasExpires = true;
        expired.Expires = EvaluationTime;
        StoredCookie secureOldest = Cookie(
            "secure-oldest", "crowded.test", 1, EvaluationTime.AddHours(-8), true);
        StoredCookie nonSecureNewest = Cookie(
            "non-secure-newest", "crowded.test", 2, EvaluationTime.AddHours(-1), false);
        StoredCookie secureMiddle = Cookie(
            "secure-middle", "crowded.test", 3, EvaluationTime.AddHours(-4), true);
        StoredCookie secureNewest = Cookie(
            "secure-newest", "crowded.test", 4, EvaluationTime, true);
        StoredCookie globalOldest = Cookie(
            "global-oldest", "other.test", 5, EvaluationTime.AddHours(-10), true);
        StoredCookie globalNewest = Cookie(
            "global-newest", "third.test", 6, EvaluationTime, false);

        CookieEvictionResult result = CookieEvictionAlgorithm.Plan(
            [
                globalNewest,
                secureMiddle,
                expired,
                secureNewest,
                globalOldest,
                nonSecureNewest,
                secureOldest
            ],
            Limits(2, 3),
            EvaluationTime,
            true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[]
            {
                "expired",
                "non-secure-newest",
                "secure-oldest",
                "global-oldest"
            }));
        Assert.That(result.Evictions.Select(entry => entry.Reason),
            Is.EqualTo(new[]
            {
                CookieErrorCode.Expired,
                CookieErrorCode.PerDomainLimitExceeded,
                CookieErrorCode.PerDomainLimitExceeded,
                CookieErrorCode.TotalLimitExceeded
            }));
        Assert.That(result.RetainedCookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[]
            {
                "secure-middle",
                "secure-newest",
                "global-newest"
            }));
    }

    [Test]
    public void EqualAccessTieBreakingUsesSequenceRegardlessOfInputOrder()
    {
        StoredCookie first = Cookie(
            "first", "one.test", 1, EvaluationTime, false);
        StoredCookie second = Cookie(
            "second", "two.test", 2, EvaluationTime, false);
        StoredCookie third = Cookie(
            "third", "three.test", 3, EvaluationTime, false);
        CookieLimits limits = Limits(10, 1);

        CookieEvictionResult forward = CookieEvictionAlgorithm.Plan(
            [first, second, third], limits, EvaluationTime, false);
        CookieEvictionResult reverse = CookieEvictionAlgorithm.Plan(
            [third, second, first], limits, EvaluationTime, false);

        Assert.That(forward.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(reverse.Evictions.Select(entry => entry.Cookie.Name),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(forward.RetainedCookies.Single().Name, Is.EqualTo("third"));
        Assert.That(reverse.RetainedCookies.Single().Name, Is.EqualTo("third"));
    }

    static CookieParseOptions ParseOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };
    }

    static CookieLimits Limits(int perDomain, int total)
    {
        return new CookieLimits
        {
            MaximumCookiesPerDomain = perDomain,
            MaximumCookies = total
        };
    }

    static SetCookie MaxAge(long seconds)
    {
        return new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = seconds
        };
    }

    static CookiePair Pair(string name, string value)
    {
        return new CookiePair
        {
            Name = name,
            Value = value
        };
    }

    static StoredCookie Cookie(
        string name,
        string domain,
        long sequence,
        DateTimeOffset lastAccess,
        bool secure)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = domain,
            Path = "/",
            CreationTime = lastAccess,
            LastAccessTime = lastAccess,
            Secure = secure,
            SequenceNumber = sequence
        };
    }
}
