namespace cookies.Tests;

public class GeneratedMalformedInputTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2041, 9, 10, 11, 12, 13, TimeSpan.Zero);

    static readonly string[] BoundaryTokens =
    [
        "\0",
        "\u0001",
        "\u0008",
        "\r",
        "\n",
        "\u001f",
        "\u007f",
        "\u0085",
        "é",
        "中",
        "😀",
        "e\u0301",
        "\u202e",
        "\ud800",
        "\udc00"
    ];

    [Test]
    public void GeneratedSetCookieInputsAreStableSafeAndNeverPartiallyStored()
    {
        CookieJarOptions options = Options();
        MemoryCookieStore store = SeededStore();
        ICookieJar jar = Jar(options, store);
        ICookieParser parser = new CookieParser();
        string before = StoreFingerprint(store);

        foreach (string input in GenerateMalformedSetCookieInputs())
        {
            SetCookieParseResult first = parser.TryParseSetCookie(
                input, options.ParseOptions);
            SetCookieParseResult second = parser.TryParseSetCookie(
                input, options.ParseOptions);

            Assert.That(SetCookieFingerprint(second),
                Is.EqualTo(SetCookieFingerprint(first)));
            AssertSafeIssues(first.Issues);
            AssertSafeSetCookie(first.Cookie, InputKey(input));

            CookieSetResult stored = jar.StoreSetCookieHeader(
                input,
                SetContext(options));

            Assert.That(stored.Status, Is.EqualTo(CookieSetStatus.Rejected));
            AssertSafeIssues(stored.Issues);
            Assert.That(StoreFingerprint(store), Is.EqualTo(before));
        }
    }

    [Test]
    public void GeneratedCookieFieldsAreStableSafeAndDropFatalPartialOutput()
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };

        foreach (string input in GenerateMalformedCookieFields())
        {
            string[] fields = ["before=1", input, "after=2"];
            CookieHeaderParseResult first = parser.TryParseCookieFields(
                fields, options);
            CookieHeaderParseResult second = parser.TryParseCookieFields(
                fields, options);

            Assert.That(CookieHeaderFingerprint(second),
                Is.EqualTo(CookieHeaderFingerprint(first)));
            AssertSafeIssues(first.Issues);
            AssertSafePairs(first.Pairs);

            if (first.ErrorCode == CookieErrorCode.HeaderInjection ||
                first.ErrorCode == CookieErrorCode.MalformedSyntax)
            {
                Assert.That(first.Success, Is.False);
                Assert.That(first.Pairs, Is.Empty);
            }
        }
    }

    [Test]
    public void GeneratedSnapshotInputsAreStableSafeAndNeverMutateTheLiveJar()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        MemoryCookieStore store = SeededStore();
        ICookieJar jar = Jar(Options(), store);
        string valid = codec.SerializeJarSnapshot(SnapshotCookie("snapshot"));
        string before = SnapshotFingerprint(jar.ExportJarSnapshot());

        foreach (string input in GenerateMalformedSnapshots(valid))
        {
            CookieSnapshotParseResult first = codec.TryParseJarSnapshot(input);
            CookieSnapshotParseResult second = codec.TryParseJarSnapshot(input);

            Assert.That(CookieSnapshotFingerprint(second),
                Is.EqualTo(CookieSnapshotFingerprint(first)));
            AssertSafeIssues(first.Issues);
            if (first.Success)
            {
                Assert.That(first.Snapshot, Is.Not.Null);
                AssertSafeStoredCookies(
                    first.Snapshot!.Cookies,
                    InputKey(input));
                CookieJarSnapshot parsed = codec.ParseJarSnapshot(input);
                Assert.That(SnapshotFingerprint(parsed),
                    Is.EqualTo(SnapshotFingerprint(first.Snapshot)));
            }
            else
            {
                Assert.That(first.Snapshot, Is.Null);
                CookieSnapshotException exception =
                    Assert.Throws<CookieSnapshotException>(() =>
                        codec.ParseJarSnapshot(input))!;
                AssertSafeText(exception.Message);
            }

            Assert.That(SnapshotFingerprint(jar.ExportJarSnapshot()),
                Is.EqualTo(before));
        }
    }

    [Test]
    public void GeneratedNetscapeInputsAreStableAndNeverReflectControls()
    {
        INetscapeCookieFileCodec codec = new NetscapeCookieFileCodec(
            new GeneratedInputClock(EvaluationTime));
        MemoryCookieStore store = SeededStore();
        string before = StoreFingerprint(store);

        foreach (string input in GenerateNetscapeFiles())
        {
            NetscapeCookieFileParseResult first =
                codec.TryParseNetscapeCookieFile(
                    input, new NetscapeCookieFileOptions());
            NetscapeCookieFileParseResult second =
                codec.TryParseNetscapeCookieFile(
                    input, new NetscapeCookieFileOptions());

            Assert.That(NetscapeFingerprint(second),
                Is.EqualTo(NetscapeFingerprint(first)));
            AssertSafeIssues(first.Issues);
            AssertSafeStoredCookies(first.Cookies, InputKey(input));
            Assert.That(StoreFingerprint(store), Is.EqualTo(before));
        }
    }

    static string[] GenerateMalformedSetCookieInputs()
    {
        global::System.Collections.Generic.List<string> inputs =
        [
            "",
            "=",
            ";",
            "name=\"unterminated",
            "name=value\rignored",
            "name=value\nignored"
        ];
        foreach (string token in BoundaryTokens)
        {
            inputs.Add("na" + token + "me=value");
            inputs.Add("name=va" + token + "lue");
        }

        return inputs.Distinct().ToArray();
    }

    static string[] GenerateMalformedCookieFields()
    {
        global::System.Collections.Generic.List<string> fields =
        [
            "",
            "missing",
            "=value",
            "name=\"unterminated",
            "one=1;;two=2"
        ];
        foreach (string token in BoundaryTokens)
        {
            fields.Add("na" + token + "me=value");
            fields.Add("name=va" + token + "lue");
        }

        return fields.Distinct().ToArray();
    }

    static string[] GenerateMalformedSnapshots(string valid)
    {
        global::System.Collections.Generic.List<string> snapshots =
        [
            "",
            "{",
            "[]",
            valid + " trailing",
            valid.Replace("\"version\":1", "\"version\":null"),
            valid.Replace("\"version\":1", "\"version\":1,\"version\":1"),
            valid.Replace("\"cookies\":[", "\"cookies\":{")
        ];
        foreach (string token in BoundaryTokens)
        {
            snapshots.Add("{" + token + "}");
            string encoded = global::System.Text.Json.JsonSerializer.Serialize(
                "snap" + token + "shot");
            snapshots.Add(valid.Replace("\"snapshot\"", encoded));
        }

        return snapshots.Distinct().ToArray();
    }

    static string[] GenerateNetscapeFiles()
    {
        global::System.Collections.Generic.List<string> files =
        [
            "",
            "not-a-cookie-line",
            NetscapeLine("before", "1") + "\nmissing\tfields\n" +
                NetscapeLine("after", "2"),
            NetscapeLine("before", "1") + "\rbroken\n" +
                NetscapeLine("after", "2")
        ];
        foreach (string token in BoundaryTokens)
        {
            files.Add(
                NetscapeLine("before", "1") + "\n" +
                NetscapeLine("na" + token + "me", "value") + "\n" +
                NetscapeLine("after", "2"));
            files.Add(
                NetscapeLine("before", "1") + "\n" +
                NetscapeLine("name", "va" + token + "lue") + "\n" +
                NetscapeLine("after", "2"));
        }

        return files.Distinct().ToArray();
    }

    static string NetscapeLine(string name, string value)
    {
        return "example.test\tFALSE\t/\tFALSE\t0\t" + name + "\t" + value;
    }

    static CookieJarOptions Options()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            RequirePublicSuffixList = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode =
            CookieComplianceMode.Rfc6265Bis;
        return options;
    }

    static ICookieJar Jar(CookieJarOptions options, ICookieStore store)
    {
        GeneratedInputClock clock = new GeneratedInputClock(EvaluationTime);
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            new GeneratedInputPublicSuffixList(),
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

    static CookieSetContext SetContext(CookieJarOptions options)
    {
        return new CookieSetContext
        {
            Origin = new Uri("https://example.test/path"),
            Options = options,
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://example.test"
        };
    }

    static MemoryCookieStore SeededStore()
    {
        MemoryCookieStore store = new MemoryCookieStore();
        store.ReplaceStoreSnapshot(SnapshotCookie("live"));
        return store;
    }

    static CookieJarSnapshot SnapshotCookie(string name)
    {
        return new CookieJarSnapshot
        {
            Cookies =
            [
                new StoredCookie
                {
                    Name = name,
                    Value = "value",
                    Domain = "example.test",
                    Path = "/",
                    CreationTime = EvaluationTime,
                    LastAccessTime = EvaluationTime,
                    HostOnly = true,
                    SequenceNumber = 0
                }
            ],
            NextSequenceNumber = 1
        };
    }

    static string SetCookieFingerprint(SetCookieParseResult result)
    {
        string cookie = result.Cookie == null
            ? "null"
            : result.Cookie.Name + ":" + result.Cookie.Value + ":" +
                result.Cookie.Domain + ":" + result.Cookie.Path;
        return result.Success + "|" + (int)result.ErrorCode + "|" + cookie +
            "|" + IssueFingerprint(result.Issues);
    }

    static string CookieHeaderFingerprint(CookieHeaderParseResult result)
    {
        string pairs = string.Join(",", result.Pairs.Select(pair =>
            pair.Name + "=" + pair.Value + ":" + pair.IsQuoted));
        return result.Success + "|" + (int)result.ErrorCode + "|" + pairs +
            "|" + IssueFingerprint(result.Issues);
    }

    static string CookieSnapshotFingerprint(CookieSnapshotParseResult result)
    {
        string snapshot = result.Snapshot == null
            ? "null"
            : SnapshotFingerprint(result.Snapshot);
        return result.Success + "|" + (int)result.ErrorCode + "|" + snapshot +
            "|" + IssueFingerprint(result.Issues);
    }

    static string NetscapeFingerprint(NetscapeCookieFileParseResult result)
    {
        string cookies = string.Join(",", result.Cookies.Select(cookie =>
            cookie.Name + "=" + cookie.Value + ":" + cookie.Domain +
                ":" + cookie.Path + ":" + cookie.SequenceNumber));
        return result.Success + "|" + (int)result.ErrorCode + "|" + cookies +
            "|" + IssueFingerprint(result.Issues);
    }

    static string IssueFingerprint(CookieIssue[] issues)
    {
        return string.Join(",", issues.Select(issue =>
            (int)issue.ErrorCode + ":" + (int)issue.Severity + ":" +
                issue.HasOffset + ":" + issue.Offset + ":" + issue.Length +
                ":" + issue.SafeFragment));
    }

    static string StoreFingerprint(ICookieStore store)
    {
        return SnapshotFingerprint(store.CaptureStoreSnapshot());
    }

    static string SnapshotFingerprint(CookieJarSnapshot snapshot)
    {
        return snapshot.Version + "|" + snapshot.NextSequenceNumber + "|" +
            string.Join(",", snapshot.Cookies.Select(cookie =>
                cookie.SequenceNumber + ":" + cookie.Name + "=" + cookie.Value +
                ":" + cookie.Domain + ":" + cookie.Path));
    }

    static void AssertSafeIssues(CookieIssue[] issues)
    {
        foreach (CookieIssue issue in issues)
        {
            Assert.That(issue.SafeFragment.Length, Is.LessThanOrEqualTo(32));
            AssertSafeText(issue.SafeFragment);
        }
    }

    static void AssertSafeSetCookie(SetCookie? cookie, string inputKey)
    {
        if (cookie == null)
        {
            return;
        }

        AssertSafeText(cookie.Name, inputKey);
        AssertSafeText(cookie.Value, inputKey);
        AssertSafeText(cookie.Domain, inputKey);
        AssertSafeText(cookie.Path, inputKey);
        foreach (CookieAttribute extension in cookie.Extensions)
        {
            AssertSafeText(extension.Name, inputKey);
            AssertSafeText(extension.Value, inputKey);
        }
    }

    static void AssertSafePairs(CookiePair[] pairs)
    {
        foreach (CookiePair pair in pairs)
        {
            AssertSafeText(pair.Name);
            AssertSafeText(pair.Value);
        }
    }

    static void AssertSafeStoredCookies(
        StoredCookie[] cookies,
        string inputKey)
    {
        foreach (StoredCookie cookie in cookies)
        {
            AssertSafeText(cookie.Name, inputKey);
            AssertSafeText(cookie.Value, inputKey);
            AssertSafeText(cookie.Domain, inputKey);
            AssertSafeText(cookie.Path, inputKey);
        }
    }

    static void AssertSafeText(string value)
    {
        Assert.That(value.Any(character => char.IsControl(character)), Is.False);
    }

    static void AssertSafeText(string value, string inputKey)
    {
        Assert.That(
            value.Any(character => char.IsControl(character)),
            Is.False,
            inputKey);
    }

    static string InputKey(string input)
    {
        return global::System.Convert.ToHexString(
            global::System.Text.Encoding.Unicode.GetBytes(input));
    }
}

public class GeneratedInputClock : ICookieClock
{
    public DateTimeOffset Value;

    public GeneratedInputClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class GeneratedInputPublicSuffixList : IPublicSuffixList
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
