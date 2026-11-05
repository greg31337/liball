namespace cookies;

internal static class CookieEvictionAlgorithm
{
    internal static CookieEvictionResult Plan(
        StoredCookie[] cookies,
        CookieLimits limits,
        global::System.DateTimeOffset evaluationTime,
        bool evictNonSecureCookiesFirst)
    {
        CookieErrorCode validationError = ValidateInputs(cookies, limits);
        if (validationError != CookieErrorCode.None)
        {
            return CreateFailure(validationError);
        }

        StoredCookie[] working = CookieGraphCloner.CloneStoredCookies(cookies);
        bool[] removed = new bool[working.Length];
        global::System.Collections.Generic.List<CookieEvictionEntry> evictions =
            new global::System.Collections.Generic.List<CookieEvictionEntry>();

        int candidateIndex = FindExpiredCandidate(
            working,
            removed,
            evaluationTime);
        while (candidateIndex >= 0)
        {
            RecordEviction(
                working,
                removed,
                candidateIndex,
                CookieErrorCode.Expired,
                evictions);
            candidateIndex = FindExpiredCandidate(
                working,
                removed,
                evaluationTime);
        }

        if (evictNonSecureCookiesFirst)
        {
            candidateIndex = FindNonSecureDomainCandidate(
                working,
                removed,
                limits.MaximumCookiesPerDomain);
            while (candidateIndex >= 0)
            {
                RecordEviction(
                    working,
                    removed,
                    candidateIndex,
                    CookieErrorCode.PerDomainLimitExceeded,
                    evictions);
                candidateIndex = FindNonSecureDomainCandidate(
                    working,
                    removed,
                    limits.MaximumCookiesPerDomain);
            }
        }

        candidateIndex = FindDomainCandidate(
            working,
            removed,
            limits.MaximumCookiesPerDomain);
        while (candidateIndex >= 0)
        {
            RecordEviction(
                working,
                removed,
                candidateIndex,
                CookieErrorCode.PerDomainLimitExceeded,
                evictions);
            candidateIndex = FindDomainCandidate(
                working,
                removed,
                limits.MaximumCookiesPerDomain);
        }

        candidateIndex = FindGlobalCandidate(
            working,
            removed,
            limits.MaximumCookies);
        while (candidateIndex >= 0)
        {
            RecordEviction(
                working,
                removed,
                candidateIndex,
                CookieErrorCode.TotalLimitExceeded,
                evictions);
            candidateIndex = FindGlobalCandidate(
                working,
                removed,
                limits.MaximumCookies);
        }

        return CreateSuccess(working, removed, evictions);
    }

    static CookieErrorCode ValidateInputs(
        StoredCookie[] cookies,
        CookieLimits limits)
    {
        if (limits == null || limits.MaximumCookiesPerDomain < 0 ||
            limits.MaximumCookies < 0)
        {
            return CookieErrorCode.InvalidConfiguration;
        }

        if (cookies == null)
        {
            return CookieErrorCode.InvalidAttribute;
        }

        int index = 0;
        while (index < cookies.Length)
        {
            StoredCookie cookie = cookies[index];
            if (!CookieStoreValidator.IsValidStoredCookie(cookie) ||
                cookie.SequenceNumber < 0)
            {
                return CookieErrorCode.InvalidAttribute;
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (CookieIdentityUtility.HaveSameStoredIdentity(
                    cookies[priorIndex],
                    cookie))
                {
                    return CookieErrorCode.DuplicateIdentity;
                }

                if (cookies[priorIndex].SequenceNumber == cookie.SequenceNumber)
                {
                    return CookieErrorCode.InvalidAttribute;
                }

                priorIndex = priorIndex + 1;
            }

            index = index + 1;
        }

        return CookieErrorCode.None;
    }

