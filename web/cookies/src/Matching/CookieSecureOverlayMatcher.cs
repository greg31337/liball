namespace cookies;

internal static class CookieSecureOverlayMatcher
{
    internal static CookieSecureOverlayResult Evaluate(
        StoredCookie candidate,
        global::System.Uri origin,
        ISecureOriginPolicy secureOriginPolicy,
        StoredCookie[] existingCookies,
        bool protectSecureCookieOverlay)
    {
        if (!IsValidCandidate(candidate) || existingCookies == null)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (!protectSecureCookieOverlay || candidate.Secure)
        {
            return CreateAllowed(false, false, false, -1, null);
        }

        int overlapIndex = FindOverlap(candidate, existingCookies);
        if (overlapIndex == -2)
        {
            return CreateFailure(CookieErrorCode.InvalidAttribute);
        }

        if (overlapIndex < 0)
        {
            return CreateAllowed(false, false, false, -1, null);
        }

        StoredCookie overlap = existingCookies[overlapIndex];
        if (origin == null || !origin.IsAbsoluteUri)
        {
            return CreateFailure(CookieErrorCode.InvalidHost);
        }

        CookieBooleanResult secureOrigin = CookieSecureOriginEvaluator.Evaluate(
            secureOriginPolicy,
            origin);
        if (!secureOrigin.Success)
        {
            return CreateFailure(CookieErrorCode.InvalidConfiguration);
        }

        if (secureOrigin.Value)
        {
            return CreateAllowed(true, true, true, overlapIndex, overlap);
        }

        return CreateDenied(overlapIndex, overlap);
    }

    internal static bool DomainsOverlap(string firstDomain, string secondDomain)
    {
        return CookieDomainMatcher.DomainMatches(firstDomain, secondDomain, false) ||
            CookieDomainMatcher.DomainMatches(secondDomain, firstDomain, false);
    }

    static int FindOverlap(StoredCookie candidate, StoredCookie[] existingCookies)
    {
        int index = 0;
        while (index < existingCookies.Length)
        {
            StoredCookie existing = existingCookies[index];
            if (existing == null || existing.Name == null || existing.Domain == null ||
                existing.Path == null)
            {
                return -2;
            }

            if (existing.Secure && candidate.Name == existing.Name &&
                CookiePartitionEvaluator.PartitionKeysMatch(
                    candidate.HasPartitionKey,
                    candidate.PartitionKey,
                    existing.HasPartitionKey,
                    existing.PartitionKey) &&
                DomainsOverlap(candidate.Domain, existing.Domain) &&
                CookiePathMatcher.PathMatches(candidate.Path, existing.Path))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool IsValidCandidate(StoredCookie candidate)
    {
        return candidate != null && candidate.Name != null && candidate.Domain != null &&
            candidate.Domain.Length > 0 && candidate.Path != null && candidate.Path.Length > 0 &&
            candidate.Path[0] == '/';
    }

    static CookieIssue CreateIssue(CookieErrorCode errorCode)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = CookieParseSeverity.Error;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    static CookieSecureOverlayResult CreateAllowed(
        bool originWasEvaluated,
        bool originIsSecure,
        bool hasOverlap,
        int overlapIndex,
        StoredCookie? overlap)
    {
        CookieSecureOverlayResult result = new CookieSecureOverlayResult();
        result.Success = true;
        result.Allowed = true;
        result.ErrorCode = CookieErrorCode.None;
        result.OriginWasEvaluated = originWasEvaluated;
        result.OriginIsSecure = originIsSecure;
        result.HasOverlappingSecureCookie = hasOverlap;
        result.OverlappingCookieIndex = overlapIndex;
        result.OverlappingCookie = overlap;
        return result;
    }

    static CookieSecureOverlayResult CreateDenied(int overlapIndex, StoredCookie overlap)
    {
        CookieSecureOverlayResult result = new CookieSecureOverlayResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.SecureOverlayViolation;
        result.OriginWasEvaluated = true;
        result.HasOverlappingSecureCookie = true;
        result.OverlappingCookieIndex = overlapIndex;
        result.OverlappingCookie = overlap;
        result.Issues = [CreateIssue(CookieErrorCode.SecureOverlayViolation)];
        return result;
    }

    static CookieSecureOverlayResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieSecureOverlayResult result = new CookieSecureOverlayResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode)];
        return result;
    }
}
