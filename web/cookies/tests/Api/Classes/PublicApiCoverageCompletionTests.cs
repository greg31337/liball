namespace cookies.Tests;

public class PublicApiCoverageCompletionTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2042, 10, 11, 12, 13, 14, TimeSpan.Zero);

    [Test]
    public void JarSanitizesMalformedParserAndSerializerDependencyResults()
    {
        CookieJar jar = Jar(new MemoryCookieStore(), new CoverageCompletionClock());
        ICookieJar api = jar;

        jar.Parser = null!;
        AssertSetFailure(
            api.StoreSetCookieHeader("id=1", SetContext()),
            CookieErrorCode.InvalidConfiguration);

        CoverageCompletionParser parser = new CoverageCompletionParser();
        jar.Parser = parser;
        parser.SetResult = null;
        AssertSetFailure(
            api.StoreSetCookieHeader("id=1", SetContext()),
            CookieErrorCode.InvalidConfiguration);

        parser.SetResult = new SetCookieParseResult();
        CookieSetResult malformed = api.StoreSetCookieHeader(
            "id=1", SetContext());
        AssertSetFailure(malformed, CookieErrorCode.MalformedSyntax);
        Assert.That(malformed.ParseResult!.Issues.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));

        jar.Parser = new CookieParser();
        jar.Serializer = null!;
        AssertSelectionFailure(
            api.CreateRequestCookieFields(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        CoverageCompletionSerializer serializer =
            new CoverageCompletionSerializer();
        jar.Serializer = serializer;
        serializer.Result = null;
        AssertSelectionFailure(
            api.CreateRequestCookieFields(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        serializer.Result = new CookieSelectionResult();
        CookieSelectionResult failed = api.CreateRequestCookieFields(
            RequestContext());
        AssertSelectionFailure(failed, CookieErrorCode.InvalidConfiguration);
        Assert.That(failed.FieldValues, Is.Empty);
    }

    [Test]
    public void JarSelectionRejectsEveryMalformedDependencyBoundary()
    {
        MemoryCookieStore store = new MemoryCookieStore();
        store.ApplyStoredCookie(Stored("selected"));
        CookieJar jar = Jar(store, new CoverageCompletionClock());
        ICookieJar api = jar;

        AssertSelectionFailure(
            api.SelectRequestCookies(null!),
            CookieErrorCode.InvalidHost);
        AssertSelectionFailure(
            api.SelectRequestCookies(new CookieRequestContext()),
            CookieErrorCode.InvalidHost);

        jar.Policy = null!;
        AssertSelectionFailure(
            api.SelectRequestCookies(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        CoverageCompletionClock failingClock = new CoverageCompletionClock();
        failingClock.Throw = true;
        jar = Jar(store, failingClock);
        api = jar;
        AssertSelectionFailure(
            api.SelectRequestCookies(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        jar = Jar(store, new CoverageCompletionClock());
        api = jar;
        CookieRequestContext relative = RequestContext();
        relative.RequestUri = new Uri("/relative", UriKind.Relative);
        AssertSelectionFailure(
            api.SelectRequestCookies(relative),
            CookieErrorCode.InvalidHost);

        CoverageCompletionPolicy policy = new CoverageCompletionPolicy();
        jar.Policy = policy;
        policy.MatchResult = null;
        AssertSelectionFailure(
            api.SelectRequestCookies(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        policy.MatchResult = new CookieMatchResult
        {
            Status = CookieMatchStatus.Included,
            Cookie = Stored("selected")
        };
        AssertSelectionFailure(
            api.SelectRequestCookies(RequestContext()),
            CookieErrorCode.InvalidConfiguration);

        jar.Policy = null!;
        AssertSelectionFailure(
            api.CreateRequestCookieFields(RequestContext()),
            CookieErrorCode.InvalidConfiguration);
    }

    [Test]
    public void JarManagementMethodsSurfaceDependencyContractFailures()
    {
        CookieJar missingStore = Jar(
            new MemoryCookieStore(), new CoverageCompletionClock());
        ICookieJar missingStoreApi = missingStore;
        missingStore.Store = null!;

        Assert.Throws<CookieStoreException>(() =>
            missingStoreApi.QueryJarCookies(new CookieStoreQuery()));
        Assert.Throws<CookieStoreException>(() =>
            missingStoreApi.RemoveJarCookies(new CookieRemovalQuery()));
        Assert.Throws<CookieStoreException>(() =>
            missingStoreApi.PurgeExpiredCookies());
        Assert.Throws<CookieStoreException>(() =>
            missingStoreApi.ExportJarSnapshot());

        CookieJar valid = Jar(
            new MemoryCookieStore(), new CoverageCompletionClock());
        ICookieJar validApi = valid;
        Assert.Throws<CookieStoreException>(() =>
            validApi.QueryJarCookies(null!));
        Assert.Throws<CookieStoreException>(() =>
            validApi.RemoveJarCookies(null!));

        CoverageCompletionStore malformedStore = new CoverageCompletionStore();
        CookieJar malformed = Jar(
            malformedStore, new CoverageCompletionClock());
        ICookieJar malformedApi = malformed;
        malformedStore.ReturnNullQuery = true;
        Assert.Throws<CookieStoreException>(() =>
            malformedApi.QueryJarCookies(new CookieStoreQuery()));
        malformedStore.ReturnNullQuery = false;
        malformedStore.ReturnNegativeRemoval = true;
        Assert.Throws<CookieStoreException>(() =>
            malformedApi.RemoveJarCookies(new CookieRemovalQuery()));

        CoverageCompletionClock failingClock = new CoverageCompletionClock();
        failingClock.Throw = true;
        ICookieJar failingClockApi = Jar(
            new MemoryCookieStore(), failingClock);
        Assert.Throws<CookieStoreException>(() =>
            failingClockApi.PurgeExpiredCookies());
    }

    [Test]
    public void JarModelStorageRollsBackEveryInvalidPolicyOrEvictionOutcome()
    {
        MemoryCookieStore store = new MemoryCookieStore();
        CookieJar jar = Jar(store, new CoverageCompletionClock());
        ICookieJar api = jar;

        jar.Policy = null!;
        AssertSetFailure(
            api.StoreSetCookieModel(Model("missing-policy"), SetContext()),
            CookieErrorCode.InvalidConfiguration);

        CoverageCompletionClock failingClock = new CoverageCompletionClock();
        failingClock.Throw = true;
        jar = Jar(store, failingClock);
        api = jar;
        AssertSetFailure(
            api.StoreSetCookieModel(Model("clock"), SetContext()),
            CookieErrorCode.InvalidConfiguration);

        jar = Jar(store, new CoverageCompletionClock());
        api = jar;
        CoverageCompletionPolicy policy = new CoverageCompletionPolicy();
        jar.Policy = policy;
        policy.SetDecision = null;
        AssertSetFailure(
            api.StoreSetCookieModel(Model("null-decision"), SetContext()),
            CookieErrorCode.InvalidConfiguration);

        policy.SetDecision = new CookieSetDecision
        {
            Status = CookieSetStatus.Accepted
        };
        AssertSetFailure(
            api.StoreSetCookieModel(Model("null-cookie"), SetContext()),
            CookieErrorCode.InvalidConfiguration);

        CookieJarOptions zeroOptions = Options();
        zeroOptions.Limits.MaximumCookies = 0;
        zeroOptions.Limits.MaximumCookiesPerDomain = 0;
        CookieJar zeroJar = Jar(
            zeroOptions,
            new MemoryCookieStore(),
            new CoverageCompletionClock());
        CookieSetResult evicted = zeroJar.StoreSetCookieModel(
            Model("evicted"), SetContext());
        Assert.That(evicted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(evicted.Cookie, Is.Null);
        Assert.That(evicted.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));

        CookieJar invalidLimits = Jar(
            new MemoryCookieStore(), new CoverageCompletionClock());
        invalidLimits.Options.Limits.MaximumCookies = -1;
        CookieSetResult rolledBack = invalidLimits.StoreSetCookieModel(
            Model("rollback"), SetContext());
        AssertSetFailure(rolledBack, CookieErrorCode.InvalidConfiguration);
        Assert.That(invalidLimits.ExportJarSnapshot().Cookies, Is.Empty);
    }

    [Test]
    public void JarImportSanitizesClockPolicyUriAndEvictionFailures()
    {
        CookieJarSnapshot snapshot = Snapshot(Stored("imported"));
        CoverageCompletionClock failingClock = new CoverageCompletionClock();
        failingClock.Throw = true;
        ICookieJar failingClockApi = Jar(
            new MemoryCookieStore(), failingClock);
        CookieImportResult clock = failingClockApi.ImportJarSnapshot(
            snapshot, CookieImportMode.Replace);
        AssertImportFailure(clock, CookieErrorCode.InvalidConfiguration);

        CookieJar jar = Jar(
            new MemoryCookieStore(), new CoverageCompletionClock());
        CoverageCompletionPolicy policy = new CoverageCompletionPolicy();
        jar.Policy = policy;
        policy.MatchResult = null;
        CookieImportResult missing = jar.ImportJarSnapshot(
            snapshot, CookieImportMode.Replace);
        Assert.That(missing.Success, Is.True);
        Assert.That(missing.Results.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));

        policy.MatchResult = new CookieMatchResult
        {
            Status = CookieMatchStatus.Excluded,
            ErrorCode = CookieErrorCode.None
        };
        CookieImportResult policyFailure = jar.ImportJarSnapshot(
            snapshot, CookieImportMode.Replace);
        Assert.That(policyFailure.Results.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.PolicyRejection));

        StoredCookie ipv6 = Stored("ipv6");
        ipv6.Domain = "::1";
        policy.MatchResult = Included(ipv6);
        CookieImportResult acceptedIpv6 = jar.ImportJarSnapshot(
            Snapshot(ipv6), CookieImportMode.Replace);
        Assert.That(acceptedIpv6.Results.Single().Status,
            Is.EqualTo(CookieSetStatus.Accepted));

        CookieJar invalidLimits = Jar(
            new MemoryCookieStore(), new CoverageCompletionClock());
        invalidLimits.Options.Limits.MaximumCookies = -1;
        CookieImportResult eviction = invalidLimits.ImportJarSnapshot(
            snapshot, CookieImportMode.Replace);
        AssertImportFailure(eviction, CookieErrorCode.InvalidConfiguration);
    }

    [Test]
    public void MatcherAndPolicyPublicFailuresReturnStableCodesAndExceptions()
    {
        CookieMatcher matcher = Matcher(new CoverageCompletionClock());
        ICookieMatcher matcherApi = matcher;
        Assert.Throws<CookiePolicyException>(() =>
            matcherApi.IsCookieExpired(null!, EvaluationTime));

        matcher.Options = null!;
        Assert.Throws<CookiePolicyException>(() =>
            matcherApi.ComputeSchemefulSite(new Uri("https://example.test/")));
        Assert.That(
            matcherApi.EvaluateCookieMatch(Stored("match"), RequestContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));

        matcher = Matcher(new CoverageCompletionClock());
        matcher.Options.ComplianceMode = (CookieComplianceMode)99;
        Assert.That(
            matcher.EvaluateCookieMatch(Stored("match"), RequestContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));
        matcher.Options.ComplianceMode = CookieComplianceMode.Rfc6265;
        matcher.Options.DefaultSameSiteMode = (CookieSameSiteMode)99;
        Assert.That(
            matcher.EvaluateCookieMatch(Stored("match"), RequestContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));

        matcher = Matcher(new CoverageCompletionClock());
        CookieRequestContext relative = RequestContext();
        relative.RequestUri = new Uri("/relative", UriKind.Relative);
        Assert.That(
            matcher.EvaluateCookieMatch(Stored("match"), relative).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidHost));

        CoverageCompletionClock failingClock = new CoverageCompletionClock();
        failingClock.Throw = true;
        matcher = Matcher(failingClock);
        Assert.That(
            matcher.EvaluateCookieMatch(Stored("match"), RequestContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));

        DefaultCookiePolicy policy = Policy(new CoverageCompletionClock());
        ICookiePolicy policyApi = policy;
        Assert.That(
            policyApi.EvaluateCookieAcceptance(null!, SetContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
        Assert.That(
            policyApi.EvaluateCookieAcceptance(Model("id"), null!).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
        CookieSetContext nullExisting = SetContext();
        nullExisting.ExistingCookies = null!;
        Assert.That(
            policyApi.EvaluateCookieAcceptance(Model("id"), nullExisting).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
        CookieSetContext nullOrigin = SetContext();
        nullOrigin.Origin = null;
        Assert.That(
            policyApi.EvaluateCookieAcceptance(Model("id"), nullOrigin).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidHost));

        policy = Policy(failingClock);
        Assert.That(
            policy.EvaluateCookieAcceptance(Model("id"), SetContext()).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));
    }

    [Test]
    public void DefaultPolicyOperationsAreUsableThroughThePublicInterface()
    {
        ICookiePolicy policy = Policy(new CoverageCompletionClock());

        CookieSetDecision accepted = policy.EvaluateCookieAcceptance(
            Model("policy"), SetContext());
        CookieMatchResult returned = policy.EvaluateCookieReturn(
            accepted.NormalizedCookie!, RequestContext());

        Assert.That(accepted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(accepted.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(returned.Status, Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(returned.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(returned.Pair!.Name, Is.EqualTo("policy"));
    }

    [Test]
    public void SecureOriginPolicyCoversLoopbackAndNonLoopbackHostFamilies()
    {
        ISecureOriginPolicy policy = new DefaultSecureOriginPolicy(true);

        Assert.That(policy.IsSecureOrigin(null!), Is.False);
        Assert.That(policy.IsSecureOrigin(
            new Uri("/relative", UriKind.Relative)), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("ftp://localhost/")), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("http://192.0.2.1/")), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("http://[::2]/")), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("http://x.localhost./")), Is.True);
        Assert.That(policy.IsSecureOrigin(new Uri("http://notlocalhost/")), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("http://x.localhosx/")), Is.False);
        Assert.That(policy.IsSecureOrigin(new Uri("https://[::1]/")), Is.True);
    }

    [Test]
    public void MemoryStoreRejectsInvalidIdentityAndSnapshotStateAtomically()
    {
        ICookieStore store = new MemoryCookieStore();
        Assert.Throws<CookieStoreException>(() =>
            store.UpdateStoredCookieAccess(null!, EvaluationTime));
        Assert.Throws<CookieStoreException>(() =>
            store.UpdateStoredCookieAccess(
                [new CookieIdentity()], EvaluationTime));
        CookieIdentity duplicate = Identity("duplicate");
        Assert.Throws<CookieStoreException>(() =>
            store.UpdateStoredCookieAccess(
                [duplicate, duplicate], EvaluationTime));

        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(null!));
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Version = CookieDefaults.SnapshotVersion + 1
            }));
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                NextSequenceNumber = -1
            }));

        StoredCookie negative = Stored("negative");
        negative.SequenceNumber = -1;
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Cookies = [negative],
                NextSequenceNumber = 1
            }));

        StoredCookie outOfRange = Stored("range");
        outOfRange.SequenceNumber = 1;
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Cookies = [outOfRange],
                NextSequenceNumber = 1
            }));

        StoredCookie first = Stored("same");
        StoredCookie second = Stored("same");
        second.SequenceNumber = 1;
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Cookies = [first, second],
                NextSequenceNumber = 2
            }));

        second = Stored("other");
        Assert.Throws<CookieStoreException>(() =>
            store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Cookies = [first, second],
                NextSequenceNumber = 2
            }));

        Assert.That(store.CaptureStoreSnapshot().Cookies, Is.Empty);
    }

    static CookieJar Jar(
        ICookieStore store,
        CoverageCompletionClock clock)
    {
        return Jar(Options(), store, clock);
    }

    static CookieJar Jar(
        CookieJarOptions options,
        ICookieStore store,
        CoverageCompletionClock clock)
    {
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            null,
            new DefaultSecureOriginPolicy(false),
            clock);
        return new CookieJar(
            options,
            new CookieParser(),
            policy,
            store,
            new CookieSerializer(),
            clock);
    }

    static CookieMatcher Matcher(CoverageCompletionClock clock)
    {
        return new CookieMatcher(
            Options(),
            null,
            new DefaultSecureOriginPolicy(false),
            clock);
    }

    static DefaultCookiePolicy Policy(CoverageCompletionClock clock)
    {
        return new DefaultCookiePolicy(
            Options(),
            null,
            new DefaultSecureOriginPolicy(false),
            clock);
    }

    static CookieJarOptions Options()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            RequirePublicSuffixList = false
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode =
            CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static SetCookie Model(string name)
    {
        return new SetCookie
        {
            Name = name,
            Value = "value"
        };
    }

    static StoredCookie Stored(string name)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "example.test",
            Path = "/",
            CreationTime = EvaluationTime,
            LastAccessTime = EvaluationTime,
            HostOnly = true
        };
    }

    static CookieIdentity Identity(string name)
    {
        return new CookieIdentity
        {
            Name = name,
            Domain = "example.test",
            Path = "/"
        };
    }

    static CookieSetContext SetContext()
    {
        return new CookieSetContext
        {
            Origin = new Uri("https://example.test/path"),
            Options = Options(),
            ExistingCookies = [],
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://example.test"
        };
    }

    static CookieRequestContext RequestContext()
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri("https://example.test/path"),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://example.test"
        };
    }

    static CookieJarSnapshot Snapshot(StoredCookie cookie)
    {
        cookie.SequenceNumber = 0;
        return new CookieJarSnapshot
        {
            Cookies = [cookie],
            NextSequenceNumber = 1
        };
    }

    static CookieMatchResult Included(StoredCookie cookie)
    {
        return new CookieMatchResult
        {
            Status = CookieMatchStatus.Included,
            ErrorCode = CookieErrorCode.None,
            Cookie = cookie,
            Pair = new CookiePair
            {
                Name = cookie.Name,
                Value = cookie.Value
            }
        };
    }

    static void AssertSetFailure(
        CookieSetResult result,
        CookieErrorCode errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(errorCode));
    }

    static void AssertSelectionFailure(
        CookieSelectionResult result,
        CookieErrorCode errorCode)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(errorCode));
    }

    static void AssertImportFailure(
        CookieImportResult result,
        CookieErrorCode errorCode)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(errorCode));
    }
}