    static int FindExpiredCandidate(
        StoredCookie[] cookies,
        bool[] removed,
        global::System.DateTimeOffset evaluationTime)
    {
        int selected = -1;
        int index = 0;
        while (index < cookies.Length)
        {
            if (!removed[index] &&
                CookieExpiryEvaluator.IsExpired(cookies[index], evaluationTime) &&
                IsOlderForEviction(cookies[index], selected < 0 ? null : cookies[selected]))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindNonSecureDomainCandidate(
        StoredCookie[] cookies,
        bool[] removed,
        int maximumCookiesPerDomain)
    {
        int selected = -1;
        int index = 0;
        while (index < cookies.Length)
        {
            StoredCookie cookie = cookies[index];
            if (!removed[index] && !cookie.Secure &&
                IsDomainOverLimit(
                    cookies,
                    removed,
                    cookie.Domain,
                    maximumCookiesPerDomain) &&
                IsOlderForEviction(cookie, selected < 0 ? null : cookies[selected]))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindDomainCandidate(
        StoredCookie[] cookies,
        bool[] removed,
        int maximumCookiesPerDomain)
    {
        int selected = -1;
        int index = 0;
        while (index < cookies.Length)
        {
            StoredCookie cookie = cookies[index];
            if (!removed[index] &&
                IsDomainOverLimit(
                    cookies,
                    removed,
                    cookie.Domain,
                    maximumCookiesPerDomain) &&
                IsOlderForEviction(cookie, selected < 0 ? null : cookies[selected]))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    static int FindGlobalCandidate(
        StoredCookie[] cookies,
        bool[] removed,
        int maximumCookies)
    {
        if (CountRemaining(cookies, removed) <= maximumCookies)
        {
            return -1;
        }

        int selected = -1;
        int index = 0;
        while (index < cookies.Length)
        {
            if (!removed[index] &&
                IsOlderForEviction(
                    cookies[index],
                    selected < 0 ? null : cookies[selected]))
            {
                selected = index;
            }

            index = index + 1;
        }

        return selected;
    }

    static bool IsDomainOverLimit(
        StoredCookie[] cookies,
        bool[] removed,
        string domain,
        int maximumCookiesPerDomain)
    {
        return CountRemainingForDomain(cookies, removed, domain) >
            maximumCookiesPerDomain;
    }

    static int CountRemainingForDomain(
        StoredCookie[] cookies,
        bool[] removed,
        string domain)
    {
        int count = 0;
        int index = 0;
        while (index < cookies.Length)
        {
            if (!removed[index] && cookies[index].Domain == domain)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static int CountRemaining(StoredCookie[] cookies, bool[] removed)
    {
        int count = 0;
        int index = 0;
        while (index < cookies.Length)
        {
            if (!removed[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static bool IsOlderForEviction(StoredCookie candidate, StoredCookie? selected)
    {
        if (selected == null)
        {
            return true;
        }

        if (candidate.LastAccessTime < selected.LastAccessTime)
        {
            return true;
        }

        if (candidate.LastAccessTime > selected.LastAccessTime)
        {
            return false;
        }

        if (candidate.SequenceNumber < selected.SequenceNumber)
        {
            return true;
        }

        if (candidate.SequenceNumber > selected.SequenceNumber)
        {
            return false;
        }

        return global::System.String.Compare(
            CookieIdentityUtility.CreateStoredCookieKey(candidate),
            CookieIdentityUtility.CreateStoredCookieKey(selected),
            global::System.StringComparison.Ordinal) < 0;
    }

    static void RecordEviction(
        StoredCookie[] cookies,
        bool[] removed,
        int index,
        CookieErrorCode reason,
        global::System.Collections.Generic.List<CookieEvictionEntry> evictions)
    {
        removed[index] = true;
        CookieEvictionEntry entry = new CookieEvictionEntry();
        entry.Cookie = cookies[index];
        entry.Reason = reason;
        evictions.Add(entry);
    }

    static CookieEvictionResult CreateSuccess(
        StoredCookie[] cookies,
        bool[] removed,
        global::System.Collections.Generic.List<CookieEvictionEntry> evictions)
    {
        global::System.Collections.Generic.List<StoredCookie> retained =
            new global::System.Collections.Generic.List<StoredCookie>();
        int index = 0;
        while (index < cookies.Length)
        {
            if (!removed[index])
            {
                retained.Add(cookies[index]);
            }

            index = index + 1;
        }

        StoredCookie[] retainedCookies = retained.ToArray();
        SortRetainedCookies(retainedCookies);
        CookieEvictionEntry[] evictionEntries = evictions.ToArray();
        CookieIssue[] issues = new CookieIssue[evictionEntries.Length];
        int issueIndex = 0;
        while (issueIndex < evictionEntries.Length)
        {
            CookieErrorCode issueCode = CookieErrorCode.QuotaEviction;
            if (evictionEntries[issueIndex].Reason == CookieErrorCode.Expired)
            {
                issueCode = CookieErrorCode.Expired;
            }

            issues[issueIndex] = CreateIssue(
                issueCode,
                CookieParseSeverity.Information);
            issueIndex = issueIndex + 1;
        }

        CookieEvictionResult result = new CookieEvictionResult();
        result.Success = true;
        result.ErrorCode = CookieErrorCode.None;
        result.RetainedCookies = retainedCookies;
        result.Evictions = evictionEntries;
        result.Issues = issues;
        return result;
    }

    static CookieEvictionResult CreateFailure(CookieErrorCode errorCode)
    {
        CookieEvictionResult result = new CookieEvictionResult();
        result.ErrorCode = errorCode;
        result.Issues = [CreateIssue(errorCode, CookieParseSeverity.Error)];
        return result;
    }

    static CookieIssue CreateIssue(
        CookieErrorCode errorCode,
        CookieParseSeverity severity)
    {
        CookieIssue issue = new CookieIssue();
        issue.ErrorCode = errorCode;
        issue.Severity = severity;
        issue.HasOffset = false;
        issue.SafeFragment = "";
        return issue;
    }

    static void SortRetainedCookies(StoredCookie[] cookies)
    {
        int index = 1;
        while (index < cookies.Length)
        {
            StoredCookie value = cookies[index];
            int insertion = index;
            while (insertion > 0 &&
                CompareRetainedCookies(cookies[insertion - 1], value) > 0)
            {
                cookies[insertion] = cookies[insertion - 1];
                insertion = insertion - 1;
            }

            cookies[insertion] = value;
            index = index + 1;
        }
    }

    static int CompareRetainedCookies(StoredCookie left, StoredCookie right)
    {
        if (left.SequenceNumber < right.SequenceNumber)
        {
            return -1;
        }

        if (left.SequenceNumber > right.SequenceNumber)
        {
            return 1;
        }

        return global::System.String.Compare(
            CookieIdentityUtility.CreateStoredCookieKey(left),
            CookieIdentityUtility.CreateStoredCookieKey(right),
            global::System.StringComparison.Ordinal);
    }
}
