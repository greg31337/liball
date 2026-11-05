namespace cookies.Tests;

public class CookieJarConformanceTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2036, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Test]
    public void AcceptancePipelinePreservesItsOrderedRejectionReasonsThroughTheJar()
    {
        CookieJarOptions options = Options();
        options.EnablePartitionedCookies = true;
        options.PrefixPolicy = CookiePrefixPolicy.ErrorReject;
        MemoryCookieStore store = new MemoryCookieStore();
        ICookieJar jar = Jar(options, store, new JarConformanceClock(EvaluationTime));

        SetCookie invalid = Cookie("bad name");
        invalid.HasDomain = true;
        invalid.Domain = "other.test";
        AssertRejected(
            jar.StoreSetCookieModel(invalid, SetContext("https://shop.example.test/path")),
            CookieErrorCode.InvalidName);

        AssertRejected(
            jar.StoreSetCookieModel(
                Cookie("scheme"),
                SetContext("ftp://shop.example.test/path")),
            CookieErrorCode.UnsupportedScheme);

        SetCookie badPath = Cookie("path");
        badPath.HasPath = true;
        badPath.Path = "relative";
        badPath.Secure = true;
        AssertRejected(
            jar.StoreSetCookieModel(
                badPath,
                SetContext("http://shop.example.test/path")),
            CookieErrorCode.InvalidPath);

        SetCookie wrongDomain = Cookie("domain");
        wrongDomain.HasDomain = true;
        wrongDomain.Domain = "other.test";
        wrongDomain.Secure = true;
        AssertRejected(
            jar.StoreSetCookieModel(
                wrongDomain,
                SetContext("http://shop.example.test/path")),
            CookieErrorCode.DomainMismatch);

        SetCookie publicSuffix = Cookie("suffix");
        publicSuffix.HasDomain = true;
        publicSuffix.Domain = "test";
        publicSuffix.Secure = true;
        AssertRejected(
            jar.StoreSetCookieModel(
                publicSuffix,
                SetContext("http://shop.example.test/path")),
            CookieErrorCode.PublicSuffix);

        SetCookie insecure = Cookie("security");
        insecure.Secure = true;
        insecure.HasSameSite = true;
        insecure.SameSiteMode = CookieSameSiteMode.Strict;
        CookieSetContext insecureContext = SetContext(
            "http://shop.example.test/path");
        insecureContext.SameSiteContext = CookieSameSiteContext.CrossSite;
        AssertRejected(
            jar.StoreSetCookieModel(insecure, insecureContext),
            CookieErrorCode.InsecureSecureCookie);

        Assert.That(
            jar.StoreSetCookieHeader(
                "overlay=secure; Secure; Path=/path",
                SetContext("https://shop.example.test/path"))
                .Status,
            Is.EqualTo(CookieSetStatus.Accepted));
        SetCookie overlay = Cookie("overlay");
        overlay.HasPath = true;
        overlay.Path = "/path";
        overlay.HasSameSite = true;
        overlay.SameSiteMode = CookieSameSiteMode.Strict;
        CookieSetContext overlayContext = SetContext(
            "http://shop.example.test/path");
        overlayContext.SameSiteContext = CookieSameSiteContext.CrossSite;
        AssertRejected(
            jar.StoreSetCookieModel(overlay, overlayContext),
            CookieErrorCode.SecureOverlayViolation);

        SetCookie sameSite = Cookie("__Secure-same-site");
        sameSite.HasSameSite = true;
        sameSite.SameSiteMode = CookieSameSiteMode.Strict;
        CookieSetContext crossSite = SetContext("https://shop.example.test/path");
        crossSite.SameSiteContext = CookieSameSiteContext.CrossSite;
        AssertRejected(
            jar.StoreSetCookieModel(sameSite, crossSite),
            CookieErrorCode.SameSiteViolation);

        SetCookie prefix = Cookie("__Secure-partition");
        prefix.Partitioned = true;
        CookieSetContext missingPartition = SetContext(
            "https://shop.example.test/path");
        missingPartition.HasTopLevelSite = false;
        missingPartition.TopLevelSite = "";
        AssertRejected(
            jar.StoreSetCookieModel(prefix, missingPartition),
            CookieErrorCode.PrefixViolation);

        CookieJarOptions restrictedOptions = Options();
        restrictedOptions.EnablePartitionedCookies = true;
        restrictedOptions.PrefixPolicy = CookiePrefixPolicy.ErrorReject;
        restrictedOptions.AllowedDomains = ["other.test"];
        ICookieJar restricted = Jar(
            restrictedOptions,
            new MemoryCookieStore(),
            new JarConformanceClock(EvaluationTime));
        SetCookie partition = Cookie("partition");
        partition.Partitioned = true;
        partition.Secure = true;
        AssertRejected(
            restricted.StoreSetCookieModel(partition, missingPartition),
            CookieErrorCode.MissingPartitionContext);
        AssertRejected(
            restricted.StoreSetCookieModel(
                Cookie("application"),
                SetContext("https://shop.example.test/path")),
            CookieErrorCode.PolicyRejection);
    }

    [Test]
    public void BatchContinuesAcrossEveryMutationDisposition()
    {
        ICookieJar jar = Jar(
            Options(),
            new MemoryCookieStore(),
            new JarConformanceClock(EvaluationTime));

        CookieBatchSetResult result = jar.StoreSetCookieHeaders(
            [
                "item=one; Path=/",
                "bad\r\nvalue",
                "item=two; Path=/",
                "item=gone; Path=/; Max-Age=0",
                "absent=gone; Path=/; Max-Age=0",
                "survivor=three; Path=/"
            ],
            SetContext("https://shop.example.test/path"));

        Assert.That(result.Results.Select(item => item.Status),
            Is.EqualTo(new[]
            {
                CookieSetStatus.Accepted,
                CookieSetStatus.Rejected,
                CookieSetStatus.Replaced,
                CookieSetStatus.Deleted,
                CookieSetStatus.Ignored,
                CookieSetStatus.Accepted
            }));
        Assert.That(result.AcceptedCount, Is.EqualTo(2));
        Assert.That(result.ReplacedCount, Is.EqualTo(1));
        Assert.That(result.DeletedCount, Is.EqualTo(1));
        Assert.That(result.IgnoredCount, Is.EqualTo(1));
        Assert.That(result.RejectedCount, Is.EqualTo(1));
        Assert.That(result.Results[1].ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Results[3].ErrorCode,
            Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(result.Results[4].ErrorCode,
            Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "survivor" }));
    }

    [Test]
    public void DomainRulesApplyToBothAcceptanceAndRequestSelection()
    {
        CookieJarOptions allowedOptions = Options();
        allowedOptions.AllowedDomains = ["example.test"];
        ICookieJar allowedJar = Jar(
            allowedOptions,
            new MemoryCookieStore(),
            new JarConformanceClock(EvaluationTime));
        CookieSetResult allowed = allowedJar.StoreSetCookieHeader(
            "allowed=1; Path=/",
            SetContext("https://shop.example.test/path"));

        CookieJarOptions blockedOptions = Options();
        blockedOptions.AllowedDomains = ["example.test"];
        blockedOptions.BlockedDomains = ["shop.example.test"];
        MemoryCookieStore blockedStore = new MemoryCookieStore();
        ICookieJar blockedJar = Jar(
            blockedOptions,
            blockedStore,
            new JarConformanceClock(EvaluationTime));
        CookieSetResult blocked = blockedJar.StoreSetCookieHeader(
            "blocked=1; Path=/",
            SetContext("https://shop.example.test/path"));
        StoredCookie retainedBeforeRuleChange = Stored(
            "retained",
            "value",
            "shop.example.test",
            "/",
            0);
        blockedStore.ApplyStoredCookie(retainedBeforeRuleChange);

        CookieSelectionResult selection = blockedJar.SelectRequestCookies(
            RequestContext("https://shop.example.test/path"));

        Assert.That(allowed.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(allowedJar.SelectRequestCookies(
            RequestContext("https://shop.example.test/path")).Cookies,
            Has.Length.EqualTo(1));
        AssertRejected(blocked, CookieErrorCode.PolicyRejection);
        Assert.That(selection.Success, Is.True);
        Assert.That(selection.Cookies, Is.Empty);
        Assert.That(selection.Matches.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.PolicyRejection));
        Assert.That(blockedStore.FindStoredCookie(Identity(retainedBeforeRuleChange))!
            .LastAccessTime,
            Is.EqualTo(EvaluationTime.AddDays(-1)));
    }

    [Test]
    public void SelectionReportsEachExclusionAndPreservesDuplicateNameOutputAndAccess()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        StoredCookie deep = Stored(
            "id", "deep", "shop.example.test", "/account/deep", 0);
        StoredCookie account = Stored(
            "id", "account", "shop.example.test", "/account", 1);
        deep.HasSameSite = true;
        deep.SameSiteMode = CookieSameSiteMode.None;
        account.HasSameSite = true;
        account.SameSiteMode = CookieSameSiteMode.None;
        StoredCookie hostOnly = Stored(
            "host-only", "value", "example.test", "/account", 2);
        hostOnly.HostOnly = true;
        StoredCookie publicSuffix = Stored(
            "public-suffix", "value", "test", "/account", 3);
        StoredCookie wrongPath = Stored(
            "wrong-path", "value", "shop.example.test", "/other", 4);
        StoredCookie expired = Stored(
            "expired", "value", "shop.example.test", "/account", 5);
        expired.HasExpires = true;
        expired.Expires = EvaluationTime;
        StoredCookie secure = Stored(
            "secure", "value", "shop.example.test", "/account", 6);
        secure.Secure = true;
        StoredCookie httpOnly = Stored(
            "http-only", "value", "shop.example.test", "/account", 7);
        httpOnly.HttpOnly = true;
        StoredCookie strict = Stored(
            "strict", "value", "shop.example.test", "/account", 8);
        strict.HasSameSite = true;
        strict.SameSiteMode = CookieSameSiteMode.Strict;
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies =
            [
                deep,
                account,
                hostOnly,
                publicSuffix,
                wrongPath,
                expired,
                secure,
                httpOnly,
                strict
            ],
            NextSequenceNumber = 9
        });
        ICookieJar jar = Jar(
            options,
            store,
            new JarConformanceClock(EvaluationTime));
        CookieRequestContext request = RequestContext(
            "http://shop.example.test/account/deep/page");
        request.AccessKind = CookieAccessKind.NonHttp;
        request.SameSiteContext = CookieSameSiteContext.CrossSite;

        CookieSelectionResult selection = jar.SelectRequestCookies(request);
        CookieSelectionResult fields = jar.CreateRequestCookieFields(request);

        Assert.That(selection.Cookies.Select(cookie => cookie.Value),
            Is.EqualTo(new[] { "deep", "account" }));
        Assert.That(selection.Pairs.Select(pair => pair.Name),
            Is.EqualTo(new[] { "id", "id" }));
        Assert.That(fields.FieldValues,
            Is.EqualTo(new[] { "id=deep; id=account" }));
        AssertMatch(selection, "host-only", CookieErrorCode.DomainMismatch);
        AssertMatch(selection, "public-suffix", CookieErrorCode.PublicSuffix);
        AssertMatch(selection, "wrong-path", CookieErrorCode.InvalidPath);
        AssertMatch(selection, "expired", CookieErrorCode.Expired);
        AssertMatch(selection, "secure", CookieErrorCode.InsecureSecureCookie);
        AssertMatch(selection, "http-only", CookieErrorCode.HttpOnlyApiViolation);
        AssertMatch(selection, "strict", CookieErrorCode.SameSiteViolation);
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Where(cookie => cookie.Name == "id")
            .Select(cookie => cookie.LastAccessTime),
            Is.All.EqualTo(EvaluationTime));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Where(cookie => cookie.Name != "id")
            .Select(cookie => cookie.LastAccessTime),
            Is.All.EqualTo(EvaluationTime.AddDays(-1)));
    }

    [Test]
    public void EveryPublicJarMethodIsUsableThroughICookieJar()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = new MemoryCookieStore();
        JarConformanceClock clock = new JarConformanceClock(EvaluationTime);
        ICookieJar jar = Jar(options, store, clock);
        CookieSetContext context = SetContext("https://shop.example.test/account");

        CookieSetResult header = jar.StoreSetCookieHeader(
            "header=one; Path=/; Max-Age=60",
            context);
        CookieBatchSetResult batch = jar.StoreSetCookieHeaders(
            ["batch=two; Path=/"],
            context);
        CookieSetResult model = jar.StoreSetCookieModel(
            Cookie("model"),
            context);
        CookieSelectionResult selected = jar.SelectRequestCookies(
            RequestContext("https://shop.example.test/account"));
        CookieSelectionResult fields = jar.CreateRequestCookieFields(
            RequestContext("https://shop.example.test/account"));
        StoredCookie[] queried = jar.QueryJarCookies(new CookieStoreQuery());
        CookieJarSnapshot exported = jar.ExportJarSnapshot();
        CookieImportResult imported = jar.ImportJarSnapshot(
            exported,
            CookieImportMode.Merge);
        int removed = jar.RemoveJarCookies(new CookieRemovalQuery
        {
            HasName = true,
            Name = "model"
        });
        clock.Value = EvaluationTime.AddMinutes(2);
        int purged = jar.PurgeExpiredCookies();

        Assert.That(header.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(batch.AcceptedCount, Is.EqualTo(1));
        Assert.That(model.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(selected.Success, Is.True);
        Assert.That(selected.Cookies, Has.Length.EqualTo(3));
        Assert.That(fields.Success, Is.True);
        Assert.That(fields.FieldValues, Has.Length.EqualTo(1));
        Assert.That(queried, Has.Length.EqualTo(3));
        Assert.That(exported.Cookies, Has.Length.EqualTo(3));
        Assert.That(imported.Success, Is.True);
        Assert.That(removed, Is.EqualTo(1));
        Assert.That(purged, Is.EqualTo(1));
        Assert.That(jar.QueryJarCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "batch" }));
    }

    static CookieJarOptions Options()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            RequirePublicSuffixList = true,
            EnforceSameSite = true,
            PrefixPolicy = CookiePrefixPolicy.Disabled,
            ProtectSecureCookieOverlay = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static ICookieJar Jar(
        CookieJarOptions options,
        ICookieStore store,
        JarConformanceClock clock)
    {
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            new JarConformancePublicSuffixList(),
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

    static SetCookie Cookie(string name)
    {
        return new SetCookie
        {
            Name = name,
            Value = "value"
        };
    }

    static CookieSetContext SetContext(string origin)
    {
        return new CookieSetContext
        {
            Origin = new Uri(origin),
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }

    static CookieRequestContext RequestContext(string requestUri)
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri(requestUri),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }

    static StoredCookie Stored(
        string name,
        string value,
        string domain,
        string path,
        long sequence)
    {
        return new StoredCookie
        {
            Name = name,
            Value = value,
            Domain = domain,
            Path = path,
            CreationTime = EvaluationTime.AddDays(-1),
            LastAccessTime = EvaluationTime.AddDays(-1),
            SequenceNumber = sequence
        };
    }

    static CookieIdentity Identity(StoredCookie cookie)
    {
        return new CookieIdentity
        {
            Name = cookie.Name,
            Domain = cookie.Domain,
            Path = cookie.Path,
            HasPartitionKey = cookie.HasPartitionKey,
            PartitionKey = cookie.PartitionKey
        };
    }

    static void AssertRejected(CookieSetResult result, CookieErrorCode errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Rejected));
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(errorCode));
    }

    static void AssertMatch(
        CookieSelectionResult result,
        string name,
        CookieErrorCode errorCode)
    {
        CookieMatchResult match = result.Matches.Single(item => item.Cookie!.Name == name);
        Assert.That(match.Status, Is.EqualTo(CookieMatchStatus.Excluded));
        Assert.That(match.ErrorCode, Is.EqualTo(errorCode));
    }
}

public class JarConformanceClock : ICookieClock
{
    public DateTimeOffset Value;

    public JarConformanceClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class JarConformancePublicSuffixList : IPublicSuffixList
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