public class CoverageCompletionClock : ICookieClock
{
    public bool Throw;

    public DateTimeOffset GetUtcNow()
    {
        if (Throw)
        {
            throw new InvalidOperationException("clock failure");
        }

        return new DateTimeOffset(2042, 10, 11, 12, 13, 14, TimeSpan.Zero);
    }
}

public class CoverageCompletionParser : ICookieParser
{
    public SetCookieParseResult? SetResult;

    public SetCookie ParseSetCookie(
        string fieldValue,
        CookieParseOptions options)
    {
        return new SetCookie();
    }

    public SetCookieParseResult TryParseSetCookie(
        string fieldValue,
        CookieParseOptions options)
    {
        return SetResult!;
    }

    public CookiePair[] ParseCookieFields(
        string[] fieldValues,
        CookieParseOptions options)
    {
        return [];
    }

    public CookieHeaderParseResult TryParseCookieFields(
        string[] fieldValues,
        CookieParseOptions options)
    {
        return new CookieHeaderParseResult { Success = true };
    }
}

public class CoverageCompletionPolicy : ICookiePolicy
{
    public CookieSetDecision? SetDecision;
    public CookieMatchResult? MatchResult;

    public CookieSetDecision EvaluateCookieAcceptance(
        SetCookie cookie,
        CookieSetContext context)
    {
        return SetDecision!;
    }

