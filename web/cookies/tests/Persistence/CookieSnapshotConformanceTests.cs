namespace cookies.Tests;

public class CookieSnapshotConformanceTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2040, 8, 9, 10, 11, 12, TimeSpan.Zero);

    [Test]
    public void JarCodecJarRoundTripPreservesEveryStoredField()
    {
        CookieJarOptions options = Options(CookieComplianceMode.Compatible);
        options.EnablePartitionedCookies = true;
        MemoryCookieStore sourceStore = new MemoryCookieStore();
        StoredCookie sourceCookie = CompleteCookie();
        sourceStore.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = [sourceCookie],
            NextSequenceNumber = 8
        });
        ICookieJar sourceJar = Jar(options, sourceStore);
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();

        CookieJarSnapshot exported = sourceJar.ExportJarSnapshot();
        string firstText = codec.SerializeJarSnapshot(exported);
        string secondText = codec.SerializeJarSnapshot(exported);
        CookieJarSnapshot parsed = codec.ParseJarSnapshot(firstText);
        MemoryCookieStore targetStore = new MemoryCookieStore();
        ICookieJar targetJar = Jar(options, targetStore);
        CookieImportResult imported = targetJar.ImportJarSnapshot(
            parsed,
            CookieImportMode.Replace);
        CookieJarSnapshot roundTrip = targetJar.ExportJarSnapshot();

        Assert.That(secondText, Is.EqualTo(firstText));
        Assert.That(imported.Success, Is.True);
        Assert.That(imported.AcceptedCount, Is.EqualTo(1));
        Assert.That(roundTrip.Version, Is.EqualTo(CookieDefaults.SnapshotVersion));
        Assert.That(roundTrip.NextSequenceNumber, Is.EqualTo(8));
        AssertStoredCookie(roundTrip.Cookies.Single(), sourceCookie);
        Assert.That(codec.SerializeJarSnapshot(roundTrip), Is.EqualTo(firstText));

        exported.Cookies[0].Value = "mutated-export";
        parsed.Cookies[0].Extensions[0].Value = "mutated-parse";
        imported.Results[0].Cookie!.Value = "mutated-import-result";
        AssertStoredCookie(targetJar.ExportJarSnapshot().Cookies.Single(), sourceCookie);
    }

    [Test]
    public void MergeAndReplaceResolveConflictsWithDistinctStableSemantics()
    {
        StoredCookie liveConflict = BasicCookie("conflict", 0);
        liveConflict.Value = "live";
        liveConflict.CreationTime = EvaluationTime.AddDays(-10);
        StoredCookie liveOnly = BasicCookie("live-only", 1);
        StoredCookie importedConflict = BasicCookie("conflict", 5);
        importedConflict.Value = "imported";
        importedConflict.CreationTime = EvaluationTime.AddDays(-5);
        StoredCookie importedOnly = BasicCookie("imported-only", 6);
        CookieJarSnapshot importedSnapshot = new CookieJarSnapshot
        {
            Cookies = [importedConflict, importedOnly],
            NextSequenceNumber = 7
        };

        MemoryCookieStore mergeStore = SeededStore(liveConflict, liveOnly);
        CookieImportResult merge = Jar(Options(), mergeStore).ImportJarSnapshot(
            importedSnapshot,
            CookieImportMode.Merge);
        StoredCookie[] merged = mergeStore.QueryStoredCookies(new CookieStoreQuery());

        MemoryCookieStore replaceStore = SeededStore(liveConflict, liveOnly);
        CookieImportResult replace = Jar(Options(), replaceStore).ImportJarSnapshot(
            importedSnapshot,
            CookieImportMode.Replace);
        StoredCookie[] replaced = replaceStore.QueryStoredCookies(new CookieStoreQuery());

        Assert.That(merge.Results.Select(result => result.Status),
            Is.EqualTo(new[] { CookieSetStatus.Replaced, CookieSetStatus.Accepted }));
        Assert.That(merged.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "conflict", "live-only", "imported-only" }));
        StoredCookie mergedConflict = merged.Single(cookie => cookie.Name == "conflict");
        Assert.That(mergedConflict.Value, Is.EqualTo("imported"));
        Assert.That(mergedConflict.CreationTime,
            Is.EqualTo(EvaluationTime.AddDays(-10)));
        Assert.That(mergedConflict.SequenceNumber, Is.Zero);
        Assert.That(merged.Single(cookie => cookie.Name == "imported-only").SequenceNumber,
            Is.EqualTo(2));
        Assert.That(mergeStore.CaptureStoreSnapshot().NextSequenceNumber,
            Is.EqualTo(3));

        Assert.That(replace.Results.Select(result => result.Status),
            Is.All.EqualTo(CookieSetStatus.Accepted));
        Assert.That(replace.RemovedCount, Is.EqualTo(2));
        Assert.That(replaced.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "conflict", "imported-only" }));
        Assert.That(replaced.Select(cookie => cookie.SequenceNumber),
            Is.EqualTo(new long[] { 5, 6 }));
        Assert.That(replaced[0].CreationTime,
            Is.EqualTo(EvaluationTime.AddDays(-5)));
        Assert.That(replaceStore.CaptureStoreSnapshot().NextSequenceNumber,
            Is.EqualTo(7));
    }

    [Test]
    public void ReaderPairsThrowingAndNonThrowingResultsForCorruptAndHugeInputs()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        StoredCookie first = BasicCookie("first", 0);
        StoredCookie second = BasicCookie("second", 1);
        string valid = codec.SerializeJarSnapshot(new CookieJarSnapshot
        {
            Cookies = [first, second],
            NextSequenceNumber = 2
        });
        string duplicate = valid.Replace("\"name\":\"second\"", "\"name\":\"first\"");
        string wrongType = valid.Replace("\"secure\":false", "\"secure\":\"false\"");
        string unsupported = valid.Replace("\"version\":1", "\"version\":2");
        string oversizedValue = new string(
            'x',
            CookieDefaults.MaximumInputLineOctets);
        string huge = valid.Replace(
            "\"value\":\"value\"",
            "\"value\":\"" + oversizedValue + "\"");
        string[] texts = ["{", wrongType, unsupported, duplicate, huge];
        CookieErrorCode[] errors =
        [
            CookieErrorCode.InvalidSnapshot,
            CookieErrorCode.InvalidSnapshot,
            CookieErrorCode.UnsupportedSnapshotVersion,
            CookieErrorCode.DuplicateIdentity,
            CookieErrorCode.InvalidSnapshot
        ];

        for (int index = 0; index < texts.Length; index++)
        {
            CookieSnapshotParseResult result = codec.TryParseJarSnapshot(texts[index]);
            CookieSnapshotException exception = Assert.Throws<CookieSnapshotException>(
                () => codec.ParseJarSnapshot(texts[index]))!;

            Assert.That(result.Success, Is.False);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(errors[index]));
            Assert.That(result.Issues.Single().ErrorCode, Is.EqualTo(errors[index]));
            Assert.That(exception.Message,
                Is.EqualTo("Cookie snapshot failed with error code " +
                    (int)errors[index] + "."));
            Assert.That(exception.Message, Does.Not.Contain(oversizedValue));
        }
    }

    [Test]
    public void ImportReportsEveryEntryPolicyIssueWithoutAbortingTheBatch()
    {
        StoredCookie invalidName = BasicCookie("bad name", 0);
        AssertEntryIssue(Options(), invalidName, CookieErrorCode.InvalidName);

        StoredCookie invalidValue = BasicCookie("bad-value", 0);
        invalidValue.Value = "space value";
        AssertEntryIssue(Options(), invalidValue, CookieErrorCode.InvalidValue);

        CookieJarOptions nameValueOptions = Options();
        SetNameValueLimit(nameValueOptions, 3);
        StoredCookie longPair = BasicCookie("id", 0);
        longPair.Value = "xx";
        AssertEntryIssue(
            nameValueOptions,
            longPair,
            CookieErrorCode.NameValueLimitExceeded);

        CookieJarOptions attributeOptions = Options();
        SetAttributeLimit(attributeOptions, 4);
        StoredCookie longAttribute = BasicCookie("attr", 0);
        longAttribute.Extensions =
        [
            new CookieAttribute
            {
                Name = "X",
                HasValue = true,
                Value = "12345"
            }
        ];
        AssertEntryIssue(
            attributeOptions,
            longAttribute,
            CookieErrorCode.AttributeLimitExceeded);

        StoredCookie disabledPartition = BasicCookie("disabled-partition", 0);
        disabledPartition.HasPartitionKey = true;
        disabledPartition.PartitionKey = "https://top.example";
        disabledPartition.Secure = true;
        AssertEntryIssue(
            Options(),
            disabledPartition,
            CookieErrorCode.PartitionMismatch);

        CookieJarOptions partitionOptions = Options();
        partitionOptions.EnablePartitionedCookies = true;
        StoredCookie insecurePartition = BasicCookie("insecure-partition", 0);
        insecurePartition.HasPartitionKey = true;
        insecurePartition.PartitionKey = "https://top.example";
        AssertEntryIssue(
            partitionOptions,
            insecurePartition,
            CookieErrorCode.InsecureSecureCookie);

        CookieJarOptions sameSiteOptions = Options();
        sameSiteOptions.RequireSecureForSameSiteNone = true;
        StoredCookie insecureNone = BasicCookie("insecure-none", 0);
        insecureNone.HasSameSite = true;
        insecureNone.SameSiteMode = CookieSameSiteMode.None;
        AssertEntryIssue(
            sameSiteOptions,
            insecureNone,
            CookieErrorCode.SameSiteViolation);

        CookieJarOptions prefixOptions = Options();
        prefixOptions.PrefixPolicy = CookiePrefixPolicy.ErrorReject;
        StoredCookie invalidPrefix = BasicCookie("__Secure-invalid", 0);
        AssertEntryIssue(
            prefixOptions,
            invalidPrefix,
            CookieErrorCode.PrefixViolation);

        StoredCookie publicSuffix = BasicCookie("public-suffix", 0);
        publicSuffix.Domain = "test";
        AssertEntryIssue(
            Options(),
            publicSuffix,
            CookieErrorCode.PublicSuffix);

        CookieJarOptions blockedOptions = Options();
        blockedOptions.BlockedDomains = ["example.test"];
        AssertEntryIssue(
            blockedOptions,
            BasicCookie("blocked", 0),
            CookieErrorCode.PolicyRejection);

        StoredCookie expired = BasicCookie("expired", 0);
        expired.HasExpires = true;
        expired.Persistent = true;
        expired.Expires = EvaluationTime;
        AssertEntryIssue(
            Options(),
            expired,
            CookieErrorCode.Expired,
            CookieSetStatus.Ignored);

        CookieJarOptions quotaOptions = Options();
        quotaOptions.Limits.MaximumCookies = 0;
        quotaOptions.Limits.MaximumCookiesPerDomain = 0;
        CookieImportResult quota = ImportSingle(
            quotaOptions,
            BasicCookie("quota", 0));
        Assert.That(quota.Success, Is.True);
        Assert.That(quota.Results.Single().Status,
            Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(quota.Results.Single().Cookie, Is.Null);
        Assert.That(quota.Results.Single().Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
        Assert.That(quota.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(CookieErrorCode.QuotaEviction));
    }

    [Test]
    public void FatalImportIssuesLeaveLiveStateUntouched()
    {
        MemoryCookieStore store = new MemoryCookieStore();
        store.ApplyStoredCookie(BasicCookie("live", 0));
        ICookieJar jar = Jar(Options(), store);
        StoredCookie first = BasicCookie("first", 0);
        StoredCookie duplicate = BasicCookie("first", 1);
        CookieJarSnapshot duplicateIdentity = new CookieJarSnapshot
        {
            Cookies = [first, duplicate],
            NextSequenceNumber = 2
        };
        CookieJarSnapshot unsupported = new CookieJarSnapshot
        {
            Version = CookieDefaults.SnapshotVersion + 1
        };

        CookieImportResult invalid = jar.ImportJarSnapshot(
            null!,
            CookieImportMode.Replace);
        CookieImportResult version = jar.ImportJarSnapshot(
            unsupported,
            CookieImportMode.Replace);
        CookieImportResult identity = jar.ImportJarSnapshot(
            duplicateIdentity,
            CookieImportMode.Replace);
        CookieImportResult mode = jar.ImportJarSnapshot(
            new CookieJarSnapshot(),
            (CookieImportMode)99);

        AssertFatalImport(invalid, CookieErrorCode.InvalidSnapshot);
        AssertFatalImport(version, CookieErrorCode.UnsupportedSnapshotVersion);
        AssertFatalImport(identity, CookieErrorCode.DuplicateIdentity);
        AssertFatalImport(mode, CookieErrorCode.InvalidConfiguration);
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "live" }));
    }

    static CookieJarOptions Options(
        CookieComplianceMode mode = CookieComplianceMode.Rfc6265Bis)
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = mode,
            RequirePublicSuffixList = true
        };
        options.ParseOptions.ComplianceMode = mode;
        options.SerializationOptions.ComplianceMode = mode;
        return options;
    }

    static ICookieJar Jar(CookieJarOptions options, ICookieStore store)
    {
        SnapshotClock clock = new SnapshotClock(EvaluationTime);
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            new SnapshotConformancePublicSuffixList(),
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

    static ICookieJar Jar(CookieJarOptions options)
    {
        return Jar(options, new MemoryCookieStore());
    }

    static CookieImportResult ImportSingle(
        CookieJarOptions options,
        StoredCookie cookie)
    {
        cookie.SequenceNumber = 0;
        return Jar(options).ImportJarSnapshot(
            new CookieJarSnapshot
            {
                Cookies = [cookie],
                NextSequenceNumber = 1
            },
            CookieImportMode.Replace);
    }

    static void AssertEntryIssue(
        CookieJarOptions options,
        StoredCookie cookie,
        CookieErrorCode expected,
        CookieSetStatus status = CookieSetStatus.Rejected)
    {
        CookieImportResult result = ImportSingle(options, cookie);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Results.Single().Status, Is.EqualTo(status));
        Assert.That(result.Results.Single().ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Results.Single().Issues.Select(issue => issue.ErrorCode),
            Does.Contain(expected));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Does.Contain(expected));
    }

    static void AssertFatalImport(
        CookieImportResult result,
        CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Results, Is.Empty);
        Assert.That(result.Issues.Single().ErrorCode, Is.EqualTo(expected));
    }

    static void SetNameValueLimit(CookieJarOptions options, int limit)
    {
        options.Limits.MaximumNameValueOctets = limit;
        options.ParseOptions.Limits.MaximumNameValueOctets = limit;
        options.SerializationOptions.Limits.MaximumNameValueOctets = limit;
        options.NetscapeFileOptions.Limits.MaximumNameValueOctets = limit;
    }

    static void SetAttributeLimit(CookieJarOptions options, int limit)
    {
        options.Limits.MaximumAttributeValueOctets = limit;
        options.ParseOptions.Limits.MaximumAttributeValueOctets = limit;
        options.SerializationOptions.Limits.MaximumAttributeValueOctets = limit;
        options.NetscapeFileOptions.Limits.MaximumAttributeValueOctets = limit;
    }

    static MemoryCookieStore SeededStore(params StoredCookie[] cookies)
    {
        MemoryCookieStore store = new MemoryCookieStore();
        store.ReplaceStoreSnapshot(new CookieJarSnapshot
        {
            Cookies = CookieGraphCloner.CloneStoredCookies(cookies),
            NextSequenceNumber = 2
        });
        return store;
    }

    static StoredCookie BasicCookie(string name, long sequence)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "example.test",
            Path = "/",
            CreationTime = EvaluationTime.AddDays(-2),
            LastAccessTime = EvaluationTime.AddDays(-1),
            SequenceNumber = sequence
        };
    }

    static StoredCookie CompleteCookie()
    {
        return new StoredCookie
        {
            Name = "complete",
            Value = "quoted value",
            Domain = "example.test",
            Path = "/account",
            HasExpires = true,
            Expires = EvaluationTime.AddDays(30),
            CreationTime = EvaluationTime.AddDays(-20),
            LastAccessTime = EvaluationTime.AddDays(-1),
            Persistent = true,
            HostOnly = true,
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict,
            HasPartitionKey = true,
            PartitionKey = "https://top.example",
            IsValueQuoted = true,
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "High"
                },
                new CookieAttribute
                {
                    Name = "Flag",
                    HasValue = false,
                    Value = ""
                }
            ],
            SequenceNumber = 7
        };
    }

    static void AssertStoredCookie(StoredCookie actual, StoredCookie expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.Domain, Is.EqualTo(expected.Domain));
            Assert.That(actual.Path, Is.EqualTo(expected.Path));
            Assert.That(actual.HasExpires, Is.EqualTo(expected.HasExpires));
            Assert.That(actual.Expires, Is.EqualTo(expected.Expires));
            Assert.That(actual.CreationTime, Is.EqualTo(expected.CreationTime));
            Assert.That(actual.LastAccessTime, Is.EqualTo(expected.LastAccessTime));
            Assert.That(actual.Persistent, Is.EqualTo(expected.Persistent));
            Assert.That(actual.HostOnly, Is.EqualTo(expected.HostOnly));
            Assert.That(actual.Secure, Is.EqualTo(expected.Secure));
            Assert.That(actual.HttpOnly, Is.EqualTo(expected.HttpOnly));
            Assert.That(actual.HasSameSite, Is.EqualTo(expected.HasSameSite));
            Assert.That(actual.SameSiteMode, Is.EqualTo(expected.SameSiteMode));
            Assert.That(actual.HasPartitionKey, Is.EqualTo(expected.HasPartitionKey));
            Assert.That(actual.PartitionKey, Is.EqualTo(expected.PartitionKey));
            Assert.That(actual.IsValueQuoted, Is.EqualTo(expected.IsValueQuoted));
            Assert.That(actual.SequenceNumber, Is.EqualTo(expected.SequenceNumber));
            Assert.That(actual.Extensions.Select(extension => extension.Name),
                Is.EqualTo(expected.Extensions.Select(extension => extension.Name)));
            Assert.That(actual.Extensions.Select(extension => extension.HasValue),
                Is.EqualTo(expected.Extensions.Select(extension => extension.HasValue)));
            Assert.That(actual.Extensions.Select(extension => extension.Value),
                Is.EqualTo(expected.Extensions.Select(extension => extension.Value)));
        });
    }
}

public class SnapshotClock : ICookieClock
{
    public DateTimeOffset Value;

    public SnapshotClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class SnapshotConformancePublicSuffixList : IPublicSuffixList
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
