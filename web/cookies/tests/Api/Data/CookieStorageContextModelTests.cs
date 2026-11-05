namespace cookies.Tests;

public class CookieStorageContextModelTests
{
    [Test]
    public void StoredCookieRetainsCompleteCanonicalState()
    {
        DateTimeOffset expires = new DateTimeOffset(2030, 2, 3, 4, 5, 6, TimeSpan.Zero);
        DateTimeOffset created = new DateTimeOffset(2029, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset accessed = created.AddMinutes(5);
        CookieAttribute extension = new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" };
        StoredCookie cookie = new StoredCookie
        {
            Name = "session",
            Value = "abc",
            Domain = "example.test",
            Path = "/account",
            HasExpires = true,
            Expires = expires,
            CreationTime = created,
            LastAccessTime = accessed,
            Persistent = true,
            HostOnly = true,
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict,
            HasPartitionKey = true,
            PartitionKey = "https://top.test",
            IsValueQuoted = true,
            Extensions = [extension],
            SequenceNumber = 17
        };

        Assert.That(cookie.Name, Is.EqualTo("session"));
        Assert.That(cookie.Value, Is.EqualTo("abc"));
        Assert.That(cookie.Domain, Is.EqualTo("example.test"));
        Assert.That(cookie.Path, Is.EqualTo("/account"));
        Assert.That(cookie.HasExpires, Is.True);
        Assert.That(cookie.Expires, Is.EqualTo(expires));
        Assert.That(cookie.CreationTime, Is.EqualTo(created));
        Assert.That(cookie.LastAccessTime, Is.EqualTo(accessed));
        Assert.That(cookie.Persistent, Is.True);
        Assert.That(cookie.HostOnly, Is.True);
        Assert.That(cookie.Secure, Is.True);
        Assert.That(cookie.HttpOnly, Is.True);
        Assert.That(cookie.HasSameSite, Is.True);
        Assert.That(cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Strict));
        Assert.That(cookie.HasPartitionKey, Is.True);
        Assert.That(cookie.PartitionKey, Is.EqualTo("https://top.test"));
        Assert.That(cookie.IsValueQuoted, Is.True);
        Assert.That(cookie.Extensions, Is.EqualTo(new[] { extension }));
        Assert.That(cookie.SequenceNumber, Is.EqualTo(17));
    }

    [Test]
    public void StoredCookiePresenceFlagsDistinguishSessionAndUnpartitionedState()
    {
        StoredCookie cookie = new StoredCookie
        {
            HasExpires = false,
            Expires = default,
            Persistent = false,
            HasSameSite = false,
            SameSiteMode = CookieSameSiteMode.Default,
            HasPartitionKey = false,
            PartitionKey = ""
        };

        Assert.That(cookie.HasExpires, Is.False);
        Assert.That(cookie.Persistent, Is.False);
        Assert.That(cookie.HasSameSite, Is.False);
        Assert.That(cookie.HasPartitionKey, Is.False);
    }

    [Test]
    public void SetContextRetainsOriginSiteAndEffectivePolicyFacts()
    {
        Uri origin = new Uri("https://origin.example/path");
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            PrefixPolicy = CookiePrefixPolicy.ErrorReject
        };
        StoredCookie existing = new StoredCookie { Name = "existing" };
        StoredCookie overlap = new StoredCookie { Name = "overlap" };
        CookieSetContext context = new CookieSetContext
        {
            Origin = origin,
            Options = options,
            SameSiteContext = CookieSameSiteContext.CrossSite,
            AccessKind = CookieAccessKind.NonHttp,
            ExistingCookie = existing,
            ExistingCookies = [overlap],
            IsTopLevelNavigation = true,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example",
            HasImportMode = true,
            ImportMode = CookieImportMode.Merge
        };

