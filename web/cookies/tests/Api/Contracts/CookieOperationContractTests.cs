namespace cookies.Tests;

public class CookieOperationContractTests
{
    [Test]
    public void SerializerContractCarriesModelsOptionsAndSplitResults()
    {
        CookieContractServices services = new CookieContractServices();
        ICookieSerializer serializer = services;
        CookieSerializationOptions options = new CookieSerializationOptions();
        SetCookie cookie = new SetCookie { Name = "id", Value = "1" };
        CookiePair[] pairs = [new CookiePair { Name = "id", Value = "1" }];

        Assert.That(serializer.SerializeSetCookie(cookie, options), Is.EqualTo("id=1"));
        Assert.That(serializer.SerializeCookiePairs(pairs, options), Is.EqualTo("id=1"));
        CookieSelectionResult result = serializer.SerializeCookieFields(pairs, options);
        Assert.That(result.Pairs, Is.SameAs(pairs));
        Assert.That(result.FieldValues, Is.EqualTo(new[] { "id=1" }));
    }

    [Test]
    public void MatcherContractUsesExplicitInputsForEveryDecision()
    {
        CookieContractServices services = new CookieContractServices();
        ICookieMatcher matcher = services;
        Uri uri = new Uri("https://Example.Test/account/page");
        StoredCookie cookie = new StoredCookie
        {
            Name = "id",
            Domain = "example.test",
            Path = "/account",
            HasExpires = true,
            Expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        CookieRequestContext context = new CookieRequestContext { RequestUri = uri };

        Assert.That(matcher.CanonicalizeHost(uri), Is.EqualTo("example.test"));
        Assert.That(matcher.DomainMatches("sub.example.test", "example.test"), Is.True);
        Assert.That(matcher.ComputeDefaultPath(uri), Is.EqualTo("/account"));
        Assert.That(matcher.PathMatches("/account/page", "/account"), Is.True);
        Assert.That(matcher.ComputeSchemefulSite(uri), Is.EqualTo("https://example.test"));
        Assert.That(matcher.EvaluateCookieMatch(cookie, context).Cookie, Is.SameAs(cookie));
        Assert.That(matcher.IsCookieExpired(cookie, cookie.Expires), Is.True);
    }

    [Test]
    public void StoreContractSupportsAtomicMutationRemovalAndSnapshots()
    {
        CookieContractServices services = new CookieContractServices();
        ICookieStore store = services;
        StoredCookie cookie = new StoredCookie { Name = "id", Domain = "example.test", Path = "/" };
        CookieIdentity identity = new CookieIdentity { Name = "id", Domain = "example.test", Path = "/" };

        CookieSetResult applied = store.ApplyStoredCookie(cookie);
        Assert.That(applied.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(store.FindStoredCookie(identity), Is.SameAs(cookie));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Is.EqualTo(new[] { cookie }));

        DateTimeOffset accessTime = new DateTimeOffset(
            2031,
            2,
            3,
            4,
            5,
            6,
            TimeSpan.Zero);
        StoredCookie[] updated = store.UpdateStoredCookieAccess([identity], accessTime);
        Assert.That(updated.Single().LastAccessTime, Is.EqualTo(accessTime));

        CookieJarSnapshot captured = store.CaptureStoreSnapshot();
        Assert.That(captured.Cookies, Is.EqualTo(new[] { cookie }));

        StoredCookie replacement = new StoredCookie { Name = "replacement" };
        store.ReplaceStoreSnapshot(new CookieJarSnapshot { Cookies = [replacement] });
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Is.EqualTo(new[] { replacement }));
        Assert.That(store.RemoveStoredCookies(new CookieRemovalQuery()), Is.EqualTo(1));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void PolicyBoundaryContractsRemainInjectableAndDeterministic()
    {
        CookieContractServices services = new CookieContractServices
        {
            UtcNow = new DateTimeOffset(2029, 5, 6, 7, 8, 9, TimeSpan.Zero)
        };
        IPublicSuffixList suffixes = services;
        ISecureOriginPolicy secureOrigins = services;
        ICookieClock clock = services;

        Assert.That(suffixes.GetPublicSuffix("www.example.test"), Is.EqualTo("test"));
        Assert.That(suffixes.GetRegistrableDomain("www.example.test"), Is.EqualTo("example.test"));
        Assert.That(suffixes.IsPublicSuffix("test"), Is.True);
        Assert.That(secureOrigins.IsSecureOrigin(new Uri("https://example.test")), Is.True);
        Assert.That(secureOrigins.IsSecureOrigin(new Uri("http://example.test")), Is.False);
        Assert.That(clock.GetUtcNow(), Is.EqualTo(services.UtcNow));
    }
}

public class CookieContractServices : ICookieSerializer, ICookieMatcher, ICookieStore, IPublicSuffixList, ISecureOriginPolicy, ICookieClock
{
    public DateTimeOffset UtcNow;
    public StoredCookie[] Cookies = [];

