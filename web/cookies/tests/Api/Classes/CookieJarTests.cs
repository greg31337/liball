namespace cookies.Tests;

public class CookieJarTests
{
    static readonly DateTimeOffset ReceiptTime =
        new DateTimeOffset(2032, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Test]
    public void RawHeaderParsesNormalizesStoresAndOwnsEveryBoundary()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        CookieSetContext context = Context();
        CookieJarOptions callerContextOptions = context.Options;
        options.Limits.MaximumCookies = 0;

        CookieSetResult result = jar.StoreSetCookieHeader(
            "id=one; Path=/account; HttpOnly; Priority=High",
            context);

        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.ParseResult, Is.Not.Null);
        Assert.That(result.ParseResult!.Success, Is.True);
        Assert.That(result.ParseResult.Cookie!.Name, Is.EqualTo("id"));
        Assert.That(result.Cookie, Is.Not.Null);
        Assert.That(result.Cookie!.Domain, Is.EqualTo("shop.example.test"));
        Assert.That(result.Cookie.Path, Is.EqualTo("/account"));
        Assert.That(result.Cookie.HostOnly, Is.True);
        Assert.That(result.Cookie.HttpOnly, Is.True);
        Assert.That(result.Cookie.CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(result.Cookie.SequenceNumber, Is.Zero);
        Assert.That(result.Cookie.Extensions[0].Name, Is.EqualTo("Priority"));
        Assert.That(context.Options, Is.SameAs(callerContextOptions));
        Assert.That(context.ExistingCookies, Is.Empty);

        result.Cookie.Value = "changed-result";
        result.Cookie.Extensions[0].Value = "changed-result";
        result.ParseResult.Cookie.Value = "changed-parse";
        StoredCookie stored = store.QueryStoredCookies(new CookieStoreQuery()).Single();
        Assert.That(stored.Value, Is.EqualTo("one"));
        Assert.That(stored.Extensions[0].Value, Is.EqualTo("High"));
    }

    [Test]
    public void BatchProcessesMalformedFieldsIndependentlyAndPreservesOrder()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);

        CookieBatchSetResult batch = jar.StoreSetCookieHeaders(
            ["first=1", "bad\r\nvalue", "second=2"],
            Context());

        Assert.That(batch.Results.Select(result => result.Status),
            Is.EqualTo(new[]
            {
                CookieSetStatus.Accepted,
                CookieSetStatus.Rejected,
                CookieSetStatus.Accepted
            }));
        Assert.That(batch.Results[1].ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(batch.Results[1].Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.HeaderInjection), Is.EqualTo(1));
        Assert.That(batch.Results.All(result => result.ParseResult != null), Is.True);
        Assert.That(batch.AcceptedCount, Is.EqualTo(2));
        Assert.That(batch.ReplacedCount, Is.Zero);
        Assert.That(batch.DeletedCount, Is.Zero);
        Assert.That(batch.IgnoredCount, Is.Zero);
        Assert.That(batch.RejectedCount, Is.EqualTo(1));
        Assert.That(batch.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.HeaderInjection));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void ModelPathDiscoversExactHttpOnlyReplacementFromStoreState()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        SetCookie original = new SetCookie
        {
            Name = "id",
            Value = "secret",
            HttpOnly = true
        };
        CookieSetResult accepted = jar.StoreSetCookieModel(original, Context());
        CookieSetContext nonHttp = Context();
        nonHttp.AccessKind = CookieAccessKind.NonHttp;
        SetCookie replacement = new SetCookie { Name = "id", Value = "exposed" };

        CookieSetResult rejected = jar.StoreSetCookieModel(replacement, nonHttp);

        Assert.That(accepted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(accepted.ParseResult, Is.Null);
        Assert.That(rejected.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(rejected.ErrorCode,
            Is.EqualTo(CookieErrorCode.HttpOnlyApiViolation));
        Assert.That(rejected.ParseResult, Is.Null);
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()).Single().Value,
            Is.EqualTo("secret"));
    }

    [Test]
    public void QuotaEvictionRunsAfterEachIndependentBatchMutation()
    {
        CookieJarOptions options = Options();
        options.Limits.MaximumCookiesPerDomain = 1;
        options.Limits.MaximumCookies = 1;
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);

        CookieBatchSetResult batch = jar.StoreSetCookieHeaders(
            ["first=1", "second=2"],
            Context());

        Assert.That(batch.AcceptedCount, Is.EqualTo(2));
        Assert.That(batch.RejectedCount, Is.Zero);
        Assert.That(batch.Results[0].Issues, Is.Empty);
        Assert.That(batch.Results[1].Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
        Assert.That(batch.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "second" }));
    }

    [Test]
    public void IngestionPurgesExpiredStateBeforeReplacementSecurityChecks()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        StoredCookie expired = new StoredCookie
        {
            Name = "id",
            Value = "expired",
            Domain = "shop.example.test",
            Path = "/account",
            HasExpires = true,
            Expires = ReceiptTime.AddSeconds(-1),
            CreationTime = ReceiptTime.AddDays(-1),
            LastAccessTime = ReceiptTime.AddDays(-1),
            HttpOnly = true,
            SequenceNumber = 0
        };
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = [expired],
            NextSequenceNumber = 1
        });
        ICookieJar jar = Jar(options, store);
        CookieSetContext nonHttp = Context();
        nonHttp.AccessKind = CookieAccessKind.NonHttp;

        CookieSetResult result = jar.StoreSetCookieModel(
            new SetCookie { Name = "id", Value = "fresh" },
            nonHttp);

        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookie!.Value, Is.EqualTo("fresh"));
        Assert.That(result.Cookie.CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(result.Cookie.SequenceNumber, Is.EqualTo(1));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()).Single().Value,
            Is.EqualTo("fresh"));
    }

    [Test]
    public void InvalidModelAndNullBatchRejectWithoutMutatingTheStore()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);

        CookieSetResult invalidModel = jar.StoreSetCookieModel(null!, Context());
        CookieBatchSetResult invalidBatch = jar.StoreSetCookieHeaders(null!, Context());

        Assert.That(invalidModel.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(invalidModel.ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
        Assert.That(invalidBatch.Results, Has.Length.EqualTo(1));
        Assert.That(invalidBatch.RejectedCount, Is.EqualTo(1));
        Assert.That(invalidBatch.Results[0].ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void SelectionUsesIndexedCandidatesOrdersByRfcAndReturnsOwnedUpdatedState()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        JarClock clock = new JarClock(ReceiptTime);
        ICookieJar jar = JarWithServices(
            options,
            store,
            clock,
            new JarPublicSuffixList());
        CookieBatchSetResult stored = jar.StoreSetCookieHeaders(
            [
                "root=1; Domain=example.test; Path=/",
                "account=2; Path=/account",
                "deep=3; Path=/account/deep",
                "wrong=4; Path=/else"
            ],
            Context());
        StoredCookie unrelated = new StoredCookie
        {
            Name = "unrelated",
            Value = "5",
            Domain = "unrelated.test",
            Path = "/",
            HostOnly = true,
            CreationTime = ReceiptTime,
            LastAccessTime = ReceiptTime
        };
        store.ApplyStoredCookie(unrelated);
        clock.Value = ReceiptTime.AddMinutes(15);
        CookieRequestContext request = RequestContext();

        CookieSelectionResult result = jar.SelectRequestCookies(request);

        Assert.That(stored.AcceptedCount, Is.EqualTo(4));
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "deep", "account", "root" }));
        Assert.That(result.Pairs.Select(pair => pair.Name),
            Is.EqualTo(new[] { "deep", "account", "root" }));
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Matches.Select(match => match.Cookie!.Name),
            Is.EqualTo(new[] { "root", "account", "deep", "wrong" }));
        Assert.That(result.Matches.Select(match => match.Status),
            Is.EqualTo(new[]
            {
                CookieMatchStatus.Included,
                CookieMatchStatus.Included,
                CookieMatchStatus.Included,
                CookieMatchStatus.Excluded
            }));
        Assert.That(result.Matches[3].ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidPath));
        Assert.That(result.Cookies.Select(cookie => cookie.LastAccessTime),
            Is.All.EqualTo(clock.Value));
        Assert.That(store.FindStoredCookie(new CookieIdentity
        {
            Name = "unrelated",
            Domain = "unrelated.test",
            Path = "/"
        })!.LastAccessTime, Is.EqualTo(ReceiptTime));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Single(cookie => cookie.Name == "wrong").LastAccessTime,
            Is.EqualTo(ReceiptTime));

        result.Cookies[0].Value = "changed-cookie";
        result.Pairs[0].Value = "changed-pair";
        result.Matches[0].Cookie!.Value = "changed-match";
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Single(cookie => cookie.Name == "deep").Value,
            Is.EqualTo("3"));
    }

    [Test]
    public void SelectionBreaksEqualPathTiesByCreationThenStableSequence()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        JarClock clock = new JarClock(ReceiptTime.AddHours(2));
        ICookieJar jar = JarWithServices(
            options,
            store,
            clock,
            new JarPublicSuffixList());
        StoredCookie later = SelectionCookie("later", ReceiptTime.AddHours(1));
        StoredCookie earlierFirst = SelectionCookie("earlier-first", ReceiptTime);
        StoredCookie earlierSecond = SelectionCookie("earlier-second", ReceiptTime);
        store.ApplyStoredCookie(later);
        store.ApplyStoredCookie(earlierFirst);
        store.ApplyStoredCookie(earlierSecond);

        CookieSelectionResult result = jar.SelectRequestCookies(RequestContext());

        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "earlier-first", "earlier-second", "later" }));
    }

    [Test]
    public void RequestFieldsPreserveSelectionOrderAndSplitOnlyBetweenPairs()
    {
        CookieJarOptions options = Options();
        options.Limits.MaximumRequestHeaderOctets = 17;
        options.SerializationOptions.Limits.MaximumRequestHeaderOctets = 17;
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        CookieBatchSetResult stored = jar.StoreSetCookieHeaders(
            [
                "root=1; Path=/",
                "deep=2; Path=/account/deep",
                "account=3; Path=/account"
            ],
            Context());

        CookieSelectionResult result = jar.CreateRequestCookieFields(
            RequestContext());

        Assert.That(stored.AcceptedCount, Is.EqualTo(3));
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "deep", "account", "root" }));
        Assert.That(result.Pairs.Select(pair => pair.Name),
            Is.EqualTo(new[] { "deep", "account", "root" }));
        Assert.That(result.FieldValues,
            Is.EqualTo(new[] { "deep=2; account=3", "root=1" }));
        Assert.That(result.Matches.Select(match => match.Cookie!.Name),
            Is.EqualTo(new[] { "root", "deep", "account" }));
        Assert.That(result.Issues, Is.Empty);

        result.Cookies[0].Value = "changed-cookie";
        result.Pairs[0].Value = "changed-pair";
        result.FieldValues[0] = "changed-field";
        CookieSelectionResult repeated = jar.CreateRequestCookieFields(
            RequestContext());
        Assert.That(repeated.FieldValues,
            Is.EqualTo(new[] { "deep=2; account=3", "root=1" }));
    }

    [Test]
    public void RequestFieldsReturnNoPartialOutputWhenOnePairExceedsTheLimit()
    {
        CookieJarOptions options = Options();
        options.Limits.MaximumRequestHeaderOctets = 10;
        options.SerializationOptions.Limits.MaximumRequestHeaderOctets = 10;
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        Assert.That(jar.StoreSetCookieHeader(
            "oversized=value; Path=/",
            Context()).Status, Is.EqualTo(CookieSetStatus.Accepted));

        CookieSelectionResult result = jar.CreateRequestCookieFields(
            RequestContext());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "oversized" }));
        Assert.That(result.Pairs.Select(pair => pair.Name),
            Is.EqualTo(new[] { "oversized" }));
        Assert.That(result.Matches.Single().Status,
            Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[] { CookieErrorCode.HeaderLimitExceeded }));
    }

    [Test]
    public void SelectionUsesOnlyTheMatchingPartitionAndRevalidatesTheCurrentPsl()
    {
        CookieJarOptions options = Options();
        options.EnablePartitionedCookies = true;
        MemoryCookieStore store = new MemoryCookieStore();
        JarClock clock = new JarClock(ReceiptTime);
        JarMutablePublicSuffixList suffixes = new JarMutablePublicSuffixList();
        ICookieJar jar = JarWithServices(options, store, clock, suffixes);
        CookieSetContext firstPartition = Context();
        firstPartition.TopLevelSite = "https://first.example";
        CookieSetContext secondPartition = Context();
        secondPartition.TopLevelSite = "https://second.example";
        Assert.That(jar.StoreSetCookieHeader(
            "plain=1; Domain=example.test; Path=/",
            Context()).Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(jar.StoreSetCookieHeader(
            "partitioned=2; Secure; Partitioned; Path=/",
            firstPartition).Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(jar.StoreSetCookieHeader(
            "other=3; Secure; Partitioned; Path=/",
            secondPartition).Status, Is.EqualTo(CookieSetStatus.Accepted));
        CookieRequestContext request = RequestContext();
        request.TopLevelSite = firstPartition.TopLevelSite;

        CookieSelectionResult partitionSelection = jar.SelectRequestCookies(request);

        Assert.That(partitionSelection.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "plain", "partitioned" }));
        Assert.That(partitionSelection.Matches.Select(match => match.Cookie!.Name),
            Is.EqualTo(new[] { "plain", "partitioned" }));

        suffixes.ExampleIsPublicSuffix = true;
        CookieSelectionResult pslSelection = jar.SelectRequestCookies(request);

        Assert.That(pslSelection.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "partitioned" }));
        Assert.That(pslSelection.Matches.Single(match =>
            match.Cookie!.Name == "plain").ErrorCode,
            Is.EqualTo(CookieErrorCode.PublicSuffix));
    }

    [Test]
    public void ManagementQueriesAndRemovalsCoverEverySupportedFilter()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        StoredCookie root = ManagementCookie(
            "root",
            "example.test",
            "/",
            false,
            "",
            0);
        StoredCookie path = ManagementCookie(
            "path",
            "shop.example.test",
            "/account",
            false,
            "",
            1);
        StoredCookie named = ManagementCookie(
            "by-name",
            "other.test",
            "/",
            false,
            "",
            2);
        StoredCookie firstPartition = ManagementCookie(
            "first-partition",
            "shop.example.test",
            "/partition",
            true,
            "https://first.example",
            3);
        StoredCookie secondPartition = ManagementCookie(
            "second-partition",
            "shop.example.test",
            "/partition",
            true,
            "https://second.example",
            4);
        StoredCookie subtree = ManagementCookie(
            "subtree",
            "deep.shop.example.test",
            "/",
            false,
            "",
            5);
        StoredCookie outside = ManagementCookie(
            "outside",
            "outside.test",
            "/",
            false,
            "",
            6);
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies =
            [
                root,
                path,
                named,
                firstPartition,
                secondPartition,
                subtree,
                outside
            ],
            NextSequenceNumber = 7
        });
        CookieIdentity rootIdentity = new CookieIdentity
        {
            Name = root.Name,
            Domain = root.Domain,
            Path = root.Path
        };

        StoredCookie[] exact = jar.QueryJarCookies(new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = rootIdentity
        });
        StoredCookie[] exactDomain = jar.QueryJarCookies(new CookieStoreQuery
        {
            HasDomain = true,
            Domain = "example.test"
        });
        StoredCookie[] domainTree = jar.QueryJarCookies(new CookieStoreQuery
        {
            HasDomain = true,
            Domain = "example.test",
            IncludeSubdomains = true
        });
        StoredCookie[] pathMatches = jar.QueryJarCookies(new CookieStoreQuery
        {
            HasPath = true,
            Path = "/account"
        });
        StoredCookie[] nameMatches = jar.QueryJarCookies(new CookieStoreQuery
        {
            HasName = true,
            Name = "by-name"
        });
        StoredCookie[] partitionMatches = jar.QueryJarCookies(new CookieStoreQuery
        {
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://first.example"
        });

        Assert.That(exact.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "root" }));
        Assert.That(exactDomain.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "root" }));
        Assert.That(domainTree.Select(cookie => cookie.Name),
            Is.EqualTo(new[]
            {
                "root",
                "path",
                "first-partition",
                "second-partition",
                "subtree"
            }));
        Assert.That(pathMatches.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "path" }));
        Assert.That(nameMatches.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "by-name" }));
        Assert.That(partitionMatches.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "first-partition" }));
        exact[0].Value = "changed-result";
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = rootIdentity
        }).Single().Value, Is.EqualTo("root-value"));

        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasIdentity = true,
            Identity = rootIdentity
        }), Is.EqualTo(1));
        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasPath = true,
            Path = "/account"
        }), Is.EqualTo(1));
        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasName = true,
            Name = "by-name"
        }), Is.EqualTo(1));
        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://first.example"
        }), Is.EqualTo(1));
        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasDomain = true,
            Domain = "outside.test"
        }), Is.EqualTo(1));
        Assert.That(jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasDomain = true,
            Domain = "example.test",
            IncludeSubdomains = true
        }), Is.EqualTo(2));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void ExpiredAndSessionPurgesRemoveOnlyTheirTargetState()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store);
        StoredCookie expired = ManagementCookie(
            "expired",
            "example.test",
            "/",
            false,
            "",
            0);
        expired.Persistent = true;
        expired.HasExpires = true;
        expired.Expires = ReceiptTime.AddMinutes(-1);
        StoredCookie persistent = ManagementCookie(
            "persistent",
            "example.test",
            "/",
            false,
            "",
            1);
        persistent.Persistent = true;
        persistent.HasExpires = true;
        persistent.Expires = ReceiptTime.AddDays(1);
        StoredCookie session = ManagementCookie(
            "session",
            "example.test",
            "/",
            false,
            "",
            2);
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = [expired, persistent, session],
            NextSequenceNumber = 3
        });

        StoredCookie[] expiredQuery = jar.QueryJarCookies(new CookieStoreQuery
        {
            ExpiredOnly = true,
            HasEvaluationTime = true,
            EvaluationTime = ReceiptTime
        });
        StoredCookie[] sessionQuery = jar.QueryJarCookies(new CookieStoreQuery
        {
            SessionOnly = true
        });
        int expiredCount = jar.PurgeExpiredCookies();
        int sessionCount = jar.RemoveJarCookies(new CookieRemovalQuery
        {
            SessionOnly = true
        });

        Assert.That(expiredQuery.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "expired" }));
        Assert.That(sessionQuery.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "session" }));
        Assert.That(expiredCount, Is.EqualTo(1));
        Assert.That(sessionCount, Is.EqualTo(1));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "persistent" }));
    }

    [Test]
    public void InjectedClockControlsRawLifetimeSelectionAccessAndExpiryPurge()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        JarClock clock = new JarClock(ReceiptTime);
        ICookieJar jar = JarWithServices(
            options,
            store,
            clock,
            new JarPublicSuffixList());

        CookieSetResult stored = jar.StoreSetCookieHeader(
            "short=1; Max-Age=60; Path=/",
            Context());
        clock.Value = ReceiptTime.AddSeconds(30);
        CookieSelectionResult selected = jar.SelectRequestCookies(
            RequestContext());
        clock.Value = ReceiptTime.AddSeconds(60);
        int purged = jar.PurgeExpiredCookies();

        Assert.That(stored.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(stored.Cookie!.CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(stored.Cookie.Expires, Is.EqualTo(ReceiptTime.AddSeconds(60)));
        Assert.That(selected.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "short" }));
        Assert.That(selected.Cookies.Single().LastAccessTime,
            Is.EqualTo(ReceiptTime.AddSeconds(30)));
        Assert.That(purged, Is.EqualTo(1));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void EvictionUsesInjectedTimeInsteadOfPolicySuppliedCreationTime()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        StoredCookie existing = ManagementCookie(
            "existing",
            "shop.example.test",
            "/",
            false,
            "",
            0);
        existing.Persistent = true;
        existing.HasExpires = true;
        existing.Expires = ReceiptTime.AddHours(1);
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = [existing],
            NextSequenceNumber = 1
        });
        JarClock clock = new JarClock(ReceiptTime);
        ICookieJar jar = new CookieJar(
            options,
            new CookieParser(),
            new JarFutureCreationPolicy(ReceiptTime.AddHours(2)),
            store,
            new CookieSerializer(),
            clock);

        CookieSetResult result = jar.StoreSetCookieModel(
            new SetCookie { Name = "new", Value = "value" },
            Context());

        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "existing", "new" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Not.Contain(CookieErrorCode.Expired));
    }

    [Test]
    public void SnapshotMergeReplacesAddsAndDeletesExpiredIdentitiesDeterministically()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        StoredCookie existing = ManagementCookie(
            "id",
            "shop.example.test",
            "/",
            false,
            "",
            0);
        existing.Value = "old";
        StoredCookie deleted = ManagementCookie(
            "delete",
            "shop.example.test",
            "/",
            false,
            "",
            1);
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = [existing, deleted],
            NextSequenceNumber = 2
        });
        ICookieJar jar = Jar(options, store);
        StoredCookie replacement = ManagementCookie(
            "id",
            "shop.example.test",
            "/",
            false,
            "",
            10);
        replacement.Value = "new";
        replacement.CreationTime = ReceiptTime.AddDays(-5);
        replacement.LastAccessTime = ReceiptTime.AddDays(-1);
        StoredCookie added = ManagementCookie(
            "added",
            "shop.example.test",
            "/",
            false,
            "",
            11);
        StoredCookie expired = ManagementCookie(
            "delete",
            "shop.example.test",
            "/",
            false,
            "",
            12);
        expired.Persistent = true;
        expired.HasExpires = true;
        expired.CreationTime = ReceiptTime.AddDays(-2);
        expired.LastAccessTime = ReceiptTime.AddDays(-2);
        expired.Expires = ReceiptTime.AddDays(-1);
        CookieJarSnapshot snapshot = new CookieJarSnapshot
        {
            Cookies = [replacement, added, expired],
            NextSequenceNumber = 13
        };

        CookieImportResult result = jar.ImportJarSnapshot(
            snapshot,
            CookieImportMode.Merge);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Mode, Is.EqualTo(CookieImportMode.Merge));
        Assert.That(result.Results.Select(entry => entry.Status),
            Is.EqualTo(new[]
            {
                CookieSetStatus.Replaced,
                CookieSetStatus.Accepted,
                CookieSetStatus.Deleted
            }));
        Assert.That(result.AcceptedCount, Is.EqualTo(1));
        Assert.That(result.ReplacedCount, Is.EqualTo(1));
        Assert.That(result.DeletedCount, Is.EqualTo(1));
        Assert.That(result.RemovedCount, Is.Zero);
        StoredCookie[] retained = jar.QueryJarCookies(new CookieStoreQuery());
        Assert.That(retained.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "id", "added" }));
        Assert.That(retained[0].Value, Is.EqualTo("new"));
        Assert.That(retained[0].CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(retained[0].SequenceNumber, Is.Zero);
        Assert.That(retained[1].SequenceNumber, Is.EqualTo(2));
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(3));

        result.Results[0].Cookie!.Value = "changed-result";
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Single(cookie => cookie.Name == "id").Value,
            Is.EqualTo("new"));
    }

    [Test]
    public void SnapshotReplaceRevalidatesPslSecurityAndQuotaBeforeAtomicCommit()
    {
        CookieJarOptions options = Options();
        options.EnablePartitionedCookies = true;
        options.PrefixPolicy = CookiePrefixPolicy.ErrorReject;
        options.Limits.MaximumCookies = 1;
        options.Limits.MaximumCookiesPerDomain = 1;
        MemoryCookieStore store = new MemoryCookieStore();
        store.ApplyStoredCookie(ManagementCookie(
            "old",
            "shop.example.test",
            "/",
            false,
            "",
            0));
        ICookieJar jar = Jar(options, store);
        StoredCookie accepted = ManagementCookie(
            "accepted",
            "example.test",
            "/",
            false,
            "",
            0);
        StoredCookie publicSuffix = ManagementCookie(
            "public-suffix",
            "test",
            "/",
            false,
            "",
            1);
        StoredCookie badPrefix = ManagementCookie(
            "__Secure-bad",
            "shop.example.test",
            "/",
            false,
            "",
            2);
        StoredCookie badPartition = ManagementCookie(
            "bad-partition",
            "shop.example.test",
            "/",
            true,
            "https://top.example",
            3);
        StoredCookie retained = ManagementCookie(
            "retained",
            "shop.example.test",
            "/",
            false,
            "",
            4);
        accepted.LastAccessTime = ReceiptTime.AddHours(-1);
        CookieJarSnapshot snapshot = new CookieJarSnapshot
        {
            Cookies =
            [
                accepted,
                publicSuffix,
                badPrefix,
                badPartition,
                retained
            ],
            NextSequenceNumber = 5
        };

        CookieImportResult result = jar.ImportJarSnapshot(
            snapshot,
            CookieImportMode.Replace);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AcceptedCount, Is.EqualTo(2));
        Assert.That(result.RejectedCount, Is.EqualTo(3));
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.Results.Select(entry => entry.ErrorCode),
            Is.EqualTo(new[]
            {
                CookieErrorCode.None,
                CookieErrorCode.PublicSuffix,
                CookieErrorCode.PrefixViolation,
                CookieErrorCode.InsecureSecureCookie,
                CookieErrorCode.None
            }));
        Assert.That(result.Results[0].Cookie, Is.Null);
        Assert.That(result.Results[0].Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "retained" }));
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(5));
    }

    [Test]
    public void InvalidSnapshotImportFailsWithoutMutatingLiveState()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        store.ApplyStoredCookie(ManagementCookie(
            "live",
            "example.test",
            "/",
            false,
            "",
            0));
        ICookieJar jar = Jar(options, store);
        StoredCookie first = ManagementCookie(
            "first",
            "example.test",
            "/",
            false,
            "",
            0);
        StoredCookie second = ManagementCookie(
            "second",
            "example.test",
            "/",
            false,
            "",
            0);
        CookieJarSnapshot invalid = new CookieJarSnapshot
        {
            Cookies = [first, second],
            NextSequenceNumber = 1
        };

        CookieImportResult result = jar.ImportJarSnapshot(
            invalid,
            CookieImportMode.Replace);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidSnapshot));
        Assert.That(result.Results, Is.Empty);
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "live" }));
    }

    static CookieRequestContext RequestContext()
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri("https://shop.example.test/account/deep/page"),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }

    static StoredCookie SelectionCookie(string name, DateTimeOffset creationTime)
    {
        return new StoredCookie
        {
            Name = name,
            Value = name,
            Domain = "shop.example.test",
            Path = "/account",
            HostOnly = true,
            CreationTime = creationTime,
            LastAccessTime = creationTime
        };
    }

    static StoredCookie ManagementCookie(
        string name,
        string domain,
        string path,
        bool hasPartitionKey,
        string partitionKey,
        long sequenceNumber)
    {
        return new StoredCookie
        {
            Name = name,
            Value = name + "-value",
            Domain = domain,
            Path = path,
            CreationTime = ReceiptTime,
            LastAccessTime = ReceiptTime,
            HasPartitionKey = hasPartitionKey,
            PartitionKey = partitionKey,
            SequenceNumber = sequenceNumber
        };
    }

    static CookieJar Jar(CookieJarOptions options, ICookieStore store)
    {
        JarClock clock = new JarClock(ReceiptTime);
        return JarWithServices(options, store, clock, new JarPublicSuffixList());
    }

    static CookieJar JarWithServices(
        CookieJarOptions options,
        ICookieStore store,
        JarClock clock,
        IPublicSuffixList publicSuffixList)
    {
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            publicSuffixList,
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

    static CookieJarOptions Options()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            RequirePublicSuffixList = true,
            PrefixPolicy = CookiePrefixPolicy.Disabled,
            ProtectSecureCookieOverlay = true,
            EvictNonSecureCookiesFirst = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static CookieSetContext Context()
    {
        return new CookieSetContext
        {
            Origin = new Uri("https://shop.example.test/account/page"),
            Options = new CookieJarOptions
            {
                ComplianceMode = (CookieComplianceMode)(-1)
            },
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }
}

public class JarClock : ICookieClock
{
    public DateTimeOffset Value;

    public JarClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class JarPublicSuffixList : IPublicSuffixList
{
    public string? GetPublicSuffix(string canonicalDomain)
    {
        return "test";
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        if (canonicalDomain == "test")
        {
            return null;
        }

        return "example.test";
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain == "test";
    }
}

public class JarMutablePublicSuffixList : IPublicSuffixList
{
    public bool ExampleIsPublicSuffix;

    public string? GetPublicSuffix(string canonicalDomain)
    {
        if (ExampleIsPublicSuffix && canonicalDomain == "example.test")
        {
            return "example.test";
        }

        return "test";
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        if (canonicalDomain == "test" ||
            (ExampleIsPublicSuffix && canonicalDomain == "example.test"))
        {
            return null;
        }

        return "example.test";
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain == "test" ||
            (ExampleIsPublicSuffix && canonicalDomain == "example.test");
    }
}

public class JarFutureCreationPolicy : ICookiePolicy
{
    public DateTimeOffset CreationTime;

    public JarFutureCreationPolicy(DateTimeOffset creationTime)
    {
        CreationTime = creationTime;
    }

    public CookieSetDecision EvaluateCookieAcceptance(
        SetCookie cookie,
        CookieSetContext context)
    {
        return new CookieSetDecision
        {
            Status = CookieSetStatus.Accepted,
            ErrorCode = CookieErrorCode.None,
            NormalizedCookie = new StoredCookie
            {
                Name = cookie.Name,
                Value = cookie.Value,
                Domain = "shop.example.test",
                Path = "/",
                HostOnly = true,
                CreationTime = CreationTime,
                LastAccessTime = CreationTime
            }
        };
    }

    public CookieMatchResult EvaluateCookieReturn(
        StoredCookie cookie,
        CookieRequestContext context)
    {
        return new CookieMatchResult
        {
            Cookie = cookie,
            Status = CookieMatchStatus.Excluded,
            ErrorCode = CookieErrorCode.PolicyRejection
        };
    }
}
