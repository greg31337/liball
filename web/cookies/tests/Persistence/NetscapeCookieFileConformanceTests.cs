namespace cookies.Tests;

public class NetscapeCookieFileConformanceTests
{
    static readonly DateTimeOffset Now = new DateTimeOffset(
        2030,
        1,
        1,
        0,
        0,
        0,
        TimeSpan.Zero);

    [Test]
    public void CurlAndMozillaBooleanTokensAreExactAndCommentsRemainInert()
    {
        INetscapeCookieFileCodec codec = Codec();
        string text =
            "# Netscape HTTP Cookie File\r\n" +
            "   # Mozilla-style comment\r\n" +
            "\t \r\n" +
            ".Example.COM\tTRUE\t/one\tTRUE\t1893456001\tdomain-secure\t1\r\n" +
            "example.com\tTRUE\t/two\tFALSE\t0\tdomain-plain\t2\r\n" +
            "host.example\tFALSE\t/three\tTRUE\t0\thost-secure\t3\r\n" +
            "#HttpOnly_host.example\tFALSE\t/four\tFALSE\t0\thost-http-only\t4\r\n" +
            "example.com\ttrue\t/\tFALSE\t0\tlower-domain\t5\r\n" +
            "example.com\tFALSE\t/\tfalse\t0\tlower-secure\t6";

        NetscapeCookieFileParseResult result = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedNetscapeLine));
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[]
            {
                "domain-secure",
                "domain-plain",
                "host-secure",
                "host-http-only"
            }));
        Assert.That(result.Cookies.Select(cookie => cookie.HostOnly),
            Is.EqualTo(new[] { false, false, true, true }));
        Assert.That(result.Cookies.Select(cookie => cookie.Secure),
            Is.EqualTo(new[] { true, false, true, false }));
        Assert.That(result.Cookies.Select(cookie => cookie.HttpOnly),
            Is.EqualTo(new[] { false, false, false, true }));
        Assert.That(result.Cookies[0].Domain, Is.EqualTo("example.com"));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.MalformedNetscapeLine),
            Is.EqualTo(2));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.LossyImport),
            Is.EqualTo(4));
    }

    [Test]
    public void EmptyZeroBoundaryFutureAndMaximumEpochValuesHaveStableMeaning()
    {
        INetscapeCookieFileCodec codec = Codec();
        NetscapeCookieFileOptions includeExpired = new NetscapeCookieFileOptions
        {
            ImportExpiredCookies = true
        };
        string text =
            Line("empty", "") + "\n" +
            Line("zero", "0") + "\n" +
            Line("epoch-one", "1") + "\n" +
            Line("equal-now", "1893456000") + "\n" +
            Line("future", "1893456001") + "\n" +
            Line("maximum", "253402300799");

        NetscapeCookieFileParseResult all = codec.TryParseNetscapeCookieFile(
            text,
            includeExpired);
        NetscapeCookieFileParseResult defaults = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());

        Assert.That(all.Success, Is.True);
        Assert.That(all.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[]
            {
                "empty",
                "zero",
                "epoch-one",
                "equal-now",
                "future",
                "maximum"
            }));
        Assert.That(all.Cookies.Take(2).Select(cookie => cookie.HasExpires),
            Is.All.False);
        Assert.That(all.Cookies.Take(2).Select(cookie => cookie.Persistent),
            Is.All.False);
        Assert.That(all.Cookies[2].Expires, Is.EqualTo(
            DateTimeOffset.FromUnixTimeSeconds(1)));
        Assert.That(all.Cookies[3].Expires, Is.EqualTo(Now));
        Assert.That(all.Cookies[4].Expires, Is.EqualTo(Now.AddSeconds(1)));
        Assert.That(all.Cookies[5].Expires, Is.EqualTo(
            DateTimeOffset.FromUnixTimeSeconds(253402300799L)));
        Assert.That(defaults.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "empty", "zero", "future", "maximum" }));
    }

    [TestCase("-1")]
    [TestCase("not-an-epoch")]
    [TestCase("253402300800")]
    [TestCase("9223372036854775808")]
    public void InvalidEpochFormsAreMalformedWithoutPartialCookieState(string epoch)
    {
        INetscapeCookieFileCodec codec = Codec();
        string text = Line("bad", epoch) + "\n" + Line("good", "0");

        NetscapeCookieFileParseResult result = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedNetscapeLine));
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "good" }));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Is.EqualTo(new[]
            {
                CookieErrorCode.MalformedNetscapeLine,
                CookieErrorCode.LossyImport
            }));
    }

    [TestCase(false, false, "future")]
    [TestCase(true, false, "session,future")]
    [TestCase(false, true, "expired,future")]
    [TestCase(true, true, "session,expired,future")]
    public void ExportInclusionOptionsCoverEverySessionExpiredCombination(
        bool includeSession,
        bool includeExpired,
        string expectedNames)
    {
        INetscapeCookieFileCodec codec = Codec();
        StoredCookie session = Cookie("session", 0);
        StoredCookie expired = Cookie("expired", 1);
        expired.HasExpires = true;
        expired.Persistent = true;
        expired.Expires = Now.AddSeconds(-1);
        StoredCookie future = Cookie("future", 2);
        future.HasExpires = true;
        future.Persistent = true;
        future.Expires = Now.AddSeconds(1);
        NetscapeCookieFileOptions options = new NetscapeCookieFileOptions
        {
            IncludeSessionCookies = includeSession,
            IncludeExpiredCookies = includeExpired
        };

        string text = codec.SerializeNetscapeCookieFile(
            [future, expired, session],
            options);
        NetscapeCookieFileOptions parseOptions = new NetscapeCookieFileOptions
        {
            ImportSessionCookies = true,
            ImportExpiredCookies = true
        };
        StoredCookie[] parsed = codec.ParseNetscapeCookieFile(text, parseOptions);

        Assert.That(parsed.Select(cookie => cookie.Name),
            Is.EqualTo(ExpectedNames(expectedNames)));
    }

    [TestCase(false, false, "future")]
    [TestCase(true, false, "session,future")]
    [TestCase(false, true, "expired,future")]
    [TestCase(true, true, "session,expired,future")]
    public void ImportInclusionOptionsCoverEverySessionExpiredCombination(
        bool importSession,
        bool importExpired,
        string expectedNames)
    {
        INetscapeCookieFileCodec codec = Codec();
        string text =
            Line("session", "0") + "\n" +
            Line("expired", "1") + "\n" +
            Line("future", "1893456001");
        NetscapeCookieFileOptions options = new NetscapeCookieFileOptions
        {
            ImportSessionCookies = importSession,
            ImportExpiredCookies = importExpired
        };

        NetscapeCookieFileParseResult result = codec.TryParseNetscapeCookieFile(
            text,
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(ExpectedNames(expectedNames)));
        Assert.That(result.Issues, Has.Length.EqualTo(3));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode),
            Is.All.EqualTo(CookieErrorCode.LossyImport));
    }

    [Test]
    public void RoundTripPreservesRepresentableStateAndMakesEveryLossExplicit()
    {
        INetscapeCookieFileCodec codec = Codec();
        StoredCookie source = Cookie("complete", 42);
        source.Value = "välue";
        source.Domain = "example.com";
        source.Path = "/account";
        source.HostOnly = false;
        source.Secure = true;
        source.HttpOnly = true;
        source.HasExpires = true;
        source.Persistent = true;
        source.Expires = Now.AddSeconds(1);
        source.CreationTime = Now.AddDays(-20);
        source.LastAccessTime = Now.AddDays(-1);
        source.HasSameSite = true;
        source.SameSiteMode = CookieSameSiteMode.Strict;
        source.HasPartitionKey = true;
        source.PartitionKey = "https://top.example";
        source.IsValueQuoted = true;
        source.Extensions =
        [
            new CookieAttribute
            {
                Name = "Priority",
                HasValue = true,
                Value = "High"
            }
        ];
        NetscapeCookieFileOptions exportOptions = new NetscapeCookieFileOptions
        {
            RejectPartitionedCookies = false
        };

        string text = codec.SerializeNetscapeCookieFile([source], exportOptions);
        NetscapeCookieFileParseResult result = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());
        StoredCookie roundTrip = result.Cookies.Single();

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.Name, Is.EqualTo(source.Name));
            Assert.That(roundTrip.Value, Is.EqualTo(source.Value));
            Assert.That(roundTrip.Domain, Is.EqualTo(source.Domain));
            Assert.That(roundTrip.Path, Is.EqualTo(source.Path));
            Assert.That(roundTrip.HasExpires, Is.EqualTo(source.HasExpires));
            Assert.That(roundTrip.Expires, Is.EqualTo(source.Expires));
            Assert.That(roundTrip.Persistent, Is.EqualTo(source.Persistent));
            Assert.That(roundTrip.HostOnly, Is.EqualTo(source.HostOnly));
            Assert.That(roundTrip.Secure, Is.EqualTo(source.Secure));
            Assert.That(roundTrip.HttpOnly, Is.EqualTo(source.HttpOnly));

            Assert.That(roundTrip.CreationTime, Is.EqualTo(Now));
            Assert.That(roundTrip.LastAccessTime, Is.EqualTo(Now));
            Assert.That(roundTrip.HasSameSite, Is.False);
            Assert.That(roundTrip.SameSiteMode,
                Is.EqualTo(CookieSameSiteMode.Default));
            Assert.That(roundTrip.HasPartitionKey, Is.False);
            Assert.That(roundTrip.PartitionKey, Is.Empty);
            Assert.That(roundTrip.IsValueQuoted, Is.False);
            Assert.That(roundTrip.Extensions, Is.Empty);
            Assert.That(roundTrip.SequenceNumber, Is.Zero);
        });
        Assert.That(result.Issues.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.LossyImport));
        Assert.That(result.Issues.Single().Severity,
            Is.EqualTo(CookieParseSeverity.Warning));
        Assert.That(source.CreationTime, Is.EqualTo(Now.AddDays(-20)));
        Assert.That(source.HasPartitionKey, Is.True);
        Assert.That(source.Extensions, Has.Length.EqualTo(1));
    }

    [Test]
    public void MalformedTabsControlsDomainsAndPathsAreHandledPerLine()
    {
        INetscapeCookieFileCodec codec = Codec();
        string text =
            "bad-fields\tFALSE\t/\tFALSE\t0\tmissing-value\n" +
            "example.test\tFALSE\t/\tFALSE\t0\textra\ttab\tfield\n" +
            "127.0.0.1\tTRUE\t/\tFALSE\t0\tip-domain\tvalue\n" +
            "not a host\tFALSE\t/\tFALSE\t0\tbad-host\tvalue\n" +
            "example.test\tFALSE\trelative\tFALSE\t0\tfallback\tvalue";

        NetscapeCookieFileParseResult result = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "fallback" }));
        Assert.That(result.Cookies.Single().Path, Is.EqualTo("/"));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.MalformedNetscapeLine),
            Is.EqualTo(4));
        Assert.That(result.Issues.Last().ErrorCode,
            Is.EqualTo(CookieErrorCode.LossyImport));
    }

    static INetscapeCookieFileCodec Codec()
    {
        return new NetscapeCookieFileCodec(
            new NetscapeConformanceClock(Now));
    }

    static string Line(string name, string epoch)
    {
        return "example.test\tFALSE\t/\tFALSE\t" + epoch + "\t" + name + "\tvalue";
    }

    static string[] ExpectedNames(string names)
    {
        if (names.Length == 0)
        {
            return [];
        }

        return names.Split(',');
    }

    static StoredCookie Cookie(string name, long sequence)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "example.test",
            Path = "/",
            HostOnly = true,
            CreationTime = Now,
            LastAccessTime = Now,
            SequenceNumber = sequence
        };
    }
}

public class NetscapeConformanceClock : ICookieClock
{
    public DateTimeOffset Value;

    public NetscapeConformanceClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}
