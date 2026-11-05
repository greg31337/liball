namespace cookies.Tests;

public class CookieGraphClonerTests
{
    [Test]
    public void OptionsAndSetContextCopiesOwnEveryMutableNestedLayer()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            AllowedDomains = ["allowed.test"],
            BlockedDomains = ["blocked.test"]
        };
        options.Limits.MaximumCookies = 41;
        options.ParseOptions.Limits.MaximumInputLineOctets = 42;
        options.SerializationOptions.Limits.MaximumRequestHeaderOctets = 43;
        options.NetscapeFileOptions.Limits.MaximumNameValueOctets = 44;
        StoredCookie exact = Stored("exact");
        StoredCookie overlap = Stored("overlap");
        CookieSetContext source = new CookieSetContext
        {
            Origin = new Uri("https://example.test/path"),
            Options = options,
            ExistingCookie = exact,
            ExistingCookies = [overlap],
            HasTopLevelSite = true,
            TopLevelSite = "https://top.test"
        };

        CookieSetContext copy = CookieGraphCloner.CloneSetContext(source);

        Assert.That(copy, Is.Not.SameAs(source));
        Assert.That(copy.Options, Is.Not.SameAs(options));
        Assert.That(copy.Options.Limits, Is.Not.SameAs(options.Limits));
        Assert.That(copy.Options.ParseOptions, Is.Not.SameAs(options.ParseOptions));
        Assert.That(copy.Options.ParseOptions.Limits, Is.Not.SameAs(options.ParseOptions.Limits));
        Assert.That(copy.Options.SerializationOptions, Is.Not.SameAs(options.SerializationOptions));
        Assert.That(copy.Options.SerializationOptions.Limits, Is.Not.SameAs(options.SerializationOptions.Limits));
        Assert.That(copy.Options.NetscapeFileOptions, Is.Not.SameAs(options.NetscapeFileOptions));
        Assert.That(copy.Options.NetscapeFileOptions.Limits, Is.Not.SameAs(options.NetscapeFileOptions.Limits));
        Assert.That(copy.Options.AllowedDomains, Is.Not.SameAs(options.AllowedDomains));
        Assert.That(copy.Options.BlockedDomains, Is.Not.SameAs(options.BlockedDomains));
        Assert.That(copy.ExistingCookie, Is.Not.SameAs(exact));
        Assert.That(copy.ExistingCookies, Is.Not.SameAs(source.ExistingCookies));
        Assert.That(copy.ExistingCookies[0], Is.Not.SameAs(overlap));
        Assert.That(copy.ExistingCookies[0].Extensions, Is.Not.SameAs(overlap.Extensions));
        Assert.That(copy.ExistingCookies[0].Extensions[0], Is.Not.SameAs(overlap.Extensions[0]));

        options.Limits.MaximumCookies = 1;
        options.ParseOptions.Limits.MaximumInputLineOctets = 2;
        options.SerializationOptions.Limits.MaximumRequestHeaderOctets = 3;
        options.NetscapeFileOptions.Limits.MaximumNameValueOctets = 4;
        options.AllowedDomains[0] = "changed.test";
        exact.Name = "changed";
        overlap.Extensions[0].Value = "changed";

        Assert.That(copy.Options.Limits.MaximumCookies, Is.EqualTo(41));
        Assert.That(copy.Options.ParseOptions.Limits.MaximumInputLineOctets, Is.EqualTo(42));
        Assert.That(copy.Options.SerializationOptions.Limits.MaximumRequestHeaderOctets, Is.EqualTo(43));
        Assert.That(copy.Options.NetscapeFileOptions.Limits.MaximumNameValueOctets, Is.EqualTo(44));
        Assert.That(copy.Options.AllowedDomains, Is.EqualTo(new[] { "allowed.test" }));
        Assert.That(copy.ExistingCookie!.Name, Is.EqualTo("exact"));
        Assert.That(copy.ExistingCookies[0].Extensions[0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void CookieIdentityAndQueryCopiesAreIndependent()
    {
        CookieIdentity identity = new CookieIdentity
        {
            Name = "id",
            Domain = "example.test",
            Path = "/",
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        };
        CookieStoreQuery store = new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = identity,
            HasDomain = true,
            Domain = "example.test",
            HasPath = true,
            Path = "/",
            HasName = true,
            Name = "id"
        };
        CookieRemovalQuery removal = new CookieRemovalQuery
        {
            HasIdentity = true,
            Identity = identity,
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        };
        SetCookie setCookie = new SetCookie
        {
            Name = "id",
            Value = "value",
            Extensions = [new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }]
        };

        CookieStoreQuery storeCopy = CookieGraphCloner.CloneStoreQuery(store);
        CookieRemovalQuery removalCopy = CookieGraphCloner.CloneRemovalQuery(removal);
        SetCookie cookieCopy = CookieGraphCloner.CloneSetCookie(setCookie);

        Assert.That(storeCopy.Identity, Is.Not.SameAs(identity));
        Assert.That(removalCopy.Identity, Is.Not.SameAs(identity));
        Assert.That(storeCopy.Identity, Is.Not.SameAs(removalCopy.Identity));
        Assert.That(cookieCopy, Is.Not.SameAs(setCookie));
        Assert.That(cookieCopy.Extensions, Is.Not.SameAs(setCookie.Extensions));
        Assert.That(cookieCopy.Extensions[0], Is.Not.SameAs(setCookie.Extensions[0]));

        identity.Name = "changed";
        setCookie.Extensions[0].Value = "changed";
        Assert.That(storeCopy.Identity.Name, Is.EqualTo("id"));
        Assert.That(removalCopy.Identity.Name, Is.EqualTo("id"));
        Assert.That(cookieCopy.Extensions[0].Value, Is.EqualTo("High"));
    }

    [Test]
    public void SelectionAndSnapshotCopiesDoNotShareNestedCookiesOrDiagnostics()
    {
        StoredCookie stored = Stored("id");
        CookieIssue issue = Issue(CookieErrorCode.Expired);
        CookiePair pair = new CookiePair { Name = "id", Value = "value" };
        CookieMatchResult match = new CookieMatchResult
        {
            Cookie = stored,
            Pair = pair,
            Status = CookieMatchStatus.Included,
            Issues = [issue]
        };
        CookieSelectionResult selection = new CookieSelectionResult
        {
            Success = true,
            Cookies = [stored],
            Pairs = [pair],
            FieldValues = ["id=value"],
            Matches = [match],
            Issues = [issue]
        };
        CookieJarSnapshot snapshot = new CookieJarSnapshot
        {
            Cookies = [stored],
            NextSequenceNumber = 9
        };

        CookieSelectionResult selectionCopy = CookieGraphCloner.CloneSelectionResult(selection);
        CookieJarSnapshot snapshotCopy = CookieGraphCloner.CloneJarSnapshot(snapshot);

        Assert.That(selectionCopy.Cookies, Is.Not.SameAs(selection.Cookies));
        Assert.That(selectionCopy.Cookies[0], Is.Not.SameAs(stored));
        Assert.That(selectionCopy.Cookies[0].Extensions[0], Is.Not.SameAs(stored.Extensions[0]));
        Assert.That(selectionCopy.Pairs[0], Is.Not.SameAs(pair));
        Assert.That(selectionCopy.FieldValues, Is.Not.SameAs(selection.FieldValues));
        Assert.That(selectionCopy.Matches[0], Is.Not.SameAs(match));
        Assert.That(selectionCopy.Matches[0].Cookie, Is.Not.SameAs(stored));
        Assert.That(selectionCopy.Matches[0].Pair, Is.Not.SameAs(pair));
        Assert.That(selectionCopy.Matches[0].Issues[0], Is.Not.SameAs(issue));
        Assert.That(selectionCopy.Issues[0], Is.Not.SameAs(issue));
        Assert.That(snapshotCopy.Cookies, Is.Not.SameAs(snapshot.Cookies));
        Assert.That(snapshotCopy.Cookies[0], Is.Not.SameAs(stored));

        stored.Value = "changed";
        stored.Extensions[0].Value = "changed";
        pair.Value = "changed";
        issue.SafeFragment = "changed";
        selection.FieldValues[0] = "changed";

        Assert.That(selectionCopy.Cookies[0].Value, Is.EqualTo("value"));
        Assert.That(selectionCopy.Cookies[0].Extensions[0].Value, Is.EqualTo("value"));
        Assert.That(selectionCopy.Pairs[0].Value, Is.EqualTo("value"));
        Assert.That(selectionCopy.Matches[0].Issues[0].SafeFragment, Is.EqualTo("safe"));
        Assert.That(selectionCopy.FieldValues[0], Is.EqualTo("id=value"));
        Assert.That(snapshotCopy.Cookies[0].Value, Is.EqualTo("value"));
    }

    [Test]
    public void ParseBatchImportAndCodecResultCopiesOwnTheirNestedGraphs()
    {
        SetCookieParseResult parsed = new SetCookieParseResult
        {
            Success = true,
            Cookie = new SetCookie
            {
                Name = "id",
                Value = "value",
                Extensions = [new CookieAttribute { Name = "Flag" }]
            },
            Issues = [Issue(CookieErrorCode.UnknownAttribute)]
        };
        CookieSetResult setResult = new CookieSetResult
        {
            Cookie = Stored("id"),
            ParseResult = parsed,
            Issues = [Issue(CookieErrorCode.None)]
        };
        CookieBatchSetResult batch = new CookieBatchSetResult
        {
            Results = [setResult],
            AcceptedCount = 1,
            Issues = [Issue(CookieErrorCode.None)]
        };
        CookieImportResult import = new CookieImportResult
        {
            Success = true,
            Results = [setResult],
            AcceptedCount = 1,
            Issues = [Issue(CookieErrorCode.None)]
        };
        CookieSnapshotParseResult snapshot = new CookieSnapshotParseResult
        {
            Success = true,
            Snapshot = new CookieJarSnapshot { Cookies = [Stored("snapshot")] },
            Issues = [Issue(CookieErrorCode.None)]
        };
        NetscapeCookieFileParseResult netscape = new NetscapeCookieFileParseResult
        {
            Success = true,
            Cookies = [Stored("netscape")],
            Issues = [Issue(CookieErrorCode.LossyImport)]
        };
        CookieHeaderParseResult header = new CookieHeaderParseResult
        {
            Success = true,
            Pairs = [new CookiePair { Name = "id", Value = "value" }],
            Issues = [Issue(CookieErrorCode.None)]
        };
        CookieSetDecision decision = new CookieSetDecision
        {
            NormalizedCookie = Stored("decision"),
            Issues = [Issue(CookieErrorCode.None)]
        };

        CookieBatchSetResult batchCopy = CookieGraphCloner.CloneBatchSetResult(batch);
        CookieImportResult importCopy = CookieGraphCloner.CloneImportResult(import);
        CookieSnapshotParseResult snapshotCopy = CookieGraphCloner.CloneSnapshotParseResult(snapshot);
        NetscapeCookieFileParseResult netscapeCopy = CookieGraphCloner.CloneNetscapeParseResult(netscape);
        CookieHeaderParseResult headerCopy = CookieGraphCloner.CloneHeaderParseResult(header);
        CookieSetDecision decisionCopy = CookieGraphCloner.CloneSetDecision(decision);

        Assert.That(batchCopy.Results[0], Is.Not.SameAs(setResult));
        Assert.That(batchCopy.Results[0].Cookie, Is.Not.SameAs(setResult.Cookie));
        Assert.That(batchCopy.Results[0].ParseResult, Is.Not.SameAs(parsed));
        Assert.That(batchCopy.Results[0].ParseResult!.Cookie, Is.Not.SameAs(parsed.Cookie));
        Assert.That(batchCopy.Results[0].ParseResult!.Cookie!.Extensions[0], Is.Not.SameAs(parsed.Cookie!.Extensions[0]));
        Assert.That(importCopy.Results[0], Is.Not.SameAs(setResult));
        Assert.That(snapshotCopy.Snapshot, Is.Not.SameAs(snapshot.Snapshot));
        Assert.That(snapshotCopy.Snapshot!.Cookies[0], Is.Not.SameAs(snapshot.Snapshot!.Cookies[0]));
        Assert.That(netscapeCopy.Cookies[0], Is.Not.SameAs(netscape.Cookies[0]));
        Assert.That(headerCopy.Pairs[0], Is.Not.SameAs(header.Pairs[0]));
        Assert.That(decisionCopy.NormalizedCookie, Is.Not.SameAs(decision.NormalizedCookie));
        Assert.That(decisionCopy.Issues[0], Is.Not.SameAs(decision.Issues[0]));
    }

    static StoredCookie Stored(string name)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "example.test",
            Path = "/",
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "value"
                }
            ]
        };
    }

    static CookieIssue Issue(CookieErrorCode errorCode)
    {
        return new CookieIssue
        {
            ErrorCode = errorCode,
            Severity = CookieParseSeverity.Warning,
            SafeFragment = "safe"
        };
    }
}