    public CookieMatchResult EvaluateCookieReturn(
        StoredCookie cookie,
        CookieRequestContext context)
    {
        return MatchResult!;
    }
}

public class CoverageCompletionSerializer : ICookieSerializer
{
    public CookieSelectionResult? Result;

    public string SerializeSetCookie(
        SetCookie cookie,
        CookieSerializationOptions options)
    {
        return "";
    }

    public string SerializeCookiePairs(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        return "";
    }

    public CookieSelectionResult SerializeCookieFields(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        return Result!;
    }
}

public class CoverageCompletionStore : ICookieStore
{
    public MemoryCookieStore Inner = new MemoryCookieStore();
    public bool ReturnNullQuery;
    public bool ReturnNegativeRemoval;

    public StoredCookie? FindStoredCookie(CookieIdentity identity)
    {
        return Inner.FindStoredCookie(identity);
    }

    public StoredCookie[] QueryStoredCookies(CookieStoreQuery query)
    {
        if (ReturnNullQuery)
        {
            return null!;
        }

        return Inner.QueryStoredCookies(query);
    }

    public CookieSetResult ApplyStoredCookie(StoredCookie cookie)
    {
        return Inner.ApplyStoredCookie(cookie);
    }

    public StoredCookie[] UpdateStoredCookieAccess(
        CookieIdentity[] identities,
        DateTimeOffset accessTime)
    {
        return Inner.UpdateStoredCookieAccess(identities, accessTime);
    }

    public int RemoveStoredCookies(CookieRemovalQuery query)
    {
        if (ReturnNegativeRemoval)
        {
            return -1;
        }

        return Inner.RemoveStoredCookies(query);
    }

    public CookieJarSnapshot CaptureStoreSnapshot()
    {
        return Inner.CaptureStoreSnapshot();
    }

    public void ReplaceStoreSnapshot(CookieJarSnapshot snapshot)
    {
        Inner.ReplaceStoreSnapshot(snapshot);
    }
}
