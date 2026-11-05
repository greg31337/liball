namespace cookies;

internal static class CookieExpiryEvaluator
{
    internal static CookieExpiryResult Normalize(
        SetCookie cookie,
        global::System.DateTimeOffset receiptTime,
        CookieComplianceMode complianceMode,
        global::System.TimeSpan maximumLifetime)
    {
        if (cookie == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!IsValidComplianceMode(complianceMode) || maximumLifetime.Ticks < 0)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        global::System.DateTimeOffset utcReceiptTime = receiptTime.ToUniversalTime();
        if (cookie.HasMaxAge)
        {
            return NormalizeMaxAge(
                cookie.MaxAgeSeconds,
                utcReceiptTime,
                complianceMode,
                maximumLifetime);
        }

        if (cookie.HasExpires)
        {
            return NormalizeExpires(
                cookie.Expires,
                utcReceiptTime,
                complianceMode,
                maximumLifetime);
        }

        return CreateSession();
    }

    internal static bool IsExpired(
        StoredCookie cookie,
        global::System.DateTimeOffset evaluationTime)
    {
        if (cookie == null || !cookie.HasExpires)
        {
            return false;
        }

        return cookie.Expires.ToUniversalTime() <= evaluationTime.ToUniversalTime();
    }

    static CookieExpiryResult NormalizeMaxAge(
        long maxAgeSeconds,
        global::System.DateTimeOffset receiptTime,
        CookieComplianceMode complianceMode,
        global::System.TimeSpan maximumLifetime)
    {
        if (maxAgeSeconds <= 0)
        {
            return CreatePersistent(
                global::System.DateTimeOffset.MinValue,
                true,
                true,
                false,
                false);
        }

        DateTimeSaturationResult desired = DecimalSecondsParser.AddSecondsSaturated(
            receiptTime,
            maxAgeSeconds);
        global::System.DateTimeOffset expires = desired.Value;
        bool wasCapped = false;
        if (UsesModernRules(complianceMode))
        {
            DateTimeSaturationResult cap = AddDurationSaturated(receiptTime, maximumLifetime);
            if (expires > cap.Value)
            {
                expires = cap.Value;
                wasCapped = true;
            }
        }

        return CreatePersistent(
            expires,
            expires <= receiptTime,
            true,
            wasCapped,
            desired.Saturated);
    }

    static CookieExpiryResult NormalizeExpires(
        global::System.DateTimeOffset suppliedExpires,
        global::System.DateTimeOffset receiptTime,
        CookieComplianceMode complianceMode,
        global::System.TimeSpan maximumLifetime)
    {
        global::System.DateTimeOffset expires = suppliedExpires.ToUniversalTime();
        bool wasCapped = false;
        bool saturated = false;
        if (UsesModernRules(complianceMode))
        {
            DateTimeSaturationResult cap = AddDurationSaturated(receiptTime, maximumLifetime);
            saturated = cap.Saturated;
            if (expires > cap.Value)
            {
                expires = cap.Value;
                wasCapped = true;
            }
        }

        return CreatePersistent(
            expires,
            expires <= receiptTime,
            false,
            wasCapped,
            saturated);
    }

    static DateTimeSaturationResult AddDurationSaturated(
        global::System.DateTimeOffset origin,
        global::System.TimeSpan duration)
    {
        DateTimeSaturationResult result = new DateTimeSaturationResult();
        global::System.DateTimeOffset utcOrigin = origin.ToUniversalTime();
        long availableTicks = global::System.DateTimeOffset.MaxValue.Ticks - utcOrigin.Ticks;
        if (duration.Ticks > availableTicks)
        {
            result.Saturated = true;
            result.Value = global::System.DateTimeOffset.MaxValue;
            return result;
        }

        result.Value = new global::System.DateTimeOffset(
            utcOrigin.Ticks + duration.Ticks,
            global::System.TimeSpan.Zero);
        return result;
    }

    static bool IsValidComplianceMode(CookieComplianceMode complianceMode)
    {
        return complianceMode == CookieComplianceMode.Rfc6265 ||
            complianceMode == CookieComplianceMode.Rfc6265Bis ||
            complianceMode == CookieComplianceMode.Compatible;
    }

    static bool UsesModernRules(CookieComplianceMode complianceMode)
    {
        return complianceMode == CookieComplianceMode.Rfc6265Bis ||
            complianceMode == CookieComplianceMode.Compatible;
    }

    static CookieIssue CreateIssue(CookieErrorCode errorCode, CookieParseSeverity severity)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = severity;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    static CookieExpiryResult CreateSession()
    {
        CookieExpiryResult result = new CookieExpiryResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        return result;
    }

    static CookieExpiryResult CreatePersistent(
        global::System.DateTimeOffset expires,
        bool isImmediateDeletion,
        bool usedMaxAge,
        bool wasCapped,
        bool arithmeticSaturated)
    {
        CookieExpiryResult result = new CookieExpiryResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.HasExpires = true;
        result.Expires = expires;
        result.Persistent = true;
        result.IsImmediateDeletion = isImmediateDeletion;
        result.UsedMaxAge = usedMaxAge;
        result.WasCapped = wasCapped;
        result.ArithmeticSaturated = arithmeticSaturated;
        if (wasCapped)
        {
            result.Issues = [CreateIssue(
                CookieErrorCode.LifetimeLimitExceeded,
                CookieParseSeverity.Warning)];
        }

        return result;
    }

    static CookieExpiryResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieExpiryResult result = new CookieExpiryResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode, CookieParseSeverity.Error)];
        return result;
    }
}