    public string SerializeSetCookie(SetCookie cookie, CookieSerializationOptions options)
    {
        return cookie.Name + "=" + cookie.Value;
    }

    public string SerializeCookiePairs(CookiePair[] pairs, CookieSerializationOptions options)
    {
        if (pairs.Length == 0)
        {
            return "";
        }

        return pairs[0].Name + "=" + pairs[0].Value;
    }

    public CookieSelectionResult SerializeCookieFields(CookiePair[] pairs, CookieSerializationOptions options)
    {
        return new CookieSelectionResult
        {
            Success = true,
            Pairs = pairs,
            FieldValues = [SerializeCookiePairs(pairs, options)]
        };
    }

    public string CanonicalizeHost(Uri uri)
    {
        return uri.Host.ToLowerInvariant();
    }

    public bool DomainMatches(string requestHost, string cookieDomain)
    {
        return requestHost == cookieDomain || requestHost.EndsWith("." + cookieDomain, StringComparison.Ordinal);
    }

    public string ComputeDefaultPath(Uri origin)
    {
        int finalSlash = origin.AbsolutePath.LastIndexOf('/');
        if (finalSlash <= 0)
        {
            return "/";
        }

        return origin.AbsolutePath.Substring(0, finalSlash);
    }

    public bool PathMatches(string requestPath, string cookiePath)
    {
        return requestPath.StartsWith(cookiePath, StringComparison.Ordinal);
    }

    public string ComputeSchemefulSite(Uri uri)
    {
        return uri.Scheme + "://" + uri.Host.ToLowerInvariant();
    }

    public CookieMatchResult EvaluateCookieMatch(StoredCookie cookie, CookieRequestContext context)
    {
        return new CookieMatchResult
        {
            Cookie = cookie,
            Status = CookieMatchStatus.Included,
            ErrorCode = CookieErrorCode.None
        };
    }

    public bool IsCookieExpired(StoredCookie cookie, DateTimeOffset evaluationTime)
    {
        return cookie.HasExpires && cookie.Expires <= evaluationTime;
    }

    public StoredCookie? FindStoredCookie(CookieIdentity identity)
    {
        if (Cookies.Length == 0)
        {
            return null;
        }

        return Cookies[0];
    }

    public StoredCookie[] QueryStoredCookies(CookieStoreQuery query)
    {
        return Cookies;
    }

    public CookieSetResult ApplyStoredCookie(StoredCookie cookie)
    {
        Cookies = [cookie];
        return new CookieSetResult
        {
            Cookie = cookie,
            Status = CookieSetStatus.Accepted,
            ErrorCode = CookieErrorCode.None
        };
    }

    public StoredCookie[] UpdateStoredCookieAccess(
        CookieIdentity[] identities,
        DateTimeOffset accessTime)
    {
        int index = 0;
        while (index < Cookies.Length)
        {
            Cookies[index].LastAccessTime = accessTime;
            index++;
        }

        return Cookies;
    }

    public int RemoveStoredCookies(CookieRemovalQuery query)
    {
        int count = Cookies.Length;
        Cookies = [];
        return count;
    }

    public CookieJarSnapshot CaptureStoreSnapshot()
    {
        return new CookieJarSnapshot { Cookies = Cookies };
    }

    public void ReplaceStoreSnapshot(CookieJarSnapshot snapshot)
    {
        Cookies = snapshot.Cookies;
    }

    public string? GetPublicSuffix(string canonicalDomain)
    {
        return "test";
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        return "example.test";
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain == "test";
    }

    public bool IsSecureOrigin(Uri origin)
    {
        return origin.Scheme == "https";
    }

    public DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }
}