        Assert.That(context.Origin, Is.SameAs(origin));
        Assert.That(context.Options, Is.SameAs(options));
        Assert.That(context.Options.ComplianceMode, Is.EqualTo(CookieComplianceMode.Rfc6265Bis));
        Assert.That(context.SameSiteContext, Is.EqualTo(CookieSameSiteContext.CrossSite));
        Assert.That(context.AccessKind, Is.EqualTo(CookieAccessKind.NonHttp));
        Assert.That(context.ExistingCookie, Is.SameAs(existing));
        Assert.That(context.ExistingCookies, Is.EqualTo(new[] { overlap }));
        Assert.That(context.IsTopLevelNavigation, Is.True);
        Assert.That(context.HasTopLevelSite, Is.True);
        Assert.That(context.TopLevelSite, Is.EqualTo("https://top.example"));
        Assert.That(context.Options.PrefixPolicy, Is.EqualTo(CookiePrefixPolicy.ErrorReject));
        Assert.That(context.HasImportMode, Is.True);
        Assert.That(context.ImportMode, Is.EqualTo(CookieImportMode.Merge));
    }

    [Test]
    public void RequestContextRetainsAllCallerSuppliedBrowsingFacts()
    {
        Uri requestUri = new Uri("https://request.example/resource");
        CookieRequestContext context = new CookieRequestContext
        {
            RequestUri = requestUri,
            RequestMethod = "POST",
            SameSiteContext = CookieSameSiteContext.CrossSite,
            AccessKind = CookieAccessKind.Http,
            IsTopLevelNavigation = true,
            IsReload = true,
            RedirectChainIsCrossSite = true,
            HasUserActivation = true,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };

        Assert.That(context.RequestUri, Is.SameAs(requestUri));
        Assert.That(context.RequestMethod, Is.EqualTo("POST"));
        Assert.That(context.SameSiteContext, Is.EqualTo(CookieSameSiteContext.CrossSite));
        Assert.That(context.AccessKind, Is.EqualTo(CookieAccessKind.Http));
        Assert.That(context.IsTopLevelNavigation, Is.True);
        Assert.That(context.IsReload, Is.True);
        Assert.That(context.RedirectChainIsCrossSite, Is.True);
        Assert.That(context.HasUserActivation, Is.True);
        Assert.That(context.HasTopLevelSite, Is.True);
        Assert.That(context.TopLevelSite, Is.EqualTo("https://top.example"));
    }

    [Test]
    public void StoreQueryDistinguishesAnyUnpartitionedAndKeyedPartitions()
    {
        CookieStoreQuery any = new CookieStoreQuery();
        CookieStoreQuery unpartitioned = new CookieStoreQuery
        {
            FilterByPartition = true,
            HasPartitionKey = false
        };
        CookieStoreQuery keyed = new CookieStoreQuery
        {
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = ""
        };

        Assert.That(any.FilterByPartition, Is.False);
        Assert.That(unpartitioned.FilterByPartition, Is.True);
        Assert.That(unpartitioned.HasPartitionKey, Is.False);
        Assert.That(keyed.FilterByPartition, Is.True);
        Assert.That(keyed.HasPartitionKey, Is.True);
        Assert.That(keyed.PartitionKey, Is.Empty);
    }

    [Test]
    public void StoreQueryRetainsManagementAndExpiryFilters()
    {
        DateTimeOffset evaluationTime = DateTimeOffset.MinValue;
        CookieIdentity identity = new CookieIdentity { Name = "id", Domain = "example.test", Path = "/" };
        CookieStoreQuery query = new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = identity,
            HasDomain = true,
            Domain = "example.test",
            IncludeSubdomains = true,
            HasPath = true,
            Path = "/account",
            HasName = true,
            Name = "id",
            SessionOnly = true,
            ExpiredOnly = true,
            HasEvaluationTime = true,
            EvaluationTime = evaluationTime
        };

        Assert.That(query.HasIdentity, Is.True);
        Assert.That(query.Identity, Is.SameAs(identity));
        Assert.That(query.HasDomain, Is.True);
        Assert.That(query.Domain, Is.EqualTo("example.test"));
        Assert.That(query.IncludeSubdomains, Is.True);
        Assert.That(query.HasPath, Is.True);
        Assert.That(query.Path, Is.EqualTo("/account"));
        Assert.That(query.HasName, Is.True);
        Assert.That(query.Name, Is.EqualTo("id"));
        Assert.That(query.SessionOnly, Is.True);
        Assert.That(query.ExpiredOnly, Is.True);
        Assert.That(query.HasEvaluationTime, Is.True);
        Assert.That(query.EvaluationTime, Is.EqualTo(DateTimeOffset.MinValue));
    }

    [Test]
    public void RemovalQueryRetainsIndependentRemovalFilters()
    {
        DateTimeOffset evaluationTime = new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero);
        CookieIdentity identity = new CookieIdentity { Name = "id", Domain = "example.test", Path = "/" };
        CookieRemovalQuery query = new CookieRemovalQuery
        {
            HasIdentity = true,
            Identity = identity,
            HasDomain = true,
            Domain = "example.test",
            IncludeSubdomains = true,
            HasPath = true,
            Path = "/",
            HasName = true,
            Name = "id",
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://top.example",
            SessionOnly = true,
            ExpiredOnly = true,
            HasEvaluationTime = true,
            EvaluationTime = evaluationTime
        };

        Assert.That(query.HasIdentity, Is.True);
        Assert.That(query.Identity, Is.SameAs(identity));
        Assert.That(query.HasDomain, Is.True);
        Assert.That(query.Domain, Is.EqualTo("example.test"));
        Assert.That(query.IncludeSubdomains, Is.True);
        Assert.That(query.HasPath, Is.True);
        Assert.That(query.Path, Is.EqualTo("/"));
        Assert.That(query.HasName, Is.True);
        Assert.That(query.Name, Is.EqualTo("id"));
        Assert.That(query.FilterByPartition, Is.True);
        Assert.That(query.HasPartitionKey, Is.True);
        Assert.That(query.PartitionKey, Is.EqualTo("https://top.example"));
        Assert.That(query.SessionOnly, Is.True);
        Assert.That(query.ExpiredOnly, Is.True);
        Assert.That(query.HasEvaluationTime, Is.True);
        Assert.That(query.EvaluationTime, Is.EqualTo(evaluationTime));
    }
}
