namespace cookies;

internal static class CookieStoreQueryEvaluator
{
    internal static bool IsValidStoreQuery(CookieStoreQuery query)
    {
        if (query == null ||
            (query.HasIdentity && !IsValidIdentity(query.Identity)) ||
            (query.HasDomain && (query.Domain == null ||
                !CookieStoreValidator.IsCanonicalDomain(query.Domain))) ||
            (query.HasPath && (query.Path == null || query.Path.Length == 0 ||
                query.Path[0] != '/')) ||
            (query.HasName && query.Name == null) ||
            (query.FilterByPartition && query.HasPartitionKey &&
                (query.PartitionKey == null || query.PartitionKey.Length == 0)) ||
            (query.ExpiredOnly && !query.HasEvaluationTime))
        {
            return false;
        }

        return true;
    }

    internal static bool IsValidRemovalQuery(CookieRemovalQuery query)
    {
        if (query == null)
        {
            return false;
        }

        return IsValidStoreQuery(CreateStoreQuery(query));
    }

    internal static CookieStoreQuery CreateStoreQuery(CookieRemovalQuery query)
    {
        if (query == null)
        {
            return new CookieStoreQuery();
        }

        return new CookieStoreQuery
        {
            HasIdentity = query.HasIdentity,
            Identity = CookieGraphCloner.CloneIdentity(query.Identity),
            HasDomain = query.HasDomain,
            Domain = query.Domain,
            IncludeSubdomains = query.IncludeSubdomains,
            HasPath = query.HasPath,
            Path = query.Path,
            HasName = query.HasName,
            Name = query.Name,
            FilterByPartition = query.FilterByPartition,
            HasPartitionKey = query.HasPartitionKey,
            PartitionKey = query.PartitionKey,
            SessionOnly = query.SessionOnly,
            ExpiredOnly = query.ExpiredOnly,
            HasEvaluationTime = query.HasEvaluationTime,
            EvaluationTime = query.EvaluationTime
        };
    }

    internal static bool Matches(StoredCookie cookie, CookieStoreQuery query)
    {
        if (cookie == null || query == null)
        {
            return false;
        }

        if (query.HasIdentity &&
            !CookieIdentityUtility.MatchesStoredCookie(cookie, query.Identity))
        {
            return false;
        }

        if (query.HasDomain && !DomainMatchesQuery(
            cookie.Domain,
            query.Domain,
            query.IncludeSubdomains))
        {
            return false;
        }

        if (query.HasPath && cookie.Path != query.Path)
        {
            return false;
        }

        if (query.HasName && cookie.Name != query.Name)
        {
            return false;
        }

        if (query.FilterByPartition &&
            (cookie.HasPartitionKey != query.HasPartitionKey ||
                (cookie.HasPartitionKey && cookie.PartitionKey != query.PartitionKey)))
        {
            return false;
        }

        if (query.SessionOnly && cookie.Persistent)
        {
            return false;
        }

        if (query.ExpiredOnly &&
            !CookieExpiryEvaluator.IsExpired(cookie, query.EvaluationTime))
        {
            return false;
        }

        return true;
    }

    internal static bool DomainMatchesQuery(
        string cookieDomain,
        string queryDomain,
        bool includeSubdomains)
    {
        if (cookieDomain == null || queryDomain == null)
        {
            return false;
        }

        if (cookieDomain == queryDomain)
        {
            return true;
        }

        if (!includeSubdomains || cookieDomain.Length <= queryDomain.Length ||
            !cookieDomain.EndsWith(
                queryDomain,
                global::System.StringComparison.Ordinal))
        {
            return false;
        }

        return cookieDomain[cookieDomain.Length - queryDomain.Length - 1] == '.';
    }

    static bool IsValidIdentity(CookieIdentity identity)
    {
        if (identity == null || identity.Name == null ||
            identity.Domain == null ||
            !CookieStoreValidator.IsCanonicalDomain(identity.Domain) ||
            identity.Path == null || identity.Path.Length == 0 || identity.Path[0] != '/' ||
            identity.PartitionKey == null ||
            (identity.HasPartitionKey && identity.PartitionKey.Length == 0))
        {
            return false;
        }

        return true;
    }
}
