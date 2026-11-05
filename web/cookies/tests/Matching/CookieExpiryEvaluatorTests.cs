namespace cookies.Tests;

public class CookieExpiryEvaluatorTests
{
    static readonly DateTimeOffset ReceiptTime = new DateTimeOffset(
        2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Test]
    public void MaxAgeTakesPrecedenceOverLaterExpires()
    {
        SetCookie cookie = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 60,
            HasExpires = true,
            Expires = ReceiptTime.AddYears(10)
        };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265);

        AssertPersistent(result, ReceiptTime.AddSeconds(60), false, true, false, false);
    }

    [Test]
    public void PositiveMaxAgeOverridesAlreadyExpiredExpires()
    {
        SetCookie cookie = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 1,
            HasExpires = true,
            Expires = ReceiptTime.AddDays(-1)
        };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265);

        AssertPersistent(result, ReceiptTime.AddSeconds(1), false, true, false, false);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(long.MinValue)]
    public void NonPositiveMaxAgeRequestsImmediateDeletion(long maxAgeSeconds)
    {
        SetCookie cookie = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = maxAgeSeconds,
            HasExpires = true,
            Expires = ReceiptTime.AddYears(10)
        };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265Bis);

        AssertPersistent(
            result,
            DateTimeOffset.MinValue,
            true,
            true,
            false,
            false);
    }

    [Test]
    public void CookieWithoutLifetimeAttributesIsASessionCookie()
    {
        CookieExpiryResult result = Normalize(new SetCookie(), CookieComplianceMode.Rfc6265Bis);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.HasExpires, Is.False);
        Assert.That(result.Expires, Is.EqualTo(default(DateTimeOffset)));
        Assert.That(result.Persistent, Is.False);
        Assert.That(result.IsImmediateDeletion, Is.False);
        Assert.That(result.UsedMaxAge, Is.False);
        Assert.That(result.WasCapped, Is.False);
        Assert.That(result.ArithmeticSaturated, Is.False);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ExpiresOnlyCookieIsPersistentAndNormalizedToUtc()
    {
        DateTimeOffset supplied = new DateTimeOffset(2025, 1, 3, 5, 4, 5, TimeSpan.FromHours(2));
        SetCookie cookie = new SetCookie { HasExpires = true, Expires = supplied };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265);

        AssertPersistent(result, ReceiptTime.AddDays(1), false, false, false, false);
        Assert.That(result.Expires.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [TestCase(-1, true)]
    [TestCase(0, true)]
    [TestCase(1, false)]
    public void ExpiresAtOrBeforeReceiptTimeRequestsImmediateDeletion(int seconds, bool expected)
    {
        SetCookie cookie = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddSeconds(seconds)
        };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265);

        Assert.That(result.IsImmediateDeletion, Is.EqualTo(expected));
        Assert.That(result.Persistent, Is.True);
        Assert.That(result.UsedMaxAge, Is.False);
    }

    [Test]
    public void BaselineProfileDoesNotApplyTheConfiguredModernCap()
    {
        SetCookie maxAge = new SetCookie { HasMaxAge = true, MaxAgeSeconds = 101 };
        SetCookie expires = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddSeconds(101)
        };

        CookieExpiryResult maxAgeResult = CookieExpiryEvaluator.Normalize(
            maxAge,
            ReceiptTime,
            CookieComplianceMode.Rfc6265,
            TimeSpan.FromSeconds(100));
        CookieExpiryResult expiresResult = CookieExpiryEvaluator.Normalize(
            expires,
            ReceiptTime,
            CookieComplianceMode.Rfc6265,
            TimeSpan.FromSeconds(100));

        AssertPersistent(maxAgeResult, ReceiptTime.AddSeconds(101), false, true, false, false);
        AssertPersistent(expiresResult, ReceiptTime.AddSeconds(101), false, false, false, false);
    }

    [TestCase(CookieComplianceMode.Rfc6265Bis)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void ModernProfilesCapMaxAgeAndExpiresAtTheConfiguredLifetime(
        CookieComplianceMode complianceMode)
    {
        SetCookie maxAge = new SetCookie { HasMaxAge = true, MaxAgeSeconds = 101 };
        SetCookie expires = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddSeconds(101)
        };
        TimeSpan limit = TimeSpan.FromSeconds(100);

        CookieExpiryResult maxAgeResult = CookieExpiryEvaluator.Normalize(
            maxAge, ReceiptTime, complianceMode, limit);
        CookieExpiryResult expiresResult = CookieExpiryEvaluator.Normalize(
            expires, ReceiptTime, complianceMode, limit);

        AssertPersistent(maxAgeResult, ReceiptTime.AddSeconds(100), false, true, true, false);
        AssertPersistent(expiresResult, ReceiptTime.AddSeconds(100), false, false, true, false);
        AssertLifetimeWarning(maxAgeResult);
        AssertLifetimeWarning(expiresResult);
    }

    [Test]
    public void ModernProfileUsesTheDefaultFourHundredDayCapForBothAttributes()
    {
        SetCookie maxAge = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 34560001
        };
        SetCookie expires = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddDays(401)
        };

        CookieExpiryResult maxAgeResult = Normalize(maxAge, CookieComplianceMode.Rfc6265Bis);
        CookieExpiryResult expiresResult = Normalize(expires, CookieComplianceMode.Rfc6265Bis);

        Assert.That(maxAgeResult.Expires, Is.EqualTo(ReceiptTime.AddDays(400)));
        Assert.That(expiresResult.Expires, Is.EqualTo(ReceiptTime.AddDays(400)));
        Assert.That(maxAgeResult.WasCapped, Is.True);
        Assert.That(expiresResult.WasCapped, Is.True);
    }

    [Test]
    public void LifetimeExactlyAtTheCapIsNotReportedAsCapped()
    {
        SetCookie maxAge = new SetCookie { HasMaxAge = true, MaxAgeSeconds = 100 };
        SetCookie expires = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddSeconds(100)
        };
        TimeSpan limit = TimeSpan.FromSeconds(100);

        CookieExpiryResult maxAgeResult = CookieExpiryEvaluator.Normalize(
            maxAge, ReceiptTime, CookieComplianceMode.Rfc6265Bis, limit);
        CookieExpiryResult expiresResult = CookieExpiryEvaluator.Normalize(
            expires, ReceiptTime, CookieComplianceMode.Rfc6265Bis, limit);

        Assert.That(maxAgeResult.WasCapped, Is.False);
        Assert.That(expiresResult.WasCapped, Is.False);
        Assert.That(maxAgeResult.Issues, Is.Empty);
        Assert.That(expiresResult.Issues, Is.Empty);
    }

    [Test]
    public void HugeMaxAgeSaturatesSafelyInTheBaselineProfile()
    {
        SetCookie cookie = new SetCookie { HasMaxAge = true, MaxAgeSeconds = long.MaxValue };

        CookieExpiryResult result = Normalize(cookie, CookieComplianceMode.Rfc6265);

        AssertPersistent(result, DateTimeOffset.MaxValue, false, true, false, true);
    }

    [Test]
    public void ModernCapArithmeticSaturatesSafelyNearTheMaximumDate()
    {
        DateTimeOffset receipt = DateTimeOffset.MaxValue.AddTicks(-5);
        SetCookie cookie = new SetCookie { HasExpires = true, Expires = DateTimeOffset.MaxValue };

        CookieExpiryResult result = CookieExpiryEvaluator.Normalize(
            cookie,
            receipt,
            CookieComplianceMode.Rfc6265Bis,
            CookieDefaults.MaximumLifetime);

        AssertPersistent(result, DateTimeOffset.MaxValue, false, false, false, true);
    }

    [Test]
    public void SubsecondConfiguredCapIsAppliedWithoutRounding()
    {
        SetCookie cookie = new SetCookie { HasMaxAge = true, MaxAgeSeconds = 1 };
        TimeSpan cap = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 2);

        CookieExpiryResult result = CookieExpiryEvaluator.Normalize(
            cookie,
            ReceiptTime,
            CookieComplianceMode.Rfc6265Bis,
            cap);

        AssertPersistent(result, ReceiptTime.Add(cap), false, true, true, false);
    }

    [Test]
    public void StoredExpiryUsesAnInclusiveClockBoundaryAndIgnoresSessionCookies()
    {
        StoredCookie persistent = new StoredCookie
        {
            HasExpires = true,
            Persistent = true,
            Expires = ReceiptTime
        };
        StoredCookie session = new StoredCookie
        {
            HasExpires = false,
            Persistent = false,
            Expires = DateTimeOffset.MinValue
        };

        Assert.That(CookieExpiryEvaluator.IsExpired(persistent, ReceiptTime.AddTicks(-1)), Is.False);
        Assert.That(CookieExpiryEvaluator.IsExpired(persistent, ReceiptTime), Is.True);
        Assert.That(CookieExpiryEvaluator.IsExpired(persistent, ReceiptTime.AddTicks(1)), Is.True);
        Assert.That(CookieExpiryEvaluator.IsExpired(session, DateTimeOffset.MaxValue), Is.False);
        Assert.That(CookieExpiryEvaluator.IsExpired(null!, ReceiptTime), Is.False);
    }

    [Test]
    public void InvalidInputsReturnStableFailures()
    {
        CookieExpiryResult nullCookie = CookieExpiryEvaluator.Normalize(
            null!, ReceiptTime, CookieComplianceMode.Rfc6265, CookieDefaults.MaximumLifetime);
        CookieExpiryResult invalidMode = CookieExpiryEvaluator.Normalize(
            new SetCookie(), ReceiptTime, (CookieComplianceMode)99, CookieDefaults.MaximumLifetime);
        CookieExpiryResult negativeLimit = CookieExpiryEvaluator.Normalize(
            new SetCookie(), ReceiptTime, CookieComplianceMode.Rfc6265, TimeSpan.FromTicks(-1));

        AssertFailure(nullCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(invalidMode, CookieErrorCode.InvalidConfiguration);
        AssertFailure(negativeLimit, CookieErrorCode.InvalidConfiguration);
    }

    static CookieExpiryResult Normalize(SetCookie cookie, CookieComplianceMode complianceMode)
    {
        return CookieExpiryEvaluator.Normalize(
            cookie,
            ReceiptTime,
            complianceMode,
            CookieDefaults.MaximumLifetime);
    }

    static void AssertPersistent(
        CookieExpiryResult result,
        DateTimeOffset expectedExpires,
        bool immediateDeletion,
        bool usedMaxAge,
        bool wasCapped,
        bool saturated)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.HasExpires, Is.True);
        Assert.That(result.Expires, Is.EqualTo(expectedExpires));
        Assert.That(result.Persistent, Is.True);
        Assert.That(result.IsImmediateDeletion, Is.EqualTo(immediateDeletion));
        Assert.That(result.UsedMaxAge, Is.EqualTo(usedMaxAge));
        Assert.That(result.WasCapped, Is.EqualTo(wasCapped));
        Assert.That(result.ArithmeticSaturated, Is.EqualTo(saturated));
    }

    static void AssertLifetimeWarning(CookieExpiryResult result)
    {
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.LifetimeLimitExceeded));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(result.Issues[0].HasOffset, Is.False);
        Assert.That(result.Issues[0].SafeFragment, Is.Empty);
    }

    static void AssertFailure(CookieExpiryResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.HasExpires, Is.False);
        Assert.That(result.Persistent, Is.False);
        Assert.That(result.IsImmediateDeletion, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
